---
feature: F-161
epic: assessment-catalog
status: idea
board: 220
version: 1
---
# Keep only the Manaus 2026 edition in the guard catalog seed

Technical terms: [glossary](../glossary.md)

## Summary
The municipal guard seed keeps only the Manaus 2026 edition (Instituto Consulplan, Edital nº 01/2026). A new data migration soft-deletes the other three seeded editions (Curitiba, Salvador, Maceió), the exams, issuing authorities and organizers only they use, and their notice subjects (F-78), in every environment, staging included. The seed migrations of F-37, F-51 and F-78 stay untouched, a row an admin already edited is skipped, and the taxonomy of F-51 stays whole. Source: discussion D-6.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: the local app host — owner; staging runs the migration at the next deploy.
- Model: unknown — settled at `/agile:refine`
- Suggested path: `/agile:refine` → `/agile:build`
- Parallel with: F-110 (the Manaus questions seed); they touch different modules.
