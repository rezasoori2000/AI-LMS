"""
Tests for tutor_learner_context.py — Phase 1, Section 13, Part 3.

Test groups:
  A. extract_topic_terms               — keyword extraction from lesson titles
  B. is_lesson_relevant                — relevance check helper
  C. PersonalizationHint / BoundedPersonalizationInput — frozen dataclass construction
  D. Assembler — no memory             — empty / has_memory=False cases
  E. Assembler — style and pace hints  — subject-level signal translation
  F. Assembler — topic hints           — weak topic + misconception with relevance
  G. Assembler — bounded counts        — MAX_PERSONALIZATION_HINTS / MAX_TOPIC_HINTS
  H. format_personalization_hints_block — output text assertions
  I. build_system_prompt integration   — end-to-end prompt content assertions

All tests use plain classes (no async).  pytest ≥ 7.4, Python 3.12.
"""
from __future__ import annotations

from dataclasses import FrozenInstanceError
from datetime import datetime, timezone

import pytest

from app.core.tutor_prompts import build_system_prompt
from app.models.learner_memory import (
    ExplanationStyleSignal,
    MIN_MISCONCEPTION_EVIDENCE,
    MIN_WEAK_TOPIC_EVIDENCE,
    MisconceptionRecord,
    PaceSignalRecord,
    TutorFacingLearnerContext,
    WeakTopicRecord,
)
from app.models.learner_memory_policy import ConfidenceLevel, LearnerMemoryCategory
from app.models.tutor import TutorContextSnapshot
from app.services.tutor_learner_context import (
    MAX_PERSONALIZATION_HINTS,
    MAX_TOPIC_HINTS,
    BoundedPersonalizationInput,
    PersonalizationHint,
    TutorLearnerContextAssembler,
    assemble_tutor_learner_context,
    extract_topic_terms,
    format_personalization_hints_block,
    is_lesson_relevant,
)


# ── Test helpers ──────────────────────────────────────────────────────────────

def _now() -> datetime:
    return datetime.now(tz=timezone.utc)


def _weak_topic(
    label: str = "fractions > mixed numbers",
    subject: str = "subj-1",
    count: int = MIN_WEAK_TOPIC_EVIDENCE,
) -> WeakTopicRecord:
    return WeakTopicRecord(
        topic_label=label,
        subject_id=subject,
        evidence_count=count,
        last_observed_at=_now(),
    )


def _misconception(
    desc: str = "confuses numerator with denominator",
    subject: str = "subj-1",
    count: int = MIN_MISCONCEPTION_EVIDENCE,
) -> MisconceptionRecord:
    return MisconceptionRecord(
        description=desc,
        subject_id=subject,
        evidence_count=count,
        last_observed_at=_now(),
    )


def _style(
    style: str = "example_first",
    confidence: str = "explicit",
) -> ExplanationStyleSignal:
    return ExplanationStyleSignal(
        style=style,  # type: ignore[arg-type]
        confidence=confidence,  # type: ignore[arg-type]
        observed_at=_now(),
    )


def _pace(signal: str = "needs_more_support", count: int = 2) -> PaceSignalRecord:
    return PaceSignalRecord(
        signal=signal,  # type: ignore[arg-type]
        evidence_count=count,
        last_observed_at=_now(),
    )


def _empty_ctx(user_id: str = "u1") -> TutorFacingLearnerContext:
    """A context with has_memory=False."""
    return TutorFacingLearnerContext(user_id=user_id, has_memory=False)


def _ctx_with(
    *,
    user_id: str = "u1",
    weak_topics: list[WeakTopicRecord] | None = None,
    misconceptions: list[MisconceptionRecord] | None = None,
    style: ExplanationStyleSignal | None = None,
    pace: PaceSignalRecord | None = None,
) -> TutorFacingLearnerContext:
    """Build a TutorFacingLearnerContext with has_memory=True and given fields."""
    return TutorFacingLearnerContext(
        user_id=user_id,
        has_memory=True,
        weak_topics=weak_topics or [],
        observed_misconceptions=misconceptions or [],
        explanation_style=style,
        pace_signal=pace,
    )


