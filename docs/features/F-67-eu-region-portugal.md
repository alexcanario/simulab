---
feature: F-67
epic: Foundation and identity
status: building
board: 106
version: 1
---
# Portuguese users' data stays in an EU region

Technical terms: [glossary](../glossary.md)

## Summary
Decide whether Portuguese users need their data hosted in an EU region (GDPR) and, if so, how: a second environment, a region move or a split by country. A deferred decision in `docs/infra.md`; captured so it is tracked on the board. Raised as out of scope by the F-62 refinement, 2026-10-02. Reframed at `/agile:refine` (2026-10-04): Brazil is adequate under GDPR since 2026-01-26, so the item records the decision "one region, Brazil South, for every user" and closes the deferred line; no code.

## Start
- Depends on: F-62 (the host decision; `validating` in `D:/wt/simulab/f-62-deploy-command-per` on 2026-10-04, ADR-0002 not on `main` yet). The build waits for its merge: the new ADR cites ADR-0002 and edits the same lines of `docs/infra.md`.
- Waits on (to start): nothing beyond the F-62 merge. When Portugal is in scope is answered (after v1, owner 2026-10-04) and no longer blocks this item.
- Needed to validate: nothing — the owner reads the documents.
- Suggested path: `/agile:refine` → `/agile:build` (after the F-62 merge).
- Parallel with: F-68 (`approved`, also edits `docs/infra.md`: expect a small conflict, take them in sequence through the merge); any code item.

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
Turn the deferred "EU region for GDPR" line into a recorded decision with its legal basis, so the host plan, the brief and the ADRs say the same thing and nobody plans a second region by habit.

## Users and use cases
- UC1 The owner reads one ADR and learns where every user's data is hosted, the GDPR basis that allows EU users' data in Brazil, what GDPR still asks outside the region (tracked by F-71 and F-72), and what would reopen the decision.
- UC2 The owner reads `docs/infra.md`, ADR-0001 and `product/brief.md` and finds no deferred EU-region decision left, and Portugal planned after v1.

## Business rules
- BR1 One region for every user: Azure, Brazil South (ADR-0002). No EU environment, no region move, no split by country.
- BR2 The legal basis recorded for EU users' data in Brazil is Art. 45 GDPR, Implementing Decision (EU) 2026/179 of 2026-01-26, with the sources and the date they were read. The ADR says it is Claude's reading, not legal advice.
- BR3 The decision is reopened when any of these happens: the adequacy decision for Brazil is suspended, amended or repealed (the Commission reviews it periodically); a court or authority invalidates it; a product reason asks for EU hosting (latency measured for users in Portugal, a customer contract). The ADR lists them.
- BR4 What GDPR still asks outside the region is not decided here: privacy policy naming the region, processors and transfer bases → F-71; EU representative (Art. 27) → F-72. The ADR points to both.
- BR5 Portugal is planned **after v1**: the brief's open question on Portugal records the timing (which exams stays open).

## Screens and API
- No screen, no endpoint, no code.
- Files: `docs/decisions/ADR-0003-data-region.md` (new), `docs/infra.md` (the deferred line replaced by a pointer to ADR-0003), `docs/decisions/ADR-0001-foundation.md` (open item "Portugal: which exams and when; EU region for GDPR" → which exams only, pointing at ADR-0003), `product/brief.md` (Portugal open question: after v1), `docs/glossary.md` (technical term "adequacy decision").

## Acceptance criteria
- AC1 Given ADR-0003, when the owner reads it, then it records BR1, the legal basis of BR2 with its sources and date, the triggers of BR3 and the pointers of BR4, and says it is not legal advice. (validation script)
- AC2 Given `docs/infra.md`, when it is searched for "deferred" next to the EU region, then nothing is found, and the environments section points to ADR-0003. (validation script)
- AC3 Given ADR-0001's open items and `product/brief.md`'s open questions, when the owner reads them, then the EU region is no longer open and Portugal says "after v1". (validation script)
- AC4 Given the glossary, when the owner reads the technical terms, then "adequacy decision" has its pt-BR word and both meanings. (validation script)
- AC5 Given the item adds no UI text and no code, when the build and the affected tests run, then they stay green, the missing-key test included, and the app manual is unchanged (no visible behavior changes).

## Decisions
- 2026-10-04 — The premise "Portuguese users may need EU hosting" is false since 2026-01-26: Brazil is adequate under Art. 45 GDPR (Implementing Decision (EU) 2026/179) — sources in `## Premise check`, read 2026-10-04 (Claude, research; not legal advice).
- 2026-10-04 — The item becomes a decision record (ADR + doc updates), not an EU environment — no legal need for a second region, which would also break F-62's US$ 80 ceiling and needs a country field users do not have (owner).
- 2026-10-04 — What GDPR still asks outside the region becomes two ideas: F-71 (privacy policy names region, processors and transfer bases; board #113) and F-72 (EU representative, Art. 27, before selling in Portugal; board #114) — each has its own size and needs legal advice (owner).
- 2026-10-04 — Portugal comes after v1; recorded in the brief by this item (owner).
- 2026-10-04 — The ADR is numbered ADR-0003 and is written after the F-62 merge — ADR-0002 (host) lives on the F-62 branch and is cited by it (Claude, technical).
- 2026-10-04 — No new package (Claude, technical: docs only).
- 2026-10-04 — Approved by the owner ("aprovo F-67"); the build waits for the F-62 merge.

## Out of scope
- Privacy policy text, processors and transfer bases: F-71.
- EU representative (Art. 27): F-72.
- Which Portuguese exams, and the Portugal launch itself.
- A country field on the user, and any EU environment.
- Verifying the Claude API's and the email provider's transfer terms (F-71).

## Open questions
- (none)

## Change notes

## Validation script

## Delivery
