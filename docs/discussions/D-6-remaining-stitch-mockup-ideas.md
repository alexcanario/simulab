---
discussion: D-6
status: decided
date: 2026-10-24
---
# Remaining ideas of the Stitch question form mockup

Technical terms: [glossary](../glossary.md)

## Idea
The owner wants the rest of the Stitch reference screen of the question form (F-95 redesigned, D-4) in the app. D-4 took its identity and composition (F-125, F-126, F-148, done) and captured five actions as ideas (F-135 to F-138, F-149); the owner listed what is still missing.

## What we already know
- F-135 (sticky section index), F-136 (inline student preview), F-137 (readiness meter), F-138 (keyboard shortcuts) and F-149 (option rows as keys) are ideas from D-4 and F-126.
- D-4 left out the screen's invented items (review queue, autosave, author, pool target, notifications, global search, peer sign-off) and kept the field icons of the current kit.
- The review queue exists (F-105, done: `ReviewQueue.razor`, `/admin/questions/review`, in the side menu); the mockup adds a count beside the menu item.
- The subject lookup keeps the default `MinChars` of 2 (`AppLookupField.razor`), so it opens only after typing; the topic lookup opens on click (`QuestionForm.razor`, `MinChars="0"`, `OpenOnFocus`).
- The difficulty options are the bare numbers 1 to 5 (`DifficultyOptions` in `QuestionForm.razor`).
- The limits exist (`QuestionLimits`: statement 10,000, base text 20,000, explanation 10,000) but no character counter is shown.
- The form's aside ("Resumo") holds the checklist and the publication actions in one block; F-137 changes the same aside.
- F-121 already covers the email to the author of a review decision.

## Options
| Option | How it works for the user | Effort | Risks / what it rules out |
|---|---|---|---|
| A — One idea per point | About 11 small items | S each | Long queue; several items edit `QuestionForm.razor` at once |
| B — Grouped by area | Kit item (font weight, icons), form item (subject, difficulty, counter), aside cards into F-137, own items for autosave, global search, notifications, review queue count | M | None found |
| C — One item | Everything at once | L | Too large for one session; would be cut at refinement |

Recommendation: B — it joins what changes the same code and keeps apart what has business rules of its own.

## Decisions
- 2026-10-24 — Group by area (option B) — joins what changes the same code, keeps apart what has its own rules.
- 2026-10-24 — Autosave, global search, notifications and the author become ideas — owner's request; reverts the D-4 line that left them out.
- 2026-10-24 — Field icons move to the right end of the controls — owner's request; reverts the D-4 decision of 2026-10-17 that kept the current icons.
- 2026-10-24 — Control text gets a lighter font weight; titles keep today's weight — owner's request, as in the reference screen.
- 2026-10-24 — The aside split into cards (readiness, publication actions, data with the author) improves F-137 instead of a new item — same aside, same checklist.
- 2026-10-24 — The review queue idea is the count in the menu — the queue itself exists (F-105).
- 2026-10-24 — Notifications are an in-app bell; F-121 stays the email — they complement each other without duplication.

## Parked
- Pool target and peer sign-off of the reference screen — still no business behind them (D-4).

## Outcome
- Epics: Foundation and identity (F-153, F-156, F-157), Question bank (F-154, F-155, F-158)
- Features: F-153, F-154, F-155, F-156, F-157, F-158; F-137 improved
- Bugs: none
