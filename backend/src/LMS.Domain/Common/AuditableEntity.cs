namespace LMS.Domain.Common;

/// <summary>
/// Extends <see cref="Entity"/> with audit trail fields.
/// EF Core will populate these via interceptors/save hooks (Phase 3).
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    // Set to the authenticated user's ID by the infrastructure save interceptor (Phase 3)
    public Guid? CreatedBy { get; protected set; }
    public Guid? UpdatedBy { get; protected set; }
}
