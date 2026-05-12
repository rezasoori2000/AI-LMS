using LMS.Domain.Enrollments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

internal sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(50);

        builder.Property(e => e.EnrolledAt).IsRequired();

        builder.HasOne(e => e.Student)
               .WithMany()
               .HasForeignKey(e => e.StudentId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_enrollments_student");

        builder.HasOne(e => e.Subject)
               .WithMany()
               .HasForeignKey(e => e.SubjectId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_enrollments_subject");

        // Filtered unique index: only ONE Active enrollment per student+subject is allowed.
        // Completed and Dropped enrollments are terminal history — new enrollment creates a
        // new row, so the constraint must not cover non-Active rows.
        builder.HasIndex(e => new { e.StudentId, e.SubjectId })
               .IsUnique()
               .HasFilter("status = 'Active'")
               .HasDatabaseName("ix_enrollments_student_subject_active");

        builder.HasIndex(e => e.TenantId)
               .HasDatabaseName("ix_enrollments_tenant_id");
    }
}
