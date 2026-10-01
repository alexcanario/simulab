---
feature: F-38
epic: Foundation and identity
status: refining
board: 757
version: 1
---
# Sign-in rate limit per client

## Summary
Limit failed sign-in attempts per client address on `/connect/token`, next to the per-account lockout (5 attempts / 15 minutes). Today someone spraying many accounts, or insisting on addresses that belong to no account, is not slowed down at all, and since F-21 every attempt also writes a row in the account event trail. Found while refining F-21 (2026-09-23).

## Start
- Depends on: nothing (F-21 account events, F-11 two-factor and B-4 client address forwarding are merged).
- Waits on: nothing.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: any item outside the Identity module and the sign-in page.

## Goal
Slow down password guessing spread over many accounts from one address, without getting in the way of a classroom of students signing in from the same network.

## What exists (verified 2026-10-01)
- `ClientRateLimiter` (`src/Modules/Identity/Simulab.Identity.Api/ClientRateLimiter.cs`): fixed window per key, in memory, per process; only `TryAcquire` (count and check in one call). Used by registration, resend and password reset with the limits in `IdentityRateLimits`.
- `ClientAddress` (`ClientAddress.cs`): trusts `X-Simulab-Client-Address` only when `X-Simulab-Client-Secret` matches the client secret (B-4); otherwise the connection address.
- `/connect/token` (`TokenEndpoints.cs`): password, refresh, two-factor code and Google grants; per-account lockout 5 / 15 minutes (`IdentityModule.cs:74`); every failure writes an account event (F-21).
- **False premise in the idea:** the Web calls `/connect/token` from `AuthClient.RequestAsync` (`src/Hosts/Simulab.Web/Services/Auth/AuthClient.cs`) **without** the visitor headers that `IdentityApiClient.AddVisitor` sends. The Api therefore sees the Web host's address for every sign-in, and a per-address limit would put every visitor in one bucket. This item fixes the forwarding (BR6).
- Sign-in page (`SignIn.razor`) shows `identity.account_locked` with the time left; the same format is reused.

## Users and use cases
- UC1 A visitor who mistypes their password a few times signs in normally; the address limit never shows.
- UC2 Many students behind one school or prep-course network sign in at the same time; successful sign-ins never count, so the class is not blocked.
- UC3 An attacker trying many accounts (or addresses with no account) from one address is refused after 30 failures in 15 minutes, without the accounts being looked up, locked or logged.
- UC4 The refused visitor sees how long to wait, and after the window ends can try again.

## Business rules
- BR1 Only failed attempts count, per client address: unknown account, wrong password, attempt on a locked account (password step), wrong two-factor code and invalid or spent challenge (code step). A successful step, an email-not-verified answer (correct password), the Google step and the refresh grant never count.
- BR2 Limit: 30 failures per address in a fixed window of 15 minutes (`IdentityRateLimits.SignInFailuresPer15Minutes`, `IdentityRateLimits.SignInWindow`).
- BR3 While an address is at the limit, the password step and the two-factor code step are refused before any account work: no account lookup, no failure count on an account, no account event, no lockout. The Google step and the refresh grant are not refused by this limit.
- BR4 The refusal answers with the OAuth error `identity.sign_in_rate_limited` and the seconds left in the window in `error_description`, like `identity.account_locked`.
- BR5 A successful sign-in does not reset the address count: one valid account must not let an attacker clear the window.
- BR6 The Web forwards the visitor address and the client secret on every `/connect/token` call, as `IdentityApiClient` does (B-4). Without a trusted header the Api keys on the connection address.
- BR7 A refused attempt writes no account event. The Api writes one warning log line per address per window, when the address first reaches the limit; the line carries the address and no user name.

