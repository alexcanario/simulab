---
feature: F-15
epic: Foundation and identity
status: idea
board: 728
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
Status flow: idea -> refining -> approved -> building -> validating -> done
- idea: title and summary only (/agile:idea).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
Remove these comments when the file leaves `idea`.
-->
# Generated technical docs

## Summary
Adopt the plugin's DocGen tool, which arrived in agile@canary 0.0.21, after this project was bootstrapped: copy `templates/dotnet/DocGen/` to `tools/Simulab.DocGen/`, reference the projects that own a `DbContext`, and generate `docs/architecture/` from the code — entity diagrams and a data dictionary per module (from the EF model), a route map per area (from the OpenAPI document) and a module diagram (from project references), all Mermaid. From then on `/agile:ship` regenerates them and `--check` fails when they are stale, and the definition of done covers it. Refinement decides which of the four documents to generate and whether the hand-written architecture overview (C4 context and containers) comes now or later.

## Goal
<!-- Why this feature exists, in one or two sentences. -->

## Users and use cases
- UC1 <Actor> <does something> <and gets a result>.

## Business rules
- BR1 <rule>.

## Screens and API
<!-- Routes under /api/v1/, error codes <area>.<error>, screens and states. -->

## Acceptance criteria
- AC1 Given <context>, when <action>, then <result>.

## Decisions
<!-- date - decision - reason -->

## Out of scope
<!-- What this feature does not do. -->

## Open questions
<!-- Each one blocks approval until answered or deferred with owner and date. -->
