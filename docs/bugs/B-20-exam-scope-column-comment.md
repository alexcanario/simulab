---
bug: B-20
feature: F-34
status: idea
board: 775
severity: low
---
# Exam scope column comment names values that do not exist

## What happens
1. `ExamConfiguration.cs:45` gives the `catalog.exams.scope` column the comment "How far the exam reaches: Federal, State, Municipal, National or International."
2. `ExamScope` has only `National`, `State` and `Municipal` (F-34 BR7).
3. The wrong comment reaches the database through migration `20260925171641_AddColumnDescriptions.cs:434` and the model snapshot (`CatalogModuleDbContextModelSnapshot.cs:97`), and the generated technical docs (`docs/architecture/Catalog/data-dictionary.md:22`).

Evidence: `grep -rn "National or International" --include=*.cs --include=*.md .` on `main` at 0f0d484 lists those four files. Found during the F-35 refinement on 2026-09-28; the owner asked to capture it.

## Start
- Depends on: nothing.
- Waits on: nothing.
- Suggested path: `/agile:autopilot B-20` (cause clear, fix small: the comment, a migration that changes it, `DocGen` regenerated). F-35 adds a migration to the same module, so either goes after the other.
- Parallel with: unknown — settled at /agile:refine.

## Expected

## Cause

## Fix

## Regression test

## Open questions
- (none)

## Validation script

## Delivery
- Branch: bug/B-20
- Merge: <commit>
