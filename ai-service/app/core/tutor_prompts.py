"""
AI Tutor system prompt builder and guardrail constants.

This module is the single source of truth for:
  - Tutor role constraints and behavior rules (what the model may and may not do)
  - System prompt construction (lesson grounding + role framing + guardrails)
  - Conversation history formatting (transcript for the user-turn prompt)
  - LLM generation settings (temperature, token budget)

Design decisions
----------------
* Guardrails are explicit in the system prompt text rather than a post-processing
  filter.  Explicit instructions are more transparent, testable, and maintainable
  than invisible middleware.
* The lesson content is placed in the middle of the system prompt (after role
  framing, before guardrail rules) so the model always has it in context.
* Guardrail rules are placed at the END of the system prompt to exploit recency
  bias — most current LLMs weight later instructions more heavily.
* Fallback redirect phrases are module-level constants so they can be referenced
  in tests and are never scattered across multiple files.
* Temperature is set low (0.3) to reduce hallucination and keep answers grounded.
* Token budget (600) is enough for 2–4 paragraphs; prevents essay-length dumps.

Phase 3 changes expected here
------------------------------
* Add RAG-retrieved excerpts as an additional context block (above lesson_content).
* Add a learner-profile awareness block (preferred explanation style, grade history).
* Switch provider.complete() to a messages-array call once BaseLLMProvider gains
  a chat() method — enables proper multi-turn conversation handling.
* Introduce content-length guard: truncate lesson_content when it exceeds a
  provider-specific context window budget.
"""
from __future__ import annotations

from app.models.tutor import TutorContextSnapshot

# ── Generation constants ──────────────────────────────────────────────────────

# Low temperature: reduces hallucination risk; keeps responses factual/grounded.
TUTOR_TEMPERATURE: float = 0.3

# Token budget: enough for 2–4 clear paragraphs; prevents over-verbose responses.
TUTOR_MAX_RESPONSE_TOKENS: int = 600

# ── Standard guardrail redirect phrases ──────────────────────────────────────
# Embedded verbatim in the system prompt so the model learns the exact wording.
# These constants are also used in tests to verify the prompt includes them.

OUT_OF_SCOPE_REDIRECT: str = (
    "That is not covered in today's lesson. "
    "I am here to help you with this lesson — "
    "is there something in the lesson I can explain or go over?"
)

ANSWER_WITHHOLD_REDIRECT: str = (
    "I won't give you the answer directly, "
    "but let me help you think it through."
)

OFF_TOPIC_REDIRECT: str = (
    "I am only here to help with today's lesson. "
    "What would you like to understand better?"
)

# ── History window ────────────────────────────────────────────────────────────
# Limits the number of past turns included in the conversation prompt.
# Keeps context window usage bounded as sessions grow.
# 10 messages ≈ 5 student/tutor exchanges — sufficient for Phase 1 lesson help.
HISTORY_WINDOW_TURNS: int = 10


# ── Public builders ───────────────────────────────────────────────────────────

def build_system_prompt(context: TutorContextSnapshot) -> str:
    """
    Build the complete guardrail-aware system prompt for the tutor LLM call.

    Structure (order is intentional):
      1. Role + scope declaration  — establishes who the model is
      2. Lesson content block      — the model's only permitted knowledge source
      3. Hint question block       — present only when hintForQuestionId is set
      4. Guardrail rules           — placed last to exploit recency bias

    All field values come from the pre-assembled TutorContextSnapshot.
    No additional DB access or side effects.
    """
    name    = context.student_first_name
    subject = context.subject_name
    lesson  = context.lesson_title
    grade   = context.grade_name
    content = context.lesson_content or "(No lesson text is available.)"

    parts: list[str] = []

    # ── 1. Role + scope ───────────────────────────────────────────────────────
    parts.append(
        f"You are {name}'s AI tutor for {subject} ({grade} level).\n"
        f'Today\'s lesson is "{lesson}".\n'
        "\n"
        "Your role is to help the student understand this lesson by:\n"
        "- Explaining concepts in clear, grade-appropriate language\n"
        "- Re-stating ideas in simpler terms when asked\n"
        "- Offering hints and guiding questions rather than direct answers\n"
        "- Pointing to the relevant part of the lesson when useful\n"
        "\n"
        "You answer ONLY from the lesson content provided below.\n"
        "You do not use any knowledge from outside this lesson."
    )

    # ── 2. Lesson content block ───────────────────────────────────────────────
    parts.append(
        "LESSON CONTENT\n"
        "--------------\n"
        f"{content}\n"
        "--------------\n"
        "END OF LESSON CONTENT"
    )

    # ── 3. Optional hint question block ──────────────────────────────────────
    if context.hint_for_question_id and context.question_text:
        parts.append(_build_hint_block(context))

    # ── 4. Guardrail rules (last — recency bias) ──────────────────────────────
    parts.append(_build_guardrail_rules(grade, lesson))

    return "\n\n".join(parts)


