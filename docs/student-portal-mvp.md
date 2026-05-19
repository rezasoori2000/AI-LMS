# Student Portal — Phase 1 MVP

> Phase 1 Section 7 deliverable. This document covers the scope, API contract, ownership model, scoring rules, and known gaps for the student-facing portal.

---

## What is included in Phase 1

| Feature | Status |
|---|---|
| View enrolled subjects with completion progress | ✅ Done |
| View subject chapter + lesson tree | ✅ Done |
| View lesson content (plain text) | ✅ Done |
| Start lesson (marks InProgress, idempotent) | ✅ Done |
| Submit lesson completion + MC/TF auto-scoring | ✅ Done |
| Per-question correctness feedback (`isCorrect` only) | ✅ Done |
| Student dashboard with live summary stats | ✅ Done |
| Free lesson access — no sequential gating | ✅ Done |

---

## API Endpoints

All endpoints require a valid JWT with role `Student`.  
Ownership is enforced inside `StudentService` — the controller does no data-access logic.

| Method | Path | Description |
|---|---|---|
| GET | `/api/student/summary` | Dashboard aggregate stats |
| GET | `/api/student/enrollments` | Enrolled subjects with per-subject progress |
| GET | `/api/student/subjects/{subjectId}/chapters` | Chapter + lesson tree with progress status |
| GET | `/api/student/lessons/{lessonId}` | Lesson content + questions (no correct answers) |
| POST | `/api/student/lessons/{lessonId}/start` | Mark lesson InProgress (idempotent) |
| POST | `/api/student/lessons/{lessonId}/complete` | Submit answers, score, mark Completed |

### Error responses

| Condition | HTTP Status |
|---|---|
| Not enrolled in subject / lesson | 403 Forbidden (`StudentAccessDeniedException`) |
| Lesson already completed | 409 Conflict (`LessonAlreadyCompletedException`) |

---

## Ownership Model

```
JWT sub claim (UserId)
  └─ StudentProfile.UserId → StudentProfile.Id
       └─ Enrollment.StudentId == StudentProfile.Id  (must be Active)
            └─ Subject → Chapter → Lesson
```

A student can only read/write progress for lessons within subjects they are **actively enrolled in**.  
If a student has no `StudentProfile` (admin-managed in Phase 1), all requests return empty data or 403 — never a 500.

---

## Progress State Machine

```
NotStarted  ──[open lesson]──►  InProgress  ──[submit completion]──►  Completed (terminal)
```

- `LessonProgress` is a single record per `(StudentId, LessonId)` — unique index enforced in DB.
- `StartLessonAsync` is idempotent — safe to call multiple times (e.g. on page reload).
- `CompleteLessonAsync` is **not idempotent** — throws 409 if called again after Completed.
- `StartedAt` and `CompletedAt` are stored for parent portal consumption.
- Phase 3 will introduce a `LessonAttempt` child table to support multiple attempts.

---

## Scoring Rules

| Question type | Scored | Encoding |
|---|---|---|
| MultipleChoice | ✅ | Zero-based option index string (`"0"`, `"1"`, `"2"`, `"3"`) |
| TrueFalse | ✅ | `"true"` or `"false"` (lowercase) |
| ShortAnswer | ❌ | Free text accepted; always `isCorrect: false` in Phase 1 |

- `scorePercent` is `null` when the lesson has no gradable questions (content-only).
- The `gradableQuestions` field in `CompleteResponse` counts MC + TF only.
- Partial answers are allowed — unanswered questions do not count against the score.
- The correct answer is **never returned** in any student-facing API response.

---

## Answer Security

`CorrectAnswer` is excluded at the DTO level:
- `QuestionForStudentDto` intentionally omits the `CorrectAnswer` field.
- `AnswerResultDto` returns only `isCorrect: bool`, never the correct string.
- This is enforced by projection in `StudentService.GetLessonAsync` — the field is never fetched.

Phase 3 decision: whether to reveal correct answers post-completion is explicitly deferred (see decisions doc).

---

## Frontend Pages

| Route | Component | Notes |
|---|---|---|
| `/student` | `StudentDashboardPage` | Live stat row + continue-learning + subject preview |
| `/student/subjects` | `SubjectsPage` | Enrolled subjects as cards with % and next-lesson link |
| `/student/subjects/:subjectId` | `SubjectDetailPage` | Chapter-grouped lesson list with progress badges |
| `/student/subjects/:subjectId/lessons/:lessonId` | `LessonPlayerPage` | Lesson content + question form + submit |
| `/student/progress` | `ComingSoonPage` | Deferred to Phase 2 |

All routes carry `handle: { access: ['student'] }` for `ProtectedRoute` role gating.

---

## Known Gaps and Deferred Items

| Gap | Deferred to |
|---|---|
| `StudentProfile` not auto-created on registration | Phase 2 (admin-managed in Phase 1) |
| ShortAnswer AI evaluation | Phase 3 (requires ai-service integration) |
| Correct-answer reveal after completion | Phase 3 (decision locked: Option A for Phase 1) |
| Re-attempt / quiz retry | Phase 3 (`LessonAttempt` child table) |
| Sequential lesson gating / prerequisites | Phase 2+ (decision locked: free access in Phase 1) |
| Student self-enrollment | Phase 2 (admin-managed in Phase 1) |
| Progress page (`/student/progress`) | Phase 2 |
| Per-question answer persistence (review mode) | Phase 3 |
| `formatLastActivity` i18n (parent portal) | Phase 2 |
| Comprehensive i18n for all student page strings | Phase 2 |
| Tenant scoping on student queries | Phase 3 (safe in Phase 1 — ownership via StudentProfile FK) |
| Race condition on concurrent completion (two identical simultaneous POSTs) | Phase 3 |
| Gamification (streaks, points, badges) | Phase 3+ |
| AI tutoring integration | Phase 3+ (LLM provider deferred) |
| Advanced analytics and progress reports | Phase 3+ |
