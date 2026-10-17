---
feature: F-125
epic: Foundation and identity
status: idea
board: 176
version: 1
---
# Control Alt Systems identity in the theme and the UI kit

Technical terms: [glossary](../glossary.md)

## Summary
Every screen takes the Control Alt Systems identity in one change of `SimulabTheme`, the UI kit and the gallery `/dev/ui`: the palette of the screen the owner approved (page `#F4F6F9`, ink `#0B1B2E`, blue `#0872B8`, green `#00806B`, one orange `#E8650A` action per screen), Bricolage Grotesque and JetBrains Mono, keycap-shaped buttons, bordered cards with no soft shadow, a white app bar and the sidebar kept in Simulab's navy `#0F3266`. Field icons and placeholders stay those of the current kit. Source: `_cas-design-system/design-system/tokens.json` and its README, the Stitch reference screen of the F-95 question form as adjusted on 2026-10-17, and D-4. It also absorbs F-83 (the literal colours of `wwwroot/app.css` come from the tokens). It records the brand decision ADR-0001 decision 30 left under "Revisit when" and updates `docs/design/identity.tokens.json` and `DESIGN.md`.

## Start
- Depends on: nothing
- Waits on (to start): nothing
- Needed to validate: the owner, on screen, in both themes
- Model: unknown — settled at /agile:refine
- Suggested path: `/agile:refine` → `/agile:screen` (gallery and one reference screen) → `/agile:build`
- Parallel with: none — F-126 to F-134 depend on it
