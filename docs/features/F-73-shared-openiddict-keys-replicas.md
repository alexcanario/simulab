---
feature: F-73
epic: Foundation and identity
status: building
board: 115
version: 2
---
# Shared OpenIddict keys across Api replicas

Technical terms: [glossary](../glossary.md)

## Summary
Found while refining F-54 (2026-10-04): the Api signed and encrypted its tokens with development certificates, so each replica would use its own and a token issued by one replica would be rejected by another. **Since v2 (2026-10-09):** F-64 already loads both certificates from configuration outside `Development` (Key Vault in the cloud) and refuses to start without them. What this item still does: lift the Api's `maxReplicas: 1` cap that F-54 added (to 2), prove with two hosts that a token crosses replicas, make the start check stricter (private key, key usage, validity), and commit the certificate policy (24 months) with the renewal documented.

## Start
- Depends on: F-54 (adds the cap this item lifts) — `done`; F-64 (loads the certificates from configuration; creates the staging environment) — `done`.
- Waits on (to start): nothing (F-54 and F-64 are merged).
- Needed to validate: the staging environment running and the two certificates created in its Key Vault with the command of BR5 — the owner.
- Suggested path: `/agile:build` → validation on staging.
- Parallel with: any item that does not touch `IdentityModule.cs`, `AzureDeployment.cs` or `AzurePublishFilesTests`; `docs/infra.md` is shared (expect small conflicts with F-54, F-64, F-68).

## Goal
Every Api replica, and every new revision after a deploy or a restart, must accept the tokens any other replica issued, so users stay signed in and the Api can run more than one replica.

