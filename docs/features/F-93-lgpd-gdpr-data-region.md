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
Simulab hosts users' data in Azure Central US (Iowa, United States) for staging and production. The first staging runs there since 2026-10-08 (`rg-simulab-staging`, F-64), while ADR-0002, ADR-0003, the policy text of F-71 and the "Environments" table of `docs/infra.md` still say Brazil South. This item records the decision in a new ADR and writes the legal basis for the international transfer of each user population (LGPD Art. 33 for users in Brazil, GDPR chapter V for users in the EU), and adds the register of what personal data is processed, where and by whom (LGPD Art. 37, GDPR Art. 30). Raised by the owner on 2026-10-07; Central US confirmed as a real choice on 2026-10-09.

**This item is Claude's reading of public sources, not legal advice. The ADR and the register ship marked as a draft until a lawyer reviews them (D4).**

## Start
- Depends on: F-67 (done, the region decision record in ADR-0003); F-64 (done, the staging in Central US); F-66 (done, the email service).
- Waits on (to start): nothing. The lawyer's review is not a start condition: it only removes the draft mark later — owner.
- Needed to validate: nothing beyond reading the files in the worktree — owner.
- Suggested path: `/agile:build`. It runs before F-71 merges: F-71's policy text names Brazil South and gets a change note afterwards (D7).
- Parallel with: F-71 is `building` in its own worktree and this item edits none of its files; F-85 and F-72 stay untouched.

## What already exists (checked 2026-10-09)
- Staging runs in `centralus`, resource group `rg-simulab-staging`, since 2026-10-08 (`docs/features/F-64-staging-on-azure.md`, "First staging"; `docs/infra.md` deploy commands set `AZURE__LOCATION=centralus` and the workflow variable `AZURE_LOCATION` is `centralus`). Production does not exist yet.
- Four places still say Brazil South as the data region: ADR-0002 (decision 1), ADR-0003 (decisions 1 and 2), the "Environments" table and the "Data region" line of `docs/infra.md`, and the "Cloud accounts" rows of `docs/infra.md` (staging and production, column Region).
- The email service of F-66 (Azure Communication Services) is created with data location Brazil (`docs/infra.md`, email Bicep paragraph) while the rest of staging is in Central US.
- Personal data held today (from the code): `User` (full name, email through Identity, preferred language, adult declaration, TOTP secret encrypted), `ConsentRecord` (terms and privacy versions, locale, accepted-at, IP address), account events, role changes, one-time tokens (email verification, password reset), sessions in Redis. The data rights features exist: erasure (F-10) and download of my data (F-16), both `done`.
- Processors in use or planned: Microsoft Azure (hosting, database, cache, storage, key vault, email), Anthropic (Claude API behind `IAiGateway`; the coach is not built, so no user content reaches it today — not verified in code at this refinement).
- F-71 (`building`) writes the privacy policy `2026-v2` naming Brazil South and, as an approved list, Azure, Anthropic and SendGrid; it is now out of date in two ways (the region, and the email provider is Azure Communication Services, F-66).
- ADR-0003 decision 3 lists when the region decision is reopened (the adequacy decision for Brazil changes, a court invalidates it, a product reason asks for EU hosting). A hosting cost is not one of them, which is why a new ADR is needed rather than a note.

## Goal
A person reading the repository (the owner, a lawyer, a future developer) learns in one place where users' personal data is stored, which companies process it, and on what legal basis it leaves Brazil and the EU, with each unverified point marked as such.

## Users and use cases
- UC1 The owner or a lawyer opens ADR-0004 and reads the region decision, the options considered, the reason, the legal basis for users in Brazil and for users in the EU, and when the decision is reopened.
- UC2 The owner or a lawyer opens the data processing register and sees, for each category of personal data: what it is, why it is processed, where it is stored, who processes it, how it crosses borders, how long it is kept and how a person exercises their rights.
- UC3 A developer who adds a processor or a new category of personal data finds the register and knows which row to add or change.

## Business rules
- BR1 Azure Central US (`centralus`) is the region of record for staging and production. ADR-0004 records it, supersedes the region decisions of ADR-0002 (decision 1) and ADR-0003 (decisions 1 and 2) and keeps ADR-0003 decisions 3 to 5 (reopen triggers, F-71 and F-72, Portugal after v1) as they are; ADR-0002 and ADR-0003 each get a note naming ADR-0004 and what it replaces. Neither is rewritten.
- BR2 ADR-0004 follows the shape of ADR-0003: context with facts checked and dated, options (A Brazil South for both, B Central US for staging only, C Central US for both), the decision, the reasons, the reopen triggers and the consequences. It records that Claude recommended A (average 7.2 against 5.2) and the owner chose C twice (D1), and why: lower hosting cost, an owner statement that is not measured. When `docs/infra.md` holds a measured cost of the Central US staging (F-64), the numbers go in next to ADR-0002's Brazil South prices; otherwise the ADR says "not measured".
- BR3 ADR-0004 states the legal basis separately for each user population, with sources and the date they were read; nothing is written from memory:
  - users in Brazil: hosting in the United States is an international transfer under LGPD Art. 33; the mechanism is the ANPD standard contractual clauses of Resolution CD/ANPD 19/2024 (the grace period to adopt them ended on 2025-08-23; source in D8), because the ANPD has recognised no adequacy for the United States and no equivalence of foreign clauses;
  - users in the EU: hosting in the United States rests on the EU-US Data Privacy Framework when the provider is certified for the service, and on the EU standard contractual clauses of the provider's data protection addendum as the fallback (GDPR Art. 45 and 46); the adequacy decision for Brazil (ADR-0003) no longer covers where the data is stored;
  - the processor contract with Microsoft (LGPD Art. 39, GDPR Art. 28) is named, with the document and the version read on the build date.
