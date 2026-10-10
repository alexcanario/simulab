# agile@canary — Worked examples (en)

> Part of the [manual](workflow.md). Português: [pt-BR](workflow-examples.pt-BR.md).

## 14. Worked examples

All examples use a fictional app, **ExamPrep**: a platform where curators build a bank of exam questions and students practice with them. `>` marks what you type; `Claude:` marks the reply (shortened). In real sessions the chat is in Portuguese; it is shown in English here.

### 14.1 Bootstrapping a new app

**Before:** an empty repository with `product/brief.md`:

```markdown
# ExamPrep
Problem: candidates study with scattered PDFs of past exams.
Users: curators (build the question bank), students (practice).
Capabilities: exam boards and exams, question bank (8 question types), practice tests, progress reports.
Constraints: Brazil and Portugal; web first; one developer.
Out of scope (v1): payments, mobile app.
```

```
> /agile:bootstrap
```

```
Claude: I read product/brief.md. Round 1 of 7 — Shape.

1. Type of app → Recommendation: web app with an API (brief says "web first").
2. Architecture profile → Recommendation: modular-monolith.
   Reason: 4 business areas (catalog, question bank, practice, reports) with clear
   boundaries, but one developer — one deployable keeps operations simple.
3. Deployment target → Recommendation: containers, local with .NET Aspire, Azure later.

Answer each one ("ok" accepts the recommendation) or tell me what to change.
```

```
> 1 ok. 2 ok. 3 ok, but no Azure decision yet.
```

```
Claude: Recorded: deployment target = containers + Aspire; cloud provider deferred.

Round 2 of 7 — Data.
4. Database → Recommendation: PostgreSQL.
5. Multi-tenancy → Question: will schools or prep courses have private question banks?
   The brief does not say. If yes, tenant isolation must exist from day one.
...
```

Round 6 always carries one question the brief cannot answer — where the item folders live:

```
Claude: 28c. Worktree root → Recommendation: D:\wt\examprep (this machine has a D: drive;
    without one it would be C:\wt\examprep). Outside the repository; each item gets a
    folder such as f-3-exam-board — the id and at most 20 characters of its slug.
> ok
```

After round 7, Claude asks the closing question, settles what it raises, shows a summary of every decision and asks for confirmation. Then it creates:

```
CLAUDE.md                                  52 lines, points to the profile
docs/decisions/ADR-0001-foundation.md      every decision, with its reason
docs/agile/profile.md                      modular-monolith profile
src/ExamPrep.AppHost, src/ExamPrep.Api, src/ExamPrep.Web, src/Modules/...
tests/...                                  unit + integration (shared Postgres container)
Resources/*.pt-BR.resx, *.pt-PT.resx, *.en.resx
docs/manual/{pt-BR,pt-PT,en}/index.md
```

and the epics from the brief on the board: *Catalog*, *Question bank*, *Practice tests*, *Progress reports*.

### 14.2 A feature from idea to done

**Idea.** During a chat about the catalog:

```
> Curators need to register exam boards (name and acronym). Register it.
```

```
> /agile:idea "Exam board registration: name and acronym, list and edit"
```

```
Claude: Epic for F-3 "Exam board registration"?
        1. Catalog (Recommended) - the epic's goal is registering what the candidate searches
        2. Content - exam content
        3. New epic: "Exam boards" - registration and upkeep of exam boards
        4. No epic
You: 1
Claude: Created feature F-3 "Exam board registration" (board #12) under epic "Catalog".
Status: idea. Nothing else is done until we refine it.
```

**Refinement.** When it is this feature's turn:

```
> /agile:refine F-3
```

```
Claude: Folder for this item: D:/wt/examprep/f-3-exam-board [feature/F-3] — created now,
outside the repository; the file moved in and everything about F-3 is written there.
I checked the code: the Catalog module exists but has no entities yet.
The user and role model from bootstrap has Curator and Admin.
```

An item about evals is the exception to "ask everything at once": runs per case and the cost ceiling are proposed only after Claude runs one case once and quotes what it cost (worked example 14.26).

The questions come as quiz cards, one per topic, the recommended option first. In the desktop app you click; without the question tool (a terminal, a headless run) each question arrives as a quoted block with the `❓` marker, answered with "ok" or a letter. The first card, "Rules":

```
[Rules]
1. Is the acronym unique?
   ● Yes, case-insensitive (Recommended) — "FGV" and "fgv" are the same board
   ○ Yes, case-sensitive
   ○ No
2. Can an exam board be deleted?
   ● Deactivate only (Recommended) — exams will reference it
   ○ Hard delete
3. Maximum lengths?
   ● Name 120, acronym 20 (Recommended)
   ○ Other (type them)
```

Then "Permissions" and "Screens":

```
[Permissions]
1. Who can edit?
   ● Curator and Admin (Recommended) — curators own the catalog
   ○ Admin only
[Screens]
1. Is the list paged or searchable?
   ● Search by name/acronym, 20 per page (Recommended)
   ○ Plain list, no search
```

```
> Rules: all recommended. Permissions: Admin only. Screens: recommended.
```

Claude writes `docs/features/F-3-exam-board-registration.md`:

```markdown
---
feature: F-3
status: refining
board: 12
---
# Exam board registration

## Goal
Let admins keep the list of exam boards used by exams and questions.

## Users and use cases
- UC1 Admin registers an exam board (name, acronym).
- UC2 Admin edits an exam board.
- UC3 Admin deactivates / reactivates an exam board.
- UC4 Admin and curators search the list.

## Business rules
- BR1 Acronym is unique, case-insensitive.
- BR2 Deactivated boards cannot be chosen for new exams but keep existing links.
- BR3 Name ≤ 120, acronym ≤ 20 characters.

## Screens and API
- /catalog/exam-boards (list, search, 20 per page)
- /catalog/exam-boards/new and /{id}/edit
- GET/POST/PUT /api/v1/catalog/exam-boards; POST .../{id}/deactivate|reactivate

## Acceptance criteria
- AC1 Given a new name and acronym, when saved, then the board appears in the list.
- AC2 Given an existing acronym in another case, when saved, then error `exam_board.acronym_taken`.
- AC3 Given a curator, when opening the new/edit screens, then access is denied.
- AC4 All texts appear in pt-BR, pt-PT and en.

## Decisions
- 2026-09-20 — Admin only edits (not curators) — owner decision.

## Out of scope
- Import from a file.

## Open questions
- (none)
```

```
Claude: F-3 is ready for approval. Read the file; reply "approve F-3" or tell me what to change.
> approve F-3
```

Status becomes `approved` (**Gate 1**).

**Build.**

```
> /agile:build F-3
```

```
Claude: No other feature is in progress. Branch feature/F-3 created. Status: building.
...
Done:
- ExamBoard entity + configuration + migration AddExamBoards
- Endpoints with error codes exam_board.acronym_taken / exam_board.not_found
- Pages List / Form, texts in 3 resource files
- 14 tests (Catalog.Tests 3.1 s, Web.Tests 1.8 s) — green

Criterion → test:
| AC1 list        | ExamBoardsPage_Open_ShowsActiveBoards                        |
| AC3 unique      | CreateExamBoard_DuplicateAcronym_Returns409                  |
| AC5 deactivate  | ExamBoardsPage_Deactivate_ShowsInactive                      |
| AC7 languages   | validation script, step 7                                    |
AC5 was first a gap: ExamBoard.Deactivate had a passing test, but no page or
endpoint called it. Added the endpoint and the button, then the page test above.

Validation script (F-3):
1. Start the AppHost and sign in as admin.
2. Menu Catalog → Exam boards: empty list.
3. New: "Fundação Getulio Vargas" / "FGV" → saved, appears in the list.
4. New: "Other" / "fgv" → message "Acronym already in use".
5. Edit FGV, change the name → list shows the new name.
6. Deactivate FGV → status "Inactive"; reactivate it.
7. Switch language to pt-PT and en → texts change.
8. Sign in as curator → New/Edit are not available.
Status: validating.
```

When a step needs a terminal (a limit, an endpoint with no screen yet), it comes for both shells, already run by Claude:

```
4. Six sign-up attempts in a row from the same address: the 6th is refused.
   Git Bash:     for i in 1 2 3 4 5 6; do curl -sk -o /dev/null -w "%{http_code}\n" -X POST https://localhost:7101/api/v1/auth/sign-up -H "Content-Type: application/json" -d '{}'; done
   PowerShell 7: 1..6 | % { (Invoke-WebRequest -Method Post https://localhost:7101/api/v1/auth/sign-up -ContentType application/json -Body '{}' -SkipCertificateCheck -SkipHttpErrorCheck).StatusCode }
   Expected: 400 five times, then 429. To repeat: wait one minute.
```

**Validation** (**Gate 2**):

```
> Step 4 shows the message, but it is in English even in pt-BR.
Claude: A resource key was missing in the pt-BR file. Fixed; test added for AC4. Try step 4 again.
> ok, validated.
```

**Ship** (**Gate 3**):

```
> /agile:ship F-3
```

```
Claude: Full suite: 212 tests green (2 min 10 s). Architecture tests green.
The run rewrote docs/api/ExamPrep.Api.json (a new route): committed with F-3.
Merged feature/F-3 into main (--no-ff), pushed; branch and worktree removed. Board #12 closed, Status Done (read back). Manual updated: "Exam boards" page in pt-BR, pt-PT, en.
Retro: one lesson — "check every language file when adding a message". Add it as a rule?
> yes
Claude: Rule added to core/i18n. Status: done.
```

When the Status write does not stick — a permission error, or a board with no Done option under that field — the close line names the problem and the exact command instead of silently moving on:

```
Claude: Merged. Board #12 closed; Status is still "In Progress" after one retry —
set it by hand: gh project item-edit --id PVTI_xxx --field-id PVTF_xxx --project-id PVT_xxx --single-select-option-id <done>.
Manual updated: "Exam boards" page in pt-BR, pt-PT, en.
```

### 14.3 Changing your mind during build

While F-7 "Question registration" is in `building`:

```
> I want the "year" field to be optional for questions without an exam.
> /agile:change F-7
```

```
Claude: Change note added to F-7:
- What: Year becomes optional when no exam is linked.
- Why: authored questions have no exam year.
- Affected: BR4 (year required) and AC5. Other criteria unchanged.
Re-approve BR4 and AC5 as rewritten below? (the rest of the work continues)
> approve
```

The build continues from where it was. Nothing is redone.

### 14.4 A wrong premise found during build

```
Claude: Stopping F-9 "Edit question exam". The feature assumes the edit screen already
lets users change the subject, but it has no subject, board or year fields, and the
update API does not accept them. Options:
A. Add subject/board/year editing to F-9 (bigger, but the feature works end to end).
B. Keep F-9 to exam link only; register a new feature for the missing fields.
Recommendation: A, because rule BR3 ("choose a covered subject") is impossible without it.
> A
Claude: Change note added; scope and criteria updated for your approval.
```

### 14.5 A bug after ship

```
> The exam board list does not find "FGV" when I search "fgv".
> /agile:idea "Bug: exam board search is case-sensitive"
Claude: Created bug B-2 (board #31), linked to F-3.
> /agile:refine B-2
Claude: Cause confirmed in the query (plain equality). One question: should search also
ignore accents ("Fundacao" finds "Fundação")? Recommendation: yes.
> yes
```

The bug follows the same lifecycle, with one short file: cause, expected behavior, a test that reproduces it, and the validation script.

When the cause is a business rule, refinement also searches the whole solution for the same rule implemented again elsewhere (common in apps that grew without a shared rules layer), writes every occurrence under `## Cause` before asking anything, and asks which ones to fix now and which to record as debt:

```
> /agile:refine B-4
Claude: Cause confirmed: Enrollment/CpfValidator.cs:41 skips the second check digit.
The same check is implemented again in two other places (not callers of the validator):
  - Import/StudentCsvReader.cs:88 (own copy, same defect)
  - Web/Pages/Enroll.razor.cs:132 (own copy, correct)
All three are listed under ## Cause in docs/bugs/B-4-cpf-check-digit.md.
[card: Business rules]
  Which occurrences does B-4 fix?
  ● All three: the reader and the page call CpfValidator (Recommended)
    Only the validator and the reader; the page copy stays as recorded debt
    Only the validator
> (picks the first)
Claude: B-4 approved. The build adds one regression test per occurrence fixed.
```

### 14.6 Pausing and resuming

You stop whenever you want, in your own words or with the command; both do the same thing. In the middle of F-7:

```
> /agile:pause
Claude: Paused. Commit "wip(F-7): question form without type editor" on feature/F-7 (local, not pushed).
Next: type editor for T1-T3. Decision pending with you: none. Nothing running.
```

"Vamos parar por hoje" or "continuo amanhã" does exactly the same. If you just close the session, nothing is lost either: the next session starts by showing the uncommitted work, and Claude commits it as `wip` before anything else, with your OK.

Next session:

```
> /agile:status
```

```
Claude:
In progress: F-7 Question registration — building (branch feature/F-7, 1 wip commit)
Next step: type editor for T1-T3
Open questions: none
Backlog head: F-8 Question review queue (idea), B-3 Search accents (idea)
```

To continue, run the build again on the same item; Claude resumes instead of starting over:

```
> /agile:build F-7
Claude: Resuming F-7 from wip commit "question form without type editor" on feature/F-7.
Next: type editor for T1-T3 (step 4 of the plan). Continuing.
```

### 14.7 From a loose idea to an epic

```
> I'm thinking about practice tests that adapt to the student's level. Not sure how.
> /agile:discuss "adaptive practice tests"
```

```
Claude: What we know: the brief lists practice tests; questions have no difficulty field yet.
Options:
A — Fixed tests chosen by the curator (S). Simple; no adaptation.
B — Difficulty levels on questions + tests that pick by level (M). Needs difficulty data.
C — Adaptive engine that adjusts during the test (L). Needs history and tuning.
Recommendation: B now, C later — B creates the difficulty data C will need.
Questions:
1. Who sets the difficulty? → Recommendation: curator, with 3 levels.
```

```
> B, 3 levels, curator sets it. Park C.
Claude: D-1 recorded (decided; C parked). Captured F-12 "Question difficulty" and
F-13 "Practice test by level". They belong to a new epic "Practice tests" — plan it now?
> /agile:epic "Practice tests"
```

```
Claude:
| Id   | Feature                 | Priority | Size | Depends on | Waits on                       | Screen design |
| F-12 | Question difficulty     | Must     | S    | -          | -                              | no            |
| F-13 | Practice test by level  | Must     | M    | F-12       | -                              | yes           |
| F-14 | Practice test results   | Must     | M    | F-13       | score rules from the curator   | yes           |
| F-15 | Retry wrong answers     | Should   | S    | F-14       | -                              | no            |
Execution plan:
1. F-12 — first: the others need the difficulty data — /agile:autopilot F-12 (small and clear)
2. F-13 — /agile:refine with /agile:screen, then /agile:build — beside it: nothing
3. F-14 — /agile:refine once the curator sends the score rules — beside it: F-15 in a worktree
4. F-15 — /agile:refine → /agile:build
Waiting outside the epic: score rules for F-14, from the curator; you ask.
First release cut: F-12 to F-14.
1. Is F-15 in the first release? → Recommendation: no.
> ok
Claude: docs/epics/practice-tests.md agreed. F-14 and F-15 captured, each with its Start.
Next: /agile:refine F-12 (waits on nothing).
```

Later, `/agile:status` shows the wait until it is cleared:

```
Blocked: F-14 — waits on the score rules, from the curator | F-15 — depends on F-14 (idea)
Backlog head: F-12 (done), F-13 (approved) — can start, F-14 (idea), F-15 (idea)
```

### 14.8 Screen design and review

During the refinement of F-13:

```
> /agile:screen F-13
Claude: Design delegated to the ux-designer agent, alone in the item's worktree.
It wrote the screen section and the mockup; I read both files before showing them:
7 states, every text in the three languages, only kit patterns,
new colour 4.8:1 on the card and 5.1:1 on the page.
Mockup sent: docs/features/mockups/F-13-practice-test-by-level.html
Decisions: one question per page (mobile-friendly); timer visible only when the test has a limit.
Its open point — 1. Can the student go back to a previous question? → Recommendation: yes, until finishing.
> yes
> approve F-13
```

The agent never talks to you. It designs; Claude reads the two files it wrote, asks its open points as its own questions and collects your approval. In the build of the same feature a second agent, `frontend`, implements that approved mockup and the tests of that screen, alone in the worktree; Claude then reads what it wrote, runs the gate and quotes the real counts. Domain, API and migrations stay with Claude, and a feature with no approved mockup is built exactly as before, with no agent. Both models are yours, in the "Models" section of `CLAUDE.md`.

