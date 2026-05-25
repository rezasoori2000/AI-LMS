from __future__ import annotations

from app.models.tutor import TutorAskPayload, TutorAskResponse, TutorContextSnapshot


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


def test_tutor_ask_payload_accepts_intent_and_scope_fields() -> None:
    payload = TutorAskPayload(
        context_snapshot=_snapshot(),
        student_message="Please simplify this.",
        interaction_intent="simplify",
        requested_depth="simple",
        section_scope="Numerator",
        chapter_neighbor_lesson_ids=["lesson-002"],
    )

    assert payload.interaction_intent == "simplify"
    assert payload.requested_depth == "simple"
    assert payload.section_scope == "Numerator"
    assert payload.chapter_neighbor_lesson_ids == ["lesson-002"]


def test_tutor_ask_response_exposes_mode_and_followups() -> None:
    response = TutorAskResponse(
        reply="A fraction has a numerator and denominator.",
        tokens_used=None,
        response_mode="explain",
        response_depth="standard",
        suggested_followups=["Would you like a simpler version?"],
    )

    assert response.response_mode == "explain"
    assert response.response_depth == "standard"
    assert response.suggested_followups
