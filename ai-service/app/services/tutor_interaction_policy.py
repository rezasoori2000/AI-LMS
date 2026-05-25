"""
Tutor interaction policy for Part 4.

This module adds a small, explicit teacher-like behavior model while preserving
lesson-grounded boundaries.
"""
from __future__ import annotations

from dataclasses import dataclass

from app.models.tutor import TutorExplanationDepth, TutorInteractionIntent
from app.services.tutor_retrieval_context import TutorIntentHint, infer_intent_hint


@dataclass(frozen=True, slots=True)
class TutorInteractionPlan:
    intent: TutorInteractionIntent
    depth: TutorExplanationDepth
    retrieval_top_k: int
    allow_curriculum_expansion: bool
    response_shape_rules: tuple[str, ...]
    followup_suggestions: tuple[str, ...]


def build_interaction_plan(
    *,
    student_message: str,
    interaction_intent: TutorInteractionIntent | None,
    requested_depth: TutorExplanationDepth | None,
) -> TutorInteractionPlan:
    resolved_intent = interaction_intent or _infer_external_intent(student_message)
    resolved_depth = _resolve_depth(resolved_intent, requested_depth, student_message)

    retrieval_top_k = {
        "explain": 3,
        "simplify": 2,
        "give-example": 5,
        "explain-step-by-step": 4,
        "clarify-confusion": 3,
    }[resolved_intent]

    allow_curriculum_expansion = resolved_intent in {
        "give-example",
        "explain-step-by-step",
    }

    response_shape_rules = _response_shape_rules(resolved_intent, resolved_depth)
    followups = _followup_suggestions(resolved_intent)

    return TutorInteractionPlan(
        intent=resolved_intent,
        depth=resolved_depth,
        retrieval_top_k=retrieval_top_k,
        allow_curriculum_expansion=allow_curriculum_expansion,
        response_shape_rules=response_shape_rules,
        followup_suggestions=followups,
    )


def build_interaction_instruction_block(plan: TutorInteractionPlan) -> str:
    lines = [
        "INTERACTION MODE",
        "----------------",
        f"Intent: {plan.intent}",
        f"Depth: {plan.depth}",
        "Response shape rules:",
    ]

    for rule in plan.response_shape_rules:
        lines.append(f"- {rule}")

    lines.extend([
        "",
        "Grounding requirement:",
        "- Use only lesson-scoped retrieved material and explicit lesson content.",
        "- If context is insufficient, say so and ask a clarifying follow-up.",
        "----------------",
        "END OF INTERACTION MODE",
    ])
    return "\n".join(lines)


def _infer_external_intent(student_message: str) -> TutorInteractionIntent:
    inferred = infer_intent_hint(student_message)
    mapping: dict[TutorIntentHint, TutorInteractionIntent] = {
        TutorIntentHint.EXPLAIN: "explain",
        TutorIntentHint.SIMPLIFY: "simplify",
        TutorIntentHint.GIVE_EXAMPLE: "give-example",
        TutorIntentHint.EXPLAIN_STEP_BY_STEP: "explain-step-by-step",
        TutorIntentHint.CLARIFY_CONFUSION: "clarify-confusion",
        TutorIntentHint.UNKNOWN: "explain",
    }
    return mapping[inferred]


def _resolve_depth(
    intent: TutorInteractionIntent,
    requested_depth: TutorExplanationDepth | None,
    student_message: str,
) -> TutorExplanationDepth:
    if requested_depth is not None:
        return requested_depth

    msg = student_message.lower()
    if intent == "simplify":
        return "simple"
    if intent == "explain-step-by-step":
        return "standard"
    if any(k in msg for k in ("more detail", "in depth", "deeper", "advanced")):
        return "deep"
    return "standard"


def _response_shape_rules(
    intent: TutorInteractionIntent,
    depth: TutorExplanationDepth,
) -> tuple[str, ...]:
    depth_rule = {
        "simple": "Use short, plain sentences and one key idea at a time.",
        "standard": "Use 2-3 concise paragraphs with one concrete anchor example.",
        "deep": "Provide a deeper concept explanation with 2 anchored examples.",
    }[depth]

    intent_rules: dict[TutorInteractionIntent, tuple[str, ...]] = {
        "explain": (
            "Restate the core concept first, then connect to the student question.",
            depth_rule,
        ),
        "simplify": (
            "Rephrase using simpler vocabulary and shorter sentences.",
            "Prefer one mini-example over abstract wording.",
            depth_rule,
        ),
        "give-example": (
            "Lead with one worked example tied to the current lesson.",
            "Optionally add one contrast example if still lesson-grounded.",
            depth_rule,
        ),
        "explain-step-by-step": (
            "Present numbered steps in logical order.",
            "Each step should build directly on the previous one.",
            depth_rule,
        ),
        "clarify-confusion": (
            "Start by naming the likely confusion point briefly.",
            "Then give a corrected explanation and a quick check question.",
            depth_rule,
        ),
    }
    return intent_rules[intent]


def _followup_suggestions(intent: TutorInteractionIntent) -> tuple[str, ...]:
    suggestions: dict[TutorInteractionIntent, tuple[str, ...]] = {
        "explain": (
            "Would you like a simpler version of this explanation?",
            "Would an example from this lesson help?",
        ),
        "simplify": (
            "Would you like this in step-by-step form?",
            "Do you want one more easy example from this lesson?",
        ),
        "give-example": (
            "Do you want one harder example from the same chapter?",
            "Should I explain why this example works?",
        ),
        "explain-step-by-step": (
            "Would you like to try the next step yourself?",
            "Do you want this same process on another example?",
        ),
        "clarify-confusion": (
            "Which part still feels unclear?",
            "Do you want a shorter version of that explanation?",
        ),
    }
    return suggestions[intent]
