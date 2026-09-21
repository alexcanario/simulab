---
feature: F-14
epic: Foundation and identity
status: done
board: 726
version: 2
---
# Role change audit trail

## Summary
Record every change to roles and their assignments (role created, edited or deleted, permissions of a role changed, role assigned to or removed from a user): who did it, when, and what changed. Admins can read the trail. Builds on F-9 (role management back office).

## What exists
- F-9 (done, `db7a88c`): every back-office change goes through three handlers in `Simulab.Identity.Application/Roles/`: `SaveRoleHandler` (create; update = rename and/or replace the permission set), `DeleteRoleHandler` (soft delete of a custom role) and `SetUserRolesHandler` (replace a user's role set). Each runs inside `IRoleAdministrationStore.RunExclusiveAsync`: one transaction, rolled back when the work returns a failure (BR8b last-manager check). No code touched these handlers since F-9 (`git log` on the Identity module: F-10, F-11, F-13 and B-9 changed other files).
- `Role` has `CreatedAt/By`, `UpdatedAt/By` and `DeletedAt/By` (F-3 interceptor), which keep only the **last** change, not what changed. `role_permissions` and `user_roles` have no audit columns. ADR-0001 #10 keeps full history for question content only, so this item adds the first history outside it.
- Other writes to `user_roles`, outside the back office: sign-up gives Student (`RegisterUserHandler:91`); account erasure deletes the erased user's rows (`AccountErasureStore:47`, F-10); the first Admin is inserted by hand in the database (`docs/infra.md`).
- F-10: an erased account keeps its id as a pseudonym (`UserErased`, ADR-0001 #9); its email and name are overwritten. Anything that copies a user's email or name elsewhere must be anonymized on `UserErased`.
- Pages: `/admin/roles` and `/admin/users` (`Simulab.Web/Components/Pages/Admin/`), both behind `identity.roles.manage`, built with `AppDataTable` (server paging, search, filters in the query string). Row actions today: Roles — Edit, Delete; Users — Edit roles.
- UI kit: `AppDataTable`, `AppSelectField`, `AppDateColumn`, `AppRowActions`, `AppTruncatedText`, empty/loading/error states. **No date picker** in the kit.
- No audit or history table, page or endpoint exists anywhere in the solution.

## Goal
Let an Admin answer "who gave this person Admin, and when?" and "who changed what this role can do?" from the app, so access changes are traceable without reading the database.

## Users and use cases
- UC1 An Admin changes a role or a user's roles on the F-9 screens; the change is recorded with who, when and what changed, with no extra step.
- UC2 An Admin opens `/admin/role-history` and sees every recorded change, newest first, and narrows it by role, by target user, by author and by period.
- UC3 An Admin clicks **History** on a role (`/admin/roles`) or on a user (`/admin/users`) and lands on `/admin/role-history` already filtered to that role or user.
- UC4 An Admin reads an entry about an account that was erased since (author or target) and sees "Erased account" instead of an email; a deleted or renamed role still shows the name it had.
- UC5 A Student or Curator never sees the history item or the History actions and gets the ordinary Not Found page on `/admin/role-history`.

## Business rules
- BR1 An entry is recorded for each change made through the back office (F-9): role created, role updated (renamed and/or permissions changed), role deleted, a user's roles changed. Automatic writes are not recorded: Student given at sign-up, roles removed by account erasure, the startup seed, the first Admin inserted in the database.
- BR2 An entry holds: when (UTC, `TimeProvider`), the author (user id), the action, the role (id and its name at that moment) or the target user (id), and what changed: for a role, the name before and after (when renamed) and the permissions added and removed; for a user, the roles added and removed (id and name at that moment). A created role lists its initial permissions as added; a deleted role records its name only.
- BR3 The entry is written in the same transaction as the change. A refused or rolled-back change (validation, `role.in_use`, `role_assignment.last_manager`, …) leaves no entry. A save that changes nothing (same name, same set) leaves no entry.
- BR4 Entries are never edited or deleted by the app and are kept forever. No endpoint changes them.
- BR5 Entries hold ids, not personal data. The author's and target's email are read at display time; an erased account (F-10) is shown as "Erased account". Account erasure does not need to touch the trail.
- BR6 Filters: role (entries about that role, plus user entries that added or removed it), target user, author, period (last 7 days, 30 days, 90 days, all; default all). Filters combine with AND and live in the query string. Order: newest first. Paged on the server (10/25/50, default 25; `pageSize` capped at 100).
- BR7 Reading the trail requires `identity.roles.manage` (F-6 policy, 403 `identity.forbidden`); the page, nav item and History actions are hidden without it.
- BR8 The role filter lists every role that appears in the trail, deleted ones included (marked "deleted"); the author filter lists every account that appears as an author. Seed role names are translated; custom names are shown as stored (F-9, BR12). Permissions show their translated name (F-9 resources).

## Screens and API

### Screen: Role history (`/admin/role-history`)
- Layout: `AppPageHeader` (breadcrumb "Administration / Role history", title, no primary action), then one `AppDataTable` card, `MainLayout`.
- Filters above the table: Role (`AppSelectField`, first option `RoleHistory.Filter.AllRoles`), Author (`AppSelectField`, first option `RoleHistory.Filter.AllAuthors`), Period (`AppSelectField`: 7 / 30 / 90 days / all), and the kit search box for the target user (email or name, `RoleHistory.Search.Placeholder`). When opened with `?userId=`, the search box is replaced by a removable chip with that user's email (`RoleHistory.Filter.User`). Changing a filter reloads from the first page.
- Columns: When (`AppDateColumn`, date and time in the user's culture and time zone, sortable, default descending), Author (email, or `RoleHistory.ErasedAccount`), Action (translated: `RoleHistory.Action.*`), Target (role name, or user email / erased account), Changes (one line per change: `Name: A → B`, `+ <permission or role>`, `− <permission or role>`; long lists truncated with a tooltip).
- States: loading; ready; empty (`RoleHistory.Empty` when no entry exists at all; `RoleHistory.NoMatch` when filters match nothing); server error with **Try again**; permission denied: ordinary Not Found page.
- Navigation: Administration section gets "Role history" (`AppIcons.History`, Material Outlined `History`) after "Users", gated by `identity.roles.manage`.
- Shortcuts: row action **History** (`AppIcons.History`, accessible name `Common.ActionOnItem` with the role name or email) on `/admin/roles` (→ `?roleId=<id>`; order: Edit, History, Delete) and `/admin/users` (→ `?userId=<id>`; order: Edit roles, History).
- Accessibility: filters in tab order before the table; each filter has a visible label; the Changes cell is plain text, readable by a screen reader in order.

### UI texts (resource key — en / pt-BR / pt-PT)
Existing keys reused: `Nav.Section.Administration`, `Common.*` (table, states, `ActionOnItem`, `TryAgain`, `LoadFailed`), `Role.System.*`, `Permission.*.Name`.

| Key | en | pt-BR | pt-PT |
|---|---|---|---|
| `Nav.RoleHistory` / `RoleHistory.Title` | Role history | Histórico de papéis | Histórico de perfis |
| `RoleHistory.Column.When` | When | Quando | Quando |
| `RoleHistory.Column.Author` | Changed by | Alterado por | Alterado por |
| `RoleHistory.Column.Action` | Action | Ação | Ação |
| `RoleHistory.Column.Target` | Role or user | Papel ou usuário | Perfil ou utilizador |
| `RoleHistory.Column.Changes` | Changes | Alterações | Alterações |
| `RoleHistory.Action.RoleCreated` | Role created | Papel criado | Perfil criado |
| `RoleHistory.Action.RoleUpdated` | Role changed | Papel alterado | Perfil alterado |
| `RoleHistory.Action.RoleDeleted` | Role deleted | Papel excluído | Perfil eliminado |
| `RoleHistory.Action.UserRolesChanged` | User's roles changed | Papéis do usuário alterados | Perfis do utilizador alterados |
| `RoleHistory.Change.Renamed` | Name: {0} → {1} | Nome: {0} → {1} | Nome: {0} → {1} |
| `RoleHistory.Filter.Role` | Role | Papel | Perfil |
| `RoleHistory.Filter.AllRoles` | All roles | Todos os papéis | Todos os perfis |
| `RoleHistory.Filter.Author` | Changed by | Alterado por | Alterado por |
| `RoleHistory.Filter.AllAuthors` | Anyone | Qualquer pessoa | Qualquer pessoa |
| `RoleHistory.Filter.Period` | Period | Período | Período |
| `RoleHistory.Filter.Period.Days7` | Last 7 days | Últimos 7 dias | Últimos 7 dias |
| `RoleHistory.Filter.Period.Days30` | Last 30 days | Últimos 30 dias | Últimos 30 dias |
| `RoleHistory.Filter.Period.Days90` | Last 90 days | Últimos 90 dias | Últimos 90 dias |
| `RoleHistory.Filter.Period.All` | All time | Todo o período | Todo o período |
| `RoleHistory.Filter.User` | User: {0} | Usuário: {0} | Utilizador: {0} |
| `RoleHistory.Filter.DeletedRole` | {0} (deleted) | {0} (excluído) | {0} (eliminado) |
| `RoleHistory.Search.Placeholder` | Search the user by email or name | Pesquisar o usuário por e-mail ou nome | Pesquisar o utilizador por e-mail ou nome |
| `RoleHistory.ErasedAccount` | Erased account | Conta excluída | Conta eliminada |
| `RoleHistory.Empty` | No role change has been recorded yet. | Nenhuma mudança de papel registrada ainda. | Ainda não foi registada nenhuma alteração de perfil. |
| `RoleHistory.NoMatch` | No change matches these filters. | Nenhuma mudança corresponde a esses filtros. | Nenhuma alteração corresponde a estes filtros. |
| `RoleHistory.Action.History` | History | Histórico | Histórico |

### API
- `GET /api/v1/identity/role-changes?page=&pageSize=&roleId=&userId=&authorId=&search=&days=` → `RoleChangePageResponse { Items: RoleChangeResponse[], Total }`; `RoleChangeResponse { Id, OccurredAt, Author: { Id, Email? }, Action, Role?: { Id, Name, IsSystem }, TargetUser?: { Id, Email? }, NameBefore?, NameAfter?, Added: { Key, Name }[], Removed: { Key, Name }[] }` (`Email` null = erased; `Key` is the permission name or the role id). `days` ∈ {7, 30, 90}, absent = all; another value → 400 `role_change.period_invalid`.
- `GET /api/v1/identity/role-changes/filters` → `RoleChangeFiltersResponse { Roles: { Id, Name, IsSystem, IsDeleted }[], Authors: { Id, Email? }[] }` (BR8).
- Error codes: `role_change.period_invalid` (400), `identity.forbidden` (403, F-6). Unknown `roleId`/`userId`/`authorId` give an empty page, as F-9's `roleId`.

## Acceptance criteria
- AC1 Given an Admin, when they create a role with two permissions, then one `RoleCreated` entry records them as author, the time, the role id and name and the two permissions as added. (UC1, BR1, BR2)
- AC2 Given a custom role, when an Admin renames it and changes its permissions in one save, then one `RoleUpdated` entry records the name before and after and exactly the permissions added and removed. (UC1, BR2)
- AC3 Given a user, when an Admin replaces their roles, then one `UserRolesChanged` entry records the target user and exactly the roles added and removed, with their names at that moment. (UC1, BR2)
- AC4 Given a custom role without holders, when it is deleted, then one `RoleDeleted` entry records its name. (UC1, BR2)
- AC5 Given a change refused by any rule (`role.name_taken`, `role.in_use`, `role.admin_permission_required`, `role_assignment.last_manager`, …) or a save that changes nothing, then no entry is recorded. (BR3)
- AC6 Given a new sign-up, an account erasure or the startup seed, then no entry is recorded. (BR1)
- AC7 Given entries about several roles, users, authors and dates, when an Admin lists them with any combination of role, user, search, author and period, then the page holds exactly the matching entries, newest first, with the total, paged on the server; a role filter includes the user entries that added or removed that role; `days=5` answers 400 `role_change.period_invalid`. (UC2, BR6)
- AC8 Given an entry whose author and target accounts were erased since and whose role was renamed and then deleted, when it is listed, then author and target have no email (shown as "Erased account") and the role keeps the name it had at that moment; the filters endpoint lists the deleted role marked as deleted. (UC4, BR5, BR8)
- AC9 Given the trail, then no endpoint edits or deletes an entry, and an entry holds no email or name of a user. (BR4, BR5)
- AC10 Given a caller without `identity.roles.manage`, when it calls either endpoint, then the answer is 403 `identity.forbidden`; the "Role history" nav item and the History row actions are hidden and `/admin/role-history` shows the ordinary Not Found page. (UC5, BR7)
- AC11 Given the history page, then it shows loading, empty, no-match and error states; changing a filter reloads from the first page; a user id in the address shows the removable user chip; History on a role or a user opens the page filtered to it. (UC2, UC3)
- AC12 Architecture: `Simulab.Identity.Domain` and `Simulab.Identity.Application` still reference neither EF Core nor ASP.NET; the Web references only `Simulab.Identity.Contracts`. (profile)
- AC13 All new texts — page, filters, actions, nav item, error code — exist in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-09-21 — Only changes made by an Admin through the back office are recorded; automatic writes (sign-up, erasure, seed, first Admin) and refused attempts are not — owner, question 1 — they are what someone decided; the rest would be thousands of rows of noise.
- 2026-09-21 — A new page `/admin/role-history` with filters, plus a History row action on roles and users that opens it pre-filtered — owner, question 2 — same pattern as F-9's user count link.
- 2026-09-21 — Each entry shows before and after: old/new name, permissions and roles added and removed — owner, question 3 — "what changed" is the question the trail answers.
- 2026-09-21 — Filters by role, target user, author and period; newest first; server paging; filters in the address — owner, question 4.
- 2026-09-21 — Entries are kept forever and never edited or deleted by the app — owner, question 5 — few rows, no cleanup job.
- 2026-09-21 — Entries hold ids only; emails are read at display time and an erased account shows as "Erased account" — owner, question 6 — no personal data copied, so F-10 erasure needs no change (LGPD/GDPR).
- 2026-09-21 — Reading the trail uses the same permission `identity.roles.manage` — owner, question 7 — as F-9 question 9; a read-only auditor role would be a later feature.
- 2026-09-21 — Roles only, in the Identity module (schema `identity`); no generic audit building block — owner, question 8 — Catalog and Plans decide their own history when they arrive.
- 2026-09-21 — No new packages, for code or tests — owner, question 9.
- 2026-09-21 — Out of scope: CSV export, rebuilding history from before F-14, email alerts — owner, question 10.
- 2026-09-21 — Audit of sign-in, password and account events is not in F-14; captured as F-21 (account event audit trail, AB#736) — owner, follow-up question 1.
- 2026-09-21 — Claude: the period filter is a select (7 / 30 / 90 days / all) instead of a from–to date range — the kit has no date picker; adding one to the kit for one filter is out of proportion. The owner can ask for the range (then `AppDateField` is added to the kit).
- 2026-09-21 — Claude: no mockup (`/agile:screen`): one list page made only of existing kit parts, following the `/admin/users` layout.
- 2026-09-21 — Claude: one table `identity.role_changes` (`id` Guid v7, `occurred_at`, `author_id`, `action` as text, `role_id` null, `role_name` null, `target_user_id` null, `name_before` null, `name_after` null, `added`/`removed` as `jsonb` arrays of `{ key, name }`), indexes on `occurred_at`, `role_id`, `target_user_id`, `author_id`. The entity is neither auditable nor soft-deletable (it is the audit). Global, no tenant (roles are global, F-9 BR1). A user entry also stores the role ids of `added`/`removed` in a `role_ids uuid[]` column (GIN index) so the role filter (BR6) is a plain index lookup.
- 2026-09-21 — Claude: the three handlers compute the difference and add the entry through `IRoleAdministrationStore` inside `RunExclusiveAsync`, before `SaveChangesAsync`, so the rollback of BR8b also removes the entry (BR3). The author is `ICurrentUser`.
- 2026-09-21 — Claude: `role_name` stores the name at the moment (the new name on a rename) — role names are not personal data, and a renamed or deleted role must still read correctly (BR8).
- 2026-09-21 — Approved by the owner ("aprovo F-14").
- 2026-09-21 — Claude (build): `RoleChange` inherits `TenantEntity` like every module entity (rule `project`, same as `ConsentRecord`), so the table also has the tenant, audit and soft-delete columns; "when" and "author" are its `created_at`/`created_by`, filled by the F-3 interceptor from `TimeProvider` and `ICurrentUser` — no separate `occurred_at`/`author_id` columns. Nothing ever updates or deletes the row. `role_ids` holds the role itself for role entries too, so the role filter is one containment test, `role_ids @> ARRAY[@id]` (SQL checked with `ToQueryString`), which the GIN index serves. `added`/`removed` are owned JSON collections (`jsonb`).
- 2026-09-21 — Claude (build): the difference (BR2, BR3) is computed in the domain (`RoleChange.RoleUpdated` / `UserRolesChanged` return null when nothing changed); the store gained `PermissionsOfAsync`, `RolesOfUserAsync`, `RoleNamesAsync` and `AddRoleChange`.
- 2026-09-21 — Claude (build): `RoleChangeActions` in Contracts carries the action names the screen translates; a test pins them to the domain enum.
- 2026-09-21 — Claude (build): times are shown in the browser's time zone (rule `i18n`): a new scoped `UserTimeZone` asks `simulabShell.timeZone()` once per circuit, after the first render; until then, or if the browser does not answer, times are UTC. No such service existed.
- 2026-09-21 — Claude (build): the user filter chip is a new kit component `AppFilterChip` (text + remove button with tooltip and accessible name `Common.RemoveFilter`), shown in the gallery (`gallery-fields`) and tested; icon `AppIcons.History` (Material Outlined `History`) added.
- 2026-09-21 — Claude (build): Web → API wiring checked through the app host: migration `AddRoleChanges` applied on the local database, `/openapi/v1.json` 200 with both new routes, both routes 401 without a token, `/admin/role-history` redirects an anonymous visitor to sign-in. **Not verified by Claude:** the signed-in page itself — signing in needs a password, which Claude does not type; it is in the validation script.
- 2026-09-21 — Found during build, not fixed here: the theme token `ActionDefault` (every icon button: row actions, the chip's remove button) is 1.94:1 on the dark surface (`#404E6A` on `#172035`) and 2.97:1 on white (`#8A96B0`), under the 3:1 WCAG asks for icons. Pre-existing since F-1; proposed as a bug.
- 2026-09-21 — Review (major, confirmed, fixed): `role_change.period_invalid` had no text in the three languages; added, and `RoleResourcesTests` now also checks `role_change.*` codes and every `RoleHistory.Action.*` (seen failing in the three cultures first).
- 2026-09-21 — Review (major, confirmed, fixed): the missing-key test did not cover the new code prefix — same fix as above.
- 2026-09-21 — Review (minor, fixed): nothing pinned the page's policy; `AdminPagesAuthorizationTests` asserts that every `/admin/*` page (Roles, Users, Role history) requires `identity.roles.manage`. The loading state is the kit's (`AppDataTableTests`); the coverage table is below.
- 2026-09-21 — Validation (owner, fixed): "Roles" and "Users" sat on one line in the menu. Cause: MudBlazor's `.mud-tooltip-inline` (inline-block) outweighed `.app-nav-tooltip { display: block }` — present since F-2, visible once short items fit side by side. The rule is now `.mud-tooltip-root.app-nav-tooltip`; measured on screen (each item 230 px wide, one per line) and pinned by `NavMenuStylesTests` (seen failing without the fix).
- 2026-09-21 — Review (minor, rejected): "the GIN index is never used because the filter is `= ANY`" — Npgsql generates `role_ids @> ARRAY[@id]::uuid[]`, which GIN serves; the decision text now says so.

## Out of scope
- CSV or other export of the trail.
- Rebuilding changes made before F-14; the trail starts empty.
- Email or other alerts when someone becomes Admin.
- Audit of sign-in, password, two-factor and account events — F-21 (AB#736).
- Recording automatic writes (sign-up Student, erasure, seed, first Admin) and refused attempts.
- A read-only auditor permission or role.
- A generic audit mechanism for other modules.

## Open questions
- (none)

## Change notes
### v2 — 2026-09-21
- What: (1) BR8: the role filter lists every current role plus the deleted roles found in the trail (not only the roles found in the trail). (2) The address keys are `?role=`, `?user=`, `?author=`, `?days=` (not `roleId`/`userId`), as F-9's `/admin/users?role=`. (3) Row actions on `/admin/roles` are Edit, Delete, History: the kit always puts Edit then Delete first (rule `ui`). (4) The Changes cell says "Added: …" / "Removed: …" (`RoleHistory.Change.Added` / `.Removed`, three languages) instead of "+ …" / "− …", which a screen reader reads as symbols.
- Why: (1) the History action on a role with no entry yet (a seed role) must open the page with that role selected, and a role absent from the list would show an empty filter; (2)–(4) found while building on the existing kit and pages.
- Affected: BR8 and the Screens section; no acceptance criterion changes.
- Re-approved: 2026-09-21 (owner, "Aprovado com ressalva")

## Validation script
You need your Admin account and a second, ordinary account (as in F-9), used in a private window.
1. Start the app host — Git Bash and PowerShell 7, same command: `dotnet run --project src/Hosts/Simulab.AppHost` → the Aspire dashboard URL is printed; the Web answers at https://localhost:7125. Sign in as Admin → Administration shows "Roles", "Users" and "Role history". Open "Role history" → "No role change has been recorded yet." (or earlier entries, if you already changed something today).
2. On "Roles", Add "Revisor" with "Manage roles and user roles"; then Edit it: rename to "Revisor sênior" and untick the permission, Save. Click its **History** action → the page opens filtered by that role with 2 entries, newest first: "Role changed" (Name: Revisor → Revisor sênior; Removed: Manage roles and user roles) and "Role created" (Added: Manage roles and user roles); "Changed by" is your email and the time is your local time.
3. On "Users", Edit roles of the second account: tick Curator, Save. Click **History** on that row → a chip "User: <email>" replaces the search box and one entry shows "User's roles changed" (Added: Curator). Remove the chip with its × → the search box is back and every entry is listed.
4. On "Users", Edit roles on your own row, untick Admin, Save → the last-manager error; Cancel. Back on "Role history" → no new entry.
5. Filters: pick "Changed by" = your email and "Period" = Last 7 days → your entries; pick Role = "Admin" → only entries that added or removed Admin (or "No change matches these filters."). Reload the page → the filters stay (they are in the address).
6. Switch to pt-BR, then pt-PT → "Histórico de papéis" / "Histórico de perfis", "Papel alterado" / "Perfil alterado"; toggle dark mode → table, filters and chip legible in both themes.
7. Permission check: in the private window, signed in with the second account (now Curator) → no "Role history" item, and `/admin/role-history` shows "Page not found".
8. Keyboard only on "Role history": Tab through the search box, the three filters (arrow keys pick a value), the column "When" sort button and the pager, each with a visible focus ring; open a user's History and remove the chip with Enter.

Validated on screen by the owner on 2026-09-21: every step passed, after the menu fix (each item on its own line).

## Coverage
| Criterion | Test(s) |
|---|---|
| AC1 | `RoleChangeTrailTests.CreateRole_RecordsAuthorTimeRoleAndItsPermissions` |
| AC2 | `RoleChangeTrailTests.UpdateRole_RenamedAndPermissionsChangedInOneSave_RecordsOneEntryWithBeforeAndAfter` |
| AC3 | `RoleChangeTrailTests.SetUserRoles_RecordsTheTargetAndExactlyTheRolesAddedAndRemoved` |
| AC4 | `RoleChangeTrailTests.DeleteRole_RecordsItsName` |
| AC5 | `RoleChangeTrailTests.RefusedOrUnchangedSaves_RecordNothing`, `RoleChangeLastManagerTests.LastManagerRefusal_LeavesNoEntry` |
| AC6 | `RoleChangeTrailTests.SignUpErasureAndSeed_RecordNothing` |
| AC7 | `RoleChangeTrailTests.ListRoleChanges_FiltersCombineAndPageNewestFirst`; `RoleHistoryPageTests.FiltersInTheAddress_AreSentAndOfferedWithDeletedRolesAndErasedAuthorsNamed`, `FilterChanged_OnALaterPage_ReloadsFromTheFirstPageWithTheFilter` |
| AC8 | `RoleChangeTrailTests.ErasedAccountsAndARenamedThenDeletedRole_ReadAsTheyWere`; `RoleHistoryPageTests.Load_ShowsWhenAuthorActionTargetAndChanges` |
| AC9 | `RoleChangeTrailTests.Trail_HasNoWriteRouteAndHoldsNoPersonalData` |
| AC10 | `RoleAdministrationTests.EveryEndpoint_WithoutRolesManage_IsForbidden` (2 new routes); `NavigationItemsTests.All_AdministrationItems_AreRolesUsersAndRoleHistoryBehindRolesManage`; `AdminPagesAuthorizationTests.EveryAdminPage_RequiresRolesManage`; Not Found page on screen: validation script step 7 |
| AC11 | `RoleHistoryPageTests.Empty_WithoutFilters_SaysNothingWasRecorded_WithAFilter_SaysNothingMatches`, `Load_ApiFails_ShowsTheErrorStateWithTryAgain`, `UserInTheAddress_ShowsARemovableChipInsteadOfTheSearch`, `FilterChanged_…`, `HistoryAction_OnRolesAndUsers_OpensTheHistoryFilteredToTheRow`, `Load_BrowserInSaoPaulo_ShowsTheLocalTime`; `AppFilterChipTests`; loading state: `AppDataTableTests` (kit) |
| AC12 | `IdentityModuleBoundaryTests` and the rest of `Simulab.ArchitectureTests` (29, green) |
| AC13 | `ResourceParityTests`, `RoleResourcesTests.EveryPermissionSystemRoleAndErrorCode_HasAText` (now with `role_change.*` and the actions), `RoleHistoryPageTests.Load_InPortuguesePortugal_TranslatesTitleActionsAndErasedAccounts`; `RoleChangeActionsTests` |

## Delivery
- Branch: `feature/F-14` (merged with `--no-ff` into `main`, AB#726, and deleted)
- Tests: 652, 33 s (full suite, architecture tests included); full build 12 s, 0 warnings, baseline still empty
- Manual pages: `docs/manual/{en,pt-BR,pt-PT}/role-history.md` (new), linked from each locale's `index.md`, `roles.md` and `users.md`
