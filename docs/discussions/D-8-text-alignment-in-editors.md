---
discussion: D-8
status: decided
date: 2026-10-25
---
# Text alignment in the editors

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: decided (2026-10-25)
- Stopped at: Decided: option 1, alignment only (left, center, right, justified) in every editor; F-164 captured.
- Next action: Run /agile:refine F-164.
- Waiting on: nothing

## Idea
Let the author align paragraphs (left, center, right, justified) in every rich text editor. The goal is for a simulated exam to be as faithful a copy of the original exam as possible.

## What we already know
- F-95 (`docs/features/F-95-single-choice-questions.md:131`) fixed the editor formats on purpose and left alignment out ("No header, colour, font, size, align, image, video, code block").
- `AppRichTextToolbar` has no alignment button, and `rich-text.js` has no `align` in `allowedFormats`.
- `QuestionHtmlSanitizer` keeps only the attributes `href`, `src` and `alt` and no CSS property, so any alignment would be dropped on save.
- Every editor is the one kit component `AppRichTextField` (statement, explanation, options in compact mode, base text), so a kit change reaches all of them.
- The stored text is shown only in the back-office previews (`QuestionPreviewContent`, used by `QuestionPreviewDialog` and `ReviewQuestion`, and the base text preview in `QuestionForm`), all inside the CSS class `app-question-content`. The student's exam and review screens do not exist yet.
- F-113 (table button in the same editor, `idea`) is related but a different subject.

## Options
| Option | How it works for the user | Effort | Risks / what it rules out |
|---|---|---|---|
| 1 — Alignment only | The author picks left, center, right or justified per paragraph, class based (`ql-align-*`) | S–M | Leaves out indent, tables, font size and spacing; each is a new request |
| 2 — Alignment plus indent and paragraph formats | Covers quoted blocks, verses and signatures of an original exam | M–L | One more allowed class, more work in every display, and "faithful" has no end; better as an epic |
| 3 — Alignment stored as inline `style="text-align"` | Same for the user | M | The sanitizer would have to allow the CSS property `text-align`, a larger attack surface with no visible gain |

Recommendation: 1 — it unlocks the most common case in one item; fidelity beyond it is a separate epic if the owner wants it. The owner chose 1.

## Decisions
- 2026-10-25 — Text alignment is added to the editors, option 1 (alignment only) — the original exam needs it, and indent and other formats stay out so the item does not grow without end.
- 2026-10-25 — Four alignments: left (default; its button clears the others), center, right and justified — exam texts are often justified.
- 2026-10-25 — It applies to every editor, full and compact, stored as the classes `ql-align-center`, `ql-align-right` and `ql-align-justify` — there is one kit component.
- 2026-10-25 — `QuestionHtmlSanitizer` keeps only those three classes, and only on a paragraph, a list item and a blockquote; any other class is still dropped; a test pins the list — the sanitizer stays closed.
- 2026-10-25 — The item delivers the editor, the storage and the CSS rules in `app-question-content`, validated on the back-office previews; the student's exam and review screens must use that class when they are built — they do not exist yet.
- 2026-10-25 — Pasted text tries to keep its alignment; the build proves what works with Word, PDF and web text and reports what does not — the owner copies real exams.
- 2026-10-25 — This replaces the "no align" decision of F-95; it is recorded in `## Decisions` of the new item with a supersession note, not in an ADR — alignment is not an architecture decision.

## Parked
- Indent, font size and spacing — the owner decides later whether fidelity becomes its own epic.
- Table button — already F-113.
- The student's exam and review screens — they use `app-question-content`; the rule is written in the item.

## Outcome
- Epics: Question bank (existing)
- Features: F-164
- Bugs: none
