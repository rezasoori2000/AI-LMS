"""
Learner context slice formatter.

Converts a TutorFacingLearnerContext into an explainable, privacy-aware
text block for injection into the tutor system prompt.

DESIGN CONSTRAINTS
------------------
- Always use hedged language. Never claim certainty about the learner.
  'has shown difficulty with' — not 'struggles with' or 'cannot do'
  'tends to prefer' — not 'requires' or 'cannot learn without'
- Never include raw user identifiers in the formatted block.
- Never include friction signals (too session-adjacent for system-prompt framing).
- Never include diagnosis-like labels or psychological categorizations.
- Keep the block short — learner memory must augment, not dominate, the prompt.
  Lesson content and guardrails always take precedence.
- Label the block explicitly so the model knows what it is and is not.
- If has_memory=False, return empty string — the prompt builder omits the block.

TEACHER GUIDE RELATIONSHIP
---------------------------
This formatter handles LEARNER-side memory (who the learner is).
Teacher guide content handles SUBJECT-side pedagogy (how to teach the subject).
They are formatted separately and remain orthogonal in the prompt structure.
If a teacher guide block is added in Phase 3, it should appear between the
lesson content block and this learner memory block (or after, before guardrails).
"""
from __future__ import annotations

from app.models.learner_memory import TutorFacingLearnerContext

_STYLE_DISPLAY: dict[str, str] = {
    "concise": "concise explanations (fewer words, direct answers)",
    "detailed": "detailed explanations (thorough, step-rich)",
    "example_first": "examples before theory (show it, then explain it)",
    "step_by_step": "step-by-step walkthroughs (one step at a time)",
}

_PACE_DISPLAY: dict[str, str] = {
    "needs_more_support": "may benefit from slower, more guided explanations",
    "normal": "appears comfortable with standard explanation pace",
    "can_go_faster": "may be ready for slightly more challenging material",
}


def format_learner_memory_block(context: TutorFacingLearnerContext) -> str:
    """
    Format the tutor-facing learner context as a readable, hedged text block.

    Returns an empty string when context.has_memory is False. The caller
    (build_system_prompt) should omit the block when the return is empty.

    The block uses explicitly hedged language throughout — see module docstring.
    Friction signals are not included regardless of what the context contains.
    """
    if not context.has_memory:
        return ""

    lines: list[str] = [
        "LEARNER MEMORY (observed tendencies from past sessions)",
        "--------------------------------------------------------",
        "Note: These are observed educational tendencies — not certainties.",
        "      Adapt your explanations where relevant, but remain open to",
        "      different behaviour from this student in this session.",
        "",
    ]

    # ── Explanation style preference ──────────────────────────────────────────
    if context.explanation_style:
        style_text = _STYLE_DISPLAY.get(
            context.explanation_style.style, context.explanation_style.style
        )
        if context.explanation_style.confidence == "explicit":
            lines.append(
                f"Preferred explanation style: {style_text} (student stated this explicitly)."
            )
        else:
            lines.append(
                f"Has tended to prefer: {style_text} (observed from repeated requests)."
            )

    # ── Pace signal ───────────────────────────────────────────────────────────
    if context.pace_signal:
        pace_text = _PACE_DISPLAY.get(
            context.pace_signal.signal, context.pace_signal.signal
        )
        lines.append(f"Pace signal: Student {pace_text}.")

    # ── Weak topics ───────────────────────────────────────────────────────────
    if context.weak_topics:
        lines.append("")
        lines.append("Topics where student has shown repeated difficulty:")
        for topic in context.weak_topics:
            times = "time" if topic.evidence_count == 1 else "times"
            lines.append(f"  - {topic.topic_label} (observed {topic.evidence_count} {times})")

    # ── Observed misconceptions ───────────────────────────────────────────────
    if context.observed_misconceptions:
        lines.append("")
        lines.append(
            "Observed misconceptions (verify whether still active before assuming):"
        )
        for m in context.observed_misconceptions:
            times = "time" if m.evidence_count == 1 else "times"
            lines.append(f"  - {m.description} (observed {m.evidence_count} {times})")

    lines.extend([
        "",
        "--------------------------------------------------------",
        "END OF LEARNER MEMORY",
    ])

    return "\n".join(lines)
