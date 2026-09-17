---
feature: F-4
epic: Foundation and identity
status: approved
board: 708
version: 1
---
# Sign-up and email verification

## Summary
Imports the Identity module skeleton from Simulae (User, ConsentRecord, email verification tokens) renamed to English. A visitor signs up with the 18+ self-declaration and consent records, verifies the email (IEmailSender, Mailpit in dev), can resend the verification, and reads the terms and privacy pages. Needs /agile:screen.

## Goal
Give Simulab its first module and its first real user: a visitor can create an account and prove the email is theirs, with the consent LGPD and GDPR require recorded in an auditable way. Sign-in (F-5), permissions (F-6) and password recovery (F-7) all start from the `User` and the Identity module this feature creates.

## What exists
- Simulab: `Simulab.Persistence` (`ModuleDbContext`, audit and soft-delete interceptor, `tenant` and `soft_delete` filters, `AreNullsDistinct(false)` helper), `Simulab.Email` (`IEmailSender` over SMTP, Mailpit in the app host), `SharedKernel` (`Entity`, `TenantEntity`, `Result`/`Error`, `AppJson`, `ICurrentUser`, integration events). No module, no `DbContext`, no ASP.NET Identity, no OpenIddict.
- `Simulab.Api`: `/api/v1/system/info`, OpenAPI, problem details, Npgsql data source and database health check.
- `Simulab.Web`: app shell (F-2) with `MainLayout`, `NavigationItems`, language and theme switches, culture from cookie then browser then `en`; UI kit (F-1) with page header, data table, row actions, confirm dialog, form actions, empty/loading/error states, `AppIcons`. **No `HttpClient` to the API, no `AuthLayout`, and no form-field components in the kit.**
- Simulae (read-only reference): `User : IdentityUser<Guid>` with B2B and MFA fields, `AccountStatus` (typo `Pendent`), `ConsentRecord` (one `TermsVersion` plus `LgpdAccepted`), `EmailVerificationToken` (hashed, 24 h), `RegisterUserCommandHandler`, `VerifyEmailCommandHandler`, `ResendVerificationEmailCommandHandler` (**no throttle at all**), `LegalContentProvider` (manifest.json plus Markdown rendered to sanitized HTML, **BR only**), `UserExistenceChecker` (ignores the tenant filter), `SecureTokenGenerator`, `Argon2idPasswordHasher`, password policy 12 / upper / digit / symbol, lockout 5 x 15 min, `RequireConfirmedEmail = true`, `Cadastro.razor` and `VerifiqueEmail.razor` (hardcoded pt-BR, inline `<style>`).

## Users and use cases
- UC1 A visitor creates an account with email, password, the 18+ self-declaration and acceptance of the terms and the privacy policy, and is told to check the inbox.
- UC2 A visitor opens the link in the verification email and the account becomes active.
- UC3 A visitor whose link expired asks for a new verification email from the "check your email" page.
- UC4 A visitor reads the terms of use and the privacy policy as public pages, in the language of the app.
- UC5 A visitor who signs up with an email that already exists gets the same answer as anyone else, and no new account is created.
- UC6 An auditor can see, for each account, which terms and privacy versions were accepted, when, from which address and in which language.
- UC7 A developer runs the app host and sees the verification email in Mailpit.

