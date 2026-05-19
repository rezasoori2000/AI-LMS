# Phase 1 · Section 7 Part 1 — Student Portal Architecture

> **Status**: Scaffolding complete. Implementation begins in Part 2.  
> **Goal**: Define the MVP architecture and boundaries for the student portal so later parts can implement a practical student-facing experience for lesson access and basic progress tracking.

---

## MVP Feature Scope

| Feature | Phase 1 MVP | Deferred |
|---|---|---|
| View enrolled subjects | ✅ | — |
| View subject chapter/lesson tree | ✅ | — |
| View lesson content | ✅ | — |
| Start lesson (tracks progress) | ✅ | — |
| Complete lesson + multiple-choice / true-false scoring | ✅ | — |
| Short-answer evaluation | ❌ | Phase 3 (AI evaluation) |
| Post-completion correct-answer reveal | ❌ | Phase 3 (by design, see open questions) |
| Sequential lesson gating | ❌ | Phase 2+ (by design, see open questions) |
| Student self-enrollment | ❌ | Phase 2 (admin-managed in Phase 1) |
| Bookmarks / notes | ❌ | Phase 3 |
| Certificate / completion badge | ❌ | Phase 3 |
| Real-time progress WebSocket updates | ❌ | Phase 4 |

---

## Backend Module Layout

```
backend/src/LMS.Application/Student/
├── IStudentService.cs                # Service contract (7 methods)
├── StudentAccessDeniedException.cs  # 403 — not enrolled in subject/lesson
├── Dtos/
│   └── StudentDtos.cs               # All response + request records
└── StudentService.cs                # EF Core implementation (Part 2)

backend/src/LMS.Api/Controllers/
└── StudentController.cs             # HTTP endpoints (Part 3)
```

`StudentService.cs` and `StudentController.cs` are **not yet created** — they are the deliverables for Parts 2 and 3 respectively.

### Exception mapping

`StudentAccessDeniedException` is already registered in `ExceptionHandlingMiddleware.cs` → HTTP 403 Forbidden.

---

## Planned API Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/student/summary` | `student` | Dashboard aggregate stats |
| GET | `/api/student/enrollments` | `student` | List of enrolled subjects with progress |
| GET | `/api/student/subjects/{subjectId}/chapters` | `student` | Subject content tree (chapters + lessons) |
| GET | `/api/student/lessons/{lessonId}` | `student` | Lesson detail + questions (no correct answers) |
| POST | `/api/student/lessons/{lessonId}/start` | `student` | Mark lesson as InProgress (idempotent) |
| POST | `/api/student/lessons/{lessonId}/complete` | `student` | Submit answers, score, mark Completed |

All endpoints require `[Authorize(Roles = "student")]`.  
Ownership is enforced in `StudentService` via `ICurrentUserService` — the controller must not perform its own ownership checks.

---

## Frontend Page Structure

```
frontend/src/
├── pages/
│   └── student/
│       ├── SubjectsPage.tsx                  # Part 4
│       ├── SubjectDetailPage.tsx             # Part 4
│       └── LessonViewPage.tsx                # Part 5
├── features/
│   └── student/
│       ├── components/
│       │   ├── EnrolledSubjectCard.tsx       # Part 4
│       │   ├── LessonOutline.tsx             # Part 4
│       │   ├── QuestionCard.tsx              # Part 5
│       │   └── LessonCompletionSummary.tsx   # Part 5
│       ├── hooks/
│       │   ├── useStudentSummary.ts          # Part 6
│       │   ├── useEnrollments.ts             # Part 4
│       │   ├── useSubjectChapters.ts         # Part 4
│       │   └── useLesson.ts                  # Part 5
│       └── student.service.ts               # API client (Part 3)
├── types/
│   └── student.ts                           # ✅ Complete (Part 1)
└── routes/modules/
    └── student.routes.tsx                   # ✅ URL stubs complete (Part 1)
```

---

## Route Structure

```
/student                                          → StudentDashboardPage
/student/subjects                                 → SubjectsPage (enrollments list)
/student/subjects/:subjectId                      → SubjectDetailPage (chapter/lesson tree)
/student/subjects/:subjectId/lessons/:lessonId    → LessonViewPage
/student/progress                                 → ProgressPage (Part 6)
```

All routes carry `handle: { access: ['student'] }` for `ProtectedRoute` role gating.

---

## Ownership Model

```
ClaimsPrincipal
  └─ UserId (sub claim)
       └─ StudentProfile.UserId → StudentProfile.Id
            └─ Enrollment.StudentId == StudentProfile.Id
                 └─ Subject → Chapter → Lesson
```

The service layer resolves the `StudentProfile` from the calling user's `UserId` on every request (same pattern as `ParentService.ResolveParentProfileIdAsync`).

**Access rule**: A student may only call `GetSubjectChaptersAsync`, `GetLessonAsync`, `StartLessonAsync`, and `CompleteLessonAsync` for lessons within subjects they are actively enrolled in. Any other access throws `StudentAccessDeniedException` → 403.

**Important**: `StudentProfile` is not auto-created on user registration in Phase 1. It is admin-managed. A student with no profile will receive an empty dashboard (same behaviour as the parent portal).

---

## Answer Security

`CorrectAnswer` must **never** appear in any student-facing API response. This is enforced by:
- `QuestionForStudentDto` intentionally omits the field.
- `AnswerResultDto` returns only `IsCorrect` (bool), not the correct answer.

