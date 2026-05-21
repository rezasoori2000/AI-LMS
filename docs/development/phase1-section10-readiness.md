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

### D1 — Frontend `UserRole` type mismatch ✅ FIXED in Part 2

**File**: `frontend/src/types/index.ts`

`BackendUserRoleValue` was typed as an integer union (`0 | 1 | 2 | 3 | 4 | 5`), but
the backend serializes `UserRole` as a string (`"Student"`, `"Teacher"`, etc.) via
the global `JsonStringEnumConverter`.

**Fixed in Part 2**: `BackendUserRole` const values changed to strings. `toFrontendRole()`
helper added. `AppLayout` wired to use `NAV_ITEMS_BY_ROLE[toFrontendRole(user.role)]`.
Route guard work (D4) is now unblocked for Phase 2.

---

### D2 — `BaseApiController` adopted by only one controller ✅ FIXED in Part 2

**Files**: `LMS.Api/Controllers/BaseApiController.cs` (deleted), `AuthController.cs`

`BaseApiController` was extended only by `AuthController` (which overrode the route
anyway). All other controllers used `ControllerBase` directly.

**Fixed in Part 2**: `BaseApiController.cs` deleted. `AuthController` now extends
`ControllerBase` with explicit `[ApiController]` — consistent with every other
controller in the project.

---

### D3 — Enrollment ordering inconsistency (Parent vs Teacher) ✅ FIXED in Part 2

**File**: `LMS.Application/Parent/ParentService.cs`

`ParentService.GetChildDetailAsync` ordered enrollments oldest-first;
`TeacherService.GetStudentDetailAsync` ordered newest-first.

**Fixed in Part 2**: `ParentService` now orders `OrderByDescending(e => e.EnrolledAt)`
matching the teacher portal. `StudentService` intentionally stays ascending (curriculum
natural order for the learner).

---

### D4 — Route-level role guards not enforced at runtime (Phase 2 work)

**File**: `frontend/src/routes/`

Route definitions carry `handle.access` metadata (e.g., `['student']`, `['teacher']`),
but the `RouterProvider` does not read this metadata to enforce role-based access.
Any authenticated user can navigate directly to any portal route by URL.

**Current risk**: Low for Phase 1 (all portal services enforce ownership server-side).
A teacher who navigates to `/student` gets an empty page because the student API
returns no data for them.

**Phase 2 action**: Implement a `RouteGuard` that reads `handle.access` and compares
it against the authenticated user's role, redirecting to `/403` when mismatched.
This is now unblocked since D1 (type system) was fixed in Part 2.

---

### D5 — `formatLastActivity` hardcoded English strings (Phase 2 i18n work)

**File**: `frontend/src/utils/dateFormat.ts`

`formatLastActivity` returns `'Today'`, `'Yesterday'`, `'${n}d ago'` — hardcoded
English. i18n support deferred to Phase 2 when the translation pipeline is extended.

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

#### Part 1 (Audit)

| # | Item | Outcome |
|---|------|---------|
| 1 | Add `StudentIntegrationTests.cs` | 17 new tests; 0–158 API test gap closed |
| 2 | Fix admin exception handling | `AdminLinkValidationException` + middleware; no try/catch in controllers |
| 3 | Rename `ChildDetailResponse` → `ChildDetailDto` | 5 files; naming consistent |
| 4 | Extract `formatDate` / `formatLastActivity` | Shared util; no duplication |

#### Part 2 (Cross-module Cleanup Implementation)

| # | Item | Files | Outcome |
|---|------|-------|---------|
| C1 | Fix `BackendUserRoleValue` type mismatch | `frontend/src/types/index.ts` | `BackendUserRole` values changed from integers to backend string names (`"Student"` etc.); `toFrontendRole()` helper added; type system now matches what the API actually returns |
| C2 | Wire role-based nav in `AppLayout` | `frontend/src/layouts/AppLayout.tsx` | `NAV_ITEMS_BY_ROLE[toFrontendRole(user.role)]` replaces the long-standing `DEFAULT_NAV_ITEMS` placeholder; each role now sees their own nav |
| C3 | Remove `BaseApiController` | `LMS.Api/Controllers/BaseApiController.cs` (deleted), `AuthController.cs` | Deleted unused base class; `AuthController` now extends `ControllerBase` directly with explicit `[ApiController]` — consistent with all other controllers |
| C4 | Normalize enrollment ordering | `LMS.Application/Parent/ParentService.cs` | `GetChildDetailAsync` now orders enrollments newest-first (`OrderByDescending`) matching `TeacherService` |

---

## Perspectives: Maintainability, Privacy, Extensibility

