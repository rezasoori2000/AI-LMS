"""
Unit tests for app.core.tutor_prompts.

Tests cover:
  - build_system_prompt: role framing, lesson grounding, guardrail inclusion
  - build_system_prompt: hint block present/absent based on context
  - build_conversation_prompt: history formatting, window capping, role labels
  - Guardrail redirect phrases are present in the system prompt
  - No correct-answer leakage pathway exists in the prompt builder
"""
from __future__ import annotations

from datetime import datetime, timezone

from app.core.tutor_prompts import (
    ANSWER_WITHHOLD_REDIRECT,
    HISTORY_WINDOW_TURNS,
    OFF_TOPIC_REDIRECT,
    OUT_OF_SCOPE_REDIRECT,
    build_conversation_prompt,
    build_system_prompt,
)
from app.models.tutor import ConversationTurn, TutorContextSnapshot


# ── Fixtures / helpers ────────────────────────────────────────────────────────

def _make_context(**overrides: object) -> TutorContextSnapshot:
    """Return a minimal valid TutorContextSnapshot, with optional field overrides."""
    base: dict[str, object] = {
        "student_profile_id":   "prof-001",
        "user_id":              "user-001",
        "student_first_name":   "Alice",
        "lesson_id":            "lesson-001",
        "lesson_title":         "Introduction to Fractions",
        "lesson_content": (
            "A fraction represents a part of a whole. "
            "The top number is called the numerator and the bottom number is the denominator."
        ),
        "grade_name":           "Grade 4",
        "subject_name":         "Mathematics",
        "lesson_progress_status": "InProgress",
        "conversation_id":      "conv-001",
    }
    base.update(overrides)
    return TutorContextSnapshot(**base)


def _make_turn(role: str, content: str) -> ConversationTurn:
    return ConversationTurn(
        role=role,
        content=content,
        sent_at=datetime(2026, 5, 24, 12, 0, 0, tzinfo=timezone.utc),
    )


# ── build_system_prompt: identity and context ─────────────────────────────────

def test_system_prompt_contains_student_name() -> None:
    ctx = _make_context(student_first_name="Bob")
    prompt = build_system_prompt(ctx)
    assert "Bob" in prompt


def test_system_prompt_contains_lesson_title() -> None:
    ctx = _make_context(lesson_title="The Water Cycle")
    prompt = build_system_prompt(ctx)
    assert "The Water Cycle" in prompt


def test_system_prompt_contains_subject_name() -> None:
    ctx = _make_context(subject_name="Earth Science")
    prompt = build_system_prompt(ctx)
    assert "Earth Science" in prompt


def test_system_prompt_contains_grade_name() -> None:
    ctx = _make_context(grade_name="Grade 6")
    prompt = build_system_prompt(ctx)
    assert "Grade 6" in prompt


def test_system_prompt_contains_lesson_content() -> None:
    ctx = _make_context(lesson_content="Plants use sunlight to make food via photosynthesis.")
    prompt = build_system_prompt(ctx)
    assert "Plants use sunlight to make food via photosynthesis." in prompt


def test_system_prompt_uses_fallback_when_lesson_content_empty() -> None:
    ctx = _make_context(lesson_content="")
    prompt = build_system_prompt(ctx)
    # Should not crash and should include the fallback marker
    assert "No lesson text is available" in prompt


# ── build_system_prompt: guardrail phrases ────────────────────────────────────

def test_system_prompt_contains_out_of_scope_redirect() -> None:
    ctx = _make_context()
    prompt = build_system_prompt(ctx)
    assert OUT_OF_SCOPE_REDIRECT in prompt


def test_system_prompt_contains_answer_withhold_redirect() -> None:
    ctx = _make_context()
    prompt = build_system_prompt(ctx)
    assert ANSWER_WITHHOLD_REDIRECT in prompt


def test_system_prompt_contains_off_topic_redirect() -> None:
    ctx = _make_context()
    prompt = build_system_prompt(ctx)
    assert OFF_TOPIC_REDIRECT in prompt


def test_system_prompt_instructs_lesson_scope() -> None:
    ctx = _make_context()
    prompt = build_system_prompt(ctx)
    # Must instruct the model to answer only from lesson content
    assert "ONLY from the lesson content" in prompt or "LESSON SCOPE" in prompt


def test_system_prompt_instructs_no_direct_answers() -> None:
    ctx = _make_context()
    prompt = build_system_prompt(ctx)
    assert "NO DIRECT ANSWERS" in prompt or "Never reveal the correct answer" in prompt


def test_system_prompt_does_not_reference_correct_answer_data() -> None:
    """
    The system prompt must never reference a 'correct_answer' value because
    TutorContextSnapshot does not carry it.  This test verifies no prompt
    builder path accidentally fabricates or exposes an answer.
    """
    ctx = _make_context()
    prompt = build_system_prompt(ctx)
    # The phrase "correct answer" appears only in prohibition instructions,
    # never as a value being disclosed.  Ensure no "the correct answer is"
    # pattern appears (i.e. the model is not being told what the answer is).
    lower = prompt.lower()
    assert "the correct answer is" not in lower
    assert "correct answer =" not in lower


# ── build_system_prompt: hint block ──────────────────────────────────────────

def test_system_prompt_includes_hint_block_when_question_set() -> None:
    ctx = _make_context(
        hint_for_question_id="q-001",
        question_text="What is the numerator in 3/4?",
        question_type="MultipleChoice",
        question_options=["1", "3", "4", "7"],
    )
    prompt = build_system_prompt(ctx)
    assert "HINT REQUEST" in prompt
    assert "What is the numerator in 3/4?" in prompt


