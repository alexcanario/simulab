---
adr: 0004
status: accepted
date: 2026-10-09
---
# ADR-0004: Data region — Azure Central US for staging and production

Technical terms: [glossary](../glossary.md)

**Draft: not reviewed by a lawyer. This is Claude's reading of public sources, not legal advice.** The region decision is the owner's; the legal basis below is a draft until a lawyer reviews it (F-93 D4).

## Context
ADR-0002 hosts every environment in Azure Container Apps, Brazil South, and ADR-0003 fixed Brazil South as the single data region, with the adequacy decision of the European Commission for Brazil as the basis for EU users. The first staging was created in Azure Central US (Iowa, United States) on 2026-10-08 (`rg-simulab-staging`, F-64), and the owner confirmed on 2026-10-09 that Central US is a real choice, for staging and for production. Production does not exist yet.

Facts checked on 2026-10-09:
- `docs/infra.md` sets `AZURE__LOCATION=centralus` in the deploy commands and the workflow variable `AZURE_LOCATION` is `centralus`; before this ADR its "Environments" table, its "Data region" line and its "Cloud accounts" rows still said Brazil South.
- The staging holds test data only (owner, 2026-10-09). No real person's data is in the United States yet.
- Personal data lives in PostgreSQL (one database, schema per module), in Redis (sessions and revocation set) and in storage accounts (Data Protection keys), all in the region of the resource group. The list is in [the data processing register](../privacy/data-processing-register.md).
- The email service of F-66 (Azure Communication Services) is created with data location `Brazil` (`src/Hosts/Simulab.AppHost/Bicep/email.bicep`, `docs/infra.md`), while the rest of staging is in Central US. Whether delivery events leave that geography is not verified.
- The Claude API (Anthropic) is a processor in the United States. Today `IAiGateway` is called only by a diagnostics route mapped in Development (`src/Hosts/Simulab.Api/Features/Ai/AiDiagnosticsEndpoints.cs`), so no user data reaches it.
- Why Central US: lower hosting cost (owner). It is not measured: `docs/infra.md` holds no measured cost of the Central US staging yet, and ADR-0002's Brazil South prices were read from the retail price list on 2026-10-02 and 2026-10-03.

## Options
**A. Brazil South for staging and production.** No international transfer for users in Brazil, the main market; the adequacy decision of ADR-0003 keeps covering EU users; no new legal text. Costs a redeploy of the staging, and the price difference is not measured. Claude recommended A (average 7.2 against 5.2 on effort now, cost over time, risk, reversibility and fit with ADR-0002 and ADR-0003).

**B. Central US for staging only, production decided before the first release.** Staging stays while it holds test data; production would still need A or C.

**C. Central US for staging and production, with the legal basis written** (chosen). Nothing is redeployed; the transfer of users' data to the United States needs a written basis for each population and a processor contract.

