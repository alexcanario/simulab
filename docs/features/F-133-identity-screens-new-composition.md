---
feature: F-133
epic: Foundation and identity
status: idea
board: 184
version: 1
---
# Identity screens in the new composition

Technical terms: [glossary](../glossary.md)

## Summary
The account pages (`Identity/SignIn.razor`, `SignUp.razor`, `VerifyEmail.razor`, `CheckEmail.razor`, `ForgotPassword.razor`, `ResetPassword.razor`, `GoogleSignUp.razor`, `LegalDocumentPage.razor` on `AuthLayout`, and `Account.razor`, `Security.razor`, `ChangePassword.razor` with their dialogs) take the composition of the Control Alt Systems identity (D-4). The colours and controls come from F-125; this item decides each page's composition, its single orange action and how `AuthLayout` carries the brand.

## Start
- Depends on: F-125
- Waits on (to start): nothing
- Needed to validate: the owner, on screen, signed out and signed in
- Model: unknown — settled at /agile:refine
- Suggested path: `/agile:refine` → `/agile:build`
- Parallel with: F-130, F-131, F-132, F-134 (different pages)