def _hint(
    behavior: str = "support_weak_topic",
    instruction: str = "some instruction",
    confidence: ConfidenceLevel = ConfidenceLevel.LOW,
    source: LearnerMemoryCategory = LearnerMemoryCategory.WEAK_TOPIC,
    is_relevant: bool = True,
) -> PersonalizationHint:
    return PersonalizationHint(
        behavior=behavior,  # type: ignore[arg-type]
        instruction=instruction,
        confidence=confidence,
        source_category=source,
        is_lesson_relevant=is_relevant,
    )


def _bpi(hints: tuple[PersonalizationHint, ...] = ()) -> BoundedPersonalizationInput:
    return BoundedPersonalizationInput(
        user_id="u1",
        lesson_id="lesson-001",
        subject_id="Mathematics",
        hints=hints,
        has_personalization=bool(hints),
    )


def _snapshot(**overrides: object) -> TutorContextSnapshot:
    base: dict[str, object] = {
        "student_profile_id": "prof-001",
        "user_id": "user-001",
        "student_first_name": "Alice",
        "lesson_id": "lesson-001",
        "lesson_title": "Introduction to Fractions",
        "lesson_content": "A fraction is a part of a whole.",
        "grade_name": "Grade 4",
        "subject_name": "Mathematics",
        "lesson_progress_status": "InProgress",
        "conversation_id": "conv-001",
    }
    base.update(overrides)
    return TutorContextSnapshot(**base)


# ── A. extract_topic_terms ─────────────────────────────────────────────────────

class TestExtractTopicTerms:

    def test_removes_stop_words(self) -> None:
        terms = extract_topic_terms("Introduction to Fractions")
        assert "to" not in terms
        assert "introduction" in terms
        assert "fractions" in terms

    def test_empty_title_returns_empty_list(self) -> None:
        assert extract_topic_terms("") == []

    def test_single_meaningful_word(self) -> None:
        assert extract_topic_terms("Photosynthesis") == ["photosynthesis"]

    def test_all_stop_words_returns_empty(self) -> None:
        # All tokens happen to be stop words
        result = extract_topic_terms("the a an and or of")
        assert result == []

    def test_multi_word_title(self) -> None:
        terms = extract_topic_terms("The Water Cycle")
        assert "the" not in terms
        assert "water" in terms
        assert "cycle" in terms

    def test_strips_punctuation(self) -> None:
        terms = extract_topic_terms("Fractions, Decimals, and Percentages!")
        assert "fractions" in terms
        assert "decimals" in terms
        assert "percentages" in terms
        # stop word "and" removed
        assert "and" not in terms

    def test_result_is_lowercase(self) -> None:
        terms = extract_topic_terms("CELL DIVISION")
        assert "cell" in terms
        assert "division" in terms


# ── B. is_lesson_relevant ─────────────────────────────────────────────────────

class TestIsLessonRelevant:

    def test_returns_true_when_term_found(self) -> None:
        assert is_lesson_relevant("fractions and mixed numbers", ["fractions"]) is True

    def test_returns_false_when_no_term_found(self) -> None:
        assert is_lesson_relevant("angles in geometry", ["fractions"]) is False

    def test_returns_true_when_topic_terms_empty(self) -> None:
        # Permissive fallback — empty terms list means no filter
        assert is_lesson_relevant("anything at all", []) is True

    def test_case_insensitive_match(self) -> None:
        assert is_lesson_relevant("Fractions Overview", ["fractions"]) is True

    def test_partial_substring_match(self) -> None:
        # "fraction" is a substring of "fractions"
        assert is_lesson_relevant("understanding fractions", ["fraction"]) is True

    def test_multiple_terms_any_match(self) -> None:
        # Only one of the terms needs to match
        assert is_lesson_relevant("photosynthesis basics", ["fractions", "photosynthesis"]) is True


