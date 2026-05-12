using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LMS.Application.Admin.Students;
using LMS.Domain.Students;
using LMS.Domain.Users;
using LMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LMS.Api.Tests;

/// <summary>
/// Integration tests for the admin student–parent linkage endpoints
/// (Section 6, Part 4).
///
/// State strategy:
///   - Each test uses unique e-mail addresses to avoid cross-test state conflicts.
///   - StudentProfiles are seeded directly into the InMemory database via the
///     factory's service scope (students do not get auto-profiles on registration).
///   - ParentProfiles are auto-created by AuthService when a Parent user registers.
/// </summary>
public sealed class StudentLinkIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public StudentLinkIntegrationTests(LmsWebApplicationFactory factory)
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
            LastName  = "User",
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
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    /// <summary>
    /// Registers a student user and inserts a StudentProfile row directly into
    /// the InMemory database.  Returns the StudentProfile's Id.
    /// </summary>
    private async Task<Guid> SeedStudentProfileAsync(string email)
    {
        // Register the student user so a User row exists in the DB.
        var client = CreateClient();
        var reg = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email     = email,
            Password  = "Test@1234!",
            FirstName = "Anna",
            LastName  = "Student",
            Role      = (int)UserRole.Student,
        });

        if (reg.StatusCode != HttpStatusCode.Created
         && reg.StatusCode != HttpStatusCode.Conflict)
            throw new Exception($"Student register failed: {reg.StatusCode}");

        var regBody = await reg.Content.ReadFromJsonAsync<JsonElement>();
        var userId  = Guid.Parse(regBody.GetProperty("userId").GetString()!);

        // Seed the StudentProfile into the InMemory DB.
        using var scope   = _factory.Services.CreateScope();
        var       db      = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
        var       profile = StudentProfile.Create(userId);
        db.StudentProfiles.Add(profile);
        await db.SaveChangesAsync();

        return profile.Id;
    }

    /// <summary>
    /// Registers a parent user (which auto-creates a ParentProfile) and returns
    /// the ParentProfile's Id by querying the InMemory database.
    /// </summary>
    private async Task<Guid> RegisterParentAndGetProfileIdAsync(string email)
    {
        var client = CreateClient();
        var reg = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email     = email,
            Password  = "Test@1234!",
            FirstName = "Alice",
            LastName  = "Parent",
            Role      = (int)UserRole.Parent,
        });

        if (reg.StatusCode != HttpStatusCode.Created
         && reg.StatusCode != HttpStatusCode.Conflict)
            throw new Exception($"Parent register failed: {reg.StatusCode}");

        using var scope   = _factory.Services.CreateScope();
        var       db      = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
        var       profile = await db.ParentProfiles
            .Include(p => p.User)
            .FirstAsync(p => p.User.Email == email.ToLowerInvariant().Trim());

        return profile.Id;
    }

    // ── GET /api/admin/students — auth/role guards ─────────────────────────

    [Fact]
    public async Task GetStudents_WithoutToken_Returns401()
    {
        var client   = CreateClient();
        var response = await client.GetAsync("/api/admin/students");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStudents_WithParentRole_Returns403()
    {
        var client = await CreateAuthorizedClientAsync(
            "sl-parent-forbidden@example.com", UserRole.Parent);

        var response = await client.GetAsync("/api/admin/students");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── GET /api/admin/students — happy path ──────────────────────────────

    [Fact]
    public async Task GetStudents_WithTenantAdmin_Returns200()
    {
        var client = await CreateAuthorizedClientAsync(
            "sl-tenant-admin-list@example.com", UserRole.TenantAdmin);

        var response = await client.GetAsync("/api/admin/students");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<List<StudentLinkSummaryDto>>(JsonOpts);
        Assert.NotNull(body);
        // Response is a valid list (content depends on what other tests seeded).
    }

    // ── GET /api/admin/students/parent-options ────────────────────────────

    [Fact]
    public async Task GetParentOptions_WithTenantAdmin_Returns200()
    {
        var client = await CreateAuthorizedClientAsync(
            "sl-tenant-admin-opts@example.com", UserRole.TenantAdmin);

        var response = await client.GetAsync("/api/admin/students/parent-options");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<List<ParentOptionDto>>(JsonOpts);
        Assert.NotNull(body);
    }

    // ── PATCH /api/admin/students/{id}/parent ─────────────────────────────

    [Fact]
    public async Task AssignParent_ValidLink_Returns200WithLinkedParent()
    {
        var studentProfileId = await SeedStudentProfileAsync(
            "sl-student-to-link@example.com");
        var parentProfileId = await RegisterParentAndGetProfileIdAsync(
            "sl-parent-for-link@example.com");

        var client = await CreateAuthorizedClientAsync(
            "sl-admin-assign@example.com", UserRole.TenantAdmin);

        var response = await client.PatchAsJsonAsync(
            $"/api/admin/students/{studentProfileId}/parent",
            new { ParentProfileId = parentProfileId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<StudentLinkSummaryDto>(JsonOpts);
        Assert.NotNull(body);
        Assert.Equal(studentProfileId,  body.StudentId);
        Assert.Equal(parentProfileId,   body.ParentProfileId);
        Assert.NotNull(body.ParentEmail);
    }

    [Fact]
    public async Task AssignParent_NullParentId_UnlinksStudent()
    {
        // Seed a student already linked to a parent.
        var studentProfileId = await SeedStudentProfileAsync(
            "sl-student-to-unlink@example.com");
        var parentProfileId = await RegisterParentAndGetProfileIdAsync(
            "sl-parent-for-unlink@example.com");

        var admin = await CreateAuthorizedClientAsync(
            "sl-admin-unlink@example.com", UserRole.TenantAdmin);

        // First: assign
        await admin.PatchAsJsonAsync(
            $"/api/admin/students/{studentProfileId}/parent",
            new { ParentProfileId = parentProfileId });

        // Then: unlink by sending null
        var response = await admin.PatchAsJsonAsync(
            $"/api/admin/students/{studentProfileId}/parent",
            new { ParentProfileId = (Guid?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<StudentLinkSummaryDto>(JsonOpts);
        Assert.NotNull(body);
        Assert.Null(body.ParentProfileId);
        Assert.Null(body.ParentFullName);
        Assert.Null(body.ParentEmail);
    }

    [Fact]
    public async Task AssignParent_UnknownStudentId_Returns404()
    {
        var client = await CreateAuthorizedClientAsync(
            "sl-admin-404@example.com", UserRole.TenantAdmin);

        var response = await client.PatchAsJsonAsync(
            $"/api/admin/students/{Guid.NewGuid()}/parent",
            new { ParentProfileId = (Guid?)null });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignParent_UnknownParentProfileId_Returns400()
    {
        var studentProfileId = await SeedStudentProfileAsync(
            "sl-student-bad-parent@example.com");

        var client = await CreateAuthorizedClientAsync(
            "sl-admin-400@example.com", UserRole.TenantAdmin);

        var response = await client.PatchAsJsonAsync(
            $"/api/admin/students/{studentProfileId}/parent",
            new { ParentProfileId = Guid.NewGuid() }); // non-existent parent profile

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
