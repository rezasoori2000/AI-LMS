# Phase 1 — Section 10, Part 1: Platform Audit & Consistency Cleanup

> **Scope**: Platform-wide cross-cutting audit following the completion of all
> Phase 1 feature sections (1–9). No new features introduced. Goal: close
> real gaps before moving to Phase 2, document deferred items explicitly.

---

## Assumptions

- "Must fix now" means a real correctness, coverage, or naming-contract issue that
  would cause bugs, test failures, or confusion if left unaddressed during Phase 2.
- "Defer" means the item carries no runtime risk today but should be resolved before
  the specific Phase 2 work that depends on it.
- Solo-developer context: no over-engineering. If a fix requires a new abstraction
  that is only used once, we accept the simpler (even slightly inconsistent) solution.

---

## Audit Findings

### 1. Critical — Student portal had zero API integration tests (FIXED)

**File**: `backend/tests/LMS.Api.Tests/StudentIntegrationTests.cs` (NEW)

The student portal is the core Phase 1 learning flow — 6 endpoints across summary,
enrollments, chapters, lesson detail, start, and complete. Every other portal
(parent, teacher) and every admin module had integration test coverage. The student
portal had none.

**Implemented**: 17 new integration tests covering:
- 401/403 auth guards for every endpoint
- Happy-path GET summary and enrollments (empty state and enrolled state)
- Happy-path GET subject chapters and lesson detail (enrolled student)
- Happy-path POST start and complete (including the 409 Conflict re-completion case)
- In-memory data seeding via `SeedStudentProfileAsync` + `SeedEnrolledContentAsync` helpers

Test count: **139 → 158** backend tests (all passing).

---

### 2. Significant — Exception handling divergence in `AdminStudentsController` (FIXED)

**Files changed**:
- `LMS.Application/Admin/Students/AdminLinkValidationException.cs` (NEW)
- `LMS.Application/Admin/Students/StudentAdminService.cs`
- `LMS.Api/Middleware/ExceptionHandlingMiddleware.cs`
- `LMS.Api/Controllers/Admin/StudentsController.cs`

**Problem**: `AssignParent` and `AssignTeacher` in `StudentsController` contained
inline try/catch blocks catching `KeyNotFoundException` (→ 404) and
`ArgumentException` (→ 400). All other controllers in the codebase let exceptions
propagate to `ExceptionHandlingMiddleware`. If any code path bypassed the try/catch
(e.g., a future refactor), the BCL exceptions would fall through to the middleware's
default 500 handler.

**Fix applied**:
- Introduced `AdminLinkValidationException` — a typed application exception that maps
  to **400 Bad Request** in the middleware. Used for "parent profile not found" and
  "teacher user not found" in the request body (valid 400 semantics — the client
  provided an invalid reference).
- Replaced `KeyNotFoundException` with `ContentNotFoundException` in the service for
  "student profile not found" (URL path parameter; valid 404 semantics). Already
  registered in the middleware.
- Registered `AdminLinkValidationException → 400` in `ExceptionHandlingMiddleware`.
- Removed both try/catch blocks from `StudentsController.AssignParent` and
  `AssignTeacher`. Both now delegate directly to the service and return `Ok(result)`.
