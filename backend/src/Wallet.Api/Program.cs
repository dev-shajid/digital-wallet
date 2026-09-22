using System.Text;

using HealthChecks.NpgSql;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

using Serilog;

using WalletSystem.Api.Conventions;
using WalletSystem.Api.Middleware;
using WalletSystem.Application.Common.Models;
using WalletSystem.Infrastructure;
using WalletSystem.Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);

// ---- Logging (Serilog) ----------------------------------------------------

// Like configuring pino/winston before anything else in an Express app,
// so every later piece of startup code can already log through it.
builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(
            context.HostingEnvironment.ContentRootPath,
            "logs",
            "wallet-.log"),
        rollingInterval: RollingInterval.Day));

// ---- Services -------------------------------------------------------------

// Every controller automatically gets "api/v1" glued onto the front of
// its route through ApiPrefixConvention.
builder.Services.AddControllers(options =>
    options.Conventions.Add(new ApiPrefixConvention("api/v1")));

// Lets the Next.js frontend (a different origin in dev) call this API from the browser.
// The JWT travels as an Authorization header, not a cookie, so credentials aren't needed.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// [ApiController]'s automatic input validation is converted into the same
// ApiResponse<T> envelope used by the rest of the API.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry =>
                entry.Value!.Errors.Select(error => new ApiError
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

// Register JWT settings from the "Jwt" configuration section.
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

builder.Services.AddInfrastructure(builder.Configuration);

// ---- JWT Authentication ---------------------------------------------------

// var jwtSettings = builder.Configuration
//     .GetSection("Jwt")
//     .Get<JwtSettings>()
//     ?? throw new InvalidOperationException("Missing 'Jwt' configuration.");

// if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
// {
//     throw new InvalidOperationException("Missing 'Jwt:Secret' configuration.");
// }

// builder.Services
//     .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options =>
//     {
//         options.TokenValidationParameters = new TokenValidationParameters
//         {
//             ValidateIssuer = true,
//             ValidateAudience = true,
//             ValidateLifetime = true,
//             ValidateIssuerSigningKey = true,

//             ValidIssuer = jwtSettings.Issuer,
//             ValidAudience = jwtSettings.Audience,

//             IssuerSigningKey = new SymmetricSecurityKey(
//                 Encoding.UTF8.GetBytes(jwtSettings.Secret))
//         };
//     });

var jwtKey = builder.Configuration["Jwt:Secret"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Without this, the handler silently remaps short claim names ("sub", "role") to
        // long legacy URIs depending on the IdentityModel version, which makes reading
        // claims back (see ClaimsPrincipalExtensions.GetUserId) version-fragile.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            )
        };
    });
builder.Services.AddAuthorization();

// ---- Exception Handling ---------------------------------------------------

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ---- Swagger --------------------------------------------------------------

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Digital Wallet & Expense Management API",
        Version = "v1"
    });

    options.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter your JWT token."
        });

    options.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

// ---- Health Checks --------------------------------------------------------

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Missing 'ConnectionStrings:Default' configuration value.");

builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString,
        name: "postgres",
        tags: ["ready"]);

var app = builder.Build();

// ---- Middleware pipeline -------------------------------------------------

app.UseExceptionHandler();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapGet(
        "/",
        () => Results.Redirect("/swagger"))
        .ExcludeFromDescription();
}

app.UseCors("Frontend");

// JWT authentication must run before authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ---- Health Endpoints ----------------------------------------------------

// /health
// Runs every registered health check.

// /health/live
// Only confirms that the application process is running.

// /health/ready
// Confirms that the application can reach required dependencies.
app.MapHealthChecks("/health");

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

app.Run();