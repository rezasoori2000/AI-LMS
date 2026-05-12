using LMS.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
               .IsRequired()
               .HasMaxLength(256);

        // Case-insensitive unique index on email using a lower-cased expression.
        // The domain factory already calls ToLowerInvariant() on Create, so a standard
        // unique index is sufficient for Phase 1.
        builder.HasIndex(u => u.Email)
               .IsUnique()
               .HasDatabaseName("ix_users_email");

        builder.Property(u => u.PasswordHash)
               .IsRequired()
               .HasMaxLength(512);

        // Store the enum as its string name so the column remains readable and
        // is not broken by reordering enum members.
        builder.Property(u => u.Role)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(50);

        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);
        builder.Property(u => u.IsActive).HasDefaultValue(true);

        // TenantId: no FK yet — Tenant aggregate is deferred to Phase 3.
        // Index supports efficient per-tenant queries when a global query filter is added.
        builder.HasIndex(u => u.TenantId)
               .HasDatabaseName("ix_users_tenant_id");
    }
}
