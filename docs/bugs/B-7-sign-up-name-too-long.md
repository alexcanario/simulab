---
bug: B-7
feature: F-4
status: refining
board: 724
severity: low
---
# Sign-up accepts a name or email longer than the column

## What happens
1. Call `POST /api/v1/identity/registrations` directly with a `fullName` of 121 characters or more.
2. `RegisterUserHandler` stores the trimmed name without a length check; the `users.full_name` column is 120 characters, so the insert fails and the answer is a 500 instead of a validation error.

The sign-up page limits the field to 120 characters (`MaxLength="120"`), so only a direct API call reaches it. Found while building F-8, which validates the same limit on the profile (`ProfileLimits.FullNameMaxLength`, `profile.full_name_too_long`). Refinement found the same gap for an email over 254 characters.

## Expected
The Api refuses a name over 120 characters or an email over 254 characters with a stable validation code (400), as the profile does, and nothing is created.

## Cause
Confirmed in code on 2026-09-19:
- `src/Modules/Identity/Simulab.Identity.Application/Registration/RegisterUserHandler.cs:69` stores `command.FullName.Trim()` with no length check. The column is `character varying(120)` (`UserConfiguration.cs:21`, migration `20260917203545_InitialIdentity.cs:131`), so `userManager.CreateAsync` (line 72) throws a `DbUpdateException` (PostgreSQL `22001`) that nothing catches: the endpoint answers 500.
- The same gap exists for the email: line 32 checks only the format (`EmailAddressAttribute`), and the `email`, `normalized_email`, `user_name` and `normalized_user_name` columns are 254 characters (`UserConfiguration.cs:17-20`). An address over 254 characters also ends in a 500.

Other places that implement the same limits (reimplementations, not callers of shared code):
- `src/Hosts/Simulab.Web/Components/Pages/Identity/SignUp.razor:72` — `MaxLength="120"` written by hand (comfort check); `Account.razor` already reads `ProfileLimits.FullNameMaxLength` (F-8), `ProfileHandler` too.
- `src/Modules/Identity/Simulab.Identity.Infrastructure/Persistence/Configurations/UserConfiguration.cs:21` — `HasMaxLength(120)` written by hand, the column itself.
- `MaxLength="254"` written by hand for the email field in `SignUp.razor:38`, `SignIn.razor:56`, `ForgotPassword.razor:33`, `ResetPassword.razor:101`, `CheckEmail.razor:44`, `VerifyEmail.razor:67`, and `HasMaxLength(254)` in `UserConfiguration.cs:17-20`.

## Fix
- BR1 Sign-up refuses a full name longer than 120 characters after trimming (blank still means no name, F-4 BR1): 400 `registration.full_name_too_long`, nothing is created and no email is sent.
- BR2 Sign-up refuses an email longer than 254 characters: 400 `registration.email_invalid` (an address that long is not a valid address, RFC 5321), nothing is created and no email is sent.
- BR3 Both checks run before the existing-account lookup, so the answer is the same whether the address is registered or not (project rule: anonymous endpoints answer the same way; F-4 BR4).
- BR4 One constant per limit, in `Simulab.Identity.Contracts`: `AccountLimits.FullNameMaxLength = 120` and `AccountLimits.EmailMaxLength = 254`. The handler, the EF mapping (`UserConfiguration`), `ProfileHandler`, `Account.razor` and the six email fields and the sign-up name field of the Web read them. The values do not change, so there is no migration.

### API
- `POST /api/v1/identity/registrations` — new error code `registration.full_name_too_long` (400). `registration.email_invalid` also covers an email over 254 characters.

### UI text
| Key | en | pt-BR | pt-PT |
|---|---|---|---|
| `registration.full_name_too_long` | Use at most 120 characters. | Use no máximo 120 caracteres. | Utilize no máximo 120 caracteres. |

## Acceptance criteria
- AC1 Given a sign-up with a full name of 121 characters (after trimming), when it is posted, then the answer is 400 `registration.full_name_too_long`, no user exists for that email and no email was sent; a name of 120 characters is accepted. (BR1)
- AC2 Given a sign-up with an email of 255 characters, when it is posted, then the answer is 400 `registration.email_invalid`, no user is created and no email was sent. (BR2)
- AC3 Given an address that is already registered, when a sign-up for it comes with a 121-character name, then the answer is the same 400 `registration.full_name_too_long` as for a new address. (BR3)
- AC4 Given the Identity model, then the `full_name` column's length is `AccountLimits.FullNameMaxLength` and the email and user-name columns' length is `AccountLimits.EmailMaxLength`; given the sign-up page, then its name field's `maxlength` is 120 and its email field's is 254. (BR4)
- AC5 The new text `registration.full_name_too_long` exists in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-09-19 — The email over 254 characters is fixed in B-7 too — owner; same cause, same endpoint and same test.
- 2026-09-19 — Every hand-written 120 and 254 reads one constant now — owner; the duplicates listed in `## Cause` are fixed, not deferred.
- 2026-09-19 — Out of scope: other Identity endpoints and fields — owner; sign-in, forgot password and resend only look the address up (no insert, no 500), and the password has no length-limited column.
- 2026-09-19 — `ProfileLimits` (F-8) becomes `AccountLimits`, holding both limits — the email limit is not a profile limit; a code constant, not an API contract, so renaming it breaks nothing.
- 2026-09-19 — An email over 254 characters reuses `registration.email_invalid` — it is an invalid address; no new text for the user.
- 2026-09-19 — The name uses a new code, `registration.full_name_too_long`, not `profile.full_name_too_long` — codes are per area (`registration.*`), and the sign-up page maps registration codes.
- 2026-09-19 — No new packages, for code or tests.

## Out of scope
- Length checks on other Identity endpoints (sign-in, forgot password, resend, reset).
- The password length.

## Regression test

## Open questions
- (none)

## Change notes
