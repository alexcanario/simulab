# Identity — routes

Generated from `docs/api/Simulab.Api.json`. Do not edit.

| Verb | Route | Summary | Responses |
|---|---|---|---|
| `DELETE` | `/api/v1/identity/roles/{id}` | DeleteRole | 200 |
| `DELETE` | `/api/v1/identity/totp` | DisableTotp | 200 |
| `GET` | `/api/v1/identity/legal-documents/{topic}` | GetLegalDocument | 200 |
| `GET` | `/api/v1/identity/permissions` | ListPermissions | 200 |
| `GET` | `/api/v1/identity/profile` | GetProfile | 200 |
| `GET` | `/api/v1/identity/role-changes/filters` | RoleChangeFilters | 200 |
| `GET` | `/api/v1/identity/role-changes` | ListRoleChanges | 200 |
| `GET` | `/api/v1/identity/roles` | ListRoles | 200 |
| `GET` | `/api/v1/identity/session` | GetSession | 200 |
| `GET` | `/api/v1/identity/totp` | GetTotpStatus | 200 |
| `GET` | `/api/v1/identity/users` | ListUsers | 200 |
| `POST` | `/api/v1/identity/account-erasures` | EraseAccount | 200 |
| `POST` | `/api/v1/identity/data-exports` | ExportData | 200 |
| `POST` | `/api/v1/identity/email-verifications/resend` | ResendVerification | 200 |
| `POST` | `/api/v1/identity/email-verifications` | VerifyEmail | 200 |
| `POST` | `/api/v1/identity/password-changes` | ChangePassword | 200 |
| `POST` | `/api/v1/identity/password-reset-requests` | RequestPasswordReset | 200 |
| `POST` | `/api/v1/identity/password-reset-token-checks` | CheckPasswordResetToken | 200 |
| `POST` | `/api/v1/identity/password-resets` | ResetPassword | 200 |
| `POST` | `/api/v1/identity/registrations` | Register | 200 |
| `POST` | `/api/v1/identity/roles` | CreateRole | 200 |
| `POST` | `/api/v1/identity/sign-out` | SignOut | 200 |
| `POST` | `/api/v1/identity/totp/enrolments/confirmations` | ConfirmTotpEnrolment | 200 |
| `POST` | `/api/v1/identity/totp/enrolments` | StartTotpEnrolment | 200 |
| `POST` | `/api/v1/identity/totp/recovery-codes` | RegenerateRecoveryCodes | 200 |
| `PUT` | `/api/v1/identity/profile/preferred-language` | UpdatePreferredLanguage | 200 |
| `PUT` | `/api/v1/identity/profile` | UpdateProfile | 200 |
| `PUT` | `/api/v1/identity/roles/{id}` | UpdateRole | 200 |
| `PUT` | `/api/v1/identity/users/{id}/roles` | SetUserRoles | 200 |
