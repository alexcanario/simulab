---
feature: F-98
epic: Question bank
status: done
board: 144
version: 1
---
# Question versions and answer key changes

Technical terms: [glossary](../glossary.md)

## Summary
Editing a published question creates a new version and keeps the earlier ones; an answer key change (after an appeal) records its reason and date; the curator sees the history. An attempt of the simulators can name the version that graded it (ADR-0001 #10), so versions must exist before epic E-5 records any attempt. Source: epic Question bank (E-4), brief capability 3 "changed answer keys".

## Start
- Depends on: F-95 (done), F-96 (done), F-97 (validating: the version stores the answer key it reshapes; building waits for its merge, owner 2026-10-12).
- Waits on (to start): nothing.
- Needed to validate: the local app host and one user with `questions.manage` (the seeded admin) — owner.
- Suggested path: `/agile:build` (no mockup: the screens reuse F-95 patterns, epic says screen design "no").
- Parallel with: F-100 or F-102 (both read questions only; F-101 depends on this item).

## Goal
Make the question's history real before any attempt exists: every state a published question was shown in is kept, an answer key change says why and when, and a later attempt can point at the exact version that graded it.

## What exists (verified 2026-10-12)
- `question_bank.questions` holds one current state. `Question.Update` replaces the content in place (`Question.cs`, `Apply`); editing a published question overwrites the statement, options and key with no trace (F-95 AC7). There is no version number, table or endpoint.
- Audit columns (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, soft delete) come from `TenantEntity` through interceptors (ADR-0001 #10); the Identity contracts expose no user-name lookup, so a stored author id cannot be shown by name from this module.
- Publishing and unpublishing change the status only (F-95 BR12); a published question cannot be deleted (BR11); an edit of a published question runs the publish check (BR4).
- F-96 (done) added the origin columns `ExamEditionId`, `PaperNumber`, `NoticeSubjectId` to `questions` (migration `AddQuestionOrigin`).
- F-97 (`feature/F-97`, validating, not merged) changes the same files: `Question.cs`, `SaveQuestionHandler`, `QuestionConfiguration`, the model snapshot and adds migration `AddTrueFalseKey`, the `AnswerKey` record with `SingleChoiceKey` and `TrueFalseKey`, and a `TrueFalseKey` column. A version must snapshot that key, hence the dependency.
- `QuestionPreviewDialog.razor` renders a question as a student sees it with the key hidden until "show answer"; the form `QuestionForm.razor` has a Publication card below the form layout.
- ADR-0001 #10 and #39 already require history for question content and tests for "answer key versions".

## Users and use cases
- UC1 A curator publishes a question for the first time; version 1 is recorded.
- UC2 A curator edits a published question; each change to what students see or are graded on records a new version.
- UC3 A curator changes the answer key of a published question (after an appeal) and writes why; the version is marked "answer key changed" with the reason and date.
- UC4 A curator unpublishes, fixes and publishes again; a new version is recorded only when the content differs from the latest version.
- UC5 A curator reads the history of a question and opens any version read-only.
- UC6 A later simulator (E-5, E-6) names the version that graded an attempt, by version id or by question id and number.

## Business rules
- BR1 A version is an immutable snapshot of the versioned content: type, content language, statement, base text, explanation, options with their correct mark, and the true/false key. Topic, difficulty and origin (edition, paper number, notice subject) are catalog data, are not in the snapshot and creating or changing them alone makes no version (owner, 2026-10-12).
- BR2 A draft that was never published has no versions. The first publication creates version 1.
- BR3 Saving a `Published` question creates version N+1 when its versioned content differs from the latest version; an identical save creates none. Saving a draft creates none. Publishing a draft that has versions creates version N+1 when its versioned content differs from the latest version, otherwise nothing.
- BR4 The answer key of a version changed when, against the latest version, the letter of the correct option differs (a reorder that moves the correct option counts) or the true/false value differs. Version 1 is never marked changed. The version carries `AnswerKeyChanged`.
- BR5 A version whose key changed needs a reason: 1 to 500 characters of plain text after trimming, sent with the save or the publish that creates it. Missing or blank is refused and nothing is stored. A reason sent with a version that did not change the key is kept as a note on it; a reason sent when no version is created is ignored.
- BR6 Versions are never edited or deleted. Deleting a draft that has versions soft-deletes the question and leaves the versions (owner, 2026-10-12); a published question still cannot be deleted (F-95 BR11).
- BR7 A version has a stable id, a number (1, 2, ...) unique per question, the date it was created and the author, both from the audit columns. The latest version is the highest number; there is no column on `questions` for it.
- BR8 Reading versions needs `questions.manage`, never a role name. A number that does not exist for the question answers 404 `question.version_not_found`.
- BR9 Existing published questions, not deleted, get version 1 in the migration, with the stored content, no reason and no author. Drafts get none.
- BR10 Two saves racing for the same next number: the unique index keeps one; the other answers 409 `question.version_conflict` and the curator reloads.
- BR11 `QuestionResponse` carries `CurrentVersion` (the latest number, null while never published) so a screen knows without a second call.

## Screens and API
No mockup: the screens reuse the patterns of F-95 (epic: screen design "no"). If the owner wants one, `/agile:screen` before the build.

### API (`/api/v1/question-bank`, all `questions.manage`)
- `GET /questions/{id}/versions` — `QuestionVersionListItemResponse(Id, Number, CreatedAt, AnswerKeyChanged, Reason, IsCurrent)`, newest first, no paging (a question has few).
- `GET /questions/{id}/versions/{number}` — `QuestionVersionResponse(Id, QuestionId, Number, Type, ContentLanguage, StatementHtml, BaseTextHtml, ExplanationHtml, the key fields `QuestionResponse` has after F-97, Options, AnswerKeyChanged, Reason, CreatedAt)`. The HTML is the sanitized HTML stored (F-95 BR8).
- `SaveQuestionRequest` gains optional `ChangeReason`; `POST /questions/{id}/publish` accepts an optional body `{ "changeReason": "..." }`.
- `QuestionResponse` gains `CurrentVersion`.
- Error codes: `question.key_change_reason_required` (400), `question.change_reason_too_long` (400), `question.version_not_found` (404), `question.version_conflict` (409).
- Migration `question_bank`: table `question_versions` (id, tenant, question id, number, type, content language, statement, base text, explanation, true/false key, options as a JSON column, answer key changed, reason, audit and soft-delete columns, column comments), unique index `ux_question_versions_tenant_question_number` (`NULLS NOT DISTINCT`), index on `question_id`; the backfill of BR9. No foreign key to anything outside the module; the key to `questions` is inside it.

### Screens
- **Question form** (`/admin/questions/{id}`), new section card **History** (`question-section-history`) below the Publication card, shown for a saved question with `questions.manage`. `AppDataTable` of the versions: version number (a "current" chip on the latest), date and time, change (`AppStatusChip`: "First publication" for 1, "Content edited" neutral, "Answer key changed" in the warning tone), reason (truncated with tooltip), row action "View". Empty state for a question never published: "The first version is recorded when the question is published." The author is stored but not shown: no contract resolves names (see Decisions).
- **View a version**: `QuestionPreviewDialog` opened on the snapshot, title "Version {n}", a note with its date; key and explanation stay behind "show answer" as in the preview.
- **Reason field**: on a `Published` question, when the key in the form differs from the stored key, an `AppTextField` (multi-line, 500 characters, required marker) "Reason for the answer key change" appears at the end of the Options section; Save is refused client-side without it and the Api is the authority. Publishing a draft that has versions whose key differs from the latest: the Api answers 400 `question.key_change_reason_required`, the Publication card then shows the same field (focused), and the second click sends the reason. A publish from the list row refused this way shows the code's text in the alert and the curator completes it on the question's page.
- Typed client: `QuestionBankApiClient` gets `ListVersionsAsync` and `GetVersionAsync`; save and publish take the reason.

## Acceptance criteria
- AC1 Given a complete draft, when it is published, then version 1 exists with the stored content, `AnswerKeyChanged` false and `CurrentVersion` 1; saving that draft before publishing created none. (UC1, BR2)
- AC2 Given a published question at version 1, when the curator changes the statement and saves, then version 2 holds the new statement, version 1 still holds the old one and `CurrentVersion` is 2. (UC2, BR1, BR3, BR6)
- AC3 Given a published question, when it is saved with the same content, or with only a different topic, difficulty or origin, then no version is created. (BR1, BR3)
- AC4 Given a published question, when the key moves to another option without a reason, then 400 `question.key_change_reason_required` and the stored question and versions are unchanged; with a reason of 1 to 500 characters, version N+1 is created with `AnswerKeyChanged` true, the reason and its date. (UC3, BR4, BR5)
- AC5 Given a reason of only spaces, then it counts as missing; given one of 501 characters, then 400 `question.change_reason_too_long`. (BR5)
- AC6 Given a published question whose options are reordered so the correct option changes letter, then the version is marked as a key change and needs the reason. (BR4)
- AC7 Given a published true/false question (F-97), when its key flips, then the version is marked as a key change and needs the reason. (BR4)
- AC8 Given a published question, when only the statement changes and a reason is sent, then the version is created with `AnswerKeyChanged` false and the reason kept as its note; given an identical save with a reason, then nothing is created. (BR5)
- AC9 Given a question with versions, when it is unpublished, edited and published with a different statement, then version N+1 is created at the publish; with no edit, then none; a changed key at that publish needs the reason. (UC4, BR3)
- AC10 Given a question with versions, when the list is read, then it returns every version newest first with `IsCurrent` on the highest; reading `/versions/{number}` returns that snapshot; an unknown number, or one of another question, answers 404 `question.version_not_found`. (UC5, BR7, BR8)
- AC11 Given a user without `questions.manage`, then both version reads answer 403, and the History card is absent. (BR8)
- AC12 Given an unpublished draft that has versions, when it is deleted, then it leaves the list and its version rows stay with their content; a published question is still refused with `question.delete_requires_draft`. (BR6)
- AC13 Given questions already stored before this item, when the migration runs, then each non-deleted published question has version 1 with its content and each draft has none. (BR9)
- AC14 Given two saves of the same published question that both create the next number, then exactly one version with that number exists and the loser answers 409 `question.version_conflict`. (BR10)
- AC15 Given a saved question, when the curator opens the form, then the History card lists the versions with the current chip, the change label and the reason, "View" opens the snapshot read-only with the key hidden until "show answer", and a never-published question shows the empty text. (UC5)
- AC16 Given a published question in the form, when the key is changed, then the reason field appears and Save is refused without it; given a draft whose publish was refused for the missing reason, then the Publication card shows the field and the second click publishes. (UC3, BR5)
- AC17 All new texts appear in every locale of `UI locales:`.

## Criterion → test

Tests locked: de51e5bd97f5d9672511912d8dc5ce0518fb903a (4 files); unlocks: 1

| Criterion | Test |
| --- | --- |
| AC1 | `QuestionVersionEndpointTests.Publish_ACompleteDraft_RecordsVersionOne_AndSavingTheDraftBeforeRecordedNone`; `QuestionVersionTests.NextVersion_FirstPublication_MakesVersionOneWithTheStoredContent`, `NextVersion_OnADraft_MakesNothing` |
| AC2 | `QuestionVersionEndpointTests.Save_AChangedStatementOnAPublishedQuestion_RecordsVersionTwo_AndVersionOneKeepsTheOldText`; `QuestionVersionTests.NextVersion_ChangedStatement_MakesTheNextNumber_AndTheLatestKeepsItsText` |
| AC3 | `QuestionVersionEndpointTests.Save_SameContent_OrOnlyTopicDifficultyAndOrigin_RecordsNoVersion`; `QuestionVersionTests.NextVersion_SameContent_MakesNothing`, `NextVersion_OnlyTopicDifficultyAndOriginChanged_MakesNothing` |
| AC4 | `QuestionVersionEndpointTests.Save_AKeyChangeWithNoReason_IsRefused_AndNothingIsStored`, `Save_AKeyChangeWithAReason_RecordsAVersionMarkedAsKeyChanged_WithTheReasonAndTheDate`; `QuestionVersionTests.NextVersion_KeyMoved_WithNoReason_IsRefusedWithKeyChangeReasonRequired`, `NextVersion_KeyMoved_WithAReason_MakesAVersionMarkedAsKeyChangedWithTheTrimmedReason` |
| AC5 | `QuestionVersionEndpointTests.Save_AKeyChangeWithABlankOrOverLongReason_IsRefused_AndNothingIsStored`; `QuestionVersionTests.NextVersion_ReasonOfExactlyTheLimit_IsAccepted_AndOneMoreIsRefusedWithChangeReasonTooLong`, `NextVersion_ReasonWhoseTrimmedLengthIsTheLimit_IsAccepted` |
| AC6 | `QuestionVersionEndpointTests.Save_AReorderThatMovesTheCorrectOption_IsAKeyChangeAndNeedsTheReason`; `QuestionVersionTests.NextVersion_ReorderThatMovesTheCorrectOption_IsAKeyChangeAndNeedsTheReason` |
| AC7 | `QuestionVersionEndpointTests.Save_ATrueFalseKeyThatFlips_IsAKeyChangeAndNeedsTheReason`; `QuestionVersionTests.NextVersion_TrueFalseKeyFlipped_IsAKeyChangeAndNeedsTheReason` |
| AC8 | `QuestionVersionEndpointTests.Save_AChangedStatementWithAReason_KeepsItAsANote_AndAnIdenticalSaveWithAReasonRecordsNothing`; `QuestionVersionTests.NextVersion_ChangedStatementWithAReason_KeepsTheReasonAsANote_AndTheKeyIsNotMarkedChanged`, `NextVersion_SameContentWithAReason_MakesNothing` |
| AC9 | `QuestionVersionEndpointTests.Publish_AfterAnEditOfTheDraft_RecordsTheNextVersion_AndWithNoEditRecordsNone`, `Publish_AfterTheKeyChangedInTheDraft_NeedsTheReason_AndTheSecondTryWithItPublishes` |
| AC10 | `QuestionVersionEndpointTests.Versions_ListNewestFirstWithTheCurrentOneMarked_AndAMissingNumberIsNotFound`, `Versions_OfAnotherQuestion_AreNotFound_AndAnUnknownQuestionAnswersQuestionNotFound`, `AVersion_StoresItsOptionsAsJsonWithPositionTextHtmlAndIsCorrect` |
| AC11 | `QuestionVersionEndpointTests.BothVersionReads_WithoutThePermission_AnswerForbidden_AndACuratorReadsThem` ; `QuestionVersionFormTests.History_ANewQuestion_HasNoHistoryCard` (the page itself is behind `questions.manage`, F-95) |
| AC12 | `QuestionVersionEndpointTests.Delete_ADraftThatHasVersions_LeavesTheVersionRowsWithTheirContent` |
| AC13 | `AddQuestionVersionsMigrationTests.Up_GivesVersionOneToEachPublishedQuestionNotDeleted_AndToNoDraft`, `Up_CopiesTheLiveOptionsInPositionOrder_UnderTheNamesTheMappingReads` |
| AC14 | `QuestionVersionRaceTests.Save_WhenAnotherSaveTookTheNextNumberFirst_AnswersVersionConflict_AndStoresNothingOfItself`, `Publish_WhenAnotherSaveTookTheNextNumberFirst_AnswersVersionConflict_AndTheQuestionStaysADraft`; `QuestionVersionEndpointTests.TheDatabase_RefusesTwoVersionsWithTheSameNumberForOneQuestion` |
| AC15 | `QuestionVersionFormTests.History_ViewOfAVersionThatIsGone_ShowsTheRefusalInsideTheHistoryCard`, `History_ListsTheVersionsNewestFirst_WithTheCurrentChipTheChangeLabelAndTheReason`, `History_TheKeyChangeRowCarriesTheWarningToneAndItsOwnWords`, `History_ANeverPublishedQuestion_ShowsTheEmptyText`, `History_View_OpensTheRecordedVersionReadOnly_WithTheKeyHiddenUntilShowAnswer`, `History_AfterAPublish_ReloadsTheList` |
| AC16 | `QuestionVersionFormTests.Reason_OnAPublishedQuestionWithTheKeyUntouched_IsNotShown`, `Reason_WhenTheKeyOfAPublishedQuestionChanges_ShowsTheFieldAndRefusesSaveWithoutIt`, `Reason_Typed_IsSentWithSave_AndTheFieldGoesAwayOnceSaved`, `Reason_OverTheLimit_IsRefusedBeforeTheCall`, `Publish_RefusedForTheMissingReason_ShowsTheFieldInTheCard_AndTheSecondClickPublishes`, `ListPublish_RefusedForTheMissingReason_ShowsTheCodesTextInTheTopAlert`; `TrueFalseQuestionFormTests.Edit_PublishedTrueFalse_ChangingTheKey_SendsAPutWithTheNewKeyAndStaysPublished` |
| AC17 | `QuestionVersionResourcesTests.EveryNewText_ExistsInTheThreeLanguagesWithTheItemsWords`; `ResourceParityTests` (every key in each locale) |

## Decisions
- 2026-10-12 — Versioned content is the statement, base text, options, key, explanation and language; topic, difficulty and origin stay catalog data and make no version — owner chose it (question "what versions"), so reclassifying many questions does not flood the history.
- 2026-10-12 — A key change records a required reason plus the automatic date and author; no separate official-date field — owner chose it; E-5/E-6 grade by version, so the recording date is enough.
- 2026-10-12 — A draft with history can be deleted and its versions stay — owner chose it; the delete is soft and a version is a copy, so an attempt still finds its version. Annulment is F-101.
- 2026-10-12 — The history is a card with a list and a read-only "View" in the existing preview dialog — owner chose it; no new mockup.
- 2026-10-12 — F-98 depends on F-97 and its build waits for the merge — owner chose it; the version snapshots the key F-97 reshapes and both change `Question.cs`, the model snapshot and the migrations.
- 2026-10-12 — One way to edit: the reason is a field of the ordinary Save (and, for a draft with history, of Publish), not a separate "change answer key" action — rule `ui-project` (one way to edit an item) and one rule at two moments: a version is created only by a save of a published question or by a publish.
- 2026-10-12 — Versions are created only for published states, so a draft's iterations make no noise and "version 1" is what students first saw — Claude, from ADR-0001 #10 ("an attempt must know which version graded it").
- 2026-10-12 — The latest version is the highest number, with no `current_version` column on `questions` — one source of truth, no field to keep in step; the unique index serves the lookup.
- 2026-10-12 — Options of a version are stored as a JSON column of the version row, not as a child table — an immutable snapshot read whole, never queried by option; the build confirms the EF mapping on the test container.
- 2026-10-12 — Everything is under `questions.manage`; no new permission — the same people who edit a question read its history.
- 2026-10-12 — The author is stored (audit column) but not shown by name — Identity contracts offer no user-name lookup and `users.manage` is not a curator permission; showing it needs a small new contract, left out of this item (see Out of scope).
- 2026-10-12 — No new package — the snapshot, the JSON column and the tests use what the module already references.
- 2026-10-12 — The glossary gets `QuestionVersion` and `AnswerKeyChange` before first use in code (naming rule).
- 2026-10-12 (build) — Design passes (`system-design`, then `architect`) verified against the files; accepted: the entity takes the snapshot (`Question.NextVersion(latest, reason)`), the handlers supply the latest version and persist in the same `SaveChanges` as the content change (a version-index violation answers 409 `question.version_conflict` and rolls the content back too); `PublishQuestionHandler` moves to `TrySaveChangesAsync`; trailing optional `ChangeReason` and `CurrentVersion`; `QuestionOptionResponse` reused for the options of a version; foreign key `question_versions.question_id` → `questions` inside the module, `RESTRICT`, no navigation (a soft delete never removes the version rows); `ChangeReasonMaxLength = 500` in `QuestionLimits`.
- 2026-10-12 (build) — Dropped / corrected from the passes: (1) the backfill excludes the soft-deleted option rows of live questions (`is_deleted = false`) and writes `[]` for a question with no option rows (a published true/false question), with the JSON member names equal to the CLR names EF writes; (2) the backfilled version 1 has `created_at = now()` (when the migration recorded it) and a `gen_random_uuid()` id; (3) an unknown or deleted question id answers 404 `question.not_found` on both reads, and `question.version_not_found` only for a number the known question does not have; (4) a reason longer than 500 characters is refused only when a version is created (a reason sent when no version is created is ignored, BR5); (5) the `question_versions.options` JSON column has no property behind it, so it joins `role_changes.added` and `role_changes.removed` in the expected list of `TableDescriptionTests.OnlyColumnsWithNoPropertyBehindThem_AreExempt` (an existing assertion changed by this item, on purpose); (6) the `type` column is kept on the version (an attempt reads the shape without a second read) and carries the comment of `questions.type`.
- 2026-10-12 (build) — The refinement said F-97 was "validating, not merged"; it was merged (`1c0d4981`) and the item branch took `main` before the first edit. The version snapshots the key as `AnswerKey` shapes it (options for single choice, `TrueFalseKey` for true/false).
- 2026-10-13 (build) — tests unlocked (owner answered "unlock and fix"): two assertions of `QuestionVersionEndpointTests` compared `CreatedAt` with the real clock while the Api test host runs on a fixed `FakeTimeProvider`; they now compare with `Factory.Clock`. Also changed on purpose, outside the lock: `TrueFalseQuestionEndpointTests.Update_PublishedTrueFalse_ChangingTheKey_StaysPublishedWithTheNewKey` sends a `ChangeReason` (F-98 AC7 makes it required) and `InitialQuestionBankMigrationTests` expects `question_versions` among the tables.
- 2026-10-13 (review) — #1 minor, fixed: a refused "View" of a version is shown inside the History card (`_historyError`), not in the Publication card; test `History_ViewOfAVersionThatIsGone_ShowsTheRefusalInsideTheHistoryCard`. #2 minor, fixed: `IQuestionStore.FindLatestVersionAsync` says the version is not tracked. #3 minor, accepted: the epic table row of F-98 (and of F-95 to F-97) reads `refining`/`idea`; the ship refreshes the table, as it did for the earlier items.
- 2026-10-13 (review) — Review coverage: checked and fine — security (both reads under `questions.manage`, tenant and soft-delete filters on the version, reason trimmed and bounded), data (migration, backfill, indexes, checks, Down), contracts (routes, four codes, trailing optional members, regenerated OpenAPI), localization (22 keys in three locales), locks (diff of the four locked files empty), tests (every criterion through the path the user reaches; the race made deterministic), simplicity, rules. Read: `git diff fddff677..HEAD` and the surrounding module code. Not checked: nothing.
- 2026-10-13 (app host) — The Web was opened through the app host of this worktree on its own database `simulab_f98` (Postgres container shared, database the item's): the migration applied on start, the signed-in History card listed two versions seeded by SQL (key change with its reason and "First publication"), "View" opened version 1 read only with the key behind "Show answer", and changing the correct option showed the reason field (500 characters, required). A test account (Curator) was made through `/sign-up` and activated in that database; the database was dropped afterwards. Not driven on screen: saving and publishing through the rich-text editors (covered by the bUnit and Api tests; the validation script has the owner do it).
- 2026-10-12 (build) — Change detection compares the sanitized HTML as stored: a later change of the sanitizer would make the next save of a published question create a version. Known and accepted; not a defect.

## Out of scope
- Restoring an old version as the current one — an `idea` if the curators ask.
- A side-by-side comparison of two versions.
- Showing the author's name in the history: needs a user-name lookup contract from Identity (the author id is stored now).
- Reading versions for students, and re-grading attempts when a key changes — epics E-5 and E-6, which will define the contract they need.
- Annulling a question — F-101.
- Bulk key changes from an imported official answer key — epic E-9.
- Versions of the catalog data (topic, difficulty, origin).

## Open questions
- (none)

## Change notes
<!-- Added by /agile:change during build. -->

## Validation script
Needed to validate: the local app host and one user with `questions.manage` (the seeded administrator `admin@simulab.local`) — owner; in place now.

Whose data: the app runs on its own database `simulab_f98` in the shared local PostgreSQL container, so the migration of this item lands there and never in `simulab`. Close any other app host first.

1. Start the app host from the item's worktree `D:\wt\simulab\f-98-question-versions` (the first start applies the new migration with no `fail` line in the console). Open https://localhost:7125 and sign in as `admin@simulab.local` → you land signed in.

   PowerShell 7:
   ```powershell
   $env:Database__Name = "simulab_f98"; dotnet run --project src/Hosts/Simulab.AppHost
   ```
   Git Bash:
   ```bash
   Database__Name=simulab_f98 dotnet run --project src/Hosts/Simulab.AppHost
   ```
2. Content → Questions → Add: pick a subject and topic (create them in Catalog if the database is empty), write a statement, two options with A correct, **Save** → the **History** card at the bottom of the page says "The first version is recorded when the question is published." Press **Publish** → the card lists version 1, "First publication", with the "Current" chip.
3. Change the statement, **Save** → version 2 "Content edited" is current and version 1 is no longer. Save again with no change → no version 3. Change only the topic or the difficulty and save → still no new version.
4. Mark option B as correct → the field "Reason for the answer key change" appears at the end of the Options section. **Save** with it empty → refused with "Write the reason for the answer key change.", nothing saved. Type a reason, **Save** → version 3 shows "Answer key changed" (warning tone) with your reason. On that row press **View** → the dialog "Version 1" (or the version you chose) opens read only with its recorded date; the correct answer stays hidden until "Show answer".
5. **Unpublish**, mark option A correct again, **Save** (no reason is asked on a draft), **Publish** → refused; the Publication card now shows the reason field, focused. Type a reason and **Publish** again → published, a new version marked "Answer key changed".
6. Add a true/false question with key "True", publish it, then flip the key to "False" → the reason field shows under the key; Save is refused without it and records version 2 with it.
7. Switch the language to Português (pt-PT) with the language button → the History card, its columns and chips, and the reason field are in Portuguese; switch back to English.
8. Keyboard only, on a published question: Tab to the option group, move the correct option with the arrow keys, Tab to the reason field and type, Tab to **Save** and press Space → the version is recorded; Tab to the **View** button of a history row and press Enter → the dialog opens, Esc closes it and the focus returns to that button.

## Events

- 2026-10-13 blocked — test wrong: two assertions compared CreatedAt with the real clock; the API host runs on a fixed FakeTimeProvider (owner chose unlock and fix, 2026-10-13)
- 2026-10-13 test-fail — 1 new warning(s): CA1865 in tests/Hosts/Simulab.Web.Tests/QuestionBank/QuestionVersionFormTests.cs, 1 red
- 2026-10-13 rework — review #1: a refused View is shown in the History card
- 2026-10-13 rework — review #2: the store port says the latest version is not tracked
- 2026-10-13 question — asked 1, from context 0

## Delivery
- Branch: feature/F-98
- Merge: 8f74f923
- Version: 0.32.0
- Tests: 3,382 passed in 14 projects (QuestionBank 244 in 2 min 6 s, Web 1565 in 23 s, Identity 501, Catalog 516, Api 58, AppHost 80, ArchitectureTests 219, others 99); 0 failed; no new warnings; `agile scan GREEN`
- Manual pages: docs/manual/en/questions.md, docs/manual/pt-BR/questions.md, docs/manual/pt-PT/questions.md

Gate time: 3 passes, 7 min 46 s — ship 2 (6 min 7 s), scan 1 (1 min 39 s); 0 red
