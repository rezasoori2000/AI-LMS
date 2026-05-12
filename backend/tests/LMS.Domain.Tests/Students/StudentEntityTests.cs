using LMS.Domain.Students;

namespace LMS.Domain.Tests.Students;

public class StudentEntityTests
{
    // ── StudentProfile ───────────────────────────────────────────────────────

    [Fact]
    public void StudentProfile_Create_SetsUserId()
    {
        var userId = Guid.NewGuid();
        var profile = StudentProfile.Create(userId);

        Assert.Equal(userId, profile.UserId);
    }

    [Fact]
    public void StudentProfile_Create_NullableFieldsAreNullByDefault()
    {
        var profile = StudentProfile.Create(Guid.NewGuid());

        Assert.Null(profile.GradeId);
        Assert.Null(profile.ParentId);
        Assert.Null(profile.DateOfBirth);
        Assert.Null(profile.TenantId);
    }

    [Fact]
    public void StudentProfile_Create_ThrowsOnEmptyUserId()
    {
        Assert.Throws<ArgumentException>(() => StudentProfile.Create(Guid.Empty));
    }

    [Fact]
    public void StudentProfile_AssignGrade_SetsGradeId()
    {
        var profile = StudentProfile.Create(Guid.NewGuid());
        var gradeId = Guid.NewGuid();

        profile.AssignGrade(gradeId);

        Assert.Equal(gradeId, profile.GradeId);
        Assert.NotNull(profile.UpdatedAt);
    }

    [Fact]
    public void StudentProfile_AssignGrade_ThrowsOnEmptyGradeId()
    {
        var profile = StudentProfile.Create(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => profile.AssignGrade(Guid.Empty));
    }

    [Fact]
    public void StudentProfile_LinkParent_SetsParentId()
    {
        var profile  = StudentProfile.Create(Guid.NewGuid());
        var parentId = Guid.NewGuid();

        profile.LinkParent(parentId);

        Assert.Equal(parentId, profile.ParentId);
        Assert.NotNull(profile.UpdatedAt);
    }

    [Fact]
    public void StudentProfile_LinkParent_ThrowsOnEmptyParentId()
    {
        var profile = StudentProfile.Create(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => profile.LinkParent(Guid.Empty));
    }

    // ── ParentProfile ────────────────────────────────────────────────────────

    [Fact]
    public void ParentProfile_Create_SetsUserId()
    {
        var userId  = Guid.NewGuid();
        var profile = ParentProfile.Create(userId);

        Assert.Equal(userId, profile.UserId);
    }

    [Fact]
    public void ParentProfile_Create_ThrowsOnEmptyUserId()
    {
        Assert.Throws<ArgumentException>(() => ParentProfile.Create(Guid.Empty));
    }

    [Fact]
    public void ParentProfile_Create_AssignsNewId()
    {
        var profile = ParentProfile.Create(Guid.NewGuid());

        Assert.NotEqual(Guid.Empty, profile.Id);
    }
}
