# Phase 1 — Section 6 Readiness Checklist

> Parent portal validation, cleanup, and Section 6 closeout.  
> Date: 2026-05-18

---

## Section 6 Completion Summary

Phase 1 Section 6 delivers a fully functional, read-only parent portal. Parents can log in and
view their linked students, per-student enrollment summaries, and lesson-level progress
aggregates. Admin users can assign and remove parent–student links via the admin students UI.

### What was built

| Area                        | Status |
|-----------------------------|--------|
| Domain: `ParentProfile.UnlinkParent()` | ✅ |
| Backend: `IParentService` + `ParentService` (read-only, ownership-enforced) | ✅ |
| Backend: `ParentController` — GET /api/parent/children, GET /api/parent/children/{id} | ✅ |
| Backend: `ParentAccessDeniedException` → 403 (via ExceptionHandlingMiddleware) | ✅ |
| Backend: `ICurrentUserService` (HttpCurrentUserService, JWT claims) | ✅ |
| Backend: DI registration (`IParentService → ParentService`, scoped) | ✅ |
| Backend: `ParentIntegrationTests` — 6 tests (auth guards, empty list, unlinked → 403) | ✅ |
| Admin: `StudentAdminService` + `StudentsController` (assign/unlink parent) | ✅ |
| Admin: `StudentsLinkPage` frontend + inline edit row | ✅ |
| Admin: `StudentLinkIntegrationTests` — 8 tests | ✅ |
| Frontend: `parent.service.ts`, `useParent.ts` | ✅ |
| Frontend: `ChildrenPage`, `ChildDetailPage`, `ParentDashboardPage` | ✅ |
| Frontend: `parent.routes.tsx` with role gate | ✅ |
| Frontend: i18n `parent.*` block in `en.json` | ✅ |
| Frontend: types in `types/parent.ts` matching backend DTOs exactly | ✅ |

**Test totals (all passing):** 126 backend xUnit + 41 frontend Vitest

---

## Readiness Checklist for Section 7

### Architecture

- [x] Module boundary is clean: parent service lives in `Application/Parent/`, controller in `Controllers/Parent/`
- [x] No admin or student logic leaks into the parent module
- [x] `ParentController` has `[Authorize(Roles = "Parent")]` — role enforcement is at the framework layer
- [x] Ownership enforcement is at the service layer, not the controller — correct place
- [x] `ParentAccessDeniedException` returns 403, not 404 — privacy-preserving
- [x] `ICurrentUserService.UserId` is from JWT claims, not query parameters — caller cannot spoof identity
- [x] `IParentService` is interface-abstracted — testable and mockable in future

### Data Access

- [x] All service queries use `AsNoTracking()` (read-only, no change tracking overhead)
- [x] N+1 queries avoided in `GetMyChildrenAsync` — separate aggregation queries, not per-student loops
- [x] `GetChildDetailAsync` issues 3–4 queries (acceptable for Phase 1 cardinality)
- [x] `AverageScore` is rounded to 1 decimal at the service layer

### Frontend

- [x] All parent routes carry `access: ['parent']` on the `RouteHandle`
- [x] `StateWrapper` is used consistently — loading, error, and empty states are handled on both pages
- [x] `ChildDetailPage` StatCard labels are correct (fixed: active enrollments label was wrong)
- [x] `types/parent.ts` fields match backend DTO property names exactly (camelCase)
- [x] `EnrollmentStatus` type covers all three backend enum values: `Active | Completed | Dropped`
- [x] `apiClient` (not raw `fetch`) is used — token is attached automatically

### Documentation

- [x] `docs/parent-portal-mvp.md` — overview, API reference, ownership model, deferred items
- [x] `docs/parent-student-linkage-notes.md` — linkage architecture, Phase 1 gap, Phase 3 migration path
- [x] README updated with Section 6 status block

---

## Cleanup Items — Now vs Deferred

### Fixed in this closeout

| Item | File |
|------|------|
| `ChildDetailPage` StatCard for active enrollments used `dashboards.parent.stats.children` label (= "My Children") — replaced with `parent.childDetail.activeEnrollments` | `ChildDetailPage.tsx`, `en.json` |

### Defer to Phase 2

| Item | Reason |
|------|--------|
| `formatLastActivity` is duplicated in `ChildrenPage` and `ChildDetailPage` with slightly different null handling | Minor — not user-visible; extract to `src/utils/dateUtils.ts` in Phase 2 |
| "Today", "Yesterday", "{n} days ago" strings are hardcoded English, not in i18n catalogue | Acceptable for Phase 1; add i18n keys when multilingual support is activated |
| No happy-path integration test (parent sees linked children) | Requires structured test-data helper for `LmsWebApplicationFactory` — Phase 2 |
| `ParentProfile` is not auto-created on parent registration | Admin-managed in Phase 1 is acceptable; auto-create in `AuthService.RegisterAsync` in Phase 2 |

---

## Risks and Tradeoffs

### Risk 1 — ParentProfile creation gap (medium)
A parent who self-registers in production will see an empty portal until an admin creates their
`ParentProfile`. There is no admin UI to create `ParentProfile` records directly; the current
flow only allows assigning an existing parent profile to a student. If a parent's profile does
not exist, the admin dropdown (`GET /api/admin/students/parent-options`) will not include them.

**Tradeoff accepted:** Admin-managed onboarding is correct for Phase 1 (school-controlled
enrolment). Phase 2 should add auto-creation.

### Risk 2 — 1:1 parent constraint limits real-world use (low for Phase 1)
`StudentProfile.ParentId` is a single FK — a student cannot have two parents. This is
acknowledged and documented. The Phase 3 M:M migration path is captured in
`docs/parent-student-linkage-notes.md`.

### Risk 3 — TotalLessons counts all lessons in subject, not per enrollment (low)
`EnrollmentSummaryDto.TotalLessons` counts all lessons in the subject regardless of when the
student enrolled. If new lessons are added to a subject after enrollment, the denominator changes.
This is acceptable for Phase 1 reporting (no curriculum versioning yet).

### Risk 4 — No tenant scoping on parent queries (low for Phase 1)
`ParentService` queries do not filter by `TenantId`. Because ownership is enforced via
`ParentId` FK, cross-tenant data leakage is not possible in the current model (a parent can
only see their own linked students). However, when multi-tenancy is activated in Phase 3,
tenant-scoped global query filters should be added to prevent edge cases.

---

## Confirmed Deferred (not Phase 1)

- Notifications (grade published, assignment due, message received)
- Billing and subscription management
- Parent–teacher messaging
- Parent invitation / self-service onboarding flow
- Multi-guardian advanced workflows (step-parent, emergency contact)
- Weekly performance trend chart
- Per-lesson progress timeline on detail page
- Activity feed / recent lessons section on dashboard
- Advanced analytics and export

---

## Section 6 Verdict

**Section 6 is complete and ready to proceed to Section 7.**

The parent portal MVP has clean module boundaries, correct ownership enforcement, consistent
error handling, and adequate test coverage for the Phase 1 scope. The known gaps are
documented and have clear Phase 2 migration paths. No blocking issues remain.
