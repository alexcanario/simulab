# Ai — data dictionary

Generated from the EF model. Do not edit. Schema: `ai`.

## ai_calls

Entity: `AiCall`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| id | uuid | no | PK |  |  |
| cost_usd | numeric(18,6) | no |  |  |  |
| created_at | timestamp with time zone | no |  |  |  |
| created_by | uuid | yes |  |  |  |
| deleted_at | timestamp with time zone | yes |  |  |  |
| deleted_by | uuid | yes |  |  |  |
| duration_ms | integer | no |  |  |  |
| error_code | character varying(100) | yes |  |  | max 100 |
| input_price_per_million | numeric(18,6) | no |  |  |  |
| input_tokens | integer | no |  |  |  |
| is_deleted | boolean | no |  |  |  |
| model | character varying(100) | no |  |  | max 100 |
| output_price_per_million | numeric(18,6) | no |  |  |  |
| output_tokens | integer | no |  |  |  |
| purpose | character varying(50) | no |  |  | max 50 |
| started_at | timestamp with time zone | no |  |  |  |
| succeeded | boolean | no |  |  |  |
| tenant_id | uuid | yes |  |  |  |
| updated_at | timestamp with time zone | yes |  |  |  |
| updated_by | uuid | yes |  |  |  |
| user_id | uuid | no |  |  |  |

Indexes:
- `ix_ai_calls_user_started_at` on user_id, started_at
