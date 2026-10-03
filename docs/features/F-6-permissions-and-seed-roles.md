---
feature: F-6
epic: Foundation and identity
status: done
board: 710
version: 2
---
# Permissions and seed roles

Technical terms: [glossary](../glossary.md)

## Summary
Permission catalog, seed roles Student, Curator and Admin, and permission checks on endpoints, pages and menu items (never by role name). Imported from Simulae RBAC.

## Goal
Give Simulab real access control — three seed roles and a permission-check mechanism every later screen and endpoint relies on, instead of every signed-in user seeing everything.

## What exists
- F-4/F-5: `User : IdentityUser<Guid>`, `AccountStatus`, sign-in/out, Redis sessions. `AddIdentityCore<User>().AddUserStore<UserOnlyStore<User, IdentityModuleDbContext, Guid>>()` — **no role store**: `AspNetRoles`/`AspNetUserRoles` do not exist yet, `RoleManager` is not registered.
- F-2: the nav registry already carries `NavigationItem.RequiredPermission` (`string?`), and `NavigationItems.Visible` hides any item that has one (`item.RequiredPermission is null`) — a placeholder until this feature.
- F-5: `GET /api/v1/identity/session` returns `SessionInfoResponse(Subject, Email, SessionJti)`; the Web's `/account/sign-in-complete` writes it into its own auth cookie.
- Simulae (read-only reference): richer RBAC than v1 needs — 8 seed roles (not the 3 ADR-0001 asks for) and a full role/permission admin back office (that is Simulab's F-9, not this feature). Its endpoint authorization is a dynamic `"Permission:<Name>"` policy resolved by a custom `IAuthorizationPolicyProvider` + `IAuthorizationHandler`, checking the database with a 10 s `IMemoryCache`, **never a token claim** (so a revoked permission takes effect immediately). Its Blazor pages and menu, however, check **role names** (`AuthorizeView Roles="..."`) — this violates the "never role names" rule ADR-0001 sets for Simulab and is not imported.

## Users and use cases
- UC1 A visitor signs up (F-4) and the new account gets the Student role automatically, with no extra step.
- UC2 An Admin (assigned outside the UI in v1) sees "Roles" in the app bar's Administration section; a Student or Curator does not.
- UC3 An Admin opens `/admin/roles` and sees the three seed role names (a minimal list, not yet the full back office F-9 replaces it with); a Student who opens the same address directly gets the ordinary Not Found page.
- UC4 A caller without `identity.roles.manage` gets a 403 from an endpoint that requires it, even with an otherwise valid, unexpired access token; a caller with it succeeds.
- UC5 A developer runs the app host and assigns Curator or Admin to a test account by hand (documented step), since F-9 does not exist yet.

## Business rules
- BR1 Three seed roles exist, global (no per-tenant custom roles in v1): Student, Curator, Admin.
- BR2 Every successful sign-up (F-4's `RegisterUserHandler`) assigns the new `User` the Student role in the same step that creates the account.
- BR3 API authorization is a dynamic policy `Permission:<name>`, resolved by a custom `IAuthorizationPolicyProvider`. A matching `IAuthorizationHandler` checks the caller's roles' permissions against the database (`RolePermission`), cached per user id in `IMemoryCache` for 10 seconds. The check never reads a role or permission from the access token: a permission revoked in the database takes effect within the cache window, not at the token's own 15-minute expiry.
- BR4 A failed permission check answers 403 with the stable code `identity.forbidden`, in an RFC 9457 body (api-contracts rule), through a custom `IAuthorizationMiddlewareResultHandler`.
- BR5 The Web never queries permissions itself: `GET /api/v1/identity/session` (F-5) gains `Permissions: string[]`, the caller's effective permission names. The Web reads it at sign-in and on every silent token refresh, and caches it in its own auth cookie for UI comfort only — hiding a menu item or a page is never the security boundary, the Api call above still is.
- BR6 `NavigationItems.Visible` shows an item with a `RequiredPermission` only when the current visitor's cached permissions contain it (replaces the "always hidden" placeholder from F-2).
- BR7 A routed page can require a permission the same way (`Routes.razor` moves from `RouteView` to `AuthorizeRouteView`). Opening such a route without the permission renders the app's ordinary Not Found page — never a distinct "forbidden" page, so the route's existence is not revealed to someone without access.
- BR8 One permission is seeded now: `identity.roles.manage`, assigned to Admin only, reserved for F-9. It gates the new placeholder nav item and page (BR9).
- BR9 A minimal placeholder page, `/admin/roles` (lists the seed role names, read from `GET /api/v1/identity/roles`), and its nav item "Roles" (Administration section) exist only to prove BR3–BR7 on screen; F-9 replaces the page's content and keeps the nav item and the gate.
- BR10 In v1 there is no screen to assign Curator or Admin to a user; it is done directly against the database, documented as a temporary step in `docs/infra.md` until F-9 ships.

## Screens and API
- Route `/admin/roles` — placeholder page listing the seed role names, Administration section; requires `identity.roles.manage`.
- Nav item "Roles" — Administration section, same permission; hidden for anyone without it (BR6).
- `GET /api/v1/identity/roles` — new: `RoleNamesResponse { Names: string[] }`, the seed role names; requires `identity.roles.manage` (BR3, BR4, BR8, BR9). What `/admin/roles` calls; also the endpoint AC3/AC4 exercise.
- `GET /api/v1/identity/session` — extended: `SessionInfoResponse` gains `Permissions: string[]` (the caller's effective permission names). Unchanged status codes (F-5).
- Error codes: `identity.forbidden` (403, BR4).

## Acceptance criteria
- AC1 Given a successful sign-up, when the account is created, then it has the Student role. (BR2)
- AC2 Given the module's migrations run, then Student, Curator and Admin exist as global roles, `identity.roles.manage` exists, and only Admin has it. (BR1, BR8)
- AC3 Given an authenticated caller without `identity.roles.manage`, when it calls an endpoint that requires it, then the answer is 403 `identity.forbidden`; given a caller with it, the call succeeds. (BR3, BR4)
- AC4 Given a session whose access token is still valid, when the permission behind it is revoked in the database, then the very next call using that same token is refused within the cache window — it does not wait for the token to expire. (BR3)
- AC5 Given an anonymous or Student visitor, then the app bar does not show "Roles"; given an Admin, it does, linking to `/admin/roles`. (BR5, BR6, BR8)
- AC6 Given a Student who opens `/admin/roles` directly, then the page shown is the ordinary Not Found page, not a distinct "forbidden" page. (BR7)
- AC7 Architecture: `Simulab.Identity.Domain` and `Simulab.Identity.Application` reference neither EF Core, ASP.NET nor the permission cache; the `IAuthorizationHandler`, the policy provider and the EF stores stay in `Simulab.Identity.Infrastructure`/`Simulab.Identity.Api`. (profile)
- AC8 All new texts — the placeholder page, the "Roles" nav item, `identity.forbidden` — exist in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-09-18 — Seed roles: exactly Student, Curator, Admin, global — no `TenantId` column on `Role` in v1 (institutions would add it) — owner, question 1.
- 2026-09-18 — Seed permission: only `identity.roles.manage`, Admin-only, reserved for F-9 — owner, question 2.
- 2026-09-18 — Every sign-up gets the Student role at creation; Admin/Curator have no UI path in v1 and are assigned directly in the database until F-9 ships — owner, question 3.
- 2026-09-18 — Api enforcement mirrors Simulae's proven pattern: dynamic `Permission:<name>` policy, `IAuthorizationHandler` querying `RolePermission` per request with a 10 s `IMemoryCache`; never a token claim — owner, question 4.
- 2026-09-18 — Web menu/page visibility reads `GET /api/v1/identity/session`'s new `Permissions`, cached in the Web's own cookie, UI comfort only — owner, question 5.
- 2026-09-18 — `Routes.razor` moves to `AuthorizeRouteView`; an unauthorized route renders the ordinary Not Found page — owner, question 6.
- 2026-09-18 — `/admin/roles` placeholder + "Roles" nav item prove the mechanism on screen; F-9 replaces the page's content — owner, question 7.
- 2026-09-18 — `Role : IdentityRole<Guid>` (same reuse pattern as `User : IdentityUser<Guid>`); `Permission` (Name as key, Description) and `RolePermission` (composite key) are new entities in the Identity module — owner, question 8.
- 2026-09-18 — Switch from `UserOnlyStore` to a role-capable store (`AddRoles<Role>()`, `RoleManager<Role>`); one new migration. No production users exist yet, so no backfill — owner, question 9.
- 2026-09-18 — No new packages: `IMemoryCache` ships with `Microsoft.AspNetCore.App`, already referenced by the Api host — owner, question 10.
- 2026-09-18 — Build: a failed permission check gets a stable `identity.forbidden` code in an RFC 9457 body via a custom `IAuthorizationMiddlewareResultHandler` (same spirit as F-5's `RevocationCheckMiddleware` writing its own body), so the Web's one code-to-text helper still covers it.

## Out of scope
- Role and permission CRUD admin screens, and assigning a role to another user via a screen (F-9).
- Tenant-scoped custom roles (Institutions epic).
- Google sign-in and TOTP (F-11).
- Deleting or editing the three seed roles.

## Open questions
- (none)

## Change notes
### v2 — 2026-09-18
- What: `/admin/roles` lists the three seed role names (via a new `GET /api/v1/identity/roles`, gated by `identity.roles.manage`) instead of a static "coming soon" text.
- Why: the endpoint doubles as the concrete example AC3/AC4 exercise for permission enforcement (403 without the permission, 200 with it), and a real list is no more work than a static placeholder while proving BR3-BR7 more directly on screen.
- Affected: BR9, UC3, Screens and API section; AC3-AC6 now point at this endpoint and page. Other criteria unchanged.
- Re-approved: 2026-09-18 (owner, in the build session)

## Validation script
1. Run the app host (`dotnet run --project src/Hosts/Simulab.AppHost`), open the Web app, sign up a new account and verify its email → the account can sign in.
2. Open the app bar menu → "Roles" is not shown (Student has no `identity.roles.manage`).
3. Navigate directly to `/admin/roles` → the ordinary "Page not found" page shows, not a distinct forbidden page.
4. In the local database, insert a row into `identity.user_roles` for this user and the `Admin` role id (`docs/infra.md`, BR10), then sign out and sign in again.
5. Open the app bar menu → "Roles" now shows; click it → `/admin/roles` lists Student, Curator, Admin.
6. Switch the language to pt-BR (language switch in the app bar) → the page title and nav item read "Papéis"; switch to pt-PT → "Perfis".
7. Tab through the app bar and the nav menu with the keyboard only → the "Roles" link is reachable and shows a focus ring, same as any other nav item.
8. Check light and dark mode on `/admin/roles` and the "not found" page → both are legible in both modes.

## Coverage
| Criterion | Test(s) |
|---|---|
| AC1 | `PermissionEnforcementTests.SignUp_NewAccount_IsAssignedTheStudentRoleAutomatically` |
| AC2 | `PermissionEnforcementTests.Migration_SeedsTheThreeRolesAndOnlyAdminHasRolesManage` |
| AC3 | `PermissionEnforcementTests.GetRoles_SignedInAsStudent_IsForbidden`, `PermissionEnforcementTests.GetRoles_SignedInAsAdmin_ReturnsTheSeedRoleNames` |
| AC4 | `PermissionEnforcementTests.GetRoles_PermissionRevokedAfterTokenIssued_TakesEffectOnceTheCacheExpires` |
| AC5 | `NavigationItemsTests.Visible_ItemWithPermission_IsHiddenWithoutIt`, `NavigationItemsTests.Visible_ItemWithPermission_IsShownWhenGranted`, `NavMenuTests.Render_EmptySectionAndPermissionItem_AreNotRendered`, `NavMenuTests.Render_PermissionItem_IsShownOnceTheVisitorHasTheClaim`; validation script step 2, 5 |
| AC6 | Validation script step 3 (screen-only: `AuthorizeRouteView` + `NotFoundContent`, no bUnit router harness in this codebase yet) |
| AC7 | `IdentityModuleBoundaryTests.InnerLayers_DoNotDependOnEfCoreOrAspNetCore`, `IdentityModuleBoundaryTests.Application_ReferencesDomainAndContractsOnly` (pre-existing, still green with the new Domain/Infrastructure/Api types) |
| AC8 | `ResourceParityTests` (Web, all cases); `EmailResourceParityTests` (unaffected, no new email text) |

## Delivery
- Branch: `feature/F-6` (merged and deleted; includes B-2, a bug found and fixed while validating this
  item's screen — see `docs/bugs/B-2-user-menu-does-not-open.md`)
- Merge: `a0a6f02` (`--no-ff` into `main`, AB#710)
- Tests: 277, 27 s (full suite, architecture tests included)
- Manual pages: `docs/manual/{en,pt-BR,pt-PT}/roles.md`, linked from each locale's `index.md`
