---
epic: partner-tenants
status: draft
board: 190
---
# Partner tenants

Technical terms: [glossary](../glossary.md)

## Goal
A partner can rent Simulab and run it as its own: its company name and logo in place of Control Alt Systems, its students and question banks kept apart from other partners. Raised in the F-125 refinement (owner, 2026-10-17); it wakes the dormant multi-tenancy of ADR-0001 ("Revisit when an institution wants private students or question banks").

## Start
- Depends on: F-125 (the Control Alt Systems identity the partner brand replaces)
- Waits on: unknown — settled at /agile:epic

## Features
| Id | Feature | Value | Priority | Size | Depends on | Waits on | Screen design | Status |
|---|---|---|---|---|---|---|---|---|
| F-139 | Partner brand in place of Control Alt Systems | A partner's name and logo on its users' screens and emails | unknown | unknown | F-125 | - | unknown | idea |
| F-150 | Grader role and user-to-tenant link | A grader signs in and grades essays; an administrator assigns the role and the user's tenant; a grader with a tenant sees only that tenant's essays (D-5, D-7) | unknown | unknown | F-9 | - | unknown | idea |
| F-162 | Tenant-scoped visibility for curators and graders | `ICurrentTenant` reads the user's tenant claim, so the global tenant filter applies in every module (D-7) | unknown | unknown | F-150 | the first partner tenant and what its curators may see — owner | unknown | idea |

## Execution plan
1. F-139 — unknown — settled at /agile:epic

## Waiting outside the epic
| What | For | Who provides it | Who asks |
|---|---|---|---|
| unknown — settled at /agile:epic | - | - | - |

## First release cut

## Out of scope
- unknown — settled at /agile:epic

## Decisions
- 2026-10-17 — Partner rental is its own epic, not part of Foundation and identity — renting to partners is a business area with several features (brand, data isolation per tenant, billing per partner), not only a look.

## Retro notes

## Related
- Discussions: D-4
