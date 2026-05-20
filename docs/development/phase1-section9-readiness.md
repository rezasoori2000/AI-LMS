# Phase 1 — Section 9 Readiness Checklist

> Personalization / persistent user-model readiness and Section 9 closeout.  
> Date: 2026-05-20

---

## Section 9 Completion Summary

Phase 1 Section 9 completes the architectural groundwork for future persistent learner modeling
and AI tutoring. No AI runtime, recommendation engine, or adaptive behavior was implemented.
Instead, Section 9 established the correct data-capture foundation, locked the boundary rules,
and documented the path forward — so Phase 3 can implement these features without reworking
Phase 1 persistence.

### What Was Built

| Area | Status |
|------|--------|
| **Part 1 — Architecture boundaries**: `LMS.Domain/Personalization/DesignNotes.cs` created; 6 boundary decisions locked | ✅ |
| **Part 1 — `AiConversation`**: Phase 3 identity-traversal note added (`StudentId → User.Id`) | ✅ |
| **Part 2 — `QuestionAnswerRecord` domain entity**: `(StudentId, LessonId, QuestionId, IsCorrect, AnsweredAt, TenantId)` | ✅ |
| **Part 2 — EF Core config**: `QuestionAnswerRecordConfiguration` + 3 named indexes | ✅ |
| **Part 2 — `ILmsDbContext`**: `QuestionAnswerRecords` DbSet added | ✅ |
| **Part 2 — `StudentService.CompleteLessonAsync`**: persists one record per question per completion | ✅ |
| **Part 2 — Migration**: `AddQuestionAnswerRecordTable` generated | ✅ |
| **Part 3 — Conceptual learner-profile design**: three-layer model, entity shapes, boundary + privacy rules | ✅ (design only) |
| **Part 4 — `DesignNotes.cs` updated**: aligned with Part 3 decisions; removed `TypicalStudyTimeOfDay`, `AiMemoryStoreRef`, `RecurringWeakArea`; added `LearnerPreferences` + `ConsistencySignal` shapes | ✅ |
| **Part 4 — `domain-model-overview.md` updated**: added `QuestionAnswerRecord` + `TeacherStudentAssignment`; added personalization pointer | ✅ |
| **Part 4 — `personalization-readiness.md` created**: authoritative boundary reference for Phase 3 implementors | ✅ |
| **Part 5 — Boundary validation**: `StudentProfile`, `User`, student DTOs, `ILmsDbContext` — all confirmed clean | ✅ |

**Test totals (unchanged through Section 9):** 139 backend xUnit · 41 frontend Vitest

---

## Readiness Checklist for Phase 3 AI Tutor Work

### Observation Data (Phase 1 tables read by Phase 3)

- [x] `LessonProgress` captures completion status and score per student per lesson
- [x] `QuestionAnswerRecord` captures binary correct/incorrect per question per lesson completion
- [x] `(StudentId, LessonId)` index enables efficient per-lesson history queries
- [x] `(StudentId, QuestionId)` index enables mastery/repeat-question analysis
- [x] `Enrollment` captures subject access and status per student
- [x] `AiConversation` + `AiMessage` captures AI tutor session history
- [x] `AiConversation.StudentId` identity traversal note in XML doc: traverse to `User.Id` via `StudentProfile.UserId`

### Architecture Boundaries

- [x] `StudentProfile` carries zero adaptive or behavioral fields (validated in Part 5)
- [x] `User` carries zero adaptive or behavioral fields (validated in Part 5)
- [x] `ILmsDbContext` has no premature personalization `DbSet`s (validated in Part 5)
- [x] `LMS.Domain/Personalization/` namespace reserved; `DesignNotes.cs` present with 6 locked decisions
- [x] Three-layer model documented: Explicit Preferences → Derived Signals → Observation Layer
- [x] Boundary rule documented: observation layer is read-only from Phase 3

### Privacy / Data Governance

- [x] `QuestionAnswerRecord` stores only binary `IsCorrect` — no submitted answer text
- [x] Student-facing DTOs do not expose `CorrectAnswer` or `OptionsJson`
- [x] Exclusion list documented in `DesignNotes.cs` and `personalization-readiness.md`
- [x] GDPR deletion order documented (10-table sequence in FK-safe order)
- [x] `LearnerPreferences` explicit-only invariant documented (no auto-population from inferred signals)
- [x] `ConsistencySignal` factual-count invariant documented (no streak/gamification framing)

### Phase 3 Implementation Readiness

