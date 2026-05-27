# Learner Memory and Tutor Memory Architecture Foundations

**Phase 1 — Section 13, Part 1**

---

## 1. Purpose

This document defines the bounded, privacy-aware, explainable learner-model and tutor-memory foundations for the AI virtual tutor. The goal is to enable the tutor to behave like a private teacher who remembers the learner over time, while staying:

- **Maintainable** — explicit field set, testable update rules, no vague blobs
- **Safe** — no psychological profiling, no diagnosis-like labels, no high-risk inferences
- **Compatible** — with lesson-grounded tutoring, the existing RAG/retrieval stack, future optional teacher-guide content, future TTS/STT, and any LLM provider

This is a **foundations document**, not a full personalization engine. Building a full personalization engine is explicitly deferred to Phase 3.

---

## 2. Assumptions Used

1. Students are minors (or may be minors). The minimum-data principle is strictly applied. No sensitive profiling fields are added.
2. The canonical identity anchor is `User.Id` (not `StudentProfile.Id`), consistent with the existing `DesignNotes.cs` decision.
3. Phase 1 has no real LLM provider wired. Learner memory architecture is tested against the in-memory store and prompt formatters.
4. The backend (ASP.NET Core) is the long-term owner of the learner profile store (PostgreSQL). The AI service uses an in-memory store for Phase 1 only.
5. Every subject may or may not have a teacher guide. The system must never assume a teacher guide exists.
6. The tutor should never overclaim what it knows about a learner. All persisted observations must have explicit educational justification and must use hedged language in prompts.

---

## 3. Recommended Learner-Model and Memory Boundaries

### 3.1 What to store

| Field | Type | Layer | Update rule |
|-------|------|-------|-------------|
| `explanation_style` | `ExplanationStyleSignal` | A (explicit) | Explicit statement: direct. Inferred: ≥ 2 requests. |
| `pace_signal` | `PaceSignalRecord` | A (explicit) | Explicit signal only. Never from speed telemetry. |
| `weak_topics` | `list[WeakTopicRecord]` | B (derived) | ≥ `MIN_WEAK_TOPIC_EVIDENCE` (2) independent observations. |
| `observed_misconceptions` | `list[MisconceptionRecord]` | B (derived) | ≥ `MIN_MISCONCEPTION_EVIDENCE` (2) independent observations. |
| `recent_friction_signals` | `list[FrictionSignal]` | C (short-lived) | Direct (single interaction). Capped at 5. Evict oldest. |

### 3.2 What NOT to store

| Excluded field | Reason |
|----------------|--------|
| Personality / cognitive-style labels (MBTI, VARK, Gardner) | Speculative; inappropriate for automated inference |
| Health, disability, mental-health inferences | Sensitive; legally risky; not educationally grounded |
| Engagement-addiction metrics (streak counters, loss-aversion) | Gamification risk; not a teaching need |
| General ability / IQ-style ranking | Diagnoses without professional context |
| Study time patterns / attendance telemetry | Surveillance concern; not needed for tutoring |
| Inferred labels from a single AI interaction | No repeated evidence; cannot be validated |
| `RecurringWeakArea` as a string label from one AI guess | Hallucination risk; must be observation-backed |

### 3.3 Layer separation

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Layer A — Explicit Preferences  (student-set or clearly observed)        │
│ explanation_style · pace_signal                                          │
└─────────────────────────────────────────────────────────────────────────┘
          │ reads from (update input)         never writes to ↓
┌─────────────────────────────────────────────────────────────────────────┐
│ Layer B — Derived Academic Signals  (threshold-gated, periodically       │
│           refreshed from observation data)                               │
│ weak_topics · observed_misconceptions                                    │
└─────────────────────────────────────────────────────────────────────────┘
          │ reads from (never writes to)      observation layer ↓
┌─────────────────────────────────────────────────────────────────────────┐
│ Layer C — Short-lived Session-adjacent Signals                           │
│ recent_friction_signals  (never promoted to A/B; never in tutor prompt) │
└─────────────────────────────────────────────────────────────────────────┘
          │ sourced from (read-only — never modified by personalization)
┌─────────────────────────────────────────────────────────────────────────┐
│ Canonical Academic Records  (Phase 1 tables — immutable to this layer)  │
│ LessonProgress · QuestionAnswerRecord · Enrollment                       │
│ AiConversation · AiMessage                                               │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Files Introduced (Phase 1, Section 13, Part 1)

