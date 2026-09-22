# Jobs — entities

Generated from the EF model. Do not edit.

Right-angle lines need a Mermaid viewer with the ELK layout, such as the VS Code built-in Markdown preview; other viewers draw the same diagram with curved lines.

```mermaid
---
config:
  layout: elk
---
erDiagram
    jobs {
        uuid id PK
        integer attempts
        varchar(2000) last_error
        text payload
        timestamptz run_after
        timestamptz started_at
        integer status
        varchar(100) type
        standard columns "1: audit - see data dictionary"
    }
```
