---
feature: F-85
epic: Foundation and identity
status: done
board: 128
version: 2
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
- BR7 Exempt routes, so a user who does not accept keeps their rights (D4): `/accept-terms`, `/terms`, `/privacy`, `/account` (download my data, delete my account, with the rest of that page), the sign-out endpoint, and the plain Web endpoints outside the Blazor router, which only proxy Api data that BR9 leaves open or finish a sign-in or a language change: `/account/sign-in-complete`, `/account/profile-applied`, `/culture/set`, the Google start and complete endpoints, the notice-document and question-image endpoints (change note v2). Nothing else.
- BR8 The page has no "decline" button. Not accepting is signing out (or leaving the app open on an exempt route); the user is not locked into a state they cannot leave.
- BR9 The gate belongs to the Web shell: the Api is not blocked for a user who owes an acceptance (D5). The token endpoint, the refresh grant and the sign-in screens are unchanged.
- BR10 No schema change: the latest acceptance is a query over `consent_records` using `ix_consent_records_user_id`.

## Screens and API
- `/accept-terms` — new page in the authentication layout: title, one line saying why, one row per document that changed (name, version, effective date, link "Read" to `/terms` or `/privacy`), the draft notice when the document is a placeholder, one primary button "Accept and continue", and a secondary "Sign out". Loading, error (with retry) and "version outdated" (reload) states.
- `GET /api/v1/identity/legal-acceptance` (authenticated, own account only) — `{ owes: bool, documents: [{ topic, version, effectiveDate, title, isPlaceholder, changed }] }` for the current versions in the caller's locale; `changed` is true for a document whose version is not the one in the last acceptance (all of them when there is none).
- `POST /api/v1/identity/legal-acceptances` (authenticated, own account only) — body `{ termsVersion, privacyVersion, locale }`; `201` when a row is written, `200` when nothing was owed (BR4); the user is the token's subject, never a body field.
- Error codes: new `legal_acceptance.version_outdated` (409) and `legal_acceptance.locale_invalid` (400, the body `locale` is not a supported UI language; change note v2); the existing authentication errors for a missing or invalid token (a token whose user no longer exists answers 401, as the profile route does).
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
|             [ Sign out ]        [ Accept and continue ]  |
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
- AC24 Given the page at a narrow width, when it is used by keyboard only, then Tab reaches the "Read" links, then "Sign out", then "Accept and continue" (the kit's order: the secondary action first, the primary last), then the "Account" link, and the title is announced. — validation script. (Reworded by change note v2, note 2.)
- AC25 Given a post whose `locale` is not a supported UI language (an unknown name, a rooted path, a value longer than the column), when it arrives, then the answer is `400 legal_acceptance.locale_invalid` and nothing is written; the current versions are always read with the request's own locale, never with the body's. (BR3, change note v2)
- AC26 Given a signed-in user who owes an acceptance, when they use the plain endpoints of BR7 (sign-in complete, language switch, notice document, question image), then they work as before. (BR7, change note v2)

## Criterion → test

Tests locked: a6cc28a10eb476d9b2fb528fb1507e9be5a8d4b0 (2 files); unlocks: 0


| Criterion | Test |
|---|---|
| AC1 | `LegalAcceptanceEndpointTests.Status_LastAcceptanceIsCurrent_DoesNotOwe` |
| AC2 | `LegalAcceptanceEndpointTests.Status_AnOlderVersion_OwesAndMarksExactlyTheDocumentsThatDiffer` (x3) |
| AC3 | `LegalAcceptanceEndpointTests.Status_NoAcceptanceAtAll_OwesAndEveryDocumentIsChanged` |
| AC4 | `LegalAcceptanceEndpointTests.Status_AnOldAndANewerCurrentAcceptance_TheNewestDecidesWhateverTheInsertionOrder` |
| AC5 | `LegalAcceptanceEndpointTests.Accept_WhenOwing_WritesANewRecordAndKeepsTheOlderOnesAndTheExportListsItLast` |
| AC6 | `LegalAcceptanceWithoutDocumentsTests.Status_NoDocumentOnTheServer_DoesNotOweAndListsNothing`, `LegalAcceptanceOneDocumentTests` (3: no earlier acceptance, an older terms with the privacy policy unreadable, and the post that keeps the last privacy version) |
| AC7 | `LegalAcceptanceEndpointTests.Status_PortugueseUser_GetsThePortugueseTitlesAndTheSameVersions` |
| AC8 | `LegalAcceptanceEndpointTests.Accept_WhenNothingIsOwed_AnswersOkAndWritesNothing` |
| AC9 | `LegalAcceptanceEndpointTests.Accept_VersionsThatAreNotTheCurrentOnes_AreRefusedAndNothingIsWritten` (x3) |
| AC10 | `LegalAcceptanceEndpointTests.BothRoutes_WithoutAToken_AreUnauthorized`, `Status_DescribesTheTokensOwnAccountOnly` |
| AC11 | `LegalAcceptanceEndpointTests.Accept_BodyCarriesAnotherUsersId_TheFieldIsIgnoredAndTheRecordIsTheCallers` |
| AC12 | `LegalAcceptanceEndpointTests.Accept_VersionsThatAreNotTheCurrentOnes_AreRefusedAndNothingIsWritten` (the two one-document cases) |
| AC13 | `LegalAcceptanceEndpointTests.Accept_TwentyPostsAtOnce_WriteExactlyOneRow` |
| AC14 | `AcceptTermsPageTests.Accept_AReturnUrlThatIsNotALocalPath_GoesHome` (x7), `Accept_SendsTheShownVersionsAndGoesToTheLocalReturnUrl` |
| AC15 | `LegalAcceptanceGateTests.Owing_AGatedPageIsNotRenderedAndTheUserIsSentToAcceptTerms`, `Owing_AnExemptPageIsRendered` (x3), `Owing_TheOtherAccountPagesStayBehindTheGate` (x2); through the real router: `LegalAcceptanceRoutesTests` (3) |
| AC16 | `LegalAcceptanceGateTests.ANewVersionIsPublishedWhileTheSessionIsOpen_TheNextNavigationIsSentToAcceptTerms` |
| AC17 | `LegalAcceptanceGateTests.Owing_TheHomePage_GoesToAcceptTermsWithNoReturnUrl` (every sign-in method ends at `/`, `AccountEndpoints.CompleteSignIn`) |
| AC18 | `AcceptTermsPageTests.Accept_SendsTheShownVersionsAndGoesToTheLocalReturnUrl`, `Accept_WithoutReturnUrl_GoesHome`, `Accept_ANewerVersionWasPublished_ReloadsTheDocumentsAndAsksAgain` |
| AC19 | `AcceptTermsPageTests.SignOut_GoesToTheSignOutEndpoint_AndThereIsNoDeclineButton` |
| AC20 | `LegalAcceptanceGateTests.Owing_AnExemptPageIsRendered` (`Account`), `AccountPageTests` unchanged (download and delete) |
| AC21 | `LegalAcceptanceEndpointTests.Accept_WhenOwing_WritesANewRecordAndKeepsTheOlderOnesAndTheExportListsItLast` |
| AC22 | `LegalAcceptanceEndpointTests.OtherAuthenticatedRoutes_AnswerAsBeforeForAUserWhoOwesAnAcceptance` (a guard: it passes before the code, by design) |
| AC23 | `AcceptTermsPageTests.Page_IsTranslated` (x3), `ResourceParityTests` and `ErrorTextTests` (existing, green) |
| AC24 | validation step 6 (keyboard only) |
| AC25 | `LegalAcceptanceEndpointTests.Accept_ALocaleThatIsNotSupported_IsBadRequestAndNothingIsWritten` (x5), `Accept_EverySupportedLocale_IsStoredAsSent` |
| AC26 | `SignInCompleteTests` (existing, unchanged: the plain endpoints sit outside the router, so the gate never meets them), validation step 4 (language switch on the acceptance page) |

## Decisions
- D1 (owner, 2026-10-21) Any new version of the terms or the privacy policy asks for an acceptance; no per-version "notice only" mark. Reason: same rule as sign-up, no case-by-case judgement to maintain — asked
- D2 (owner, 2026-10-21) The acceptance is an evidence row written once and never updated; a new acceptance is a new row, the newest wins. Reason: F-4 BR6; the history feeds the export and the LGPD/GDPR proof — confirmed: docs/features/F-4-sign-up-and-email-verification.md (BR6, `ConsentRecord` comment)
- D3 (owner, 2026-10-21) The acceptance stores the client address like the sign-up record; the erasure of F-10 already clears it — confirmed: docs/features/F-4-sign-up-and-email-verification.md
- D4 (owner, 2026-10-21) A user who has not accepted can read the documents, sign out, download their data and delete their account, and nothing else. Reason: a person who refuses keeps their data rights — asked
- D5 (owner, 2026-10-21) The gate lives in the Web shell, not in the token endpoint: it covers every sign-in method and the sessions already open. Cost: the Api itself is not blocked (BR9) — asked
- D6 (owner, 2026-10-21) An account with no acceptance (the seeded administrator) owes one like an outdated account; no special case — asked
- D7 (owner, 2026-10-21) The acceptance screen is its own page in the authentication layout; no dialog — asked
- D8 (owner, 2026-10-21) Left out: the e-mail notice of a new version (needs F-66 and a rule of when to notify) and the acceptance history on the account page (the export already has it); captured as F-145 (board #203) and F-146 (board #204) — asked
- D9 (Claude, 2026-10-21) Technical: no schema change and no migration; the newest record is read by `AcceptedAt` descending, then `Id`.
- D10 (Claude, 2026-10-21) Technical: a single "Accept and continue" button, no checkbox. Reason: one explicit click on a page that names the documents; sign-up keeps its two checkboxes.
- D11 (Claude, 2026-10-21) Technical: where the gate is evaluated (layout or router) and whether the circuit caches "accepted" is a build decision for the design pass; BR6 fixes only the behaviour.
- D12 (Claude, 2026-10-21) Technical: the summary's premise "F-71 is the first version change" is stale (privacy is at `2026-v3`); every sign-up before the current versions is already behind, which the rule handles.
- D13 (Claude, 2026-10-21) New packages: none (production and tests).

- D14 (Claude, 2026-10-21) Build design passes (`system-design`, then `architect`, both read-only on opus; the main session checked the advisory-lock precedent `RoleAdministrationStore.cs:21-40`, `Routes.razor`, `SupportedCultures.SafeLocalPath` and the error-text location in `SharedResources*.resx`, and the second `IConsentRecordStore` implementer `TooLongConsentRecordStore` in `SignUpTransactionTests.cs:299`). Accepted: `FindLatestAsync` and `RunExclusiveForUserAsync` on the existing port; the comparison extracted into one Application class used by sign-up and the new handler (`RegistrationTerms` keeps only its own error code); handler in Application, endpoints in `IdentityEndpoints.cs`; the gate component wrapping `AuthorizeRouteView` in `Routes.razor`, exempting pages by page type (so `/account/password` and `/account/security` stay gated); no "accepted" cache; error-code text in `SharedResources`, page keys in `IdentityResources`; `.ThenBy(Id)` in the data export (AC21); a token whose user is gone answers 401; the current versions are read with the request's locale. Dropped or changed: the gate renders nothing only on the first load and keeps the last allowed route while the next check runs (the layout stays mounted); the open point "unreadable document while the other changed" is decided as: copy that topic's version from the latest record, and with no earlier record nothing is owed (BR2); a failed status call lets the page open and logs a warning (B-3 BR3).
- D15 (owner, 2026-10-21) Design trade-offs, as proposed by the architect: (8) a body locale outside the supported languages is `400 legal_acceptance.locale_invalid`; (9) the stricter `returnUrl` check lives in the page and calls `SafeLocalPath` first; (10) one row under concurrency by a per-user `pg_advisory_xact_lock` transaction — asked
- D16 (owner, 2026-10-21) The plain Web endpoints outside the router are exempt by change note v2 (BR7, AC26) — asked
- D17 (Claude, 2026-10-21) Design coverage: contracts (routes, records, codes, both `IConsentRecordStore` implementers), layout (Application/Infrastructure/Api/Web split, gate placement, steps), rules (module boundary, problem-details, banned symbols, vocabulary, error-text check), simplicity (one extraction), data (no migration, existing index, `Id` v7 tie-break); not checked: the redirect during prerender (checked on screen through the app host in step 12).
- D18 (Claude, 2026-10-21) Traps: T1 cache "accepted" per circuit: breaks BR6/AC16; T2 exempt `/account` by prefix: opens password and security pages; T3 call `RegistrationTerms.CheckAsync` as is for the POST: wrong error code and flags.
- D19 (Claude, 2026-10-21) plan — 1 Glossary row, contracts, `FindLatestAsync` (and the test fake that implements the port), extraction of the version comparison, GET status route → AC1-4, 6, 7, AC10 (GET half); 2 POST acceptance with the per-user lock, locale check, 201/200/409/400/401, `ThenBy(Id)` in the export → AC5, 8, 9, 11, 12, 13, 21, 22, 25, AC10 (POST half); 3 Web client methods, gate component in `Routes.razor`, bare `AcceptTerms` page → AC15, 16, 17, 20, 26; 4 full `AcceptTerms` page, resources and error texts → AC14, 18, 19, 23; 5 flow page, pt-BR manual, OpenAPI and DocGen route docs → techdocs check, docs checks; 6 app host check, review, scan, validation script → AC24.

- D20 (Claude, 2026-10-21) Independent review of the built branch (reviewer agent, opus), all findings checked in the code: (1) major, confirmed — no test went through `Routes.razor`: added `LegalAcceptanceRoutesTests` (3); (2) major, confirmed — the one-unreadable-document branches of BR2/D14 had no test: added `LegalAcceptanceOneDocumentTests` (3); (3) minor, confirmed — a missing user carried the code `identity.forbidden` on a not-found kind: now `user.not_found`; (4) minor, confirmed — the `ConsentRecord` comment contradicted itself about the erasure update: rewritten; (5) minor, not done — AC20/AC26 lean on existing tests that never use a user who owes; the plain endpoints sit outside the router by structure, and the validation script covers the language switch on the page. Traps the reviewer listed: T1 one Redis read, one Api call and one query per navigation (accepted: BR6 and AC16 ask for a check on each navigation; a cache needs a change note); T2 the provider falls back to `en` silently if a locale lacks a document (all three ship both documents today); T3 `SafeReturnPath` is a blocklist and `/%61ccept-terms` passes the prefix check but only lands on the page and moves home (accepted).
- D21 (Claude, 2026-10-21) Verified on screen through the app host (own database `simulab_f85_val`, seeded administrator with no acceptance): sign-in lands on `/accept-terms` with Terms 2026-v1 and Privacy 2026-v3; `/account/password` by URL returns to the page with `?returnUrl=%2Faccount%2Fpassword` on the first load; `/account` opens; accepting from that address lands on `/account/password`; `/` does not redirect afterwards; the console had no error. Not seen on screen: dark mode, narrow width, keyboard order (steps 4 and 6 of the script). The prerender redirect works (D17's open point).

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
### v2 — 2026-10-21
- What: (1) BR7 also exempts the plain Web endpoints outside the Blazor router (sign-in complete, profile applied, language switch, Google start and complete, notice document, question image); (2) a new error code `legal_acceptance.locale_invalid` (400) for a body locale outside the supported languages.
- Why: found by the build design pass: the gate sits in the router, so these endpoints are outside it, and sign-in complete and the language switch must stay open or sign-in and the language switch on the acceptance page break; the body locale reaches a file path and a `varchar(10)` column.
- Affected: BR7, `## Screens and API`, AC15 (read with BR7), new AC25 and AC26; other criteria unchanged.
- Re-approved: 2026-10-21 (owner, in the build design round)

### v2, note 2 — 2026-10-21
- What: the wireframe and AC24 follow the kit's order of actions (secondary "Sign out" first, primary "Accept and continue" last); the Tab order is the Read links, Sign out, Accept and continue, Account.
- Why: found while building the page: every kit form puts the primary action last (`AppFormActions`), and the wireframe had drawn it first.
- Affected: the wireframe, AC24; other criteria unchanged.
- Re-approved: 2026-10-21 (owner, with the validation of the script).

## Validation script
### English
Needed to validate: nothing beyond the local app host, in place now. The start command gives the item its own database, so the seeded administrator has accepted nothing yet (no `consent_records` row), which is the case to see. Sign in with `admin@simulab.local` and the development password of `appsettings.Development.json` (`Admin@Simulab123!`). Stop any other app host first. To repeat the whole script after accepting, stop the app host and start it again with another database name (`simulab_f85_own2`).

1. Start the app host from the F-85 worktree. Expected: the dashboard opens at `https://localhost:17162` with `api` and `web` Running; the Web address is usually `https://localhost:7125`.
   - Git Bash:
     ```bash
     cd /d/wt/simulab/f-85-users-accept-new && Database__Name=simulab_f85_own dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https
     ```
   - PowerShell 7:
     ```powershell
     Set-Location D:\wt\simulab\f-85-users-accept-new; $env:Database__Name = "simulab_f85_own"; dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https
     ```
2. [ ] Open `https://localhost:7125/sign-in` and sign in as `admin@simulab.local`. Expected: instead of the home page you land on **Our documents changed** (the acceptance page, a page that asks you to accept the new legal documents): a line "Read what is new and accept it to keep using Simulab", two rows (Terms of use, version 2026-v1; Privacy policy, version 2026-v3), each with a **Read** link and the draft notice, then **Sign out**, **Accept and continue**, and "Download or delete your data: Account".
3. Before accepting, check the gate (the check that sends you back to that page):
   - [ ] 3.1 In the address bar open `https://localhost:7125/account/password`. Expected: you come back to **Our documents changed**; the address ends with `?returnUrl=%2Faccount%2Fpassword`.
   - [ ] 3.2 Click **Read** on Privacy policy. Expected: the privacy policy opens (version 2026-v3) with no redirect. Press the browser's back button.
   - [ ] 3.3 Click **Account** in the last line. Expected: **My account** opens, with the profile and, at the end, the download and delete sections (do not use them now). Go back to `https://localhost:7125/accept-terms`.
4. [ ] Click the globe in the top bar (language switch) and choose Português (Brasil). Expected: **Nossos documentos mudaram**, **Aceitar e continuar**, **Sair**, **Ler**. Choose English again.
5. [ ] Click **Sign out**, then sign in again as the administrator. Expected: you land on **Our documents changed** again (signing out is not accepting).
6. [ ] Keyboard only: press Tab until the focus ring reaches each control. Expected order: **Read** (Terms), **Read** (Privacy), **Sign out**, **Accept and continue**, **Account**; the title is read first by a screen reader. On **Accept and continue** press Enter. Expected: you land on the home page.
7. After accepting:
   - [ ] 7.1 In the address bar open `https://localhost:7125/account/password`. Expected: **Change password** opens with no detour.
   - [ ] 7.2 Click the account menu (top right), **Sign out**, then sign in again. Expected: straight to the home page, no acceptance page.
8. [ ] Stop the app host (Ctrl+C in its terminal).

Not shown on screen, covered by tests: the status and the acceptance routes, the rows written, the older ones kept, the lock under concurrency, the sec criteria and the other languages of the error texts (AC1 to AC13, AC21, AC22, AC25). The case of a user whose last acceptance is older than the current versions (AC2) is a test; on screen the administrator, who never accepted, shows the same page.

### pt-BR
Necessário para validar: nada além do app host local, já disponível. O comando de início dá ao item um banco só dele, então o administrador semeado ainda não aceitou nada (nenhuma linha em `consent_records`), que é o caso a ver. Entre com `admin@simulab.local` e a senha de desenvolvimento do `appsettings.Development.json` (`Admin@Simulab123!`). Pare qualquer outro app host antes. Para repetir o roteiro inteiro depois de aceitar, pare o app host e suba de novo com outro nome de banco (`simulab_f85_own2`).

1. Suba o app host a partir da worktree da F-85. Esperado: o dashboard abre em `https://localhost:17162` com `api` e `web` Running; o endereço da Web costuma ser `https://localhost:7125`.
   - Git Bash:
     ```bash
     cd /d/wt/simulab/f-85-users-accept-new && Database__Name=simulab_f85_own dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https
     ```
   - PowerShell 7:
     ```powershell
     Set-Location D:\wt\simulab\f-85-users-accept-new; $env:Database__Name = "simulab_f85_own"; dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https
     ```
2. [ ] Abra `https://localhost:7125/sign-in` e entre como `admin@simulab.local`. Esperado: em vez da página inicial você cai em **Nossos documentos mudaram** (a página de aceite, que pede para aceitar os documentos legais novos): a linha "Leia o que mudou e aceite para continuar usando o Simulab", duas linhas (Termos de Uso, versão 2026-v1; Política de Privacidade, versão 2026-v3), cada uma com o link **Ler** e o aviso de texto provisório, depois **Sair**, **Aceitar e continuar** e "Baixar ou excluir seus dados: Conta".
3. Antes de aceitar, confira a trava (a checagem que devolve você a essa página):
   - [ ] 3.1 Na barra de endereço abra `https://localhost:7125/account/password`. Esperado: você volta para **Nossos documentos mudaram**; o endereço termina com `?returnUrl=%2Faccount%2Fpassword`.
   - [ ] 3.2 Clique em **Ler** na Política de Privacidade. Esperado: a política abre (versão 2026-v3) sem redirecionamento. Use o botão voltar do navegador.
   - [ ] 3.3 Clique em **Conta** na última linha. Esperado: **Minha conta** abre, com o perfil e, no fim, as seções de baixar e apagar os dados (não use agora). Volte para `https://localhost:7125/accept-terms`.
4. [ ] Clique no globo da barra superior (troca de idioma) e escolha Português (Brasil). Esperado: **Nossos documentos mudaram**, **Aceitar e continuar**, **Sair**, **Ler**. Escolha English de novo.
5. [ ] Clique em **Sair** e entre de novo como administrador. Esperado: você cai em **Nossos documentos mudaram** outra vez (sair não é aceitar).
6. [ ] Só com o teclado: aperte Tab até o foco passar por cada controle. Esperado, nesta ordem: **Ler** (Termos), **Ler** (Privacidade), **Sair**, **Aceitar e continuar**, **Conta**; um leitor de tela lê o título primeiro. Em **Aceitar e continuar** aperte Enter. Esperado: você cai na página inicial.
7. Depois de aceitar:
   - [ ] 7.1 Na barra de endereço abra `https://localhost:7125/account/password`. Esperado: **Alterar senha** abre sem desvio.
   - [ ] 7.2 Clique no menu da conta (canto superior direito), **Sair**, e entre de novo. Esperado: direto para a página inicial, sem a página de aceite.
8. [ ] Pare o app host (Ctrl+C no terminal dele).

Não mostrado na tela, coberto por testes: as rotas de status e de aceite, as linhas gravadas, as antigas mantidas, o lock sob concorrência, os critérios de segurança e os textos de erro nos outros idiomas (AC1 a AC13, AC21, AC22, AC25). O caso do usuário cujo último aceite é mais antigo que as versões atuais (AC2) é um teste; na tela o administrador, que nunca aceitou, mostra a mesma página.

## Events

- 2026-10-21 question — asked 6, from context 2
- 2026-10-21 test-fail — tests failed (Simulab.slnx): Simulab.ArchitectureTests.FrozenManualTests.FrozenPage_CarriesTheFrozenKeyAndTheNotice(loca, 1 red

## Delivery

Gate time: 7 passes, 25 min 32 s — turn 2 (1 min 19 s), ship 3 (21 min 9 s), scan 2 (3 min 4 s); 1 red
Usage: 6497.2k weighted (estimate) — refine 6% · build 80% · ship 11% · other 3% — agents 25% — context per turn 156k → 488k — 1 session, 189 turns
- Branch `feature/F-85`, merge commit f2e427f (board #128). App version 1.10.0 -> 1.11.0.
- Tests: full suite green, 0 failed, 4613 tests in 14 projects (Identity 548, Catalog 655, QuestionBank 526, Web 2305, Architecture 301, Api 83, AppHost 83, the rest 112); 20 s build and 273 s test run, slowest projects 4 m 29 s (Catalog), 4 m 25 s (Identity), 4 m 3 s (QuestionBank): inside the 5 min budget. The first full run of the ship took 7 m 12 s to 7 m 49 s per slow project (machine shared with other sessions, cause not measured, reported to the retro) and was red on four `FrozenManualTests` cases until the frozen pt-PT and en copies of the new manual page existed.
- New tests: 24 API tests locked at a6cc28a (`LegalAcceptanceEndpointTests`, `LegalAcceptanceWithoutDocumentsTests`), 3 `LegalAcceptanceOneDocumentTests`, and on the Web 3 `LegalAcceptanceRoutesTests`, 11 `LegalAcceptanceGateTests`, 12 `AcceptTermsPageTests`.
- Scan: `agile scan GREEN` (2 medium open findings in `tools/Simulab.DocGen`, not touched by this item).
- Manual: `accept-new-documents` in pt-BR; frozen copies in pt-PT and en. Technical pages: `docs/flows/accept-new-legal-version.md`; `docs/api/Simulab.Api.json` and `docs/architecture/Identity/routes.md` regenerated. Glossary: 3 rows (`LegalAcceptance`, advisory lock, returnUrl).
- Review: independent review before validation, findings in D20.
- Validation: the owner said "validado F-85" in the conversation on 2026-10-21 and typed `/agile:ship F-85`; the steps were not ticked in the widget.
- Validation evidence: 0 of 10 paths ticked (all `not tested` in the widget); the owner's word in chat is the evidence.
- Follow-up ideas: F-145 (e-mail notice of a new version, board #203) and F-146 (acceptance history on the account page, board #204).
