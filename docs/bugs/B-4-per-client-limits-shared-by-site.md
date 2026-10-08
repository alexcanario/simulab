---
bug: B-4
feature: F-4
status: done
board: 720
severity: high
---
# Per-client limits are shared by the whole site

Technical terms: [glossary](../glossary.md)

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
- 2026-09-19 (build) — The address travels from `App` to `Routes` as a root parameter of the interactive component, like `ShellPreferences`: the framework protects root parameters in the page, so the browser cannot change them. `Routes` stores it in a scoped `VisitorContext`, which `IdentityApiClient` reads.
- 2026-09-19 (build) — The Web's forwarded-headers middleware is added only when a proxy is listed, and the framework's default trust of loopback is dropped: in dev a local caller cannot claim another address.
- 2026-09-19 (build) — Plain Web endpoints (`/account/profile-applied`, `/culture/set`) have no circuit, so they send no address; they only call the profile endpoints, which have no per-client limit.
- 2026-09-19 (build) — Checked through the app host: six reset requests with the headers for one visitor answer 202 ×5 then 429, and another visitor still gets 202; `/forgot-password` works from the browser.
- 2026-09-19 (review) — `/agile:review`: 0 blockers, 2 majors, 1 minor, all confirmed and fixed:
  - major, `docs/infra.md` did not list `ForwardedHeaders:KnownProxies`/`KnownNetworks` although BR5 says it does: added to the settings table, marked as required in the cloud.
  - major, `## Regression test` was empty: filled with the failing output seen before the fix (above).
  - minor, a wrong proxy entry failed the start with a bare `FormatException`: it now names the setting and the value (`TrustedProxiesTests.WrongEntry_NamesTheSetting`).

## Out of scope
- A per-client limit on sign-in (`/connect/token`).
- A shared counter across several Api instances.
- Rewriting the address of existing consent records.

## Regression test
`ClientAddressTests` (Identity, through the real Api pipeline), written first and run on the unfixed code on 2026-09-19, before `ClientAddress` existed (the tests and the fix landed in one commit, `c73137e`, but the failing run came first):
- `TwoVisitors_ThroughTheWeb_HaveTheirOwnLimit` — "Expected otherVisitor.StatusCode to be HttpStatusCode.Accepted {value: 202}, but found HttpStatusCode.TooManyRequests {value: 429}": the second visitor was refused by the first one's bucket.
- `SignUp_ThroughTheWeb_RecordsTheVisitorsAddressInTheConsent` — "Expected consent.IpAddress to be "203.0.113.10", but found <null>" (the duplicate occurrence: the consent record).
- The three guards (`AddressHeader_WithoutTheWebSecret_IsIgnored` ×2, `AddressHeader_ThatIsNotAnAddress_IsIgnored`) passed before and after: the header must stay ignored without the secret.

Web side: `VisitorAddressHostTests.PageRequest_HandsTheVisitorsAddressToTheApiClient` was run with the capture line removed from `App.razor` and failed ("Expected visitor.Address to be "203.0.113.10", but found <null>"), then passed with it restored.

## Open questions
- (none)

## Change notes

## Validation script
1. Close any IDE build, run `dotnet run --project src/Hosts/Simulab.AppHost`, open https://localhost:7125/forgot-password (signed out).
2. Keyboard only: type an email, Tab to "Send link", press Enter. → The generic answer and the 60 s countdown, as before (the Web now sends your address to the Api; nothing changes on screen).
3. Switch the language with the globe and repeat step 2 after the countdown. → Same answer in the other language.
4. Open a terminal at the repository root (`D:\dev\_icontrol\simulab`), with the app host still running, and load the shared secret and a helper that asks for one reset link as a given visitor. The secret is the Web's OpenIddict client secret, the dev-only value in `src/Hosts/Simulab.Api/appsettings.Development.json` (`Authentication:OpenIddict:ClientSecret`); the Web sends it with the visitor's address so the Api believes the address. The helper prints only the HTTP status.

   Git Bash:
   ```bash
   SECRET=$(grep -o '"ClientSecret": *"[^"]*"' src/Hosts/Simulab.Api/appsettings.Development.json | sed 's/.*: *"//; s/"$//')
   URL=https://localhost:7287/api/v1/identity/password-reset-requests
   ask() { curl -sk -o /dev/null -w "%{http_code} " -X POST $URL -H "Content-Type: application/json" -H "X-Simulab-Client-Address: $1" -H "X-Simulab-Client-Secret: $2" -d '{"email":"x@example.com"}'; }
   ```

   PowerShell 7:
   ```powershell
   $secret = (Get-Content src/Hosts/Simulab.Api/appsettings.Development.json -Raw | ConvertFrom-Json).Authentication.OpenIddict.ClientSecret
   $url = 'https://localhost:7287/api/v1/identity/password-reset-requests'
   function Ask($address, $key) { (Invoke-WebRequest -Uri $url -Method Post -ContentType 'application/json' -Body '{"email":"x@example.com"}' -Headers @{ 'X-Simulab-Client-Address' = $address; 'X-Simulab-Client-Secret' = $key } -SkipCertificateCheck -SkipHttpErrorCheck).StatusCode }
   ```
   → Nothing is printed. In Bash `echo ${#SECRET}` and in PowerShell `$secret.Length` print a number above 0: the secret was found.
5. Ask for 6 links as one visitor, `203.0.113.50`:
   - Bash: `for i in 1 2 3 4 5 6; do ask 203.0.113.50 "$SECRET"; done; echo`
   - PowerShell: `(1..6 | ForEach-Object { Ask '203.0.113.50' $secret }) -join ' '`

   → `202 202 202 202 202 429`: the limit is 5 links an hour per visitor, and the sixth is refused.
6. Ask once as another visitor, `198.51.100.50`:
   - Bash: `ask 198.51.100.50 "$SECRET"; echo`
   - PowerShell: `Ask '198.51.100.50' $secret`

   → `202`: this visitor has their own limit. Before the fix every visitor shared one limit, so this answer was `429`.
7. Claim a new address, `203.0.113.150`, with a wrong secret:
   - Bash: `ask 203.0.113.150 wrong; echo`
   - PowerShell: `Ask '203.0.113.150' 'wrong'`

   → `202` or `429`, never judged by `203.0.113.150`: without the Web's secret the Api ignores the claimed address and counts the call under your own connection, which the browser in step 2 and earlier runs may already have used. The automated test `AddressHeader_WithoutTheWebSecret_IsIgnored` proves this case exactly.

   To run steps 5-7 again within the hour, restart the app host (the counters live in the Api's memory) or use other addresses: a used address stays at `429` for one hour.

## Delivery
- Branch: bug/B-4 (removed; never pushed)
- Merge: 2f2f32c (--no-ff, AB#720)
- Validation: passed by the owner, 2026-09-19
- Regression: ClientAddressTests seen failing (429 for the second visitor, null consent address) before the fix
- Review: agile:reviewer, 0 blockers, 2 majors and 1 minor, all fixed
- Tests: full suite 425 passed, 0 failed; build 14 s, tests 27 s; 0 warnings, baseline empty
- Manual pages: none changed (password.md already says "this device"; now it is true)
- Infra: `ForwardedHeaders:KnownProxies` / `KnownNetworks` must be filled when the cloud environment is created (docs/infra.md)
