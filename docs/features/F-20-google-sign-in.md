---
feature: F-20
epic: Foundation and identity
status: idea
board: 733
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
Status flow: idea -> refining -> approved -> building -> validating -> done
- idea: title and summary only (/agile:idea).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
Remove these comments when the file leaves `idea`.
-->
# Google sign-in

## Summary
Sign up and sign in with a Google account, behind a configuration switch (ADR-0001 #13). Split out of F-11 on 2026-09-20, which became TOTP only: the two are different jobs, and this one cannot start before the owner registers an OAuth client in Google Cloud and gets a client id, a client secret and the redirect URIs for development and for the future public address.

Simulae has the shape to follow (read-only, 2026-09-20): `Identity:GoogleSsoEnabled` plus a `GoogleSso` options section, an `ExternalLogin` entity with its store, four endpoints (`start`, `complete`, `confirm`, `cancel`) and a button on the sign-in page, with four test files. Simulab already maps the `user_logins` table (`IdentityUserLogin<Guid>`), which may replace Simulae's own entity. Refinement has to settle: which host owns the Google scheme and the redirect (Api or Web, with two hosts and a Blazor Server circuit that cannot redirect by itself); what happens when the Google address already has a password account; whether the 18+ declaration and the consent records are collected before or after the Google round trip; and how a first sign-in creates the account.
