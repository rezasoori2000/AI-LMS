using LMS.Application.Auth;
using LMS.Application.Auth.Dtos;
using LMS.Application.Common.Interfaces;
using LMS.Domain.Catalog;
using LMS.Domain.Conversations;
using LMS.Domain.Curriculum;
using LMS.Domain.Enrollments;
using LMS.Domain.Progress;
using LMS.Domain.Students;
using LMS.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LMS.Application.Tests.Auth;

// ── In-test fakes ─────────────────────────────────────────────────────────────
// No external mocking library required. Each fake is minimal—just enough to
// support the scenarios under test.

/// <summary>
/// Stores users in a plain dictionary. Identical logic to InMemoryUserRepository
/// but kept here so Application.Tests has zero dependency on Infrastructure.
/// </summary>
internal sealed class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User>   _byId    = [];
    private readonly Dictionary<string, Guid> _byEmail = [];

    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default)
    {
        _byId.TryGetValue(id, out var u);
        return Task.FromResult(u);
    }

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        User? u = null;
        if (_byEmail.TryGetValue(email.ToLowerInvariant(), out var id))
            _byId.TryGetValue(id, out u);
        return Task.FromResult(u);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(_byEmail.ContainsKey(email.ToLowerInvariant()));

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        _byId[user.Id]            = user;
        _byEmail[user.Email]      = user.Id;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        Task.CompletedTask;
}

/// <summary>
/// Stores the raw password with a "{raw}:hashed" suffix so Verify() can
/// recover it—suitable for unit tests only. Never use in production.
/// </summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string raw) => $"{raw}:hashed";
    public bool Verify(string raw, string hash) => hash == $"{raw}:hashed";
}

/// <summary>Returns a deterministic token that encodes the user's email for easy assertions.</summary>
internal sealed class FakeTokenService : ITokenService
{
    public string GenerateAccessToken(User user) => $"fake-token-for:{user.Email}";
}

/// <summary>
/// Minimal ILmsDbContext fake for AuthService unit tests.
/// Only ParentProfiles needs real DbSet behaviour; all other sets throw NotImplemented.
/// Uses EF Core InMemory provider scoped to this instance.
/// </summary>
internal sealed class FakeDbContextForAuth : ILmsDbContext
{
    private readonly DbContextOptions<FakeInMemoryDbContext> _opts;
    private readonly FakeInMemoryDbContext _ctx;

    public FakeDbContextForAuth()
    {
        _opts = new DbContextOptionsBuilder<FakeInMemoryDbContext>()
            .UseInMemoryDatabase($"auth-test-{Guid.NewGuid()}")
            .Options;
        _ctx = new FakeInMemoryDbContext(_opts);
    }

    public DbSet<ParentProfile> ParentProfiles => _ctx.Set<ParentProfile>();

    // Unused in AuthService unit tests — throw if accidentally called.
    public DbSet<User>           Users           => throw new NotImplementedException();
    public DbSet<Grade>          Grades          => throw new NotImplementedException();
    public DbSet<Subject>        Subjects        => throw new NotImplementedException();
    public DbSet<Chapter>        Chapters        => throw new NotImplementedException();
    public DbSet<Lesson>         Lessons         => throw new NotImplementedException();
    public DbSet<Question>       Questions       => throw new NotImplementedException();
    public DbSet<StudentProfile>           StudentProfiles           => throw new NotImplementedException();
    public DbSet<TeacherStudentAssignment> TeacherStudentAssignments => throw new NotImplementedException();
    public DbSet<Enrollment>               Enrollments               => throw new NotImplementedException();
    public DbSet<LessonProgress>        LessonProgress        => throw new NotImplementedException();
    public DbSet<QuestionAnswerRecord>   QuestionAnswerRecords => throw new NotImplementedException();
    public DbSet<AiConversation> AiConversations => throw new NotImplementedException();
    public DbSet<AiMessage>      AiMessages      => throw new NotImplementedException();

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _ctx.SaveChangesAsync(ct);
}

/// <summary>Thin EF Core DbContext used only by FakeDbContextForAuth.</summary>
internal sealed class FakeInMemoryDbContext : DbContext
{
    public FakeInMemoryDbContext(DbContextOptions<FakeInMemoryDbContext> opts) : base(opts) { }
    public DbSet<ParentProfile> ParentProfiles => Set<ParentProfile>();
}

// ── Test helpers ──────────────────────────────────────────────────────────────

file static class AuthServiceFactory
{
    public static AuthService Create(
        IUserRepository?  repo    = null,
        ILmsDbContext?    db      = null,
        IPasswordHasher?  hasher  = null,
        ITokenService?    tokens  = null,
        JwtSettings?      settings = null)
    {
        settings ??= new JwtSettings
        {
            SecretKey    = "unit-test-secret-key-min32-chars!!",
            Issuer       = "test-issuer",
            Audience     = "test-audience",
            ExpiryMinutes = 60,
        };

        return new AuthService(
            repo    ?? new FakeUserRepository(),
            db      ?? new FakeDbContextForAuth(),
            hasher  ?? new FakePasswordHasher(),
            tokens  ?? new FakeTokenService(),
            Options.Create(settings));
    }
}

// ── AuthService tests ─────────────────────────────────────────────────────────

