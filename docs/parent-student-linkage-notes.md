# Parent–Student Linkage — Architecture Notes

> Phase 1, Section 6. Last updated: 2026-05-18.

---

## Phase 1 Model (Current)

### Schema

```
ParentProfile
  Id (PK)
  UserId (FK → Users.Id)
  TenantId (nullable)

StudentProfile
  Id (PK)
  UserId (FK → Users.Id)
  GradeId (FK → Grades.Id)
  ParentId (FK → ParentProfiles.Id, nullable)   ← linkage point
```

### Constraints

- **1 student : at most 1 parent.** `ParentId` is a single nullable FK.
- **1 parent : 0..N students.** Multiple `StudentProfile` rows can share the same `ParentId`.
- Linkage is **admin-managed**: set via `PATCH /api/admin/students/{id}/parent`.
- Unlinking: send `{ "parentProfileId": null }` to the same endpoint.
- `StudentProfile.UnlinkParent()` is the domain method that sets `ParentId = null`.

### Data flow (admin assigns a parent to a student)

```
Admin UI → PATCH /api/admin/students/{studentId}/parent
         → StudentAdminService.AssignParentAsync
         → validates ParentProfile exists (400 if not)
         → sets StudentProfile.ParentId = parentProfileId
         → saves via LmsDbContext
```

### Data flow (parent reads their children)

```
JWT (role=Parent) → GET /api/parent/children
                 → ParentService.GetMyChildrenAsync
                 → resolves ParentProfile.Id from ICurrentUserService.UserId
                 → queries StudentProfiles WHERE ParentId = resolvedId
                 → returns ChildSummaryDto[]
```

---

## ParentProfile Lifecycle (Phase 1 Gap)

**ParentProfile records are not auto-created when a parent registers.**

In Phase 1 there is no registration-time hook that creates a `ParentProfile`.
The gap is handled two ways:

| Context     | How ParentProfile is created                                   |
|-------------|----------------------------------------------------------------|
| Development | `DatabaseSeeder` seeds 2 `ParentProfile` rows with known IDs. |
| Production  | Manual: an admin must create the record (no UI in Phase 1).    |

**Why this is acceptable in Phase 1:**
Linkage is admin-managed. The platform admin controls which users are parents and which
students belong to them. Self-service parent onboarding is deferred to Phase 2.

**Recommended fix in Phase 2:**
Add a `ParentProfile` auto-creation step in `AuthService.RegisterAsync` when the registered
role is `UserRole.Parent`. One option:

```csharp
// Inside AuthService.RegisterAsync, after creating the User:
if (request.Role == UserRole.Parent)
{
    var profile = ParentProfile.Create(user.Id, request.TenantId);
    await _db.ParentProfiles.AddAsync(profile, ct);
}
```

Alternatively, a dedicated admin endpoint to provision profiles could be added in Phase 2.

---

## Phase 3 Migration Path — M:M Linkage

Phase 1 uses a single FK (`StudentProfile.ParentId`). This does not support:
- Step-parents / divorced families (multiple parents per child)
- Students monitored by a school counsellor
- Multi-guardian advanced workflows

**Phase 3 plan:**

1. Introduce a `ParentStudentLink` join table:

```
ParentStudentLink
  Id (PK)
  ParentProfileId (FK → ParentProfiles.Id)
  StudentProfileId (FK → StudentProfiles.Id)
  RelationshipType (string: "Parent", "Guardian", "Emergency")
  LinkedAt (DateTime)
  UNIQUE (ParentProfileId, StudentProfileId)
```

2. Migrate existing `StudentProfile.ParentId` rows into `ParentStudentLink` rows.
3. Null out `StudentProfile.ParentId` after migration.
4. Remove or deprecate `StudentProfile.ParentId` FK column.
5. Update `ParentService` queries to join through `ParentStudentLink`.

This migration is backward-compatible because the `ParentId` column can remain nullable
during the transition period.

---

## Privacy Notes

- A parent can only access students where `StudentProfile.ParentId == their ParentProfile.Id`.
- `ParentService` throws `ParentAccessDeniedException` (→ 403) on any access to an unlinked
  student. This is intentionally 403 and not 404 to prevent student ID enumeration.
- `ICurrentUserService.UserId` is resolved from the JWT `sub` claim at request time, never
  from a query parameter. The caller cannot impersonate a different parent.
- `ParentController` carries `[Authorize(Roles = "Parent")]` — non-parent roles receive 403
  from ASP.NET Core authorization before reaching the service.

---

## Test Coverage (Phase 1)

| Test                                          | Covered |
|-----------------------------------------------|---------|
| Anonymous → 401                               | ✅      |
| Non-parent role → 403                         | ✅      |
| Parent with no linked students → 200 `[]`     | ✅      |
| Parent accessing unlinked student → 403       | ✅      |
| Parent accessing own linked student → 200     | ❌ deferred (Phase 2 — requires seeding) |

The happy-path linked-student test is missing because the `LmsWebApplicationFactory` does not
yet have a helper to seed a `ParentProfile` + `StudentProfile` + linked data in a single step.
This is a known Phase 2 gap.
