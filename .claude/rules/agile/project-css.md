---
paths:
  - "**/*.css"
  - "**/*.scss"
---
# Project rules

- After a palette change, search app.css for color: var(--mud-palette-primary) and var(--mud-palette-info) used as text and move them to --app-link or --app-brand-name: a blue fill can read under 4.5:1 as text on the dark surface (4.12:1 in F-148). (F-148, 2026-10-23)
