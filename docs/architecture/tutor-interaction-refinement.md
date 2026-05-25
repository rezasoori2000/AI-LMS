# Tutor Interaction Refinement (Section 12, Part 4)

## Purpose

Define a small, testable interaction model that makes the tutor feel more like a
private teacher while staying lesson-grounded and narrow.

---

## Supported Intents

- `explain`
- `simplify`
- `give-example`
- `explain-step-by-step`
- `clarify-confusion`

## Behavior Model

Each intent influences:

- retrieval query shaping
- retrieval item count
- explanation depth
- response shape rules
- suggested follow-up prompts

### Retrieval shaping

- `simplify` → fewer items, higher preference for directly anchored chunks
- `give-example` → more items, allowance for same-chapter neighbor expansion
- `explain-step-by-step` → moderate item count, sequential explanation style
- `clarify-confusion` → focused retrieval and direct clarification tone

### Response shaping

- `simplify` → short, plain-language explanation
- `give-example` → example-first response pattern
- `explain-step-by-step` → numbered steps
- `clarify-confusion` → direct naming of the confusion point, then correction

---

## Current implementation sketch

Implemented in `ai-service/app/services/tutor_interaction_policy.py`:

- `TutorInteractionPlan`
- `build_interaction_plan(...)`
- `build_interaction_instruction_block(...)`

Response metadata contract added in `ai-service/app/models/tutor.py`:

- `interaction_intent` and `requested_depth` on request payload
- `response_mode`, `response_depth`, `suggested_followups` on response payload

---

## Deferred

- Full pedagogy engine
- Personalized teaching style adaptation
- Longer-term learner modeling
- Autonomy over lesson sequencing
- Production evaluation and response optimization