# ── C. PersonalizationHint / BoundedPersonalizationInput ──────────────────────

class TestFrozenDataclasses:

    def test_personalization_hint_is_frozen(self) -> None:
        h = _hint()
        with pytest.raises(FrozenInstanceError):
            h.instruction = "mutated"  # type: ignore[misc]

    def test_bounded_personalization_input_is_frozen(self) -> None:
        bpi = _bpi()
        with pytest.raises(FrozenInstanceError):
            bpi.user_id = "hacked"  # type: ignore[misc]

    def test_has_personalization_true_when_hints_present(self) -> None:
        bpi = _bpi(hints=(_hint(),))
        assert bpi.has_personalization is True

    def test_has_personalization_false_when_no_hints(self) -> None:
        bpi = _bpi(hints=())
        assert bpi.has_personalization is False

    def test_hint_fields_correctly_assigned(self) -> None:
        h = PersonalizationHint(
            behavior="prefer_style",
            instruction="Use examples first.",
            confidence=ConfidenceLevel.HIGH,
            source_category=LearnerMemoryCategory.EXPLANATION_STYLE,
            is_lesson_relevant=True,
        )
        assert h.behavior == "prefer_style"
        assert h.instruction == "Use examples first."
        assert h.confidence == ConfidenceLevel.HIGH
        assert h.source_category == LearnerMemoryCategory.EXPLANATION_STYLE
        assert h.is_lesson_relevant is True


# ── D. Assembler — no memory ──────────────────────────────────────────────────

class TestAssemblerNoMemory:

    def test_returns_no_personalization_when_has_memory_false(self) -> None:
        ctx = _empty_ctx()
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Mathematics")
        assert result.has_personalization is False

    def test_returns_empty_hints_when_has_memory_false(self) -> None:
        ctx = _empty_ctx()
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Mathematics")
        assert result.hints == ()

    def test_user_id_and_lesson_id_preserved_when_no_memory(self) -> None:
        ctx = _empty_ctx(user_id="user-xyz")
        result = assemble_tutor_learner_context(ctx, "lesson-99", "Science")
        assert result.user_id == "user-xyz"
        assert result.lesson_id == "lesson-99"

    def test_subject_name_used_as_subject_id(self) -> None:
        ctx = _empty_ctx()
        result = assemble_tutor_learner_context(ctx, "lesson-1", "History")
        assert result.subject_id == "History"


# ── E. Assembler — style and pace hints ─────────────────────────────────────

