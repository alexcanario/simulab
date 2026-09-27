---
feature: F-31
epic: Foundation and identity
status: building
board: 750
version: 1
---
# Recent account activity for the user

## Summary
Show each person their own recent security events on `/account/security` — signed in, sign-in failed, password changed, two-factor turned on or off, and the rest of the F-21 trail — read from the same `identity.account_events` table, scoped to their own account. F-21 keeps the full trail for Admins only (owner, question 2, 2026-09-23); this is what lets the owner of an account notice an access that is not theirs, without asking an Admin.

## Start
- Depends on: F-21 (done) — `identity.account_events` and `IAccountEventQueries` already exist.
- Waits on: nothing.
- Suggested path: `/agile:build F-31` (small and clear).
- Parallel with: none in progress.

## Goal
Let a Student, Curator or Admin answer "did someone get into my account, and from where?" from their own `/account/security` page, the same question F-21 answers for an Admin about any account.

## What exists
- `identity.account_events` (F-21) already records, for every account: `SignInSucceeded`, `SignInFailed`, `AccountLocked`, `SignedOut`, `PasswordChanged`, `PasswordResetRequested`, `PasswordResetCompleted`, `TwoFactorEnabled`, `TwoFactorDisabled`, `RecoveryCodesRegenerated`, `AccountErased`, and `GoogleLinked`/`GoogleUnlinked` (F-29). Each row: `UserId` (nullable, null only when no account matched an attempt), `Type`, `Method` (sign-in only), `Reason` (failures only), `IpAddress` (nullable, cleared by erasure), and `CreatedAt` (`Simulab.Identity.Domain/Entities/AccountEvent.cs`).
- `IAccountEventQueries.ListAsync` (`Simulab.Identity.Infrastructure/Persistence/AccountEventQueries.cs`) already filters by `UserId`, paginates and sorts newest-first — it needs no change to serve one account's events.
- The only existing route, `GET /api/v1/identity/account-events`, is gated at the group level by permission `identity.roles.manage` (`RoleAdministrationEndpoints.cs`) — a plain user calling it with their own id would still get 403. It cannot be reused for self-service.
- The established shape for "my own data" in this module is a separate, permission-free endpoint that reads the id from the token: `TryGetUserId(ClaimsPrincipal user, out Guid userId)`, used by `GetProfileAsync`, `EraseAccountAsync`, `ExportDataAsync`, `ChangePasswordAsync`, `SignOutAsync` (`IdentityEndpoints.cs`) — none of them checks a permission, all of them trust the token's subject claim.
- `/account/security` (`Simulab.Web/Components/Pages/Identity/Security.razor`) today has three cards in this order: two-factor management, Google link/unlink, and a "danger zone" to disable two-factor. It already loads data on `OnInitializedAsync` through `IdentityApiClient` with an access token from `WebSessionTokenAccessor`, and already injects both `IStringLocalizer<IdentityResources>` (its own texts) and `IStringLocalizer<SharedResources>` (shared texts) — the 46 `AccountEvents.Event.*`/`Method.*`/`Reason.*` keys the admin page uses already live in `SharedResources`, translated in en, pt-BR and pt-PT.
- The only list/pagination kit component is `AppDataTable` (search box, sortable columns, page-size picker) — built for the admin grid. There is no lighter "simple list" kit component; a short, non-paginated card fits the page's existing hand-rolled `MudPaper`/`app-security-card` style better than the full grid.

## Users and use cases
- UC1 A signed-in person opens `/account/security` and sees, in a new card above the two-factor card, their own 20 most recent account events, newest first: when, what happened, how or why (when it applies), and from where (when known).
- UC2 A person with no recorded event yet sees a message saying so, instead of an empty card.
- UC3 A person whose session has expired gets the same treatment as the rest of the page (redirected to sign in); nobody can see another account's events through this card.

