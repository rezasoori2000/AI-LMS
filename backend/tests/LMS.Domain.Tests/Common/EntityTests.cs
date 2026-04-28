using LMS.Domain.Common;

namespace LMS.Domain.Tests.Common;

/// <summary>
/// Unit tests for <see cref="Entity"/> and <see cref="AuditableEntity"/>.
/// These are pure in-memory tests — no infrastructure dependencies.
/// </summary>
public class EntityTests
{
    // ── Concrete subclasses for testing (abstract types can't be instantiated) ──

    private sealed class TestEntity : Entity { }

    private sealed class TestAuditableEntity : AuditableEntity { }

    // ── Entity identity ───────────────────────────────────────────────────────

    [Fact]
    public void NewEntity_HasNonEmptyId()
    {
        var entity = new TestEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void TwoNewEntities_HaveDifferentIds()
    {
        var a = new TestEntity();
        var b = new TestEntity();

        Assert.NotEqual(a.Id, b.Id);
    }

    // ── AuditableEntity defaults ──────────────────────────────────────────────

    [Fact]
    public void NewAuditableEntity_HasNonEmptyId()
    {
        var entity = new TestAuditableEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void NewAuditableEntity_UpdatedAtIsNull()
    {
        var entity = new TestAuditableEntity();

        Assert.Null(entity.UpdatedAt);
    }

    [Fact]
    public void NewAuditableEntity_AuditUserFieldsAreNull()
    {
        var entity = new TestAuditableEntity();

        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.UpdatedBy);
    }
}
