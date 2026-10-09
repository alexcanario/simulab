---
feature: F-112
epic: Cloud hosting and operations
status: idea
board: 159
version: 1
---
# The privacy policy names the real region and email service

Technical terms: [glossary](../glossary.md)

## Summary
The published privacy policy `2026-v2` (F-71, done, version 0.23.0) says users' data is stored in Brazil South and names SendGrid as the email processor. The real region is Azure Central US (staging since 2026-10-08, recorded by F-93 in ADR-0004) and the real email service is Azure Communication Services (F-66). Publish version `2026-v3` in pt-BR, pt-PT and en with the real region, the real email service and the transfer bases that ADR-0004 writes for users in Brazil and in the EU, keeping `2026-v1` and `2026-v2` in the manifest. Raised by the F-93 build stop, 2026-10-09.

## Start
- Depends on: F-93 (the ADR-0004 decision and the transfer bases); F-71 (done, the policy `2026-v2` it replaces).
- Waits on (to start): F-93 merged — owner. The lawyer's review is not a start condition: the text stays marked as a draft (F-71 D1).
- Needed to validate: the local app host, the `/privacy` page in the three locales.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine. F-85 (existing users accept a new legal version) touches the same consent code and runs after it.
