---
feature: F-101
epic: Question bank
status: validating
board: 147
version: 1
---
# Annul a question

Technical terms: [glossary](../glossary.md)

## Summary
A curator annuls a published question with a reason, as the organizer did (`AnnulledQuestion`). The question stays in the bank, in its editions and in attempt history; it leaves practice because it is no longer `Published`. How an annulled question is scored in an edition simulation is the edition's scoring rule (epic E-6), not this item. An annulment is a version (ADR-0001 #10) and can be undone by reinstating the question, also as a version. Source: epic Question bank (E-4), brief capability 3.

## Start
- Depends on: F-98 (done).
- Waits on (to start): nothing.
- Needed to validate: the local app host and one user with `questions.manage` (the seeded administrator) — owner.
- Suggested path: `/agile:build F-101` (no mockup: the owner chose text wireframes).
- Parallel with: F-102 only with care — it changes the same `Question.cs`, `QuestionForm.razor` and model snapshot, so in sequence is safer. F-100 is already merged.

## Goal
Let the curator record that the organizer annulled a question, so practice stops using it and the history says when and why, without losing the question, its paper number or any attempt that already graded it.

## What exists (verified 2026-10-13)
- `QuestionStatus` has `Draft` and `Published` only; the column `questions.status` is a string (max 40) with no check on its values (`QuestionConfiguration.cs`, `Contracts/QuestionStatus.cs`).
- `Question.NextVersion` records a version only for a `Published` question whose versioned content or key changed; `QuestionVersion` has `AnswerKeyChanged` and `Reason` and a check `answer_key_changed = false OR reason IS NOT NULL` (F-98).
- `Question.Unpublish`, `Publish`, `Update` and `EnsureDeletable` know two statuses; `EnsureDeletable` answers `question.delete_requires_draft` whose text says to unpublish first, which would mislead for an annulled question.
- The list filter takes statuses from `Enum.GetValues<QuestionStatus>()` and parses with `Enum.TryParse` plus `IsDefined` (F-100), so a third value appears in the filter by itself; `QuestionText.StatusName` and `StatusTone` map only the two known values.
- The topic, edition and notice subject usage checks, and the unique paper number per edition, count every live question whatever its status (`Question*UsageCheck.cs`, F-96 BR3).
- The form's Publication card shows the status chip, Publish or Unpublish and, since F-98, the reason field of a refused publish; the History card lists versions with a change label and tone.
- Everything is under `questions.manage`; there is no review state (F-105) and no simulator that reads questions yet (E-5, E-6).
- Premises not code: the glossary row `AnnulledQuestion` exists ("cancelled by the organizer; scoring follows the organizer's rule"); the rule "an annulment creates a version; it never rewrites history" is in `.claude/rules/agile/project.md`.

## Users and use cases
- UC1 A curator annuls a published question and writes the reason; the question becomes `Annulled`, leaves practice and a version records it.
- UC2 A curator reinstates an annulled question (a mistake, or the organizer reversed it) and writes the reason; the question is `Published` again and a version records it.
- UC3 A curator finds annulled questions in the list by the status filter and sees on the question's page that it is annulled, when and why.
- UC4 A curator reads the history and sees the annulment and the reinstatement among the versions.

