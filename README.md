# Twilight Imperium 4 — Companion

A web companion for **Twilight Imperium: Fourth Edition** matches. Project the shared overview onto
the wall with a beamer while every player steers it live from their phone or an iPad. Tracks turn
order, strategy cards, who has passed, who performed their strategy action, public objectives and
who scored them, and each player's technologies.

Built with **.NET Aspire + Blazor WebAssembly + ASP.NET Core REST API + SignalR + SQLite**, in
English and German. No Docker required — all data lives in a single SQLite file.

## Highlights

- **Live shared overview** — one session state, broadcast to all devices over SignalR. The beamer
  view enlarges a strategy card's ability text the moment its action is played.
- **Open control** — any joined device can update any player's status (so the host can act for a
  player who has no phone). Join with a 5-character code.
- **Turn tracker** — initiative order from the strategy cards, with the Naalu Collective always
  acting first (initiative 0). Passed players are skipped.
- **All 30 factions** (Base + Prophecy of Kings + Council Keleres + Thunder's Edge), the current
  8 strategy cards (Codex IV + Thunder's Edge revisions), public objectives, and the tech tree.
- **Toggleable tech overview**, configurable **expansions**, **EN/DE** switch (saved per device),
  persistent sessions with a configurable **auto-wipe**.

## Projects

```
Ti4Companion.AppHost          .NET Aspire orchestration (runs the API) — local dev
Ti4Companion.ServiceDefaults  shared telemetry / health / resilience
Ti4Companion.ApiService       REST API + SignalR hub + EF Core + content seeding + auto-wipe worker
                              (also hosts the published Blazor client)
Ti4Companion.Web              Blazor WebAssembly client (PWA) — control views + beamer view
Ti4Companion.Shared           DTOs / enums shared by API and client
```

## Run locally

**Prerequisites:** the .NET 10 SDK. That's it — no Docker, no database server. SQLite is built in,
and the database file (`ti4.db`) is created automatically on first run.

```bash
dotnet run --project Ti4Companion.AppHost
```

The Aspire dashboard opens; from it open the **apiservice** endpoint. The API applies EF Core
migrations and seeds the TI4 content on startup. Open the app, create a session, and open a second
browser to join with the code. (In Visual Studio: set **Ti4Companion.AppHost** as the startup
project and press F5.)

### Without the AppHost

The Aspire AppHost just launches the API, so you can also run the API directly:

```bash
dotnet run --project Ti4Companion.ApiService
```

It listens on `http://localhost:5116` and creates `ti4.db` in the project directory. Override the
location with a connection string if you like:

```bash
ConnectionStrings__ti4db="Data Source=/path/to/ti4.db" \
  dotnet run --project Ti4Companion.ApiService
```

### Resetting the dev database

The SQLite database is the single file `ti4.db` (plus its `-wal`/`-shm` companions). To start fresh —
stop the app, then delete it:

```powershell
Remove-Item ti4.db, ti4.db-wal, ti4.db-shm -ErrorAction SilentlyContinue
```

The next run recreates the database from the current migration and re-seeds the content. (This only
discards local test sessions; the game content lives in the JSON seed files.)

## Editing game content

Strategy cards, factions, objectives and technologies are bilingual JSON seed files in
`Ti4Companion.ApiService/Data/Seed/`. Edit them and restart — the content tables re-sync on startup
(session data is left untouched). German text falls back to English where a translation is missing.
Some Thunder's Edge details are seeded from the best available public info and are easy to correct
there.

## Test on your phone (same Wi‑Fi)

Yes — you can run it on your PC and open it from your iPhone/Android on the same network. The app
just needs to listen on all interfaces instead of only `localhost` (no database setup — SQLite is a
local file):

```powershell
# 1. Run the API bound to all interfaces (the "lan" launch profile does this):
dotnet run --project Ti4Companion.ApiService --launch-profile lan

# 2. Allow the port through the Windows firewall (private network), once:
New-NetFirewallRule -DisplayName "TI4 Companion" -Direction Inbound -Protocol TCP -LocalPort 5116 -Action Allow -Profile Private
```

Find your PC's IPv4 address with `ipconfig`, then on the phone open `http://<PC-IP>:5116`. Create a
session on the PC and join with the code from the phone. (Plain HTTP over the LAN is fine for use;
iOS "Add to Home Screen" works best over HTTPS, e.g. via the Caddy setup in deployment.)

## Deployment

See [DEPLOY.md](DEPLOY.md) for the Docker-free Hetzner setup (a `dotnet publish` build run as a
systemd service behind Caddy, with automatic HTTPS and SQLite backups).
