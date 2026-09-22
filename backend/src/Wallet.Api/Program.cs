using HealthChecks.NpgSql;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using WalletSystem.Api.Middleware;
using WalletSystem.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---- Logging (Serilog) ----------------------------------------------------
// Like configuring pino/winston before anything else in an Express app, so every
// later piece of startup code can already log through it.
builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(context.HostingEnvironment.ContentRootPath, "logs", "wallet-.log"),
        rollingInterval: RollingInterval.Day));

// ---- Services (this is the "container" you register your app's dependencies into,
// similar to building up an Express `app` object or a NestJS module) --------
builder.Services.AddControllers();

builder.Services.AddInfrastructure(builder.Configuration);

// Turns any unhandled exception into a safe RFC 7807 ProblemDetails JSON response.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Digital Wallet & Expense Management API",
        Version = "v1"
    });
});

// Liveness = "is the process up" (no dependencies checked).
// Readiness = "is the process up AND can it reach everything it needs" (here: Postgres).
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Default' configuration value.");

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);

var app = builder.Build();

// ---- Middleware pipeline (order matters - same idea as Express middleware chain) ----
app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseAuthorization();

app.MapControllers();

// No business routes exist yet (Phase 1 = structure + schema + logging + health only),
// so these two health endpoints are the only operational HTTP surface for now.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // don't run any checks, just confirms the process can respond
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") // runs the Postgres check
});

app.Run();
