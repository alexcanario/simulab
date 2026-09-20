---
feature: F-18
epic: Foundation and identity
status: idea
board: 731
version: 1
---
# Job claim index for stale rows

## Summary
The claim query of the job queue (F-13, `JobRunner.ClaimSql`) filters on `(status = Pending AND run_after <= now) OR (status = Running AND started_at <= stale)`. Only the first branch is covered by `ix_jobs_status_run_after_created_at`, so PostgreSQL cannot use the index for the OR and every poll — one every 5 seconds — scans `jobs.jobs` whole. It costs nothing while the table is small, but BR7 lets `Failed` rows accumulate with no cleanup, so the scan grows for good. Either add an index over `(status, started_at)` and let the planner use both, or split the claim into two statements. Found by `/agile:review` on F-13 (2026-09-20) and deferred by the owner: ADR-0001 #20 says the volume is low.
