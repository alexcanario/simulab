---
feature: F-54
epic: Foundation and identity
status: validating
board: 95
version: 1
---
# Shared client rate limits across Api instances

Technical terms: [glossary](../glossary.md)

## Summary
Move `ClientRateLimiter` (registration, resend, password reset and the F-38 sign-in failure limit) from the in-memory, per-process counter to a shared counter in Redis, so the limits still hold when the Api runs more than one instance. Found while refining F-38 (2026-10-01).

## Start
- Depends on: F-38 (adds the sign-in failure limit to the same limiter) — done, merged 2026-10-01.
- Waits on (to start): nothing.
- Needed to validate: nothing beyond the local app host (its Redis container) — the owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: any item that does not touch the Identity module's `ClientRateLimiter` or `AzureDeployment.cs`; `docs/infra.md` is a shared file (expect a small conflict with F-68).

## Goal
The per-client limits protect registration, email resends, password reset and sign-in from abuse. Today each Api process keeps its own counters, and the deployed Api may run up to 10 replicas (see `## What exists`), so a client spreading calls across replicas multiplies its limit. One counter shared by every replica makes each limit mean what it says.

## What exists (verified 2026-10-04)
- `src/Modules/Identity/Simulab.Identity.Api/ClientRateLimiter.cs`: in memory, per process, registered as a singleton (`src/Hosts/Simulab.Api/Program.cs:70`). Fixed window per key with its own length (`TryAcquire`); the F-38 set of distinct failed account names per address (`ReserveSignInName`, `ReleaseSignInName`, `CheckSignIn`), atomic per address, with the first refusal of a window reported once (F-38 BR7); a sweep once a minute.
- The account names are kept as HMAC-SHA256 hashes with a key made at process start (`ClientRateLimiter.cs:26`) — a second replica would hash the same name differently.
- Limits and windows: `IdentityRateLimits.cs` — registrations 10/h, resends 5/h, reset requests 5/h, reset uses 10/h, failed sign-in account names 30 per 15 min.
- Callers: `IdentityEndpoints.cs` (register, resend, reset request, reset use, reset check), `GoogleSignInEndpoints.cs`, `TokenEndpoints.cs` and `SignInAttempt.cs` (sign-in).
- Keys come from `ClientAddress.KeyFor` (`<scope>:<address group>`, B-4 and F-38 BR8): an address or an IPv6 /64 prefix, never a user name.
- Redis is already wired into the Api through `Aspire.StackExchange.Redis` (`Program.cs:58`); `RedisTotpChallengeStore` is the pattern (value carries its own expiry read against `TimeProvider`, key TTL only cleans up). Tests have `Testcontainers.Redis` 4.15.0 and `tests/Simulab.Testing/RedisServer.cs`.
- Cloud replicas: the Api's publish model sets only `minReplicas: 1` (`src/Hosts/Simulab.AppHost/AzureDeployment.cs`); the generated `api-containerapp.module.bicep` has `scale: { minReplicas: 1 }` and no rule. Azure Container Apps' default scale rule then applies: HTTP, up to 10 replicas (Microsoft Learn, "Scaling in Azure Container Apps", "Default scale rule", read 2026-10-04). The premise "one Api instance today" is true locally, not in the cloud model.
- A second Api replica also breaks sign-in today, independently of this item: OpenIddict uses development certificates per machine (`src/Modules/Identity/Simulab.Identity.Infrastructure/IdentityModule.cs:220`), so a token from one replica is rejected by another. Captured as F-73; this item caps the Api at one replica until F-73 lands (BR7).
- Items that touched this code since F-38: none (`git log` on `ClientRateLimiter.cs`).

## Users and use cases
- UC1 A visitor registers, asks for a resend or a password reset; the limit counts the call once, whichever Api replica serves it.
- UC2 A client that tries many account names from one address is refused at the limit even when its calls land on different replicas (F-38).
- UC3 The operator runs the Api with one or more replicas without the limits changing meaning; when Redis is down, registration, resend and reset keep working and the outage shows in the log.

## Business rules
- BR1 The five per-client limits of `IdentityRateLimits` (registrations, resends, reset requests, reset uses, failed sign-in account names) count in one counter shared by every Api replica, kept in Redis. Values, windows, keys and the responses (status, error codes, `Retry-After`) are unchanged.
- BR2 The behavior of each limit is today's: a fixed window per key that keeps its own length, so a short window never ends a long one (F-38 AC13); the failed-name set adds a name atomically and refuses while full, so parallel calls on any replicas cannot pass a check together (F-38 AC14); releasing a name removes only that name; the check reads without adding.
- BR3 The first refusal of a sign-in window writes one warning line with the client address (F-38 BR7) — once per window across all replicas, not once per replica.
- BR4 Account names reach Redis only as HMAC-SHA256 hashes (F-38 BR2). The key is derived with HKDF-SHA256 from the OpenIddict client secret the Api already holds (`Authentication:OpenIddict:ClientSecret`) and the fixed label `simulab/identity/rate-limit-names`; it is the same on every replica and is never written to Redis.
- BR5 When Redis cannot be reached (connection or timeout), the call is allowed and not counted, and the Api logs one error line per replica at most once a minute while the outage lasts. A release or check that cannot reach Redis does nothing and allows.
- BR6 Every limiter entry expires by itself in Redis no later than its window ends; no sweep runs in the Api. Redis keys hold only the limit scope and the address group (BR1), under the prefix `identity:rate-limit:`.
- BR7 Until F-73 shares the OpenIddict keys, the Api's publish model caps it at one replica (`maxReplicas: 1`, `minReplicas: 1` kept). F-73 lifts the cap.

