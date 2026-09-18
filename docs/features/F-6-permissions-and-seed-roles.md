---
feature: F-6
epic: Foundation and identity
status: approved
board: 710
version: 1
---
# Permissions and seed roles

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
- UC3 An Admin opens `/admin/roles` and sees a "coming soon" placeholder that F-9 replaces later; a Student who opens the same address directly gets the ordinary Not Found page.
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
- BR9 A minimal placeholder page, `/admin/roles` ("Coming soon", same style as the Home page), and its nav item "Roles" (Administration section) exist only to prove BR3–BR7 on screen; F-9 replaces the page's content and keeps the nav item and the gate.
- BR10 In v1 there is no screen to assign Curator or Admin to a user; it is done directly against the database, documented as a temporary step in `docs/infra.md` until F-9 ships.

## Screens and API
- Route `/admin/roles` — placeholder page ("Coming soon"), Administration section; requires `identity.roles.manage`.
- Nav item "Roles" — Administration section, same permission; hidden for anyone without it (BR6).
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
<!-- Added by /agile:change during build. Increase `version` in the header. -->
<!--
### v2 — YYYY-MM-DD
- What: <change>
- Why: <reason>
- Affected: <BR/AC ids>; other criteria unchanged.
- Re-approved: <YYYY-MM-DD>
-->

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->
1. <Step> → <expected result>

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: <feature/F-<number>>
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
