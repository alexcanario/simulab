---
bug: B-4
feature: F-4
status: building
board: 720
severity: high
---
# Per-client limits are shared by the whole site

## What happens
1. Every per-client limit (F-4 registration and resend, F-7 reset request, token check and reset) keys on `Connection.RemoteIpAddress` in `src/Modules/Identity/Simulab.Identity.Api/IdentityEndpoints.cs` (`ClientKey`).
2. The Web calls the Api from the server (B-3), so the Api only ever sees the Web server's address.
3. Result: the whole site shares one bucket, for example 5 password reset links an hour for everyone, and anyone can spend it.

## Expected
Each visitor has their own bucket: the Web sends the visitor's address with each call and the Api uses it, trusting it only from the Web.

## Cause
Confirmed in code on 2026-09-19 (by reading; not reproduced through the app host yet):
- `src/Modules/Identity/Simulab.Identity.Api/IdentityEndpoints.cs:348-349` — `ClientKey` builds every per-client key from `context.Connection.RemoteIpAddress`. The five limited calls (`registrations` :162, `email-verifications/resend` :216, `password-reset-requests` :235, `password-reset-token-checks` :254, `password-resets` :276) all come from the Web's `IdentityApiClient`, which runs on the Web server inside the visitor's Blazor circuit. The Api therefore sees one address, the Web server's, for every visitor: each limit is one bucket for the whole site (for example 5 reset links an hour in total, `IdentityRateLimits.PasswordResetRequestsPerHour`).
- The Api has no forwarded-headers handling, and the Web sends nothing that says who the visitor is.

Same cause, another place (not a rate limit, same wrong address):
- `IdentityEndpoints.cs:177` — sign-up passes `context.Connection.RemoteIpAddress` into `RegisterUserCommand.IpAddress`, which `RegisterUserHandler.cs:102` stores in `ConsentRecord.IpAddress`. Every consent record (the auditable proof of acceptance, ADR-0001 #17) holds the Web server's address, not the visitor's.

No other reimplementation of the per-client limit found: sign-in (`/connect/token`) has only the per-account lockout, and the Web has no limits of its own.

## Fix
- BR1 The Web captures the visitor's address when a page is first requested (the static render of `App.razor`, where the `HttpContext` exists) and hands it to the interactive circuit as a root parameter, the way `ShellPreferences` travel today. Calls made from the circuit carry it.
- BR2 Every call of the Web's `IdentityApiClient` sends the visitor's address in `X-Simulab-Client-Address`, together with the Web's client secret (the OpenIddict secret it already shares with the Api) in `X-Simulab-Client-Secret`.
- BR3 The Api uses `X-Simulab-Client-Address` as the client's address only when `X-Simulab-Client-Secret` matches its configured client secret (constant-time comparison) and the value is a valid IP address. Otherwise, header or not, it uses the connection's address, as today.
- BR4 One helper gives the client's address to every use: the five per-client limits (register, resend, reset request, token check, reset) and the consent record's `IpAddress` at sign-up.
- BR5 The Web reads `X-Forwarded-For` only from the proxies listed in its configuration (`ForwardedHeaders:KnownProxies`, `ForwardedHeaders:KnownNetworks`). Both are empty in dev, so the connection's address is used; `docs/infra.md` says to fill them when the cloud environment is created.
- BR6 The limits, their windows and their error codes do not change (F-4 BR12, F-7 BR3).

## Acceptance criteria
- AC1 Given two visitors with different addresses, when one of them uses up the password reset request limit, then the other can still ask for a link (each has its own bucket), through the Api with the trusted headers. (BR2, BR3, BR4)
- AC2 Given a request with `X-Simulab-Client-Address` and a missing or wrong `X-Simulab-Client-Secret`, or with an address that is not an IP, then the Api counts it under the connection's address, not the header's. (BR3)
- AC3 Given a sign-up that carries the trusted headers, then its consent record stores the visitor's address from the header. (BR4)
- AC4 Given a page opened by a visitor, when the page calls the Api through `IdentityApiClient`, then the request carries that visitor's address and the Web's secret. (BR1, BR2)
- AC5 Given the Web with `ForwardedHeaders:KnownProxies` listing a proxy, when a request from that proxy carries `X-Forwarded-For`, then the visitor's address is the forwarded one; with the list empty, the header is ignored. (BR5)
- AC6 Given each of the five limited endpoints, then its limit and error code are unchanged (existing `RateLimitEndpointTests` stay green). (BR6)
- AC7 No new user-facing text (nothing to translate); the missing-key test stays green.

## Decisions
- 2026-09-19 — The Api trusts the address header only with the Web's secret — owner; it keeps working when the Api becomes public for a mobile client (ADR-0001 #32), unlike trusting the network.
- 2026-09-19 — The consent record's address is fixed in B-4 too — owner; same cause and data path, and it is the auditable proof of acceptance.
- 2026-09-19 — The Web's forwarded headers are configurable and empty in dev — owner; the cloud proxy list is filled when the environment exists, and `docs/infra.md` records it so it is not forgotten.
- 2026-09-19 — Existing consent records keep the Web server's address — owner; dev only, and a proof of acceptance is never rewritten.
- 2026-09-19 — The secret reused is the OpenIddict client secret (`Authentication:OpenIddict:ClientSecret`), already configured on both hosts — no new secret to provision; it only travels between the two hosts, over the service-discovery connection.
- 2026-09-19 — Out of scope (default, not asked): a per-client limit on sign-in (it has the per-account lockout), and a shared (Redis) counter for several Api instances (`ClientRateLimiter` remarks: one instance in v1).
- 2026-09-19 — No new packages, for code or tests (`ForwardedHeaders` is part of ASP.NET Core).

## Out of scope
- A per-client limit on sign-in (`/connect/token`).
- A shared counter across several Api instances.
- Rewriting the address of existing consent records.

## Regression test

## Open questions
- (none)

## Change notes

## Validation script

## Delivery