## Screens and API
- No new route, screen or error code. `/api/v1/` routes, error codes (`registration.rate_limited`, `email_verification.rate_limited`, `password_reset.rate_limited`, `identity.sign_in_rate_limited` in `IdentityErrorCodes`) and `Retry-After` are unchanged.

## Acceptance criteria
- AC1 (BR1) Given two limiter instances on the same Redis (two replicas), when the calls of one address for each of the four call limits are spread across both, then the call past the limit is refused on either instance, and the next window allows again. (integration, Redis container)
- AC2 (BR1, BR2) Given two instances, when 30 distinct failed account names from one address are spread across both, then the 31st name is refused on either instance with the time left in the window, and a name already in the set is still refused while the set is full. (integration)
- AC3 (BR2) Given an address one name short of its limit, when two different names are reserved in parallel on two instances, then exactly one is added. (integration)
- AC4 (BR2) Given a name added on one instance, when it is released on the other, then only that name leaves the set, and the check on either instance reflects it. (integration)
- AC5 (BR2) Given a key with a 15-minute window and the same key on a one-hour window, when the 15 minutes pass, then the one-hour window still counts (F-38 AC13 kept). (integration, fake clock)
- AC6 (BR3) Given an address at its limit, when refusals hit both instances in one window, then exactly one warning line is written for that window; a new window writes one again. (integration)
- AC7 (BR4) Given two instances with the same client secret, when a name is reserved, then Redis holds no value or key containing the typed name, both instances produce the same hash, and an instance with another client secret produces a different one. (integration)
- AC8 (BR5) Given Redis unreachable, when a registration, a resend, a reset and a sign-in are attempted, then each proceeds as if under its limit, and the error line is written once per minute, not once per call. (integration)
- AC9 (BR6) Given any limiter entry written, then its Redis key has a time to live no longer than its window, and every key starts with `identity:rate-limit:` and contains no account name. (integration)
- AC10 (BR7) Given the publish model, when it is built, then `api` has `minReplicas: 1` and `maxReplicas: 1`. (test, `AzurePublishFilesTests`)
- AC11 (BR1) Given the existing rate-limit endpoint tests of F-4, F-7 and F-38, when they run against the Redis-backed limiter, then they stay green unchanged in what they assert. (integration)
- AC12 (localization) No UI text is added or changed; the missing-key test stays green. (test)
- AC13 (BR1) Given the app host running, when the owner reaches the registration limit, restarts the `api` resource and tries again, then the limit still refuses: the counter outlived the process. (validation script)