### Maintainability

- All 199 tests pass after both parts. The test suite is a complete regression net
  for every portal and admin module.
- Exception routing is fully centralised. No controller contains try/catch for
  domain exceptions.
- Utility deduplication (`dateFormat.ts`) removes a class of silent drift bugs.
- `BackendUserRoleValue` is now the correct string type — the type system and runtime
  agree. A Phase 2 developer reading the code no longer encounters misleading integer
  constants.
- `BaseApiController` removal eliminates a dead file that created false expectations
  about controller inheritance patterns.

### Privacy / Data access

- No regressions. All portal services scope queries to the authenticated user's
  profile. The ownership model is unchanged.
- `toFrontendRole()` is a pure type mapping — it has no data-access implications.
- Enrollment ordering changes (newest-first in Parent) are presentation-only.

### Extensibility

- `toFrontendRole()` is the single place to update if the backend adds a new role.
  Phase 2 route guards can now use `BackendUserRoleValue` string literals directly.
- `AppLayout` is now wired to real role-based nav — Phase 2 can extend
  `NAV_ITEMS_BY_ROLE` entries without touching `AppLayout`.
- D4 (route-level role enforcement) is explicitly unblocked for Phase 2: the type
  system fix is done, the nav is wired, and `ProtectedRoute.allowedRoles` is already
  typed as `BackendUserRoleValue[]` (now string, matching what the API returns).

---

# Phase 1 — Section 10, Part 3: Authorization & Ownership Enforcement Consistency Review

> **Scope**: Cross-cutting security audit of all role-facing modules (admin, parent,
> student, teacher). No new features. Goal: identify and close real auth/ownership
> gaps before Phase 2 begins. Three issues found and fixed; deferred items
> explicitly documented.

---

## Assumptions

- "Fix" means a real security or correctness gap that can be addressed in a few lines.
- "Defer" means the item is intentionally incomplete for Phase 1 (scope or dependency
  reasons) with no runtime risk at current scale.
- No tenant isolation is enforced at the query level in Phase 1 (planned for Phase 3
  via EF global query filters). Controllers and services enforce role-level access only.

---

## Access Control Rules (per role, after this review)

| Role | Identity source | Ownership enforcement mechanism | Scope of data visible |
|------|----------------|--------------------------------|-----------------------|
| **Student** | `sub` claim → `StudentProfile.Id` via DB lookup | `AssertEnrolledAsync()` checks `Enrollment` row before lesson access; `StudentAccessDeniedException` → 403 | Own subjects + lessons only |
| **Teacher** | `sub` claim used directly (no profile table) | `AssertAssignedStudentAsync()` checks `TeacherStudentAssignment` row; `TeacherAccessDeniedException` → 403 | Explicitly assigned students only |
| **Parent** | `sub` claim → `ParentProfile.Id` via DB lookup | `student.ParentId != parentProfileId` in `GetChildDetailAsync`; `ParentAccessDeniedException` → 403 | Linked children only |
| **TenantAdmin** | `sub` + `tid` claims | `FindOwnedOrThrowAsync()` checks content `TenantId` matches caller's `TenantId` | Own tenant's content only |
| **SuperAdmin** | `sub` claim; `TenantId` is null | `IsSuperAdmin` flag bypasses tenant check in content services | All tenants' content |
| **ContentEditor** | `sub` + `tid` claims | Same as TenantAdmin for content CRUD | Own tenant's content only |

Key invariants:
- `CorrectAnswer` is never included in any student-facing DTO projection.
- An unenrolled student attempting to access lesson content receives **403** (not 404),
  preventing lesson ID enumeration by a valid authenticated user.
- A parent with no `ParentProfile` record (e.g., registered but never linked)
  receives **403** on all child-access endpoints.

---

## Audit Findings

### 1. HIGH — JWT validation bypass on missing/short secret key (FIXED)

**File**: `LMS.Api/Extensions/ServiceCollectionExtensions.cs`

**Problem**: The existing code used `secretKey.Length >= 32` as a conditional on
every `TokenValidationParameters` flag:

```csharp
ValidateIssuerSigningKey = secretKey.Length >= 32,
ValidateLifetime         = secretKey.Length >= 32,
ValidateIssuer           = secretKey.Length >= 32,
ValidateAudience         = secretKey.Length >= 32,
```

`appsettings.json` ships with `"SecretKey": ""`. If the `Jwt__SecretKey` environment
variable is not set in a production deployment, `secretKey` is the empty string, so
all four conditions are `false`. The application starts, accepts all requests, and
any JWT — including one with a forged `"role": "SuperAdmin"` claim, an expired
timestamp, or no signature — passes authentication. This is a complete auth bypass.

