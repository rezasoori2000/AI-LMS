namespace LMS.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// A new GUID is assigned on construction; the ID is never externally settable.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
