---
feature: F-7
epic: Foundation and identity
status: done
board: 711
version: 2
---
# Password recovery and change

## Summary
Forgot password (email link), reset password, expired and invalid link states, and change password for a signed-in user. Imported from Simulae and converted to the UI kit.

## Goal
Let a user who forgot the password get back into the account through the email alone, and let a signed-in user change the password, without either flow revealing which addresses are registered, and ending the sessions an attacker might hold.

## Depends on
- B-3 (the Web never refreshes the access token nor notices a revoked session). Without it, BR9 ends the other sessions only at the API: the other device still shows the user signed in. B-3 is built and shipped before F-7 starts building (owner, 2026-09-19).

## What exists
- Simulab (F-4, F-5, F-6): `User` with `Status` (`Pending`, `Active`), `EmailConfirmed`, `EmailVerifiedAt`, `PreferredLanguage`; password policy 12 / upper / digit / symbol and lockout 5 x 15 min in `IdentityModule`; `SecureToken` (random value, hash stored); `EmailVerificationToken` and its store, which already give per-address throttling from the token rows (`ResendVerificationHandler`); `ClientRateLimiter` for per-client limits; `IVerificationMailer` and the `IdentityEmails` resources for emails in the user's language; `IRefreshSessionStore` over Redis with one key per session (`identity:session:{jti}`) and one per revoked access token (`identity:revoked:{jti}`), **with no index of a user's sessions**, so nothing can revoke every session of one user today.
- Web: `AuthLayout`, `AppTextField`, `AppPasswordField`, `AppPasswordStrength`, `AppAlert`, the 60 s cooldown pattern of `/check-email`, `/sign-in` with **no "forgot password" link**, `UserMenu` with only "Sign out", no "My account" page (F-8). The auth cookie carries the tokens for 30 days; `AuthClient.RefreshAsync` exists but **has no caller**, so the Web neither refreshes the access token nor notices a revoked session (F-5 UC5 not met; B-3).
- Simulae (read-only reference): `ForgotPasswordCommandHandler` (generic answer, 1 h token, pending tokens invalidated, **sends to `Pending` accounts too but does not activate them**, no throttle), `ResetPasswordCommandHandler` (generic invalid-or-expired, policy check, `ResetAccessFailedCount` — which does not clear an active lockout — and revoke all sessions), `ChangePasswordCommandHandler` (lockout shared with sign-in, revoke all sessions except the current one), `PasswordResetToken`, pages `EsqueciSenha`, `RedefinirSenha`, `LinkExpirado`, `ChangePasswordPanel`, and tests `ForgotPasswordHandlerTests`, `ForgotPasswordEndpointTests`, `ResetPasswordHandlerTests`, `ResetPasswordEndpointTests`, `ChangePasswordCommandHandlerTests`, `ChangePasswordEndpointTests`.

## Users and use cases
- UC1 A visitor who forgot the password asks for a reset link from `/sign-in` → `/forgot-password` and gets the same answer whether or not the address is registered.
- UC2 A visitor opens the link, chooses a new password and is taken to `/sign-in` with a success message; every session of the account ends.
- UC3 A visitor whose link expired asks for a new one from the same page, without going back to `/forgot-password`.
- UC4 A visitor whose account is still `Pending` resets the password through the link, and the account becomes `Active`: the link proved the mailbox is theirs.
- UC5 A locked-out visitor resets the password and can sign in right away with the new one.
- UC6 A signed-in user changes the password at `/account/password` (user menu → "Change password"), stays signed in on this device, and every other session ends.
- UC7 The account owner receives a "your password was changed" email after a reset or a change, in their language, and can react if it was not them.

