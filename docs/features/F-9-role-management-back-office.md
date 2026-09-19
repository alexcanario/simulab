---
feature: F-9
epic: Foundation and identity
status: refining
board: 713
version: 1
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
- UC5 An Admin opens `/admin/users`, searches a user by email or name and sees their account status and roles.
- UC6 An Admin edits a user's roles (several roles allowed) and the user's API access reflects it within 10 s; their menu reflects it at their next token refresh or sign-in.
- UC7 A Student or Curator never sees the Administration items and gets the ordinary Not Found page on `/admin/roles` and `/admin/users`.

## Business rules
- BR1 Roles are global (no tenant) as in F-6. A role is a system role (`IsSystem`) or a custom role. Student, Curator and Admin are system roles, marked by the migration.
- BR2 A system role cannot be renamed or deleted; its permissions can be changed (subject to BR8).
- BR3 A role name is trimmed, 2 to 50 characters, and unique case-insensitively among all roles, deleted ones included (the unique index covers deleted rows, so a deleted role's name stays taken).
- BR4 A role's permissions are a set of names from the catalog; an unknown name is refused. Saving replaces the whole set.
- BR5 A custom role is deleted only while no user holds it; deleting is a soft delete (rule `ui-project`). A deleted role disappears from both screens and grants nothing.
- BR6 A user may hold several roles; their effective permissions are the union. Saving a user's roles replaces the whole set with the chosen ones; an empty set is allowed.
- BR7 Accounts of any status (Pending or Active) are listed and can receive roles. Deleted accounts are not listed.
- BR8 Never lock everyone out: (a) the Admin role always keeps `identity.roles.manage`; (b) any change — a user's roles or a role's permissions — that would leave no Active, non-deleted user holding `identity.roles.manage` is refused. The check and the write run in one transaction, so two Admins acting at once cannot both remove the last manager.
- BR9 Propagation stays as in F-6: the API reflects a change within the 10 s cache; the affected user's menu reflects it at their next token refresh (at most 15 min) or sign-in. No active invalidation.
- BR10 Every endpoint and page of this feature requires `identity.roles.manage` (F-6 policy, 403 `identity.forbidden`); the pages and nav items are hidden without it (F-6, BR6–BR7).
- BR11 The first Admin of an installation is still assigned in the database (`docs/infra.md`, now reworded as "first Admin only"); every later assignment goes through the screen.
- BR12 Permission names and descriptions shown on screen come from resource files in the three languages (key = the permission name), grouped by module (the name's prefix before the first dot). The technical name is shown below, small. Seed role names are shown translated; custom role names are shown as stored.

## Screens and API
Detailed screens and mockup: `/agile:screen F-9` (to be added before approval).
- `/admin/roles` — replaces the F-6 placeholder. Kit table: name, system badge, permission count, user count; row actions Edit, Delete (Delete hidden for system roles); primary action Add. Add/Edit in a dialog: name field (read-only for a system role) and permission checkboxes grouped by module. Delete through the kit confirmation dialog naming the role.
- `/admin/users` — new. Kit table, server-side paging, search by email or name: email, name, account status, roles (chips); row action "Edit roles" opens a dialog with one checkbox per role.
- Nav: "Roles" stays; new "Users" in the Administration section, same permission.
- `GET /api/v1/identity/permissions` — the catalog: `PermissionResponse[] { Name }`.
- `GET /api/v1/identity/roles` — changed: `RoleResponse[] { Id, Name, IsSystem, Permissions: string[], UserCount }` (replaces `RoleNamesResponse`; its only caller is the Web).
- `POST /api/v1/identity/roles` — `SaveRoleRequest { Name, Permissions: string[] }` → 201 `RoleResponse`.
- `PUT /api/v1/identity/roles/{id}` — `SaveRoleRequest` → 200 `RoleResponse`.
- `DELETE /api/v1/identity/roles/{id}` → 204.
- `GET /api/v1/identity/users?page=&pageSize=&search=&sortBy=&descending=` → `UserPageResponse { Items: UserSummaryResponse[] { Id, Email, FullName, Status, Roles: { Id, Name, IsSystem }[] }, TotalCount }`.
- `PUT /api/v1/identity/users/{id}/roles` — `SetUserRolesRequest { RoleIds: Guid[] }` → 200 `UserSummaryResponse`.
- Error codes:
  - `role.name_invalid` (400) — blank, shorter than 2 or longer than 50 (BR3).
  - `role.name_taken` (409) (BR3).
  - `role.permission_unknown` (400) (BR4).
  - `role.not_found` (404).
  - `role.system_role_protected` (409) — rename or delete of a system role (BR2).
  - `role.admin_permission_required` (409) — removing `identity.roles.manage` from Admin (BR8a).
  - `role.in_use` (409) — delete while users hold it (BR5).
  - `role_assignment.role_unknown` (400) — a role id that does not exist or is deleted (BR6).
  - `role_assignment.last_manager` (409) — the change would leave no active manager (BR8b); also returned by `PUT /roles/{id}` when a custom role's permission change causes it.
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
- AC8 Given a system role, when it is renamed or deleted, then the answer is 409 `role.system_role_protected`; when only its permissions change, the change is saved. (BR2)
- AC9 Given the Admin role, when a save would remove `identity.roles.manage` from it, then the answer is 409 `role.admin_permission_required`. (BR8a)
- AC10 Given a custom role held by at least one user, when it is deleted, then the answer is 409 `role.in_use`; given no holder, then 204, it leaves the list and its name stays taken. (UC4, BR3, BR5)
- AC11 Given users with Pending and Active accounts and one deleted account, when an Admin lists users with a search term, then the page holds the matching non-deleted accounts (email or name, case-insensitive), with status, roles and the total count, paged and sorted on the server. (UC5, BR7)
- AC12 Given a user, when an Admin sets their roles to Curator and a custom role, then the user holds exactly those two and their effective permissions are the union; an empty set is also accepted. (UC6, BR6)
- AC13 Given exactly one Active user holding `identity.roles.manage`, when a change would take it from them (their roles, or a custom role's permissions), then the answer is 409 `role_assignment.last_manager` and nothing changes; given a second Active manager, the same change succeeds. (BR8b)
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
- 2026-09-19 — No audit history of role changes in F-9; roles only get CreatedBy/UpdatedBy — owner, question 11 — captured as an idea.
- 2026-09-19 — Two pages with dialogs (roles, users) — owner, question 12 — rule "simple entity = dialog".
- 2026-09-19 — Permissions shown by translated name and description, grouped by module, technical name below — owner, question 13 — no hardcoded UI text.
- 2026-09-19 — Mockup through `/agile:screen F-9` before approval — owner, question 14.
- 2026-09-19 — No new packages, for code or tests — owner, question 15.
- 2026-09-19 — Claude: `Role` gains `IsSystem`, the audit fields and the soft-delete fields (interfaces `IAuditableEntity`, `ISoftDeletableEntity`, same way as `User`); one migration marks the three seed roles as system roles. The unique index on `normalized_name` stays unfiltered so a deleted name stays taken.
- 2026-09-19 — Claude: `GET /api/v1/identity/roles` changes shape (`RoleResponse[]`) instead of adding a second route; its only caller is the Web, deployed together.
- 2026-09-19 — Claude: saving a role's permissions or a user's roles replaces the whole set (PUT), which matches the checkbox dialogs and keeps one endpoint per dialog.
- 2026-09-19 — Claude: the rules live in the Application layer (`SaveRoleHandler`, `DeleteRoleHandler`, `SetUserRolesHandler`), the BR8b check in one serializable transaction with the write; concurrent edits of the same role are last-write-wins (two Admins today, no conflict screen).
- 2026-09-19 — Claude: user search is a case-insensitive `ILIKE` on email and full name; default sort by email; page sizes from the kit.

## Out of scope
- History of role and permission changes (audit log) — becomes an idea.
- Tenant-scoped roles (Institutions epic).
- Blocking, suspending, editing or deleting user accounts (F-10 covers erasure), ending a user's sessions.
- Permissions granted directly to a user (only through roles).
- Immediate menu refresh for the affected user.
- A second permission for assignment and anti-escalation rules (Simulae's `Role.Assign`).

## Open questions
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->
1. <Step> → <expected result>

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: <feature/F-9>
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