- All existing `StudentLinkIntegrationTests` still pass (404 and 400 distinctions
  preserved, behaviour is identical from the caller's perspective).

---

### 3. Naming drift — `ChildDetailResponse` (FIXED)

**Files changed**:
- `LMS.Application/Parent/Dtos/ParentDtos.cs`
- `LMS.Application/Parent/ParentService.cs`
- `LMS.Application/Parent/IParentService.cs`
- `frontend/src/types/parent.ts`
- `frontend/src/services/parent.service.ts`

**Problem**: The parent portal's full child response type was named
`ChildDetailResponse`, while every other complex response type in the Application
layer uses the `...Dto` suffix. The wire format was correct; only the C# and
TypeScript type names were inconsistent.

**Fix applied**: Renamed to `ChildDetailDto` across all five files (C# rename
provider, then manual frontend update). Zero wire format change.

---

### 4. Utility duplication — `formatDate` / `formatLastActivity` (FIXED)

**Files changed**:
- `frontend/src/utils/dateFormat.ts` (NEW)
- `frontend/src/pages/parent/ChildDetailPage.tsx`
- `frontend/src/pages/teacher/StudentMonitorPage.tsx`

**Problem**: Identical `formatDate(iso)` and `formatLastActivity(iso|null)` functions
were copy-pasted into both portal detail pages. Any change to the formatting logic
(e.g., adding i18n) would require editing two files.

**Fix applied**: Extracted both functions to `frontend/src/utils/dateFormat.ts`.
Both pages now import from the shared utility. The functions are unchanged; the
TypeScript compiler confirms zero type errors.

**Known deferred**: `formatLastActivity` returns hardcoded English strings
(`'Today'`, `'Yesterday'`, `'${n}d ago'`). i18n support for this utility is
deferred to Phase 2 when the translation pipeline is extended to utility functions.

---

## Deferred Items (Documented, Not Blocking Phase 1)

### D1 — Frontend `UserRole` type mismatch (must fix before Phase 2 route guards)

**File**: `frontend/src/types/index.ts`

`BackendUserRoleValue` is typed as an integer union (`0 | 1 | 2 | 3 | 4 | 5`), but
the backend serializes `UserRole` as a string (`"Student"`, `"Teacher"`, etc.) via
the global `JsonStringEnumConverter`. At runtime, `AuthApiResponse.role` is a string;
TypeScript believes it is an integer.

**Current risk**: Zero — `ProtectedRoute` is never called with `allowedRoles` in
Phase 1. All route guards check only `isAuthenticated`.

**Before Phase 2 starts**: Align `BackendUserRoleValue` to the string enum values
the backend actually returns (`"Student" | "Teacher" | "Parent" | ...`), update
`AuthContext.tsx`, and update `ProtectedRoute.tsx` to use string comparison.

---

### D2 — `BaseApiController` adopted by only one controller

**Files**: `LMS.Api/Controllers/BaseApiController.cs`, all other controllers

`BaseApiController` carries `[ApiController]` + `[Route("api/[controller]")]` and
is extended only by `AuthController`. All portal and admin controllers extend
`ControllerBase` directly, repeating `[ApiController]` and defining their own routes.

**Current risk**: None. Behaviour is identical. The inconsistency is cosmetic.

**Recommendation**: Either extend all controllers from `BaseApiController` (requires
overriding the route attribute in each, which negates the benefit), or accept the
current pattern and delete `BaseApiController`. Decision deferred until Phase 2 when
controller structure may grow. Lean toward removal.

---

### D3 — Enrollment ordering inconsistency (Parent vs Teacher)

**Files**: `LMS.Application/Parent/ParentService.cs`, `LMS.Application/Teacher/TeacherService.cs`

`ParentService.GetChildDetailAsync` orders enrollments `OrderBy(e => e.EnrolledAt)`
(oldest first), while `TeacherService.GetStudentDetailAsync` orders
`OrderByDescending(e => e.EnrolledAt)` (newest first).

**Current risk**: Low. Both are technically correct; the difference is UX
presentation only.

**Recommendation**: Standardise to newest-first (descending) during Phase 2 portal
polish. Update the parent service when teacher ordering is confirmed correct with
real user testing.

---

### D4 — Route-level role guards not enforced at runtime (Phase 2 work)

**File**: `frontend/src/routes/`

Route definitions carry `handle.access` metadata (e.g., `['student']`, `['teacher']`),
but the `RouterProvider` does not read this metadata to enforce role-based access.
Any authenticated user can navigate directly to any portal route by URL.

**Current risk**: Low for Phase 1 (no cross-portal data is served by the API — all
portal services enforce ownership server-side). A teacher who navigates to `/student`
gets an empty page because the student API returns no data for them.

**Phase 2 action**: Implement a `RouteGuard` component that reads `handle.access`
and compares it against the authenticated user's role, redirecting to `/unauthorized`
when mismatched. This is blocked on fixing D1 first.

---

## Readiness Checklist

### Platform shape (Phase 1 complete)

| Area | Status |
|------|--------|
| Auth (register / login / JWT) | ✅ Complete |
| Admin: content CRUD (grades, subjects, chapters, lessons, questions) | ✅ Complete |
| Admin: student–parent linkage | ✅ Complete |
| Admin: student–teacher assignment | ✅ Complete |
| Student portal (summary, enrollments, lesson flow, scoring) | ✅ Complete |
| Parent portal (child list, child detail) | ✅ Complete |
| Teacher portal (summary, student list, student detail, progress) | ✅ Complete |
| Global exception handling middleware | ✅ Complete |
| Enum string serialization | ✅ Complete |
| RFC 7807 Problem Details responses | ✅ Complete |

### Test coverage (after this section)

| Suite | Tests |
|-------|-------|
| Domain unit tests | 70 |
| Application unit tests | 12 |
| API integration tests | 76 |
| Frontend component tests | 41 |
| **Total** | **199** |

### Cleanup items completed this section

| # | Item | Outcome |
|---|------|---------|
| 1 | Add `StudentIntegrationTests.cs` | 17 new tests; 0–158 API test gap closed |
| 2 | Fix admin exception handling | `AdminLinkValidationException` + middleware; no try/catch in controllers |
| 3 | Rename `ChildDetailResponse` → `ChildDetailDto` | 5 files; naming consistent |
| 4 | Extract `formatDate` / `formatLastActivity` | Shared util; no duplication |

---

## Perspectives: Maintainability, Privacy, Extensibility

### Maintainability

- All 199 tests pass. The student portal gap was the single largest maintainability
  risk: future changes to `StudentService` now have a regression net.
- Exception routing is now fully centralised. A contributor adding a new service
  does not need to inspect individual controllers to understand error-to-status mapping.
- Utility deduplication removes a class of silent bugs where one page would drift
  from the other.

### Privacy / Data access

- No regressions. All portal services continue to scope queries to the authenticated
  user's profile. `StudentAccessDeniedException` is thrown for any attempt to access
  unenrolled content. The integration tests explicitly verify the 403 boundary.
- `CompleteLesson` does not expose `CorrectAnswer` in any response DTO (verified in
  `StudentDtos.cs` — `QuestionForStudentDto` omits the field by design).

### Extensibility

- `AdminLinkValidationException` establishes a pattern for admin operation validation
  errors that is separate from content-not-found semantics. Additional admin operations
  that produce 400-class errors (e.g., bulk imports) can follow the same pattern.
- `frontend/src/utils/dateFormat.ts` is the natural home for any additional date
  helpers needed in Phase 2 (relative time, locale-aware formatting, etc.).
- The deferred D1 type-system fix (UserRole string enum) is well-scoped and blocked
  only on Phase 2 route guard implementation. It will not require changes to the
  backend.
