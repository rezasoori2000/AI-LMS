using LMS.Domain.Enrollments;

namespace LMS.Domain.Tests.Enrollments;

public class EnrollmentEntityTests
{
    private static Enrollment MakeEnrollment() =>
        Enrollment.Create(Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Create_SetsStudentAndSubjectIds()
    {
        var studentId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();

        var enrollment = Enrollment.Create(studentId, subjectId);

        Assert.Equal(studentId, enrollment.StudentId);
        Assert.Equal(subjectId, enrollment.SubjectId);
    }

    [Fact]
    public void Create_InitialStatusIsActive()
    {
        var enrollment = MakeEnrollment();

        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
    }

    [Fact]
    public void Create_ThrowsOnEmptyStudentId()
    {
        Assert.Throws<ArgumentException>(() => Enrollment.Create(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Create_ThrowsOnEmptySubjectId()
    {
        Assert.Throws<ArgumentException>(() => Enrollment.Create(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void Complete_TransitionsToCompleted()
    {
        var enrollment = MakeEnrollment();

        enrollment.Complete();

        Assert.Equal(EnrollmentStatus.Completed, enrollment.Status);
    }

    [Fact]
    public void Complete_SetsCompletedAt()
    {
        var enrollment = MakeEnrollment();

        enrollment.Complete();

        Assert.NotNull(enrollment.CompletedAt);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_Throws()
    {
        var enrollment = MakeEnrollment();
        enrollment.Complete();

        Assert.Throws<InvalidOperationException>(() => enrollment.Complete());
    }

    [Fact]
    public void Drop_TransitionsToDropped()
    {
        var enrollment = MakeEnrollment();

        enrollment.Drop();

        Assert.Equal(EnrollmentStatus.Dropped, enrollment.Status);
    }

    [Fact]
    public void Drop_WhenAlreadyDropped_Throws()
    {
        var enrollment = MakeEnrollment();
        enrollment.Drop();

        Assert.Throws<InvalidOperationException>(() => enrollment.Drop());
    }

    [Fact]
    public void Drop_WhenCompleted_Throws()
    {
        var enrollment = MakeEnrollment();
        enrollment.Complete();

        Assert.Throws<InvalidOperationException>(() => enrollment.Drop());
    }

    [Fact]
    public void Complete_SetsUpdatedAt()
    {
        var enrollment = MakeEnrollment();

        enrollment.Complete();

        Assert.NotNull(enrollment.UpdatedAt);
    }
}