## Decisions
- 2026-10-04 — Shared counter in Redis, not a one-replica cap alone — owner; the limits must hold at any replica count, and Redis is already in the Api.
- 2026-10-04 — Redis down: allow and log an error (fail-open) — owner; registration, resend and reset stay up, and sign-in already depends on Redis (F-5 sessions), so no new gap opens there.
- 2026-10-04 — The name-hash key is derived from the OpenIddict client secret with HKDF — owner; precedent B-4 (no new secret to provision), the key never sits next to the hashes, and rotating the secret only resets windows of 15 min to 1 h.
- 2026-10-04 — Development certificates per replica: captured as F-73 (#115), and F-54 caps the Api at `maxReplicas: 1` until F-73 lands — owner; prevents a hidden sign-in breakage at the first deploy.
- 2026-10-04 — One implementation (Redis); the in-memory limiter is removed, and `ClientRateLimiterTests` / `SignInNameLimitTests` run against the Redis test container — technical; two implementations would let the tested one differ from the deployed one.
- 2026-10-04 — Atomic operations as Lua scripts, with the time passed in from the host's `TimeProvider`; the window's start and length live in the value and the key TTL only cleans up (pattern of `RedisTotpChallengeStore`) — technical; keeps the fake-clock tests and the per-window semantics of F-38 without relying on Redis' clock.
- 2026-10-04 — The limiter's methods become async — technical; a network call must not block a request thread.
- 2026-10-04 — No new package: `StackExchange.Redis` 3.3.0 (through `Aspire.StackExchange.Redis` 13.6.0) and `Testcontainers.Redis` 4.15.0 are already in `Directory.Packages.props` — verified.
- 2026-10-04 — `docs/infra.md` gains Redis' new role (rate-limit counters) and the Api's one-replica cap; no app manual change (no visible behavior changes).

- 2026-10-05 — Test seam `Identity:RateLimit:Namespace` (empty in every deployed environment; documented in `docs/infra.md` as test-only) — technical; test hosts share one Redis container and every in-process call has no address, so without it the counters leak between test classes.
- 2026-10-05 — Review (independent, fresh context), major: fail-open might not cover an outage present when the limiter is first built — rejected as a defect after checking: the Aspire client registration does not abort on a failed first connection; a new test (`Registration_RealRedisRegistrationUnreachableFromTheStart_StillAccepts`) runs the Api's real registration against `localhost:1` and the registration is accepted. Kept as a regression test.
- 2026-10-05 — Review, minor: only `RedisConnectionException`, `RedisTimeoutException` and `TimeoutException` count as an outage; a server error (a script defect) now surfaces instead of being logged as "cannot reach Redis" — fixed.
- 2026-10-05 — Review, minor: the namespace setting could split the shared counter if set in a deployed environment — accepted with a warning line in `docs/infra.md`.
- 2026-10-05 — Not measured: with Redis down, each limited call waits for the client's own timeout (5 s default) before it is allowed; accepted for v1 (sign-in already depends on Redis), to revisit if an outage is ever seen in the cloud.

## Out of scope
- Sharing the OpenIddict signing and encryption keys across replicas, and lifting the cap — F-73 (#115).
- Data Protection keys and `ForwardedHeaders` for the cloud ingress (without the latter every client shares the proxy's address in the cloud) — F-64 (idea).
- The Web's in-memory sign-in tickets (the Web stays one instance, `docs/infra.md`).
- Other per-process caches in the Api (`PermissionCache`, `LegalDocumentProvider`): caches, not limits.
- Changing any limit value or window.

## Open questions
- (none)

## Change notes

## Validation script
Needed to validate: nothing beyond the local app host and its Redis container — the owner; in place now. Stop the app host of any other checkout first (ports collide). The app host has not been run by Claude for this item (the workflow forbids starting the local database outside the test containers without the owner's yes); this script is its first run.

1. In the worktree, start the app host: `dotnet run --project src/Hosts/Simulab.AppHost` (Git Bash and PowerShell 7 alike) → the dashboard shows `api`, `web` and `redis` running.
2. On the registration page, submit 10 registrations with different emails from the same browser within an hour → each is accepted (generic "check your email" message). Do the last one with the keyboard only (Tab through the fields, Enter to submit).
3. Submit an 11th → the page shows the "too many registrations, try again later" message.
4. In the dashboard, restart the `api` resource and wait until it is running again → it shows running.
5. Submit one more registration → still refused with the same message (the counter lives in Redis, not in the process).
6. Sign in with the admin account → sign-in works normally.
7. Optional, outage: in the dashboard stop `redis`, then submit a registration with a new email → it is accepted after a few seconds, and the `api` console log shows one error line "The client rate limits cannot reach Redis". Start `redis` again.

No UI text was added, so there is no language step.

## Delivery
- Branch: feature/F-54
- Tests: `ClientRateLimiterTests`, `SignInNameLimitTests`, `RateLimitRedisOutageTests` (Identity.Tests); `AzurePublishFilesTests` (AppHost.Tests). Measured 2026-10-05: Identity.Tests 424 passed in 1 m 3 s, AppHost.Tests 19 in 2 s, ArchitectureTests 167 in 1 s, Api.Tests 12 in 24 s; `gate.js stop` GREEN.

| Criterion | Test |
|---|---|
| AC1 | `ClientRateLimiterTests.TryAcquire_CallsSpreadAcrossTwoReplicas_RefusesThePastTheLimitOnEither` (four scopes) |
| AC2 | `SignInNameLimitTests.Reserve_ThirtyNamesSpreadAcrossTwoReplicas_RefusesTheThirtyFirstOnEither` |
| AC3 | `SignInNameLimitTests.Reserve_TwoNamesAtOnceOnTwoReplicasWithOnePlaceLeft_AddsExactlyOne` |
| AC4 | `SignInNameLimitTests.Release_OnTheOtherReplica_TakesOutOnlyThatNameForBoth` |
| AC5 | `ClientRateLimiterTests.TryAcquire_AShortWindowOnTheSameKey_DoesNotEndTheLongOne` |
| AC6 | `SignInNameLimitTests.Reserve_RefusalsOnBothReplicas_ReportOncePerWindow`; the warning line itself: `SignInRateLimitTests` (endpoint) |
| AC7 | `SignInNameLimitTests.Reserve_StoresOnlyAKeyedHashOfTheName` |
| AC8 | `RateLimitRedisOutageTests` (registration, resend, reset, sign-in, cold start) and `ClientRateLimiterTests.TryAcquire_RedisUnreachable_AllowsAndLogsOneErrorAMinute` |
| AC9 | `ClientRateLimiterTests.Keys_AreUnderThePrefixWithATimeToLiveWithinTheWindow` |
| AC10 | `AzurePublishFilesTests.Publish_KeepsTheApiAwakeAndBothHostsAtMostOnce` |
| AC11 | `RateLimitEndpointTests`, `SignInRateLimitTests`, `SignInRateLimitCodeStepTests`, `PasswordResetTests`, `GoogleSignInTests` (unchanged assertions, green) |
| AC12 | no UI text changed; missing-key test unchanged |
| AC13 | validation script, steps 3-5 |