## Decision
1. **Azure Central US (`centralus`) is the region of record for staging and production** (owner, 2026-10-09, over the recommendation of A; asked twice and kept). The owner's reason is cost; it is not measured.
2. **This ADR replaces** decision 1 of ADR-0002 (the host region) and decisions 1 and 2 of ADR-0003 (the single region and the Art. 45 basis for storage in Brazil). Decisions 3 to 5 of ADR-0003 (reopen triggers, F-71 and F-72, Portugal after v1) stay as they are. ADR-0002 and ADR-0003 each name this ADR.
3. **Legal basis, one block per population** (draft; sources read on 2026-10-09):
   - **Users in Brazil.** Hosting in the United States is an international transfer under LGPD Art. 33. The mechanisms that apply are the standard contractual clauses of Resolution CD/ANPD 19/2024 (published 2024-08-23; the period to adopt them ended on 2025-08-23) or another mechanism the ANPD approved; the ANPD had recognised no adequacy for the United States and no equivalence of foreign clauses in the sources read ([Mayer Brown](https://www.mayerbrown.com/en/insights/publications/2025/08/end-of-grace-period-implementation-of-brazils-standard-contractual-clauses-in-international-transfers-of-personal-data), read 2026-10-09). **Microsoft Azure: basis not in place.** The Microsoft Products and Services Data Protection Addendum page lists the English edition of May 2026 ([Microsoft licensing page](https://www.microsoft.com/licensing/docs/view/Microsoft-Products-and-Services-Data-Protection-Addendum-DPA), read 2026-10-09, the index only); a Microsoft source that offers the ANPD clauses was not found, and the text of the addendum was not read. Action: obtain and sign the ANPD standard clauses with Microsoft, or have a lawyer confirm that the addendum covers them. Owner: Alex Canario.
   - **Users in the EU.** Hosting in the United States rests on the EU-U.S. Data Privacy Framework when the receiver is certified for the data (GDPR Art. 45), with the EU standard contractual clauses of the processor's addendum as the fallback (GDPR Art. 46). The adequacy decision for Brazil (Implementing Decision (EU) 2026/179, ADR-0003) no longer covers where the data is stored. **Microsoft Azure: Data Privacy Framework stated, scope not confirmed.** Microsoft states that all U.S. subsidiaries using the Microsoft brand name are covered entities and does not name Azure or any product on that page ([Microsoft, Data Privacy Framework covered entities](https://www.microsoft.com/en-us/privacy/microsoft-data-privacy-framework-covered-entities), "Last Updated: September 2026", read 2026-10-09); the listing at dataprivacyframework.gov was not read. The EU standard clauses in the addendum were not read. Action: read the listing and the addendum. Owner: Alex Canario.
   - **Anthropic (Claude API).** Its data processing addendum, with the EU standard contractual clauses, is incorporated into its Commercial Terms of Service; the page names no Data Privacy Framework and no Brazil mechanism ([Anthropic privacy center](https://privacy.claude.com/en/articles/7996862-how-do-i-view-and-sign-your-data-processing-addendum-dpa), dated 2026-03-16, read 2026-10-09). **Anthropic: basis not in place for users in Brazil.** It does not apply until a feature sends user data to the model. Action: before the first such feature, obtain the ANPD standard clauses from Anthropic or have a lawyer confirm that its addendum covers them. Owner: Alex Canario.
4. **A basis the provider's published terms do not give is written as `basis not in place`**, with its action and owner (the three lines above and the register). It is never written as if it were in place.
5. **The processor contract** with Microsoft is the Products and Services Data Protection Addendum (LGPD Art. 39, GDPR Art. 28), edition May 2026 as listed on 2026-10-09; the text was not read in this item.
6. **The decision is reopened when any of these happens:**
   - a lawyer's review changes a basis written above;
   - the EU-U.S. Data Privacy Framework adequacy decision is suspended, amended, repealed or invalidated, or Microsoft's certification lapses;
   - the ANPD recognises, or refuses to recognise, an adequacy or an equivalence that changes what Art. 33 asks for;
   - the first real user data reaches the staging or production environment while a basis above is still `not in place`;
   - a product reason asks for another region: latency measured for users in Brazil or Portugal, a measured cost showing Brazil South is not dearer, or a customer contract that requires it.
7. **Not decided here:** the privacy policy text (F-112 publishes `2026-v3` with this region and these bases); the data protection officer (encarregado, LGPD Art. 41) and the incident procedure (no item yet); the EU representative of GDPR Art. 27 (F-72); Portugal after v1 (owner, 2026-10-04).

## Consequences
- No code, Bicep or infrastructure changes: the region is already `centralus`.
- Real users' data must not reach the staging or production environment while the Brazil line of decision 3 reads `basis not in place`. Until then the staging is for test accounts only.
- The published privacy policy `2026-v2` (F-71) names Brazil South and SendGrid and is wrong until F-112 ships.
- `docs/infra.md` says `centralus` in the "Environments" table, the "Data region" line and the "Cloud accounts" rows, each pointing here.
- Moving to Brazil South later is a redeploy of the resource group in another location and a migration of the data; the cheaper that is, the sooner it is done.
