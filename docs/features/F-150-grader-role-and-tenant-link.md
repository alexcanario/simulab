---
feature: F-150
epic: Partner tenants
status: approved
board: 208
version: 1
---
# Grader role and user-to-tenant link

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: approved (2026-10-25)
- Stopped at: Approved by the owner on 2026-10-25; no code written yet.
- Next action: Run /agile:build F-150.
- Waiting on: nothing

## Summary
A `Grader` system role (a person who signs in and grades essays and open answers) and an assignable tenant on the user, for the curator and the grader. A grader with no tenant sees every essay to grade; with a tenant, only that tenant's essays, not the global ones. Until the first partner tenant exists, the one grader sees every essay. Source: discussion D-5 (owner's model, 2026-10-24); it reverts ADR-0001 row 18 ("Teacher / grader roles: Not in v1"), whose text says RBAC lets roles be added as data.
Added 2026-10-25 (D-7), correcting the first version of this item: `User.TenantId` already exists and the token already carries the claim `tenant_id` (empty today); what is missing is a way to assign the tenant, the role and a visibility rule. Scope settled in D-7: the role and its permission (any role may hold it; the seed gives it to `Grader` and `Admin`); an administrator assigns the role and the user's `TenantId` in the users screen (F-9); the claim carries the tenant; `ICurrentTenant` stays `NoTenant` (wiring it is F-162); the grader's own visibility rule is applied by the correction queue (F-151), because the global filter would also show the global rows. A curator with a tenant gains the field only. No `Tenant` entity yet.

## Start
- Depends on: F-9 (roles and permissions, done).
- Waits on (to start): nothing — scope settled in D-7 (2026-10-25).
- Needed to validate: the local app host with an administrator and a second account — owner.
- Model: opus (a door: permissions and the tenant of an account).
- Suggested path: `/agile:build` (no mockup: the existing roles dialog gets one field; the wireframe below is enough).
- Parallel with: none that touches Identity. F-162 and F-151 wait on this item.

## Goal
Let an administrator give someone the `Grader` role and attach any account to a tenant, so the correction queue (F-151) has a grader to serve and a tenant to scope by. Nothing is filtered by tenant yet.

## Users and use cases
- UC1 An Admin gives the `Grader` role to a person in the users screen; the role shows with the other system roles in the roles screen.
- UC2 An Admin sets a user's tenant by pasting its id, or clears it, in the dialog that already edits the user's roles.
- UC3 An Admin sees each user's tenant in the users list.
- UC4 An Admin finds who moved a person to another tenant, and when, in the role history (F-14).
- UC5 A user with a tenant signs in and the token carries that tenant (`tenant_id`).
- UC6 A user downloads their own data (F-16) and sees their tenant and the changes of it.

