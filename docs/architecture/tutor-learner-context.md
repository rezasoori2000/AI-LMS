# Tutor-Facing Learner Context Assembly and Safety Boundaries

**Phase 1 — Section 13, Parts 3 and 4**

---

## 1. Purpose

This document describes the boundary between stored learner memory and live tutor behavior. It covers:

- How stored signals are translated into bounded behavioral teaching hints (Part 3)
- What content is permanently forbidden from learner memory (Part 4)
- How learner memory is reset or deleted at different scopes (Part 4)

The earlier foundations document ([Learner Memory Architecture Foundations](learner-memory-foundations.md)) covers what is stored, how evidence accumulates, and how the tutor-facing context slice is projected.

---

## 2. Three-Layer Pipeline

```
LearnerMemorySlice (store)
        │
        │  LearnerMemoryService.get_tutor_facing_context()
        ▼
TutorFacingLearnerContext (Part 1–2 projection)
  · Already capped: ≤ 3 weak topics, ≤ 2 misconceptions
  · Friction signals excluded
  · Stale records filtered by decay policy (is_stale())

        │
        │  TutorLearnerContextAssembler.assemble()
        ▼
BoundedPersonalizationInput (Part 3 — this layer)
  · Relevance-sorted by keyword overlap with lesson title
  · Further capped: ≤ MAX_TOPIC_HINTS (2) topic-level hints
  · Total capped: ≤ MAX_PERSONALIZATION_HINTS (4)
  · Translated into behavioral instructions (HOW to teach)
  · Confidence-aware hedging applied

        │
        │  format_personalization_hints_block()
        ▼
Prompt block: "LEARNER MEMORY" / "END OF LEARNER MEMORY"
  · Injected as block 4 in system prompt (after lesson content, before guardrails)
  · Omitted entirely when has_personalization=False
```

---

## 3. Hint Types and Slot Budget

| Behavior | Max slots | Source category | Always included |
|----------|-----------|-----------------|-----------------|
| `prefer_style` | 1 | EXPLANATION_STYLE | Yes, when present |
| `prefer_pace` | 1 | PACE_SIGNAL | Yes, when present |
| `support_weak_topic` | combined ≤ 2 | WEAK_TOPIC | Relevance-sorted |
| `watch_for_misconception` | combined ≤ 2 | MISCONCEPTION | Relevance-sorted |

- `MAX_PERSONALIZATION_HINTS = 4` — total cap across all hint types
- `MAX_TOPIC_HINTS = 2` — combined cap for `support_weak_topic` + `watch_for_misconception`
- Style and pace hints are subject-level (not lesson-specific) and always included when present
- Topic hints are lesson-relevance sorted; relevant topics win when capacity forces exclusion

---

## 4. Relevance Filtering

Phase 1 uses keyword overlap between non-stop-word tokens from the lesson title and the topic label or misconception description:

```
extract_topic_terms("Introduction to Fractions") → ["introduction", "fractions"]
is_lesson_relevant("mixed numbers", ["fractions"]) → False  (no overlap in Phase 1)
is_lesson_relevant("mixed numbers", [])            → True   (empty terms = permissive)
```

**Phase 1 limitation:** Semantic relationships are not captured. "Mixed numbers" will not match "Fractions" even though they are sub-topics. This is acceptable because:
- Single topic signals are always included regardless of relevance
- Multiple topics are sorted (relevant first) — not hard-excluded
- `is_lesson_relevant=False` is informational; topics are not blocked

**Phase 3:** Replace with embedding similarity between lesson content and topic labels.

---

## 5. Confidence and Hedging

Hint instructions use hedged language controlled by confidence level:

| Confidence | Instruction strength | Example phrasing |
|------------|---------------------|------------------|
| `HIGH` | Definitive guidance | "This student has shown consistent difficulty with..." |
| `MEDIUM` | Standard guidance | "This student has shown difficulty with..." |
| `LOW` | Gentle suggestion | "You may notice some difficulty with..." |

Confidence is derived from evidence count via `evidence_count_to_confidence()`:
- < `MIN_*_EVIDENCE` + 1 → LOW
- ≥ 2 × threshold → HIGH
- Otherwise → MEDIUM

No hint ever asserts certainty, diagnoses, or personality traits.

---

## 6. Safety Guardrails

### 6.1 What is permanently forbidden

Four pattern groups, compiled to word-boundary regex (`\b...\b`), block any text containing:

| Group | Examples |
|-------|---------|
| Diagnostic labels | `adhd`, `autism`, `dyslexia`, `learning disability`, `iep`, `developmental delay` |
| Psychological claims | `anxiety disorder`, `clinical depression`, `ptsd`, `bipolar disorder`, `ocd` |
| Ability ranking | `stupid`, `dumb`, `slow learner`, `unteachable`, `remedial`, `low iq` |
| Engagement manipulation | `addiction score`, `dopamine loop`, `engagement optimization` |

Word-boundary matching prevents false positives:
- `"automorphism"` does NOT match `autism` ✓
- `"the Great Depression"` does NOT match `clinical depression` ✓
- `"bipolar coordinates"` does NOT match `bipolar disorder` ✓

### 6.2 Guardrail enforcement point

`LearnerMemoryService.apply_update()` validates **all** topic labels and misconception descriptions through `LearnerMemorySafetyGuard.validate_update_observations()` **before any state change**. If any violation is found, `LearnerMemoryGuardrailError` is raised and the entire update is rejected atomically.

