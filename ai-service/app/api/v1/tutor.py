"""
AI Tutor API endpoint.

POST /api/v1/tutor/ask — accepts an assembled TutorContextSnapshot + student
message and returns a lesson-grounded, guardrail-bounded AI tutor reply.

Guardrail strategy (Section 11, Part 4)
----------------------------------------
All behavioral constraints live in the system prompt built by
``app.core.tutor_prompts.build_system_prompt``.  This is a deliberate choice:
explicit prompt-level guardrails are transparent, testable, and maintainable
without requiring a separate moderation layer.

Key constraints enforced by the system prompt:
  1. LESSON SCOPE   — model must answer only from the provided lesson content
  2. NO DIRECT ANSWERS — model must withhold correct answers even if asked
  3. HONEST LIMITS  — model must acknowledge gaps rather than fabricate
  4. CONCISE OUTPUT — 2–3 paragraphs, grade-appropriate language
  5. OFF-TOPIC / INAPPROPRIATE — fixed redirect phrase; no engagement

Error handling
--------------
  NotImplementedError  → 503  (no LLM provider is configured — expected in Phase 1)
  Any provider error   → 503  (LLM unavailable; do not expose internal details)
  Pydantic validation  → 422  (handled automatically by FastAPI)

Phase 2: Replace ``StubTutorProvider`` in the backend with a real ``HttpTutorProvider``
that forwards to this endpoint.  The guardrail system prompt is already in place.
"""
from __future__ import annotations

import logging

from fastapi import APIRouter, HTTPException, status

from app.core.tutor_prompts import (
    TUTOR_MAX_RESPONSE_TOKENS,
    TUTOR_TEMPERATURE,
    build_conversation_prompt,
    build_system_prompt,
)
from app.models.tutor import TutorAskPayload, TutorAskResponse
from app.providers.registry import get_provider
from app.services.tutor_interaction_policy import (
    build_interaction_instruction_block,
    build_interaction_plan,
)
from app.services.tutor_retrieval_context import (
    LessonFirstRetrievalStrategy,
    format_retrieval_context_block,
    TutorRetrievalContextAssembler,
)
from app.services.retrieval_pipeline import InMemoryRetrievalArtifactRepository, LessonScopedRetriever

logger = logging.getLogger(__name__)
router = APIRouter()


@router.post(
    "/ask",
    response_model=TutorAskResponse,
    summary="Ask the AI tutor",
    description=(
        "Accepts an assembled lesson context snapshot and a student message, "
        "then returns an AI-generated tutor reply grounded in the lesson content.\n\n"
        "The system prompt applies lesson-scope guardrails, answer-withhold rules, "
        "and off-topic redirect patterns.  See ``app.core.tutor_prompts`` for details.\n\n"
        "Returns **503** when the LLM provider is not configured or unavailable."
    ),
    tags=["Tutor"],
)
async def ask_tutor(payload: TutorAskPayload) -> TutorAskResponse:
    context = payload.context_snapshot
    student_message = payload.student_message

    interaction_plan = build_interaction_plan(
        student_message=student_message,
        interaction_intent=payload.interaction_intent,
        requested_depth=payload.requested_depth,
    )

    retrieval_assembler = TutorRetrievalContextAssembler(
        strategy=LessonFirstRetrievalStrategy(
            LessonScopedRetriever(repository=InMemoryRetrievalArtifactRepository())
        ),
        max_items=interaction_plan.retrieval_top_k,
    )

    retrieval_context = retrieval_assembler.assemble_context(
        context,
        student_message,
        section_scope=payload.section_scope,
        allow_curriculum_expansion=interaction_plan.allow_curriculum_expansion,
        chapter_neighbor_lesson_ids=tuple(payload.chapter_neighbor_lesson_ids),
        intent_hint=None,
    )

    # Build the guardrail-aware prompts.
    system_prompt = build_system_prompt(context)
    interaction_block = build_interaction_instruction_block(interaction_plan)
    retrieval_block = format_retrieval_context_block(retrieval_context)
    conversation_prompt = "\n\n".join(
        [
            interaction_block,
            retrieval_block,
            build_conversation_prompt(context, student_message),
        ]
    )

    # Obtain the configured LLM provider and call it.
    try:
        provider = get_provider()
    except NotImplementedError:
        # Expected in Phase 1 — no real provider is wired yet.
        logger.info(
            "Tutor ask rejected: no LLM provider configured "
            "(conversation=%s, lesson=%s)",
            context.conversation_id,
            context.lesson_id,
        )
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="AI tutor provider is not configured.",
        )

    try:
        reply = await provider.complete(
            conversation_prompt,
            system_prompt=system_prompt,
            max_tokens=TUTOR_MAX_RESPONSE_TOKENS,
            temperature=TUTOR_TEMPERATURE,
        )
    except Exception:
        logger.exception(
            "Tutor provider error (conversation=%s, lesson=%s)",
            context.conversation_id,
            context.lesson_id,
        )
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="AI tutor is temporarily unavailable. Please try again shortly.",
        )

    # tokens_used is not available from BaseLLMProvider.complete() — it returns
    # a plain string.  Phase 2 provider implementations may add token reporting
    # via a richer return type; set to None for now.
    return TutorAskResponse(
        reply=reply,
        tokens_used=None,
        response_mode=interaction_plan.intent,
        response_depth=interaction_plan.depth,
        suggested_followups=list(interaction_plan.followup_suggestions),
    )
