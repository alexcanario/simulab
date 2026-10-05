---
feature: F-87
epic: Foundation and identity
status: idea
board: 130
version: 1
---
# Production emails leave from our own domain

Technical terms: [glossary](../glossary.md)

## Summary
F-66 sends staging emails from the Azure-managed domain of Azure Communication Services (`...azurecomm.net`). Production sends from Simulab's own domain (`no-reply@<domain>`): a custom domain on the email service, its DNS records (SPF, DKIM, domain verification) and the sender address. Raised as out of scope by the F-66 refinement, 2026-10-04.

## Start
- Depends on: F-66 (the email service in the cloud).
- Waits on (to start): the owner owns the domain and controls its DNS — owner.
- Needed to validate: the production environment, created at the first release — owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
