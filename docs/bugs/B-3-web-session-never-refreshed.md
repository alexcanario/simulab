---
bug: B-3
feature: F-5
status: validating
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

- 2026-09-19 — Build: the store key is a Web-owned random id, not the Api's `session_jti`: every refresh issues a new `session_jti` (`TokenEndpoints.IssueTokensAsync`), and inside an open page the cookie that holds the key cannot be rewritten. The entry keeps the current `session_jti` instead.
- 2026-09-19 — Build: only an explicit refusal ends a session (an OAuth error on refresh, a 401 on the session check). An Api that does not answer, or answers 5xx, keeps the session and its current token, so an Api outage never signs everyone out.
- 2026-09-19 — Build: a page load (`Sec-Fetch-Mode: navigate` or an HTML `Accept`) always asks the Api; any other request with the cookie (assets, the circuit's own) asks at most once a minute. Without this, each page would cost one Api call per asset.
- 2026-09-19 — Build: permissions are added to the principal per request (`ReplacePrincipal`), never written to the cookie; an open page keeps the permissions it started with until the next page load.
- 2026-09-19 — Build: the planned architecture test `WebTokenClaimsTests.NoComponent_ReadsTokenClaims` became `SignInCompleteTests.SignInComplete_StoresTokensServerSide_CookieCarriesOnlyIdentityAndSession`: the token claim types were removed from `WebAuthClaims`, so no code can read them (the build proves it), and the test decrypts the real cookie to prove it carries none.
- 2026-09-19 — Build: `docs/infra.md` no longer asks for a new sign-in after granting a role; the Web picks it up on the next page load.

## Regression test
`AuthCookieValidationTests.ValidatePrincipal_RevokedSession_RejectsPrincipal` was seen failing on the unfixed code (`Expected context.Principal to be <null>, but found ...`, commit `7fbecb9`) and passes after the fix.

| Rule | Tests (`Simulab.Web.Tests` unless noted) |
|---|---|
| BR1 tokens server side, cookie with identity and id only | `SignInCompleteTests.SignInComplete_StoresTokensServerSide_CookieCarriesOnlyIdentityAndSession`; `RedisWebSessionStoreTests.Save_ThenGet_RoundTripsAndExpiresWithRefreshLifetime`, `Remove_ThenGet_ReturnsNull` (Redis container) |
| BR2 one accessor, refresh, permissions, single flight | `WebSessionTokenAccessorTests.GetAccessToken_ValidAccessToken_ReturnsItWithoutRefreshing`, `GetAccessToken_ExpiredAccessToken_RefreshesAndStoresNewPairAndPermissions`, `GetAccessToken_ConcurrentCallsOnExpiredToken_RefreshesOnce` |
| BR3 refusal ends the session, silence does not | `WebSessionTokenAccessorTests.GetAccessToken_RefreshRejected_EndsSessionAndRemovesEntry`, `GetAccessToken_ApiUnreachable_KeepsSession`, `GetAccessToken_NoStoredSession_ReturnsNull`, `Check_ApiSaysEnded_RemovesEntry`, `Check_ApiFails_KeepsSession` |
| BR4 cookie checked per request, permissions applied | `AuthCookieValidationTests.ValidatePrincipal_RevokedSession_RejectsPrincipal`, `ValidatePrincipal_PermissionsChanged_PrincipalCarriesNewPermissions`, `ValidatePrincipal_AssetRequestRecentlyChecked_DoesNotCallApi`; `WebSessionTokenAccessorTests.Check_RecentlyCheckedWithoutForce_DoesNotCallApi`, `Check_Forced_AsksApiAndStoresPermissions` |
| BR5 circuit revalidation and the sign-in alert | `SessionRevalidationTests.Revalidate_RevokedSession_ReturnsFalse`, `Revalidate_LiveSession_ReturnsTrueAndAsksTheApiEveryTime`, `Revalidate_Anonymous_ReturnsTrue`; `SignInSessionEndedTests.SessionEndedQuery_ShowsTheSessionEndedAlert`, `NoQuery_ShowsNoAlert`; the redirect from an open page: validation script step 6 |
| BR6 no code reads tokens from the cookie | `SignInCompleteTests` (above); the token claim types no longer exist; `/admin/roles` after 15 minutes: validation script step 4 |
| BR7 a cookie from before the fix | `AuthCookieValidationTests.ValidatePrincipal_CookieFromBeforeTheFix_RejectsPrincipal` |
| UI text in three languages | `ResourceParityTests` |

## Open questions
- (none)

## Validation script
A container runtime must be running. Close any app host or IDE running from this checkout first.
1. Start `dotnet run --project src/Hosts/Simulab.AppHost` and open the dashboard URL printed on start. Wait for `redis`, `api` and `web` to reach Running.
2. Keyboard only: open the `web` URL, go to `/sign-in`, Tab to the email, type your account from F-4, Tab to the password, type it, press Enter. The home page opens with your account menu.
3. Grant yourself Admin with the SQL in `docs/infra.md` ("Assigning Curator or Admin") **without signing out**. Wait one minute and reload the page: "Roles" appears in the menu. (Before B-3 it needed a new sign-in.)
4. Leave the app open for at least 16 minutes (or come back later), then open "Roles": the three roles load. (Before B-3 the page failed once the 15-minute access token expired.)
5. In the dashboard, restart the `web` resource, then reload the page: you are still signed in.
6. Open a second tab of the same browser on the `web` URL and sign out there. Go back to the first tab and do nothing: within one minute it moves to sign-in with "Your session ended. Sign in again."
7. On that page switch the language with the globe to English and to Português (Brasil): the alert text changes. Switch the theme (sun/moon): the alert stays readable.
8. Sign in again: it works as usual.

## Delivery
