from __future__ import annotations

from app.services.tutor_interaction_policy import (
    build_interaction_instruction_block,
    build_interaction_plan,
)


def test_simplify_intent_prefers_simple_depth_and_shorter_retrieval() -> None:
    plan = build_interaction_plan(
        student_message="Can you simplify this and make it easier?",
        interaction_intent=None,
        requested_depth=None,
    )

    assert plan.intent == "simplify"
    assert plan.depth == "simple"
    assert plan.retrieval_top_k == 2
    assert plan.allow_curriculum_expansion is False


def test_example_intent_allows_neighbor_expansion_and_more_items() -> None:
    plan = build_interaction_plan(
        student_message="Give me another example from this lesson.",
        interaction_intent=None,
        requested_depth=None,
    )

    assert plan.intent == "give-example"
    assert plan.retrieval_top_k == 5
    assert plan.allow_curriculum_expansion is True
    assert any("example" in rule.lower() for rule in plan.response_shape_rules)


def test_step_by_step_intent_uses_numbered_steps() -> None:
    plan = build_interaction_plan(
        student_message="Can you explain this step by step?",
        interaction_intent=None,
        requested_depth=None,
    )

    assert plan.intent == "explain-step-by-step"
    assert plan.retrieval_top_k == 4
    assert plan.allow_curriculum_expansion is True
    assert any("numbered steps" in rule.lower() for rule in plan.response_shape_rules)


def test_clarify_confusion_has_direct_clarification_rules() -> None:
    plan = build_interaction_plan(
        student_message="I'm confused, can you clarify?",
        interaction_intent=None,
        requested_depth=None,
    )

    assert plan.intent == "clarify-confusion"
    assert plan.depth == "standard"
    assert plan.retrieval_top_k == 3
    assert plan.allow_curriculum_expansion is False


def test_instruction_block_mentions_grounding_and_depth() -> None:
    plan = build_interaction_plan(
        student_message="Please simplify this.",
        interaction_intent=None,
        requested_depth=None,
    )
    block = build_interaction_instruction_block(plan)

    assert "INTERACTION MODE" in block
    assert "Intent: simplify" in block
    assert "Depth: simple" in block
    assert "Use only lesson-scoped retrieved material" in block
