---
feature: F-4
epic: Foundation and identity
status: validating
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
Designed with `/agile:screen` on 2026-09-17; mockup: `docs/features/mockups/F-4-sign-up-and-email-verification.html`.

### Kit additions (F-1 kit, all shown in `/dev/ui`)
- `AuthLayout` — centred card, maximum 440 px, on the app background. App name at the top, the card, and a footer with the links to `/terms` and `/privacy`. Language and theme switches sit above the card, right-aligned. No app menu, no breadcrumb, no user slot. Declared exception in `ui-project.md`, like the exam session screens.
- `AppTextField` — label above, required marker, hint below, error text replacing the hint, `aria-describedby` wired to whichever is showing, `aria-invalid` on error.
- `AppPasswordField` — `AppTextField` plus a show/hide button with an accessible name (never icon only).
- `AppPasswordStrength` — bar plus the word (Weak / Fair / Strong). Never colour alone; announced with `aria-live="polite"`.
- `AppCheckbox` — label to the right, may hold a link, required marker, error text below the group.
- `AppAlert` — info, warning, error and success, an optional action, `role="alert"` for error and `role="status"` for the rest.

### Route `/sign-up`
Layout: `AuthLayout` → title, subtitle, form alert slot, form, submit button.
Fields, in tab order:

| Field | Component | Required | Limits |
|---|---|---|---|
| Email | `AppTextField` (`type=email`, `autocomplete=username`) | yes | 254 characters, email format |
| Password | `AppPasswordField` (`autocomplete=new-password`) + `AppPasswordStrength` | yes | 12 to 128 characters, one uppercase, one digit, one symbol |
| Confirm password | `AppPasswordField` (`autocomplete=new-password`) | yes | equal to the password |
| Full name | `AppTextField` (`autocomplete=name`) | no | 120 characters |
| 18 or older | `AppCheckbox` | yes | — |
| Terms of use | `AppCheckbox` with a link to `/terms` (new tab) | yes | — |
| Privacy policy | `AppCheckbox` with a link to `/privacy` (new tab) | yes | — |

