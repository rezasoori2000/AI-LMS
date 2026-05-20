using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LMS.Application.Student.Dtos;
using LMS.Domain.Curriculum;
using LMS.Domain.Enrollments;
using LMS.Domain.Students;
using LMS.Domain.Users;
using LMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using System.Text.Json.Serialization;

namespace LMS.Api.Tests;

/// <summary>
/// Integration tests for the student portal API (Section 7 + 9).
///
/// Auth strategy:
///   Tests register users via POST /api/auth/register, log in, and attach the
///   resulting JWT as a Bearer token for subsequent requests.
///
/// Ownership gate:
///   Student portal endpoints require an active Enrollment linking the student
///   profile to the requested subject/chapter/lesson. Accessing unenrolled
///   content returns 403 Forbidden via StudentAccessDeniedException.
///
/// State strategy:
///   Each test uses unique e-mail addresses to avoid cross-test conflicts.
///   Happy-path tests that need an enrolled student seed data directly into the
///   InMemory database via the factory's service scope.
/// </summary>
public sealed class StudentIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public StudentIntegrationTests(LmsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

    private async Task<(HttpClient Client, Guid UserId)> CreateAuthorizedClientAsync(
        string email, UserRole role)
    {
        var client = CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email     = email,
            Password  = "Test@1234!",
            FirstName = "Test",
            LastName  = "Student",
            Role      = (int)role,
        });

        if (register.StatusCode != HttpStatusCode.Created
         && register.StatusCode != HttpStatusCode.Conflict)
            throw new Exception($"Register failed: {register.StatusCode}");

        var regBody = await register.Content.ReadFromJsonAsync<JsonElement>();
        var userId  = Guid.Parse(regBody.GetProperty("userId").GetString()!);

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email    = email,
            Password = "Test@1234!",
        });

        login.EnsureSuccessStatusCode();

        var payload = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token   = payload.GetProperty("accessToken").GetString()!;

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return (client, userId);
    }

    /// <summary>
    /// Seeds a StudentProfile for the given userId and returns the profile Id.
    /// </summary>
    private async Task<Guid> SeedStudentProfileAsync(Guid userId)
    {
        using var scope   = _factory.Services.CreateScope();
        var       db      = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
        var       profile = StudentProfile.Create(userId);
        db.StudentProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile.Id;
    }

    /// <summary>
    /// Seeds the minimum content tree (Grade → Subject → Chapter → Lesson)
    /// and enrols the given student profile. Returns the IDs for test assertions.
    /// </summary>
    private async Task<(Guid SubjectId, Guid ChapterId, Guid LessonId, Guid EnrollmentId)>
        SeedEnrolledContentAsync(Guid studentProfileId)
    {
        using var scope = _factory.Services.CreateScope();
        var       db    = scope.ServiceProvider.GetRequiredService<LmsDbContext>();

        // Use a unique slug per call to avoid conflicts when multiple tests seed content.
        var slug = $"test-subject-{Guid.NewGuid():N}";

        var grade   = Grade.Create("Test Grade", 1);
        var subject = Subject.Create("Test Subject", slug);
        var chapter = Chapter.Create(subject.Id, grade.Id, "Test Chapter", 1);
        var lesson  = Lesson.Create(chapter.Id, "Test Lesson", 1, "Content here");

        db.Grades.Add(grade);
        db.Subjects.Add(subject);
        db.Chapters.Add(chapter);
        db.Lessons.Add(lesson);

        var enrollment = Enrollment.Create(studentProfileId, subject.Id);
        db.Enrollments.Add(enrollment);

        await db.SaveChangesAsync();

        return (subject.Id, chapter.Id, lesson.Id, enrollment.Id);
    }

    // ── GET /api/student/summary ──────────────────────────────────────────────

    [Fact]
    public async Task GetSummary_FreshStudent_Returns200WithZeroCounts()
    {
        var (client, userId) = await CreateAuthorizedClientAsync(
            "student-summary-empty@example.com", UserRole.Student);

        await SeedStudentProfileAsync(userId);

        var response = await client.GetAsync("/api/student/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<StudentSummaryDto>(JsonOpts);
        Assert.NotNull(body);
        Assert.Equal(0, body.ActiveEnrollments);
        Assert.Equal(0, body.CompletedLessons);
        Assert.Equal(0, body.InProgressLessons);
        Assert.Equal(0m, body.OverallProgressPercent);
    }

    [Fact]
    public async Task GetSummary_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/student/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_WithNonStudentRole_Returns403()
    {
        var (client, _) = await CreateAuthorizedClientAsync(
            "parent-not-student-summary@example.com", UserRole.Parent);

        var response = await client.GetAsync("/api/student/summary");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── GET /api/student/enrollments ─────────────────────────────────────────

    [Fact]
    public async Task GetEnrollments_FreshStudent_Returns200EmptyArray()
    {
        var (client, userId) = await CreateAuthorizedClientAsync(
            "student-enrollments-empty@example.com", UserRole.Student);

        await SeedStudentProfileAsync(userId);

        var response = await client.GetAsync("/api/student/enrollments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<List<EnrolledSubjectDto>>(JsonOpts);
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    [Fact]
    public async Task GetEnrollments_EnrolledStudent_Returns200WithOneSubject()
    {
        var (client, userId) = await CreateAuthorizedClientAsync(
            "student-enrollments-one@example.com", UserRole.Student);

        var profileId = await SeedStudentProfileAsync(userId);
        var (subjectId, _, _, _) = await SeedEnrolledContentAsync(profileId);

        var response = await client.GetAsync("/api/student/enrollments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<List<EnrolledSubjectDto>>(JsonOpts);
        Assert.NotNull(body);
        Assert.Single(body);
        Assert.Equal(subjectId, body[0].SubjectId);
    }

    [Fact]
    public async Task GetEnrollments_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/student/enrollments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── GET /api/student/subjects/{subjectId}/chapters ────────────────────────

    [Fact]
    public async Task GetSubjectChapters_NotEnrolled_Returns403()
    {
        var (client, _) = await CreateAuthorizedClientAsync(
            "student-chapters-denied@example.com", UserRole.Student);

        // Random Guid guaranteed not enrolled
        var response = await client.GetAsync($"/api/student/subjects/{Guid.NewGuid()}/chapters");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSubjectChapters_EnrolledStudent_Returns200WithChapters()
    {
        var (client, userId) = await CreateAuthorizedClientAsync(
            "student-chapters-ok@example.com", UserRole.Student);

        var profileId = await SeedStudentProfileAsync(userId);
        var (subjectId, chapterId, lessonId, _) = await SeedEnrolledContentAsync(profileId);

        var response = await client.GetAsync($"/api/student/subjects/{subjectId}/chapters");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<SubjectChaptersDto>(JsonOpts);
        Assert.NotNull(body);
        Assert.Equal(subjectId, body.SubjectId);
        Assert.Single(body.Chapters);
        Assert.Equal(chapterId, body.Chapters[0].ChapterId);
        Assert.Single(body.Chapters[0].Lessons);
        Assert.Equal(lessonId, body.Chapters[0].Lessons[0].LessonId);
    }

    [Fact]
    public async Task GetSubjectChapters_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"/api/student/subjects/{Guid.NewGuid()}/chapters");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── GET /api/student/lessons/{lessonId} ───────────────────────────────────

    [Fact]
    public async Task GetLessonDetail_NotEnrolled_Returns403()
    {
        var (client, _) = await CreateAuthorizedClientAsync(
            "student-lesson-denied@example.com", UserRole.Student);

        var response = await client.GetAsync($"/api/student/lessons/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetLessonDetail_EnrolledStudent_Returns200WithLesson()
    {
        var (client, userId) = await CreateAuthorizedClientAsync(
            "student-lesson-ok@example.com", UserRole.Student);

        var profileId = await SeedStudentProfileAsync(userId);
        var (subjectId, chapterId, lessonId, _) = await SeedEnrolledContentAsync(profileId);

        var response = await client.GetAsync($"/api/student/lessons/{lessonId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<LessonDetailDto>(JsonOpts);
        Assert.NotNull(body);
        Assert.Equal(lessonId, body.LessonId);
        Assert.Equal(subjectId, body.SubjectId);
        Assert.Equal(chapterId, body.ChapterId);
        Assert.Empty(body.Questions);
    }

    [Fact]
    public async Task GetLessonDetail_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"/api/student/lessons/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── POST /api/student/lessons/{lessonId}/start ────────────────────────────

    [Fact]
    public async Task StartLesson_NotEnrolled_Returns403()
    {
        var (client, _) = await CreateAuthorizedClientAsync(
            "student-start-denied@example.com", UserRole.Student);

        var response = await client.PostAsync(
            $"/api/student/lessons/{Guid.NewGuid()}/start", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task StartLesson_EnrolledStudent_Returns200()
    {
        var (client, userId) = await CreateAuthorizedClientAsync(
            "student-start-ok@example.com", UserRole.Student);

        var profileId = await SeedStudentProfileAsync(userId);
        var (_, _, lessonId, _) = await SeedEnrolledContentAsync(profileId);

        var response = await client.PostAsync(
            $"/api/student/lessons/{lessonId}/start", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StartLesson_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.PostAsync(
            $"/api/student/lessons/{Guid.NewGuid()}/start", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── POST /api/student/lessons/{lessonId}/complete ─────────────────────────

    [Fact]
    public async Task CompleteLesson_NotEnrolled_Returns403()
    {
        var (client, _) = await CreateAuthorizedClientAsync(
            "student-complete-denied@example.com", UserRole.Student);

        var response = await client.PostAsJsonAsync(
            $"/api/student/lessons/{Guid.NewGuid()}/complete",
            new { Answers = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CompleteLesson_NoQuestions_Returns200WithNullScore()
    {
        var (client, userId) = await CreateAuthorizedClientAsync(
            "student-complete-ok@example.com", UserRole.Student);

        var profileId = await SeedStudentProfileAsync(userId);
        var (_, _, lessonId, _) = await SeedEnrolledContentAsync(profileId);

        // Start the lesson first so the progress record exists
        await client.PostAsync($"/api/student/lessons/{lessonId}/start", null);

        var response = await client.PostAsJsonAsync(
            $"/api/student/lessons/{lessonId}/complete",
            new { Answers = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<CompleteResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.Null(body.ScorePercent);        // no gradable questions
        Assert.Equal(0, body.GradableQuestions);
        Assert.Empty(body.Results);
    }

    [Fact]
    public async Task CompleteLesson_AlreadyCompleted_Returns409()
    {
        var (client, userId) = await CreateAuthorizedClientAsync(
            "student-complete-twice@example.com", UserRole.Student);

        var profileId = await SeedStudentProfileAsync(userId);
        var (_, _, lessonId, _) = await SeedEnrolledContentAsync(profileId);

        // Start then complete once
        await client.PostAsync($"/api/student/lessons/{lessonId}/start", null);
        await client.PostAsJsonAsync(
            $"/api/student/lessons/{lessonId}/complete",
            new { Answers = Array.Empty<object>() });

        // Second complete attempt should be 409
        var response = await client.PostAsJsonAsync(
            $"/api/student/lessons/{lessonId}/complete",
            new { Answers = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CompleteLesson_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/student/lessons/{Guid.NewGuid()}/complete",
            new { Answers = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
