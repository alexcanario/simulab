| AC16 | QuestionFormCompositionTests.AnnulDialog_HasItsReasonFieldAndNoAmberAction; QuestionPreviewDialogTests || AC15 | QuestionFormCompositionTests.HistoryCards_StayBelowTheAsideAndAreNotNumbered; QuestionReviewFormTests (review history) || AC14 | QuestionFormCompositionTests.DomOrder_IsTheFormCardThenTheAsideThenTheHistoryCards || AC13 | QuestionFormCompositionTests.Transcription_HasNoPublicationBlockNoAmberActionAndSaveAndContinueIsThePrimary || AC12 | QuestionFormCompositionTests.FrozenQuestion_HasNoSaveAndTheWayOutIsInTheAside || AC11 | QuestionFormCompositionTests.SaveOfADraftWithChanges_SavesAndDoesNotPublishAndIsTheOnlyNavyPrimaryButton || AC10 | AppFormAsideContentTests.AsideBackground_IsThePageOrTheSurfaceColour || AC9 | QuestionFormActionScopeTests (both); ActionColourScopeTests || AC8 | QuestionFormCompositionTests.PublishRunning_TheAmberKeyIsDisabledAndShowsProgress; AppActionButtonFormTests.Busy_* || AC7 | QuestionFormCompositionTests.DraftWithAnIncompleteChecklist_*, DraftWithUnsavedChanges_* || AC6 | QuestionFormCompositionTests.CompleteSavedDraft_PublishIsTheOneEnabledCompactAmberKey... || AC5 | QuestionFormCompositionTests.NewQuestion_TheAsideSaysToSaveFirstAndHasNoPublicationButton || AC4 | QuestionFormCompositionTests.Published_*, InReview_*, Annulled_* || AC3 | QuestionFormCompositionTests.SavedDraft_TheAsideHoldsTheChipPreviewSendForReviewAndPublishAndNoCardBelow || AC2 | AppActionButtonFormTests; AppSectionCardNumberTests; AppFormAsideContentTests.Content_*; UiGalleryFormCompositionTests || AC1 | QuestionFormCompositionTests.Sections_AreNumberedOneToSixWithTheirHeadingsAndIdsUnchanged (5 types); AppSectionCardNumberTests |---
feature: F-126
epic: Interface and screen composition
status: done
board: 177
version: 1
---
# Question form in the new composition

Technical terms: [glossary](../glossary.md)

## Summary
Cut on 2026-10-23 into two parts: this is part 1 (part 2 is F-149, option rows as keys). The question form (`QuestionBank/QuestionForm.razor`) takes the frame of the Stitch reference screen approved on 2026-10-17 (D-4): numbered section cards, and the summary aside that now also holds the publication actions, with **Publish** as the screen's single amber action. The preview and status-reason dialogs keep their composition and only wear the F-148 look. The colours and controls come from F-125 and F-148; the option and key rows (keycap letters, the correct option as a green key with "Correct") and the student preview content are F-149.

## Start
- Depends on: F-125 (done), F-148 (done)
- Waits on (to start): nothing
- Needed to validate: the owner, on screen, in both themes, at desktop and at 1280px and below; the local app host with Docker Desktop running; a `Curator` account and an `Admin` account (rule: a screen that calls a permission-guarded route is validated with the least-privileged role) — owner
- Model: sonnet (no signal: no door, no public contract, no schema, size M after the cut)
- Suggested path: `/agile:screen F-126` (the mockup with every state) → approval → `/agile:build F-126`
- Parallel with: F-127, F-128, F-129 (different pages). Not with F-122, F-123, F-135, F-136, F-137, F-138 (ideas that change the same form): they come after this item.

## Goal
Give the question form the frame of the new identity: a numbered path through its sections and one place, the aside, where the state of the question and every publication action live, with the one action that matters most in amber.

## What exists (verified 2026-10-23)
- `QuestionForm.razor` is 2664 lines. Its form card holds six `AppSectionCard` sections in this order: Classification (`question-section-classification`), Origin (`-origin`), Statement (`-statement`), Base text (`-base-text`), then one of five variants (Options `-options`; True/false key `-true-false-key`; `MatchingKeySection`; `FillInTheBlanksKeySection`), then Explanation (`-explanation`). `AppSectionCard` (F-43) has no number.
- Save is `AppFormActions` at the bottom right of the form card; a frozen question (annulled or in review) has none (F-101 BR5, F-105 BR3).
- The aside is `AppFormAside` (F-43): summary rows (`AsideRows`) and the "Before publishing" checklist (`AsideChecklist`). It is sticky, 20rem wide beside the form and stacked under it below 1280px (`app.css` `.app-form-layout`). It carries no button (F-43 BR4, comment in `AppFormAside.razor`).
- Publication is a separate `MudPaper` card below the form (`question-section-publication`, visible when not transcribing): status chip and text, annulment details, the rejection alert (F-105), Preview, and per state Send for review + Publish (draft), Withdraw (in review), Unpublish + Annul (published), Reinstate (annulled), the visible disabled reason (`question-publication-reason`, `question-annul-reason-hint`) and the change-reason field a publish may ask for (F-98). Below it come the review history and version history cards.
- Every publication button is an outlined `MudButton` today; none is amber. `AppActionButton` (F-148) is the amber key: it fixes its icon to `AppIcons.Restart` (`AppActionButton.razor`, `AppIcons.cs:17`), has no id, no busy state and no `aria-describedby`.
- `ActionColourScopeTests.RoutedPages_PutNoTertiaryColourAndNoActionShortcut` fails on any routed page that uses `Color.Tertiary` on a button or `AppActionButton` (F-148 AC5); this item is the first screen allowed to.
- 19 test files in `tests/Hosts/Simulab.Web.Tests/QuestionBank/` render the form; 8 of them use the publication ids.
- Items that touched this code since its creation: F-95, F-96, F-97, F-98, F-99, F-101, F-102, F-104, F-105, F-106, F-107, F-148.

## Users and use cases
- UC1 A curator opens a question and reads its sections as numbered steps, 1 to 6, whatever the type.
- UC2 A curator sees the state of the question and every publication action together in the aside, next to the summary and the "Before publishing" checklist, without scrolling to a card below.
- UC3 A curator publishes a complete, saved draft with the one amber action of the screen.
- UC4 A curator sees, in words, why Publish is not available (unsaved changes, incomplete checklist).
- UC5 A curator uses the other publication actions (preview, send for review, withdraw, unpublish, annul, reinstate) exactly as before, from the aside.

