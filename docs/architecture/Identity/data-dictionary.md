# Identity — data dictionary

Generated from the EF model. Do not edit. Schema: `identity`.

## consent_records

Entity: `ConsentRecord`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| id | uuid | no | PK |  |  |
| accepted_at | timestamp with time zone | no |  |  |  |
| created_at | timestamp with time zone | no |  |  |  |
| created_by | uuid | yes |  |  |  |
| declares_adult | boolean | no |  |  |  |
| deleted_at | timestamp with time zone | yes |  |  |  |
| deleted_by | uuid | yes |  |  |  |
| ip_address | character varying(45) | yes |  |  | max 45 |
| is_deleted | boolean | no |  |  |  |
| locale | character varying(10) | no |  |  | max 10 |
| privacy_version | character varying(40) | no |  |  | max 40 |
| tenant_id | uuid | yes |  |  |  |
| terms_version | character varying(40) | no |  |  | max 40 |
| updated_at | timestamp with time zone | yes |  |  |  |
| updated_by | uuid | yes |  |  |  |
| user_id | uuid | no |  |  |  |

Indexes:
- `ix_consent_records_user_id` on user_id

## email_verification_tokens

Entity: `EmailVerificationToken`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| id | uuid | no | PK |  |  |
| consumed_at | timestamp with time zone | yes |  |  |  |
| created_at | timestamp with time zone | no |  |  |  |
| created_by | uuid | yes |  |  |  |
| deleted_at | timestamp with time zone | yes |  |  |  |
| deleted_by | uuid | yes |  |  |  |
| expires_at | timestamp with time zone | no |  |  |  |
| is_deleted | boolean | no |  |  |  |
| tenant_id | uuid | yes |  |  |  |
| token_hash | character varying(64) | no |  |  | max 64 |
| updated_at | timestamp with time zone | yes |  |  |  |
| updated_by | uuid | yes |  |  |  |
| user_id | uuid | no |  |  |  |

Indexes:
- `ix_email_verification_tokens_user_id` on user_id
- `ux_email_verification_tokens_hash` on token_hash (unique)

## jobs

Entity: `Job`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| id | uuid | no | PK |  |  |
| attempts | integer | no |  |  |  |
| created_at | timestamp with time zone | no |  |  |  |
| last_error | character varying(2000) | yes |  |  | max 2000 |
| payload | text | no |  |  |  |
| run_after | timestamp with time zone | no |  |  |  |
| started_at | timestamp with time zone | yes |  |  |  |
| status | integer | no |  |  |  |
| type | character varying(100) | no |  |  | max 100 |

Indexes:
- `ix_jobs_status_run_after_created_at` on status, run_after, created_at

## openiddict_applications

Entity: `OpenIddictEntityFrameworkCoreApplication`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| id | text | no | PK |  |  |
| application_type | character varying(50) | yes |  |  | max 50 |
| client_id | character varying(100) | yes |  |  | max 100 |
| client_secret | text | yes |  |  |  |
| client_type | character varying(50) | yes |  |  | max 50 |
| concurrency_token | character varying(50) | yes |  |  | max 50 |
| consent_type | character varying(50) | yes |  |  | max 50 |
| display_name | text | yes |  |  |  |
| display_names | text | yes |  |  |  |
| json_web_key_set | text | yes |  |  |  |
| permissions | text | yes |  |  |  |
| post_logout_redirect_uris | text | yes |  |  |  |
| properties | text | yes |  |  |  |
| redirect_uris | text | yes |  |  |  |
| requirements | text | yes |  |  |  |
| settings | text | yes |  |  |  |

Indexes:
- `ix_openiddict_applications_client_id` on client_id (unique)

## openiddict_authorizations

Entity: `OpenIddictEntityFrameworkCoreAuthorization`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| id | text | no | PK |  |  |
| application_id | text | yes | FK → openiddict_applications |  |  |
| concurrency_token | character varying(50) | yes |  |  | max 50 |
| creation_date | timestamp with time zone | yes |  |  |  |
| properties | text | yes |  |  |  |
| scopes | text | yes |  |  |  |
| status | character varying(50) | yes |  |  | max 50 |
| subject | character varying(400) | yes |  |  | max 400 |
| type | character varying(50) | yes |  |  | max 50 |

