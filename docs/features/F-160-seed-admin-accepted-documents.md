---
feature: F-160
epic: test-infrastructure
status: idea
board: 219
version: 1
---
# Seed the development admin with the legal documents accepted

Technical terms: [glossary](../glossary.md)

## Summary
In Development, the seeded admin account (`admin@simulab.local`, `SeedAdmin`) should start with the current legal documents already accepted, so that a signed-in screen can be reached and checked on the app host right after sign-in, with no click on "Accept and continue" and no terms acceptance by whoever drives the browser. Seen at the build of F-108: on a fresh database, signing in lands on `/accept-terms` ("Our documents changed"), and an automated screen check stops there because it must not accept terms. Source: retro of F-108, lesson 3.

## Start
- Depends on: nothing.
- Waits on (to start): nothing. The shape (a seed step beside `SeedAdmin`, only when the environment is Development) is settled at `/agile:refine`.
- Needed to validate: the local app host — owner.
- Model: unknown — settled at `/agile:refine`
- Suggested path: `/agile:refine` → `/agile:build`
- Parallel with: none known.

Evidence: after `Database__Name=simulab_f108 dotnet run --project src/Hosts/Simulab.AppHost`, signing in as the seeded admin on the new database redirected to `https://localhost:7125/accept-terms` with the heading "Our documents changed" (2026-10-24, F-108 build). Cause of the page not verified beyond that observation: measure which acceptance rows the sign-in gate reads at `/agile:refine`.