class TestAssemblerStylePace:

    def test_style_hint_included_when_style_present(self) -> None:
        ctx = _ctx_with(style=_style("example_first", "explicit"))
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        behaviors = [h.behavior for h in result.hints]
        assert "prefer_style" in behaviors

    def test_pace_hint_included_when_pace_present(self) -> None:
        ctx = _ctx_with(pace=_pace("needs_more_support"))
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        behaviors = [h.behavior for h in result.hints]
        assert "prefer_pace" in behaviors

    def test_explicit_style_confidence_is_high(self) -> None:
        ctx = _ctx_with(style=_style("concise", "explicit"))
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        style_hints = [h for h in result.hints if h.behavior == "prefer_style"]
        assert len(style_hints) == 1
        assert style_hints[0].confidence == ConfidenceLevel.HIGH

    def test_inferred_style_confidence_is_moderate(self) -> None:
        ctx = _ctx_with(style=_style("detailed", "inferred_from_repeated_request"))
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        style_hints = [h for h in result.hints if h.behavior == "prefer_style"]
        assert len(style_hints) == 1
        assert style_hints[0].confidence == ConfidenceLevel.MODERATE

    def test_explicit_style_instruction_mentions_stated_preference(self) -> None:
        ctx = _ctx_with(style=_style("example_first", "explicit"))
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        style_hint = next(h for h in result.hints if h.behavior == "prefer_style")
        assert "stated this" in style_hint.instruction.lower() or "explicitly" in style_hint.instruction.lower()

    def test_inferred_style_instruction_mentions_observed_tendency(self) -> None:
        ctx = _ctx_with(style=_style("step_by_step", "inferred_from_repeated_request"))
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        style_hint = next(h for h in result.hints if h.behavior == "prefer_style")
        assert "repeatedly" in style_hint.instruction.lower() or "observed" in style_hint.instruction.lower()

    def test_pace_needs_more_support_instruction_mentions_slower(self) -> None:
        ctx = _ctx_with(pace=_pace("needs_more_support"))
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        pace_hint = next(h for h in result.hints if h.behavior == "prefer_pace")
        assert "slower" in pace_hint.instruction.lower() or "guided" in pace_hint.instruction.lower()

    def test_pace_can_go_faster_instruction_mentions_challenging(self) -> None:
        ctx = _ctx_with(pace=_pace("can_go_faster"))
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        pace_hint = next(h for h in result.hints if h.behavior == "prefer_pace")
        assert "challeng" in pace_hint.instruction.lower()

    def test_style_hint_is_always_lesson_relevant(self) -> None:
        ctx = _ctx_with(style=_style("concise", "explicit"))
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math", topic_terms=["fractions"])
        style_hint = next(h for h in result.hints if h.behavior == "prefer_style")
        assert style_hint.is_lesson_relevant is True

    def test_pace_hint_is_always_lesson_relevant(self) -> None:
        ctx = _ctx_with(pace=_pace("normal"))
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math", topic_terms=["fractions"])
        pace_hint = next(h for h in result.hints if h.behavior == "prefer_pace")
        assert pace_hint.is_lesson_relevant is True

    def test_style_hint_source_category(self) -> None:
        ctx = _ctx_with(style=_style("concise", "explicit"))
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        style_hint = next(h for h in result.hints if h.behavior == "prefer_style")
        assert style_hint.source_category == LearnerMemoryCategory.EXPLANATION_STYLE

    def test_pace_hint_source_category(self) -> None:
        ctx = _ctx_with(pace=_pace("needs_more_support"))
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        pace_hint = next(h for h in result.hints if h.behavior == "prefer_pace")
        assert pace_hint.source_category == LearnerMemoryCategory.PACE_SIGNAL


# ── F. Assembler — topic hints with relevance ─────────────────────────────────

