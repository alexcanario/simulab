# Jobs — data dictionary

Generated from the EF model. Do not edit. Schema: `jobs`.

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
- `ix_jobs_active_created_at` on created_at
