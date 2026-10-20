---
feature: F-84
epic: Foundation and identity
status: building
board: 127
version: 1
---
Autopilot: reviewed
# Rotate the OpenIddict certificates without signing users out

Technical terms: [glossary](../glossary.md)

## Summary
F-73 moves the Api's OpenIddict signing and encryption certificates to Key Vault with 24 months of validity and a manual renewal that signs every user out once (F-73 BR7). Here the Api loads the current and the previous certificate of each role, signs and encrypts with the current one and still reads tokens made with the previous one, so a renewal keeps every session. Raised in the F-73 refinement on 2026-10-04; the owner kept it out of F-73.

## Start
- Depends on: F-73 (certificates in Key Vault) — `done`.
- Waits on (to start): nothing.
- Needed to validate: the staging environment running with the two certificates of F-73, and the owner running the `az` renewal commands on it (they change the shared Azure staging; Claude does not run them).
- Model: opus (door: authentication — it changes which keys the token server trusts)
- Suggested path: `/agile:refine` → `/agile:build` → validation on staging.
- Parallel with: any item that does not touch `IdentityModule.cs`, `OpenIddictCertificates.cs`, `Simulab.Api/Program.cs` or `docs/infra.md` (shared with F-68-style infra edits; expect small conflicts there).

## Goal
A renewal of the OpenIddict certificates must not sign the students out: a token made before the renewal keeps working until it expires, and every token made after it is made with the new certificate.

