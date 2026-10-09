---
feature: F-71
epic: Foundation and identity
status: done
board: 113
version: 1
---
# Privacy policy names where data lives and who processes it

Technical terms: [glossary](../glossary.md)

## Summary
The privacy legal document states the hosting region (Azure, Brazil South), the processors of personal data (Azure, Anthropic for the Claude API, the email provider of F-66) and the legal basis of each transfer: Brazil's EU adequacy (Implementing Decision (EU) 2026/179) for EU users, and for the United States the EU-US Data Privacy Framework or standard contractual clauses (to verify in each provider's terms). Raised as out of scope by the F-67 refinement, 2026-10-04.

## Start
- Depends on: F-67 (done: ADR-0003, the region decision). F-66 is no longer a dependency: the policy names SendGrid, the provider `docs/infra.md` plans (D2).
- Waits on (to start): nothing. Legal advice is no longer a start condition: the text ships marked as a draft (D1); the lawyer's review, owner, only removes the draft mark later.
- Needed to validate: nothing beyond the local app host (the `/privacy` page in the three locales).
- Suggested path: `/agile:build`.
- Parallel with: any item that does not touch the Identity legal content or the sign-up tests (F-85 touches the same consent code: run it after).

## What already exists (checked 2026-10-04)
- Legal documents are files (F-4 BR14): `src/Modules/Identity/Simulab.Identity.Infrastructure/Content/Legal/<locale>/privacy/manifest.json` plus `2026-v1.md`, for `en`, `pt-BR`, `pt-PT`. Rendered by `LegalDocumentProvider` (Markdig, pipe tables on, raw HTML off) and shown on the public page `LegalDocumentPage.razor` and at sign-up.
- The current privacy version is `2026-v1`, effective 2026-09-17, `isPlaceholder: true` in the three locales (F-4 BR15: a visible draft notice on the page and at sign-up).
- The current `2026-v1` text has four sections (what we collect, why, how long, your rights) and names no region, no processor and no transfer.
- Sign-up stores the accepted privacy version in `ConsentRecord` and refuses an outdated one (`RegistrationTerms.CheckAsync`, `registration.terms_version_outdated`). A signed-in user is never asked to accept a new version: captured as F-85.
- `IAiGateway` → `AnthropicAiGateway` (`src/BuildingBlocks/Simulab.Ai`) is the only path to the Claude API. F-66 (email provider) is still `idea`; `docs/infra.md` plans SendGrid.
- Tests that pin the version `2026-v1` against the real content: `tests/Modules/Identity/Simulab.Identity.Tests/SignUpForm.cs` (`CurrentVersion`, used for terms and privacy). Others use their own fixtures.
- No item touched the legal content since F-4 (`git log` on the folder: `5c35e79`, `c319db4`).

## Goal
A person reading the privacy policy learns where their data is stored, which companies process it on Simulab's behalf, and on what legal basis it leaves their country — under LGPD and GDPR.

## Users and use cases
- UC1 A visitor or a signed-in user opens the privacy policy (`/privacy`) in pt-BR, pt-PT or en and reads where their data is hosted, who processes it, and the legal basis of each international transfer.
- UC2 A visitor signing up accepts the privacy policy and the version stored in their `ConsentRecord` is the new one.

## Business rules
- BR1 The privacy policy has a new version `2026-v2` in the three locales, with the same version code in all three (F-4 BR14). The manifest keeps `2026-v1` in its `versions` list and points `currentVersion` to `2026-v2`. The `2026-v1.md` files stay unchanged.
- BR2 `2026-v2` is marked `isPlaceholder: true`: the draft notice (F-4 BR15) stays until a lawyer reviews the text (D1). Its effective date is the build date.
- BR3 `2026-v2` keeps the four sections of `2026-v1` and adds the content of `## Approved list`, in that order.
- BR4 Each processor is named with: the company, what it does for Simulab, which personal data it receives, where it processes it, and the transfer basis under LGPD and under GDPR.
- BR5 The legal basis written for each transfer to the United States is the one each provider's own published terms state on the build date (Data Privacy Framework certification, standard contractual clauses, or both), with the source and the date recorded in `## Decisions`. Nothing is written from memory.
- BR6 The text keeps the plain, short tone of `2026-v1` and does not claim more than the code does (for example, it does not claim encryption or a retention period the code does not apply).
- BR7 The terms of use are unchanged (version `2026-v1`).

## Approved list
Content added to the privacy policy in `2026-v2` (D2, D3):
1. Where your data is stored: Microsoft Azure, region Brazil South (São Paulo), for every user, in the database, the session cache and the file storage (ADR-0002, ADR-0003).
2. Processors:
   1. Microsoft Azure — hosting, database, session cache and file storage; all personal data; Brazil South.
   2. Anthropic — the Claude API behind the study coach (`IAiGateway`); the content of the practice sessions sent to the model; United States.
   3. SendGrid (Twilio) — sending emails (sign-up verification, password reset, account farewell); email address, name and the email content; United States.
