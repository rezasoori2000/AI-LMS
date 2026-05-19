# Phase 1 — Section 8 Readiness Checklist

> Teacher portal, M:M assignment model, and Section 8 closeout.  
> Date: 2026-05-19

---

## Section 8 Completion Summary

Phase 1 Section 8 delivers a fully functional teacher portal and the M:M teacher–student
assignment model. Teachers can log in and view their assigned students, per-student enrollment
summaries, and lesson-level progress. Admin users can assign and reassign teachers to students
via the admin Student Linkage page.

### What Was Built

| Area                                                                          | Status |
|-------------------------------------------------------------------------------|--------|
| **Domain**: `TeacherStudentAssignment` M:M join entity                        | ✅ |
| **Domain**: `StudentProfile.TeacherAssignments` navigation property            | ✅ |
| **Domain**: `StudentProfile.TeacherId` and 1:1 model removed                 | ✅ |
| **Infrastructure**: `TeacherStudentAssignmentConfiguration` (EF Core)         | ✅ |
| **Infrastructure**: `ILmsDbContext.TeacherStudentAssignments` DbSet            | ✅ |
| **Infrastructure**: Migration `AddTeacherStudentAssignmentTable` generated    | ✅ |
| **Application**: `ITeacherService` + `TeacherService` (4 read methods)        | ✅ |
| **Application**: `TeacherAccessDeniedException` → 403                        | ✅ |
| **Application**: `ICurrentUserService` JWT identity in `TeacherService`       | ✅ |
| **API**: `TeacherController` — 4 endpoints, Teacher role guard                | ✅ |
| **Admin**: `IStudentAdminService.GetTeacherOptionsAsync` + `AssignTeacherAsync` | ✅ |
| **Admin**: `StudentAdminService` M:M assignment (delete-then-insert)          | ✅ |
| **Admin**: `StudentsController` teacher option + assign endpoints             | ✅ |
| **Frontend**: `types/student-admin.ts` — teacher fields + `TeacherOptionDto` | ✅ |
| **Frontend**: `student-admin.service.ts` — `getTeacherOptions` + `assignTeacher` | ✅ |
| **Frontend**: `useStudentAdmin.ts` — `useTeacherOptions` + `useAssignTeacher` | ✅ |
| **Frontend**: `StudentsLinkPage` — teacher column + inline assignment UI      | ✅ |
| **Frontend**: `types/teacher.ts` — teacher portal TS types                   | ✅ |
| **Frontend**: `teacher.service.ts` — 4 teacher API calls                     | ✅ |
| **Frontend**: `useTeacher.ts` — teacher portal hooks                         | ✅ |
| **Frontend**: `TeacherDashboardPage`, `StudentsPage`, `StudentMonitorPage`    | ✅ |
| **Frontend**: `teacher.routes.tsx` with role gate                            | ✅ |
| **Frontend**: `teacher.*` i18n block + `admin.students` teacher keys          | ✅ |
| **Docs**: `teacher-portal-mvp.md`, `teacher-student-assignment-notes.md`     | ✅ |
| **Bug fix**: `student-admin.service.ts` double `/api/` URL prefix corrected  | ✅ |

**Test totals (all passing):** 139 backend xUnit + 41 frontend Vitest

---

## Readiness Checklist for Section 9

### Architecture

- [x] `TeacherStudentAssignment` M:M model is in place — no 1:1 FK remnants in domain or infrastructure
- [x] `TeacherController` has `[Authorize(Roles = "Teacher")]` — role enforcement at framework layer
- [x] Ownership enforcement is in `TeacherService.AssertAssignedStudentAsync`, not the controller
- [x] `TeacherAccessDeniedException` returns 403, not 404 — privacy-preserving (matches parent portal pattern)
- [x] `ICurrentUserService.UserId` is from JWT claims — callers cannot spoof identity
- [x] `Question.CorrectAnswer` is not included in any teacher-facing DTO
- [x] `TeacherStudentAssignment.AssignedByUserId` provides audit trail for assignments
- [x] Unique composite index `(TeacherUserId, StudentProfileId)` prevents duplicate assignments at DB level

### Data Access

- [x] All `TeacherService` read queries use `AsNoTracking()`
- [x] N+1 queries avoided — assigned student IDs loaded in one query, then students fetched separately
- [x] `AssignTeacherAsync` uses delete-then-insert (correctly enforces Phase 1 one-teacher convention)
- [x] Admin student list resolves current teacher via `GroupBy + OrderByDescending(AssignedAt)` — handles multiple historical assignments correctly

### Frontend

- [x] All teacher routes carry `access: ['teacher']` on the `RouteHandle`
- [x] `StudentsLinkPage` uses discriminated `EditState` — only one field editable at a time per student
- [x] `useTeacherOptions` and `useAssignTeacher` follow same TanStack Query pattern as parent hooks
- [x] `assignTeacher` mutation invalidates `studentAdminKeys.students` on success — list stays fresh
- [x] All new i18n strings are in `admin.students.*` and `teacher.*` blocks in `en.json`
- [x] API URL path inconsistency fixed (`/api/admin/...` → `/admin/...` in `student-admin.service.ts`)

### Testing

- [x] 57 API integration tests pass — includes 13 teacher endpoint tests + 8 student-link tests
- [x] 12 Application unit tests pass
- [x] 70 Domain unit tests pass
- [x] 41 Vitest frontend tests pass
- [x] Backend build: 0 errors, 0 warnings
- [x] Frontend: `tsc --noEmit` clean

### Migration

- [x] Migration `AddTeacherStudentAssignmentTable` generated
- [ ] **Migration not yet applied** — Docker not running. Apply with:
  ```
  dotnet ef database update --project src/LMS.Infrastructure --startup-project src/LMS.Api
  ```

---

## Known Gaps and Deferred Items

| Gap                                              | Decision     | Notes                                               |
|--------------------------------------------------|--------------|-----------------------------------------------------|
| `formatLastActivity` not i18n'd in `StudentMonitorPage` | Deferred | Hardcoded English. Deferred to Phase 3 with parent portal same gap. |
| No `TeacherProfile` entity                       | Deferred     | Phase 1 uses `User.Id` as teacher identity. Phase 3 adds profile entity. |
| Subject-scoped co-teaching                       | Deferred     | Phase 1: one teacher per student (admin convention). Phase 3: true M:M. |
| Teacher self-service assignment                  | Out of scope | Teachers cannot assign themselves students in Phase 1. |

---

## Section 8 Test Summary

```
Backend (dotnet test):
  LMS.Domain.Tests     — 70 tests  ✅
  LMS.Application.Tests — 12 tests ✅
  LMS.Api.Tests        — 57 tests  ✅
  Total                — 139 tests ✅

Frontend (vitest run):
  41 tests             ✅
  tsc --noEmit         ✅ (0 errors)
```
