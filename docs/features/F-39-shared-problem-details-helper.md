---
feature: F-39
epic: Foundation and identity
status: idea
board: 760
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
Status flow: idea -> refining -> approved -> building -> validating -> done
- idea: title and summary only (/agile:idea).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
Remove these comments when the file leaves `idea`.
-->
# One problem-details helper for every module's API

## Summary
`CatalogEndpoints.Problem`/`StatusFor` and `IdentityEndpoints.Problem`/`StatusFor` are the same code: they turn an
`Error` into the problem-details response with its status and its stable `code`. F-33 was the second use, which is
when the profile says to extract the shape. Move it to a small API building block both modules call, so the answer
a caller gets does not depend on which module wrote the endpoint, and a third module inherits it. Raised by the
independent review of F-33 (2026-09-23).
