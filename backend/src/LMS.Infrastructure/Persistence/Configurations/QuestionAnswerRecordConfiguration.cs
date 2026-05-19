using LMS.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

internal sealed class QuestionAnswerRecordConfiguration : IEntityTypeConfiguration<QuestionAnswerRecord>
{
    public void Configure(EntityTypeBuilder<QuestionAnswerRecord> builder)
    {
        builder.ToTable("question_answer_records");

        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.Student)
               .WithMany()
               .HasForeignKey(r => r.StudentId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_question_answer_records_student");

        builder.HasOne(r => r.Lesson)
               .WithMany()
               .HasForeignKey(r => r.LessonId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_question_answer_records_lesson");

        builder.HasOne(r => r.Question)
               .WithMany()
               .HasForeignKey(r => r.QuestionId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_question_answer_records_question");

        // Primary query: all answers a student gave in a lesson (teacher/parent monitoring,
        // AI context assembly). Also uniquely identifies the record in Phase 1 (one
        // completion per lesson).
        builder.HasIndex(r => new { r.StudentId, r.LessonId })
               .HasDatabaseName("ix_question_answer_records_student_lesson");

        // Secondary query: has this student ever answered this question correctly?
        // Used by Phase 3 mastery checks and adaptive hint selection.
        builder.HasIndex(r => new { r.StudentId, r.QuestionId })
               .HasDatabaseName("ix_question_answer_records_student_question");

        builder.HasIndex(r => r.TenantId)
               .HasDatabaseName("ix_question_answer_records_tenant_id");
    }
}
