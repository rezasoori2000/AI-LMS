namespace LMS.Domain.Users;

/// <summary>
/// Defines the role assigned to a platform user.
///
/// Roles are stored as their string name (not integer) in the JWT "role" claim so
/// payloads are self-describing and do not break if the enum order changes.
///
/// Current Phase 1 roles (admin, parent, student) are fully implemented.
/// Teacher, ContentEditor, and TenantAdmin are defined now so route handles and
/// authorization policies can reference them without a breaking change later.
/// </summary>
public enum UserRole
{
    /// <summary>Platform-level super-administrator; not scoped to any tenant.</summary>
    SuperAdmin,

    /// <summary>Administrator of a specific tenant organisation.</summary>
    TenantAdmin,

    /// <summary>Content author / curriculum editor within a tenant.</summary>
    ContentEditor,

    /// <summary>Classroom teacher within a tenant.</summary>
    Teacher,

    /// <summary>Parent or guardian with visibility into linked student accounts.</summary>
    Parent,

    /// <summary>Learner — the primary end-user of the platform.</summary>
    Student,
}
