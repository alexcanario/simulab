---
title: Accepting a new version of the legal documents
summary: How a signed-in user who has not accepted the current terms or privacy policy is stopped, shown what changed and recorded as accepting it.
items: [F-85]
---
# Accepting a new version of the legal documents

Technical terms: [glossary](../glossary.md)

Manual page for the user: [Aceitar documentos atualizados](../manual/pt-BR/accept-new-documents.md)

## Trigger
A signed-in user opens any Web page and their last `ConsentRecord` is not of the current terms and privacy versions (or they have none, like the seeded administrator).

## Steps
1. `LegalAcceptanceGate` (wrapping `AuthorizeRouteView` in `Routes.razor`) runs on every navigation. Anonymous users and the exempt pages pass without a call.
2. For a signed-in user it asks `GET /api/v1/identity/legal-acceptance` with the user's access token.
3. `LegalAcceptanceHandler.GetStatusAsync` reads the newest acceptance (`IConsentRecordStore.FindLatestAsync`, by `AcceptedAt` then `Id`) and the current documents of the request locale (`LegalDocumentReader`); `Owes` is true when any readable document differs.
4. When the user owes, the gate sends them to `/accept-terms?returnUrl=<page asked>` and does not render the page asked. The last allowed page stays mounted, so the shell is not torn down.
5. `AcceptTerms` lists the documents with `Changed = true`, each with a link to read it.
6. "Accept and continue" posts `POST /api/v1/identity/legal-acceptances` with the versions shown and the UI language.
7. The handler checks the locale and the versions, then, under a per-user advisory lock, reads the newest acceptance again and writes a new `ConsentRecord` (both current versions, locale, time, client address, `DeclaresAdult` of the user) unless nothing is owed.
8. The page goes to `returnUrl` when it is a local path, otherwise to `/`.

## Rules
- Any new version of either document counts; a new acceptance is a new row, older rows are never changed.
- A document the server cannot read is not compared; with no earlier acceptance nothing is owed.
- Exempt pages (matched by page type): `/accept-terms`, `/terms`, `/privacy`, `/account`. `/account/password` and `/account/security` stay gated. The plain Web endpoints outside the router (sign-in complete, language switch, Google, notice documents, question images) are not gated.
- The Api is not blocked for a user who owes an acceptance; the gate belongs to the Web shell.
- A status call that fails lets the page open (the Api never locks anyone out).
- There is no decline button; not accepting is signing out, or downloading or deleting the data from `/account`.

## Wireframe
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

## Errors
- `legal_acceptance.version_outdated` — 409: the versions posted are not the current ones; the page reloads the documents and asks again.
- `legal_acceptance.locale_invalid` — 400: the posted locale is not a supported UI language; nothing is written.
- A missing or invalid token, or a token whose user is gone, answers 401.
