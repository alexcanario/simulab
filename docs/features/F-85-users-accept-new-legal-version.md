---
feature: F-85
epic: Foundation and identity
status: refining
board: 128
version: 1
---
# Existing users accept a new legal document version

Technical terms: [glossary](../glossary.md)

## Summary
Today only sign-up checks the terms and privacy versions (`RegistrationTerms.CheckAsync`); a signed-in user is never told when the privacy policy or the terms move to a new version. Show a notice, or ask for a new acceptance, at sign-in when the current version differs from the one in the user's `ConsentRecord`. Raised as out of scope by the F-71 refinement, 2026-10-04. Must exist before the first environment with real users.

## Start
- Depends on: nothing (F-71 and F-112 already moved the privacy policy to `2026-v3`; the terms are at `2026-v1`).
- Waits on (to start): nothing.
- Needed to validate: nothing beyond the local app host. Claude prepares the user with an older acceptance (a row in the worktree's own database); the seeded administrator has no acceptance at all and covers the other case.
- Model: opus (signal 1: a door — legal evidence and personal data behind a new authenticated endpoint)
- Suggested path: `/agile:build F-85` (no `/agile:screen`: one simple page, wireframe below).
- Parallel with: any item that does not touch the Identity consent code, the main layout or the sign-up tests.

## What already exists (checked 2026-10-21)
- `ConsentRecord` (`Simulab.Identity.Domain/Entities/ConsentRecord.cs`): one row per acceptance with `TermsVersion`, `PrivacyVersion`, `DeclaresAdult`, `Locale`, `AcceptedAt`, `IpAddress`; "written once, never deleted" (F-4 BR6); only `IpAddress` is cleared when the account is erased (F-10 BR9). `IConsentRecordStore` has `AddAsync` only; there is no read of a user's latest acceptance anywhere.
- Rows are written by `RegisterUserHandler` and `RegisterGoogleUserHandler`. The seeded administrator (`SeedAdmin.cs`) gets no row.
- `RegistrationTerms.CheckAsync` compares the versions the page showed with `ILegalDocumentProvider.GetCurrentAsync`; a document the server cannot read is skipped, not blocking.
- Current versions today: privacy `2026-v3` (F-71 made v2, F-112 v3), terms `2026-v1`, the same code in the three locales (F-4 BR14). The older ones stay in the manifests.
- Nothing at sign-in, in the token endpoint (`TokenEndpoints.cs`) or in the Web layout (`MainLayout.razor`, `Routes.razor`) looks at consent. F-53 is the only post-password step (a challenge at the token endpoint); it does not cover a session that is already open.
- Download my data (F-16) and delete my account (F-10) are dialogs on `/account` (`DownloadDataDialog.razor`, `EraseAccountDialog.razor`); the data export already lists every consent row in date order (`DataExportQueries.cs`).
- The public pages `/terms` and `/privacy` (`LegalDocumentPage.razor`) render the current version.

## Goal
Nobody keeps using Simulab on terms or a privacy policy they never saw: when either document changes, every signed-in person is asked to accept the new version before using the app, and the acceptance is kept as evidence.

## Users and use cases
- UC1 A signed-in user (or one who signs in) whose last acceptance is older than the current terms or privacy version is taken to an acceptance page, reads the changed documents and accepts; the app continues where they were going.
- UC2 A user who has not accepted yet reads the documents, signs out, downloads their data or deletes their account from `/account`; nothing else opens.
- UC3 The seeded administrator, who never accepted anything, is asked at the first access like any other account.
- UC4 A user whose acceptance is already current is never interrupted.

## Business rules
- BR1 A user owes an acceptance when they have no `ConsentRecord`, or when their most recent one (by `AcceptedAt`, then `Id`) has a `TermsVersion` or a `PrivacyVersion` different from the current one. Any new version of either document counts; there is no "minor change" mark (D1).
- BR2 The current version is what `ILegalDocumentProvider` returns for the user's locale (the same code in all locales, F-4 BR14). A document the server cannot read at all is not checked, as at sign-up: a server-side problem never locks people out.
- BR3 An acceptance is a new `ConsentRecord`; the existing rows are never changed (F-4 BR6, D2). It stores both current versions (even when only one changed), the locale the documents were shown in, the time (UTC) and the client address (D3); `DeclaresAdult` is copied from the user's own `IsAdultDeclared`.
- BR4 Accepting when nothing is owed (already current, a second click, two tabs) writes no row and succeeds.
- BR5 Accepting needs the versions the page showed. When they are not the current ones (a new version was published meanwhile), the answer is `legal_acceptance.version_outdated` and nothing is written; the page reloads with the newer documents.
- BR6 Gate: a signed-in user who owes an acceptance is sent to `/accept-terms` from every Web route except the exempt ones (BR7), whatever the sign-in method (password, code, Google) and also when the session was already open before the version changed. The check happens on each navigation (a version published while the user is on a page is caught at the next one). The page asked for is kept as a local `returnUrl` only; any other value goes to the home page.
- BR7 Exempt routes, so a user who does not accept keeps their rights (D4): `/accept-terms`, `/terms`, `/privacy`, `/account` (download my data, delete my account, with the rest of that page), and the sign-out endpoint. Nothing else.
- BR8 The page has no "decline" button. Not accepting is signing out (or leaving the app open on an exempt route); the user is not locked into a state they cannot leave.
- BR9 The gate belongs to the Web shell: the Api is not blocked for a user who owes an acceptance (D5). The token endpoint, the refresh grant and the sign-in screens are unchanged.
- BR10 No schema change: the latest acceptance is a query over `consent_records` using `ix_consent_records_user_id`.

## Screens and API
- `/accept-terms` — new page in the authentication layout: title, one line saying why, one row per document that changed (name, version, effective date, link "Read" to `/terms` or `/privacy`), the draft notice when the document is a placeholder, one primary button "Accept and continue", and a secondary "Sign out". Loading, error (with retry) and "version outdated" (reload) states.
- `GET /api/v1/identity/legal-acceptance` (authenticated, own account only) — `{ owes: bool, documents: [{ topic, version, effectiveDate, title, isPlaceholder, changed }] }` for the current versions in the caller's locale; `changed` is true for a document whose version is not the one in the last acceptance (all of them when there is none).
- `POST /api/v1/identity/legal-acceptances` (authenticated, own account only) — body `{ termsVersion, privacyVersion, locale }`; `201` when a row is written, `200` when nothing was owed (BR4); the user is the token's subject, never a body field.
- Error codes: new `legal_acceptance.version_outdated` (409); the existing authentication errors for a missing or invalid token.
- Resource keys (identity resources, three locales): page title, explanation, button, sign-out, "Read", the version line, the outdated message, the error code text; the draft notice reuses `Legal.DraftNotice`.

### Wireframe: Accept the updated documents
```text
+----------------------------------------------------------+
| Simulab                                                  |
+----------------------------------------------------------+
|  Our documents changed                          [new]    |
|  Read what is new and accept it to keep using Simulab.   |
|                                                          |
|  +----------------------------------------------------+  |
|  | Privacy policy      version 2026-v3 - 2026-10-09   |  |
|  | Draft text, pending legal review     [ Read ]      |  |
|  +----------------------------------------------------+  |
|  | Terms of use        version 2026-v1 - 2026-09-17   |  |
|  |                                      [ Read ]      |  |
|  +----------------------------------------------------+  |
|                                                          |
|  [ Accept and continue ]        [ Sign out ]             |
|  Download or delete your data: Account                   |
+----------------------------------------------------------+
```
Rows show only the documents that changed since the last acceptance (both when there is no acceptance). The "Account" link opens `/account`.

## Security

### Surfaces
- S1 `GET /api/v1/identity/legal-acceptance` — any signed-in user, for their own account.
- S2 `POST /api/v1/identity/legal-acceptances` — any signed-in user, for their own account.
- S3 The Web gate and `/accept-terms` with its `returnUrl` — any signed-in user.

### Threats
- T1 (S1, I) A user (or an anonymous caller) reads another account's acceptance state → AC10 [sec]
- T2 (S2, S) An acceptance is recorded for another user by sending an id in the body → AC11 [sec]
- T3 (S2, T) The client sends stale or invented versions, so the record claims text the person did not see → AC12 [sec]
- T4 (S2, R) The person later denies having accepted → covered by AC5 (the stored fields are the evidence); no extra criterion
- T5 (S2, D) Repeated posts fill the table with identical rows → AC13 [sec]
- T6 (S3, T) `returnUrl` sends the user to another site after accepting → AC14 [sec]
- T7 (S3, E) A direct URL or a deep link reaches the app without accepting → AC15 [sec]

### Personal data
| Datum | Lives in | Read by | Kept for | Legal basis |
| --- | --- | --- | --- | --- |
| Versions accepted, locale, time, client address | `consent_records` (Identity) | the person (export, F-16); the gate reads the versions only | Never deleted; the address is cleared when the account is erased (F-10 BR9), as for the sign-up record | LGPD Art. 8 §2 and GDPR Art. 7(1): the controller must be able to show the consent; unchanged from F-4 |

## Docs
- flow: new docs/flows/accept-new-legal-version.md — the acceptance page, the gate in the Web shell, the exempt routes and the evidence written.

## Acceptance criteria
- AC1 Given a user whose last `ConsentRecord` has the current privacy and terms versions, when the status is read, then `owes` is false and no row is written. (BR1, UC4)
- AC2 Given a user whose last record has an older privacy version (or terms version, or both), when the status is read, then `owes` is true and `documents` marks as `changed` exactly the documents whose version differs. (BR1, UC1)
- AC3 Given a user with no `ConsentRecord` (the seeded administrator), when the status is read, then `owes` is true and both documents are `changed`. (BR1, UC3)
- AC4 Given a user with two records, an old one and a newer current one, when the status is read, then the newest decides (`owes` false), whatever the order of insertion. (BR1)
- AC5 Given a user who owes an acceptance and posts the current versions, when the answer is `201`, then a new `ConsentRecord` exists with both current versions, the locale sent, the time, the client address and `DeclaresAdult` of the user; the older records are unchanged. (BR3, UC1)
- AC6 Given a document the server cannot read, when the status is read, then that topic is not compared and does not make `owes` true by itself. (BR2)
- AC7 Given the user locale is `pt-PT`, when the status is read, then titles and dates come from the `pt-PT` documents and the versions are the same as in `en`. (BR2)
- AC8 Given a user whose acceptance is already current, when the same post is repeated (or two arrive together), then the answer is `200` and the number of rows does not change. (BR4)
- AC9 Given versions that are not the current ones, when the post arrives, then the answer is `409 legal_acceptance.version_outdated` and nothing is written. (BR5)
- AC10 [sec] (T1) Given no token, then both routes answer `401`; given a token, then the status describes the token's own account and no parameter can name another one. (S1)
- AC11 [sec] (T2) Given a body that carries a `userId`, when the post is read, then the field is ignored and the record belongs to the token's subject; a test with two users checks that the other has no new row. (S2)
- AC12 [sec] (T3) Given versions that differ from the current ones in only one document, then the post is refused as in AC9, and nothing is stored. (S2, BR5)
- AC13 [sec] (T5) Given 20 posts in a row from a user who owes an acceptance, then exactly one row is written (the first), as AC8. (S2)
- AC14 [sec] (T6) Given `returnUrl` is an absolute address, a protocol-relative address (`//host`) or a path with a backslash, when the page finishes, then the user lands on the home page; a local path lands on it. (S3, BR6)
- AC15 [sec] (T7) Given a user who owes an acceptance, when they open any Web route outside BR7 by URL, then they are sent to `/accept-terms`; the exempt routes open. (S3, BR6, BR7)
- AC16 Given a user whose session was open before a new version was published, when they navigate to another page, then they are sent to `/accept-terms` without signing in again. (BR6)
- AC17 Given a user who owes an acceptance and signs in by password, by code or by Google, when sign-in completes, then the first page they reach is `/accept-terms`. (BR6, UC1)
- AC18 Given the page and a click on "Accept and continue", when the answer is successful, then the user lands on the page they had asked for, or on the home page; given a newer version published meanwhile, the page shows the outdated message and reloads the documents. (BR5, BR6, UC1)
- AC19 Given the page, when "Sign out" is chosen, then the session ends and the next sign-in asks again; there is no decline button. (BR8)
- AC20 Given a user who owes an acceptance, when they open `/account`, then download my data and delete my account work. (BR7, UC2)
- AC21 Given the user data export, when it is built after an acceptance, then it lists the new record after the older ones. (BR3)
- AC22 Given the Api, when a user who owes an acceptance calls another authenticated route, then it answers as before. (BR9)
- AC23 Localization: every text of the page and the error code exists in pt-BR, pt-PT and en; the missing-key test and the error-code-text test are green. (all)
- AC24 Given the page at a narrow width, when it is used by keyboard only, then Tab reaches "Read", "Accept and continue" and "Sign out" in that order and the title is announced. — validation script.

## Criterion → test

| Criterion | Test |
|---|---|
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
| AC12 | pending |
| AC13 | pending |
| AC14 | pending |
| AC15 | pending |
| AC16 | pending |
| AC17 | pending |
| AC18 | pending |
| AC19 | pending |
| AC20 | pending |
| AC21 | pending |
| AC22 | pending |
| AC23 | pending |
| AC24 | pending |

## Decisions
- D1 (owner, 2026-10-21) Any new version of the terms or the privacy policy asks for an acceptance; no per-version "notice only" mark. Reason: same rule as sign-up, no case-by-case judgement to maintain — asked
- D2 (owner, 2026-10-21) The acceptance is an evidence row written once and never updated; a new acceptance is a new row, the newest wins. Reason: F-4 BR6; the history feeds the export and the LGPD/GDPR proof — confirmed: docs/features/F-4-sign-up-and-email-verification.md (BR6, `ConsentRecord` comment)
- D3 (owner, 2026-10-21) The acceptance stores the client address like the sign-up record; the erasure of F-10 already clears it — confirmed: docs/features/F-4-sign-up-and-email-verification.md
- D4 (owner, 2026-10-21) A user who has not accepted can read the documents, sign out, download their data and delete their account, and nothing else. Reason: a person who refuses keeps their data rights — asked
- D5 (owner, 2026-10-21) The gate lives in the Web shell, not in the token endpoint: it covers every sign-in method and the sessions already open. Cost: the Api itself is not blocked (BR9) — asked
- D6 (owner, 2026-10-21) An account with no acceptance (the seeded administrator) owes one like an outdated account; no special case — asked
- D7 (owner, 2026-10-21) The acceptance screen is its own page in the authentication layout; no dialog — asked
- D8 (owner, 2026-10-21) Left out: the e-mail notice of a new version (needs F-66 and a rule of when to notify) and the acceptance history on the account page (the export already has it); each captured as an idea — asked
- D9 (Claude, 2026-10-21) Technical: no schema change and no migration; the newest record is read by `AcceptedAt` descending, then `Id`.
- D10 (Claude, 2026-10-21) Technical: a single "Accept and continue" button, no checkbox. Reason: one explicit click on a page that names the documents; sign-up keeps its two checkboxes.
- D11 (Claude, 2026-10-21) Technical: where the gate is evaluated (layout or router) and whether the circuit caches "accepted" is a build decision for the design pass; BR6 fixes only the behaviour.
- D12 (Claude, 2026-10-21) Technical: the summary's premise "F-71 is the first version change" is stale (privacy is at `2026-v3`); every sign-up before the current versions is already behind, which the rule handles.
- D13 (Claude, 2026-10-21) New packages: none (production and tests).

## Out of scope
- An e-mail telling everyone about a new version (idea).
- The history of acceptances on the account page (idea).
- A "decline and close my account" button: deleting the account stays the F-10 dialog on `/account`.
- Marking a version as minor or material.
- Blocking the Api for a user who owes an acceptance.
- Changing the content of the documents or their versions.

## Open questions
- (none)

## Change notes

## Validation script
(written at the end of the build)
