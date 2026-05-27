# Phase 1 Section 13 — Persistent Learner Model: Validation and Closeout

**Section 13 Parts 1–5 — Readiness Checklist**

---

## 1. Assumptions Used

1. Students are minors (or may be minors). The minimum-data principle is strictly applied throughout.
2. The canonical identity anchor is `User.Id` (not `StudentProfile.Id`), consistent with `DesignNotes.cs`.
3. Phase 1 uses `InMemoryLearnerMemoryStore` — no real DB persistence yet. Phase 3 replaces with PostgreSQL.
4. Pending observations are session-local (in-memory dict). They are **not** persisted across server restarts.
5. The system must function with or without a teacher guide. Teacher-guide content is optional and orthogonal.
6. The system must work with any LLM provider (cloud or local/self-hosted). Memory assembly is provider-agnostic.
7. All rule violations detected during validation are noted as accepted risks or Phase 3 items; none block forward progress.

---

## 2. Section 13 Validation Summary

### Part 1 — Model boundaries and memory architecture
✅ **Valid.** `LearnerMemorySlice` is narrowly scoped. 3-layer model (A/B/C) is explicit and documented.  
✅ Threshold gating (`MIN_WEAK_TOPIC_EVIDENCE=2`, `MIN_MISCONCEPTION_EVIDENCE=2`) prevents single-interaction promotion.  
✅ `LearnerMemoryService` is the sole writer — validated by code (no other service imports learner memory write path).  
✅ Friction signals are deliberately excluded from tutor context — not an omission.  
✅ Pending observations are session-local and explicitly documented as non-persistent.  

### Part 2 — Categories, update rules, evidence and confidence policy
✅ **Valid.** `CategoryPolicy` frozen dataclass — all rules are data-driven and testable.  
✅ Five categories with clear evidence source constraints. No category sprawl.  
✅ Recovery observations (`SuccessfulRecoveryObservation`, `MisconceptionResolutionObservation`) provide counter-signals.  
✅ Stale filtering at context-slice time (soft decay) — does not delete from store.  
✅ `EvidenceSource` enum prevents arbitrary string evidence tags.  

### Part 3 — Tutor-facing context assembly
✅ **Valid.** `BoundedPersonalizationInput` is the correct abstraction for the boundary.  
✅ `MAX_PERSONALIZATION_HINTS=4`, `MAX_TOPIC_HINTS=2` prevents prompt bloat.  
✅ Style/pace hints always included; topic hints relevance-sorted, not hard-excluded.  
✅ `format_personalization_hints_block()` preserves "LEARNER MEMORY"/"END OF LEARNER MEMORY" headers for backwards compatibility.  

**Accepted ambiguity:** `BoundedPersonalizationInput.subject_id` field stores a subject name string (not a UUID) in Phase 1. The field is already documented with this caveat. Renaming to `subject_name` is deferred to Phase 3 when it will carry an actual FK alongside.

### Part 4 — Safety, privacy, reset/deletion
✅ **Valid.** Word-boundary regex prevents false positives on legitimate educational vocabulary.  
✅ `LearnerMemoryGuardrailError` raised before any state change — atomicity guaranteed.  
✅ `LearnerMemoryResetScope` covers all lifecycle events (session, topic, preference, friction, full).  
✅ `reset_memory` clears pending unconditionally (before the `if slice_ is None: return` guard — a subtle but critical ordering).  
✅ `delete_user_memory()` hard-delete is separate from `reset_memory(ALL)` soft-reset.  
✅ `MemoryRetentionTier` annotations present on all `CategoryPolicy` entries.  

---

## 3. Naming Observations

| Item | Status | Note |
|------|--------|------|
| `BoundedPersonalizationInput.subject_id` stores a name, not a UUID | Accepted / Phase 3 | Already documented in code and module docstring |
| `learner_context_slice.py` / `format_learner_memory_block()` is superseded in the prompt path | Accepted / Phase 3 | Legacy formatter kept for backwards-compat tests; **not dead code** in tests but dead code in production path |
| `TutorFacingLearnerContext` vs `BoundedPersonalizationInput` two-step projection | Correct design | Layer 2→3 split is intentional: staleness filtering in Layer 2, relevance+hint translation in Layer 3 |
| `_UserPending` / `_PendingEntry` are private dataclasses | Correct | Not part of public API; Phase 3 replaces with a DB table |