## Business rules
- BR1 Asking for a reset link always returns the same generic answer. A link is sent only when the account exists and is `Pending` or `Active`; every unused reset link of that user is invalidated first, so only the newest one works.
- BR2 A reset token is a cryptographically random value; only its hash is stored (`password_reset_tokens`). It is valid for 1 hour and can be used once.
- BR3 Per-address limits, silent: at most one reset email every 60 seconds and 5 per hour per account; a refused request sends nothing and still gets the generic answer of BR1. Per-client limit: 5 requests per hour per client address, answered with `password_reset.rate_limited` (429) and nothing sent. Resetting the password (consuming a token) is limited to 10 attempts per hour per client address, answered with `password_reset.rate_limited`.
- BR4 The reset email is rendered in the user's `PreferredLanguage` (pt-BR, pt-PT, en), carries the link with the raw token, says it is valid for 1 hour, and says to ignore it if the user did not ask.
- BR5 Resetting with a valid token sets the new password, consumes the token, clears the failed-attempt count **and any active lockout**, and sends the password-changed email (BR11). An unknown or used token returns `password_reset.invalid`; an expired one returns `password_reset.expired`.
- BR6 When the account is `Pending`, a successful reset also sets `Status = Active`, `EmailConfirmed = true` and `EmailVerifiedAt`, and consumes every pending verification token of the user.
- BR7 The new password follows the policy of F-4 BR2 (`password_reset.password_too_weak`, `password_change.password_too_weak`) and must differ from the current one (`password_reset.same_as_current`, `password_change.same_as_current`). A refused password changes nothing and does not consume the token.
- BR8 Changing the password requires a signed-in caller and the current password. A wrong current password returns `password_change.current_password_invalid` and counts toward the same lockout as sign-in (5 attempts, 15 minutes); while locked, the change is refused with `identity.account_locked` and the remaining seconds, even with the right current password.
- BR9 A successful reset ends **every** session of the user: each refresh session is removed and each session's access token is revoked (F-5 BR6, BR7). A successful change ends every session **except the caller's own**. Sessions are indexed per user in Redis so they can be found (`identity:user-sessions:{userId}`).
- BR10 A successful change clears the failed-attempt count.
- BR11 After a successful reset or change, a "your password was changed" email goes to the account, in the user's `PreferredLanguage`, with the instant and a link to `/forgot-password` for when it was not them. It carries no token.
- BR12 Asking for a link and resetting are anonymous. Changing is for any signed-in user, on their own account only; no permission is involved (the caller's own `sub` is the only account it can touch).

## Screens and API
No mockup (owner, 2026-09-19): every screen reuses the F-4 and F-5 patterns and kit components.

### `/sign-in` (F-5) — changes
- A link **Forgot your password?** below the password field, to `/forgot-password`.
- A success `AppAlert` (`role="status"`) **Your password was changed. Sign in with the new one.** when the visitor arrives from a successful reset. The signal carries no personal data (no email in the URL).

### Route `/forgot-password`
Layout: `AuthLayout` → title, subtitle, form alert slot, email field, submit button, link back to `/sign-in`.

| Field | Component | Required | Limits |
|---|---|---|---|
| Email | `AppTextField` (`type=email`, `autocomplete=username`) | yes | 254 characters, email format |

Actions: **Send link** (primary, full width). Footer link: **Back to sign in** → `/sign-in`.
Permissions: anonymous. A signed-in user who opens it is not redirected (a user may want a link for the account even while signed in on another device).
States:
- **ready** — normal.
- **validation error** — on blur and on submit; focus moves to the field.
- **sending** — button disabled with a progress indicator, field read-only.
- **sent** — success `AppAlert` with the generic text; the field keeps the address; the button starts a 60 s cooldown showing the remaining seconds (same announcement throttling as F-4).
- **throttled** — `password_reset.rate_limited` error alert (per-client limit only).
- **server error** — error alert with **Try again**.

### Route `/reset-password?token=...`
Layout: `AuthLayout`. On load the page checks the token without consuming it, so an expired link is known before the visitor types a password. The token never appears on screen.

| Field | Component | Required | Limits |
|---|---|---|---|
| New password | `AppPasswordField` (`autocomplete=new-password`) + `AppPasswordStrength` | yes | 12 to 128 characters, one uppercase, one digit, one symbol |
| Confirm new password | `AppPasswordField` (`autocomplete=new-password`) | yes | equal to the new password |

Actions: **Save new password** (primary, full width).
Navigation: success goes to `/sign-in` with the success alert.
States:
- **checking** — spinner and text, `role="status"`.
- **ready** — the form.
- **validation error** — per field; mismatch on the confirm field.
- **submitting** — button disabled with progress, fields read-only.
- **password refused** — `password_reset.password_too_weak` or `password_reset.same_as_current` as the error of the new-password field; both password fields cleared.
- **expired** — title, explanation, an `AppTextField` for the email and **Send a new link**, with the 60 s cooldown and the generic sent text of `/forgot-password`.
- **invalid** — title, explanation, and a link **Ask for a new link** → `/forgot-password`.
- **server error** — error alert with **Try again**.

### Route `/account/password`
Layout: `MainLayout` (the signed-in shell), page header **Change password**, one form card following the kit's form layout.
Permissions: signed-in only; an anonymous visitor is sent to `/sign-in` (cookie `LoginPath`).

| Field | Component | Required | Limits |
|---|---|---|---|
| Current password | `AppPasswordField` (`autocomplete=current-password`) | yes | — |
| New password | `AppPasswordField` (`autocomplete=new-password`) + `AppPasswordStrength` | yes | policy of F-4 BR2 |
| Confirm new password | `AppPasswordField` (`autocomplete=new-password`) | yes | equal to the new password |

Actions (kit form actions): **Save** (`Common.Save`, primary) and **Cancel** (`Common.Cancel`, back to `/`).
States:
- **ready**, **validation error**, **submitting** — as on the other forms.
- **current password wrong** — `password_change.current_password_invalid` as the error of the current-password field, which is cleared.
- **locked** — `identity.account_locked` error alert with the remaining time counting down (F-5 pattern); Save disabled until zero.
- **password refused** — `password_change.password_too_weak` or `password_change.same_as_current` on the new-password field.
- **changed** — success alert **Password changed. Other devices were signed out.**; the three fields cleared; the user stays on the page, signed in.
- **server error** — error alert with **Try again**.

### User menu (F-5)
- Signed in: a new item **Change password** → `/account/password`, above **Sign out**.

### Accessibility
Same field, alert, countdown and tab-order rules as F-4 and F-5: labelled inputs, `aria-describedby` for hint and error, `aria-invalid`, the form alert `role="alert"` taking focus, success texts `role="status"`, the cooldown and lockout counters announced at most once every 10 seconds. Each page sets its own `<title>` and one `<h1>`.

### UI texts
Extends `IdentityResources` (screens), the shared resources (error codes, menu) and `IdentityEmails` (emails). Error codes are keys as they are.

| Key | en | pt-BR | pt-PT |
|---|---|---|---|
| `SignIn.ForgotPassword` | Forgot your password? | Esqueceu a senha? | Esqueceu-se da palavra-passe? |
| `SignIn.PasswordReset` | Your password was changed. Sign in with the new one. | Sua senha foi alterada. Entre com a nova senha. | A sua palavra-passe foi alterada. Inicie sessão com a nova. |
| `ForgotPassword.Title` | Reset your password | Redefinir a senha | Redefinir a palavra-passe |
| `ForgotPassword.Subtitle` | Enter the email of your account and we send you a link to choose a new password. | Informe o e-mail da sua conta e enviaremos um link para escolher uma nova senha. | Indique o e-mail da sua conta e enviaremos uma ligação para escolher uma nova palavra-passe. |
| `ForgotPassword.Email.Label` | Email | E-mail | E-mail |
| `ForgotPassword.Submit` | Send link | Enviar link | Enviar ligação |
| `ForgotPassword.Sending` | Sending... | Enviando... | A enviar... |
| `ForgotPassword.Sent` | If this email belongs to an account, a link to reset the password is on its way. It is valid for 1 hour. | Se este e-mail pertencer a uma conta, um link para redefinir a senha está a caminho. Ele vale por 1 hora. | Se este e-mail pertencer a uma conta, será enviada uma ligação para redefinir a palavra-passe. É válida durante 1 hora. |
| `ForgotPassword.Cooldown` | You can ask for a new link in {0} s. | Você pode pedir um novo link em {0} s. | Pode pedir uma nova ligação dentro de {0} s. |
| `ForgotPassword.BackToSignIn` | Back to sign in | Voltar para o login | Voltar ao início de sessão |
| `ResetPassword.Title` | Choose a new password | Escolha uma nova senha | Escolha uma nova palavra-passe |
| `ResetPassword.Checking` | Checking the link... | Verificando o link... | A verificar a ligação... |
| `ResetPassword.NewPassword.Label` | New password | Nova senha | Nova palavra-passe |
| `ResetPassword.ConfirmPassword.Label` | Confirm new password | Confirmar nova senha | Confirmar nova palavra-passe |
| `ResetPassword.Submit` | Save new password | Salvar nova senha | Guardar nova palavra-passe |
| `ResetPassword.Submitting` | Saving... | Salvando... | A guardar... |
| `ResetPassword.Expired.Title` | This link expired | Este link expirou | Esta ligação expirou |
| `ResetPassword.Expired.Message` | Reset links last 1 hour. Enter your email and we send a new one. | Os links de redefinição valem 1 hora. Informe seu e-mail e enviamos um novo. | As ligações de redefinição duram 1 hora. Indique o seu e-mail e enviamos uma nova. |
| `ResetPassword.Invalid.Title` | This link is not valid | Este link não é válido | Esta ligação não é válida |
| `ResetPassword.Invalid.Message` | It may have been used already, replaced by a newer one, or copied only in part. | Ele pode já ter sido usado, substituído por um mais novo, ou copiado pela metade. | Pode já ter sido usada, substituída por uma mais recente, ou copiada apenas em parte. |
| `ResetPassword.AskNewLink` | Ask for a new link | Pedir um novo link | Pedir uma nova ligação |
| `ChangePassword.Title` | Change password | Alterar senha | Alterar palavra-passe |
| `ChangePassword.CurrentPassword.Label` | Current password | Senha atual | Palavra-passe atual |
| `ChangePassword.NewPassword.Label` | New password | Nova senha | Nova palavra-passe |
| `ChangePassword.ConfirmPassword.Label` | Confirm new password | Confirmar nova senha | Confirmar nova palavra-passe |
| `ChangePassword.Changed` | Password changed. Other devices were signed out. | Senha alterada. Os outros dispositivos foram desconectados. | Palavra-passe alterada. Os outros dispositivos terminaram a sessão. |
| `Layout.UserMenu.ChangePassword` | Change password | Alterar senha | Alterar palavra-passe |
| `password_reset.invalid` | This reset link is not valid. | Este link de redefinição não é válido. | Esta ligação de redefinição não é válida. |
| `password_reset.expired` | This reset link expired. | Este link de redefinição expirou. | Esta ligação de redefinição expirou. |
| `password_reset.rate_limited` | Too many attempts from this device. Try again in an hour. | Muitas tentativas neste dispositivo. Tente de novo em uma hora. | Demasiadas tentativas neste dispositivo. Tente novamente dentro de uma hora. |
| `password_reset.password_too_weak` | This password does not follow the rules above. | Esta senha não segue as regras acima. | Esta palavra-passe não segue as regras acima. |
| `password_reset.same_as_current` | Choose a password different from the current one. | Escolha uma senha diferente da atual. | Escolha uma palavra-passe diferente da atual. |
| `password_change.current_password_invalid` | The current password is not correct. | A senha atual não está correta. | A palavra-passe atual não está correta. |
| `password_change.password_too_weak` | This password does not follow the rules above. | Esta senha não segue as regras acima. | Esta palavra-passe não segue as regras acima. |
| `password_change.same_as_current` | Choose a password different from the current one. | Escolha uma senha diferente da atual. | Escolha uma palavra-passe diferente da atual. |
| `Email.PasswordReset.Subject` | Reset your password — Simulab | Redefina sua senha — Simulab | Redefina a sua palavra-passe — Simulab |
| `Email.PasswordReset.Heading` | Reset your password | Redefina sua senha | Redefina a sua palavra-passe |
| `Email.PasswordReset.Body` | Someone asked to reset the password of the Simulab account with this address. | Alguém pediu para redefinir a senha da conta Simulab com este endereço. | Alguém pediu para redefinir a palavra-passe da conta Simulab com este endereço. |
| `Email.PasswordReset.Button` | Choose a new password | Escolher uma nova senha | Escolher uma nova palavra-passe |
| `Email.PasswordReset.Expiry` | This link is valid for 1 hour and works once. | Este link vale por 1 hora e funciona uma única vez. | Esta ligação é válida durante 1 hora e funciona uma única vez. |
| `Email.PasswordReset.Ignore` | If it was not you, ignore this message. Your password stays the same. | Se não foi você, ignore esta mensagem. Sua senha continua a mesma. | Se não foi você, ignore esta mensagem. A sua palavra-passe continua a mesma. |
| `Email.PasswordChanged.Subject` | Your password was changed — Simulab | Sua senha foi alterada — Simulab | A sua palavra-passe foi alterada — Simulab |
| `Email.PasswordChanged.Heading` | Your password was changed | Sua senha foi alterada | A sua palavra-passe foi alterada |
| `Email.PasswordChanged.Body` | The password of your Simulab account was changed on {0}. | A senha da sua conta Simulab foi alterada em {0}. | A palavra-passe da sua conta Simulab foi alterada em {0}. |
| `Email.PasswordChanged.NotYou` | If it was not you, reset the password now: | Se não foi você, redefina a senha agora: | Se não foi você, redefina a palavra-passe agora: |

### API
- `POST /api/v1/identity/password-reset-requests` — `{ email }`. 202 always when well formed (BR1, BR3); 429 `password_reset.rate_limited` (per client).
- `POST /api/v1/identity/password-reset-token-checks` — `{ token }`, does not consume it. 204 valid; 400 `password_reset.invalid`; 410 `password_reset.expired`; 429 `password_reset.rate_limited`.
- `POST /api/v1/identity/password-resets` — `{ token, newPassword }`. 204; 400 `password_reset.invalid` or `password_reset.password_too_weak`; 410 `password_reset.expired`; 422 `password_reset.same_as_current`; 429 `password_reset.rate_limited`.
- `POST /api/v1/identity/password-changes` — `{ currentPassword, newPassword }`, signed-in caller. 204; 400 `password_change.password_too_weak`; 422 `password_change.current_password_invalid` or `password_change.same_as_current`; 423 `identity.account_locked` with `retryAfterSeconds`; 401 when not signed in.
- Error codes: `password_reset.invalid`, `password_reset.expired`, `password_reset.rate_limited`, `password_reset.password_too_weak`, `password_reset.same_as_current`, `password_change.current_password_invalid`, `password_change.password_too_weak`, `password_change.same_as_current`; reused: `identity.account_locked`.

## Acceptance criteria
- AC1 Given an `Active` or `Pending` account, when a reset link is requested, then its older unused reset tokens no longer work, one new token is stored hashed with a 1-hour expiry, one email is sent, and the answer is 202; given an unknown address, then the same 202 and nothing is stored or sent. (BR1, BR2)
- AC2 Given a reset requested less than 60 s after the previous one, or the sixth in an hour for that account, then nothing is sent and the answer is the same 202; given the sixth request in an hour from one client address, then 429 `password_reset.rate_limited`; given the eleventh reset or token check in an hour from one client address, then 429 `password_reset.rate_limited`. (BR3)
- AC3 Given a user whose `PreferredLanguage` is pt-PT, when the reset email is sent, then subject and body come from the pt-PT resources and the link carries the raw token; the same for pt-BR and en. (BR4)
- AC4 Given a valid token, when it is checked, then 204 and it stays usable; given an unknown or used token, then `password_reset.invalid`; given one older than 1 hour, then `password_reset.expired`. (BR2, BR5)
- AC5 Given a valid token and a compliant new password, when the reset is posted, then the new password signs in, the old one does not, the token is consumed and a second use returns `password_reset.invalid`. (BR2, BR5)
- AC6 Given a locked-out account, when its password is reset, then the failed count is zero, the lockout is cleared, and sign-in with the new password succeeds at once. (BR5)
- AC7 Given a `Pending` account, when its password is reset, then it is `Active` with `EmailConfirmed` true and `EmailVerifiedAt` set, and its pending verification tokens are consumed. (BR6)
- AC8 Given a new password that breaks the policy or equals the current one, when a reset or a change is posted, then the matching `*.password_too_weak` or `*.same_as_current` code is returned, the password is unchanged, and a reset token is not consumed. (BR7)
- AC9 Given a signed-in user, when a change is posted with a wrong current password, then `password_change.current_password_invalid` and the failed count increases; given the fifth wrong attempt, then the account is locked and the next change, even with the right password, returns `identity.account_locked` with the remaining seconds. (BR8)
- AC10 Given a user with three sessions, when the password is reset, then all three refresh sessions are gone and all three access tokens return `identity.token_revoked`; when instead one of them changes the password, then the caller's session keeps working and the other two are revoked. (BR9)
- AC11 Given a successful change, then the failed count is zero; given a successful reset or change, then one password-changed email is sent in the user's language with the instant and a `/forgot-password` link, and no token. (BR10, BR11)
- AC12 Given an anonymous caller, then `POST /api/v1/identity/password-changes` returns 401; the three reset endpoints answer anonymously. (BR12)
- AC13 Given the Identity `DbContext`, then `password_reset_tokens` lives in the `identity` schema with snake_case names, created by a migration. (F-3 BR1)
- AC14 Given `/sign-in`, then it shows the "Forgot your password?" link, and after a successful reset it shows the success alert; given a signed-in user, then the user menu shows "Change password" linking to `/account/password`. (UC1, UC2, UC6)
- AC15 All new texts — screens, error codes and the two emails — exist in pt-BR, pt-PT and en, and the missing-key test is green.
- AC16 On screen: forgot → Mailpit → reset → sign in with the new password; an expired or tampered link shows its state; a signed-in user changes the password and a second browser is signed out — validation script. (UC1 to UC3, UC6, UC7)

## Decisions
- 2026-09-19 — A reset on a `Pending` account also activates it: the reset link proves ownership of the mailbox exactly as the verification link does — owner, card 1 q1. Simulae sent the reset but left the account `Pending`, so the user changed the password and still could not sign in.
- 2026-09-19 — Change password lives at `/account/password`, reached from the user menu; F-8 links it from "My account" — owner, card 1 q2.
- 2026-09-19 — A "your password was changed" email after both reset and change — owner, card 1 q3. The real owner learns of a change they did not make.
- 2026-09-19 — After a reset the visitor goes to `/sign-in` with a success alert, not signed in automatically — owner, card 1 q4. Keeps a single sign-in path (the F-5 password flow).
- 2026-09-19 — The reset link lasts 1 hour, as in Simulae — owner, card 2 q1.
- 2026-09-19 — Forgot-password limits mirror F-4's resend: 60 s and 5 per hour per account, silent; 5 per hour per client, 429 — owner, card 2 q2. Simulae has no limit, which lets anyone flood a mailbox from our domain.
- 2026-09-19 — A successful reset clears an active lockout — owner, card 2 q3. Simulae's `ResetAccessFailedCount` alone leaves `LockoutEnd` in place.
- 2026-09-19 — A wrong current password on change counts toward the sign-in lockout — owner, card 2 q4. Otherwise a session left open is an unlimited password-guessing oracle.
- 2026-09-19 — Reset ends every session; change ends every session except the caller's — owner, card 3 q1.
- 2026-09-19 — The Web's missing token refresh and revocation check (F-5 UC5) is bug B-3, built and shipped before F-7 — owner, card 3 q2. It keeps F-5's fix apart, with its own regression test.
- 2026-09-19 — The new password must differ from the current one, on reset and on change — owner, card 3 q3.
- 2026-09-19 — No mockup: the screens reuse F-4 and F-5 patterns — owner, card 4 q1.
- 2026-09-19 — Expired and invalid links are states of `/reset-password`, not a separate page like Simulae's `LinkExpirado` — owner, card 4 q2.
- 2026-09-19 — `/forgot-password` confirms on the same page with the generic alert and a 60 s cooldown, not through `/check-email` — owner, card 4 q3.
- 2026-09-19 — `PasswordResetToken` is its own entity and table (`password_reset_tokens`), same shape as `EmailVerificationToken`, and the per-account limit is computed from its rows (as F-4 does), so it needs no extra state and survives a restart.
- 2026-09-19 — The reset page checks the token on load through a non-consuming endpoint, so an expired link is known before the visitor types a password. The token is 256-bit random, and the check shares the per-client limit of the reset, so it is no guessing oracle.
- 2026-09-19 — `IRefreshSessionStore` gains a per-user index (`identity:user-sessions:{userId}`, a Redis set with the refresh lifetime) and `RevokeAllAsync(userId, exceptSessionJti)`. Sessions created before F-7 are not in the index; acceptable, since nothing is deployed yet.
- 2026-09-19 — Password hashing, policy and lockout stay those of `UserManager` (F-4, F-5). The reset is applied through `UserManager` with an internally generated Identity token, as in Simulae, after our own token check.
- 2026-09-19 — Changing the own password needs only a signed-in caller, no permission: the endpoint acts on the caller's `sub` and can reach no other account (rule: permissions are checked for actions over others' data).
- 2026-09-19 — The Simulae tests listed in "What exists" come with the code, renamed to English (rule: import with tests).
- 2026-09-19 — New packages: none. Redis, Testcontainers.Redis and the rest are already approved (F-4, F-5).

