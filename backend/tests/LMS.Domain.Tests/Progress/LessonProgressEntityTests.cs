using LMS.Domain.Progress;

namespace LMS.Domain.Tests.Progress;

public class LessonProgressEntityTests
{
    private static LessonProgress MakeProgress() =>
        LessonProgress.Create(Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Create_InitialStatusIsNotStarted()
    {
        var progress = MakeProgress();

        Assert.Equal(ProgressStatus.NotStarted, progress.Status);
    }

    [Fact]
    public void Create_ThrowsOnEmptyStudentId()
    {
        Assert.Throws<ArgumentException>(() =>
            LessonProgress.Create(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Create_ThrowsOnEmptyLessonId()
    {
        Assert.Throws<ArgumentException>(() =>
            LessonProgress.Create(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void Start_TransitionsToInProgress()
    {
        var progress = MakeProgress();

        progress.Start();

        Assert.Equal(ProgressStatus.InProgress, progress.Status);
    }

    [Fact]
    public void Start_SetsStartedAt()
    {
        var progress = MakeProgress();

        progress.Start();

        Assert.NotNull(progress.StartedAt);
    }

    [Fact]
    public void Start_WhenAlreadyInProgress_IsNoOp()
    {
        var progress = MakeProgress();
        progress.Start();
        var firstStartedAt = progress.StartedAt;

        // Should not throw and should not reset StartedAt.
        progress.Start();

        Assert.Equal(firstStartedAt, progress.StartedAt);
        Assert.Equal(ProgressStatus.InProgress, progress.Status);
    }

    [Fact]
    public void Complete_TransitionsToCompleted()
    {
        var progress = MakeProgress();
        progress.Start();

        progress.Complete();

        Assert.Equal(ProgressStatus.Completed, progress.Status);
    }

    [Fact]
    public void Complete_SetsCompletedAt()
    {
        var progress = MakeProgress();

        progress.Complete();

        Assert.NotNull(progress.CompletedAt);
    }

    [Fact]
    public void Complete_FromNotStarted_SetsStartedAt()
    {
        var progress = MakeProgress();

        // Complete() without Start() should defensively set StartedAt.
        progress.Complete();

        Assert.NotNull(progress.StartedAt);
    }

    [Fact]
    public void Complete_SetsScorePercent()
    {
        var progress = MakeProgress();

        progress.Complete(87.5m);

        Assert.Equal(87.5m, progress.ScorePercent);
    }

    [Fact]
    public void Complete_NullScore_IsAllowed()
    {
        var progress = MakeProgress();

        progress.Complete(null);

        Assert.Null(progress.ScorePercent);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_Throws()
    {
        var progress = MakeProgress();
        progress.Complete();

        Assert.Throws<InvalidOperationException>(() => progress.Complete());
    }

    [Fact]
    public void Complete_WithScoreAbove100_Throws()
    {
        var progress = MakeProgress();

        Assert.Throws<ArgumentOutOfRangeException>(() => progress.Complete(101m));
    }

    [Fact]
    public void Complete_WithNegativeScore_Throws()
    {
        var progress = MakeProgress();

        Assert.Throws<ArgumentOutOfRangeException>(() => progress.Complete(-1m));
    }
}
