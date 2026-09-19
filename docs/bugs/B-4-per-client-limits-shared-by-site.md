---
bug: B-4
feature: F-4
status: idea
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
<!-- Confirmed in code during refinement. -->

## Fix

## Regression test

## Open questions
- (none)

## Validation script

## Delivery