---

## 4. Small Cleanup Items (Now)

These are the only items that should be done before moving on:

1. **README update** — Section 13 only shows Part 1 status. Parts 2–4 and the closeout must be added.
2. **Architecture doc** — `docs/architecture/tutor-learner-context.md` was missing; created in Part 5.
3. **No code changes required** — All code, tests, and inline documentation are consistent and complete.

---

## 5. Testing Checklist

### Unit tests — all present ✅

| Test file | Coverage |
|-----------|---------|
| `test_learner_memory.py` (46 tests) | Update-boundary rules, eviction, tutor context projection, formatter, prompt integration, canonical isolation |
| `test_learner_memory_policy.py` (52 tests) | Category policies, evidence sources, confidence levels, decay/staleness, CategoryPolicy structure |
| `test_tutor_learner_context.py` (69 tests) | Assembler output, relevance sorting, hint caps, confidence-driven wording, formatter output |
| `test_learner_memory_safety.py` (79 tests) | Guardrail blocks (A-C), safe content passes (D), violation collection (E), apply_update integration (F), all reset scopes (G-K), hard delete (L), retention tiers (M) |

### Integration tests — status
- `test_learner_memory.py` group G: system prompt integration with `build_system_prompt` — ✅ present
- `test_learner_memory.py` group H: canonical-record isolation — ✅ present
- End-to-end memory → prompt path exercised in group G

