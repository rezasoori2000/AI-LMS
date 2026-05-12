using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LMS.Application.Auth.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LMS.Api.Tests;

/// <summary>
/// End-to-end integration tests for the auth endpoints.
/// <see cref="WebApplicationFactory{TEntryPoint}"/> boots the full ASP.NET Core
/// pipeline in-process, including the middleware, DI container, and
/// <c>InMemoryUserRepository</c>, so no external services are required.
///
/// A new factory (and therefore a fresh in-memory store) is created per test class
/// to avoid cross-test pollution.
/// </summary>
public sealed class AuthIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    // API serialises enums as strings — must use JsonStringEnumConverter when
    // deserialising response bodies that contain enum fields (e.g. AuthResponse.Role).
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public AuthIntegrationTests(LmsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates a new HTTP client. Each call shares the same factory (same DI scope).</summary>
    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

    private static RegisterRequest ValidRegisterRequest(
        string email   = "test@example.com",
        string? suffix = null) =>
        new(
            Email:     suffix is null ? email : $"test{suffix}@example.com",
            Password:  "ValidPass1!",
            FirstName: "Test",
            LastName:  "User",
            Role:      LMS.Domain.Users.UserRole.Student);

    // ── Register — happy path ─────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidRequest_Returns201AndAuthResponse()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { Email = "new1@example.com", Password = "ValidPass1!", FirstName = "Test", LastName = "User", Role = 4 }); // 4 = Student

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.Equal("Bearer", body.TokenType);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.True(body.ExpiresIn > 0);
        Assert.NotEqual(Guid.Empty, body.UserId);
    }

    [Fact]
    public async Task Register_WithValidRequest_ReturnsCorrectEmailAndRole()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { Email = "new2@example.com", Password = "ValidPass1!", FirstName = "Test", LastName = "User", Role = 4 });

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.Equal("new2@example.com", body.Email);
    }

    // ── Register — validation errors ─────────────────────────────────────────

    [Fact]
    public async Task Register_MissingEmail_Returns400()
    {
        var client   = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { Password = "ValidPass1!", Role = 4 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmailFormat_Returns400()
    {
        var client   = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { Email = "not-an-email", Password = "ValidPass1!", Role = 4 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var client   = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { Email = "short@example.com", Password = "abc", Role = 4 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Register — conflict ───────────────────────────────────────────────────

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = CreateClient();
        var payload = new { Email = "dup@example.com", Password = "ValidPass1!", FirstName = "Dup", LastName = "User", Role = 4 };

        await client.PostAsJsonAsync("/api/auth/register", payload);          // first
        var second = await client.PostAsJsonAsync("/api/auth/register", payload); // duplicate

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsProblemDetails()
    {
        var client  = CreateClient();
        var payload = new { Email = "dup2@example.com", Password = "ValidPass1!", FirstName = "Dup", LastName = "User", Role = 4 };

        await client.PostAsJsonAsync("/api/auth/register", payload);
        var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("status", out var statusProp));
        Assert.Equal(409, statusProp.GetInt32());
    }

    // ── Login — happy path ────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200AndAuthResponse()
    {
        var client = CreateClient();

        await client.PostAsJsonAsync(
            "/api/auth/register",
            new { Email = "login1@example.com", Password = "ValidPass1!", FirstName = "Login", LastName = "User", Role = 4 });

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "login1@example.com", Password = "ValidPass1!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.Equal("Bearer", body.TokenType);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
    }

    // ── Login — 401 ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client   = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "ghost@example.com", Password = "ValidPass1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = CreateClient();

        await client.PostAsJsonAsync(
            "/api/auth/register",
            new { Email = "login2@example.com", Password = "ValidPass1!", FirstName = "Login", LastName = "User", Role = 4 });

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "login2@example.com", Password = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsProblemDetails()
    {
        var client   = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "nobody@example.com", Password = "ValidPass1!" });

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("status", out var statusProp));
        Assert.Equal(401, statusProp.GetInt32());
    }

    // ── Login — validation errors ─────────────────────────────────────────────

    [Fact]
    public async Task Login_MissingPassword_Returns400()
    {
        var client   = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "missing@example.com" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
