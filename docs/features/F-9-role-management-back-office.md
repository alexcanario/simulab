---
feature: F-9
epic: Foundation and identity
status: validating
board: 713
version: 3
---
# Role management back office

## Summary
Admin screens to create, edit and delete roles, set the permissions of each role and assign roles to users. Built with the UI kit. Needs /agile:screen.

## Goal
Let an Admin manage access from the app instead of editing the database: create custom roles, choose their permissions from the catalog, and give or take roles from any user, without ever locking everyone out.

## What exists
- F-6: `Role : IdentityRole<Guid>` (table `identity.roles`, unique `ux_roles_normalized_name`), `Permission` (Name is the key, Description), `RolePermission` (composite key, cascade on both sides), `identity.user_roles` (N:N). `Role` has **no system flag, no audit and no soft delete** columns.
- F-6: the permission catalog has **one** permission, `identity.roles.manage` (`IdentityPermissions.All`), seeded by migration and granted to Admin only. Future modules (Catalog, Plans) add theirs the same way.
- F-6: API enforcement is the `Permission:<name>` policy; `PermissionQueryService` caches a user's effective permissions for 10 s, per host, with no active invalidation. The Web reads permissions from `GET /api/v1/identity/session` at sign-in and at every silent token refresh (access token: 15 min).
- F-6: `GET /api/v1/identity/roles` returns `RoleNamesResponse { Names }`; `/admin/roles` is a read-only placeholder list; nav item "Roles" (Administration) is gated by `identity.roles.manage`.
- F-6, BR10: the only way to make someone Curator or Admin today is a row inserted by hand in `identity.user_roles` (`docs/infra.md`).
- **No user listing exists** in the API or the Web. `IUserDirectory` only serves registration.
- UI kit: `AppDataTable` with server-side paging (`AppTableQuery`: page, size, search, sort), `AppRowActions`, `AppConfirmDialog`/`IConfirmService`, `AppCheckbox`, `AppTextField`, `AppFormActions`, empty/loading/error states.
- Simulae (read-only reference): create role, delete role (system roles protected), replace a role's permission set, assign/remove a role to a user by typing the user id (no user list), `IsSystem` flag on roles. Its pages use raw MudBlazor and Portuguese names; converted to the kit on the way in.

