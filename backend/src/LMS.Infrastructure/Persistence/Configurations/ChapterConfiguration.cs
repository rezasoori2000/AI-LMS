using LMS.Domain.Curriculum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

internal sealed class ChapterConfiguration : IEntityTypeConfiguration<Chapter>
{
    public void Configure(EntityTypeBuilder<Chapter> builder)
    {
        builder.ToTable("chapters");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title).IsRequired().HasMaxLength(300);
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.Order).IsRequired();

        // Restrict prevents accidental cascade-delete of chapters when a subject or grade
        // is removed.  Admin actions should explicitly remove chapters first.
        builder.HasOne(c => c.Subject)
               .WithMany()
               .HasForeignKey(c => c.SubjectId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_chapters_subject");

        builder.HasOne(c => c.Grade)
               .WithMany()
               .HasForeignKey(c => c.GradeId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_chapters_grade");

        // No two chapters may share the same display order within the same subject+grade.
        builder.HasIndex(c => new { c.SubjectId, c.GradeId, c.Order })
               .IsUnique()
               .HasDatabaseName("ix_chapters_subject_grade_order");

        builder.HasIndex(c => c.TenantId)
               .HasDatabaseName("ix_chapters_tenant_id");
    }
}
