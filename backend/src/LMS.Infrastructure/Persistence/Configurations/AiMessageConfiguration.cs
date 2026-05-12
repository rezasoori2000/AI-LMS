using LMS.Domain.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

internal sealed class AiMessageConfiguration : IEntityTypeConfiguration<AiMessage>
{
    public void Configure(EntityTypeBuilder<AiMessage> builder)
    {
        builder.ToTable("ai_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(50);

        // Content may be very long (AI responses).  No max length → PostgreSQL TEXT.
        builder.Property(m => m.Content).IsRequired().HasColumnType("text");

        builder.Property(m => m.SentAt).IsRequired();

        // The relationship is configured in AiConversationConfiguration to keep the
        // aggregate root as the single configuration point.  Nothing to configure here.

        builder.HasIndex(m => m.ConversationId)
               .HasDatabaseName("ix_ai_messages_conversation_id");
    }
}