## Business rules
- BR1 The sections of the form card are numbered in the order shown: 1 Classification, 2 Origin, 3 Statement, 4 Base text, 5 the key section (Options, True/false key, Matching or Blanks), 6 Explanation. Every type has the same six numbers. The number is decorative (`aria-hidden`); the heading text and the section ids do not change. The publication block and the history cards are not numbered.
- BR2 The numbered card is a kit pattern (`AppSectionCard` gets an optional `Number`), shown in the gallery; the two key-section components receive the number from the page.
- BR3 The publication block lives in the aside (`AppFormAside` gets a content slot after the checklist, so the aside is no longer buttonless: F-43 BR4 is changed here). The Publication card below the form no longer exists. The review history and version history cards stay below, in the same order.
- BR4 The block holds exactly what the card held, with the same ids, texts, order of buttons, enabling rules and visible reasons: the status chip and text; the annulment date and reason; the rejection alert; Preview; per state Send for review and Publish (draft), Withdraw (in review), Unpublish and Annul (published), Reinstate (annulled); the disabled reason; the publish change-reason field; "Save the question first" for an unsaved question. No rule, route, permission or error code changes.
- BR5 Publish is the screen's single amber action: it appears only for a saved draft, as the compact `AppActionButton` (the aside is narrower than 360px). When Publish is not available it is shown disabled (sunken, a real `disabled`) with the reason as visible text under it, as today. In every other state of the question there is no amber action. Send for review, Withdraw, Unpublish, Reinstate, Preview and Annul stay outlined (Annul keeps the error colour).
- BR6 `AppActionButton` gets what the form needs: its icon is a required parameter (the right value depends on what the action means: Publish passes `AppIcons.Publish`, the gallery passes `AppIcons.Restart`), an `Id` on its button, a busy state (progress in place of the icon, button disabled) and an `aria-describedby` for the visible reason. The tooltip around it stays as today. A disabled amber key sits on the page or surface colour, never on a sunken or tint background (F-148 BR8): the aside's background is checked for it.
- BR7 Save never publishes and Publish never saves; unsaved changes block Publish (F-95 BR12, confirmed).
- BR8 Save stays the only navy primary button of the page, at the bottom right of the form card; an annulled or in-review question has no Save (F-101 BR5, F-105 BR3, confirmed). In transcription mode (F-99) there is no publication block and no amber action; "Save and continue" is the primary, as today.
- BR9 Below 1280px the aside, with the publication block, stacks under the form card and before the history cards, in that DOM order (so keyboard order is form, Save, aside, histories).
- BR10 The preview and status-reason dialogs keep their composition; they carry no amber action. Their content with keycap letters is F-149.
- BR11 Only `QuestionForm.razor` among the routed pages may use `AppActionButton` or `Color.Tertiary` on a button until another screen item (F-127 to F-134) picks its own; the scan names the allowed page.

## Screens and API
- No route, endpoint, permission or error code changes. Routes `/admin/questions/new`, `/admin/questions/{id}` and `/admin/questions/transcribe` stay.
- Kit (all shown in the dev gallery `/dev/ui`): `AppSectionCard` + `Number`; `AppFormAside` + content slot; `AppActionButton` + `Icon`, `Id`, `Busy`, `DescribedBy`.
- Files: `QuestionForm.razor`, `MatchingKeySection.razor` and `FillInTheBlanksKeySection.razor` (the number only), `Components/Ui/AppSectionCard.razor`, `AppFormAside.razor`, `AppActionButton.razor`, `Components/Pages/Dev/UiGallery.razor`, `wwwroot/app.css`, the resource files of the shared texts if the gallery needs new keys, `docs/agile/ui-map.md`.
- Left to `/agile:screen F-126`, settled there (2026-10-23, "Aside: layout settled by the screen pass" below): the order and width of the buttons inside the aside; whether the "Status" summary row stays beside the chip; the fallback when the tallest state of the block is taller than a 1280×720 viewport.
- Mockup: [F-126-question-form-new-composition.html](mockups/F-126-question-form-new-composition.html) — every state below, light and dark, pt-BR / pt-PT / en, at 1280×720 and stacked at 1024px.

### Wireframe: Question form (saved draft, desktop)
```text
+----------------------------+-----------------------+
| Questions > Edit question  |                       |
+----------------------------+-----------------------+
| (1) Classification  [chg]  | SUMMARY       [chg]   |
| (2) Origin                 | Status    Draft       |
| (3) Statement              | Subject   Law         |
| (4) Base text              | Topic     Contracts   |
| (5) Options                | Before publishing     |
| (6) Explanation            |  [x] Topic            |
|                            |  [x] Statement        |
|        Cancel  [ Save ]    |  [ ] Key              |
|                            | PUBLICATION   [new]   |
| Review history  (below)    | (Draft)               |
| Version history (below)    | [Preview]             |
|                            | [Send for review]     |
|                            | [(P) Publish] amber   |
|                            | Why disabled: ...     |
+----------------------------+-----------------------+
```

### Wireframe: Publication block of the aside, by state
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

### Aside: layout settled by the screen pass
At 1280×720 (app bar 64px, drawer 240px, container padding 24px, so the form column is 656px and the aside 320px, 288px inside) the room for the sticky aside under the app bar is 624px. Estimated from the kit's sizes (the mockup prints the height it measures for each state; not measured in the app): a complete saved draft needs about 900px, the tallest state (draft with a rejection alert, a long reason and the change-reason field) about 1,350px. Every saved state overflows, so the fallback is the normal case at that size, not an edge.

