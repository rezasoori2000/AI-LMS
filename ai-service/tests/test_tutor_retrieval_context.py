from __future__ import annotations

from app.models.tutor import TutorContextSnapshot
from app.services.retrieval_pipeline import (
    InMemoryRetrievalArtifactRepository,
    LessonIndexEvent,
    LessonIngestionInput,
    LessonRetrievalIndexer,
    LessonScopedRetriever,
)
from app.services.tutor_retrieval_context import (
    LessonFirstRetrievalStrategy,
    TutorIntentHint,
    TutorRetrievalContextAssembler,
    format_retrieval_context_block,
    infer_intent_hint,
)


def _snapshot() -> TutorContextSnapshot:
    return TutorContextSnapshot(
        student_profile_id="student-001",
        user_id="user-001",
        student_first_name="Alice",
        tenant_id="tenant-001",
        lesson_id="lesson-001",
        lesson_title="Introduction to Fractions",
        lesson_content="Fractions represent parts of a whole.",
        grade_name="Grade 4",
        subject_name="Mathematics",
        lesson_progress_status="InProgress",
        conversation_id="conv-001",
        history=[],
    )


async def _seed(repo: InMemoryRetrievalArtifactRepository) -> None:
    indexer = LessonRetrievalIndexer(repository=repo)
    content = (
        "## Numerator\n"
        "The numerator is the top number in a fraction.\n\n"
        "## Denominator\n"
        "The denominator is the bottom number in a fraction."
    )
    await indexer.handle_event(
        LessonIngestionInput(
            lesson_id="lesson-001",
            chapter_id="chapter-001",
            lesson_title="Introduction to Fractions",
            lesson_content=content,
            subject_name="Mathematics",
            grade_name="Grade 4",
            tenant_id="tenant-001",
            event=LessonIndexEvent.CREATED,
        )
    )


async def _seed_lesson(
    repo: InMemoryRetrievalArtifactRepository,
    *,
    lesson_id: str,
    chapter_id: str,
    content: str,
) -> None:
    indexer = LessonRetrievalIndexer(repository=repo)
    await indexer.handle_event(
        LessonIngestionInput(
            lesson_id=lesson_id,
            chapter_id=chapter_id,
            lesson_title=f"Lesson {lesson_id}",
            lesson_content=content,
            subject_name="Mathematics",
            grade_name="Grade 4",
            tenant_id="tenant-001",
            event=LessonIndexEvent.CREATED,
        )
    )


def test_infer_intent_hint_simplify() -> None:
    assert infer_intent_hint("Can you simplify this explanation?") == TutorIntentHint.SIMPLIFY


def test_build_query_keeps_lesson_scope() -> None:
    repo = InMemoryRetrievalArtifactRepository()
    strategy = LessonFirstRetrievalStrategy(LessonScopedRetriever(repository=repo))
    assembler = TutorRetrievalContextAssembler(strategy=strategy, max_items=3)

    q = assembler.build_query(_snapshot(), "What is numerator?", section_scope="Numerator")
    assert q.lesson_id == "lesson-001"
    assert q.top_k == 3
    assert q.section_scope == "Numerator"
    assert q.chapter_neighbor_lesson_ids == ()


async def test_assemble_context_selects_items_and_preserves_progress() -> None:
    repo = InMemoryRetrievalArtifactRepository()
    await _seed(repo)

    strategy = LessonFirstRetrievalStrategy(LessonScopedRetriever(repository=repo))
    assembler = TutorRetrievalContextAssembler(strategy=strategy, max_items=2)

    rag = assembler.assemble_context(_snapshot(), "Explain top number in fraction")

    assert rag.lesson_id == "lesson-001"
    assert rag.lesson_progress_status == "InProgress"
    assert len(rag.items) <= 2
    assert rag.items


async def test_section_scope_prefers_matching_block_when_available() -> None:
    repo = InMemoryRetrievalArtifactRepository()
    await _seed(repo)

    strategy = LessonFirstRetrievalStrategy(LessonScopedRetriever(repository=repo))
    assembler = TutorRetrievalContextAssembler(strategy=strategy, max_items=2)

    rag = assembler.assemble_context(
        _snapshot(),
        "What does denominator mean?",
        section_scope="Denominator",
    )

    assert rag.items
    assert any((item.block_title or "").lower().find("denominator") >= 0 for item in rag.items)


async def test_format_retrieval_context_block_contains_headers() -> None:
    repo = InMemoryRetrievalArtifactRepository()
    await _seed(repo)

    strategy = LessonFirstRetrievalStrategy(LessonScopedRetriever(repository=repo))
    assembler = TutorRetrievalContextAssembler(strategy=strategy, max_items=1)
    rag = assembler.assemble_context(_snapshot(), "Give an example")

    text = format_retrieval_context_block(rag)
    assert "RETRIEVAL CONTEXT" in text
    assert "Retrieved lesson excerpts:" in text
    assert "END OF RETRIEVAL CONTEXT" in text


async def test_expansion_uses_same_chapter_neighbors_only_when_primary_empty() -> None:
    repo = InMemoryRetrievalArtifactRepository()
    # Current lesson has no relevant terms for query
    await _seed_lesson(
        repo,
        lesson_id="lesson-001",
        chapter_id="chapter-001",
        content="## Intro\nThis lesson is about basic fraction definitions.",
    )
    # Neighbor lesson in same chapter has matching content
    await _seed_lesson(
        repo,
        lesson_id="lesson-002",
        chapter_id="chapter-001",
        content="## Example\nA worked example shows top number and bottom number.",
    )
    # Another lesson (pretend broader scope) not passed as neighbor should not be used
    await _seed_lesson(
        repo,
        lesson_id="lesson-099",
        chapter_id="chapter-999",
        content="## Broad\nUnrelated but keyword-rich top number explanation.",
    )

    strategy = LessonFirstRetrievalStrategy(LessonScopedRetriever(repository=repo))
    assembler = TutorRetrievalContextAssembler(strategy=strategy, max_items=2)

    rag = assembler.assemble_context(
        _snapshot(),
        "Explain top number",
        allow_curriculum_expansion=True,
        chapter_neighbor_lesson_ids=("lesson-002",),
    )

    assert rag.items
    assert all(item.source_scope in {"lesson", "chapter_neighbor"} for item in rag.items)
    assert any(item.source_scope == "chapter_neighbor" for item in rag.items)
    assert all(item.metadata.get("lesson_id") != "lesson-099" for item in rag.items)


async def test_expansion_without_neighbors_returns_empty_when_primary_empty() -> None:
    repo = InMemoryRetrievalArtifactRepository()
    await _seed_lesson(
        repo,
        lesson_id="lesson-001",
        chapter_id="chapter-001",
        content="## Intro\nFractions have numerator and denominator.",
    )

    strategy = LessonFirstRetrievalStrategy(LessonScopedRetriever(repository=repo))
    assembler = TutorRetrievalContextAssembler(strategy=strategy, max_items=2)

    rag = assembler.assemble_context(
        _snapshot(),
        "photosynthesis chloroplast",
        allow_curriculum_expansion=True,
        chapter_neighbor_lesson_ids=(),
    )

    assert rag.items == ()
