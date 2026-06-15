# Faction icons

The app shows a generated badge (faction colour + initials) for every faction out of the box —
no files are required here.

To use real artwork for a faction, drop a square image into this folder named after the
faction's **slug**, e.g.:

```
naalu.png       → The Naalu Collective
sol.png         → The Federation of Sol
crimson.png     → The Crimson Rebellion
```

The slugs are the `id` values in `Ti4Companion.ApiService/Data/Seed/factions.json`, and each
faction's `iconPath` already points at `factions/<slug>.png`. If the file is missing the badge is
shown instead; as soon as the file exists it is used automatically (no rebuild of the seed needed
— just rebuild/redeploy the Web project so the static file is published). PNG or SVG both work;
if you use SVG, change the faction's `iconPath` extension to `.svg`.

> Please use artwork you have the rights to. Official Fantasy Flight Games faction symbols are
> copyrighted and are intentionally **not** bundled with this project.