## Business rules
- BR1 Only a `Published` question is annulled. A draft (even one with versions) answers 409 `question.not_published` and an annulled one 409 `question.already_annulled`; nothing is stored.
- BR2 An annulment is a new version: the content of the latest version, `Annulled` true, the reason, the date and the author from the audit columns. Earlier versions are never touched (`project.md`, ADR-0001 #10). The version says the state the question had when it was recorded; the question's `Status` is its state now.
- BR3 The reason is required: 1 to 500 characters of plain text after trimming; blank or missing answers 400 `question.status_reason_required`, longer answers 400 `question.status_reason_too_long`; nothing is stored. The date is the day the annulment was recorded; there is no field for the organizer's official date (F-98).
- BR4 Annulling changes no content, key, topic, difficulty or origin, and does not touch the question's paper number.
- BR5 An annulled question is frozen: save, publish, unpublish and delete answer 409 `question.annulled` and change nothing. Reads (get, list, versions, preview) work. Only a reinstatement leaves the state.
- BR6 Reinstating an annulled question makes it `Published` again and records a version with the same content, `Annulled` false and the reason (1 to 500 characters, as BR3) kept as its note. A question that is not annulled answers 409 `question.not_annulled`.
- BR7 The version number is unique per question; two writes racing for the next number leave one version and the loser answers 409 `question.version_conflict` (F-98 BR10).
- BR8 An annulled question keeps its topic, edition, paper number and notice subject: it still blocks the delete of its topic, edition and notice subject and still holds its paper number (F-96 BR3), because the paper really had that question.
- BR9 Practice leaves an annulled question out by its status: whoever reads questions for students reads `Published` only. What an edition simulation does with it is epic E-6's rule; this item builds no simulator.
- BR10 Annul and reinstate need `questions.manage`, never a role name (F-95 BR14); the screen offers the actions only to it.
- BR11 The list filter of status offers `Annulled`, an unknown value is still ignored (F-95 BR15, F-100 BR6).

## Screens and API
No mockup (owner, 2026-10-13); the text wireframes below are the design. A screen with no `AppIcons` entry for annul and reinstate takes the nearest Material Outlined ones the kit already has (the build picks and records them).

### API (`/api/v1/question-bank`, all `questions.manage`)
- `POST /questions/{id}/annul` — body `AnnulQuestionRequest(Reason)`; 200 `QuestionResponse`.
- `POST /questions/{id}/reinstate` — body `ReinstateQuestionRequest(Reason)`; 200 `QuestionResponse`.
- `QuestionResponse` gains `Annulment` (`QuestionAnnulmentResponse(Reason, AnnulledAt)`), null unless the status is `Annulled`; read from the latest version.
- `QuestionVersionListItemResponse` gains `Annulled` and `Reinstated` (the version is the first one after an annulled version); `QuestionVersionResponse` gains `Annulled`.
- `QuestionStatus` gains `Annulled`. Error codes: `question.annulled` (409), `question.already_annulled` (409), `question.not_annulled` (409), `question.status_reason_required` (400), `question.status_reason_too_long` (400); `question.not_published` and `question.version_conflict` are reused.
- Migration in schema `question_bank`: `question_versions.annulled` boolean not null default false (column comment), check `ck_question_versions_annulled_reason` (`annulled = false OR reason IS NOT NULL`) and `ck_question_versions_annulled_key_changed` (`annulled = false OR answer_key_changed = false`), the comment of `questions.status` updated. No data is changed: no stored status becomes invalid.

### Screens
- **Question form** (`/admin/questions/{id}`), Publication card [changed]: for a `Published` question a button **Annul question** next to Unpublish opens the dialog below. For an `Annulled` question the card shows the chip "Annulled", the date and the reason, and one button **Reinstate question** (dialog with the same reason field); Save, Publish, Unpublish and Delete are not offered, the fields are read-only and an alert above the form says the question is frozen and how to leave it. Annulment is only here: the list has no annul action (owner, 2026-10-13).
- **History card** [changed]: the annulment row has the chip "Annulled" in the error tone and the reinstatement row "Reinstated" in the neutral tone, each with its reason.
- **Preview dialog** [changed]: a question that is annulled shows a note "Annulled on <date>: <reason>" above the statement.
- **Question list** (`/admin/questions`) [changed]: the status chip says "Annulled" (error tone); the status filter offers it; the row menu has no Publish or Unpublish for it and Delete is disabled with the reason said in words.
- **Edition's question list** (`EditionQuestionsSection`) [changed]: shows the same chip through the shared status text.

### Wireframe: Question form, Publication card
```text
+--------------------------------------------------------+
| Publication                                            |
| [Published]  This question is published.               |
| [ Unpublish ]  [ Annul question ] [new]                |
+--------------------------------------------------------+

+--------------------------------------------------------+
| (!) This question is annulled and cannot be edited.    |
|     Reinstate it to change it.                  [new]  |
+--------------------------------------------------------+
| Publication                                            |
| [Annulled] [new]   Annulled on 13 Oct 2026             |
| Reason: The organizer annulled item 12 in the          |
| final answer key.                               [new]  |
| [ Reinstate question ] [new]                           |
+--------------------------------------------------------+
```

### Wireframe: Annul dialog
```text
+--------------------------------------------+
| Annul this question?                [new]  |
|                                            |
| Practice will stop using it. You can       |
| reinstate it later; the history is kept.   |
|                                            |
| Reason *                                   |
| [______________________________]   0/500   |
|                                            |
|                  [ Cancel ]  [ Annul ]     |
+--------------------------------------------+
```

### Wireframe: Question list, one annulled row
```text
+--------------------------------------------------------+
| Status: [x Annulled] [changed]                         |
+--------------------------------------------------------+
| Statement        | Source      | Status      | Type    |
| Which article... | CEBRASPE 24 | [Annulled]  | T/F     |
|                  | #12         | [changed]   |         |
| row menu: no Publish/Unpublish, Delete disabled [changed]|
+--------------------------------------------------------+
```

## Docs
- flow: new docs/flows/annul-question.md — annul and reinstate: states, the frozen question, the versions, the screens.

## Acceptance criteria
- AC1 Given a published question at version N, when a curator annuls it with a reason, then its status is `Annulled`, version N+1 holds the same content with `Annulled` true, the trimmed reason and its date, versions 1 to N are unchanged, and the response carries `Annulment` with the reason and date. (UC1, BR2, BR4)
- AC2 Given a draft that was never published, and a draft that has versions, when annul is called, then 409 `question.not_published` and nothing changes; given an annulled question, then 409 `question.already_annulled`. (BR1)
- AC3 Given a published question, when the reason is missing, blank or has 501 characters, then 400 `question.status_reason_required` or `question.status_reason_too_long`, the status stays `Published` and no version is made; a reason of exactly 500 characters is accepted. (BR3)
- AC4 Given an annulled question, when it is saved with any content, published, unpublished or deleted, then 409 `question.annulled` and the stored question, options and versions are unchanged; get, list, versions and preview still work. (BR5)
- AC5 Given an annulled question, when it is reinstated with a reason, then its status is `Published`, version N+2 has the same content with `Annulled` false and the reason as note, `Annulment` is null and it can be edited again, creating versions as in F-98. (UC2, BR6)
- AC6 Given a question that is not annulled, when reinstate is called, then 409 `question.not_annulled`; a missing, blank or over-long reason answers as AC3 and changes nothing. (BR6, BR3)
- AC7 Given a question that was annulled and reinstated, when its versions are listed, then the annulment version has `Annulled` true, the next one has `Reinstated` true, and reading each version returns its `Annulled`. (UC4, BR2, BR6)
- AC8 Given two annul calls racing for the same next version number, then exactly one version with that number exists and the other call answers 409 (`question.version_conflict` or `question.already_annulled`). (BR7)
- AC9 Given an annulled question, when the list is read with `status=Annulled` it appears, with `status=Published` it does not, and an unknown status value is ignored. (UC3, BR11)
- AC10 Given an annulled question of an edition, then its paper number cannot be given to another question of the edition (409 `question.paper_number_taken`), and its topic, edition and notice subject still refuse their delete; the edition's question list shows it with status `Annulled`. (BR8)
- AC11 Given a user without `questions.manage`, then both routes answer 403 and the question page that holds the buttons is not reachable. (BR10)
- AC12 Given databases that already have questions and versions, when the migration runs, then every stored version has `annulled` false, every question keeps its status, and the two checks reject an annulled version with no reason and one that is also a key change. (migration)
- AC13 Given a published question open in the form, when the curator opens the dialog, leaves the reason empty and confirms, then the dialog refuses with the reason's message and calls nothing; with a reason it annuls, the card shows the chip, date, reason and **Reinstate question**, the fields are read-only and Save, Publish, Unpublish and Delete are not offered. (UC1, BR3, BR5)
- AC14 Given an annulled question in the form, when the curator reinstates it with a reason, then the card shows "Published", the frozen alert is gone and the form can be edited. (UC2)
- AC15 Given a question with an annulment and a reinstatement, when the History card opens, then both rows show their label, tone and reason, and "View" opens the version read-only. (UC4)
- AC16 Given an annulled question in the list, then its chip says "Annulled", the status filter offers it, the row menu has no Publish or Unpublish and Delete is disabled with the reason in words. (UC3)
- AC17 Given an annulled question, when its preview opens, then a note shows the date and the reason. (UC3)
- AC18 All new texts appear in every locale of `UI locales:`.

## Criterion → test

Tests locked: be185550d1e1ce838691a64362794505691c2329 (8 files); unlocks: 1

| Criterion | Test |
| --- | --- |
| AC1 | `QuestionAnnulmentEndpointTests.Annul_APublishedQuestion_BecomesAnnulled_WithVersionTwoThatSaysSo`, `Annul_ATrueFalseQuestion_KeepsItsKeyInTheVersion`, `TheAnnulledFlag_IsStoredOnTheVersionRow`; `QuestionAnnulmentTests.Annul_APublishedQuestion_MakesTheNextVersionAnnulled_WithTheSameContentAndTheTrimmedReason`, `Annul_ATrueFalseQuestion_KeepsItsKey_AndChangesNothingButTheStatus` |
| AC2 | `QuestionAnnulmentEndpointTests.Annul_ADraftOrADraftWithVersions_IsRefusedWithNotPublished_AndAnAnnulledOneWithAlreadyAnnulled`; `QuestionAnnulmentTests.Annul_ADraft_IsRefusedWithNotPublished_AndNothingChanges`, `Annul_AnAnnulledQuestion_IsRefusedWithAlreadyAnnulled`, `Annul_ADraftWithABlankReason_AnswersTheStatusFirst` |
| AC3 | `QuestionAnnulmentEndpointTests.Annul_WithAMissingBlankOrOverLongReason_IsRefused_AndNothingIsStored`; `QuestionAnnulmentTests.Annul_WithAMissingOrBlankReason_IsRefusedWithStatusReasonRequired_AndStaysPublished`, `Annul_AReasonOfExactlyTheLimit_IsAccepted_AndOneMoreIsRefusedWithStatusReasonTooLong` |
| AC4 | `QuestionAnnulmentEndpointTests.AnAnnulledQuestion_RefusesSavePublishUnpublishAndDelete_AndStillReads`; `QuestionAnnulmentTests.AnAnnulledQuestion_RefusesUpdatePublishUnpublishAndDelete_WithAnnulled` |
| AC5 | `QuestionAnnulmentEndpointTests.Reinstate_AnAnnulledQuestion_IsPublishedAgain_AndCanBeEdited`; `QuestionAnnulmentTests.Reinstate_AnAnnulledQuestion_MakesTheNextVersionPublished_WithTheReasonAsNote`, `AReinstatedQuestion_CanBeEditedAgain_AndTheEditMakesTheNextVersion` |
| AC6 | `QuestionAnnulmentEndpointTests.Reinstate_AQuestionThatIsNotAnnulled_IsRefused_AndTheReasonRulesAreTheSame`; `QuestionAnnulmentTests.Reinstate_AQuestionThatIsNotAnnulled_IsRefusedWithNotAnnulled`, `Reinstate_WithAMissingOrBlankReason_IsRefused_AndTheQuestionStaysAnnulled`, `Reinstate_AReasonOverTheLimit_IsRefusedWithStatusReasonTooLong` |
| AC7 | `QuestionAnnulmentEndpointTests.Versions_TellTheAnnulmentFromTheReinstatement` |
| AC8 | `QuestionAnnulmentRaceTests.Annul_WhenAnotherWriteTookTheNextNumberFirst_AnswersVersionConflict_AndTheQuestionStaysPublished`, `Reinstate_WhenAnotherWriteTookTheNextNumberFirst_AnswersVersionConflict_AndTheQuestionStaysAnnulled`, `Annul_WhenTheQuestionWasUnpublishedMeanwhile_AnswersVersionConflict_AndStoresNoVersion`, `Unpublish_WhenTheQuestionWasAnnulledMeanwhile_AnswersVersionConflict_AndTheQuestionStaysAnnulled`, `Delete_WhenTheQuestionWasPublishedMeanwhile_AnswersVersionConflict_AndTheQuestionIsNotDeleted`, `Save_WhenTheQuestionWasAnnulledMeanwhile_AnswersVersionConflict_AndTheContentIsNotWritten` |
| AC9 | `QuestionAnnulmentEndpointTests.List_FilteredByStatus_FindsTheAnnulledQuestionOnlyUnderAnnulled` |
| AC10 | `QuestionAnnulmentEndpointTests.AnAnnulledQuestion_KeepsItsPaperNumber_ItsTopicAndItsPlaceInTheEdition`; `AnnulledQuestionUsageGuardTests.DeleteEdition_WithAnAnnulledQuestion_IsRefusedWithHasQuestions`, `DeleteNoticeSubject_WithAnAnnulledQuestion_IsRefusedWithHasQuestions` |
| AC11 | `QuestionAnnulmentEndpointTests.BothRoutes_WithoutThePermission_AnswerForbidden_AndACuratorUsesThem` (the page is behind `questions.manage`, F-95) |
| AC12 | `AddQuestionAnnulmentMigrationTests.Up_KeepsEveryStoredStatus_AndGivesAnnulledFalseToEveryStoredVersion`, `TheDatabase_RefusesAnAnnulledVersionWithNoReason_AndOneThatIsAlsoAKeyChange`, `Down_TurnsAnnulledQuestionsBackIntoDrafts_AndDropsTheColumnAndTheChecks` |
| AC13 | `QuestionAnnulmentFormTests.Publication_OnlyAPublishedQuestion_OffersAnnul`, `Annul_WithAnEmptyReason_IsRefusedInTheDialog_AndNothingIsSent`, `Annul_CancelledInTheDialog_SendsNothingAndLeavesTheQuestionPublished`, `Annul_WithAReason_SendsItTrimmed_AndTheFormBecomesFrozen`, `Annul_RefusedByTheApi_ShowsTheCodesTextInTheDialog_AndTheFormStaysEditable`, `Annul_WithUnsavedChanges_IsDisabledWithTheReasonInWords`, `AnAnnulledQuestion_OpensFrozen_WithTheReasonTheDateAndOnlyReinstateToLeave` |
| AC14 | `QuestionAnnulmentFormTests.Reinstate_WithAReason_SendsItTrimmed_AndTheFormIsEditableAgain` |
| AC15 | `QuestionAnnulmentFormTests.History_TheAnnulmentAndTheReinstatement_HaveTheirOwnLabelToneAndReason` |
| AC16 | `QuestionAnnulmentListTests.Row_Annulled_ShowsTheAnnulledChipInTheErrorTone`, `Row_Annulled_HasNeitherPublishNorUnpublish_AndDeleteIsDisabledWithTheReason`, `Filter_ByAnnulled_SendsTheStatus_AndOnlyTheAnnulledRowStays`; `QuestionsPageTests.Filter_Status_OffersDraftPublishedAndAnnulled`; `QuestionStatusTextTests.EveryStatus_HasItsOwnName_AndOnlyTheDraftIsNeutral` |
| AC17 | `QuestionAnnulmentPreviewTests.Preview_OfAnAnnulledQuestion_ShowsTheDateAndTheReasonAboveTheStatement` |
| AC18 | `QuestionAnnulmentResourcesTests.EveryNewText_ExistsInTheThreeLanguagesWithTheItemsWords`; `ResourceParityTests` (every key in each locale) |

## Decisions
- 2026-10-13 — Annul uses the permission `questions.manage`, no new permission — as decided in F-98 (everything of the module is under it); no other role needs a different rule in v1 — confirmed: F-98.
- 2026-10-13 — The reason is required, 1 to 500 characters, the same limit as the key change reason — one rule for the curators to learn — confirmed: F-98.
- 2026-10-13 — An annulment creates a version and never rewrites history — the rule of the project and ADR-0001 #10; attempts keep the version that graded them — confirmed: `.claude/rules/agile/project.md`.
- 2026-10-13 — The date is the recording date, with no field for the organizer's official date — the free reason can cite the act; the same choice as F-98 — confirmed: F-98.
- 2026-10-13 — An annulment can be undone (reinstate), with a required reason and a new version — without it a curator's mistake leaves the question out of practice for ever and only the database can fix it; it costs one route, one button and one version kind — asked.
- 2026-10-13 — An annulled question is frozen: no save, publish, unpublish or delete, until reinstated — one rule and one error, content is a historical fact; changing catalog data needs a reinstatement first — asked.
- 2026-10-13 — Annulling is only in the Publication card of the form; the list shows the chip and the filter — one way to do it, with the statement in sight (rule `ui-project`) — asked.
- 2026-10-13 — No HTML mockup; the text wireframes are the design — the screen reuses the Publication card, the History card and a plain dialog; the epic marks screen design "no" — asked.
- 2026-10-13 — `Annulled` is a third value of `QuestionStatus`, not a flag beside `Published` — one state at a time, so every reader of `Published` leaves the annulled question out without a change, and F-100 already asks that "a future annulled status must not slip in" (`F-100` decisions) — Claude.
- 2026-10-13 — The version carries `Annulled` (the state when recorded); "Reinstated" is derived from the previous version, with no second column — one fact is stored once — Claude.
- 2026-10-13 — Five new error codes and the reuse of `question.not_published` and `question.version_conflict`; the reason codes are their own pair because a code is the resource key and `question.key_change_reason_required` names another thing — Claude.
- 2026-10-13 — Delete of an annulled question answers `question.annulled`, not `question.delete_requires_draft`, whose text says "unpublish first" — Claude.
- 2026-10-13 — The annulment and reinstatement versions copy the latest version's content: it cannot differ because the question is frozen and a reinstatement changes only the status — Claude.
- 2026-10-13 — Tone of the annulled chip is `Error` (the kit's "cancelled"), with the contrast already asserted by `ThemeContrastTests` — Claude.
- 2026-10-13 — No new package, permission, setting or job; the glossary gets `Annulled` and `Reinstate` rows before first use in code (naming rule), and the row `AnnulledQuestion` keeps its meaning — Claude.
- 2026-10-13 — No data is migrated: stored statuses stay valid and the new column defaults to false — Claude, from the code.
- 2026-10-13 (build) — Design passes (`system-design`, then `architect`, opus) verified against the files; accepted: `Question.Annul(latest, reason)` and `Reinstate(latest, reason)` return the next version (a copy of the latest with new option instances, `AnswerKeyChanged` false) and change the status only after every check; the frozen guard sits in `Publish`, `Unpublish`, `Update` and `EnsureDeletable` and, in the handler, right after the not-found check of `SaveQuestionHandler` so the answer is 409 and not a topic 404; handlers shaped like `PublishQuestionHandler`; the status is checked before the reason (409 before 400); `QuestionResponse.Annulment` and the version list's `Reinstated` are read in `QuestionQueries` from the latest version row; migration `AddQuestionAnnulment` with the two checks as constants beside `ReasonCheck` and no `HasDefaultValue`; the Web freezes the form with `Disabled` fields plus the alert, a read-only value for a true/false key (`AppRadioCards` has no `Disabled`), Annul and Reinstate disabled while the form has unsaved changes (reason in words), one `QuestionStatusReasonDialog`, icons `AppIcons.Error`/`Archive` for annul and `Retry` for reinstate, `MaxLength` read from `QuestionLimits.ChangeReasonMaxLength`.
- 2026-10-13 (build) — Design trade-off 8 `Status` as a concurrency token — owner chose as proposed (yes). Consequence recorded here because it changes existing routes: a save, unpublish or delete that loses a race on the status now answers 409 `question.version_conflict` instead of last-writer-wins; `QuestionStore.TrySaveChangesAsync` also catches `DbUpdateConcurrencyException` (clears the tracker, returns that code) and `UnpublishQuestionHandler` and `DeleteQuestionHandler` move to it.
- 2026-10-13 (build) — Design trade-off 9 `Down` of `AddQuestionAnnulment` — owner chose as proposed: it first sets `status = 'Draft'` where `status = 'Annulled'`, then drops the checks and the column, so the old binary still reads every row and the questions stay out of practice.
- 2026-10-13 (build) — tests unlocked (owner answered "unlock and fix"): two helper methods of `QuestionAnnulmentRaceTests` raised new CA1859 warnings; only their return types changed to `Task<int>`, no assertion moved, then locked again.
- 2026-10-13 (review) — #1 minor, fixed: the dialog's call is awaited even when Esc closed the dialog, so the form never keeps a stale status. #2 minor, fixed: progress on the confirm button. #3 minor, fixed: comment corrected and `QuestionStatusTextTests` reads every enum value. #4 minor, fixed: `AnnulledQuestionUsageGuardTests` (a new file; the locked one was not edited). #5 minor, accepted: the race test named `Delete_WhenTheQuestionWasPublishedMeanwhile_...` sets "Annulled"; renaming needs an unlock for a name only. #6 minor, accepted: `IQuestionStore.SaveChangesAsync` has no production caller now, but the locked race tests of F-98 and F-101 call it on the real store from their fakes; deleting it needs an unlock of both and is left as an `idea`. #7 is the scan below. Review coverage: security, data, contracts, localization, locks, rules, correctness read; not run by the reviewer: build and tests (the full gate ran before the review fixes: 3,475 passed in 13 projects, 0 failed, 0 warnings; the three tests added after the review passed in their projects).
- 2026-10-13 (app host) — The Web was opened through the app host of this worktree on its own database `simulab_f101`: the migration applied on start, the signed-in administrator annulled a published question (the empty reason was refused in the dialog, the reason was trimmed, the alert, the chip, the date and the reason showed, 9 inputs and 5 rich-text fields were disabled, no editable editor, no Save, only Reinstate), reinstated it (History: 3 rows, "Reinstated" first), and annulled a second one: the list showed the red chip "Annulled", the row had only Edit and a disabled Delete, and the status filter offered Draft, Published and Annulled. The status chip dot measures 5.7:1 on white and its text 16:1. Not driven on screen: saving and publishing through the rich-text editors (the questions were seeded by SQL), the pt-PT switch and the keyboard pass (the validation script has the owner do them), the Curator role (the Api test `BothRoutes_WithoutThePermission_AnswerForbidden_AndACuratorUsesThem` covers it).
- 2026-10-13 (scan) — `agile scan BLOCKED: 0 critical, 9 high open`: nine advisories on transitive packages `System.Security.Cryptography.Xml` and `...Pkcs` (10.0.0, 10.0.10, 10.0.11) in 20 projects, first seen today; this item changes no package. The owner accepted the 9 high and the 3 medium of those packages with a reason (asked) and the bump is B-29 (board #170); the other two medium (DocGen path combine) were already open and never blocked.
- 2026-10-13 (build) — The glossary gets `Annulled`, `Annulment` and `Reinstate` before first use in code; the test for the status column comment (`EnumColumnCommentTests`) and the migration test with its `Down` round trip are part of the item.

## Out of scope
- How an annulled question is scored in an edition simulation, and practice itself — epics E-5 and E-6.
- Re-grading attempts that already used the question.
- Annulling or reinstating many questions at once, and an annul action in the list row.
- Changing a question's topic, difficulty or origin while it is annulled (reinstate first).
- The organizer's official date or act number as a field.
- Showing who annulled it by name (needs the user-name lookup F-98 left out).
- Restoring an old version as current, and comparing versions (F-98 out of scope).

## Open questions
- (none)

## Change notes
<!-- Added by /agile:change during build. -->

## Validation script
Needed to validate: the local app host and one user with `questions.manage` (the seeded administrator `admin@simulab.local`) — owner; in place now.

Whose data: the app runs on its own database `simulab_f101` in the shared local PostgreSQL volume, so the migration of this item lands there and never in `simulab`. That database still holds three questions from Claude's check (one annulled, one published, one draft, topic "Crase F101"); use them or write your own. Close any other app host first.

1. Start the app host from the item's worktree `D:\wt\simulab\f-101-annul-question` (the first start applies `AddQuestionAnnulment` with no `fail` line in the console). Open https://localhost:7125 and sign in as `admin@simulab.local` → you land signed in.

   PowerShell 7:
   ```powershell
   $env:Database__Name = "simulab_f101"; dotnet run --project src/Hosts/Simulab.AppHost
   ```
   Git Bash:
   ```bash
   Database__Name=simulab_f101 dotnet run --project src/Hosts/Simulab.AppHost
   ```
2. Content → Questions, open a **Published** question (or add one: statement, two options with A correct, **Save**, **Publish**). In the Publication card press **Annul question** → a dialog "Annul this question?" asks for a reason. Press **Annul** with the field empty → "Write the reason." and nothing is sent. Type a reason and press **Annul** → a yellow alert says the question is annulled and cannot be edited, the card shows the chip "Annulled", "Annulled on <date>" and your reason, every field and option is read only, there is no Save, Unpublish or Publish, and the only action besides the preview is **Reinstate question**; the History card has a new first row "Annulled" in red with your reason.
3. Back to the list (Content → Questions): the question shows the chip "Annulled"; open the **Status** filter → it offers Draft, Published and Annulled, and picking Annulled leaves only annulled questions. Its row menu has no Publish or Unpublish, and Delete is disabled with "This question is annulled and cannot be deleted."
4. On the annulled question press **Preview as a student** → above the statement a note says "Annulled on <date>: <your reason>".
5. Press **Reinstate question**, type a reason, confirm → the alert goes away, the card says "Published", the fields are editable again and Save is back; History has a new first row "Reinstated" with that reason. Then change the statement without saving → **Annul question** is disabled and the card says "Save your changes first."; **Save**, and annulling is possible again.
6. Switch the language to Português (pt-PT) with the language button → the card, the dialog, the chips and the History labels are in Portuguese; switch back to English.
7. Keyboard only, on a published question: Tab to **Annul question** and press Enter → the dialog opens; type a reason, Tab to **Annul**, press Enter → the question is annulled and the focus lands on **Preview as a student**; Tab to **Reinstate question**, Enter, Esc → the dialog closes with nothing changed and the focus returns to the Reinstate button.
8. Sign in as a user without `questions.manage` (a Student account made at `/sign-up`) and open `/admin/questions` → Page not found, as before; nothing about annulment is reachable.

## Events

- 2026-10-13 question — asked 4, from context 4
- 2026-10-13 question — asked 2, from context 0
- 2026-10-13 override — none: both design trade-offs answered as recommended
- 2026-10-13 correction — the override event above was written in error: both design trade-offs were answered as recommended
- 2026-10-13 blocked — test wrong: two helper methods of QuestionAnnulmentRaceTests triggered new CA1859 warnings; only their return types change
- 2026-10-13 rework — review #1: an Esc during the call no longer loses the Api's answer
- 2026-10-13 rework — review #2: progress on the dialog's confirm button
- 2026-10-13 rework — review #3: status text and tone pinned by a test over the enum values
- 2026-10-13 rework — review #4: edition and notice subject delete refused for an annulled question, tested
- 2026-10-14 test-fail — 38 new warning(s): NU1902 in src/Modules/Identity/Simulab.Identity.Infrastructure/Simulab.Identity.Infrastructure.csproj, 1 red

## Delivery
<!-- Filled by /agile:ship. -->
