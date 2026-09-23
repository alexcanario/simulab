---
feature: F-19
epic: Foundation and identity
status: done
board: 732
version: 1
---
# Quiet the first-migration log

## Summary
On a database that has just been created — a fresh clone, or the local volume deleted — every module context logs `fail: Microsoft.EntityFrameworkCore.Database.Command[20102]` while probing its own `__ef_migrations_history` table, which does not exist yet. EF catches it, treats it as "no migration applied" and creates everything, so nothing is wrong; but the console shows one red failure per context (two since F-13: `identity` and `jobs`) at the exact moment a newcomer is least able to tell noise from a real problem. Look at whether the probe can be quieted in Development without hiding real command failures — an event-id filter on `RelationalEventId.CommandError` during the migration step, or logging the step ourselves around `MigrateAsync`. Reported by the owner on 2026-09-20 after deleting the PostgreSQL volume.

## Goal
A first start on an empty database shows no `fail` line that is not a real failure, and a real failure during migration is still logged as `fail`.

## What exists today
- Two module contexts migrate on start, in Development only (`Database:ApplyMigrationsOnStart`): `MigrateJobsAsync` (`src/BuildingBlocks/Simulab.Jobs/JobsServiceCollectionExtensions.cs:53`) and `MigrateIdentityModuleAsync` (`src/Modules/Identity/Simulab.Identity.Infrastructure/IdentityModule.cs:198`), called from `src/Hosts/Simulab.Api/Program.cs:90-91`. Both are the same three lines: scope, context, `Database.MigrateAsync`.
- Each context keeps its history table in its own schema through `UseModuleHistoryTable` (`src/BuildingBlocks/Simulab.Persistence/PersistenceServiceCollectionExtensions.cs:42`).
- Packages: `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3, EF Core 10.0.3.

## Cause
Reproduced on 2026-09-23 with the Jobs context against an empty PostgreSQL 17 test container (scratch console, outside the repository):

```
fail: Microsoft.EntityFrameworkCore.Database.Command[20102] Failed executing DbCommand (15ms) [...] SELECT migration_id, product_version FROM jobs.__ef_migrations_history ORDER BY migration_id;
info: Microsoft.EntityFrameworkCore.Migrations[20402] Applying migration '20260920152540_InitialJobs'.
```

- The Npgsql provider does not check whether the history table exists: `NpgsqlHistoryRepository.Exists()` always returns `true`, and `GetAppliedMigrationsAsync` runs the `SELECT` and catches `42P01 undefined_table` (efcore.pg v10.0.3, `src/EFCore.PG/Migrations/Internal/NpgsqlHistoryRepository.cs:282-291`, "we catch this rather than try to detect whether the migration table already exists").
- The command logger writes `CommandError` (20102, level Error) before the provider catches the exception, so the log shows a failure that the provider then treats as "no migration applied".
- On a database already migrated the same `SELECT` succeeds, so the line appears only on the first start.
- Duplicates: the probe is in the provider, not in our code; our two migrate helpers are the only callers (listed above). No reimplementation found.

Checked in the same scratch run: calling the public `IHistoryRepository.CreateIfNotExistsAsync()` before `MigrateAsync` makes the first start log no `fail` line, both migrations apply, and a real bad command (`SELECT * FROM jobs.no_such_table`) is still logged as `fail ... [20102]` and still throws.

## Users and use cases
- UC1 A developer starts the app host on an empty database (fresh clone or deleted volume) and the console shows the migrations applied, with no failure line.
- UC2 A developer starts the app host and a migration really fails: the console shows the failure as `fail` and the start stops, as today.

## Business rules
- BR1 Before applying a module's migrations, its history table is created if it does not exist, so the provider's probe never meets a missing table.
- BR2 No log filter lowers `CommandError` or the `Microsoft.EntityFrameworkCore.Database.Command` category: a real command failure keeps its `fail` level.
- BR3 One shared helper applies a module context's migrations; the Jobs and Identity helpers call it.

## Screens and API
- No screen, no endpoint. Console log only.

## Acceptance criteria
- AC1 Given an empty database, when the Jobs and Identity migrations are applied, then no log entry at level Error or above is written, and every migration of both contexts is in its history table. (UC1, BR1)
- AC2 Given a database already migrated, when the migrations are applied again, then nothing is applied and no log entry at level Error or above is written. (UC1)
- AC3 Given a module context whose migration fails, when the migrations are applied, then a `CommandError` entry at level Error is logged and the call throws. (UC2, BR2)
- AC4 The Jobs and Identity migrate helpers go through the shared helper. (BR3)
- No UI text: the localization criterion does not apply.

## Decisions
- 2026-09-23 — The fix is in our migrate step, not in logging configuration — a filter on the command category or on `CommandError` would also hide a real migration failure (BR2).
- 2026-09-23 — The criteria are covered by integration tests on the existing PostgreSQL test container — the probe only fails against a real empty database.
- 2026-09-23 — Owner: create the history table before migrating (not a log filter) — no filter, and a real failure stays `fail`; already checked in the scratch run.
- 2026-09-23 — Owner: the Information-level SQL echo on start stays as it is — it helps debugging, and the request was only the false `fail`.
- 2026-09-23 — `docs/infra.md` loses the "it is noise" note about the first start — the note describes behavior this item removes.
- 2026-09-23 — Build: the shared helper is `ModuleMigrationExtensions.MigrateModuleAsync<TContext>` in `Simulab.Persistence`; it calls EF's public `IHistoryRepository.CreateIfNotExistsAsync` before `MigrateAsync` — the same call EF makes itself, only earlier.
- 2026-09-23 — Build: AC1/AC2 start the real Api (`IdentityApiFactory`, migrations on start) with a recording logger, so they go through `Program.cs` and both module helpers; that also covers AC4 by its effect. Seen failing without the fix (`Failed: 1, Passed: 1`).
- 2026-09-23 — Build: the app-host check (empty volume) is in the validation script, not run by Claude — deleting the local volume is the owner's data (workflow rule: never reset a volume outside the test containers).

## Out of scope
- The Information-level echo of every SQL command on start (`Database.Command[20101]`) — owner, 2026-09-23.
- How a release applies migrations (pipeline, not the app start).

## Open questions
- (none)

## Change notes

## Validation script
Step 2 erases every local account and datum in the PostgreSQL volume. No screen, so no language, permission or keyboard step.

1. Stop any running app host (Ctrl+C) → no `simulab` container is running: `docker ps --filter name=postgres` (Git Bash and PowerShell 7) prints only the header line (`CONTAINER ID   IMAGE ...`).
2. Delete the database volume: `docker volume rm simulab-postgres-data` (Git Bash and PowerShell 7) → prints `simulab-postgres-data`. If it says "volume is in use", repeat step 1.
3. Start the app host from the F-19 worktree.
   - Git Bash: `cd /d/dev/_icontrol/wt/simulab/F-19 && dotnet run --project src/Hosts/Simulab.AppHost`
   - PowerShell 7: `cd D:\dev\_icontrol\wt\simulab\F-19; dotnet run --project src/Hosts/Simulab.AppHost`
4. In the Aspire dashboard, open the `api` resource's console log → `Applying migration '20260920152540_InitialJobs'` and the Identity migrations appear, and there is no `fail` line (no `Database.Command[20102]`).
5. Open the `web` URL from the dashboard → the home page loads.
6. Stop the app host (Ctrl+C) and start it again as in step 3 → the `api` log shows `No migrations were applied. The database is already up to date.` twice, and no `fail` line.
7. Stop the app host (Ctrl+C) before `/agile:ship`.

Validated by the owner on 2026-09-23: the script passed.

## Delivery
- Branch: `feature/F-19`, built in the worktree `D:\dev\_icontrol\wt\simulab\F-19`
- Merge: `b7c7863` on `main` (2026-09-23, authorized by the owner)
- Tests: 804 passed, 0 failed, suite 46 s, full build 21 s, 0 warnings (`gate.js ship`, 2026-09-23)
- Manual pages: none (no visible behaviour changed). `docs/infra.md` updated (first-start note, measured times); `DocGen --check` up to date
