# Contributing to TI4 Companion

Thanks for your interest! This is a free, non-commercial fan project — contributions are welcome,
from bug reports to translations to features.

## Getting started

All you need is the **.NET 10 SDK** — no Docker, no database server. See the
[README](README.md#run-locally) for how to run the app locally. The solution file is
`Ti4Companion.slnx` (the new XML solution format; open it with Visual Studio 2026 / a current IDE,
or just use the `dotnet` CLI).

For a tour of the codebase — the two databases, the content versioning model, the session domain
rules, and the known implementation gotchas — read the
[architecture & developer guide](docs/ARCHITECTURE.md) first.

## Project layout

```
Ti4Companion.AppHost          .NET Aspire orchestration (local dev entry point)
Ti4Companion.ServiceDefaults  telemetry / health / resilience defaults
Ti4Companion.ApiService       REST API + SignalR hub + EF Core; serves the published client
Ti4Companion.Web              Blazor WebAssembly client (PWA)
Ti4Companion.Shared           DTOs / enums shared by API and client
```

There are two SQLite databases with **two separate EF Core DbContexts**:

- `ti4.db` (`Ti4DbContext`) — runtime session data, created on first run, gitignored.
- `ti4master.db` (`MasterDbContext`) — the versioned game reference content, **committed to git**.

Because of the two contexts, every `dotnet ef` command needs `--context`:

```bash
dotnet ef migrations add <Name> --project Ti4Companion.ApiService --context Ti4DbContext -o Data/Migrations
dotnet ef migrations add <Name> --project Ti4Companion.ApiService --context MasterDbContext -o Data/MasterMigrations
```

## The one hard rule: never invent game content

Card, technology, ability, leader, planet and tile text — anything printed on a TI4 component —
must be **transcribed verbatim from a verified source** (the physical cards, the
[TI4 wiki](https://twilight-imperium.fandom.com), or [ti4lookup](https://github.com/bern/ti4lookup)).
Do not paraphrase, summarise, or reconstruct from memory. If a piece of content isn't available
from a verified source, leave it out and open an issue instead.

Game content lives in `Ti4Companion.ApiService/ti4master.db`. Edit it directly with any SQLite tool
(e.g. [DB Browser for SQLite](https://sqlitebrowser.org/)) and commit the changed file. Content rows
are versioned (logical id + `Version` + `Source`); the API serves the newest revision per id, and old
printings (Ω revisions) are kept for history. If the DB was opened in WAL mode, checkpoint it before
committing so the change is in the main file.

## Translations

The UI is bilingual (English/German) via `Ti4Companion.Web/Localization/Loc.cs`. Game content
carries `*De` columns in the master DB; German falls back to English where a translation is missing.
German game text must come from the printed German cards — same verbatim rule as above.

## Code style

- Match the surrounding code — this codebase favours small components, records for DTOs, and
  minimal-API endpoint groups.
- Format culture-sensitive numbers in generated CSS/SVG with `FormattableString.Invariant` —
  a German browser culture turns `1.5rem` into the invalid `1,5rem` otherwise.
- Verify with `dotnet build Ti4Companion.slnx` before opening a PR.

## Legal

By contributing you agree that your **code** contributions are licensed under the repository's
[MIT license](LICENSE). Twilight Imperium game content and artwork remain © & ™ Asmodee North
America, Inc. (Fantasy Flight Games) — see the license file's scope note. Please keep the project
strictly non-commercial: no ads, no paywalls, no selling.
