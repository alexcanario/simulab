---
feature: F-133
epic: Interface and screen composition
status: idea
board: 184
version: 1
---
# Identity screens in the new composition

Technical terms: [glossary](../glossary.md)

## Summary
The account pages (`Identity/SignIn.razor`, `SignUp.razor`, `VerifyEmail.razor`, `CheckEmail.razor`, `ForgotPassword.razor`, `ResetPassword.razor`, `GoogleSignUp.razor`, `LegalDocumentPage.razor` on `AuthLayout`, and `Account.razor`, `Security.razor`, `ChangePassword.razor` with their dialogs) take the composition of the Control Alt Systems identity (D-4). The colours and controls come from F-125; this item decides each page's composition, its single orange action and how `AuthLayout` carries the brand.

Added 2026-10-24: the terms acceptance page (`Identity/AcceptTerms.razor`, the page `LegalAcceptanceGate` sends a signed-in user to when a legal document has a new version) is part of this item; no other composition item names it.

## Start
- Depends on: F-125
- Palette note (2026-10-22): the identity moved to palette v2 (design system handoff H-04); F-148 takes it to the theme. Refine this item after F-148 so mockups use the new values and the amber action.
- Waits on (to start): nothing
- Needed to validate: the owner, on screen, signed out and signed in
- Model: unknown — settled at /agile:refine
- Suggested path: `/agile:refine` → `/agile:build`
- Parallel with: F-130, F-131, F-132, F-134 (different pages)
