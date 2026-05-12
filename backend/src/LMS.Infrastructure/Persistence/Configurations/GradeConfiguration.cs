using LMS.Domain.Curriculum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

internal sealed class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.ToTable("grades");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).IsRequired().HasMaxLength(100);
        builder.Property(g => g.Level).IsRequired();

        // Same level number can coexist in different tenants (e.g., Level=1 in tenant A and B).
        // null TenantId = platform-wide template grades — also covered by this constraint.
        builder.HasIndex(g => new { g.Level, g.TenantId })
               .IsUnique()
               .HasDatabaseName("ix_grades_level_tenant");

        builder.HasIndex(g => g.TenantId)
               .HasDatabaseName("ix_grades_tenant_id");
    }
}