Actions: **Create account** (primary, full width). No sign-in link and no Google button: `/sign-in` does not exist until F-5 and Google is off (ADR-0001 #13).
Navigation: success goes to `/check-email` carrying the address in the navigation state, not in the query string (the address is personal data and must not reach a URL or a log).
Permissions: anonymous. A signed-in user cannot reach this page before F-5 exists.

States:
- **loading** — the legal versions are being fetched; the form is visible and disabled, the submit button shows the loading text.
- **ready** — normal.
- **validation error** — per field, shown when the field loses focus and again on submit; focus moves to the first field in error.
- **server error** — `AppAlert` error at the top of the form, focus moved to it; the fields keep what the visitor typed, except the two password fields, which are cleared.
- **submitting** — submit disabled with a progress indicator; every field read-only.
- **draft legal text** — `AppAlert` warning above the checkboxes when either document is a placeholder (BR15).
- **legal documents unavailable** — `AppAlert` error and the submit button disabled, with **Try again**; nothing can be accepted when the text cannot be shown.
- **terms changed** — the `registration.terms_version_outdated` alert, the two acceptance checkboxes cleared, and the new versions loaded (BR5).

### Route `/check-email`
Layout: `AuthLayout` → title, message with the address, "did not arrive" hint, resend button.
Actions: **Send a new link** — starts disabled for 60 seconds, showing the remaining seconds (BR12).
Navigation: reached from `/sign-up`. Opened directly, with no address in the navigation state, it asks for the email in an `AppTextField` before resending.
States: **ready**, **cooling down** (button disabled with the counter), **sending**, **sent** (success alert with the generic text), **throttled** (`email_verification.rate_limited`), **server error**.

### Route `/verify-email?token=...`
The page reads the token from the query string and posts it (Decisions, 2026-09-17). The token never appears on screen.
States:
- **verifying** — spinner and text, announced with `role="status"`.
- **verified** — success alert, title and message. No sign-in link (F-5 adds it).
- **expired** — title, explanation, an `AppTextField` for the email and the resend button, with the same 60 s cooldown as `/check-email`.
- **invalid** — title, explanation, and a link to `/sign-up`.
- **server error** — error alert with **Try again**.

### Routes `/terms` and `/privacy`
Layout: `AuthLayout` in its wide variant (maximum 760 px), because a legal document is a page of prose.
Content: document title, "Version {0}, effective {1}", the draft notice when the version is a placeholder, and the sanitized HTML body.
States: **loading**, **ready**, **draft** (warning alert above the body), **not found** (`legal_document.not_found`), **server error** with **Try again**.

### Accessibility
- Every field has a visible `<label>` bound by `for`; the required marker is decorative and the requirement is also on `aria-required`.
- Hint and error are linked through `aria-describedby`; the error sets `aria-invalid="true"`.
- The form alert is `role="alert"` and takes focus when it appears; the success and status texts are `role="status"`.
- The password strength gives the level as a word, not only as a colour, and is announced politely while typing.
- The show/hide password button is a real button with an accessible name that changes with the state, and it does not move focus out of the field.
- The resend counter is announced at most once every 10 seconds (`aria-live="polite"` on a text that only changes at those points), never on every tick.
- Tab order follows the visual order; the checkbox links are reachable and open in a new tab, which the accessible name says.
- Each page sets its own `<title>` and one `<h1>`. Contrast follows WCAG 2.2 AA, checked in light and dark mode.

### UI texts
New resource set `Simulab.Identity/Resources/Identity.resx` (neutral = en) plus `.pt-BR.resx` and `.pt-PT.resx`. Error codes are keys as they are (i18n rule).

| Key | en | pt-BR | pt-PT |
|---|---|---|---|
| `Auth.Legal.Terms` | Terms of use | Termos de Uso | Termos de Utilização |
| `Auth.Legal.Privacy` | Privacy policy | Política de Privacidade | Política de Privacidade |
| `SignUp.Title` | Create your account | Crie sua conta | Crie a sua conta |
| `SignUp.Subtitle` | Practise with realistic exams and find out what to study next. | Pratique com simulados realistas e descubra o que estudar em seguida. | Pratique com simulações realistas e descubra o que estudar a seguir. |
| `SignUp.Email.Label` | Email | E-mail | E-mail |
| `SignUp.Email.Placeholder` | you@example.com | voce@exemplo.com | voce@exemplo.com |
| `SignUp.Email.Required` | Enter your email. | Informe seu e-mail. | Indique o seu e-mail. |
| `SignUp.Password.Label` | Password | Senha | Palavra-passe |
| `SignUp.Password.Hint` | At least 12 characters, with an uppercase letter, a digit and a symbol. | No mínimo 12 caracteres, com maiúscula, número e símbolo. | No mínimo 12 caracteres, com maiúscula, número e símbolo. |
| `SignUp.Password.Required` | Enter a password. | Informe uma senha. | Indique uma palavra-passe. |
| `SignUp.Password.Show` | Show password | Mostrar senha | Mostrar palavra-passe |
| `SignUp.Password.Hide` | Hide password | Ocultar senha | Ocultar palavra-passe |
| `SignUp.Password.Strength` | Password strength: {0} | Força da senha: {0} | Segurança da palavra-passe: {0} |
| `SignUp.Password.Strength.Weak` | Weak | Fraca | Fraca |
| `SignUp.Password.Strength.Fair` | Fair | Média | Média |
| `SignUp.Password.Strength.Strong` | Strong | Forte | Forte |
| `SignUp.ConfirmPassword.Label` | Confirm password | Confirmar senha | Confirmar palavra-passe |
| `SignUp.ConfirmPassword.Required` | Repeat the password. | Repita a senha. | Repita a palavra-passe. |
| `SignUp.ConfirmPassword.Mismatch` | The two passwords are not the same. | As senhas não são iguais. | As palavras-passe não coincidem. |
| `SignUp.FullName.Label` | Full name (optional) | Nome completo (opcional) | Nome completo (opcional) |
| `SignUp.Adult.Label` | I declare that I am 18 or older | Declaro ter 18 anos ou mais | Declaro ter 18 anos ou mais |
| `SignUp.Terms.Label` | I have read and accept the {0} | Li e aceito os {0} | Li e aceito os {0} |
| `SignUp.Privacy.Label` | I agree with the {0} | Concordo com a {0} | Concordo com a {0} |
| `SignUp.LinkOpensNewTab` | opens in a new tab | abre em uma nova aba | abre num novo separador |
| `SignUp.Submit` | Create account | Criar conta | Criar conta |
| `SignUp.Submitting` | Creating the account... | Criando a conta... | A criar a conta... |
| `SignUp.LegalLoading` | Loading the legal texts... | Carregando os textos legais... | A carregar os textos legais... |
| `SignUp.LegalUnavailable` | The legal texts could not be loaded, so sign-up is unavailable right now. | Não foi possível carregar os textos legais, então o cadastro está indisponível agora. | Não foi possível carregar os textos legais, por isso o registo está indisponível neste momento. |
| `Legal.DraftNotice` | Draft text: the legal documents are under review and may still change. | Texto provisório: os documentos legais estão em revisão e ainda podem mudar. | Texto provisório: os documentos legais estão em revisão e ainda podem mudar. |
| `Legal.Version` | Version {0}, effective {1} | Versão {0}, em vigor desde {1} | Versão {0}, em vigor desde {1} |
| `CheckEmail.Title` | Check your email | Verifique seu e-mail | Verifique o seu e-mail |
| `CheckEmail.Message` | We sent a verification link to {0}. It is valid for 24 hours. | Enviamos um link de verificação para {0}. Ele vale por 24 horas. | Enviámos uma ligação de verificação para {0}. É válida durante 24 horas. |
| `CheckEmail.NotArrived` | Did not arrive? Look in the spam folder before asking for a new link. | Não chegou? Procure na caixa de spam antes de pedir um novo link. | Não chegou? Procure na pasta de spam antes de pedir uma nova ligação. |
| `CheckEmail.Resend` | Send a new link | Enviar um novo link | Enviar uma nova ligação |
| `CheckEmail.Resend.Cooldown` | You can ask for a new link in {0} s. | Você pode pedir um novo link em {0} s. | Pode pedir uma nova ligação dentro de {0} s. |
| `CheckEmail.Resend.Sent` | If the address is registered and still pending, a new link is on its way. | Se o endereço estiver cadastrado e ainda pendente, um novo link está a caminho. | Se o endereço estiver registado e ainda pendente, será enviada uma nova ligação. |
| `VerifyEmail.Verifying` | Verifying your email... | Verificando seu e-mail... | A verificar o seu e-mail... |
| `VerifyEmail.Verified.Title` | Email verified | E-mail verificado | E-mail verificado |
| `VerifyEmail.Verified.Message` | Your account is active and ready to use. | Sua conta está ativa e pronta para uso. | A sua conta está ativa e pronta a usar. |
| `VerifyEmail.Expired.Title` | This link expired | Este link expirou | Esta ligação expirou |
| `VerifyEmail.Expired.Message` | Verification links last 24 hours. Enter your email and we send a new one. | Os links de verificação valem 24 horas. Informe seu e-mail e enviamos um novo. | As ligações de verificação duram 24 horas. Indique o seu e-mail e enviamos uma nova. |
| `VerifyEmail.Invalid.Title` | This link is not valid | Este link não é válido | Esta ligação não é válida |
| `VerifyEmail.Invalid.Message` | It may have been used already, or copied only in part. Sign up again or ask for a new link. | Ele pode já ter sido usado, ou ter sido copiado pela metade. Cadastre-se de novo ou peça um novo link. | Pode já ter sido usada, ou ter sido copiada apenas em parte. Registe-se novamente ou peça uma nova ligação. |
| `VerifyEmail.BackToSignUp` | Go to sign-up | Ir para o cadastro | Ir para o registo |
| `registration.age_declaration_required` | You must be 18 or older to create an account. | É preciso ter 18 anos ou mais para criar uma conta. | É preciso ter 18 anos ou mais para criar uma conta. |
| `registration.consent_required` | Accept the terms of use and the privacy policy. | Aceite os Termos de Uso e a Política de Privacidade. | Aceite os Termos de Utilização e a Política de Privacidade. |
| `registration.terms_version_outdated` | The legal texts changed while you were filling the form. Read them again and accept. | Os textos legais mudaram enquanto você preenchia o formulário. Leia de novo e aceite. | Os textos legais mudaram enquanto preenchia o formulário. Leia novamente e aceite. |
| `registration.password_too_weak` | This password does not follow the rules above. | Esta senha não segue as regras acima. | Esta palavra-passe não segue as regras acima. |
| `registration.email_invalid` | Enter a valid email address. | Informe um e-mail válido. | Indique um e-mail válido. |
| `registration.rate_limited` | Too many attempts from this device. Try again in an hour. | Muitas tentativas neste dispositivo. Tente de novo em uma hora. | Demasiadas tentativas neste dispositivo. Tente novamente dentro de uma hora. |
| `email_verification.invalid` | This verification link is not valid. | Este link de verificação não é válido. | Esta ligação de verificação não é válida. |
| `email_verification.expired` | This verification link expired. | Este link de verificação expirou. | Esta ligação de verificação expirou. |
| `email_verification.rate_limited` | You asked for a link a moment ago. Wait a little before asking again. | Você pediu um link há pouco. Espere um pouco antes de pedir de novo. | Pediu uma ligação há pouco. Aguarde um pouco antes de pedir novamente. |
| `legal_document.not_found` | This document does not exist. | Este documento não existe. | Este documento não existe. |
| `Email.Verification.Subject` | Confirm your email — Simulab | Confirme seu e-mail — Simulab | Confirme o seu e-mail — Simulab |
| `Email.Verification.Heading` | Confirm your email | Confirme seu e-mail | Confirme o seu e-mail |
| `Email.Verification.Body` | Someone created a Simulab account with this address. Confirm it to activate the account. | Alguém criou uma conta no Simulab com este endereço. Confirme para ativar a conta. | Alguém criou uma conta no Simulab com este endereço. Confirme para ativar a conta. |
| `Email.Verification.Button` | Confirm email | Confirmar e-mail | Confirmar e-mail |
| `Email.Verification.Expiry` | This link is valid for 24 hours. | Este link vale por 24 horas. | Esta ligação é válida durante 24 horas. |
| `Email.Verification.Ignore` | If it was not you, ignore this message. Nothing happens without this confirmation. | Se não foi você, ignore esta mensagem. Nada acontece sem esta confirmação. | Se não foi você, ignore esta mensagem. Nada acontece sem esta confirmação. |
| `Email.Verification.LinkFallback` | If the button does not work, copy this address into the browser: | Se o botão não funcionar, copie este endereço no navegador: | Se o botão não funcionar, copie este endereço no navegador: |

### API
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
- 2026-09-17 — Build: `AddRateLimiter` does not resolve in this SDK (checked in an isolated class library and Web SDK project), so the per-client limits of BR12 are a small fixed-window `ClientRateLimiter` over `TimeProvider` in the module's Api project. It is deterministic in tests and needs no package; a second instance would need a shared counter, which arrives with Redis (F-5).
- 2026-09-17 — Build: the Identity context inherits `ModuleDbContext`, not ASP.NET Identity's own context (C# has no multiple inheritance), and maps the identity tables itself with `UserOnlyStore`. The `user_claims`, `user_logins` and `user_tokens` tables are required by that store and are filled from F-5 on.
- 2026-09-17 — Build: `User` cannot inherit `TenantEntity`, so the base context does not reach it; its `tenant` and `soft_delete` filters are declared in `IdentityModuleDbContext` with the same names and the same capture rule.
- 2026-09-17 — Build: the API error codes live in the shared resources (one helper, `ErrorText`, turns a code into text) and the screen texts in a new `IdentityResources` set; the email texts are the module's own `IdentityEmails` resources, because the email is rendered server side in the user's language.
- 2026-09-17 — Build: the address goes from `/sign-up` to `/check-email` through a scoped `SignUpFlow`, never through the query string: it is personal data and would end up in logs.
- 2026-09-17 — Build (found on screen): the MailPit connection string carries the container's internal SMTP port (1025), not the port the Api reaches from the host, so every email was refused. The app host now passes `ConnectionStrings__mailpit` built from the mapped endpoint, and `SmtpEmailSender` names the host and port it could not reach.
- 2026-09-17 — Build (found on screen): the legal Markdown files no longer repeat the document title, which the page already shows from the manifest, and the password strength bar is hidden while the field is empty.
- 2026-09-17 — Screen approved by the owner from the mockup, as designed: full name after the password fields, the confirm-password field kept, `/check-email` as its own page, and the legal documents opening in a new tab — owner, screen questions 1 to 4.

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
A container runtime (Docker Desktop or Podman) must be running. Close any app host or IDE running from this checkout first.
1. Start `dotnet run --project src/Simulab.AppHost` and open the dashboard URL printed on start. Wait for `postgres`, `simulab`, `mailpit`, `api` and `web` to reach Running.
2. Open the `web` URL and go to `/sign-up`. Submit the empty form: each required field shows its own message, the two acceptances show theirs, and nothing is sent. The orange notice above says the legal texts are a draft.
3. Fill in an address of yours, the password `Estudar#2026!` twice, tick the three boxes and create the account. The page moves to "Check your email" with your address, and the resend button counts down from 60 s.
4. Open the `mailpit` URL from the dashboard: the message is there, in the language of the app, with a "Confirm email" button. Click the link inside it: the page says "Email verified". There is no sign-in link yet — sign-in is F-5.
5. Open the same link again: it still says "Email verified" (clicking twice is not an error). Now change one character of the token in the address bar and reload: the page says the link is not valid and offers sign-up.
6. Go back to `/sign-up` and repeat step 3 with the **same** address: the answer is the same "Check your email" page, and no second message arrives in Mailpit. (An address that is already registered is never revealed.)
7. Switch the language to Português (Portugal) with the globe, then to English, on `/sign-up`, `/terms` and `/privacy`: every text changes, including the field labels, the draft notice and the document body. Switch the theme (moon/sun) on the same pages.
8. On `/check-email`, press "Send a new link" as soon as the counter ends, then press it again at once: the second attempt is refused by the server and no extra message arrives in Mailpit.

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: feature/F-4
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
