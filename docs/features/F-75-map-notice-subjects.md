---
feature: F-75
epic: Subject taxonomy
status: idea
board: 119
version: 1
---
# Map notice subjects to the canonical taxonomy

Technical terms: [glossary](../glossary.md)

## Summary
An admin maps each `NoticeSubject` of an edition to N canonical subjects and topics (ADR-0001 #44), so "Raciocínio Lógico-Matemático" points to Mathematics and Logical Reasoning. The simulators show the notice's labels; analytics and recommendations use the canonical taxonomy.

## Start
- Depends on: F-74.
- Waits on (to start): nothing.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` with `/agile:screen` → `/agile:build`.
- Parallel with: F-77.