## Business rules
- BR1 `IdentityRoles.Grader = "Grader"` joins `IdentityRoles.All`. The startup seed creates it as a system role, like Student, Curator and Admin: it cannot be renamed or deleted, only its permissions change (F-9 BR1, BR2). A custom role already named `Grader` is marked as a system role by the same seed.
- BR2 The permission `questions.grade` (module `questions`) is declared in `QuestionBankPermissions` (it joins `All`), in the `PermissionCatalog` of `QuestionBankModule` with the initial grant to `Grader`, and in `WebPermissions`. The name and the grant ship in the same commit: the grant is made only in the start that creates the permission row, and a later start never gives it back. Admin holds every permission (F-6 BR3).
- BR3 Nothing checks `questions.grade` in this item: no route requires it and the `Grader` role has no menu item. A user whose only role is `Grader` sees what a Student sees until F-151 adds the queue.
- BR4 A user's tenant is the nullable `User.TenantId`. Any account that is not deleted can have one (student, curator, grader, admin). There is no `Tenant` entity: any non-empty GUID is accepted, and none clears it. The mechanism stays dormant: `ICurrentTenant` stays `NoTenant`, no query filter changes and every row stays global (project rule "Data"; F-162).
- BR5 Only `PUT /api/v1/identity/users/{id}/tenant` changes it, under the permission `identity.roles.manage`. The request holds `TenantId` and nothing else. Unknown, deleted or erased user: 404 `user.not_found`. The empty GUID: 400 `user.tenant_invalid`. The value already stored: 200, nothing written, no audit entry (F-14 BR3). Roles, status and every other column stay as they were.
- BR6 A change writes one role-change entry (F-14) with the new action `UserTenantChanged`, in the same transaction as the change: `TargetUserId` is the user; `Removed` holds one item with the former tenant (when there was one) and `Added` one item with the new tenant (when there is one), each item with the GUID as key and name and `IsSystem` false; `RoleIds` is empty. The entry rolls back with the change. No schema column is added: the action is stored as text; the column comment is brought up to date if it lists the values.
- BR7 The token carries the stored tenant: the claim `tenant_id` is built from `User.TenantId` at every sign-in and refresh (`TokenEndpoints`), empty when none. A change reaches the user with their next token (the access token lives 15 minutes), as a role change does; this item adds no revocation. F-151 must not treat the claim as instant.
- BR8 `UserSummaryResponse` gains `TenantId` (`Guid?`, optional, last); `GET /api/v1/identity/users` returns it. The account data of the export (F-16) gains the user's `TenantId` the same way, and the user's role-change entries in it include the `UserTenantChanged` ones.
- BR9 An erased account keeps its `TenantId`: the id is a pseudonym that identifies an organization, not a person (ADR-0001 #9); the erasure code does not touch it.
- BR10 The last-manager rule (F-9 BR8b) is untouched: the tenant has no effect on who can manage roles.
- BR11 The dialog of `/admin/users` saves roles and tenant together: the roles request first and only when the roles changed, then the tenant request only when the tenant changed. When the second fails the dialog stays open with the error, and the part already saved shows as saved.
- BR12 New words go to `docs/glossary.md` before their first use in code: `Grader` (Corretor) and the user's tenant (`User.TenantId`, "tenant" in the sense of a partner organization, distinct from the Azure tenant row).

## Screens and API
- `/admin/users` — [changed] the dialog (title now `Users.Dialog.Title`: "Access of {0}") gains a Tenant field; the list gains a Tenant column.
- `/admin/roles` — [changed] lists `Grader` with its permission `questions.grade`; no code change.
- `/admin/role-history` — [changed] shows the action `UserTenantChanged`, translated, with the user and the tenant ids.
- `PUT /api/v1/identity/users/{id}/tenant` — body `SetUserTenantRequest { TenantId: Guid? = null }` → `UserSummaryResponse` (now with `TenantId`).
- `GET /api/v1/identity/users` — `UserSummaryResponse` gains `TenantId`.
- Error codes: `user.tenant_invalid` (400, new), `user.not_found` (404, existing), `identity.forbidden` (403, existing).
- Resource keys (pt-BR, pt-PT, en): `Role.System.Grader`, `Permission.questions.grade.Name`, `Permission.questions.grade.Description`, `Users.Column.Tenant`, `Users.Field.Tenant`, `Users.Field.TenantHelp`, `Users.Tenant.Invalid`, `RoleHistory.Action.UserTenantChanged`, the error text of `user.tenant_invalid`; `Users.Dialog.Title` and `Users.Dialog.Help` change.

### Wireframe: Access dialog of a user (/admin/users)
```text
+----------------------------------------------------------+
| Access of ana@example.com [changed]                      |
+----------------------------------------------------------+
| (i) A user can hold several roles; their permissions add |
|     up. The user's menu changes on their next page load. |
|                                                          |
| [x] Student    ...                                       |
| [ ] Curator    Manage the question bank, Review ...      |
| [x] Grader [new]   Grade questions                       |
| [ ] Admin      ...                                       |
|                                                          |
| Tenant [new]                                             |
| [ 3f2a9c10-7b1e-4c55-9d2e-0a1b2c3d4e5f ____________ ]    |
| Paste the tenant id; leave empty for none.               |
|                                                          |
|                              [ Cancel ]  [ Save ]        |
+----------------------------------------------------------+
```

### Wireframe: Users list (/admin/users)
```text
+----------------------------------------------------------+
| Users                                      [ Search ___ ]|
+----------------------------------------------------------+
| Email           | Name | Status | Roles    | Tenant [new] |
| ana@example.com | Ana  | Active | Grader   | 3f2a9c10     |
| bia@example.com | Bia  | Active | Student  | —            |
+----------------------------------------------------------+
```

## Security
### Surfaces
- S1 `PUT /api/v1/identity/users/{id}/tenant` — callers holding `identity.roles.manage`.
- S2 `PUT /api/v1/identity/users/{id}/roles` — same callers; it can now give `Grader`.
- S3 `GET /api/v1/identity/users` — same callers; it now returns tenant ids.
- S4 The token endpoint — any user signing in or refreshing; it now carries a filled `tenant_id`.
### Threats
- T1 (S1, E) A Student, Curator or Grader (or no one) sets their own or another account's tenant → AC11 [sec]
- T2 (S1, T) A body with other fields, or a value that is not a GUID, changes something else or a wrong tenant → AC12 [sec]
- T3 (S1, R) A tenant is changed and nothing records who did it → AC13 [sec]
- T4 (S3, I) Tenant ids reach a caller without the permission → AC14 [sec]
- T5 (S2, E) The `Grader` role opens the administration or authoring routes → AC15 [sec]
- T6 (S4, S) The `tenant_id` claim is taken from anything but the stored value → AC16 [sec]
### Personal data
| Datum | Lives in | Read by | Kept for | Legal basis |
| --- | --- | --- | --- | --- |
| The user's tenant id | `identity.users.TenantId`; the items of `identity.role_changes` | Administrators holding `identity.roles.manage`; the user, in their own export (F-16) | While the account exists; after erasure the id stays with the pseudonymous account (BR9) | Contract: giving the user access to a partner's service (LGPD art. 7 V; GDPR art. 6(1)(b)) — written by Claude, for the owner to read |

## Docs
- flow: new docs/flows/grader-role-and-user-tenant.md — the role, the tenant assignment, its audit and the token claim, with the two wireframes.

## Acceptance criteria
- AC1 Given a start of the app, when the seed runs, then the role `Grader` exists as a system role and the permission `questions.grade` exists, held by `Grader` and `Admin` and by no other seed role; a second start changes nothing and does not give the permission back to a role it was removed from.
- AC2 Given the role `Grader`, when an Admin renames or deletes it, then it is refused as for the other system roles; when the Admin changes its permissions, they are saved.
- AC3 Given an Admin and a user, when the Admin gives the user the `Grader` role, then the user holds it, the users list shows it and the roles screen lists it with its permission.
- AC4 Given a user with no tenant, when an Admin sets a non-empty GUID, then the answer is 200 with that `TenantId` and the users list returns it.
- AC5 Given a user with a tenant, when an Admin sets `TenantId` to null, then the tenant is cleared.
- AC6 Given a user with a tenant, when an Admin sets the same value, then the answer is 200 and no role-change entry is written.
- AC7 Given any user, when an Admin sets the empty GUID, then the answer is 400 `user.tenant_invalid` and the tenant is unchanged.
- AC8 Given an unknown, deleted or erased user, when an Admin sets a tenant, then the answer is 404 `user.not_found`.
- AC9 Given a user with a tenant, when the user signs in and when the token is refreshed after an Admin changes it, then the claim `tenant_id` is the stored value at that moment, and empty when there is none.
- AC10 Given an erased account with a tenant, then it keeps its `TenantId`.
- AC11 [sec] (T1) Given a caller without `identity.roles.manage` (a Student, a Curator, a Grader, and the caller's own id), when the caller sends `PUT .../users/{id}/tenant`, then the answer is 403 `identity.forbidden` and nothing changes; with no token it is 401.
- AC12 [sec] (T2) Given an Admin, when the body carries fields besides `TenantId`, or a `TenantId` that is not a GUID, then only a valid `TenantId` is applied: the roles, status and other columns stay as they were, and an invalid value is a 400 that changes nothing.
- AC13 [sec] (T3) Given an Admin who changes a user's tenant, when the change is saved, then one entry `UserTenantChanged` records the author, the time, the user and the tenant before and after, in the same transaction: if the entry cannot be saved, the change is not saved either.
- AC14 [sec] (T4) Given a caller without `identity.roles.manage`, when the caller lists users, then the answer is 403 and no tenant id is in the body.
- AC15 [sec] (T5) Given a user whose only role is `Grader`, when the user calls the roles, users and role-history routes of the administration and the question authoring and review routes, then each answers 403 `identity.forbidden`.
- AC16 [sec] (T6) Given a user with tenant A, when any request tries to send a `tenant_id` of its own to the token endpoint or to the session endpoint, then the claim in the token is still A, read from the store.
- AC17 Given a tenant change, when an Admin opens the role history, then the entry is listed with the translated action, the user and the tenant ids before and after.
- AC18 Given a user with a tenant and a tenant change, when the user downloads their data, then the export holds the current `TenantId` and the `UserTenantChanged` entry.
- AC19 Given an Admin on `/admin/users`, when the Admin changes roles and tenant in the dialog and saves, then both are saved; when the second request fails, the dialog stays open with the error text and the part already saved shows as saved.
- AC20 Given the users list, then it has a Tenant column with the first 8 characters of the id and a dash when there is none.
- AC21 Given the least-privileged role that uses the screen (a custom role holding only `identity.roles.manage`), when its holder opens `/admin/users` and edits roles and tenant, then every lookup of the dialog answers 200.
- AC22 All new texts appear in every locale of `UI locales:`.

## Criterion → test
<!-- Written by /agile:build at its start, one row per criterion above, in the same order. -->

## Decisions
- 2026-10-25 — F-150 delivers the `Grader` system role, an assignable `TenantId` on the user and the permission to grade; the visibility rule of the grader lives in the queue (F-151) — D-7 (option A); no filter is woken here — asked
- 2026-10-25 — `TenantId` stays dormant: the item stores the id and nothing is filtered — as decided in `.claude/rules/agile/project.md` (Data) and ADR-0001 #7. Still true here? Yes — confirmed: .claude/rules/agile/project.md
- 2026-10-25 — `Grader` is a system role like Student, Curator and Admin — as decided in F-9 (BR2). Still true here? Yes — confirmed: F-9
- 2026-10-25 — The permission `questions.grade` is declared in this item, in the QuestionBank catalog with the initial grant to `Grader`; F-151 only starts requiring it — a role without a permission would be empty, and the grant is made only in the start that creates the row — asked
- 2026-10-25 — Any account can have a tenant — the grader's rule compares the grader's tenant with the tenant of the student who wrote the essay, so students need one too — asked
- 2026-10-25 — The administrator pastes the tenant as a GUID; empty means none — there is no `Tenant` entity until the first partner; a wrong GUID makes a tenant nobody sees, which F-162 or the partner registration will catch — asked
- 2026-10-25 — A tenant change is recorded in the F-14 history as `UserTenantChanged` — the tenant defines what a person sees, like a role — asked
- 2026-10-25 — Changing a tenant needs `identity.roles.manage`, no new permission — tenant and role are two sides of the same access and share the user administration route — asked
- 2026-10-25 — The tenant is edited in the dialog that already edits roles, and the list shows a Tenant column — one way to edit an item (rule `ui-project`) — asked
- 2026-10-25 — The change reaches the user with the next token (15 minutes at most), no revocation — the same lag a role change has; F-151 reads the tenant knowing this — Claude (technical)
- 2026-10-25 — The audit entry reuses the items of `RoleChange` for the tenant ids and adds no column — the action is stored as text, so only a new enum value and, if the column comment lists them, a comment migration — Claude (technical)
- 2026-10-25 — An erased account keeps its `TenantId` — the id is the pseudonym of an organization, not of a person (ADR-0001 #9) — Claude (technical)
- 2026-10-25 — Retention and legal basis of the tenant id (contract) are written in `## Security` for the owner to read: ADR-0001 has no answer for them — Claude (technical)

## Out of scope
- A `Tenant` entity, a partner registration screen and a tenant picker — the first partner brings them (epic Partner tenants).
- Wiring `ICurrentTenant` to the claim and filtering any module by tenant, including what a curator with a tenant sees — F-162.
- The correction queue and the grader's own visibility rule — F-151.
- Revoking a user's tokens when the tenant changes — not decided; the 15-minute lag stays.
- A menu item or page for the `Grader` — F-151.

## Open questions
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->

## Validation script
<!-- Written at the end of build. -->

## Events
<!-- Written only by events.js. -->
- 2026-10-25 question — asked 7, from context 2

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: feature/F-150
