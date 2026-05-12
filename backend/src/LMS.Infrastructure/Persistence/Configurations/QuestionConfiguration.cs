using LMS.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions");

        builder.HasKey(q => q.Id);

        // Question text may be arbitrarily long; no max length.
        builder.Property(q => q.Text).IsRequired().HasColumnType("text");

        builder.Property(q => q.Type)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(50);

        builder.Property(q => q.Difficulty)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(50);

        // JSON array of option strings for MultipleChoice; null for TrueFalse / ShortAnswer.
        // Phase 3: normalise into a question_options child table.
        builder.Property(q => q.OptionsJson).HasColumnType("text");

        builder.Property(q => q.CorrectAnswer)
               .IsRequired()
               .HasMaxLength(1000);

        // LessonId is optional — a question can exist as a standalone bank item.
        // SetNull so removing a lesson does not cascade-delete its questions.
        builder.HasOne(q => q.Lesson)
               .WithMany()
               .HasForeignKey(q => q.LessonId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull)
               .HasConstraintName("fk_questions_lesson");

        builder.HasIndex(q => q.LessonId)
               .HasDatabaseName("ix_questions_lesson_id");

        builder.HasIndex(q => q.TenantId)
               .HasDatabaseName("ix_questions_tenant_id");
    }
}
