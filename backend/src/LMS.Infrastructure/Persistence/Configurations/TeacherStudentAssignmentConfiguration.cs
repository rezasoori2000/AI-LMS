using LMS.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

internal sealed class TeacherStudentAssignmentConfiguration
    : IEntityTypeConfiguration<TeacherStudentAssignment>
{
    public void Configure(EntityTypeBuilder<TeacherStudentAssignment> builder)
    {
        builder.ToTable("teacher_student_assignments");

        builder.HasKey(a => a.Id);

        // Phase 1 admin convention: one teacher per student.
        // The unique constraint enforces idempotent "replace-assign" semantics at the DB level.
        // Phase 3: drop this index when co-teaching is introduced.
        builder.HasIndex(a => new { a.TeacherUserId, a.StudentProfileId })
               .IsUnique()
               .HasDatabaseName("ix_teacher_student_assignments_unique");

        // Fast lookup: "which students does this teacher have?" (TeacherService primary path).
        builder.HasIndex(a => a.TeacherUserId)
               .HasDatabaseName("ix_teacher_student_assignments_teacher");

        // Fast lookup: "which teacher does this student have?" (admin detail path).
        builder.HasIndex(a => a.StudentProfileId)
               .HasDatabaseName("ix_teacher_student_assignments_student");

        // FK: teacher → Users.Id  (Restrict: don't silently drop assignments if a user is removed)
        builder.HasOne(a => a.Teacher)
               .WithMany()
               .HasForeignKey(a => a.TeacherUserId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_teacher_student_assignments_teacher");

        // FK: student → StudentProfiles.Id  (Cascade: assignments removed when student is removed)
        builder.HasOne(a => a.Student)
               .WithMany(s => s.TeacherAssignments)
               .HasForeignKey(a => a.StudentProfileId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("fk_teacher_student_assignments_student");

        // FK: assigned-by → Users.Id  (Restrict: audit record must stay intact)
        builder.HasOne<LMS.Domain.Users.User>()
               .WithMany()
               .HasForeignKey(a => a.AssignedByUserId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_teacher_student_assignments_assigned_by");
    }
}
