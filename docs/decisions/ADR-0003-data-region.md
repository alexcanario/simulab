---
adr: 0003
status: accepted
date: 2026-10-04
---
# ADR-0003: Data region — one region, Brazil South, for every user

## Context
ADR-0001 deferred "Portugal: which exams and when; EU region for GDPR", and `docs/infra.md` carried the line "An EU region for Portuguese users (GDPR) is a deferred decision". ADR-0002 hosts every environment in Azure Container Apps, Brazil South, and left the EU region to F-67.

Facts checked on 2026-10-04:
- One deployment serves every user. Personal data lives in PostgreSQL (one database, schema per module), Redis (sessions) and, later, blob storage, all in Brazil South once an environment exists. Only `local` runs today.
- The `User` entity has a preferred language but no country or residence field: a split by country has nothing to split on.
- Multi-tenancy is dormant (`TenantId` null for every user, ADR-0001 #7); it is not a country split.
- GDPR does not require EU residents' data to stay in the EU. It requires that a transfer outside the EU rests on a legal basis (Art. 44-49).
- Since 2026-01-26 the European Commission recognises Brazil as giving adequate protection: Implementing Decision (EU) 2026/179, under Art. 45 GDPR. Brazil's ANPD recognises the EU in return. Personal data of EU users may be hosted in Brazil with no extra transfer mechanism. Sources, read on 2026-10-04:
  - [CMS — European Union and Brazil adopt mutual adequacy decision on personal data protection](https://cms.law/en/prt/news-information/european-union-and-brazil-adopt-mutual-adequacy-decision-on-personal-data-protection)
  - [Mayer Brown — A new era for personal data transfers: Brazil and European Union establish mutual adequacy decision](https://www.mayerbrown.com/en/insights/publications/2026/02/a-new-era-for-personal-data-transfers-brazil-and-european-union-establish-mutual-adequacy-decision)
  - [White & Case — Mutual adequacy between EU and Brazil: a new era for transatlantic data transfers](https://www.whitecase.com/insight-alert/mutual-adequacy-between-eu-and-brazil-new-era-transatlantic-data-transfers)

**This is Claude's reading of public sources, not legal advice. It has not been verified by a lawyer.**

## Options
**A. One region, Brazil South, for every user** (chosen). No new environment, no new cost.

**B. A second environment in an EU region for users in Portugal.** Needs a country field users do not have, a routing rule, a second database and a second set of resources; roughly doubles the monthly cost and breaks ADR-0002's US$ 80 ceiling.

**C. Move every user to an EU region.** Worse latency for the main market (Brazil), and LGPD would then ask the same question in reverse; no legal need.

## Decision
1. **Every user's data is hosted in one region: Azure, Brazil South** (ADR-0002). No EU environment, no region move, no split by country.
2. **The legal basis for EU users' data in Brazil is Art. 45 GDPR**: Implementing Decision (EU) 2026/179 of 2026-01-26 (adequacy of Brazil), sources and date above.
3. **The decision is reopened when any of these happens:**
   - the adequacy decision for Brazil is suspended, amended or repealed (the Commission reviews it periodically);
   - a court or a data-protection authority invalidates it;
   - a product reason asks for EU hosting: latency measured for users in Portugal, or a customer contract that requires it.
4. **Not decided here** — what GDPR still asks outside the region:
   - the privacy policy naming where data is stored, the processors (the Claude API behind `IAiGateway`, the email provider) and the transfer basis of each one: F-71;
   - an EU representative (Art. 27) for a controller with no establishment in the EU that offers its service to people in the EU: F-72.
5. **Portugal comes after v1** (owner, 2026-10-04). Which Portuguese exams stays an open question of `product/brief.md`.

## Consequences
- `docs/infra.md`, ADR-0001 and `product/brief.md` no longer carry an open EU-region decision.
- No code, no country field, no second environment; ADR-0002's cost ceiling is unchanged.
- The adequacy decision becomes a dependency to watch: a change in it reopens this ADR before Portugal launches.
- F-71 and F-72 must be resolved before selling in Portugal, each with legal advice.
