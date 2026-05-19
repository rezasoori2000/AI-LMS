# Teacher–Student Assignment — Architecture Notes

> Phase 1, Section 8. Last updated: 2026-05-19.

---

## Overview

This document covers the `TeacherStudentAssignment` entity introduced in Section 8, Part 4:
the schema, query patterns, admin assignment flow, and the rationale for the M:M design.

---

## Schema

**Table**: `teacher_student_assignments`

| Column              | Type        | Constraints                        |
|---------------------|-------------|------------------------------------|
| `id`                | UUID PK     | Generated                          |
| `teacher_user_id`   | UUID FK     | → `users.id` ON DELETE RESTRICT    |
| `student_profile_id`| UUID FK     | → `student_profiles.id` ON DELETE CASCADE |
| `assigned_at`       | timestamptz | Set on creation (UTC)              |
| `assigned_by_user_id` | UUID FK   | → `users.id` ON DELETE RESTRICT    |

**Unique index**: `ix_teacher_student_assignments_unique` on `(teacher_user_id, student_profile_id)`

**Migration**: `20260519120258_AddTeacherStudentAssignmentTable`

---

## Entity

```csharp
public sealed class TeacherStudentAssignment : Entity
{
    public Guid TeacherUserId      { get; private set; }
    public Guid StudentProfileId   { get; private set; }
    public DateTime AssignedAt     { get; private set; }
    public Guid AssignedByUserId   { get; private set; }
    public User? Teacher           { get; private set; }
    public StudentProfile? Student { get; private set; }
}
```

`TeacherUserId` is `User.Id` — there is no `TeacherProfile` entity in Phase 1.

---

## Why M:M Instead of a 1:1 FK on StudentProfile

The previous design had `StudentProfile.TeacherId: Guid?` — a nullable FK directly on the
student row. This was removed in Section 8, Part 4 for these reasons:

1. **Phase 3 requires M:M anyway.** Co-teaching (multiple teachers per student, scoped by subject) is a planned Phase 3 feature. Introducing M:M now means no breaking schema change later.
2. **Audit trail.** `AssignedAt` and `AssignedByUserId` record who made each assignment and when — not possible on a bare FK column.
3. **Cascade clarity.** The join table makes delete semantics explicit: deleting a student cascades to delete their assignments; deleting a teacher does *not* cascade (RESTRICT) — preventing silent data loss.
4. **Clean aggregate boundary.** `StudentProfile` no longer holds a direct reference to a `User` (teacher), keeping the domain aggregate tighter.

---

## Phase 1 Convention: One Teacher Per Student

The join table supports M:M, but Phase 1 enforces a single-teacher-per-student rule in the
**admin assignment flow** (`StudentAdminService.AssignTeacherAsync`):

```csharp
// Remove all existing teacher assignments for this student, then insert the new one.
var existing = await _db.TeacherStudentAssignments
    .Where(a => a.StudentProfileId == studentId)
    .ToListAsync(ct);
_db.TeacherStudentAssignments.RemoveRange(existing);
_db.TeacherStudentAssignments.Add(
    TeacherStudentAssignment.Create(request.TeacherUserId!.Value, studentId, assignedBy));
await _db.SaveChangesAsync(ct);
```

This means:
- A student can only appear in one teacher's roster at any time.
- The schema supports future co-teaching without migration.
- The Phase 1 admin UI reflects this: one "Assign Teacher" slot per student row.

---

## Query Patterns

### Teacher portal — load assigned students

```csharp
// Load the teacher's assigned student IDs first, then fetch student details.
var assignedIds = await _db.TeacherStudentAssignments
    .Where(a => a.TeacherUserId == teacherUserId)
    .Select(a => a.StudentProfileId)
    .ToListAsync(ct);

var students = await _db.StudentProfiles
    .Where(s => assignedIds.Contains(s.Id))
    .Include(s => s.Grade)
    // ...
    .ToListAsync(ct);
```

### Teacher portal — ownership gate (AssertAssignedStudentAsync)

```csharp
var assigned = await _db.TeacherStudentAssignments
    .AnyAsync(a => a.TeacherUserId == teacherUserId && a.StudentProfileId == studentId, ct);
if (!assigned) throw new TeacherAccessDeniedException();
```

### Admin list — resolve current teacher per student

```csharp
// Load all assignments, group by student, take most-recent by AssignedAt.
var assignments = await _db.TeacherStudentAssignments
    .Include(a => a.Teacher)
    .ToListAsync(ct);

var latestByStudent = assignments
    .GroupBy(a => a.StudentProfileId)
    .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.AssignedAt).First());
```

---

## AssignedByUserId Audit Trail

Every assignment records the admin user who performed it:

```csharp
TeacherStudentAssignment.Create(teacherUserId, studentProfileId, assignedByUserId)
// AssignedAt = DateTime.UtcNow, AssignedByUserId = _currentUser.UserId ?? Guid.Empty
```

This supports future audit log views without a schema change.

---

## Phase 3 Migration Path

When Phase 3 introduces co-teaching and/or subject-scoped assignments:

1. Add `SubjectId: Guid?` column to `teacher_student_assignments` (nullable — null = whole-student assignment as in Phase 1).
2. Add `TeacherProfile` entity (bio, display name, specialisations) linked `1:1` to `User`.
3. Remove the "delete-then-insert" convention in `AssignTeacherAsync` — allow multiple rows per student.
4. Update `TeacherService.GetMyStudentsAsync` to deduplicate (a student may appear via multiple subject assignments).
5. Admin UI upgrade: replace single-select with a multi-select or role-assignment modal.
