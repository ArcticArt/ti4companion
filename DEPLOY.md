# Deploying to a Hetzner server

The production stack runs three containers via `docker-compose.yml`:

| Service    | Role                                                                 |
|------------|----------------------------------------------------------------------|
| `postgres` | PostgreSQL 17, data persisted in the `pgdata` volume                  |
| `api`      | ASP.NET Core API **and** the Blazor WebAssembly client + SignalR hub  |
| `caddy`    | Reverse proxy with automatic HTTPS (Let's Encrypt)                    |

> The `.NET Aspire AppHost` is for local development/orchestration only. Production uses this
> compose file directly — no Aspire dashboard, no Docker-in-Docker.

## 1. Prepare the server

A small Hetzner Cloud VM (e.g. CX22, 2 vCPU / 4 GB) running Ubuntu is plenty.

```bash
# Install Docker Engine + compose plugin
curl -fsSL https://get.docker.com | sh

# (Optional) open the firewall for web traffic
ufw allow 80 && ufw allow 443 && ufw allow OpenSSH && ufw enable
```

## 2. Get the code and configure

```bash
git clone <your-repo> ti4companion && cd ti4companion   # or scp the folder up
cp .env.example .env
nano .env      # set a strong POSTGRES_PASSWORD and your SITE_ADDRESS
```

For HTTPS, point your domain's DNS **A/AAAA record at the server's IP first**, then set
`SITE_ADDRESS=ti4.example.com` in `.env`. Caddy obtains and renews the certificate automatically.
To test without a domain, leave `SITE_ADDRESS=:80` and browse to `http://<server-ip>`.

## 3. Launch

```bash
docker compose up -d --build
docker compose logs -f api      # watch migrations + content seeding run on first start
```

The API applies EF Core migrations and (re)seeds the TI4 content on startup, so the database is
ready automatically. Open `https://your-domain` (or `http://<server-ip>`):

- **Beamer:** open a session, click **▣ Beamer**, fullscreen the tab on the projector.
- **Players:** share the 5-character join code; they open the same URL and tap **Join**.

## 4. Updating

```bash
git pull
docker compose up -d --build
```

## 5. Backups

All durable data is in PostgreSQL (sessions) — the content is re-seeded from JSON on every start.

```bash
# Backup
docker compose exec postgres pg_dump -U ti4admin ti4db > backup_$(date +%F).sql
# Restore
cat backup.sql | docker compose exec -T postgres psql -U ti4admin -d ti4db
```

## Notes

- **Auto-wipe:** inactive sessions are deleted automatically after their `RetentionHours`
  (default 7 days, configurable per session in **Settings**). The cleanup worker runs every 15 min.
- **Editing game content:** the strategy cards, factions, objectives and technologies live in
  `Ti4Companion.ApiService/Data/Seed/*.json`. Edit them and redeploy; the content tables re-sync on
  startup (session data is untouched).
- **Faction icons:** drop `*.png` files into `Ti4Companion.Web/wwwroot/factions/` — see the README
  there. Until then a generated colour-and-initials badge is shown.

## Security review & public-hosting hardening

A review of the v9 codebase (June 2026). **Bottom line:** safe for friends-only / unlisted hosting
as-is; for a fully public, advertised URL, add the rate-limiting + input caps below first. The risk is
griefing and DoS, **not** data compromise.

**What's already solid**
- **No SQL injection.** Every query is EF Core LINQ (parameterised); there is no `FromSqlRaw` /
  `ExecuteSqlRaw` anywhere (the one `migrationBuilder.Sql` is a static, controlled migration).
- **No secret leakage.** `PlayerDto` deliberately omits `DeviceToken`; tokens never go over the wire in
  responses. Device tokens are 128-bit GUIDs (unguessable) sent in the `X-Device-Token` header.
- **No CSRF.** Auth is a custom header (not a cookie) and the client is same-origin (hosted model, no
  CORS opened), so cross-site requests can't ride along.
- **TLS** is correctly delegated to Caddy in production (the API serves plain HTTP behind the proxy);
  HTTPS redirection is dev-only. Make sure Postgres' port is **not** published to the host publicly.

**Auth model (by design).** Device-token only — no accounts. The host is the device that created the
session; host / self / active-player / current-picker rules are enforced server-side from the token.
A device with no token is a read-only spectator. Scoring objectives, casting votes and switching the
wall display are intentionally open to **any joined device**.

**Risks for a fully public URL (prioritised)**
1. **Open join + short codes (MEDIUM).** `GET /api/sessions/{code}` is unauthenticated (the wall display
   needs it) and join codes are 5 chars from a 30-symbol alphabet (~24M combinations) — enumerable. A
   determined griefer could scan for live sessions and, after joining, disrupt votes/scores/the display.
   *Mitigate:* keep the URL unlisted; and/or rate-limit `GET /{code}`; and/or lengthen codes; and/or put
   the whole site behind Caddy basic-auth; and/or make display/score/vote host-or-self-only.
2. **No rate limiting / DoS (MEDIUM).** `POST /api/sessions` is uncapped, so sessions (and their players
   /votes) can be spam-created until auto-wipe reclaims them (default 7 days). *Mitigate:* add ASP.NET
   rate-limiting (per-IP token bucket) on create + mutations; cap sessions per IP; shorten the public
   default `Ti4:DefaultRetentionHours`.
3. **No input length caps (LOW).** Session/player names are trimmed but unbounded (Postgres `text`).
   *Mitigate:* clamp lengths (~60 chars) in the create/join/update endpoints.

**Code-quality notes (non-blocking).** `CastVote` and `LockVote` are ~90% duplicate (extractable into a
shared apply-vote helper); the two `IsDevelopment()` blocks in `Program.cs` could merge; the cleanup
worker loads all sessions into memory per tick (fine at this scale). No correctness bugs were found in
the reviewed paths.
