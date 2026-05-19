# Phase 1 Section 8 Part 1 — Teacher Portal MVP Architecture

**Status:** Architecture defined — implementation begins Part 2  
**Date:** 2026-05-19  
**Scope:** Module boundaries, access model, DTOs, implementation sequence

---

## 1. Core Constraint: No TeacherProfile in Phase 1

The domain model notes explicitly defer `TeacherProfile` and teacher-to-student assignment to Phase 3:

- `StudentProfile.cs` → `"Phase 3: add class/room assignment, TeacherId, learning-profile settings."`
- `Subject.cs` → `"Phase 3: add teacher assignment."`

**Consequence:** There is no teacher-to-student or teacher-to-subject relationship table in Phase 1.

**Phase 1 ownership gate for teacher access:**  
A teacher is scoped to their `TenantId` (from the JWT `tid` claim). A teacher sees **all students and their progress within the same tenant**. No per-teacher filtering is possible until Phase 3 adds a `TeacherProfile` and assignment relationship.

This is the simplest MVP-safe interpretation given the existing domain. It is also realistic for small-school single-tenant deployments where a teacher is responsible for the whole school.

---

## 2. MVP Feature Scope

### Included in Phase 1 (Parts 2–5)

| Feature | Description |
|---|---|
| Teacher dashboard | Stat summary: total students, active enrollments, completions this week |
| Students list | All `StudentProfile` records for the teacher's tenant, with grade name and enrollment count |
| Student enrollment detail | Per-student enrollment list with per-subject lesson completion totals |
| Student lesson progress | Per-student, per-subject lesson-level progress breakdown (status, score, timestamps) |

### Not included in Phase 1

| Feature | Reason |
|---|---|
| Assigned students only view | No `TeacherProfile` or assignment table exists — Phase 3 |
| Class/group management | No class entity — Phase 3 |
| Attendance | Not in scope for Section 8 |
| Assignment creation | Not in scope |
| Grading center | Not in scope |
| Parent-teacher messaging | Not in scope |
| Real-time dashboards / analytics | Not in scope |
| Content authoring | Admin-only in Phase 1 |
| Intervention flags | Phase 3+ |
| Answer reveal in progress view | Correct answers never shown to teachers in Phase 1 (same policy as students). Phase 3 adds grading review. |

---

## 3. Backend Module Structure

### New module: `LMS.Application/Teacher/`

```
LMS.Application/Teacher/
  ITeacherService.cs
  TeacherService.cs
  TeacherAccessDeniedException.cs     ← tenant-scope violation → HTTP 403
  Dtos/
    TeacherDtos.cs
```

**Module responsibilities:**
- All teacher-facing read queries live here
- No write operations in Phase 1
- All queries are tenant-gated via `ICurrentUserService.TenantId`
- Access denied if teacher's `TenantId` is null (should not occur in production — guard defensively)

### New controller: `LMS.Api/Controllers/Teacher/`

```
LMS.Api/Controllers/Teacher/
  TeacherController.cs
```

---

## 4. Planned API Endpoints

All endpoints: `[Authorize(Roles = "Teacher")]`, route prefix `/api/teacher`.

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/teacher/summary` | Dashboard stat row |
| `GET` | `/api/teacher/students` | All students in tenant (paginated in Phase 3; list in Phase 1) |
| `GET` | `/api/teacher/students/{studentId}/enrollments` | Student's active enrollment list with per-subject totals |
| `GET` | `/api/teacher/students/{studentId}/progress` | Student's lesson-level progress across all enrollments |

**Access gate:** Every endpoint verifies the target `studentId`'s `TenantId` matches the calling teacher's `TenantId`. Mismatch → `TeacherAccessDeniedException` → 403.

---

## 5. DTOs

All records defined in `TeacherDtos.cs`:

```csharp
// Dashboard
record TeacherSummaryDto(
    int TotalStudents,
    int ActiveEnrollments,
    int LessonsCompletedThisWeek);

// Students list
record StudentSummaryForTeacherDto(
    Guid   StudentId,
    string FullName,
    string Email,
    string? GradeName,
    int    ActiveEnrollmentCount,
    int    TotalLessonsCompleted);

