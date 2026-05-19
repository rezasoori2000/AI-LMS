using LMS.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

internal sealed class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.ToTable("student_profiles");

        builder.HasKey(s => s.Id);

        // One user may only hold one student profile.
        builder.HasIndex(s => s.UserId)
               .IsUnique()
               .HasDatabaseName("ix_student_profiles_user_id");

        builder.HasOne(s => s.User)
               .WithMany()
               .HasForeignKey(s => s.UserId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_student_profiles_user");

        // GradeId is nullable: student may be created before a grade is assigned.
        // SetNull so removing a grade doesn't cascade-delete student records.
        builder.HasOne(s => s.Grade)
               .WithMany()
               .HasForeignKey(s => s.GradeId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull)
               .HasConstraintName("fk_student_profiles_grade");

        // ParentId is optional (not all students have a linked parent on the platform).
        // SetNull so removing a parent profile doesn't cascade-delete student records.
        // Phase 3: replace with ParentStudentLink M:M join table.
        builder.HasOne(s => s.Parent)
               .WithMany()
               .HasForeignKey(s => s.ParentId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull)
               .HasConstraintName("fk_student_profiles_parent");

        // Teacher linkage is managed via TeacherStudentAssignment M:M join table.
        // See TeacherStudentAssignmentConfiguration for FK and index definitions.

        builder.HasIndex(s => s.TenantId)
               .HasDatabaseName("ix_student_profiles_tenant_id");

        builder.HasIndex(s => s.GradeId)
               .HasDatabaseName("ix_student_profiles_grade_id");
    }
}
