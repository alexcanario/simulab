---
feature: F-103
epic: Question bank
status: idea
board: 149
version: 1
---
# Images in questions

Technical terms: [glossary](../glossary.md)

## Summary
The curator adds images to the statement, the base text and the options; the files are stored through `IFileStorage` (ADR-0001 #23) and served to students. Simulae has no file storage: its images were inline HTML. Source: epic Question bank (E-4), brief capability 3 "base text and images".

## Start
- Depends on: F-95; F-89 (creates `IFileStorage` and Azurite).
- Waits on (to start): nothing beyond F-89.
- Needed to validate: the local app host with Docker running and a few images — owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-104.