At the end of the build of F-14 (it writes results and changes the students' data):

```
Claude: This change touches student data. Running /agile:review before validation.
| Severity | Finding                                   | Verdict   | Action               |
| blocker  | Results endpoint does not check the owner | confirmed | fixed + test         |
| minor    | Duplicate mapping in ResultsPage          | confirmed | fixed                |
| major    | "Score can be negative"                   | rejected  | BR2 allows penalties |
Criterion → test: AC1-AC6 covered; AC7 (language switch) in the validation script.
Status: validating. Validation script: ...
```

### 14.9 Two items in parallel

F-6 is a long build in one session. A bug appears and the owner wants it fixed meanwhile, in another session:

```
> /agile:build B-2 --worktree
Claude: F-6 is building in its own worktree (feature/F-6). B-2 touches Identity/Login only;
F-6 touches Catalog. No shared schema or screen: they can run in parallel.
B-2 already has the worktree from its refinement: D:\wt\examprep\b-2-exam-board-search. Continue there?
> yes
Claude: B-2 in D:\wt\examprep\b-2-exam-board-search, status building (on that branch).
Only one app host at a time: stop the one from the main checkout before I open the screen here.
```

At the start of any session, the status shows both:

```
[agile] branch feature/F-6
In progress: F-6 Practice test by level — building (docs/features/F-6-...)
In progress in a worktree: B-2 — building [bug/B-2] D:/wt/examprep/b-2-exam-board-search
```

Shipping the bug: the full check runs in the worktree, the merge in the main checkout.

```
> /agile:ship B-2
Claude: Shutting down the .NET build server, then running the full suite in the worktree.
Full suite green (212 tests, 1 min 58 s). Merged bug/B-2 into main. Lock probe on
D:\wt\examprep\b-2-exam-board-search passed. Worktree removed, branch deleted, 0 0 against origin/main.
remote branch: never pushed, nothing to delete.
F-6 is now 1 commit behind main: bringing it up to date before its next test run.
```

The item branch lives only on your machine unless someone pushed it, so there is usually nothing to delete on the remote. Claude asks the remote first with `git ls-remote` and runs `git push` with `--delete` only when the branch is there; before 0.0.71 the ship tried it every time and ended with `error: unable to delete 'bug/B-2': remote ref does not exist`.

### 14.10 Updating the plugin and the project

A lesson for a plugin starts in a project's retro. The project's `CLAUDE.md` lists the plugins it uses on its `Plugins:` line: bootstrap writes `Plugins: agile`, and another plugin's adoption appends its own short name (`Plugins: agile, lens`). Each plugin note names its plugin next to its scope, and becomes a ⏳ row under "Plugin notes — status" in `docs/agile/retro-log.md`, with a Plugin column. In a project that uses both:

```
Claude: Lessons of F-12:
1. Plugin note → plugin: lens [generic] — the entry-point map missed a call made through a
   generic repository base class (Orders/OrderService.cs:88).
2. Project rule → project.md: "A new endpoint gets its authorization test in the same commit."
Approve?
> aprovo as duas
Claude: Retro log: ⏳ row for lens added. The legacy-lens session takes it in; agile leaves it alone.
```

A lesson that names a cause shows how the cause was measured, or says it was not. An item captured from it carries the same line:

```
Claude: Lessons of F-6:
1. Plugin note → plugin: agile [stack: dotnet] — the gate built with the 11.0 preview, not the SDK the
   solution pins. Evidence: `dotnet build adapters/dotnet/Lens.DotNet.slnx -getProperty:NETCoreSdkVersion`
   gives 11.0.100-rc.1 from the root and 10.0.401 from adapters/dotnet.
2. New item → "Clear the 911 baseline warnings": cause not verified (seen: 911 warnings accepted since
   the adoption). Its Start says "Cause not verified: measure it at /agile:refine".
Approve?
```

Each lesson also comes with two scores from 0 to 10 (Example 14.66): `plugin`, how much it matters to the plugin and to every project that uses it, and `project`, how much it matters to this app. The scale is fixed (plugin: 0 a one-off, 3-4 one stack, 5-6 one profile, 7-8 every profile, 9-10 a silent wrong answer or seen twice; project: 0 nothing changes, 5-6 would recur on this app soon, 9-10 already caused a defect), each number has a one-line reason, and a score of 7 or more cites its evidence, otherwise it stops at 6 and says `no evidence yet`. The scores only order and recommend: the 3-lesson cap stays, you decide, and a lesson with both scores of 2 or less is shown last as `not recommended`, never dropped unseen. A plugin note carries its scores in its own text (`⏳ plugin: agile [generic] score 8/5 — ...`, plugin first), so the table keeps its columns and an older note simply has none.

With no `Plugins:` line, or only `agile` on it, every note is `plugin: agile` and nothing is asked. An older table with no Plugin column gets the column, and `agile` in every row, from the next retro that writes to it. `/agile:status` counts the waiting notes per plugin ("Plugin notes waiting: 2 agile, 1 lens"). The agile plugin session takes in only `agile` rows, and it never writes in the project's repository (since 0.0.68). It comments on the issue it delivered and lists that issue in the plugin's `scripts/delivered.json`. The project's own session marks the note ✅ at its next `/agile:sync`. From the plugin's session:

```
> vamos ajustar o plugin com as recomendações em retro-log.md
Claude: 2 open notes (F-8), agile-canary#31 and #32. Done in 0.0.29: bUnit waits (4 profiles),
Npgsql pool (5 profiles), manuals en/pt-BR. Both issues closed with a comment and listed as delivered.
1. Push the plugin?  2. Update the installed plugin?
> pode fazer o push e atualizar o plugin
```

Each is asked for separately. The project gets the new files, and its ✅ marks, only through `/agile:sync`, between features, run by its own session.

The plugin moved from 0.0.10 to 0.0.13. First the plugin itself (terminal, then a new session):

```powershell
claude plugin marketplace update canary
claude plugin update agile@canary
```

```bash
claude plugin marketplace update canary
claude plugin update agile@canary
```

In a new session, `/agile:version` says where you stand:

```
> /agile:version
Claude: agile@canary 0.0.13 in this session. Project copies: 0.0.10.
Next step: /agile:sync, between features.
```

Then, in the project, between features:

```
> /agile:sync
Claude: agile@canary 0.0.10 → 0.0.13.
| File                                   | State  | Proposal                                              |
| .claude/rules/agile/git.md             | update | copy: branch check before every commit, guard hook    |
| docs/agile/workflow.md                 | update | copy: manual 0.0.13                                   |
| docs/agile/profile.md                  | edited | merge: two new test lines; your Simulab section stays |
| .editorconfig                          | manual | one line differs: CA1716 = none. Add it?              |
| CLAUDE.md                              | manual | no Worktrees: line. Add "Worktrees: D:\wt\examprep"?  |
| CLAUDE.md                              | manual | no Plugins: line. Add "Plugins: agile"?               |
Nothing else changed. Your project.md and ui-project.md are not touched; existing worktrees keep their names.
> ok
Claude: Copied 2, merged 1 (no conflicts), .editorconfig edited. Build 0 warnings; 252 tests green (9 s).
The plugin also ships DocGen (technical docs from the code, since 0.0.21), which bootstrap installs and this project does not have. Capture it as a feature?
Recorded 0.0.13 in .claude/agile/sync.json. Commit "chore: sync with agile@canary 0.0.13" on main. Authorize?
```

A project with no warnings baseline, such as a legacy repository just adopted by legacy-lens, is offered one, because until the file exists the gate calls no warning new:

```
> /agile:sync
Claude: agile@canary 0.0.64 → 0.0.65.
| File                                   | State   | Proposal                                                    |
| .claude/rules/agile/git.md             | update  | copy: one line about item worktrees                         |
| warnings baseline                      | missing | take it now (full rebuild of src/Billing.sln, may take minutes) |
> ok
Claude: Copied 1. Baseline: built src/Billing.sln (212 s), 1,847 entries, agile gate GREEN.
From the next item on, only the warnings it adds are new. Commit "chore: sync with agile@canary 0.0.65"
(with the baseline) on main. Authorize?
```

If that build fails, or no solution is found, Claude quotes the gate's RED or SKIPPED line, writes no baseline, finishes the rest of the sync, and records in the retro log that the baseline is still missing. A solution that only MSBuild can build fails there, because the gate runs `dotnet build` (#20).

A ⏳ plugin note that cites an issue the new version delivered gets its ✅ from the same sync. A note with no `agile-canary#N`, or one citing an issue still open, stays ⏳:

```
> /agile:sync
Claude: agile@canary 0.0.66 → 0.0.68.
| File                                   | State     | Proposal                                          |
| docs/agile/workflow.md                 | update    | copy: manual 0.0.68                               |
| plugin notes delivered                 | 2 lines   | mark ✅: agile-canary#19 (0.0.66), row and note    |
> ok
Claude: Copied 1. Notes: marked line 10 (row) and line 47 (note), agile@canary 0.0.66 (`6270ad6`).
Commit "chore: sync with agile@canary 0.0.68" (with the retro log) on main. Authorize?
```

A project that has DocGen but never declared it as its docs command gets the block offered once (0.0.72). The ship before this sync already ran DocGen, as the implicit command, and said so:

```
> /agile:sync
Claude: agile@canary 0.0.71 → 0.0.72.
| File / item                             | State     | Proposal                                          |
| docs/agile/workflow.md                  | update    | copy: manual 0.0.72                               |
| docs command                            | missing   | declare DocGen: dotnet run --project tools/Hutch.DocGen (check with -- --check, paths docs/architecture/) |
> ok
Claude: Copied 1. Declared the docs command: updated .claude/agile/build.json
(docs.command = dotnet run --project tools/Hutch.DocGen). From the next ship, gate.js docs runs it
as a declared command and the "implicit DocGen" line is gone.
Commit "chore: sync with agile@canary 0.0.72" (with build.json) on main. Authorize?
```

A project that already declares another docs command gets one line instead — `docs already declared: lens-map --out docs/legacy/inventory` — and nothing changes: combining two commands is done by hand. Two `tools/*.DocGen` folders are named as well, because `gate.js docs` is RED until you declare one yourself.

From 0.0.23 the template carries the final `.editorconfig`. In a project that still has the basic one, the `.editorconfig` row lists the new groups of rules (braces, pattern matching, expression-bodied members, formatting) and Claude proposes them one group at a time; code that breaks an approved rule shows up as warnings in the build that follows the sync.

### 14.11 When a gate says no

**A running app locks the build.** Claude opened the screen to check it and forgot the app host:

```
[agile gate] build blocked: ExamPrep.slnx
the build output is held by a running process (ExamPrep.Web (18344)): another build or test in this folder
(an IDE, a terminal, another session) or an app host, preview or debugger started from it. Stop it, then build again.
Claude: My app host was still running. Stopped it; building again... green.
```

**A new warning at ship.** The list and the verdict come last, so they survive any view of the output:

```
new warnings (fix them, or accept with `node gate.js baseline` after the owner agrees):
src/ExamPrep.Web/Pages/Results.razor.cs|RS0030 (0 -> 1)
  src/ExamPrep.Web/Pages/Results.razor.cs(42,19): RS0030 The symbol 'DateTime.Now' is banned in this project: ...
agile gate RED: 1 new warning(s): RS0030 in src/ExamPrep.Web/Pages/Results.razor.cs
Claude: Replaced DateTime.Now with the injected TimeProvider; ship again... agile gate GREEN.
```

**A baseline taken with another SDK.** The solution lives in `src/` with its own `global.json`, and the baseline was taken before the gate built from that folder:

```
sdk 10.0.401 (src)
baseline taken with SDK 11.0.100-rc.1.26425.128, this build used 10.0.401: warning counts may differ; retake it with gate.js baseline after the owner agrees
built src/ExamPrep.Web/ExamPrep.Web.csproj (6 s)
agile gate GREEN
Claude: The baseline came from the 11.0 preview, which the repository root picked. It accepts warnings
this SDK does not raise, so a new one could hide behind them. Retake it now (full rebuild, a few minutes)?
```

**A test hangs.** Instead of blocking the turn for minutes:

```
[agile gate] a test hung (more than 120 s) or the test host crashed: tests/ExamPrep.Web.Tests
not finished: ExamPrep.Web.Tests.Ui.ConfirmDialogTests.Confirm_ReturnsTrue
Claude: The test awaited a dialog result inside InvokeAsync. Fixed with the shared test context; 14 tests green (2 s).
```

**A commit on the wrong branch.** The IDE switched the branch to main while F-6 was being built:

```
Claude: git commit -m "feat(F-6): add level filter"
[agile guard] This commit would land on `main`, but `feature/F-6` is not merged and is checked out nowhere.
The branch was probably switched outside this session (an IDE?). Run `git branch --show-current` ...
Claude: The checkout is on main; feature/F-6 is where this belongs. I am switching back to feature/F-6
and committing there. Nothing landed on main.
```

The same guard refuses a refinement or build commit that would put an item file on main:

```
Claude: git commit -m "docs(F-8): refine and approve"
[agile guard] This commit would land on `main` and carries `docs/features/F-8-question-review-queue.md` (approved).
An item that is not done is committed on its own branch ...
Claude: Creating feature/F-8 from main and committing there.
```

When a commit really belongs on main while an item is open (a retro note, for instance), Claude asks, and with your yes repeats the command ending with `# agile:main-ok`. A retro commit carries only rules, `CLAUDE.md`, templates and the retro log; a lesson that needs code becomes an item, so the code goes through the gate on its own branch.

**The docs command touched a file it does not own.** In an adopted repository whose `.claude/agile/build.json` declares a code map, the ship step `gate.js docs` runs it after the green full check:

```
ran docs.command (48 s): lens-map --out docs/legacy/inventory
changed outside docs.paths (not committed):
  src/Billing/Billing.csproj
agile docs RED: 1 file(s) changed outside docs/legacy/inventory: src/Billing/Billing.csproj
Claude: The map command rewrote a project file, which is outside docs.paths. I did not commit it and
restored it with git restore. This belongs to the tool that writes the command (legacy-lens), so I
logged it as a plugin note in the retro log. Merge on hold: ship it without the map this time, or wait?
```

A green run lists the files it changed under `docs.paths`, and they go into the `docs(F-<n>)` commit with the manual:

```
ran docs.command (47 s): lens-map --out docs/legacy/inventory
ran docs.check (3 s): lens-map --check
to commit with the item:
  docs/legacy/inventory/entry-points.md
agile docs GREEN: 1 file(s) changed under docs/legacy/inventory
```

**The docs command nobody declared** (0.0.72). A project that got DocGen before the block existed. The ship regenerates the code map anyway, and says who declares it:

```
ran docs.command (11 s): dotnet run --project tools/Hutch.DocGen
ran docs.check (4 s): dotnet run --project tools/Hutch.DocGen -- --check
to commit with the item:
  docs/architecture/modules.md
implicit DocGen docs command: declare it with /agile:sync (tools/Hutch.DocGen)
agile docs GREEN: 1 file(s) changed under docs/architecture/
Claude: The code map was regenerated and committed with the manual. This project runs DocGen without
declaring it: run /agile:sync between features and I will write the block into build.json once.
```

Two `tools/*.DocGen` folders and nothing declared is RED instead, because either choice would leave the other stale without saying so:

```
agile docs RED: 2 DocGen tools and no docs in .claude/agile/build.json: tools/Hutch.DocGen,
tools/Hutch.Legacy.DocGen — declare which one in .claude/agile/build.json
```

**A repository only MSBuild can build.** An adopted repository whose solution holds an ASP.NET web application. Before 0.0.69 every gate here failed on `dotnet build`, even with `engine: msbuild` in `build.json`:

```
sdk 10.0.401 (.)
build failed: App.sln
src\Web\Web.csproj(19,3): error MSB4019: The imported project "...\sdk\11.0.100\Microsoft\VisualStudio\v18.0\WebApplications\Microsoft.WebApplication.targets" was not found.
agile gate RED: build failed (App.sln)
```

From 0.0.69 the same repository builds. A turn that touched one project:

```
agile gate (by hand): 1 code file(s) changed since the main branch
msbuild 18.10.1.42706 (.)
built src/Lib/Lib.csproj (0 s)
tests skipped: engine msbuild runs the whole suite at ship
agile gate GREEN
```

The ship restores, rebuilds the solution and runs the declared suite:

```
msbuild 18.10.1.42706 (.)
restored (12 s): nuget restore App.sln
built App.sln (58 s)
tested (214 s): vstest.console.exe tests\**\bin\Debug\*.Tests.dll
  Total tests: 312. Passed: 312. Failed: 0.
warnings baseline: 41 entries -> .claude/agile/warnings-baseline.json
agile gate GREEN
```

Two failures worth recognising. A typo in the engine, which used to pass silently as `dotnet`:

```
agile gate RED: unknown engine "MSBuild2" in .claude/agile/build.json (legal: dotnet, msbuild, none)
```

And a machine with no Visual Studio, which says where it looked instead of blaming the solution:

```
MSBuild not found (engine msbuild in .claude/agile/build.json): tried no msbuildPath in .claude/agile/build.json, msbuild on the PATH, vswhere at C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe
agile gate RED: MSBuild not found
```

Claude: This machine has no Visual Studio, so the gate cannot build this repository. Install the Build Tools, run from a Developer Command Prompt, or name the MSBuild you want as `msbuildPath` in `build.json`. Which?

**A blocked heredoc write, and its bypass.** Claude tries to patch the roadmap through a shell heredoc instead of the Edit tool:

```
Claude: cat > docs/ROADMAP.md <<'EOF'
...
EOF
[agile literal-guard] This command would write `docs/ROADMAP.md` through the command's own text.
rules/core/workflow.md: "Write or edit files (code, tests, docs; new or existing) with the Write and Edit
tools, never through a script's string literals": escapes turn into control characters or stray text, and a test can compile
and pass while matching nothing.
If this write really has to happen this way, ask the owner; with a yes, repeat the command ending with
the comment  # agile:literal-ok
Claude: Using the Edit tool instead of a heredoc for docs/ROADMAP.md.
```

A `sed -i` on a tracked file is caught the same way; when the owner really wants the shell form (a one-off rewrite across many lines a script drives), the bypass repeats the exact command with the comment:

```
Owner: yes, go ahead
Claude: sed -i 's/0\.0\.74/0.0.75/' docs/ROADMAP.md  # agile:literal-ok
```

### 14.12 The closing question of the quiz

After round 7, before the summary:

```
Claude: Closing question, no recommendation: which domain concept worries you most, or did the quiz not touch?
> The subject taxonomy. Every exam board names subjects differently and some group two of them.
Claude: Facts from the code you gave me: three fixed levels, questions point to the lowest one, no aliases.
Options: A) keep three levels; B) two canonical levels + the notice's own subjects mapped to them + aliases.
Recommendation: B — the notice keeps its vocabulary, analytics compare across exams.
> B
Claude: Recorded in ADR-0001 under "After the quiz"; brief capability 2 updated. Now the summary...
```

### 14.13 One item in a single run

`/agile:autopilot` takes one item from idea to done with two stops (section 5): the question round, where your
answers approve the item, and the validation script, where you can validate and authorize the merge in one message.
The examples below use the ExamPrep app from 14.1.

#### 14.13.1 An item with a screen, from idea to done

F-17 adds filters to the question bank screen. It is an `idea` with an empty `## Start`.

```
> /agile:autopilot F-17
Claude: F-17 (idea) — two stops: questions now, validation script later. Reading the question bank code.
What exists: /questions lists 25 per page, no filters; Question has Discipline, Difficulty, ExamBoard; the
permission questions.view already guards the page. No premise failed.

Draft acceptance criteria (from the recommended options below; your answers can change them):
  AC1 Given questions of three disciplines, when I filter by one, then only its questions are listed.
  AC2 Given filters by discipline and difficulty, when both are set, then both apply (AND).
  AC3 Given a filtered list, when I reload the page, then the filters stay (kept in the URL).
  AC4 Given no question matches, when I filter, then the empty state "No question matches these filters" shows.
  AC5 The texts exist in pt-BR, pt-PT and en.
Mockup: docs/features/mockups/F-17-question-bank-filters.html (states: loading, ready, empty, error; 3 languages)

[card — Rules]       Several filters combine as: AND (Recommended) | OR
[card — Screens]     Filters sit: in a bar above the list (Recommended) | in a side panel
                     Filters kept after reload: in the URL (Recommended) | no
[card — Packages]    New packages: none needed (Recommended)
[card — Approval]    Approve F-17 with these answers and criteria? Aprovo F-17 (Recommended) | Change something
```

You open the mockup, prefer a side panel, and answer:

```
> Rules: AND. Screens: side panel; URL yes. Packages: none. Aprovo F-17.
Claude: Your screen answer differs from the recommendation (side panel), so the mockup changed:
docs/features/mockups/F-17-question-bank-filters.html — the panel closes on narrow screens. The rest is recorded
in ## Decisions and the criteria stand. This extra stop is for the screen only: does the panel look right?
> sim
Claude: F-17 approved (Autopilot: approved). Building on feature/F-17.
```

The run goes on alone: branch, code, affected tests, gate, the screen opened through the app host in both themes.
The change touches no permission, schema or shared data, so no review is needed. Then stop 2:

```
Claude: STOP 2 — F-17 is in validating (Autopilot: stop 2).
  8 files; 12 tests green (2.9 s); gate GREEN, 0 new warnings.
  Criterion → test: AC1 QuestionBankFilterTests.Filter_ByDiscipline_ListsOnlyIt; AC2 ...BothFilters_ApplyAnd;
  AC3 ...Reload_KeepsFiltersFromUrl; AC4 ...NoMatch_ShowsEmptyState; AC5 missing-key test + step 6.
  Validation script: docs/features/F-17-question-bank-filters.md#validation-script (6 steps).
  Answer "validado e autorizo o merge de F-17" to ship it now, or "validado" to stop here.
> validado e autorizo o merge de F-17
Claude: Shipping F-17 (Autopilot: shipping). Full suite 214 tests green (41 s); manual updated in pt-BR, pt-PT
and en (question-bank.md); merged --no-ff as 3f9c2e1, pushed; main...origin/main 0 0; branch removed.
Board #31 closed, Status Done (read back). F-17 done. Retro: 1 lesson proposed — keep or drop?
```

Your "validado e autorizo o merge" was the ship's merge authorization.

#### 14.13.2 Validated, merge later

The same stop 2 for F-19, answered with "validado" only:

```
> validado
Claude: F-19 validated and kept in validating; nothing merged. When you want it on main: /agile:ship F-19.
```

"ok", "pode seguir" or "validado" never authorize a merge. Only a message that names the merge does, or typing `/agile:ship F-19` afterwards.

#### 14.13.3 A defect at stop 2

```
> passo 4 falhou: o filtro some quando troco de página
Claude: Reproduced: the page link drops the query string (QuestionList.razor:88). Fixed; added
Paging_KeepsFilters (seen red before the fix). 13 tests green (3.0 s), gate GREEN.
STOP 2 again — repeat steps 4 to 6 of the script.
```

#### 14.13.4 `--assume`, and the package it never assumes

```
> /agile:autopilot F-18 --assume
Claude: F-18 (idea) — --assume: stop 1 skipped with my recommendations, except what only you decide.
F-18 exports the question bank to PDF and needs a new package. That question is yours:
[card — Packages]  QuestPDF <latest> (Community licence) (Recommended) |
                   PdfSharpCore <latest> (MIT) | no PDF — CSV only
> QuestPDF
Claude: Recorded. 5 answers assumed (in ## Decisions as "assumed by autopilot"). F-18 approved. Building.
...
Claude: STOP 2 — F-18 in validating. Assumed: A4 portrait; one question per block; answers on a last page;
admin and editor only (permission questions.export). Any of them wrong: tell me, it becomes a change note.
Mockup of the export page (you did not see it at stop 1): docs/features/mockups/F-18-question-export.html
```

#### 14.13.5 A risky change: the review runs by itself

F-20 lets an editor archive questions (permissions and data). Before stop 2 the run calls the fresh-context
reviewer without being asked, fixes what it confirms, and shows everything:

```
Claude: STOP 2 — F-20 in validating. Review (change-review): 1 major confirmed and fixed — the archive endpoint
checked questions.edit instead of questions.archive (ArchiveQuestion.cs:21), test added; 1 minor accepted —
the archived list has no page size limit (recorded in ## Decisions). Validation script: 7 steps.
```

#### 14.13.6 An owner-reserved stop, and resuming the next day

```
Claude: STOP (owner) — F-21 needs a column the file does not mention: questions.archived_at (schema change).
Done: approved, 5 of 9 files, tests green. Left: the migration and 2 criteria.
Options: A) add archived_at by /agile:change F-21 (Recommended — the archive rule needs the date);
B) keep a flag without the date. Then /agile:autopilot F-21 continues.
> A
(the change note is recorded; you close the session)
```

The next day, in a new session:

```
> /agile:autopilot F-21
Claude: F-21 resumes from its file (Autopilot: approved, build in progress; change note 1 applied). Adding the migration...
```

The run never reverts work at a stop, and it never merges, pushes or sets `done` without the merge named by you.

### 14.14 A quiz for an app that calls a model

ExamPrep's brief gains a capability: "a tutor explains a wrong answer to the student". Round 4 of the quiz now has three more questions, and round 2 one:

```
Claude: Round 2 — Data.
9b. Personal data and retention → Recommendation: stored, with a retention period.
    The tutor sees the student's answers. Answers kept 2 years; tutor conversations
    30 days, deleted by a job (a soft-delete flag still holds the text). May they
    leave the network? That decides question 17b.

Round 4 — Integration.
17b. LLM provider → Recommendation: IChatClient, provider by configuration, hosted.
     Answers carry no names; if 9b says they may not leave the network, self-hosted.
17c. Cost ceiling → Recommendation: per-student cap + global breaker. Every message
     costs money and a free plan exists (question 13): without a cap nothing stops it.
17d. Model in tests → Recommendation: a scripted fake IChatClient. Validation,
     permissions and limits stay fast and deterministic; the real model is not in the suite.
```

```
> 9b ok, they may leave. 17b ok. 17c ok, 50 messages a day. 17d ok.
```

Round 7 adds question 30b:

```
Claude: 30b. Evals → Recommendation: a test level of their own. 40 wrong answers with
    the explanation a teacher would accept, scored against the real model at ship and
    on demand, never in the turn gate. Baseline pass rate committed; a drop fails the
    ship. Which model runs the cases? → Recommendation: the one the tutor calls in
    production. The command pins it (--model) and runs with --ablation none: the
    default would run every case twice. The ceiling is set later, after one measured run.
> ok
```

Round 8 then asks question 38 — the system prompt as a versioned file, reviewed like code — and, if the tutor gets tools, question 37 for the tool catalogue.

With the catalogue chosen, bootstrap copies `ModelToolAttribute.cs` into the project that owns the tools. A tool is then a method that says what the code alone cannot:

```csharp
[ModelTool(Permissions = ["EXAM_READ"], Reaches = ["Catalog.ExamService.GetAnswer"])]
[Description("Gets the official answer and rationale for a question.")]
public Task<Answer> GetOfficialAnswerAsync([Description("The question id.")] Guid questionId)
```

At the next ship, `docs/architecture/tools.md` gains a row per tool and a section with its input schema; while no method carries the attribute yet, it says `0 tools.`, and an app without the attribute gets no `tools.md`. The name the model sees is the method's, minus a trailing `Async` only when the method returns a `Task`, a `ValueTask` or an `IAsyncEnumerable<T>` (`GetOfficialAnswerAsync` above becomes `GetOfficialAnswer`; a `GetExamAsync` that returns a `string` keeps its name). When a tool is added without saying who may call it, the ship stops:

```
> /agile:ship F-30
Claude: Full check GREEN (212 tests). Technical docs:

tool catalogue: 2 problem(s)
  ExamPrep.Ai.TutorTools.ExplainAsync: no Permissions. Name them, or say the tool is
  open with a value of its own (for example ["authenticated"]).
  ExamPrep.Ai.TutorTools.ResetProgressAsync: no Reaches. Name the systems and
  operations it reaches.

Two tools are missing declarations, so I did not regenerate the catalogue. ExplainAsync
reads a question and the student's answer: EXAM_READ, or "authenticated" if any signed-in
student may use it? ResetProgressAsync writes — which service does it reach?
```

The owner answers, the code changes, and the ship goes on. A tool that writes without asking the user first is allowed, and shows as `⚠ no` in the summary table: the policy is the app's, the visibility is the catalogue's.

### 14.15 The two passes before the code

F-21 adds an endpoint other modules consume and one new table, so the build runs both read-only passes before planning:

```
> /agile:build F-21
Claude: This item creates a contract and a table, so the two passes ran before the plan.
system-design proposed: Catalog.Api takes POST /api/v1/exams (error codes exam.duplicate,
exam.invalid-board), the ExamPublished message owned by Catalog, and the Exams table with a
unique index on (BoardId, Year). Risk: 312 rows already duplicate that pair.
architect (blocker): the unique index over existing duplicates fails the migration → clean the
rows in the same migration, or add the index after the cleanup item. Keep as proposed: the slice
and the contract. Checks this project really has: tests/Architecture, no layout test.
I checked both against the code: the 312 rows are real (verified with a count), the message has no
consumer yet. Accepted the blocker, dropped the second index nothing queries — both in ## Decisions.
Plan (6 steps): 1. migration with cleanup, 2. Exam entity, ...
```

Nothing was written by the two agents: they read and proposed, Claude checked, decided, and only then started. An item with none of those four triggers — a thin CRUD, a text change — skips both passes entirely.

### 14.16 A feature reproduces its premise before the fix

F-11's Summary states the transcript export crashes today on any exam with more than 2,000 students. The build follows the fixture-reproduction rule before writing the fix, the same way a bug shows its regression test failing first:

```
> /agile:build F-11
Claude: Reproducing the premise first. Built ExamStub with 2,500 students and ran the
transcript export: System.OutOfMemoryException at TranscriptExporter.cs:41 (loads every
row into memory before writing). Confirmed — the fixture reproduces the stated failure.
Applying the fix: streaming the rows instead of buffering them.
Claude: Same fixture, same 2,500-student exam, export now completes: 2,500 rows written,
1.8s. The fix is proven against the failure it claims to solve.
```

A feature with no "fails today" premise — a new capability, not a claim that something breaks — skips this step entirely: one test per acceptance criterion, as before.

### 14.17 A board with no "Ready" option yet

A project bootstrapped before the board had a distinct Ready state — its Status field is just Backlog/In Progress/Done. F-20 reaches approval and its first mirror needs "Ready":

```
> /agile:refine F-20
...
Claude: aprovo F-20
Claude: F-20 approved. The board's Status field had no "Ready" option: created it right
after "Backlog" (Backlog, Ready, In Progress, Done) and re-applied every other item's
Status by name — nothing else on the board moved. F-20 is now Ready.
Next: /agile:build F-20 (nothing else is building or validating).
```

A later item on the same board just uses the option that is already there; `/agile:build` and `/agile:ship`, which never target "Ready", still only report — never create — any option their own mirror needs but does not find.

### 14.18 Folding an open item into the one being refined

While refining B-22, the owner proposes folding B-23's scope into it, "since they touch the same rule". Claude checks B-23 before folding it in:

```
> Fold B-23 into this one, it's the same rule.
Claude: Checking B-23 first — git worktree list shows D:/wt/examprep/b-23-ship-sets-status
[bug/B-23], and its file starts "Status: approved (owner, 2026-09-27; worktree ...)".
B-23 is already approved on its own branch, not just an idea: folding it here would leave
two items building the same fix. Fold it into B-22 anyway, or drop the fold and leave B-23
where it is?
> Drop it, ship B-23 on its own.
Claude: B-23 stays untouched and out of scope for B-22. Recorded in ## Decisions.
```

A later proposal, to fold B-24 ("just an idea, no worktree yet") in the same round finds nothing beyond `idea` anywhere, so it folds in without a question:

```
> Fold B-24 too, it's still just an idea.
Claude: B-24 has no worktree and is idea everywhere — folding it into B-22 now.
```

`epic-breakdown` and `discuss` are unaffected: they only capture new ideas, never absorb an existing open item's scope, so this check runs in `/agile:refine` only.

### 14.19 A desktop app in the quiz

The brief describes a standalone app for a clinic's front desk, Windows and macOS, several users on one PostgreSQL:

```
Claude: Question 1, type of app. Recommendation: desktop (the brief says "installed on the reception PCs").
Question 2, profile. Recommendation: desktop.
> ok, ok
Claude: Question 2a, desktop technology. Windows and macOS, no mobile app in the brief.
Options: A) Avalonia 12; B) WinUI 3 (Windows only: ruled out by the macOS target); C) .NET MAUI.
Recommendation: A — one code base for both systems, no platform controls to reconcile.
> A
Claude: Question 5 is PostgreSQL. Question 2a2, how the client reaches it.
Options: A) through an API; B) directly to the database.
Recommendation: A — several users share the data, and B would put the database credential on every machine.
> A
Claude: Questions 4 and 23 are not asked in MudBlazor terms here: the stack is XAML. Question 23a, visual identity: a file,
a site, an image or three questions (see 14.22). The UI kit and gallery are not a question any more: shared styles and
controls with a dev-only gallery window, captured as the first item. Question 29: view-model
tests, plus Avalonia.Headless for the screens; the gate builds Desktop.Core and the API, never the head.
```

### 14.20 What an item waits on to start, and what it needs to validate

`## Start` of a feature or bug has two lines: `Waits on (to start)` and `Needed to validate`. Only the first, with `Depends on`, can stop a start. F-21 needs a real exam-board export, but only to be validated:

```
## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: a real exam-board export (.csv) — from the curator.
```

```
> /agile:autopilot F-21
Claude: F-21 (idea) — two stops: questions now, validation script later. Start: depends on nothing, waits on
nothing to start. It needs a real export to be validated: I will ask for it at stop 2, not now.
```

Before, the single "Waits on" line would have stopped the run here. At stop 2 the same text comes with the script:

```
Claude: STOP 2 — F-21 is in validating (Autopilot: stop 2).
  Needed to validate: a real exam-board export (.csv), from the curator — not in the repository yet.
  Validation script: docs/features/F-21-import-exam-board.md#validation-script (5 steps).
  Answer "validado e autorizo o merge de F-21" to ship it now, or "validado" to stop here.
```

An item that waits on something to start (`Waits on (to start): the API key, from the platform team`) still stops the run and `/agile:status` still lists it as Blocked. An item file with the old single `Waits on:` line is read as "to start", so nothing already open changes by itself.

### 14.21 Typing ship is the merge authorization

You validated F-35 and type the command. There is no "Authorize?" question: the command is the authorization, for this item only.

```
> validado
> /agile:ship F-35
Claude: Full suite 231 tests green (2 min 4 s). Main moved (0.0.85 by another session): merged into the branch,
suite green again, version bumped to 0.0.86. Merged feature/F-35 into main (--no-ff), pushed; main...origin/main 0 0.
scripts/worktree.js remove D:\wt\agile-canary\f-35-ship-authorizes: build server shut down, probe passed
(renamed and back), worktree removed and pruned (REMOVED). Branch deleted locally (remote: never pushed).
Board #35 closed, Status Done (read back). Retro: 1 lesson proposed — which do you adopt?
```

When something still holds the folder, the probe stops once, before anything is removed:

```
Claude: Merged feature/F-35 into main. I stopped before removing the worktree: renaming
D:\wt\agile-canary\f-35-ship-authorizes failed, a process holds a file in it (an IDE or an app host).
Close it and tell me; I run the same command again. Nothing was removed (example 14.78 shows the report).
> closed
Claude: Probe passed. Worktree removed, branch deleted. 0 0 against origin/main.
```

### 14.22 The app's visual identity, from a website

Round 5 of the bootstrap of a clinic's web app. The identity is asked before the design system library, and the UI kit is no longer a question:

```
Claude: Question 23a, visual identity. Options: A) a file (DTCG *.tokens.json or DESIGN.md); B) a website URL;
C) an image (logo, brand board); D) none, three questions derive one.
Recommendation: B — the brief names the clinic's site. Then 23 is only the library choice: MudBlazor.
> B https://clinica-exemplo.example
Claude: I read the page and its stylesheets. Nothing is written yet. Found:
| Role | Light | Dark (derived) | Text on it | Light | Dark |
| primary | #0B6E6E | #4FD1C5 | on-primary #FFFFFF | 6.1:1 | on #0B2A2A: 8.2:1 |
| secondary | #F2A93B | #F2B85F | text #1A1A1A | 8.7:1 | text #101010: 10.7:1 |
| background | #FAFAF7 | #121614 | text #1A1A1A | 16.6:1 | text #ECEFEC: 15.8:1 |
| error | #D92D20 | #F97066 | on-error #FFFFFF | 4.8:1 | on #1A0B09: 6.9:1 |
Fonts: Inter (headings and body). Radius: 8 px. Icon family: Outlined. Source: URL, 2026-09-28.
Two pairs fail AA: link #3AA8A8 on the light background is 2.7:1 (nearest passing: #1F7F7F, 4.6:1) and secondary as
text on white is 2.0:1 (use it only as a fill). Fix the link color to #1F7F7F?
> yes, confirm
Claude: Written docs/design/identity.tokens.json and docs/design/DESIGN.md (front matter generated from the tokens,
re-read and compared: 4 colors x 2 themes, fonts, radius, icon family equal). ADR-0001 line: source URL, 2026-09-28.
First item captured: "UI kit and gallery, built from the identity". Both files go into the first commit.
```

An app bootstrapped before this version gets the same question later:

```
> /agile:sync
Claude: Also missing (only /agile:identity adds it): Visual identity. The project has screens and no
docs/design/identity.tokens.json. I write nothing here.
> /agile:identity
Claude: No identity recorded. Question 23a: a file, a website, an image or three questions?
> none
Claude: Primary color, font, light, dark or both? ... (derives the palette, shows the table, waits for "confirm")
Claude: Written docs/design/ and the ADR-0001 line. The project already has a UI kit, so I captured the idea
"Apply the identity to the theme". No application code was changed.
```

With an identity already there, `/agile:identity` shows it as a table (source, date, colors, fonts, radius, icon family) and asks what changes.

### 14.23 The sections of a site in the quiz

The brief describes a nutritionist's site: who the nutritionist is, what patients say, the plans she sells, a newsletter, and "maybe an online shop later":

```
Claude: Question 2, profile. Recommendation: website — a public site found by search engines, texts you edit
yourself, and it can grow into an app (patient area) without changing profile.
> ok
Claude: Question 2c, the sections of the site. All selected; answer "ok" or the numbers to untick.
Always in the site (not in the list): content area, home, basic SEO, 404 and error pages, privacy policy
and cookie consent.
Launch:  1 About me · 2 Testimonials · 3 FAQ · 4 E-mail signup · 5 Products · 6 Contact and WhatsApp
Later:   7 Services and prices · 8 Portfolio · 9 Blog with RSS · 10 Moderated comments · 11 Gallery
         12 Events / scheduling · 13 Search · 14 Location · 15 Team · 16 Partners' logos
         17 Downloadable materials · 18 Newsletter sending · 19 Banner / pop-up · 20 Launch page
         21 Link in bio · 22 Press kit · 23 Careers · 24 Accessibility statement · 25 Visitor dashboard
         26 Installable site (PWA)
> untick 4, 15, 23
Claude: 18 (newsletter sending) needs 4 (e-mail signup): without 4 there is nobody to send to.
Keep 4, or untick 18 too?
> keep 4
Claude: Selected: 24 sections (4 kept; 15 and 23 out). The online shop in the brief is not a section: it is an
app feature with payments, so I will capture it as its own epic idea.
...
Claude: Question 19, e-mail. You chose "none", but sections 4 and 6 need to send (the confirmation link,
the contact message). Recommendation: SMTP.
> SMTP
...
Claude: Board: epic "Website sections" with 29 ideas — the 5 of the foundation (content area first), the
6 launch sections, then 18 later ones — and the epic idea "Online shop". Create them?
```

### 14.24 A mobile app for a running site

The nutritionist's site (14.23) has a patient area, and she wants patients to use it from their phones:

```
> /agile:epic "Patient app"
Claude: The profile is website, and a mobile app for it follows the mobile-client complement. Two questions:
(a) Where does the app live? Recommendation: in this repository and solution — the app reuses the site's
    code and ships with the same version. (The other answer, a new repository, is example 14.30.)
(b) Screens? Recommendation: MAUI Blazor Hybrid — the patient-area components are reused inside the app.
    Alternative: MAUI XAML, native look, every screen written again.
Which of the site's features go to the app? Tick by number:
Done items: 1 F-31 Meal plan · 2 F-34 Appointments · 3 F-37 Progress chart
Areas:      4 Pages/Messages
(The public pages stay on the site and are not offered.)
> a: this repository, b: Hybrid; 1, 2 and 4
Claude: Mobile foundation comes first (the app's projects, sign-in with a token on the site, the version 1.8.0
the site has today). Then:
- Meal plan on mobile — the plan is read in one page only: this feature extracts it.
- Appointments on mobile — the booking rule (no two in the same slot) sits in two pages. I suggest
  "API for appointments" before it. OK?
- Messages on mobile — the page reads directly, no rule: this feature extracts it.
> ok
Claude: Epic "Patient app": Mobile foundation, API for appointments, Meal plan on mobile, Appointments on mobile,
Messages on mobile. The list you ticked goes into the epic file as you ticked it. Create them?
```

Had she answered "a new repository" in (a), the epic would be the site's half only: example 14.30.

### 14.25 A list you approved ends up in the file

While refining "Export a report", Claude proposes the formats in the question round and the owner edits the list:

```
Claude: Export formats, in this order:
1 PDF · 2 XLSX · 3 CSV · 4 JSON · 5 XML
> Drop XML, add DOCX at the end.
Claude: Done: PDF, XLSX, CSV, JSON, DOCX.
```

Claude writes the file. Before asking for approval it re-reads it and finds one line, in the acceptance criteria: "the report exports in the formats shown in the refinement". It pastes the list in and points the criterion to it:

```
## Approved list
Export formats
1. PDF
2. XLSX
3. CSV
4. JSON
5. DOCX

## Acceptance criteria
- AC2 Given a finished report, when the owner picks any format of `## Approved list`, then the file downloads in that format.
```

Claude re-reads again, finds no line that sends the reader to the chat, and only then shows the summary and asks for "aprovo F-n". The build, opened days later in another session, reads the five formats from the file.

### 14.26 Evals: the model is pinned, the ceiling is measured, the verdict is code

Bootstrap of the exam tutor, question 30b. Claude asks the model as well and says what the command will carry:

```
Claude: 30b. Which model runs the cases? → Recommendation: the one the tutor calls in
    production. The Evals line of CLAUDE.md gets --model, --ablation none (the default
    would run every case twice, with and without the plugin), --max-cost-usd and --json.
    The baseline records the model and the ablation.
> ok
```

Later, refining "Score the tutor's explanations", Claude does not fix the number of runs yet. It runs one case once and quotes it:

```
Claude: claude plugin eval . --case wrong-answer-1 --runs 1 --ablation none --model <production model>
    → cost US$ 0.21. Six cases × 3 runs = US$ 3.78; with a 30% margin, ceiling US$ 5.
    Recorded in ## Decisions. Runs per case: 3? Ceiling: US$ 5?
> ok
```

At ship, Claude runs the suite with `--json` and then the comparator, and quotes its whole output:

```powershell
node evals/compare.js result.json
```

```bash
node evals/compare.js result.json
```

```
ok       wrong-answer-1: 1 (baseline 1)
DROP     wrong-answer-2: 0.67 < baseline 1
cost US$ 3.61, model production-model, ablation none
evals RED            (exit 1)
```

The ship stops. Had the run used another model than the baseline's, the last line would read `evals NOT MEASURED` (exit 2): not a drop and not a pass, and re-recording the baseline (`--write`) needs the owner's yes.

### 14.27 A VB.NET repository: the turn gate builds and tests a `.vb` change

An adopted repository written in VB.NET (`engine: dotnet`, SDK-style projects): `src/VbLib`, `tests/VbLib.Tests` (MSTest on `Microsoft.NET.Test.Sdk`), `tests/Sdk.Tests` (a VB project on `MSTest.Sdk/3.6.4`), a C# `tests/CsApp.Tests` that references `VbLib.vbproj`, and `src/FsLib` in F#. Claude edits `Class1.vb`. Until 0.0.90 the turn ended in silence, and `gate.js stop` by hand said:

```
agile gate SKIPPED: no code file changed since the main branch
```

Now the same edit gives:

```
agile gate (by hand): 1 code file(s) changed since the main branch
sdk 8.0.131 (.)
built tests/CsApp.Tests/CsApp.Tests.csproj (4 s)
built tests/Sdk.Tests/Sdk.Tests.vbproj (3 s)
built tests/VbLib.Tests/VbLib.Tests.vbproj (2 s)
tested tests/CsApp.Tests/CsApp.Tests.csproj (2 s)
  Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: < 1 ms - CsApp.Tests.dll (net8.0)
tested tests/Sdk.Tests/Sdk.Tests.vbproj (1 s)
  Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 156ms
tested tests/VbLib.Tests/VbLib.Tests.vbproj (2 s)
  Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 36 ms - VbLib.Tests.dll (net8.0)
agile gate GREEN
```

The C# test project runs because the graph now has the `.vbproj` as a node; `Sdk.Tests` runs because `Sdk="MSTest.Sdk` counts as a test project (a C# project on `MSTest.Sdk` is found the same way). A second unused local in `Class1.vb` is a new VB warning, RED on every turn until it is fixed:

```
new warnings (fix them, or accept with `node gate.js baseline` after the owner agrees):
src/VbLib/Class1.vb|BC42024 (1 -> 2)
  src/VbLib/Class1.vb(3,13): BC42024 Unused local variable: 'unused'.
  src/VbLib/Class1.vb(4,13): BC42024 Unused local variable: 'second'.
agile gate RED: 1 new warning(s): BC42024 in src/VbLib/Class1.vb
```

A failing assertion in `Sdk.Tests` ends with `agile gate RED: tests failed (tests/Sdk.Tests/Sdk.Tests.vbproj)`. A change to `Library.fs` builds `src/FsLib/FsLib.fsproj` the same way. Under `engine: msbuild` (an old-style `.vbproj`) the affected project is rebuilt with MSBuild and the report says `tests skipped: engine msbuild runs the whole suite at ship`, as it does for C#.

### 14.28 An app that is only an API: question 10a

Bootstrap of "Rota", whose brief says a mobile app and a partner's system will call it and that it has no screens. Round 1:

```
Claude: 1. Type of app → Recommendation: API only. The brief has no screen of its own.
    2. Architecture profile → Recommendation: web-api (an API for your own clients,
       no screens). monolith would bring a Blazor project and bUnit you do not need.
    Already clear from the brief: no back office (14), no screens (21, 22, 23a–23e).
> ok
```

Round 3, instead of question 10:

```
Claude: 10a. How do the API's consumers authenticate? (several allowed)
    · Identity bearer: users sign in at the API
    · External IdP: users, and client credentials for machine clients
    · API key: machine clients only, never a browser or mobile app
    Recommendation: Identity bearer and API key. The mobile app signs in users, and the
    partner's system is a machine client you control; an API key is never given to a
    mobile app, because its code is public.
    11. Social login or MFA in v1? → Recommendation: no (asked because Identity bearer is on).
> ok
```

The skeleton then has `Rota.Api` with login, refresh and `me`, the API key handler and the selector between the two, `Rota.Tests` with the foundation's HTTP tests and no template test, `docs/api/openapi.json` written by the first test run, and in `docs/infra.md` the token lifetimes (1 h, 14 days) and where the key ring lives. The first endpoint a feature adds accepts users only; the partner's endpoint names the `Clients` policy.

### 14.29 Publishing a release

ExamPrep is a `web-app`; two items were shipped since the last release (`v0.1.0`), and `main` carries `<Version>0.2.0</Version>` in `Directory.Build.props`. You are in the main checkout.

```
> /agile:publish
Claude: Plan: v0.2.0, previous tag v0.1.0, 2 items since it.
  Feature F-3: question bank import
  Bug B-2: wrong total on the report
  Package: ExamPrep.Web (artifacts/publish/v0.2.0/ExamPrep.Web + ExamPrep.Web.zip).
  Going on: typing the command is the authorization for the commit of the notes, the tag, the push and the GitHub Release.
Claude: Packaged ExamPrep.Web: 156 files, zip 4.7 MB; docs/releases/v0.2.0.md written.
Claude: Release v0.2.0: notes committed on main (docs(release): v0.2.0), annotated tag v0.2.0, main and the tag pushed, GitHub Release created with the notes.
  Not packaged: nothing else. No environment was named, so nothing was deployed (example 14.35).
```

The same command a minute later stops and writes nothing, because the tag now exists:

```
> /agile:publish
Claude: Nothing to release: the tag v0.2.0 already exists. The next release comes after the next /agile:ship.
```

A project with a `mobile-client` complement and no signing set up says what it left out: `Not packaged: ExamPrep.Mobile (signing is not set up: ANDROID_SIGNING_KEYSTORE, ... unset); only ExamPrep.Web was.` (example 14.32). A desktop project with `<RuntimeIdentifiers>win-x64;linux-x64</RuntimeIdentifiers>` gets one folder and one zip per runtime, and the linux zip comes with `made on Windows: after unzipping run chmod +x on the executable`. If `dotnet publish` fails on a compile error, the run ends with `nothing was committed or tagged` and the error line, and the fix goes through an item. If the push fails after the tag, the report names `git push origin main` and `git push origin v0.2.0` to rerun; the tag stays.

### 14.30 A mobile app in its own repository, on the site's API

The nutritionist (14.24) prefers the app in a repository of its own, versioned and released apart from the site. First, in the **site's** repository:

```
> /agile:epic "Patient app"
Claude: The profile is website, and a mobile app for it follows the mobile-client complement.
(a) Where does the app live? Recommendation: in this repository (it reuses the site's code and version).
    Or a new repository, with its own version.
Which of the site's features go to the app? Tick by number:
Done items: 1 F-31 Meal plan · 2 F-34 Appointments · 3 F-37 Progress chart
Areas:      4 Pages/Messages
> a: a new repository; 1 and 2
Claude: A new repository means the screens are MAUI XAML, so I skip that question. The site's epic is "API for the mobile app":
- Mobile API foundation — sign-in for the app, the forced-update gate (minimum version, answer 426), the OpenAPI document
- API for meal plan
- API for appointments
Meal plan and Appointments are saved under "Features taken to the app" as you ticked them. Create them?
> ok
Claude: Next step: create a new repository whose product/brief.md names this one (nutri-site), and run /agile:bootstrap there.
The app's ideas "<X> on mobile" are written there, not here.
```

Then, in the **app's** repository, with a brief that names the site:

```
> /agile:bootstrap
Claude: Round 1. Question 1: mobile on an existing API. Question 2: mobile. Question 2d, where does the backend live?
    · in this solution · an existing API in another repository (a site or web-api of yours, or any API with an OpenAPI document)
    Recommendation: an existing API in another repository — the brief names nutri-site.
    Already clear: 3 (deployment), the data round, 12, 13 and 14 — the API decides them. Question 10 is "the API's sign-in": I read it from the site, Identity bearer.
> ok
...
Claude: Board. nutri-site has the epic "API for the mobile app" with two ticked features. Proposed ideas, nothing written there:
  1. Mobile foundation — sign-in against login/refresh/me, the handler with X-App-Version, the pinned document, the contract test (waits on nutri-site#41 "Mobile API foundation")
  2. Meal plan on mobile (waits on nutri-site#42)
  3. Appointments on mobile (waits on nutri-site#43)
```

`/agile:status` in the app shows the three as blocked while those issues are open on the site, and as free once they are closed (for the foundation also when the site's `docs/api/openapi.json` declares `X-App-Version`). If the API were an agile `web-api` or an API of someone else, the ideas come from the brief; when that API has no forced-update gate yet, Claude offers to open an issue "Forced-update gate for <App>" on its board, with your yes, and "Mobile foundation" waits on it. After the first ship in the app repository, only `<App>.Mobile.csproj` goes from `0.1.0` to `0.2.0`.

### 14.31 Push notifications for a mobile app

ExamPrep is a `website` with the `mobile-client` complement. You ask for push in a feature, "Notify when a result is ready", and Claude refines it against `docs/agile/profile-mobile-client.md`.

```
> /agile:refine F-9
Claude: The profile describes push (section "Push notifications"). Two decisions stay with you:
  1. Provider. Recommendation: Firebase Cloud Messaging through FirebaseAdmin 3.7.0 (Apache-2.0)
     for both platforms; the client head adds Plugin.Firebase.CloudMessaging 4.0.1 (MIT).
  2. Device registration. Recommendation: PUT/DELETE /api/v1/devices/{installationId} in the site's
     Features/Devices class, so the API knows whom it can reach.
You: ok
Claude: The criteria cover 204 on register, 401 anonymous, 400 devices.invalid-token, a device
  moving to the user who registers it, one message per device in its culture, and an unregistered
  token deleted. The tests use a fake sender; real delivery is step 6 of the validation script,
  which needs a Firebase project key and one Android device from you.
```

The gate stays green with no device and no Firebase account. At validation Claude hands you the script: sign in on the phone, accept the permission prompt, trigger the result, see the notification in the phone's language, sign out and trigger it again to see nothing arrives.

### 14.32 Publishing a mobile app to the stores

ExamPrep is a `website` with the `mobile-client` complement, at `0.2.0`, and this is its first store release. The four signing variables are not set yet.

```
> /agile:publish
Claude: Plan: v0.2.0, previous tag v0.1.0, 1 item since it.
  Package: ExamPrep.Web.
  Left out: ExamPrep.Mobile: not published to the Play Store, signing is not set up (ANDROID_SIGNING_KEYSTORE,
  ANDROID_SIGNING_ALIAS, ANDROID_SIGNING_STORE_PASS, ANDROID_SIGNING_KEY_PASS unset).
  Want to create the upload keystore now? Tell me where it should live (an absolute path outside the repository).
You: D:\keys\examprep-upload.keystore
Claude: Run this in your own terminal (it asks for the passwords there, and I never see them):
```

```powershell
keytool -genkeypair -keystore D:\keys\examprep-upload.keystore -alias upload -keyalg RSA -keysize 2048 -validity 10000
```

```bash
keytool -genkeypair -keystore /d/keys/examprep-upload.keystore -alias upload -keyalg RSA -keysize 2048 -validity 10000
```

```
Claude: Then set ANDROID_SIGNING_KEYSTORE (that path), ANDROID_SIGNING_ALIAS (upload), ANDROID_SIGNING_STORE_PASS and
  ANDROID_SIGNING_KEY_PASS, name the four in docs/infra.md, and back the keystore up outside this machine:
  it is the upload key, and Play can only reset it through support.
```

Later, with the variables set and `ApplicationId` changed from the template's `com.companyname.exampreps` to `br.com.exampreps`:

```
> /agile:publish
Claude: Plan: v0.2.0 ... Package: ExamPrep.Web and ExamPrep.Mobile (android, aab).
Claude: Packaged ExamPrep.Web (zip 4.7 MB) and ExamPrep.Mobile: br.com.exampreps-Signed.aab, 31 MB, jarsigner: jar verified.
  docs/releases/v0.2.0.md and docs/releases/v0.2.0-store.md written.
Claude: Release v0.2.0: both files committed on main (docs(release): v0.2.0), tag, push, GitHub Release.
  Upload the .aab by hand: the plugin uploads nothing. docs/releases/v0.2.0-store.md lists the Play Console steps
  (testing track first, then promote), the first-release ones and, for a Mac, the iOS ones.
```

With `ApplicationId` still `com.companyname.exampreps`, the same command leaves the Mobile head out (`the id is permanent once uploaded: change it first`) and publishes the site.

### 14.33 A Hybrid app in its own repository, sharing the site's components through a package

The nutritionist's patient app (14.30) now wants the site's own screens inside the app. First, in the **site's** repository, the epic round:

```
> /agile:epic "Patient app"
Claude: ... (a) Where does the app live? Recommendation: in this repository. Or a new repository, with its own version.
(b) Screens? Recommendation: MAUI Blazor Hybrid — the site's components are reused in the app; in a new repository they travel
    as the NutriSite.Shared package. Or MAUI XAML (native look).
> a: a new repository, b: Hybrid; 1 and 2
Claude: The site's epic is "API for the mobile app":
- Mobile API foundation — sign-in for the app, the forced-update gate, the OpenAPI document, and NutriSite.Contracts and
  NutriSite.Shared as packable projects (the Complement line will name the package; docs/infra.md names GITHUB_PACKAGES_TOKEN)
- API and shared screen for meal plan
- API and shared screen for appointments
```

When the site is released:

```
> /agile:publish
Claude: Plan: v0.4.0, previous tag v0.3.0, 3 items since it.
  Package: NutriSite.Web; NutriSite.Contracts (nuget 0.4.0); NutriSite.Shared (nuget 0.4.0).
Claude: Packaged NutriSite.Web (zip 6.1 MB); packages/NutriSite.Contracts.0.4.0.nupkg and packages/NutriSite.Shared.0.4.0.nupkg
  (Shared holds staticwebassets/ and depends on NutriSite.Contracts 0.4.0).
Claude: Release v0.4.0: notes committed, tag, push, GitHub Release; both packages pushed to
  https://nuget.pkg.github.com/<owner>/index.json with --skip-duplicate.
```

With `GITHUB_PACKAGES_TOKEN` not set, the same command says `Left out: NutriSite.Contracts, NutriSite.Shared: not published as NuGet packages, GITHUB_PACKAGES_TOKEN is not set` and publishes the site. If the push of the packages fails after the tag, the report names the `dotnet nuget push` command to rerun, with `--skip-duplicate` and without the token.

Then, in the **app's** repository, with a brief that names the site:

```
> /agile:bootstrap
Claude: Round 1 ... Question 2d, where does the backend live? Recommendation: an existing API — the brief names nutri-site.
    Already clear: the screens are MAUI Blazor Hybrid — nutri-site's CLAUDE.md says its shared screens are in the NutriSite.Shared package.
> ok
...
Claude: Skeleton: NutriApp.Mobile.Core (references the NutriSite.Shared package), NutriApp.Mobile (BlazorWebView head),
  a nuget.config with nuget.org and the site's feed (source mapping: NutriSite.* on the feed, * on nuget.org; the token as
  %GITHUB_PACKAGES_TOKEN%), Directory.Packages.props pinning NutriSite.Shared 0.4.0, docs/api/backend.md with release v0.4.0.
  No NutriApp.Contracts project: the records come in the package. CLAUDE.md gets "- Shared screens: package NutriSite.Shared ...".
```

Later, `/agile:sync` in the app, after the site shipped `v0.5.0`:

```
Claude: Plugin files: all the same. The site has release v0.5.0, past the pinned NutriSite.Shared 0.4.0; moving the pin and
  refreshing docs/api/backend-openapi.json from that release is an item of this app. Nothing was written.
```

### 14.34 A new site born with its mobile app

A physiotherapist's brief asks for a site where patients see their exercise plans and book sessions, and "an app for patients, from the start":

```
> /agile:bootstrap
Claude: Round 1.
2. Architecture profile — Recommendation: web-app. Reason: screens over simple data (plans, sessions), one business area,
   and the app shows part of the site. mobile on monolith would fit if an API were the main consumer; it is not.
2e. A mobile app from day one? no · yes, MAUI Blazor Hybrid · yes, MAUI XAML
   Recommendation: yes, Hybrid. Reason: the brief names an app for patients; with Hybrid the site's screens are born in
   PhysioSite.Shared and the app reuses them. It lives in this repository and solution.
   Which of the brief's features go to the app? All ticked; answer "ok" or the numbers to untick.
   Patient area: 1 Exercise plan · 2 Session booking · 3 Messages
   Back office:  4 Patient records · 5 Schedule settings
> ok, untick 4 and 5
Claude: Taken to the app: 1 Exercise plan, 2 Session booking, 3 Messages.
...
Claude: Board proposal:
- UI kit and gallery, built from the identity — the kit in PhysioSite.Shared, the gallery page in PhysioSite.Web
- Epic "Patient area": Exercise plan, Session booking, Messages — each screen a component in Shared hosted by a page in
  Web, its rule or query in Features/ behind an interface in Shared
- Epic "Back office": Patient records, Schedule settings
- Epic "Mobile app" (docs/epics/mobile-app.md, the three features under "Features taken to the app"):
  Mobile foundation (after the UI kit and sign-in), Exercise plan on mobile, Session booking on mobile, Messages on mobile
  — each "on mobile" after Mobile foundation and the site's matching item
Create them?
> ok
Claude: Skeleton: PhysioSite.Web, PhysioSite.Contracts, PhysioSite.Shared (Razor Class Library, texts in
  pt-BR, pt-PT and en), PhysioSite.Tests with the architecture and layout tests. No app project and no Complement line yet:
  Mobile foundation brings them. Create it?
```

Months later, `/agile:refine` of "Mobile foundation" reads the complement's "Born with the site": `Contracts` and `Shared` are already there, so the item adds `PhysioSite.Mobile.Core` and `PhysioSite.Mobile`, the app's sign-in, the `Complement:` line and the head at the site's current version.

### 14.35 Deploying a release

ExamPrep (14.29) is a `web-app` with an AppHost on the recipe. Its `docs/infra.md` has staging (`http://localhost:5081`) and production (`http://localhost:5080`) with the deploy command and a Check URL, and `Parameters__apikey` in `## Expected secrets` as an environment variable of both. `main` carries `<Version>0.2.0</Version>` and there is no tag yet. Before starting Claude you set `Parameters__apikey` in your own terminal; its value is never typed in the chat.

```
> /agile:publish staging
Claude: Plan: release v0.2.0 (previous v0.1.0, 2 items), then deploy it to staging.
  Deployed on staging now: nothing recorded.
  Command: aspire deploy --apphost src/ExamPrep.AppHost/ExamPrep.AppHost.csproj -e Staging
           -o artifacts/deploy/staging --clear-cache --non-interactive --nologo
  Secrets, names only: Parameters__apikey (set).
  Going on: typing the command is the authorization. The deploy builds the image: it takes about a minute.
Claude: Release v0.2.0 done: notes committed, tag and main pushed, GitHub Release created.
Claude: Deployed v0.2.0 to staging in 43 s. http://localhost:5081/health answered 200 after 0.1 s.
  docs/infra.md says "v0.2.0 (2026-10-02)" for staging: commit 7172abd, pushed.
  Output saved in C:\Users\you\AppData\Local\Temp\agile-deploy\staging-v0.2.0-....log.
  1 image tag aspire-deploy-* is left (docker image ls --filter "reference=*:aspire-deploy-*"); old ones are never deleted here.
```

Staging is checked by you, then:

```
> /agile:publish production
Claude: Plan: promotion. v0.2.0 is already tagged: no new package, notes or tag.
  Deployed on production now: nothing recorded. To deploy: v0.2.0. Secrets, names only: Parameters__apikey (set).
Claude: Deployed v0.2.0 to production in 18 s; http://localhost:5080/health answered 200. docs/infra.md updated (commit pushed).
  Staging was not touched: two compose projects run side by side.
```

A later version, 0.3.0, goes to production and turns out wrong. The rollback needs the tag and no question:

```
> /agile:publish production v0.2.0
Claude: Plan: rollback. Deployed on production now: v0.3.0. To deploy: v0.2.0 (the tag exists). Secrets: Parameters__apikey (set).
Claude: Deployed v0.2.0 to production in 17 s; /health answered 200. docs/infra.md says "v0.2.0 (2026-10-02)".
  One production project is running (replaced, not duplicated); your main checkout stayed on main.
```

A broken version fails and says how to go back; the secret is checked first:

```
> /agile:publish staging v0.3.1
Claude: The deploy command exited 0 but http://localhost:5081/health never answered 200 in 60 s (last: HTTP 503).
  Nothing was recorded and nothing was rolled back. Output tail: ...
  To go back to the version recorded before: /agile:publish staging v0.3.0

> /agile:publish production
Claude: The environment variable Parameters__apikey is not set: set it outside this chat (its value is never read, printed
  or asked for), then run the command again. Nothing ran.
```

A compile error at the tag ends the same way (`the deploy command exited 6`, the build's last lines, the running version still answering). With no environment, `/agile:publish` after a ship lists them: `local (not declared) · staging v0.3.0 · production v0.2.0`, and asks which one or "none" (the release only). An environment whose command says `not declared` deploys nothing: `Claude: production declares no deploy command: write it in the "Deploy command" cell of the production row of docs/infra.md.`

### 14.36 A pushed tag deploys itself

ExamPrep (14.35) has `staging` and `production` declared in `docs/infra.md`, `Parameters__apikey` as an environment variable of both, and a GitHub `origin`. It was bootstrapped before the pipeline existed, so after a plugin update `/agile:sync` says so and writes nothing:

```
> /agile:sync
Claude: Deploy pipeline (since 0.0.102) is missing: a GitHub Actions workflow that deploys a pushed v<Version> tag
  by the Deploy command of docs/infra.md. A sync never installs it; capture an item for it? (y/n)
> y
```

The item runs the pipeline writer of sync.js and prints what it wrote; the report before it shows the choices it made from the table:

```
wrote .github/workflows/deploy.yml (tag deploys to staging; secrets: Parameters__apikey) and .github/scripts/agile-deploy.js
```

You add `Parameters__apikey` to the GitHub environments `staging` and `production` (Settings, Environments), and a required reviewer to `production`. From then on, a local release says it before running:

```
> /agile:publish staging
Claude: Plan: release v0.3.0 (previous v0.2.0, 2 items), then deploy it to staging.
  The pipeline (.github/workflows/deploy.yml) also deploys v0.3.0 to staging when the release pushes the tag:
  the two run the same command on the same environment at the same time and are not serialized (the concurrency group of the pipeline queues only its own runs).
  ...
```

Pushing the tag starts the run. Its summary, for the tag:

```
## Deployed

v0.3.0 deployed to staging

It took 43 s.
Record it: the Version (deployed on) cell of staging in docs/infra.md is written only by a local /agile:publish.
Check: http://staging.examprep.example/health answered 200 in 0.4 s.
```

Production is a manual run (Actions, Deploy, Run workflow: `environment` = `production`, `tag` = `v0.3.0`), which waits for the reviewer you set. A secret that was never added stops the run before the command, and nothing but its name is printed:

```
the secret Parameters__apikey is not set for the GitHub environment production: add it to the environment's secrets,
make sure .github/workflows/deploy.yml passes it (secrets.<name>), then run again; the command did not run
```

A version that deploys but never answers fails the run and names how to go back; rolling back is the same manual run with the old tag, or `/agile:publish production v0.2.0` from your machine:

```
the deploy command exited 0 but http://staging.examprep.example/health never answered 200 in 60 s (last: HTTP 503)

Nothing was rolled back. To go back: /agile:publish staging v<the version recorded for staging in docs/infra.md> locally, or run this workflow by hand with that tag.
```

When the table later lists one more secret, the next `/agile:sync` shows `.github/workflows/deploy.yml` as a `manual` difference (the new `secrets.<name>` line) and you merge it by hand.

### 14.37 The size of what is always loaded

Simulab's `CLAUDE.md` has 553 words. At the next `/agile:sync` the table has one row, whatever else changed:

```
| File / check | State | Action |
| CLAUDE.md size | 553 words (limit ~900); always loaded ~3.6k tokens (budget ~4.5k) | none |
```

Months later retros have grown `## Project-specific rules` and `## Models`; the row now marks it:

```
| CLAUDE.md size | 941 words (limit ~900); always loaded ~4.6k tokens (budget ~4.5k) | over the limit and the budget: trim "Project-specific rules" or "Models" |
```

The sync asks nothing and writes nothing here. At the next `/agile:retro`, before adding a line to `CLAUDE.md`, Claude proposes moving one first:

```
Claude: CLAUDE.md is at 941 words (limit ~900). Before adding this line, move "Use the shared JSON options" to .claude/rules/agile/project.md? (y/n)
```

You decide; if you say no, the line is added anyway: nothing blocks on size.

### 14.38 The app version moves to Directory.Build.props

AndreaLisboa is a `website` bootstrapped before 0.0.104: `<Version>0.4.0</Version>` sits on `AndreaLisboa.Web.csproj`, and `AndreaLisboa.Core.dll` reports `1.0.0` because no library carries a number. You update the plugin, restart, run `/agile:sync` (it reports the state and offers nothing: `<Version> is on AndreaLisboa.Web.csproj (0.4.0): the next /agile:ship moves it to Directory.Build.props`) and, a feature later, you type `/agile:ship F-12`. The ship report has two more lines, and no question came before them:

```
app version: moved 0.4.0 from AndreaLisboa.Web.csproj to Directory.Build.props (highest wins: 0.4.0)
app version: 0.4.0 -> 0.5.0 in Directory.Build.props (feature)
```

The change is a commit of its own on the item's branch, before the merge. From then on every DLL of the app reports `0.5.0+<commit>`: the file properties of `AndreaLisboa.Core.dll` on any machine name the release and the commit it came from. A project with no version anywhere (Simulab) reads `app version: added 0.1.0 to Directory.Build.props (no bump: this item ships as 0.1.0)` instead, and `/agile:publish` reads the number from the same file.

### 14.39 Offline-first from the first day

FieldCheck's brief says inspectors fill checklists in basements and plants "with no signal", and the app uses the existing API of the company's `ops-api` repository, an agile `web-api`:

```
> /agile:bootstrap
Claude: Round 1. Question 1: mobile on an existing API. Question 2: mobile. Question 2d, where does the backend live?
   Recommendation: an existing API. Reason: the brief names ops-api.
2f. Online or offline-first? online · offline-first
   Recommendation: offline-first. Reason: the brief says inspectors work "with no signal".
   ops-api's docs/api/openapi.json has no Idempotency-Key header on its write operations: I propose the issue
   "Idempotent writes for FieldCheck" on ops-api's board, and "Offline foundation" waits on it. I never write in that repository.
> ok, create the issue
Claude: Summary: CLAUDE.md gets "- Offline: first", ADR-0001 one line. Board: UI kit and gallery, Mobile foundation,
   Offline foundation (after both; waits on ops-api#31), then the features.
```

Weeks later `/agile:refine` of "Checklist on mobile" asks the extra round the profile requires, `## Offline`: which checklists are cached (the inspector's assigned ones), which writes queue (answers and notes; the photo upload is a separate write), and who wins a conflict — the default (the server refuses a stale write and the user chooses) unless you say otherwise. The feature file carries your answers, and every acceptance criterion about saving has its offline twin.

### 14.40 The optional arguments: resume a quiz, a screen by name, a change in one line, a pause note, a retro of one item

Five commands take an argument you may leave out. Each one below is the same command you already know, with the argument written.

Resume a quiz you stopped (`/agile:bootstrap [round]`). The answers of the rounds already done are in `docs/decisions/.quiz-draft.md`, so nothing is asked again:

```
> /agile:bootstrap 4
Claude: Resuming from round 4. Rounds 1 to 3 are in docs/decisions/.quiz-draft.md (mobile, offline-first, PostgreSQL).
   Round 4. Question 9: ...
```

Design one screen of a feature that has several (`/agile:screen F-12 "Checkout summary"`). The name tells Claude which screen to send to the `ux-designer`:

```
> /agile:screen F-12 "Checkout summary"
Claude: F-12 is refining. Designing only "Checkout summary" with the ux-designer; "Cart" follows an existing screen, so I only mention it in the file.
```

Say the change in the same line (`/agile:change F-12 "the discount is per item, not per order"`). Claude still shows the affected ids and waits for your "aprovo" before it writes the change note:

```
> /agile:change F-12 "the discount is per item, not per order"
Claude: F-12 is building. Affected: BR-2 (discount per order → per item), AC-3, AC-5; not affected: UC-1, AC-1. Code already written: DiscountCalculator.cs and 2 tests to adjust. Approve?
> aprovo
Claude: Change note v2 written, F-12 stays building; resuming at step 4 of the plan.
```

Leave a note for the next session (`/agile:pause "waiting for the payment sandbox key"`). The note goes into the pause message, next to the wip commit and the next step:

```
> /agile:pause "waiting for the payment sandbox key"
Claude: wip(F-12): discount per item, tests of AC-3 half written (4e1f2a9). Next: finish AC-3, then the screen.
   Pending with you: the payment sandbox key. Nothing running.
```

Turn the lessons of one item, or of the whole session, into rules (`/agile:retro F-12` or `/agile:retro session`). With an item id Claude reads that item's change notes, decisions and validation fixes; with `session` it reads what went wrong or was corrected in this chat. At most 3 lessons either way, each with a plugin and a project score from 0 to 10 (Example 14.66):

```
> /agile:retro session
Claude: 2 lessons from this session, each with its scores, its destination and the exact line. Approve?
```

### 14.41 The offline queue is sent with the app closed

FieldCheck's "Offline foundation" is being built (Example 14.39). Nothing extra is asked: the background sending is part of that item, so its acceptance criteria already carry it, and the validation script gains one pass for it:

```
> /agile:build #12
Claude: ... IBackgroundSync (Schedule, Cancel) in Mobile.Core; the Android head implements it with WorkManager
   (Xamarin.AndroidX.Work.Runtime 2.11.2.1, MIT AND Apache-2.0, head only). Core tests with a fake IBackgroundSync: 14 passed.
   Validation script, step 6 (emulator): airplane mode on, save two checklists, swipe the app from recents,
   network on. Without opening the app, the API receives both, once each, in order. Open the app: 0 pending.
   Step 7: a checklist someone else changed meanwhile ends in the conflict list at the next open; the other one was sent.
   Step 8: Settings, Force stop, network on: nothing is sent until you open the app, and then the queue goes out at once.
```

With the app closed nothing is shown: no notification and no permission. An expired session or a required update waits for the next open, where the banner says "sign in again" or "update required"; a background run never signs out and never deletes a pending write. iOS is a separate item (agile-canary#55).

### 14.42 A conflict is merged field by field

FieldCheck is offline-first (Examples 14.39 and 14.41). Two inspectors changed the same checklist, one of them with no signal. Nothing is asked at refinement: the merge is part of "Offline foundation" for every record, and a feature turns it off for a record in its `## Offline` section, with the reason (an amount, where mixing fields is wrong).

```
[Inspector Ana's phone, signal back] Conflict list: "Checklist 12 was changed by someone else"
   [Merge]  [Send mine again]  [Discard]
> Merge
   3 fields were changed on both sides — pick one for each
   Phone      changed on both    ( ) Yours 555-0101   ( ) Current 555-0199
   Notes      changed on both    ( ) Yours "door fixed"   ( ) Current "door replaced"
   Contacts   whole list         ( ) Yours (3 people)   ( ) Current (4 people)
   Email      only yours changed  ana@field.test
   City       only theirs changed Porto
   [Cancel]   [Send merged] (disabled until the three picks are made)
> picks Phone: Yours, Notes: Current, Contacts: Current → Send merged
Claude (build report): Core tests: 13 passed (each row of the field rule, a list as one pick, "Send merged" disabled,
   one update left on the record with the current version, other records untouched, a stale merge returns to the list).
   Validation script, step 7 (emulator): two users change one record, the offline one merges, the API holds the merged values once;
   step 8: a second stale merge returns the record to the list.
```

"Cancel" leaves the conflict as it was. A conflict over a delete, a `404`, a `400` or a `422` has nothing to merge and keeps the two older actions.

### 14.43 A desktop app that works without the network

The brief describes a field-sales desktop app for Windows and macOS: representatives price quotes at a customer's site, often with no connection, and sync later. Question 2a2 has just chosen an API.

```
Claude: Question 2f, online or offline-first? online · offline-first
   Recommendation: offline-first. Reason: the brief says the quotes are priced "at the customer's site, often with no connection".
> ok
Claude: Summary: CLAUDE.md gets "- Offline: first" under Profile:, ADR-0001 one line. Board: UI kit and gallery,
   the first feature, Offline foundation (after both), then the features. The queue is not sent with the app closed:
   that is out of the profile (no tray icon or service).
```

With the same brief but a single-user app on a closed network that goes straight to its PostgreSQL, 2f is not asked and ADR-0001 says: "Offline-first needs an API; a SQLite store is already local."

Later, `/agile:refine` of "Quote list" asks for the `## Offline` section: which quotes are cached (the representative's open ones), which writes queue (price edits, notes), and who wins a conflict. The validation script of "Offline foundation" has two steps only you can run, one per head you chose: with the network adapter off, save twice; the banner shows "2 pending"; turn the adapter on and the pending count empties in order, and the API holds each change once.

### 14.44 A desktop app that updates itself

The reception app (Avalonia, Windows) is bootstrapped with a company share. In round 6:

```
Claude: Question 26b, update source? a network share · an https URL · a public GitHub repository · not declared
   Recommendation: a network share. Reason: the brief says the PCs are in the clinic's network.
> a network share, the clinic's file server, folder updates/reception
Claude: docs/infra.md gets "## Desktop updates" with the Update source line; the head gets Velopack,
   IAppUpdater with a fake for the tests, the Main that runs Velopack first, and the two dialogs in pt-BR, pt-PT and en.
```

`/agile:publish` on 1.0.0 and, later, on 1.1.0:

```
> /agile:publish
Claude: v1.1.0, App.Desktop (velopack, win-x64 -> the share).
   Packed: App-win-Setup.exe 47 MB, App-1.1.0-full.nupkg 39 MB, App-1.1.0-delta.nupkg 62 KB
   (delta built from 1.0.0, downloaded from the share). Tagged v1.1.0, feed sent to the share.
```

On a PC with 1.0.0 installed, the app opens normally; a moment later a small window appears:

```
Update available
Version 1.1.0 is available.            [ Update now ]  [ Later ]
```

"Update now" restarts the app on 1.1.0. "Later" lets the receptionist keep working; the update is applied when the app is closed, even after hours. On the first release the notes also say: "Install once with App-win-Setup.exe" and that Windows SmartScreen warns because the installer is unsigned. A PC that cannot reach the share simply keeps running its version.

### 14.45 The glossary under every document

The reception app's `docs/infra.md` says "Bicep" and "ACR" and the glossary has no row for either. At ship, the item's diff under `docs/` is read:

```
> /agile:ship F-12
Claude: Full suite 214 tests green (41 s). Glossary: 2 rows added: Bicep, ACR
   (the diff of docs/infra.md names both; the rows are on the branch, with the pt-BR meaning). Merged.
```

`docs/glossary.md` now has, under "Technical terms":

```
| Bicep | Bicep | Azure's language for describing infrastructure as code. | A linguagem do Azure para descrever infraestrutura como código. |
| ACR | ACR (registro de contêineres) | Azure Container Registry, where the app's container images are kept. | Azure Container Registry, onde ficam guardadas as imagens de contêiner da app. |
```

A ship whose diff adds no term says `Glossary: nothing missing`. `docs/infra.md` shows, right under its title, `Technical terms: [glossary](glossary.md)`; an ADR or an item file shows `../glossary.md`.

In a project from before 0.0.111, `/agile:sync`:

```
> /agile:sync
Claude: glossary, 3 proposals:
   1. the "Meaning (pt-BR)" column on both tables, written for all 36 existing rows;
   2. 3 rows for terms with none: Bicep, Azure Container Apps, Azure Key Vault (docs/infra.md, ADR-0001);
   3. the glossary line under the title of docs/infra.md and 14 other documents.
   Nothing is written before your OK.
```

### 14.46 The build finds the item's worktree first

You refined B-65 to `approved`; its file lives only in its worktree. A new session sits in the main checkout, which has uncommitted files of its own, and you type:

```
> /agile:build B-65
Claude: Worktree of B-65: D:\wt\examprep\b-65-search-index [bug/B-65]
   status: approved (read there; the main checkout has no copy). That worktree is clean;
   the uncommitted files in the main checkout are not the build's business. Building.
```

An item approved before worktrees existed, or whose worktree was removed, has none; the branch may still exist:

```
> /agile:build B-12
Claude: Worktree of B-12: D:\wt\examprep\b-12-export-csv [bug/B-12] (created now)
   (the branch bug/B-12 already existed, so the worktree was added on it, without -b).
```

`/agile:ship` and `/agile:change` read the status in the same worktree. With none, they stop and name the branch they looked for (`bug/B-12`); they never create one.

### 14.47 A deploy that lands in the client's Azure, never in another one

Simulab's repository and board are yours; its staging belongs to the client Acme, in their Azure tenant. In round 6 of the bootstrap:

```
Claude: Question 25a, cloud account of staging? the client's name and azure · aws · gcp · other · none
        Recommended: azure (question 3 says Azure Container Apps).
You: Acme, azure. I don't have the ids yet.
Claude: docs/infra.md gets, under "## Cloud accounts":
        | staging | Acme | azure | | | | |
        The ids stay blank until Acme gives them; /agile:publish stops until then.
```

Acme's administrator invites you as a guest and gives you the Contributor role on the subscription; you write the two ids in the row. The first publish stops on the login, before the tag is pushed:

```
> /agile:publish staging
Claude: staging is in Acme's Azure (tenant 11111111-1111-4111-8111-111111111111, subscription
        22222222-2222-4222-8222-222222222222): no login in C:/Users/you/.agile/azure/11111111-.../22222222-...;
        nothing ran. Log in once, in Git Bash:
        AZURE_CONFIG_DIR="C:/Users/you/.agile/azure/11111111-.../22222222-..." az login --tenant 11111111-...
        In PowerShell (it does not stay set):
        & { $env:AZURE_CONFIG_DIR = 'C:\Users\you\.agile\azure\11111111-...\22222222-...'; az login --tenant 11111111-...; Remove-Item Env:AZURE_CONFIG_DIR }
```

You run one of the two lines in your own terminal and type the command again. The plan now shows the account and what was cleaned:

```
Claude: staging: v0.4.0 (release, then deploy) · client Acme · azure · login folder .agile/azure/1111.../2222...
        removed from the environment: AZURE_CLIENT_SECRET, ARM_SUBSCRIPTION_ID (names only; the row's values are set instead)
```

Your own default `az` login (another client's subscription) is untouched. Had the login not seen Acme's subscription, the stop would name the declared one and the one it found: `... but the login in <folder> is Basic subscription 2fae... (tenant ...) and cannot see it: ask the client for access to that subscription, or correct the row; nothing ran`. When the project has the deploy pipeline, `/agile:sync` offers the `azure/login` step to its workflow, and Acme's administrator creates the federated credential and you set `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID` as variables of the GitHub environment `staging`, with a protection rule that allows only `v*` tags.

### 14.48 Where a new item lives: the epic question

Two epics are open, "Catalog" and "Content", and no epic fits what you type next. Nothing is created before your answer:

```
> /agile:idea "Exam board registration: name and acronym, list and edit"
```

```
Claude: Epic for F-3 "Exam board registration"?
        (also fits: "Admin" - support registers; choose it through Other)
        1. Catalog (Recommended) - the epic's goal is registering what the candidate searches
        2. Content - exam content
        3. New epic: "Exam boards" - registration and upkeep of exam boards
        4. No epic
```

With a text that fits none of the epics, "New epic" comes first, as the recommendation. Answering it creates the epic, then the feature under it:

```
> /agile:idea "Reports on candidate retention"
Claude: Epic for F-4 "Reports on candidate retention"?
        1. New epic: "Retention" - understand why candidates stop practising (Recommended)
        2. No epic
You: 1
Claude: Created epic "Retention" (board #14, docs/epics/retention.md, draft) and feature F-4 (board #15) under it.
```

"No epic" creates no epic and the output says `epic: none`. `/agile:epic` never asks: it already knows its epic. In `/agile:refine F-3`, an out-of-scope answer that becomes a new idea lists "Catalog" first, the epic of the item being refined.

### 14.49 A duplicate found before it is captured, and one cancelled at refinement

ExamPrep has F-12 "Exam board search" in `idea`, and F-15 "Search by acronym" in `refining` in its own worktree. You type:

```
> /agile:idea "search exam boards by acronym"
Claude: 2 similar items (14 items read):
  1. F-12 Exam board search (idea) — the same search, the acronym is one more field.
  2. F-15 Search by acronym (refining, only in its worktree) — the same subject in other words.
  Recommended: improve F-12. [Improve F-12 | Improve F-15 | Capture anyway]
> Improve F-12
Claude: F-12 now ends its Summary with "Added 2026-10-04: search also by board acronym".
No number was taken and nothing was created on the board.
```

Had you chosen F-15, Claude would have added the text to `## Open questions` of `D:/wt/examprep/f-15-search-by-acronym/docs/features/F-15-search-by-acronym.md`, saying that path. For an `approved` F-12 it would have written nothing and printed `/agile:change F-12 "search also by board acronym"` for you to type. Another time nothing is close:

```
> /agile:idea "export the exam results to a spreadsheet"
Claude: No similar item (14 items read).
F-16 Export results, epic Results, board #31, status idea. Nothing else happens until /agile:refine F-16.
```

Later you refine an item captured by hand before this check existed:

```
> /agile:refine F-14
Claude: 1 similar item (15 items read):
  1. F-12 Exam board search (idea) — F-14 asks for the same acronym search.
  Recommended: carry the improvement to F-12 and cancel F-14.
  [Carry over and cancel F-14 | Keep both | Fold F-12 into F-14]
> Carry over and cancel F-14
Claude: F-12's Summary now has "Added 2026-10-04: ...". F-14 is `cancelled` ("Duplicate of F-12
(2026-10-04): the Summary of F-12"); issue #29 closed as a duplicate of #27 and archived from the project.
No branch or worktree was created. The next /agile:idea takes F-17.
```

### 14.50 Every merge reaches the testers as a beta

The reception app of 14.44 has two receptionists who try each change before the clinic gets it. Its `docs/infra.md` gains one line under `## Desktop updates` (a new project answers question 26c with "every merge"):

```
- Update source: \\fs01\updates\reception
- Beta channel: every merge
```

Each tester runs `Reception-beta-Setup.exe` from the share once (it replaces the stable install on that PC). The ship of F-21 ends with one more line:

```
> /agile:ship F-21
Claude: ... merged (a1b2c3d), board #21 Done (read back).
   Beta: 0.5.1-beta sent to \\fs01\updates\reception (delta 72 KB built from 0.5.0-beta);
   testers install Reception-beta-Setup.exe from there.
```

At their next start the testers' PCs show "Version 0.5.1-beta is available."; the clinic's PCs, on stable, see nothing. When the version is released with `/agile:publish`, the clinic takes 0.5.1; the testers stay on beta and take 0.5.2-beta after the next ship.

A week later the file server is down during a ship:

```
> /agile:ship F-22
Claude: ... merged (d4e5f6a), board #22 Done (read back).
   Beta: not sent. vpk upload of 0.5.2-beta (beta) to \\fs01\updates\reception failed:
   "The network path was not found." The merge stays. When the share is back: /agile:publish --beta
> /agile:publish --beta
Claude: Beta: 0.5.2-beta sent to \\fs01\updates\reception (delta 70 KB built from 0.5.1-beta).
```

Running it once more says "0.5.2-beta is already on the share: nothing was packed or sent". A tester who wants stable again runs `Reception-win-Setup.exe`.

### 14.51 The same app updates itself on Linux

The clinic's new front desk runs Ubuntu. The reception app of 14.44 is Avalonia, so its head lists one more runtime: `<RuntimeIdentifiers>win-x64;linux-x64</RuntimeIdentifiers>`. The first `/agile:publish` after that stops before building anything:

```
> /agile:publish
Claude: Not published. Reception.Desktop: the Update source is the share \\fs01\updates\reception,
   which Linux machines do not read by that name: add
   `- Update source (linux): <the path where Linux mounts it, or https://...>` under `## Desktop updates`.
```

The Linux PCs mount the file server at `/mnt/updates`, so `docs/infra.md` gains one line (an item on the main branch, like any change):

```
- Update source: \\fs01\updates\reception
- Update source (linux): /mnt/updates/reception
```

Publishing again, still from the Windows PC:

```
> /agile:publish
Claude: v1.3.0, Reception.Desktop (velopack, win-x64, linux-x64 -> \\fs01\updates\reception;
   Linux reads /mnt/updates/reception).
   win-x64: delta 74 KB built from 1.2.0. linux-x64: Reception.AppImage 49 MB, first release on channel linux
   at this source: no delta. Tagged v1.3.0; both feeds sent to the share; the GitHub Release carries the AppImage.
```

The notes of v1.3.0 end with the Linux install lines: download `Reception.AppImage`, put it in `~/Applications`, `chmod +x`, start it; a copy from an earlier zip does not update itself; the first update downloads the full package, the next ones only what changed. From 1.4.0 on, the Ubuntu desk sees the same "Version 1.4.0 is available." window as the Windows PCs, in the language of the session (`LANG`), and "Later" applies it when the app is closed.

### 14.52 The installer carries the clinic's name

The reception app of 14.44 asks every new PC "Do you want to allow this app from an unknown publisher?". The clinic's company, in Portugal, opens a Microsoft Artifact Signing account with a public-trust certificate profile, and the owner writes its `metadata.json` to `C:\signing\clinic\metadata.json`, outside the repository. `docs/infra.md` gains, through an item like any change:

```
## Code signing (a desktop app only)
- Code signing: artifact-signing
- Signing account: 0f3c.../7a1d...
```

The first publish after the merge stops before building anything:

```
> /agile:publish
Claude: Not published. Code signing is artifact-signing in docs/infra.md but VPK_AZURE_TRUSTED_SIGN_FILE
   is not set: set it in the shell that runs /agile:publish (its value is never written in docs/infra.md).
   A declared mode is never published unsigned; nothing was built.
```

The owner sets the variable in their own terminal (`$env:VPK_AZURE_TRUSTED_SIGN_FILE = 'C:\signing\clinic\metadata.json'`), restarts the session from there, and logs the signing account in once, with the line the next stop gives (`AZURE_CONFIG_DIR="C:/Users/owner/.agile/azure/0f3c.../7a1d..." az login --tenant 0f3c...`). Then:

```
> /agile:publish
Claude: v1.5.0, Reception.Desktop (velopack, win-x64 -> \\fs01\updates\reception, signed: artifact-signing;
   login in C:/Users/owner/.agile/azure/0f3c.../7a1d...).
   win-x64: delta 81 KB built from 1.4.0. Signed by Clinica Sorriso, Lda: Reception-win-Setup.exe,
   Reception.Desktop.exe and Update.exe checked, Valid. Tagged v1.5.0; feed sent to the share.
```

The notes of v1.5.0 say: "The installer is signed by Clinica Sorriso, Lda. During the first weeks Windows may still show "unrecognized app" with that name: click "More info", then "Run anyway"." The installed PCs update to 1.5.0 by the usual delta and notice nothing; a new PC's download dialog names the clinic. v1.6.0's notes do not repeat the line. Had the certificate profile expired, the publish would have stopped with `Reception-win-Setup.exe is not validly signed (status ...)`, before the notes and the tag.

### 14.53 The installers go public, the code stays private

The clinic's reception app of 14.44 grows past its file server: a second clinic, in another city, has no access to it. The code lives in the company's private Azure DevOps repository, so the owner creates an empty **public** repository on GitHub, `clinica-sorriso/reception-releases`, only to hold the releases, and changes one line of `docs/infra.md` (an item like any change):

```
- Update source: https://github.com/clinica-sorriso/reception-releases
```

The head reads the source with a `GithubSource` when the line starts with `https://github.com/`. The next `/agile:publish` checks the repository first:

```
> /agile:publish
Claude: Not published. Reception.Desktop: the Update source is https://github.com/clinica-sorriso/reception-releases,
   a PRIVATE repository: a private repository would need a token inside every installed app, and that token
   reads the code: use a public repository, a share or an https source; nothing was built.
```

The owner had created it private by mistake; once it is public:

```
> /agile:publish
Claude: v2.1.0, Reception.Desktop (velopack, win-x64 -> GitHub Releases of clinica-sorriso/reception-releases).
   win-x64: delta 64 KB built from 2.0.0 (downloaded from the repository, no token). Tagged v2.1.0 in the code repository.
   GitHub Release v2.1.0 created in clinica-sorriso/reception-releases with the notes; Setup.exe, the portable zip,
   the packages and the feed index attached. Installed apps see it up to 60 s after the upload (GitHub's API cache).
```

A day later the upload of a release is cut halfway by a dropped connection:

```
> /agile:publish
Claude: v2.2.0 tagged and pushed. vpk upload to clinica-sorriso/reception-releases failed (win-x64): connection reset;
   the release v2.2.0 stays with Reception-2.2.0-full.nupkg, and the installed apps see no update until
   releases.win.json is attached (vpk uploads it last). To retry:
   gh release delete-asset v2.2.0 Reception-2.2.0-full.nupkg --repo clinica-sorriso/reception-releases --yes
   dotnet vpk upload github --repoUrl https://github.com/clinica-sorriso/reception-releases --token "$(gh auth token)" --merge --tag v2.2.0 -o artifacts/publish/v2.2.0/velopack/win-x64
```

The token in the last line is your own `gh` login, read when the command runs; it never appears in the chat. The PCs of the second clinic update by delta as soon as the release is complete; a PC that starts the app more than 60 times in an hour behind one office address sees a failed check in its log and tries again at the next start.

### 14.54 The staff manage the data the app shows

FieldCheck's brief (a `mobile` app on `monolith`, with its API in the same solution) says: "our staff add the checklists and see every inspector's visits". Right after 2f:

```
Claude: Question 2g, a web backoffice for the staff? no · yes
   Recommendation: yes. Reason: the brief names "our staff" who "add the checklists and see every inspector's visits".
> ok
Claude: Summary: CLAUDE.md gets "- Backoffice: staff" under Profile:, ADR-0001 one line (a Blazor Server area under
   /backoffice in the API's host, one Identity, staff by role, an authenticator app for Admin, staff see every tenant).
   Board: UI kit and gallery, the first feature, Backoffice foundation (after the UI kit), then the features.
   No skeleton change: the backoffice is built by its own item.
```

With the same brief but the API in another repository (`Backend: external`), 2g is not asked and ADR-0001 says: "The backoffice lives with the data, in the API's repository."

Later "Backoffice foundation" is built and validated. Its validation script has the steps only you can run: the app starts with `Backoffice:FirstAdminEmail` set; the link in the log sets the first Admin's password; the Admin signs in, scans the QR code, keeps the 10 recovery codes and reaches the data pages; invites a colleague as `Staff`; the colleague signs in and cannot open the staff page; the Admin removes the colleague, and within a minute the colleague's open page says it is no longer allowed.

A project bootstrapped before 0.9.0 sees the capability at its next sync:

```
> /agile:sync
Claude: Missing capability: Staff backoffice (since 0.9.0): a web area for the staff under /backoffice in the API's host,
   one Identity, staff by role. Item to capture: "Backoffice foundation", which builds "Staff backoffice" of
   profiles/mobile.md and writes "- Backoffice: staff" in CLAUDE.md with one ADR line. Nothing was written.
   Capture it? To decline for good, add "- Backoffice: none" under Profile: in CLAUDE.md and the note stops.
```

### 14.55 A deploy that lands on Azure Container Apps

Acme's production runs on Azure Container Apps, its staging stays on your own host. In round 6 of the bootstrap, after the cloud account of each environment:

```
Claude: Question 25b, recipe of production (cloud azure)? aca · compose
        Recommended: aca (managed database, no host to patch, scales to zero).
You: aca. Staging stays compose.
Claude: Monthly budget of production (a whole number, the ceiling of one month in the billing currency of the subscription, or none)?
You: 80.
Claude: The AppHost is written from the profile's "Deploy recipe (Azure Container Apps + Aspire)": both shapes in one Program.cs.
        src/App.AppHost/appsettings.Production.json gets { "Deploy": { "Target": "aca" } } and no HostPort; staging's has its HostPort.
        docs/infra.md: production's URL and Check URL stay blank (the address is generated at the first deploy), and
        "## Cloud accounts" gets | production | Acme | azure | | | | | 80 |: Tenant, Subscription, Resource group and Region
        are required before the first deploy. Acme's administrator grants Contributor and Role Based Access Control
        Administrator on the subscription.
```

Acme gives you the two ids and you write them, but forget the last two columns. The command stops before anything runs, naming what is blank:

```
> /agile:publish production
Claude: production: Deploy:Target is aca, but its ## Cloud accounts row leaves Resource group and Region blank; fill them (Azure__ResourceGroup, Azure__Location) — nothing ran
```

You write `rg-acme-prod` and `brazilsouth` in the row and type the command again. The plan now says where it will land:

```
> /agile:publish production
Claude: production: v0.5.0 (release, then deploy) · client Acme · azure · login folder .agile/azure/1111.../2222...
        Target: aca (Azure Container Apps), resource group rg-acme-prod, region brazilsouth
        Budget: 80 per month on rg-acme-prod (billing currency of the subscription), alerts at 80 % and 100 % actual and 100 % forecast to Owner and Contributor
```

The first deploy creates the resource group's contents and prints the app's address; you write it in URL and Check URL of the production row. The next deploy, `v0.5.1`, keeps every signed-in user signed in, because the Data Protection keys sit in a blob. When the tag `v0.5.1` is pushed, the pipeline's run log starts with `Aspire CLI 13.6.0 installed (from Aspire.AppHost.Sdk of src/App.AppHost/App.AppHost.csproj)` before the command. A project bootstrapped before 0.10.0 receives the new profile section at its next `/agile:sync`; its own session changes its AppHost to the two shapes. The plugin never edits it.

### 14.56 A deploy that lands in the client's AWS account, never in another one

Acme's staging is in their AWS account `123456789012`, region `us-east-1`; your own machine is logged in to another client's AWS. You write the rows; the account id of staging is wrong, and in production, which is on Google Cloud, you type the project's name instead of its id:

```
| staging | Acme | aws | | 1234 | | us-east-1 |
| production | Acme | gcp | | Acme Shop | | |

> /agile:publish staging --plan
Claude: the Subscription or account of staging in `## Cloud accounts` is "1234": an AWS row needs the 12-digit account id
        (aws sts get-caller-identity prints it); nothing ran
        (/agile:publish production --plan stops the same way: ... of production ... is "Acme Shop": a GCP row needs the project id,
        not its name or number (gcloud projects list prints it); nothing ran)
```

No CLI was called. You fix the id (`123456789012`) and run it again; the plan stops on the login, before the tag is pushed:

```
> /agile:publish staging --plan
Claude: staging is in Acme's AWS account 123456789012 (us-east-1): no login in C:/Users/you/.agile/aws/123456789012;
        nothing ran. Log in once, in Git Bash:
        AWS_CONFIG_FILE="C:/Users/you/.agile/aws/123456789012/config" AWS_SHARED_CREDENTIALS_FILE="C:/Users/you/.agile/aws/123456789012/credentials" aws configure sso
        (then `aws sso login` with the same two variables when the session expires; `aws configure` instead for access keys)
        In PowerShell (it does not stay set):
        & { $env:AWS_CONFIG_FILE = 'C:\Users\you\.agile\aws\123456789012\config'; $env:AWS_SHARED_CREDENTIALS_FILE = 'C:\Users\you\.agile\aws\123456789012\credentials'; aws configure sso; Remove-Item Env:AWS_CONFIG_FILE, Env:AWS_SHARED_CREDENTIALS_FILE }
```

You run one line in your own terminal, signing in with the account Acme gave you, and type the command again:

```
Claude: staging: v0.4.0 (release, then deploy) · client Acme · aws · login folder .agile/aws/123456789012 · check ok
        removed from the environment: AWS_PROFILE, AWS_ACCESS_KEY_ID (names only; the row's AWS_CONFIG_FILE, AWS_SHARED_CREDENTIALS_FILE and AWS_REGION are set instead)
```

Had you signed in to the wrong account in that folder, the stop would name both: `staging is declared in Acme's AWS account 123456789012, but the login in <folder> is account 210987654321 (arn:aws:iam::210987654321:user/you): log in to the right account in that folder, or correct the row; nothing ran`. The AWS login of your own machine (another client's account) is untouched. For production, once its project id is right, the stop on a missing login lists both Google Cloud logins (`gcloud auth login` and `gcloud auth application-default login`, each with `CLOUDSDK_CONFIG` set to `.agile/gcp/<project>`), and an identity that cannot read the project is named with the permission it lacks: `... but you@acme.example in <folder> cannot read it (needs resourcemanager.projects.get: Browser, Viewer, Editor or Owner): ask the client for access, or correct the row; nothing ran`. With the pipeline, `/agile:sync` shows the `configure-aws-credentials` (or `google-github-actions/auth`) step for the workflow, Acme's administrator creates the IAM role, and you set `AWS_ROLE_ARN` and `AWS_REGION` as variables of the GitHub environment `staging`, with a protection rule that allows only `v*` tags.

### 14.57 The zip carries the studio's name too

A small studio sends its inventory tool, `Stock.Desktop`, as a zip to a few customers; there is no update source, so no installer. The customers' PCs say "Unknown publisher". The studio buys a code-signing certificate on a token and, through an item like any change, `docs/infra.md` gains:

```
## Code signing (a desktop app only)
- Code signing: signtool
```

With `VPK_SIGN_PARAMS` set in the owner's own shell (`/fd SHA256 /tr <the CA's timestamp URL> /td SHA256 /sha1 <thumbprint>`), the first publish stops, because the project has no `vpk`:

```
> /agile:publish
Claude: Not published. Code signing is signtool and Stock.Desktop is packed as a zip, which vpk signs, but this project
   has no vpk local tool in its tool manifest (.config/dotnet-tools.json or dotnet-tools.json); /agile:publish asks first and
   commits the manifest; nothing was built. The two steps, if you prefer to run them yourself:
```

```powershell
dotnet new tool-manifest
dotnet tool install vpk --version 1.2.161
```

```bash
dotnet new tool-manifest
dotnet tool install vpk --version 1.2.161
```

```
Claude: Install vpk 1.2.161 in this project? It adds the tool manifest (dotnet-tools.json), which I commit and push on main.
> yes
Claude: Installed, committed (build(release): vpk local tool for code signing) and pushed. Plan again:
   v1.2.0, Stock.Desktop (zip, win-x64, signed: signtool); linux-x64 packed as zips, which are not signed (Windows only).
   Stock.Desktop-win-x64.zip: 41 files, every .exe and .dll Valid, signed by Stock Studio Ltd. Tagged v1.2.0.
```

The notes of v1.2.0 say: "The app is signed by Stock Studio Ltd. During the first weeks Windows may still show "unrecognized app" with that name: click "More info", then "Run anyway"." v1.3.0's notes do not repeat the line. The zip stays in `artifacts/publish/v1.2.0/`; the owner sends it. Had one library been left unsigned (the certificate profile expired mid-run, say), the publish would have stopped with `Stock.Core.dll is not validly signed (status NotSigned)` (up to five files named), before the notes and the tag; and a zip packed before the mode was declared is refused at `release` with the main `.exe` named.

### 14.58 A ceiling for the month, and an e-mail before the bill

Acme's production runs on Azure Container Apps and was bootstrapped before the monthly ceiling existed; you added the `Monthly budget` column to `## Cloud accounts` after the update but left production's cell blank (without the column the stop would say `has no Monthly budget column; add it after Region and ...`). The next publish stops before anything runs:

```
> /agile:publish production
Claude: production: Deploy:Target is aca, but its ## Cloud accounts row has no Monthly budget; write a whole number (the ceiling in the subscription's billing currency) or none — nothing ran
```

You write `80 USD` in the cell. The ceiling is a number, and the currency is the subscription's own, so it stops again, naming the cell and the value:

```
> /agile:publish production
Claude: production: Monthly budget "80 USD" is not a whole number or none — nothing ran
```

You write `80` and type the command again. The plan adds the line after `Target:`:

```
> /agile:publish production
Claude: production: v0.5.0 (release, then deploy) · client Acme · azure · login folder .agile/azure/1111.../2222...
        Target: aca (Azure Container Apps), resource group rg-acme-prod, region brazilsouth
        Budget: 80 per month on rg-acme-prod (billing currency of the subscription), alerts at 80 % and 100 % actual and 100 % forecast to Owner and Contributor
```

After the deploy command the report says what was written: `Budget agile-monthly-ceiling on rg-acme-prod: created, 80 USD per month from 2026-10-01`. Acme's administrator (Owner) and you (Contributor, a guest in their tenant) now get Azure's e-mail when the month's spend reaches 80 % or 100 % of 80 USD, or when the forecast says it will pass 100 %. Nothing is stopped when it fires; deciding what to do is yours. In November the ceiling goes to 120: you change the cell and publish again, and the report says `updated, 120 USD per month (from 2026-10-01, unchanged)`, because Azure keeps the start date of a budget.

When Azure refuses the write, the deploy and the release stand and the run says so after them, with a non-zero exit:

```
Claude: deploy ok; budget not written: Start date of budgets cannot be updated. Please delete and create a new budget.
```

When production no longer needs an alert you write `none`: nothing is written or deleted, and the plan names the budget that is still there: `a budget agile-monthly-ceiling still exists on rg-acme-prod; delete it by hand if the environment should have none`. When the environment is gone for good, you delete its resource group and then its budget, because the budget outlives the group. The command for the budget is:

```powershell
az rest --method delete --url "/subscriptions/<subscription>/resourceGroups/<group>/providers/Microsoft.Consumption/budgets/agile-monthly-ceiling?api-version=2023-11-01"
```

```bash
az rest --method delete --url "/subscriptions/<subscription>/resourceGroups/<group>/providers/Microsoft.Consumption/budgets/agile-monthly-ceiling?api-version=2023-11-01"
```

### 14.59 Parking staging between test windows

Acme's staging runs on Azure Container Apps, with a PostgreSQL server and a background host that sends e-mail (minimum 1 replica). The test window ended on Friday, so you park it. Claude shows what would change and asks:

```
> /agile:publish staging --park
Claude: staging (rg-acme-staging):
        api: minimum 1 -> 0
        web: already 0
        postgres-xyz: Ready -> Stopped (mark agile-parked)
        Park staging now? (yes runs these changes)
You: yes
Claude: It takes about 5 minutes (the server stop is the slow part).
        staging (rg-acme-staging): api minimum 1 -> 0; web already 0; postgres-xyz stopped (5 min 7 s). Azure starts postgres-xyz by itself on 2026-10-13 unless it is resumed first.
```

The registry, the log workspace and the storage keep costing, about 8.7 a month in USD plus what Log Analytics ingests; the database compute and the background host, about 24 a month together, stop. Any answer other than yes, and nothing runs.

On Tuesday the next version goes to staging while it is still parked. You type the usual command; the plan says what will happen first, and the deploy does it:

```
> /agile:publish staging
Claude: staging: v0.6.0 (release, then deploy) · client Acme · azure · login folder .agile/azure/1111.../2222...
        Target: aca (Azure Container Apps), resource group rg-acme-staging, region brazilsouth
        staging is parked: the deploy starts PostgreSQL postgres-xyz first (about 3 to 4 min)
        ...
Claude: staging was parked: PostgreSQL postgres-xyz started (3 min 27 s) before the deploy
        v0.6.0 deployed to staging ...
```

Deploying over a stopped server would have failed half way (Azure refuses to change a stopped PostgreSQL server), so the start comes first and the mark is removed; the apps get the AppHost's minimums from the deploy. The pipeline does the same when a pushed tag starts it. You never type a yes for this: typing `/agile:publish` already was the authorization, and the plan said it.

Two weeks later you park it again and come back to it for a demo. Azure had started the server on its own after 7 days; the mark `agile-parked` was still there, so the second park stops it again. For the demo:

```
> /agile:publish staging --resume
Claude: staging (rg-acme-staging):
        postgres-xyz: Stopped -> Ready (mark agile-parked removed)
        api: minimum 0 -> 1
        web: left at 0 (no agile-min-replicas tag)
        Resume staging now? (yes runs these changes)
You: yes
Claude: staging: postgres-xyz started (3 min 27 s), mark removed; api minimum 0 -> 1; web left at 0 (no agile-min-replicas tag); Check URL https://staging.acme.example/health answered 200 in 12 s.
```

`web` stays at 0 because it was never changed: only an app that parking changed carries the tag that resume reads. When Azure answers that an app or the server is busy, the command waits and retries for 5 minutes, then stops with `api: (ContainerAppOperationInProgress) ... (still busy after 5 min 0 s); already changed: nothing`; running it again is safe. Another environment (a `compose` one, or an AWS or Google Cloud row) stops with `staging: --park works only on an aca environment (Deploy:Target is compose); nothing ran`.

### 14.60 How much has staging spent so far this month?

Acme's staging has a ceiling of 80 a month in its `## Cloud accounts` row, and Azure sends an e-mail at 80 % and at 100 %. Ten days in you do not want to wait for the e-mail; you want the number:

```
> /agile:publish staging --cost
Claude: staging (aca, rg-acme-staging): Spend this month: 42.10 BRL of 80 (52 %) — figures from Azure Cost Management lag 8 to 24 hours
```

The currency is the one Azure bills the subscription in, never assumed by the plugin; 52 % is 42.10 of 80, rounded down. The figures lag by 8 to 24 hours, so what you spent this morning is not in yet, and a group deployed today reads `0.00`. Nothing was written: no tag, no file, no commit. The next deploy shows the same reading in its plan, after the budget line:

```
> /agile:publish staging
Claude: staging: v0.6.0 (release, then deploy) · client Acme · azure · login folder .agile/azure/1111.../2222...
        Target: aca (Azure Container Apps), resource group rg-acme-staging, region brazilsouth
        Budget: 80 per month on rg-acme-staging (billing currency of the subscription), alerts at 80 % and 100 % actual and 100 % forecast to Owner and Contributor
        Spend: 42.10 BRL of 80 (52 %) — figures from Azure Cost Management lag 8 to 24 hours
        ...
```

Azure Cost Management answers only a few queries a minute. If you ask again right away, the command waits and tries up to three more times, and then says what happened instead of failing silently:

```
> /agile:publish staging --cost
Claude: staging: Azure Cost Management is rate-limited; try again in a minute
```

In a plan the same failure never stops the release: the line reads `Spend: not read (Azure Cost Management is rate-limited; try again in a minute)` and the deploy goes on. A `compose` environment, an AWS or Google Cloud row, or an `aca` row with a blank Resource group is refused before anything is asked of Azure (`staging is not an aca environment (Deploy:Target is compose) — nothing ran`).

### 14.61 Which item am I on?

You type `/agile:refine F-12`, thinking of the CSV export. The first line Claude writes shows what that id is, before any other check:

```
> /agile:refine F-12
Claude: F-12 — Export the monthly report as CSV [idea]
        No similar item (14 items read).
        Worktree of F-12: D:\wt\app\f-12-export-the-monthly [feature/F-12] (created now)
```

Had you meant another item, the line is your chance to stop before a branch and a worktree exist. After the approval, the build starts the same way, and the line now comes from the worktree's copy of the file:

```
> /agile:build F-12
Claude: F-12 — Export the monthly report as CSV [approved]
        Worktree of F-12: D:\wt\app\f-12-export-the-monthly [feature/F-12]
```

An id with no file says so and the command goes on with its own rule for a missing item:

```
> /agile:refine F-999
Claude: F-999 — no item file found
        There is no item F-999. Nothing was created.
```

Typing `/agile:ship F-13` for an item still `building` prints `F-13 — Import the exam list [building]` above the stop for a wrong status. `/agile:autopilot F-14` prints its line once at its start, and the refinement, build and ship it runs do not repeat it.

### 14.62 A step of the validation script turned out wrong

F-12 is `validating`. On step 3 you find the command needs a flag the script did not give. Claude fixes the code; the script still says the old command. You type what changed:

```
> /agile:script F-12 step 3 now needs --no-build
Claude: F-12 — Export the monthly report as CSV [validating]
        Step 3 changed: both forms run (Git Bash and PowerShell 7), the output quoted is the real one.
        Steps 1, 2, 4, 5 are unchanged. Commit: docs(F-12): validation script. The issue body was sent whole and read back: same.
        You repeat the script from step 3.
```

The file got a new section text and one commit on the item's branch. There is no change note, no new `version` and no re-approval, and the status stays `validating`. With no text after the id (`/agile:script F-12`) the whole script is rebuilt from the criteria and the branch diff.

If your text really changes what a criterion says, nothing is written:

```
> /agile:script F-12 export only the last 3 months
Claude: F-12 — Export the monthly report as CSV [validating]
        That changes AC2 ("the file holds every month of the year"), not only how it is checked.
        Nothing written. Type: /agile:change F-12 export only the last 3 months
```

An item that is `approved` or `done` stops at once with one line saying why: the script is written at the build, and a done item is never reopened.

### 14.63 Who changed that product?

Acme's mobile app has a staff backoffice since 0.9.0 and no audit. At the next sync after the plugin update:

```
> /agile:sync
Claude: Missing capability: Staff audit log (since 0.18.0): who changed what in the backoffice, in the same transaction,
   read at /backoffice/audit. Item to capture: "Backoffice audit", which builds "Audit log" of "Staff backoffice" in
   profiles/mobile.md and writes "- Backoffice audit: on" in CLAUDE.md. Nothing was written.
   Capture it? To decline for good, add "- Backoffice audit: none" under Profile: in CLAUDE.md and the note stops.
```

A new project does not need this: with `- Backoffice: staff` in the bootstrap, the "Backoffice foundation" idea it captures already includes the audit log, with no extra question.

Once the item is built, a staff member edits a product's price on `/backoffice/products`, and a staff member with the `Admin` role opens `/backoffice/audit`:

```
time (UTC)        member            action   entity   id   changes
2026-10-12 14:03  ana@acme.test     update   Product  41   Price: 19.90 → 24.90
2026-10-12 13:58  ana@acme.test     update   Product  41   ContactEmail: *** → ***
2026-10-12 09:10  rui@acme.test     invite            maya@acme.test
```

The second row says the supplier's e-mail changed and never what it was. The third is a staff action, recorded by name with its target. A member with the `Staff` role opening the same page gets "denied".

### 14.64 Putting staging on the client's domain

Acme wants staging at `https://staging.acme.com.br`. You write that as the URL of the `staging` row of `docs/infra.md` and deploy as usual. The DNS is not ready, so the deploy runs without the domain and says what to ask for:

```
> /agile:publish staging
Claude: staging: v0.6.1 (deploy) · client Acme · azure · login folder .agile/azure/1111.../2222...
        Domain staging.acme.com.br: not bound yet; the client creates these records at their DNS provider, then deploy again:
          CNAME staging → web.happyfield-1a2b3c4d.brazilsouth.azurecontainerapps.io (found: nothing)
          TXT asuid.staging → 8F3C...D210 (found: nothing)
        v0.6.1 deployed to staging; https://web.happyfield-1a2b3c4d.brazilsouth.azurecontainerapps.io/health answered 200 in 9 s.
```

The app keeps answering on its generated address. You send the two lines to Acme's administrator; the `CNAME` has to point straight at that address (a proxy such as Cloudflare's orange cloud in between would block the certificate), and if the zone has a CAA record it must allow `digicert.com`. The next day the records exist; the next deploy finds them, adds the domain, has Azure issue the certificate and binds it, with no step in the portal:

```
> /agile:publish staging --plan
Claude: ...
        Domain staging.acme.com.br: the deploy adds it, creates certificate mc-staging-acme-com-br (validation CNAME) and binds it
> /agile:publish staging
Claude: Domain staging.acme.com.br: added, certificate mc-staging-acme-com-br issued in 94 s and bound; https://staging.acme.com.br is live.
        v0.6.2 deployed to staging; https://staging.acme.com.br/health answered 200 in 3 s.
```

Every later deploy, yours or the pipeline's, says `Domain staging.acme.com.br: certificate mc-staging-acme-com-br kept.` and nothing else changes. If Azure had not finished issuing within 10 minutes the run would end `deploy ok; certificate pending: the next deploy binds it` (exit 0), and the next deploy would bind it. A record that Acme later changes (a proxy put in front, say) makes the next deploy list that record with what was found, and the domain stays as it was: the plugin never undoes a deploy.

### 14.65 A question that cannot be missed

You build F-12 in a headless run, where there is no question tool. The report comes first, as plain text; the question is the last thing in the message, in a block of its own:

```
> /agile:build F-12
Claude: F-12 — Export the student list [approved]
        Worktree of F-12: D:\dev\wt\school\f-12-export-student-list [feature/F-12]
        Built: 3 files, 4 tests green (11 s). Stopped before the last step.

        > ❓ **Pergunta**
        > A exportação precisa de um pacote novo (CsvHelper 33.0.1, licença MS-PL). Posso adicionar?
        > a) Sim, adicionar o CsvHelper
        > b) Não, escrever o CSV à mão
        >
        > **Recomendação:** a) sim. **Motivo:** trata aspas e vírgulas dentro do campo, que o CSV à mão erraria.
```

You answer "a" and the build goes on. Where the session has the question tool, the same question is a card and the report above it is the same. Two questions in one message are two blocks, `**Pergunta 1**` and `**Pergunta 2**`; a question the brief and the code give no basis for says `**Sem recomendação:**` and why.

### 14.66 A lesson comes with a score

At the end of the ship of F-12 the retro proposes three lessons. Each one has two scores, `plugin` and `project`, with the reason for each number, and they come ordered by the larger score:

```
> /agile:ship F-12
Claude: ... Retro: 3 lessons, by importance.
1. Plugin note → plugin: agile [generic] — score plugin 8 / project 5
   plugin 8: a `grep` of the gate output hid the new warnings in F-12 and in F-9 (two cases);
   project 5: it would recur on the next items of this app.
   Line: ⏳ plugin: agile [generic] score 8/5 — save the whole gate output, never filter it.
2. Project rule → .claude/rules/agile/project.md — score plugin 1 / project 6
   plugin 1: this app's own habit; project 6: three of the next five items add an endpoint.
   Line: "A new endpoint gets its authorization test in the same commit."
3. Template tweak → not recommended — score plugin 2 / project 1
   plugin 2: the wording of one project; project 1: it cost a minute.
Approve?
> aprovo 1 e 2
Claude: Retro log: entry with the three lessons and their scores; the ⏳ row of lesson 1 added to
   "Plugin notes — status" with the score in its Note column (same five columns). Lesson 3 left out.
```

The plugin score of lesson 1 is 8 because it cites two cases. With only the one in F-12 and no second case it would stop at 6 and say `no evidence yet`: a guess does not become an 8. The scores recommend and never decide: you approved 1 and 2, and a `not recommended` lesson is shown, never hidden, so you can still pick it. The agile plugin session reads the score in the note when it takes the notes in, and nothing else reads it.

### 14.67 The bank has not sent its key yet

F-12 is `building` and waits on the bank's sandbox key. Rather than leave it holding the slot:

```
> /agile:change F-12 --block "waits on the bank's sandbox key"
Claude: F-12 — Bank sync [building]
        Who unblocks it? (the reason alone is not enough)
> the bank
Claude: F-12 is blocked: status: blocked, "Blocked (2026-10-06): waits on the bank's sandbox key — unblocked by the bank — returns to building",
        a line in ## Decisions, label `blocked` on the issue (the Status field stays In progress).
        Nothing was committed: the file stays in the worktree for the next commit; /agile:pause writes the wip commit of the half-done code.
```

The slot is free, so F-13 starts at once with `/agile:build F-13`. Typing `/agile:build F-12` meanwhile stops:

```
> /agile:build F-12
Claude: F-12 — Bank sync [blocked]
        F-12 is blocked (waits on the bank's sandbox key), unblocked by the bank: /agile:change F-12 --unblock
```

`/agile:status` lists it on its own line, `Blocked: F-12 Bank sync — waits on the bank's sandbox key — unblocked by the bank — returns to building`, apart from `Waits on:`, which is still what `## Start` says an item depends on. When the key arrives while F-13 is `building`:

```
> /agile:change F-12 --unblock
Claude: F-12 can go back to `building`, but F-13 is building in this checkout. Nothing changed.
        Finish F-13, or type /agile:change F-12 --unblock --worktree to run both in parallel.
```

Once the slot is free (or with `--worktree`), `--unblock` sets `status: building`, removes the `Blocked` line and the label and writes `2026-10-09: blocked (waits on the bank's sandbox key), unblocked (the key arrived)` to `## Decisions`. `/agile:change F-12 --block "later"` on an `idea`, a `done` item or one already blocked is refused with its status.

### 14.68 The conversation was compacted mid-build

F-13 is `building`. You stopped yesterday with `/agile:pause`, so the item file has a `## Paused` section. Today the session is long, and Claude Code compacts the conversation:

```
> /compact
(Claude Code compacts; the hook adds to what the summary keeps:)
Keep in the summary (agile@canary), word for word: the item in progress and its next step.
Resume F-13 Exam board registration — building [feature/F-13] D:\wt\examprep\f-13-exam-board
  Paused: 2026-10-07: stopped at the endpoint tests, wip commit 4e1c2aa
  Next: write the 409 test for a duplicate acronym
(the session continues, and its start adds:)
After a compact, resume here:
Resume F-13 Exam board registration — building [feature/F-13] D:\wt\examprep\f-13-exam-board
  Paused: 2026-10-07: stopped at the endpoint tests, wip commit 4e1c2aa
  Next: write the 409 test for a duplicate acronym
> continue
Claude: F-13 — Exam board registration [building]
        Writing the 409 test for a duplicate acronym, in D:\wt\examprep\f-13-exam-board.
```

Without a `## Paused` note the next step comes from the status: `continue /agile:build F-13 from the last commit on feature/F-13` for `building`, or `the owner runs the validation script of F-13, then /agile:ship F-13` for `validating`. A plain session start (not after a compaction or a resume) shows only the usual status lines.

### 14.69 Does staging's backup really restore?

Acme's staging has been running for three weeks, and Azure keeps its database backup for 7 days, but a backup is only a hope until someone restores it. You ask for the drill:

```
> /agile:publish staging --restore-drill
Claude: Restore drill staging (aca, rg-acme-staging): PostgreSQL postgres-xyz, Standard_B1ms, 32 GB, version 16, backup retention 7 days, earliest restore point 2026-09-29 14:02 UTC
        Would create postgres-xyz-drill-2610062015 (restore to now), billed per hour until removed; the drill removes it
        psql found: the data will be read
        > ❓ **Question**
        > Run the restore drill on staging now? (yes creates a PostgreSQL server billed per hour and removes it at the end)
        > a) Yes, run it
        > b) No, nothing is created
        >
        > **Recommendation:** a) yes. **Reason:** it is the only way to know the backup restores before the day it is needed.
You:    a
```

Typing the command did not start anything: it only showed the plan, because the drill creates a server that costs money while it exists. After your yes the command runs in the background (a restore takes minutes) and then reports:

```
Claude: Source postgres-xyz: PASSED
          backup retention 7 days: the earliest point you can restore to is 2026-09-29 14:02 UTC
          restored in 7 min 12 s as postgres-xyz-drill-2610062015
          shape: Ready, version 16, Standard_B1ms, 32 GB; databases app
          database app: public.orders 1204 rows, public.products 87 rows, public.users 12 rows
          removed postgres-xyz-drill-2610062015
        Restore drill staging: PASSED (restored in 7 min 12 s, data read)
```

The throwaway server is gone (Claude checks with `az postgres flexible-server list` that no `-drill-` server is left in the group), and what you read is evidence: the tables came back with rows. If `psql` is not on your PATH the run still restores, checks the shape and deletes, and the last line says so instead of passing quietly:

```
Claude: Restore drill staging: PASSED, data not read (psql not found on PATH)
```

If something fails, the server is removed all the same and the line says the backup is not proven, with Azure's own reason when it gave one:

```
Claude: Restore drill staging: FAILED: restore still running after 60 min; the backup is not proven
```

A parked staging is refused before anything is created (`postgres-xyz is Stopped: run /agile:publish staging --resume first`), and a drill that was interrupted last week leaves a `postgres-xyz-drill-...` server that the next plan lists (`Left over from an earlier drill: ..., billed per hour; the drill removes it first`) and the next run removes before it restores. If a delete itself fails, the report says so on its first line, with the command for you to run, because that server costs money until it is gone.

### 14.70 You pick the option Claude did not recommend

In the refinement round of F-14, question 2 recommends a) "Soft delete" and you answer b) "Hard delete". After the whole round is answered, Claude prints one table for that answer (the report is outside any question block):

```
Claude: Avaliação: Exclusão de aluno — recomendado: Soft delete, escolhido: Hard delete
        | Critério                        | Recomendado | Escolhido | Motivo |
        | esforço agora                   | 6           | 8         | hard delete é um DELETE; soft delete pede filtro em toda consulta |
        | custo ao longo do tempo         | 7           | 4         | sem histórico, cada pedido de auditoria vira restauração de backup |
        | risco                           | 8           | 3         | brief.md:41 exige o histórico de provas do aluno; apagar perde as provas |
        | reversibilidade                 | 9           | 1         | soft delete desfaz com um UPDATE; o DELETE só volta do backup (brief.md:41) |
        | Média                           | 7.5         | 4.0       | diferença 3.5: confirmação abaixo |

        [card] Exclusão de aluno: a diferença é 3.5
        Trocar para a recomendada (Recommended) — Soft delete
        Manter a escolha — Hard delete
```

You choose "Manter a escolha". The answer stands, and F-14's `## Decisions` gets one line:

```
2026-10-07: Exclusão de aluno — owner chose "Hard delete" over the recommended "Soft delete" (average 4.0 vs 7.5); kept — the school never needs the history
```

With a gap under 2.0 there is no card: the table ends with `Diferença abaixo de 2.0: a escolha fica.` A recommended answer, a question with `Sem recomendação`, a multi-select question and `--assume` print no table.

### 14.71 Claude gives you a command to run

`/agile:version` finds that the copies in the acme-shop project are older than the plugin, and the one step that is yours is a command in a terminal. Claude writes it in two blocks, PowerShell first and Git Bash second, one command per line, never as a line inside a sentence:

```
Claude: acme-shop: cópias da 0.24.0, plugin na 0.25.0. Atualize o plugin num terminal, abra uma sessão nova e rode /agile:sync.
```

```powershell
claude plugin marketplace update canary
claude plugin update agile@canary
```

```bash
claude plugin marketplace update canary
claude plugin update agile@canary
```

A step that needs a value of yours uses a placeholder such as `<subscription>`, or a made-up sample such as `F-12` in an example; the same rule holds for the manuals, the README and the templates copied into your project. What Claude itself runs, such as `git status` in the middle of a step, stays in its sentence: you do not copy from it.


### 14.72 The security scan: the first triage and a blocked ship

HR-Portal is an older project. At the end of F-21's build, before the validation script, Claude runs the scan's plan and then the scan.

```
Claude: Security scan: dotnet (native), gitleaks v8.30.1 (docker, image not present, 77 MB), semgrep 1.179.0 (docker, image not present, 1.56 GB)
        [card] Download the two images (1.6 GB, once)?
> yes
Claude: dotnet: 1 package with 1 advisory
        gitleaks: tree 0, history 214 commits read, 1 finding
        semgrep: 2 findings
        | id       | severity | where                          | rule                |
        | 3f9a12c0 | critical | src/Legacy/Mail.cs:14 @a41c9e2 | aws-access-token    |
        | 9be40d17 | high     | Newtonsoft.Json@9.0.1          | GHSA-5crp-9r3c-p9vr |
        | c27d5e88 | high     | src/Reports/Query.cs:12        | csharp-sqli         |
        | 70aa31f4 | medium   | src/Reports/Hash.cs:5          | warn-only           |
        Triaged: none
        agile scan BLOCKED: 1 critical, 2 high open
```

Nothing is accepted by itself: Claude asks about each finding in turn. For the secret, found only in a past commit, the card says deleting the line leaves it in every clone; you answer that the key was rotated this morning, and Claude records the row as fixed with the reason "rotated". The package is fixed like a bug: a test that reads the resolved version fails, the version moves, the rescan no longer lists it, the suite is green and the `reviewer` agent confirms the diff. The query goes to a card (fix now, accept with a reason, false positive with a reason) and you choose "fix now". At the ship the scan runs again:

```
Claude: dotnet: 0 packages with 0 advisories
        gitleaks: tree 0, history 216 commits read, 0 findings
        semgrep: 1 finding
        | 5d01c3aa | high | src/Reports/Export.cs:30 | insecure-binaryformatter-deserialization |
        Triaged: 3 fixed
        agile scan BLOCKED: 0 critical, 1 high open
        Ship stopped: a new high finding (src/Reports/Export.cs:30, never the value). [card] Fix it · Accept with a reason
```

Without Docker running, the same step ends `agile scan PARTIAL: gitleaks, semgrep did not run (docker daemon not running)` and a card asks whether to go on; the answer goes to the item's `## Decisions`.

### 14.73 Staging sleeps at night by itself

You are tired of typing `--park` on Friday evening and `--resume` on Monday morning. In Acme's `docs/infra.md` you write, under `## Schedule`:

```
| Environment | Action | When | Timezone |
|---|---|---|---|
| staging | park | 0 20 * * 1-5 | America/Sao_Paulo |
| staging | resume | 0 8 * * 1-5 | America/Sao_Paulo |
| staging | re-park | 0 3 * * * | America/Sao_Paulo |
```

Then, from the project root, you look at what the table renders and write it (the plugin's `sync.js`, in the folder of the installed version):

```powershell
node "$env:USERPROFILE\.claude\plugins\cache\canary\agile\<version>\scripts\sync.js" schedule
node "$env:USERPROFILE\.claude\plugins\cache\canary\agile\<version>\scripts\sync.js" schedule write
```

```bash
node ~/.claude/plugins/cache/canary/agile/<version>/scripts/sync.js schedule
node ~/.claude/plugins/cache/canary/agile/<version>/scripts/sync.js schedule write
```

and they print:

```
Schedule (docs/infra.md): 3 rows, 1 environment (staging)
.github/workflows/park.yml: missing (run node sync.js schedule write)
wrote .github/workflows/park.yml (staging: park 0 20 * * 1-5, resume 0 8 * * 1-5, re-park 0 3 * * *, America/Sao_Paulo) and .github/scripts/agile-park.js
Next: add main to the deployment branches of the GitHub environment staging (Settings > Environments); the tag rule v* stays for the deploy
```

You commit both files, add `main` to the branches of the GitHub environment `staging`, and that is all. At 20:00 on Friday the run page of GitHub shows (the plan first, then what happened; nothing was asked, the row was the yes):

```
## Parked staging
staging (rg-acme-staging): api minimum 1 -> 0; web already 0; postgres-xyz stopped (5 min 7 s). Azure starts postgres-xyz by itself on 2026-10-16 unless it is resumed first.
Azure: Acme's subscription Acme staging (2222...), tenant 1111...; removed from the environment: nothing.
Scheduled park (0 20 * * 1-5, America/Sao_Paulo). Plan:
- api: minimum 1 -> 0
- web: already 0
- postgres-xyz: Ready -> Stopped (mark agile-parked)
```

At 03:00 every night the `re-park` row looks at the server: on Friday week Azure has started it by itself after its 7 days, and the run says `PostgreSQL postgres-xyz re-parked (4 min 58 s); Azure starts it by itself on 2026-10-23 unless it is resumed first`. If you had resumed staging on Thursday for a late test, the mark was gone and the run says `postgres-xyz: Ready, no agile-parked mark; left as it is`. A time typed as `*/5` in the minute is refused at the table (`the minute "*/5" of "*/5 * * * *" must be a number from 0 to 59 (a list is allowed; a * or a step is not)`), and a deploy that is running at 20:00 makes the park wait for it. When a run cannot finish (a server that Azure still answers as busy after 5 minutes), it goes red with the resource and what it had changed; the next night's run, or `/agile:publish staging --park`, is safe to repeat.

### 14.74 The main branch is `develop`

Your project keeps its work on `develop`, and its `CLAUDE.md` says `- Main branch: `develop`.` (or the bootstrap's line, ``pushing or merging to `develop` ``). One reader, the same for the gate, the commit guard and `/agile:publish`, takes the name from those two lines only; a sentence such as "the main branch is `develop`" names nothing.

Everything is committed on `feature/F-6` and Claude runs the gate by hand. Before 0.29.1 it looked only for `main` or `master` and answered `agile gate SKIPPED: no code file changed since the last commit`. Now:

```
agile gate (by hand): 2 code file(s) changed since the main branch `develop`
...
agile gate GREEN
```

In a project whose `CLAUDE.md` names no branch the gate still goes on with `main` and says so first: ``main branch `main` assumed: CLAUDE.md names none``. `/agile:publish` does not guess, because it pushes a tag and a release:

```
CLAUDE.md names no main branch: add the line - Main branch: `<name>`
```

and, when the two lines disagree (`- Main branch: `develop`` and ``merging to `main` ``), `CLAUDE.md lines name different main branches (`develop` in the Main branch line, `main` in the merging to line): keep one`. You fix the line and type the command again.

### 14.75 The sync marks notes it never cited, and stops proposing what is not its to change

Toolbox's retro log has ⏳ notes the plugin delivered long ago, none of them citing an issue. From 0.29.2 the plugin carries a map from the text of a note to its issue, and the row says how each was found:

```
> /agile:sync
Claude: agile@canary 0.29.1 → 0.29.2.
| File / item                      | State   | Proposal                                                                 |
| docs/agile/workflow.md           | edited  | merge: the plugin changed the sync section; your edit is elsewhere         |
| Directory.Build.props            | same    | (the <Version> 0.4.0 is yours; the template's 0.1.0 is not proposed)      |
| plugin notes delivered           | 3 lines | mark ✅: line 13 #21 (mapped), line 15 #27 (mapped), line 16 #29 (mapped) |
| DocGen tool catalogue            | -       | not offered: docgen.json says "tools": false                              |
> ok
Claude: Merged docs/agile/workflow.md with the plugin text as the sync copies it: no conflicts, the header links still point at workflow.pt-BR.md.
Notes: marked line 13, 15 and 16, agile@canary 0.29.2.
Commit "chore: sync with agile@canary 0.29.2" on main. Authorize?
```

Before this, the hand merge brought `../pt-BR/MANUAL.md` back into the header, the sync proposed the template's `<Version>0.1.0</Version>` over your `0.4.0`, and the catalogue was offered again at every sync. A project that leaves `tools` out of `docgen.json` is still offered the catalogue, with the line that declines it: add `"tools": false` to `docgen.json`. A `docgen.json` that cannot be read is named in the report and nothing is offered until you fix it.

### 14.76 The screen comes back from the agent with its tests checked, not trusted

Toolbox's text-blocks screen replaces a component and changes what an old test asserts. The plan Claude wrote at the start of the build listed `SectionOrnament` as removed and budgeted one fix round. Claude removed it, with its two tests updated first, then called the `frontend` agent in the foreground.

```
Claude: Plan: 1 remove SectionOrnament (its 2 tests first); 2 hand the screen to frontend; 3 whole suite; 4 one fix round; 5 coverage; 6 validation script.
Agent report: Nothing here was built or run.
  Existing tests that assert what I changed: TextsPageTests.ShowsBlocks (Texts.Tests): assertion changed under AC3.
  Removals for the session: none.
  Assumptions I could not verify: the new block card keeps the data-testid "block-card".
Claude: node gate.js ship, before reading its files: 76 passed, 21 failed (2 patterns: the card testid, the empty-state text).
Claude: Read the 9 files it named; the first assumption was right (testid "block-card"), the second was not. Fixed both; suite again: 97 passed, 0 failed. agile gate GREEN.
```

The build report closes with one block:

```
Frontend hand-off: first suite 76 passed, 21 failed; after the fix round 97 passed. Removed: SectionOrnament (2 tests updated first). Assertions changed: TextsPageTests.ShowsBlocks (AC3).
```

Before this, the agent could be called in the background, so the gate built half-written files; a removed type was left as a comment-only stub; told only "do not edit existing tests", it left a required conversion undone; and the failures showed up only when the first test run came after reading everything.

### 14.77 The database password stops changing at every deploy

Acme's shop deployed staging twice with the old recipe and the second deploy left the app unable to sign in to its database. After the plugin update the sync names the cause; the sync changes nothing, because the AppHost is the project's code.

```
> /agile:sync
Claude: agile@canary 0.30.0 → 0.30.1.
Claude: Missing capability: Database password (since 0.30.1): the AppHost declares a database container with no password
   parameter, so every `aspire deploy` of a compose environment changes the password and the app cannot sign in from the
   second deploy on. src/Shop.AppHost/Program.cs declares AddPostgres("postgres") for the compose environment Staging.
   An environment already deployed keeps the password of its first deploy: use it as the value of Parameters__dbpassword
   if it is still known, otherwise the database volume must be dropped (its data is lost); the AppHost is not changed by the sync.
   Item to capture: a bug "The database password changes at every deploy". Nothing was written. Capture it?
You: yes
Claude: Captured as B-14, ready to refine.
```

The bug is built in the shop's repository from the profile's lines: `AddPostgres("postgres", password: ...)` with the secret parameter `dbpassword` in publish mode only, and the row `Parameters__dbpassword` in `## Expected secrets`. Acme's owner generates a value with the command pair of the recipe text above and keeps it in the GitHub environment `Staging`. Before the variable is set, the deploy stops by name:

```
> /agile:publish staging
Claude: the environment variable Parameters__dbpassword is not set: set it outside this chat (the value is never read, printed or asked for), then run the command again; nothing ran
```

With the variable set in the shell that started Claude, two deploys in a row leave the same container running and the same password signing in; the data created after the first is still there. The first password of the old environment was lost, so Acme dropped its staging volume once, by hand, and started again from an empty database.

### 14.78 A held folder stops the removal, and the report names who holds it

Acme's owner validated F-21 and typed the ship while a Visual Studio window still has the worktree open. The merge is done; the script that removes the worktree stops at its probe, before anything is removed:

```
> /agile:ship F-21
Claude: Merged feature/F-21 into main (--no-ff), pushed; main...origin/main 0 0.
Claude: The worktree was not removed. scripts/worktree.js stopped at the probe, nothing renamed or removed:
   Cannot rename D:\wt\shop\f-21-order-export (EBUSY): something holds the folder.
   Holders (best effort):
     process 18244 devenv.exe: "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe" D:\wt\shop\f-21-order-export\Shop.slnx
     A .vs folder is inside the worktree: Visual Studio may hold it.
   agile worktree HELD: D:\wt\shop\f-21-order-export
   Close Visual Studio on that folder and tell me; I run the same command again.
> closed
Claude: Probe passed; worktree removed and pruned (REMOVED). Branch deleted locally (remote: never pushed).
```

When the report finds nobody, it says so and names the tool that can: a program that only has the folder as its working directory (a terminal `cd`'d into it) is seen by open handle only, and `handle.exe` from Sysinternals finds it once you ran it by hand and accepted its licence; the plugin never accepts a licence for you.

A removal that failed halfway before 0.30.2 leaves an orphan, a folder git no longer lists. The script reports it and deletes nothing; the folder is removed only after your yes:

```
> /agile:ship F-22
Claude: Merged feature/F-22 into main. Folder D:\wt\shop\f-22-invoice-pdf is not a registered worktree but is still on disk
   (an earlier removal failed halfway). Probe passed. 3 top-level entries, 41 files: src, tests, Shop.slnx.
   agile worktree ORPHAN: D:\wt\shop\f-22-invoice-pdf
   Nothing was deleted. It holds only what was merged. Delete the folder?
> yes
Claude: Folder deleted (REMOVED). Branch deleted locally (remote: never pushed).
```

### 14.79 The gate waits its turn and keeps what failed

Simulab's session starts `gate.js ship` for F-28 and, while the suite runs, the turn ends. The Stop hook meets the lock the ship holds and does not build beside it:

```
[agile gate]
agile gate deferred: ship (pid 4812) holds D:\dev\wt\simulab\f-28-exam-timer since 2026-10-08T14:02:11.402Z; if that pid is not a gate run, delete D:\dev\simulab\.git\worktrees\f-28-exam-timer\agile-gate.lock; the marked files build on the next turn
```

The ship ends red. The report keeps its last lines, names the failing tests and, because the branch changed only `Simulab.Exams`, says where the failure is not:

```
tests failed: Simulab.slnx
  Failed Fails_when_the_vocabulary_is_missing [12 ms]
  ...
every failing test is in tests/Simulab.Reports.Tests/Simulab.Reports.Tests.csproj, which reaches none of the projects this branch changed (src/Simulab.Exams/Simulab.Exams.csproj): probably not this item's; run it on the main branch to confirm
full output: C:\Users\me\AppData\Local\Temp\agile-canary\gate-logs\20261008-140433-210-ship.log
agile gate RED: tests failed (Simulab.slnx): Fails_when_the_vocabulary_is_missing — full output: C:\Users\me\AppData\Local\Temp\agile-canary\gate-logs\20261008-140433-210-ship.log
Claude: The failing test is in Reports, which F-28 did not touch. I ran it on main: it fails there too, so it is not this item's. But the ship needs a green run and this one is red, so I am stopping here: fix Reports first (a small change on its own item), or tell me to wait for whoever owns it?
```

The file has the message and the stack of every failing test, so a test that fails once and passes on the next run still leaves its trace. Before this, the ship in the background and the Stop hook built the same `obj/` folders and one of them failed with `CS2012`, the hint blamed an app host that was not running, and a failure in another project looked like the item's own.

In another turn Claude edits `docs/glossary.md` for F-29: the test project that reads `glossary.md` runs at the end of that turn; a note it adds to `docs/notes.md`, which no test names, runs nothing and prints nothing. A change made outside Claude's editing tools is seen by `gate.js stop` run by hand, which asks git, and by the ship.

### 14.80 A staging that deploys without Aspire

Acme's shop is developed with Aspire for the dashboard, but its staging host should run only the shop's images and a compose file the owner can read. In round 6 of the bootstrap, after the cloud account of each environment (`none`, a host of Acme's own):

```
Claude: Question 25c, staging (compose on your own host): with or without Aspire?
        a) with Aspire: aspire deploy, the AppHost describes the environment
        b) without Aspire: the shop's own images and deploy/compose.yaml; the AppHost stays a local tool
        Recommended: a) with Aspire. Reason: it is today's behavior and needs nothing else written.
You: b.
Claude: staging deploys without Aspire. Written from the profile's "Deploy recipe (containers without Aspire)":
   docs/infra.md  Deploy command: dotnet publish src/Shop.Web/Shop.Web.csproj -c Release -t:PublishContainer && docker compose -f deploy/compose.yaml --env-file deploy/staging.env up -d --wait
                  Expected secrets: DB_PASSWORD (environment variable)
   deploy/compose.yaml   postgres:17 with a data volume, the web service on shop-web:${AGILE_VERSION}, a keys volume
   deploy/staging.env    ASPNETCORE_ENVIRONMENT=Staging, HOST_PORT=5081 (the port of the URL)
   src/Shop.Web/Shop.Web.csproj   ContainerRepository shop-web, ContainerImageTag $(Version)
   The AppHost is not changed.
```

You generate a random `DB_PASSWORD` (22 or more letters and digits) and keep it in the shell that starts Claude. Before it is set, the command stops and nothing runs:

```
> /agile:publish staging
Claude: the environment variable DB_PASSWORD is not set: set it outside this chat (the value is never read, printed or asked for), then run the command again; nothing ran
```

With the variable set, the first deploy publishes `shop-web:0.1.0` and starts both containers. You open the site, sign in, and keep the cookie. The release `v0.2.0` deploys the same way, with the same password:

```
> /agile:publish staging
Claude: staging: v0.2.0 deployed · previous v0.1.0 · check http://localhost:5081/health 200 in 0.3 s
```

The web container is now `shop-web:0.2.0`, the database container kept its data and its password, and the cookie from `v0.1.0` is still accepted, because the keys volume was not touched. `docker compose logs` on the web service shows the request log; there is no dashboard container. Production, with its own `deploy/production.env` and port, runs beside staging on the same host with its own containers and volumes.

### 14.81 A database that keeps its address, its password and its data

Acme's shop is bootstrapped with Aspire and PostgreSQL. In round 1, right after question 3, Claude asks:

```
Claude: Question 3a, fixed local services: the database keeps its port, its password and its data from one aspire run to the next?
        a) yes: port 5432, password in the AppHost's user secrets, a data volume
        b) no: the AppHost as the recipes write it (a new port on every run)
        Recommended: a) yes. Reason: a DataGrip connection you save once keeps working, and the data survives a restart.
        Port 5432 is free on this machine.
You: ok.
```

With the skeleton written, the report says what was done, never the password:

```
Claude: Fixed local services, from the profile's "Local services (fixed ports and passwords)":
   Acme.AppHost/Program.cs   postgres on port 5432 outside publish mode, password parameter postgres-password, WithDataVolume()
   user secrets of the AppHost   Parameters:postgres-password set (random, 35 characters; its value is not shown)
   docs/infra.md   ## Local services: postgres · 5432 · Parameters:postgres-password · the volume ending in -postgres-data
   CLAUDE.md   - Local services: fixed
   Deployed environments are unchanged.
```

You read the password once, on this machine, and save the DataGrip connection with host `localhost`, port 5432 and the user `postgres`:

```powershell
dotnet user-secrets list --project src/Acme.AppHost
```

```bash
dotnet user-secrets list --project src/Acme.AppHost
```

The next day `aspire run` starts the database on the same port, with the same password and yesterday's rows. If you want another password, change the secret and remove the volume named in `docs/infra.md` (the data in it is lost), or the database refuses the new one with `password authentication failed`. A second machine, or a fresh clone, sets its own value with `dotnet user-secrets set` before the first `aspire run`; until then the resources that use the password stay `Waiting`.

A project bootstrapped last month has an AppHost and no such line, so `/agile:sync` lists it:

```
Claude: New in this version: Fixed local services (- Local services: fixed | none in CLAUDE.md).
        Item to capture: "Fixed local services", which builds the profile's "Local services (fixed ports and passwords)"; "- Local services: none" declines it and stops this note.
```

### 14.82 The item's own data, and an agent kept in the worktree

F-12 adds a `Discount` column to Acme's orders, built in its worktree `D:\wt\shop\f-12-order-discount` while the main checkout stays on `main`. The first line of the validation script depends on where the project keeps its local data. Three projects, three start steps.

The AppHost declares `WithDataVolume()` with no name (the profiles write it that way): the volume name carries the AppHost's path, so the worktree already has its own database.

```
1. Start the app from the worktree: aspire run in D:\wt\shop\f-12-order-discount.
   Data: this worktree has its own volume (the AppHost's data volume has no name); nothing to set.
   The migration of F-12 lands only there; the main checkout's database is untouched.
```

The project names its volume and keeps its own knob in `docs/infra.md` (`Database__Name`):

```
1. Stop any app host you have running, then start the app from the worktree on a database of its own:
```

```powershell
$env:Database__Name = "shop_f12"
aspire run
```

```bash
export Database__Name="shop_f12"
aspire run
```

```
   Expected: the dashboard lists the database shop_f12. Repeat: the same two lines in a new terminal.
```

The project has neither:

```
1. Start the app from the worktree: aspire run.
   Warning: the local database is shared with the main checkout; the migration of F-12 lands there
   (recorded in ## Decisions with your yes of today).
```

A step that asks you to change code says where and what fails, and a step that compiles stops the host first:

```
5. Stop the app host (Ctrl+C in its terminal). In src/Shop/Orders/Order.cs, inside the class Order, after the
   property Total, add: public decimal Discount { get; init; } = -1m;
   Run the tests: OrderTests.Discount_cannot_be_negative fails with "Discount must be zero or more".
   Remove the line again.
```

During the same build, the `reviewer` agent once searched with no folder. The guard sent it back:

```
[agile agent-guard] Grep from the `reviewer` agent targets the main checkout (D:/dev/shop), not an item's worktree.
The item's code and status live in its worktree: D:/wt/shop/f-12-order-discount [feature/F-12].
Repeat the call with the item's worktree (or a folder inside it) as the path; never report something as missing from a search rooted elsewhere.
Bypass, only with the owner's yes: restart Claude Code with AGILE_HOOKS=off (this turns off every agile hook).
```

The agent repeated the search in the worktree and found `Order.Discount`; you see only its findings.
### 14.83 Which criteria still have no test

F-14 is `building` and has five acceptance criteria. When the build starts, Claude adds the section to the feature file with every row `pending`, and each commit that adds a test fills its row. Halfway, the file says:

```
## Criterion → test
| Criterion | Test |
| --- | --- |
| AC1 | ExamFeesPage_Open_ShowsFees |
| AC2 | pending |
| AC3 | CreateExamFee_Negative_Returns422 |
| AC4 | pending |
| AC5 | validation step 6 |
```

The conversation is compacted. The summary keeps the resume block, which now has a fourth line:

```
Resume F-14 Exam fees — building [feature/F-14] D:/wt/examprep/f-14-exam-fees
  Paused: no pause note
  Next: continue `/agile:build F-14` from the last commit on feature/F-14 (git log in D:/wt/examprep/f-14-exam-fees)
  Criteria with no test: AC2, AC4 (2 of 5)
```

The session that continues goes straight to AC2 and AC4 without rereading the code. The line says `none (5)` when every row is filled, and `no section` for a bug or for an item built before the section existed.

Then you change a criterion mid-build:

```
> /agile:change F-14 AC3 must also refuse a fee of zero
Claude: AC3 changes (Given a negative fee, then 422 becomes Given a fee below one, then 422).
        Its row in ## Criterion → test goes back to pending; AC1, AC2, AC4 and AC5 are untouched.
```

At the end of the build every row is filled, and on the day you ship, Claude checks the file instead of trusting the report. This ship has a gap:

```
> /agile:ship F-14
Claude: F-14 — Exam fees [validating]
        Criterion → test: stopped before the merge.
        AC4 is still pending; AC2 names ExamFeeTests.Refund_Partial, which is in no test project.
        Add the tests, or tell me to move a criterion to the validation script (validation step <n>, recorded in ## Decisions).
```

An older feature without the section prints `Criterion → test: no section (built before this rule)` and the ship goes on; a bug prints nothing.

### 14.84 A test run that hangs no longer leaves its tool running

The `legacy-shop` repository is `engine: msbuild`, and its `testCommand` runs a suite through `vstest.console.exe`. One day the suite hangs. At the end of the ship the gate has waited its 1800 seconds:

```
> /agile:ship F-31
Claude: F-31 — Refund by e-mail [validating]
        agile gate RED: tests timed out (vstest.console.exe LegacyShop.Tests.dll) — full output: C:/Users/you/AppData/Local/Temp/agile-canary/gate-logs/20261009-1815-ship.log
        tests ran longer than 1800 s (AGILE_TESTCMD_TIMEOUT): vstest.console.exe LegacyShop.Tests.dll
```

Before 0.37.0 only the shell was stopped here: `vstest.console.exe` kept running, and when you fixed the suite and shipped again the worktree removal failed with the folder in use. Now the gate ended the whole tree before it printed the verdict, so the folder is free and the next ship starts clean. If Windows had refused to end it, the same RED would carry one more line:

```
        a process the command started may still be running
```

That line is the cue to look in the Task Manager for a leftover `vstest.console.exe` or `testhost.exe` before shipping again.

### 14.85 A deploy that hangs is stopped with everything it started

The `staging` row of `docs/infra.md` deploys with `dotnet publish && az containerapp up ...`. One day `az` hangs on a login prompt. Thirty minutes later:

```
> /agile:publish staging v0.9.0
Claude: deploy failed
        the deploy command ran longer than 1800 s (AGILE_DEPLOY_TIMEOUT) and was stopped: staging may be half deployed, check it before running again
        Output saved: C:/Users/you/AppData/Local/Temp/agile-deploy/staging-v0.9.0-2026-10-09T18-42-11-004Z.log
        Nothing was recorded and nothing was rolled back. To go back to the version recorded before: /agile:publish staging v0.8.0
```

Before 0.38.0 only the shell was stopped: `az` kept running, held the `deploy` worktree, and could finish the deploy after the run had said it failed. Now every process the command started was ended before this message; the folder is free and the next run starts clean. Stopping the local processes does not undo what Azure had already accepted, which is why the message tells you to look at the environment first. If Windows had refused to end the tree, one more line follows:

```
        a process the command started may still be running
```

That line is the cue to look in the Task Manager for a leftover `az.cmd` or `dotnet.exe`. The limit is 30 minutes unless `AGILE_DEPLOY_TIMEOUT` (seconds) says otherwise; the budget of an `aca` environment is still written after a timeout, as after any failed deploy.

### 14.86 The manual and its worked examples are two files

From 0.39.0 the manual keeps the rules and the reference, and the worked examples (this file) sit beside it. Nothing in the examples changed: the same numbers, the same text. In the manual, "example 14.54" is plain text and means the example with that number here. The manual is about half of its old size, so what you open to check a rule is shorter.

A project that updates the plugin sees the split at its next `/agile:sync`:

```
> /agile:sync
Claude: agile@canary 0.38.0 → 0.39.0.
| File                                    | State  | Proposal                                                   |
| docs/agile/workflow.md                  | update | copy: the manual, now without section 14                   |
| docs/agile/workflow.pt-BR.md            | update | copy: the same, in Portuguese                              |
| docs/agile/workflow-examples.md         | new    | copy: the worked examples (14.1 to 14.86), linked from the manual |
| docs/agile/workflow-examples.pt-BR.md   | new    | copy: the same, in Portuguese                              |
Copy all four?
> sim
Claude: Copied 4 files. The links between the four files point at each other inside docs/agile.
```

If you had added your own paragraph to your `workflow.md`, the row would say `edited` instead of `update`, and Claude would show the three-way merge: your paragraph stays, section 14 goes away with the rest of the plugin's change. A new example is added at the end of this file, in both languages, and the next free number is the one `plugin-version.js ship` prints.

### 14.87 The build stops before it starts on a promise nobody kept

F-12 "Monthly report screen" was approved with `## Start` saying it waits on the wireframes the owner would draw. Nobody saved them yet:

```
> /agile:build F-12
Claude: F-12 — Monthly report screen [approved]
        Worktree of F-12: D:/wt/acme-shop/f-12-monthly-report [feature/F-12]
        F-12 cannot start: "Waits on (to start)" names docs/specs/F-12-wireframes.md, and the worktree has no such file.
        Who resolves it: you, by saving the wireframes there. Status stays approved; nothing was written.
```

Before 0.40.0 the build set `building` and started; F-12's screen criterion would only have come back open at the ship. The same stop names an item of "Depends on" that is not `done` yet, with its status.

F-14 "Download the report as CSV" was refined while F-13 was still building, and its BR1 says F-13 adds `ExportService.ToCsv`. F-13 shipped since, with JSON only:

```
> /agile:build F-14
Claude: F-14 — Download the report as CSV [approved]
        Worktree of F-14: D:/wt/acme-shop/f-14-csv-download [feature/F-14]
        F-14's premise is false today: BR1 says F-13 adds ExportService.ToCsv, and src/Export.cs (F-13, done) has only ToJson.
        AC1 cannot be met as written. Nothing was written; status stays approved.
        Decide how BR1 changes with /agile:change F-14
```

The build does not choose the fix for you (write the method in F-14, or wait for another item): that is a change note, your decision, and the build starts again after it. "Needed to validate" never stops a build, and a build resumed from `building` skips this check.

### 14.88 A desktop screen is checked in its real window, without clicking by coordinates

A `desktop` item with an Avalonia head reaches the end of its build. The browser pane cannot drive a desktop app, so step 12 does not use it:

```
> /agile:build F-12
Claude: Screen check (desktop, Windows session): starting Toolbox.Desktop from the worktree, reading the window through UI Automation.
        Window "Connections": filters present, DataGrid has 0 children while the footer says "3 of 3 connections".
        Found: the DataGrid theme is not included (avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml), the grid drew black.
        Fixed, and added a headless test that reads the grid's rows. Dark theme: switched through the Settings button (UIA Invoke), same result.
        Not reached by this check, so it goes to your validation script: tab order, the arrows and Esc inside the row menu.
        Process stopped. No mouse or key was sent, and nothing was captured by screen coordinates.
```

The check may press a button through the app's own automation tree, because that does not depend on which window has focus. It never sends a synthetic mouse or key: that would land in whatever window is in front, usually your IDE. On Linux or macOS, or with no desktop session, the report says the whole screen goes to your validation script.

A keyboard criterion (Enter, Space, Alt+Arrow) on an Avalonia screen is written as a headless test, not left to you; and every component that shows user or Core text gets a long-text state: in the gallery and in a wrap test.

### 14.89 The ship opens a pull request and you merge it yourself

The `CLAUDE.md` of acme-shop has `- Merge: pr`, because its main branch is protected. F-12 is validated:

```
> validado
> /agile:ship F-12
Claude: F-12 — Export the monthly report as CSV [validating]
        Full check: agile gate GREEN (212 passed, 0 failed, 1 m 48 s). Scan GREEN. Manual updated. Version 0.7.0 -> 0.8.0.
        origin is github.com and merge commits are allowed. ## Delivery and status: done are in the branch.
        Pushed feature/F-12. PR #31 opened: body = summary, validation script as a checklist, "Validated by the owner on 2026-10-09 (chat)",
        the criterion -> test table, the gate's last line, Closes #12. Recorded "PR: #31" in the item file and pushed again.
        PR #31 open: merge it on GitHub when you are ready
```

Typing ship authorized the pull request only. You read the diff on your phone and press "Create a merge commit". Another item can be built meanwhile: the next session start says

```
Waiting on PR: F-12 #31 MERGED — close not run: /agile:ship F-12
```

```
> /agile:ship F-12
Claude: F-12 — Export the monthly report as CSV [done]
        PR #31 is MERGED (merge commit 4e1c0aa). Main checkout: pull --ff-only, 0 0 against the remote, the merge commit is not empty.
        Remote branch: already deleted by GitHub. Worktree D:/wt/acme-shop/f-12-csv-export removed, local branch deleted.
        Board #12 closed, Status Done (read back). Nothing was committed on main: done and Delivery came with the PR.
        Retro: 1 lesson proposed.
```

Had F-13 shipped to main while PR #31 waited, and both bumped the version, GitHub would show the PR in conflict ("dirty") and you could not merge it; the second `/agile:ship F-12` would then merge main into the branch, run the gate again, bump the version from main's, push, and say `PR #31 open` again, with the same number. Had you closed the PR without merging, the item would go back to `validating` and the `PR:` line would be removed. GitHub does not let you approve your own PR: if the branch rule demands an approval, the admin bypass, a second account or a bot is yours to arrange.

### 14.90 A secret is stopped at the door, before it leaves the session

In acme-shop, Claude finishes F-14 (a connection to the reporting database) and commits it in one command, the way it always does:

```
Claude: git add src/Acme.Reports/appsettings.json && git commit -m "feat(F-14): connect to the reporting database"
        [agile secret-guard] Possible secret in the commit: src/Acme.Reports/appsettings.json:7 (password).
        Remove it, or name the setting instead (${DB_PASSWORD}, <password>); for fake test data put agile:allow-secret in a comment on that line. The value is not shown here.
        If this is a mistake, ask the owner; with a yes, repeat the command ending with the comment  # agile:secret-ok
Claude: The commit was refused: line 7 holds the real password of the reporting database. Nothing was committed.
        I moved it to the user secrets (ConnectionStrings:Reporting) and left `Password=<password>` in the file. Committing again.
```

The guard reads what the command is about to commit, even when `git add` and `git commit` are one command and nothing is staged yet. It names the file, the line and the kind of secret, and never prints the value. The same door guards `git push` (the commits the remote does not have yet) and the text of `gh issue|pr create|edit|comment`, `gh release create|edit` and `az boards work-item create|update`, inline or from a file:

```
Claude: gh issue create --title "Login fails" --body-file body.md
        [agile secret-guard] Possible secret in the body of gh issue create: body.md:3 (GitHub token).
Claude: The report quotes a token from the log. I replaced it with `<token>` and created the issue.
```

A test fixture that needs a fake key carries `agile:allow-secret` in a comment on that very line:

```
var fakeKey = "AKIA<the fake key>"; // agile:allow-secret
```

The marker stays in the code, so the reviewer sees every exception in the diff. Placeholders (`<password>`, `${DB_PASSWORD}`, `changeme`) pass on their own; `var password = request.Password;` is code that reads a password, not a secret, and passes too. If the secret is already in a commit that was not pushed, the push is refused and the message says that rewriting that commit needs your yes: nothing is force-pushed. The guard is a cheap check at each door; the scan before the ship (gitleaks and semgrep) still reads the whole project and its history. Only agile projects are guarded, and any doubt (a diff over 4 MB, a command it cannot read) lets the call through.


### 14.91 A client's ticket id is the item id

acme-shop fixes bugs that arrive as Jira tickets. You want the ticket, the branch and the worktree to share one name:

```
> /agile:idea bug --ticket PRJ-123 --url https://acme.atlassian.net/browse/PRJ-123 "Crash when saving an order with no lines"
Claude: No similar item (31 items read).
        Card: which epic? Orders (Recommended) / New epic / No epic
> Orders
Claude: PRJ-123 — Crash when saving an order with no lines [idea]
        Created docs/bugs/PRJ-123-crash-saving-order.md with ticket-url, board #44. No B-<n> number was taken.
```

The same ticket cannot become a second item, and a wrong id writes nothing:

```
> /agile:idea bug --ticket PRJ-123 "Crash on an empty order, again"
Claude: PRJ-123 is already an item: PRJ-123 — Crash when saving an order with no lines [idea]. Nothing was written.
        New work on the same ticket is /agile:change PRJ-123; another ticket has its own id.
> /agile:idea bug --ticket B-12 "Wrong total"
Claude: B-12 is not a ticket id: a ticket id is a key of two or more characters (a letter, then letters, digits or _),
        a hyphen and digits, like PRJ-123 or TFS-4521. Nothing was written.
```

From here every step carries the ticket's name:

```
> /agile:refine PRJ-123
Claude: PRJ-123 — Crash when saving an order with no lines [idea]
        Worktree: D:/wt/acme-shop/b-prj-123-crash-saving-order, branch bug/PRJ-123
```

Commits read `fix(PRJ-123): reject an order with no lines`, the merge message names `bug/PRJ-123`, and the release notes list `Bug PRJ-123`. Items without `--ticket` (F-12, B-3) are numbered and work as before.

### 14.92 A project far behind is told how far, and what arrived is read in blocks

acme-shop last synced when the plugin was 0.28.0; the plugin is now 0.46.0. The sync still goes straight to the current version (it compares each file with today's text, never version by version), but past 2 minor versions the report says how far, and groups what has a version by the minor series it arrived in:

```
> /agile:sync
Claude: 0.28.0 -> 0.46.0: 18 minor versions behind; the sync goes straight to 0.46.0. What arrived, by version:

        0.30 (1 capability, 0 notes)
          - Database password: the AppHost declares a database with no password parameter ...
        0.35 (0 capabilities, 1 note)
          - retro-log line 14: "gate.js reads stdin ..." (#25), delivered in 0.35.0
        Before your recorded version 0.28.0 (still pending)
          - DocGen: technical documentation generated from the code ...

        Files, one table by state (nothing dates a file, so these are not grouped):
        | file                              | state  | proposal |
        | .claude/rules/agile/git.md        | update | copy: ... |
        | docs/agile/workflow.md            | edited | merge: ... |
        Approve the rows, one by one or "ok" for all.
```

A project 2 minor versions behind or less, or whose `.claude/agile/sync.json` records no version, gets exactly the report it got before: no warning, no blocks. The approval does not change: one OK per row, not one per block.

### 14.93 The tests come first and are locked while the item builds

F-12 of acme-shop, an order total, is approved. The build does not write the tests along with the code: it runs a test phase, locks the tests at a commit, and only then writes the code.

```
> /agile:build F-12
Claude: F-12 — Order total [approved]
        Worktree of F-12: D:/wt/acme-shop/f-12-order-total [feature/F-12]
        Test phase: 3 tests for AC1-AC3, and the skeleton of Order (signatures that throw NotImplementedException).
        Tests: Failed: 3, Passed: 0 — each fails at Order.Add or Order.Total, in the skeleton; none is a compile error.
        test(F-12): order total acceptance tests committed.
        Tests locked: 9f89899c0d1e... (1 file); unlocks: 0
        The line is in docs/features/F-12-order-total.md, under ## Criterion → test.
```

From then on a hook refuses every write of that file, from the edit tools and from the shell, and a history rewrite (a rebase, an amend, a hard reset) on the item's branch:

```
Claude: [agile test-lock] this command writes tests/Orders.Tests/OrderTests.cs, locked by the acceptance
        tests of F-12 (commit 9f89899): the code is made to satisfy them, never the reverse.
        If the test is wrong, stop and ask the owner.
```

If the test really is wrong, the build stops and asks you; the session never unlocks alone:

```
Claude: ❓ A locked test looks wrong
        AC2 expects the total without tax, and the rule BR2 says it includes it.
        a) Unlock and rewrite the test
        b) Keep the test and fix the code
        Recommendation: a). Reason: the rule in the file says the total includes tax; the test contradicts it.
> a
Claude: Tests unlocked: F-12 (AC2 contradicted BR2; owner chose to rewrite); unlocks: 1.
        Rewritten, committed, locked again: Tests locked: 3b7c21aa4e90... (1 file); unlocks: 1
        Written in ## Decisions: tests unlocked (AC2 contradicted BR2).
```

At the ship, the lock is checked again: the files equal the lock commit, and every unlock has its line in `## Decisions`. A changed file, an unlock with no decision, or a lock file that was lost stops the ship like a red build:

```
> /agile:ship F-12
Claude: Tests lock: 3b7c21aa4e90... (1 files) unchanged — F-12; unlocks: 1
          unlock: 2026-10-10T14:02:11Z AC2 contradicted BR2; owner chose to rewrite
```

The tests of a screen that the `frontend` agent writes with the approved mockup are not locked. A project that reaches the ship with no lock (an item built before this rule) prints `Tests lock: none (built before this rule)` and goes on.
