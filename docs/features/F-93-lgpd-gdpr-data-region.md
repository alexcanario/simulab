---
feature: F-93
epic: Cloud hosting and operations
status: idea
board: 139
version: 1
---
# Users' data in Azure Central US meets LGPD and GDPR

Technical terms: [glossary](../glossary.md)

## Summary
Bring the handling of users' personal data hosted in the Azure Central US region in line with LGPD (Brazil) and GDPR (EU): the legal basis for the international transfer (LGPD Art. 33, GDPR chapter V), the records and notices it asks for, and what changes in the app, the infrastructure and the privacy policy. Raised by the owner on 2026-10-07. Note: the repository records Brazil South as the single data region (ADR-0003, `docs/infra.md`); no file or worktree names Central US. Whether Central US is a real or planned choice is not verified and ADR-0003 may need to be reopened.
Added 2026-10-09: the owner confirmed Central US is a real choice, not a planned one. ADR-0003 (Brazil South as the single data region) no longer matches it and is reopened at `/agile:discuss`. F-71 waits on this item, since the privacy policy names the region.

## Start
- Depends on: F-67 (done, the region decision record in ADR-0003); related to F-71 (the privacy policy names where data lives and who processes it) and F-64 (staging on Azure).
- Waits on (to start): the owner confirms which environment and region hold users' data (Central US versus Brazil South), and legal advice on the transfer basis — owner.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:discuss` first (open direction: region and legal basis).
- Parallel with: unknown — settled at /agile:refine.
