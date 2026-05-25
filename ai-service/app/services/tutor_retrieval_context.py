"""
Tutor retrieval query design and lesson-scoped context assembly scaffold.

Part 3 goal:
  - Convert a student tutor request into a narrow internal retrieval query.
  - Retrieve lesson-scoped chunks from derived artifacts.
  - Assemble a compact tutoring context package (retrieved chunks + lesson state).

Non-goals for this part:
  - Advanced reranking
  - Open-domain retrieval
  - Multi-turn memory retrieval
  - Provider-specific prompt tuning
"""
from __future__ import annotations

from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from enum import Enum
import re

from app.models.tutor import TutorContextSnapshot
from app.services.retrieval_pipeline import LessonScopedRetriever, RetrievalCandidate, RetrievalQuery


class TutorIntentHint(str, Enum):
    EXPLAIN = "explain"
    SIMPLIFY = "simplify"
    GIVE_EXAMPLE = "give-example"
    EXPLAIN_STEP_BY_STEP = "explain-step-by-step"
    CLARIFY_CONFUSION = "clarify-confusion"
    UNKNOWN = "unknown"


@dataclass(frozen=True, slots=True)
class TutorRetrievalQuery:
    """Internal query DTO for tutor retrieval."""

    lesson_id: str
    question_text: str
    intent_hint: TutorIntentHint
    top_k: int = 4
    section_scope: str | None = None
    topic_terms: tuple[str, ...] = ()
    allow_curriculum_expansion: bool = False
    chapter_neighbor_lesson_ids: tuple[str, ...] = ()


@dataclass(frozen=True, slots=True)
class TutorRetrievalItem:
    chunk_id: str
    text: str
    score: float
    source_scope: str
    block_title: str | None
    metadata: dict[str, str | int | None]


@dataclass(frozen=True, slots=True)
class TutorRagContext:
    """Compact context object to feed future tutor prompt assembly."""

    lesson_id: str
    lesson_title: str
    subject_name: str
    grade_name: str
    lesson_progress_status: str
    student_question: str
    intent_hint: TutorIntentHint
    items: tuple[TutorRetrievalItem, ...] = ()
    notes: tuple[str, ...] = ()


class TutorRetrievalStrategy(ABC):
    @abstractmethod
    def retrieve(self, query: TutorRetrievalQuery) -> list[TutorRetrievalItem]:
        ...


@dataclass(slots=True)
class LessonFirstRetrievalStrategy(TutorRetrievalStrategy):
    """
    Narrow retrieval strategy:
      1) Retrieve from current lesson only.
            2) If allowed and needed, expand ONLY to same-chapter neighboring lessons.
    """

    lesson_retriever: LessonScopedRetriever
    min_score: float = 0.0001

    def retrieve(self, query: TutorRetrievalQuery) -> list[TutorRetrievalItem]:
        candidates = self.lesson_retriever.retrieve(
            RetrievalQuery(
                query_text=query.question_text,
                lesson_id=query.lesson_id,
                top_k=max(query.top_k * 2, query.top_k),
            )
        )

        lesson_items = self._to_items(candidates, source_scope="lesson")
        lesson_items = [item for item in lesson_items if item.score >= self.min_score]
        lesson_items = self._apply_section_scope(lesson_items, query.section_scope)

        selected = lesson_items[: query.top_k]
        if selected:
            return selected

        if query.allow_curriculum_expansion:
            return self._retrieve_chapter_neighbors(query)
        return []

    def _to_items(
        self,
        candidates: list[RetrievalCandidate],
        *,
        source_scope: str,
    ) -> list[TutorRetrievalItem]:
        items: list[TutorRetrievalItem] = []
        for c in candidates:
            block_title_raw = c.metadata.get("block_title")
            block_title = str(block_title_raw) if block_title_raw is not None else None
            items.append(
                TutorRetrievalItem(
                    chunk_id=c.chunk_id,
                    text=c.text,
                    score=c.score,
                    source_scope=source_scope,
                    block_title=block_title,
                    metadata=dict(c.metadata),
                )
            )
        return items

    def _apply_section_scope(
        self,
        items: list[TutorRetrievalItem],
        section_scope: str | None,
    ) -> list[TutorRetrievalItem]:
        if not section_scope:
            return items
        target = section_scope.strip().lower()
        if not target:
            return items

        filtered = [
            item
            for item in items
            if item.block_title and target in item.block_title.lower()
        ]
        return filtered or items

    def _retrieve_chapter_neighbors(self, query: TutorRetrievalQuery) -> list[TutorRetrievalItem]:
        # Conservative expansion rule:
        #   - Only same-chapter neighbor lesson IDs explicitly supplied by caller.
        #   - No same-subject/same-grade broadening in this version.
        if not query.chapter_neighbor_lesson_ids:
            return []

        out: list[TutorRetrievalItem] = []
        per_neighbor_k = max(1, min(query.top_k, 2))

        for neighbor_lesson_id in query.chapter_neighbor_lesson_ids:
            if neighbor_lesson_id == query.lesson_id:
                continue

            candidates = self.lesson_retriever.retrieve(
                RetrievalQuery(
                    query_text=query.question_text,
                    lesson_id=neighbor_lesson_id,
                    top_k=per_neighbor_k,
                )
            )
            items = self._to_items(candidates, source_scope="chapter_neighbor")
            items = [item for item in items if item.score >= self.min_score]
            items = self._apply_section_scope(items, query.section_scope)
            out.extend(items)

            if len(out) >= query.top_k:
                break

        out.sort(key=lambda item: item.score, reverse=True)
        return out[: query.top_k]


