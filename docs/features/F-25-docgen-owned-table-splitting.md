---
feature: F-25
epic: Foundation and identity
status: idea
board: 743
version: 1
---
# DocGen owned types sharing a table

## Summary
An owned type mapped without `ToJson` (table splitting, `OwnsOne` stored in the owner's table) would still be rendered by DocGen as a second section with the owner's table name, like B-12 did for JSON. B-12 fixes only the JSON case, the one real case today (`RoleChangeItem`). When a module adds a table-split owned type, DocGen should merge its columns into the owner's table section and box. Deferred from B-12 by the owner (2026-09-22): no real case to test yet.
