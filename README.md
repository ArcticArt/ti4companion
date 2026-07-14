# Twilight Imperium 4 — Companion

A web companion for **Twilight Imperium: Fourth Edition** matches. Project the shared overview onto
the wall with a beamer while every player steers it live from their phone or an iPad. Tracks turn
order, strategy cards, who has passed, who performed their strategy action, public objectives and
who scored them, and each player's technologies.

Built with **.NET Aspire + Blazor WebAssembly + ASP.NET Core REST API + SignalR + SQLite**, in
English and German. No Docker required — all data lives in a single SQLite file.

A hosted instance runs at **[ti4companion.com](https://ti4companion.com)** — free to use, no
account needed. This repository is the full source, licensed under the [MIT license](LICENSE)
(code only — see [Legal](#legal--credits) below for the game content). Contributions are welcome:
see [CONTRIBUTING.md](CONTRIBUTING.md).

## Highlights

- **Live shared overview** — one session state, broadcast to all devices over SignalR. The beamer
  view enlarges a strategy card's ability text the moment its action is played.
- **Open control** — any joined device can update any player's status (so the host can act for a
  player who has no phone). Join with a 5-character code.
- **Turn tracker** — initiative order from the strategy cards, with the Naalu Collective always
  acting first (initiative 0). Passed players are skipped.
- **All 31 factions** (Base + Prophecy of Kings + Council Keleres + Thunder's Edge), the current
  8 strategy cards (Codex IV + Thunder's Edge revisions), public + secret objectives, agendas with
  a full voting flow (open and face-down), planets, units, and the tech tree.
- **Agenda phase on the wall** — the players as a "galactic council" portrait arc with live votes,
  influence and the result line.
- **Match statistics** — round/phase/per-player durations from the built-in match log, as charts
  on the wall when the host ends the game.
- **Toggleable tech overview**, configurable **expansions**, **EN/DE** switch (saved per device),
  persistent sessions with a configurable **auto-wipe**.

## Projects

```
Ti4Companion.AppHost          .NET Aspire orchestration (runs the API) — local dev
Ti4Companion.ServiceDefaults  shared telemetry / health / resilience
Ti4Companion.ApiService       REST API + SignalR hub + EF Core (sessions + master content) + auto-wipe
                              (also hosts the published Blazor client)
Ti4Companion.Web              Blazor WebAssembly client (PWA) — control views + beamer view
Ti4Companion.Shared           DTOs / enums shared by API and client
```

## Run locally

**Prerequisites:** the .NET 10 SDK. That's it — no Docker, no database server. SQLite is built in,
the session database (`ti4.db`) is created automatically on first run, and the content database (`ti4master.db`) ships with the repo.

```bash
dotnet run --project Ti4Companion.AppHost
```

The Aspire dashboard opens; from it open the **apiservice** endpoint. The API applies EF Core
migrations to both databases on startup. Open the app, create a session, and open a second
browser to join with the code. (In Visual Studio: set **Ti4Companion.AppHost** as the startup
project and press F5.)

### Without the AppHost

The Aspire AppHost just launches the API, so you can also run the API directly:

```bash
dotnet run --project Ti4Companion.ApiService
```

It listens on `http://localhost:5116` and creates `ti4.db` in the project directory (`ti4master.db` is
already there, committed). Override either location with a connection string if you like (`ti4db` / `ti4masterdb`):

```bash
ConnectionStrings__ti4db="Data Source=/path/to/ti4.db" \
  dotnet run --project Ti4Companion.ApiService
```

### Resetting the dev database

There are two SQLite files: `ti4.db` (live sessions, gitignored) and `ti4master.db` (the reference
content, **committed to git**). To discard local test sessions, stop the app and delete `ti4.db`:

```powershell
Remove-Item ti4.db, ti4.db-wal, ti4.db-shm -ErrorAction SilentlyContinue
```

The next run recreates `ti4.db` from its migrations. To discard **content** edits, restore the committed
DB instead: `git checkout -- Ti4Companion.ApiService/ti4master.db`.

## Editing game content

All reference content (strategy cards, factions, objectives, technologies, planets, units — plus faction
abilities, leaders, breakthroughs and starting units) lives in the **master content database**
`Ti4Companion.ApiService/ti4master.db`, which is **committed to git**. Edit it directly with any SQLite tool
(e.g. [DB Browser for SQLite](https://sqlitebrowser.org/)), then commit the changed `ti4master.db`. Each
content row carries a version + source (Base/PoK/Codex/Thunder's Edge) and the app uses the newest revision.
German text falls back to English where a translation is missing. (There are no JSON seed files anymore — the
content was bootstrapped once and the seeds removed; some Thunder's Edge details are from the best available
public info and are easy to correct in the DB.)

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
iOS "Add to Home Screen" works best over HTTPS, e.g. via the reverse-proxy setup in deployment.)

## Deployment

See [DEPLOY.md](DEPLOY.md) for the Docker-free Hetzner setup (a `publish.ps1` build run as a
systemd service behind Apache, with Let's Encrypt HTTPS and SQLite backups).

## Legal & credits

This is an **unofficial, fan-made** companion — not affiliated with, endorsed, or sponsored by
Asmodee or Fantasy Flight Games. Twilight Imperium, Prophecy of Kings, Thunder's Edge and all
related names, marks, text and artwork are © & ™ **Asmodee North America, Inc. (Fantasy Flight
Games)**. All rights reserved.

The [MIT license](LICENSE) covers the **source code only**. The game reference content in
`ti4master.db` and the game imagery under `Ti4Companion.Web/wwwroot/` remain the property of the
rights holder; they are included solely for this free, non-commercial fan project and will be
removed on request. Do not reuse them commercially.

The project is free and non-commercial: no sale, no ads, no paywall. Voluntary donations toward
the server costs are welcome via [PayPal](https://www.paypal.me/Frostforgestudio), but never required.

Special thanks to [ti4lookup](https://github.com/bern/ti4lookup) (the primary structured-data
source), the [TI4 Fandom wiki](https://twilight-imperium.fandom.com), and the
[BGG vectorized race & tech symbols](https://boardgamegeek.com/filepage/180049/ti4-race-and-tech-symbols-vectorized).

Created by [FrostForgeStudio](https://frostforge.studio).
**Contact:** bugs and suggestions via GitHub issues, or [Frostforgestudio@proton.me](mailto:Frostforgestudio@proton.me).