- 2026-09-19 — Build: `EmailVerificationToken` and `PasswordResetToken` share an abstract `SingleUseToken` (hash, expiry, consume): the second use of the shape (profile: add structure on the second use). Neither is mapped as a hierarchy; the migration only adds `password_reset_tokens`.
- 2026-09-19 — Build: Identity's `DataProtectorTokenProvider` is registered as the default token provider: `ResetPasswordAsync` asks for Identity's own token, generated in the same call after our emailed token was checked. The emailed token stays the only proof.
- 2026-09-19 — Build: BR7's checks (policy, not the current password) run before anything changes, in one shared `PasswordRules` used by reset and change; checking the current password there never counts as a failed sign-in.
- 2026-09-19 — Build: the token check and the reset share one per-client bucket (10 per hour), so the check is no free guessing oracle.
- 2026-09-19 — Build: a lockout on change answers 423 with `retryAfterSeconds`; the page counts it down and keeps Save disabled until zero.
- 2026-09-19 — Build: the password-changed email states the instant in UTC ("... UTC"): the user's time zone is not known until F-8.
- 2026-09-19 — Build: the email-culture switch used by the verification email became `EmailCulture`, shared with the new `PasswordMailer`.
- 2026-09-19 — Build: after a reset the page goes to `/sign-in?password=changed` (no personal data in the URL); sign-in shows the success alert from that path.
- 2026-09-19 — Build: `/account/password` sends an anonymous visitor to `/sign-in` itself, instead of the Not Found `AuthorizeRouteView` shows for a missing permission (F-6 BR7): this page is no secret, and Not Found would read as a broken link from the user menu.
- 2026-09-19 — Build: kit additions — `AppPasswordField.Autocomplete` (default `new-password`; `current-password` on the change page) and `AppIcons.Password` for the user-menu item. The cooldown and lockout counters use one small `Countdown` class in the identity pages.
- 2026-09-19 — Build (found on screen): raw links on the sign-in pages ("Forgot your password?", and F-5's "Create an account") had the text colour and no underline, so they read as text. They are now underlined in the text colour; the primary blue was tried and gives 3.5:1 on the dark card, under AA.
- 2026-09-19 — Build, noticed and not built (ideas): the sign-in password field still says `autocomplete="new-password"` (F-5 asked for `current-password`); the Terms/Privacy links in the auth footer are primary blue on the dark background, about 3.5:1.

- 2026-09-19 — Review (`/agile:review`, 0 blockers, 6 majors, 7 minors; every major checked in the code):
  - major, a refresh took the old session id out of the per-user index without revoking its access token, so a reset missed it for up to 15 min: a refresh now revokes the access token it replaces. Fixed; test `Reset_SessionThatRefreshed_EndsTooAndItsOlderAccessTokenIsRevoked`.
  - major, a refresh racing a revoke-all could leave a new session behind: each session now remembers the user's security stamp, which a reset or change renews, and a refresh with an old stamp is refused. The caller of a change gets the new stamp on its own session, so it keeps working (found by the tests: without it the caller was signed out at its next refresh). Worst case left: an access token from that race lives until its own 15 minutes. Fixed; test `Refresh_SessionFromBeforeThePasswordChanged_IsRefusedEvenIfRevokeAllMissedIt`.
  - major, two concurrent resets with one link could both succeed: the link is now spent first with one conditional update; a failure of Identity's own reset after the checks is a server error, never `password_too_weak`. Fixed; test `Reset_TwoConcurrentResetsWithOneLink_OnlyOneSucceeds`.
  - major, a failing mail server made a 500 that only real accounts reach: the reset email failure is logged and the answer stays 202. Fixed; test `RequestLink_MailServerFails_SameAnswerAsAnUnknownAddress`. The response time still differs (only real accounts wait for SMTP) until emails go through the job table: accepted and recorded as an idea.
  - major, the password-changed email failing after a successful change returned 500: the notice is best effort (`PasswordNotice`). Fixed; test `Change_MailServerFails_TheChangeStillSucceeds`.
  - major, the per-client limits key on the connection address, and the Api only ever sees the Web server: see `## Open questions
- (none) — the per-client limit question from the review was answered by the owner on 2026-09-19: see change note v2.

## Change notes

### v2 — 2026-09-19
- What: BR3's per-client limits hold at the Api (they key on the connection address), but end to end every visitor arrives from the Web server, so they are shared by the whole site until bug B-4 makes the Web send the visitor's address. F-7 ships with the per-client limits as built; B-4 fixes them for F-4 and F-7 together, right after F-7.
- Why: found by `/agile:review`. The bug predates F-7 (F-4's registration and resend have it too) and nothing is deployed, so no visitor is affected now.
- Affected: BR3 and AC2 (per-client half, end to end); other criteria unchanged. AC2 stays proved at the Api by its tests.
- Re-approved: 2026-09-19 (owner, option "Bug B-4, after F-7").

## Coverage
| Criterion | Tests (`Simulab.Identity.Tests` unless noted) |
|---|---|
| AC1 link for Active/Pending, generic answer | `PasswordResetTests.RequestLink_ActiveAccount_StoresOneHashedTokenForAnHourAndInvalidatesTheOlderOne`, `RequestLink_UnknownOrEmptyAddress_SameAnswerAndNothingStoredOrSent`; Pending: `Reset_PendingAccount_ActivatesItAndConsumesItsVerificationLinks` |
| AC2 limits | `PasswordResetTests.RequestLink_PerAccountLimits_AreSilent`, `RequestLink_AboveTheHourlyCapForOneClient_IsRefused`, `Reset_AboveTheHourlyCapForOneClient_IsRefused` |
| AC3 reset email language | `PasswordEmailLanguageTests.ResetAndChangedEmails_UseTheAccountLanguage` (en, pt-BR, pt-PT) |
| AC4 check without consuming, invalid, expired | `PasswordResetTests.CheckAndReset_UnknownOrExpiredToken_AnswerWithTheirOwnCodes`, `RequestLink_ActiveAccount_...` |
| AC5 reset works once | `PasswordResetTests.Reset_ValidToken_NewPasswordSignsInOldDoesNotAndTheLinkWorksOnce` |
| AC6 lockout cleared | `PasswordResetTests.Reset_LockedOutAccount_ClearsTheLockoutAndSignsInAtOnce` |
| AC7 pending activated | `PasswordResetTests.Reset_PendingAccount_ActivatesItAndConsumesItsVerificationLinks` |
| AC8 refused password | `PasswordResetTests.Reset_RefusedPassword_ChangesNothingAndKeepsTheLink`, `PasswordChangeTests.Change_RefusedNewPassword_ChangesNothing` (both codes each) |
| AC9 wrong current and lockout | `PasswordChangeTests.Change_WrongCurrentPassword_CountsTowardTheSignInLockout` |
| AC10 sessions | `PasswordResetTests.Reset_EndsEverySessionOfTheAccount`, `PasswordChangeTests.Change_KeepsTheCallersSessionAndEndsTheOthers` |
| AC11 failed count, changed email | `PasswordChangeTests.Change_Success_ClearsTheFailedCountAndSendsThePasswordChangedEmail`, `PasswordResetTests.Reset_SendsOnePasswordChangedEmailWithTheInstantAndNoToken`, `PasswordEmailLanguageTests` |
| AC12 anonymous / signed in | `PasswordChangeTests.Change_Anonymous_Returns401`, `PasswordResetTests.Endpoints_ForTheLostPassword_AreAnonymous`; the page: `ChangePasswordTests.Anonymous_IsSentToSignIn` (`Simulab.Web.Tests`) |
| AC13 schema | `IdentitySchemaTests.Migration_CreatesEveryTableInTheModuleSchema`, `Migration_NamesEveryColumnInSnakeCase` |
| AC14 sign-in link and alert, user menu | `Simulab.Web.Tests`: `SignInSessionEndedTests.Page_LinksToForgotPassword`, `PasswordChangedQuery_ShowsTheSuccessAlert`; `UserMenuTests.SignedIn_OffersChangePasswordBeforeSignOut`; `ResetPasswordTests.Submit_Success_GoesToSignInWithTheAlertAndNoPersonalData` |
| AC15 three languages | `ResourceParityTests` (`Simulab.Web.Tests`), `EmailResourceParityTests` |
| AC16 on screen | validation script below; screen states: `ForgotPasswordTests` (4), `ResetPasswordTests` (7), `ChangePasswordTests` (5) in `Simulab.Web.Tests` |

## Validation script
A container runtime must be running. Close any app host or IDE running from this checkout first.
1. Start `dotnet run --project src/Hosts/Simulab.AppHost` and open the dashboard. Wait for `postgres`, `redis`, `mailpit`, `api` and `web` to reach Running.
2. Keyboard only: open the `web` URL at `/sign-in`, Tab to "Forgot your password?", press Enter. Type your account's email, Tab to "Send link", press Enter: the generic message appears and the button counts down from 60 s.
3. Open Mailpit: the reset email is in your account's language. Open its link: "Choose a new password". Type your **current** password twice and save: "Choose a password different from the current one." Type a new one twice and save: sign-in opens with "Your password was changed. Sign in with the new one." Sign in with the new password. Mailpit also has "Your password was changed".
4. Open the same reset link again: "This link is not valid", with "Ask for a new link".
5. Sign in with the same account in a private window too. In the first window: account menu → "Change password". Type a wrong current password and save: the message sits on that field. Then the right current password and a new one twice: "Password changed. Other devices were signed out."
6. Go to the private window and do nothing: within a minute it moves to sign-in with "Your session ended. Sign in again." The first window is still signed in.
7. On `/forgot-password` and `/account/password`, switch the language with the globe to Português (Brasil) and Português (Portugal), and switch the theme: texts change, and the links on the sign-in pages are underlined and readable in both themes.
8. On "Change password", type a wrong current password five times: the lock message shows the time left and Save stays disabled. (Wait 15 minutes, or reset the password by email, which unlocks at once.)

## Delivery
- Branch: `feature/F-7` (deleted after merge)
- Merge: the `--no-ff` merge commit "Merge feature/F-7: password recovery and change (AB#711)" on `main`, 2026-09-19
- Validated on screen by the owner: 2026-09-19
- Review: `/agile:review`, 6 majors (5 fixed, 1 deferred to B-4 by change note v2) and 7 minors fixed — see `## Decisions`
- Tests: full suite 363 passed, 0 failed (SharedKernel 12, AppHost 6, Architecture 27, Web 196, Persistence 14, Email 2, Identity 100, Api 6), 27 s; full build 13 s, 0 warnings
- Manual pages: `docs/manual/{en,pt-BR,pt-PT}/password.md` (new), and the index and `sign-in-and-sign-out.md` in the three languages
