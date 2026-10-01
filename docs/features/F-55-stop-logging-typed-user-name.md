---
feature: F-55
epic: Foundation and identity
status: idea
board: 93
version: 1
---
# Stop logging the typed user name on token requests

## Summary
The Api's OpenIddict server logs "The token request was successfully extracted" at Information level (category `OpenIddict.Server.OpenIddictServerDispatcher`, event 6075) with the whole request form. The password and the client secret are redacted, but `username` is not: the typed sign-in name, an e-mail address, is written in clear text to every log sink. This goes against the intent of F-21 BR4 (the attempt is recorded, the typed name is not) and F-38 BR7 (the warning line carries no user name). Found while building F-38 (2026-10-01), when a test that looked for the name in the log found it in this entry.

## Start
- Depends on: nothing.
- Waits on: nothing.
- Suggested path: `/agile:refine` → `/agile:build`. Direction to confirm at refinement: raise the `OpenIddict.Server` log category to Warning in the Api logging configuration, or filter that event; either way with a test that a failed and a successful sign-in write no log entry containing the typed name.
- Parallel with: any item outside the Api logging configuration.
- Evidence: in F-38's test `SignInRateLimitTests.Refusals_WriteNoAccountEventAndOneWarningLineWithTheAddress`, an assertion over all log entries failed on entries of category `OpenIddict.Server.OpenIddictServerDispatcher`, event 6075, message containing `"username": "visivel-14@exemplo.com"`.