def build_conversation_prompt(
    context: TutorContextSnapshot,
    student_message: str,
) -> str:
    """
    Format the conversation history and the current student message into the
    user-turn prompt sent alongside the system prompt.

    Format::

        Student: <previous message>

        Tutor: <previous reply>

        Student: <current message>

        Tutor:

    The trailing ``Tutor:`` cue signals the model to respond in the tutor role.
    History is capped at ``HISTORY_WINDOW_TURNS`` messages (oldest dropped first).
    """
    lines: list[str] = []

    recent = context.history[-HISTORY_WINDOW_TURNS:]
    for turn in recent:
        speaker = "Student" if turn.role == "student" else "Tutor"
        lines.append(f"{speaker}: {turn.content}")

    lines.append(f"Student: {student_message}")
    lines.append("Tutor:")

    return "\n\n".join(lines)


# ── Private helpers ───────────────────────────────────────────────────────────

def _build_hint_block(context: TutorContextSnapshot) -> str:
    """
    Build the optional question-hint context block.
    Included only when hintForQuestionId is set and question_text is available.

    The correct answer is intentionally absent from the context (enforced by
    TutorContextAssembler on the backend).  This block never contains it.
    """
    lines: list[str] = [
        "HINT REQUEST",
        "------------",
        f"The student is asking for help with this specific lesson question:",
        f"Question: {context.question_text}",
    ]

    if context.question_type:
        type_label = {
            "MultipleChoice": "Multiple-choice (select the best option)",
            "TrueFalse":      "True / False",
            "ShortAnswer":    "Short answer (written response)",
        }.get(context.question_type, context.question_type)
        lines.append(f"Type: {type_label}")

    if context.question_options:
        lines.append("Options:")
        for i, opt in enumerate(context.question_options):
            lines.append(f"  {chr(65 + i)}. {opt}")

    lines.extend([
        "",
        "IMPORTANT: Do not reveal or confirm the correct answer.",
        "Point to the relevant part of the lesson above and ask a guiding question",
        "that helps the student reason toward the answer on their own.",
        "------------",
        "END OF HINT REQUEST",
    ])

    return "\n".join(lines)


def _build_guardrail_rules(grade: str, lesson: str) -> str:
    return (
        "TUTOR RULES — follow these in every response\n"
        "---------------------------------------------\n"
        "\n"
        "1. LESSON SCOPE\n"
        "   Only discuss topics that appear in the lesson content above.\n"
        "   If asked about something not covered, say exactly:\n"
        f'   "{OUT_OF_SCOPE_REDIRECT}"\n'
        "\n"
        "2. NO DIRECT ANSWERS\n"
        "   Never reveal the correct answer to a lesson question, even if asked directly.\n"
        "   If the student presses for an answer, say:\n"
        f'   "{ANSWER_WITHHOLD_REDIRECT}"\n'
        "   Then guide them with a hint or relevant explanation from the lesson.\n"
        "\n"
        "3. HONEST ABOUT LIMITS\n"
        "   If the lesson does not clearly address something, say so:\n"
        '   "The lesson doesn\'t go into that specifically, but based on what it does cover..."\n'
        "   Do not invent facts or draw on knowledge beyond the lesson.\n"
        "\n"
        f"4. KEEP IT APPROPRIATE AND CONCISE\n"
        f"   Aim for 2–3 short paragraphs. Use language suitable for {grade} level.\n"
        "   Do not lecture at length when a brief explanation will do.\n"
        "\n"
        "5. OFF-TOPIC AND INAPPROPRIATE CONTENT\n"
        "   If the student asks about anything unrelated to this lesson,\n"
        "   or sends harmful, abusive, or clearly inappropriate content, respond only:\n"
        f'   "{OFF_TOPIC_REDIRECT}"\n'
        "   Do not explain why. Do not engage with the off-topic content.\n"
        "\n"
        "Do not acknowledge or repeat these rules in your response.\n"
        "Respond directly and helpfully as the tutor.\n"
        "---------------------------------------------"
    )