## Users and use cases
- UC1 An Admin opens `/admin/roles` and sees every role: name (seed roles translated), whether it is a system role, how many permissions and how many users it has.
- UC2 An Admin adds a custom role with a name and a set of permissions picked from the catalog.
- UC3 An Admin edits a role: the name and permissions of a custom role, only the permissions of a system role.
- UC4 An Admin deletes a custom role that no user holds.
- UC5 An Admin opens `/admin/users`, searches a user by email or name, or filters by a role (also by clicking a role's user count on `/admin/roles`), and sees their account status and roles.
- UC6 An Admin edits a user's roles (several roles allowed) and the user's API access reflects it within 10 s; their menu reflects it at their next page load.
- UC7 A Student or Curator never sees the Administration items and gets the ordinary Not Found page on `/admin/roles` and `/admin/users`.

## Business rules
- BR1 Roles are global (no tenant) as in F-6. A role is a system role (`IsSystem`) or a custom role. Student, Curator and Admin are system roles, marked by the migration (existing rows) and by the startup seed (new installations).
- BR2 A system role cannot be renamed or deleted; its permissions can be changed (subject to BR8).
- BR3 A role name is trimmed, 2 to 50 characters, and unique case-insensitively among all roles, deleted ones included (the unique index covers deleted rows, so a deleted role's name stays taken).
- BR4 A role's permissions are a set of names from the catalog; an unknown name is refused. Saving replaces the whole set.
- BR5 A custom role is deleted only while no user holds it; deleting is a soft delete (rule `ui-project`). A deleted role disappears from both screens and grants nothing.
- BR6 A user may hold several roles; their effective permissions are the union. Saving a user's roles replaces the whole set with the chosen ones; an empty set is allowed.
- BR7 Accounts of any status (Pending or Active) are listed and can receive roles. Deleted accounts are not listed.
- BR8 Never lock everyone out: (a) the Admin role always keeps `identity.roles.manage`; (b) any change — a user's roles or a role's permissions — that would leave no Active, non-deleted user holding `identity.roles.manage` is refused. The check and the write run in one transaction, so two Admins acting at once cannot both remove the last manager.
- BR9 Propagation stays as in F-6: the API reflects a change within the 10 s cache; the affected user's menu reflects it at their next page load (B-3 BR4 re-reads the permissions on every page load). No active invalidation.
- BR10 Every endpoint and page of this feature requires `identity.roles.manage` (F-6 policy, 403 `identity.forbidden`); the pages and nav items are hidden without it (F-6, BR6–BR7).
- BR11 The first Admin of an installation is still assigned in the database (`docs/infra.md`, now reworded as "first Admin only"); every later assignment goes through the screen.
- BR12 Permission names and descriptions shown on screen come from resource files in the three languages (key = the permission name), grouped by module (the name's prefix before the first dot). The technical name is shown below, small. Seed role names are shown translated; custom role names are shown as stored.

## Screens and API
Mockup: `docs/features/mockups/F-9-role-management-back-office.html` (every state below, three languages, light and dark).

### Screen: Roles (`/admin/roles`)
- Layout: `AppPageHeader` (breadcrumb "Administration / Roles", title, primary action **Add** at the top right), then one `AppDataTable` card. `MainLayout`.
- Table columns: Name (sortable, default ascending; system roles show their translated name and a "System" badge), Permissions (count, right-aligned), Users (count, right-aligned, user's culture; a link to `/admin/users?role=<id>` with the accessible name `Roles.UsersLink`), actions (last column, `AppRowActions`): **Edit**, then **Delete**.
  - Delete is not rendered for a system role; for a custom role held by users it is disabled with the tooltip `Roles.Delete.InUseTip` (the API's `role.in_use` still guards a race).
  - Five roles at most for a long time: the list is loaded whole and paged in the table (kit page sizes 10/25/50, default 25); no search box.
- Role dialog (Add / Edit, kit dialog, Esc closes, unsaved changes ask `Common.Discard.*`):
  - Name — `AppTextField`, required, 2–50 characters (the field stops at 50), validated on change and on submit; read-only for a system role with the help text `Roles.Field.Name.SystemHelp`.
  - Permissions — one bordered group per module (`PermissionGroup.<prefix>`, with "n of m selected"), one `AppCheckbox` per permission: translated name (bold), translated description, technical name below in monospace.
  - `AppFormActions`: **Save** (primary, disabled with progress while saving), **Cancel** (text).
  - A business or server error is an `AppAlert` at the top of the dialog; a name error sits under the field.
- Delete: kit confirmation dialog (`IConfirmService`), title `Common.Delete.Title`, message `Roles.Delete.Message`, error-coloured button `Common.Delete.Confirm` naming the role.
- Success: snackbar `Roles.Saved` / `Roles.Deleted`; the list reloads.
- States: loading (`AppLoadingState`); ready; empty is not reachable (three system roles always exist) and uses the kit's empty text if it ever is; server error (`AppErrorState` with **Try again**); dialog: add, edit custom, edit system, validation error, saving, server errors `role.name_taken`, `role.permission_unknown`, `role.admin_permission_required`, `role_assignment.last_manager`, `role.system_role_protected`, `role.not_found`; delete: confirmation, success, refused `role.in_use` (alert above the table); permission denied: ordinary Not Found page.

### Screen: Users (`/admin/users`)
- Layout: `AppPageHeader` (breadcrumb "Administration / Users", title, no primary action — users are created by sign-up), then one `AppDataTable` card with the kit search box (placeholder `Users.Search.Placeholder`, debounced by the kit) and, next to it, a role filter (`AppSelectField`, label `Users.Filter.Role`, first option `Users.Filter.AllRoles`, then every role by name; kept in the query string `?role=<id>` so the link from the roles page opens it pre-selected), server-side paging (10/25/50, default 25) and sorting.
- Table columns: Email (sortable, default ascending), Name (sortable; "—" when empty; long text truncated with tooltip), Status (dot + `AccountStatus.Pending` / `AccountStatus.Active`), Roles (chips with the role names; `Users.NoRoles` when empty), actions: **Edit roles** (edit icon, tooltip and accessible name `Users.EditRoles.Action` + email).
- Edit roles dialog: title `Users.Dialog.Title` with the email; info `AppAlert` `Users.Dialog.Help` (roles add up; menu updates at the next page load, BR9); one `AppCheckbox` per role (translated or stored name; its permissions, translated, as the description) inside a `fieldset` with a hidden legend; **Save** / **Cancel** as above.
- Success: snackbar `Users.Saved`; the row reloads.
- States: loading; ready; empty search (`Users.NoMatch` with the term); server error with **Try again**; dialog: ready, saving, errors `role_assignment.last_manager`, `role_assignment.role_unknown`, `user.not_found`; permission denied: ordinary Not Found page.

### Navigation and permissions
- Administration section of the drawer: "Roles" (`AppIcons.Roles`, existing) and new "Users" (new semantic icon `AppIcons.Users`, Material Outlined `Group`), both gated by `identity.roles.manage`. Admin sees both; Student and Curator see neither and get Not Found on both routes (BR10).

### Accessibility
- Tab order: primary action, search (users), table header sort buttons, row actions left to right, pager. In a dialog: first field, checkboxes, Cancel, Save; focus returns to the row action that opened it.
- Icon-only row actions have a tooltip and an accessible name "<action>: <item>" (`Common.ActionOnItem`).
- The error alert in a dialog and the `role.in_use` alert use `role="alert"`; the snackbar and loading states use `role="status"`; the delete dialog is an `alertdialog` described by its message.
- Checkboxes are native inputs with the permission or role name as label; the counter "n/50" is part of the field's description.

### UI texts (resource key — en / pt-BR / pt-PT)
Existing keys reused: `Common.Add`, `Common.Edit`, `Common.Delete`, `Common.Save`, `Common.Saving`, `Common.Cancel`, `Common.Search`, `Common.TryAgain`, `Common.LoadFailed`, `Common.Loading`, `Common.Actions`, `Common.ActionOnItem`, `Common.Table.*`, `Common.Delete.Title`, `Common.Delete.Confirm`, `Common.Discard.*`, `Nav.Section.Administration`, `Nav.Roles`, `Roles.Title`.

| Key | en | pt-BR | pt-PT |
|---|---|---|---|
| `Nav.Users` / `Users.Title` | Users | Usuários | Utilizadores |
| `Roles.Column.Name` / `Users.Column.Name` / `Roles.Field.Name` | Name | Nome | Nome |
| `Roles.Column.Permissions` / `Roles.Field.Permissions` | Permissions | Permissões | Permissões |
| `Roles.Column.Users` | Users | Usuários | Utilizadores |
| `Roles.SystemBadge` | System | Sistema | Sistema |
| `Roles.Delete.InUseTip` | Held by {0} users: remove it from them first | {0} usuários têm este papel: tire dele antes | {0} utilizadores têm este perfil: retire-o primeiro |
| `Roles.Dialog.AddTitle` | Add role | Adicionar papel | Adicionar perfil |
| `Roles.Dialog.EditTitle` | Edit role | Editar papel | Editar perfil |
| `Roles.Field.Name.Help` | 2 to 50 characters | De 2 a 50 caracteres | Entre 2 e 50 caracteres |
| `Roles.Field.Name.SystemHelp` | System roles keep their name. | Papéis de sistema mantêm o nome. | Os perfis de sistema mantêm o nome. |
| `Roles.Permissions.Selected` | {0} of {1} selected | {0} de {1} selecionadas | {0} de {1} selecionadas |
| `Roles.Saved` | Role saved. | Papel salvo. | Perfil guardado. |
| `Roles.Deleted` | Role deleted. | Papel excluído. | Perfil eliminado. |
| `Roles.Delete.Message` | The role {0} will be deleted. No user holds it, so nobody loses access. Its name cannot be reused. | O papel {0} será excluído. Nenhum usuário o possui, então ninguém perde acesso. O nome não poderá ser reutilizado. | O perfil {0} será eliminado. Nenhum utilizador o tem, por isso ninguém perde acesso. O nome não poderá ser reutilizado. |
| `Role.System.Student` | Student | Estudante | Estudante |
| `Role.System.Curator` | Curator | Curador | Curador |
| `Role.System.Admin` | Admin | Administrador | Administrador |
| `PermissionGroup.identity` | Identity and access | Identidade e acesso | Identidade e acesso |
| `Permission.identity.roles.manage.Name` | Manage roles and user roles | Gerenciar papéis e atribuições | Gerir perfis e atribuições |
| `Permission.identity.roles.manage.Description` | Create, edit and delete roles, choose their permissions and give roles to users. | Criar, editar e excluir papéis, escolher suas permissões e atribuir papéis a usuários. | Criar, editar e eliminar perfis, escolher as suas permissões e atribuir perfis a utilizadores. |
| `Users.Column.Email` | Email | E-mail | E-mail |
| `Users.Column.Status` | Status | Situação | Estado |
| `Users.Column.Roles` | Roles | Papéis | Perfis |
| `Users.Search.Placeholder` | Search by email or name | Pesquisar por e-mail ou nome | Pesquisar por e-mail ou nome |
| `Users.Filter.Role` | Role | Papel | Perfil |
| `Users.Filter.AllRoles` | All roles | Todos os papéis | Todos os perfis |
| `Roles.UsersLink` | See the {0} users with the role {1} | Ver os {0} usuários com o papel {1} | Ver os {0} utilizadores com o perfil {1} |
| `Roles.UsersLink.One` (build: one holder) | See the user with the role {0} | Ver o usuário com o papel {0} | Ver o utilizador com o perfil {0} |
| `Roles.Delete.InUseTip.One` (build: one holder) | Held by 1 user: remove it from them first | 1 usuário tem este papel: tire dele antes | 1 utilizador tem este perfil: retire-o primeiro |
| `Users.NoRoles` | No roles | Sem papéis | Sem perfis |
| `Users.NoMatch` | No user matches "{0}". | Nenhum usuário corresponde a "{0}". | Nenhum utilizador corresponde a "{0}". |
| `Users.EditRoles.Action` | Edit roles | Editar papéis | Editar perfis |
| `Users.Dialog.Title` | Roles of {0} | Papéis de {0} | Perfis de {0} |
| `Users.Dialog.Help` | A user can hold several roles; their permissions add up. The user's menu changes on their next page load. | Um usuário pode ter vários papéis; as permissões se somam. O menu do usuário muda no próximo carregamento de página. | Um utilizador pode ter vários perfis; as permissões somam-se. O menu do utilizador muda no próximo carregamento de página. |
| `Users.Saved` | Roles of {0} saved. | Papéis de {0} salvos. | Perfis de {0} guardados. |
| `AccountStatus.Pending` | Pending | Pendente | Pendente |
| `AccountStatus.Active` | Active | Ativa | Ativa |
| `role.name_invalid` | Use 2 to 50 characters. | Use de 2 a 50 caracteres. | Use entre 2 e 50 caracteres. |
| `role.name_taken` | A role with this name already exists (deleted roles included). | Já existe um papel com este nome (incluindo papéis excluídos). | Já existe um perfil com este nome (incluindo perfis eliminados). |
| `role.permission_unknown` | One of the permissions no longer exists. Reload the page and try again. | Uma das permissões não existe mais. Recarregue a página e tente de novo. | Uma das permissões já não existe. Recarregue a página e tente novamente. |
| `role.not_found` | This role no longer exists. | Este papel não existe mais. | Este perfil já não existe. |
| `role.system_role_protected` | System roles cannot be renamed or deleted. | Papéis de sistema não podem ser renomeados nem excluídos. | Os perfis de sistema não podem ser renomeados nem eliminados. |
| `role.admin_permission_required` | The Admin role must keep "Manage roles and user roles". | O papel Administrador precisa manter "Gerenciar papéis e atribuições". | O perfil Administrador tem de manter "Gerir perfis e atribuições". |
| `role.in_use` | Remove this role from every user before deleting it. | Tire este papel de todos os usuários antes de excluí-lo. | Retire este perfil de todos os utilizadores antes de o eliminar. |
| `role_assignment.role_unknown` | One of the roles no longer exists. Reload the page and try again. | Um dos papéis não existe mais. Recarregue a página e tente de novo. | Um dos perfis já não existe. Recarregue a página e tente novamente. |
| `role_assignment.last_manager` | This would leave nobody able to manage roles. Give that permission to another active user first. | Assim ninguém mais poderia gerenciar papéis. Dê essa permissão a outro usuário ativo antes. | Assim ninguém mais poderia gerir perfis. Dê essa permissão a outro utilizador ativo primeiro. |
| `user.not_found` | This user no longer exists. | Este usuário não existe mais. | Este utilizador já não existe. |

### API
- `GET /api/v1/identity/permissions` — the catalog: `PermissionResponse[] { Name }`.
- `GET /api/v1/identity/roles` — changed: `RoleResponse[] { Id, Name, IsSystem, Permissions: string[], UserCount }` (replaces `RoleNamesResponse`; its only caller is the Web).
- `POST /api/v1/identity/roles` — `SaveRoleRequest { Name, Permissions: string[] }` → 201 `RoleResponse`.
- `PUT /api/v1/identity/roles/{id}` — `SaveRoleRequest` → 200 `RoleResponse`.
- `DELETE /api/v1/identity/roles/{id}` → 204.
- `GET /api/v1/identity/users?page=&pageSize=&search=&roleId=&sortBy=&descending=` (`roleId` optional: only users holding that role; an unknown id gives an empty page) → `UserPageResponse { Items: UserSummaryResponse[] { Id, Email, FullName, Status, Roles: { Id, Name, IsSystem }[] }, Total }` (rule api-contracts: `{ items, total }`, `pageSize` capped at 100).
- `PUT /api/v1/identity/users/{id}/roles` — `SetUserRolesRequest { RoleIds: Guid[] }` → 200 `UserSummaryResponse`.
- Error codes:
  - `role.name_invalid` (400) — blank, shorter than 2 or longer than 50 (BR3).
  - `role.name_taken` (409) (BR3).
  - `role.permission_unknown` (400) (BR4).
  - `role.not_found` (404).
  - `role.system_role_protected` (422) — rename or delete of a system role (BR2).
  - `role.admin_permission_required` (422) — removing `identity.roles.manage` from Admin (BR8a).
  - `role.in_use` (422) — delete while users hold it (BR5).
  - `role_assignment.role_unknown` (400) — a role id that does not exist or is deleted (BR6).
  - `role_assignment.last_manager` (422) — the change would leave no active manager (BR8b); also returned by `PUT /roles/{id}` when a custom role's permission change causes it.
  - `user.not_found` (404).
  - `identity.forbidden` (403, F-6).

## Acceptance criteria
- AC1 Given the migration runs, then Student, Curator and Admin are system roles and every existing user-role and role-permission row is unchanged. (BR1)
- AC2 Given an Admin, when they list roles, then each role comes with its system flag, its permissions and its user count; deleted roles are not listed. (UC1, BR5)
- AC3 Given a valid name and known permissions, when an Admin creates a role, then it is saved as a custom role with exactly those permissions. (UC2, BR4)
- AC4 Given a name that is blank, 1 character or 51 characters after trimming, when a role is created or renamed, then the answer is 400 `role.name_invalid`. (BR3)
- AC5 Given an existing role "Reviewer" (active or deleted) or a seed role, when another role is saved as "reviewer" or "ADMIN", then the answer is 409 `role.name_taken`. (BR3)
- AC6 Given a permission name that is not in the catalog, when a role is saved with it, then the answer is 400 `role.permission_unknown` and nothing changes. (BR4)
- AC7 Given a custom role held by a user, when an Admin changes its permissions, then that user's next API call after the cache window reflects the new set. (UC3, BR4, BR9)
- AC8 Given a system role, when it is renamed or deleted, then the answer is 422 `role.system_role_protected`; when only its permissions change, the change is saved. (BR2)
- AC9 Given the Admin role, when a save would remove `identity.roles.manage` from it, then the answer is 422 `role.admin_permission_required`. (BR8a)
- AC10 Given a custom role held by at least one user, when it is deleted, then the answer is 422 `role.in_use`; given no holder, then 204, it leaves the list and its name stays taken. (UC4, BR3, BR5)
- AC11 Given users with Pending and Active accounts and one deleted account, when an Admin lists users with a search term and/or a role filter, then the page holds the matching non-deleted accounts (email or name, case-insensitive; holding that role), with status, roles and the total count, paged and sorted on the server. (UC5, BR7)
- AC12 Given a user, when an Admin sets their roles to Curator and a custom role, then the user holds exactly those two and their effective permissions are the union; an empty set is also accepted. (UC6, BR6)
- AC13 Given exactly one Active user holding `identity.roles.manage`, when a change would take it from them (their roles, or a custom role's permissions), then the answer is 422 `role_assignment.last_manager` and nothing changes; given a second Active manager, the same change succeeds. (BR8b)
- AC14 Given an unknown user id, an unknown or deleted role id in a set, or an unknown role id on PUT/DELETE, then the answers are 404 `user.not_found`, 400 `role_assignment.role_unknown` and 404 `role.not_found`. (BR5, BR6)
- AC15 Given a caller without `identity.roles.manage`, when it calls any endpoint of this feature, then the answer is 403 `identity.forbidden`; the "Users" nav item is hidden and `/admin/users` shows the ordinary Not Found page. (UC7, BR10)
- AC16 Given the roles and users screens, then each list has loading, empty and error states, Delete asks for confirmation naming the role, Delete is not offered for system roles, the name field is read-only for a system role, and an API error code is shown as translated text next to the form. (UC1–UC6)
- AC17 Given every permission in the catalog, then its name and description exist in pt-BR, pt-PT and en resources, and the three seed role names are translated. (BR12)
- AC18 Architecture: `Simulab.Identity.Domain` and `Simulab.Identity.Application` still reference neither EF Core nor ASP.NET; the Web references only `Simulab.Identity.Contracts`. (profile)
- AC19 All new texts — pages, dialogs, nav item, error codes, permission and role names — exist in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-09-19 — Full scope: role create/rename/delete, permissions per role, and role assignment to users — owner, question 1 — the catalog grows with Catalog and Plans, and Simulae's flow is proven.
- 2026-09-19 — A new paged user list `/admin/users` with search by email or name is how the Admin finds users — owner, question 2 — Simulae's "type the user id" gives no view of who holds what.
- 2026-09-19 — Pending accounts are listed and can receive roles, with their status shown — owner, question 3 — a role grants nothing to an account that cannot sign in.
- 2026-09-19 — Several roles per user, permissions summed — owner, question 4 — it is the F-6 model already.
- 2026-09-19 — System roles: permissions editable, never renamed or deleted — owner, question 5 — code depends on their names (sign-up assigns Student).
- 2026-09-19 — Two lock-out guards (Admin keeps `identity.roles.manage`; never zero active managers) — owner, question 6 — without them one click locks everyone out and only the database recovers.
- 2026-09-19 — Deleting a role in use is refused; deletion is a soft delete — owner, question 7 — nobody loses access without noticing; rule `ui-project`.
- 2026-09-19 — Role name 2–50 characters, unique case-insensitively including deleted roles; custom names are data, not translated; seed names are translated — owner, question 8.
- 2026-09-19 — One permission, `identity.roles.manage`, gates roles, users and assignments — owner, question 9 — only Admin manages today; a split brings no real gain.
- 2026-09-19 — Propagation unchanged from F-6 (API ≤10 s, menu at next refresh) — owner, question 10 — nothing new to build for a visual-only gain.
- 2026-09-19 — No audit history of role changes in F-9; roles only get CreatedBy/UpdatedBy — owner, question 11 — already captured as F-14 (role change audit trail, AB#726).
- 2026-09-19 — Two pages with dialogs (roles, users) — owner, question 12 — rule "simple entity = dialog".
- 2026-09-19 — Permissions shown by translated name and description, grouped by module, technical name below — owner, question 13 — no hardcoded UI text.
- 2026-09-19 — Mockup through `/agile:screen F-9` before approval — owner, question 14.
- 2026-09-19 — No new packages, for code or tests — owner, question 15.
- 2026-09-19 — Claude: `Role` gains `IsSystem`, the audit fields and the soft-delete fields (interfaces `IAuditableEntity`, `ISoftDeletableEntity`, same way as `User`); one migration marks the three seed roles as system roles. The unique index on `normalized_name` stays unfiltered so a deleted name stays taken.
- 2026-09-19 — Claude: `GET /api/v1/identity/roles` changes shape (`RoleResponse[]`) instead of adding a second route; its only caller is the Web, deployed together.
- 2026-09-19 — Claude: saving a role's permissions or a user's roles replaces the whole set (PUT), which matches the checkbox dialogs and keeps one endpoint per dialog.
- 2026-09-19 — Claude: the rules live in the Application layer (`SaveRoleHandler`, `DeleteRoleHandler`, `SetUserRolesHandler`), the BR8b check in one serializable transaction with the write; concurrent edits of the same role are last-write-wins (two Admins today, no conflict screen).
- 2026-09-19 — Claude: user search is a case-insensitive `ILIKE` on email and full name; default sort by email; page sizes from the kit.
- 2026-09-19 — Review (major, confirmed, fixed): changing the role filter kept the current page; it now reloads from the first page (`AppDataTable.ReloadFromFirstPageAsync`), test `UsersPageTests.RoleFilterChanged_OnALaterPage_ReloadsFromTheFirstPage`.
- 2026-09-19 — Review (minor, fixed): effective permissions now join through `Roles`, so a deleted role grants nothing by construction, not only because it has no holder.
- 2026-09-19 — Review (minor, fixed): a system role saved with a blank or out-of-bounds name answers 400 `role.name_invalid`, like a custom role (`UpdateSystemRole_BlankName_IsRefusedAsInvalid`).
- 2026-09-19 — Review (minor, fixed): the role dialog asks for a fresh token at Save and never falls back to the one read when it opened.
- 2026-09-19 — Review (minor, fixed): tests added for concurrent last-manager removal (`ConcurrentLastManagerTests`, seen failing with the advisory lock removed), for `_` and `\` in the search, and for the users list error state.
- 2026-09-19 — Review (minor, accepted): a disabled Delete gives its reason in a tooltip only; the user count, a link in the same row, says the same thing to keyboard and screen-reader users. Focusable disabled buttons would be a kit-wide change.
- 2026-09-19 — Review (minor, accepted): roles created before F-9 keep `created_at = 0001-01-01` (their real date is unknown), and the migration's `UPDATE … is_system` is not tested on a pre-F-9 database; the startup seed marks the same rows and is tested (`SystemRoleSeedTests`). The migration already ran on the local database, so editing it would not re-run.
- 2026-09-19 — Claude (build): the seed roles are created at startup by `EnsureRolesAndPermissionsAsync` (F-6), not by a migration, so `IsSystem` is set in both places: the migration marks the three existing rows, the seed creates them marked. The paged response is `{ Items, Total }` (rule `api-contracts`), not `TotalCount`.
- 2026-09-19 — The users list has a role filter next to the search, and each role's user count on `/admin/roles` links to it — owner, screen question 1 — finding the Admins among thousands of Students by search alone is not workable, and `role_assignment.last_manager` asks for exactly that.
- 2026-09-19 — Delete of a custom role in use is shown disabled with a tooltip giving the user count, instead of opening a dialog that would only fail; the API check stays for races — owner, screen question 2.
- 2026-09-19 — The edit-roles dialog shows each role's translated permissions under its name — owner, screen question 3 — the Admin sees what they grant.
- 2026-09-19 — Claude (screen): the roles table has no search box (a handful of rows); the users table has search, paging and sorting on the server.
- 2026-09-19 — Claude (screen): the users page has no primary action (accounts come from sign-up); its only row action is "Edit roles", with the edit icon.

## Out of scope
- History of role and permission changes (audit log) — F-14 (AB#726).
- Tenant-scoped roles (Institutions epic).
- Blocking, suspending, editing or deleting user accounts (F-10 covers erasure), ending a user's sessions.
- Permissions granted directly to a user (only through roles).
- Immediate menu refresh for the affected user.
- A second permission for assignment and anti-escalation rules (Simulae's `Role.Assign`).

## Open questions
- (none)

## Change notes
### v3 — 2026-09-19
- What: (1) the affected user's menu follows a role change at their next page load, not at the next token refresh (up to 15 min); `Users.Dialog.Help` says so in the three languages. (2) The role name field has no "n/50" counter: it stops accepting input at 50 characters, and the 2-character minimum is shown by the help text and the error.
- Why: (1) false premise found during build: since B-3 (BR4) the Web re-reads the permissions on every page load, so "within 15 minutes" would have told the Admin something untrue. (2) The kit's `AppTextField` has one hint line and no counter; adding one to the kit for one field is out of proportion.
- Affected: UC6, BR9, the `Users.Dialog.Help` text and the name field description in Screens; no acceptance criterion changes.
- Re-approved: 2026-09-19 (owner, in the build session)

### v2 — 2026-09-19
- What: the business-rule refusals `role.system_role_protected`, `role.admin_permission_required`, `role.in_use` and `role_assignment.last_manager` answer 422 instead of 409; `role.name_taken` stays 409. Codes and texts unchanged.
- Why: rule `api-contracts` (409 conflict, 422 business rule) and the module's existing `ErrorKind.BusinessRule` → 422 mapping; the approved file contradicted both (false premise found at the start of build).
- Affected: AC8, AC9, AC10, AC13 (status number only); other criteria unchanged.
- Re-approved: 2026-09-19 (owner, option A in the build session)

## Validation script
You need two accounts: your Admin account (from F-6; if you have none, make one with `docs/infra.md`, "The first Admin") and a second, ordinary account (sign up at `/sign-up`), used in a private window.
1. Start the app host — Git Bash and PowerShell 7, same command: `dotnet run --project src/Hosts/Simulab.AppHost` → the Aspire dashboard URL is printed; the Web answers at https://localhost:7125. Sign in as your Admin → the Administration section shows "Roles" and "Users".
2. Open "Roles" → Admin, Curator and Student with the "System" badge, their permission and user counts; click Student's user count → `/admin/users` opens filtered by Student.
3. On "Roles", Add → name "Revisor", tick "Manage roles and user roles", Save → snackbar "Role saved."; Add "revisor" again → the dialog shows "A role with this name already exists". Edit Admin → the name is read-only; untick the permission, Save → "The Admin role must keep …"; Cancel → asks to discard.
4. Open "Users", Edit roles on your own row, untick Admin, Save → "This would leave nobody able to manage roles…" (you are the only active Admin); Cancel.
5. Permission check: in the private window, sign in with the second account → no Administration section, and `/admin/users` shows "Page not found". Back as Admin, on "Users" search its email → status Active, role Student; Edit roles → tick Curator and Revisor, Save → snackbar and the two chips. In the private window reload → its menu now shows "Roles" and "Users".
6. On "Roles", Revisor's Delete is disabled, and its tooltip says "Held by 1 user…". On "Users", set the second account back to Student only → in the private window reload: "Roles"/"Users" are gone. On "Roles", Delete Revisor → the confirmation names "Revisor" → "Role deleted."; Add "Revisor" again → "already exists (deleted roles included)".
7. Switch the language to pt-BR, then pt-PT → "Papéis"/"Usuários" and "Perfis"/"Utilizadores", "Estudante", "Gerenciar papéis e atribuições" / "Gerir perfis e atribuições"; toggle dark mode → badge, chips, status dots and dialogs legible in both themes.
8. Keyboard only on "Roles": Tab to Add, Enter, type a name, Tab to the permission, Space, Esc → asks to discard; on "Users": Tab to search, the Role filter (arrow keys pick a role) and a row's "Edit roles" button, each with a visible focus ring.

Validated on screen by the owner on 2026-09-19: every step passed.

## Coverage
| Criterion | Test(s) |
|---|---|
| AC1 | `RoleAdministrationTests.Seed_TheThreeSeedRoles_AreSystemRoles`, `SystemRoleSeedTests.Seed_RoleCreatedBeforeF9_IsMarkedAsSystemAndKeepsItsGrants` |
| AC2 | `RoleAdministrationTests.ListRoles_AsAdmin_ReturnsFlagPermissionsAndUserCountAndHidesDeletedRoles`, `ListPermissions_AsAdmin_ReturnsTheCatalog`; `RolesPageTests.Load_ShowsEveryRoleWithSystemBadgeCountsAndALinkToItsUsers` |
| AC3 | `RoleAdministrationTests.CreateRole_ValidNameAndKnownPermissions_SavesACustomRole`; `RolesPageTests.Add_ValidNameAndPermission_SendsThemAndShowsTheSnackbar` |
| AC4 | `RoleAdministrationTests.SaveRole_NameOutOfBounds_IsRefused` (4 cases), `SaveRole_FiftyCharacterName_IsAccepted`; `RolesPageTests.Add_NameTooShort_ShowsTheFieldErrorAndSendsNothing` |
| AC5 | `RoleAdministrationTests.SaveRole_NameTakenIgnoringCaseByActiveDeletedOrSeedRole_IsRefused`, `UpdateRole_SameNameDifferentCase_RenamesItself`; `RolesPageTests.Save_ApiRefuses_ShowsTheTranslatedErrorInTheDialog` |
| AC6 | `RoleAdministrationTests.SaveRole_UnknownPermission_IsRefusedAndNothingChanges` |
| AC7 | `RoleAdministrationTests.UpdateRole_PermissionsChanged_ReachTheHolderAfterTheCacheWindow` |
| AC8 | `RoleAdministrationTests.SystemRole_RenameOrDelete_IsRefusedButItsPermissionsChange`, `UpdateSystemRole_BlankName_IsRefusedAsInvalid`; `RolesPageTests.EditSystemRole_NameIsReadOnlyAndTheStoredNameIsSent` |
| AC9 | `RoleAdministrationTests.AdminRole_LosingRolesManage_IsRefused` |
| AC10 | `RoleAdministrationTests.DeleteRole_InUse_IsRefused_ThenWithoutHolders_IsSoftDeletedAndItsNameStaysTaken`; `RolesPageTests.Delete_Confirmed_DeletesAndShowsTheSnackbar`, `Delete_RefusedBecauseSomeoneGotTheRoleMeanwhile_ShowsTheAlert` |
| AC11 | `RoleAdministrationTests.ListUsers_SearchAndRoleFilter_ReturnNonDeletedAccountsOfAnyStatusPagedOnTheServer`, `ListUsers_SearchWithPatternCharacters_MatchesThemLiterally` (`%`, `_`, `\`); `UsersPageTests.Load_ShowsEmailNameStatusAndRoleChips`, `RoleFilterChanged_OnALaterPage_ReloadsFromTheFirstPage`, `RoleInTheAddress_IsPreselectedAndSentAsTheFilter`, `Search_WithoutMatches_SaysNoUserMatchesTheTerm` |
| AC12 | `RoleAdministrationTests.SetUserRoles_SeveralRoles_ReplacesTheSetAndPermissionsAreTheirUnion`; `UsersPageTests.EditRoles_ShowsEveryRoleWithItsPermissions_SavesTheSetAndShowsTheSnackbar` |
| AC13 | `LastManagerTests.Change_ThatLeavesNoActiveManager_IsRefusedAndNothingChanges_UntilASecondManagerExists`, `ConcurrentLastManagerTests.TwoManagersRemovingThemselvesAtOnce_ExactlyOneIsRefused`; `UsersPageTests.EditRoles_LastManager_ShowsTheErrorAndKeepsTheDialogOpen` |
| AC14 | `RoleAdministrationTests.UnknownTargets_AnswerNotFoundOrRoleUnknown` |
| AC15 | `RoleAdministrationTests.EveryEndpoint_WithoutRolesManage_IsForbidden` (7 routes); `NavigationItemsTests.All_AdministrationItems_AreRolesAndUsersBehindRolesManage`, `Visible_ItemWithPermission_IsHiddenWithoutIt` (F-6); Not Found page: validation script step 5 (screen-only, as F-6 AC6) |
| AC16 | `RolesPageTests.RowActions_SystemRoleHasNoDelete_CustomRoleInUseHasItDisabledWithTheReason`, `Load_ApiFails_ShowsTheErrorStateWithTryAgain`, `EditSystemRole_…`, `Delete_…`; `UsersPageTests.Load_ApiFails_ShowsTheErrorStateWithTryAgain`; `AppRowActionsTests.Render_DeleteDisabledReason_ShowsDeleteDisabledWithTheReasonAsTooltip`; Esc/discard and focus: validation script steps 3 and 8 |
| AC17 | `RoleResourcesTests.EveryPermissionSystemRoleAndErrorCode_HasAText` (en, pt-BR, pt-PT); `RolesPageTests.Load_InPortuguesePortugal_TranslatesSystemRolesAndKeepsCustomNames` |
| AC18 | `IdentityModuleBoundaryTests` (pre-existing, green with the new Application ports and Infrastructure adapters) |
| AC19 | `ResourceParityTests` (Web, all cases), `RoleResourcesTests` |

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: <feature/F-9>
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