Indexes:
- `ix_openiddict_authorizations_application_id_status_subject_type` on application_id, status, subject, type

## openiddict_scopes

Entity: `OpenIddictEntityFrameworkCoreScope`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| id | text | no | PK |  |  |
| concurrency_token | character varying(50) | yes |  |  | max 50 |
| description | text | yes |  |  |  |
| descriptions | text | yes |  |  |  |
| display_name | text | yes |  |  |  |
| display_names | text | yes |  |  |  |
| name | character varying(200) | yes |  |  | max 200 |
| properties | text | yes |  |  |  |
| resources | text | yes |  |  |  |

Indexes:
- `ix_openiddict_scopes_name` on name (unique)

## openiddict_tokens

Entity: `OpenIddictEntityFrameworkCoreToken`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| id | text | no | PK |  |  |
| application_id | text | yes | FK → openiddict_applications |  |  |
| authorization_id | text | yes | FK → openiddict_authorizations |  |  |
| concurrency_token | character varying(50) | yes |  |  | max 50 |
| creation_date | timestamp with time zone | yes |  |  |  |
| expiration_date | timestamp with time zone | yes |  |  |  |
| payload | text | yes |  |  |  |
| properties | text | yes |  |  |  |
| redemption_date | timestamp with time zone | yes |  |  |  |
| reference_id | character varying(100) | yes |  |  | max 100 |
| status | character varying(50) | yes |  |  | max 50 |
| subject | character varying(400) | yes |  |  | max 400 |
| type | character varying(150) | yes |  |  | max 150 |

Indexes:
- `ix_openiddict_tokens_application_id_status_subject_type` on application_id, status, subject, type
- `ix_openiddict_tokens_authorization_id` on authorization_id
- `ix_openiddict_tokens_reference_id` on reference_id (unique)

## password_reset_tokens

Entity: `PasswordResetToken`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| id | uuid | no | PK |  |  |
| consumed_at | timestamp with time zone | yes |  |  |  |
| created_at | timestamp with time zone | no |  |  |  |
| created_by | uuid | yes |  |  |  |
| deleted_at | timestamp with time zone | yes |  |  |  |
| deleted_by | uuid | yes |  |  |  |
| expires_at | timestamp with time zone | no |  |  |  |
| is_deleted | boolean | no |  |  |  |
| tenant_id | uuid | yes |  |  |  |
| token_hash | character varying(64) | no |  |  | max 64 |
| updated_at | timestamp with time zone | yes |  |  |  |
| updated_by | uuid | yes |  |  |  |
| user_id | uuid | no |  |  |  |

Indexes:
- `ix_password_reset_tokens_user_id` on user_id
- `ux_password_reset_tokens_hash` on token_hash (unique)

## permissions

Entity: `Permission`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| name | character varying(100) | no | PK |  | max 100 |
| description | character varying(500) | yes |  |  | max 500 |

## role_changes

Entity: `RoleChange`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| id | uuid | no | PK |  |  |
| action | character varying(40) | no |  |  | max 40 |
| created_at | timestamp with time zone | no |  |  |  |
| created_by | uuid | yes |  |  |  |
| deleted_at | timestamp with time zone | yes |  |  |  |
| deleted_by | uuid | yes |  |  |  |
| is_deleted | boolean | no |  |  |  |
| name_after | character varying(256) | yes |  |  | max 256 |
| name_before | character varying(256) | yes |  |  | max 256 |
| role_id | uuid | yes |  |  |  |
| role_ids | uuid[] | no |  |  |  |
| role_name | character varying(256) | yes |  |  | max 256 |
| target_user_id | uuid | yes |  |  |  |
| tenant_id | uuid | yes |  |  |  |
| updated_at | timestamp with time zone | yes |  |  |  |
| updated_by | uuid | yes |  |  |  |
| added | jsonb | yes |  |  | JSON: RoleChangeItem (Key, Name) |
| removed | jsonb | yes |  |  | JSON: RoleChangeItem (Key, Name) |

Indexes:
- `ix_role_changes_created_at` on created_at
- `ix_role_changes_created_by` on created_by
- `ix_role_changes_role_ids` on role_ids
- `ix_role_changes_target_user_id` on target_user_id

## role_claims

