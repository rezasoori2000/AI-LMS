using LMS.Domain.Curriculum;

namespace LMS.Domain.Tests.Curriculum;

public class CurriculumEntityTests
{
    // ── Grade ────────────────────────────────────────────────────────────────

    [Fact]
    public void Grade_Create_SetsNameAndLevel()
    {
        var grade = Grade.Create("Grade 5", 5);

        Assert.Equal("Grade 5", grade.Name);
        Assert.Equal(5, grade.Level);
    }

    [Fact]
    public void Grade_Create_TrimsWhitespaceName()
    {
        var grade = Grade.Create("  Grade 1  ", 1);

        Assert.Equal("Grade 1", grade.Name);
    }

    [Fact]
    public void Grade_Create_NullTenantId_WhenNotProvided()
    {
        var grade = Grade.Create("Grade 3", 3);

        Assert.Null(grade.TenantId);
    }

    [Fact]
    public void Grade_Create_ThrowsOnEmptyName()
    {
        Assert.Throws<ArgumentException>(() => Grade.Create("  ", 1));
    }

    [Fact]
    public void Grade_Create_ThrowsOnZeroLevel()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Grade.Create("Grade 0", 0));
    }

    [Fact]
    public void Grade_Create_AssignsNewId()
    {
        var grade = Grade.Create("Grade 2", 2);

        Assert.NotEqual(Guid.Empty, grade.Id);
    }

    // ── Subject ──────────────────────────────────────────────────────────────

    [Fact]
    public void Subject_Create_SetsNameAndSlug()
    {
        var subject = Subject.Create("Mathematics", "mathematics");

        Assert.Equal("Mathematics", subject.Name);
        Assert.Equal("mathematics", subject.Slug);
    }

    [Fact]
    public void Subject_Create_LowercasesSlug()
    {
        var subject = Subject.Create("English", "ENGLISH");

        Assert.Equal("english", subject.Slug);
    }

    [Fact]
    public void Subject_Create_ThrowsOnEmptyName()
    {
        Assert.Throws<ArgumentException>(() => Subject.Create("", "math"));
    }

    [Fact]
    public void Subject_Create_ThrowsOnEmptySlug()
    {
        Assert.Throws<ArgumentException>(() => Subject.Create("Math", "   "));
    }

    // ── Chapter ──────────────────────────────────────────────────────────────

    [Fact]
    public void Chapter_Create_SetsAllProperties()
    {
        var subjectId = Guid.NewGuid();
        var gradeId   = Guid.NewGuid();

        var chapter = Chapter.Create(subjectId, gradeId, "Fractions", 1, "Intro to fractions");

        Assert.Equal(subjectId, chapter.SubjectId);
        Assert.Equal(gradeId,   chapter.GradeId);
        Assert.Equal("Fractions", chapter.Title);
        Assert.Equal(1, chapter.Order);
        Assert.Equal("Intro to fractions", chapter.Description);
    }

    [Fact]
    public void Chapter_Create_ThrowsOnEmptySubjectId()
    {
        Assert.Throws<ArgumentException>(() =>
            Chapter.Create(Guid.Empty, Guid.NewGuid(), "Title", 1));
    }

    [Fact]
    public void Chapter_Create_ThrowsOnEmptyGradeId()
    {
        Assert.Throws<ArgumentException>(() =>
            Chapter.Create(Guid.NewGuid(), Guid.Empty, "Title", 1));
    }

    [Fact]
    public void Chapter_Create_ThrowsOnInvalidOrder()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Chapter.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", 0));
    }

    // ── Lesson ───────────────────────────────────────────────────────────────

    [Fact]
    public void Lesson_Create_SetsAllProperties()
    {
        var chapterId = Guid.NewGuid();

        var lesson = Lesson.Create(chapterId, "What is a Fraction?", 1, "## Fractions\n...", 15);

        Assert.Equal(chapterId, lesson.ChapterId);
        Assert.Equal("What is a Fraction?", lesson.Title);
        Assert.Equal(1, lesson.Order);
        Assert.Equal("## Fractions\n...", lesson.Content);
        Assert.Equal(15, lesson.EstimatedMinutes);
    }

    [Fact]
    public void Lesson_Create_ThrowsOnEmptyChapterId()
    {
        Assert.Throws<ArgumentException>(() => Lesson.Create(Guid.Empty, "Title", 1));
    }

    [Fact]
    public void Lesson_Create_ThrowsOnInvalidOrder()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Lesson.Create(Guid.NewGuid(), "Title", 0));
    }

    [Fact]
    public void Lesson_UpdateContent_ChangesContent()
    {
        var lesson = Lesson.Create(Guid.NewGuid(), "Lesson", 1, "original");

        lesson.UpdateContent("updated");

        Assert.Equal("updated", lesson.Content);
        Assert.NotNull(lesson.UpdatedAt);
    }
}
