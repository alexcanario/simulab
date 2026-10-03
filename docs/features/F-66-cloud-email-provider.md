---
feature: F-66
epic: Foundation and identity
status: idea
board: 105
version: 1
---
# Emails leave the cloud environments

Technical terms: [glossary](../glossary.md)

## Summary
Staging and production send real emails (sign-up verification, password reset, farewell) through a cloud email provider; SendGrid is the one planned in `docs/infra.md` (`Email:SendGrid:ApiKey`). Locally Mailpit stays. Testers in Brazil cannot finish sign-up on staging without it. Raised as out of scope by the F-62 refinement, 2026-10-02.

## Start
- Depends on: nothing.
- Waits on (to start): the owner chooses the provider and creates its account — owner.
- Needed to validate: a sending domain or verified sender at the provider — owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-62.
