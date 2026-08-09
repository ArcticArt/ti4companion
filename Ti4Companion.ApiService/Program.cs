using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Ti4Companion.ApiService.Data;
using Ti4Companion.ApiService.Endpoints;
using Ti4Companion.ApiService.Realtime;
using Ti4Companion.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (OpenTelemetry, health checks, resilience, service discovery).
builder.AddServiceDefaults();

// SQLite — local files, no external database process (and no Docker). Two databases:
//  • ti4.db        — runtime session state (Ti4DbContext).
//  • ti4master.db  — the master reference content (MasterDbContext), bootstrapped once from the JSON.
var connectionString = builder.Configuration.GetConnectionString("ti4db") ?? "Data Source=ti4.db";
builder.Services.AddDbContext<Ti4DbContext>(options => options.UseSqlite(connectionString));

var masterConnectionString = builder.Configuration.GetConnectionString("ti4masterdb") ?? "Data Source=ti4master.db";
builder.Services.AddDbContext<MasterDbContext>(options => options.UseSqlite(masterConnectionString));

builder.Services.AddSignalR();
builder.Services.AddOpenApi();

// Public-hosting hardening (see DEPLOY.md "Security review"): per-IP rate limits so session
// creation can't be spammed and join codes can't be enumerated in bulk. Limits are generous —
// a whole game night behind one NAT IP stays far below them.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Creating sessions: 20 per 10 minutes per IP.
    options.AddPolicy("session-create", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(10) }));
    // Looking a session up by code (unauthenticated; every client refresh uses it): 600/min per IP —
    // plenty for 8 devices refreshing on every SignalR event, useless for scanning the code space.
    options.AddPolicy("session-read", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 600, Window = TimeSpan.FromMinutes(1) }));
});

// Background worker that wipes inactive sessions after their retention window.
builder.Services.AddHostedService<SessionCleanupWorker>();

var app = builder.Build();

// Apply migrations to both databases on startup. The master content DB is bootstrapped from the JSON
// only when it is empty (first run); thereafter it is canonical and never re-seeded.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var db = scope.ServiceProvider.GetRequiredService<Ti4DbContext>();
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;"); // better concurrency for SQLite

    var master = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
    await master.Database.MigrateAsync();
    await master.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");

    // The master content DB (ti4master.db) is a committed artifact, edited directly — there is no JSON
    // bootstrap anymore. Warn loudly if it somehow came up empty (e.g. the file was lost/not restored).
    if (!await master.Factions.AnyAsync())
        logger.LogWarning("Master content DB has no content. ti4master.db is a committed artifact — restore it from source control (the JSON bootstrap has been removed).");

    // Cache the static faction initiative overrides so the session-mutation path doesn't hit the master DB.
    await FactionInitiative.LoadAsync(master);
    // Same for objective points — needed when a finished game is summarised.
    await ObjectivePoints.LoadAsync(master);
}

// Behind the reverse proxy (Apache/Caddy on the same box) the client address arrives in
// X-Forwarded-For; honour it so the per-IP rate limits see real clients, not 127.0.0.1.
// The defaults only trust loopback proxies, which matches the deployment.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});
app.UseRateLimiter();

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
