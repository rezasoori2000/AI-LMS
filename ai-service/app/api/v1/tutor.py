"""
AI Tutor API endpoint.

POST /api/v1/tutor/ask — accepts an assembled TutorContextSnapshot + student
message and returns an AI-generated tutor reply.

Status (Section 11, Part 1): Architecture stub — returns HTTP 501 until
Section 11, Part 2 wires a real LLM provider call.

Part 2 implementation checklist:
  1. Import get_provider from app.providers.registry
  2. Build a system prompt from context_snapshot (lesson-grounded, grade-appropriate)
  3. Format conversation history as the prompt context
  4. Call provider.complete(prompt, system_prompt=..., max_tokens=2000, temperature=0.3)
  5. Return TutorAskResponse(reply=reply, tokens_used=...)
  6. Add error handling: provider unavailable → HTTP 503
"""
from __future__ import annotations

from fastapi import APIRouter, HTTPException, status

from app.models.tutor import TutorAskPayload, TutorAskResponse

router = APIRouter()


@router.post(
    "/ask",
    response_model=TutorAskResponse,
    summary="Ask the AI tutor",
    description=(
        "Accepts an assembled lesson context snapshot and a student message, "
        "then returns an AI-generated tutor reply grounded in the lesson content.\n\n"
        "**Status**: Returns HTTP 501 until Section 11, Part 2 wires the LLM provider."
    ),
    tags=["Tutor"],
)
async def ask_tutor(payload: TutorAskPayload) -> TutorAskResponse:
    # TODO (Section 11, Part 2):
    #   from app.providers.registry import get_provider
    #   provider = get_provider()
    #   system_prompt = _build_system_prompt(payload.context_snapshot)
    #   prompt = _format_prompt(payload.context_snapshot, payload.student_message)
    #   reply = await provider.complete(
    #       prompt,
    #       system_prompt=system_prompt,
    #       max_tokens=2000,
    #       temperature=0.3,
    #   )
    #   return TutorAskResponse(reply=reply, tokens_used=None)
    raise HTTPException(
        status_code=status.HTTP_501_NOT_IMPLEMENTED,
        detail="AI tutoring is not yet available. See Section 11, Part 2.",
    )
