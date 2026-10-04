---
feature: F-73
epic: Foundation and identity
status: refining
board: 115
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
Status flow: idea -> refining -> approved -> building -> validating -> done
- idea: title, summary and start only (/agile:idea, /agile:epic).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
Remove these comments when the file leaves `idea`.
-->
# Shared OpenIddict keys across Api replicas

Technical terms: [glossary](../glossary.md)

## Summary
The Api signs and encrypts its tokens with development certificates (`AddDevelopmentSigningCertificate` / `AddDevelopmentEncryptionCertificate`, `src/Modules/Identity/Simulab.Identity.Infrastructure/IdentityModule.cs:220`), so each replica would use its own certificate and a token issued by one replica is rejected by another; `docs/infra.md` marks the cloud certificates (Key Vault) as planned. Move the keys to Key Vault and lift the Api's `maxReplicas: 1` cap that F-54 adds. Found while refining F-54 (2026-10-04).

## Start
- Depends on: F-54 (adds the cap this item lifts); F-64 (creates the staging environment and its Key Vault).
- Waits on (to start): nothing beyond those items.
- Needed to validate: the staging environment running with two Api replicas — provided by F-64.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.

## Goal
<!-- Why this feature exists, in one or two sentences. -->

## Users and use cases
- UC1 <Actor> <does something> <and gets a result>.

## Business rules
- BR1 <Rule>.

## Screens and API
- <Route> — <purpose>

## Acceptance criteria
- AC1 Given <context>, when <action>, then <result>.

## Decisions
- <YYYY-MM-DD> — <decision> — <reason>

## Out of scope
- <Item>

## Open questions
- (none)

## Change notes

## Validation script
1. <Step> → <expected result>

## Delivery
- Branch: feature/F-73
