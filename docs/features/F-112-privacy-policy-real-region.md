---
feature: F-112
epic: Cloud hosting and operations
status: building
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
- Parallel with: nothing in progress touches the Identity legal content or the sign-up tests (checked 2026-10-09: F-73, F-76, F-77, F-78, F-86, F-89 are other areas). F-85 (existing users accept a new legal version, `idea`) touches the same consent code and runs after it.

## What already exists (checked 2026-10-09)
- F-93 is `done` and merged (`a06d53c`, version 0.24.0): ADR-0004 (region `centralus`, a legal-basis block per population) and `docs/privacy/data-processing-register.md` are on `main`. ADR-0004 decision 7 leaves the policy text to this item.
- The privacy policy `2026-v2` is files, not code: `src/Modules/Identity/Simulab.Identity.Infrastructure/Content/Legal/<en|pt-BR|pt-PT>/privacy/manifest.json` plus `2026-v1.md` and `2026-v2.md` (F-4 BR14, F-71). No item touched that folder since F-71 (`f492eb3`).
- `2026-v2` has nine sections: sections 1 to 4 are `2026-v1` word for word; 5 (Brazil South), 6 (a table of Azure, Anthropic, SendGrid), 7 (EU: Art. 45, Decision (EU) 2026/179, then US providers), 8 (Brazil: LGPD Art. 33 for the US providers), 9 (the list changes with a new version). It is wrong in three ways: the region (Central US, ADR-0004), the email processor (Azure Communication Services since F-66; no code names SendGrid), and the Art. 45 sentence (adequacy for Brazil no longer covers where data is stored).
- The email service has data location Brazil (`src/Hosts/Simulab.AppHost/Bicep/email.bicep:13`, `docs/infra.md:223`); the rest of staging is in Central US. ADR-0004 says its delivery path outside that location is not verified.
- `IAiGateway` is called only by `AiDiagnosticsEndpoints.cs` (Development): no user content reaches Anthropic today (ADR-0004 context). The `2026-v2` table says Anthropic receives "the content of your practice sessions".
- ADR-0004 decision 3 marks as `basis not in place`: Microsoft for users in Brazil, Anthropic for users in Brazil; Microsoft for users in the EU is "Data Privacy Framework stated, scope not confirmed". It also says real users' data must not reach staging or production while a basis is not in place (staging holds test data only).
- Sign-up stores the privacy version in `ConsentRecord` and refuses an outdated one (`registration.terms_version_outdated`). Tests that pin `2026-v2`: `SignUpForm.PrivacyVersion`, `PrivacyPolicyContentTests`, `LegalDocumentEndpointTests` (date `2026-10-09`), `RegistrationEndpointTests.Register_PreviousPrivacyVersion_IsRefusedAndTheCurrentOnesAreStored`.
- The app manual (`docs/manual/<locale>/create-account.md`, line 12) repeats "Brazil South" and "SendGrid" in the three languages.
- No new screen, route, endpoint, error code, resource key or package is needed.

## Goal
A person reading the privacy policy learns where their data is really stored (Azure Central US), which companies process it today, and on what legal basis it crosses a border, with the same honesty as ADR-0004: a basis that is not in place yet is said to be so.

## Users and use cases
- UC1 A visitor or a signed-in user opens `/privacy` in pt-BR, pt-PT or en and reads the real region, the real processors and the transfer basis for their population (Brazil or the EU).
- UC2 A visitor signing up accepts the privacy policy and the version stored in their `ConsentRecord` is `2026-v3`.
- UC3 The owner or a lawyer reads the policy and finds nothing that ADR-0004 or the register contradicts.

