---
feature: F-21
epic: Foundation and identity
status: approved
board: 736
version: 1
---
# Account event audit trail

## Summary
Record account and security events (sign-in, failed sign-in, password change and reset, two-factor on/off, account erasure) so an Admin can trace them. Left out of F-14 (role change audit trail) by the owner on 2026-09-21; F-14 decides the pattern for role changes only.

## What exists
- **No account or security event is recorded anywhere.** `identity.role_changes` (F-14) is the only trail in the solution and holds role changes only; the Identity tables are `users`, `roles`, `role_permissions`, `user_roles`, `user_claims`, `user_logins`, `user_tokens`, `permissions`, `consent_records`, `email_verification_tokens`, `password_reset_tokens`, `role_changes`. `User` has no `LastSignInAt` or equivalent (`Simulab.Identity.Domain/Entities/User.cs`).
- **Where the events happen.** Sign-in is OpenIddict's `/connect/token` (`TokenEndpoints.cs`), outside `/api/v1` by F-5 decision 1, with four grants: password, refresh, TOTP code (F-11) and Google (F-20, done). The rest are `/api/v1/identity/…`: `sign-out`, `password-changes`, `password-reset-requests`, `password-resets`, `account-erasures`, `registrations`, `email-verifications`, `google-registrations`, and the `totp` group (`POST enrolments`, `POST enrolments/confirmations`, `DELETE`, `POST recovery-codes`).
- **A failed password** calls `UserManager.AccessFailedAsync` (`TokenEndpoints:172`); lockout is 5 attempts / 15 minutes (`IdentityModule.cs:73`). An unknown username is refused with the same `invalid_credentials` and **no user id at all** (`TokenEndpoints:156`). `/connect/token` has **no per-client rate limit** — `ClientRateLimiter` guards registration, resend and password reset only (`IdentityRateLimits.cs`), so the number of failed attempts a stranger can cause is bounded by nothing but the lockout of each account.
- **F-14 BR3 does not transfer.** The role handlers write their entry inside `IRoleAdministrationStore.RunExclusiveAsync`, one transaction. The account handlers do not have one: `UserManager` saves as it goes (`ChangePasswordHandler`, `TotpAccountHandler`), and `IIdentityUnitOfWork` (F-13) exists only to flush a staged email job. Sign-in writes nothing at all today. So an account event is written **after** the change succeeded, not with it; the erasure (`EraseAccountHandler`, inside `RunExclusiveAsync`) is the one exception.
- **The visitor's address is available** at the Api: the Web forwards it with its client secret and `ClientAddress.Of(context)` returns it (B-4), falling back to the connection. No user agent or device is captured anywhere in the solution.
- **IP is treated as personal data here.** `ConsentRecord.IpAddress` is documented as "the one personal field of the record", and F-10 BR9 clears it on erasure in one statement (`AccountErasureStore.ClearConsentAddressesAsync`). Erasure keeps the user id as a pseudonym (`UserErased`, ADR-0001 #9) and overwrites email and name; F-16's data export returns the consent records with their IP (`DataExportQueries`).
- **The F-14 pattern is ready to copy**: entity on `TenantEntity` (`created_at`/`created_by` are when and who), `jsonb` owned collections, GIN index, page `/admin/role-history` behind `identity.roles.manage`, `AppDataTable` with filters in the query string, `AppDateColumn`, `AppFilterChip`, `UserTimeZone` (browser time zone), `AppIcons.History`, and a History row action on `/admin/users`.
- **The user's own pages** are `/account` (F-8) and `/account/security` (F-11, two-factor), with no activity list. `/admin` has Roles, Users and Role history.
- ADR-0001 #10 keeps full history for question content only; F-14 opened the first exception, this is the second.

## Goal
Let an Admin answer "did someone get into this account, and from where?" and "who turned two-factor off?" from the app, so a suspicion about an account is settled without reading the database.

## Users and use cases
- UC1 A person signs in, fails to sign in, is locked out, signs out, changes or resets the password, turns two-factor on or off, regenerates the recovery codes or erases the account; the event is recorded with no extra step.
- UC2 An Admin opens `/admin/account-events` and sees every recorded event, newest first, and narrows it by account, by event and by period.
- UC3 An Admin clicks **Security events** on a user (`/admin/users`) and lands on `/admin/account-events` already filtered to that account.
- UC4 An Admin sees a run of failed sign-ins from one address, clicks that address and gets every event from it, whatever the account.
- UC5 An Admin reads an event of an account erased since and sees "Erased account" instead of an email and no address; an attempt on a name that belongs to no account reads as "Unknown account".
- UC6 A Student or Curator never sees the item or the action and gets the ordinary Not Found page on `/admin/account-events`.

## Business rules
- BR1 An event is recorded for each of these, and for nothing else: sign-in succeeded, sign-in failed, account locked, signed out, password changed, password reset requested, password reset completed, two-factor turned on, two-factor turned off, recovery codes regenerated, account erased.
- BR2 An event holds: when (UTC, `TimeProvider`), the account it is about (user id, null when no account matched), the event, the way it was attempted (password, Google, authenticator code, recovery code) when the event is a sign-in, the reason when it failed, and the client address. It holds **no email, no name and no user agent**.
- BR3 A sign-in event is recorded for a sign-in, never for a silent token renewal: the refresh grant records nothing. A sign-in that ends in a two-factor challenge is not yet a sign-in; the event is written when the last step issues the tokens, with the way that last step was passed.
- BR4 A failed sign-in is recorded for a wrong password, a wrong code, an expired or invalid challenge, a Google token that failed a check, an unverified e-mail, and an attempt made while the account is locked. An attempt whose user name matches no account is recorded with no user id and the reason "unknown account"; **the typed name is never stored**.
- BR5 "Account locked" is recorded when a failure crosses the limit — the account was not locked before the attempt and is locked after it. Attempts that follow, while the lockout lasts, are failed sign-ins, not new lockouts.
- BR6 "Password reset requested" is recorded only when the address belongs to an account; the answer to the caller stays the same either way (F-7), and an unknown address leaves nothing behind.
- BR7 The event is written after the fact it records has succeeded — the account handlers have no transaction of their own (`UserManager` saves as it goes), so F-14 BR3 does not apply here. Two exceptions: the erasure event is written inside the erasure transaction (F-10) and a rollback takes it along; and a failure to write an event never fails the request that caused it, and never turns a refused sign-in into an accepted one.
- BR8 Events are never edited or deleted by the app and are kept forever. No endpoint changes them.
- BR9 The client address is personal data: account erasure clears it on every event of the erased account, inside the erasure transaction, as F-10 BR9 does for the consent records. The user id stays as the pseudonym (ADR-0001 #9), so the trail still reads.
- BR10 Filters: account (search by email or name, or a user id in the address), event, client address, period (last 7 days, 30, 90, all; default all). Filters combine with AND and live in the query string. Order: newest first. Paged on the server (10/25/50, default 25; `pageSize` capped at 100).
- BR11 Reading the trail requires `identity.roles.manage` (F-6 policy, 403 `identity.forbidden`); the page, the nav item and the row action are hidden without it.
- BR12 Events, ways and reasons are shown with their translated names; an account erased since shows "Erased account" with no address, and an event with no account shows "Unknown account".

## Screens and API

### Screen: Account events (`/admin/account-events`)
- Layout: `AppPageHeader` (breadcrumb "Administration / Account events", title, no primary action), then one `AppDataTable` card, `MainLayout` — the `/admin/role-history` layout.
- Filters above the table: Event (`AppSelectField`, first option `AccountEvents.Filter.AllEvents`), Period (`AppSelectField`: 7 / 30 / 90 days / all) and the kit search box for the account (email or name, `AccountEvents.Search.Placeholder`). Opened with `?user=`, the search box is replaced by a removable `AppFilterChip` with that account's email (`AccountEvents.Filter.User`). With `?ip=`, a second removable chip carries the address (`AccountEvents.Filter.Address`). Changing a filter reloads from the first page.
- Columns: When (`AppDateColumn`, date and time in the user's culture and time zone, sortable, default descending), Account (email, `AccountEvents.ErasedAccount`, or `AccountEvents.UnknownAccount`), Event (translated `AccountEvents.Event.*`), Details (the way and the reason, translated: `AccountEvents.Method.*`, `AccountEvents.Reason.*`; empty when neither applies), From (the client address as a button that filters by it, accessible name `AccountEvents.FilterByAddress`; empty when it is unknown or was cleared by an erasure).
- States: loading; ready; empty (`AccountEvents.Empty` when nothing is recorded at all; `AccountEvents.NoMatch` when filters match nothing); server error with **Try again**; permission denied: ordinary Not Found page.
- Navigation: Administration gets "Account events" (`AppIcons.Security`, Material Outlined `Security`) after "Role history", gated by `identity.roles.manage`.
- Shortcut: row action **Security events** on `/admin/users` (→ `?user=<id>`; order: Edit roles, History, Security events).
- Accessibility: filters in tab order before the table; each filter has a visible label; the address button is reachable by keyboard and says what it does; the Details cell is plain text, read in order.

### UI texts (resource key — en / pt-BR / pt-PT)
Existing keys reused: `Nav.Section.Administration`, `Common.*` (table, states, `ActionOnItem`, `TryAgain`, `LoadFailed`, `RemoveFilter`).

| Key | en | pt-BR | pt-PT |
|---|---|---|---|
| `Nav.AccountEvents` / `AccountEvents.Title` | Account events | Eventos de conta | Eventos de conta |
| `AccountEvents.Column.When` | When | Quando | Quando |
| `AccountEvents.Column.Account` | Account | Conta | Conta |
| `AccountEvents.Column.Event` | Event | Evento | Evento |
| `AccountEvents.Column.Details` | Details | Detalhes | Detalhes |
| `AccountEvents.Column.Address` | From | De | De |
| `AccountEvents.Event.SignInSucceeded` | Signed in | Sessão iniciada | Sessão iniciada |
| `AccountEvents.Event.SignInFailed` | Sign-in failed | Falha ao entrar | Falha ao iniciar sessão |
| `AccountEvents.Event.AccountLocked` | Account locked | Conta bloqueada | Conta bloqueada |
| `AccountEvents.Event.SignedOut` | Signed out | Sessão encerrada | Sessão terminada |
| `AccountEvents.Event.PasswordChanged` | Password changed | Senha alterada | Palavra-passe alterada |
| `AccountEvents.Event.PasswordResetRequested` | Password reset requested | Redefinição de senha solicitada | Reposição de palavra-passe pedida |
| `AccountEvents.Event.PasswordResetCompleted` | Password reset | Senha redefinida | Palavra-passe reposta |
| `AccountEvents.Event.TwoFactorEnabled` | Two-factor turned on | Verificação em duas etapas ativada | Verificação em duas etapas ativada |
| `AccountEvents.Event.TwoFactorDisabled` | Two-factor turned off | Verificação em duas etapas desativada | Verificação em duas etapas desativada |
| `AccountEvents.Event.RecoveryCodesRegenerated` | Recovery codes regenerated | Códigos de recuperação gerados de novo | Códigos de recuperação gerados de novo |
| `AccountEvents.Event.AccountErased` | Account erased | Conta excluída | Conta eliminada |
| `AccountEvents.Method.Password` | with the password | com a senha | com a palavra-passe |
| `AccountEvents.Method.Google` | with Google | com o Google | com a Google |
| `AccountEvents.Method.TotpCode` | with the app code | com o código do aplicativo | com o código da aplicação |
| `AccountEvents.Method.RecoveryCode` | with a recovery code | com um código de recuperação | com um código de recuperação |
| `AccountEvents.Reason.UnknownAccount` | unknown account | conta desconhecida | conta desconhecida |
| `AccountEvents.Reason.WrongPassword` | wrong password | senha incorreta | palavra-passe incorreta |
| `AccountEvents.Reason.LockedOut` | account locked | conta bloqueada | conta bloqueada |
| `AccountEvents.Reason.WrongCode` | wrong code | código incorreto | código incorreto |
| `AccountEvents.Reason.ChallengeInvalid` | expired or invalid challenge | desafio expirado ou inválido | desafio expirado ou inválido |
| `AccountEvents.Reason.EmailNotVerified` | e-mail not verified | e-mail não verificado | e-mail não verificado |
| `AccountEvents.Reason.GoogleTokenRefused` | Google token refused | token do Google recusado | token da Google recusado |
| `AccountEvents.Filter.Event` | Event | Evento | Evento |
| `AccountEvents.Filter.AllEvents` | All events | Todos os eventos | Todos os eventos |
| `AccountEvents.Filter.Period` | Period | Período | Período |
| `AccountEvents.Filter.Period.Days7` | Last 7 days | Últimos 7 dias | Últimos 7 dias |
| `AccountEvents.Filter.Period.Days30` | Last 30 days | Últimos 30 dias | Últimos 30 dias |
| `AccountEvents.Filter.Period.Days90` | Last 90 days | Últimos 90 dias | Últimos 90 dias |
| `AccountEvents.Filter.Period.All` | All time | Todo o período | Todo o período |
| `AccountEvents.Filter.User` | Account: {0} | Conta: {0} | Conta: {0} |
| `AccountEvents.Filter.Address` | From: {0} | De: {0} | De: {0} |
| `AccountEvents.FilterByAddress` | Show every event from {0} | Mostrar todos os eventos de {0} | Mostrar todos os eventos de {0} |
| `AccountEvents.Search.Placeholder` | Search the account by email or name | Pesquisar a conta por e-mail ou nome | Pesquisar a conta por e-mail ou nome |
| `AccountEvents.ErasedAccount` | Erased account | Conta excluída | Conta eliminada |
| `AccountEvents.UnknownAccount` | Unknown account | Conta desconhecida | Conta desconhecida |
| `AccountEvents.Empty` | No account event has been recorded yet. | Nenhum evento de conta registrado ainda. | Ainda não foi registado nenhum evento de conta. |
| `AccountEvents.NoMatch` | No event matches these filters. | Nenhum evento corresponde a esses filtros. | Nenhum evento corresponde a estes filtros. |
| `AccountEvents.Action.SecurityEvents` | Security events | Eventos de segurança | Eventos de segurança |

### API
- `GET /api/v1/identity/account-events?page=&pageSize=&user=&event=&ip=&search=&days=` → `AccountEventPageResponse { Items: AccountEventResponse[], Total }`; `AccountEventResponse { Id, OccurredAt, Account?: { Id, Email? }, Event, Method?, Reason?, IpAddress? }` (`Account` null = no account matched; `Email` null = erased; `IpAddress` null = unknown or cleared by an erasure). `days` ∈ {7, 30, 90}, absent = all; another value → 400 `account_event.period_invalid`. An `event` outside the known set → 400 `account_event.event_invalid`.
- No filters endpoint (unlike F-14): the event list is a fixed set the screen already translates, and the account is a search box.
- Error codes: `account_event.period_invalid` (400), `account_event.event_invalid` (400), `identity.forbidden` (403, F-6). An unknown `user` or `ip` gives an empty page, as F-14 does.

## Acceptance criteria
- AC1 Given an account with a password, when it signs in, then one `SignInSucceeded` event records the account, the time, the way "password" and the client address; with Google the way is "Google"; through a two-factor step the way is the app code or the recovery code, and the password step wrote **no** event. (UC1, BR2, BR3)
- AC2 Given a silent token renewal (refresh grant), then no event is recorded. (BR3)
- AC3 Given a wrong password, a wrong code, an expired challenge, a refused Google token, an unverified e-mail and an attempt while locked, then each records one `SignInFailed` with its reason and the address. (UC1, BR4)
- AC4 Given a user name that matches no account, when a sign-in is attempted, then one `SignInFailed` records the reason "unknown account" and no user id, and the typed name is stored nowhere. (BR4)
- AC5 Given four failed attempts, when the fifth crosses the limit, then exactly one `AccountLocked` is recorded, and a sixth attempt during the lockout records a `SignInFailed` and no second lockout. (BR5)
- AC6 Given a signed-in account, when it signs out, changes the password, resets the password, turns two-factor on, turns it off, regenerates the recovery codes or erases the account, then each records its own event; a password reset requested for a known address records one event and an unknown address records none. (UC1, BR1, BR6)
- AC7 Given a change that is refused (wrong current password, weak password, spent token) and an erasure rolled back by the last-manager rule, then no event of that change is recorded. (BR7)
- AC8 Given events of several accounts, events and addresses, when an Admin lists them with any combination of account, event, address, search and period, then the page holds exactly the matching events, newest first, with the total, paged on the server; `days=5` answers 400 `account_event.period_invalid` and an unknown `event` answers 400 `account_event.event_invalid`. (UC2, UC4, BR10)
- AC9 Given an account erased after its events were recorded, when they are listed, then they still exist, the account shows as "Erased account", and every one of its events has no address; events of other accounts keep theirs. (UC5, BR9)
- AC10 Given the trail, then no endpoint edits or deletes an event, and an event holds no email, no name and no user agent. (BR2, BR8)
- AC11 Given a caller without `identity.roles.manage`, when it calls the endpoint, then the answer is 403 `identity.forbidden`; the nav item and the row action are hidden and `/admin/account-events` shows the ordinary Not Found page. (UC6, BR11)
- AC12 Given the page, then it shows loading, empty, no-match and error states; changing a filter reloads from the first page; a user id in the address shows the removable account chip; clicking an address filters by it and shows its removable chip; **Security events** on a user opens the page filtered to that account. (UC2, UC3, UC4)
- AC13 Architecture: `Simulab.Identity.Domain` and `Simulab.Identity.Application` still reference neither EF Core nor ASP.NET; the Web references only `Simulab.Identity.Contracts`. (profile)
- AC14 All new texts — page, filters, events, ways, reasons, action, nav item, error codes — exist in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-09-23 — The eleven security events of BR1 only; sign-up, e-mail verification and profile changes stay out — owner, question 1 — they answer "did someone get into this account?"; the rest is already in the audit columns.
- 2026-09-23 — Admin only, permission `identity.roles.manage`, page under `/admin` — owner, question 2 — same reach as F-14; a "recent activity" list on the user's own security page is a separate item (F-31).
- 2026-09-23 — The client address is recorded, and account erasure clears it — owner, question 3 — without it a failed sign-in cannot tell the owner's typo from an attack; same treatment the consent record already gets (F-10 BR9).
- 2026-09-23 — An attempt on an unknown account is recorded without the typed name — owner, question 4 — it shows the attack without storing the personal data of someone who is not a user, and without catching a password typed into the e-mail field.
- 2026-09-23 — No user agent or device — owner, question 5 — nothing in the solution captures one and the Web would have to forward another header; it belongs with session management.
- 2026-09-23 — Kept forever, nothing prunes them — owner, question 6 — as F-14; a trail that disappears is not evidence. `/connect/token` has no rate limit, so if a flood ever appears, pruning becomes its own item.
- 2026-09-23 — An erased account keeps its events, with the address cleared — owner, question 7 — the erasure itself is the most sensitive event to keep, and the id is already a pseudonym.
- 2026-09-23 — The trail does not enter the data export of F-16 — owner, question 8 — F-16 is shipped; F-32 decides it.
- 2026-09-23 — A new page `/admin/account-events` in the F-14 shape, plus a **Security events** action on `/admin/users` — owner, question 9 — it leaves F-14's validated page untouched.
- 2026-09-23 — No new packages, for code or tests — owner, question 10.
- 2026-09-23 — Out of scope: CSV export, e-mail alert on a new sign-in, session and device management, a generic audit mechanism — owner, question 11.
- 2026-09-23 — Claude: one table `identity.account_events`, entity on `TenantEntity` like `RoleChange` (`created_at` is when; `created_by` is left as the interceptor fills it and is never read — the subject is `user_id`, and a failed sign-in has no signed-in author). Columns: `user_id` (null), `event`, `method` (null), `reason` (null), `ip_address` (null, 45). Indexes on `created_at`, `user_id`, `event`, `ip_address`. Never updated, never deleted; the one write after birth is the erasure clearing `ip_address`, in one `ExecuteUpdateAsync` next to `ClearConsentAddressesAsync`.
- 2026-09-23 — Claude: a port `IAccountEventLog` in `Application/Abstractions` with one `RecordAsync`, written through the module's `DbContext` with its own `SaveChangesAsync`; the address comes from a scoped `ICallerAddress` implemented in the Api host over `ClientAddress` and `IHttpContextAccessor` — the `ICurrentUser` pattern. The sign-in call sites are in `TokenEndpoints` (Api), the rest in their handlers (Application).
- 2026-09-23 — Claude: writing an event never fails the request (BR7); the log records its own failure through `ILogger` and returns. An audit row is worth less than the sign-in it would break.
- 2026-09-23 — Claude: no mockup (`/agile:screen`): one list page made only of existing kit parts, following `/admin/role-history`.
- 2026-09-23 — Approved by the owner ("Aprovo f-21").

## Out of scope
- A "recent activity" list for the user on `/account/security` — F-31 (AB#750).
- Account events in the data export of F-16 — F-32 (AB#751).
- CSV or other export of the trail.
- E-mail or other alert on a sign-in from a new address.
- Seeing and ending active sessions or devices (device management).
- A generic audit mechanism for other modules.
- Rebuilding events from before F-21; the trail starts empty.
- Pruning or archiving old events.
- Recording a failed current-password check outside sign-in (password change, account erasure): only the lockout it may cause is recorded.
- Recording sign-up, e-mail verification and profile changes.

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build. -->

## Delivery
- Branch: feature/F-21
