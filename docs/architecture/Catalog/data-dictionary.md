# Catalog — data dictionary

Generated from the EF model. Do not edit. Schema: `catalog`.

## exams

Entity: `Exam`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| id | uuid | no | PK |  |  |
| assessment_type | character varying(40) | no |  |  | max 40 |
| content_language | character varying(16) | no |  |  | max 16 |
| created_at | timestamp with time zone | no |  |  |  |
| created_by | uuid | yes |  |  |  |
| deleted_at | timestamp with time zone | yes |  |  |  |
| deleted_by | uuid | yes |  |  |  |
| is_deleted | boolean | no |  |  |  |
| issuing_authority_id | uuid | no | FK → organizers |  |  |
| name | character varying(200) | no |  |  | max 200 |
| normalized_name | character varying(200) | no |  |  | max 200 |
| scope | character varying(40) | no |  |  | max 40 |
| scope_detail | character varying(120) | yes |  |  | max 120 |
| tenant_id | uuid | yes |  |  |  |
| updated_at | timestamp with time zone | yes |  |  |  |
| updated_by | uuid | yes |  |  |  |

Indexes:
- `ix_exams_issuing_authority` on issuing_authority_id
- `ux_exams_tenant_authority_normalized_name` on tenant_id, issuing_authority_id, normalized_name (unique, NULLS NOT DISTINCT)

## organizers

Entity: `Organizer`

| Column | Type | Null | Key | Default | Notes |
|---|---|---|---|---|---|
| id | uuid | no | PK |  |  |
| acronym | character varying(20) | no |  |  | max 20 |
| created_at | timestamp with time zone | no |  |  |  |
| created_by | uuid | yes |  |  |  |
| deleted_at | timestamp with time zone | yes |  |  |  |
| deleted_by | uuid | yes |  |  |  |
| description | character varying(500) | yes |  |  | max 500 |
| is_deleted | boolean | no |  |  |  |
| kind | character varying(40) | no |  |  | max 40 |
| name | character varying(150) | no |  |  | max 150 |
| normalized_acronym | character varying(20) | no |  |  | max 20 |
| normalized_name | character varying(150) | no |  |  | max 150 |
| tenant_id | uuid | yes |  |  |  |
| updated_at | timestamp with time zone | yes |  |  |  |
| updated_by | uuid | yes |  |  |  |
| website | character varying(300) | yes |  |  | max 300 |

Indexes:
- `ux_organizers_tenant_normalized_acronym` on tenant_id, normalized_acronym (unique, NULLS NOT DISTINCT)
- `ux_organizers_tenant_normalized_name` on tenant_id, normalized_name (unique, NULLS NOT DISTINCT)
