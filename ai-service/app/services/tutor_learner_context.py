"""
Tutor-facing learner context assembly and bounded personalization input design.

Phase 1, Section 13, Part 3.

PURPOSE
-------
Defines the boundary between:
  - Stored learner memory (TutorFacingLearnerContext, from Parts 1–2)
  - Tutor response behavior (behavioral teaching hints)

A private-teacher-like tutor should:
  - remember repeated difficulty areas and explain with extra care
  - notice recurring misconceptions and address them proactively
  - respect stated or observed explanation style preferences
  - adapt pacing signals gently
  - stay grounded in the current lesson (never dump the full learner history)
  - never over-claim or assert speculative personal traits about the student

LAYERED BOUNDARY
----------------
  Layer 1 — Learner memory store (LearnerMemorySlice, InMemoryLearnerMemoryStore)
              Written only by LearnerMemoryService. Holds all accumulated signals.

  Layer 2 — Tutor-facing context slice (TutorFacingLearnerContext)
              Produced by LearnerMemoryService.get_tutor_facing_context().
              Already caps: 3 weak topics, 2 misconceptions, excludes friction.
              Filtered for staleness (decay policy from Part 2).

  Layer 3 — Bounded personalization input (BoundedPersonalizationInput) ← THIS MODULE
              Assembled at request time from Layer 2 + current lesson context.
              Relevance-filtered (keyword overlap with lesson title).
              Translated into behavioral teaching hints.
              Further capped: MAX_TOPIC_HINTS topic-level hints, MAX_PERSONALIZATION_HINTS total.
              This is what enters the system prompt.

DESIGN CONSTRAINTS
------------------
- At most MAX_PERSONALIZATION_HINTS hints per request
- At most MAX_TOPIC_HINTS topic-level hints (weak topics + misconceptions combined)
- Style/pace hints are always included when present (subject-level, not topic-specific)
- Topic hints are sorted by relevance (keyword match with lesson title) — relevant first
- When MAX_TOPIC_HINTS forces exclusion, relevant topics win over non-relevant
- Hints are instructions to the tutor (HOW to teach), not raw memory labels
- All hint instructions use hedged language — no certainty claims
- No raw user identifiers in formatted output
- No diagnosis-like labels or personality claims
- Confidence is computed per-hint and influences instruction strength

RELEVANCE FILTER — Phase 1 limitations
---------------------------------------
Phase 1 uses keyword overlap between lesson title terms and topic labels.
Limitation: "Introduction to Fractions" extracts ["introduction", "fractions"].
If a weak topic is labeled "mixed numbers", it will not be keyword-matched —
even though mixed numbers are a fractions sub-topic. Both outcomes (include or
exclude) are acceptable in Phase 1 because:
  - If there is only one weak topic, it is always included regardless
  - If there are multiple, relevant ones are prioritised (not hard-excluded)
  - is_lesson_relevant=False is an informational flag — topics are not hard-blocked
Phase 3: replace with semantic similarity (embedding overlap between lesson content
and topic label) for meaningful relevance filtering.

SUBJECT FILTERING — Phase 1 note
----------------------------------
Phase 1 does NOT filter topic hints by subject_id because TutorContextSnapshot
carries subject_name (string) but WeakTopicRecord.subject_id is a UUID. Exact
matching is not possible without a lookup table.
Phase 3: add subject_id to TutorContextSnapshot for exact FK-based filtering.
Until then, all topics from all subjects may appear. In practice, Phase 1
students work in single-subject sessions, so cross-subject noise is minimal.

TEACHER GUIDE COMPATIBILITY
----------------------------
This module handles LEARNER memory signals (who the student is).
Teacher-guide content (how to teach the subject) is a separate concern.
If a teacher-guide block is added in Phase 3, its position in the prompt is:
  role → lesson_content → [teacher_guide] → [this personalization block] → guardrails

DEFERRED
--------
- Semantic relevance (embedding similarity between lesson content and topic labels)
- Subject-level filtering by subject_id UUID
- Confidence decay: LOW hints are currently included with softer language.
  Phase 3 may exclude LOW-confidence hints when slot pressure is high.
- Cross-session friction aggregation feeding into topic hints
- Personalization block version header for prompt A/B testing
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Literal

from app.models.learner_memory import (
    ExplanationStyleSignal,
    MIN_MISCONCEPTION_EVIDENCE,
    MIN_WEAK_TOPIC_EVIDENCE,
    MisconceptionRecord,
    PaceSignalRecord,
    TutorFacingLearnerContext,
    WeakTopicRecord,
)
from app.models.learner_memory_policy import (
    ConfidenceLevel,
    LearnerMemoryCategory,
    evidence_count_to_confidence,
)


# ── Bounded personalization constants ─────────────────────────────────────────

MAX_PERSONALIZATION_HINTS: int = 4
"""
Maximum number of behavioral hints that may enter the tutor prompt context.
Caps total personalization to prevent prompt bloat and over-reliance on memory.
Breakdown: up to 1 style + 1 pace + 2 topic-level = 4 total.
"""

MAX_TOPIC_HINTS: int = 2
"""
Maximum topic-level hints (weak topics + misconceptions combined).
Keeps topic personalization narrow and focused on the most important signals.
"""


# ── Stop words for topic-term extraction ─────────────────────────────────────

_STOP_WORDS: frozenset[str] = frozenset({
    "the", "a", "an", "and", "or", "of", "to", "in", "for", "with",
    "is", "are", "was", "were", "be", "been", "being", "on", "at",
    "by", "from", "that", "this", "which", "it", "its", "as", "how",
    "what", "when", "where", "why", "about", "into", "through", "during",
    "using", "use", "uses", "used", "an", "s",
})


# ── Hint types ────────────────────────────────────────────────────────────────

HintBehavior = Literal[
    "prefer_style",
    "prefer_pace",
    "support_weak_topic",
    "watch_for_misconception",
]
"""
The four hint behaviors that may enter the tutor personalization block.
Each maps to a distinct kind of teaching adjustment:
  prefer_style            — adjust explanation format/approach
  prefer_pace             — adjust explanation speed/depth
  support_weak_topic      — offer extra support/examples for a known difficulty area
  watch_for_misconception — be alert for a known factual misunderstanding