| File | Type | Role |
|------|------|------|
| `ai-service/app/models/learner_memory.py` | New | Pydantic models: `LearnerMemorySlice`, update input types, `TutorFacingLearnerContext`, evidence constants |
| `ai-service/app/services/learner_memory_service.py` | New | `LearnerMemoryService`, `InMemoryLearnerMemoryStore`, update-boundary rule engine |
| `ai-service/app/services/learner_context_slice.py` | New | `format_learner_memory_block()` — tutor prompt block formatter |
| `ai-service/app/models/tutor.py` | Updated | Added `learner_memory_slice: TutorFacingLearnerContext | None = None` to `TutorContextSnapshot`; teacher-guide note in docstring |
| `ai-service/app/core/tutor_prompts.py` | Updated | Added optional learner memory block (block 4 of 5) to `build_system_prompt`; updated docstring |
| `ai-service/tests/test_learner_memory.py` | New | Unit tests for all of the above (9 test classes, ~35 tests) |
| `backend/src/LMS.Domain/Personalization/LearnerMemoryBoundaryNotes.cs` | New | C# boundary doc: isolation contract, update rules, teacher-guide compat, Phase 3 migration path |
| `docs/architecture/learner-memory-foundations.md` | New | This document |
| `docs/development/phase1-section13-part1-readiness.md` | New | Section 13, Part 1 readiness checklist |

---

## 5. Update-Boundary Rules

### 5.1 Direct update (single interaction — stored immediately)

| Signal | Stored as | Condition |
|--------|-----------|-----------|
| `FrictionObservation` | `FrictionSignal` | Always. Capped at `MAX_FRICTION_SIGNALS_STORED` (5). Oldest evicted. |
| `ExplanationStyleObservation` (explicit) | `ExplanationStyleSignal(confidence='explicit')` | `is_explicit_statement=True` |
| `PaceObservation` (explicit) | `PaceSignalRecord` | `is_explicit_statement=True` |

### 5.2 Requires repeated evidence

| Signal | Threshold | Stored as |
|--------|-----------|-----------|
| `WeakTopicObservation` | ≥ `MIN_WEAK_TOPIC_EVIDENCE` (2) independent observations | `WeakTopicRecord(evidence_count=N)` |
| `MisconceptionObservation` | ≥ `MIN_MISCONCEPTION_EVIDENCE` (2) observations | `MisconceptionRecord(evidence_count=N)` |
| `ExplanationStyleObservation` (inferred) | ≥ 2 same-style requests | `ExplanationStyleSignal(confidence='inferred_from_repeated_request')` |

### 5.3 Remains temporary (session-local, discarded if threshold never met)

- Single-interaction weak topic observation that never repeats
- Single non-explicit style request below the 2-request threshold
- Pending observations are held in `LearnerMemoryService._pending` (in-memory dict)
- Discarded explicitly via `clear_session_pending(user_id)` at session end
- **Phase 3**: persist pending in a `learner_pending_observations` table

---

## 6. Tutor-Facing Context Slice

Only a bounded subset of learner memory enters the system prompt:

| Field | Included in prompt | Cap |
|-------|-------------------|-----|
| `weak_topics` | Yes | `MAX_WEAK_TOPICS_IN_CONTEXT` (3), most-recent first |
| `observed_misconceptions` | Yes | `MAX_MISCONCEPTIONS_IN_CONTEXT` (2), most-recent first |
| `explanation_style` | Yes (if any) | One value |
| `pace_signal` | Yes (if any) | One value |
| `recent_friction_signals` | **No** | Excluded entirely |
| Raw `user_id` | **No** | Excluded from formatted text |

### Hedging language contract

The `format_learner_memory_block()` function enforces this language contract:

```
✓  'has shown difficulty with'    ✗  'struggles with'
✓  'tends to prefer'              ✗  'requires' / 'cannot learn without'
✓  'observed tendencies'          ✗  'definitive profile'
✓  'remain open to...'            ✗  'always prefers'
```

The block header includes an explicit caveat:  
*"These are observed educational tendencies — not certainties. Adapt where relevant, and remain open to different behaviour this session."*

### Prompt block position (5-block structure)

```
1. Role + scope declaration
2. Lesson content block (primary knowledge source)
3. Optional hint question block
4. Optional learner memory block  ← NEW (omitted when has_memory=False)
5. Guardrail rules (last — recency bias)
```

---

## 7. Teacher Guide Architecture

### Current state (Phase 1)

There is no teacher guide content source. The tutor uses the student lesson content (`lesson_content`) for both WHAT to teach and HOW to explain.

### Planned architecture (Phase 3)

| Source | Role | Priority |
|--------|------|----------|
| `lesson_content` (student textbook) | WHAT-TO-STUDY — canonical learner-facing content | Always present |
| Teacher guide (optional) | HOW-TO-TEACH — pedagogical method guidance | Higher priority for tutoring style when available |