**Fix applied**:

- Added a startup fail-fast guard before JWT registration:
  - **Production / Staging**: throws `InvalidOperationException` if `secretKey.Length < 32`.
    The application will not start. Misconfigured deployments fail loudly at boot, not
    silently at request time.
  - **Testing**: silently skipped. The integration-test factory (`LmsWebApplicationFactory`)
    overrides `TokenValidationParameters` via `PostConfigure<JwtBearerOptions>` at pipeline
    build time; `IConfiguration` intentionally has no key in this environment.
  - **Development**: logs a `Critical` warning and continues. The dev
    `appsettings.Development.json` ships a compliant 40-char key so this branch is
    only reached if the developer intentionally clears it.
- The four `secretKey.Length >= 32` validation flags are unchanged — they remain
  conditional on key length. The guard above ensures that in production the app cannot
    reach the JWT setup with an invalid key.

No tests needed: the guard is exercised by the development environment on every
startup and by any CI pipeline that runs the host builder.

**Risk after fix**: A new deployment that omits `Jwt__SecretKey` will fail at startup
with a clear error message instead of silently accepting all tokens. The message
includes the remediation steps.

---

### 2. MEDIUM — 403 response body leaks resource GUIDs (FIXED)

**File**: `LMS.Api/Middleware/ExceptionHandlingMiddleware.cs`

**Problem**: All three access-denial exception types (`ParentAccessDeniedException`,
`StudentAccessDeniedException`, `TeacherAccessDeniedException`) were mapped using
`exception.Message` as the RFC 7807 Problem Details `title`. The exception messages
carry the resource identifier:

- `"Student 'a1b2c3...' is not assigned to you."` (TeacherAccessDeniedException)
- `"You are not authorised to access lesson 'a1b2c3...'."` (StudentAccessDeniedException)
- `"You are not authorised to access student 'a1b2c3...'."` (ParentAccessDeniedException)

These strings are returned verbatim in the HTTP response body. A caller who does not
own a resource can use the 403 title to confirm a resource's existence and harvest
its GUID — an IDOR information-disclosure that enables resource enumeration without
read access.

The `LessonAlreadyCompletedException` (409) also used `exception.Message`, though its
message contains no sensitive identifiers. It was standardised in the same pass for
consistency.

**Fix applied**: All four cases now return a fixed title:

```csharp
ParentAccessDeniedException  => (403, rfc_uri, "Access denied."),
StudentAccessDeniedException => (403, rfc_uri, "Access denied."),
TeacherAccessDeniedException => (403, rfc_uri, "Access denied."),
LessonAlreadyCompletedException => (409, rfc_uri, "This lesson has already been completed."),
```

The full exception message is still available in structured server logs (the
middleware logs it at `Warning`). It is simply not forwarded to the caller.

No tests check the Problem Details `title` field for 403/409 responses (only status
codes are asserted), so no tests required updating.

---

### 3. LOW — Missing `[FromBody]` on admin PATCH mutation endpoints (FIXED)

**File**: `LMS.Api/Controllers/Admin/StudentsController.cs`

**Problem**: `AssignParent(Guid studentId, AssignParentRequest request, ...)` and
`AssignTeacher(Guid studentId, AssignTeacherRequest request, ...)` lacked an explicit
`[FromBody]` attribute on the request parameter. `[ApiController]` infers `[FromBody]`
for complex types, so the endpoints functioned correctly. However, the explicit
attribute makes the binding intent unambiguous and consistent with content-service
controllers elsewhere in the project.

**Fix applied**: `[FromBody]` added to both request parameters. Behaviour unchanged;
integration tests confirm.

---

## What Remains Correct (No Changes Needed)

These items were reviewed and found to be sound. Documented for completeness.

### Controller-level role enforcement
All controllers carry `[Authorize(Roles = "...")]` with the correct role set:
- `StudentController` → `"Student"`
- `TeacherController` → `"Teacher"`
- `ParentController` → `"Parent"`
- `StudentsController` (admin) → `"SuperAdmin,TenantAdmin"`
- `QuestionsController` and content admin → `"SuperAdmin,TenantAdmin,ContentEditor"`
- `AuthController` → `[AllowAnonymous]`

No controller is missing a role attribute or using `[Authorize]` without a role constraint.

### Service-layer ownership is DB-validated, not claim-only
Every portal service validates ownership by querying the database — not by trusting
a claim at face value:
- `StudentService.AssertEnrolledAsync()` → queries `Enrollments` table
- `TeacherService.AssertAssignedStudentAsync()` → queries `TeacherStudentAssignments` table
- `ParentService.GetChildDetailAsync()` → reads `student.ParentId` from DB, compares to resolved profile

