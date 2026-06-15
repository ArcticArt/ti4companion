using Microsoft.EntityFrameworkCore;
using Ti4Companion.ApiService.Data;
using Ti4Companion.ApiService.Endpoints;
using Ti4Companion.ApiService.Realtime;
using Ti4Companion.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (OpenTelemetry, health checks, resilience, service discovery).
builder.AddServiceDefaults();

// PostgreSQL via Aspire (connection string "ti4db" injected by the AppHost).
builder.AddNpgsqlDbContext<Ti4DbContext>("ti4db");

builder.Services.AddSignalR();
builder.Services.AddOpenApi();

// Background worker that wipes inactive sessions after their retention window.
builder.Services.AddHostedService<SessionCleanupWorker>();

var app = builder.Build();

// Apply migrations and (re-)seed the bilingual TI4 content on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Ti4DbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await db.Database.MigrateAsync();
    await ContentSeeder.SeedAsync(db, logger);
}

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// In production the app sits behind Caddy (which terminates TLS) and listens on plain HTTP,
// so HTTPS redirection only applies to local development.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Serve the Blazor WebAssembly client (hosted model: same origin as the API → no CORS).
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapGet("/api/ping", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }));

// REST API + real-time hub.
app.MapContentEndpoints();
app.MapSessionEndpoints();
app.MapHub<SessionHub>("/hubs/session");

// SPA fallback for the Blazor client routes.
app.MapFallbackToFile("index.html");

app.Run();
