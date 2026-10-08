---
feature: F-94
epic: Cloud hosting and operations
status: idea
board: 140
version: 1
---
# Every setting a host requires has a source in the publish

Technical terms: [glossary](../glossary.md)

## Summary
A test, in the app host tests or the architecture tests, that lists the settings the Web and the Api bind and require (for example `OpenIddictClientOptions`) and asserts that each has a value in the publish model or in the app host's `appsettings.<Environment>.json`, so the next missing one fails the build and not the staging. Raised by the retro of F-64 (2026-10-08): the Web had `Authentication:OpenIddict:ClientId` only in `appsettings.Development.json`, `docs/infra.md` said "cloud: same value" but nothing set it, and every sign-in got a 401 `invalid_client` ("The mandatory 'client_id' parameter is missing"). Evidence: the Web log line added in `b66539e` on the first staging. F-64 D23 fixed that one key with `AzurePublishModelTests.Publish_TheWebIsToldTheClientIdTheApiSeeds`; this item generalizes it.

## Start
- Depends on: F-64 (done).
- Waits on (to start): nothing.
- Needed to validate: nothing; the test itself is the proof.
- Suggested path: `/agile:refine` → `/agile:build`. How the hosts declare which settings are required (options classes with a validator, or a list in the test) is settled at `/agile:refine`.
- Parallel with: unknown — settled at /agile:refine.
