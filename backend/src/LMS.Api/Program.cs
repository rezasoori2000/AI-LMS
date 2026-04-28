using LMS.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ── Service registration ────────────────────────────────────────────────────
builder.Services.AddApiServices(builder.Configuration, builder.Environment);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// ── App / middleware pipeline ───────────────────────────────────────────────
var app = builder.Build();

app.UseApiPipeline(builder.Environment);

app.Run();

// Expose Program to integration test projects that use WebApplicationFactory<Program>.
// The class generated from top-level statements is internal by default.
public partial class Program { }
