---
feature: F-64
epic: Foundation and identity
status: idea
board: 103
version: 1
---
# Staging runs on Azure for testers in Brazil

## Summary
Provision the staging environment on Azure Container Apps (Brazil South) with the topology F-62's ADR records, and run the first deploy, so testers in Brazil can use the app. Staging is started for test windows and parked outside them with the start and stop commands F-62 declares. Measure the real monthly cost against the US$ 80 ceiling of F-62's ADR; above it, the ADR's plan B (one Linux VM with Docker Compose) applies. Also here: persist the Data Protection keys and set `ForwardedHeaders` for the cloud ingress. Raised as out of scope by the F-62 refinement, 2026-10-02.

## Start
- Depends on: F-62 (ADR, AppHost ready for Azure, declared commands); F-66 (cloud email: testers must receive the sign-up verification email).
- Waits on (to start): the owner decides the first test window — owner.
- Needed to validate: an Azure subscription with billing (owner) and at least one tester in Brazil — owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