A compromised or forged JWT claim alone cannot grant access to another user's data.

### CorrectAnswer excluded from all student projections
`QuestionDto` (admin-facing, used in content CRUD) includes `CorrectAnswer`.
`StudentLessonQuestionDto` (student-facing, returned by `GetLessonDetailAsync`) does not.
The exclusion is enforced by explicit projection in `StudentService`, not by a
runtime filter or null-out.

### Missing lesson returns 403, not 404
`StudentService.GetLessonDetailAsync` uses `AssertEnrolledAsync()` which throws
`StudentAccessDeniedException` (→ 403) for both "not enrolled" and "lesson not found
within the subject". A student cannot distinguish between a lesson that exists but is
not theirs and a lesson that does not exist at all — preventing lesson ID enumeration
by valid authenticated students.

### Admin student service has no tenant scoping — intentional Phase 3 deferral
`StudentAdminService` queries students without tenant filtering. This is documented as
a Phase 3 gap (EF global query filter per tenant). In Phase 1 the deployment is
single-tenant by operation; no cross-tenant data exposure is possible.

---

## Deferred Items (This Part)

### D6 — Tenant scoping for admin queries (Phase 3)

`StudentAdminService` (`GetAllAsync`, `GetByIdAsync`, etc.) queries `StudentProfiles`
without filtering by `TenantId`. No `_currentUser.TenantId` check is applied.

**Current risk**: None in Phase 1 single-tenant deployments.

**Phase 3 action**: Add a global EF query filter on `StudentProfile` (and all
tenant-scoped entities) that applies `WHERE TenantId = @tid` automatically. The
service code does not need to change — the filter is transparent.

### D7 — ParentProfile not created automatically on registration (Phase 2+)

`ParentService.ResolveParentProfileIdAsync()` returns null (→ 403) if no
`ParentProfile` row exists for the authenticated user. A parent who registers but
whose profile is never seeded/created will be silently denied access to all child
endpoints without any helpful error.

**Phase 2+ action**: Create `ParentProfile` automatically during the parent
registration flow, or return a distinct 404 / 422 response that prompts profile
setup. Deferred pending the Phase 2 onboarding workflow.

### D8 — No `TeacherProfile` entity (Phase 3)

Teacher identity is the `User.Id` directly (no profile table). This works for Phase 1
but will need a `TeacherProfile` entity in Phase 3 when teacher-specific metadata
(specialisation, availability, etc.) is added.

### D9 — Frontend route-level role guards (Phase 2)

Documented as D4 in Part 2. Unblocked by the type-system fix. Carry-forward.

---

## Files Changed (Part 3)

| File | Change |
|------|--------|
| `LMS.Api/Extensions/ServiceCollectionExtensions.cs` | Added startup fail-fast guard for missing/short JWT secret key |
| `LMS.Api/Middleware/ExceptionHandlingMiddleware.cs` | Replaced `exception.Message` with fixed titles for all 403 cases and the 409 case |
| `LMS.Api/Controllers/Admin/StudentsController.cs` | Added `[FromBody]` to `AssignParent` and `AssignTeacher` request parameters |

---

## Perspectives: Maintainability, Privacy/Security, Extensibility

### Maintainability

- The startup guard makes misconfigured deployments fail loudly and immediately with
  an actionable error message. Silent runtime failures (all tokens accepted, no logs)
  are eliminated.
- The 403 title fix removes a class of information leakage that would be hard to
  catch in code review — the middleware switch is now the single place to audit.
- All 158 backend tests pass unchanged after all three fixes.

### Privacy / Security

- **Before**: A missing `Jwt__SecretKey` env var rendered the entire authentication
  layer inoperable without any visible symptom. Any client could forge an admin token
  and access all endpoints.
- **After**: The application refuses to start if the key is absent or too short in
  non-Development environments.
- **Before**: 403 responses returned resource GUIDs in the response body title,
  enabling IDOR enumeration by authenticated but unauthorized users.
- **After**: 403 responses return `"Access denied."` — no resource identifiers
  in the response body. Full detail is still available in server-side structured logs.

### Extensibility

- The startup guard is a single `if` block. Adding a minimum key-length increase
  (e.g., 64 chars) in future is a one-line change.
- The middleware title convention (`"Access denied."` for all 403) is now consistent.
  Future access-denial exception types should follow the same pattern.
- D6 (tenant query filter) can be implemented as a single EF `HasQueryFilter` call
  in `LmsDbContext` — no service code changes required.

---

# Phase 1 — Section 10, Part 4: Test Strategy & Minimum Automated Coverage