Phase 3 will revisit whether to reveal correct answers after completion (see open questions below).

---

## Scoring Rules (Phase 1)

| Question type | Scored? | Encoding |
|---|---|---|
| MultipleChoice | ✅ | Zero-based option index string (`"0"`, `"1"`, `"2"`, `"3"`) |
| TrueFalse | ✅ | `"true"` or `"false"` (lowercase) |
| ShortAnswer | ❌ | Free text accepted, stored, always `IsCorrect: false` |

`CompleteResponse.ScorePercent` is `null` when the lesson has no gradable questions.

---

## Progress State Machine

```
           StartLessonAsync
NotStarted ──────────────► InProgress
                                │
                    CompleteLessonAsync
                                ▼
                          Completed (terminal — one attempt in Phase 1)
```

- `StartLessonAsync` is idempotent — no-op if already `InProgress` or `Completed`.
- `CompleteLessonAsync` throws if already `Completed` (re-attempt not supported in Phase 1).

---

## Implementation Order (Parts 2–6)

| Part | Deliverable | Depends on |
|---|---|---|
| **Part 2** | `StudentService.cs` — EF Core implementation of all 7 service methods | Part 1 contracts |
| **Part 3** | `StudentController.cs` + `student.service.ts` API client | Part 2 |
| **Part 4** | `SubjectsPage`, `SubjectDetailPage`, components + hooks | Part 3 |
| **Part 5** | `LessonViewPage`, `QuestionCard`, `LessonCompletionSummary` | Part 4 |
| **Part 6** | `StudentDashboardPage` wired to real data, `ProgressPage` | Parts 4–5 |

---

## Deferred Items

| Item | Reason | Target |
|---|---|---|
| Short-answer AI evaluation | Requires ai-service integration | Phase 3 |
| Correct-answer reveal post-completion | User decision pending (see open questions) | Phase 3+ |
| Sequential lesson gating | User decision pending (see open questions) | Phase 2+ |
| Student self-enrollment | Out of Phase 1 scope | Phase 2 |
| Re-attempt / quiz retry | One attempt per lesson in Phase 1 | Phase 3 |
| `StudentProfile` auto-creation on registration | Requires registration flow change | Phase 2 |
| Progress export / report card | No requirement yet | Phase 3 |

---

## Confirmed Decisions (Phase 1 Section 7)

> Resolved during Part 3 implementation. These decisions are locked for Phase 1.

### Decision 1 — Lesson completion: partial answers allowed

Students may submit lesson completion with some questions unanswered.

- There is **no client-side or server-side enforcement** requiring all gradable questions to be answered before submit.
- Unanswered questions are simply absent from the `answers` array in `CompleteRequest`.
- The backend scores only the answers provided; missing questions do not count against the score.
- Rationale: Phase 1 focuses on simple completion tracking, not strict assessment enforcement. Strict answer-gating is deferred to a future phase.

### Decision 2 — Lesson content rendering: plain text for Phase 1

Lesson content is displayed as plain-text (whitespace-preserved) in the lesson player.

- **No markdown rendering** is added in Phase 1.
- **No rich content runtime** (editors, renderers) is introduced at this stage.
- Rationale: minimises UI complexity, avoids locking content-authoring decisions early, and preserves flexibility for a richer lesson runtime later.

---

## Open Questions for User — Confirm Before Part 2

> These design decisions affect `StudentService.cs` and the lesson completion flow. Answer before Part 2 implementation begins.

### Q1 — Post-completion answer reveal

After a student completes a lesson, should they see the correct answers?

**Option A (current design)**: Show only `IsCorrect` per question — no correct answer revealed. Simple, no cheating risk. Student knows what they got wrong but not what the right answer was.

**Option B**: After completion, a second GET endpoint returns the full results including correct answers. Requires a new DTO and endpoint. Correct answers are never returned during or before completion.

**Option C**: The `CompleteResponse` itself includes correct answers. Simpler but increases exposure.

*Current default is Option A. If you want B or C, confirm before Part 2.*

---

### Q2 — Sequential lesson gating

Should students be required to complete lessons in order (lesson 1 before lesson 2), or may they access any lesson freely within enrolled subjects?

**Option A (current design)**: Free access — any lesson in any enrolled subject, any order. Simpler implementation; better for self-paced learners.

**Option B**: Sequential gating per chapter — must complete previous lesson to unlock the next. `LessonSummaryDto` would need a `IsLocked` field; `GetLessonAsync` would throw `StudentAccessDeniedException` for locked lessons.

*Current default is Option A. If you want B, confirm before Part 2 so `IStudentService` can be updated.*

---

## Risks and Tradeoffs

| Risk | Severity | Mitigation |
|---|---|---|
| `StudentProfile` not auto-created → empty dashboard | Low | Document clearly; admin must provision profile before student can use portal |
| `TotalLessons` counts all lessons regardless of enrollment date | Low | Accept in Phase 1; document as known gap |
| One-attempt completion — students cannot retry | Medium | Acceptable for Phase 1; re-attempt feature scoped to Phase 3 |
| Short-answer always `IsCorrect: false` | Low | Clearly documented in DTO comments; student sees blank/incorrect but that's Phase 1 |
| No TenantId filter on student queries | Low | Safe in Phase 1: `StudentProfile.UserId` FK is the ownership gate; multi-tenant isolation is Phase 2 hardening |
