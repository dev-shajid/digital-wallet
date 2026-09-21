using Serilog;
using Wallet.Api.Extensions;
using Wallet.Application;
using Wallet.Application.Common.Models;
using Wallet.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseSerilogRequestLogging();
app.UseApiMiddleware();

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Json(ApiResponse<object?>.Ok(null, "Service is healthy.")))
    .ExcludeFromDescription();

app.Run();

public partial class Program;
