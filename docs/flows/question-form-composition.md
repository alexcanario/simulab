---
title: Question form composition
summary: How the question form is framed — six numbered sections, one aside that holds the state and every publication action, and Publish as the screen's single amber action.
items: [F-126]
---
# Question form composition

Technical terms: [glossary](../glossary.md)

Manual page for the user: [Questions](../manual/pt-BR/questions.md)

## Trigger
A curator with `questions.manage` opens `/admin/questions/new`, `/admin/questions/{id}` or `/admin/questions/transcribe`. The page is `QuestionForm.razor`; no route, endpoint, permission or error code changed in F-126.

## Steps
1. The form card shows six `AppSectionCard` sections numbered 1 to 6 in this order: Classification, Origin, Statement, Base text, the key section of the type (Options, True/false key, Matching or Blanks), Explanation. The number is a decorative tag (`aria-hidden`) in place of the icon; each heading keeps its text and its `<Id>-title`.
2. Beside the form the aside (`AppFormAside`, sticky, 20rem) holds the summary rows, the "Before publishing" checklist and, after them, the publication block: the `section#question-section-publication` drawn by the aside's content slot. Below 1280px the aside is neither sticky nor capped and stacks under the form card, before the history cards.
3. The block shows, in this order: the publication error alert, the status chip and text, the annulment date and reason, the rejection alert, the change-reason field (when the Api asked for it), the buttons of the state, then the visible reason.
4. **Publish** is the one amber action: the compact `AppActionButton`, only for a saved draft. Disabled (unsaved changes, then incomplete checklist) it is a real `disabled` button with `aria-describedby="question-publication-reason"`; running, it is disabled and shows progress in place of the icon. Every other button is outlined (Annul keeps the error colour).
5. Save stays the only navy primary at the bottom right of the form card; a frozen question (in review, annulled) has none. In transcription mode there is no publication block and no amber action.

## Rules
- Save never publishes and Publish never saves (F-95 BR12); unsaved changes block Publish.
- Only `QuestionForm.razor` among the routed pages may use `AppActionButton` or `Color.Tertiary` on a button until another screen item picks its own (`ActionColourScopeTests`, `AllowedPages`).
- Buttons in the slot are direct children of the slot; the kit styles them (full width and stacked beside the form, natural width in a wrapping row below 1280px). A page does not spell `app-form-aside-actions` or any other pattern class.
- Beside the form the aside is capped at the viewport under the app bar and scrolls inside itself; with a slot it is focusable (`tabindex="0"`).
- The key sections receive `SectionNumber` (not `Number`: `MatchingKeySection` already has a method named `Number`).
- Every id and text of the former Publication card is kept (`question-preview`, `question-send-for-review`, `question-publish`, `question-withdraw`, `question-unpublish`, `question-annul`, `question-reinstate`, `question-publication-reason`, `question-annul-reason-hint`).
- The "Status" summary row is gone from this form's aside (the chip is the one place); the key `Questions.Form.Aside.Status` stays for `ReviewQuestion.razor`.

## Wireframe
```text
+----------------------------+-----------------------+
| Questions > Edit question  |                       |
+----------------------------+-----------------------+
| (1) Classification  [chg]  | SUMMARY       [chg]   |
| (2) Origin                 | Subject   Law         |
| (3) Statement              | Topic     Contracts   |
| (4) Base text              | Before publishing     |
| (5) Options                |  [x] Topic            |
| (6) Explanation            |  [x] Statement        |
|                            |  [ ] Key              |
|        Cancel  [ Save ]    | PUBLICATION   [new]   |
|                            | (Draft)               |
| Review history  (below)    | [Preview]             |
| Version history (below)    | [Send for review]     |
|                            | [(P) Publish] amber   |
|                            | Why disabled: ...     |
+----------------------------+-----------------------+
```
The block by state:
```text
 Draft (above)          Published            In review
+-----------------+  +-----------------+  +-----------------+
| (Draft)         |  | (Published)     |  | (In review)     |
| [Preview]       |  | [Preview]       |  | [Preview]       |
| [Send review]   |  | [Unpublish]     |  | [Withdraw]      |
| [(P) Publish]   |  | [Annul]         |  |                 |
+-----------------+  +-----------------+  +-----------------+

 Annulled                 New (not saved)
+-----------------+  +---------------------+
| (Annulled)      |  | Save the question   |
| date + reason   |  | first.              |
| [Preview]       |  |                     |
| [Reinstate]     |  |                     |
+-----------------+  +---------------------+
```
Mockup: [F-126-question-form-new-composition.html](../features/mockups/F-126-question-form-new-composition.html)

## Errors
- A refused publication call (403, 404, 409) is the block's error alert, through `ErrorText`; no new code.
