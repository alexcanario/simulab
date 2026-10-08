---
feature: F-75
epic: Subject taxonomy
status: validating
board: 119
version: 1
---
# Map notice subjects to the canonical taxonomy

Technical terms: [glossary](../glossary.md)

## Summary
An admin maps each `NoticeSubject` of an edition to N canonical subjects and topics (ADR-0001 #44), so "Raciocínio Lógico-Matemático" points to Mathematics and Logical Reasoning. The simulators show the notice's labels; analytics and recommendations use the canonical taxonomy.

## Start
- Depends on: F-74 (approved, not built yet — `NoticeSubject` exists only after its merge) and F-79 (validating on `feature/F-79` — `Subject` and `Topic` exist only after its merge). The build branches from `main` after both merges, never from their branches.
- Waits on (to start): nothing beyond those two merges.
- Needed to validate: an edition with notice subjects and a few subjects and topics in the local database — the F-37 seed gives the edition; F-51 (if merged first) or Claude through the F-79 and F-74 screens gives the rest (Claude).
- Suggested path: `/agile:screen F-75` → approval → `/agile:build` after F-74 and F-79 are done.
- Parallel with: F-77 (refinement and build: different tables and screens).

## Goal
Link the notice's own vocabulary to the canonical one, so the Exam Simulator can keep showing the notice's labels while analytics (E-10) and recommendations (E-11) compare results across exams.

## What exists (verified 2026-10-04)
- `Subject`, `Topic`, `Area` (F-79) on `feature/F-79`, status `validating`: tables `catalog.subjects`, `catalog.topics`; `DeleteSubjectHandler` refuses with `subject.has_topics`; `DeleteTopicHandler` is a plain soft delete and its comment asks each later referrer to add its guard (F-79 BR9); a topic can move to another subject (F-79 BR10).
- `NoticeSubject` (F-74), status `approved`, no code yet: table `catalog.notice_subjects`, section "Notice subjects" on `/admin/exams/{examId}/editions/{id}` with an add/edit dialog `NoticeSubjectDialog.razor`, endpoints under `/api/v1/exams/{examId}/editions/{editionId}/notice-subjects`.
- No mapping entity anywhere in `src/` (main, F-74 and F-79 branches). Simulae's `ExamNoticeTopic` links a notice straight to topics; there is nothing to import.

## Users and use cases
- UC1 An admin with `catalog.manage` sees, on each notice subject row of an edition, the canonical subjects and topics it covers, and which rows are not mapped yet.
- UC2 In the add/edit notice subject dialog, the admin picks the canonical items the row covers: whole subjects and single topics, any number of them.
- UC3 The admin removes a canonical item from a row's mapping, or clears it.
- UC4 A curator tries to delete a subject or topic that some notice subject maps to and is told it is in use.

## Business rules
- BR1 A mapping entry is either a whole canonical subject or one canonical topic, never both and never neither (`notice_subject.mapping_invalid`). A notice subject has from 0 to 50 entries (`notice_subject.mapping_too_many`).
- BR2 Mapping is optional. A notice subject with no entry is "not mapped"; nothing is refused because of it, publishing an edition included (F-74 BR9 keeps a published edition editable).
- BR3 An entry must point at a subject or topic that exists and is not deleted, else 400 `notice_subject.mapping_target_not_found`.
- BR4 Within one notice subject, a topic may not be mapped together with its own subject as a whole: there was no such overlap before the save and there would be one after → 400 `notice_subject.mapping_overlap`. The check runs on save only; an overlap that a later topic move creates (F-79 BR10) is not refused there and is shown as is.
- BR5 The same entry sent twice in one save is stored once (no error).
- BR6 The same subject or topic may be mapped by several notice subjects of the same edition (a law listed under two headings, ADR-0001 #44).
- BR7 A save replaces the whole mapping of the notice subject: the request carries the full list of entries.
- BR8 A topic entry follows its topic: when the topic moves to another subject (F-79 BR10), the entry now counts under the new subject. The entry stores the topic only, not its subject.
- BR9 Deleting a topic that at least one live notice subject maps to is refused with 409 `topic.in_use`. Deleting a subject that at least one live notice subject maps to as a whole is refused with 409 `subject.in_use` (a subject with topics is still refused first by `subject.has_topics`). "Live" excludes deleted notice subjects and notice subjects of deleted editions.
- BR10 The subject list and the topic section of F-79 know whether each item is in use, so the delete action is disabled with its reason before the click, like `subject.has_topics`.
- BR11 Deleting a notice subject or an edition (F-74 BR10) leaves its mapping rows in place; they no longer count for BR9 because their notice subject is not live.
- BR12 Every endpoint requires `catalog.manage`; the mapping field and its display appear only where the F-74 section already appears (F-74 BR11).
- BR13 The canonical names are shown as typed (not translated, F-79 BR3); a topic is shown with its subject ("Mathematics › Fractions"), a whole subject with a marker meaning "whole subject".

## Screens and API
Screens follow the rules `ui` and `ui-project`; the detailed screen section and mockup come from `/agile:screen F-75`.
- `/admin/exams/{examId}/editions/{id}`, section "Notice subjects" (F-74): each row shows its mapped items as chips under the label, or a "Not mapped" marker; the footer adds the count of rows not mapped (UC1).
- `NoticeSubjectDialog.razor` (F-74): new field "Covers" after the number of questions — a multi-pick with search over subjects and topics, grouped by subject, where a subject can be picked whole; picked items show as removable chips (UC2, UC3). One dialog edits the whole row.
- `/admin/subjects` and `/admin/subjects/{id}` (F-79): delete disabled with the in-use reason (BR10).
- GET `/api/v1/catalog/taxonomy` — every live subject with its live topics, alphabetical, for the picker (one call per dialog opening).
- POST / PUT `/api/v1/exams/{examId}/editions/{editionId}/notice-subjects[/{id}]` (F-74) — the body gains `mappings: [{ "subjectId": guid | null, "topicId": guid | null }]`; omitted or empty means not mapped.
- GET `/api/v1/exams/{examId}/editions/{editionId}/notice-subjects` (F-74) — each row gains `mappings: [{ "subjectId", "subjectName", "topicId", "topicName" }]`.
- GET `/api/v1/catalog/subjects` and GET `/api/v1/catalog/subjects/{subjectId}/topics` (F-79) — each item gains `inUse` (BR10).
- Error codes: `notice_subject.mapping_invalid`, `notice_subject.mapping_too_many`, `notice_subject.mapping_target_not_found`, `notice_subject.mapping_overlap`, `subject.in_use`, `topic.in_use`.
- Table `catalog.notice_subject_mappings`: `id`, `tenant_id`, `notice_subject_id` (FK, cascade), `subject_id` (FK, restrict, nullable), `topic_id` (FK, restrict, nullable), audit columns and soft delete (`TenantEntity`), column comments; check constraint "exactly one of `subject_id`, `topic_id`"; unique index `ux_notice_subject_mappings_tenant_notice_subject_target` over (`tenant_id`, `notice_subject_id`, `subject_id`, `topic_id`) `NULLS NOT DISTINCT` and filtered by `is_deleted = false`; indexes on `notice_subject_id`, `subject_id` and `topic_id` (BR9); check constraint `ck_notice_subject_mappings_one_target`.

## Screen
Mockup: [`mockups/F-75-map-notice-subjects.html`](mockups/F-75-map-notice-subjects.html) (states, languages, light and dark). It shows only the F-75 deltas on the F-74 and F-79 screens, with enough of those screens around them; everything not named here stays as F-74 `## Screen` and the F-79 pages describe it.

### Screen 1 — "Notice subjects" section on the edition page (delta on F-74 Screen 1)
- Place: `NoticeSubjectsSection.razor` on `/admin/exams/{examId}/editions/{id}` (F-74). Card, groups, move/edit/delete actions, Add button and sum stay as F-74.
- Each row (`RowTemplate`) becomes two lines in the content column; the number of questions stays right-aligned beside it:
  1. The label (`AppTruncatedText`), as F-74.
  2. The mapping (UC1, BR13):
     - Mapped: a list of read-only chips (`ul`, labelled `NoticeSubjects.Field.Covers` + the row's label), wrapping onto more lines when needed; every entry is shown, none is hidden behind a "more". Order on the screen: subject name (case and accents ignored), the whole subject before its topics, then topic name. The Api's order is not relied on.
     - Whole-subject chip: icon `AppIcons.Subjects` (decorative), the subject name, then the marker `NoticeSubjects.Covers.WholeMarker` in secondary text ("Matemática · disciplina inteira").
     - Topic chip: icon `AppIcons.Topics` (decorative), `NoticeSubjects.Covers.Topic` ("Raciocínio Lógico › Proposições"); the `›` form is `aria-hidden` and the visually hidden `NoticeSubjects.Covers.Topic.Spoken` ("Proposições, tópico de Raciocínio Lógico") is what a screen reader hears. The subject is the topic's **current** subject (BR8): after a topic move the chip shows the new one.
     - An overlap made by a later topic move (BR4, AC6) is shown as is: both chips, no marker.
     - Not mapped (BR2): `AppStatusChip` with tone `Warning` and text `NoticeSubjects.Row.NotMapped`. The tone means "still to do"; nothing is blocked.
- Footer (F-74 item 5), right side: after the sum lines, `NoticeSubjects.Total.Unmapped.One` / `.Many` with the count of rows not mapped, in secondary text, only when that count is above zero (AC10). Computed from the loaded list, like the sum.
- Chip names are content: shown as typed, never translated (BR13). The chips do not link anywhere and are not focusable.
- The read-only chip is a kit component (`AppChip`, Screen 4) — not the raw `.app-chip` class `Users.razor` uses today.
- Permissions: unchanged from F-74 (BR12): the section exists only on a page that requires `catalog.manage`.

### Screen 2 — Add / edit dialog, new field "Covers" (delta on F-74 Screen 2)
- `NoticeSubjectDialog.razor`. Fields in this order: group, label, number of questions (F-74), then **Covers** (UC2, UC3). Dialog size, alert, `AppFormActions`, discard confirmation and saving behaviour stay as F-74; a change of the picks makes the dialog "changed" for the discard confirmation.

| Id | Label key | Type | Required | Limits | Hint / placeholder keys |
|---|---|---|---|---|---|
| `notice-subject-covers` | `NoticeSubjects.Field.Covers` | multi-pick with search over the taxonomy, grouped by subject, a subject pickable whole (new kit field `AppMultiPickField`, Screen 4) | no (BR2) | 0 to 50 entries (BR1) | `NoticeSubjects.Field.Covers.Hint`, `.Covers.Placeholder`; listbox label `.Covers.Options`; picked list label `.Covers.Picked` |

- Anatomy, top to bottom: label; the search input (`combobox`); its popup list; the description line (hint, or a state message, or the field error); the line `NoticeSubjects.Field.Covers.Count` ("Escolhidos: 3 de 50") with, on the right, the text button `Lookup.Clear` (only when at least one item is picked; UC3 "clear"); then the picked items as removable chips. With nothing picked, the chips area says `NoticeSubjects.Field.Covers.None`.
- Taxonomy load: `GET /api/v1/catalog/taxonomy` once when the dialog opens (Screens and API). The picked chips of an edit come from the row (`mappings` carries the names), so they show and can be removed before and without the taxonomy.
  - Loading: input disabled, description `NoticeSubjects.Field.Covers.Loading` with a small progress indicator.
  - Load error: input disabled, description `NoticeSubjects.Field.Covers.LoadFailed` in error style with the text button `Common.TryAgain` (calls the endpoint again). Save still works with the picks already there.
  - Empty taxonomy: input disabled, description `NoticeSubjects.Field.Covers.EmptyTaxonomy`. No link (it would leave a dialog with unsaved changes).
- Popup list: opens on a click in the input, on typing or on Arrow down — not on focus alone, so a field error under the input stays visible when Save moves the focus there (the whole taxonomy is local: no minimum of characters, no debounce). Groups are the subjects in the Api's order (alphabetical, AC13); each group starts with the **whole-subject option** (the subject name, secondary text `NoticeSubjects.Covers.WholeMarker`), followed by its topics, indented. A subject without topics has only its whole option.
- Search: contains-match ignoring case and accents. A subject whose name matches shows with all its topics; otherwise a subject shows with only its matching topics (its whole option stays, for context). No match: `Lookup.NoResults` with the term. The result count is announced (`Lookup.Results`).
- Picking: each option toggles (a check mark shows the picked ones); the list stays open so several can be picked in a row; the input keeps its term. Each toggle is announced by the polite live region: `NoticeSubjects.Covers.Added` / `.Removed`. BR5 never arises from the screen: an option is picked or not.
- Comfort checks in the list (the Api stays the authority), compared with the **saved** mapping exactly like BR4, so an overlap that a topic move created is never blocked:
  - a topic whose subject is picked whole: disabled, secondary text `NoticeSubjects.Covers.CoveredByWhole`;
  - a whole-subject option while some of its topics are picked: disabled, secondary text `NoticeSubjects.Covers.WholeBlocked`;
  - 50 entries picked: every unpicked option disabled, and the description shows `NoticeSubjects.Field.Covers.LimitReached` (50) instead of the hint.
  Disabled options keep their place; the arrow keys still reach them so their reason is read, and Space or Enter does nothing on them (`aria-disabled`).
- Picked chips: the same chip as Screen 1 with a remove button (`AppIcons.Close`, tooltip and accessible name `Common.RemoveItem` with the chip's spoken name). Removing announces `NoticeSubjects.Covers.Removed` and moves focus to the next chip's remove button, else the previous one, else the search input. `Lookup.Clear` removes all, announces `NoticeSubjects.Covers.Cleared` and moves focus to the search input. The row's mapping only changes on Save (BR7).
- Save sends the full list of picks as `mappings` (BR7); none picked sends an empty list (not mapped, AC9).
- Api refusal, under the Covers field (it replaces the hint; the dialog stays open with what was picked):
  - `notice_subject.mapping_overlap`: the topic chips whose subject is picked whole are marked (error border and the text `NoticeSubjects.Covers.Overlaps`). Arises only when the taxonomy changed while the dialog was open (a topic moved).
  - `notice_subject.mapping_target_not_found`: the taxonomy is loaded again; the chips whose subject or topic is no longer in it are marked (error border and `NoticeSubjects.Covers.Gone`) and the options are rebuilt.
  - `notice_subject.mapping_too_many`, `notice_subject.mapping_invalid`: the message alone (neither should arise from the screen).
  Other codes keep F-74's placement (fields or the top alert).
- Focus: on open, the group field (F-74); after Save, F-74. The first field in error on a failed Save is focused; for the Covers errors that is the search input.

### Screen 3 — Subject list and topics section (delta on the F-79 screens, BR10)
- `/admin/subjects` (`Subjects.razor`): the row's `AppRowActions.DeleteDisabledReason` is `Subjects.Delete.DisabledHasTopics` when the topic count is above zero (as today, BR9: `subject.has_topics` first), else `Subjects.Delete.DisabledInUse` when `inUse` is true, else none. No new column.
- `/admin/subjects/{id}` (`TopicsSection.razor`): each topic row's `AppRowActions` gets `DeleteDisabledReason` `Topics.Delete.DisabledInUse` when the topic's `inUse` is true. The subject page itself has no delete (F-79): nothing else changes there.
- A refusal at the click (someone mapped the item after the list loaded): `subject.in_use` / `topic.in_use` in the page or section alert through `Errors.For`, and the list is reloaded — the existing F-79 refusal path.
- The disabled Delete keeps its tooltip with the reason, as `subject.has_topics` does (kit behaviour).

### Screen 4 — Kit additions (`Simulab.Web/Components/Ui/`, each with a `/dev/ui` gallery entry)
- `AppChip`: a read-only label chip (the look of today's `.app-chip`), with an optional decorative leading icon, an optional secondary text, an error state (for a marked chip) and an optional remove button whose accessible name is `[EditorRequired]` when the button is shown (rule `ui`: the wording depends on the page). `AppFilterChip` keeps its own wording.
- `AppMultiPickField`: label above, search input as a `combobox` with a multi-select `listbox` popup (`aria-multiselectable="true"`), options in labelled groups where the group's first option can be the group itself, per-option disabled reason shown as secondary text, client-side contains-match ignoring case and accents, loading / load-failed (with try again) / empty states in the description line, count line with clear, and the picked items as `AppChip`s with remove. Built on MudBlazor parts; no new package. `AppLookupField` stays the single-pick, server-search field.
- `AppIcons`: no new constant (`Subjects`, `Topics`, `Close` exist).

### States
| State | What shows |
|---|---|
| Section: loading, load error, empty, unsaved edition | As F-74; no mapping shown. |
| Section: rows mapped and not mapped | Chips under each mapped label; `Not mapped` chip on the others; footer adds the not-mapped count. |
| Section: every row mapped | No not-mapped line in the footer. |
| Section: no row mapped | Every row marked; footer says all of them. |
| Section: overlap after a topic move (AC6) | Both chips shown, no marker. |
| Section: row with many entries | Chips wrap on several lines; the row grows. |
| Saved | Snackbar `NoticeSubjects.Saved`; the row shows its new chips (or `Not mapped` after clearing). |
| Dialog, Covers: add (nothing picked) | Count 0 of 50; `Covers.None`. |
| Dialog, Covers: edit with picks | Chips from the row; taxonomy loading in the background. |
| Dialog, Covers: taxonomy loading / load error / empty | As described above; chips stay removable. |
| Dialog, Covers: list open, no term | Every subject with its topics; picked ones checked. |
| Dialog, Covers: search with results / no results | Filtered groups / `Lookup.NoResults`. |
| Dialog, Covers: subject picked whole | Its topics disabled with `Covers.CoveredByWhole`. |
| Dialog, Covers: topics picked | Their subject's whole option disabled with `Covers.WholeBlocked`. |
| Dialog, Covers: limit reached | 50 of 50; unpicked options disabled; `Covers.LimitReached`. |
| Dialog, Covers: overlap refused (400) | Field error; topic chips marked `Covers.Overlaps`. |
| Dialog, Covers: target not found (400) | Field error; taxonomy reloaded; the gone chip marked `Covers.Gone`. |
| Dialog: saving, unsaved changes, server error | As F-74; the Covers input, clear and remove buttons are disabled while saving. |
| Subject list: subject in use | Delete disabled, tooltip `Subjects.Delete.DisabledInUse`. |
| Subject list: subject with topics (and possibly in use) | Delete disabled, tooltip `Subjects.Delete.DisabledHasTopics` (BR9 order). |
| Topics section: topic in use | Delete disabled, tooltip `Topics.Delete.DisabledInUse`. |
| Delete refused at the click (409 in use) | Alert with `subject.in_use` / `topic.in_use`; list reloaded. |
| Permission denied | As F-74 and F-79: the pages are not reachable without `catalog.manage` ("page not found"). |

### Accessibility
- Section rows: each mapped row's chips are a `ul` labelled "Covers: <row label>", so a screen reader announces "Abrange: Raciocínio Lógico-Matemático, list, 3 items"; each chip reads "Matemática, disciplina inteira" or "Proposições, tópico de Raciocínio Lógico". The `Not mapped` chip is plain text after the label. Chip icons are `aria-hidden`; the meaning is always in the text.
- Tab order of a row is unchanged (move up, move down, edit, delete): the chips are not focusable.
- The footer's not-mapped line is plain text, not live.
- Dialog Covers field: the input has `role="combobox"`, `aria-expanded`, `aria-controls` (the listbox), `aria-autocomplete="list"`, `aria-describedby` (description and count line), `aria-invalid` on an error. The popup is a `listbox` with `aria-multiselectable="true"` and `aria-label` `NoticeSubjects.Field.Covers.Options`; each subject is a `group` labelled by its name; each option has `aria-selected`, and `aria-disabled` with its reason in its accessible description when disabled.
- Keyboard: Arrow down opens the list and enters it; Arrow up/down move through the options (groups included, disabled options included); Space or Enter toggles the option and keeps the list open; Home/End go to the first/last option; Esc closes the list and returns to the input without closing the dialog (a second Esc is the dialog's, F-74); Tab leaves the list closed. Typing while in the list returns to the input.
- Tab order of the field: search input → `Lookup.Clear` (when shown) → each chip's remove button in order → the dialog actions.
- Announcements (polite, visually hidden): result count while typing, picked/removed/cleared after each toggle, the taxonomy load failure.
- Real elements: options are listbox options (not links, not checkboxes); remove and clear are buttons; nothing on these screens navigates except the existing breadcrumbs and the subject name link on the list.
- Targets: chip remove buttons 24 px (rule minimum, inside a 28 px chip); options at least 36 px high; other buttons as F-74.
- Colours: only existing `SimulabTheme` tokens (no identity tokens file yet): chips use surface, divider and text-primary / text-secondary; the picked option uses primary-lighten with text-primary and a primary check; marked chips use the error colour for border and text, the pair already used by field errors; the `Not mapped` chip is `AppStatusChip` Warning (its dot is decorative, the text carries the meaning). No new colour.

### UI texts
New keys. `›` sits inside a resource format, so a language can change it. Reused unchanged: `Lookup.Clear`, `Lookup.NoResults`, `Lookup.Results`, `Common.TryAgain`, `NoticeSubjects.Saved`.

| Key | pt-BR | pt-PT | en |
|---|---|---|---|
| `Common.RemoveItem` | Remover {0} | Retirar {0} | Remove {0} |
| `NoticeSubjects.Field.Covers` | Abrange | Abrange | Covers |
| `NoticeSubjects.Field.Covers.Placeholder` | Busque uma disciplina ou um tópico | Pesquise uma disciplina ou um tópico | Search a subject or a topic |
| `NoticeSubjects.Field.Covers.Hint` | Opcional. As disciplinas e os tópicos da taxonomia que esta disciplina do edital cobre: escolha uma disciplina inteira ou só alguns tópicos dela. Até 50 itens. | Opcional. As disciplinas e os tópicos da taxonomia que esta disciplina do aviso abrange: escolha uma disciplina inteira ou apenas alguns dos seus tópicos. No máximo 50 itens. | Optional. The taxonomy subjects and topics this notice subject covers: pick a whole subject or only some of its topics. At most 50 items. |
| `NoticeSubjects.Field.Covers.Options` | Taxonomia: disciplinas e tópicos | Taxonomia: disciplinas e tópicos | Taxonomy: subjects and topics |
| `NoticeSubjects.Field.Covers.Picked` | Itens escolhidos | Itens escolhidos | Picked items |
| `NoticeSubjects.Field.Covers.Count` | Escolhidos: {0} de {1} | Escolhidos: {0} de {1} | Picked: {0} of {1} |
| `NoticeSubjects.Field.Covers.None` | Nada escolhido: a disciplina fica não mapeada. | Nada escolhido: a disciplina fica sem mapeamento. | Nothing picked: the subject stays not mapped. |
| `NoticeSubjects.Field.Covers.Loading` | Carregando a taxonomia... | A carregar a taxonomia... | Loading the taxonomy... |
| `NoticeSubjects.Field.Covers.LoadFailed` | Não foi possível carregar a taxonomia. Os itens já escolhidos continuam aqui. | Não foi possível carregar a taxonomia. Os itens já escolhidos continuam aqui. | The taxonomy could not be loaded. The items already picked stay here. |
| `NoticeSubjects.Field.Covers.EmptyTaxonomy` | A taxonomia ainda não tem disciplinas. Cadastre-as em Conteúdo › Disciplinas. | A taxonomia ainda não tem disciplinas. Registe-as em Conteúdo › Disciplinas. | The taxonomy has no subject yet. Add them under Content › Subjects. |
| `NoticeSubjects.Field.Covers.LimitReached` | Limite de {0} itens atingido. Remova um para escolher outro. | Limite de {0} itens atingido. Retire um para escolher outro. | Limit of {0} items reached. Remove one to pick another. |
| `NoticeSubjects.Covers.WholeMarker` | disciplina inteira | disciplina inteira | whole subject |
| `NoticeSubjects.Covers.Topic` | {0} › {1} | {0} › {1} | {0} › {1} |
| `NoticeSubjects.Covers.Topic.Spoken` | {1}, tópico de {0} | {1}, tópico de {0} | {1}, topic of {0} |
| `NoticeSubjects.Covers.CoveredByWhole` | Já coberto pela disciplina inteira | Já abrangido pela disciplina inteira | Already covered by the whole subject |
| `NoticeSubjects.Covers.WholeBlocked` | Remova os tópicos dela já escolhidos para escolher a disciplina inteira | Retire os tópicos dela já escolhidos para escolher a disciplina inteira | Remove its picked topics to pick the whole subject |
| `NoticeSubjects.Covers.Added` | Escolhido: {0}. | Escolhido: {0}. | Picked: {0}. |
| `NoticeSubjects.Covers.Removed` | Removido: {0}. | Retirado: {0}. | Removed: {0}. |
| `NoticeSubjects.Covers.Cleared` | Todos os itens foram removidos. | Todos os itens foram retirados. | All items were removed. |
| `NoticeSubjects.Covers.Overlaps` | repete a disciplina inteira | repete a disciplina inteira | repeats the whole subject |
| `NoticeSubjects.Covers.Gone` | não existe mais na taxonomia | já não existe na taxonomia | no longer in the taxonomy |
| `NoticeSubjects.Row.NotMapped` | Não mapeada | Não mapeada | Not mapped |
| `NoticeSubjects.Total.Unmapped.One` | 1 disciplina ainda não está mapeada. | 1 disciplina ainda não está mapeada. | 1 subject is not mapped yet. |
| `NoticeSubjects.Total.Unmapped.Many` | {0} disciplinas ainda não estão mapeadas. | {0} disciplinas ainda não estão mapeadas. | {0} subjects are not mapped yet. |
| `Subjects.Delete.DisabledInUse` | Disciplinas do edital estão mapeadas para esta disciplina inteira. Retire-a desses mapeamentos antes de excluir. | Há disciplinas do aviso mapeadas para esta disciplina inteira. Retire-a desses mapeamentos antes de a eliminar. | Notice subjects map this whole subject. Remove it from their mappings before deleting it. |
| `Topics.Delete.DisabledInUse` | Disciplinas do edital estão mapeadas para este tópico. Retire-o desses mapeamentos antes de excluir. | Há disciplinas do aviso mapeadas para este tópico. Retire-o desses mapeamentos antes de o eliminar. | Notice subjects map this topic. Remove it from their mappings before deleting it. |
| `notice_subject.mapping_invalid` | Um dos itens escolhidos não é válido. Remova-o e escolha de novo. | Um dos itens escolhidos não é válido. Retire-o e volte a escolhê-lo. | One of the picked items is not valid. Remove it and pick it again. |
| `notice_subject.mapping_too_many` | No máximo 50 itens por disciplina do edital. | No máximo 50 itens por disciplina do aviso. | At most 50 items per notice subject. |
| `notice_subject.mapping_target_not_found` | Um dos itens escolhidos não existe mais na taxonomia; ele está marcado abaixo. Remova-o e salve de novo. | Um dos itens escolhidos já não existe na taxonomia; está assinalado abaixo. Retire-o e guarde novamente. | One of the picked items is no longer in the taxonomy; it is marked below. Remove it and save again. |
| `notice_subject.mapping_overlap` | Um tópico não pode ser escolhido junto com a disciplina inteira dele; os itens estão marcados abaixo. Remova o tópico ou a disciplina inteira. | Um tópico não pode ser escolhido juntamente com a disciplina inteira a que pertence; os itens estão assinalados abaixo. Retire o tópico ou a disciplina inteira. | A topic cannot be picked together with its whole subject; the items are marked below. Remove the topic or the whole subject. |
| `subject.in_use` | Esta disciplina está mapeada em disciplinas do edital e não pode ser excluída. A lista foi recarregada. | Esta disciplina está mapeada em disciplinas do aviso e não pode ser eliminada. A lista foi recarregada. | Notice subjects map this subject, so it cannot be deleted. The list was reloaded. |
| `topic.in_use` | Este tópico está mapeado em disciplinas do edital e não pode ser excluído. A lista foi recarregada. | Este tópico está mapeado em disciplinas do aviso e não pode ser eliminado. A lista foi recarregada. | Notice subjects map this topic, so it cannot be deleted. The list was reloaded. |

## Acceptance criteria
- AC1 Given a notice subject "Raciocínio Lógico-Matemático", when the admin maps it to Mathematics (whole) and to the topic Logical Reasoning › Propositions and saves, then both entries are stored and the row shows both chips (UC2, BR1, BR13).
- AC2 Given an entry with both `subjectId` and `topicId`, or with neither, when saved, then 400 `notice_subject.mapping_invalid` and nothing changes (BR1).
- AC3 Given 51 entries, when saved, then 400 `notice_subject.mapping_too_many` (BR1).
- AC4 Given an unknown or deleted subject or topic id in the body, when saved, then 400 `notice_subject.mapping_target_not_found` (BR3).
- AC5 Given Mathematics (whole) and the topic Mathematics › Fractions in one save, then 400 `notice_subject.mapping_overlap`; given Mathematics (whole) and Logical Reasoning › Propositions, then it is stored (BR4).
- AC6 Given a row mapped to Mathematics (whole) and topic T of Logic, when a curator moves T to Mathematics, then the move succeeds and the row shows both entries, T now under Mathematics (BR4, BR8).
- AC7 Given the same topic twice in one save, then it is stored once (BR5).
- AC8 Given two notice subjects of one edition, when both map the same topic, then both are stored (BR6).
- AC9 Given a row mapped to A and B, when the admin saves it with only C, then the mapping is exactly C; saved with an empty list, the row shows "Not mapped" (UC3, BR7, BR2).
- AC10 Given an edition with three rows of which one is not mapped, when the section loads, then that row shows "Not mapped" and the footer says one row is not mapped; the edition can still be published (UC1, BR2).
- AC11 Given a topic mapped by a live notice subject, when a curator deletes it, then 409 `topic.in_use` and its delete action was disabled with the reason; given a subject mapped whole, then 409 `subject.in_use` with the same screen behaviour (UC4, BR9, BR10).
- AC12 Given a topic mapped only by a deleted notice subject or by a notice subject of a deleted edition, when the curator deletes the topic, then it is deleted (BR9, BR11).
- AC13 Given the taxonomy endpoint, when called, then it returns the live subjects alphabetically, each with its live topics alphabetically, and no deleted item (Screens and API).
- AC14 Given a user without `catalog.manage`, when the taxonomy endpoint or a notice subject endpoint with mappings is called, then 403 (BR12).
- AC15 Given two rows inserted directly with the same notice subject, `tenant_id` null and the same topic, then PostgreSQL rejects the second; given a row with both `subject_id` and `topic_id`, then the check constraint rejects it (schema).
- AC16 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-10-04 — A mapping entry is a whole subject or one topic — notices often name only a subject, others name single laws or topics (owner, question 1).
- 2026-10-04 — A topic together with its own whole subject on one row is refused — the whole subject already covers it; keeps analytics weights clean (owner, question 2).
- 2026-10-04 — The same canonical item may be mapped by several rows of one edition — real notices repeat a law under two headings (owner, question 3).
- 2026-10-04 — Deleting a mapped subject or topic is refused with 409, delete disabled with its reason — the F-79 `subject.has_topics` pattern; a silent unmap would leave holes nobody sees (owner, question 4).
- 2026-10-04 — Mapping is optional; unmapped rows are marked and counted, publishing is not blocked — F-74 keeps published editions editable (owner, question 5).
- 2026-10-04 — The mapping is edited in the F-74 add/edit dialog, new field "Covers" — one place to edit a notice subject (rule `ui-project`) (owner, question 6).
- 2026-10-04 — F-86, when built, copies the mappings too; a dated line was added to its summary — the mapping is the slow part to retype (owner, question 7).
- 2026-10-04 — No new package, production or tests — EF Core, the MudBlazor kit, xunit, bunit and Testcontainers.PostgreSql already cover it (owner, question 8).
- 2026-10-04 — One table with nullable `subject_id` and `topic_id` and a check constraint, the topic entry storing only the topic — a topic move (F-79 BR10) needs no update of the mappings (Claude, technical).
- 2026-10-04 — The overlap of BR4 is checked on save only, not on a topic move — refusing a move because of an edition's mapping would couple the taxonomy screens to every notice (Claude, technical).
- 2026-10-08 — SUPERSEDED by change note 1: mapping rows were to be link rows without soft delete.
- 2026-10-04 — At most 50 entries per notice subject — the widest real heading lists about 20 laws; a ceiling stops a runaway request (Claude, technical).
- 2026-10-04 — The picker loads the whole taxonomy in one call (`GET /api/v1/catalog/taxonomy`) — the F-51 guard taxonomy is 23 subjects and 70 topics; not measured at larger sizes, revisit with E-9 imports (Claude, technical).
- 2026-10-04 — Mapping lives in the `Catalog` module, `catalog` schema — epic decision; plain foreign keys inside one schema (Claude, epic decision).
- 2026-10-04 — Two new kit components, `AppMultiPickField` and `AppChip`, each with a `/dev/ui` gallery entry, built on MudBlazor parts with no new package — the kit has no grouped multi-pick and no general chip; question filters (E-4) and aliases (F-77) reuse them (owner, screen question 1).
- 2026-10-04 — A whole-subject option is disabled, with its reason, while some of its topics are picked; picking it never removes picks — nothing vanishes without the admin seeing it (owner, screen question 2).
- 2026-10-04 — The in-use reason does not say where the item is used; a "where used" view is captured as F-90 (owner, screen question 3).
- 2026-10-04 — Field label "Abrange" / "Covers" and row marker "Não mapeada" / "Not mapped", as in `### UI texts` (owner, screen question 4).
- 2026-10-04 — A row with many entries wraps its chips (no "more" collapse), and chips do not show the area — the widest real heading has about 20 entries; BR13 asks only for "Subject › Topic" (Claude, screen design).

- 2026-10-08 — Design passes (`system-design`, `architect`) verified against the files. Accepted: save replaces the mapping by a diff (soft-delete dropped entries, keep unchanged, add new) in the same `SaveChanges` as the row; a shared `LiveNoticeSubjectMappings` query relies on the global soft-delete filters of `NoticeSubject` and `ExamEdition`; the BR4 rules class receives the saved mapping so an overlap made by a topic move is kept (AC6); it is named `NoticeSubjectMappingRules` (the glossary retires "Coverage"); `mapping_target_not_found` is `ErrorKind.Validation` (400); the 50 limit counts after dedup; `mappings` omitted or empty clears (BR7), so the API and the dialog change in the same commit; `inUse` is computed in the subject and topic save answers too; the new unique index violation (concurrent save) becomes a 409; routes keep their existing `/api/v1/catalog/exams/...` prefix (the item text omitted `/catalog`). Dropped: nothing. Owner stop: none besides change note 1 (owner, session 2026-10-08).

- 2026-10-08 — Independent review (reviewer agent): three major findings in the dialog (overlap comparison per subject instead of per topic, stale overlap marks, a test locking the mismatch) fixed and re-run green; two minor ones fixed (outside-click listeners now removed on dispose) or reworded (the Esc-stays-in-the-field guarantee is on screen in validation step 7, bUnit cannot see it) (Claude).

## Out of scope
- AI suggestion of the closest topic or a new draft topic — epic E-9.
- Aliases and lookup by alias — F-77.
- The student's view of the syllabus with its topics — F-76.
- Copying notice subjects and their mapping from another edition — F-86.
- Mapping the municipal guard editions — F-78.
- Weight and minimum per notice subject — epic E-6.
- Showing where a subject or topic is used — F-90.
- Weighting the entries of one mapping (how much of a row is Mathematics vs. Logic) — not asked by any epic yet.

## Open questions
- (none)

## Change notes
- 1 (2026-10-08, owner): `NoticeSubjectMapping` inherits `TenantEntity` (soft delete and tenant filter) instead of being a link row without soft delete, as `project.md` requires ("soft delete only", `User` the only exception). The unique index is filtered by `is_deleted = false` so a dropped entry can be added again; an index on `notice_subject_id` is added. Affects the table line of `## Screens and API` and the Decisions line of 2026-10-04 on link rows; AC15 still holds; no other criterion changes.

## Validation script
Needed to validate: the app host started from this worktree, an Admin account signed in by you, and an edition with a few notice subjects. The F-51 guard taxonomy (23 subjects, 70 topics) is already in the database by migration, so the Covers list has content. Claude did not start the app host (it starts the local PostgreSQL, Redis and Mailpit containers, which needs your yes) and does not enter credentials; the two commands below were not run by Claude for that reason. Close any app host running from another checkout first: two hosts fight for the same ports. The migration `AddNoticeSubjectMappings` is applied at start (Development), in a database of this item.

Git Bash, from `D:/wt/simulab/f-75-map-notice-subjects`:

```bash
Database__Name=simulab_f75 dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https
```

PowerShell 7, from the same folder:

```powershell
$env:Database__Name = "simulab_f75"; dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https
```

Expected: the Aspire dashboard URL is printed and the Web is at https://localhost:7125, with no `fail` line while the migrations apply. To repeat, stop it (Ctrl+C) and run it again.

1. Sign in as `admin@simulab.local`. Open **Exams**, an exam, then an edition. If its **Notice subjects** card is empty, add three rows (`Portuguese`, `Math`, `Law`). Every row shows the chip **Not mapped** (warning colour) under its name, and the footer says "3 subjects are not mapped yet." The edition still saves and publishes.
2. Edit **Math**. After the number of questions there is the field **Covers** with the hint and "Picked: 0 of 50". Click the field or press Arrow down: the list opens with every subject, each starting with its whole-subject option ("whole subject") followed by its topics. Type a few letters of a subject (accents and case do not matter): the list filters; a subject whose name matches shows all its topics. Pick one subject whole and one topic of **another** subject; the list stays open, a check mark shows the picks, and the chips below read "Subject · whole subject" and "Subject › Topic". Save: the snackbar says "Notice subject saved.", the row shows both chips (subject first, then its topics) and the footer now says 2 subjects are not mapped.
3. Edit **Math** again and try the comfort rules: with a subject picked whole, its topics are greyed with "Already covered by the whole subject"; remove that subject's chip (its X button), pick one of its topics, and now its whole-subject option is greyed with "Remove its picked topics to pick the whole subject". **Clear the selection** removes every chip; with nothing picked the dialog says "Nothing picked: the subject stays not mapped." Pick one subject again and Save; then edit and Save with everything cleared: the row goes back to **Not mapped**.
4. Map **Portuguese** and **Law** to the same topic (a law listed under two headings): both save. Go to **Content › Subjects**: a subject with topics has its **Delete** disabled and the tooltip says it has topics (that reason comes first, even when the subject is also mapped whole). Add a new subject with no topics (for example `Validation subject`), go back to the edition and map **Math** to it whole, then return to the subject list: its **Delete** is disabled and the tooltip says notice subjects map this whole subject. Open the subject of the topic you mapped: that topic has **Delete** disabled with its reason; an unmapped topic still offers Delete (do not delete seeded data: cancel the confirmation). Clear the mapping of the rows that use them: the Delete buttons enable again. Delete `Validation subject` at the end.
5. Move a topic: on the subject page edit the mapped topic and move it to another subject. Back on the edition page its chip shows the new subject ("New subject › Topic") without editing the row.
6. Switch the language to **pt-PT** and then **pt-BR**: the field, the hint, the chips, the messages and the tooltips are in that language (pt-BR: "Abrange", "Não mapeada", "disciplina inteira"). Light and dark theme: the chips, the Not mapped chip, the greyed options and the picked option stay readable.
7. Keyboard only: Tab to **Covers**, Arrow down opens the list, Arrow up/down move through options (greyed ones included, they read their reason), Space or Enter toggles and keeps the list open, Home/End jump, Esc closes only the list and the dialog stays open; Tab goes to **Clear** and then to each chip's remove button (Enter removes it and focus moves to the next chip); a second Esc on a dialog with changes asks before discarding. A screen reader (if you use one) announces the number of results, each pick and each removal.
8. Permission check: sign in as a Student. `/admin/exams`, the edition page and `/admin/subjects` show Page not found, so none of this is reachable.

## Delivery
<!-- Filled by /agile:ship. -->