## Business rules
- BR1 The privacy policy has a new version `2026-v3` in the three locales, with the same version code in all three (F-4 BR14). Each manifest keeps `2026-v1` and `2026-v2` in `versions` and points `currentVersion` to `2026-v3`. The `2026-v1.md` and `2026-v2.md` files stay unchanged (the consent records of earlier sign-ups keep pointing at the text they accepted, F-71 D4).
- BR2 `2026-v3` is marked `isPlaceholder: true`: the draft notice stays until a lawyer reviews the text (F-71 D1, F-93 D4). Its effective date is the build date.
- BR3 `2026-v3` keeps sections 1 to 4 of `2026-v2` word for word and replaces sections 5 to 9 with the content of `## Approved list`, in that order.
- BR4 Each processor row names the company, what it does for Simulab, which personal data it receives, and where it processes it (as in F-71 BR4).
- BR5 Every transfer basis written in the text comes from ADR-0004 decision 3 and the provider's own published terms, with the source and the date recorded in `## Decisions`; nothing is written from memory. A basis ADR-0004 marks `basis not in place` is said in the text to be in preparation, never as if it were in place (D2).
- BR6 The text does not claim more than the code does: Anthropic is listed with "today no data is sent" (D1); no encryption, retention period or purpose the code does not apply is claimed (F-71 BR6).
- BR7 The terms of use are unchanged (version `2026-v1`).
- BR8 The app manual page `create-account` in the three languages names the real region and processors (no "Brazil South", no "SendGrid").
- BR9 A signed-in user is not asked to accept `2026-v3` here: F-85. Sign-up accepts `2026-v3` and refuses `2026-v2` (`registration.terms_version_outdated`).

## Approved list
Content of sections 5 to 9 of `2026-v3` (D1, D2, D3):
1. Where your data is stored: Microsoft Azure, region Central US (Iowa, United States), for every user, in the database, the session cache and the file storage. Emails are the exception: their service stores its data in Brazil (item 2).
2. Processors, as a table with a header and three rows:
   1. Microsoft Azure — hosting, database, session cache and file storage; all the data described in section 1; Central US (United States).
   2. Microsoft Azure Communication Services — sending emails (sign-up verification, password reset, password changed, account farewell); email address, name and the content of the email; data location Brazil.
   3. Anthropic — the Claude API behind the study coach; the content of your practice sessions, when the study coach is on. Today no data of yours is sent to it; United States.
3. If you are in the European Union: storing your data in the United States rests on the EU-US Data Privacy Framework (Art. 45 GDPR) when the receiver is certified for the data, with the standard contractual clauses approved by the European Commission (Decision (EU) 2021/914) as the fallback (Art. 46 GDPR). The adequacy decision for Brazil (Implementing Decision (EU) 2026/179) covers the emails stored in Brazil. For each company the text states the safeguard its own published terms give (D4); where a safeguard is not confirmed yet, the text says so.
4. If you are in Brazil: storing your data in the United States is an international transfer (Art. 33 LGPD). The mechanism is the standard contractual clauses approved by the ANPD (Resolution CD/ANPD 19/2024). The text says that they are being put in place with Microsoft and, before the study coach sends data, with Anthropic (D2). Emails stored in Brazil involve no transfer for data at rest.
5. A sentence that, until those safeguards are in place, the service holds test accounts only (D2).
6. A sentence that the list of companies changes with a new version of the policy, and that this version replaces `2026-v2`, which named Brazil South and SendGrid.

## Screens and API
- No new screen, route, endpoint or error code. The public page `/privacy` and the sign-up page show the new version through the existing `LegalDocumentProvider`.

## Acceptance criteria
- AC1 Given the content folder, when the privacy policy is requested in `en`, `pt-BR` and `pt-PT`, then the current version is `2026-v3` in all three and is marked as a placeholder. (BR1, BR2)
- AC2 Given each locale's privacy manifest, when it is read, then it lists `2026-v1`, `2026-v2` and `2026-v3`, and the three files exist. (BR1)
- AC3 Given the `2026-v3` text in each locale, when it is rendered, then it names Central US, Microsoft Azure, Azure Communication Services, Anthropic, the Data Privacy Framework, Decision (EU) 2026/179 and the LGPD article 33, and names neither Brazil South nor SendGrid. (BR3, BR4, Approved list)
- AC4 Given the `2026-v3` text, when the processors table is rendered, then it has a header and three rows, and the Anthropic row says no data is sent today. (BR4, BR6)
- AC5 Given the `2026-v3` text, when sections 1 to 4 are compared with `2026-v1`, then they are identical word for word, and the text says the safeguards for users in Brazil are being put in place and that the service holds test accounts only. (BR3, BR5, D2)
- AC6 Given a visitor who signs up with privacy `2026-v3` and terms `2026-v1`, when the registration is accepted, then the `ConsentRecord` stores `2026-v3` and `2026-v1`; with privacy `2026-v2` the answer is `registration.terms_version_outdated`. (UC2, BR7, BR9)
- AC7 Given the public `/privacy` page in each of the three UI locales, when it opens through the app host, then it shows version `2026-v3`, the draft notice and sections 5 to 9. (UC1) — validation script.
- AC8 Given the manual page `create-account` in the three languages, when it is read, then it names Central US and the three processors and does not name Brazil South or SendGrid. (BR8)
- AC9 Localization: the policy text exists in pt-BR, pt-PT and en; no UI resource key is added and the missing-key test stays green.

