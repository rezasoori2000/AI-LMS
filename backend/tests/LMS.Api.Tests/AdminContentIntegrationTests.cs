using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LMS.Application.Auth.Dtos;
using LMS.Application.Content.Grades;
using LMS.Application.Content.Subjects;
using LMS.Application.Content.Chapters;
using LMS.Application.Content.Lessons;
using LMS.Application.Content.Questions;
using LMS.Domain.Catalog;
using LMS.Domain.Users;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LMS.Api.Tests;

/// <summary>
/// Integration tests for the admin content management endpoints (Section 5, Part 2).
///
/// Auth strategy:
///   Tests register a user with a content-management role via POST /api/auth/register,
///   log in to obtain a JWT, and attach it as a Bearer token on subsequent requests.
///
///   The backend register endpoint does not restrict which UserRole values are accepted
///   (the frontend restricts to Student/Parent only).  Tests therefore register as
///   ContentEditor (role=2) to exercise the authorized flow.
///
/// State isolation:
///   All tests use unique IDs / names to avoid cross-test state conflicts.
///   The shared InMemory database within LmsWebApplicationFactory is not reset
///   between tests; tests are responsible for not stepping on each other.
/// </summary>
public sealed class AdminContentIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    // A small JSON options bag used to deserialise admin DTOs.
    // JsonStringEnumConverter is required because the API serialises enums as strings.
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public AdminContentIntegrationTests(LmsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

    /// <summary>
    /// Registers + logs in a user and returns an HTTP client with the Bearer token pre-set.
    /// </summary>
    private async Task<HttpClient> CreateAuthorizedClientAsync(
        string email,
        UserRole role = UserRole.ContentEditor)
    {
        var client = CreateClient();

        // Register (backend accepts any role — frontend restricts to Student/Parent only)
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email     = email,
            Password  = "Test@1234!",
            FirstName = "Test",
            LastName  = "User",
            Role      = (int)role,
        });

        if (register.StatusCode != HttpStatusCode.Created
         && register.StatusCode != HttpStatusCode.Conflict)
            throw new Exception($"Register failed: {register.StatusCode}");

        // Login
        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email    = email,
            Password = "Test@1234!",
        });

        login.EnsureSuccessStatusCode();

        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts)
            ?? throw new Exception("Auth response was null");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        return client;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Authorization boundary tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AdminContent_WithoutToken_Returns401()
    {
        var client   = CreateClient();
        var response = await client.GetAsync("/api/admin/grades");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminContent_WithStudentToken_Returns403()
    {
        var client = await CreateAuthorizedClientAsync(
            "student.auth.test@example.com", UserRole.Student);

        var response = await client.GetAsync("/api/admin/grades");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Grades CRUD — full happy path
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Grades_Create_Returns201WithDto()
    {
        var client   = await CreateAuthorizedClientAsync("grades.create@example.com");
        var response = await client.PostAsJsonAsync("/api/admin/grades",
            new { Name = "Grade 10", Level = 10 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<GradeDto>(JsonOpts);
        Assert.NotNull(dto);
        Assert.Equal("Grade 10", dto.Name);
        Assert.Equal(10, dto.Level);
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.True(response.Headers.Location is not null, "Location header must be set");
    }

    [Fact]
    public async Task Grades_GetList_Returns200WithItems()
    {
        var client = await CreateAuthorizedClientAsync("grades.list@example.com");
        await client.PostAsJsonAsync("/api/admin/grades", new { Name = "Grade 11", Level = 11 });

        var response = await client.GetAsync("/api/admin/grades");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<List<GradeDto>>(JsonOpts);
        Assert.NotNull(list);
        Assert.Contains(list, g => g.Level == 11);
    }

    [Fact]
    public async Task Grades_GetById_Returns200()
    {
        var client  = await CreateAuthorizedClientAsync("grades.getbyid@example.com");
        var created = await (await client.PostAsJsonAsync("/api/admin/grades",
            new { Name = "Grade 12", Level = 12 }))
            .Content.ReadFromJsonAsync<GradeDto>(JsonOpts);

        var response = await client.GetAsync($"/api/admin/grades/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<GradeDto>(JsonOpts);
        Assert.Equal(created.Id, dto!.Id);
    }

    [Fact]
    public async Task Grades_Update_Returns200WithUpdatedDto()
    {
        var client  = await CreateAuthorizedClientAsync("grades.update@example.com");
        var created = await (await client.PostAsJsonAsync("/api/admin/grades",
            new { Name = "Grade 13 (old)", Level = 13 }))
            .Content.ReadFromJsonAsync<GradeDto>(JsonOpts);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/grades/{created!.Id}",
            new { Name = "Grade 13", Level = 13 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<GradeDto>(JsonOpts);
        Assert.Equal("Grade 13", updated!.Name);
    }

    [Fact]
    public async Task Grades_Delete_Returns204()
    {
        var client  = await CreateAuthorizedClientAsync("grades.delete@example.com");
        var created = await (await client.PostAsJsonAsync("/api/admin/grades",
            new { Name = "Grade 99", Level = 99 }))
            .Content.ReadFromJsonAsync<GradeDto>(JsonOpts);

        var deleteResponse = await client.DeleteAsync($"/api/admin/grades/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/admin/grades/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Grades_DuplicateLevel_Returns409()
    {
        var client = await CreateAuthorizedClientAsync("grades.dup@example.com");
        await client.PostAsJsonAsync("/api/admin/grades", new { Name = "Grade 50", Level = 50 });

        var response = await client.PostAsJsonAsync("/api/admin/grades",
            new { Name = "Grade 50 again", Level = 50 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Grades_GetById_Unknown_Returns404()
    {
        var client   = await CreateAuthorizedClientAsync("grades.notfound@example.com");
        var response = await client.GetAsync($"/api/admin/grades/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Subjects CRUD — happy path and slug conflict
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Subjects_Create_Returns201()
    {
        var client   = await CreateAuthorizedClientAsync("subjects.create@example.com");
        var response = await client.PostAsJsonAsync("/api/admin/subjects",
            new { Name = "Physics", Slug = "physics" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<SubjectDto>(JsonOpts);
        Assert.Equal("physics", dto!.Slug);
    }

    [Fact]
    public async Task Subjects_DuplicateSlug_Returns409()
    {
        var client = await CreateAuthorizedClientAsync("subjects.dup@example.com");
        await client.PostAsJsonAsync("/api/admin/subjects",
            new { Name = "Chemistry", Slug = "chemistry" });

        var response = await client.PostAsJsonAsync("/api/admin/subjects",
            new { Name = "Chemistry II", Slug = "chemistry" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Chapters -- requires Grade + Subject first
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Chapters_Create_Returns201()
    {
        var client = await CreateAuthorizedClientAsync("chapters.create@example.com");

        var grade = await (await client.PostAsJsonAsync("/api/admin/grades",
            new { Name = "Grade 4", Level = 4 }))
            .Content.ReadFromJsonAsync<GradeDto>(JsonOpts);

        var subject = await (await client.PostAsJsonAsync("/api/admin/subjects",
            new { Name = "Biology", Slug = "biology-ch" }))
            .Content.ReadFromJsonAsync<SubjectDto>(JsonOpts);

        var response = await client.PostAsJsonAsync("/api/admin/chapters", new
        {
            SubjectId   = subject!.Id,
            GradeId     = grade!.Id,
            Title       = "Chapter 1: Cells",
            Order       = 1,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<ChapterDto>(JsonOpts);
        Assert.Equal("Chapter 1: Cells", dto!.Title);
        Assert.Equal(grade.Id,   dto.GradeId);
        Assert.Equal(subject.Id, dto.SubjectId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Lessons -- requires Chapter
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Lessons_Create_Returns201()
    {
        var client = await CreateAuthorizedClientAsync("lessons.create@example.com");

        var grade = await (await client.PostAsJsonAsync("/api/admin/grades",
            new { Name = "Grade 3", Level = 3 }))
            .Content.ReadFromJsonAsync<GradeDto>(JsonOpts);

        var subject = await (await client.PostAsJsonAsync("/api/admin/subjects",
            new { Name = "History", Slug = "history-ls" }))
            .Content.ReadFromJsonAsync<SubjectDto>(JsonOpts);

        var chapter = await (await client.PostAsJsonAsync("/api/admin/chapters", new
        {
            SubjectId = subject!.Id,
            GradeId   = grade!.Id,
            Title     = "Ancient Rome",
            Order     = 1,
        })).Content.ReadFromJsonAsync<ChapterDto>(JsonOpts);

        var response = await client.PostAsJsonAsync("/api/admin/lessons", new
        {
            ChapterId        = chapter!.Id,
            Title            = "The Republic",
            Order            = 1,
            Content          = "# The Roman Republic\n\nContent here.",
            EstimatedMinutes = 15,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<LessonDto>(JsonOpts);
        Assert.Equal("The Republic", dto!.Title);
        Assert.Equal(15, dto.EstimatedMinutes);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Questions -- can be created without a LessonId (bank question)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Questions_Create_BankQuestion_Returns201()
    {
        var client   = await CreateAuthorizedClientAsync("questions.create@example.com");
        var response = await client.PostAsJsonAsync("/api/admin/questions", new
        {
            Text          = "What is 2 + 2?",
            Type          = (int)QuestionType.ShortAnswer,
            Difficulty    = (int)DifficultyLevel.Easy,
            CorrectAnswer = "4",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<QuestionDto>(JsonOpts);
        Assert.Equal("What is 2 + 2?", dto!.Text);
        Assert.Null(dto.LessonId);
    }

    [Fact]
    public async Task Questions_GetById_Unknown_Returns404()
    {
        var client   = await CreateAuthorizedClientAsync("questions.notfound@example.com");
        var response = await client.GetAsync($"/api/admin/questions/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
