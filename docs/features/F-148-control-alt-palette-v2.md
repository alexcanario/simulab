---
feature: F-148
epic: Foundation and identity
status: idea
board: 206
version: 1
---
# Control Alt Systems palette v2 in the theme and kit

Technical terms: [glossary](../glossary.md)

## Summary
The Control Alt Systems identity changed palette on 2026-10-22 (`_cas-design-system`, `tokens.json` version 2, handoff H-04). Simulab recorded the previous palette in F-125. This item takes the new palette to the theme (`SimulabTheme`), `docs/design/DESIGN.md`, `docs/design/identity.tokens.json`, F-125's `## Approved list`, the kit stylesheet and the brand files (keys symbol and app icon).

What changes for Simulab:
- Amber instead of orange: `#F5A020` in both themes, with the edge `#B87410`. Text and icons on amber and on teal are always navy (white fails contrast).
- Teal `#1A9E8C`; blue `#2478C5` (light) / `#47A0EB` (dark).
- Backgrounds: light `#F2F5FB`, `#E6ECF5`; dark `#0B2149`, `#12386F`, `#081938`.
- Navy `#0F3266` becomes a brand token: sidebar, top bar and primary button.
- Links `#1D64A8` (light) / `#8CC4F5` (dark).
- Error `#B83232` / `#FF9C94`: Simulab may adopt it or keep its own.
- The action button becomes the Ctrl + Alt + amber key shortcut, with a compact variant.

Out of scope: each screen's composition (F-126 to F-134) and the colour approval decisions, which are settled at `/agile:refine`.

## Start
- Depends on: F-125
- Waits on (to start): nothing
- Needed to validate: the owner, on screen, in both themes
- Model: unknown — settled at /agile:refine
- Suggested path: `/agile:refine` → `/agile:build`
- Parallel with: none. Do it before the screens F-126 to F-134, which inherit the colours.
