# Teacher Portal — Phase 1 MVP

> Phase 1, Section 8. Last updated: 2026-05-19.

---

## What This Document Covers

Concise reference for the Phase 1 teacher portal: what is built, how ownership is enforced,
the known architectural gaps, and what is explicitly deferred to later phases.

---

## Scope (Phase 1)

The Phase 1 teacher portal is read-only from the teacher's perspective. A teacher can:

- View a dashboard summary (assigned student count, active enrollment count, completions in the past 7 days)
- Browse all students assigned to their account with per-student enrollment and completion counts
- Navigate to a student's detail view showing per-subject enrollment summaries
- View lesson-level progress for one assigned student across all active enrollments

An **admin** manages teacher→student assignment via the admin Student Linkage page. No write operations are exposed to teachers in Phase 1.

---

## Assignment Model

Teacher→student assignments are stored in the `teacher_student_assignments` join table
(`TeacherStudentAssignment` entity). See [teacher-student-assignment-notes.md](./teacher-student-assignment-notes.md) for the full
architectural rationale.

**Phase 1 convention**: each student has at most one assigned teacher. The admin UI enforces
this by replacing any existing assignment when a new one is saved (delete-then-insert).

---

## Module Structure

```
backend/
  LMS.Application/Teacher/
    ITeacherService.cs                    — read-only service interface; ownership-enforced
    TeacherService.cs                     — EF Core implementation
    TeacherAccessDeniedException.cs       — maps to HTTP 403
    Dtos/
      TeacherDtos.cs                      — all teacher-facing DTO records

  LMS.Api/Controllers/Teacher/
    TeacherController.cs                  — GET /api/teacher/* endpoints

  LMS.Application/Admin/Students/
    IStudentAdminService.cs               — includes GetTeacherOptionsAsync + AssignTeacherAsync
    StudentAdminService.cs                — M:M assignment logic
    StudentAdminDtos.cs                   — TeacherOptionDto, AssignTeacherRequest

  LMS.Api/Controllers/Admin/
    StudentsController.cs                 — GET + PATCH teacher assignment endpoints

frontend/
  src/types/teacher.ts                    — TS types matching teacher portal DTOs
  src/types/student-admin.ts              — includes TeacherOptionDto, AssignTeacherPayload
  src/services/teacher.service.ts         — teacher portal API calls
  src/services/student-admin.service.ts   — includes getTeacherOptions + assignTeacher
  src/features/admin/students/hooks/
    useStudentAdmin.ts                    — includes useTeacherOptions + useAssignTeacher
  src/pages/teacher/
    TeacherDashboardPage.tsx
    StudentsPage.tsx
    StudentMonitorPage.tsx
  src/pages/admin/students/
    StudentsLinkPage.tsx                  — parent + teacher assignment inline edit
  src/routes/modules/teacher.routes.tsx
```

---

## API Endpoints

### Teacher portal (read-only, Teacher role)

| Method | Path                                          | Description                                          |
|--------|-----------------------------------------------|------------------------------------------------------|
| GET    | `/api/teacher/summary`                        | Dashboard stat counts for the calling teacher.       |
| GET    | `/api/teacher/students`                       | All students assigned to the calling teacher.        |
| GET    | `/api/teacher/students/{studentId}`           | Enrollment detail for one assigned student.          |
| GET    | `/api/teacher/students/{studentId}/progress`  | Lesson-level progress for one assigned student.      |

All endpoints return 403 when the student is not assigned to the calling teacher.

### Admin teacher assignment (Admin role)

| Method | Path                                          | Description                                          |
|--------|-----------------------------------------------|------------------------------------------------------|
| GET    | `/api/admin/students/teacher-options`         | List of teachers for the assignment dropdown.        |
| PATCH  | `/api/admin/students/{studentId}/teacher`     | Assign or unassign a teacher for a student.          |

---

## Ownership / Security Properties

- `TeacherController` carries `[Authorize(Roles = "Teacher")]` — role enforcement at the framework layer.
- Ownership is enforced at the **service** layer in `TeacherService` via `AssertAssignedStudentAsync`. The controller never bypasses the service.
- `AssertAssignedStudentAsync` queries `TeacherStudentAssignment` using the calling teacher's `UserId` from JWT claims — callers cannot spoof identity.
- `TeacherAccessDeniedException` returns **403**, not 404 — consistent with parent portal privacy model (student existence is not revealed to unassigned teachers).
- `Question.CorrectAnswer` is **never** included in any teacher-facing DTO.
- Only **active** enrollments are shown in the progress detail view.

---

## Frontend Pages

| Route                                     | Component               | Description                                      |
|-------------------------------------------|-------------------------|--------------------------------------------------|
| `/teacher`                                | `TeacherDashboardPage`  | Stat cards + recent students.                    |
| `/teacher/students`                       | `StudentsPage`          | Pageable student list with enrollment counts.    |
| `/teacher/students/:studentId`            | `StudentMonitorPage`    | Per-student tab view: enrollments + progress.    |

All teacher routes carry `access: ['teacher']` on the `RouteHandle`, enforced by `ProtectedRoute`.

---

## Known Gaps and Deferred Items

| Gap                                         | Status    | Notes                                                  |
|---------------------------------------------|-----------|--------------------------------------------------------|
| `formatLastActivity` not i18n'd             | Deferred  | Hardcoded English strings in `StudentMonitorPage`. Deferred to Phase 3. |
| No `TeacherProfile` entity                  | Deferred  | Phase 1 uses `User.Id` as teacher identity. Phase 3 adds `TeacherProfile`. |
| Subject-scoped co-teaching                  | Deferred  | Phase 1: one teacher per student. Phase 3: M:M with subjects. |
| Teacher self-assignment                     | Out of scope | Teachers cannot assign themselves students in Phase 1. |
| Notification on new assignment              | Deferred  | Phase 3 messaging layer.                               |
