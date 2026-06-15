# Deploying TI4 Companion

No Docker, no database server. The app is a single self-contained ASP.NET Core process — it serves the
REST API, the Blazor WebAssembly client and the SignalR hub — and keeps everything in local **SQLite**
files. Run it as a **systemd** service behind a reverse proxy that terminates TLS.

| Piece            | Role                                                                        |
|------------------|-----------------------------------------------------------------------------|
| `ti4companion`   | the published app: API + Blazor client + SignalR hub (Kestrel, loopback)    |
| `ti4.db`         | SQLite sessions DB — created on first start                                 |
| `ti4master.db`   | SQLite master content DB — committed to the repo, copied into the data dir  |
| reverse proxy    | TLS termination + WebSocket upgrade (Caddy and Apache are both shown below) |

> The **.NET Aspire AppHost is for local development only.** In production you run the published
> `Ti4Companion.ApiService` directly as a service — no Aspire dashboard, no container.

Throughout this guide, replace `example.com` with your own domain and `SERVER` with your host.

## 1. Prepare the server

Any small Linux VM will do — the app is a single process with a couple of SQLite files, so 1–2 vCPU and
1–2 GB RAM are plenty.

```bash
# Only needed if you publish ON the server. The recommended path (step 2) produces a self-contained
# build, so the server needs no .NET runtime at all.
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --runtime aspnetcore

# sqlite3 is optional but worth having: it is what makes consistent online backups possible (step 6).
apt-get update && apt-get install -y sqlite3

# A reverse proxy. Caddy gets you automatic HTTPS with no extra config:
apt-get install -y debian-keyring debian-archive-keyring apt-transport-https curl
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | tee /etc/apt/sources.list.d/caddy-stable.list
apt-get update && apt-get install -y caddy
# ...or Apache, if the host already serves other sites through it (see step 4).
```

**Recommended hardening.** Expose only 22, 80 and 443. Use key-based SSH (`PasswordAuthentication no`,
root login `prohibit-password`) and something that throttles brute-force attempts. Everything the app
itself needs is covered in "Security notes" at the end.

## 2. Build & publish

Publish with the repo script **`publish.ps1`** (PowerShell; runs on your dev machine). It produces a
**self-contained linux-x64** build — so the server needs no .NET runtime — and fixes up the Blazor boot
script (see the gotcha):

```powershell
./publish.ps1            # output in ./publish
```

> ### ⚠️ Don't `dotnet publish` by hand for a deployment
> This app is served as plain static files (`UseBlazorFrameworkFiles`/`UseStaticFiles`), not via
> `MapStaticAssets`. With .NET 9+ Blazor WASM fingerprinting that means **`index.html`'s import map is
> never populated** and the boot script stays the literal `blazor.webassembly#[.{fingerprint}].js`
> placeholder → the boot chain 404s and the page spins forever with no error.
>
> Two things fix it, both already in the repo: `<WasmFingerprintAssets>false</WasmFingerprintAssets>`
> keeps the `dotnet.*` runtime files at stable names, and `publish.ps1` writes the real
> `blazor.webassembly.<hash>.js` name into `index.html`. A bare `dotnet publish` skips that rewrite.
> If you must publish by hand, do the rewrite yourself — or verify afterwards that no `#[` placeholder
> survives in `index.html`.

Create the directories and a non-root service user, then upload:

```bash
ssh SERVER 'adduser --system --group --no-create-home --home /opt/ti4companion ti4; \
            mkdir -p /opt/ti4companion /var/lib/ti4companion; chown ti4:ti4 /var/lib/ti4companion'
scp -r publish/* SERVER:/opt/ti4companion/          # first deploy: everything
ssh SERVER 'chmod +x /opt/ti4companion/Ti4Companion.ApiService; chmod -R a+rX /opt/ti4companion'
```

On first start the app creates `ti4.db` and applies the EF Core migrations to **both** databases.
**`ti4master.db` is NOT auto-created** — it is a committed content artifact, so copy it over before the
first start, or the app comes up with no game content (and says so in the log):

```bash
scp Ti4Companion.ApiService/ti4master.db SERVER:/var/lib/ti4companion/
ssh SERVER 'chown ti4:ti4 /var/lib/ti4companion/ti4master.db'
```

## 3. Run it as a systemd service