- [x] `LearnerProfile`, `LearnerPreferences`, `TopicMasterySnapshot`, `ConsistencySignal` entity shapes defined in `DesignNotes.cs`
- [x] `LearnerProfile` keyed by `(UserId, TenantId)` — identity anchor documented and justified
- [x] `SubjectId` derivation path documented: `QuestionAnswerRecord.LessonId → Lesson → Chapter → Subject`
- [x] `TopicMasterySnapshot` refresh strategy documented (inline at `CompleteLessonAsync` for MVP, background job later)
- [x] `StudentDataDeletionService` table checklist documented for Phase 3 implementation

### Known Signal Quality Gap (must be handled in Phase 3 mastery computation)

- [ ] `ShortAnswer` questions always produce `IsCorrect = false` in Phase 1 (no AI grading yet). Phase 3 `TopicMasterySnapshot` computation must exclude or weight `ShortAnswer` questions differently, or accept that mastery percent is understated for subjects with `ShortAnswer` questions. Historical Phase 1 records cannot be retroactively corrected.

---

## Known Gaps and Deferred Items

| Gap | Decision | Notes |
|-----|----------|-------|
| Pending migrations not applied | Deferred | `AddTeacherStudentAssignmentTable` + `AddQuestionAnswerRecordTable`. Apply when Docker is running: `dotnet ef database update --project src/LMS.Infrastructure --startup-project src/LMS.Api` |
| `ShortAnswer` AI grading and `IsCorrect` back-fill | Deferred Phase 3 | Phase 3 AI grader will set IsCorrect correctly for new answers; historical records remain `false` |
| `LearnerProfile` entity + EF config + migration | Deferred Phase 3 | Namespace reserved; entity shapes defined in `DesignNotes.cs` |
| `LearnerPreferences` student-facing settings page | Deferred Phase 3 | Requires UX design before backend work |
| `TopicMasterySnapshot` refresh service | Deferred Phase 3 | Inline at `CompleteLessonAsync` first, then background job |
| `ConsistencySignal` computation | Deferred Phase 3 | Depends on `LearnerProfile` landing first |
| `TeacherStudentAssignment.TeacherProfileId` FK | Deferred Phase 3 | When `TeacherProfile` entity is added, a migration will add this FK. Current `TeacherUserId` stays as fallback |
| `Question.TopicId` for finer-grained mastery tagging | Deferred Phase 3 | Separate content-taxonomy sub-task |
| `AuditableEntity.CreatedBy`/`UpdatedBy` population | Deferred Phase 3 | Requires `ICurrentUserService` in `SaveChangesAsync` interceptor |
| Accessibility preferences (`LearnerPreferences`) | Deferred Phase 3+ | UX/WCAG requirements drive the model |
| Teacher observation notes on a student | Deferred Phase 3+ | Requires parent-visibility policy and audit trail design |
| Vector store integration | Deferred Phase 3+ | Qdrant for AI context retrieval |
| `formatLastActivity` i18n (teacher + parent portals) | Deferred Phase 3 | Hardcoded English strings in `StudentMonitorPage` and `ChildDetailPage` |
| Global `TenantId` query filter | Deferred Phase 3 | Requires `ICurrentUserService` in `OnModelCreating` |

---

## Section 9 Test Summary

```
Backend (dotnet test):
  LMS.Domain.Tests       — 70 tests  ✅
  LMS.Application.Tests  — 12 tests  ✅
  LMS.Api.Tests          — 57 tests  ✅
  Total                  — 139 tests ✅

Frontend (vitest run):
  41 tests               ✅
  tsc --noEmit           ✅ (0 errors)
```

No new tests were added in Section 9 (no new application-layer endpoints or domain behavior).
The `QuestionAnswerRecord` write path is covered by the existing `StudentService` completion
integration tests.

---

## Section 9 Closeout Validation

### Is the platform sufficiently prepared for Phase 3 AI tutor work?

**Yes**, with one documented caveat (ShortAnswer signal quality gap above).

The minimum required foundation is complete:
1. **Durable observation data** — `LessonProgress` and `QuestionAnswerRecord` capture what a student has done and how they performed, indexed for the queries Phase 3 will run
2. **Clean boundaries** — `StudentProfile`, `User`, and all existing services are free of adaptive fields; no boundary violations found in validation
3. **Reserved namespace** — `LMS.Domain/Personalization/` is reserved with accurate entity shapes that Phase 3 can implement directly
4. **Identity anchor documented** — `LearnerProfile` by `UserId`; traversal path to observation tables explained
5. **Privacy rules documented** — what to store, what to exclude, GDPR deletion order
6. **AI context signal map** — documented in `personalization-readiness.md`: which table answers each AI tutor question

Phase 3 can begin `LearnerProfile` implementation without any Phase 1 rework.