## Business rules
- BR1 The card shows only the caller's own events — every type F-21/F-29 record for an account (`SignInSucceeded`, `SignInFailed`, `AccountLocked`, `SignedOut`, `PasswordChanged`, `PasswordResetRequested`, `PasswordResetCompleted`, `TwoFactorEnabled`, `TwoFactorDisabled`, `RecoveryCodesRegenerated`, `AccountErased`, `GoogleLinked`, `GoogleUnlinked`) — including the caller's own failed sign-ins, which is what lets them tell their own mistyped password from someone else's attempt.
- BR2 The list is capped at the 20 most recent events, newest first. No filters, no search, no pagination: a longer history stays exclusive to `/admin/account-events` for an Admin.
- BR3 Each row shows: when (the person's own culture and time zone), the event, the method (sign-ins only) or the reason (failures only) translated the same way the admin page translates them, and the client address when it is known (never shown when the event never had one, or an erasure cleared it).
- BR4 No permission is checked beyond being signed in. The account is always the one in the caller's own access token; nothing lets a caller ask for another account's events through this endpoint.
- BR5 The card has four states: loading, ready with items, ready empty ("no event yet"), and error with **Try again** — the same states the rest of `/account/security` already uses.

## Screens and API

### Screen: `/account/security` — new "Recent activity" card
- Placement: a new card, first on the page, above the two-factor card — before the action cards, since this one is read-only information.
- Same visual shape as the page's existing cards (`MudPaper`, `app-form-card`/`app-security-card` classes, an `<h2>` title), not `AppDataTable`: a plain list of rows, no header row, no search box, no pager.
- Each row: date and time (`AppDateColumn`'s formatting rule — the person's culture and time zone), the event name, the method or reason in parentheses when either applies, and the address when known.
- States: loading (`AppLoadingState`); ready with up to 20 rows; ready and empty (`Security.Activity.Empty`); error (`AppErrorState` with **Try again**, same retry pattern as the rest of the page).

### API
- `GET /api/v1/identity/account-events/mine` → 200 `AccountEventPageResponse` (reused from F-21), always `UserId` = the caller's own id (from the token, never a caller-supplied value), `Page = 0`, `PageSize = 20`, newest first. `RequireAuthorization()` only, no permission policy.
- No new error code: an unauthenticated caller gets the ordinary 401 every other `/account/*` route gives.

## Acceptance criteria
- AC1 Given a signed-in person with recorded events, when they open `/account/security`, then the new card lists their own up to 20 most recent events, newest first, each with when, event, method or reason (when applicable) and address (when known), including their own failed sign-ins. (UC1, BR1, BR2, BR3)
- AC2 Given a signed-in person with no recorded event, then the card shows the empty message instead of an empty list. (UC2, BR5)
- AC3 Given the endpoint, then it always returns the caller's own events only — the caller cannot make it return anyone else's, whatever it sends; there is no permission to check, only the token's own subject. (UC3, BR4)
- AC4 Given the card, then the event, method and reason use the exact same translations `/admin/account-events` uses, in en, pt-BR and pt-PT, and the card's own texts (title, empty message, error message) exist in all three too. (BR3)
- AC5 Given the card, then it shows loading, ready-with-items, ready-empty and error states, the error state offering **Try again** and reloading on retry. (BR5)
- AC6 Given no access token or an expired session, when the endpoint is called, then the answer is 401, same as any other `/account/*` route. (BR4)
- AC7 Architecture: the new endpoint follows the same `TryGetUserId` pattern as `GetProfileAsync`/`EraseAccountAsync` in `Simulab.Identity.Api`; `Simulab.Identity.Domain` and `Simulab.Identity.Application` still reference neither EF Core nor ASP.NET; the Web references only `Simulab.Identity.Contracts`. (profile)
- AC8 All new texts (card title, empty message) exist in pt-BR, pt-PT and en, and the missing-key test is green. (i18n)

## Decisions
- 2026-09-27 — All account event types the account can have, including the caller's own failed sign-ins — owner — the point is exactly to let someone notice an attempt that was not theirs, and their own mistyped password is part of that picture.
- 2026-09-27 — A simple card, last 20 events, no pagination — owner — "recent" is deliberately smaller than the admin trail; a longer history stays an Admin-only capability.
- 2026-09-27 — The client address is shown to the account's own owner — owner — it is the detail that answers "from where", and it is already the owner's own data.
- 2026-09-27 — Method and reason shown, reusing the admin page's translations — owner — no new strings to translate for those values, and the same wording everywhere.
- 2026-09-27 — No permission beyond being signed in — owner — same treatment as `/account/profile`, `/account/password` and the data export: the id comes from the token, not from a permission.
- 2026-09-27 — New card at the top of `/account/security`, before the two-factor card — owner — it is information to read, not an action to take, so it comes before the action cards.
- 2026-09-27 — No new packages, for code or tests — owner.
- 2026-09-27 — Claude: a new endpoint `GET /api/v1/identity/account-events/mine` in `Simulab.Identity.Api`, `RequireAuthorization()` only, `TryGetUserId` from the token, calling the existing `IAccountEventQueries.ListAsync` with `UserId` forced to that id, `Page = 0`, `PageSize = 20`, `Ascending = false` — no query parameters accepted. Reuses `AccountEventListQuery`/`AccountEventPageResponse`/`AccountEventResponse` unchanged; the admin route and its permission are untouched.
- 2026-09-27 — Claude: the Web calls it through a new `IdentityApiClient.GetMyAccountEventsAsync(accessToken, cancellationToken)`, following the same `Authorized(...)`/`SendAsync<T>` pattern as `ListAccountEventsAsync`, with no query-string parameters to build.
- 2026-09-27 — Claude: card texts (`Security.Activity.Title`, `Security.Activity.Empty`) go in `IdentityResources` next to the page's other `Security.*` keys; the event/method/reason values keep using the existing `AccountEvents.Event.*`/`Method.*`/`Reason.*` keys already in `SharedResources`, both already injected on this page.
- 2026-09-27 — Claude: no mockup (`/agile:screen`) — one small read-only card made of existing kit parts (`AppLoadingState`, `AppErrorState`, the page's own `MudPaper` card style), no new visual pattern.
- 2026-09-27 — Approved by the owner ("aprovo f-31").

## Out of scope
- Filters, search or any pagination beyond the fixed last 20 events — that stays on `/admin/account-events`.
- CSV or any other export of this list.
- Any change to `/admin/account-events`, its permission, or the account-events trail's data model.
- Session and device management.
- Real-time updates — the card loads once, with the rest of the page.
- Rebuilding or backfilling events from before F-21.

## Open questions
- (none)

## Change notes

## Validation script
You need a signed-in account with some account history (from F-21's own validation script, or a fresh account: sign in, sign out, change the password, toggle two-factor).

1. Start the app host: `dotnet run --project src/Hosts/Simulab.AppHost` → the Web answers at https://localhost:7125. Sign in and open `/account/security` → a new card at the top lists your own recent events, newest first: your sign-in with "with the password" and your address, above the two-factor card.
2. Sign out, sign in with a wrong password once, then sign in properly. Back on `/account/security` → "Signed out" and "Sign-in failed / wrong password" rows appear, newest first, above the earlier ones.
3. Change your password on `/account/password`, then turn two-factor on and off on this same page → a "Password changed" row and two two-factor rows appear on reload.
4. Switch to pt-BR, then pt-PT → the card title and every event/method/reason read in that language, matching `/admin/account-events`'s wording; toggle dark mode → the card is legible in both themes.
5. Sign in with a brand-new account that has no history yet → the card shows its empty message instead of an empty list.
6. Turn off the network (or stop the Api host) and reload `/account/security` → the card shows its error state with **Try again**; restore the connection and click it → the card loads normally.
7. As a Student or Curator (no `identity.roles.manage`), confirm the card still works normally (it needs no permission) while `/admin/account-events` still shows "Page not found" for that account, unchanged from F-21.

## Delivery
- Branch: feature/F-31
