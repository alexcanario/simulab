---
feature: F-40
epic: Foundation and identity
status: idea
board: 761
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
Status flow: idea -> refining -> approved -> building -> validating -> done
- idea: title, summary and start only (/agile:idea, /agile:epic).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
Remove these comments when the file leaves `idea`.
-->
# Eval suite for model calls

## Summary
Simulab will call the Claude API through `IAiGateway`, but it has no eval suite. Add an evals command that runs at ship and on demand (never in the turn gate), with a pass-rate baseline file and a cost ceiling per run, and add the Evals line to `CLAUDE.md` (agile@canary 0.0.57 template, manual section 14.14).

## Start
- Depends on: the first feature that calls a model through `IAiGateway` (not built yet: no reference in `src/`, see F-23)
- Waits on: unknown — settled at /agile:refine
- Suggested path: unknown — settled at /agile:refine
- Parallel with: unknown — settled at /agile:refine
