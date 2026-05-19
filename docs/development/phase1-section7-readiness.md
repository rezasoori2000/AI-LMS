# Phase 1 · Section 7 Readiness Checklist

> Confirms that the student portal MVP is complete and stable before starting Section 8.

---

## Section 7 Completion Summary

Phase 1 Section 7 delivered the full student-facing portal:

- **Part 1** — Architecture spec, DTO contracts, `IStudentService`, `StudentAccessDeniedException`, TypeScript types, URL structure
- **Part 2** — `StudentService` EF Core implementation (all 7 methods, N+1-safe queries, ownership gates)
- **Part 3** — `student.service.ts` API client + `useStudent.ts` React Query hooks
- **Part 4** — Progress model review and `StudentController.cs` creation; `LessonAlreadyCompletedException` (409); `useStartStudentLesson` hook; `LessonPlayerPage` wired to start-on-mount
- **Part 5** — Validation, cleanup, documentation, Section 7 closeout (this part)

All locked Phase 1 decisions are implemented and documented:
- ✅ Free lesson access (no sequential gating)
- ✅ Partial answer submission allowed
- ✅ IsCorrect only — correct answers never revealed
- ✅ Plain text content rendering (no markdown)
- ✅ One attempt per lesson (terminal Completed state)

---

## Readiness Checklist

### Backend

- [x] `StudentService` — all 6 service methods implemented and registered in DI
- [x] `StudentController` — all 5 HTTP endpoints wired at `/api/student/*`
- [x] `[Authorize(Roles = "Student")]` on `StudentController` — role gate in place
- [x] `StudentAccessDeniedException` → 403 registered in `ExceptionHandlingMiddleware`
- [x] `LessonAlreadyCompletedException` → 409 registered in `ExceptionHandlingMiddleware`
- [x] `CorrectAnswer` never projected in `GetLessonAsync` — confirmed by query projection
- [x] Ownership enforced by `AssertEnrolledAsync` on all lesson-access methods
- [x] `ResolveStudentProfileIdAsync` returns null gracefully (no 500 on missing profile)
- [x] Unique index `ix_lesson_progress_student_lesson` on `(StudentId, LessonId)` — in migration
- [x] `LessonProgress.ScorePercent` stored on completion — parent portal can query it
- [x] `StartLessonAsync` idempotent — safe to call multiple times (page reload)
- [x] `CompleteLessonAsync` throws 409 on re-completion — not 500

### Frontend

- [x] `student.service.ts` — 5 API calls (summary, enrollments, chapters, lesson, start, complete)
- [x] `useStudent.ts` — 5 query hooks + 2 mutation hooks (`useCompleteStudentLesson`, `useStartStudentLesson`)
- [x] `StudentDashboardPage` — live stat row + continue-learning + subject preview
- [x] `SubjectsPage` — enrolled subjects with % progress and next-lesson shortcut
- [x] `SubjectDetailPage` — chapter-grouped lesson list with human-readable progress badges
- [x] `LessonPlayerPage` — plain text content + question answering + submit + score feedback
- [x] `QuestionAnswerForm` — handles MultipleChoice (radio), TrueFalse (radio), ShortAnswer (textarea)
- [x] Start-on-mount (`useEffect`) in `LessonPlayerPage` — fires `POST /start` when lesson opens
- [x] `ProtectedRoute` with `access: ['student']` on all student routes
- [x] `ProgressStatus` displayed as human-readable labels (not raw enum strings)
- [x] Dashboard stat labels use `t()` consistently (no hardcoded English in stat row)
- [x] `tsc --noEmit` clean — zero TypeScript errors

### Architecture

- [x] Module boundaries respected — student service does not depend on admin or parent modules
- [x] No correct-answer data in any student-facing DTO (DTO-level enforcement)
- [x] `ICurrentUserService` resolves ownership; controller has no data-access logic
- [x] EF Core queries are N+1-safe (batch GroupBy/dictionary patterns consistent with `ParentService`)
- [x] `ExceptionHandlingMiddleware` maps all student exceptions to correct HTTP status codes
- [x] TypeScript types in `student.ts` mirror backend DTOs exactly

---

## Validation Results

### Module boundaries — PASS
`LMS.Application.Student` is self-contained. No cross-dependencies on Parent or Admin modules.

### Privacy / ownership — PASS
Every `StudentService` method gates on `ResolveStudentProfileIdAsync` + `AssertEnrolledAsync`. A student cannot read or write progress for another student. A missing `StudentProfile` returns empty data (not a 500 or cross-user leak).

### Answer security — PASS
`CorrectAnswer` is excluded from `QuestionForStudentDto` at the projection level in EF Core. The column is never fetched for student-facing endpoints.

### Progress model stability — PASS
`LessonProgress` with `(StudentId, LessonId)` unique pair, `Status`, `StartedAt`, `CompletedAt`, `ScorePercent` is the right shape for Phase 1 and is directly extensible to a `LessonAttempt` child table in Phase 3 without any schema migration to existing data.

### DTO naming consistency — PASS (one minor note)
`CompleteResponse.GradableQuestions` deliberately counts only MC+TF, not total questions. This naming is intentional and documented. No fix needed.

### Error handling — PASS
- 403 for unenrolled access (prevents resource enumeration)
- 409 for re-completion (not 500)
- Empty-state returns (not 404 or 500) for missing `StudentProfile`

---

## What Must Be Fixed Before Section 8

None. Section 7 is complete.

---

## Important Risks / Tradeoffs

| Risk | Severity | Status |
|---|---|---|
| `StudentProfile` not auto-created on registration — student sees empty dashboard | Low | Documented gap; admin must provision profile |
| Race condition on concurrent `CompleteLessonAsync` (duplicate DB insert attempt) | Low | Unique index catches it at DB level; acceptable for Phase 1 solo-user flows |
| No per-question answer persistence — cannot support review mode later without additional table | Medium | Accepted Phase 1 trade-off; `LessonAttempt` + `StudentAnswer` tables are Phase 3 work |
| `ShortAnswer` always `isCorrect: false` | Low | Documented in DTOs and student portal docs; student sees feedback but not why |
| Comprehensive i18n for student pages is incomplete | Low | Stat row and progress badges fixed; remaining strings are acceptable English hardcode for Phase 1 |

---

## Intentionally Deferred Items

The following are **not bugs** — they are explicit Phase 1 deferral decisions:

- Review mode (see answers after completion)
- Correct-answer reveal post-completion
- Re-attempt / quiz retry
- ShortAnswer AI evaluation
- Sequential lesson gating / prerequisites
- Student self-enrollment
- Progress page at `/student/progress`
- Gamification (streaks, points, badges)
- AI tutoring integration
- Advanced analytics

---

## Section 8 Entry Criteria

Section 8 may begin when all items in the Readiness Checklist above are checked.  
All items are now checked. ✅

> **Recommended Section 8 focus**: Teacher portal, or enrollment management for admin/student flows,
> or authentication hardening (StudentProfile auto-creation on registration).
> Confirm with product roadmap before starting.