def test_system_prompt_hint_block_lists_options() -> None:
    ctx = _make_context(
        hint_for_question_id="q-001",
        question_text="Which is larger: 1/2 or 1/4?",
        question_type="MultipleChoice",
        question_options=["1/2", "1/4", "They are equal", "Cannot tell"],
    )
    prompt = build_system_prompt(ctx)
    # All four options should appear in the prompt
    assert "1/2" in prompt
    assert "1/4" in prompt
    assert "They are equal" in prompt
    assert "Cannot tell" in prompt


def test_system_prompt_hint_block_instructs_no_answer_reveal() -> None:
    ctx = _make_context(
        hint_for_question_id="q-001",
        question_text="Is 2/4 the same as 1/2?",
        question_type="TrueFalse",
    )
    prompt = build_system_prompt(ctx)
    assert "Do not reveal or confirm the correct answer" in prompt


def test_system_prompt_excludes_hint_block_when_no_question() -> None:
    ctx = _make_context()  # no hint_for_question_id
    prompt = build_system_prompt(ctx)
    assert "HINT REQUEST" not in prompt


def test_system_prompt_excludes_hint_block_when_question_id_set_but_no_text() -> None:
    # hint_for_question_id set but question_text is None — block must be skipped
    ctx = _make_context(hint_for_question_id="q-001", question_text=None)
    prompt = build_system_prompt(ctx)
    assert "HINT REQUEST" not in prompt


# ── build_conversation_prompt: structure and formatting ──────────────────────

def test_conversation_prompt_ends_with_tutor_cue() -> None:
    ctx = _make_context()
    prompt = build_conversation_prompt(ctx, "Can you re-explain fractions?")
    assert prompt.strip().endswith("Tutor:")


def test_conversation_prompt_contains_student_message() -> None:
    ctx = _make_context()
    msg = "What does the denominator mean?"
    prompt = build_conversation_prompt(ctx, msg)
    assert msg in prompt


def test_conversation_prompt_no_history_still_valid() -> None:
    ctx = _make_context(history=[])
    prompt = build_conversation_prompt(ctx, "Hello tutor!")
    assert "Student: Hello tutor!" in prompt
    assert prompt.strip().endswith("Tutor:")


def test_conversation_prompt_formats_history_with_role_labels() -> None:
    ctx = _make_context(
        history=[
            _make_turn("student", "What is a fraction?"),
            _make_turn("tutor",   "A fraction is a part of a whole."),
        ]
    )
    prompt = build_conversation_prompt(ctx, "Can you give an example?")
    assert "Student: What is a fraction?" in prompt
    assert "Tutor: A fraction is a part of a whole." in prompt
    assert "Student: Can you give an example?" in prompt


def test_conversation_prompt_caps_history_to_window() -> None:
    # Use collision-free content strings so "old-msg" cannot appear as a
    # substring of "new-msg" or vice versa.
    old_content = "oldest-dropped-message-alpha"
    new_content = "newest-kept-message-beta"

    # 4 old turns that should be dropped, followed by HISTORY_WINDOW_TURNS new
    # turns that must be kept.
    old_turns = [_make_turn("student", old_content) for _ in range(4)]
    new_turns = [_make_turn("tutor",   new_content) for _ in range(HISTORY_WINDOW_TURNS)]

    ctx = _make_context(history=old_turns + new_turns)
    prompt = build_conversation_prompt(ctx, "latest question")

    assert old_content not in prompt
    assert new_content in prompt


# ── Endpoint-level: 503 when no provider configured ──────────────────────────

import pytest
from httpx import AsyncClient


@pytest.mark.asyncio
async def test_ask_tutor_returns_503_when_no_provider_configured(
    client: AsyncClient,
) -> None:
    """
    The endpoint must return 503 (not 501) once the guardrail implementation is
    in place.  The provider is not configured in Phase 1, so every real request
    hits the NotImplementedError → 503 path.
    """
    payload = {
        "context_snapshot": {
            "student_profile_id":     "prof-001",
            "user_id":                "user-001",
            "student_first_name":     "Alice",
            "lesson_id":              "lesson-001",
            "lesson_title":           "Introduction to Fractions",
            "lesson_content":         "A fraction represents a part of a whole.",
            "grade_name":             "Grade 4",
            "subject_name":           "Mathematics",
            "lesson_progress_status": "InProgress",
            "conversation_id":        "conv-001",
        },
        "student_message": "Can you explain what a numerator is?",
    }
    response = await client.post("/api/v1/tutor/ask", json=payload)
    assert response.status_code == 503


@pytest.mark.asyncio
async def test_ask_tutor_returns_422_for_missing_body(client: AsyncClient) -> None:
    response = await client.post("/api/v1/tutor/ask", json={})
    assert response.status_code == 422


@pytest.mark.asyncio
async def test_ask_tutor_returns_422_for_empty_student_message(
    client: AsyncClient,
) -> None:
    payload = {
        "context_snapshot": {
            "student_profile_id":     "prof-001",
            "user_id":                "user-001",
            "student_first_name":     "Alice",
            "lesson_id":              "lesson-001",
            "lesson_title":           "Test Lesson",
            "lesson_content":         "Some content.",
            "grade_name":             "Grade 5",
            "subject_name":           "Science",
            "lesson_progress_status": "NotStarted",
            "conversation_id":        "conv-002",
        },
        "student_message": "",  # violates min_length=1
    }
    response = await client.post("/api/v1/tutor/ask", json=payload)
    assert response.status_code == 422