## Screens and API
- `/signin` (`SignIn.razor`) — on `identity.sign_in_rate_limited`, in the password form and in the code form: alert `identity.sign_in_rate_limited` with the time left (`Muitas tentativas neste dispositivo. Tente de novo em {0}.`).
- `POST /connect/token` — declared exception to `/api/v1/` (F-5, decision 1); OAuth error shape.
- Error codes: `identity.sign_in_rate_limited` (new).

## Acceptance criteria
- AC1 (BR1, BR2, BR3, BR4) Given 30 failed password attempts from address A in the last 15 minutes, when a 31st password attempt comes from A, then it is refused with `identity.sign_in_rate_limited` and the seconds left, and the named account gets no failure count and no account event.
- AC2 (BR1) Given 30 successful sign-ins from address A in 15 minutes, when a 31st attempt with the right password comes from A, then tokens are issued.
- AC3 (BR1) Given failures on unknown accounts, wrong passwords and a locked account from address A, when they add up to 30, then the next password attempt from A is refused.
- AC4 (BR1, BR3) Given wrong two-factor codes from address A count towards the 30, when A reaches the limit, then a code attempt from A is refused with `identity.sign_in_rate_limited` and the challenge is not consumed.
- AC5 (BR1, BR3) Given address A is at the limit, when A uses the Google step or the refresh grant, then each is processed as before; and failures of those grants never add to the count.
- AC6 (BR2) Given address A is at the limit, when a password attempt comes from address B, then it is processed normally; and when the 15-minute window of A ends, the next attempt from A is processed.
- AC7 (BR5) Given 29 failures from address A, when a sign-in from A succeeds, then the next failure from A is the 30th and the one after it is refused.
- AC8 (BR6) Given a sign-in through the Web, when the Api receives `/connect/token`, then it keys the count on the visitor address sent by the Web; and a request with the address header but a wrong secret is keyed on the connection address.
- AC9 (BR7) Given address A reaches the limit and is refused several times, then no account event is written for the refused attempts and exactly one warning log line is written for A in that window.
- AC10 (screen) Given `/connect/token` answers `identity.sign_in_rate_limited` with 600 seconds, when the visitor submits the password form or the code form, then the page shows the alert with the time left.
- AC11 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-10-01 — Count only failures — owner; a class behind one network signs in without being blocked.
- 2026-10-01 — 30 failures per 15 minutes — owner; room for a class that mistypes once each, still stops spraying; same window as the account lockout.
- 2026-10-01 — Password and two-factor code steps count; Google and refresh do not — owner; only guessable secrets count, a Google token is checked by Google.
- 2026-10-01 — A refused attempt writes no account event; one warning log line per address per window — owner; the point is to stop the flood of rows.
- 2026-10-01 — The message shows the time left — owner; same format as the account lockout, better for the student.
- 2026-10-01 — Shared counter in Redis is out of scope and captured as F-54 — owner; one Api instance today, and the four limits move together.
- 2026-10-01 — The Web forwards the visitor address on `/connect/token` (BR6) — Claude; without it the per-address limit is one bucket for everyone (false premise, see "What exists").
- 2026-10-01 — The check runs before the account lookup (BR3) — Claude; a refused attempt then costs no database work and cannot lock out or log a victim's account.
- 2026-10-01 — `ClientRateLimiter` gets a check that does not count and a separate count that returns when the window ends, next to `TryAcquire` — Claude; counting only failures needs the two steps apart, and the existing limits keep their behavior.
- 2026-10-01 — Error code `identity.sign_in_rate_limited`, OAuth error shape, seconds in `error_description` — Claude; the token endpoint answers with the OAuth shape (F-5, decision 1) and the page already formats seconds for `identity.account_locked`.
- 2026-10-01 — No new packages — Claude; everything needed is in the solution.

## Out of scope
- Shared counter across Api instances (Redis) for all client limits — captured as F-54 (idea).
- Limits on other endpoints, CAPTCHA, and changes to the per-account lockout.

## Open questions
- (none)

## Change notes

## Validation script
1. <Step> → <expected result>

## Delivery
- Branch: feature/F-38
