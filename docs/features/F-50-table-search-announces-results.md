---
feature: F-50
epic: Foundation and identity
status: refining
board: 87
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
# Table search announces results

## Summary
The search box of `AppDataTable` gets an accessible description (its placeholder, through `aria-describedby`) and a polite live announcement of how many rows came back after each reload, in every list of the app. Today its accessible name is only "Search" and nothing tells a screen reader user what the search found. Raised by F-36's screen design (owner, 2026-09-29).

## Start
- Depends on: nothing (the kit's `AppDataTable` is in `main`).
- Waits on: nothing.
- Suggested path: `/agile:refine` → `/agile:build`; unknown whether it needs `/agile:screen` — settled at `/agile:refine`.
- Parallel with: unknown — settled at `/agile:refine` (it touches the kit table every list uses, so a feature adding a list may conflict).

## Goal

## Users and use cases
- UC1 <Actor> <does something> <and gets a result>.

## Business rules
- BR1 <Rule>.

## Screens and API
- <Route> — <purpose>

## Acceptance criteria
- AC1 Given <context>, when <action>, then <result>.
- AC<n> All new texts appear in pt-BR, pt-PT and en.

## Decisions
- <YYYY-MM-DD> — <decision> — <reason>

## Out of scope
- <Item>

## Open questions
- (none)

## Change notes

## Validation script
1. <Step> → <expected result>

## Delivery
- Branch: feature/F-50
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
