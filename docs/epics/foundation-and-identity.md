---
epic: foundation-and-identity
status: agreed
board: 690
---
# Foundation and identity

Technical terms: [glossary](../glossary.md)

## Goal
Give Simulab the base every other epic stands on: one UI standard, the app shell, the data and messaging foundation, and accounts with sign-up, sign-in, permissions and privacy rights. Source: `product/brief.md` capabilities 10 and 11, ADR-0001 rounds 1, 3 and 5.

## What exists
Solution skeleton, SharedKernel (`Entity`, `TenantEntity`, `Result`, `AppJson`), Api and Web hosts, Aspire AppHost, language switch (cookie, then browser, then `en`), `SimulabTheme`, resource parity and architecture tests. Simulae's Identity module (157 files, 86 test files) is the source for F-4 to F-11.

## Features
| Id | Feature | Value | Priority | Size | Depends on | Screen design | Status |
|---|---|---|---|---|---|---|---|
| F-1 | UI kit and gallery | One visual behavior before any screen is imported | Must | M | - | no (dev gallery) | done |
| F-2 | App shell and navigation | Side menu, app-wide theme switch, user menu slot, skip link | Must | S | F-1 | yes | idea |
| F-3 | Data and messaging foundation | Aspire resources, audit/soft-delete interceptors, tenant filter, shared test fixture, in-process events | Must | M | - | no | idea |
| F-4 | Sign-up and email verification | Identity module, 18+ sign-up with consent, email verification, terms and privacy pages | Must | L | F-1, F-3 | yes | idea |
| F-5 | Sign-in and sign-out | OpenIddict password + refresh token, sign-in page, sign-out, revocation | Must | L | F-4 | yes | idea |
| F-6 | Permissions and seed roles | Permission catalog, Student/Curator/Admin, checks on endpoint, page and menu | Must | M | F-2, F-5 | no | idea |
| F-7 | Password recovery and change | Forgot, reset, expired link, change password | Must | M | F-5 | no | idea |
| F-8 | My account and preferred language | Profile; profile language becomes the first culture source | Must | M | F-5 | no | idea |
| F-9 | Role management back office | Create, edit, delete roles; permissions per role; assign roles | Must | L | F-1, F-6 | yes | idea |
| F-10 | Account erasure | Anonymize personal data (LGPD/GDPR) and publish an event for other modules | Should | M | F-8 | no | idea |
| F-11 | Google sign-in and TOTP (switched off) | Import both, off by configuration (ADR-0001 #13) | Could | M | F-5 | no | idea |

## Order
1. F-1 — every screen after it is built from the kit.
2. F-2 — the shell every page lives in.
3. F-3 — database, cache, email and events that Identity needs.
4. F-4 — users must exist before they can sign in.
5. F-5 — sign-in unlocks everything that needs a user.
6. F-6 — permissions guard every later screen and endpoint.
7. F-7 — recovery is required before real users arrive.
8. F-8 — profile and preferred language.
9. F-9 — admins manage roles without touching the database.
10. F-10 — privacy right, after the profile exists.
11. F-11 — dormant code, after the first release.

## First release cut
F-1 to F-9 (all Must). F-10 is Should, F-11 is Could and comes after the first release.

## Out of scope
- Admin "terminate all sessions" and contract session limits (institutional; see the Institutions epic, board 703).
- Parental consent for minors (epic, board 702).
- Plans, entitlements and promo codes (epics, boards 696 and 697).
- The job table and worker (arrives with AI-assisted exam import).

## Decisions
- 2026-09-17 — F-3 stays a technical slice without a screen — inside F-4 it would not fit one session (owner, question 1).
- 2026-09-17 — First release cut is F-1 to F-9 — without them nobody signs in or manages roles (owner, question 2).
- 2026-09-17 — Account erasure (F-10) belongs to this epic — Identity owns personal data; other modules subscribe to its event (owner, question 3).
- 2026-09-17 — Google sign-in and TOTP (F-11) are Could, after the first release — importing switched-off code now costs sessions and delivers nothing (owner, question 4).

## Related
- Discussions: none
- Decisions: `docs/decisions/ADR-0001-foundation.md`
