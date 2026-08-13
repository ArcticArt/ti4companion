# Third-party notices

TI4 Companion is MIT-licensed (see `LICENSE`, which is about the source code only). It ships the following
third-party components. Apache-2.0 asks that the licence and the notices travel with the work — this file is
where they do; the app's own footer credits the game-content sources instead.

## Google Material Symbols
- Source: https://github.com/google/material-design-icons
- Licence: Apache License 2.0 — https://www.apache.org/licenses/LICENSE-2.0
- How it is used: individual 24px outlined icon paths are **inlined** in
  `Ti4Companion.Web/Components/MaterialIcon.razor` (no font or image is fetched, so the app works on a LAN
  with no internet). The paths are unmodified.

## ZXing.Net
- Source: https://github.com/micjahn/ZXing.Net (NuGet package `ZXing.Net`)
- Licence: Apache License 2.0 — https://www.apache.org/licenses/LICENSE-2.0
- Copyright: ZXing authors; .NET port by Michael Jahn.
- How it is used: unmodified, as a compiled dependency. It decodes the join QR code in
  `Ti4Companion.Web/Components/QrScanModal.razor` so a phone can join by pointing its camera at the wall.

## Net.Codecrete.QrCodeGenerator
- Source: https://github.com/manuelbl/QrCodeGenerator
- Licence: MIT
- How it is used: unmodified, as a compiled dependency. It renders the join QR code (`JoinQr.razor`).

## Bootstrap
- Source: https://getbootstrap.com
- Licence: MIT
- How it is used: the stylesheet shipped with the Blazor template, loaded before the app's own `app.css`.

---

Game content and artwork are **not** covered by any of the above, nor by this project's MIT licence — see
`LICENSE` and the §Legal section of `README.md`. Twilight Imperium is © & ™ Asmodee North America, Inc.