## Criterion → test

| Criterion | Test |
|---|---|
| AC1 | `PrivacyPolicyContentTests.Current_IsVersionThreeAndStillADraft` (x3), `LegalDocumentEndpointTests.GetLegalDocument_ReturnsTheCurrentVersionInTheRequestLanguage` |
| AC2 | `PrivacyPolicyContentTests.Manifest_KeepsEarlierVersionsAndTheirFiles` (x3) |
| AC3 | `PrivacyPolicyContentTests.Current_NamesTheRealRegionAndProcessors`, `Current_NamesNeitherTheOldRegionNorTheOldEmailProvider` (x3) |
| AC4 | `PrivacyPolicyContentTests.Current_ListsThreeProcessorsInATable`, `Current_SaysAnthropicReceivesNothingToday` (x3) |
| AC5 | `PrivacyPolicyContentTests.VersionThree_StartsWithTheFourSectionsOfVersionOne`, `Current_SaysSafeguardsAreBeingPutInPlace` (x3) |
| AC6 | `RegistrationEndpointTests.Register_PreviousPrivacyVersion_IsRefusedAndTheCurrentOnesAreStored` (with `SignUpForm.PrivacyVersion`) |
| AC7 | validation steps 2 to 4 (app host checked by Claude) |
| AC8 | `PrivacyPolicyContentTests.Manual_NamesTheRealRegionAndProcessors` (x3) |
| AC9 | no resource key added; the existing missing-key test is unchanged |

## Decisions
- D1 (owner, 2026-10-09) Anthropic stays in the table with "today no data is sent", over removing it for now or keeping the v2 claim. Reason: it does not claim more than the code does and needs no new version when the coach is turned on. Recommended option chosen.
- D2 (owner, 2026-10-09) The text says plainly that the ANPD clauses for users in Brazil (Microsoft, and Anthropic before the coach sends data) are being put in place, and that the service holds test accounts only until they are. Reason: it matches ADR-0004 decision 3 and its D12; the draft mark stays and a lawyer adjusts the tone later. Recommended option chosen.
- D3 (owner, 2026-10-09) Azure Communication Services gets its own table row, with data location Brazil. Reason: its region differs from the rest of staging. Recommended option chosen.
- D4 (Claude, 2026-10-09) Technical: the build reads the Microsoft entry in the Data Privacy Framework list (dataprivacyframework.gov) and the Microsoft addendum text, which ADR-0004 left unread, and writes only what they say, with source and date in this section (BR5). If the scope for Azure is still not confirmed, the EU paragraph says so in plain words instead of stating it as in place; the ADR-0004 actions stay with the owner.
- D5 (Claude, 2026-10-09) Technical: the content tests that pinned `2026-v2` now pin `2026-v3`; `SignUpForm.PrivacyVersion` becomes `2026-v3`. `PrivacyPolicyContentTests` gains the cases of AC3 to AC5 and AC8, with one marker per locale for the Anthropic and "being put in place" sentences. pt-PT keeps European Portuguese ("RGPD", "subcontratantes") and pt-BR Brazilian ("LGPD", "operadoras"), as in F-71 D7.
- D6 (Claude, 2026-10-09) Technical: no data migration. `ConsentRecord` rows of `2026-v2` stay as evidence; staging holds test accounts only (ADR-0004). Asking existing users to accept `2026-v3` is F-85.
- D7 (Claude, 2026-10-09) Technical: no new package, route, resource key or schema change; `docs/glossary.md` needs no new word.
- **This text is Claude's reading of public sources, not legal advice** (as in ADR-0003 and ADR-0004).

## Out of scope
- Removing the draft mark after the lawyer's review (owner).
- Asking signed-in users to accept the new version: F-85.
- Obtaining the ANPD clauses from Microsoft and Anthropic, and reading the Microsoft addendum for the owner's actions in ADR-0004: owner.
- The data protection officer (LGPD Art. 41) and the incident procedure; the EU representative (F-72).
- Moving the region; changing any code other than the version constants in tests.
- The terms of use.

## Open questions
(none)

## Change notes

## Validation script
(written at build)