class TestAssemblerTopicHints:

    def test_weak_topic_included(self) -> None:
        ctx = _ctx_with(weak_topics=[_weak_topic("mixed numbers")])
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        behaviors = [h.behavior for h in result.hints]
        assert "support_weak_topic" in behaviors

    def test_misconception_included(self) -> None:
        ctx = _ctx_with(misconceptions=[_misconception("confuses numerator with denominator")])
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        behaviors = [h.behavior for h in result.hints]
        assert "watch_for_misconception" in behaviors

    def test_topic_marked_relevant_when_keyword_matches(self) -> None:
        ctx = _ctx_with(weak_topics=[_weak_topic("fractions review")])
        result = assemble_tutor_learner_context(
            ctx, "lesson-1", "Math", topic_terms=["fractions"]
        )
        weak_hint = next(h for h in result.hints if h.behavior == "support_weak_topic")
        assert weak_hint.is_lesson_relevant is True

    def test_topic_marked_not_relevant_when_no_keyword_match(self) -> None:
        ctx = _ctx_with(weak_topics=[_weak_topic("geometry angles")])
        result = assemble_tutor_learner_context(
            ctx, "lesson-1", "Math", topic_terms=["fractions"]
        )
        weak_hint = next(h for h in result.hints if h.behavior == "support_weak_topic")
        assert weak_hint.is_lesson_relevant is False

    def test_topic_included_when_not_relevant_if_below_cap(self) -> None:
        # Non-relevant topics should still be included if under MAX_TOPIC_HINTS
        ctx = _ctx_with(weak_topics=[_weak_topic("geometry angles")])
        result = assemble_tutor_learner_context(
            ctx, "lesson-1", "Math", topic_terms=["fractions"]
        )
        behaviors = [h.behavior for h in result.hints]
        assert "support_weak_topic" in behaviors

    def test_all_topics_included_when_empty_topic_terms(self) -> None:
        ctx = _ctx_with(weak_topics=[_weak_topic("geometry"), _weak_topic("algebra")])
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math", topic_terms=[])
        assert all(h.is_lesson_relevant is True for h in result.hints if h.behavior == "support_weak_topic")

    def test_relevant_topic_sorted_before_irrelevant(self) -> None:
        # Two weak topics: second one matches, first one does not
        irrelevant = _weak_topic("geometry angles")
        relevant = _weak_topic("fractions review")
        # Pass irrelevant first — after sort, relevant should come out first
        ctx = _ctx_with(weak_topics=[irrelevant, relevant])
        result = assemble_tutor_learner_context(
            ctx, "lesson-1", "Math", topic_terms=["fractions"]
        )
        weak_hints = [h for h in result.hints if h.behavior == "support_weak_topic"]
        assert len(weak_hints) >= 1
        # First topic hint should be the relevant one
        assert weak_hints[0].is_lesson_relevant is True

    def test_weak_topic_label_appears_in_instruction(self) -> None:
        ctx = _ctx_with(weak_topics=[_weak_topic("mixed numbers")])
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        weak_hint = next(h for h in result.hints if h.behavior == "support_weak_topic")
        assert "mixed numbers" in weak_hint.instruction

    def test_misconception_description_appears_in_instruction(self) -> None:
        ctx = _ctx_with(misconceptions=[_misconception("confuses numerator with denominator")])
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        misc_hint = next(h for h in result.hints if h.behavior == "watch_for_misconception")
        assert "confuses numerator with denominator" in misc_hint.instruction

    def test_weak_topic_source_category(self) -> None:
        ctx = _ctx_with(weak_topics=[_weak_topic()])
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        weak_hint = next(h for h in result.hints if h.behavior == "support_weak_topic")
        assert weak_hint.source_category == LearnerMemoryCategory.WEAK_TOPIC

    def test_misconception_source_category(self) -> None:
        ctx = _ctx_with(misconceptions=[_misconception()])
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        misc_hint = next(h for h in result.hints if h.behavior == "watch_for_misconception")
        assert misc_hint.source_category == LearnerMemoryCategory.MISCONCEPTION


# ── G. Assembler — bounded counts ─────────────────────────────────────────────