## Business rules
- BR1 Sign-up requires email, password, password confirmation, the 18+ self-declaration, acceptance of the terms of use and acceptance of the privacy policy. The full name is optional; blank or whitespace is stored as null.
- BR2 The password policy is 12 characters minimum, with at least one uppercase letter, one digit and one non-alphanumeric character. Lockout is 5 failed attempts for 15 minutes, and `RequireConfirmedEmail` is on; both are configuration used from F-5 on.
- BR3 The email identifies the account and is unique across every tenant, including rows the tenant filter hides.
- BR4 Sign-up never reveals whether an email is already registered: an existing email returns the same success answer and creates nothing. The distinction is logged, never returned.
- BR5 The request carries the terms and privacy versions the page showed. If either differs from the current version, sign-up is rejected with `registration.terms_version_outdated` and the page reloads the documents. When no current document exists (server misconfiguration) the check is skipped rather than blocking sign-up.
- BR6 A successful sign-up writes a `ConsentRecord` with `TermsVersion`, `PrivacyVersion`, `DeclaresAdult`, `AcceptedAt`, `IpAddress` and the `Locale` the documents were shown in. The record is never updated or deleted.
- BR7 A new `User` is created with `Status = Pending`, `EmailConfirmed = false`, `EmailVerifiedAt = null`, `IsAdultDeclared = true`, and `PreferredLanguage` set to the UI culture of the request.
- BR8 A verification token is a cryptographically random value; only its hash is stored. It is valid for 24 hours and can be consumed once.
- BR9 Verifying with a valid token sets `Status = Active`, `EmailConfirmed = true` and `EmailVerifiedAt`, and consumes the token. `Pending` is the only status that can become `Active`.
- BR10 Verifying again with the token of an account that is already active succeeds and changes nothing. An unknown or already consumed token returns `email_verification.invalid`; an expired one returns `email_verification.expired` and the page offers a resend.
- BR11 Resending always returns the same generic answer. A new email is sent only when the account exists and is `Pending`; every pending token of that user is invalidated first.
- BR12 Resending is limited to one email every 60 seconds per address, and to 5 per hour per address and per client address. Sign-up is limited to 10 attempts per hour per client address. Refused attempts return `email_verification.rate_limited` or `registration.rate_limited` and send nothing.
- BR13 The verification email is rendered in the user's `PreferredLanguage` and exists in pt-BR, pt-PT and en. It carries the link with the raw token and says how long the link is valid.
- BR14 A legal document (terms of use, privacy policy) is a Markdown file plus a manifest per locale, under `Legal/<locale>/<topic>/`. The manifest names the current version; the version code is the same in the three locales. The body is rendered to sanitized HTML, with raw HTML tags disabled.
- BR15 A document whose manifest marks the current version a placeholder is shown with a visible draft notice, on the public page and on sign-up.
- BR16 Sign-up, verification, resend and reading a legal document are anonymous. No permission is checked in this feature.

## Screens and API
Detailed design and the mockup come from `/agile:screen` before build; the elements below are fixed.

**Kit additions (F-1 kit, shown in `/dev/ui`)**
- `AuthLayout`: centred card, no app menu and no breadcrumb, with the language and theme switches. A declared exception in `ui-project.md`, like the exam session screens.
- `AppTextField`, `AppPasswordField` (show and hide, with an accessible name), `AppPasswordStrength`, `AppCheckbox`, `AppAlert`: the form patterns this screen needs, reused by F-5 and F-7.

**Web routes**
- `/sign-up` — sign-up form in `AuthLayout`. Fields: email, password, confirm password, full name (optional); checkboxes for 18+, terms and privacy, each linking to its public page. Server errors are an alert at the top of the form; field errors sit next to the field.
- `/check-email` — "we sent you a link", with the address and a resend button that shows its cooldown.
- `/verify-email?token=...` — consumes the token and shows verified, expired (with resend) or invalid. No sign-in link: F-5 adds it.
- `/terms`, `/privacy` — public documents with title, version, effective date and the draft notice when the version is a placeholder.

**API**
- `POST /api/v1/identity/registrations` — create an account. 202 on success, including an email that already exists; 400 validation; 409 `registration.terms_version_outdated`; 429 rate limited.
- `POST /api/v1/identity/email-verifications` — consume a token. 200 verified; 400 invalid; 410 expired; 429 rate limited.
- `POST /api/v1/identity/email-verifications/resend` — 202 always; 429 when throttled.
- `GET /api/v1/identity/legal-documents/{topic}` — `topic` is `terms` or `privacy`; returns topic, locale, version, effective date, title, sanitized HTML body and the placeholder flag. 404 when the topic is unknown.
- Error codes: `registration.consent_required`, `registration.age_declaration_required`, `registration.terms_version_outdated`, `registration.password_too_weak`, `registration.email_invalid`, `registration.rate_limited`, `email_verification.invalid`, `email_verification.expired`, `email_verification.rate_limited`, `legal_document.not_found`.

