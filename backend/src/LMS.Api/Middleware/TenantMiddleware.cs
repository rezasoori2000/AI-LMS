namespace LMS.Api.Middleware;

/// <summary>
/// Placeholder for multi-tenant context resolution middleware.
/// </summary>
/// <remarks>
/// Will be activated in Phase 2 when multi-tenancy is introduced.
///
/// Implementation strategies to evaluate:
/// <list type="bullet">
///   <item>Subdomain — <c>context.Request.Host.Host</c> (e.g. "school-a.lms.com")</item>
///   <item>Request header — <c>X-Tenant-Id</c></item>
///   <item>JWT claim — read after <c>UseAuthentication()</c> runs</item>
/// </list>
///
/// This middleware must be placed AFTER <c>UseAuthentication</c> if resolving from JWT claims,
/// or BEFORE it if using subdomain / header strategies.
/// </remarks>
public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // TODO (Phase 2): resolve tenant identifier and attach to context
        // Example (header strategy):
        //   var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        //   if (string.IsNullOrEmpty(tenantId)) { context.Response.StatusCode = 400; return; }
        //   context.Items["TenantId"] = tenantId;

        await _next(context);
    }
}
