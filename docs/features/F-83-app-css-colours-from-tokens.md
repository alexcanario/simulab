---
feature: F-83
epic: Foundation and identity
status: idea
board: 126
version: 1
---
# The stylesheet paints colours from the identity tokens

Technical terms: [glossary](../glossary.md)

## Summary
`src/Hosts/Simulab.Web/wwwroot/app.css` still carries literal colours from the Blazor template: the validation outlines (`.valid.modified` `#26b050`, `.invalid` `#e50000`), the validation message (`#e50000`), the error boundary banner (`#b32121`) and the checkbox border (`#929292`). They should come from the theme tokens recorded by F-63 (`docs/design/identity.tokens.json`, `SimulabTheme`). The QR code's white background (`.app-security-qr`) is deliberate (scan quiet zone) and stays. Raised in the F-63 refinement on 2026-10-04; the owner kept it out of F-63.

## Start
- Depends on: F-63 (the identity tokens).
- Waits on (to start): nothing.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
