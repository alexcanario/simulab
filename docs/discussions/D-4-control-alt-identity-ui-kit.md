---
discussion: D-4
status: decided
date: 2026-10-17
---
# Control Alt Systems identity in the UI kit and the screens

Technical terms: [glossary](../glossary.md)

## Idea
Bring the Control Alt Systems identity (fonts, keycap-shaped buttons, one orange action per screen, bordered cards)
into the UI kit of every screen, keeping the navy sidebar and the current field icons and placeholders. The reference
screen redesigned in Stitch also brought actions and data the other screens do not have; each one needs a decision.

## What we already know
- Reference screen: the F-95 question form, redesigned in Stitch (project "Simulab - F-95 Questions (CAS identity)")
  from a DESIGN.md written from `_cas-design-system/design-system/tokens.json` and its README, then adjusted by hand
  on 2026-10-17: sidebar back to Simulab's `#0F3266`, field icons and placeholders back to those of the F-95 mockup.
  The owner approved that version on screen.
- D-2 (2026-09-25) decided "kit, gallery and every screen in one item" and parked the own brand (option C);
  ADR-0001 decision 30 lists "Simulab has its own brand (theme)" under "Revisit when". The Control Alt Systems
  identity is that brand decision, so it needs a new ADR.
- Colours and controls live in one place: `SimulabTheme` and `src/Hosts/Simulab.Web/Components/Ui/` (about 70 files).
  Changing the kit changes the 33 routed pages at once; a per-screen item cannot change a colour on its own without
  breaking `ui.md` (a pattern is defined once, in the kit or the theme).
- `ThemeContrastTests`, `ThemePaletteTests` and `IdentityTokensTests` hold today's values; every new token must pass
  them in both themes (WCAG 2.2 AA, `ui-project.md`).
- Palette of the approved mockup, against today's theme:

  | Role | Today (Simulae) | Approved mockup (Control Alt Systems) |
  |---|---|---|
  | Page background | `#F2F5FB` | `#F4F6F9` |
  | Text / primary button | `#19243E` / blue button `#216DB5` | `#0B1B2E` / ink button |
  | Blue (links, info) | `#216DB5` | `#0872B8` |
  | Secondary / success | teal `#1A9E8C` | green `#00806B` |
  | Highlight | amber `#F5A020` | orange `#E8650A`, once per screen |
  | Sidebar | `#0F3266` | `#0F3266` (kept at the owner's request) |
  | App bar | `#0F3266` | white `#FFFFFF` |
  | Fonts | system (Segoe UI / Roboto) | Bricolage Grotesque + JetBrains Mono |

- The Stitch screen invented items with no business behind them (Review Queue, Taxonomy, autosave, Author,
  Pool Target, notifications, global search, "peer sign-off"). They are not part of the identity and are not
  captured.

## Options
| Option | How it works for the user | Effort | Risks / what it rules out |
|---|---|---|---|
| A — Foundation, then screens | One item changes tokens, fonts, keycap buttons and the gallery: all 33 screens get the look at once. Then one item per screen (or area) only for the new composition and actions | M + S each | For a while the older screens carry the new look on the old layout |
| B — Screens only | Each screen takes its own style | L (33 items) | Breaks `ui.md` (one pattern, defined once); two looks side by side during the transition |
| C — Foundation only (as D-2) | New look everywhere, same layout | M | The new actions and composition stay out |

Recommendation: A — the only option where a colour or a control changes in one place, and it still gives each screen
its own item, as the owner suggested.

## Decisions
- 2026-10-17 — Take option A: one foundation item for the theme, the kit and the gallery, then screen items for
  composition and actions — colours and controls live in the kit, so they change once for every screen.
- 2026-10-17 — Screen items: one per complex form (question, exam, exam edition, base text) and one per area for the
  rest (admin, catalog, question bank lists, identity, home and errors) — this replaces D-2's "every screen in one
  item" for the composition work only; the look itself still changes in one item.
- 2026-10-17 — Palette: the Control Alt Systems tokens of the approved mockup, with the sidebar in Simulab's navy
  `#0F3266` and a white app bar — it is the version the owner approved on screen.
- 2026-10-17 — Field icons and placeholders stay those of the current kit (F-95 mockup) — the owner asked to keep them.
- 2026-10-17 — F-83 (literal colours of `app.css`) is absorbed by the foundation — same theme and the same token
  swap, as D-2 absorbed B-16 into F-43.
- 2026-10-17 — F-70 stays as it is; the foundation is captured as a new item — it is wider than recording tokens
  (theme, kit, gallery), and F-70 was already covered by F-63.
- 2026-10-17 — Four actions of the Stitch screen become ideas: sticky section index, inline student preview,
  publishing readiness meter, keyboard shortcuts.

## Parked
- Which action is the single orange one on each screen — each screen item decides it at its refinement.
- The exam session screens (distraction-free exception in `ui-project.md`) — wait on the first exam session screen,
  as D-2 already parked.
- Dark theme values of the Control Alt Systems tokens inside Simulab — the foundation's refinement measures them
  against the contrast tests.

## Outcome
- Epics: Foundation and identity
- Features: F-125 (foundation), F-126 to F-129 (complex forms), F-130 to F-134 (areas), F-135 to F-138 (actions)
- Bugs: none
- Absorbed: F-83 (into F-125)