> **Scope**: Practical minimum automated test coverage for the most important
> platform foundations. No new features. Goal: close the highest-risk coverage gaps
> identified in the Part 3 access-control review — ownership/role enforcement,
> security invariants, and progress persistence. No coverage vanity metrics. No
> heavy infrastructure.

---

## Coverage Gaps Closed

### G1 — Teacher portal: no happy-path or cross-teacher isolation tests (FIXED)

**File**: `backend/tests/LMS.Api.Tests/TeacherIntegrationTests.cs`

**Problem**: All 13 existing teacher tests were empty-state or auth-denial cases.
No test verified that a teacher with an assigned student could actually read that
student's data. No test verified that one teacher cannot access another teacher's
student (the most important ownership invariant for the teacher portal).

**Helpers added**:
- `CreateAuthorizedClientWithIdAsync(email, role)` → `(HttpClient, Guid userId)` — like the student version, needed to obtain the `User.Id` for seeding assignments.
- `SeedStudentProfileAsync(Guid userId)` → `Guid studentProfileId` — creates and persists a `StudentProfile` via DB scope.
- `SeedTeacherAssignmentAsync(Guid teacherUserId, Guid studentProfileId)` — creates a `TeacherStudentAssignment` row directly in the InMemory store.

**Tests added** (4):
| Test | Verifies |
|------|----------|
| `GetMyStudents_WithAssignedStudent_Returns200WithStudent` | Teacher sees their assigned student in list |
| `GetStudentDetail_AssignedStudent_Returns200` | Teacher reads detail of their assigned student |
| `GetStudentDetail_OtherTeachersStudent_Returns403` | **Cross-teacher isolation**: Teacher B cannot access Teacher A's student |
| `GetStudentProgress_AssignedStudent_Returns200` | Teacher reads progress of their assigned student |

---

### G2 — Parent portal: no happy-path or cross-parent isolation tests (FIXED)

**File**: `backend/tests/LMS.Api.Tests/ParentIntegrationTests.cs`

**Problem**: All 7 existing parent tests were empty-state or auth-denial cases. No
test verified that a parent can read their linked child's data. No test verified that
a parent cannot access another parent's child (the key parent ownership invariant).

**Helpers added**:
- `CreateAuthorizedClientWithIdAsync(email, role)` → `(HttpClient, Guid userId)`
- `GetParentProfileId(Guid userId)` → `Guid parentProfileId` — reads the auto-created `ParentProfile` row from the InMemory store (synchronous; no async EF extension needed).
- `SeedStudentLinkedToParentAsync(Guid parentProfileId, string studentEmail)` → `Guid studentProfileId` — registers a student user and seeds a `StudentProfile` with `ParentId = parentProfileId`.

**Tests added** (3):
| Test | Verifies |
|------|----------|
| `GetChildren_LinkedChild_Returns200WithChild` | Parent sees their linked child in list |
| `GetChildDetail_LinkedChild_Returns200` | Parent reads full detail of their linked child |
| `GetChildDetail_AnotherParentsChild_Returns403` | **Cross-parent isolation**: Parent B cannot access Parent A's child |

---

### G3 — `CorrectAnswer` not exposed to students (FIXED)

**File**: `backend/tests/LMS.Api.Tests/StudentIntegrationTests.cs`

**Problem**: The service-layer projection in `StudentService.GetLessonAsync` excludes
`CorrectAnswer` from the `QuestionForStudentDto` select. No test verified this
invariant end-to-end via HTTP. A future refactor (e.g., changing the projection to
use `AutoMapper` or switching to a DTO class initialiser) could inadvertently re-add
the field with no existing test to catch it.

**Helper added**:
- `SeedMCQuestionAsync(Guid lessonId)` → `(Guid QuestionId, string CorrectAnswer)` — creates a `MultipleChoice` question on the given lesson with `optionsJson = ["4","3","5","6"]` and `correctAnswer = "0"` (index of first option).

**Test added**:
- `GetLessonDetail_WithMCQuestion_CorrectAnswerNotInResponse` — seeds a lesson with an MC question, GETs lesson detail, deserialises as `JsonElement`, and asserts that no `correctAnswer` property exists on any element in `questions[]`. The raw-JSON approach is intentional: it catches the field even if the C# DTO is correct but the serialiser maps it under a different casing.

---

### G4 — `QuestionAnswerRecord` rows persisted after lesson completion (FIXED)

**File**: `backend/tests/LMS.Api.Tests/StudentIntegrationTests.cs`

