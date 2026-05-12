using LMS.Domain.Curriculum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

internal sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("subjects");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Slug).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Description).HasMaxLength(1000);

        // Slug must be unique per tenant so that URL routing is unambiguous.
        builder.HasIndex(s => new { s.Slug, s.TenantId })
               .IsUnique()
               .HasDatabaseName("ix_subjects_slug_tenant");

        builder.HasIndex(s => s.TenantId)
               .HasDatabaseName("ix_subjects_tenant_id");
    }
}