// Per-student enrollments
record TeacherStudentEnrollmentsDto(
    Guid   StudentId,
    string FullName,
    string? GradeName,
    List<TeacherEnrollmentItemDto> Enrollments);

record TeacherEnrollmentItemDto(
    Guid             EnrollmentId,
    Guid             SubjectId,
    string           SubjectName,
    EnrollmentStatus Status,
    DateTime         EnrolledAt,
    int              LessonsTotal,
    int              LessonsCompleted,
    int              LessonsInProgress,
    DateTime?        LastActivityAt);

// Per-student lesson progress
record TeacherStudentProgressDto(
    Guid   StudentId,
    string FullName,
    List<TeacherLessonProgressItemDto> LessonProgress);

record TeacherLessonProgressItemDto(
    Guid           LessonId,
    string         LessonTitle,
    Guid           SubjectId,
    string         SubjectName,
    string         ChapterTitle,
    ProgressStatus Status,
    decimal?       ScorePercent,
    DateTime?      StartedAt,
    DateTime?      CompletedAt);
```

**Security note:** `CorrectAnswer` is never included in any teacher-facing DTO. Phase 3 may add a grading-review flow.

---

## 6. Frontend Module Structure

### New files

```
frontend/src/types/teacher.ts               ← TypeScript interfaces mirroring backend DTOs
frontend/src/services/teacher.service.ts    ← API client calls via apiClient
frontend/src/features/teacher/
  hooks/
    useTeacher.ts                           ← React Query hooks
frontend/src/pages/teacher/
  StudentsPage.tsx                          ← Student list with grade + enrollment count
  StudentProgressPage.tsx                  ← Per-student detail (enrollments + lessons)
frontend/src/pages/dashboards/
  TeacherDashboardPage.tsx                 ← Already exists as placeholder — wire to real data in Part 5
