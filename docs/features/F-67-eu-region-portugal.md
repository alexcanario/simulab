---
feature: F-67
epic: Foundation and identity
status: refining
board: 106
version: 1
---
# Portuguese users' data stays in an EU region

Technical terms: [glossary](../glossary.md)

## Summary
Decide whether Portuguese users need their data hosted in an EU region (GDPR) and, if so, how: a second environment, a region move or a split by country. A deferred decision in `docs/infra.md`; captured so it is tracked on the board. Raised as out of scope by the F-62 refinement, 2026-10-02.

## Start
- Depends on: F-62 (the host decision; `validating` in `D:/wt/simulab/f-62-deploy-command-per` on 2026-10-04, ADR-0002 not on `main` yet).
- Waits on (to start): the owner decides when Portugal is in scope (`product/brief.md`, open question on Portugal) — owner.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:discuss` first (open direction).
- Parallel with: unknown — settled at /agile:refine.

## What already exists (checked 2026-10-04)
- Hosting: every environment is planned in Azure Container Apps, **Brazil South** (`docs/infra.md`, ADR-0001 #32; ADR-0002 on the F-62 branch). No environment is provisioned; only `local` runs.
- Stored personal data: PostgreSQL (one database, schema per module), Redis (sessions), planned blob storage. One deployment serves every user.
- Users carry no country: the `User` entity has a preferred language (F-8, pt-PT possible) but no country or residence field. A split by country has nothing to split on today.
- Multi-tenancy is dormant (`TenantId` null for every user, ADR-0001 #7); it is not a country split.
- Other processors of personal data: the Claude API behind `IAiGateway` (Anthropic, outside the EU and Brazil) and the cloud email provider (F-66, `idea`).
- Privacy policy: served by the app as a versioned legal document (`LegalDocumentPage`, sign-up consent); its text does not name where data is stored (not verified in the seeded content).

## Premise check
- The summary assumes GDPR may require EU hosting for Portuguese users. **That premise is weak:** GDPR does not require data to stay in the EU; it requires that a transfer outside it rests on a legal basis (Art. 44-49). Since 2026-01-26 the European Commission recognises Brazil as adequate (Implementing Decision (EU) 2026/179, Art. 45 GDPR), and Brazil's ANPD recognises the EU in return. Personal data of EU users may be hosted in Brazil South with no extra transfer mechanism. Sources: [CMS](https://cms.law/en/prt/news-information/european-union-and-brazil-adopt-mutual-adequacy-decision-on-personal-data-protection), [Mayer Brown](https://www.mayerbrown.com/en/insights/publications/2026/02/a-new-era-for-personal-data-transfers-brazil-and-european-union-establish-mutual-adequacy-decision), [White & Case](https://www.whitecase.com/insight-alert/mutual-adequacy-between-eu-and-brazil-new-era-transatlantic-data-transfers).
- What GDPR still asks, not settled by the region: the transfer to the Claude API (United States: EU-US Data Privacy Framework or standard contractual clauses in the provider's data processing terms — not verified), the privacy policy naming where data is stored and who processes it, and possibly an EU representative (Art. 27) for a controller with no establishment in the EU that offers its service to people in the EU. These are legal readings by Claude, not legal advice; not verified by a lawyer.

## Goal
<!-- filled after the question round -->

## Users and use cases
<!-- filled after the question round -->

## Business rules
<!-- filled after the question round -->

## Screens and API
<!-- filled after the question round -->

## Acceptance criteria
<!-- filled after the question round -->

## Decisions
<!-- filled after the question round -->

## Out of scope
<!-- filled after the question round -->

## Open questions
- Q1 With Brazil adequate under GDPR, what does this item become (close, reframe to a decision record, keep EU hosting)?
- Q2 When is Portugal in scope?

## Change notes

## Validation script

## Delivery
