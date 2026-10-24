---
feature: F-119
epic: Interface and screen composition
status: idea
board: 169
version: 1
---
# A list that keeps its state in the address follows a menu click to the plain URL

Technical terms: [glossary](../glossary.md)

## Summary
The pages that keep their filters, search and page in the address (`/admin/questions` of F-100, `/catalog` of F-36 in `CatalogSearch.razor`) read the address only when they are created. Clicking the side-menu entry that points at the plain URL, while on a filtered list, keeps the same component instance: the address becomes plain, but the pickers, the table's search box and the filters stay, and the next load writes them back to the address. This item re-reads the address on a location change and resets the page state and the table, including the table's search box, when the query differs from what the page holds. Source: independent review of F-100, finding 1 (minor, 2026-10-13); the catalog list has the same limitation since F-36, so the fix is shared.

## Start
- Depends on: F-100.
- Waits on (to start): nothing.
- Needed to validate: the local app host with a few questions and one filtered list — owner.
- Suggested path: `/agile:refine` → `/agile:build` (or `/agile:autopilot` once refined small).
- Parallel with: unknown — settled at `/agile:refine`.
