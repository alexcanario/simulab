---
feature: F-55
epic: Foundation and identity
status: refining
board: 93
version: 1
---
# Stop logging the typed user name on token requests

Technical terms: [glossary](../glossary.md)

## Summary
The Api's OpenIddict server logs "The token request was successfully extracted" at Information level (category `OpenIddict.Server.OpenIddictServerDispatcher`, event 6075) with the whole request form. The password and the client secret are redacted, but `username` is not: the typed sign-in name, an e-mail address, is written in clear text to every log sink. This goes against the intent of F-21 BR4 (the attempt is recorded, the typed name is not) and F-38 BR7 (the warning line carries no user name). Found while building F-38 (2026-10-01), when a test that looked for the name in the log found it in this entry.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing beyond the local app host (the check is a log reading in the Aspire dashboard).
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: any item outside the Identity module's service registration and its sign-in tests.
- Evidence: in F-38's test `SignInRateLimitTests.Refusals_WriteNoAccountEventAndOneWarningLineWithTheAddress`, an assertion over all log entries failed on entries of category `OpenIddict.Server.OpenIddictServerDispatcher`, event 6075, message containing `"username": "visivel-14@exemplo.com"`.

## Goal
No log entry written by the Api, in any category and at any level, contains a user name or e-mail address typed by a visitor.

## What exists (verified 2026-10-04)
- OpenIddict 7.7.1 (`Directory.Packages.props`). `OpenIddictServerHandlers.Exchange.cs:132` (tag 7.7.1) logs event 6075 at Information with the request object. `OpenIddictMessage.ToString()` (`OpenIddictMessage.cs:408-423`) redacts `access_token`, `assertion`, `client_assertion`, `client_secret`, `code`, `id_token`, `id_token_hint`, `password`, `refresh_token`, `token` and any `*_token`; `username` is written as is. Read from the library source, not from memory.
- Every OpenIddict server event logs through `context.Logger`, whose category is `OpenIddict.Server.OpenIddictServerDispatcher`: a category filter cannot keep the other server events and drop only 6075.
- Api logging (`src/Hosts/Simulab.Api/appsettings.json`, `appsettings.Development.json`): `Default` Information, `Microsoft.AspNetCore` Warning, nothing for OpenIddict. Logs also go to OpenTelemetry (`Simulab.ServiceDefaults/Extensions.cs:51`); a logging filter applies to every provider.
- OpenIddict is registered in `Simulab.Identity.Infrastructure/IdentityModule.cs:194`.
- Endpoints that receive a typed e-mail: `POST /connect/token` (password grant, `TokenEndpoints.cs:36`), `POST /identity/registrations` and `POST /identity/password-reset-requests` (`IdentityEndpoints.cs:38`, `:49`). The module's own log lines in those paths carry no name (`SignInAttempt.cs:41`, `RegisterUserHandler.cs:60`, `:131`). `SeedAdmin.cs:65`, `:81` log the configured seed administrator's address, which is configuration, not typed by a visitor.
- The Web host posts to `/connect/token` with a plain `HttpClient` (`AuthClient.cs:152`); its HTTP client logs carry the URL, not the form.
- No staging or production environment exists (`docs/infra.md`, Environments): no log store holds the leaked names outside local dev (the Aspire dashboard keeps logs in memory only).
- Test support: `tests/Simulab.Testing/RecordingLoggerProvider.cs`, already used by `SignInRateLimitTests`.

## Users and use cases
- UC1 A visitor signs in with a password (success, wrong password, unknown name, refused by the F-38 limit): the attempt is handled as today and the typed name appears in no log entry.
- UC2 A visitor signs up or asks for a password reset: the typed e-mail appears in no log entry.

## Business rules
- BR1 The Api writes no log entry, in any category and at any level, that contains a user name or e-mail address typed by a visitor at sign-in, sign-up or password-reset request.
- BR2 The OpenIddict server's log category (`OpenIddict.Server`) writes Warning and above only. The rule lives in the Identity module's code, so a configuration value cannot lower it.
- BR3 Sign-in, sign-up and password reset behave exactly as before (answers, account events, F-38 limits and its single warning line).

## Screens and API
No screen, route or error code changes. No user-visible behavior changes, so the app manual is not updated.

## Acceptance criteria
- AC1 (UC1, BR1, BR2) Given a recording logger at Trace level for every category, when a visitor signs in with a correct password, then no recorded entry contains the typed name.
- AC2 (UC1, BR1) Given the same logger, when sign-ins fail with a wrong password and with an unknown name, then no recorded entry contains either typed name.
- AC3 (UC1, BR1) Given the same logger, when an address is refused by the F-38 limit, then no recorded entry contains any typed name, and the single F-38 warning line is still written with the address.
- AC4 (UC2, BR1) Given the same logger, when a visitor signs up and asks for a password reset, then no recorded entry contains the typed e-mail.
- AC5 (BR2) Given the Api host started with `Logging:LogLevel:OpenIddict.Server` and, in a second case, `Logging:LogLevel:OpenIddict.Server.OpenIddictServerDispatcher` set to `Trace` in configuration, when a sign-in runs, then no entry of category `OpenIddict.Server.*` below Warning is recorded.
- AC6 (BR3) The existing sign-in, sign-up, password-reset and F-38 tests stay green.
- Localization: no UI text is added or changed.

## Decisions
- 2026-10-04 — Direction: raise the `OpenIddict.Server` category to Warning with a logging filter registered in code by the Identity module, not an `appsettings.json` line and not a wrapper that drops only event 6075 — owner, at refinement; the rule travels with the module and a configuration value cannot reopen it; the wrapper would depend on OpenIddict internals. Trade-off accepted: OpenIddict's Information diagnostics (for example, the reason a token request was rejected) no longer appear; its warnings and errors still do.
- 2026-10-04 — The tests cover sign-in (success, failure, refused by the limit), sign-up and the password-reset request, across every category and level — owner, at refinement; these are the endpoints that receive a typed e-mail, and the test costs the same.
- 2026-10-04 — The tests record at Trace level, not at the configured Information level — Claude; a developer or operator may lower `Default`, and the rule must hold then too.
- 2026-10-04 — Logging picks the rule with the longest matching category and, between equal ones, the last registered; a configuration key on the full dispatcher category would beat a code rule on the `OpenIddict.Server` prefix. The code rule therefore covers the prefix and the full category name, and AC5 tests both configuration keys — Claude; not yet reproduced, AC5 is written first in the build and must be seen failing without the filter.
- 2026-10-04 — No purge of existing logs — Claude; no environment with a persistent log store exists yet (`docs/infra.md`).
- 2026-10-04 — No new packages — Claude; `Microsoft.Extensions.Logging` filtering and the existing `RecordingLoggerProvider` are enough.

## Out of scope
- `SeedAdmin` log lines with the configured seed administrator address (configuration, not visitor input).
- Google sign-in and sign-up (`/identity/google-registrations`): the visitor types no name; the request carries a Google token, which OpenIddict already redacts.
- Redacting personal data in other log categories in general (a wider policy); only the typed sign-in name is covered here.

## Open questions
None.

## Change notes

## Validation script

## Delivery