The guard operates in the service layer (code-level), independent of prompt wording. Prompt guardrails are additional defence-in-depth, not the primary safety mechanism.

### 6.3 Safe content examples

```
✓ "fractions > mixed numbers"          — curriculum topic label
✓ "confuses numerator with denominator" — factual educational misconception
✓ "asked for step-by-step explanation three times" — style signal
✓ "re-explained photosynthesis in one session" — friction signal
```

---

## 7. Reset and Delete Scopes

Two separate operations with different semantics:

### reset_memory(scope) — Soft reset

Clears selected fields from the slice but keeps the slice record in the store.

| Scope | What is cleared |
|-------|-----------------|
| `SESSION_PENDING` | Session-local pending observations only; persisted slice untouched |
| `TOPIC_SIGNALS` | `weak_topics`, `observed_misconceptions` in slice + related pending |
| `PREFERENCES` | `explanation_style`, `pace_signal` in slice + inferred_styles pending |
| `FRICTION` | `recent_friction_signals` in slice only |
| `ALL` | All fields in slice + all pending; slice record remains in store |

**Critical invariant:** Pending observations are cleared **before** checking whether a slice exists. This prevents stale pending counts from surviving a reset-before-first-promotion scenario.

### delete_user_memory() — Hard delete

Removes the `LearnerMemorySlice` from the store entirely. Clears all pending observations.

| Use case | Operation |
|----------|-----------|
| Start of a new school year (keep account) | `reset_memory(ALL)` |
| Account closure / right-to-erasure | `delete_user_memory()` |
| End of a tutor session (discard unpromotable) | `reset_memory(SESSION_PENDING)` |
| Parent requests clean slate, not account deletion | `reset_memory(ALL)` |

---

## 8. Memory Retention Tiers

Each `CategoryPolicy` carries a `retention_tier` annotation for Phase 3 scheduled purge decisions:

| Category | Tier | Decay days | Meaning |
|----------|------|-----------|---------|
| `FRICTION_SIGNAL` | `EPHEMERAL` | 7 | Session-adjacent; purge quickly |
| `PACE_SIGNAL` | `SHORT_TERM` | 60 | Semester-scale preference |
| `WEAK_TOPIC` | `MEDIUM_TERM` | 90 | Academic difficulty signal |
| `MISCONCEPTION` | `MEDIUM_TERM` | 90 | Academic difficulty signal |
| `EXPLANATION_STYLE` | `EXPLICIT_PREFERENCE` | 180 | Directly stated; longest retention |

In Phase 1, `decay_days` is used for soft staleness filtering only. Hard deletion by tier is a Phase 3 scheduled-job concern.

---

## 9. Prompt Structure (5-block order)

```
[1] Role + scope declaration
[2] Lesson content block              ← primary knowledge source
[3] Optional hint block               ← present only when hintForQuestionId is set
[4] Optional learner memory block     ← this module; omitted when has_personalization=False
[5] Guardrail rules                   ← last (recency bias)
```

**Teacher guide note (Phase 3):** If a teacher guide block is added, it should appear between block 2 and block 3:
```
[1] Role
[2] Lesson content
[2b] Teacher guide (HOW to teach — optional)
[3] Hint block
[4] Learner memory block
[5] Guardrails
```

Teacher guide content (subject-level pedagogy) is **orthogonal** to learner memory content (learner-specific signals). They are assembled and formatted independently.

---

## 10. Module Map

| Module | Role |
|--------|------|
| `app/models/learner_memory.py` | All model types; boundary constants; `LearnerMemoryUpdateInput`; `TutorFacingLearnerContext` |
| `app/models/learner_memory_policy.py` | `CategoryPolicy`, `CATEGORY_POLICIES`, evidence/confidence/retention enums |
| `app/services/learner_memory_service.py` | Sole writer; update-boundary rules; pending accumulator; `reset_memory`; `delete_user_memory` |
| `app/services/learner_memory_safety.py` | Guardrails; forbidden patterns; `LearnerMemoryResetScope`; `LearnerMemoryGuardrailError` |
| `app/services/learner_context_slice.py` | Legacy formatter (`format_learner_memory_block`); superseded by Part 3 in prompt path; retained for test compatibility |
| `app/services/tutor_learner_context.py` | Assembler; `BoundedPersonalizationInput`; `PersonalizationHint`; `format_personalization_hints_block` |
| `app/core/tutor_prompts.py` | System prompt builder; wires assembler into block 4 |

---

## 11. Deferred to Phase 3

| Item | Reason deferred |
|------|----------------|
| Semantic relevance (embedding similarity) | Requires embedding infrastructure not yet built |
| Subject-level filtering by `subject_id` UUID | `TutorContextSnapshot` carries `subject_name` only; no FK lookup table in Phase 1 |
| Scheduled hard-deletion job by retention tier | GDPR operational concern; needs audit log and actor tracking |
| Audit log for reset/delete (actor, timestamp, scope) | Requires dedicated DB table and backend endpoint |
| Parent-facing reset/delete UI and API endpoint | Blocked on parent portal Phase 2 feature work |
| API-boundary validation for memory observations | Guard currently runs in service layer only; HTTP-level validation is Phase 3 |
| Per-category retention enforcement (beyond soft staleness) | Phase 3 scheduled purge job |
| Cross-session friction aggregation into topic hints | Would require persistent pending table (Phase 3) |
