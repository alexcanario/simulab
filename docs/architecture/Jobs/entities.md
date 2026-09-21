# Jobs — entities

Generated from the EF model. Do not edit.

```mermaid
erDiagram
    jobs {
        uuid id PK
        integer attempts
        timestamp_with_time_zone created_at
        character_varying_2000_ last_error
        text payload
        timestamp_with_time_zone run_after
        timestamp_with_time_zone started_at
        integer status
        character_varying_100_ type
    }
```
