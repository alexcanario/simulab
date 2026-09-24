# Catalog — data dictionary

Generated from the EF model. Do not edit. Schema: `catalog`.

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