```

### Route updates (Part 3)

Existing stubs in `teacher.routes.tsx`:
- `/teacher` → `TeacherDashboardPage` (already wired, placeholder)
- `/teacher/students` → wire to `StudentsPage`
- `/teacher/students/:studentId` → wire to `StudentProgressPage`

The existing `/teacher/lessons`, `/teacher/courses`, `/teacher/progress` stubs should remain as `ComingSoonPage` for Phase 1.

### i18n

New keys to add under `dashboards.teacher.*`:
- `stats.totalStudents`, `stats.activeEnrollments`, `stats.completionsThisWeek`
- `sections.students`
- `studentProgress.*` (status, score labels)
- `common.*` keys already exist (shared with student portal)

---

## 7. Ownership and Access Rules

| Question | Phase 1 Answer |
|---|---|
| What can a teacher read? | All `StudentProfile` + `Enrollment` + `LessonProgress` rows where `TenantId` matches teacher's JWT `TenantId` |
| What can a teacher write? | Nothing in Phase 1 — read-only portal |
| Can a teacher see another tenant's students? | No — `TeacherAccessDeniedException` → 403 |
| Can a teacher see question correct answers? | No — excluded at projection level, same as student portal |
| Can a teacher access admin content APIs? | No — admin routes require `SuperAdmin`, `TenantAdmin`, or `ContentEditor` roles |
| Can a teacher access parent routes? | No — parent routes require `Parent` role |
| What if a teacher's JWT has no `TenantId`? | `TeacherAccessDeniedException` immediately — defensive guard, no data returned |

**Comparison with parent portal ownership model:**

| | Parent | Teacher |
|---|---|---|
| Scope | Own linked children only (`ParentId` FK) | All students in tenant (`TenantId` match) |
| Gate mechanism | `AssertLinkedChildAsync` helper | `AssertSameTenantAsync` helper |
| Phase 3 refinement | M:M ParentStudentLink | TeacherProfile + assignment table |

---

## 8. Implementation Sequence

| Part | Work | New Backend Files | New Frontend Files |
|---|---|---|---|
| **Part 2** | Backend service + controller | `ITeacherService.cs`, `TeacherDtos.cs`, `TeacherAccessDeniedException.cs`, `TeacherService.cs`, `TeacherController.cs`, middleware entry | — |
| **Part 3** | Frontend service + hooks + StudentsPage | — | `teacher.ts` types, `teacher.service.ts`, `useTeacher.ts`, `StudentsPage.tsx`, route wiring |
| **Part 4** | StudentProgressPage (per-student detail) | — | `StudentProgressPage.tsx`, route wiring, i18n keys |
| **Part 5** | Wire TeacherDashboard + validate + Section 8 closeout | — | `TeacherDashboardPage.tsx` wired, readiness doc, README update |

This mirrors the Section 7 student portal pattern exactly.

---

## 9. Maintainability Review

**Strengths:**
- Follows the same module pattern as `LMS.Application/Parent/` and `LMS.Application/Student/` — zero new conventions to learn
- Read-only Phase 1 service eliminates mutation complexity
- Tenant-scoped queries are simple EF Core `Where(x => x.TenantId == teacherTenantId)` filters — no new abstractions needed
- Frontend hooks follow the identical `useStudent.ts` / `useParent.ts` pattern

**Weaknesses / future friction:**
- "All students in tenant" is a coarse access model — a teacher at a large school seeing 2,000 students is impractical. Acceptable for Phase 1 (small schools), must be refined in Phase 3.
- `TeacherDashboardPage` currently shows hardcoded placeholder content — wiring in Part 5 means the component will be refactored rather than replaced, which is fine but worth tracking.

---

## 10. Data Access / Privacy Review

- Teacher reads `StudentProfile.UserId` → `User.Email` and `User.FirstName + LastName`. This is standard teacher access to student identity and is appropriate.
- Teacher reads `LessonProgress.ScorePercent` — a learner outcome. Appropriate for a teacher.
- Teacher does NOT read `User.PasswordHash` — the projection never touches it.
- Teacher does NOT read `Question.CorrectAnswer` — excluded at the projection level.
- No cross-tenant data is accessible given the `TenantId` gate on every query.
- Parent's linked-child relationship is not exposed to teachers (ParentId is not included in any teacher DTO).

**Single concern:** Email addresses are visible to teachers. This is intentional and expected. No action needed.

---

## 11. Phase 1 Scope Appropriateness

The scope is appropriate for Phase 1:

- It mirrors what a form teacher at a small school actually needs: see my students, check who's making progress, spot who hasn't started
- It reuses all existing domain entities and EF Core queries — no new migrations needed
- It does not add any new domain complexity
- The deferred Phase 3 items (TeacherProfile, assignment relationships, class management) are clearly separated and will not require breaking changes to Phase 1 data

---

## 12. Risks and Tradeoffs

| Risk | Severity | Mitigation |
|---|---|---|
| "All students in tenant" is too broad for large schools | Medium | Acceptable for Phase 1 (small school assumption); Phase 3 adds TeacherProfile + assignment table. Document explicitly. |
| Teacher with `TenantId = null` (like SuperAdmin) has no valid scope | Low | `TeacherAccessDeniedException` guard in service — no data returned. SuperAdmin uses admin routes. |
| Teacher portal is read-only — no write path | Intentional | Stated Phase 1 constraint. Phase 3 adds feedback/grading writes. |
| No `TeacherProfile` means no teacher-specific settings, bio, subject assignments | Intentional | Phase 3 deferred. The `UserRole.Teacher` JWT claim is sufficient for Phase 1 access control. |
| Correct answers not shown — teacher cannot review submitted answers in Phase 1 | Intentional | Phase 3 grading review will add this. The same domain data (`LessonProgress.ScorePercent`) is already stored. |

---

## 13. Section 8 Entry Criteria (Confirmed Met)

- [x] `UserRole.Teacher` defined in domain
- [x] Frontend `teacher.routes.tsx` stub routes exist
- [x] Teacher nav configured in `nav.ts`
- [x] `TeacherDashboardPage` placeholder exists
- [x] `LessonProgress` entity stores `ScorePercent`, `Status`, `StartedAt`, `CompletedAt` — sufficient for teacher progress views
- [x] `Enrollment` entity stores `Status`, `EnrolledAt`, `CompletedAt` — sufficient for enrollment summaries
- [x] `ICurrentUserService` provides `UserId` and `TenantId` — ownership gate is implementable
- [x] `ExceptionHandlingMiddleware` is extensible — `TeacherAccessDeniedException` can be added in Part 2
