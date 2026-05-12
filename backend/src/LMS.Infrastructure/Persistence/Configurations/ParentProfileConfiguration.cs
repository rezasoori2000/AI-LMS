using LMS.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

internal sealed class ParentProfileConfiguration : IEntityTypeConfiguration<ParentProfile>
{
    public void Configure(EntityTypeBuilder<ParentProfile> builder)
    {
        builder.ToTable("parent_profiles");

        builder.HasKey(p => p.Id);

        // One user may only hold one parent profile.
        builder.HasIndex(p => p.UserId)
               .IsUnique()
               .HasDatabaseName("ix_parent_profiles_user_id");

        // Restrict so that deleting a User requires explicit profile removal first.
        builder.HasOne(p => p.User)
               .WithMany()
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_parent_profiles_user");

        builder.HasIndex(p => p.TenantId)
               .HasDatabaseName("ix_parent_profiles_tenant_id");
    }
}