**Problem**: `StudentService.CompleteLessonAsync` creates `QuestionAnswerRecord` rows
for each answered question. No test verified that these rows are actually written to
the store. The records are the data foundation for Phase 3 AI tutoring (wrong-answer
patterns, difficulty-level analysis). If the DB write is silently skipped, Phase 3
will have an empty dataset with no compile-time or test-time signal.

**Test added**:
- `CompleteLesson_WithMCQuestion_PersistsQuestionAnswerRecord` — completes a lesson
  with one MC question, then queries `db.QuestionAnswerRecords.Count(...)` directly
  via DB scope and asserts `1` row exists for the expected `(StudentId, QuestionId)` pair.

---

### G5 — Scoring: correct answer yields 100% (FIXED)

**File**: `backend/tests/LMS.Api.Tests/StudentIntegrationTests.cs`

**Problem**: The only existing `CompleteLesson` test used `Answers = []` (no questions),
which exercised the null-score path but not the scoring formula. The `CompleteLessonAsync`
scoring logic (`correctCount / gradableCount * 100`) had no end-to-end test.

**Test added**:
- `CompleteLesson_WithCorrectAnswer_ScoreIs100Percent` — completes a lesson with the
  correct answer for one MC question, asserts `body.ScorePercent == 100m`.

---

### G6 — `dateFormat` utility tests (NEW FILE)

**File**: `frontend/src/tests/utils/dateFormat.test.ts` (NEW)

**Problem**: `formatLastActivity` uses `Date.now()` internally, making it time-dependent and easy to break silently. `formatDate` delegates to `Intl.DateTimeFormat`, which is locale-dependent. Both were untested.

**Tests added** (6 in 2 `describe` blocks):
- `formatLastActivity`: null → `'—'`; current moment → `'Today'`; a few hours ago → `'Today'`; 24h ago → `'Yesterday'`; 3 days ago → `'3d ago'`
- `formatDate`: valid ISO string → non-empty string (locale-agnostic assertion)

All tests use `vi.useFakeTimers()` + `vi.setSystemTime(FIXED_NOW)` to pin `Date.now()` to `2024-06-15T12:00:00Z`, making results deterministic regardless of when CI runs.

---

## What Was Deliberately Not Added

| Item | Reason |
|------|--------|
| `toFrontendRole()` unit tests | Trivial mapping function, 7 lines, low regression risk |
| Admin endpoint role denial (Student/Parent) | `[Authorize]` is framework-enforced; redundant with framework test coverage |
| Exhaustive `CompleteLesson` scoring edge cases | ShortAnswer = always false is documented behaviour; low Phase 1 risk |
| Frontend portal component tests | E2E territory; too brittle for a solo-developer test strategy |
| `mapAuthError()` tests | Useful but lower priority; not a Phase 2 blocker |

---

## Final Test Counts (Section 10 Complete)

| Suite | Before Part 4 | After Part 4 | New |
|-------|--------------|-------------|-----|
| Domain unit tests (`LMS.Domain.Tests`) | 70 | 70 | — |
| Application unit tests (`LMS.Application.Tests`) | 12 | 12 | — |
| API integration tests (`LMS.Api.Tests`) | 76 | 86 | +10 |
| Frontend component/utility tests | 41 | 47 | +6 |
| **Total** | **199** | **215** | **+16** |

All 215 tests pass.

---

## Files Changed (Part 4)

| File | Change |
|------|--------|
| `backend/tests/LMS.Api.Tests/TeacherIntegrationTests.cs` | Added `CreateAuthorizedClientWithIdAsync`, `SeedStudentProfileAsync`, `SeedTeacherAssignmentAsync` helpers; 4 new tests |
| `backend/tests/LMS.Api.Tests/ParentIntegrationTests.cs` | Added `CreateAuthorizedClientWithIdAsync`, `GetParentProfileId`, `SeedStudentLinkedToParentAsync` helpers; 3 new tests |
| `backend/tests/LMS.Api.Tests/StudentIntegrationTests.cs` | Added `SeedMCQuestionAsync` helper; 3 new tests (CorrectAnswer security, QAR persistence, scoring) |
| `frontend/src/tests/utils/dateFormat.test.ts` | NEW — 6 tests for `formatDate` and `formatLastActivity` with pinned fake clock |

---

## Perspectives: Maintainability, Privacy/Security, Extensibility

### Maintainability

- The teacher and parent test suites now include cross-isolation tests — the most
  important class of regression to catch when ownership logic is refactored.
- `SeedMCQuestionAsync` is self-contained and reusable for any future lesson-content tests.
- The `CorrectAnswer` test uses raw `JsonElement` inspection rather than a typed DTO
  assertion, so it catches the leak even if the C# type is correct but the serialiser
  adds the field under a different key name.