## Acceptance criteria
- AC1 Given the current terms and privacy versions and a valid form, when a visitor signs up, then a `User` exists with `Status = Pending`, `EmailConfirmed` false, `IsAdultDeclared` true and `PreferredLanguage` from the request culture; a `ConsentRecord` holds both versions, the locale, the address and the instant; one verification token is stored hashed; and one email was sent. (BR1, BR6, BR7, BR8)
- AC2 Given a form without the 18+ declaration, or without either acceptance, when it is submitted, then sign-up is refused with `registration.age_declaration_required` or `registration.consent_required` and nothing is created. (BR1)
- AC3 Given a password that breaks the policy, when sign-up is submitted, then it is refused with `registration.password_too_weak` and nothing is created; a compliant password is accepted. (BR2)
- AC4 Given an email already registered, including one on a row the tenant filter hides, when a visitor signs up with it, then the answer is the same 202 as for a new account, and no second `User`, token or email exists. (BR3, BR4)
- AC5 Given the page loaded version `A` and the manifest now says `B`, when sign-up is submitted, then it is refused with `registration.terms_version_outdated`; given no current document at all, then sign-up succeeds. (BR5)
- AC6 Given a valid unconsumed token, when it is posted, then the user is `Active` with `EmailConfirmed` true and `EmailVerifiedAt` set, and the token is consumed. (BR9)
- AC7 Given the token of an account that is already active, when it is posted again, then the answer is success and nothing changes; given an unknown or consumed token, then `email_verification.invalid`; given a token older than 24 hours, then `email_verification.expired`. (BR8, BR10)
- AC8 Given a pending account with an outstanding token, when a resend is requested, then the previous token no longer verifies, a new email is sent, and the answer is the generic one; given an unknown email or an already active account, then the same generic answer is returned and no email is sent. (BR11)
- AC9 Given a resend less than 60 seconds after the previous one, or the sixth in an hour for that address, then the answer is `email_verification.rate_limited` and no email is sent; given the eleventh sign-up attempt in an hour from one client address, then `registration.rate_limited`. (BR12)
- AC10 Given a user whose `PreferredLanguage` is pt-PT, when the verification email is sent, then its subject and body come from the pt-PT resources and the link carries the raw token; the same holds for pt-BR and en. (BR13)
- AC11 Given a manifest and Markdown for a locale and topic, when the legal document is requested, then the current version, effective date, title and sanitized HTML come back, raw HTML in the source is not rendered as markup, and an unknown topic returns `legal_document.not_found`. (BR14)
- AC12 Given a manifest marking the current version a placeholder, when the public page is rendered, then the draft notice is visible. (BR15)
- AC13 Given the Identity `DbContext`, then its tables and `__ef_migrations_history` live in the `identity` schema with snake_case names, and the email unique index over `TenantId` is `NULLS NOT DISTINCT`. (F-3 BR1, ADR-0001 #8)
- AC14 Architecture: `Simulab.Identity.Domain` and `Simulab.Identity.Application` reference neither EF Core nor ASP.NET; `Simulab.Identity.Contracts` references only `SharedKernel`; `Simulab.Web` references `Simulab.Identity.Contracts` and no module project; `User` is the declared exception to the `TenantEntity` rule. (profile)
- AC15 All new texts — screens, validation messages, error codes and the verification email — exist in pt-BR, pt-PT and en, and the missing-key test is green.
- AC16 On screen: a visitor signs up through the app host, the email arrives in Mailpit, the link activates the account, an expired link offers a resend, and both legal pages open in the three languages — validation script. (UC1 to UC4, UC7)

## Decisions
- 2026-09-17 — Resending is throttled: 60 s per address plus 5 per hour per address and per client address; sign-up is limited to 10 per hour per client address. The answer stays generic, so the cooldown reveals nothing — owner, question 1. Simulae has no throttle at all, which turns its anonymous resend endpoint into a way to send mail from our domain in a loop.
- 2026-09-17 — Legal documents keep the file provider (manifest plus Markdown, sanitized HTML), one folder per locale (`Legal/<locale>/<topic>/`), one version code shared by the three locales, and initial content marked `isPlaceholder` — owner, question 2. A version per locale would make the consent ambiguous; `.resx` was the simpler alternative and was rejected because it loses versioning and the acceptance history.
- 2026-09-17 — `ConsentRecord` stores `TermsVersion`, `PrivacyVersion`, `DeclaresAdult`, `AcceptedAt`, `IpAddress` and `Locale` — owner, question 3. Simulae stored one version and a bool, so it could not prove which privacy text the user read.
- 2026-09-17 — The imported `User` keeps `TenantId`, the audit and soft-delete fields, `Status` (`Pending`, `Active`), `EmailVerifiedAt`, `FullName`, `IsAdultDeclared` and `PreferredLanguage`. Dropped: `Plan` (owned by the Plans module, ADR-0001 #15e), `ContractId`, `MaxContractSessions`, `MfaEnabled`, `TotpSecretEncrypted` (they return with F-11). `Blocked` and `Suspended` arrive with the feature that uses them. The Simulae typo `Pendent` becomes `Pending` — owner, question 4.
- 2026-09-17 — `PreferredLanguage` is written at sign-up from the request culture and is the source for every email this module sends; F-8 makes it editable and promotes it to the first culture source (ADR-0001 #28) — owner, question 5. Without the column the email would leave the request scope and render in `en`.
- 2026-09-17 — The kit gains `AuthLayout` and the form components (`AppTextField`, `AppPasswordField`, `AppPasswordStrength`, `AppCheckbox`, `AppAlert`), all shown in `/dev/ui`; `AuthLayout` is a declared exception in `ui-project.md` — owner, question 6. Without it F-5 and F-7 would copy CSS from this screen.
- 2026-09-17 — The verified page shows the message with no sign-in link; F-5 adds the button — owner, question 7. `/login` does not exist yet and a dead link fails validation on screen.
- 2026-09-17 — No integration event is published yet; `UserRegistered` and `EmailVerified` arrive with the Plans module that consumes them — owner, question 8 (profile: add structure on the second use).
- 2026-09-17 — Password policy and lockout are Simulae's (12 / upper / digit / symbol, 5 x 15 min), and `RequireConfirmedEmail` is on from now — owner, question 9.
- 2026-09-17 — The Identity module uses the five-project Clean Architecture shape (ADR-0001 #2b); its schema is `identity` and its first migration is fresh, with English names (ADR-0001 #11).
- 2026-09-17 — Verification is `POST /api/v1/identity/email-verifications`, not Simulae's `GET /verify-email`: consuming a token changes state, and mail scanners follow GET links. The Web page reads the token from the query string and posts it.
- 2026-09-17 — `Simulab.Web` gets its first typed API client: base address from Aspire service discovery, `AppJson.Options` on every call, no hardcoded host (api-contracts rule).
- 2026-09-17 — Password hashing uses ASP.NET Identity's default hasher, not Simulae's Argon2id: Argon2id adds a package and a tuning decision this feature does not need. Revisit when a security review asks for it.
- 2026-09-17 — Token generation keeps Simulae's pattern: a random value, only the hash stored, the raw value only in the email.

## Out of scope
- Sign-in, sign-out, tokens and sessions (F-5). No OpenIddict and no Redis in this feature.
- Permissions, roles and seeding (F-6): every endpoint here is anonymous.
- Password recovery and change (F-7).
- My account, editing the profile and changing the preferred language (F-8).
- Account erasure (F-10).
- Google sign-in and TOTP (F-11): those fields are not imported.
- Plans and entitlements: no plan is assigned at sign-up.
- Admin screens over users and consent records.
- Real legal text: the documents ship as reviewed placeholders.
- Support page and any other institutional page.

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->
1. <Step> → <expected result>

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: feature/F-4
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