class TestAssemblerBoundedCounts:

    def test_total_hints_never_exceed_max(self) -> None:
        # Supply more signals than MAX_PERSONALIZATION_HINTS across all categories
        ctx = _ctx_with(
            style=_style(),
            pace=_pace(),
            weak_topics=[
                _weak_topic("topic A"),
                _weak_topic("topic B"),
                _weak_topic("topic C"),
            ],
            misconceptions=[_misconception("misc A")],
        )
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        assert len(result.hints) <= MAX_PERSONALIZATION_HINTS

    def test_topic_hints_capped_at_max_topic_hints(self) -> None:
        # More weak topics than MAX_TOPIC_HINTS — only MAX_TOPIC_HINTS should appear
        ctx = _ctx_with(
            weak_topics=[
                _weak_topic("topic A"),
                _weak_topic("topic B"),
                _weak_topic("topic C"),
            ],
        )
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        topic_hints = [h for h in result.hints if h.behavior == "support_weak_topic"]
        assert len(topic_hints) <= MAX_TOPIC_HINTS

    def test_weak_topics_and_misconceptions_share_topic_budget(self) -> None:
        # MAX_TOPIC_HINTS applies to weak topics + misconceptions combined
        ctx = _ctx_with(
            weak_topics=[_weak_topic("A"), _weak_topic("B")],
            misconceptions=[_misconception("misc A"), _misconception("misc B")],
        )
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        topic_level = [
            h for h in result.hints
            if h.behavior in ("support_weak_topic", "watch_for_misconception")
        ]
        assert len(topic_level) <= MAX_TOPIC_HINTS

    def test_style_and_pace_fill_first_two_slots(self) -> None:
        # With style + pace + 2 topic hints, we exactly hit MAX_PERSONALIZATION_HINTS
        ctx = _ctx_with(
            style=_style(),
            pace=_pace(),
            weak_topics=[_weak_topic("A"), _weak_topic("B")],
        )
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        assert len(result.hints) == MAX_PERSONALIZATION_HINTS

    def test_style_appears_before_pace_in_output(self) -> None:
        ctx = _ctx_with(style=_style(), pace=_pace())
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        behaviors = [h.behavior for h in result.hints]
        assert behaviors.index("prefer_style") < behaviors.index("prefer_pace")

    def test_style_and_pace_appear_before_topic_hints(self) -> None:
        ctx = _ctx_with(
            style=_style(),
            pace=_pace(),
            weak_topics=[_weak_topic("A")],
        )
        result = assemble_tutor_learner_context(ctx, "lesson-1", "Math")
        behaviors = [h.behavior for h in result.hints]
        topic_idx = next(
            (i for i, b in enumerate(behaviors) if b == "support_weak_topic"), -1
        )
        assert behaviors.index("prefer_style") < topic_idx
        assert behaviors.index("prefer_pace") < topic_idx


# ── H. format_personalization_hints_block ────────────────────────────────────

class TestFormatPersonalizationHintsBlock:

    def test_returns_empty_string_when_no_personalization(self) -> None:
        bpi = _bpi(hints=())
        assert format_personalization_hints_block(bpi) == ""

    def test_includes_learner_memory_header(self) -> None:
        bpi = _bpi(hints=(_hint(),))
        output = format_personalization_hints_block(bpi)
        assert "LEARNER MEMORY" in output

    def test_includes_end_of_learner_memory_footer(self) -> None:
        bpi = _bpi(hints=(_hint(),))
        output = format_personalization_hints_block(bpi)
        assert "END OF LEARNER MEMORY" in output

    def test_includes_hint_instruction_text(self) -> None:
        h = _hint(instruction="Use more examples when explaining.")
        bpi = _bpi(hints=(h,))
        output = format_personalization_hints_block(bpi)
        assert "Use more examples when explaining." in output

    def test_includes_topic_label_in_weak_topic_instruction(self) -> None:
        ctx = _ctx_with(weak_topics=[_weak_topic("mixed numbers")])
        assembler = TutorLearnerContextAssembler()
        bpi = assembler.assemble(ctx, "lesson-1", "Math")
        output = format_personalization_hints_block(bpi)
        assert "mixed numbers" in output

    def test_includes_hedging_caveat(self) -> None:
        h = _hint()
        bpi = _bpi(hints=(h,))
        output = format_personalization_hints_block(bpi)
        # Should include hedging language about observed tendencies
        assert "observed" in output.lower() or "tendencies" in output.lower()

    def test_no_user_id_in_output(self) -> None:
        bpi = BoundedPersonalizationInput(
            user_id="very-private-user-id-12345",
            lesson_id="lesson-001",
            subject_id="Mathematics",
            hints=(_hint(instruction="Some generic advice."),),
            has_personalization=True,
        )
        output = format_personalization_hints_block(bpi)
        assert "very-private-user-id-12345" not in output

    def test_high_confidence_weak_topic_uses_strong_language(self) -> None:
        # HIGH confidence: evidence_count >= threshold + 3
        high_count = MIN_WEAK_TOPIC_EVIDENCE + 3
        ctx = _ctx_with(weak_topics=[_weak_topic("long division", count=high_count)])
        assembler = TutorLearnerContextAssembler()
        bpi = assembler.assemble(ctx, "lesson-1", "Math")
        output = format_personalization_hints_block(bpi)
        assert "often" in output.lower()

    def test_low_confidence_weak_topic_uses_soft_language(self) -> None:
        # LOW confidence: at exactly the threshold
        ctx = _ctx_with(weak_topics=[_weak_topic("fractions", count=MIN_WEAK_TOPIC_EVIDENCE)])
        assembler = TutorLearnerContextAssembler()
        bpi = assembler.assemble(ctx, "lesson-1", "Math")
        output = format_personalization_hints_block(bpi)
        assert "recently" in output.lower() or "be attentive" in output.lower()

    def test_multiple_hints_all_appear_in_output(self) -> None:
        h1 = _hint(instruction="First instruction.")
        h2 = _hint(instruction="Second instruction.")
        bpi = _bpi(hints=(h1, h2))
        output = format_personalization_hints_block(bpi)
        assert "First instruction." in output
        assert "Second instruction." in output

    def test_each_hint_formatted_as_bullet(self) -> None:
        h = _hint(instruction="Do something specific.")
        bpi = _bpi(hints=(h,))
        output = format_personalization_hints_block(bpi)
        assert "- Do something specific." in output


