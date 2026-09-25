---
discussion: D-2
status: decided
date: 2026-09-25
---
# Visual language and control patterns

## Idea
On the screen validation of F-34 the owner rejected the look of the exam form: the colours and the controls
do not read like Simulae, whose back office he had already approved. He asked to rediscuss the visual
language and the control patterns of every Simulab screen, not only that form.

## What we already know
- The palette is already Simulae's. ADR-0001 decision 30 says so, and `src/Hosts/Simulab.Web/Theme/SimulabTheme.cs`
  carries the same tones: primary `#2478C5` (darkened to `#216DB5` by B-8 for contrast), secondary teal `#1A9E8C`,
  tertiary amber `#F5A020`, background `#F2F5FB`, surface `#FFFFFF`, drawer and appbar `#0F3266`. So the difference
  the owner sees is not in the colour tokens.
- Nine passes have already tuned those tokens for contrast (B-8, B-9, B-10, F-10, F-17), and `ThemeContrastTests`
  and `ThemePaletteTests` hold the numbers. A new tone has to keep those tests green.
- The UI kit (`src/Hosts/Simulab.Web/Components/Ui/`, 30 files) has field, table, row-action, dialog, alert and
  empty/loading/error components. It has **no page-composition component**: no section card, no multi-column form
  grid, no summary aside. Pages are therefore a single card with one column of fields.
- The reference mockup, `simulae/docs/4. UI UX Design/mockups/[US #351] Cadastro e Edicao de Concursos.html`
  (read-only source, read 2026-09-25), is built from patterns the kit does not have:

  | Pattern in the Simulae mockup | In the Simulab kit? |
  |---|---|
  | Section card with title and icon (`section-title`, `section-sub`) | no |
  | Two- and three-column form grid (`field-row cols-3`) | no |
  | Radio group as cards with a subtitle (`radio-opt`, `radio-sub`) | no |
  | Conditional field revealed by a choice (`conditional-field`) | no |
  | Leading icon inside a field (`field-icon`) | no |
  | Status chip (`status-chip open/closed`) | no |
  | Consolidated error panel listing the offending fields (`alert-list`) | `AppAlert` exists, without the field list |
  | Summary aside with a completion checklist and the actions (`checklist`, `check-item`) | no |
  | Child-item rows with per-row actions (`notice-row`) | only as a table |

- `ui.md` already requires that a pattern is defined once, in the kit or the theme, and shown on the dev gallery
  `/dev/ui`. Adding these as page-local markup would break that rule.
- B-16 (field hint at 1.90:1 dark, 2.64:1 light) is open against the same theme and the same kit fields. Its file
  lives on branch `feature/F-34` and reaches `main` with that merge.
- Accessibility target is WCAG 2.2 AA (`ui-project.md`), so any new tone or surface has to be measured before it is
  written down, in both themes.

## Options
| Option | How it works for the user | Effort | Risks / what it rules out |
|---|---|---|---|
| A — Patterns only | The missing composition patterns enter the kit and the gallery; the tokens do not change | S | If the discomfort really is about colour, nothing changes: the tones stay exactly as they are |
| B — Patterns plus visual language | A, plus a typography scale, a spacing scale, a radius scale and written rules for **where** the accent colour appears in content (section header, chip, field icon, focus). New tokens go into an ADR with their measured contrast. The Simulae base palette stays | M | A larger diff across every screen; the contrast tests grow; a rebrand later would redo part of it |
| C — Own brand | A new palette and identity replace Simulae's | L | Needs a brand decision that does not exist yet; ADR-0001 "Revisit when" already parks it |

Recommendation: B — A alone cannot change how the colour reads, because the tones do not move, and C costs a brand
decision the project has not taken.

## Decisions
- 2026-09-25 — Take option B: the missing composition patterns plus a written visual language (typography, spacing,
  radius, where the accent appears), keeping Simulae's base palette — the tokens are already Simulae's, so what is
  missing is composition and the use of colour in content, not the hues.
- 2026-09-25 — Simulae's back office is copied closely in composition, not merely used as inspiration — the owner
  has already validated those screens, which shortens the design round.
- 2026-09-25 — Scope: the kit, the gallery and **all existing screens migrate in the same item**, not screen by
  screen as features touch them — `ui.md` requires one pattern defined once, and the app has few screens today.
- 2026-09-25 — B-16 is absorbed by the new feature instead of being fixed on its own — it is the same theme file
  and the same kit fields the feature rewrites.
- 2026-09-25 — Before any code, one reference screen (the exam form) gets an `/agile:screen` mockup the owner
  approves — that artefact is what prevents a second rejection at validation.
- 2026-09-25 — F-34 is not reworked. It is validated on behaviour only and merges as it is; its screens are
  restyled by the new feature — holding the branch for a full restyle would block it and grow the merge conflict.

## Parked
- Own brand (option C) — waits on a brand decision by the owner; ADR-0001 already lists it under "Revisit when".
- Whether the exam session screens (the declared distraction-free exception in `ui-project.md`) follow the new
  composition patterns — waits on the first exam session screen existing.

## Outcome
- Epics: Foundation and identity
- Features: F-43
- Bugs: B-16 (absorbed by F-43; the file is on branch `feature/F-34`)