public sealed class AuthServiceTests
{
    // ── RegisterAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ReturnsAuthResponse()
    {
        var svc = AuthServiceFactory.Create();

        var response = await svc.RegisterAsync(
            new RegisterRequest("alice@example.com", "Password123!", "Alice", "Smith", UserRole.Student));

        Assert.NotEqual(Guid.Empty, response.UserId);
        Assert.Equal("alice@example.com", response.Email);
        Assert.Equal(UserRole.Student, response.Role);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal(60 * 60, response.ExpiresIn);                  // 60 min in seconds
        Assert.Equal("fake-token-for:alice@example.com", response.AccessToken);
    }

    [Fact]
    public async Task RegisterAsync_EmailIsCanonicalisedToLowercase()
    {
        var repo = new FakeUserRepository();
        var svc  = AuthServiceFactory.Create(repo: repo);

        await svc.RegisterAsync(
            new RegisterRequest("BOB@EXAMPLE.COM", "Password123!", "Bob", "Jones", UserRole.Teacher));

        // The stored email should have been normalised by User.Create()
        var user = await repo.FindByEmailAsync("bob@example.com");
        Assert.NotNull(user);
        Assert.Equal("bob@example.com", user.Email);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsEmailAlreadyRegisteredException()
    {
        var svc = AuthServiceFactory.Create();

        await svc.RegisterAsync(
            new RegisterRequest("dup@example.com", "Password123!", "Dup", "User", UserRole.Student));

        await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(
            () => svc.RegisterAsync(
                new RegisterRequest("dup@example.com", "Different99!", "Dup", "User", UserRole.Student)));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_IsCaseInsensitive()
    {
        var svc = AuthServiceFactory.Create();

        await svc.RegisterAsync(
            new RegisterRequest("user@example.com", "Password123!", "First", "Last", UserRole.Student));

        await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(
            () => svc.RegisterAsync(
                new RegisterRequest("USER@EXAMPLE.COM", "DifferentPass456!", "First", "Last", UserRole.Student)));
    }

    [Fact]
    public async Task RegisterAsync_PasswordIsNeverStoredAsPlaintext()
    {
        var repo = new FakeUserRepository();
        var svc  = AuthServiceFactory.Create(repo: repo);

        await svc.RegisterAsync(
            new RegisterRequest("secure@example.com", "SuperSecret!", "Sec", "Ure", UserRole.Student));

        var user = await repo.FindByEmailAsync("secure@example.com");
        Assert.NotNull(user);
        Assert.NotEqual("SuperSecret!", user.PasswordHash);
        Assert.Contains(":hashed", user.PasswordHash);  // fake hasher marker
    }

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        var svc = AuthServiceFactory.Create();

        await svc.RegisterAsync(
            new RegisterRequest("carol@example.com", "MyPass123!", "Carol", "Danvers", UserRole.Teacher));

        var response = await svc.LoginAsync(
            new LoginRequest("carol@example.com", "MyPass123!"));

        Assert.Equal("carol@example.com", response.Email);
        Assert.Equal("fake-token-for:carol@example.com", response.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsInvalidCredentialsException()
    {
        var svc = AuthServiceFactory.Create();

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => svc.LoginAsync(new LoginRequest("ghost@example.com", "irrelevant")));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsInvalidCredentialsException()
    {
        var svc = AuthServiceFactory.Create();

        await svc.RegisterAsync(
            new RegisterRequest("dave@example.com", "RealPassword1!", "Dave", "Smith", UserRole.Student));

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => svc.LoginAsync(new LoginRequest("dave@example.com", "WrongPassword!")));
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsInvalidCredentialsException()
    {
        var repo = new FakeUserRepository();
        var svc  = AuthServiceFactory.Create(repo: repo);

        await svc.RegisterAsync(
            new RegisterRequest("inactive@example.com", "MyPass123!", "In", "Active", UserRole.Student));

        // Deactivate the user through the aggregate
        var user = await repo.FindByEmailAsync("inactive@example.com");
        user!.Deactivate();

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => svc.LoginAsync(new LoginRequest("inactive@example.com", "MyPass123!")));
    }

    [Fact]
    public async Task LoginAsync_RecordsLastLoginAt()
    {
        var repo = new FakeUserRepository();
        var svc  = AuthServiceFactory.Create(repo: repo);

        await svc.RegisterAsync(
            new RegisterRequest("eve@example.com", "MyPass123!", "Eve", "Last", UserRole.Teacher));

        var before = DateTime.UtcNow.AddSeconds(-1);

        await svc.LoginAsync(new LoginRequest("eve@example.com", "MyPass123!"));

        var user = await repo.FindByEmailAsync("eve@example.com");
        Assert.NotNull(user!.LastLoginAt);
        Assert.True(user.LastLoginAt > before);
    }

    [Fact]
    public async Task RegisterAsync_TenantIdIsPropagatedToResponse()
    {
        var svc      = AuthServiceFactory.Create();
        var tenantId = Guid.NewGuid();

        var response = await svc.RegisterAsync(
            new RegisterRequest("tenant-user@example.com", "Pass123!", "Tenant", "User", UserRole.TenantAdmin, tenantId));

        Assert.Equal(tenantId, response.TenantId);
    }

    [Fact]
    public async Task RegisterAsync_SuperAdmin_TenantIdIsNull()
    {
        var svc = AuthServiceFactory.Create();

        var response = await svc.RegisterAsync(
            new RegisterRequest("admin@example.com", "AdminPass1!", "Super", "Admin", UserRole.SuperAdmin));

        Assert.Null(response.TenantId);
    }
}