3. Transfers for users in the European Union: storage in Brazil rests on Art. 45 GDPR, Implementing Decision (EU) 2026/179 of 2026-01-26 (adequacy of Brazil); transfers to the United States rest on the basis BR5 finds for each provider.
4. Transfers for users in Brazil: storage in Brazil involves no international transfer; transfers to the United States rest on LGPD Art. 33 with the basis BR5 finds for each provider (standard contractual clauses of ANPD Resolution CD/ANPD 19/2024, or the provider's own clauses).
5. A sentence that the list of processors changes when a provider changes, with a new version of the policy.

## Screens and API
- No new screen, route, endpoint or error code. The existing public page (`/privacy`) and the sign-up page show the new version through the existing `LegalDocumentProvider`.

## Acceptance criteria
- AC1 Given the content folder, when the privacy policy is requested in `en`, `pt-BR` and `pt-PT`, then the current version is `2026-v2` in all three and is marked as a placeholder. (BR1, BR2)
- AC2 Given the content folder, when each locale's privacy manifest is read, then it still lists `2026-v1` and its file still exists. (BR1)
- AC3 Given the `2026-v2` privacy text in each locale, when it is rendered, then it names Brazil South, Microsoft Azure, Anthropic and SendGrid, and cites Implementing Decision (EU) 2026/179 and LGPD Art. 33. (BR3, BR4, Approved list)
- AC4 Given a visitor who signs up with privacy version `2026-v2` and terms version `2026-v1`, when the registration is accepted, then their `ConsentRecord` stores `2026-v2` for privacy and `2026-v1` for terms; with privacy `2026-v1` the answer is `registration.terms_version_outdated`. (UC2, BR1, BR7)
- AC5 Given the public `/privacy` page in each of the three UI locales, when it opens through the app host, then it shows version `2026-v2`, the draft notice and the new sections. (UC1) — validation script.
- AC6 Localization: the policy text exists in pt-BR, pt-PT and en; no UI resource key is added, and the missing-key test stays green.

## Criterion → test

| Criterion | Test |
|---|---|
| AC1 | `PrivacyPolicyContentTests.Current_IsVersionTwoAndStillADraft` (x3 locales), `LegalDocumentEndpointTests.GetLegalDocument_ReturnsTheCurrentVersionInTheRequestLanguage` |
| AC2 | `PrivacyPolicyContentTests.Manifest_KeepsVersionOneAndItsFile` (x3) |
| AC3 | `PrivacyPolicyContentTests.Current_NamesTheRegionTheProcessorsAndTheTransferBasis`, `Current_ListsThreeProcessorsInATable`, `VersionTwo_StartsWithTheFourSectionsOfVersionOne` (x3) |
| AC4 | `RegistrationEndpointTests.Register_PreviousPrivacyVersion_IsRefusedAndTheCurrentOnesAreStored`, `Register_ValidForm_CreatesPendingUserConsentAndToken` (consent assertions with `SignUpForm.TermsVersion` / `PrivacyVersion`) |
| AC5 | validation step 2 (app host checked by Claude: `/privacy` shows 2026-v2, the draft notice and sections 5 to 9, en) |
| AC6 | no resource key added; the existing missing-key test is unchanged |

## Decisions
- D1 (owner, 2026-10-04) Claude drafts the text in the three locales and it ships marked as a draft (`isPlaceholder: true`). Reason: the build does not wait for the lawyer; removing the draft mark after review is a small later change.
- D2 (owner, 2026-10-04) The email processor is named SendGrid, the provider `docs/infra.md` plans. Reason: a named processor is clearer to the data subject than a category. F-66's `## Start` carries a line: if it picks another provider, it updates the privacy policy with a new version.
- D3 (owner, 2026-10-04) The policy states the transfer basis under LGPD (users in Brazil) as well as GDPR (users in the EU). Reason: Brazil is the main market and Anthropic and SendGrid are in the United States.
- D4 (owner, 2026-10-04) The new text is a new version `2026-v2`; `2026-v1` stays in the manifest and on disk. Reason: the consent records of earlier sign-ups keep pointing at the text they accepted.
- D5 (owner, 2026-10-04) Existing users are not asked to accept the new version in this item: captured as F-85 (board #128), before the first environment with real users.
- D6 (Claude, 2026-10-04) Technical: the content tests read the real content folder (copied to the test output), so a missing locale or a version mismatch fails the build. `SignUpForm.CurrentVersion` is split into a terms version and a privacy version.
- D7 (Claude, 2026-10-04) Technical: the pt-PT text uses European Portuguese ("RGPD", "subcontratante") and the pt-BR text Brazilian Portuguese ("LGPD", "operador"), not one copied from the other.
- D8 (Claude, 2026-10-04) Not included: Google as a processor (Google sign-in is off in v1, CLAUDE.md), GitHub and the CI (no personal data of users). Reason: the policy lists only who processes users' personal data today.
- D9 (Claude, 2026-10-09) BR5 sources, read from each provider's own published terms on the build date:
  - Anthropic: [Data Processing Addendum](https://www.anthropic.com/legal/data-processing-addendum) (effective 2025-02-24): EU SCCs Module Two and Three incorporated; it does not mention the Data Privacy Framework or the LGPD. [Privacy policy](https://www.anthropic.com/legal/privacy) (effective 2026-09-10), section 11: "In the case of Brazil, we will rely on standard contractual clauses (SCCs)" approved by the ANPD. The text therefore names SCCs only, and no DPF certification.
  - SendGrid (Twilio): [Data Protection Addendum](https://www.twilio.com/en-us/legal/data-protection-addendum) (updated 2026-04-09), Schedule 3: Twilio Inc. self-certified under the EU-US DPF (first in the order of precedence), then EU SCCs (2021/914), then the Brazil SCCs; its BCRs do not cover SendGrid. The text names the DPF with the SCCs as the fallback for the EU, and the ANPD SCCs for Brazil.
  - Microsoft Azure: no transfer basis written. The data-residency page lists Brazil South in the Brazil geo and says copies between regions stay within a geo; the DPA text itself (a .docx) was not read, so the policy states only where the data is stored. To verify: the DPA's transfer clauses and whether support or telemetry access leaves Brazil.
- D10 (Claude, 2026-10-09) The ANPD SCCs named for Anthropic and SendGrid in the Brazil section come from their published statements (Anthropic privacy policy section 11, Twilio DPA Schedule 3 section 2.7); no signed contract was seen.
- D11 (Claude, 2026-10-09) Security scan of this branch ends `BLOCKED: 12 critical, 0 high open`; every finding is `generic-api-key` in files this item does not touch (tests of Identity, `appsettings.Development.json`, `AzurePublishFilesTests.cs`), plus two medium in DocGen. Left for the owner's triage.
- **This text is Claude's reading of public sources, not legal advice** (as in ADR-0003).

## Out of scope
- Removing the draft mark after the lawyer's review (a change on this content, owner).
- Asking existing users to accept a new version: F-85.
- The EU representative (Art. 27 GDPR): F-72.
- Changing the email provider: F-66.
- The terms of use.

## Open questions
(none)

## Change notes

## Validation script
Needed to validate: nothing beyond the local app host, in place now. Data: the AppHost's own volumes (no database change in this item).

1. Start the app host from the item's worktree (stop any other app host first).
   PowerShell: `cd D:\wt\simulab\f-71-privacy-policy-names; dotnet run --project src/Hosts/Simulab.AppHost`
   Git Bash: `cd /d/wt/simulab/f-71-privacy-policy-names && dotnet run --project src/Hosts/Simulab.AppHost`
2. Open the Web URL from the dashboard, then `/privacy` in English. Expect: "Version 2026-v2, effective 10/9/2026", the draft notice, sections 1 to 9, and a table with Microsoft Azure, Anthropic and SendGrid (Twilio).
3. Switch the language to pt-BR, then pt-PT. Expect the same structure in each: "Quem trata seus dados por nós" (pt-BR) with LGPD and "operadoras"; "Quem trata os seus dados por nós" (pt-PT) with RGPD and "subcontratantes" written as in D7. Read the three texts as the person they address.
4. Open `/terms`: still version 2026-v1, unchanged.
5. Open the sign-up page and accept both documents. Expect the privacy link to open the 2026-v2 text and the sign-up to be accepted (no "outdated version" message).
6. Keyboard only: on `/privacy`, Tab reaches the language switch and the footer links; the table is readable at a narrow window.

## Delivery
- Branch `feature/F-71`, merge commit 665b24f (board #113). App version 0.23.0.
- Tests: full suite green, 0 failed (Identity 488, Catalog 512, Web 1229, Architecture 201, Api 47, AppHost 57); new `PrivacyPolicyContentTests` (12 cases) and `Register_PreviousPrivacyVersion_IsRefusedAndTheCurrentOnesAreStored`.
- Scan: `agile scan GREEN` (13 false positive and 3 accepted by the owner, 2026-10-09).
- Manual: `create-account` in pt-BR, pt-PT and en.