## What exists (verified 2026-10-21)
- `OpenIddictCertificates.Load` (`OpenIddictCertificates.cs:22`) reads one signing and one encryption certificate from `OpenIddict:SigningCertificate` / `OpenIddict:EncryptionCertificate` and checks private key, key usage and validity (F-73 BR3). `IdentityModule.cs:253-255` registers exactly those two with `AddSigningCertificate` / `AddEncryptionCertificate`. In `Development` the development certificates are used (`IdentityModule.cs:246-250`).
- OpenIddict 7.7.1 (`OpenIddictServerConfiguration.cs`, read from the tag's raw source) already supports several credentials: it sorts them, X.509 ones by the furthest `NotAfter` first (lines 544-545, 580), so the first signs and encrypts; `TokenValidationParameters` gets all signing keys and all decryption keys (lines 556-563); the validation side imports the same credentials through `UseLocalServer` (`OpenIddictValidationServerIntegrationConfiguration.cs:43-49`). The start refuses only when **every** certificate of a role is outside its validity (lines 263-275), so an expired extra one does not stop it.
- Key Vault returns only the newest enabled version of a secret when the Api reads the vault as configuration (`Program.cs:26-28`, `AddAzureKeyVaultSecrets`); the older versions of `OpenIddict--SigningCertificate` and `OpenIddict--EncryptionCertificate` are in the vault but never reach the Api. This is the missing piece.
- The Api already has the vault address (`ConnectionStrings:keyvault`) and the role `KeyVaultSecretsUser` (`AzureDeployment.cs:132-133`). `Azure.Security.KeyVault.Secrets 4.11.1` and `Azure.Identity 1.21.0` are already in the Api's dependency graph (`obj/project.assets.json`; `Directory.Packages.props:124,128`).
- Tokens: access 15 min, refresh 30 days (`TokenLifetimes`). A renewal today invalidates both.
- Items that touched this code since F-73: none.
- Not verified: that the role `KeyVaultSecretsUser` allows listing the versions of a secret (validation step 2 shows it); that the Azure CLI keeps older versions enabled when a new version of a certificate is created (the portal default; validation step 1).

## Users and use cases
- UC1 A signed-in student keeps their session across a certificate renewal, while their refresh token is valid.
- UC2 The operator renews both certificates with one command each, restarts the Api, and nobody has to sign in again.
- UC3 The operator cuts off a compromised old certificate by disabling its version in the vault and restarting the Api.

## Business rules
- BR1 Outside `Development`, besides the current certificate of each role (F-73), the Api also loads the previous one: **the version immediately before the current one** of the same vault certificate (versions ordered by their creation date), and only when that version is enabled. When it is disabled, or there is no older version, there is no previous certificate (an older enabled version is never promoted in its place) and the role works as today.
- BR2 The current certificate is the vault's current version, as the configuration provider reads it (F-73; if that version is disabled the provider leaves the key out and the start refuses, as F-73 BR3 says). The host takes the current and the previous from **one** listing of the versions and loads the previous only when the newest listed version holds the same value as the configured current one; when they differ (a renewal landed between the two reads) the previous is skipped with the warning of BR5. The Api signs and encrypts with whichever loaded certificate expires last (OpenIddict's own ordering); the previous one only validates and decrypts.
- BR3 The previous certificate stays accepted until the next renewal, when it stops being the immediately previous version. No date rule.
- BR4 A previous certificate that cannot do its job (not a PKCS#12 file, no private key, wrong key usage, expired, not valid yet) or that does not expire **earlier** than the current one is ignored with a `Warning` in the log that names the vault certificate and the reason, never the value; the Api starts with the current one. The current certificate keeps the strict start check of F-73 BR3, unchanged.
- BR5 If the Api cannot list the versions of a certificate (permission, network, no answer within a bound of 10 seconds in total, with a limited number of retries) after reading the current one, or the listing disagrees with the configured current value (BR2), it starts with the current only and logs a `Warning` naming the certificate and the reason (exception type), never the value.
- BR6 A disabled version is never loaded as the previous certificate. Disabling the previous version in the vault and restarting the Api cuts that certificate off (tokens it made stop working). Disabling the current version stops the start, as F-73 already does.
- BR7 The private key of a certificate never leaves the vault by any step of the renewal: the operator only creates a new version with the committed policy (F-73 BR5); the Api reads versions with its own identity.
- BR8 A renewal is: run the F-73 create command for each certificate (it adds a version), then restart or redeploy the `api` right away. No mass sign-out: sessions whose tokens a replica can read stay. The overlap runs from the create command until every `api` replica has restarted (a replica started by a scale-out or a crash in between already reads the new version): a replica that still holds only the old certificate refuses a token made with the new one, and the Web ends the session of that user when the Api answers 401 or `invalid_grant` (`AuthClient.cs:145-147`, `WebSessionTokenAccessor.cs:57-65`). So a few users can be signed out in that window. The text says "no mass sign-out", never "zero sign-outs".
- BR9 In `Development` and in a run without a vault nothing changes (development certificates; no previous certificate).
- BR10 No new route, error code, screen or token format; token lifetimes unchanged.
- BR11 At start the Api logs, at `Information`, the role and the thumbprint of every certificate it loaded and whether it is current or previous (thumbprints are public), so the operator can confirm which certificates are trusted.

## Screens and API
- No new route, screen or error code. `/connect/token` and every `/api/v1/` route are unchanged. No wireframe (no screen).
- The previous certificates reach the Identity module as objects handed over in code by the Api host (an internal option the host sets after its vault read), never as configuration keys: an environment variable or a vault secret cannot add a trusted key. No new configuration key.

## Security
### Surfaces
- S1 The Api's start reading the vault (versions list and secret get) with the Api's managed identity — only the Api container.
- S2 Token validation on every authenticated `/api/v1/` route and the token endpoint's refresh flow — any caller holding a token.
### Threats
- T1 (S2, E) A token signed by a key that is neither the current nor the previous certificate is accepted → AC8 [sec]
- T2 (S2, E) A retired or compromised certificate keeps working after the operator disables its version → AC9 [sec]
- T3 (S1, I) A certificate value (PFX) appears in a log line or an exception message → AC10 [sec]
- T4 (S1, D) A failure or a hang listing the versions takes the Api down (every replica that restarts) → AC7 [sec]
- T5 (S1, E) A principal that can set container-app settings or environment variables makes the Api trust a key of their choice → AC16 [sec]
- T6 (S1, R) Nothing records which certificates are trusted, so a cut-off cannot be confirmed → AC17
### Personal data
- None handled by this item. Tokens carry the same claims as today.

## Docs
- flow: new docs/flows/rotate-openiddict-certificates.md — how a renewal keeps sessions, the overlap window and the emergency cut-off.

## Acceptance criteria
- AC1 (BR1, BR2, UC1) Given a first host with certificate pair A and a second host with pair B as current (B expires after A) and pair A as previous, when a user signs in on the first, then the second accepts the access token on an authenticated `/api/v1/` route and redeems the refresh token. (integration, two test hosts)
- AC2 (BR2) Given the second host of AC1, when a user signs in on it, then a third host loaded with pair B only accepts its access token and redeems its refresh token (the new certificate is the one that signs), and the first host, holding pair A only, refuses the access token with 401 (the overlap of BR8 is real and tested). The same holds when the previous pair is registered before the current one. (integration)
- AC3 (BR1) Given only one version of each certificate in the vault, when the Api starts, then it loads no previous certificate and behaves as today. (unit, version selection; existing F-73 tests stay green)
- AC4 (BR1, BR2, BR6) Given the versions of a certificate ordered by creation `v1`, `v2`, `v3` (newest), all enabled, then the previous is `v2`; with `v2` disabled there is no previous (`v1` is not promoted); with `v3` disabled the provider leaves the current out (start refused, F-73) and no previous is looked up. (unit, fake version source)
- AC5 (BR4) Given a previous certificate that is not a PKCS#12 file, has no private key, lacks its key usage, is expired, is not yet valid or does not expire earlier than the current one, when the host starts, then it starts, signs with the current one, and logs a warning that names the certificate and the reason and holds no part of the value. (integration, one case per reason, both roles)
- AC6 (BR4) Given a current certificate with any of those faults, when the host starts, then it still refuses to start as in F-73 (F-73 AC5 tests, unchanged).
- AC7 [sec] (T4, BR5) Given the vault client throws when listing versions, or never answers, or lists a newest version whose value differs from the configured current one, when the host starts, then it starts (within the bound of BR5) with the current certificates only and logs one warning per certificate naming it and the reason. (integration, fake version source: throws, never answers, disagrees)
- AC8 [sec] (T1) Given a token signed and encrypted with a pair that is neither current nor previous, when it reaches the host, then the host answers 401, and a refresh token of that pair is refused with `invalid_grant`. (integration)
- AC9 [sec] (T2, BR6) Given the previous version disabled in the vault after it was loaded, and an older enabled version `v1` still inside its validity, when the host starts, then neither is loaded and a token made by either is refused with 401. (integration, fake version source)
- AC10 [sec] (T3) Given any start outcome of AC5, AC7 and AC9, then no log line or exception message produced by the loading contains the certificate value or its base64. (unit/integration, assertion over the captured log; the Azure SDK's own categories are not covered)
- AC11 (BR9) Given the `Development` environment or a run with no vault connection, when the Api starts, then no version source is called and sign-in works as today. (existing sign-in tests stay green; a test with a source that fails if called)
- AC12 (BR7, BR8, UC2, UC3) `docs/infra.md` step 7 and its renewal paragraph say: create a new version with the committed policy, restart the `api` right away, no mass sign-out, a few users can be signed out until every replica has restarted, disabling the previous version cuts it off, disabling the current one stops the start; the F-73 sentence "a renewal signs every user out once" is replaced. (test over `docs/infra.md`, in the style of `OpenIddictCertificatePolicyTests`)
- AC13 (BR2) Given the publish model, when it is built, then the Api still has `KeyVaultSecretsUser` on the vault and no certificate or version id is carried by a deploy parameter or a committed file. (test, `AzurePublishFilesTests`, extending F-73 AC6)
- AC14 (localization) No UI text is added or changed; the missing-key test stays green. (test)
- AC15 (UC1, UC2, UC3) In staging, a tester signed in shortly before the renewal stays signed in after both certificates are renewed and the `api` restarted, and a tester whose session was refreshed after the renewal survives the cut-off; a second browser signed in before the renewal and left idle is signed out after the previous versions are disabled and the `api` restarted (the cut-off is done within about 10 minutes of that sign-in, before the Web's one-minute-early refresh moves it to the new pair). (validation script)
- AC16 [sec] (T5, BR1) Given the environment variables `OpenIddict__PreviousSigningCertificate` and `OpenIddict__PreviousEncryptionCertificate` hold a valid certificate pair, when the host starts, then they have no effect: that pair is not trusted and its tokens are refused. (integration)
- AC17 (T6, BR11) Given a start with a previous certificate, then the log has one `Information` line per role with the thumbprints and `current` or `previous`, and none holds the value. (integration, captured log)

## Criterion → test

Tests locked: 3a970ad5771dbeb6865f9bebe93c08e96c5e8e97 (5 files); unlocks: 0

| Criterion | Test |
| --- | --- |
| AC1 | `OpenIddictPreviousCertificateTokenTests.Renewal_TokenMadeWithThePreviousPair_TheRenewedHostAcceptsItAndRedeemsTheRefreshToken` |
| AC2 | `OpenIddictPreviousCertificateTokenTests.Renewal_TokenMadeAfterIt_IsMadeWithTheNewPairAndTheOverlapIsReal`, `Options_PreviousRegisteredBeforeOrAfterTheCurrent_TheOneThatExpiresLastComesFirst` |
| AC3 | `PreviousCertificateSelectorTests.Select_OneVersionOnly_HasNoPreviousAndNothingToReport`, `OpenIddictPreviousCertificateStartTests.Start_OneVersionOnly_LoadsNoPreviousCertificate` |
| AC4 | `PreviousCertificateSelectorTests` (three enabled versions, previous disabled, newest disabled) |
| AC5 | `OpenIddictPreviousCertificateStartTests.Start_PreviousCertificateCannotDoItsJob_StartsWithTheCurrentOneAndWarns` (7 reasons x 2 roles) |
| AC6 | `OpenIddictCertificateStartTests.Start_CertificateCannotDoItsJob_RefusesNamingTheSettingAndTheReason` (F-73, unchanged) |
| AC7 | `OpenIddictPreviousCertificateStartTests.Start_ListingFails_StartsWithTheCurrentCertificatesAndWarnsOncePerCertificate` (throws, never answers, disagrees) |
| AC8 | `OpenIddictPreviousCertificateTokenTests.Renewal_TokenMadeWithAnUnknownPair_IsRefusedWith401AndInvalidGrant` |
| AC9 | `OpenIddictPreviousCertificateTokenTests.Renewal_PreviousVersionDisabled_NeitherItNorAnOlderOneIsTrusted`, `OpenIddictPreviousCertificateStartTests.Start_PreviousVersionDisabled_LoadsNoPreviousCertificate` |
| AC10 | `ShouldHoldNoValue` in every `OpenIddictPreviousCertificateStartTests` case of AC5, AC7, AC9 |
| AC11 | `OpenIddictPreviousCertificateStartTests.Start_DevelopmentWithASourceThatMustNotBeCalled_NeverCallsIt`, `Start_NoVaultConnection_HasNoPreviousCertificate` |
| AC12 | `OpenIddictCertificatePolicyTests.Infra_RenewalKeepsSessionsAndDocumentsTheOverlapAndTheCutOff` |
| AC13 | `AzurePublishFilesTests.Publish_CarriesNoPreviousCertificateOrVersionId`, `Publish_KeepsEachHostsKeyRingApartAndGivesOnlyTheApiTheSecrets` (the role) |
| AC14 | the existing missing-key test (no UI text is added) |
| AC15 | validation step |
| AC16 | `OpenIddictPreviousCertificateTokenTests.Renewal_PreviousPairSetAsConfiguration_IsNotTrusted`, `OpenIddictPreviousCertificateStartTests.Start_PreviousCertificateOfferedThroughConfiguration_IsNotLoaded` |
| AC17 | `OpenIddictPreviousCertificateStartTests.Start_WithAPreviousCertificate_LogsTheThumbprintsOfEachRole` |

## Decisions
- 2026-10-21 — Build started in parallel with F-89 (validating) — owner (`/agile:build F-84 --worktree --autopilot`); F-89 also edits `Simulab.Api/Program.cs` and `docs/infra.md`, so expect small conflicts at merge
- 2026-10-21 — plan — 1 contracts: `ICertificateVersionSource`, `CertificateVersion`, selection of the previous version → unit tests (AC3, AC4); 2 loader `PreviousOpenIddictCertificates` (validation, bounded listing, warnings, no value in messages) → AC5, AC7, AC9, AC10; 3 wiring in `IdentityModule` (options configured from the loaded set, hosted logger of thumbprints) → AC1, AC2, AC8, AC16, AC17; 4 Api host: real Key Vault source registered only with a vault outside Development → AC11, AC13; 5 `docs/infra.md` renewal text → AC12; 6 flow page `docs/flows/rotate-openiddict-certificates.md`; each step ends with the affected tests green
- 2026-10-21 — Independent review of the built code (reviewer, opus): 0 blockers, 0 majors, 3 minors, none fixed — technical. (1) a current certificate whose `NotBefore` is up to 5 minutes ahead of OpenIddict's clock could make the previous one sign for those minutes: accepted, the previous stays trusted until the next renewal anyway and the window is minutes; (2) the checks repeat those of `OpenIddictCertificates.Read`: accepted, kept apart so the F-73 start check stays untouched; (3) the current PKCS#12 is parsed a second time: accepted, once per start
- 2026-10-21 — Review coverage: security T1-T6 against the code and tests, data (no schema), contracts (no route or error code), localization (no UI text), locks (`git diff 3a970ad` empty on the 5 locked files), tests (AC1-AC14, AC16, AC17 map to tests that can fail), rules — technical
- 2026-10-21 — Traps: T1 the blocking vault read sits in a singleton that the server options depend on, resolved at start only because `OpenIddictCertificateReporter` injects it; T2 two copies of the locked fake version source; T3 the real source lists every version of the secret before sorting (negligible at one renewal per 24 months) — technical
- 2026-10-21 — Scan: finding 0f4dea5d (the public Azure role id of Key Vault Secrets User in `AzurePublishFilesTests.cs:388`, moved down by this item's test) closed as false positive by Claude with the reviewer's confirmation; finding 7ba94396 (the same id, in the history at commit 3a970ad of this item's own test commit) is a history finding, which only the owner closes — technical; the run stopped there
- 2026-10-21 — The current certificate keeps the strict start check of F-73 BR3 — F-73 decision of 2026-10-04, "Missing or invalid certificates outside Development stop the Api's start" — confirmed: docs/features/F-73-shared-openiddict-keys-replicas.md
- 2026-10-21 — The private key never leaves the vault, so the previous certificate is not copied by hand to another secret — F-73 decision of 2026-10-04, "Key Vault creates the certificates from a committed policy" — confirmed: docs/features/F-73-shared-openiddict-keys-replicas.md
- 2026-10-21 — The Api takes the previous certificate from the version immediately before the current one in the vault, read at start with the Api's own identity — owner; no manual copy, one command per certificate to renew — asked (sharpened by the review: "immediately previous, and enabled", never "the newest older enabled one")
- 2026-10-21 — The previous certificate stays accepted until the next renewal — owner; the refresh token lives 30 days, so the margin is large and there is no date rule to get wrong — asked
- 2026-10-21 — An unusable previous certificate is ignored with a warning — owner; it only widens what is accepted, so it must not take the Api down — asked
- 2026-10-21 — The overlap of the redeploy is accepted and documented, no two-phase rollout — owner; short window, affects only a request that meets the old revision with a new token — asked
- 2026-10-21 — If the versions cannot be listed the Api starts with the current certificates and warns — owner; availability over a rare sign-out — asked
- 2026-10-21 — Renewal stays manual and the Api does not reload certificates without a restart — owner; hot reload would remove the overlap but is much more code, a possible future item — asked
- 2026-10-21 — The signer is chosen by OpenIddict's own ordering (furthest `NotAfter` first), not by code of ours — technical; read in `OpenIddictServerConfiguration.cs:544-580` of 7.7.1, so no reordering hack and the order of the registration does not matter
- 2026-10-21 — The versions are read in the Api host (`Program.cs`, only with a vault connection) and handed to the Identity module in code, not as configuration keys — technical (review finding 5); a configuration key could be set by a container-app writer through an environment variable, a power nobody has over the current certificate today because the vault is added last
- 2026-10-21 — The previous version is chosen from one listing ordered by creation date and used only when the newest listed value equals the configured current one — technical (review findings 2 and 3); the provider exposes no version id, and a renewal between the two reads would shift the previous by one
- 2026-10-21 — The provider's current is "the vault's current version" (a disabled current version makes the key absent and the start refuse, `AzureKeyVaultConfigurationProvider.cs:82`, package 1.5.2, read from the tag's raw source) — technical (review finding 1); the first draft's "newest enabled version" was false
- 2026-10-21 — The promise is "no mass sign-out", not "sessions are kept": the Web ends a session on a 401 from the session lookup and on `invalid_grant` (`AuthClient.cs:145-147`, `WebSessionTokenAccessor.cs:57-65`), so during the overlap a few users can be signed out — technical (review finding 4); `docs/infra.md:107` already said so; the owner reads this in the summary
- 2026-10-21 — A previous certificate that does not expire earlier than the current is refused — technical (review trap T1); if the policy's validity ever shortens, the old certificate would otherwise keep signing
- 2026-10-21 — The previous certificate stays trusted until the next renewal, up to about 24 months if never replaced; the cut-off is the manual disable plus restart of UC3 — owner (asked) with the cost named by the review (trap T2)
- 2026-10-21 — The listing of versions has a 10-second bound with limited retries, and the loaded thumbprints are logged at `Information` — technical (review finding 6)
- 2026-10-21 — AC10 covers the Api's own log lines, not the Azure SDK categories; version ids are not secret — technical (review finding 9)
- 2026-10-21 — Approved by the owner ("aprovo F-84") after reading the file, including the narrower promise "no mass sign-out" — owner
- 2026-10-21 — Independent review (change-review, opus) of the refined file: 1 blocker and 5 majors, all confirmed and fixed here; minors 6, 8 and 9 fixed; none rejected — technical
- 2026-10-21 — Manual pages unchanged — technical; no visible behavior beyond staying signed in

## Out of scope
- Automatic renewal by the vault (`lifetimeActions`) and certificate hot reload without a restart — owner; a possible future item.
- A two-phase rollout that removes the overlap window of BR8.
- Rotation of the Data Protection keys (F-64) and of the client secret.
- Any change to token lifetimes or formats.
- Changes to the policies' validity (24 months) or key usage (F-73).

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build. -->

## Events

- 2026-10-21 test-fail — 2 new warning(s): CA1873 in src/Modules/Identity/Simulab.Identity.Infrastructure/OpenIddictCertificateReporter.cs; CA106, 2 reds
- 2026-10-21 test-fail — tests failed (Simulab.slnx): Simulab.AppHost.Tests.OpenIddictCertificatePolicyTests.Infra_RenewalKeepsSessionsAndDocumen, 1 red
- 2026-10-21 stop — scan: history finding 7ba94396 needs the owner

## Delivery
<!-- Filled by /agile:ship. -->
