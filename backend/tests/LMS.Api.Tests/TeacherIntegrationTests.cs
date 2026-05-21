using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LMS.Application.Teacher.Dtos;
using LMS.Domain.Students;
using LMS.Domain.Users;
using LMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LMS.Api.Tests;

/// <summary>
/// Integration tests for the teacher portal API (Section 8, Part 2).
///
/// Auth strategy:
///   Tests register users via POST /api/auth/register, log in, and attach the
///   resulting JWT as a Bearer token for subsequent requests.
///
/// Ownership gate:
///   Teacher endpoints require a row in <c>TeacherStudentAssignment</c> linking the
///   calling teacher to the student. Accessing an unassigned student returns 403 Forbidden.
///
/// State isolation:
///   Each test uses unique e-mail addresses to avoid cross-test conflicts.
///   The InMemory database is shared within the factory but not reset between tests.
/// </summary>
public sealed class TeacherIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public TeacherIntegrationTests(LmsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

    private async Task<HttpClient> CreateAuthorizedClientAsync(string email, UserRole role)
    {
        var client = CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email     = email,
            Password  = "Test@1234!",
            FirstName = "Test",
            LastName  = "Teacher",
            Role      = (int)role,
        });

        if (register.StatusCode != HttpStatusCode.Created
         && register.StatusCode != HttpStatusCode.Conflict)
            throw new Exception($"Register failed: {register.StatusCode}");

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

        return client;
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateAuthorizedClientWithIdAsync(
        string email, UserRole role)
    {
        var client = CreateClient();

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

    private async Task<Guid> SeedStudentProfileAsync(Guid userId)
    {
        using var scope   = _factory.Services.CreateScope();
        var       db      = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
        var       profile = StudentProfile.Create(userId);
        db.StudentProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile.Id;
    }

    private async Task SeedTeacherAssignmentAsync(Guid teacherUserId, Guid studentProfileId)
    {
        using var scope      = _factory.Services.CreateScope();
        var       db         = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
        var       assignment = TeacherStudentAssignment.Create(
                                   teacherUserId,
                                   studentProfileId,
                                   assignedByUserId: Guid.NewGuid());
        db.TeacherStudentAssignments.Add(assignment);
        await db.SaveChangesAsync();
    }

    // ── GET /api/teacher/summary ──────────────────────────────────────────────

    [Fact]
    public async Task GetSummary_WithNoAssignedStudents_Returns200WithZeroCounts()
    {
        var client = await CreateAuthorizedClientAsync(
            "teacher-summary-empty@example.com", UserRole.Teacher);

        var response = await client.GetAsync("/api/teacher/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TeacherSummaryDto>(JsonOpts);
        Assert.NotNull(body);
        Assert.Equal(0, body.TotalAssignedStudents);
        Assert.Equal(0, body.ActiveEnrollments);
        Assert.Equal(0, body.LessonsCompletedThisWeek);
    }

    [Fact]
    public async Task GetSummary_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/teacher/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_WithNonTeacherRole_Returns403()
    {
        var client = await CreateAuthorizedClientAsync(
            "parent-not-teacher-summary@example.com", UserRole.Parent);

        var response = await client.GetAsync("/api/teacher/summary");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── GET /api/teacher/students ─────────────────────────────────────────────

    [Fact]
    public async Task GetMyStudents_WithNoAssignedStudents_Returns200EmptyArray()
    {
        var client = await CreateAuthorizedClientAsync(
            "teacher-no-students@example.com", UserRole.Teacher);

        var response = await client.GetAsync("/api/teacher/students");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<AssignedStudentSummaryDto>>(JsonOpts);
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    [Fact]
    public async Task GetMyStudents_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/teacher/students");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyStudents_WithNonTeacherRole_Returns403()
    {
        var client = await CreateAuthorizedClientAsync(
            "student-not-teacher-list@example.com", UserRole.Student);

        var response = await client.GetAsync("/api/teacher/students");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── GET /api/teacher/students/{studentId} ─────────────────────────────────

    [Fact]
    public async Task GetStudentDetail_UnassignedStudent_Returns403()
    {
        var client = await CreateAuthorizedClientAsync(
            "teacher-unassigned-detail@example.com", UserRole.Teacher);

        // Random Guid is guaranteed not to be assigned to this teacher.
        var response = await client.GetAsync($"/api/teacher/students/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentDetail_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"/api/teacher/students/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentDetail_WithNonTeacherRole_Returns403()
    {
        var client = await CreateAuthorizedClientAsync(
            "content-editor-not-teacher-detail@example.com", UserRole.ContentEditor);

        var response = await client.GetAsync($"/api/teacher/students/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── GET /api/teacher/students/{studentId}/progress ────────────────────────

    [Fact]
    public async Task GetStudentProgress_UnassignedStudent_Returns403()
    {
        var client = await CreateAuthorizedClientAsync(
            "teacher-unassigned-progress@example.com", UserRole.Teacher);

        var response = await client.GetAsync($"/api/teacher/students/{Guid.NewGuid()}/progress");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentProgress_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"/api/teacher/students/{Guid.NewGuid()}/progress");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentProgress_WithNonTeacherRole_Returns403()
    {
        var client = await CreateAuthorizedClientAsync(
            "parent-not-teacher-progress@example.com", UserRole.Parent);

        var response = await client.GetAsync($"/api/teacher/students/{Guid.NewGuid()}/progress");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Teacher profile creation on registration ──────────────────────────────

    [Fact]
    public async Task Register_AsTeacher_SummaryEndpointReturns200NotError()
    {
        // Verifies that registration succeeds and the teacher can call the summary
        // endpoint without a 500 — confirming the service resolves correctly.
        var client = await CreateAuthorizedClientAsync(
            "teacher-profile-check@example.com", UserRole.Teacher);

        var response = await client.GetAsync("/api/teacher/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Happy-path and cross-isolation ────────────────────────────────────────

    [Fact]
    public async Task GetMyStudents_WithAssignedStudent_Returns200WithStudent()
    {
        var (teacherClient, teacherUserId) = await CreateAuthorizedClientWithIdAsync(
            "teacher-happy-list@example.com", UserRole.Teacher);
        var (_, studentUserId) = await CreateAuthorizedClientWithIdAsync(
            "student-for-teacher-list@example.com", UserRole.Student);

        var studentProfileId = await SeedStudentProfileAsync(studentUserId);
        await SeedTeacherAssignmentAsync(teacherUserId, studentProfileId);

        var response = await teacherClient.GetAsync("/api/teacher/students");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<List<AssignedStudentSummaryDto>>(JsonOpts);
        Assert.NotNull(body);
        Assert.Contains(body, s => s.StudentId == studentProfileId);
    }

    [Fact]
    public async Task GetStudentDetail_AssignedStudent_Returns200()
    {
        var (teacherClient, teacherUserId) = await CreateAuthorizedClientWithIdAsync(
            "teacher-happy-detail@example.com", UserRole.Teacher);
        var (_, studentUserId) = await CreateAuthorizedClientWithIdAsync(
            "student-for-teacher-detail@example.com", UserRole.Student);

        var studentProfileId = await SeedStudentProfileAsync(studentUserId);
        await SeedTeacherAssignmentAsync(teacherUserId, studentProfileId);

        var response = await teacherClient.GetAsync(
            $"/api/teacher/students/{studentProfileId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentDetail_OtherTeachersStudent_Returns403()
    {
        // Teacher A's student must not be visible to Teacher B.
        var (_, teacherAUserId) = await CreateAuthorizedClientWithIdAsync(
            "teacher-a-isolation@example.com", UserRole.Teacher);
        var (teacherBClient, _) = await CreateAuthorizedClientWithIdAsync(
            "teacher-b-isolation@example.com", UserRole.Teacher);
        var (_, studentUserId) = await CreateAuthorizedClientWithIdAsync(
            "student-teacher-isolation@example.com", UserRole.Student);

        var studentProfileId = await SeedStudentProfileAsync(studentUserId);
        await SeedTeacherAssignmentAsync(teacherAUserId, studentProfileId);

        var response = await teacherBClient.GetAsync(
            $"/api/teacher/students/{studentProfileId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentProgress_AssignedStudent_Returns200()
    {
        var (teacherClient, teacherUserId) = await CreateAuthorizedClientWithIdAsync(
            "teacher-happy-progress@example.com", UserRole.Teacher);
        var (_, studentUserId) = await CreateAuthorizedClientWithIdAsync(
            "student-for-teacher-progress@example.com", UserRole.Student);

        var studentProfileId = await SeedStudentProfileAsync(studentUserId);
        await SeedTeacherAssignmentAsync(teacherUserId, studentProfileId);

        var response = await teacherClient.GetAsync(
            $"/api/teacher/students/{studentProfileId}/progress");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