## What existed at refinement (verified 2026-10-04; superseded where the v2 section below says so)
- `IdentityModule.cs:220` calls `AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate()` in every environment. OpenIddict 7.7.1 (`OpenIddictServerBuilder.cs`, read from the tag's raw source) looks for the certificate in `X509Store(StoreName.My, StoreLocation.CurrentUser)` and creates a self-signed one (RSA 4096, 2 years) when none is valid.
- In a container that store lives in the container's file system. So the premise of the summary is true and incomplete: besides two replicas disagreeing, **every new revision (each deploy) and every restart (each staging start after a park) gets new keys**, and every access token (15 min, `TokenLifetimes.AccessToken`) and refresh token (30 days, `TokenLifetimes.RefreshToken`) already issued becomes unreadable: every user is signed out. This holds with one replica too, so the F-54 cap does not prevent it.
- Only the Api holds the keys: it is the OpenIddict server and validates with `UseLocalServer()` (`IdentityModule.cs:235`, `Program.cs:72`). The Web only calls the token endpoint (`AuthClient.cs`) and never validates a token.
- The publish model already has `builder.AddAzureKeyVault("keyvault")` (`src/Hosts/Simulab.AppHost/AzureDeployment.cs:19`, F-62). `Aspire.Hosting.Azure.KeyVault` 13.6.0 offers `GetSecret(string)` (its XML docs, read from the package), which a project resource can take as an environment value.
- A certificate created in Key Vault also exposes its PFX (private key included, base64, no password) as a secret with the same name; Key Vault offers no ARM/Bicep way to create a certificate, only the data-plane command (`az keyvault certificate create`).
- The Api's cloud scale today on `main`: `minReplicas: 1`, no maximum (Azure Container Apps default: up to 10). F-54 (approved) sets `maxReplicas: 1` (its BR7, AC10); this item replaces that cap.
- Tests: `tests/Hosts/Simulab.AppHost.Tests/AzurePublishFilesTests.cs` already reads the generated Bicep (`web` has `minReplicas: 0` and `maxReplicas: 1`, line 97). The test hosts run as `Development` (the `WebApplicationFactory` default).
- Items that touched this code since F-5: F-11 and F-20 (custom flows in the same `AddServer` block), F-62 (`AzureDeployment.cs`); none changed the certificates.

## What exists (verified 2026-10-09, v2)
- Delivered by F-64: `IdentityModule.cs:244-257` uses the development certificates only in `Development`; elsewhere `OpenIddictCertificates.Load` (`OpenIddictCertificates.cs`) reads `OpenIddict:SigningCertificate` and `OpenIddict:EncryptionCertificate` (base64 PKCS#12, no password) and throws, naming the key, when one is missing or is not a PKCS#12 file. It does **not** check the private key, the key usage or the validity period.
- The Api reads Key Vault as configuration (`AzureDeployment.cs:121-133`, role `KeyVaultSecretsUser`); a vault certificate `OpenIddict--SigningCertificate` is the key `OpenIddict:SigningCertificate`. No Container Apps secret reference exists or is needed.
- Tests today: `OpenIddictCertificateStartTests` (refusals, `tests/Hosts/Simulab.Api.Tests`) and `OpenIddictCertificateRestartTests` (same certificates keep a token valid, other certificates refuse it; `tests/Modules/Identity/Simulab.Identity.Tests`). No test runs two hosts at once.
- `docs/infra.md` step 7 creates the certificates with `az keyvault certificate get-default-policy` (exportable, 12 months).
- `AzureDeployment.cs:159` sets `MaxReplicas = 1` for the Api with a comment pointing to this item; `AzurePublishFilesTests.cs:97` asserts it.

## Users and use cases
- UC1 A signed-in student keeps using the app when a request lands on another Api replica: no sign-out, no error.
- UC2 A signed-in student keeps their session across a deploy, a restart of the Api and a staging park and start, while the refresh token is valid.
- UC3 The operator creates the two certificates once per environment with one declared command, and a deploy with a missing or expired certificate fails at the Api's start with a message naming what is missing.

## Business rules
- BR1 (delivered by F-64, kept as context) Outside `Development`, the Api signs tokens with one signing certificate and encrypts them with one encryption certificate, both read from configuration (`OpenIddict:SigningCertificate`, `OpenIddict:EncryptionCertificate`: base64 PKCS#12, no password). Every replica and every revision of an environment reads the same two values.
- BR2 (delivered by F-64, kept as context) In `Development` (local runs and the test hosts) the development certificates stay as today.
- BR3 Beyond what F-64 checks (missing value, not a PKCS#12 file), outside `Development` the Api refuses to start when either certificate has no private key, lacks its key usage (signing: digital signature; encryption: key encipherment) or is outside its validity period; the start error names the configuration key and the reason, never the value.
- BR4 (changed in v2) In the cloud, both values come from the environment's Key Vault read as configuration, as F-64 already does (certificates `OpenIddict--SigningCertificate` and `OpenIddict--EncryptionCertificate`); never as a deploy parameter or a committed file. No Container Apps secret reference is added.
- BR5 Key Vault itself creates both certificates (self-signed, RSA 2048 or more, valid 24 months) from a policy file committed in the repository, with one command per certificate declared in `docs/infra.md` (replacing `get-default-policy`); the private key never leaves the vault. Run once per environment, before its first deploy that includes this item.
- BR6 The Api's publish model allows up to 2 replicas (`minReplicas: 1`, `maxReplicas: 2`), in staging and in production; this replaces F-54's `maxReplicas: 1`.
- BR7 Renewal is manual and documented in `docs/infra.md` (create a new version of each certificate, redeploy); a renewal signs every user out once. Rotation without sign-out is F-84.

## Screens and API
- No new route, screen or error code. `/connect/token` and every `/api/v1/` route are unchanged.

## Acceptance criteria
- AC1 (BR1) Given two Api hosts started outside `Development` with the same two configured certificates, when a user signs in on the first, then the second accepts its access token on an authenticated `/api/v1/` route and redeems its refresh token. (integration, two test hosts)
- AC2 (UC2, BR1) Given a host outside `Development` that issued a refresh token, when the host is disposed and a new one starts with the same certificates, then the refresh token is redeemed. (integration)
- AC3 (BR1) Given two hosts with different signing certificates, when the second receives the first's access token, then it answers 401 — the test proves AC1 is not passing by accident. (integration)
- AC4 (BR2) Given the `Development` environment with no configured certificates, when the Api starts, then sign-in works as today. (existing sign-in tests stay green unchanged)
- AC5 (BR3) Given an environment other than `Development`, when the Api starts with a certificate without a private key, with the wrong key usage, expired or not yet valid, then the start fails with a message naming `OpenIddict:SigningCertificate` or `OpenIddict:EncryptionCertificate` and the reason, and the message contains no part of the value. The missing and not-a-PKCS#12 cases stay covered by `OpenIddictCertificateStartTests` (F-64). (unit, one case per new reason and key)
- AC6 (BR4, changed in v2) Given the publish model for `Staging` and `Production`, when it is built, then no deploy parameter and no committed file carries either certificate. (test, `AzurePublishFilesTests`)
- AC7 (BR6) Given the publish model, when it is built, then `api` has `minReplicas: 1` and `maxReplicas: 2`, and `web` keeps `minReplicas: 0` and `maxReplicas: 1`. (test, `AzurePublishFilesTests`, replacing F-54's AC10)
- AC8 (BR5) Given the committed certificate policy, then it asks for a self-signed certificate, RSA 2048 or more, 24 months, exportable key, with the key usage BR3 requires for its role. (unit, reads the policy file)
- AC9 (BR5, BR7) `docs/infra.md` step 7 uses the committed policy instead of `get-default-policy`, and declares the renew command and its effect (everybody signed out once); the row of `OpenIddict:SigningCertificate` and `OpenIddict:EncryptionCertificate` stays accurate. (review at ship)
- AC10 (localization) No UI text is added or changed; the missing-key test stays green. (test)
- AC11 (UC1, UC2) In staging with two Api replicas running, a signed-in tester keeps working across many requests and across a redeploy of the Api. (validation script)

## Criterion → test
| Criterion | Test |
| --- | --- |
| AC1 | pending |
| AC2 | pending |
| AC3 | pending |
| AC4 | pending |
| AC5 | pending |
| AC6 | pending |
| AC7 | pending |
| AC8 | pending |
| AC9 | pending |
| AC10 | pending |
| AC11 | pending |

## Decisions
- 2026-10-04 — The certificates reach the Api as Key Vault secret references in Container Apps, not through the Key Vault SDK in the Api nor through Data Protection — owner; no new package, no managed-identity code in the Api, and no coupling with F-64's Data Protection keys.
- 2026-10-04 — Key Vault creates the certificates from a committed policy — owner; the private key never passes through a laptop.
- 2026-10-04 — 24 months of validity, manual renewal documented; rotation without sign-out becomes F-84 (#127) — owner; a renewal every two years that signs users out once is acceptable for v1.
- 2026-10-04 — Missing or invalid certificates outside `Development` stop the Api's start — owner; a broken deploy shows in the first minute instead of as silent sign-outs.
- 2026-10-04 — `maxReplicas: 2` in staging and production — owner; a number the validation can prove, with a predictable cost; raising it is a one-line change.
- 2026-10-04 — Two certificates (signing, encryption), not one for both — technical; OpenIddict recommends separate keys and BR3 checks a distinct key usage for each.
- 2026-10-04 — The configuration carries the PFX as Key Vault exposes it (base64, no password), loaded with `X509CertificateLoader.LoadPkcs12` — technical; that is the secret's own format, so no conversion step exists between vault and Api.
- 2026-10-04 — No new package: `Aspire.Hosting.Azure.KeyVault` 13.6.0 is already in `Directory.Packages.props`; the Api uses only `System.Security.Cryptography` — verified.
- 2026-10-04 — The tests generate throwaway self-signed certificates in memory (`CertificateRequest`); no certificate file is committed — technical; nothing secret or expiring in the repository.
- 2026-10-04 — The build waits for F-54's merge and branches the work from `main` after it — technical; both items edit the Api's scale in `AzureDeployment.cs` and the same assertion in `AzurePublishFilesTests`.
- 2026-10-04 — Approved by the owner ("aprovo F-73"); the build waits for F-54's merge, the validation for F-64's staging — owner.
- 2026-10-04 — The app manual is not changed (no visible behavior changes beyond staying signed in) — technical.
- 2026-10-09 — The build stopped on a false premise: F-64 (done) already loads the certificates from configuration and refuses to start without them. Owner chose option a (reduce F-73 to what is missing); see change note v2.
- 2026-10-09 — Parallel build with F-93 (validating) confirmed by the owner; only `docs/infra.md` is shared. The branch was brought up to date with `main` before the first line of code.

## Out of scope
- Rotation without sign-out (the Api loads the current and the previous certificate) — F-84 (#127, idea).
- Data Protection keys and `ForwardedHeaders` for the cloud ingress — F-64.
- More than one Web instance (its sign-in tickets are in memory, `docs/infra.md`).
- Creating the staging and production environments and their first deploy — F-64 and the first release.
- Any change to token lifetimes or token formats.

## Open questions
- (none)

## Change notes
### v2 — 2026-10-09
- What: F-73 is reduced to what F-64 did not deliver. BR1 and BR2 become context (delivered by F-64, with the keys `OpenIddict:*`); BR3 and AC5 keep only private key, key usage and validity; BR4 is rewritten (the Api reads the vault as configuration, no secret reference) and AC6 with it; BR5, BR6, BR7, AC1 to AC4, AC7 to AC11 stay.
- Why: the build found that F-64 already loads the certificates outside `Development` and refuses to start without them (`IdentityModule.cs:244-257`, `OpenIddictCertificates.cs`, `OpenIddictCertificateStartTests`).
- Affected: Summary, Start, BR1 to BR5, AC5, AC6, AC9, Validation script steps 1 and 2.
- Re-approved: 2026-10-09 ("aprovo F-73").

## Validation script
1. With staging running (F-64), run the two create commands of `docs/infra.md` step 7 (now with the committed policy) against its Key Vault → both certificates are listed by `az keyvault certificate list --vault-name <vault>` (a certificate created earlier with the default policy is created again as a new version).
2. Deploy with the declared staging command → the deploy finishes; in the portal, the `api` container app shows scale 1 to 2 and the Api starts (no certificate error in its log).
3. Set the `api` minimum to 2 for the test (`az containerapp update -n api -g <rg> --min-replicas 2`) → two replicas running.
4. Sign in on the staging Web and use the app for a few minutes (open exams, change pages, at least 20 requests) → no sign-out, no error page.
5. Deploy again with the same command (a new revision) and keep using the app in the same browser → still signed in, no sign-in page.
6. Set the minimum back (`--min-replicas 1`) → one replica running.

## Delivery
- Branch: feature/F-73