"""


@dataclass(frozen=True)
class PersonalizationHint:
    """
    A single behavioral teaching hint derived from learner memory.

    Hints are instructions to the tutor on HOW to adapt teaching — not raw
    memory labels or diagnostic claims. They are:
      - Confidence-aware (HIGH → stronger instruction, LOW → gentler suggestion)
      - Relevance-flagged (is_lesson_relevant marks keyword match with lesson title)
      - Hedged in language (observed tendencies, not certainties)

    The instruction field contains the ready-to-use text for the tutor prompt,
    already formatted and hedged. No further escaping is required.
    """

    behavior: HintBehavior
    instruction: str
    """The teaching instruction text for the prompt. Already formatted and hedged."""

    confidence: ConfidenceLevel
    source_category: LearnerMemoryCategory
    is_lesson_relevant: bool
    """
    True if this hint's topic/content has keyword overlap with the current
    lesson title. False means it is included as background context but may be
    less immediately actionable for this specific lesson.
    For style/pace hints, this is always True (subject-level, not topic-specific).
    """


@dataclass(frozen=True)
class BoundedPersonalizationInput:
    """
    Narrow, request-time personalization context assembled for a single tutor request.

    This is the authoritative boundary between learner memory and tutor behavior.

    Key invariants:
      - len(hints) <= MAX_PERSONALIZATION_HINTS
      - at most MAX_TOPIC_HINTS topic-level hints combined (support + misconception)
      - style and pace hints are always included when present
      - has_personalization == bool(hints)
      - no raw user identifiers appear in any hint instruction
    """

    user_id: str
    lesson_id: str
    subject_id: str
    """Subject name used as proxy for subject_id in Phase 1. See module docstring."""

    hints: tuple[PersonalizationHint, ...]
    has_personalization: bool


# ── Relevance helpers ─────────────────────────────────────────────────────────

def extract_topic_terms(lesson_title: str) -> list[str]:
    """
    Extract non-trivial keyword terms from a lesson title for relevance filtering.

    Phase 1: simple word tokenization with stop-word removal and punctuation stripping.
    Phase 3: replace with NLP tokenizer + stemming for better semantic coverage.

    Args:
        lesson_title: The current lesson's title string.

    Returns:
        Lowercase non-stop-word tokens. May be empty for very short or stop-word-only
        titles. Duplicates are preserved (unlikely in practice).

    Examples:
        "Introduction to Fractions" → ["introduction", "fractions"]
        "The Water Cycle" → ["water", "cycle"]
        "Addition and Subtraction" → ["addition", "subtraction"]
    """
    words = lesson_title.lower().split()
    return [
        cleaned
        for w in words
        if (cleaned := w.strip(".,!?:;()\"'")) and cleaned not in _STOP_WORDS
    ]


def is_lesson_relevant(text: str, topic_terms: list[str]) -> bool:
    """
    Check whether a topic label or misconception description has at least one
    term in common with the current lesson's topic terms.

    Permissive when topic_terms is empty: returns True (no filter applied).
    This preserves lesson-first intent while not hard-excluding important
    background signals when the lesson title doesn't provide usable terms.

    Phase 1 limitation: keyword substring match only. May miss semantically related
    terms (e.g. "mixed numbers" will not match terms extracted from "Fractions").
    Phase 3: use embedding similarity for meaningful relevance scoring.

    Args:
        text: The topic_label or misconception description to check.
        topic_terms: Lowercase keyword terms extracted from the lesson title.

    Returns:
        True if any term appears as a substring in text (case-insensitive),
        or if topic_terms is empty.
    """
    if not topic_terms:
        return True
    text_lower = text.lower()
    return any(term in text_lower for term in topic_terms)


# ── Assembler ─────────────────────────────────────────────────────────────────

class TutorLearnerContextAssembler:
    """
    Assembles a BoundedPersonalizationInput from the tutor-facing learner context
    slice, filtered and translated for the current lesson.

    This is the single place where:
      - stored learner memory meets current lesson context
      - raw memory facts are translated into behavioral teaching hints
      - relevance ordering is applied (relevant topics first)
      - the total hint count is capped at MAX_PERSONALIZATION_HINTS

    Assembly order (designed to be stable):
      1. Style hint (if present) — always first, subject-level
      2. Pace hint (if present) — always second, subject-level
      3. Topic-level hints (weak topics, then misconceptions) — relevance-sorted,
         capped at MAX_TOPIC_HINTS

    Thread-safety: TutorLearnerContextAssembler is stateless. A module-level
    singleton is used via assemble_tutor_learner_context().
    """

    def assemble(
        self,
        learner_ctx: TutorFacingLearnerContext,
        lesson_id: str,
        subject_name: str,
        topic_terms: list[str] | None = None,
    ) -> BoundedPersonalizationInput:
        """
        Assemble a bounded personalization input from a learner memory slice.

        Args:
            learner_ctx: Tutor-facing learner memory slice (already capped by
                         LearnerMemoryService — 3 weak topics, 2 misconceptions max).
            lesson_id: The current lesson identifier.
            subject_name: Current subject name (used as subject_id proxy in Phase 1).
            topic_terms: Lowercase keyword terms extracted from the lesson title.
                         Pass None or [] for permissive filtering (no keyword filter).

        Returns:
            BoundedPersonalizationInput with at most MAX_PERSONALIZATION_HINTS hints.
            has_personalization is False when hints is empty.
        """
        if not learner_ctx.has_memory:
            return BoundedPersonalizationInput(
                user_id=learner_ctx.user_id,
                lesson_id=lesson_id,
                subject_id=subject_name,
                hints=(),
                has_personalization=False,
            )

        terms = topic_terms or []
        hints: list[PersonalizationHint] = []

        # ── Subject-level signals: always included when present ───────────────
        # Style and pace are learner-wide preferences, not topic-specific.
        # They provide gentle behavioral guidance regardless of current lesson.
        if learner_ctx.explanation_style is not None:
            hints.append(self._make_style_hint(learner_ctx.explanation_style))

        if learner_ctx.pace_signal is not None:
            hints.append(self._make_pace_hint(learner_ctx.pace_signal))

        # ── Topic hints: relevance-sorted, capped ─────────────────────────────
        # Sort so that lesson-relevant topics appear first when there are more
        # topics than MAX_TOPIC_HINTS allows.
        sorted_weak = sorted(
            learner_ctx.weak_topics,
            key=lambda r: 0 if is_lesson_relevant(r.topic_label, terms) else 1,
        )
        sorted_misc = sorted(
            learner_ctx.observed_misconceptions,
            key=lambda r: 0 if is_lesson_relevant(r.description, terms) else 1,
        )

        topic_count = 0

        for record in sorted_weak:
            if topic_count >= MAX_TOPIC_HINTS or len(hints) >= MAX_PERSONALIZATION_HINTS:
                break
            relevant = is_lesson_relevant(record.topic_label, terms)
            hints.append(self._make_weak_topic_hint(record, is_relevant=relevant))
            topic_count += 1

        for record in sorted_misc:
            if topic_count >= MAX_TOPIC_HINTS or len(hints) >= MAX_PERSONALIZATION_HINTS:
                break
            relevant = is_lesson_relevant(record.description, terms)
            hints.append(self._make_misconception_hint(record, is_relevant=relevant))
            topic_count += 1

        return BoundedPersonalizationInput(
            user_id=learner_ctx.user_id,
            lesson_id=lesson_id,
            subject_id=subject_name,
            hints=tuple(hints),
            has_personalization=bool(hints),
        )

    # ── Private hint factories ────────────────────────────────────────────────

    def _make_style_hint(self, style: ExplanationStyleSignal) -> PersonalizationHint:
        _style_display: dict[str, str] = {
            "concise": "concise explanations (fewer words, direct answers)",
            "detailed": "detailed explanations (thorough, step-rich)",
            "example_first": "examples before theory (show it, then explain it)",
            "step_by_step": "step-by-step walkthroughs (one step at a time)",
        }
        style_text = _style_display.get(style.style, style.style)

        if style.confidence == "explicit":
            instruction = (
                f"Teaching style: Use {style_text}."
                " The student has explicitly stated this preference."
            )
            confidence = ConfidenceLevel.HIGH
        else:
            instruction = (
                f"Teaching style: Try {style_text}."
                " The student has repeatedly asked for this style (observed tendency)."
            )
            confidence = ConfidenceLevel.MODERATE

        return PersonalizationHint(
            behavior="prefer_style",
            instruction=instruction,
            confidence=confidence,
            source_category=LearnerMemoryCategory.EXPLANATION_STYLE,
            is_lesson_relevant=True,
        )

    def _make_pace_hint(self, pace: PaceSignalRecord) -> PersonalizationHint:
        _pace_instruction: dict[str, str] = {
            "needs_more_support": (
                "Pace: Provide slower, more guided explanations."
                " The student has indicated needing more support."
            ),
            "normal": (
                "Pace: Standard explanation pace appears comfortable for this student."
            ),
            "can_go_faster": (
                "Pace: The student may be ready for slightly more challenging material."
                " Avoid over-simplifying."
            ),
        }
        instruction = _pace_instruction.get(
            pace.signal,
            f"Pace signal: {pace.signal}.",
        )
        confidence = evidence_count_to_confidence(pace.evidence_count, min_threshold=1)
        return PersonalizationHint(
            behavior="prefer_pace",
            instruction=instruction,
            confidence=confidence,
            source_category=LearnerMemoryCategory.PACE_SIGNAL,
            is_lesson_relevant=True,
        )

    def _make_weak_topic_hint(
        self,
        record: WeakTopicRecord,
        *,
        is_relevant: bool,
    ) -> PersonalizationHint:
        confidence = evidence_count_to_confidence(
            record.evidence_count,
            min_threshold=MIN_WEAK_TOPIC_EVIDENCE,
        )
        times_str = f"{record.evidence_count} time{'s' if record.evidence_count != 1 else ''}"

        if confidence == ConfidenceLevel.HIGH:
            instruction = (
                f"This student has often shown difficulty with: '{record.topic_label}' ({times_str})."
                " Proactively offer extra examples or scaffolding if this topic comes up."
            )
        elif confidence == ConfidenceLevel.MODERATE:
            instruction = (
                f"This student has shown difficulty with: '{record.topic_label}' ({times_str})."
                " Offer additional examples or a step-by-step explanation if this topic arises."
            )
        else:
            instruction = (
                f"This student has recently shown difficulty with: '{record.topic_label}' ({times_str})."
                " Be attentive if this topic comes up — offer support if needed."
            )

        return PersonalizationHint(
            behavior="support_weak_topic",
            instruction=instruction,
            confidence=confidence,
            source_category=LearnerMemoryCategory.WEAK_TOPIC,
            is_lesson_relevant=is_relevant,
        )

    def _make_misconception_hint(
        self,
        record: MisconceptionRecord,
        *,
        is_relevant: bool,
    ) -> PersonalizationHint:
        confidence = evidence_count_to_confidence(
            record.evidence_count,
            min_threshold=MIN_MISCONCEPTION_EVIDENCE,
        )
        times_str = f"{record.evidence_count} time{'s' if record.evidence_count != 1 else ''}"

        if confidence == ConfidenceLevel.HIGH:
            instruction = (
                f"Known recurring misconception: '{record.description}' ({times_str})."
                " This is well-established — address it proactively if the concept appears."
                " Verify it is still active before assuming."
            )
        elif confidence == ConfidenceLevel.MODERATE:
            instruction = (
                f"Observed misconception: '{record.description}' ({times_str})."
                " Watch for this if the concept appears. Verify before assuming."
            )
        else:
            instruction = (
                f"Recent possible misconception: '{record.description}' ({times_str})."
                " Keep this in mind but verify whether it is still active."
            )

        return PersonalizationHint(
            behavior="watch_for_misconception",
            instruction=instruction,
            confidence=confidence,
            source_category=LearnerMemoryCategory.MISCONCEPTION,
            is_lesson_relevant=is_relevant,
        )


# ── Formatter ─────────────────────────────────────────────────────────────────

def format_personalization_hints_block(bpi: BoundedPersonalizationInput) -> str:
    """
    Format a BoundedPersonalizationInput as behavioral teaching instructions
    for injection into the tutor system prompt.

    Unlike format_learner_memory_block() (in learner_context_slice.py) which
    outputs an informational memory dump, this function formats hints as
    explicit, confidence-aware teaching instructions.

    Key constraints:
      - Returns empty string when has_personalization is False
      - Must not include raw user identifiers
      - Uses the 'LEARNER MEMORY' / 'END OF LEARNER MEMORY' header convention
        for compatibility with the existing prompt block structure and tests
      - All instructions are already hedged (from the hint factory methods)
      - One bullet point per hint — keeps the block compact
    """
    if not bpi.has_personalization:
        return ""

    lines: list[str] = [
        "LEARNER MEMORY (observed tendencies from past sessions)",
        "--------------------------------------------------------",
        "Note: These are observed educational tendencies — not certainties.",
        "      Adapt your explanations where relevant, but remain open to",
        "      different behaviour from this student in this session.",
        "",
    ]

    for hint in bpi.hints:
        lines.append(f"- {hint.instruction}")

    lines.extend([
        "",
        "--------------------------------------------------------",
        "END OF LEARNER MEMORY",
    ])

    return "\n".join(lines)


# ── Module-level assembler singleton and convenience function ─────────────────

_default_assembler = TutorLearnerContextAssembler()


def assemble_tutor_learner_context(
    learner_ctx: TutorFacingLearnerContext,
    lesson_id: str,
    subject_name: str,
    topic_terms: list[str] | None = None,
) -> BoundedPersonalizationInput:
    """
    Module-level convenience wrapper around the default assembler singleton.

    Suitable for use in tutor_prompts.build_system_prompt() and tests.
    The assembler is stateless — the singleton is safe to share.

    Args:
        learner_ctx: Tutor-facing learner memory slice.
        lesson_id: Current lesson identifier.
        subject_name: Current subject name (proxy for subject_id in Phase 1).
        topic_terms: Lesson title keywords for relevance filtering. Pass None or []
                     for permissive mode (no keyword filtering applied).

    Returns:
        BoundedPersonalizationInput with at most MAX_PERSONALIZATION_HINTS hints.
    """
    return _default_assembler.assemble(
        learner_ctx=learner_ctx,
        lesson_id=lesson_id,
        subject_name=subject_name,
        topic_terms=topic_terms,
    )
