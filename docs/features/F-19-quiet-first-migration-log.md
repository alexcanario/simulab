---
feature: F-19
epic: Foundation and identity
status: idea
board: 732
version: 1
---
# Quiet the first-migration log

## Summary
On a database that has just been created — a fresh clone, or the local volume deleted — every module context logs `fail: Microsoft.EntityFrameworkCore.Database.Command[20102]` while probing its own `__ef_migrations_history` table, which does not exist yet. EF catches it, treats it as "no migration applied" and creates everything, so nothing is wrong; but the console shows one red failure per context (two since F-13: `identity` and `jobs`) at the exact moment a newcomer is least able to tell noise from a real problem. Look at whether the probe can be quieted in Development without hiding real command failures — an event-id filter on `RelationalEventId.CommandError` during the migration step, or logging the step ourselves around `MigrateAsync`. Reported by the owner on 2026-09-20 after deleting the PostgreSQL volume.
