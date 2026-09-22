using HealthChecks.NpgSql;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using WalletSystem.Api.Conventions;
using WalletSystem.Api.Middleware;
using WalletSystem.Application.Common.Models;
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
// Every controller automatically gets "api/v1" glued onto the front of its route
// (see ApiPrefixConvention), so a controller only ever declares its own resource
// name - e.g. [Route("diagnostics")], never [Route("api/v1/diagnostics")].
builder.Services.AddControllers(options =>
    options.Conventions.Add(new ApiPrefixConvention("api/v1")));

// [ApiController]'s automatic input validation (e.g. a missing required field) would
// otherwise return its own ValidationProblemDetails shape. This makes it return the
// same ApiResponse<T> envelope as everything else instead, so EVERY response -
// success, validation failure, or unhandled exception - looks the same.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error => new ApiError
            {
                Field = entry.Key,
                Message = error.ErrorMessage
            }))
            .ToList();

        return new BadRequestObjectResult(new ApiResponse<object?>
        {
            Success = false,
            Status = StatusCodes.Status400BadRequest,
            Message = "One or more validation errors occurred.",
            Data = null,
            Errors = errors
        });
    };
});

builder.Services.AddInfrastructure(builder.Configuration);

// Turns any unhandled exception into the same ApiResponse<T> envelope (see
// GlobalExceptionHandler) instead of an ASP.NET Core default error page.
// AddProblemDetails() is required here too - not because we use its format (our
// handler always writes the ApiResponse envelope itself), but because
// UseExceptionHandler() refuses to start up without a fallback formatter registered.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Digital Wallet & Expense Management API",
        Version = "v1"
    });

    // Pulls in the /// <summary> comments from controllers (see GenerateDocumentationFile
    // in Wallet.Api.csproj) so Swagger UI shows real descriptions, not just route names.
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
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
// so these health endpoints are the only operational HTTP surface for now.
// /health          - runs every registered check (here: just Postgres) - the simple "is everything OK" endpoint
// /health/live      - liveness: no checks, just confirms the process can respond
// /health/ready     - readiness: process up AND can reach Postgres
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
