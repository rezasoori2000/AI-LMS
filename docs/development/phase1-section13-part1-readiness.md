# Phase 1 — Section 13, Part 1: Readiness Checklist

**Bounded Learner Model and Tutor Memory Architecture Foundations**

---

## Status: ✅ COMPLETE

---

## Checklist

### Learner model boundaries

- [x] Purpose of learner memory defined (what it is, what it is not)
- [x] Field set is bounded and explicit (no open-ended blob storage)
- [x] Excluded fields list documented (psychological labels, telemetry, ability ranking)
- [x] Layer separation defined: A (explicit) / B (derived) / C (short-lived) / canonical records
- [x] Canonical academic records (`LessonProgress`, `QuestionAnswerRecord`, `Enrollment`) are read-only — never written by this layer
- [x] Raw interaction history (`AiConversation`, `AiMessage`) is read-only — never written by this layer

### Learner memory models (`app/models/learner_memory.py`)

- [x] `LearnerMemorySlice` — aggregate with all 3 layers; keyed by (user_id, tenant_id)
- [x] `WeakTopicRecord` — evidence_count enforced ≥ `MIN_WEAK_TOPIC_EVIDENCE`
- [x] `MisconceptionRecord` — evidence_count enforced ≥ `MIN_MISCONCEPTION_EVIDENCE`
- [x] `ExplanationStyleSignal` — confidence is `'explicit'` or `'inferred_from_repeated_request'` only
- [x] `PaceSignalRecord` — explicit only; never auto-inferred from speed telemetry
- [x] `FrictionSignal` — short-lived; excluded from tutor context slice
- [x] All update input observation types defined
- [x] `LearnerMemoryUpdateInput` with update-boundary rule documentation
- [x] `TutorFacingLearnerContext` — bounded read-only projection with `has_memory` guard
- [x] Evidence threshold constants (`MIN_WEAK_TOPIC_EVIDENCE=2`, `MIN_MISCONCEPTION_EVIDENCE=2`) are named, testable, not inlined
- [x] Context slice caps (`MAX_WEAK_TOPICS_IN_CONTEXT=3`, `MAX_MISCONCEPTIONS_IN_CONTEXT=2`) documented

### Learner memory service (`app/services/learner_memory_service.py`)

- [x] `LearnerMemoryStore` abstract interface — replaceable without code changes
- [x] `InMemoryLearnerMemoryStore` — Phase 1 in-memory implementation (not thread-safe; replace before production)
- [x] `LearnerMemoryService` — sole writer of learner memory
- [x] Friction: direct store, capped, oldest evicted ✓
- [x] Explicit style: direct store ✓
- [x] Explicit pace: direct store, evidence_count increments ✓
- [x] Non-explicit style: pending count, promotes at ≥ 2 ✓
- [x] Non-explicit pace: discarded (never stored) ✓
- [x] Weak topics: pending → threshold → promoted; already-promoted → increment ✓
- [x] Misconceptions: same threshold/promotion pattern ✓
- [x] Eviction rules: `MAX_WEAK_TOPICS_STORED`, `MAX_MISCONCEPTIONS_STORED` enforced ✓
- [x] `get_tutor_facing_context()` returns bounded projection (most-recent-first, capped) ✓
- [x] `clear_session_pending()` discards below-threshold observations ✓

### Learner context slice formatter (`app/services/learner_context_slice.py`)

- [x] `format_learner_memory_block()` returns empty string when `has_memory=False`
- [x] Hedged language contract enforced (no 'struggles', 'cannot', certainty language)
- [x] Hedging caveat note in block header ('observed tendencies — not certainties')
- [x] Friction signals excluded from formatted block
- [x] Raw user_id excluded from formatted block
- [x] Block labeled 'LEARNER MEMORY' / 'END OF LEARNER MEMORY' for parsing clarity

### Tutor model integration (`app/models/tutor.py`)

- [x] `TutorContextSnapshot.learner_memory_slice: TutorFacingLearnerContext | None = None`
- [x] Optional field — existing callers unaffected (backward compatible)
- [x] Teacher guide architecture note in docstring
- [x] `teaching_guide_available` documented as planned Phase 3 field (not yet added)

### Tutor prompt integration (`app/core/tutor_prompts.py`)

- [x] 5-block prompt structure documented (was 4 blocks)
- [x] Block 4 (learner memory) silently omitted when `learner_memory_slice is None` or `has_memory=False`
- [x] Block 4 appears before guardrail rules (maintains recency-bias contract)
- [x] Phase 3 teacher guide block position documented in module docstring

### C# boundary notes (`LMS.Domain/Personalization/LearnerMemoryBoundaryNotes.cs`)

- [x] Canonical-record isolation contract documented
- [x] Layer separation documented
- [x] Update-boundary rules documented
- [x] Tutor-facing context slice boundaries documented
- [x] Teacher guide architecture compatibility documented
- [x] Phase 3 migration path documented
- [x] GDPR deletion order documented

### Tests (`ai-service/tests/test_learner_memory.py`)

- [x] Group A: Evidence threshold rules (weak topics, misconceptions)
- [x] Group B: Direct-update rules (friction, explicit style, explicit pace)
- [x] Group C: Inferred-update rules (non-explicit style repeated requests)
- [x] Group D: Eviction / cap rules
- [x] Group E: Tutor-facing context slice boundaries
- [x] Group F: Learner context slice formatter (format_learner_memory_block)
- [x] Group G: System prompt integration (memory block in build_system_prompt)
- [x] Group H: Canonical-record isolation (service must not import academic record types)
- [x] Group I: Boundary contracts (constants, model validation)

### Architecture documentation

- [x] `docs/architecture/learner-memory-foundations.md` — full architecture reference
- [x] `docs/development/phase1-section13-part1-readiness.md` — this document

---

## Known Carry-Forward Items

| Item | Deferred to | Notes |
|------|-------------|-------|
| `LearnerProfile` entity + EF Core config + migration | Phase 3 | Design fully documented in `DesignNotes.cs` |
| Backend `TutorContextAssembler` learner-memory population | Phase 3 | Depends on Phase 3 DB entity |
| Cross-session pending observations persistence | Phase 3 | In-memory pending is session-local only in Phase 1 |
| Teacher guide content source + prompt block | Phase 3 | Architecture direction documented; no code needed in Phase 1 |
| `teaching_guide_available` field in `TutorContextSnapshot` | Phase 3 | Documented in tutor.py docstring only |
| Tutor-initiated learner observation endpoint | Phase 3 | Needs API design review |
| GDPR student data deletion service | Phase 3 | Deletion order documented in `LearnerMemoryBoundaryNotes.cs` |

---

## Test Counts

| Suite | Before Part 1 | After Part 1 |
|-------|--------------|--------------|
| AI service (pytest) | 51 | ~85 (51 existing + ~35 new learner memory tests) |
| Backend (.NET xUnit) | 158 | 158 (no backend code changes) |
| Frontend (Vitest) | 47 | 47 (no frontend changes) |

---

## Section 13, Part 1: ✅ COMPLETE
