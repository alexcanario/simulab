---
feature: F-40
epic: Foundation and identity
status: idea
board: 79
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

Technical terms: [glossary](../glossary.md)

## Summary
Simulab will call the Claude API through `IAiGateway`, but it has no eval suite. Add an evals command that runs at ship and on demand (never in the turn gate), with a pass-rate baseline file and a cost ceiling per run, and add the Evals line to `CLAUDE.md` (agile@canary 0.0.57 template, manual section 14.14).

## Start
- Depends on: F-41 (AI gateway) — done. The first product feature that calls a model (AI-assisted exam import or the AI coach, brief items 4 and 9) — no item exists for either yet.
- Waits on (to start): that feature's first real prompt, with its own `AiPurposes` value; today `AiPurposes` holds only `Diagnostics` (the `/dev/ai` page).
- Suggested path: refine together with, or right after, the first feature that calls a model; measure one case once before fixing runs per case and the cost ceiling.
- Parallel with: unknown — settled when it is refined again.

## Decisions
- 2026-10-01 — Refinement paused, item back to `idea` (owner). Reason: no product feature calls a model yet; the only caller is the development diagnostics page, so a pass-rate baseline and a cost ceiling measured now would not describe any real prompt. Also found: `claude plugin eval` evaluates plugins, not the app's own calls, so the runner will be the app's own (tests that call the real API, outside the turn gate).
- 2026-10-06 — Confirmed by the owner: F-40 stays blocked until the first feature that calls a model enters; it is skipped as "next in the backlog" until then.
- 2026-10-02 — Refinement asked again and kept at `idea` (owner, option A). Reason: `AiPurposes` still holds only `Diagnostics` and no item exists yet for a product feature that calls a model; F-40 is refined together with that first feature.
- 2026-10-05 — Refinement asked a third time and kept at `idea` (owner). Reason: `AiPurposes` still holds only `Diagnostics` in the main checkout and in every item worktree, and epic E-9 (AI-assisted exam import) is still an unbroken `idea` epic; no worktree or branch was created. F-40 is refined together with the first E-9 feature that calls a model.
