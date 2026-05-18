# Parent Portal — Phase 1 MVP

> Phase 1, Section 6. Last updated: 2026-05-18.

---

## What This Document Covers

Concise reference for the Phase 1 parent portal: what is built, how ownership is enforced,
the known architectural gaps, and what is explicitly deferred to later phases.

---

## Scope (Phase 1)

The Phase 1 parent portal is read-only. A parent can:

- View all students linked to their account
- See each student's grade, active enrollment count, and lessons-completed count
- Navigate to a student's detail view showing per-subject enrollment + progress summaries
- See a dashboard overview combining the children list with aggregate stats

No write operations are exposed to parents in Phase 1.

---

## Module Structure

```
backend/
  LMS.Application/Parent/
    IParentService.cs           — read-only service interface
    ParentService.cs            — EF Core implementation; enforces ownership
    ParentAccessDeniedException.cs
    Dtos/
      ParentDtos.cs             — ChildSummaryDto, EnrollmentSummaryDto, ChildDetailResponse

  LMS.Api/Controllers/Parent/
    ParentController.cs         — GET /api/parent/children, GET /api/parent/children/{id}

frontend/
  src/types/parent.ts           — TS types matching backend DTOs
  src/services/parent.service.ts
  src/features/parent/hooks/useParent.ts
  src/pages/parent/
    ChildrenPage.tsx
    ChildDetailPage.tsx
  src/pages/dashboards/ParentDashboardPage.tsx
  src/routes/modules/parent.routes.tsx
```

---

## API Endpoints

| Method | Path                                  | Auth          | Description                                 |
|--------|---------------------------------------|---------------|---------------------------------------------|
| GET    | `/api/parent/children`                | Bearer Parent | All linked students. Returns `[]` when none.|
| GET    | `/api/parent/children/{studentId}`    | Bearer Parent | Full detail for one linked student.         |

**Ownership enforcement:** Both endpoints resolve the caller's `ParentProfile.Id` from the JWT
`sub` claim via `ICurrentUserService`. If a student's `StudentProfile.ParentId` does not match
the resolved profile, a `ParentAccessDeniedException` is thrown → 403 Forbidden.

The 403 is deliberate: returning 404 would allow enumeration of valid student IDs.

---

## Ownership Model

```
User (role=Parent)
  └── ParentProfile (UserId FK)          ← resolved from JWT sub claim
        └── StudentProfile.ParentId FK   ← ownership gate
              ├── StudentProfile A
              └── StudentProfile B
```

`ParentService.ResolveParentProfileIdAsync` looks up `ParentProfile.Id` for `currentUser.UserId`.
If no `ParentProfile` exists, it returns `null` and `GetMyChildrenAsync` returns `[]` (safe default).

---

## Response DTOs

### `ChildSummaryDto`
```json
{
  "studentId":         "guid",
  "fullName":          "string",
  "gradeName":         "string | null",
  "activeEnrollments": 2,
  "lessonsCompleted":  14,
  "lastActivityAt":    "2026-05-01T10:30:00Z | null"
}
```

### `ChildDetailResponse`
```json
{
  "summary": { /* ChildSummaryDto */ },
  "enrollments": [
    {
      "enrollmentId":     "guid",
      "subjectId":        "guid",
      "subjectName":      "Mathematics",
      "status":           "Active | Completed | Dropped",
      "enrolledAt":       "ISO 8601",
      "totalLessons":     20,
      "completedLessons": 12,
      "inProgressLessons": 1,
      "averageScore":     87.5
    }
  ]
}
```

Enums are serialised as strings (`JsonStringEnumConverter`).

---

## Known Gaps in Phase 1

### 1. ParentProfile is not auto-created on registration

When a user registers with `UserRole.Parent`, no `ParentProfile` record is created automatically.
The portal will show an empty children list (safe but unhelpful). In the current dev environment,
profiles are created by `DatabaseSeeder`. In production, an admin or onboarding flow must create
the record.

**Impact:** A parent who self-registers in a real environment cannot use the portal until an admin
creates their `ParentProfile`.  
**Mitigation:** Documented. DatabaseSeeder covers the dev case. See
[parent-student-linkage-notes.md](parent-student-linkage-notes.md) for the full flow.

### 2. Linkage is admin-managed only

Parent-student links are set via `PATCH /api/admin/students/{id}/parent`. There is no
parent-facing onboarding or invitation flow in Phase 1.

### 3. formatLastActivity uses hardcoded English strings

`ChildrenPage` and `ChildDetailPage` both contain a `formatLastActivity` helper that outputs
"Today", "Yesterday", and "{n} days ago" as hardcoded English. These strings are not in the i18n
catalogue. This is noted as a Phase 2 cleanup item.

### 4. No end-to-end happy-path integration test

The integration tests cover auth guards (401/403) and the empty-list case. They do not exercise
the full happy path (parent with seeded linked children) because setting up `ParentProfile` +
`StudentProfile` + `Enrollment` + `LessonProgress` in the InMemory test factory is not yet
structured. This is a known Phase 2 test gap.

---

## Frontend Routes

| Path                          | Component             | Role Gate |
|-------------------------------|-----------------------|-----------|
| `/parent`                     | ParentDashboardPage   | parent    |
| `/parent/children`            | ChildrenPage          | parent    |
| `/parent/children/:studentId` | ChildDetailPage       | parent    |
| `/parent/progress`            | ComingSoonPage        | parent    |

Role gate is enforced by `ProtectedRoute` (`RouteHandle.access: ['parent']`).

---

## Explicitly Deferred to Later Phases

| Feature                                      | Target     |
|----------------------------------------------|------------|
| ParentProfile auto-creation on registration  | Phase 2    |
| Parent-teacher messaging                     | Phase 3    |
| Notifications (new grade, assignment due)    | Phase 3    |
| Billing and subscription management          | Phase 3+   |
| Parent invitation / onboarding flow          | Phase 2    |
| Multi-guardian M:M linkage                   | Phase 3    |
| Per-lesson progress timeline                 | Phase 2    |
| Weekly performance trend chart               | Phase 2    |
| formatLastActivity i18n                      | Phase 2    |
| formatLastActivity deduplication (util)      | Phase 2    |
| Activity feed / recent lessons               | Phase 2    |
| Advanced analytics and reporting             | Phase 3+   |