`/etc/systemd/system/ti4companion.service`:

```ini
[Unit]
Description=TI4 Companion
After=network.target

[Service]
User=ti4
Group=ti4
WorkingDirectory=/opt/ti4companion
# Self-contained publish → run the native host directly (no `dotnet` on the server).
ExecStart=/opt/ti4companion/Ti4Companion.ApiService
Restart=always
RestartSec=5
# Kestrel listens on loopback only; the reverse proxy handles TLS.
Environment=ASPNETCORE_URLS=http://localhost:5000
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=HOME=/var/lib/ti4companion
# NOTE THE QUOTES: systemd splits Environment= on spaces, and "Data Source=" contains one. Without them
# only "Data" reaches the app and it dies with "Format of the initialization string does not conform...".
Environment="ConnectionStrings__ti4db=Data Source=/var/lib/ti4companion/ti4.db"
Environment="ConnectionStrings__ti4masterdb=Data Source=/var/lib/ti4companion/ti4master.db"
# Auto-wipe windows for inactive sessions, in hours, counted from the LAST activity.
# These OVERRIDE appsettings.json — changing the repo default alone does nothing here.
Environment=Ti4__DefaultRetentionHours=2160
# A paused game is an interrupted match somebody means to resume, so it is kept longer.
Environment=Ti4__PausedRetentionHours=8760
# Optional: Web Push ("you're up"). Leave unset to disable the feature entirely — the client then hides
# it rather than offering a switch that cannot work. See step 7 for generating the keys.
# Environment=Ti4__Vapid__PublicKey=...
# Environment=Ti4__Vapid__PrivateKey=...
# Environment=Ti4__Vapid__Subject=mailto:you@example.com
# Optional: marks a non-production instance with a permanent badge on every screen.
# Environment=Ti4__InstanceLabel=TEST
# Hardening (the service runs as an unprivileged user)
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
systemctl status ti4companion            # should be active (running)
journalctl -u ti4companion -f            # watch the migrations for both DBs on first start
```

## 4. Put a reverse proxy in front

Point your domain's DNS A/AAAA record at the server **first** — both options below obtain a certificate
over HTTP, which needs the name to resolve already.

**Caddy.** The repo's [`Caddyfile`](Caddyfile) proxies everything to the loopback port; set your domain
and restart:

```bash
cp Caddyfile /etc/caddy/Caddyfile        # put your domain in place of {$SITE_ADDRESS}
systemctl restart caddy
```

**Apache**, if the host already serves other sites through it. Enable
`proxy proxy_http proxy_wstunnel headers rewrite`, then a vhost:

```apache
<VirtualHost *:80>
    ServerName example.com
    ServerAlias www.example.com          # be PRECISE here — see the certbot warning below
    DocumentRoot /var/www/html
    ProxyPass /.well-known/acme-challenge/ !     # let certbot serve the ACME challenge itself
    RewriteEngine On
    RewriteCond %{HTTP:Upgrade} =websocket [NC]
    RewriteRule ^/(.*) ws://127.0.0.1:5000/$1 [P,L]      # SignalR WebSocket
    ProxyPreserveHost On
    ProxyPass / http://127.0.0.1:5000/
    ProxyPassReverse / http://127.0.0.1:5000/
</VirtualHost>
```

```bash
a2ensite ti4companion
apache2ctl configtest      # ALWAYS before a reload
systemctl reload apache2
certbot --apache -d example.com -d www.example.com --redirect
```

Then open `https://example.com`:

- **Wall display:** open a session and go to `/display/<code>` (or use the in-app display link), then
  fullscreen that tab on the projector or TV.
- **Players:** share the 5-character join code, or let them scan the QR code the wall can show.

## 5. Updating

Always publish with `publish.ps1` (step 2), then:

```bash
systemctl stop ti4companion
# back up first — see step 6
scp -r publish/* SERVER:/opt/ti4companion/
systemctl start ti4companion
```

Migrations apply on start. The databases in the data directory are untouched by a redeploy. To ship
**updated game content**, copy the new committed `ti4master.db` into the data directory while the service
is stopped, and delete any stale `-wal`/`-shm` beside it.

