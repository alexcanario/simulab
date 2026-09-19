---
bug: B-3
feature: F-5
status: approved
board: 719
severity: high
---
# The Web never refreshes the access token nor notices a revoked session

## What happens
1. Sign in and leave the tab open for more than 15 minutes.
2. Any call the Web makes to the Api with the stored access token fails (401): the token expired and was never refreshed.
3. Revoke the session from elsewhere (sign-out from the Api, or F-7's "end every session"): the Web still shows the user signed in, with the cached permissions, until the 30-day cookie expires.

## Expected
F-5 UC5: while the refresh token is valid, the Web refreshes the access token silently. When the refresh fails (session revoked, refresh token unknown or used), the auth cookie is cleared and the visitor is anonymous on the next request.

## Cause
Found while refining F-7 (2026-09-19), confirmed in code:
- `src/Hosts/Simulab.Web/Services/Auth/AuthClient.cs:44` — `RefreshAsync` exists and has no caller anywhere in the solution.
- `src/Hosts/Simulab.Web/Program.cs:28-38` — the cookie lives 30 days (`ExpireTimeSpan = TokenLifetimes.RefreshToken`, no sliding) with no `OnValidatePrincipal` or other check, so a cookie is trusted for its full life, whatever happened to the session at the Api.
- `src/Hosts/Simulab.Web/Services/Auth/AccountEndpoints.cs:43-48` — the access token, the refresh token and the permissions are written once into the cookie at sign-in and never replaced. After 15 minutes the stored access token is expired.
- `src/Hosts/Simulab.Web/Components/Pages/Admin/Roles.razor:44` — reads the access token straight from the cookie claims, so `/admin/roles` fails with a server error after 15 minutes of session.
- Design constraint: the Web is Blazor Interactive Server. Inside an open page (circuit) there is no HTTP response, so a token refreshed there cannot be written back into the cookie; and the Api's refresh token is single use (F-5 BR5), so the cookie's copy would then be dead and the next page load would sign the user out. Refreshing only works if the tokens live on the server, not in the cookie.

Not a business rule. Other places that read the tokens from the cookie claims, and must move to the fix's single token accessor: `Roles.razor:44`, `AccountEndpoints.cs:57` (sign-out). No other duplicate found.

## Fix
- BR1 The auth cookie carries only the user's identity (`sub`, email, display name) and the session id (`session_jti`). The access token, the refresh token, the access token's expiry and the effective permissions live in Redis, one entry per session (`web:session:{sessionJti}`), expiring with the refresh token. The Web reaches Redis through `Aspire.StackExchange.Redis` (`builder.AddRedisClient`, project rule), with a `WithReference(redis)` in the app host.
- BR2 One service, the web session store's token accessor, is the only way Web code gets an access token. When the stored access token expires in less than a minute, it refreshes through `AuthClient.RefreshAsync`, stores the new pair and re-reads the permissions (`GET /api/v1/identity/session`, F-6 BR5). Two refreshes of the same session never run at once: the second waits and reuses the first one's result (the refresh token is single use, F-5 BR5).
- BR3 A refresh that fails (`identity.refresh_token_invalid`), or a session check answered 401 (`identity.token_revoked`, or no stored entry), ends the session in the Web: the Redis entry is removed and the visitor is anonymous.
- BR4 On every HTTP request that carries the cookie (page load, forced navigation), the cookie's `OnValidatePrincipal` checks the session through BR2 and BR3; an ended session rejects the principal and clears the cookie. The permissions from the store are added to the principal there, so menus and page gates follow the latest permissions from the next page load on.
- BR5 With a page open, the circuit revalidates the session every minute (a revalidating authentication state provider using BR2 and BR3). An ended session sends the browser to `/sign-in` with the info alert `SignIn.SessionEnded`, through the plain sign-out endpoint, so the cookie is cleared too.
- BR6 `Roles.razor` and the sign-out endpoint get their tokens from BR2, never from cookie claims. No component reads a token claim any more (architecture test).
- BR7 A cookie issued before this fix has no stored session; it is treated as an ended session (BR3). Nothing is deployed, so nobody but a developer signs in once more.

### UI text
| Key | en | pt-BR | pt-PT |
|---|---|---|---|
| `SignIn.SessionEnded` | Your session ended. Sign in again. | Sua sessão terminou. Entre novamente. | A sua sessão terminou. Inicie sessão novamente. |

## Decisions
- 2026-09-19 — Tokens live server side in Redis, the cookie keeps only identity and the session id — owner, q1. Inside an open Blazor Server page there is no HTTP response to rewrite the cookie, and the single-use refresh token would leave the cookie's copy dead. In memory was rejected because a restart of the Web would sign everyone out; keeping the cookie was rejected because pages open for more than 15 minutes break.
- 2026-09-19 — Another device notices an ended session on its next page load, or within 1 minute with a page open — owner, q2. One light Api call per active user per minute; the 15-minute alternative made F-7's "other devices are signed out" invisible for too long.
- 2026-09-19 — An ended session sends the page to `/sign-in` with "Your session ended. Sign in again." — owner, q3.
- 2026-09-19 — Permissions are re-read on every refresh and applied at the next page load — owner, q4; this is what F-6 BR5 promised.
- 2026-09-19 — The single-flight refresh (BR2) is an in-process lock per session: v1 runs one Web instance. A second instance needs a Redis lock; recorded here, not built.
- 2026-09-19 — No new package: `Aspire.StackExchange.Redis` 13.5.4 (MIT) is already approved and pinned (F-5); the Web project only gains the reference. Tests use the existing `RedisServer` (Testcontainers.Redis) from `Simulab.Testing`.

## Regression test
Each one fails on `main` before the fix and passes after it.
- `WebSessionTokenAccessorTests.GetAccessToken_ExpiredAccessToken_RefreshesAndStoresNewPairAndPermissions` — `Simulab.Web.Tests` (BR2)
- `WebSessionTokenAccessorTests.GetAccessToken_ConcurrentCallsOnExpiredToken_RefreshesOnce` — `Simulab.Web.Tests` (BR2)
- `WebSessionTokenAccessorTests.GetAccessToken_RefreshRejected_EndsSessionAndRemovesEntry` — `Simulab.Web.Tests` (BR3)
- `AuthCookieValidationTests.ValidatePrincipal_RevokedSession_RejectsPrincipal` — `Simulab.Web.Tests`, against the cookie options `Program` really configures (BR4)
- `AuthCookieValidationTests.ValidatePrincipal_PermissionsChanged_PrincipalCarriesNewPermissions` — `Simulab.Web.Tests` (BR4, F-6 BR5)
- `SessionRevalidationTests.Revalidate_RevokedSession_ReturnsFalse` — `Simulab.Web.Tests` (BR5)
- `RedisWebSessionStoreTests.Save_ThenGet_RoundTripsAndExpiresWithRefreshLifetime` — `Simulab.Web.Tests`, Redis container (BR1)
- `WebTokenClaimsTests.NoComponent_ReadsTokenClaims` — `Simulab.ArchitectureTests` (BR6)
- `ResourceParityTests` stays green with the new key (the three languages).

## Open questions
- (none)

## Validation script

## Delivery
