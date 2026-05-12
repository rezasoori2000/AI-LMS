using LMS.Domain.Curriculum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

internal sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Title).IsRequired().HasMaxLength(300);

        // Markdown content — no max length; maps to PostgreSQL TEXT.
        builder.Property(l => l.Content).HasColumnType("text");

        builder.Property(l => l.Order).IsRequired();

        builder.HasOne(l => l.Chapter)
               .WithMany()
               .HasForeignKey(l => l.ChapterId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_lessons_chapter");

        // No two lessons may share the same order within a chapter.
        builder.HasIndex(l => new { l.ChapterId, l.Order })
               .IsUnique()
               .HasDatabaseName("ix_lessons_chapter_order");

        builder.HasIndex(l => l.TenantId)
               .HasDatabaseName("ix_lessons_tenant_id");
    }
}