@dataclass(slots=True)
class TutorRetrievalContextAssembler:
    """Builds TutorRetrievalQuery and assembles TutorRagContext."""

    strategy: TutorRetrievalStrategy
    max_items: int = 4

    def build_query(
        self,
        context: TutorContextSnapshot,
        student_message: str,
        *,
        section_scope: str | None = None,
        intent_hint: TutorIntentHint | None = None,
        allow_curriculum_expansion: bool = False,
        chapter_neighbor_lesson_ids: tuple[str, ...] = (),
    ) -> TutorRetrievalQuery:
        inferred = intent_hint or infer_intent_hint(student_message)
        topic_terms = tuple(_extract_topic_terms(student_message))

        return TutorRetrievalQuery(
            lesson_id=context.lesson_id,
            question_text=student_message.strip(),
            intent_hint=inferred,
            top_k=self.max_items,
            section_scope=section_scope,
            topic_terms=topic_terms,
            allow_curriculum_expansion=allow_curriculum_expansion,
            chapter_neighbor_lesson_ids=chapter_neighbor_lesson_ids,
        )

    def assemble_context(
        self,
        context: TutorContextSnapshot,
        student_message: str,
        *,
        section_scope: str | None = None,
        intent_hint: TutorIntentHint | None = None,
        allow_curriculum_expansion: bool = False,
        chapter_neighbor_lesson_ids: tuple[str, ...] = (),
    ) -> TutorRagContext:
        query = self.build_query(
            context,
            student_message,
            section_scope=section_scope,
            intent_hint=intent_hint,
            allow_curriculum_expansion=allow_curriculum_expansion,
            chapter_neighbor_lesson_ids=chapter_neighbor_lesson_ids,
        )

        items = self.strategy.retrieve(query)
        items = items[: self.max_items]

        notes: list[str] = []
        if not items:
            notes.append("No high-signal lesson chunk retrieved; keep response conservative.")
        if allow_curriculum_expansion and not any(i.source_scope != "lesson" for i in items):
            notes.append("Curriculum expansion was allowed but no close-scope artifacts were used.")
        if query.intent_hint == TutorIntentHint.SIMPLIFY:
            notes.append("Prefer simpler wording and fewer concepts.")
        elif query.intent_hint == TutorIntentHint.GIVE_EXAMPLE:
            notes.append("Prefer one anchored example before abstract explanation.")
        elif query.intent_hint == TutorIntentHint.EXPLAIN_STEP_BY_STEP:
            notes.append("Prefer numbered steps and sequential explanation.")
        elif query.intent_hint == TutorIntentHint.CLARIFY_CONFUSION:
            notes.append("Prefer direct clarification of the likely confusion point.")

        return TutorRagContext(
            lesson_id=context.lesson_id,
            lesson_title=context.lesson_title,
            subject_name=context.subject_name,
            grade_name=context.grade_name,
            lesson_progress_status=context.lesson_progress_status,
            student_question=student_message.strip(),
            intent_hint=query.intent_hint,
            items=tuple(items),
            notes=tuple(notes),
        )


def format_retrieval_context_block(rag: TutorRagContext) -> str:
    """Render a compact retrieval context block for future prompt assembly."""
    lines = [
        "RETRIEVAL CONTEXT",
        "-----------------",
        f"Lesson: {rag.lesson_title}",
        f"Subject: {rag.subject_name}",
        f"Grade: {rag.grade_name}",
        f"Progress: {rag.lesson_progress_status}",
        f"Intent: {rag.intent_hint.value}",
        f"Question: {rag.student_question}",
        "",
        "Retrieved lesson excerpts:",
    ]

    if not rag.items:
        lines.append("  - (No retrieval excerpts available)")
    else:
        for i, item in enumerate(rag.items, start=1):
            title = item.block_title or "Untitled section"
            lines.append(f"  {i}. [{title}] score={item.score:.3f}")
            lines.append(f"     {item.text}")

    if rag.notes:
        lines.append("")
        lines.append("Notes:")
        for note in rag.notes:
            lines.append(f"  - {note}")

    lines.append("-----------------")
    lines.append("END OF RETRIEVAL CONTEXT")
    return "\n".join(lines)


def infer_intent_hint(message: str) -> TutorIntentHint:
    m = message.lower()
    if any(k in m for k in ("step by step", "step-by-step", "steps", "walk me through")):
        return TutorIntentHint.EXPLAIN_STEP_BY_STEP
    if any(k in m for k in ("simpler", "simplify", "easier", "simple")):
        return TutorIntentHint.SIMPLIFY
    if any(k in m for k in ("example", "another example", "sample")):
        return TutorIntentHint.GIVE_EXAMPLE
    if any(k in m for k in ("clarify", "what do you mean", "not clear", "confused")):
        return TutorIntentHint.CLARIFY_CONFUSION
    if any(k in m for k in ("explain", "how", "why", "what is")):
        return TutorIntentHint.EXPLAIN
    return TutorIntentHint.UNKNOWN


def _extract_topic_terms(message: str) -> list[str]:
    tokens = re.findall(r"[a-zA-Z0-9_]{3,}", message.lower())
    stop = {
        "what", "when", "where", "which", "about", "please", "could", "would", "from",
        "with", "this", "that", "have", "into", "your", "explain", "simplify", "example",
    }
    terms: list[str] = []
    for token in tokens:
        if token in stop:
            continue
        if token not in terms:
            terms.append(token)
    return terms[:8]
