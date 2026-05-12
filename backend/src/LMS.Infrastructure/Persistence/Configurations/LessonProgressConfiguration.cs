using LMS.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

internal sealed class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
{
    public void Configure(EntityTypeBuilder<LessonProgress> builder)
    {
        builder.ToTable("lesson_progress");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Status)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(50);

        // 5 integer digits, 2 decimal places → 0.00–100.00 %.
        builder.Property(p => p.ScorePercent)
               .HasColumnType("decimal(5,2)");

        builder.HasOne(p => p.Student)
               .WithMany()
               .HasForeignKey(p => p.StudentId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_lesson_progress_student");

        builder.HasOne(p => p.Lesson)
               .WithMany()
               .HasForeignKey(p => p.LessonId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_lesson_progress_lesson");

        // Each student has exactly one progress record per lesson.
        // Phase 3: replace with a LessonAttempt child table to support multiple attempts.
        builder.HasIndex(p => new { p.StudentId, p.LessonId })
               .IsUnique()
               .HasDatabaseName("ix_lesson_progress_student_lesson");

        builder.HasIndex(p => p.TenantId)
               .HasDatabaseName("ix_lesson_progress_tenant_id");
    }
}
