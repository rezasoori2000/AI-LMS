using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LMS.Application.Parent.Dtos;
using LMS.Domain.Students;
using LMS.Domain.Users;
using LMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LMS.Api.Tests;

/// <summary>
/// Integration tests for the parent portal API (Section 6, Part 2).
///
/// Auth strategy:
///   Tests register users via POST /api/auth/register, log in, and attach the
///   resulting JWT as a Bearer token for subsequent requests.
///
/// State isolation:
///   Each test uses unique e-mail addresses to avoid cross-test conflicts.
///   The InMemory database is shared within the factory but not reset between tests.
/// </summary>
public sealed class ParentIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public ParentIntegrationTests(LmsWebApplicationFactory factory)
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
            LastName  = "Parent",
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

    /// <summary>
    /// Retrieves the ParentProfile.Id for the given user. AuthService creates the
    /// profile automatically during registration, so it always exists after a Parent user
    /// is registered.
    /// </summary>
    private Guid GetParentProfileId(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var       db    = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
        return db.ParentProfiles.First(p => p.UserId == userId).Id;
    }

    /// <summary>
    /// Registers a student user and seeds a StudentProfile linked to the given parent.
    /// Returns the StudentProfile.Id.
    /// </summary>
    private async Task<Guid> SeedStudentLinkedToParentAsync(
        Guid   parentProfileId,
        string studentEmail)
    {
        using var httpClient = CreateClient();
        var register = await httpClient.PostAsJsonAsync("/api/auth/register", new
        {
            Email     = studentEmail,
            Password  = "Test@1234!",
            FirstName = "Child",
            LastName  = "Linked",
            Role      = (int)UserRole.Student,
        });

        if (register.StatusCode != HttpStatusCode.Created
         && register.StatusCode != HttpStatusCode.Conflict)
            throw new Exception($"Student register failed: {register.StatusCode}");

        var regBody   = await register.Content.ReadFromJsonAsync<JsonElement>();
        var studentId = Guid.Parse(regBody.GetProperty("userId").GetString()!);

        using var scope   = _factory.Services.CreateScope();
        var       db      = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
        var       profile = StudentProfile.Create(studentId, parentId: parentProfileId);
        db.StudentProfiles.Add(profile);
        await db.SaveChangesAsync();

        return profile.Id;
    }

    // ── GET /api/parent/children ──────────────────────────────────────────────

    [Fact]
    public async Task GetChildren_WithNoLinkedStudents_Returns200EmptyArray()
    {
        var client = await CreateAuthorizedClientAsync(
            "parent-no-children@example.com", UserRole.Parent);

        var response = await client.GetAsync("/api/parent/children");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<ChildSummaryDto>>(JsonOpts);
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    [Fact]
    public async Task GetChildren_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/parent/children");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetChildren_WithNonParentRole_Returns403()
    {
        var client = await CreateAuthorizedClientAsync(
            "content-editor-not-parent@example.com", UserRole.ContentEditor);

        var response = await client.GetAsync("/api/parent/children");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── GET /api/parent/children/{studentId} ─────────────────────────────────

    [Fact]
    public async Task GetChildDetail_UnlinkedStudent_Returns403()
    {
        var client = await CreateAuthorizedClientAsync(
            "parent-unlinked@example.com", UserRole.Parent);

        // Use a random Guid — guaranteed not to be linked to this parent.
        var randomStudentId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/parent/children/{randomStudentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetChildDetail_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"/api/parent/children/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetChildDetail_WithNonParentRole_Returns403()
    {
        var client = await CreateAuthorizedClientAsync(
            "teacher-not-parent@example.com", UserRole.Teacher);

        var response = await client.GetAsync($"/api/parent/children/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── ParentProfile creation on registration ────────────────────────────────

    [Fact]
    public async Task Register_AsParent_ChildrenEndpointReturns200NotError()
    {
        // Verifies that AuthService created a ParentProfile during registration,
        // so the service can resolve the profile without error.
        var client = await CreateAuthorizedClientAsync(
            "parent-profile-check@example.com", UserRole.Parent);

        var response = await client.GetAsync("/api/parent/children");

        // 200 (not 500) confirms the ParentProfile was created and resolved.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Happy-path and cross-isolation ────────────────────────────────────────

    [Fact]
    public async Task GetChildren_LinkedChild_Returns200WithChild()
    {
        var (parentClient, parentUserId) = await CreateAuthorizedClientWithIdAsync(
            "parent-happy-list@example.com", UserRole.Parent);

        var parentProfileId  = GetParentProfileId(parentUserId);
        var studentProfileId = await SeedStudentLinkedToParentAsync(
            parentProfileId, "child-for-parent-list@example.com");

        var response = await parentClient.GetAsync("/api/parent/children");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<List<ChildSummaryDto>>(JsonOpts);
        Assert.NotNull(body);
        Assert.Contains(body, c => c.StudentId == studentProfileId);
    }

    [Fact]
    public async Task GetChildDetail_LinkedChild_Returns200()
    {
        var (parentClient, parentUserId) = await CreateAuthorizedClientWithIdAsync(
            "parent-happy-detail@example.com", UserRole.Parent);

        var parentProfileId  = GetParentProfileId(parentUserId);
        var studentProfileId = await SeedStudentLinkedToParentAsync(
            parentProfileId, "child-for-parent-detail@example.com");

        var response = await parentClient.GetAsync(
            $"/api/parent/children/{studentProfileId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetChildDetail_AnotherParentsChild_Returns403()
    {
        // Parent A's child must not be accessible by Parent B.
        var (_, parentAUserId) = await CreateAuthorizedClientWithIdAsync(
            "parent-a-isolation@example.com", UserRole.Parent);
        var (parentBClient, _) = await CreateAuthorizedClientWithIdAsync(
            "parent-b-isolation@example.com", UserRole.Parent);

        var parentAProfileId = GetParentProfileId(parentAUserId);
        var studentProfileId = await SeedStudentLinkedToParentAsync(
            parentAProfileId, "child-for-parent-isolation@example.com");

        var response = await parentBClient.GetAsync(
            $"/api/parent/children/{studentProfileId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
