---
epic: <slug>
status: draft
board: <work item id or ->
---
<!--
One file per epic. Save as: docs/epics/<slug>.md
Status: draft -> agreed (owner said so) -> done (every feature done or dropped).
Created by /agile:epic. Features are refined one at a time with /agile:refine.
-->
# <Epic name>

Technical terms: [glossary](../glossary.md)

## Goal
<!-- The business outcome, in one or two sentences. -->

## Start
<!-- What the epic as a whole needs before its first feature can start. -->
- Depends on: <other epics or items — or "nothing">
- Waits on: <owner decisions, people or teams, access, environments — who provides each — or "nothing">

## Features
| Id | Feature | Value | Priority | Size | Depends on | Waits on | Screen design | Status |
|---|---|---|---|---|---|---|---|---|
| F-<n> | <title> | <one line> | Must / Should / Could | S / M / L | <ids or -> | <decision, person, access or -> | yes / no | idea |

## Execution plan
<!-- One line per feature, in order: why here, how to run it, what can run beside it. -->
1. F-<n> — <why first> — path: <`/agile:refine` → `/agile:build` | `/agile:autopilot` | `/agile:discuss` first | `/agile:screen` in refinement> — parallel with: <ids or none>

## Waiting outside the epic
<!-- Everything a feature waits on that is not an item: what, for which features, who provides it, who asks for it. -->
| What | For | Who provides it | Who asks |
|---|---|---|---|
| <decision / access / environment / team> | F-<n> | <person or team> | <owner or Claude> |

## First release cut
<!-- Which features must be done before the first release. -->

## Out of scope
- <Item>

## Decisions
- <YYYY-MM-DD> — <decision> — <reason>

## Related
- Discussions: <D-<n>>
