---
feature: F-32
epic: Foundation and identity
status: validating
board: 751
version: 1
---
# Account events in the data export

## Summary
Include the account's own security events (F-21) in the data export of F-16, which today returns the profile, the sessions and the consent records with their IP address. The events are personal data of the account holder, so a data subject request should carry them. F-21 left F-16 untouched (owner, question 8, 2026-09-23) because F-16 is already shipped.

## What exists
Checked in the code on 2026-09-27, on `main` at `3afb362`.
- `POST /api/v1/identity/data-exports` (`ExportDataHandler.cs:16-105`) builds `DataExportResponse { Format, Version, ExportedAt, Identity }` from `IDataExportQueries.GetIdentityDataAsync` (`DataExportQueries.cs:11-68`), which reads `account`, `roles`, `consents` (with `IpAddress`), `roleChanges` and `sessions` (a count) directly from `IdentityModuleDbContext`, one section per query, each ordered oldest first where order matters. Synchronous, JSON only, never stored (F-16 BR3, BR6).
- `AccountEvent : TenantEntity` (`Domain/Entities/AccountEvent.cs:12-79`, table `identity.account_events`, `DbSet<AccountEvent> AccountEvents`): `UserId` (nullable — null when no account matched the attempt), `Type` (`AccountEventType`, 13 values), `Method?`, `Reason?`, `IpAddress?`. Never updated or deleted after it is written (F-21 BR8); the one exception is `IpAddress`, cleared on that account's erasure (F-21 BR9) — so a live account's own events always carry the address they were recorded with.
- `IAccountEventQueries.ListAsync` (`Application/Abstractions/IAccountEventQueries.cs`) is the back office's paged, filtered read for `/admin/account-events` (admin permission `identity.roles.manage`, page size capped at 100). It is built for that screen, not for a full unbounded read of one account's trail; `DataExportQueries` reads `RoleChanges` and `ConsentRecords` directly the same way instead of going through their own admin-facing services, so `AccountEvents` is read the same way here — a straight `context.AccountEvents.Where(e => e.UserId == userId)` projection, no new port.
- `/account`'s "Your data" card (`IdentityResources.resx:114`, key `Account.Data.Explanation`) lists what the file contains today: account, roles, consents with IP, role changes, session count. It does not mention security events.
- F-21's own Out of scope: "Account events in the data export of F-16 — F-32 (AB#751)."; its decision (question 8, 2026-09-23) is quoted in the Summary above.

## Goal
Answer a data subject's portability request completely: the account's own security trail (sign-ins, failures, lockouts, password and two-factor changes, erasure) is personal data the app holds, so F-16's export should carry it the same way it already carries consents and role changes.

## Users and use cases
- UC1 A signed-in user downloads their data (F-16 UC2); the file's `identity` section now also lists every security event recorded about their account (F-21), oldest first, in the same file, same request.

## Business rules
- BR1 The `identity` section of the export gains `accountEvents`: every `AccountEvent` row whose `UserId` is the requesting user, oldest first (matching `consents` and `roleChanges`), each with when it happened (`occurredAt`, UTC), the event (`type`), the way (`method`, null when the event is not a sign-in), the reason (`reason`, null when nothing failed) and the client address (`ipAddress`, null when it was never known).
- BR2 No cap on how far back the events go and no pagination: the whole trail the account holds, as every other section of the file (F-16 BR6 — no download limit, generating it is cheap).
- BR3 The export only reads the trail. It writes no new account event of its own (matching F-16, where the export itself "is not an event of its own" except the lockout it may cause, unchanged), and it changes nothing in `identity.account_events` (F-21 BR8 already forbids editing or deleting events by any endpoint).
- BR4 The "Your data" card's explanation (`Account.Data.Explanation`) is updated in pt-BR, pt-PT and en to name the account's security events among what the file contains.

## Screens and API
- No new route. `POST /api/v1/identity/data-exports` keeps its request and its 200/422/423 answers (F-16); the 200 body's `identity` object gains one more field, `accountEvents: AccountEventDataResponse[]`, additive inside version 1 (rule `api-contracts`: "inside a version only add optional fields").
- `AccountEventDataResponse { OccurredAt, Type, Method?, Reason?, IpAddress? }` — the same fields the admin trail already translates (`AccountEvents.Event.*`, `.Method.*`, `.Reason.*`), but the export is machine-readable JSON like the rest of the file (F-16 BR3): raw enum names as strings, no translation, no `Account` field (it is always the requesting user).
- `/account`: the "Your data" card's explanation gains a mention of the account's security events, in the three languages.

