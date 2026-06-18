# Deploying to a Hetzner server

No Docker. The app is a single self-contained ASP.NET Core process (it also serves the Blazor
WebAssembly client and the SignalR hub) that stores everything in a local **SQLite** file. It runs
as a **systemd** service behind **Caddy**, which terminates TLS with an automatic Let's Encrypt
certificate.

| Piece            | Role                                                                          |
|------------------|-------------------------------------------------------------------------------|
| `ti4companion`   | the published ASP.NET Core API **+** Blazor client + SignalR hub (Kestrel)    |
| `ti4.db`         | the SQLite database (sessions) — one file, lives under `/var/lib/ti4companion` |
| `caddy`          | reverse proxy with automatic HTTPS (Let's Encrypt)                            |

> The `.NET Aspire AppHost` is for local development only. In production you run the published
> `Ti4Companion.ApiService` directly as a service — there is no Aspire dashboard and no container.

## Live deployment (ti4companion.com)
The live box (Hetzner, Debian 13) **also hosts an SVN repo via Apache** (`mod_dav_svn`, reachable over
the bare IP). SVN is Apache-only, so the live site uses **Apache as the reverse proxy** instead of Caddy
(steps 1 + 4 below — install Caddy only on a clean box). The rest is identical: publish (step 2) → systemd
service as the `ti4` user (step 3). Key facts:
- **Hetzner Cloud Firewall:** inbound **22** (SSH, ideally your IP), **80** and **443** (`0.0.0.0/0` + `::/0`).
  SSH is **key-only** (`PasswordAuthentication no`, root `prohibit-password`) + `fail2ban`.
- **Apache vHost** for the domain (a separate `sites-available/ti4companion.conf`; the SVN stays the
  default vHost so the IP keeps serving it). Enable `proxy proxy_http proxy_wstunnel headers rewrite`:
  ```apache
  <VirtualHost *:80>
      ServerName ti4companion.com
      ServerAlias *.ti4companion.com
      DocumentRoot /var/www/html
      ProxyPass /.well-known/acme-challenge/ !            # let certbot serve the ACME challenge
      RewriteEngine On
      RewriteCond %{HTTP:Upgrade} =websocket [NC]
      RewriteRule ^/(.*) ws://127.0.0.1:5000/$1 [P,L]     # SignalR WebSocket
      ProxyPreserveHost On
      ProxyPass / http://127.0.0.1:5000/
      ProxyPassReverse / http://127.0.0.1:5000/
  </VirtualHost>
  ```
  Then HTTPS: `certbot --apache -d ti4companion.com -d www.ti4companion.com --redirect` (auto-renews).
- **After every upload:** if you `dotnet publish` by hand instead of `publish.ps1`, you MUST rewrite the
  boot script in `index.html` (see step 2) or the page spins forever.

## 1. Prepare the server

A small Hetzner Cloud VM (e.g. CX22, 2 vCPU / 4 GB) running Ubuntu is plenty.

```bash
# .NET 10 runtime (ASP.NET Core) — no SDK needed on the server if you publish elsewhere,
# but installing the SDK lets you build on the box too.
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --runtime aspnetcore
# (or: apt-get install -y dotnet-sdk-10.0  via the Microsoft package feed)

# Caddy (reverse proxy + automatic HTTPS)
apt-get install -y debian-keyring debian-archive-keyring apt-transport-https curl
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | tee /etc/apt/sources.list.d/caddy-stable.list
apt-get update && apt-get install -y caddy sqlite3

# (Optional) open the firewall for web traffic
ufw allow 80 && ufw allow 443 && ufw allow OpenSSH && ufw enable
```

## 2. Build & publish

Publish with the repo script **`publish.ps1`** (PowerShell, runs on the dev machine). It builds a
**self-contained linux-x64** bundle (so the server needs **no .NET runtime** — important on Debian 13,
which has no .NET 10 packages) and fixes the Blazor boot script in `index.html` (see the gotcha below):

```powershell
./publish.ps1            # output in ./publish
```

> **Don't `dotnet publish` by hand for deployment.** This app is served as plain static files
> (`UseBlazorFrameworkFiles`/`UseStaticFiles`), not via `MapStaticAssets`, so the .NET 9+ Blazor WASM
> fingerprint **import map is never populated** → the boot chain 404s and the page spins forever. The
> fixes (both already in the repo): `<WasmFingerprintAssets>false</WasmFingerprintAssets>` keeps the
> `dotnet.*` files at stable names, and `publish.ps1` writes the real `blazor.webassembly.<hash>.js`
> name into `index.html`. `dotnet publish` alone skips that rewrite → broken deploy.

Create the server dirs + a non-root service user, then upload:

```bash
ssh root@SERVER 'adduser --system --group --no-create-home --home /opt/ti4companion ti4; \
                 mkdir -p /opt/ti4companion /var/lib/ti4companion; chown ti4:ti4 /var/lib/ti4companion'
scp -r publish/* root@SERVER:/opt/ti4companion/        # first deploy: everything
# later updates (client-only changes): scp -r publish/wwwroot root@SERVER:/opt/ti4companion/
ssh root@SERVER 'chmod +x /opt/ti4companion/Ti4Companion.ApiService; chmod -R a+rX /opt/ti4companion/wwwroot'
```

## 3. Run it as a systemd service

Create `/etc/systemd/system/ti4companion.service`:

```ini
[Unit]
Description=TI4 Companion
After=network.target

[Service]
User=ti4
Group=ti4
WorkingDirectory=/opt/ti4companion
# Self-contained publish → run the native host directly (no `dotnet` runtime on the server).
ExecStart=/opt/ti4companion/Ti4Companion.ApiService
Restart=always
RestartSec=5
# Kestrel listens on localhost; the reverse proxy (Apache/Caddy) handles TLS.
Environment=ASPNETCORE_URLS=http://localhost:5000
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=HOME=/var/lib/ti4companion
# NOTE the quotes: systemd splits Environment= on spaces, and "Data Source=" contains one — without
# quotes only "Data" reaches the app → "Format of the initialization string..." crash.
Environment="ConnectionStrings__ti4db=Data Source=/var/lib/ti4companion/ti4.db"
# Auto-wipe inactive sessions (hours). Lower this for a public URL.
Environment=Ti4__DefaultRetentionHours=168
# Hardening (runs as the unprivileged ti4 user)
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=full
ProtectHome=true

[Install]
WantedBy=multi-user.target
```

```bash
systemctl daemon-reload
systemctl enable --now ti4companion
systemctl status ti4companion          # should be active (running)
journalctl -u ti4companion -f          # watch migrations + content seeding on first start
```

On first start the API creates `ti4.db`, applies the EF Core migration and seeds the TI4 content, so
the database is ready automatically.

## 4. Put Caddy in front

The repo's [`Caddyfile`](Caddyfile) proxies everything to `localhost:5000`. Point your domain's DNS
**A/AAAA record at the server's IP first**, then install the Caddyfile with your domain:

```bash
# Use your domain for automatic HTTPS (or ":80" to serve plain HTTP against the server IP).
SITE_ADDRESS=ti4.example.com   # export it, or just hard-code the domain in the Caddyfile
cp Caddyfile /etc/caddy/Caddyfile
systemctl restart caddy
```

Open `https://your-domain`:

- **Wall display:** open a session and open `/display/<code>` (or use the in-app display link),
  then fullscreen the tab on the projector.
- **Players:** share the 5-character join code; they open the same URL and tap **Join**.

## 5. Updating

```bash
dotnet publish Ti4Companion.ApiService -c Release -o publish      # on your dev machine
scp -r publish/* root@SERVER:/opt/ti4companion/
ssh root@SERVER 'systemctl restart ti4companion'
```

The database in `/var/lib/ti4companion/ti4.db` is untouched by the redeploy; migrations apply on
restart and the content re-seeds from the JSON files.

## 6. Backups

All durable data is the single SQLite file (sessions). Game content is re-seeded from JSON on every
start, so the `.db` is the only thing to back up. Use SQLite's online backup so you get a consistent
copy even while the app is running:

```bash
# One-off / cron — a consistent snapshot copied to a dated file:
sqlite3 /var/lib/ti4companion/ti4.db ".backup '/var/backups/ti4-$(date +%F).db'"
```

A daily cron entry (`crontab -e`):

```cron
0 4 * * * sqlite3 /var/lib/ti4companion/ti4.db ".backup '/var/backups/ti4-$(date +\%F).db'"
```

To restore: stop the service, drop the backup file in place as `ti4.db` (delete any stale
`ti4.db-wal`/`ti4.db-shm` first), then start the service.

```bash
systemctl stop ti4companion
rm -f /var/lib/ti4companion/ti4.db-wal /var/lib/ti4companion/ti4.db-shm
cp /var/backups/ti4-2026-06-17.db /var/lib/ti4companion/ti4.db
systemctl start ti4companion
```

**Continuous backups (optional).** For point-in-time recovery, [Litestream](https://litestream.io)
streams the SQLite WAL to S3-compatible object storage (e.g. Hetzner Object Storage) — run it as a
second systemd service pointed at `/var/lib/ti4companion/ti4.db`.

## Notes

- **Auto-wipe:** inactive sessions are deleted automatically after their `RetentionHours`
  (default 7 days = `Ti4__DefaultRetentionHours=168`, also configurable per session in **Settings**).
  The cleanup worker runs every 15 min.
- **Editing game content:** the strategy cards, factions, objectives, technologies, planets and units
  live in `Ti4Companion.ApiService/Data/Seed/*.json`. Edit them and redeploy; the content tables
  re-sync on startup (session data is untouched).
- **Faction icons:** drop `*.png` files into `Ti4Companion.Web/wwwroot/factions/` — see the README
  there. Until then a generated colour-and-initials badge is shown. (Rebuild/republish after adding.)

## Security review & public-hosting hardening

A review of the v9 codebase (June 2026). **Bottom line:** safe for friends-only / unlisted hosting
as-is; for a fully public, advertised URL, add the rate-limiting + input caps below first. The risk is
griefing and DoS, **not** data compromise.

**What's already solid**
- **No SQL injection.** Every query is EF Core LINQ (parameterised); there is no `FromSqlRaw` anywhere
  user input reaches (the one `ExecuteSqlRaw` is the static `PRAGMA journal_mode=WAL` at startup, and
  the one `migrationBuilder.Sql` is a static, controlled migration).
- **No secret leakage.** `PlayerDto` deliberately omits `DeviceToken`; tokens never go over the wire in
  responses. Device tokens are 128-bit GUIDs (unguessable) sent in the `X-Device-Token` header.
- **No CSRF.** Auth is a custom header (not a cookie) and the client is same-origin (hosted model, no
  CORS opened), so cross-site requests can't ride along.
- **TLS** is correctly delegated to Caddy in production (the API serves plain HTTP on localhost behind
  the proxy); HTTPS redirection is dev-only. Kestrel binds `localhost` only, so the API isn't reachable
  except through Caddy.

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
3. **No input length caps (LOW).** Session/player names are trimmed but unbounded. *Mitigate:* clamp
   lengths (~60 chars) in the create/join/update endpoints.

**SQLite note.** SQLite is a single-writer database; with WAL mode (enabled at startup) concurrent
reads are fine and writes are serialised. For a friends-only game-night companion this is a non-issue
(a handful of devices), and it removes the whole class of "is my Postgres port exposed?" risk.

**Code-quality notes (non-blocking).** `CastVote` and `LockVote` are ~90% duplicate (extractable into a
shared apply-vote helper); the two `IsDevelopment()` blocks in `Program.cs` could merge; the cleanup
worker loads all sessions into memory per tick (fine at this scale). No correctness bugs were found in
the reviewed paths.