# ── I. build_system_prompt integration ────────────────────────────────────────

class TestBuildSystemPromptIntegration:

    def test_learner_memory_omitted_when_slice_is_none(self) -> None:
        # Backwards compatibility: learner_memory_slice=None → no block
        ctx = _snapshot(learner_memory_slice=None)
        prompt = build_system_prompt(ctx)
        assert "LEARNER MEMORY" not in prompt

    def test_learner_memory_omitted_when_has_memory_false(self) -> None:
        # has_memory=False (empty context) → no block
        empty_memory = TutorFacingLearnerContext(user_id="u1", has_memory=False)
        ctx = _snapshot(learner_memory_slice=empty_memory)
        prompt = build_system_prompt(ctx)
        assert "LEARNER MEMORY" not in prompt

    def test_learner_memory_present_when_has_memory_true(self) -> None:
        memory = _ctx_with(
            weak_topics=[_weak_topic("mixed numbers")],
        )
        ctx = _snapshot(learner_memory_slice=memory)
        prompt = build_system_prompt(ctx)
        assert "LEARNER MEMORY" in prompt

    def test_topic_label_present_in_prompt_when_has_memory_true(self) -> None:
        # Backwards compat: existing group G test "mixed numbers in prompt"
        memory = _ctx_with(
            weak_topics=[_weak_topic("mixed numbers")],
        )
        ctx = _snapshot(learner_memory_slice=memory)
        prompt = build_system_prompt(ctx)
        assert "mixed numbers" in prompt

    def test_learner_memory_block_appears_before_guardrails(self) -> None:
        # Backwards compat: existing group G test ordering
        memory = _ctx_with(
            weak_topics=[_weak_topic("mixed numbers")],
        )
        ctx = _snapshot(learner_memory_slice=memory)
        prompt = build_system_prompt(ctx)
        mem_pos = prompt.index("LEARNER MEMORY")
        # Guardrail block is identified by a known constant heading
        assert "TUTOR RULES" in prompt
        rules_pos = prompt.index("TUTOR RULES")
        assert mem_pos < rules_pos

    def test_style_hint_appears_in_prompt(self) -> None:
        memory = _ctx_with(style=_style("example_first", "explicit"))
        ctx = _snapshot(learner_memory_slice=memory)
        prompt = build_system_prompt(ctx)
        # The style display text for example_first
        assert "examples before theory" in prompt.lower() or "example_first" in prompt or "Teaching style" in prompt

    def test_misconception_description_appears_in_prompt(self) -> None:
        memory = _ctx_with(
            misconceptions=[_misconception("confuses numerator with denominator")],
        )
        ctx = _snapshot(learner_memory_slice=memory)
        prompt = build_system_prompt(ctx)
        assert "confuses numerator with denominator" in prompt