> ### ⚠️ Keep the unit file in sync with step 3
> When you deploy code that introduces a new connection string or setting, add the matching
> `Environment=` line **before** starting the service. A version of this app that gained a second
> DbContext was once deployed onto a unit that only had the first connection string: the app fell back to
> a relative default, tried to create the file in a root-owned working directory, and died with
> `SQLite Error 14: 'unable to open database file'`. Nothing in the build warns you about this.

## 6. Backups

Durable state is two SQLite files: `ti4.db` (sessions) and `ti4master.db` (content — a committed artifact
you edit directly, so back it up too, especially after content edits). Use SQLite's online backup so the
copy is consistent even while the app runs:

```bash
sqlite3 /var/lib/ti4companion/ti4.db       ".backup '/var/backups/ti4-$(date +%F).db'"
sqlite3 /var/lib/ti4companion/ti4master.db ".backup '/var/backups/ti4master-$(date +%F).db'"
```

As a daily cron entry (`crontab -e`; note the escaped `%`):

```cron
0 4 * * * sqlite3 /var/lib/ti4companion/ti4.db ".backup '/var/backups/ti4-$(date +\%F).db'"
5 4 * * * sqlite3 /var/lib/ti4companion/ti4master.db ".backup '/var/backups/ti4master-$(date +\%F).db'"
```

To restore: stop the service, remove any stale `-wal`/`-shm`, drop the backup in place, start again.

```bash
systemctl stop ti4companion
rm -f /var/lib/ti4companion/ti4.db-wal /var/lib/ti4companion/ti4.db-shm
cp /var/backups/ti4-2026-01-31.db /var/lib/ti4companion/ti4.db
systemctl start ti4companion
```

