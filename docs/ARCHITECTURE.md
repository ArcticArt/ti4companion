# TI4 Companion — Architecture & Developer Guide

Technical documentation for contributors. For a general introduction and how to run the app, see the
[README](../README.md); for contribution rules see [CONTRIBUTING.md](../CONTRIBUTING.md); for server
setup see [DEPLOY.md](../DEPLOY.md).

## Overview

TI4 Companion is a web companion for Twilight Imperium 4th Edition game nights: a shared overview is
projected on a wall (the "beamer" / display view) while every player controls the game state live from
their phone or tablet. All devices stay in sync in real time.

**Stack:** .NET 10 · ASP.NET Core minimal APIs · SignalR · EF Core (SQLite) · Blazor WebAssembly (PWA)
· .NET Aspire (local dev orchestration only). The UI is bilingual (English/German).

The app ships as a **single self-contained server process**: the ASP.NET Core API also serves the
published Blazor client (hosted model, same origin — no CORS) and the SignalR hub. There is no external
database server and no Docker; all data lives in two local SQLite files.

## Solution layout

```
Ti4Companion.AppHost          .NET Aspire orchestration — local dev entry point (F5)
Ti4Companion.ServiceDefaults  Telemetry / health checks / resilience defaults
Ti4Companion.ApiService       REST API + SignalR hub + EF Core + background cleanup;
                              serves the published Blazor client
Ti4Companion.Web              Blazor WebAssembly client (PWA): control views + wall display
Ti4Companion.Shared           DTOs, request records and enums shared by API and client
```

The solution file is `Ti4Companion.slnx` (the XML solution format).

## The two databases

| File           | DbContext         | Contents                              | In git? |
|----------------|-------------------|---------------------------------------|---------|
| `ti4.db`       | `Ti4DbContext`    | Runtime session state (games, players, votes, log) | no (created on first run) |
| `ti4master.db` | `MasterDbContext` | Versioned game reference content (cards, factions, planets, …) | **yes — committed artifact** |

Connection strings: `ConnectionStrings:ti4db` (default `Data Source=ti4.db`) and
`ConnectionStrings:ti4masterdb` (default `Data Source=ti4master.db`). Both databases are migrated on
startup; both run in WAL mode. The master DB is **read-only at runtime** (plus a small startup cache of
faction initiative overrides in `FactionInitiative`); if it comes up empty the app logs a warning.

Because there are two DbContexts, **every `dotnet ef` command needs `--context`**:

```bash
dotnet ef migrations add <Name> --project Ti4Companion.ApiService --context Ti4DbContext    -o Data/Migrations
dotnet ef migrations add <Name> --project Ti4Companion.ApiService --context MasterDbContext -o Data/MasterMigrations
```

To reset local test sessions: stop the app and delete `ti4.db` (+ `-wal`/`-shm`). To discard content
edits: `git checkout -- Ti4Companion.ApiService/ti4master.db`.

## Master content model

All game reference content lives in `ti4master.db` (entities in `Data/Master/MasterEntities.cs`). There
are no seed files or importers — **the committed DB file is the source of truth**; edit it directly with
any SQLite tool and commit the changed file. If your tool opened it in WAL mode, checkpoint before
committing so the change is in the main file.

**Versioning.** Most content entities implement `IMasterContent`:

- a surrogate `Guid Id` primary key (internal to the DB),
- a **logical key** — a `Slug` for most content, the card `Number` for strategy cards,
- a `Version` that counts up across reprints, and a `Source` (`Base`, `ProphecyOfKings`, `Codex1–4`,
  `ThundersEdge`) plus a coarse `Expansion` flag derived from it (used for the client's
  active-expansion filtering).

A logical item can have several revision rows (e.g. the base printing of a card and its Ω/ΩΩ reprints)
sharing one logical key, unique on (logical key, `Version`). The API always serves the **newest version
per logical key** (`ContentEndpoints.Latest<T>`); older printings stay in the DB as history. DTOs expose
the logical key as their `Id`, so all references (from session data or between content rows) are loose
string references to the slug.