**Rules:**
1. The teacher guide is **optional** — the system must never assume it exists for any given lesson/subject.
2. When a teacher guide exists, it should inform HOW the tutor explains (analogies, sequence, difficulty progression), not WHAT is studied.
3. The student textbook remains the canonical study content regardless of teacher guide availability.
4. Learner memory and teacher guide are **orthogonal** — learner memory describes the learner; the teacher guide describes the subject's instructional approach.

### Planned prompt structure with teacher guide (Phase 3)

```
1. Role + scope
2. Lesson content (student textbook — WHAT)
3. Teacher guide context (HOW-TO-TEACH — when available)
4. Optional hint block
5. Optional learner memory block
6. Guardrail rules
```

### Planned TutorContextSnapshot field (Phase 3 only — not yet added)

```python
# teaching_guide_available: bool = False
# """
# When True, the backend also passes teacher-guide retrieval context.
# The tutor should use the guide for pedagogical style (HOW to teach),
# not as additional factual content. Lesson content remains the authoritative
# learner-facing study source.
# """
```

---

## 8. Provider Compatibility

The learner memory architecture has zero dependency on any specific LLM provider:

- `LearnerMemoryService` is pure Python with no LLM calls
- `InMemoryLearnerMemoryStore` is a plain dict — replaceable with any store
- `format_learner_memory_block()` produces a plain string injected as a prompt block
- The `TutorContextSnapshot.learner_memory_slice` field is provider-agnostic
- The same learner memory slice works with any future model (OpenAI, Anthropic, local Ollama, cloud Azure)

---

## 9. Future TTS/STT Compatibility

The learner memory architecture is TTS/STT-compatible:

- The prompt block is plain prose (no markdown formatting that would be read aloud)
- No learner memory fields depend on the modality (text vs. voice)
- Modality-specific signals (e.g., "prefers spoken explanation") can be added to `ExplanationStyleValue` in Phase 3 without breaking the current architecture

---

## 10. Deferred Items

| Item | Deferred to | Reason |
|------|-------------|--------|
| `LearnerProfile` entity + EF Core config + migration | Phase 3 | No premature DB schema churn in Phase 1 |
| `ILearnerProfileService` .NET interface + implementation | Phase 3 | Phase 1 AI-service in-memory store is sufficient for testing |
| Backend `TutorContextAssembler` learner-memory population | Phase 3 | Depends on Phase 3 DB entity |
| Cross-session pending observations persistence (DB table) | Phase 3 | In-memory pending is sufficient for Phase 1 architecture validation |
| `TopicMasterySnapshot` refresh job | Phase 3 | Requires DB + background job infrastructure |
| Teacher guide field (`teaching_guide_available`) in `TutorContextSnapshot` | Phase 3 | Teacher guide ingestion pipeline not yet built |
| GDPR student data deletion service | Phase 3 | Requires full LearnerProfile entity set |
| Per-subject mastery percentages | Phase 3 | Requires `TopicMasterySnapshot` backed by `QuestionAnswerRecord` joins |
| Tutor-initiated learner observation endpoint | Phase 3 | Needs API design review; Phase 1 uses service directly |
| Multi-tenant pending observations isolation | Phase 3 | In-memory store uses (user_id, tenant_id) key; DB impl must do the same |

---

## 11. Risks and Trade-offs

### Risks

| Risk | Mitigation |
|------|------------|
| Weak topic label quality depends on AI-generated descriptions | Phase 1: labels are architecture-tested only. Phase 3: validate label generation with human review before enabling auto-population |
| Pending observations in-memory not persisted across AI service restarts | Acceptable in Phase 1; Phase 3 migrates to DB table |
| Evidence threshold of 2 may be too low for noisy interactions | Threshold is a named constant (`MIN_WEAK_TOPIC_EVIDENCE`). Can be raised without code changes; explicitly tested |
| Learner memory block increases prompt token usage | Block is narrow (≤ 3 weak topics + ≤ 2 misconceptions + style + pace). Max ~200 tokens. Acceptable within 600-token response budget |

### Trade-offs

| Trade-off | Decision |
|-----------|----------|
| Rich memory vs. minimal memory | Chose minimal. Fewer fields = less hallucination risk, less overclaiming, easier GDPR compliance |
| Auto-inferred signals vs. explicit-only | Explicit-only for style/pace. Threshold-gated for academic signals. No passive behavioral inference |
| Session-only vs. persistent memory | Persistent for Layer A/B (real value). Session-only for Layer C (friction). Pending for below-threshold |
| Memory in AI service vs. backend | AI service holds in-memory store for Phase 1 testing. Backend owns the store in Phase 3 (PostgreSQL) |