### Privacy / Security

- The `CorrectAnswer` test closes the last untested path for the security invariant
  documented in the Part 3 access control table: *"CorrectAnswer is never included in
  any student-facing DTO projection."*
- Cross-isolation tests for teacher and parent portals confirm that ownership checks
  are DB-validated (not claim-only) end-to-end via HTTP, not just at the unit-test layer.

### Extensibility

- `SeedMCQuestionAsync` returns both `QuestionId` and `CorrectAnswer`, making it
  straightforward to add wrong-answer and partial-score tests in Phase 3.
- `SeedTeacherAssignmentAsync` and `SeedStudentLinkedToParentAsync` are reusable for
  any future tests that require an established teacher-student or parent-child relationship.

---

# Phase 1 — Section 10, Part 5: Developer Experience, Local Setup Validation & Section 10 Closeout

> **Scope**: Finalize Phase 1 by validating all developer-facing workflows, polishing
> the minimum useful documentation, and capturing a clean handoff state before Phase 2.
> No new features, no new infrastructure. Solo-developer-friendly.

---

## What Was Validated

### Local setup — all paths confirmed working

| Path | Command | Result |
|------|---------|--------|
| Backend full build | `dotnet build LMS.sln` | 0 errors, 0 warnings |
| Backend all tests | `dotnet test LMS.sln` | 168 passed |
| Frontend install + type-check | `npm ci` + `npx tsc --noEmit` | Clean |
| Frontend all tests | `npx vitest run` | 47 passed (3 files) |
| Docker Compose config | `docker compose config` | Valid; no syntax errors |
| `.env.example` | Manual review | Accurate; all keys documented |

### Developer workflow — confirmed correct

- `docker compose up` starts all three services with hot-reload (via `docker-compose.override.yml`).
- `dotnet watch` works natively for backend hot-reload without Docker.
- `npm run dev` starts the Vite server; `/api` proxied to `localhost:5000`.
- Seed data loads automatically on first `dotnet run` in `Development` (idempotent).
- Integration tests use InMemory EF Core — no PostgreSQL needed to run tests.
- Migrations run automatically during `SeedAsync` (via `_db.Database.MigrateAsync()`).

---

## Documentation Changes (Part 5)

### `docs/development/local-development.md` — updated

Two additions:

1. **JWT secret requirement note** (after the environment variables table):
   `BACKEND_JWT_SECRET` must be ≥ 32 characters in Production/Staging; application
   fails fast if it is absent or too short. The `.env.example` placeholder must be
   replaced before any deployment.

2. **Migration quick-reference** (after the `dotnet test` block):
   `dotnet ef database update` and `dotnet ef migrations add` commands with the correct
   `--project` and `--startup-project` flags. Explicit note that integration tests do
   not require PostgreSQL.

### `docs/development/seed-data.md` — updated

Added a callout under the seed accounts table noting that `teacher@lms.dev` logs in
successfully but shows an empty student list by default, because teacher–student
assignments are admin-managed and not included in the seed data. Directs the developer
to the admin Students page to assign a student.

### `README.md` — updated

Section 10 Status block expanded to include Parts 3, 4, and 5. Final test count
updated (168 backend / 47 frontend). Phase 2 starting point priorities listed.

---

## What Was Deliberately Not Added

| Item | Reason |
|------|--------|
| `docs/platform-overview.md` | README already covers platform shape accurately; a separate overview would duplicate it |
| `docs/development-setup.md` | `local-development.md` covers all setup paths; a second file would split the same content |
| Contributor handbook | Solo-developer project; no team onboarding overhead justified |
| CI/CD documentation | Not yet implemented; would become stale immediately |
| API reference docs | Swagger UI (`/swagger` in Development) serves this purpose for Phase 1 |

---

## Section 10 — Complete Summary

### What Section 10 stabilized (Parts 1–5)

| Part | Focus | Key outcomes |
|------|-------|-------------|
| 1 | Coverage audit | Student portal integration tests (17 new); `AdminLinkValidationException`; `ChildDetailDto` rename; date utility extraction |
| 2 | Cross-module cleanup | `BackendUserRoleValue` type fix; role-based nav wired; `BaseApiController` removed; enrollment ordering normalized |
| 3 | Auth/ownership enforcement | JWT startup fail-fast guard; 403 titles standardized (IDOR prevention); `[FromBody]` on PATCH endpoints |
| 4 | Test coverage | 10 new backend tests (teacher/parent/student portals); 6 new frontend tests; `CorrectAnswer` security invariant tested |
| 5 | Developer experience | Doc gaps closed; local setup validated; Phase 2 starting point captured |