Entity: `IdentityRoleClaim`1`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| id | integer | no | PK |  |  |
| claim_type | text | yes |  |  |  |
| claim_value | text | yes |  |  |  |
| role_id | uuid | no |  |  |  |

Indexes:
- `ix_role_claims_role_id` on role_id

## role_permissions

Entity: `RolePermission`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| permission_name | character varying(100) | no | PK |  |  |
| role_id | uuid | no | PK |  |  |

Indexes:
- `ix_role_permissions_permission_name` on permission_name

## roles

Entity: `Role`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| id | uuid | no | PK |  |  |
| concurrency_stamp | text | yes |  |  |  |
| created_at | timestamp with time zone | no |  |  |  |
| created_by | uuid | yes |  |  |  |
| deleted_at | timestamp with time zone | yes |  |  |  |
| deleted_by | uuid | yes |  |  |  |
| is_deleted | boolean | no |  |  |  |
| is_system | boolean | no |  |  |  |
| name | character varying(256) | yes |  |  | max 256 |
| normalized_name | character varying(256) | yes |  |  | max 256 |
| updated_at | timestamp with time zone | yes |  |  |  |
| updated_by | uuid | yes |  |  |  |

Indexes:
- `ux_roles_normalized_name` on normalized_name (unique)

## user_claims

Entity: `IdentityUserClaim`1`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| id | integer | no | PK |  |  |
| claim_type | text | yes |  |  |  |
| claim_value | text | yes |  |  |  |
| user_id | uuid | no |  |  |  |

Indexes:
- `ix_user_claims_user_id` on user_id

## user_logins

Entity: `IdentityUserLogin`1`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| login_provider | character varying(128) | no | PK |  | max 128 |
| provider_key | character varying(128) | no | PK |  | max 128 |
| provider_display_name | text | yes |  |  |  |
| user_id | uuid | no |  |  |  |

## user_roles

Entity: `IdentityUserRole`1`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| role_id | uuid | no | PK |  |  |
| user_id | uuid | no | PK |  |  |

Indexes:
- `ix_user_roles_role_id` on role_id

## user_tokens

Entity: `IdentityUserToken`1`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| login_provider | character varying(128) | no | PK |  | max 128 |
| name | character varying(128) | no | PK |  | max 128 |
| user_id | uuid | no | PK |  |  |
| value | text | yes |  |  |  |

## users

Entity: `User`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| id | uuid | no | PK |  |  |
| access_failed_count | integer | no |  |  |  |
| concurrency_stamp | text | yes |  |  |  |
| created_at | timestamp with time zone | no |  |  |  |
| created_by | uuid | yes |  |  |  |
| deleted_at | timestamp with time zone | yes |  |  |  |
| deleted_by | uuid | yes |  |  |  |
| email | character varying(254) | no |  |  | max 254 |
| email_confirmed | boolean | no |  |  |  |
| email_verified_at | timestamp with time zone | yes |  |  |  |
| full_name | character varying(120) | yes |  |  | max 120 |
| is_adult_declared | boolean | no |  |  |  |
| is_deleted | boolean | no |  |  |  |
| lockout_enabled | boolean | no |  |  |  |
| lockout_end | timestamp with time zone | yes |  |  |  |
| normalized_email | character varying(254) | no |  |  | max 254 |
| normalized_user_name | character varying(254) | yes |  |  | max 254 |
| password_hash | text | yes |  |  |  |
| phone_number | text | yes |  |  |  |
| phone_number_confirmed | boolean | no |  |  |  |
| preferred_language | character varying(10) | no |  |  | max 10 |
| security_stamp | text | yes |  |  |  |
| status | character varying(20) | no |  |  | max 20 |
| tenant_id | uuid | yes |  |  |  |
| totp_enabled_at | timestamp with time zone | yes |  |  |  |
| totp_last_accepted_step | bigint | yes |  |  |  |
| totp_secret_encrypted | character varying(256) | yes |  |  | max 256 |
| two_factor_enabled | boolean | no |  |  |  |
| updated_at | timestamp with time zone | yes |  |  |  |
| updated_by | uuid | yes |  |  |  |
| user_name | character varying(254) | yes |  |  | max 254 |

Indexes:
- `ux_users_tenant_normalized_email` on tenant_id, normalized_email (unique, NULLS NOT DISTINCT)
- `ux_users_tenant_normalized_user_name` on tenant_id, normalized_user_name (unique, NULLS NOT DISTINCT)
