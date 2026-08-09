# Changelog

Player-facing changes to [TI4 Companion](https://ti4companion.com). One line per change, newest first.

## Unreleased

- Games are now kept for **90 days** after the last activity instead of 7, so a session survives the gap between game nights.
- A **paused** game is kept for a **full year**, so an interrupted match can still be resumed much later.
- The browser tab icon now shows up everywhere, because the site finally ships a classic `favicon.ico` next to the PNG one.
- Added a `robots.txt` so search engines index the start page and leave the per-game session URLs alone.
- Updated the bundled SQLite database engine to close a security advisory (CVE-2025-6965).

## 2026-07-14

- Fixed the "copy invite link" and "copy join code" buttons, which silently did nothing on Safari and iPad.
- Starting a game now asks you to confirm the seating order first, so a wrong seat is caught before round 1.
- The app has its own icon in the browser tab and on the home screen instead of the default framework one.
- The project is now open source under the MIT licence, with voluntary donations welcome toward the server costs.
- Hardened the public server with per-IP rate limits and input length caps, and face-down agenda votes are now hidden from the API until the host reveals them.