## Acceptance criteria
- AC1 (UC1, BR1) Given a user with a successful sign-in, a failed sign-in and a password change recorded, when they export their data, then `identity.accountEvents` lists the three, oldest first, each with `occurredAt`, `type`, and `method`/`reason`/`ipAddress` exactly as stored (null where the event carries none).
- AC2 (BR1) Given two users, when user A exports, then no event of user B appears in user A's `accountEvents`.
- AC3 (BR2) Given an account with more recorded events than the admin trail's page-size cap (100), when it exports, then every one of them appears in `accountEvents` — no cap, no pagination.
- AC4 (BR3) Given a successful export, when the request ends, then no new row was written to `identity.account_events`, and every event's other fields are unchanged.
- AC5 (BR4) Given the `/account` page, when the Your data card is read, then its explanation names the account's security events, in pt-BR, pt-PT and en, and the missing-key test stays green.
- AC6 (BR1) Given an account with no recorded event at all (created before F-21, or that never signed in again), when it exports, then `accountEvents` is an empty list, not missing and not null.

## Decisions
- 2026-09-27 — All of the account's recorded events, unbounded, oldest first — owner, question 1 — the export already has no size cap (F-16 BR6) and the events are never pruned (F-21 BR8); a 90-day cap would break the completeness the rest of the file already gives.
- 2026-09-27 — The client IP address is included on each event — owner, question 2 — the export already includes it on consents (F-16 BR4); it is the one personal field F-21 keeps on the event, and it is only ever cleared by that same account's own erasure.
- 2026-09-27 — The "Your data" card's explanation is updated to mention the new section — owner, question 3 — the paragraph already lists everything else the file contains; leaving the new section out would make it read wrong.
- 2026-09-27 — Claude: `DataExportQueries` reads `context.AccountEvents` directly, the same way it already reads `ConsentRecords` and `RoleChanges`, instead of calling `IAccountEventQueries` (the admin back office's paged, filtered port) — the concerns differ (one account's full trail vs. a filtered admin page) and the existing sections already read the `DbContext` directly.
- 2026-09-27 — Claude: new contract `AccountEventDataResponse`, appended as the last field of `IdentityDataResponse` — additive, so the export's `Version` stays 1 (rule `api-contracts`).
- 2026-09-27 — Claude: no new resource keys — `Account.Data.Explanation`'s existing value changes in the three files together (rule `i18n`: a key changes value, it does not need a new key).
- 2026-09-27 — Claude: ordering is oldest first, matching `consents` and `roleChanges` (both already ordered ascending by their timestamp) rather than the admin trail's newest-first default, which exists for browsing a live list, not for a portability file.
- 2026-09-27 — Approved by the owner ("aprovo f-32").

## Out of scope
- Filtering, searching or paging the events inside the export (BR2): it is the whole trail or nothing, as the rest of the file.
- Any change to `/admin/account-events`, its API, or F-21's retention policy.
- A user-facing "recent activity" view — F-31 (AB#750), refining in parallel in another worktree; it reads the same table for a different purpose (a live, paged screen) and does not share code with this export.
- Translating the event, method or reason inside the export file: the file is machine-readable JSON (F-16 BR3), like every other section.

## Open questions
- (none)

## Change notes

## Validation script
You need a signed-in account with at least one sign-in, one failed sign-in attempt and one password change on record (or make them: sign out and back in once, try a wrong password once, change the password once).
1. Start the app host — Git Bash and PowerShell 7, same command: `dotnet run --project src/Hosts/Simulab.AppHost` → the Aspire dashboard URL is printed; the Web answers at https://localhost:7125. Sign in, open **Minha conta**.
2. The **Seus dados** card's text now mentions your security events among what the file contains.
3. Click **Baixar meus dados**, type your password, confirm. Open the downloaded file: `identity.accountEvents` lists your sign-in, your failed attempt and your password change, oldest first, each with `occurredAt`, `type`, and `method`/`reason`/`ipAddress` where they apply.
4. Switch the language to English, reopen the card: the explanation reads in English and still names the security events.

## Delivery
- Branch: feature/F-32
