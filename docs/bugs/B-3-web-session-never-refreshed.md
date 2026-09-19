---
bug: B-3
feature: F-5
status: idea
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
Found while refining F-7 (2026-09-19), confirmed in code: `AuthClient.RefreshAsync` (`src/Hosts/Simulab.Web/Services/Auth/AuthClient.cs:44`) has no caller, and the cookie options (`src/Hosts/Simulab.Web/Program.cs:28-38`) have no `OnValidatePrincipal` or any other check, so the cookie is trusted for its full 30 days.

## Fix
<!-- Filled in refinement. -->

## Regression test
<!-- Filled in refinement. -->

## Open questions
- (none)

## Validation script

## Delivery