For point-in-time recovery, [Litestream](https://litestream.io) can stream the SQLite WAL to
S3-compatible storage as a second systemd service.

## 7. A staging instance (optional)

A second instance on the same host is worth having if you want to try features on real devices without
touching live games — **Web Push especially**, because a subscription is bound to the origin and
therefore cannot be tested on a different host or under a path prefix.

Run it as a **separate service with its own databases**, so test games can never mix with real ones:

| | Production | Staging |
|---|---|---|
| URL | `https://example.com` | `https://staging.example.com` |
| systemd service | `ti4companion` | `ti4companion-staging` |
| Kestrel | `127.0.0.1:5000` | `127.0.0.1:5001` |
| App directory | `/opt/ti4companion` | `/opt/ti4companion-staging` |
| Data directory | `/var/lib/ti4companion` | `/var/lib/ti4companion-staging` |
| Retention | long | short — test games are throwaway |

Copy the unit from step 3, change those values, and add `Ti4__InstanceLabel=TEST` so every screen of the
staging instance carries a badge — a pixel-perfect copy of your live site is otherwise very easy to
mistake for it. Copy `ti4master.db` into the new data directory before the first start (step 2).

If the staging site is publicly reachable, keep crawlers out so an indexed duplicate cannot compete with
the real site in search results. Serve a `Disallow: /` robots file from the proxy rather than the app —
the app's own `robots.txt` is the production one:

```apache
ProxyPass /robots.txt !
Alias /robots.txt /var/www/staging-robots.txt
```

> ### ⛔ certbot can hijack the certificate of an existing site
> If a vhost carries a **wildcard** `ServerAlias *.example.com`, then `certbot --apache -d
> staging.example.com` will match **that** vhost and rewrite **its** `SSLCertificateFile`/`KeyFile` to the
> new certificate. The result: your main site serves a certificate for the staging name, every browser
> refuses it, and the site is effectively down.
>
> Routing is not the issue — an exact `ServerName` beats a wildcard alias — but that says nothing about
> which vhost certbot picks to *install into*. And `apache2ctl configtest` reports **Syntax OK** for a
> wrong-but-valid certificate path, so it cannot catch this either.
>
> **Avoid it:** never leave a wildcard `ServerAlias` on a vhost that owns a real certificate (name the
> hosts precisely), give the staging site its **own** `:443` vhost, and after any certbot run check the
> certificate that is actually **served**:
>
> ```bash
> echo | openssl s_client -servername example.com -connect example.com:443 2>/dev/null \
>   | openssl x509 -noout -subject -ext subjectAltName
> ```
>
> Finish with `certbot renew --dry-run` — a renewal runs the installer again.

### Generating VAPID keys for Web Push

Generate them **on the server** so the private half never travels, and keep it in the unit file only —
never in a committed `appsettings.json`:

```bash
umask 077
openssl ecparam -genkey -name prime256v1 -noout -out /root/vapid.pem
openssl pkey -in /root/vapid.pem -noout -text     # -> priv: (32 bytes) / pub: (65 bytes, starts 04)
```

Base64url-encode both raw values: the public key is the uncompressed point (`0x04 ‖ X ‖ Y`, 65 bytes),
the private key is the raw scalar (32 bytes — strip a leading `00` byte if openssl printed 33). Put them
in `Ti4__Vapid__PublicKey` / `Ti4__Vapid__PrivateKey`. With no keys configured the feature is simply off.

## Notes

- **Auto-wipe.** Inactive sessions are deleted once they have been idle for their `RetentionHours`,
  counted from the **last activity**, so any interaction resets the clock. A **paused** game gets the
  longer paused window; a stored `RetentionHours = 0` means "never wipe". The worker runs every 15 minutes
  and logs the active windows once at startup, so
  `journalctl -u ti4companion | grep "cleanup active"` tells you what is really in force.
  ⚠️ The unit's `Environment=` lines override `appsettings.json`, and the value is **stamped onto each
  session at creation** — so existing rows keep their old window unless a migration raises them.
- **Finished games** are additionally kept as a small permanent summary (rounds, players, duration,
  winner) that survives the auto-wipe. It holds no IP addresses and no device tokens, and there is
  deliberately no HTTP endpoint to read it — the summaries contain player names.
- **Editing game content.** All reference content lives in the committed
  `Ti4Companion.ApiService/ti4master.db`. Edit it with any SQLite tool, commit it, and copy it to the data
  directory on the next deploy (step 5). Session data is untouched.
- **Faction icons.** Drop `*.png` files into `Ti4Companion.Web/wwwroot/factions/` (see the README there);
  until then a generated colour-and-initials badge is shown. Rebuild and republish afterwards.

## Security notes

The app is designed for a friends-only game night but is safe to host publicly. Summary of what is in
place and why:

**Auth model, by design.** Device tokens only, no accounts. The host is the device that created the
session; host / self / active-player / current-picker rules are enforced server-side from the
`X-Device-Token` header. A device with no token is a read-only spectator. Scoring objectives, casting
votes and switching the wall display are intentionally open to any joined device.

**What was already solid.** No SQL injection (everything is parameterised EF Core LINQ; the only raw SQL
is a static `PRAGMA` at startup and static migrations). No secret leakage — device tokens are 128-bit
GUIDs and never appear in a response DTO. No CSRF, because auth is a custom header rather than a cookie
and the client is same-origin. TLS is delegated to the proxy, and Kestrel binds loopback only.

**Hardening for a public URL.**
- **Rate limiting** per client IP: session creation is capped at 20 per 10 minutes, session reads at 600
  per minute (on the session route **and** the log route — otherwise the log route is an unmetered
  enumeration oracle). Rejections are 429 and the client treats them as a graceful no-op.
- **Input caps** on every user-supplied string: free text 60–100 characters, loose content id references
  60, and `ColorHex` must be a plain CSS hex — which also closes a **CSS-injection** vector, since the
  colour is interpolated into inline `style` attributes.
- **Scoring validates** that the scorer is a real player in that session, so an anonymous caller cannot
  insert score rows for random GUIDs.
- **Face-down votes are redacted server-side** until the host reveals them: while a hidden vote runs, each
  vote's outcome, weight and choice are stripped from the DTO and only "locked" survives. In the
  intermediate "totals only" stage the per-player rows stay redacted and the aggregate is computed on the
  server, so the attribution never leaves it.
- **Web Push** stores one row per browser subscription, keyed by the push endpoint URL. The rows are kept
  out of the session graph so their encryption keys cannot ride along into a DTO, and are deleted with the
  session.

**Residual risks, accepted.** Join codes are 5 characters from a 30-symbol alphabet (~24M combinations);
bulk scanning is impractical under the read limit, but somebody who can *see the wall* can join and
disrupt. That is the right trade-off for a companion app you point at a projector.

**SQLite note.** SQLite is single-writer; with WAL mode (enabled at startup) concurrent reads are fine and
writes serialise. For a handful of devices at one table this is a non-issue, and it removes the whole
class of "is my database port exposed?" question.