- **Order of the buttons**: the order of the old card, unchanged (BR4), one per line: draft — Preview, Send for review, Publish; published — Preview, Unpublish, Annul; in review — Preview, Withdraw; annulled — Preview, Reinstate. Publish is the last button of the block, so the visible reason sits right under it and describes both disabled buttons, as today. Annul, the destructive one, stays last.
- **Width of the buttons**: beside the form (≥ 1280px) every button of the block takes the full width of the aside (288px), stacked with a 12px gap (the room the amber key's 4px edge needs); a long label wraps inside its button, centred, and the button grows. Stacked under the form (< 1280px) they return to their natural width, in one wrapping row, left-aligned, in the same order (the old card's look). The width comes from the kit (the aside's content slot styles the buttons it holds), never from a page parameter (`DESIGN.md`: a page sets only `Variant` and `Color`).
- **The amber key** is the compact `AppActionButton` in both layouts (AC9), full width beside the form, natural width when stacked.
- **"Status" summary row**: removed from the question form's summary (`AsideRows` loses `Questions.Form.Aside.Status`). The chip at the top of the publication block is the one place the status lives; two copies 300px apart said the same thing in a column that already overflows. In transcription mode and on a new question the row only ever read "Not filled". The key stays: the review screen (`ReviewQuestion.razor`) still uses it.
- **Fallback when the aside is taller than the viewport**: beside the form, the aside stays sticky (F-43 BR4) and its height is capped at the viewport under the app bar (`max-height: calc(100dvh - <app bar height> - 2 × space-4)`, `overflow-y: auto`, `overscroll-behavior: contain`); its content scrolls inside it. Keyboard focus on a button below the fold scrolls the aside to it. Stacked under the form, the aside is neither sticky nor capped. The cap is a rule of `.app-form-aside` in the kit: an aside that fits (the exam form's, F-43) is not changed. Rejected alternatives: dropping the sticky position (the checklist and Publish leave the screen while the curator edits the key, the section that changes them); pinning only the publication block (the checklist scrolls away).

### Screen: Question form (`/admin/questions/new`, `/admin/questions/{id}`, `/admin/questions/transcribe`)
Page anatomy, top to bottom (DOM order = tab order, BR9):
1. `AppPageHeader`: breadcrumb `Questions.Title` › title (`Questions.Form.EditTitle`, `Questions.Form.AddTitle` or `Questions.Transcribe.Title`); no action at the top right (Save stays at the bottom of the form, as today).
2. Transcription header card (transcription mode only, unchanged): edition, counter, Finish (outlined).
3. `.app-form-layout`: the form card (`MudPaper .app-form-card`), then `AppFormAside`.
4. Review history card, then version history card (unchanged, not numbered).

Form card, unchanged except the numbers:
- `AppErrorSummary`, the form's error alert, the annulled alert (`question-annulled-alert`) or the in-review alert (`question-in-review-alert`) at the top, as today.
- Six `AppSectionCard`, each with `Number` 1 to 6 (BR1): 1 Classification, 2 Origin, 3 Statement, 4 Base text, 5 the key section of the type (Options / Answer key / Matches / Blanks; `MatchingKeySection` and `FillInTheBlanksKeySection` receive `Number="5"` from the page), 6 Explanation. The number is drawn in place of the section icon: a 28px tag (6px radius) in the tint blue (`primaryLighten`) with the brand-name ink (`brandName`), weight 800, `aria-hidden="true"`. The heading text, its id (`<Id>-title`) and the subtitle do not change. Fields, limits and validation: unchanged (F-95 to F-107).
- `AppFormActions` (Cancel text, Save filled navy) at the bottom right, absent when frozen (annulled, in review); "Save and continue" in transcription mode (BR8).

`AppFormAside` (`aside`, labelled by its title "Summary"):
- Title `Questions.Form.Aside.Title`.
- Summary rows (`dl`): Subject, Topic, Content language, Difficulty, Base text, Source, then Options (count) or Type, as today, without Status.
- Checklist `Questions.Form.Aside.Checklist`, as today.
- Content slot (new, BR3), absent in transcription mode: the publication block, a `section` with the id `question-section-publication`, separated from the checklist by a 1px divider line, headed by an `h2` "Publication" (`Questions.Section.Publication`, id `question-section-publication-title`) in the aside-title style, and its subtitle (`Questions.Section.Publication.Subtitle`, caption, ink-muted). Then, in this order:
  1. The publication error alert (`AppAlert`, error) when the last publication call was refused.
  2. Status row: `AppStatusChip` (draft neutral, published success, in review warning, annulled error) and the status text (`Questions.Publication.Draft` / `.Published` / `.InReview` with the send date / `.Annulled`); the text wraps under the chip when it does not fit.
  3. Annulled only: the annulment date (`question-annulment-date`) and reason (`question-annulment-reason`).
  4. Draft only, while the latest review entry is a rejection: the rejection alert (`question-rejected-alert`, `AppAlert` info, `Questions.Publication.Rejected` with the date and the reviewer's reason). It moves below the chip (it was above it): the block now opens on the state, and the alert explains it.
  5. Draft only, after the Api asked for it (F-98): the change-reason field `question-publish-reason` — `AppTextField`, label `Questions.ChangeReason.Label`, hint `Questions.ChangeReason.Hint`, 3 lines, required, at most `QuestionLimits.ChangeReasonMaxLength` (500) characters, its error under it; focused when it appears. It sits above the buttons (owner, 2026-10-23: the curator types the reason and reaches Publish without going back).
  6. The buttons of the state (order and width above). Ids unchanged: `question-preview`, `question-send-for-review`, `question-publish`, `question-withdraw`, `question-unpublish`, `question-annul`, `question-reinstate`.
  7. The visible reason: `question-publication-reason` (draft, Publish and Send for review disabled) or `question-annul-reason-hint` (published with unsaved changes, Annul disabled).
- New question (not saved): the block holds only `Questions.Publication.SaveFirst`; no button (AC5).

Buttons of the block, by state (enabling rules unchanged, BR4):

| State | Buttons (in order) | Disabled when | Amber |
|---|---|---|---|
| Draft | Preview (outlined, `View`); Send for review (outlined, `Send`); Publish (`AppActionButton` compact, `Publish` icon) | Preview: while a publication call runs. Send for review and Publish: unsaved changes, then incomplete checklist (one reason, `aria-describedby` on both), or a call running | Publish only |
| Draft, Publish running | Preview, Send for review disabled; Publish busy: progress in place of the icon, label kept, disabled | until the call returns | Publish (busy: the sunken disabled look) |
| Published | Preview; Unpublish (outlined, `Unpublish`); Annul (outlined error, `Archive`) | Annul: unsaved changes (`question-annul-reason-hint`) or a call running | none |
| In review | Preview; Withdraw (outlined, `Withdraw`) | a call running | none |
| Annulled | Preview; Reinstate (outlined, `Retry`) | — | none |
| New, transcription | none | — | none |

Permissions: the route keeps `QuestionBankPermissions.Manage`; no button gets a new permission check. A visitor without it gets the not-found content (`Routes.razor`: a page whose permission the visitor lacks looks like one that does not exist); a refused call (403, 409, 404) is the block's error alert through `ErrorText`. Roles: as today (`Curator` and `Admin` hold it).

Navigation: unchanged. Preview and the status-reason dialogs (Annul, Reinstate) open from the aside as from the card (BR10); focus comes back to the button that opened them, or to Preview after a state change (`_pendingFocus`, as today).

### Kit changes (shown in `/dev/ui`, BR2, BR6)
- `AppSectionCard`: `[Parameter] int? Number`. With a number, the head shows the number tag in place of the icon (`aria-hidden`); without it, unchanged. `Icon` stays `[EditorRequired]` (not shown when a number is given).
- `AppFormAside`: a `ChildContent` slot after the checklist (the kit draws the block's divider, heading and subtitle from `ContentTitle`, `ContentSubtitle` and `ContentId`). Inside the slot, beside the form, MudBlazor buttons and the action key take the full width, stacked, 12px apart; stacked under the form, natural width, one wrapping row. The aside gets the height cap of the fallback above, only in the two-column layout.
- `AppActionButton`: `[EditorRequired] string Icon` (the page passes `AppIcons.Publish`, the gallery `AppIcons.Restart`), `string? Id` on the `button`, `bool Busy` (a small progress ring in place of the icon, `disabled`), `string? DescribedBy` (`aria-describedby` on the `button`). The busy and the disabled key share the sunken look: no edge, ink-muted text.

### States
| State | What the curator sees |
|---|---|
| Loading | Page header, `AppLoadingState` in place of the layout (unchanged). |
| Load failed (server error) | `AppErrorState` with "Try again" (unchanged). |
| Not found / permission denied | Not found: the form card with the error alert and the link back to the list. No permission: the app's not-found content (unchanged). |
| Draft, complete (ready) | Chip "Draft"; Preview, Send for review enabled; Publish amber enabled; no reason. |
| Draft, incomplete checklist | A pending item in the checklist; Send for review and Publish disabled; reason "Complete the checklist before publishing." under Publish. |
| Draft, unsaved changes | Save enabled; Send for review and Publish disabled; reason "Save your changes first." (it wins over the checklist reason). |
| Draft, rejected, change reason asked | Rejection alert under the chip; Publish enabled; the change-reason field above the buttons with its error "Write the reason for the answer key change.", focused. The tallest state (the fallback above). |
| Saving | Save disabled with progress and "Saving..." (`AppFormActions`); the block is as for unsaved changes. |
| Validation error | `AppErrorSummary` at the top of the form card and the error next to the field (for example Topic "Choose the topic."); Publish disabled by the unsaved changes. |
| Publish running (busy) | Publish disabled with progress; Preview and Send for review disabled. |
| Publish succeeded | Snackbar "Question published." (bottom right, the duration set once in `UiKitServiceCollectionExtensions`); the block turns to the published state; no amber left (AC6). |
| Publish refused | The block's error alert at its top (for example "This question is already published."). |
| Published | Chip "Published"; Preview, Unpublish, Annul; Save present (a published question is edited). |
| Published, unsaved changes | Annul disabled; "Save your changes first." under it (`question-annul-reason-hint`). |
| In review | In-review alert at the top of the form card; fields read-only; no Save; Preview, Withdraw. |
| Annulled | Annulled alert at the top of the form card; no Save; annulment date and reason; Preview, Reinstate. |
| New, not saved | Title "Add question"; type cards in section 1; block: "Save the question first to preview or publish it." |
| Transcription | Transcription header above; "Save and continue" primary; aside without the publication block; no amber. |
| Long text | Long subject, topic, base text and edition names break inside the summary rows (`overflow-wrap: anywhere`); a 500-character rejection reason wraps inside its alert; a label longer than 288px (pt-PT "Pré-visualizar como estudante" is close to it in Bricolage 800 at 14px; not measured with the font) wraps inside its full-width button; the aside scrolls inside its cap. |

### Accessibility
- Tab order: form fields in section order, Cancel, Save, then the aside (its content-slot buttons in the order of the table), then the history cards (BR9). Stacked under 1280px, the same order: the DOM does not change, only the grid.
- Headings: the page `h1`; the six sections `h2` with their names (no number in the name: the number is `aria-hidden`); "Summary" `h2` labels the `aside`; "Publication" `h2` labels the block's `section` (`aria-labelledby`), as the card's heading did.
- What each element is: every action of the block is a `button`; the amber key is the one `button` inside `AppActionButton` (its icon is `aria-hidden`); Preview opens a dialog and is a button, not a link.
- Disabled Publish and Send for review: real `disabled`, with `aria-describedby="question-publication-reason"`, the visible text under Publish (a disabled button is not focusable, so the reason is never a tooltip alone; the tooltip around them stays, BR6). Annul: `aria-describedby="question-annul-reason-hint"`.
- Busy: the key is `disabled` while the call runs; the outcome is announced by the snackbar or the error alert (`role="alert"`), as today. No new live region.
- The rejection alert is `role="status"` (info `AppAlert`); the publication error alert `role="alert"`.
- The change-reason field: label above, required marked, its error under it and announced by the field's description, focused when it appears (F-98).
- The aside's capped height: a keyboard user reaches the buttons below its fold by Tab (the browser scrolls the aside to the focused button); text below the fold with no button after it (a new question, transcription mode) is reached by Tab because the capped aside is focusable (`tabindex="0"`).
- Checklist: tick or circle `aria-hidden`, "Done" / "Pending" in visually hidden text, as today.
- Focus ring: 3px info blue, 2px away, on every button of the aside (kit); targets ≥ 44px high.
- Claims about MudBlazor's rendered markup are `unverified (gallery DOM)`: `MudButton` with `Disabled="true"` renders a `button` with the `disabled` attribute and keeps `aria-describedby` passed as an attribute; `MudTooltip` around a disabled button does not take focus and adds no `aria-describedby` of its own; `MudAlert` keeps the `role` passed by `AppAlert`.

### UI texts
No new resource key: every text of the block exists today and keeps its key (BR4). The gallery's samples reuse `Gallery.*` keys or add their own in the three locales (build). Keys the block and the numbered frame show:

| Key | en | pt-BR | pt-PT |
|---|---|---|---|
| `Questions.Section.Publication` | Publication | Publicação | Publicação |
| `Questions.Section.Publication.Subtitle` | A draft is seen only here. A published question is ready for the bank and must stay complete. | Um rascunho só aparece aqui. Uma questão publicada está pronta para o banco e precisa continuar completa. | Um rascunho só aparece aqui. Uma questão publicada está pronta para o banco e tem de continuar completa. |
| `Questions.Publication.SaveFirst` | Save the question first to preview or publish it. | Salve a questão primeiro para visualizar ou publicar. | Guarde primeiro a questão para a pré-visualizar ou publicar. |
| `Questions.Publication.Draft` | Draft. Not available outside the back office. | Rascunho. Não aparece fora do back office. | Rascunho. Não aparece fora do back office. |
| `Questions.Publication.Published` | Published. Edits apply at once and must keep it complete. | Publicada. Edições valem na hora e precisam mantê-la completa. | Publicada. As edições aplicam-se de imediato e têm de a manter completa. |
| `Questions.Publication.InReview` | In review. Waiting for a reviewer since {0}. | Em revisão. Aguardando um revisor desde {0}. | Em revisão. À espera de um revisor desde {0}. |
| `Questions.Publication.Annulled` | Annulled. Out of practice and frozen until it is reinstated. | Anulada. Fora da prática e congelada até ser reintegrada. | Anulada. Fora da prática e congelada até ser reintegrada. |
| `Questions.Publication.Rejected` | Rejected on {0}: {1} | Rejeitada em {0}: {1} | Rejeitada em {0}: {1} |
| `Questions.Publication.Publish.DisabledIncomplete` | Complete the checklist before publishing. | Complete a lista "Antes de publicar" primeiro. | Complete primeiro a lista "Antes de publicar". |
| `Questions.Publication.Publish.DisabledUnsaved` | Save your changes first. | Salve suas alterações primeiro. | Guarde primeiro as suas alterações. |
| `Questions.Annulment.Date` | Annulled on {0} | Anulada em {0} | Anulada em {0} |
| `Questions.Annulment.Reason` | Reason: {0} | Motivo: {0} | Motivo: {0} |
| `Questions.Action.Preview` | Preview as a student | Visualizar como aluno | Pré-visualizar como estudante |
| `Questions.Action.SendForReview` | Send for review | Enviar para revisão | Enviar para revisão |
| `Questions.Action.Publish` | Publish | Publicar | Publicar |
| `Questions.Action.Unpublish` | Unpublish | Despublicar | Retirar publicação |
| `Questions.Action.Annul` | Annul question | Anular questão | Anular questão |
| `Questions.Action.Withdraw` | Withdraw from review | Retirar da revisão | Retirar da revisão |
| `Questions.Action.Reinstate` | Reinstate question | Reintegrar questão | Reintegrar questão |
| `Questions.ChangeReason.Label` | Reason for the answer key change | Motivo da troca de gabarito | Motivo da alteração da chave de respostas |
| `Questions.Published` (snackbar) | Question published. | Questão publicada. | Questão publicada. |
| `Questions.Status.Draft` / `.Published` / `.InReview` / `.Annulled` | Draft / Published / In review / Annulled | Rascunho / Publicada / Em revisão / Anulada | Rascunho / Publicada / Em revisão / Anulada |

### Colours
No new colour and no new token. One new pairing of existing tokens, the section number tag: `brandName` on `primaryLighten` — light `#0F3266` on `#DCE8F5` 10.13:1, dark `#F2F5FB` on `#1A4583` 8.65:1 (text, ≥ 4.5:1); the tag is decorative, so its fill needs no 3:1 against the card. The amber key sits on the page colour (the aside has no background of its own): light `#F5A020` on `#F2F5FB` 1.94:1, identified by its edge `#B87410` at 3.47:1; ink `#0B1B2E` on the amber passes (F-148, `KeycapContrastTests`). The disabled and busy key: the sunken fill `#E6ECF5` / `#081938` with ink-muted text on the page (disabled controls are exempt from 1.4.3; the visible reason carries the meaning). The reason text: `textSecondary` on the page, light `#52647A` on `#F2F5FB` 5.55:1.

## Docs
- flow: new docs/flows/question-form-composition.md — the frame of the form (numbered sections, the aside with the publication block, the one amber action) with the wireframes above.
- flow: update docs/flows/review-before-publishing.md — the "Publication card" wireframes become the aside's publication block.
- flow: update docs/flows/annul-question.md — the same for the annul and reinstate wireframes.

## Acceptance criteria
- AC1 Given the form for each of the five types (single choice, true/false, multiple answer, matching, fill in the blanks), when it renders, then its six sections show the numbers 1 to 6 in order, the number is `aria-hidden`, and each heading text and section id is the one it had (BR1).
- AC2 Given the gallery, when `/dev/ui` opens, then it shows a numbered section card, an aside with a content slot holding actions, and the action key with another icon, with an id, busy and disabled (BR2, BR3, BR6).
- AC3 Given a saved draft, when the form renders, then the aside holds the status chip, Preview, Send for review and Publish with the ids they had, and no element with the id `question-section-publication` exists outside the aside (BR3, BR4).
- AC4 Given a saved published, in-review or annulled question, when the form renders, then the aside holds the actions of that state (published: Preview, Unpublish, Annul; in review: Preview, Withdraw; annulled: the annulment date and reason, Preview, Reinstate) and nothing amber (BR4, BR5).
- AC5 Given a new unsaved question, when the form renders, then the aside shows "Save the question first" and no publication button (BR4).
- AC6 Given a complete saved draft with no unsaved change, when Publish is clicked, then the question is published as before, the chip and the block change to the published state and no amber action remains (BR5, BR7).
- AC7 Given a saved draft with unsaved changes or an incomplete checklist, when the form renders, then Publish is a real `disabled` button, sunken, with the reason as visible text referenced by `aria-describedby`, and Send for review is disabled for the same reason (BR5, BR6).
- AC8 Given Publish is running, when the call has not returned, then the amber key is disabled and shows progress (BR6).
- AC9 Given the routed pages, when they are scanned, then `QuestionForm.razor` is the only one with `AppActionButton` or a tertiary button, at most one amber action is rendered in any state of the form, and it is the compact variant (BR5, BR11).
- AC10 Given the stylesheet resolved against the theme, when the aside background is read in both themes, then it is the page or the surface colour (BR6).
- AC11 Given a draft with changes, when Save is clicked, then the question is saved and not published, and the form has exactly one navy primary button (BR7, BR8).
- AC12 Given an annulled or in-review question, when the form renders, then there is no Save and the way out is in the aside (BR8).
- AC13 Given transcription mode, when the form renders, then there is no publication block, no amber action, and "Save and continue" is the primary (BR8).
- AC14 Given the rendered page, when the DOM order is read, then the form card comes first, the aside next and the history cards last (BR9).
- AC15 Given the review history and version history cards, when the form renders a saved question, then they are still below and unchanged (BR3).
- AC16 Given the preview and status-reason dialogs, when they open, then their structure is unchanged and they contain no amber action (BR10).
- AC17 All new texts appear in every locale of `UI locales:` and the missing-key test is green.

## Criterion → test

Tests locked: fc0d83100287ad5ac97f678d5997bf9511645e00 (6 files); unlocks: 1

| Criterion | Test |
|---|---|
| AC1 | QuestionFormCompositionTests.Sections_AreNumberedOneToSixWithTheirHeadingsAndIdsUnchanged (5 types); AppSectionCardNumberTests |
| AC2 | AppActionButtonFormTests; AppSectionCardNumberTests; AppFormAsideContentTests.Content_*; UiGalleryFormCompositionTests |
| AC3 | QuestionFormCompositionTests.SavedDraft_TheAsideHoldsTheChipPreviewSendForReviewAndPublishAndNoCardBelow |
| AC4 | QuestionFormCompositionTests.Published_TheAsideHoldsPreviewUnpublishAnnulAndNothingAmber, InReview_TheAsideHoldsPreviewAndWithdrawAndNothingAmber, Annulled_TheAsideHoldsTheAnnulmentDateAndReasonPreviewAndReinstateAndNothingAmber |
| AC5 | QuestionFormCompositionTests.NewQuestion_TheAsideSaysToSaveFirstAndHasNoPublicationButton |
| AC6 | QuestionFormCompositionTests.CompleteSavedDraft_PublishIsTheOneEnabledCompactAmberKeyAndPublishingEndsInTheBlockOfAPublishedQuestion |
| AC7 | QuestionFormCompositionTests.DraftWithAnIncompleteChecklist_PublishIsADisabledKeyWithTheVisibleReasonItPointsAt, DraftWithUnsavedChanges_PublishAndSendForReviewAreDisabledWithTheSaveFirstReason |
| AC8 | QuestionFormCompositionTests.PublishRunning_TheAmberKeyIsDisabledAndShowsProgress; AppActionButtonFormTests.Busy_DisablesTheKeyShowsProgressInPlaceOfTheIconAndKeepsTheLabel |
| AC9 | QuestionFormActionScopeTests (both tests); ActionColourScopeTests.RoutedPages_PutNoTertiaryColourOnAButtonAndNoActionShortcut |
| AC10 | AppFormAsideContentTests.AsideBackground_IsThePageOrTheSurfaceColour |
| AC11 | QuestionFormCompositionTests.SaveOfADraftWithChanges_SavesAndDoesNotPublishAndIsTheOnlyNavyPrimaryButton |
| AC12 | QuestionFormCompositionTests.FrozenQuestion_HasNoSaveAndTheWayOutIsInTheAside |
| AC13 | QuestionFormCompositionTests.Transcription_HasNoPublicationBlockNoAmberActionAndSaveAndContinueIsThePrimary |
| AC14 | QuestionFormCompositionTests.DomOrder_IsTheFormCardThenTheAsideThenTheHistoryCards |
| AC15 | QuestionFormCompositionTests.HistoryCards_StayBelowTheAsideAndAreNotNumbered; QuestionReviewFormTests (review history card) |
| AC16 | QuestionFormCompositionTests.AnnulDialog_HasItsReasonFieldAndNoAmberAction; QuestionPreviewDialogTests |
| AC17 | ResourceParityTests.Every_key_exists_in_every_language (the three new `Gallery.*` keys) |

## Decisions
- 2026-10-23 — Size L judged; cut in two: this item keeps the frame (numbered cards, aside with publication, amber action, dialogs untouched); the option and key rows as keys are F-149 — the form has 2664 lines, 19 test files and five types, and the two halves are read separately — asked
- 2026-10-23 — Keep both F-126 and F-137: F-126 moves the publication actions into the aside, F-137 (idea) later adds only the readiness meter; F-137's summary is updated — similar item, asked
- 2026-10-23 — Save never publishes and Publish never saves; unsaved changes block Publish — confirmed: docs/features/F-95-single-choice-questions.md
- 2026-10-23 — An annulled or in-review question is frozen: no Save, only the way out — confirmed: docs/features/F-101-annul-question.md and docs/features/F-105-review-before-publishing.md
- 2026-10-23 — Save stays the only navy primary button; the amber action is a second button of another role, at most one — confirmed: docs/features/F-148-control-alt-palette-v2.md (BR7) and ui.md
- 2026-10-23 — The amber action is Publish, only on a saved draft; every other action is outlined and in other states there is no amber — asked
- 2026-10-23 — All publication actions and texts move to the aside; the Publication card below disappears; the histories stay below — asked; changed: the aside carries no button (docs/features/F-43-exam-form.md BR4)
- 2026-10-23 — The preview and status-reason dialogs do not change composition in this item — asked
- 2026-10-23 — The amber key is the compact variant in the aside (the aside is narrower than 360px, F-148 BR8) — Claude
- 2026-10-23 — `AppActionButton.Icon` is required, with no default: the right icon depends on the action (ui-project rule on kit parameters); `Id`, `Busy` and `DescribedBy` are added for the form — Claude
- 2026-10-23 — The number of a section is decorative and the heading keeps its text and id, so the 19 existing test files still find their selectors; a screen reader reads the section name as before — Claude
- 2026-10-23 — Every publication id and text is kept (`question-publish`, `question-send-for-review`, `question-unpublish`, `question-withdraw`, `question-reinstate`, `question-annul`, `question-publication-reason`, `question-annul-reason-hint`, the preview button id, the rejection and annulment ids); tests that depended on the card's position are changed, not the ids — Claude
- 2026-10-23 — No new package; no migration; no data changes — Claude
- 2026-10-23 — The mockup of `/agile:screen` shows every state at 1280×720; the tallest state decides the fallback if the sticky aside is taller than the viewport — Claude
- 2026-10-23 — The change-reason field (F-98) sits above the action buttons of the block — asked; owner chose the recommended option
- 2026-10-23 — The "Status" summary row is removed from the question form's summary; the chip of the block is the one place for the status — asked; owner chose the recommended option
- 2026-10-23 — The Publish key's arrow rests pointing down and turns up while the key is pressed (replaces the 200-degree press spin for this key); the kit takes it as an optional `IconRestRotation` of `AppActionButton`, the gallery keeps `Restart` with the spin — owner (screen review)
- 2026-10-23 — Approved by the owner ("aprovo F-126"), together with the screen section and the mockup (version 1) — owner
- 2026-10-23 — Stitch offered — declined
- 2026-10-23 — The kit draws the publication block's divider, heading and subtitle from `AppFormAside` parameters (`ContentTitle`, `ContentSubtitle`, `ContentId`); pages never draw section chrome by hand — Claude (ui rule)
- 2026-10-23 — `AppSectionCard.Icon` stays `[EditorRequired]` and is not shown when `Number` is set: the history cards still need it, and a conditional requirement cannot be enforced at build time — Claude
- 2026-10-23 — The block keeps its subtitle (BR4: same texts) — Claude
- 2026-10-23 — The aside gets `tabindex="0"` when its height is capped, so text below the fold is reachable by keyboard — Claude
- 2026-10-23 — Build checks, not owner questions: sticky `top` under the fixed app bar and the app bar height variable are read from the MudBlazor CSS and measured in `/dev/ui`; the aside height cap uses the measured value — Claude
- 2026-10-23 — The pt-BR manual (`docs/manual/pt-BR/questions.md`, "Publicar e despublicar", "cartão Publicação, abaixo do formulário") is updated at the ship; pt-PT and en are frozen (F-124) — Claude

- 2026-10-23 — plan — 1 tests first + skeletons of the kit parameters, shown red and locked → check: the new tests fail for a missing behavior; 2 `frontend` hand-off (kit `AppActionButton`/`AppSectionCard`/`AppFormAside`, gallery, `app.css`, `QuestionForm.razor` with the two key sections; removes nothing: the Publication card is a block inside the page file, the `Aside.Status` row only leaves `AsideRows`) → check: AC1–AC8, AC10–AC16 green; 3 scan scope (`ActionColourScopeTests` names the allowed page) and the existing tests that depend on the card's position or on the amber key → check: AC9 and the whole suite green; 4 one fix round after `gate.js ship` → check: gate GREEN; 5 docs (3 flow pages, `docs/agile/ui-map.md`, pt-BR manual) → check: `techdocs.js check F-126`; 6 review, scan, validation script → check: `validating`
- 2026-10-23 — Build: the `frontend` agent writes the screen and kit code; the locked tests are mine (kit parameters exist as skeletons so they compile) — Claude
- 2026-10-23 — tests unlocked (the section selector of `QuestionFormCompositionTests` also matched the version history card inside `.app-form-card`; test bug, owner agreed): selector narrowed to `.app-form-layout .app-form-card section.app-section-card` — owner
- 2026-10-23 — The two key-section components take `SectionNumber`, not `Number`: `MatchingKeySection` already has a method `Number(int)` — Claude
- 2026-10-23 — The slot's buttons are direct children of the slot and the kit styles them (`.app-form-aside-content > *`): a page may not spell `app-form-aside-actions` (`UiKitBoundaryTests`) — Claude

## Out of scope
- Option rows as keys, the "Correct" green key, the student preview content: F-149.
- Sticky section index F-135, inline student preview F-136, readiness meter F-137, keyboard shortcuts F-138: ideas that come after this item.
- Assigning a reviewer (F-122) and review of published edits (F-123): ideas on the same form, after this item.
- The exam, exam edition and base text forms: F-127, F-128, F-129. The question list: F-132.
- Short answer and open answer types (F-108, F-109): they take the numbered frame for free when built.

## Open questions
- (none)

## Validation script
### English
Needed to validate: you, on screen, in both themes, at desktop width and at 1280px or narrower; Docker Desktop running for the app host; a `Curator` account and an `Admin` account (use the Curator one for the steps; the Admin one only to confirm step 5 works the same). Docker Desktop was running during this build; the accounts are yours to provide.

1. Stop any app host you have open (a running one locks `bin/`), then start this item's app host from the worktree folder. It runs on its own database, `simulab_f126`, on the local PostgreSQL server; nothing lands in the shared `simulab` database. Wait until the Aspire dashboard address is printed, then open https://localhost:7125.

```powershell
cd D:\wt\simulab\f-126-question-form-new
$env:Database__Name = "simulab_f126"; dotnet run --project src/Hosts/Simulab.AppHost
```

```bash
cd /d/wt/simulab/f-126-question-form-new
Database__Name=simulab_f126 dotnet run --project src/Hosts/Simulab.AppHost
```

   Expected: a line `Dashboard: https://localhost:<port>/login?t=<token>`; https://localhost:7125 answers with the home page.
2. The numbered frame of a new question. Sign in as the Curator. In the sidebar (the left menu) click Content, then Questions, then Add (top right of the list page). The page "Add question" opens.
   - [ ] 2.1 Where: the big white card on the left, under the title "Add question" (the form card). It shows six sections stacked from top to bottom, each with a small blue square holding its number: 1 Classification, 2 Origin, 3 Statement, 4 Base text, 5 Options, 6 Explanation.
   - [ ] 2.2 Where: the box on the right of the form card (the Summary aside: the facts of the question and the "Before publishing" list). Scroll to the bottom of that box: its last block is "Publication", with the text "Save the question first to preview or publish it." and no button.
   - [ ] 2.3 Where: in section 1 (Classification), the "Question type" cards at the top. Choose True/false, then Matching, then Fill in the blanks: the section that holds the answer key (Answer key, Matches, Blanks) is always number 5. Go back to Single choice.
   - [ ] 2.4 Where: the whole page. No button is amber; Save, at the bottom right of the form card (next to the text button Cancel), is the only navy button; the Summary aside has no "Status" row.
3. Save a draft and read the Publication block. In section 1 pick a subject and a topic, in section 3 write a statement, in section 5 add two options without marking a correct one, then click Save (bottom right of the form card).
   - [ ] 3.1 Where: the "Publication" block at the bottom of the Summary aside. It shows a grey "Draft" chip, "Draft. Not available outside the back office.", and one button per line: Preview as a student, Send for review, Publish.
   - [ ] 3.2 Where: the last button of the block, Publish. It is the amber key (a darker amber edge under it, an arrow pointing down) and it is greyed and disabled. Right under it the text "Complete the checklist before publishing." is visible; Send for review, the button above, is disabled for the same reason.
   - [ ] 3.3 Where: the statement in section 3 and the text under Publish. Change the statement without saving: the text under Publish becomes "Save your changes first." Click Save to go on.
4. Publish. In section 5 mark the first option as correct and click Save, then click the amber Publish key in the Publication block.
   - [ ] 4.1 Where: the Publish key, last button of the Publication block. It is now amber and enabled. Hold the mouse button down on it: the key sinks and its arrow turns to point up.
   - [ ] 4.2 Where: the bottom right of the window and the Publication block. After the click the snackbar (the message that appears at the bottom right) says "Question published."; the chip turns green "Published"; the block shows Preview as a student, Unpublish and Annul question; no amber button is left on the page.
5. The other publication actions, all in the Publication block at the bottom of the Summary aside.
   - [ ] 5.1 Where: the buttons Annul question and Unpublish of a published question. Click Annul question: the dialog asks for a reason and has no amber button; cancel it. Click Unpublish: the question is a draft again and Publish is amber again.
   - [ ] 5.2 Where: the button Send for review (second button of a draft) and, afterwards, the page top and the block. Click Send for review: the chip is yellow "In review", a notice at the top of the form card says it cannot be edited, the form is read-only with no Save, and the block shows Preview as a student and Withdraw from review. Click Withdraw from review to go back to a draft.
   - [ ] 5.3 Where: Publish, then Annul question in the Publication block. Publish again, click Annul question, write a reason and confirm: the chip is red "Annulled", the date and the reason show in the block, and the block has Preview as a student and Reinstate question. Click Reinstate question and confirm.
6. Themes and language, on the same question form.
   - [ ] 6.1 Where: the theme switch (the moon, top right of the app bar, the bar at the top of the page). Click it: the number squares, the chip and the amber key stay readable; the disabled amber key (change the statement without saving, look at Publish in the block) is grey with no edge on the page colour. Switch back to light.
   - [ ] 6.2 Where: the language switch (the globe, top right of the app bar). Choose Português (Brasil): the block heading reads "Publicação" and the amber key "Publicar". Switch back to English.
7. Narrow width and keyboard.
   - [ ] 7.1 Where: the browser window itself. Make it 1280px wide or narrower (or press F12, Ctrl+Shift+M and pick 1024): the Summary aside moves under the form card and above the history cards (Review history, History); its buttons sit in one wrapping row at their natural width.
   - [ ] 7.2 Where: the Summary aside on the right, back at desktop width. With a tall draft (a rejection or a long reason in the block), the aside stays in view while you scroll the form and scrolls inside itself when it is taller than the window.
   - [ ] 7.3 Where: the whole page, from the first field with the Tab key. You go through the form fields, Cancel, Save, then the aside buttons (Preview, Send for review, Publish), then the history cards below. Every control shows a blue focus ring.
8. The UI gallery. In the sidebar, under Development, click UI kit; the page "UI kit gallery" opens.
   - [ ] 8.1 Where: the gallery sections "Section card" and "Form aside" (scroll down the page, or use your browser's Find with the section name). Under "Section card" one card shows a number square in place of its icon; under "Form aside" the aside has a block with a heading, a subtitle and two buttons.
   - [ ] 8.2 Where: the gallery section "Buttons", the lines under "Action". The compact amber key with an id and the arrow pointing down, a busy one with a spinner and a disabled one.

### pt-BR
Necessário para validar: você, na tela, nos dois temas, em largura de desktop e em 1280px ou menos; Docker Desktop em execução para o app host; uma conta `Curator` e uma conta `Admin` (use a de Curator nos passos; a de Admin só para confirmar que o passo 5 funciona igual). O Docker Desktop estava em execução durante este build; as contas são suas.

1. Pare qualquer app host que esteja aberto (um em execução trava o `bin/`) e inicie o app host deste item pela pasta do worktree. Ele usa o próprio banco, `simulab_f126`, no servidor PostgreSQL local; nada vai para o banco compartilhado `simulab`. Espere o endereço do painel do Aspire aparecer e abra https://localhost:7125.

```powershell
cd D:\wt\simulab\f-126-question-form-new
$env:Database__Name = "simulab_f126"; dotnet run --project src/Hosts/Simulab.AppHost
```

```bash
cd /d/wt/simulab/f-126-question-form-new
Database__Name=simulab_f126 dotnet run --project src/Hosts/Simulab.AppHost
```

   Esperado: uma linha `Dashboard: https://localhost:<porta>/login?t=<token>`; https://localhost:7125 responde com a página inicial.
2. A moldura numerada de uma questão nova. Entre como Curator. No menu lateral (o menu à esquerda) clique em Conteúdo, depois em Questões e em Adicionar (canto superior direito da lista). A página "Adicionar questão" abre.
   - [ ] 2.1 Onde: o cartão branco grande à esquerda, abaixo do título "Adicionar questão" (o cartão do formulário). Ele mostra seis seções empilhadas de cima para baixo, cada uma com um quadradinho azul com o número: 1 Classificação, 2 Origem, 3 Enunciado, 4 Texto-base, 5 Alternativas, 6 Explicação.
   - [ ] 2.2 Onde: a caixa à direita do cartão do formulário (a coluna Resumo: os dados da questão e a lista "Antes de publicar"). Role até o fim dessa caixa: o último bloco dela é "Publicação", com o texto "Salve a questão primeiro para visualizar ou publicar." e nenhum botão.
   - [ ] 2.3 Onde: na seção 1 (Classificação), os cartões "Tipo de questão" no alto. Escolha Certo/errado, depois Associação, depois Preencher lacunas: a seção que guarda o gabarito (Gabarito, Associações, Lacunas) é sempre a 5. Volte para Escolha única.
   - [ ] 2.4 Onde: a página inteira. Nenhum botão é âmbar; Salvar, no canto inferior direito do cartão do formulário (ao lado do botão de texto Cancelar), é o único botão azul-marinho; a coluna Resumo não tem linha "Situação".
3. Salve um rascunho e leia o bloco Publicação. Na seção 1 escolha uma disciplina e um tópico, na seção 3 escreva um enunciado, na seção 5 acrescente duas alternativas sem marcar a correta e clique em Salvar (canto inferior direito do cartão do formulário).
   - [ ] 3.1 Onde: o bloco "Publicação" no fim da coluna Resumo. Ele mostra uma etiqueta cinza "Rascunho", "Rascunho. Não aparece fora do back office." e um botão por linha: Visualizar como aluno, Enviar para revisão, Publicar.
   - [ ] 3.2 Onde: o último botão do bloco, Publicar. É a tecla âmbar (com uma borda âmbar mais escura embaixo e uma seta apontando para baixo) e está cinza e desabilitado. Logo embaixo dele aparece o texto "Complete a lista "Antes de publicar" primeiro."; Enviar para revisão, o botão acima, está desabilitado pelo mesmo motivo.
   - [ ] 3.3 Onde: o enunciado na seção 3 e o texto embaixo de Publicar. Altere o enunciado sem salvar: o texto embaixo de Publicar passa a ser "Salve suas alterações primeiro." Clique em Salvar para continuar.
4. Publique. Na seção 5 marque a primeira alternativa como correta, clique em Salvar e depois na tecla âmbar Publicar do bloco Publicação.
   - [ ] 4.1 Onde: a tecla Publicar, último botão do bloco Publicação. Ela agora está âmbar e habilitada. Segure o botão do mouse sobre ela: a tecla afunda e a seta gira para apontar para cima.
   - [ ] 4.2 Onde: o canto inferior direito da janela e o bloco Publicação. Depois do clique a mensagem no canto inferior direito (o snackbar) diz "Questão publicada."; a etiqueta fica verde "Publicada"; o bloco mostra Visualizar como aluno, Despublicar e Anular questão; não sobra nenhum botão âmbar na página.
5. As outras ações de publicação, todas no bloco Publicação, no fim da coluna Resumo.
   - [ ] 5.1 Onde: os botões Anular questão e Despublicar de uma questão publicada. Clique em Anular questão: a janela pede um motivo e não tem botão âmbar; cancele. Clique em Despublicar: a questão volta a rascunho e Publicar volta a ser âmbar.
   - [ ] 5.2 Onde: o botão Enviar para revisão (segundo botão de um rascunho) e, depois, o alto da página e o bloco. Clique em Enviar para revisão: a etiqueta fica amarela "Em revisão", um aviso no alto do cartão do formulário diz que ela não pode ser editada, o formulário fica só para leitura, sem Salvar, e o bloco mostra Visualizar como aluno e Retirar da revisão. Clique em Retirar da revisão para voltar a rascunho.
   - [ ] 5.3 Onde: Publicar e depois Anular questão, no bloco Publicação. Publique de novo, clique em Anular questão, escreva um motivo e confirme: a etiqueta fica vermelha "Anulada", a data e o motivo aparecem no bloco, e o bloco tem Visualizar como aluno e Reintegrar questão. Clique em Reintegrar questão e confirme.
6. Temas e idioma, no mesmo formulário de questão.
   - [ ] 6.1 Onde: o botão de tema (a lua, canto superior direito da barra superior, a faixa no alto da página). Clique nele: os quadradinhos numerados, a etiqueta e a tecla âmbar continuam legíveis; a tecla âmbar desabilitada (altere o enunciado sem salvar e olhe Publicar no bloco) fica cinza, sem borda, sobre a cor da página. Volte para o claro.
   - [ ] 6.2 Onde: o botão de idioma (o globo, canto superior direito da barra superior). Escolha Português (Brasil): o título do bloco aparece como "Publicação" e a tecla âmbar como "Publicar". Volte para o inglês.
7. Largura estreita e teclado.
   - [ ] 7.1 Onde: a própria janela do navegador. Deixe-a com 1280px de largura ou menos (ou pressione F12, Ctrl+Shift+M e escolha 1024): a coluna Resumo vai para baixo do cartão do formulário e acima dos cartões de histórico (Histórico de revisão, Histórico); os botões dela ficam em uma linha que quebra, na largura natural.
   - [ ] 7.2 Onde: a coluna Resumo à direita, de volta à largura de desktop. Com um rascunho alto (uma rejeição ou um motivo longo no bloco), a coluna continua à vista enquanto você rola o formulário e rola dentro dela quando é mais alta que a janela.
   - [ ] 7.3 Onde: a página inteira, a partir do primeiro campo, com a tecla Tab. Você passa pelos campos do formulário, Cancelar, Salvar, depois pelos botões da coluna (Visualizar, Enviar para revisão, Publicar) e por fim pelos cartões de histórico abaixo. Cada controle mostra um anel de foco azul.
8. A galeria do kit. No menu lateral, em Desenvolvimento, clique em Kit de interface; a página "UI kit gallery" abre.
   - [ ] 8.1 Onde: as seções "Section card" e "Form aside" da galeria (role a página ou use a busca do navegador com o nome da seção). Em "Section card" um cartão mostra um quadradinho numerado no lugar do ícone; em "Form aside" a coluna tem um bloco com título, subtítulo e dois botões.
   - [ ] 8.2 Onde: a seção "Buttons" da galeria, as linhas sob "Action". A tecla âmbar compacta com id e a seta para baixo, uma ocupada com um indicador de progresso e uma desabilitada.

## Events

- 2026-10-23 question — asked 8, from context 3
- 2026-10-23 blocked — test wrong: section selector also matched the version history card inside .app-form-card (test bug, owner agreed)
- 2026-10-23 test-fail — build failed (Simulab.slnx), 2 reds
- 2026-10-23 test-fail — 1 new warning(s): MSB3061 in C:/Program Files/dotnet/sdk/10.0.402/Microsoft.Common.CurrentVersion.targets, 1 red

## Delivery

Gate time: 11 passes, 38 min 10 s — turn 3 (1 min 6 s), ship 6 (28 min 42 s), scan 2 (8 min 22 s); 3 red
Usage: 6160.9k weighted (estimate) — refine 42% · build 47% · ship 10% — agents 45% — context per turn 150k → 406k — 2 sessions, 159 turns

- Branch: `feature/F-126` (removed), merge commit `0916129` on `main`, app version 1.17.0.
- Tests (full gate, `agile gate GREEN`): Web 2489, QuestionBank 677, Identity 548, Catalog 665, Api 83, AppHost 122, Architecture 301, Email 19, Storage 8, Ai 10, Persistence 25, Jobs 25, ApiResults 13, SharedKernel 12; 0 failed. 30 new tests locked at the test commit, 1 unlock (a selector bug in `QuestionFormCompositionTests`, owner agreed).
- Scan: `agile scan GREEN` (2 medium findings in `tools/Simulab.DocGen`, outside the diff).
- Manual: `docs/manual/pt-BR/questions.md` (pt-PT and en frozen, F-124). Technical pages: 3 (`docs/flows/question-form-composition.md`, `review-before-publishing.md`, `annul-question.md`). Glossary: 2 rows added (aside, content slot).
- Validation evidence: the owner said "validado" in the conversation; the 19 boxes were not ticked on this machine, so `validation-ticks.js evidence` recorded 0 of 19 (`not tested` for each).
- Not checked on screen by the build: the question form itself (behind sign-in); the gallery `/dev/ui` was read at 1440px (sticky, cap 704px, full-width buttons). The owner's validation covered the form.
