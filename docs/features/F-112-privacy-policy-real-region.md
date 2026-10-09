---
feature: F-112
epic: Cloud hosting and operations
status: refining
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