- BR4 A basis the provider's published terms do not give is written as a gap, never as if it were in place: the line says `basis not in place`, names the action (for example, obtain the ANPD standard clauses from the provider) and the owner who takes it. The item does not claim a basis it could not verify.
- BR5 The register is `docs/privacy/data-processing-register.md`, a table with one row per category of personal data and the columns: category, data fields, purpose, legal basis for processing (LGPD Art. 7 / GDPR Art. 6, as the code and the consent flow show it), storage service and region, processor, transfer basis, retention (what the code really applies, else `none defined`), and how the person exercises their rights (F-10, F-16 where they apply). Every row is derived from the code on the build date, with the file it came from; the register claims no encryption, retention period or purpose the code does not apply (same rule as F-71 BR6).
- BR6 The register has a second table of processors: company, service used, what personal data it receives, where it processes it, the transfer basis and the contract document. At least: Microsoft Azure (every service in `docs/infra.md`, with the email service's data location Brazil noted), Anthropic (Claude API) and any other provider the code calls on the build date.
- BR7 The ADR and the register both open with the draft notice of D4. Removing the notice after the lawyer's review is a later, separate edit by the owner.
- BR8 `docs/infra.md` is corrected so that no sentence names Brazil South as the data region: the "Environments" table, the "Data region" line and the "Cloud accounts" Region column say `centralus`, each pointing to ADR-0004. Text that is history (ADRs, closed items, change notes, examples in `docs/agile/`) keeps what it said.
- BR9 This item changes no code, no infrastructure, no Bicep and no database; the region is already `centralus` and nothing is moved.
- BR10 The staging holds only test data (owner, 2026-10-09; D2). If real people's data reaches the staging before this item merges, the build stops and asks, because that data would already be in the United States without the basis BR3 writes.

## Screens and API
- No screen, route, endpoint or error code. Documents only: `docs/decisions/ADR-0004-data-region-central-us.md`, `docs/privacy/data-processing-register.md`, and edits to ADR-0002, ADR-0003 and `docs/infra.md`.

## Acceptance criteria
- AC1 Given the decisions folder, when ADR-0004 is read, then it names `centralus`, lists options A, B and C, records the owner's choice of C over the recommended A with the averages 5.2 and 7.2, and names the reopen triggers. (UC1, BR1, BR2)
- AC2 Given ADR-0004, when its legal basis section is read, then it has one block for users in Brazil that cites LGPD Art. 33 and Resolution CD/ANPD 19/2024, one for users in the EU that cites GDPR Art. 45 and 46 and the Data Privacy Framework, and each block has a source and a read-on date. (UC1, BR3)
- AC3 Given a basis the sources do not confirm for the provider, when ADR-0004 and the register are read, then that basis is marked `basis not in place` with its action and owner, and the text does not say it is in place. (BR4)
- AC4 Given ADR-0002 and ADR-0003, when they are read, then each names ADR-0004 and what it replaces, and ADR-0003 still lists decisions 3 to 5 unchanged. (BR1)
- AC5 Given the register file, when it is read, then it has the personal-data table with the columns of BR5 and the processors table of BR6, and the rows for `User`, `ConsentRecord`, account events, one-time tokens and sessions each cite the source file they came from. (UC2, BR5, BR6)
- AC6 Given the register, when its processors table is read, then it names Microsoft Azure, Anthropic and the email service with the region each processes in, including the email service's data location Brazil. (BR6)
- AC7 Given the ADR and the register, when the first lines are read, then both carry the draft notice and say they are not legal advice. (BR7)
- AC8 Given `docs/infra.md`, when it is searched for `Brazil South`, then no sentence uses it as the data region of a Simulab environment, and the three places of BR8 say `centralus` and name ADR-0004. (BR8) — validation script.
- AC9 Given the solution, when the diff of this item is read, then it contains no change under `src/` and no Bicep or migration file. (BR9) — validation script.
- AC10 Given the three UI locales, when the item is delivered, then no UI resource key is added or changed and the missing-key test stays green.

## Criterion → test

| Criterion | Test |
|---|---|
| AC1 | `DataRegionDocumentsTests.Adr0004_NamesTheRegionTheOptionsAndTheChoice` |
| AC2 | `DataRegionDocumentsTests.Adr0004_HasALegalBasisBlockPerPopulationWithSourceAndDate` |
| AC3 | `DataRegionDocumentsTests.UnverifiedBasis_IsMarkedAsAGapWithActionAndOwner` |
| AC4 | `DataRegionDocumentsTests.Adr0002AndAdr0003_NameAdr0004` |
| AC5 | `DataProcessingRegisterTests.Register_HasBothTablesAndEveryRowCitesItsSource` |
| AC6 | `DataProcessingRegisterTests.Processors_NameAzureAnthropicAndTheEmailServiceWithTheirRegion` |
| AC7 | `DataRegionDocumentsTests.AdrAndRegister_OpenWithTheDraftNotice` |
| AC8 | validation step 2 (search of `docs/infra.md`, run by Claude and shown to the owner) |
| AC9 | validation step 3 (`git diff --stat` against the main branch shows documents only) |
| AC10 | no resource key added; the existing missing-key test is unchanged |

The tests live in `tests/Simulab.ArchitectureTests/`, beside `InfraAccessTests`, and read the files by path from the repository root. They check structure and wording markers, not the truth of the legal text.

## Decisions
- 2026-10-09 — Region of record: Azure Central US for staging and production — owner chose "Central US for both, with the written legal basis" over the recommended "Brazil South for both" (average 5.2 vs 7.2; gap 2.0); asked once more with the table and the owner kept the choice. Reason given: lower hosting cost. Kept.
- 2026-10-09 — The reason for Central US is cost (owner); it is not measured. BR2 records it as an owner statement and adds numbers only when `docs/infra.md` holds the F-64 measurement.
- 2026-10-09 (D2) — The staging in Central US holds only test data (owner). No migration, deletion or notice to people is part of this item; BR10 stops the build if that changes.
- 2026-10-09 (D3) — Scope chosen by the owner: a new ADR that supersedes the region decisions, and the data processing register. Not chosen: a test that fails when the region in `docs/infra.md`, the Bicep and the ADR differ; the data protection officer (encarregado, LGPD Art. 41) contact and the incident notification procedure. The recommendation for the officer and the incident procedure is a separate item; it is not captured yet and is offered to the owner at the end of the refinement.
- 2026-10-09 (D4) — The legal basis is written now and marked as a draft until a lawyer reviews it, the same criterion as F-71 D1 (owner). Nothing waits on the review.
- 2026-10-09 (D5) — Technical: the tests are content checks in `tests/Simulab.ArchitectureTests/` (markers, headings, columns, the names of the ADRs). They are not the region-consistency guard the owner left out (D3). Decided by Claude, no product impact.
- 2026-10-09 (D6) — Technical: the ADR is `ADR-0004-data-region-central-us.md` (next free number, rechecked at build) and the register is `docs/privacy/data-processing-register.md`; a new folder `docs/privacy/` is the place for privacy documents that are not the policy text, which stays in the Identity content folder (F-4 BR14).
- 2026-10-09 (D7) — F-71 is `building` and names Brazil South and SendGrid. This item edits none of its files. When ADR-0004 is accepted, the owner types `/agile:change F-71` so the policy text names the real region, the real email service and the transfer bases of ADR-0004 (same rule as F-71 BR5: bases from the providers' published terms). The execution order in `docs/epics/README.md` already puts F-93 before F-71 and F-85.
- 2026-10-09 (D8) — Sources read for this refinement, not legal advice: [Mayer Brown, end of the grace period for the ANPD standard contractual clauses](https://www.mayerbrown.com/en/insights/publications/2025/08/end-of-grace-period-implementation-of-brazils-standard-contractual-clauses-in-international-transfers-of-personal-data) (Resolution CD/ANPD 19/2024 published 2024-08-23; the 12-month period ended on 2025-08-23; no recognition of foreign clauses as equivalent had been granted in the sources read); [Microsoft Trust Center, EU standard contractual clauses](https://www.microsoft.com/en-us/TrustCenter/Compliance/EU-Model-Clauses) (clauses offered through the data protection addendum for Azure). Not verified: whether Microsoft offers the ANPD standard clauses, the current scope of its Data Privacy Framework certification, and the current text of the data protection addendum. The build reads the providers' own published documents and records the version and the date (BR3, BR4).

## Out of scope
- Moving staging or production to another region; any change of code, Bicep or database (BR9).
- A test that compares the region in `docs/infra.md`, the Bicep and the ADR (D3).
- The data protection officer's contact (LGPD Art. 41) and the incident notification procedure (LGPD Art. 48, GDPR Art. 33).
- The privacy policy text and its draft notice: F-71, with a change note after this item (D7). New acceptance of the policy by existing users: F-85.
- The EU representative (GDPR Art. 27) and Portugal: F-72, after v1.
- Production email with the product's own domain (F-87), bounces and complaints (F-88).
- A rule or a test that forces the register to be updated whenever a feature adds a processor: proposed at the retro, not built here.

## Open questions
- (none)

## Change notes

## Validation script
Written at the end of build.

## Delivery
Filled by `/agile:ship`.