Exceptions that are *not* versioned (`IMasterContent`): `SystemTile` (keyed by the printed tile number,
e.g. `"01"`, `"82A"`), `Breakthrough` (Thunder's Edge only, one per faction), `TypeValue` (a bilingual
label lookup for every content enum value), and the `FactionStartingUnit` join rows.

**Bilingual content.** Content rows carry English text plus `*De` columns; German falls back to English
where a translation is missing. Planet names are intentionally not translated.

**Unit stats are structured.** Units and unit-upgrade technologies carry structured stat columns
(`Cost`/`ProducedCount`/`Combat`/`CombatDice`/`Move`/`Capacity`) and their ability keywords
(SUSTAIN DAMAGE, BOMBARDMENT, …) as atomic `UnitAbilityEntry` child rows — nothing is parsed out of
card text at runtime. `Text`/`TextDe` hold only the prose. The client localizes keyword names and stat
labels for display.

**The one hard rule:** never invent or paraphrase game text. Anything printed on a TI4 component must be
transcribed verbatim from a verified source — the physical cards, the
[TI4 wiki](https://twilight-imperium.fandom.com), or [ti4lookup](https://github.com/bern/ti4lookup)
(the primary structured-data source; faction/tech slugs follow its data set). If something isn't
available from a verified source, leave it out and open an issue.

## Session domain model & rules

Session entities live in `Data/Entities.cs`; the rules are enforced server-side in
`Endpoints/SessionEndpoints.cs` and `Services/TurnService.cs`.

**Identity & authorization.** There are no accounts. Each device generates a persistent token and sends
it as the `X-Device-Token` header on every request; the server resolves it to a player in the session.
The session creator is the **host** (`Player.IsHost`, stable across seat changes). Rules:

- Host only: phase/round changes, seat order & speaker, revealing/removing objectives, session
  settings, jumping the active player, removing other players, pause/resume, agenda control.
- Active player or host: playing a strategy action, passing, advancing the turn.
- Current picker or host: picking a strategy card.
- Self or host: profile edits, technologies, agenda influence, casting/locking a vote.
- Any joined device: scoring objectives (validated to be a real session member) and switching the wall
  display mode. A device with no token is a read-only spectator (that's how the wall display works).

Rejected calls return 403/400; the client treats both as "refresh to authoritative state", never as an
exception.

**Game flow.** Phases: Setup → Strategy → Action → Status → (Agenda) → Strategy (next round).

- **Setup** runs as four steps for the host — session & options → players → seating & speaker → objectives —
  while everyone else always sees the player step, because a joiner has to be able to pick a faction and ready
  up whenever they arrive. Players pick faction and colour and mark themselves ready (pickers hide options
  other players already hold); the host arranges the seat order (drag or ▲/▼), says who the speaker is, and
  records the two starting public objectives by hand. **The start button is the last thing on the last step**,
  so the walk through seating and objectives *is* the confirmation — there is no separate "is this right?"
  dialog, and the seating step will not let the host past without a speaker. Faction changes reconcile
  starting technologies automatically. Maximum 8 players (server-enforced).
- **Joining:** players join with a 5-character code or an invite link, and can either create a new seat
  or **take over** an existing one — **including the host's**, which is what lets a host who cleared their
  browser or switched device pick the role back up instead of leaving the table unable to change phases.
  The join code is the only thing guarding that, and a device holding it can already score and vote; the
  take-over is written to the match log so the table can see it. The claiming device gives up any other
  seat it held (one device, one seat).
- **Coming back:** each device remembers the sessions it has played in `localStorage` — the last ten with
  the session name, the code and **which player it was there**. That last part is the reason the record
  exists: the device token identifies the device and is shared by every session it plays, so a single
  stored player id could only ever describe one game. Leaving a session keeps its entry; an entry whose
  code the server no longer knows removes itself on the next attempt.
- **Strategy:** cards are picked in order — speaker first, then clockwise by seat; with ≤4 players
  everyone picks 2 cards. Unpicked cards gain 1 trade good per round; goods are settled when the action
  phase starts. Initiative = lowest held card number (the Naalu Collective is always 0).
- **Action:** turns run in initiative order. Playing a strategy action highlights the card on the wall;
  the highlight clears on the next turn change. A player may pass only once all of their strategy cards
  are exhausted; the round can end only when everyone has passed. Whoever is up — or the host, who may always
  act for them — can declare a **combat** and record a **technology**, both as popups on the player card.
  Two things stop that player's clock while they are open, and both are the same mechanism: a logged
  start/end interval that `MatchStats` subtracts from **time on turn only** (not from the round or the match).
  A battle with someone else is not their thinking time, and neither is hunting for a technology in a list —
  but both are still time the table spent playing. Because a popup can be walked away from, the server closes
  either one at every turn and phase boundary.
- **Status:** objectives are revealed (searchable picker) and scored; custom/secret-made-public
  objectives can be added by hand.
- **Optional table variants** (all off by default — the app never forces a rule). The **Red Tape**
  community variants are the one deliberate exception to "the app tracks, it does not enforce": a table
  that chose one gets its rules applied server-side in `Services/RedTape.cs`, so a taped objective simply
  cannot be scored. Two of those rules would otherwise take something away from the table irreversibly —
  purging the Stage I objectives left over once five are clear, and pulling a tape at random in a round
  where nobody took the carrier card. **Both are questions, not events:** the server marks them as
  *proposed* and the host confirms in a dialog, and only that answer changes anything. The purge proposal
  is flagged per objective at the moment it is raised, which is what keeps an objective revealed *later*
  out of a pending purge; the random question stores the round it belongs to, because the moment it is
  raised in can end before anyone answers it. The variant's *timing* rules can also be **overruled** by the
  host through an explicit dialog (and the override is logged) — the app enforces them so nobody has to
  remember them, but the table remains the authority on its own game. A purged objective is the exception:
  that is not a lock, it is out of the game.
- **Agenda:** a small state machine driven by `CurrentAgendaId` + `VotingStarted` + `AgendaVotesHidden`:
  1. *Influence entry* — every player enters their available influence (non-hosts see only their own).
  2. *Agenda revealed* — the host picks an agenda and starts the vote, open or **face-down**.
  3. *Voting* — each player builds a local draft and commits it with a single **lock** call; the choice
     reaches the server only on lock, so nobody (not even the host) can peek at a face-down vote. While
     a face-down vote runs, the server additionally **redacts vote contents in the state DTO** (only
     the "locked" flag is visible). The host may reveal only after everyone has locked.
  4. *Results* (tally, elect winner, passed/rejected/speaker decides) are shown **only on the wall**.
  Voting order is seat order with the speaker last (the Argent Flight always votes first). Clearing the
  agenda deducts each player's locked votes from their influence and returns to influence entry.
- **Pause:** the host can pause the game; an endpoint filter rejects every mutation with HTTP 423 while
  paused, the clients show a lock overlay, and the paused interval is excluded from all statistics.
- **Match log & statistics:** every meaningful mutation writes a structured `SessionLogEntry`. The
  client derives match/round/phase/per-player durations from the log timeline (`MatchStats`); the host's
  "End game" action switches the wall to a statistics view with charts. Because those figures come from the
  `RoundChange`/`PhaseChange` entries and nothing else, **anything that moves the round or the phase has to
  log it** — including a host correcting either by hand in the settings, which is easy to forget and leaves
  a round missing from the statistics with its time absorbed by the round before it. Such a correction can
  also point backwards, so the per-round figures are aggregated by round *number* rather than per entry.
- **Retention:** inactive sessions are wiped automatically after `Ti4:DefaultRetentionHours`
  (server-side background worker).

## API surface

Everything lives under `/api/sessions` (mutations broadcast a SignalR `SessionChanged` event and return
the full updated session state) plus one content endpoint:

- Lifecycle: `POST /` · `GET /{code}` · `GET /{code}/log` · `PATCH /{id}` · `DELETE /{id}` ·
  `POST /{id}/display` · `POST /{id}/pause` · `POST /{id}/resume`
- Phases/rounds: `POST /{id}/phase/{start|action|status|agenda}` · `POST /{id}/round/next`
- Turn: `POST /{id}/active-strategy` · `POST /{id}/turn/{active|advance|previous}`
- Players: `POST /{id}/players` (join / take over) · `PATCH|DELETE /{id}/players/{pid}` ·
  `POST /{id}/players/{pid}/pass` · `POST /{id}/players/{pid}/influence`
- Strategy cards: `POST /{id}/players/{pid}/strategy-cards` · `DELETE …/{cardId}` ·
  `POST …/{cardId}/used`
- Objectives: `POST /{id}/objectives` · `POST /{id}/objectives/custom` · `DELETE …/{soid}` ·
  `POST …/{soid}/scores` · `DELETE …/scores/{pid}`
- Technologies: `POST /{id}/players/{pid}/technologies` · `DELETE …/{techId}`
- Agenda: `POST /{id}/agenda` (reveal/clear) · `POST /{id}/agenda/{start|cancel|reveal|lock}`
- Content: `GET /api/content` — one bundle with the newest revision of every content table.
- Hub: `/hubs/session` (one group per join code; single event `SessionChanged`).

Enums travel over the wire **as numbers** (no string conversion) — keep numeric enum values stable.

## Client architecture

- `Services/SessionStore.cs` is the single client state holder: session state, device identity, the
  SignalR connection (auto-reconnect, group switching), and content lookups. Mutations follow one
  pattern: `Store.Mutate(Store.Api.XxxAsync(...))` — a rejected call (400/403/423/429) resolves to
  `null` and triggers a refresh instead of throwing.
- `Services/Ti4ApiClient.cs` is the typed REST wrapper; it attaches the device-token header.
- `Localization/Loc.cs` holds all UI strings as EN/DE pairs (`Loc["key"]`, `Loc.Pick(en, de)`); the
  language toggle persists per device.
- Components inherit `Ti4ComponentBase` (re-renders on store/language changes).
- **Anything clickable is a real control**, never a `<div>` with a click handler: a styled div is
  unreachable by keyboard and shows up as nothing in the accessibility tree. Tab strips are `<button>`s in a
  `role="tablist"` with `aria-selected` and a `role="tabpanel"`; cards that act as buttons carry
  `role="button"`, `tabindex` and Enter/Space handling. Styling a `<button>` back to a flat look needs
  `appearance: none; background: none; font-family: inherit`.
- Pages: `Home` (create/join, `/join/{code}` invite links), `Session` (`/s/{code}` control view with
  Phase/Players/Objectives/Tech tabs), `Display` (`/display/{code}` — the full-screen wall).
- The wall display has three player-switchable modes (Objectives / Secondary abilities / Tech overview)
  plus a Statistics mode reachable only through the host's "End game" action. During the agenda phase it
  shows the voting arc ("galactic council") instead.
- **Updates.** The app is a PWA, and its service worker answers navigations from its own cache. It
  deliberately does **not** call `skipWaiting()`: activating a new worker clears the cache the running
  version is still reading from, and its fingerprinted framework files are gone from the server after a
  deploy, so a game open on somebody's phone would break mid-update. Consequence: a deploy does not reach
  an open browser by itself. `Components/UpdateNotice.razor` therefore shows a bar when a new worker is
  waiting and offers a reload that hands over properly (it posts `SKIP_WAITING`, waits for
  `controllerchange`, then reloads) — and says "close every tab" instead when the waiting worker is old
  enough not to answer that message.

## Security & public hosting

The threat model and hardening are documented in detail in
[DEPLOY.md → Security review](../DEPLOY.md#security-review--public-hosting-hardening). Highlights:

- All queries are EF Core LINQ (parameterised); device tokens are never exposed in DTOs; auth is a
  custom header (no cookies → no CSRF); TLS terminates at the reverse proxy.
- Per-IP **rate limits**: session creation 20/10 min; the unauthenticated by-code reads
  (`GET /{code}`, `GET /{code}/log`) 600/min. `UseForwardedHeaders` makes the real client IP visible
  behind the local reverse proxy. Rejections use HTTP 429, which the client degrades on gracefully.
- All user-supplied strings are bounded server-side: free text is trimmed and length-capped, loose
  content ids are capped, and player colours must be plain CSS hex values (they're interpolated into
  inline styles — anything else would be a CSS injection vector).
- Face-down votes are redacted server-side until revealed (see the agenda flow above).

## Development

**Run:** `dotnet run --project Ti4Companion.AppHost` (Aspire dashboard) or run the API directly:
`dotnet run --project Ti4Companion.ApiService` (listens on `http://localhost:5116`). Only the .NET 10
SDK is required. To test from a phone on the same Wi-Fi, use the `lan` launch profile and open a
firewall port — see the README.

**Build/verify:** `dotnet build Ti4Companion.slnx`. For API testing without a browser, drive the REST
endpoints directly (create a session, keep the returned `deviceToken`, send it as `X-Device-Token`).

**Deployment** is a self-contained linux-x64 publish behind a reverse proxy — see
[DEPLOY.md](../DEPLOY.md). Always publish with `./publish.ps1`, not bare `dotnet publish`: the app
serves the client as plain static files, so the script pins non-fingerprinted runtime names and writes
the real fingerprinted `blazor.webassembly.<hash>.js` into `index.html` (a bare publish leaves a broken
placeholder and the page never boots).

## Implementation notes & gotchas

- **Culture-sensitive number formatting in generated CSS/SVG.** Interpolating a `double` into a style
  string uses the current culture — a German browser culture emits `1,5rem`, which is invalid CSS and
  silently ignored. Always format with `FormattableString.Invariant($"…")`.
- **EF Core Guid keys.** Every entity sets its `Guid Id` in the initializer, and keys are configured
  `ValueGeneratedNever()`. Without this, attaching a new child to a tracked graph makes EF emit an
  UPDATE instead of an INSERT (`DbUpdateConcurrencyException`). Follow the same pattern for new
  entities (and register new master-content entities in `MasterDbContext.OnModelCreating` with the
  unique (logical key, Version) index).
- **SQLite + EF quirks.** SQLite can't `ORDER BY` a `DateTimeOffset` in SQL — materialise first, then
  sort in memory. `List<string>` properties map to JSON text columns (EF primitive collections).
- **Razor quirks.** `title="@Loc["x"]"` breaks on the nested quotes — use single-quoted attributes
  (`title='@Loc["x"]'`) or a code property. In a Razor `@switch`, each case's markup and `break;` must
  be on their own lines. Don't pass conditional `EventCallback` lambdas via ternary — use an explicit
  `bool` gate parameter.
- **Multi-line text on cards.** Ability texts store bullet points separated by `\n`; components render
  them by splitting on `\n` (★/✦ bullets).

## Assets

- `wwwroot/factions/*.png` — faction symbols (community-vectorized set, see the credits in the app
  footer). `wwwroot/portraits/*.jpg` — faction portraits for the agenda wall. `wwwroot/units/*.png` —
  unit silhouettes, mapped by `UnitType` (capitalised file names).
- `favicon.png` / `icon-192.png` / `icon-512.png` — original generated art (gold hexagon + four-point
  star in the app palette).
- Twilight Imperium content and artwork are © & ™ Asmodee North America, Inc. (Fantasy Flight Games) —
  see the [LICENSE](../LICENSE) scope note. The MIT license covers the source code only; keep the
  project strictly non-commercial.
