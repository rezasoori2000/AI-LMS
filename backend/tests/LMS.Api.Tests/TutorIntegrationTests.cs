using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LMS.Domain.Curriculum;
using LMS.Domain.Enrollments;
using LMS.Domain.Students;
using LMS.Domain.Users;
using LMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LMS.Api.Tests;

/// <summary>
/// Integration tests for the AI Tutor API (Section 11, Part 2).
///
/// Auth strategy:
///   Tests register and log in via /api/auth, then attach the JWT as a Bearer token.
///
/// State strategy:
///   Each test uses unique e-mail addresses.  Content and enrollments are seeded
///   directly into the InMemory database through the factory's service scope.
///
/// Provider:
///   StubTutorProvider is active during tests (no real LLM call is made).
///   The reply text is deterministic and non-empty.
/// </summary>
public sealed class TutorIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public TutorIntegrationTests(LmsWebApplicationFactory factory) => _factory = factory;

    // ── Helpers ────────────────────────────────────────────────────────────────

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<(HttpClient Client, Guid UserId)> CreateAuthorizedClientAsync(
        string email, UserRole role = UserRole.Student)
    {
        var client = CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email     = email,
            Password  = "Test@1234!",
            FirstName = "Tutor",
            LastName  = "Test",
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

        var token = (await login.Content.ReadFromJsonAsync<JsonElement>())
                    .GetProperty("accessToken").GetString()!;

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return (client, userId);
    }

    /// <summary>
    /// Seeds a StudentProfile, Grade, Subject, Chapter, Lesson, and Enrollment.
    /// Returns (studentProfileId, lessonId).
    /// </summary>
    private async Task<(Guid ProfileId, Guid LessonId)> SeedEnrolledStudentAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var       db    = scope.ServiceProvider.GetRequiredService<LmsDbContext>();

        var profile = StudentProfile.Create(userId);
        var grade   = Grade.Create("Tutor Grade", 1);
        var subject = Subject.Create("Tutor Subject", $"tutor-subject-{Guid.NewGuid():N}");
        var chapter = Chapter.Create(subject.Id, grade.Id, "Tutor Chapter", 1);
        var lesson  = Lesson.Create(chapter.Id, "Intro to AI Tutoring", 1,
                          content: "AI tutors help students learn by answering questions.");

        db.StudentProfiles.Add(profile);
        db.Grades.Add(grade);
        db.Subjects.Add(subject);
        db.Chapters.Add(chapter);
        db.Lessons.Add(lesson);
        db.Enrollments.Add(Enrollment.Create(profile.Id, subject.Id));

        await db.SaveChangesAsync();
        return (profile.Id, lesson.Id);
    }

    /// <summary>
    /// Seeds only a StudentProfile (no enrollment), for negative-path tests.
    /// </summary>
    private async Task SeedStudentProfileOnlyAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var       db    = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
        db.StudentProfiles.Add(StudentProfile.Create(userId));
        await db.SaveChangesAsync();
    }

    // ── POST /api/student/tutor/sessions ──────────────────────────────────────

    [Fact]
    public async Task StartSession_WithEnrolledLesson_Returns201WithConversationId()
    {
        var (client, userId) = await CreateAuthorizedClientAsync("tutor-start-ok@example.com");
        var (_, lessonId)   = await SeedEnrolledStudentAsync(userId);

        var response = await client.PostAsJsonAsync(
            "/api/student/tutor/sessions",
            new { LessonId = lessonId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body           = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var conversationId = Guid.Parse(body.GetProperty("conversationId").GetString()!);
        Assert.NotEqual(Guid.Empty, conversationId);
        Assert.Equal(lessonId, Guid.Parse(body.GetProperty("lessonId").GetString()!));
    }

    [Fact]
    public async Task StartSession_WithoutToken_Returns401()
    {
        var client   = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/student/tutor/sessions",
            new { LessonId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StartSession_WithNonStudentRole_Returns403()
    {
        var (client, _) = await CreateAuthorizedClientAsync(
            "tutor-parent-role@example.com", UserRole.Parent);

        var response = await client.PostAsJsonAsync(
            "/api/student/tutor/sessions",
            new { LessonId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task StartSession_WithUnenrolledLesson_Returns403()
    {
        var (client, userId) = await CreateAuthorizedClientAsync("tutor-unenrolled@example.com");
        await SeedStudentProfileOnlyAsync(userId); // profile exists, but no enrollment

        var response = await client.PostAsJsonAsync(
            "/api/student/tutor/sessions",
            new { LessonId = Guid.NewGuid() }); // lesson does not exist → 403

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── POST /api/student/tutor/sessions/{id}/ask ─────────────────────────────

    [Fact]
    public async Task Ask_WithValidSession_Returns200WithReply()
    {
        var (client, userId) = await CreateAuthorizedClientAsync("tutor-ask-ok@example.com");
        var (_, lessonId)   = await SeedEnrolledStudentAsync(userId);

        // Start session
        var start = await client.PostAsJsonAsync(
            "/api/student/tutor/sessions",
            new { LessonId = lessonId });
        Assert.Equal(HttpStatusCode.Created, start.StatusCode);

        var conversationId = Guid.Parse(
            (await start.Content.ReadFromJsonAsync<JsonElement>(JsonOpts))
            .GetProperty("conversationId").GetString()!);

        // Ask
        var ask = await client.PostAsJsonAsync(
            $"/api/student/tutor/sessions/{conversationId}/ask",
            new { Message = "What is this lesson about?" });

        Assert.Equal(HttpStatusCode.OK, ask.StatusCode);

        var body = await ask.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(conversationId, Guid.Parse(body.GetProperty("conversationId").GetString()!));
        var reply = body.GetProperty("reply").GetString();
        Assert.NotEmpty(reply!);
    }

    [Fact]
    public async Task Ask_OnEndedSession_Returns409()
    {
        var (client, userId) = await CreateAuthorizedClientAsync("tutor-ask-ended@example.com");
        var (_, lessonId)   = await SeedEnrolledStudentAsync(userId);

        var start = await client.PostAsJsonAsync(
            "/api/student/tutor/sessions",
            new { LessonId = lessonId });
        var conversationId = Guid.Parse(
            (await start.Content.ReadFromJsonAsync<JsonElement>(JsonOpts))
            .GetProperty("conversationId").GetString()!);

        // End the session
        await client.PostAsync($"/api/student/tutor/sessions/{conversationId}/end", null);

        // Attempt to ask on the ended session
        var ask = await client.PostAsJsonAsync(
            $"/api/student/tutor/sessions/{conversationId}/ask",
            new { Message = "Can I still ask?" });

        Assert.Equal(HttpStatusCode.Conflict, ask.StatusCode);
    }

    [Fact]
    public async Task Ask_OnOtherStudentsSession_Returns403()
    {
        // Student A starts a session
        var (clientA, userAId) = await CreateAuthorizedClientAsync("tutor-owner-a@example.com");
        var (_, lessonId)     = await SeedEnrolledStudentAsync(userAId);

        var start = await clientA.PostAsJsonAsync(
            "/api/student/tutor/sessions",
            new { LessonId = lessonId });
        var conversationId = Guid.Parse(
            (await start.Content.ReadFromJsonAsync<JsonElement>(JsonOpts))
            .GetProperty("conversationId").GetString()!);

        // Student B gets their own profile but tries to hijack Student A's session
        var (clientB, userBId) = await CreateAuthorizedClientAsync("tutor-owner-b@example.com");
        await SeedStudentProfileOnlyAsync(userBId);

        var ask = await clientB.PostAsJsonAsync(
            $"/api/student/tutor/sessions/{conversationId}/ask",
            new { Message = "Can I access this?" });

        Assert.Equal(HttpStatusCode.Forbidden, ask.StatusCode);
    }

    // ── POST /api/student/tutor/sessions/{id}/end ─────────────────────────────

    [Fact]
    public async Task EndSession_Returns204AndIsIdempotent()
    {
        var (client, userId) = await CreateAuthorizedClientAsync("tutor-end-ok@example.com");
        var (_, lessonId)   = await SeedEnrolledStudentAsync(userId);

        var start = await client.PostAsJsonAsync(
            "/api/student/tutor/sessions",
            new { LessonId = lessonId });
        var conversationId = Guid.Parse(
            (await start.Content.ReadFromJsonAsync<JsonElement>(JsonOpts))
            .GetProperty("conversationId").GetString()!);

        // First end — should succeed
        var end1 = await client.PostAsync(
            $"/api/student/tutor/sessions/{conversationId}/end", null);
        Assert.Equal(HttpStatusCode.NoContent, end1.StatusCode);

        // Second end — AiConversation.End() is idempotent; should still return 204
        var end2 = await client.PostAsync(
            $"/api/student/tutor/sessions/{conversationId}/end", null);
        Assert.Equal(HttpStatusCode.NoContent, end2.StatusCode);
    }
}