### Missing (acceptable for Phase 1)
- No HTTP-layer integration test for learner memory observations (guardrail runs at service layer; HTTP validation deferred to Phase 3)
- No cross-session persistence test (pending observations are in-memory, not persisted in Phase 1)
- No contract test for `BoundedPersonalizationInput` as a cross-service DTO (it doesn't cross a service boundary yet in Phase 1)

### Regression tests for privacy / guardrail
✅ Groups A, B, C in `test_learner_memory_safety.py` — forbidden content is blocked  
✅ Group D — false-positive tests (`automorphism` ≠ `autism`; `"the Great Depression"` context)  
✅ Group F — atomicity guarantee tested (`apply_update` does not mutate state on guardrail violation)  

---

## 6. Readiness Checklist for Next Sections

### For TTS/STT foundations
- [x] Tutor prompt structure is stable (5-block system prompt, clean separation of concerns)
- [x] `build_conversation_prompt()` formats turns as `Student: / Tutor:` — compatible with TTS transcript display
- [x] No STT-specific preprocessing in learner memory — safe to add a speech-input observation type later
- [ ] Phase 3: Add `SPEECH_PATTERN_REPEATED` as an EvidenceSource value if STT yields repeatable signals

### For tutor-led interaction flow
- [x] Interaction intent model exists (`TutorInteractionPolicy`, intents: explain/simplify/give-example/etc.)
- [x] Personalization hints enter the prompt (block 4); interaction intent shapes retrieval (separate concern)
- [x] Suggested follow-ups are metadata, not autonomous branching — safe for controlled interaction flow
- [ ] Phase 2: Wire `hintForQuestionId` in `TutorPanel` UI (noted in Section 11 deferred items)

### For progress / mastery foundations
- [x] `LessonProgress` + `QuestionAnswerRecord` canonical records are read-only from the learner-memory perspective
- [x] Recovery observations (`SuccessfulRecoveryObservation`) allow positive counter-signals from mastery events
- [x] `LearnerMemorySlice` does NOT store mastery scores or progress percentages — those stay in canonical records
- [ ] Phase 3: `TopicMasterySnapshot` refresh in `CompleteLessonAsync` can generate `SuccessfulRecoveryObservation` inputs

### For parent controls
- [x] `delete_user_memory()` provides the hard-delete primitive for parental erasure requests
- [x] `reset_memory(ALL)` provides a soft-reset for "start fresh" requests
- [x] `LearnerMemoryResetScope` is explicit and covers all expected parent-control scenarios
- [ ] Phase 3: Parent-facing reset/delete UI + backend endpoint (`DELETE /api/parent/children/{id}/memory`)
- [ ] Phase 3: Audit log for reset/delete operations (actor, timestamp, scope)

---

## 7. Deferred Items (Explicitly Not Section 13)

| Item | Phase | Reason |
|------|-------|--------|
| PostgreSQL-backed `LearnerMemoryStore` | Phase 3 | Requires DB schema + migration |
| Persistent pending observations table | Phase 3 | Requires `learner_pending_observations` DB table |
| Scheduled hard-deletion job by retention tier | Phase 3 | GDPR operational concern |
| Audit log for reset/delete operations | Phase 3 | Requires dedicated DB table |
| Parent-facing reset/delete API endpoint | Phase 3 | Blocked on parent portal feature work |
| HTTP-layer guardrail validation | Phase 3 | Guard in service layer is sufficient for Phase 1 |
| Semantic relevance for topic filtering | Phase 3 | Requires embedding infrastructure |
| Subject-level filtering by subject_id UUID | Phase 3 | No FK lookup table available in Phase 1 |
| `subject_id` field rename in `BoundedPersonalizationInput` | Phase 3 | Low impact; rename alongside FK addition |
| GDPR deletion audit trail | Phase 3 | Operational compliance concern |

---

## 8. Risks and Trade-offs

| Risk | Impact | Mitigation |
|------|--------|------------|
| Pending observations lost on server restart (in-memory) | Medium | Documented. In Phase 1, sessions are short. Phase 3 adds persistent table. |
| Keyword relevance may produce false negatives (e.g. "mixed numbers" ≠ "fractions") | Low | Single topics always included. Multiple topics sorted, not blocked. Phase 3: embedding similarity. |
| `format_learner_memory_block()` in `learner_context_slice.py` is dead code in production path | Low | Kept for test backwards-compatibility. Not harmful. Document as legacy. |
| `BoundedPersonalizationInput.subject_id` naming is misleading (stores name, not UUID) | Low | Documented in code. No runtime impact. Rename deferred to Phase 3. |
| Stale soft-decay may exclude a recently relevant topic after a long break | Low | Acceptable Phase 1 behaviour. No hard delete of stale records; they remain in store. |
| No multi-tenant scope isolation in `delete_user_memory` / `reset_memory` | Medium | Phase 1 has no multi-tenant learner memory writes yet. Add tenant-scoped delete before Phase 3 multi-tenancy. |
| Guardrail word-boundary patterns may need updates as curriculum expands | Low | Patterns documented in `learner_memory_safety.py`. Add Phase 3 audit log for violation monitoring. |

---

## 9. Section 13 Completion Verdict

**Section 13 is complete enough to move forward.**

The learner memory foundation is:
- Bounded and explainable (explicit field set, named constants, no vague blobs)
- Privacy-aware and safe for child users (guardrails, forbidden patterns, no psych profiling)
- Tutor-grounded (personalization augments the lesson, never replaces it)
- Reset/delete capable (soft reset + hard delete, all lifecycle scopes covered)
- Tested (312 AI service tests, all passing)
- Documented (architecture docs, inline module docstrings, C# boundary notes)

### What must be done before the next section:
1. **README Section 13 status updated** — Parts 2–4 and closeout added ← do this now
2. **No code changes required** before proceeding

### Questions for the team before proceeding:
1. Should the next section be **TTS/STT foundations**, **tutor-led interaction flow**, or **progress/mastery foundations**? Each is independently unblocked by Section 13.
2. Is the Phase 3 PostgreSQL migration for `LearnerMemoryStore` expected to happen within Phase 2, or only in Phase 3?
3. Should parent memory-reset controls (the UI/API layer) be part of the parent portal Phase 2 work, or explicitly deferred to Phase 3?