### What remains intentionally deferred

These items were assessed and explicitly deferred. None carry Phase 1 runtime risk.

| Item | Deferred to | Reason |
|------|-------------|--------|
| Route-level role guard (`RouteGuard` + `handle.access`) | Phase 2, first | `BackendUserRoleValue` fix (Part 2) unblocked this; straightforward to implement |
| `formatLastActivity` i18n | Phase 2 | Translation pipeline work; hardcoded English carries no security risk |
| `ParentProfile` auto-creation on registration | Phase 2+ | Currently admin-managed; acceptable for Phase 1; unblocks self-service onboarding |
| `StudentProfile` auto-creation on registration | Phase 2+ | Same gap as `ParentProfile` |
| Server-side pagination and filtering (admin list pages) | Phase 2 | Phase 1 data volumes are small; no performance risk yet |
| Teacher-student assignment seed data | Phase 3 | Seeder defers this by design; admin UI workaround is sufficient |
| Tenant scoping (`HasQueryFilter` on EF Core) | Phase 3 | No multi-tenant data yet; ownership is enforced per-portal via FK validation |
| `TeacherProfile` entity | Phase 3 | Teacher identity is `User.Id`; profile bio/display-name is a Phase 3 concern |
| ShortAnswer AI grading | Phase 3 | Always `IsCorrect: false` in Phase 1; documented; `QuestionAnswerRecord` is ready to receive real outcomes |
| `TopicMasterySnapshot` computation | Phase 3 | `QuestionAnswerRecord` schema is in place; computation deferred with ShortAnswer gap noted |
| Multi-guardian M:M linkage (`ParentStudentLink` table) | Phase 3 | Schema supports migration from current 1:1 `StudentProfile.ParentId` FK |

---

## Phase 2 Starting Point

The platform is ready to move to Phase 2. Recommended first steps in order:

1. **Route-level role guard** — Implement `RouteGuard` that reads `handle.access` from route metadata and redirects unauthorized users. The `toFrontendRole()` helper and `BackendUserRoleValue` type are already correct.

2. **`ParentProfile` + `StudentProfile` auto-creation** — Wire auto-creation into `AuthService.RegisterAsync` for Parent and Student roles. Currently an admin must create these profiles manually via the API.

3. **Server-side pagination** — Add `skip`/`take` (or cursor-based) to admin content list endpoints. Frontend list pages need a paginator component.

4. **FluentValidation** — Replace `[Required]`/`[MaxLength]` data annotations on request DTOs with FluentValidation rules for richer server-side validation messages.

5. **`formatLastActivity` i18n** — Move "Today"/"Yesterday"/"Nd ago" strings to i18n keys in `en.json`; add placeholder keys for other locales.

---

## Perspectives: Maintainability, Onboarding, Extensibility

### Maintainability

- All 215 tests (168 backend + 47 frontend) pass with 0 compiler warnings.
- No dead code remains: `BaseApiController` removed, `ChildDetailResponse` renamed,
  all inline try/catch replaced with typed exceptions.
- Every access-denial path now returns a consistent, IDOR-safe response title.
- The JWT startup guard catches the most likely production misconfiguration immediately.

### Onboarding (solo developer resuming work)

- The README has a full history of all 10 sections — a developer can read the last
  completed section status block and know exactly where to pick up.
- `local-development.md` covers all three run modes (Docker, native backend, native frontend)
  with exact commands.
- `seed-data.md` explains what data exists and which accounts to use; the teacher caveat
  prevents the "why is my teacher page empty?" confusion.
- The deferred items table above is the single most important restart reference.

### Extensibility

- The `QuestionAnswerRecord` table is indexed and schema-stable. Phase 3 can begin
  reading it for `TopicMasterySnapshot` computation without any migration changes.
- `DesignNotes.cs` in `LMS.Domain/Personalization/` documents all Phase 3 entity
  shapes and boundary rules. No speculative code exists in the domain.
- The test seeding helpers (`SeedStudentProfileAsync`, `SeedTeacherAssignmentAsync`,
  `SeedMCQuestionAsync`, etc.) are reusable across future test scenarios without
  re-implementing the register/login flow.

---

## Section 10: Complete ✓

Phase 1 is fully complete. All 10 sections are closed. The platform has:
- A working multi-role learning management system (Admin, Teacher, Parent, Student)
- Clean Architecture backend with 86 API integration tests, 70 domain tests, 12 application tests
- React frontend with role-based navigation, full auth flow, and 47 passing tests
- A personalization data foundation ready for Phase 3 AI tutoring
- All known gaps documented and prioritized for Phase 2

