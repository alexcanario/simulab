---
feature: F-93
epic: Cloud hosting and operations
status: refining
board: 139
version: 1
---
# Users' data in Azure Central US meets LGPD and GDPR

Technical terms: [glossary](../glossary.md)

## Summary
Bring the handling of users' personal data hosted in the Azure Central US region in line with LGPD (Brazil) and GDPR (EU): the legal basis for the international transfer (LGPD Art. 33, GDPR chapter V), the records and notices it asks for, and what changes in the app, the infrastructure and the privacy policy. Raised by the owner on 2026-10-07. Central US is real: the first staging runs there since 2026-10-08 (`rg-simulab-staging`, F-64), while ADR-0002, ADR-0003, the policy text of F-71 and the "Environments" table of `docs/infra.md` still say Brazil South.
Added 2026-10-09: the owner confirmed Central US is a real choice, not a planned one. ADR-0003 (Brazil South as the single data region) no longer matches it and is reopened at `/agile:discuss`. F-71 waits on this item, since the privacy policy names the region.

## Start
- Depends on: F-67 (done, the region decision record in ADR-0003); related to F-71 (the privacy policy names where data lives and who processes it) and F-64 (staging on Azure).
- Waits on (to start): nothing. Legal advice on the transfer basis is not a start condition: the written record ships marked as a draft until a lawyer reviews it — owner (to be confirmed at the question round).
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build` (the region question is answered in the refinement round). It runs before F-71 merges: F-71's policy text names Brazil South.
- Parallel with: unknown — settled at /agile:refine.
