---
bug: B-13
feature: F-6
status: idea
board: 745
severity: medium
---
# Identity tables have no foreign keys to users

## What happens
The Identity tables that point to a user or a role have no foreign key in the database. `IdentityModuleDbContext` derives from `ModuleDbContext`, not `IdentityDbContext`, and the configurations (for example `IdentityUserRoleConfiguration.cs`) declare no relationships. `user_roles`, `user_claims`, `user_logins`, `user_tokens`, `consent_records`, `email_verification_tokens` and `password_reset_tokens` keep a `user_id`, and `role_claims` a `role_id`, with no foreign key in the model snapshot, so rows can point to a user or role that does not exist. Found while refining F-26 (2026-09-22): the Identity entity diagram draws `users` with no relation. Whether this was intentional: not verified, no decision found.

## Expected

## Cause

## Fix
