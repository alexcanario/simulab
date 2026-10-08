---
feature: F-89
epic: Assessment catalog
status: idea
board: 132
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
Status flow: idea -> refining -> approved -> building -> validating -> done
- cancelled: exit from idea or refining only, as a duplicate; the file stays, with "Duplicate of <id> (<date>): <where the improvement went>" under the header.
- idea: title, summary and start only (/agile:idea, /agile:epic).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
Remove these comments when the file leaves `idea`.
-->
# Upload the notice PDF of an edition

Technical terms: [glossary](../glossary.md)

## Summary
In the back office, on the exam edition screen (F-35), the curator can upload the edition's notice as a PDF file, besides the official link (`NoticeUrl`) stored today. This is the notice document of the edition itself, not the exam paper uploaded for the AI-assisted import of epic E-9: the owner separated the two on 2026-10-04, which revises the F-35 note "the upload arrives with epic 698" for the notice document.

## Start
- Depends on: F-35 (done: the edition screen and `NoticeUrl`).
- Waits on (to start): nothing known. No `IFileStorage` implementation exists in `src/` today (`grep IFileStorage src` finds nothing), so this item may be the one that brings `Simulab.Storage` (Azurite in dev) — settled at /agile:refine.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build`; `/agile:screen` only if the edition screen changes beyond a file field.
- Parallel with: unknown — settled at /agile:refine.

## Goal

## Users and use cases

## Business rules

## Screens and API

## Acceptance criteria

## Decisions

## Out of scope

## Open questions
- (none)

## Change notes

## Validation script

## Delivery
