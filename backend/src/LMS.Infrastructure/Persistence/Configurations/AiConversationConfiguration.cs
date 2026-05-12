using LMS.Domain.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

internal sealed class AiConversationConfiguration : IEntityTypeConfiguration<AiConversation>
{
    public void Configure(EntityTypeBuilder<AiConversation> builder)
    {
        builder.ToTable("ai_conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.StartedAt).IsRequired();

        builder.HasOne(c => c.Student)
               .WithMany()
               .HasForeignKey(c => c.StudentId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_ai_conversations_student");

        builder.HasOne(c => c.Lesson)
               .WithMany()
               .HasForeignKey(c => c.LessonId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull)
               .HasConstraintName("fk_ai_conversations_lesson");

        // AiMessage is a child of this aggregate.  Cascade so deleting a conversation
        // also removes its messages (conversation is the root owner).
        builder.HasMany(c => c.Messages)
               .WithOne(m => m.Conversation)
               .HasForeignKey(m => m.ConversationId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("fk_ai_messages_conversation");

        // Messages uses a private backing field — configure EF Core to use it directly.
        builder.Navigation(c => c.Messages)
               .UsePropertyAccessMode(PropertyAccessMode.Field)
               .HasField("_messages");

        builder.HasIndex(c => c.StudentId)
               .HasDatabaseName("ix_ai_conversations_student_id");

        builder.HasIndex(c => c.TenantId)
               .HasDatabaseName("ix_ai_conversations_tenant_id");
    }
}
