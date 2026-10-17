---
feature: F-124
epic: Foundation and identity
status: refining
board: 175
version: 1
---
# Freeze the pt-PT and en app manual

Technical terms: [glossary](../glossary.md)

## Summary
Stop updating the pt-PT and en app manual (`docs/manual/pt-PT/`, `docs/manual/en/`) when a feature ships; pt-BR stays the only maintained manual. Today every ship writes each page in three languages (`Manual locales:` in `CLAUDE.md`, and the manual line of `.claude/rules/agile/definition-of-done.md`), which triples the manual work of every item while Simulab has no real pt-PT or en user. Captured from the owner's question of 2026-10-17. Recommendation (owner's session, 2026-10-17):
- Only stop updating: it is reversible and deletes nothing.
- Put a banner on each frozen page with date and version (for example "Frozen on 2026-10-17, may be outdated; see the pt-BR version").
- Leave the UI locales (`pt-BR, pt-PT, en`) and the missing-key test untouched: the freeze is about the manual, not the product's languages.
- Change `Manual locales:` in `CLAUDE.md` to `pt-BR` and rewrite the manual line of the definition of done; the owner approves the rule change (possibly through `/agile:retro`).
- Unfreeze with one full translation from pt-BR when the first real pt-PT or en user exists. Verified in `references/conventions.md` ("App manual languages"): a folder outside the line is left as it is, and putting the locale back only adds the pages the folder lacks, so stale pages are not refreshed by the plugin and need that full translation.

Open points for `/agile:refine`: whether the manual navigation lets the user pick a language (if it does, the banner matters more); whether to also hide the frozen locales instead of only labelling them (not recommended).

## Start
- Depends on: nothing.
- Waits on (to start): nothing; the rule change in `CLAUDE.md` and the definition of done needs the owner's approval at refinement — owner.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
