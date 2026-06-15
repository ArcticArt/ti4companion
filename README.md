# Twilight Imperium 4 — Companion

A web companion for **Twilight Imperium: Fourth Edition** matches. Project the shared overview onto
the wall with a beamer while every player steers it live from their phone or an iPad. Tracks turn
order, strategy cards, who has passed, who performed their strategy action, public objectives and
who scored them, and each player's technologies.

Built with **.NET Aspire + Blazor WebAssembly + ASP.NET Core REST API + SignalR + PostgreSQL**, in
English and German.

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
Ti4Companion.AppHost          .NET Aspire orchestration (Postgres + API) — local dev
Ti4Companion.ServiceDefaults  shared telemetry / health / resilience
Ti4Companion.ApiService       REST API + SignalR hub + EF Core + content seeding + auto-wipe worker
                              (also hosts the published Blazor client)
Ti4Companion.Web              Blazor WebAssembly client (PWA) — control views + beamer view
Ti4Companion.Shared           DTOs / enums shared by API and client
```

## Run locally

**Prerequisites:** the .NET 10 SDK and a container runtime (**Docker Desktop** or Podman) — Aspire
starts PostgreSQL in a container.

```bash
dotnet run --project Ti4Companion.AppHost
```

The Aspire dashboard opens; from it open the **apiservice** endpoint. The API applies EF Core
migrations and seeds the TI4 content on startup. Open the app, create a session, and open a second
browser to join with the code. (In Visual Studio: set **Ti4Companion.AppHost** as the startup
project and press F5.)

### Without the AppHost

You can also run the API on its own against any PostgreSQL by setting a connection string:

```bash
ConnectionStrings__ti4db="Host=localhost;Database=ti4db;Username=postgres;Password=***" \
  dotnet run --project Ti4Companion.ApiService
```

### Resetting the dev database

The Aspire AppHost keeps PostgreSQL data in a Docker volume (`ti4-pgdata`) so sessions survive
restarts. If the **schema** changes (e.g. after pulling new migrations) you may see
`relation "..." already exists` on startup, because the old volume still has the previous schema.
Reset it once — stop the app, then:

```powershell
docker volume rm ti4-pgdata
```

The next F5 recreates the database from the current migration and re-seeds the content. (This only
discards local test sessions; the game content lives in the JSON seed files.)

## Editing game content

Strategy cards, factions, objectives and technologies are bilingual JSON seed files in
`Ti4Companion.ApiService/Data/Seed/`. Edit them and restart — the content tables re-sync on startup
(session data is left untouched). German text falls back to English where a translation is missing.
Some Thunder's Edge details are seeded from the best available public info and are easy to correct
there.

## Test on your phone (same Wi‑Fi)

Yes — you can run it on your PC and open it from your iPhone/Android on the same network. The app
just needs to listen on all interfaces instead of only `localhost`:

```powershell
# 1. A database (Docker):
docker run -d --name ti4-pg -e POSTGRES_DB=ti4db -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=*** -p 5432:5432 postgres:17

# 2. Run the API bound to all interfaces (the "lan" launch profile does this):
$env:ConnectionStrings__ti4db = "Host=localhost;Port=5432;Database=ti4db;Username=postgres;Password=***"
dotnet run --project Ti4Companion.ApiService --launch-profile lan

# 3. Allow the port through the Windows firewall (private network), once:
New-NetFirewallRule -DisplayName "TI4 Companion" -Direction Inbound -Protocol TCP -LocalPort 5116 -Action Allow -Profile Private
```

Find your PC's IPv4 address with `ipconfig`, then on the phone open `http://<PC-IP>:5116`. Create a
session on the PC and join with the code from the phone. (Plain HTTP over the LAN is fine for use;
iOS "Add to Home Screen" works best over HTTPS, e.g. via the docker-compose + Caddy setup.)

## Deployment

See [DEPLOY.md](DEPLOY.md) for the Linux host / docker-compose setup (Postgres + API + Caddy with
automatic HTTPS).
