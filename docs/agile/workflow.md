# agile@canary — Manual (en)

> Version 0.37.0 (draft). Português: [pt-BR](workflow.pt-BR.md).

Contents
1. Concepts in two minutes
2. Roles
3. Setting up a project
4. Bootstrap quiz
5. Feature lifecycle
6. Changing your mind
7. The feature file
8. Definition of done
9. Quality gates
10. Board
11. Languages and the app manual
12. Architecture profiles
13. Sessions and pauses
14. Worked examples
15. Quick reference
16. Command flows
17. Glossary

---

## 1. Concepts in two minutes

- **The chat is where ideas mature.** You and Claude talk about epics, features and use cases until they are clear. Nothing is coded while a feature is still being discussed.
- **The disk is the source of truth.** Every decision that matters ends up in a file with a `status:` header. The chat can be lost; the files cannot.
- **One feature at a time.** Only one feature is in `building` or `validating` at any moment; a `blocked` item holds no slot.
- **Three human gates.** A feature is **approved** before code, **validated on screen** before merge, and **merged** only with your go-ahead.
- **Simple code.** The architecture profile chosen at bootstrap decides how much structure a module gets. CRUD stays a thin vertical slice.
- **Fast feedback.** Hooks build and test only what changed. The full suite runs once, when shipping.

## 2. Roles

| You (product owner) | Claude (engineer) |
|---|---|
| Write the product brief | Runs the bootstrap quiz and records the decisions |
| Propose epics and features in the chat | Registers them on the board, unrefined |
| Answer refinement questions and approve features | Asks **all** questions in one round, each with a recommendation, after checking the code |
| Validate each feature on screen | Implements, tests and hands over a validation script |
| Authorize merges (typing `/agile:ship <id>` is the authorization) | Merges, removes the branch and worktree, updates the board and the app manual, runs the retro |

Subagents are the exception: a fresh-context reviewer for risky changes, two read-only passes before the code of a heavier item (`system-design` proposes the slice, the contracts and the data; `architect` reviews that proposal), two producing roles for screens (`ux-designer` designs one, `frontend` implements the mockup you approved), or parallel work that does not touch the same files. They are named jobs, not a chain of roles per feature, and none of them talks to you: the conversation is always with Claude, which reads what an agent wrote or proposed before following it. One writer at a time in a folder, and the two passes write nothing at all. Every agent works in the item's worktree: Claude's brief gives it the worktree as an absolute path, every other path absolute inside it, and the list `Other items in progress: <id> → <worktree path>` taken from `git worktree list`; the agent passes the worktree as the folder of every search, reads another item's status only from that item's own worktree (or says it is unknown), and the `reviewer` reads the code around the diff there too. A search left in the main checkout once reported types committed on the item branch as missing; now a hook refuses it (section 9).

**Models.** The model is chosen per activity, on purpose: the independent review, the design and architecture passes and the screen design run on the strongest model, code searches on the smallest, screen implementation and bulk mechanical work that the tests verify on a mid model, and the main session on the model you pick. Each agent (`reviewer`, `system-design`, `architect`, `ux-designer`, `frontend`) declares its default model; your project overrides it in the "Models" section of `CLAUDE.md` (quiz question 34), never by editing the plugin.

## 3. Setting up a project

1. Install the plugin:
   ```
   /plugin marketplace add <path to your agile-canary clone>
   /plugin install agile@canary
   ```
2. Create an empty repository and write `product/brief.md` using the brief template: problem, users, main capabilities, constraints, what is out of scope.
3. Run `/agile:bootstrap`.

## 4. Bootstrap quiz

`/agile:bootstrap` reads `product/brief.md` and asks questions in **rounds by topic**. For every question Claude gives a recommendation based on the brief and challenges answers that conflict with it. Decisions are made together; nothing is assumed.

Before anything is written, Claude checks that the skill it loaded and the installed plugin are the same version. When they differ it says so and copies from the installed one: otherwise the project records where its copies came from incorrectly, and the next `/agile:sync` compares against the wrong text.

| Round | Topics |
|---|---|
| 1. Shape | Type of app (web, mobile, desktop, API), architecture profile, the desktop technology (WinUI 3, Avalonia or MAUI) and how a desktop client reaches its data, **where a mobile app's backend lives** (question 2d), the **sections of a site** (question 2c, profile `website`), **a mobile app from day one beside a site** (question 2e), **online or offline-first for a mobile app** (question 2f), **a staff backoffice for a mobile app** (question 2g), deployment target |
| 2. Data | Database (the house engine when there is an existing system next door), multi-tenancy, deletion, auditing, personal data and retention |
| 3. Access | Authentication (for `web-api`, question 10a: how the API's consumers authenticate), RBAC, the identity bridge to a neighbouring system's users, entitlements/plans (usage limits, trials, time-bound grants, promo codes), admin back office |
| 4. Integration | Messaging (none, in-process, broker), external services, file storage, and — when a feature calls a model — the LLM provider and where it runs, its cost ceiling and how it is faked in tests |
| 5. Experience | UI stack, languages (default pt-BR, pt-PT, en), accessibility, the **visual identity** (23a: a file, a website, an image or three basic questions), the design system library, icon family, how an item is edited — or, for a conversational UI, the language it answers in, how a proposal is corrected and a states gallery. The UI kit and gallery are no longer a question: an app with screens always gets one, built from the identity. Profile `web-api` has no screens: it skips the UI stack, the language switch (21), accessibility (22) and everything about the visual identity and the UI, and question 20 covers only the text the API sends to people |
| 6. Operations | Observability, hosting, CI, board (GitHub or Azure), environments, the client's cloud account of each non-local one (question 25a) and, for an Azure one, its recipe and, for `aca`, its monthly ceiling (question 25b), for a compose environment of an app with an AppHost whether it deploys with or without Aspire (question 25c), for a desktop app where its installed copies update from (question 26b) and, from a share, whether every merge goes to testers as a beta (question 26c), the house conventions when the code lives beside an existing system, and — always — where the item worktrees live (`D:\wt\<repository>`, or `C:\` without a D: drive) |
| 7. Quality | Test budget per level, coverage expectations, architecture tests, models per activity, and evals when a feature calls a model (which model runs the cases, with or without the no-plugin arm) |
| 8. Documentation | Technical docs generated from the code (entity diagrams, data dictionary, route map, module diagram, and a tool catalogue when the app exposes tools to a model), the system prompt as a versioned file, and a hand-written architecture overview |

When a feature calls a model, the quiz treats it as a system with its own decisions, not as a library: which provider and where it runs (a self-hosted one is the only place the data never leaves), a cost ceiling per user and a global breaker, a scripted fake so everything around the model stays testable, the system prompt as a versioned file, and a generated tool catalogue saying what each tool reaches and with whose permission. Example 14.14 shows the round.

For a public site (profile `website`), question 2c comes right after the profile: Claude shows the catalogue of 26 sections as a numbered list in the chat, grouped into **launch** (about me, testimonials, FAQ, e-mail signup, products, contact and WhatsApp) and **later** (services, portfolio, blog, gallery, search, newsletter, PWA and the rest), every one selected. You answer "ok" or the numbers to untick; unticking a section another one needs (e-mail signup while downloadable materials stays) is said once, and your answer stands. The foundation is not in the list, because the site always has it: content area, home, basic SEO, 404 and error pages, privacy policy and cookie consent. After the quiz the board holds one epic "Website sections": the foundation first, then launch, then later, one feature idea per section. An online store is never a section: it is captured as its own epic. For `website`, question 9b is always asked (signups are personal data), 12 and 14 are fixed (roles `Admin` and `Editor`, admin area in the same app), and e-mail (19) cannot be "none" with the signup or the contact section. Example 14.23 shows the question.

For an app that is only an API (profile `web-api`), answering "API only" in question 1 makes question 2 recommend `web-api`. Question 10a then asks, instead of question 10, how the API's consumers authenticate, as a multiple choice: **Identity bearer** (users sign in at the API; the default for the owner's own clients), an **external IdP** (users, and client credentials for machine clients; the API only validates the token) and **API key** (machine clients only, never a browser or mobile app, whose code is public). Each answer generates only the schemes, endpoints and policies it needs, and question 11 (social login, MFA) is asked only with Identity bearer. Claude does not ask 14 (there is no back office), 21 and 22 (no screens) or the visual identity and UI questions, and question 29 has no UI level. Example 14.28 shows the question.

For a mobile app (profile `mobile`), question 2d asks where the backend lives: **in this solution** (today's behavior: `monolith` by default) or **an existing API in another repository** — an agile `web-app`, `website` or `web-api` (its repository), or any API that publishes an OpenAPI document (its URL); question 1 offers "mobile on an existing API" for it. With the external answer the app is its own repository, and Claude skips what the API already decides — question 3, the whole data round, 12, 13 and 14 — lists them under "Already clear", and turns question 10 into "the API's sign-in", read from the API. `CLAUDE.md` gets the line ``- Backend: external — <API name>, <repository or URL>; contract in `docs/api/backend-openapi.json`.`` and ADR-0001 says so. When the API is an agile site whose `CLAUDE.md` names the `<App>.Shared` package, the screens are MAUI Blazor Hybrid: Claude reads that from the site, never asks it again, and adds ``- Shared screens: package `<App>.Shared` from the site's GitHub Packages feed``; otherwise they are XAML. Example 14.30 shows the quiz and what the bootstrap proposes; example 14.33 shows it with Hybrid.

For a mobile app, question 2f follows 2d with either backend answer: **online · offline-first**. The recommendation comes from the brief: the app works in the field, with poor or no connectivity, or the brief says "offline" or "sem internet" → offline-first; otherwise online, which keeps today's behavior (a feature may still ask for offline in its refinement). Offline-first covers reads and writes: `CLAUDE.md` gets `- Offline: first` under `Profile:`, ADR-0001 one line, and the board proposal gains "Offline foundation" (the local store, the write queue, the sender, the connectivity interface and the `OfflineStatus` banner) right after the UI kit and the first feature; from then on every feature file has an `## Offline` section. With an existing API, Claude reads the pinned document: an agile API whose write operations lack the `Idempotency-Key` header gets a proposed issue "Idempotent writes for <App>" on its board (with your yes; Claude never writes in that repository) and "Offline foundation" waits on it; any other API that does not declare it leaves the app offline for reads only, and ADR-0001 says so. Going back from offline-first to online is not offered; an online project turns it on later with an "Offline foundation" idea. Example 14.39.

For a mobile app whose backend is in this solution, question 2g follows 2f: **a web backoffice for the staff: no · yes**. The recommendation comes from the brief: it names people who manage the app's data (staff, back office, admin panel, "painel", "gestão", operators) → yes; otherwise no. Yes writes `- Backoffice: staff` under `Profile:` in `CLAUDE.md`, no writes `- Backoffice: none`, both add one line to ADR-0001, and yes puts "Backoffice foundation" on the board right after the UI kit; the skeleton does not change. With the backend in another repository (`Backend: external`) 2g is not asked: the backoffice lives with the data, in the API's repository, and ADR-0001 says so. A running `mobile` project with neither line gets "Staff backoffice" from `/agile:sync` as a missing capability with the item to capture; `- Backoffice: none` stops the note. "Backoffice foundation" already includes the audit log of what the staff change (no extra question; it writes `- Backoffice audit: on`); a running project with `- Backoffice: staff` and no `- Backoffice audit:` line gets "Staff audit log" from `/agile:sync` the same way, and `- Backoffice audit: none` stops that note. Examples 14.54 and 14.63.

For a desktop app, question 2f follows 2a2 when 2a2 chose an API, with the same recommendation and the same writes (the `- Offline: first` line, one line in ADR-0001, "Offline foundation" after the UI kit and the first feature). With direct access to the database it is not asked, and ADR-0001 says that offline-first needs an API (a SQLite store is already local). Example 14.43.

For a site that needs a mobile app from day one, question 2 first weighs the shape of the brief: when the API is the main consumer or there are several business areas, it recommends `mobile` (on `monolith`, or `modular-monolith`); when the product is a site with screens over simple data, or a public site, it recommends `web-app` or `website` and question 2e follows, right after 2 (after 2c for `website`): **a mobile app from day one: no · yes, MAUI Blazor Hybrid · yes, MAUI XAML**. The recommendation comes from the brief: no app named → no; an app named → Hybrid, so the site's screens are born where the app will reuse them. The app lives in this repository and solution; an app in its own repository is still started by its own bootstrap (question 2d). With yes, Claude lists the brief's features numbered in the same message, grouped as the brief groups them and all ticked (never a public section of a `website`); you answer "ok" or the numbers to untick. The bootstrap then adds `<App>.Contracts` and, with Hybrid, `<App>.Shared` (a Razor Class Library whose components have no `@page` and no render mode, with texts in every language of the app), both covered by the architecture and layout tests; it does **not** add the app's projects, the `Complement:` line or a second `<Version>`: those come with the epic's first item, so `/agile:sync` never asks for an app version before the app exists. The board proposal gains one epic "Mobile app" (`docs/epics/mobile-app.md`, with the list you approved written whole) whose ideas are "Mobile foundation" first, then one "<X> on mobile" per ticked feature; the site's own idea for each ticked feature says its screen is a component in `Shared` (Hybrid) and its rule or query a class in `Features/`, and with Hybrid the UI kit lives in `Shared` too. ADR-0001 records the answer. "No" leaves the bootstrap exactly as it was. Example 14.34.

When question 3 is "containers + Aspire" and the skeleton has an AppHost, question 3a follows it: **fixed local services: yes · no**, yes recommended. Without it the database container gets a new host port on every `aspire run`, so a DataGrip or pgAdmin connection you saved stops working, and the password Aspire generates is one nobody knows. Yes covers every container the AppHost declares (today the database): its image's default port (PostgreSQL 5432, SQL Server 1433, or the next free one when the port is in use on this machine, which Claude checks first; you accept it or type another), a password parameter `<resource>-password` that the bootstrap generates and stores in the AppHost's user secrets (the value is never printed in the chat, never written to a file of the repository), and `WithDataVolume()`, so the data survives a restart. With more than one API in the solution, a free start (such as 5100) is proposed too and each API takes the next port in the order of the solution, `https` at that port + 1000. The answer is `- Local services: fixed` (or `none`) under `Profile:` in `CLAUDE.md`, and a yes also writes `## Local services` in `docs/infra.md`: each service's port, the user-secrets key of its password and its volume, never a value, and how to reset the volume (the image applies the password only when its data is created, so a changed password needs the volume removed, and the data in it is lost). You read the password with `dotnet user-secrets`, and a clone on another machine sets its own. A worktree runs on the same fixed ports but with its own volume, so one app host at a time. Deployed environments do not change: nothing of this runs in publish mode. A project bootstrapped before this has `/agile:sync` propose a "Fixed local services" item while it has an AppHost and neither `- Local services:` line; `none` declines it, and an AppHost that already pins them only needs `- Local services: fixed` written. Example 14.81.

When the brief names an existing code base to reuse and you give Claude access, it reads that code (never edits it) and uses what it finds as reasons. After round 8 comes one **closing question** — "which domain concept worries you most, or did the quiz not touch?" — because the core vocabulary of a domain (a taxonomy, an entitlement model, a scoring rule) rarely fits a fixed question list. What it raises is decided like any quiz question and recorded in ADR-0001.

Outputs, all in English:
- `CLAUDE.md` — short (under ~900 words, none of the template's comments), pointing to one profile.
- `docs/decisions/ADR-0001-foundation.md` — every quiz decision with its reason.
- `docs/agile/profile.md` — copy of the chosen architecture profile.
- `docs/agile/workflow.md` and `docs/agile/workflow.pt-BR.md` — this workflow in both languages, and `docs/agile/templates/` — the templates, copied into the project.
- `docs/glossary.md` — business terms and their English identifiers, and the technical terms Claude uses in reports and reviews (the review severities, for example) with the pt-BR word it uses when talking to you. Both tables end with a column `Meaning (pt-BR)`, the meaning written in Portuguese: the one text in a project that is not English. A technical term is one you may not know: a product or service name (Bicep, ACR), an acronym, a protocol, a pattern, a library; a business term, a plain word, an identifier, a path or a command is not. The skill that writes a document adds the missing rows in the same step and the same commit: `/agile:bootstrap` for ADR-0001 and `docs/infra.md`, `/agile:ship` for the item's changes under `docs/` (outside `docs/manual/`), `/agile:publish` for the release notes. Each of those documents (infra, ADRs, item files, discussions, epics, release notes) has, right under its title, the line `Technical terms: [glossary](../glossary.md)`; the app manual for end users does not. A project that predates this gets the column, the missing rows and the link lines proposed by `/agile:sync`, written only with your OK. Example 14.45.
- `docs/infra.md` — how to run locally, which environments really exist (`provisioned` or `planned`), expected secrets (names only), for each environment the command that deploys it, its check URL and the version deployed there (written only by `/agile:publish`), for a desktop app the update source of the installed apps, release steps and measured build and test times. Updated on ship when any of it changes.
- `.github/workflows/deploy.yml` and `.github/scripts/agile-deploy.js` — only when `origin` is on github.com and a non-local environment declares a deploy command: the pipeline that deploys a pushed `v<Version>` tag by that same command (see "The deploy pipeline" below).
- Solution skeleton for the profile, with i18n and the test projects in place.
- `docs/design/identity.tokens.json` and `docs/design/DESIGN.md` — the app's visual identity (question 23a), for every app with screens. The tokens follow the W3C Design Tokens format (DTCG 2025.10) and are the source of the values: colors by role in light and dark, typography, corner radius, and the icon family, the source (file, URL, image or derived) and the date under `$extensions.agile`. `DESIGN.md` (Google's design-file format) has the same values as YAML front matter, generated from the tokens, followed by your own prose on why and where; regenerating keeps the prose. You can give a file (tokens or a `DESIGN.md`), a website URL, an image (a logo, a brand board) or nothing: three questions (primary color, font, light, dark or both) derive a full identity. A URL or an image is read by Claude, which shows one table before anything is written: colors by role (an image's are marked estimated), fonts, radius, icon family, and the WCAG 2.2 AA contrast of every foreground/background pair in both themes; a failing pair names its ratio and the nearest passing value, and you decide. Nothing is stored before you say "confirm". The plugin never writes the app's theme: the UI kit item does, from these tokens, and a test in the profile compares the two. Screen mockups take their colors, fonts and radius from the same file.
- `docs/architecture/` — when round 8 chose any document: `tools/<App>.DocGen` generates, per module, a DBML schema read in a dbdiagram viewer (`entities: "mermaid"` in `docgen.json` gives an ER diagram instead, which renders on the board but grows unreadable past a dozen tables) and a data dictionary (from the EF model), a route map per area (from the OpenAPI document that the `/openapi/v1.json` integration test writes to `docs/api/`) and a module diagram (from project references, in Mermaid); the generator references the app's own EF Core provider (question 5: PostgreSQL, SQL Server or SQLite) and calls it by reflection, so it documents the real column types and default schema (`public`, `dbo`, or none on SQLite) even for a `DbContext` with no design-time factory; a context with a factory is built through it instead, so the names follow the database (snake_case, for example) — the output is the same either way; when round 8 also chose the tool catalogue (question 37), `tools.md` lists every tool the app offers to a model — the description the model receives, the input schema, what it reaches, the permissions it requires, whether it writes and whether it asks first — and `--check` fails when a tool has no description, no permissions or no reaches, or when two tools would reach the model under one name. Bootstrap declares the generator as the project's **docs command** in `.claude/agile/build.json` (`{ "docs": { "command": "dotnet run --project tools/<App>.DocGen", "check": "… -- --check", "paths": ["docs/architecture/"] } }`), so from 0.0.72 `/agile:ship` regenerates and checks them through the one generic step (section 9, "A declared docs command") instead of a step of its own; `--check` fails when they are stale. A project that got DocGen before 0.0.72 declares nothing, and the ship still runs it: the gate finds the single `tools/*.DocGen` and runs it as an **implicit** docs command, saying `implicit DocGen docs command: declare it with /agile:sync` — so no window leaves the code map stale, and `/agile:sync` offers the block once. Optionally a one-page hand-written overview (C4 context and containers), which the generator never touches: only files carrying its `Do not edit` marker are rewritten or deleted; a module or external system a feature adds is written there by hand at ship.
- The first epics on the board, if the brief already names them.

## 5. Feature lifecycle

```mermaid
stateDiagram-v2
    [*] --> idea: /agile:idea
    idea --> refining: /agile:refine
    refining --> approved: you say "aprovo F-n"
    idea --> cancelled: a duplicate, chosen at the similar-item card
    refining --> cancelled: a duplicate, chosen at the similar-item card
    cancelled --> [*]
    building --> blocked: /agile:change F-n --block "reason" (also from refining, approved, validating)
    blocked --> building: /agile:change F-n --unblock (back to the status it left)
    approved --> building: /agile:build
    building --> validating: coverage table + validation script
    validating --> building: a fix you reported
    validating --> done: you say "validado", then /agile:ship (typing it authorizes the merge)
    done --> [*]
    note right of approved: gate 1 — approved
    note right of validating: gate 2 — validated on screen
    note right of done: gate 3 — /agile:ship typed = merge authorized
```

| Status | What happens | Who moves it |
|---|---|---|
| `idea` | Captured from the chat with `/agile:idea`. Title, 2-3 lines and how it starts (`## Start`): what it depends on, what it waits on to start, what only its validation needs (never a block) and from whom, the suggested path, what can run beside it. What nobody said is written as unknown. A cause it names comes with its evidence (the command and the output line that shows it); without one, `## Start` says `Cause not verified: measure it at /agile:refine`, with the symptom seen. For a feature or bug, before anything is created Claude asks where it lives: one question with the epic it suggests first (an open epic whose goal fits, with the reason), then "New epic" and "No epic"; a new epic is created only after your yes, on the board and as `docs/epics/<slug>.md` (status `draft`). `/agile:epic` and a text that is itself an epic ask nothing. | Claude |
| `refining` | `/agile:refine` (an answer that turns something into a later item becomes an idea through the same procedure as `/agile:idea`: template, board, next number; an answer that folds an existing open item's scope into this one instead first checks that item — `git worktree list` for its folder and its file or issue for its status. Any status beyond `idea` anywhere blocks an automatic fold: Claude names the other item's status and worktree path and you decide, fold anyway (recorded in `## Decisions` with the reason) or drop it and leave the other item untouched. Still `idea` everywhere, with no worktree: it folds in exactly as before, no new question): before anything is written, Claude creates the item's branch and its own worktree — a folder outside the repository whose full path it tells you — and everything this item produces (the feature file, the cause of a bug, the mockup) is written there, on its branch; an item file not yet committed is moved in and no longer exists where it was created. No checkout is switched, so a session sitting in another item's folder can no longer leave this item's documents on that item's branch. Then Claude reads the related code and checks in today's code every premise about how something already behaves (an earlier item's file is not proof: a later bug may have moved it), every premise about how a library stores or protects data in the library's source or docs, a query-performance premise with `EXPLAIN` on the test container, and a premise a library's docs and source both leave open by reproducing it in a scratch project against a test container (source read raw, never summarized) — a premise that nothing uses a feature is checked by its effect, not by the callers of one helper. A bug whose cause lives only in an unmerged item's branch says so in `## Cause` and waits for that merge before branching. Then Claude asks every open question in one round, as quiz cards grouped by topic (rules, permissions, states, screens, data, packages, scope) with the recommended option first; in a terminal the same questions come as a numbered list. The round includes the new packages the item needs, for the code **and for the tests**, with versions checked against the registry at that moment, so your yes is given once and not in the middle of the build. You answer; at most one follow-up round. A list you approved or edited in the chat (a catalogue, a set of options, numbered choices) is written whole into the item's `## Approved list`, in the approved order with your edits applied, and the file never says "the list shown in the refinement"; before asking for your approval Claude re-reads the file for phrases that send the reader to the chat, and pastes the list in where it finds one. An item whose output is visual (a diagram, a generated page) is prototyped and seen at real size in the viewer it is meant for before you approve it. An authentication or account-linking item gets the independent review (`/agile:review`) on this file, before you approve it. A new or complex screen is designed by the `ux-designer` agent (`/agile:screen`), which never talks to you: Claude reads what it wrote and asks its open points as its own. The feature file is committed on the item's branch, in its worktree. | Claude |
| `approved` | You approve the feature file after reading it. Open questions block approval. **Gate 1.** | You |
| `building` | `/agile:build`: first it finds the item's worktree by its branch and says `Worktree of <id>: <path> [<branch>]` (`(created now)` when an older item had none); the status and the clean-tree check are read there, never in the main checkout, and it continues in that worktree — code and tests for what changed. When the item creates a project, an API contract, a message between modules or a schema change, two read-only passes run before the plan: `system-design` proposes the slice, the contracts, the data and the risks, and `architect` reviews that proposal against the profile and the checks your project really has. They write nothing; Claude verifies both against the files, writes the plan from them and records in `## Decisions` what it accepted and dropped. A thin CRUD skips both. When the item has a mockup you approved, the screen and its tests are written by the `frontend` agent, alone in that worktree and in the foreground, so the Stop gate never builds files it is still writing. The plan lists what the screen removes; Claude removes it before the call (the tests of each removed type first, then the files and what points at them: usings, imports, registrations), because the agent cannot delete a file and never leaves a stub. The agent adapts test helpers, contexts and fixtures freely, and changes an existing assertion only under a named approved criterion. It starts its report with `Nothing here was built or run.` and lists the existing tests that assert what it changed, the removals it needs from Claude and the assumptions it could not verify. Claude then runs the whole suite (the gate's `ship` mode) before reading its files and quotes the real counts, reads every file it names, fixes the failures itself in one planned round and runs the suite again; the agent is not called again. Claude answers its stops or brings you the ones that are decisions (a pattern the kit lacks, a contract that does not exist). The build report closes the hand-off in one block (example 14.76). Domain, API and migrations stay with Claude. Before copying an existing pattern, Claude checks whether an open item exists to remove it and, if so, lets you choose between following it now or recording the copy as debt. Before the coverage table Claude opens the screen through the app host, started from the item's worktree in a background shell and opened in the preview by its URL (no launch configuration is edited), and stops it before its turn ends. When the data is the item's own (an AppHost data volume with no name, or your project's own database knob set for the worktree) it asks nothing; data shared with the main checkout or another worktree needs your yes, once per item, recorded in `## Decisions`. Tests do not see how the component library renders its states (an active link with no contrast, a link that is not a link). Claude reads the page as text; a screenshot is only for a visual check and never while a dialog is open (a timed-out screenshot can drop a Blazor Server circuit and the dialog with it). An accessibility claim the screen section marks `unverified (gallery DOM)` is checked in the rendered markup and corrected. Keyboard checks stay in your validation script. Claude never changes state (sign-ups, counted requests, data) in an app host it did not start: it asks first, or uses data no one else uses and says which. A screen behind sign-in is not checked by Claude, whose rules forbid typing passwords: it says so, checks what needs no account (the route, the 401, the redirect) and puts the signed-in flow in your validation script. Only one feature can be here. | Claude |
| `validating` | Claude hands over a validation script (≤ 8 steps). You try it on screen. A step that needs a terminal gives the command for Git Bash and for PowerShell 7, with the expected output and how to repeat it, and Claude has already run both. The start step says whose data the app runs on: its own volume (an AppHost data volume with no name: nothing to set), your project's own knob set to `<database>_f<n>` (`_b<n>` for a bug) in both shells, or a plain warning that the local database is shared and the item's migration lands there (example 14.82). A step that asks you to change code names the file, where the lines go and the exact failure to expect, and a step that compiles tells you to stop the app host first (a running host locks `bin/`: `MSB3027`). A step that a fix made wrong is corrected with `/agile:script <id> <what changed>`, without a change note. **Gate 2.** | You |
| `done` | `/agile:ship`: full test suite, `## Criterion → test` checked (a `pending` row or a test not found stops it), app version bumped, merge — typing the command is your go-ahead (**Gate 3**), board updated, app manual updated, retro. | Claude |
| `cancelled` | An exit, not a step: only from `idea` or `refining`, only as a duplicate, chosen at the card `/agile:refine` shows when it finds a similar item (below). The file stays with `status: cancelled` and the line `Duplicate of <id> (<date>): <where the improvement went>`, so its number is never reused; the session status and the backlog head ignore it. On GitHub the issue is closed as a duplicate of the other and its project item archived (never Done: that would show undelivered work as delivered); on Azure Boards the state is `Removed`; with no board its row in `docs/agile/backlog.md` is struck through. | You, at the card |
| `blocked` | A side status, not a step: something outside the work stops the item (a decision, another item, a provider, your own check). `/agile:change <id> --block "<reason>"` sets it from `refining`, `approved`, `building` or `validating`, never from `idea`, `done`, `cancelled` or `blocked`. It needs the reason and who unblocks it (`unknown` is not accepted for either; Claude asks for the missing part and changes nothing). The file says `status: blocked` and, under it, `Blocked (<date>): <what stops it> — unblocked by <who> — returns to <status>`; `## Decisions` gets a line. The item keeps its worktree and branch, holds no WIP slot, and nothing is committed, merged or switched (on a dirty tree Claude says `/agile:pause` writes the `wip` commit). `/agile:build`, `/agile:ship`, `/agile:refine`, `/agile:autopilot` and `/agile:screen` stop on it after the item line, name the reason and print `/agile:change <id> --unblock`; nothing is approved while it is blocked. `--unblock` returns the item to the status named in `returns to`, removes the line and the board mark, and writes the pair line to `## Decisions`; back to `building` or `validating` it is refused while another item is in one of them in that checkout (it names it; `--unblock --worktree` is your OK for parallel work). On the board the GitHub label (or Azure tag) `blocked` is added and the Status field is left alone; with no board, its row in `docs/agile/backlog.md` gets `blocked: <reason>`. `/agile:status` and the session start show a `Blocked:` line per blocked item (id, title, reason, who unblocks it, where it returns to); the line that comes from `## Start` is now called `Waits on:`. | You, with `/agile:change` |

**The item line.** `/agile:refine`, `/agile:build`, `/agile:ship`, `/agile:change`, `/agile:review`, `/agile:autopilot`, `/agile:screen`, `/agile:script` and `/agile:pause` start by naming the item they act on: `F-12 — Export the monthly report as CSV [idea]` (id, title, status), before any check and before anything is created or changed, so a wrong id is caught at once even when a check then stops the command. The title and the status are read from the item's file (the copy in its worktree when it has one), never from the board, so it works with `Board: none`; an id with no file prints `F-99 — no item file found` and the command goes on with its own rule for a missing item. It is written once per run: `/agile:autopilot` prints it at its start, not again for each phase, and `/agile:build` keeps its `Worktree of ...` line right below it. `/agile:pause` prints one line per item it commits. `/agile:status` and `/agile:idea` do not need it: one lists every item with its status, the other ends with the new item's id and title.

**Similar items.** Before `/agile:idea` takes a number, and before `/agile:refine` creates a branch, Claude reads every item (`docs/features`, `docs/bugs`, the item file in each worktree, where an item in progress keeps its current status, and the board, open and closed, `done` and `cancelled` included) and judges which ones cover the same subject in other words. It never uses a keyword rule. With no candidate it says `No similar item (<N> items read).` and goes on. With candidates (at most 3, a reason each) it asks one card, the recommended action first. To improve the existing item: an `idea` gets `Added <date>: <text>` in its summary; a `refining` item gets the text as an open question in its own worktree (Claude says the path); an `approved`, `building` or `validating` item is not touched and Claude prints `/agile:change <id>` with the text for you to type; a `done` item is reported as `Already delivered in <id>`, and you create nothing or create the new item with a line linking the `done` one. In `/agile:refine` the card has three actions: carry the improvement to the other item and cancel this one (a resumed refinement also loses its worktree and branch), keep both (a line in `## Decisions`), or fold the other into this one (the check of its status and worktree of section 14.18). `/agile:epic` and `/agile:discuss` search once for the whole list and show every collision in one card; `/agile:autopilot --assume` ends with the question, since a duplicate is your decision.

Small fixes found during validation are done right away, without leaving `validating`.

Before handing over the validation script, Claude keeps a **`## Criterion → test`** section in the feature file, after `## Acceptance criteria`: a two-column table, written when the build starts with every row `pending`, and a row is filled in the same commit that adds its test. Every acceptance criterion points to its tests, or to `validation step <n>` when it can only be checked on screen (the step is in the validation script); no row stays `pending` at the end. Because it lives in the file, a compaction, a pause or a new session still knows which criteria have no test, and `/agile:ship` checks it (14.83). A bug has no such section: its `## Regression test` plays that part. The test must go through the path a user reaches (page, endpoint, the handler that calls the code): a method written for a criterion that nothing in the app calls is a gap, even when its own test passes. When an effect leaves the request (an email or an event moves to a queue), every test that asserted it is re-read: "nothing was sent" passes for free once nothing is sent right away. A flaky test is reproduced in a loop (clean build before each run) and its fix proven by the same loop, N green in a row; a test of generated output counts each section, exactly once.

### The same path in one run: `/agile:autopilot`

`/agile:autopilot <id>` runs this whole path, from `idea` to `done`, and stops for you **exactly twice**. The three gates are all still there: the first stop is gate 1, the second stop holds gates 2 and 3, and you choose whether to pass them in one message.

```mermaid
sequenceDiagram
    actor You
    participant A as autopilot
    You->>A: /agile:autopilot F-n
    A->>A: checks Start, reads the code, verifies premises
    A-->>You: STOP 1 — question cards, packages, draft criteria, mockup
    You->>A: answers + "Aprovo F-n"
    A->>A: build, tests, gate, screen check, review when risky
    A-->>You: STOP 2 — coverage table, review findings, validation script
    alt "validado e autorizo o merge de F-n"
        You->>A: validation + merge authorization
        A->>A: full suite, manual, merge --no-ff, board, retro
        A-->>You: done — merge commit, totals, lessons
    else "validado"
        You->>A: validation only
        A-->>You: stays in validating — /agile:ship F-n when you want
    end
```

| Stop | What you get | How you answer | What follows |
|---|---|---|---|
| 1 — questions | Every open question as cards (recommended option first), the new packages with license and version, the **draft acceptance criteria** written from the recommended options, and for an item with a new or complex screen the **HTML mockup** built from the same options. The last card: "Approve F-n with these answers and criteria?" | Pick the answers and "Aprovo F-n". Any other answer is a follow-up round, never an approval. | The item file is filled with your answers and set to `approved`; the build starts in the same run. If a screen answer differs from the recommendation, the mockup is regenerated and the run stops once more for the screen only. |
| 2 — validation | Files, tests with real numbers, the `## Criterion → test` section, the reviewer's findings (it runs by itself when the change is risky: authentication, permissions, data, contracts, money, more than ~400 lines), what `--assume` assumed, the validation script. | "validado e autorizo o merge de F-n" — or "validado" alone — or the defect you found. | With the merge named: full suite, app manual in three languages, `merge --no-ff`, board, retro, `done`, without asking again. "validado" alone: the item stays in `validating` until `/agile:ship F-n`. A defect: fixed, then stop 2 again. |

`--assume` skips stop 1: every question gets its recommended option, recorded in `## Decisions` as `assumed by autopilot` with its reason, and the flag counts as your approval for that item. It never assumes a new package — when the item needs one, stop 1 happens with that question only. The mockup, when there is one, is then shown at stop 2.

Some decisions stay yours even inside a run. The run **ends** — nothing is reverted — and says where it stopped, what is done, what is left and the command to continue, when it meets: a package you did not approve at stop 1, a schema change not in the file, money, permissions, shared data, credentials, a screen behind sign-in, another module's contract, a false premise or an impossible criterion, a gate red three times, a blocker it cannot fix inside the item, a red suite at ship, or a merge conflict.

The run keeps an `Autopilot:` line in the item file (`refined`, `stop 1`, `approved`, `built`, `reviewed`, `stop 2`, `shipping`). `/agile:autopilot F-n` resumes from that line, so a new session or a summarized context loses nothing.

### Optional steps

| Command | When | Result |
|---|---|---|
| `/agile:discuss` | An idea with several possible directions, or doubts only you can answer | `docs/discussions/D-<n>-<slug>.md` (options, decisions, parked points) and the items captured as `idea` |
| `/agile:epic` | A new epic to plan | `docs/epics/<slug>.md` with prioritized, session-sized features, what each depends on and waits on, an execution plan (order, suggested path, what runs in parallel) and what waits outside the epic; each feature captured as `idea`. On a `web-app`/`website`, a mobile app epic follows the `mobile-client` complement (section 12) |
| `/agile:screen` | A feature in `refining` with a new or complex screen | The `ux-designer` agent writes the detailed screen section in the feature file and an HTML mockup (every state, three languages; a new colour only after its contrast is computed on every surface, in both themes), alone in the item's worktree; Claude reads both, sends you the mockup as a file (never through a server), asks the agent's open points with its own questions, and you approve the screen together with the feature. In the build, the `frontend` agent implements that approved mockup and the tests of that screen. The mockup's colors, fonts and radius come from `docs/design/identity.tokens.json`; a project without one is told so in the agent's open points, and the mockup uses the library defaults. What a library component announces (a role, an `aria-*` state) is not taken from its docs: the agent marks it `unverified (gallery DOM)`, Claude checks it in the dev-only gallery's rendered markup and corrects the section, and with no gallery yet the build checks it |
| `/agile:review` | A risky change (authentication, permissions, tenant isolation, data, contracts, money, or more than ~400 lines), before validation | Findings by severity from a read-only reviewer with fresh context; confirmed blockers are fixed before you validate |
| `/agile:publish` | After one or more ships, when you want the app version on `main` as a release | A `dotnet publish` package of the profile's deployable project(s) in `artifacts/publish/v<Version>/` with one `.zip` each (a mobile head: a signed Android `.aab`), the notes in `docs/releases/v<Version>.md` (and `v<Version>-store.md`, the store checklist, for a mobile head), an annotated tag `v<Version>`, both pushed, and a GitHub Release with the notes. Typing it is the authorization. With an environment named (`/agile:publish production [v<x.y.z>]`) it then deploys that release by the command `docs/infra.md` declares for the environment (see "Deploying" below); `/agile:publish <environment> --park` and `--resume` stop and bring back an Azure Container Apps environment (see "Parking an Azure Container Apps environment"). `/agile:publish --beta` only reruns a desktop app's beta of `main` when the ship's beta step failed (see "Desktop updates") |

**Releasing: `/agile:publish`.** A ship bumps the app's `<Version>` and merges; it does not build anything you can hand to someone. `/agile:publish` turns the version already on `main` into a release. It runs from the main checkout and stops, changing nothing, unless the main branch is the one `CLAUDE.md` names (a line `- Main branch: `<name>`` or the bootstrap's ``merging to `<name>` ``; none, or two different ones, is a stop that says which line to add) and it is clean, level with `origin` and `v<Version>` exists neither locally nor there. Claude shows the plan once (version, previous tag, the items merged since it, the projects and runtimes) and goes on: typing the command is the authorization for the commit of the notes, the tag, the push and the GitHub Release. It does not rerun the tests (the ship ran the full suite on what `main` holds); a compile error still fails the Release `dotnet publish` (or `dotnet pack`), and then nothing is committed or tagged. The site of a Hybrid app in its own repository also packs its two packable projects, `<App>.Contracts` and `<App>.Shared`, at that same version and pushes them to its GitHub Packages feed after the tag (`--skip-duplicate`, so a rerun is safe), reading the token from `GITHUB_PACKAGES_TOKEN`; without the token, or with an `origin` outside github.com, the packages are left out with that reason and the site is still published.

What is packaged follows the profile: `web-app` and `website` → `<App>.Web`; `monolith`, `modular-monolith` and `web-api` → `<App>.Api`; `desktop` → the head, self-contained, once per runtime in its `<RuntimeIdentifiers>` (else this machine's), plus `<App>.Api` when there is one (with an update source declared in `docs/infra.md`, the head's Windows runtimes, and an Avalonia head's `linux-x64` as an AppImage, are packed by Velopack instead of zipped: see "Desktop updates" below); `mobile` → `<App>.Api` plus the Mobile head as a signed Android `.aab`, and a web app with the `mobile-client` complement → `<App>.Web` plus the same `.aab` (see "Store publishing" below); `microservices` is not supported. All projects of one release carry the same `<Version>`. The package drops `appsettings.Development.json` and the project's own `.xml` documentation file and keeps the `.pdb` files. A WinUI 3 head without `<EnableMsixTooling>true</EnableMsixTooling>` stops the plan: it would publish an exe that crashes on start. A linux or macOS runtime zipped on Windows loses the execute bit, and the report says to `chmod +x` it. The notes list one line per merge on `main` since the previous `v*` tag (`Feature F-3: …`, `Bug B-2: …`, other branches under "Other"), in English; the tag carries the same text. If the push or the GitHub Release fails after the tag, Claude names the exact command to rerun and never deletes the tag or the commit.

**Deploying: `/agile:publish <environment> [v<x.y.z>]`.** With an environment named, the release is followed by a deploy of it, by the command that `docs/infra.md` declares for that environment: its environments table has three more columns, `Deploy command` (`not declared` when there is none), `Check URL` (optional) and `Version (deployed on)`, which only `/agile:publish` writes. Typing the command is the authorization for that deploy too. Before it runs Claude shows the environment, the version deployed there now, the version to deploy, the command and the names (never the values) of the secrets it needs, and goes on. Three shapes: a new version (`/agile:publish staging` with no tag yet) runs the release above first, and a failed release deploys nothing; a **promotion** (`/agile:publish production` when `v<Version>` is already tagged) deploys that tag with no new package, notes or tag; a **rollback** (`/agile:publish production v0.3.0`) deploys an older tag, with no extra question. With no environment named, Claude lists the environments with their recorded versions and asks which one, or "none" (the release only). An environment whose command is `not declared` deploys nothing: Claude says where to declare it, and a new version is still released.

Every deploy runs in one fixed worktree, `<worktree root>/deploy`, detached at the tag (made the first time, moved with a detached `git checkout` after), so your main checkout is never switched and a tool that keys on the folder path (Aspire names its compose project after it) replaces the running app instead of starting a second one. The command gets `AGILE_ENVIRONMENT`, `AGILE_VERSION`, `AGILE_TAG` and `AGILE_ARTIFACTS` (the package folder of that version, rebuilt from the tag when it is missing and the command is not the Aspire recipe); its output is saved whole outside the repository, with the value of every secret blanked, and its exit code decides success. The command runs in the machine's default shell (cmd.exe on Windows, so `%AGILE_TAG%`; /bin/sh elsewhere, `$AGILE_TAG`) and is limited to 30 minutes; two deploys never run at once (a lock file next to the deploy worktree), and one that finds tracked files changed in that worktree stops before running. A secret listed in `## Expected secrets` with "environment variable" and this environment is only checked by name: the variable must be set in the shell that started Claude (for the Aspire recipe it is `Parameters__<name>`), or the publish stops before anything runs. With a `Check URL`, after exit 0 the URL is polled for HTTP 200 for up to 60 seconds; no 200 fails the deploy. On success the environment's `Version (deployed on)` cell becomes `v<x.y.z> (YYYY-MM-DD)`, committed on `main` (`docs(release): v<x.y.z> deployed to <environment>`) and pushed. On a failure nothing is recorded and **nothing is rolled back by itself**: the report quotes the output's tail and gives the exact command that redeploys the version recorded before (`/agile:publish production v0.3.0`). The plugin never deletes old images (it counts the `aspire-deploy-*` tags and names the commands) and never takes the compose environment down with its volumes, which would delete the volume that keeps the app's Data Protection keys and with it every signed-in session. `/agile:sync` lists a project whose `docs/infra.md` lacks the `Deploy command` column as a missing capability and offers to capture an item; it never writes that file.

**The deploy pipeline (GitHub Actions).** A project whose `origin` is on github.com and whose `docs/infra.md` declares a deploy command for a non-local environment gets `.github/workflows/deploy.yml` and its script `.github/scripts/agile-deploy.js`, written by `/agile:bootstrap` (through the pipeline writer of sync.js). Pushing the tag `v<Version>` (which `/agile:publish` does) deploys it to the first environment that is neither `local` nor production, `staging` in the usual table, or to production when there is no other; the name is written once, as a literal, when the file is generated. Production is promoted by a manual run (Actions, Deploy, "Run workflow": `environment` and an existing `tag`, so a rollback is the same run with the old tag) or locally by `/agile:publish production`. What the run deploys is the **same `Deploy command` of the same row** that the local command runs, read from `docs/infra.md` when the run starts and never copied into the workflow, so the two cannot drift: edit the table, not the YAML. The job declares `environment: <name>`, so the secrets live in the GitHub environment of that name and you can add required reviewers there (the plugin never creates an environment, a reviewer or a secret, and never reads a value); it checks the tag out with its history, installs the .NET SDK from `global.json` and, for a recipe command, the Aspire CLI (the second recipe below), sets `AGILE_ENVIRONMENT`, `AGILE_VERSION`, `AGILE_TAG` and `AGILE_ARTIFACTS` (rebuilt from the tag when the command uses it), and polls the `Check URL` for 200 for up to 60 seconds. Every secret that `## Expected secrets` lists with "environment variable" for a non-local environment is passed from `secrets.<name>`; one that has no value stops the run **before** the command, naming it (values are never printed, and a value in the command's output is blanked). Runs of one environment never overlap, and the workflow holds read-only `contents` permission and uses only `actions/checkout` and `actions/setup-dotnet`. A failed command or check fails the run, nothing is rolled back, and the run summary names the rollback; a success prints `v<x.y.z> deployed to <environment>` and **commits nothing**: `Version (deployed on)` is still written only by a local `/agile:publish`. Before a local `/agile:publish <environment>` that will push the tag, Claude says that the pipeline also deploys it to its target (and, when that is the same environment, that the two run at the same time and are not serialized: the concurrency of the workflow queues only its own runs); typing the command stays the authorization. The file is yours once written: add a step before "Deploy" for any tool the command needs besides the SDK and what the Ubuntu runner has (the command runs in `/bin/sh` there, so `%AGILE_TAG%` forms do not work). `/agile:sync` never writes it: a project with a declared command and no workflow gets "Deploy pipeline" as a missing capability and the offer to capture an item, and one that has it sees a newer template, or a secret added to the table, as a `manual` difference to merge by hand. Only GitHub Actions is supported. Example 14.36.

**Deploying to the client's cloud account (`## Cloud accounts`, #66).** The repository, the board, the tag, the GitHub Release and the package feed stay in your GitHub; only the deploy goes to the account of the client the project is built for. `docs/infra.md` has a section `## Cloud accounts` with one row per non-local environment: `Environment | Client | Cloud | Tenant | Subscription or account | Resource group | Region`. `Cloud` is `azure`, `aws`, `gcp`, `other`, or `none` (a host with no cloud account, such as the compose recipe on your own machine). The ids are not secrets and are written there; a secret never is. An environment that declares a deploy command and has no row stops `/agile:publish` (and `--plan`) before anything runs, naming the row to add; two rows for one environment stop it too. Quiz question 25a writes the rows at bootstrap (client and cloud; the ids may stay blank until the client gives them, and the publish stops until then), and `/agile:sync` offers the section to a project without it, one blank row per non-local environment, never inventing an id.

For `azure`, Tenant and Subscription must be GUIDs (a domain name such as `contoso.onmicrosoft.com` is refused). The plugin keeps one `az` login per subscription in `<your home>/.agile/azure/<tenant>/<subscription>`, outside every repository, so two clients, or staging and production of one client, never share an active subscription and your own default `az` login is never touched. Before the deploy command, and already in `--plan` (so a missing login stops before the tag is pushed), it asks `az account show` in that folder and compares `tenantId` and `id` with the row, ignoring case (a guest's `homeTenantId` is never compared). No login in the folder: it stops and prints the one-time login, `AZURE_CONFIG_DIR="<folder>" az login --tenant <tenant>` for Git Bash and a PowerShell form that does not stay set. Logged in with another subscription active: the plugin runs `az account set` in that folder itself and checks again. The login cannot see the subscription, or sees another tenant: it stops with the declared and the active account (names and ids) and asks you to get access from the client or to correct the row. Then the command runs with every inherited `AZURE_*`, `ARM_*` and `Azure__*` variable removed (the plan lists their names, never a value) and with `AZURE_CONFIG_DIR`, `AZURE_EXTENSION_DIR`, `Azure__TenantId`, `Azure__SubscriptionId`, `Azure__CredentialSource=AzureCli` and, when the row fills them, `Azure__ResourceGroup` and `Azure__Location`: what `aspire deploy` reads, so the account is the row's and nothing else on the machine can pick another one (measured: the .NET `AzureCliCredential` honors `AZURE_CONFIG_DIR`, and an environment variable wins over a value saved in the AppHost's user secrets). `aws` and `gcp` are checked the same way (next paragraph); `other` is shown in the plan and the output as "not checked by the plugin"; `--plan` and `deploy --list` show client, cloud and account for every environment.

What to ask the client's administrator (the section's comment in `templates/infra.md` says the same): for your machine, you as a guest in their tenant with the role `Contributor` on the subscription, plus `Role Based Access Control Administrator` limited to the roles the app's managed identities need when the deploy creates role assignments (an Aspire Azure deploy does; for an `aca` environment it always does, so the pair is required, example 14.55); for the pipeline, an app registration in their tenant with the same roles and a federated credential for `repo:<owner>/<repo>:environment:<environment>`. With an `azure` row the generated `deploy.yml` also logs in with `azure/login@v3` by OIDC, from the GitHub environment's variables `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID` (variables, not secrets; the step is skipped when `AZURE_TENANT_ID` is empty, so a run to another environment is not affected), the job's permissions are `contents: read` and `id-token: write`, `docs/infra.md` is read from the commit that holds the workflow (so a rollback to a tag from before the section is checked against today's row), and `agile-deploy.js` stops before the command when the login does not match the row. That check catches a misconfiguration; it is not the security boundary: a manual run can start from any branch and gets the same federated subject, so give every Azure GitHub environment a deployment protection rule, "Selected branches and tags" with the tag pattern `v*` only, and required reviewers on production. `/agile:sync` offers the step, the permissions and the sparse-checkout line together with the new `agile-deploy.js` to a project whose workflow lacks them, and reports "login step without the check" for a workflow that logs in with an old script; the workflow stays yours, never written over. Example 14.47.

**AWS and Google Cloud rows (#68).** The same check, the same one login folder per account and the same clean environment, for the two clouds that were only shown. An `aws` row has `Subscription or account` = the 12-digit account id (`aws sts get-caller-identity` prints it) and a `Region` (a region code such as `us-east-1`); a `gcp` row has the project id (6 to 30 characters: lowercase letters, digits and hyphens, starting with a letter; not the project's name or its number; `gcloud projects list` prints it). A missing or malformed value stops with the cell's name and its expected form before any CLI is called. `Tenant` and `Resource group` are not used for these clouds: a value there is reported as ignored, never a stop. The logins live in `<your home>/.agile/aws/<account>/` (`AWS_CONFIG_FILE` and `AWS_SHARED_CREDENTIALS_FILE` point there) and `<your home>/.agile/gcp/<project>/` (`CLOUDSDK_CONFIG`), so the AWS and Google Cloud logins of your own machine are never touched. Before the deploy command, and already in `--plan`, the plugin runs `aws sts get-caller-identity` (its `Account` must be the row's) or `gcloud projects describe <project>` (its `projectId` must be the row's) in that environment, and again right before the command; a failed check runs nothing. No login in the folder (or an expired AWS SSO session): it stops and prints the one-time login for Git Bash and a PowerShell form that does not stay set: `AWS_CONFIG_FILE="<folder>/config" AWS_SHARED_CREDENTIALS_FILE="<folder>/credentials" aws configure sso` (then `aws sso login` with the same two variables when the session expires, or `aws configure` for access keys), and for Google Cloud `CLOUDSDK_CONFIG="<folder>" gcloud auth login` and then `gcloud auth application-default login` with the same variable (a deploy command that uses a Google SDK reads that second login, which `gcloud auth login` does not make; the check reads only the first). Another AWS account: it names both accounts and the caller's ARN. A Google Cloud identity that cannot read the project: it names the active account and says it needs `resourcemanager.projects.get` (Browser, Viewer, Editor or Owner has it). Then the command runs with every inherited `AWS_*` variable removed and `AWS_CONFIG_FILE`, `AWS_SHARED_CREDENTIALS_FILE`, `AWS_REGION` and `AWS_DEFAULT_REGION` set from the row (the folder uses its default profile, so no profile name is needed), or, for Google Cloud, every inherited `CLOUDSDK_*`, `GOOGLE_APPLICATION_CREDENTIALS`, `GOOGLE_CLOUD_PROJECT`, `GCLOUD_PROJECT` and `GCP_PROJECT` removed and `CLOUDSDK_CONFIG`, `CLOUDSDK_CORE_PROJECT` and `GOOGLE_CLOUD_PROJECT` set (the active project is chosen by that variable, nothing is written to the folder's configuration, so `--plan` stays read-only); the plan lists the names removed, never a value, and the `cloud` object of its JSON has `loginFolder`, `removedVariables` and `check` as for Azure. For the pipeline, an `aws` row makes the generated `deploy.yml` log in with `aws-actions/configure-aws-credentials@v6` by OIDC (`role-to-assume` from the GitHub environment's variable `AWS_ROLE_ARN`, `aws-region` from `AWS_REGION`), and a `gcp` row with `google-github-actions/auth@v3` (`workload_identity_provider` from `GCP_WORKLOAD_IDENTITY_PROVIDER`, `service_account` from `GCP_SERVICE_ACCOUNT`, `project_id` the row's project, or the variable `GCP_PROJECT_ID` when the table names more than one project) and `google-github-actions/setup-gcloud@v3`: variables, never secrets, each step skipped when its variable is empty, with the same `id-token: write` permission and the same read of `docs/infra.md`. `agile-deploy.js` runs the same check against that login (compare only, no login folder) and stops before the command on a mismatch; the run summary names the account or project it checked. As for Azure, that check catches a misconfiguration; the protection rule of each GitHub environment is the boundary. What to ask the client's administrator: for AWS an IAM role that trusts GitHub's OIDC provider for `repo:<owner>/<repo>:environment:<environment>` (its ARN is `AWS_ROLE_ARN`); for Google Cloud a Workload Identity pool and provider and a service account with `resourcemanager.projects.get` on the project, plus what the deploy command needs. The plugin does not create any of these. `/agile:sync` offers the login step of each of these clouds, with the new `agile-deploy.js`, to a project whose workflow lacks it, as it does for Azure, and the workflow stays yours, never written over. Example 14.56.

The plugin ships three recipes; the third, without Aspire, closes this section. The first is for "containers + Aspire" (every profile that can have an AppHost: `monolith`, `modular-monolith`, `web-api`, `microservices`, `web-app`, `website`; the profile's "Deploy recipe" section has the exact lines): the deploy command is:

```powershell
aspire deploy --apphost src/<App>.AppHost/<App>.AppHost.csproj -e <Environment> -o artifacts/deploy/<environment> --clear-cache --non-interactive --nologo
```

```bash
aspire deploy --apphost src/<App>.AppHost/<App>.AppHost.csproj -e <Environment> -o artifacts/deploy/<environment> --clear-cache --non-interactive --nologo
```

It builds the container image and starts the compose environment with `docker compose` for that environment. The AppHost declares one compose environment per environment name and pins each external port from its `appsettings.<Environment>.json` (staging and production run side by side, on `5081` and `5080` for example), each project resource sets `ASPNETCORE_ENVIRONMENT`, publishes only its `http` endpoint, and keeps the Data Protection keys in a named volume per environment, so a redeploy keeps cookies and antiforgery tokens valid. The Aspire CLI and every Aspire package of the AppHost are the latest stable and carry the same version (checked before the command runs; a mismatch stops with both versions). A `mobile` project has no AppHost: its `<App>.Api` is deployed only by a command you declare, and its `.aab` is never deployed. Docker must be running on the machine that deploys. Another target (a server over SSH, another cloud) is a command you declare in the same column; the GitHub Actions pipeline is #51.

**The database password of a compose environment (`dbpassword`, #153).** A database that runs as a container (`AddPostgres("postgres")`, `AddSqlServer("sql")`) takes its password from a secret parameter named `dbpassword`, declared only in publish mode, so a local `aspire run` keeps the password Aspire saves in the AppHost's user secrets and asks nothing. Without it Aspire generated a new password at every `aspire deploy` while the database kept the one its data was created with: from the second deploy on the container was recreated with the new password and the app failed with `password authentication failed` (measured on Aspire 13.6.1, with and without a named data volume). The bootstrap lists `Parameters__dbpassword` in `## Expected secrets` of `docs/infra.md` for every compose environment, as an environment variable (and in the GitHub environment of the same name, for the pipeline), and a deploy without it stops before a container is touched. The value is yours: random, at least 22 letters and digits (a `$` or a quote breaks the `.env` that compose reads), kept where the table says; Claude and the plugin never read, print or write it. One way to make one:

```powershell
$b = New-Object byte[] 16; [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); -join ($b | ForEach-Object { $_.ToString('x2') })
```

```bash
openssl rand -hex 16
```

The image applies the password only when the database is created, so a new value does not reach an existing database. To rotate it, change the password inside the database first (`ALTER USER`, `ALTER LOGIN` on SQL Server, signed in with the old one) and then the secret. An environment deployed before this keeps the password of its first deploy: use that one as the value if you still have it, otherwise the database volume has to be dropped and its data is lost (the container's own socket asks for the password too, so there is no way in from inside). `/agile:sync` tells a project in that state with the row "Database password" (example 14.77); it changes nothing.

**The second recipe: Azure Container Apps (`Deploy:Target`, #67).** For an environment that runs in the client's Azure, the same `aspire deploy` command provisions Azure Container Apps instead of a compose environment (the profile's section "Deploy recipe (Azure Container Apps + Aspire)" has the exact lines, measured on Aspire 13.6.0). The AppHost, written once at bootstrap with both shapes in one `Program.cs`, reads `Deploy:Target` from its own `appsettings.<Environment>.json`: `compose` (the default) or `aca`, so staging may stay on your host while production goes to the client's subscription, and a local run or a `compose` environment never needs an Azure sign-in. Quiz question 25b asks it once for each environment whose cloud is `azure` (recommended: `aca`). An `aca` environment needs Resource group and Region in its `## Cloud accounts` row: `/agile:publish <environment>` (`--plan` too) and the pipeline stop without them, with `staging: Deploy:Target is aca, but its ## Cloud accounts row leaves Resource group and Region blank; fill them (Azure__ResourceGroup, Azure__Location) — nothing ran` (a row that is not `azure` stops the same way), and `--plan` shows `Target: aca (Azure Container Apps), resource group <rg>, region <region>`; the check runs again at the tag, so a rollback to a tag whose AppHost says `aca` is checked against today's row. **The monthly budget (#77).** `## Cloud accounts` has a `Monthly budget` column after Region: a whole number, the ceiling of one month's spend in the billing currency of the subscription, or `none`. An `aca` environment must say one or the other (quiz question 25b asks it): a blank cell, or a table without the column, stops `/agile:publish` (`--plan` too) and the pipeline before the deploy command with `staging: Deploy:Target is aca, but its ## Cloud accounts row has no Monthly budget; write a whole number (the ceiling in the subscription's billing currency) or none — nothing ran` (a table without the column says `has no Monthly budget column; add it after Region and write a whole number ...` instead), and `80 USD`, `0`, `-5` or `80.5` stop with `staging: Monthly budget "80 USD" is not a whole number or none — nothing ran`; filled on any other environment (compose, aws, gcp, other, none) the cell is only reported as ignored. `--plan` shows `Budget: 80 per month on <rg> (billing currency of the subscription), alerts at 80 % and 100 % actual and 100 % forecast to Owner and Contributor` or `Budget: none (declared)`. After the deploy command, whatever its exit code as long as the resource group exists (a deploy that failed halfway may have created it), the plugin writes the budget `agile-monthly-ceiling` on that group with `az rest`, never from the AppHost: Azure refuses to change the start date of a budget and refuses a start date before the current month, so a budget declared in the AppHost would fail a month after it was created. The first write starts it on the first day of the current month and every later one keeps that date and changes only the amount, so raising the ceiling is typing the new number and publishing again (the pipeline does the same on a pushed tag, with the workflow's login). Azure e-mails Owner and Contributor of the resource group, with no address written in the repository, when the spend reaches 80 % or 100 % of the ceiling or the forecast reaches 100 %; the alert only warns, nothing is stopped or scaled, and Azure's cost data arrives 8 to 24 hours late, so it is never instant. The result says `Budget agile-monthly-ceiling on <rg>: created, 80 USD per month from 2026-10-01` (or `updated, ... (from 2026-10-01, unchanged)`); a failed write ends the run as `deploy ok; budget not written: <Azure's reason>` with a non-zero exit, the deploy and the release stand, and the missing alert is never silent. `none` writes nothing and deletes nothing: a budget left from an earlier ceiling is named by the plan (`a budget agile-monthly-ceiling still exists on <rg>; delete it by hand if the environment should have none`). Example 14.58. What it creates: a Container Apps environment with its own Azure Container Registry (Basic, the only registry it accepts) and a Log Analytics workspace, with no Aspire dashboard; a managed PostgreSQL (`Standard_B1ms`, 32 GB) or a serverless Azure SQL database with the free offer and auto-pause, both with Entra ID sign-in only, so no database password exists in the cloud (the app reaches PostgreSQL through `Aspire.Azure.Npgsql.EntityFrameworkCore.PostgreSQL`, and each app's managed identity becomes the server's administrator, so migrations run); one container app per web or API project, scaled from 0 to 1 replicas (1 for the host that runs background work) and with `ASPNETCORE_FORWARDEDHEADERS_ENABLED`, so it answers on https behind the ingress; and a storage account (`Standard_LRS`) whose blob holds the Data Protection keys, so a redeploy keeps every session. A secret stays an Aspire parameter (`Parameters__<name>`) and becomes a secret of the container app, with no Key Vault. The address is generated at the first deploy: you write URL and Check URL of `docs/infra.md` by hand from the command's output (or, for the client's own domain, write it as the URL: see "A custom domain" below). The pipeline runs the same command after installing `Aspire.Cli` at the version of `Aspire.AppHost.Sdk` of the AppHost the command names (it logs `Aspire CLI 13.6.0 installed (from Aspire.AppHost.Sdk of src/App.AppHost/App.AppHost.csproj)`, and stops with the reason when the version cannot be read or the install fails; the compose recipe gets the CLI too). The administrator of the client grants Contributor and Role Based Access Control Administrator on the subscription, because the recipe always creates role assignments. Deleting an environment is deleting its resource group and then its budget, both by hand in the client's subscription (the budget outlives its group, measured). The command for the budget is:

```powershell
az rest --method delete --url "/subscriptions/<subscription>/resourceGroups/<group>/providers/Microsoft.Consumption/budgets/agile-monthly-ceiling?api-version=2023-11-01"
```

```bash
az rest --method delete --url "/subscriptions/<subscription>/resourceGroups/<group>/providers/Microsoft.Consumption/budgets/agile-monthly-ceiling?api-version=2023-11-01"
```

No plugin command does either (parking an environment, `--park`, is another thing: it stops the database and the apps and deletes nothing), and the warning about taking a compose environment down with its volumes does not apply. Cost, retail for Brazil South from the ADR of the first app that used the recipe (an estimate, not a measurement): PostgreSQL about US$ 33 a month, the registry about US$ 5, Log Analytics by the GB ingested; the measured cost of one validation day in `eastus2` is recorded in the item (#67). An existing project receives the profile through `/agile:sync`; its own session changes its AppHost, never the plugin. Example 14.55.

**Parking an Azure Container Apps environment (`--park` and `--resume`, #75).** A staging environment that nobody uses between test windows still costs: the managed PostgreSQL server (about 12.4 a month) and a background host that runs at minimum 1 replica (about 11.8 before the monthly free grant). `/agile:publish <environment> --park` stops both and `/agile:publish <environment> --resume` brings them back; they work on any environment whose AppHost says `Deploy:Target` `aca`, whatever its name, on demand, from your machine with the same per-subscription login as a deploy. Each shows its plan first (the lines `--plan` prints, one per resource: `api: minimum 1 -> 0`, `postgres-xyz: Ready -> Stopped (mark agile-parked)`) and asks for your yes in one question card; any other answer runs nothing. Park sets every container app to minimum 0, keeping the old minimum in the tag `agile-min-replicas` of the app, and stops the PostgreSQL server of the environment's resource group after marking it `agile-parked=<UTC date>`, apps first so a background host never runs without its database; web and API projects already sleep at minimum 0, so in practice only a background host changes. Resume starts the server (about 3 to 4 minutes; the stop takes about 5), removes the mark, gives each app its minimum back and, when the environment's Check URL is filled, polls it as after a deploy. The registry, the Log Analytics workspace, the storage account and the database's disk keep costing: measured on 2026-10-06 in `eastus2`, a parked environment costs about 8.7 a month in USD plus what Log Analytics ingests. An Azure SQL database already pauses itself and has no stop, so `--park` only says so. Azure starts a stopped PostgreSQL server by itself after 7 days, so `--park` ends with the date of that start (`PostgreSQL <server> stopped; Azure starts it by itself on <date> unless it is resumed first`); the server keeps its mark, so running `--park` again parks it again. A deploy of a parked environment, by `/agile:publish` or by the pipeline, starts the database first (`staging was parked: PostgreSQL postgres-xyz started (3 min 27 s) before the deploy`) and removes the mark, because Azure refuses to change a stopped server and the deploy would end half done; the apps then get the AppHost's minimums from the deploy itself (`--plan` says `staging is parked: the deploy starts PostgreSQL <server> first (about 3 to 4 min)` and starts nothing). A container app or a server that Azure still answers as busy is retried every 15 seconds for 5 minutes; after that the run stops naming the resource and what it had already changed, and every run is safe to repeat. Example 14.59.

**Reading what an environment has cost this month (`--cost`, #99).** `/agile:publish <environment> --cost` asks Azure Cost Management, with the same per-subscription login as a deploy, what the environment's resource group has cost since the first day of the UTC month, and prints one line: `staging (aca, rg-acme-staging): Spend this month: 42.10 BRL of 80 (52 %) — figures from Azure Cost Management lag 8 to 24 hours`. The currency is the subscription's billing currency, as Azure answers it; `of 80` is the row's Monthly budget (with `none` the line says `(no ceiling declared)`) and the percentage is rounded down. It is one read: no yes to give, and nothing is written (no tag, no file, no commit). It works on any environment whose AppHost says `Deploy:Target` `aca`; another one is refused (`staging is not an aca environment (Deploy:Target is compose) — nothing ran`). Azure documents that the figures lag the spend by 8 to 24 hours, so a group created today reads `0.00` and the line says so every time. The plan of an `aca` environment shows the same reading as `Spend:` after `Budget:`; when it cannot be read the line says `Spend: not read (<reason>)` and the plan goes on, because a rate limit must not block a release. The service accepts only a few queries a minute: a 429 answer is retried three times, then the command stops with `Azure Cost Management is rate-limited; try again in a minute`. A login or a role that cannot read cost gets Azure's own message. A deploy, the pipeline and `/agile:status` never read it. Example 14.60.

**Proving the database backup works (`--restore-drill`, #100).** A backup nobody has restored is a hope. `/agile:publish <environment> --restore-drill` takes the automatic backup that the recipe's PostgreSQL Flexible server already keeps (7 days by default) and restores it, to the present moment, into a separate throwaway server called `<server>-drill-<yymmddhhmm>`; checks that it comes up as the same server (version, SKU, storage and every application database); reads the data on it with `psql` when you have it on your PATH; deletes it, always, pass or fail; and prints one closing line: `Restore drill staging: PASSED (restored in 7 min 12 s, data read)`, `Restore drill staging: PASSED, data not read (psql not found on PATH)` (nothing was read, and it says so) or `Restore drill staging: FAILED: <reason>; the backup is not proven`. It works on an `aca` environment whose PostgreSQL server is Ready, on demand, from your machine with the same per-subscription login as a deploy; a parked one is refused (`postgres-xyz is Stopped: run /agile:publish staging --resume first`, because a stopped server makes no new backups), and Azure SQL is not covered yet (`restore drill covers PostgreSQL only; Azure SQL is #104`). It shows its plan first (the server, its size, the backup retention and the earliest restore point, the throwaway server it would create, and that it is billed per hour until removed), then asks for your yes in one question card: typing the command is not the yes, because it creates a billed resource; any other answer runs nothing. The restore takes from a few minutes up to a few hours (measured on 2026-10-07 on an empty `Standard_B1ms` of 32 GB in `eastus2`: 6 min 39 s in the drill, 7 min 12 s for a blocking `az` restore, and about a minute to delete the server; a restore carries the Entra administrators and the sign-in settings of the source, and starts about 40 s after the command is accepted, which the drill waits for); the command gives up after 60 minutes (`AGILE_DRILL_WAIT_MS` changes it), removes the server and says the backup is not proven. The data read signs in with a short-lived Entra token of your own login that goes to `psql` and nowhere else (it is never printed, logged or written), allows this machine's IP (an IPv4 address asked of api.ipify.org; `AGILE_PUBLIC_IP` replaces it) in the firewall of the throwaway server and makes you an Entra administrator of it; the source server, its firewall, its tags and the apps are only read, never written. For each application database it prints the tables with their exact row counts: a table of rows is evidence, not a verdict, so an empty table shows `0 rows` and still passes, and a table that takes more than 30 s is `not counted`. If a run crashes and leaves a `-drill-` server behind, the next plan lists it and the run removes it first; a delete that fails is the loudest line of the report, with the exact `az` command for you to run, and the exit code is non-zero. A deploy, the pipeline, `/agile:status` and the scheduled park never run a drill. **A restore is not a recovery.** A restore never overwrites the server: it makes a new one, in the same region, and does not copy the firewall rules, the server parameters, the logins or the database-level permissions. On the day the backup is needed, restore into a new server, repoint the app's connection to it and recreate those. Example 14.69.

**Parking on a schedule (`## Schedule`, #102).** Nobody has to type `--park` at the end of a window: the `## Schedule` table of `docs/infra.md` (Environment, Action, When, Timezone) says when the pipeline parks, resumes or re-parks an `aca` environment. Action is `park`, `resume` or `re-park`; When is a five-field cron whose minute and hour are numbers (`0 20 * * 1-5` is 20:00 on weekdays); Timezone is an IANA name such as `America/Sao_Paulo` (blank is UTC). `re-park` is the guard against Azure's 7-day start: it stops again a PostgreSQL server that is `Ready` and still carries the mark `agile-parked`, and leaves one you resumed on purpose (resume removes the mark). The plugin's `sync.js schedule` shows what the table renders and whether `.github/workflows/park.yml` matches it; `sync.js schedule write` writes `park.yml` (replacing it: the table is its source, so edit the table and write again, never the file) and `.github/scripts/agile-park.js`, and needs the deploy pipeline first (`sync.js pipeline write`); example 14.73 has the commands. `/agile:sync` lists the schedule as a missing capability when the table has rows and the project has no `park.yml`, or when they no longer match. The run logs in as the deploy pipeline does and checks the login against the environment's `## Cloud accounts` row; it asks nothing: the row on `main` is your yes, the run prints its plan, and then does it. GitHub fires a scheduled workflow on the default branch only, so add `main` to the deployment branches of each scheduled GitHub environment (the tag rule `v*` stays); an environment named `production` is never scheduled, because its required reviewers would hold the run. A run waits for a deploy of the same environment (it shares the deploy's concurrency group; GitHub keeps one pending run per group, so a newer run cancels the pending one, which may be a deploy), takes about 10 minutes of Actions time (two runs a weekday are about 220 minutes a month on a private repository), may be delayed by GitHub under load, and in a public repository stops after 60 days without activity. A failed run is red and GitHub's own notification is the only alert. Measured on 2026-10-07 in a private scratch repository: the `timezone:` key is accepted and `TZ=...` in front of a cron is not, two workflows with one concurrency group queue one behind the other, and the runner needs az's `containerapp` extension, which the job installs outside `AZURE_EXTENSION_DIR`. Example 14.73.

**A custom domain on the app (#76).** An `aca` environment answers on the client's own domain, with a free managed certificate and no step in the Azure portal, when the URL of the environment in `## Environments` of `docs/infra.md` is that domain (`https://app.client.com` or `https://client.com`); a blank URL, an IP address or the generated `...azurecontainerapps.io` address means no custom domain, and the deploy runs exactly as before. Before the command, `/agile:publish <environment>` (the pipeline does the same with its own login) reads from Azure what the domain needs (the app's verification code, its generated address, the environment's static IP) and resolves the client's DNS records over public DNS. A subdomain needs `CNAME app` pointing straight at the generated address (a proxy in between, like Cloudflare's orange cloud, blocks issuance and renewal) and `TXT asuid.app` with the code; an apex domain (`client.com`; the zone's own SOA record decides, so `client.com.br` is an apex too, never a subdomain of `com.br`) needs `A @` pointing at the environment's static IP and `TXT asuid`; a CAA record on the root, when there is one, must allow `digicert.com`. With a record missing or wrong the deploy runs without the domain and lists each one with the value it should have and what was found; you send the list to the client, who creates the records at their own DNS provider, and the next deploy goes on. On a first deploy the list comes after the command, because the values only exist once the app does. With the records right, the command gets the domain (`Deploy__CustomDomain`, which the AppHost reads the way it reads `Deploy:Target`: a value passed as an Aspire parameter would be saved by Aspire and come back on every later run, measured), and then the plugin has Azure issue the managed certificate under a fixed name (`mc-` and the host with hyphens), waits up to 10 minutes and binds it; every later deploy finds the certificate and keeps the binding. The result says `Domain app.client.com: added, certificate mc-app-client-com issued in 94 s and bound; https://app.client.com is live.` or `Domain app.client.com: certificate mc-app-client-com kept.`. A certificate still pending at the end of the wait ends the run as `deploy ok; certificate pending: the next deploy binds it` (exit 0: nothing is wrong, Azure keeps trying), and one that Azure refuses as `deploy ok; domain not bound: <reason>` with a non-zero exit (the deploy is not undone). `--plan` names the step the deploy will take. When the Check URL is on the domain and the domain is not bound yet, the generated address with the same path is polled instead, and the run says so. The generated address keeps answering, and redirecting from it is the app's business. The app must stay reachable while Azure issues and renews the certificate: parking (minimum 0) keeps the address answering, but Azure's own stop action would break the renewal. When the plugin cannot read the state of the domain (public DNS does not answer, `az` fails or lacks its `containerapp` extension, the host has no DNS zone, a certificate of that name belongs to another host) the run stops before the command with the reason, because a deploy without the domain would take a live one off the app; blank the URL of the environment to deploy without it. An AppHost written before this feature has no domain code: the step says so, names the profile section to copy it from, and the deploy runs without the domain. A certificate that failed earlier (the records were wrong when Azure tried) is deleted and created again by the next deploy that finds the records right, and `--plan` says `recreates`. One domain per environment, on the one public container app (none or two with external ingress stops the domain step, naming them); the plugin does not create the DNS records. Example 14.64.

**The third recipe: containers without Aspire (#151).** Some apps are developed with Aspire for its logs, metrics and traces and still want the server to run only the app's own images and a compose file kept in the repository. The recipe is chosen per environment by its Deploy command, like the other two (there is no flag on `/agile:publish`), and only for a host that runs Docker: Azure Container Apps without Aspire is not a recipe. The profile's section "Deploy recipe (containers without Aspire)" has the exact lines, measured on .NET SDK 10.0.401 and Docker 29.8.2. The Deploy command is one line that works in cmd.exe and in a Unix shell, with `-t:` as the switch (in Git Bash `/t:` is rewritten as a path), then the compose file of the repository:

```powershell
dotnet publish src/<App>.Web/<App>.Web.csproj -c Release -t:PublishContainer && docker compose -f deploy/compose.yaml --env-file deploy/<environment>.env up -d --wait
```

```bash
dotnet publish src/<App>.Web/<App>.Web.csproj -c Release -t:PublishContainer && docker compose -f deploy/compose.yaml --env-file deploy/<environment>.env up -d --wait
```

Each deployable project sets `ContainerRepository` and `ContainerImageTag` (`$(Version)`) in its `.csproj`, so the image carries the version of the release (the SDK would tag `latest`). One `deploy/compose.yaml` serves every environment: its name includes the environment, so staging and production on one host never replace each other, and each has its own database volume and keys volume. `deploy/<environment>.env` is committed and holds only what is not secret: `ASPNETCORE_ENVIRONMENT` and `HOST_PORT`, the port of the environment's URL. **The database password is a fixed secret**: `DB_PASSWORD` is listed in `## Expected secrets` as an environment variable and is the same at every deploy (random, 22 or more letters and digits, generated and kept by you, never written to a file); `/agile:publish` stops before anything runs when it is unset, and compose refuses by itself when you run it by hand. Logs go to the console (read them with `docker compose logs`) and no OpenTelemetry endpoint is set: a project that wants export sets `OTEL_EXPORTER_OTLP_ENDPOINT` in `deploy/<environment>.env`. The AppHost is untouched and stays the local tool (`aspire run`). An environment that already ran the Aspire compose recipe starts on new volumes when it switches: its data and signed-in sessions do not carry over, and moving them is by hand. The compose `down` with its volumes option is never run.

## 6. Changing your mind

- **Before approval:** change anything. It is just conversation.
- **During build:** `/agile:change` adds a **change note** to the feature file (what changed, why, which acceptance criteria are affected). You re-approve only those criteria, and their rows in `## Criterion → test` go back to `pending` (a new criterion gets a row, a dropped one loses its row). Work continues.
- **Stuck on something outside the work:** `/agile:change <id> --block "<reason>"` sets the status `blocked` and `--unblock` brings the item back to the status it left (14.67).
- **After ship:** it is a new feature or a bug, captured with `/agile:idea`.
- **Wrong premise found** (for example, "the screen already has this field" and it does not): Claude stops and asks before coding around it.

## 7. The feature file

`docs/features/F-<number>-<slug>.md`, one per feature, created from `docs/agile/templates/feature.md`. Bugs use a shorter file, `docs/bugs/B-<number>-<slug>.md` (template `bug.md`): what happens, expected, confirmed cause (with every duplicate of a business rule), fix (which occurrences now, which deferred), one regression test per occurrence fixed and validation script.

Epics live in `docs/epics/<slug>.md` (features table, order, first release cut) and discussions in `docs/discussions/D-<n>-<slug>.md`. Screen mockups go to `docs/features/mockups/`.

A rule that protects a minimum ("never leave the last manager without a replacement") is written as a before-and-after — "there was at least one before and none after" — so it does not also block a case where the hole already existed.

Main sections of the feature file:

```markdown
---
feature: F-<number>
status: refining | approved | building | validating | blocked | done
board: <work item id>
---
# <Feature name>

## Goal
## Users and use cases
## Business rules
## Screens and API
## Acceptance criteria
## Criterion → test (written by /agile:build)
## Decisions (date — decision — reason)
## Out of scope
## Open questions
## Change notes
## Validation script
```

`## Approved list` is an optional section, after `## Screens and API`: it exists only when you approved a list in the chat (26 site sections, five export formats, ten error codes). It holds every element, numbered, one per line, in the approved order and with your edits applied; criteria and decisions point to it by name. The build and any later reader take the list from the file, never from the refinement chat.

## 8. Definition of done

- [ ] Acceptance criteria met and covered by tests.
- [ ] Build with no new warnings; affected tests green.
- [ ] Every UI string localized in pt-BR, pt-PT and en.
- [ ] Validated on screen by you.
- [ ] Full test suite green before merge.
- [ ] Generated technical docs up to date — the docs command and its check, `gate.js docs` (DocGen, declared or implicit), when the project has them.
- [ ] App manual updated in the profile's languages (three by default; `web-api`: pt-BR and en).
- [ ] Board updated; the feature file reflects what was decided.

## 9. Quality gates

Hooks run outside the model. They are Node scripts (no bash) and do nothing in a repository without a .NET project.

| When | What happens |
|---|---|
| **Session start** | Shows the branch, the item in progress, the backlog head, approved files with open questions, and uncommitted work in **every** worktree. When the session comes back from a compaction or a resume, it adds the resume block below. |
| **Compaction** (`/compact`, or automatic when the context fills) | `status.js compact` prints the resume block, and Claude Code adds it to what the summary must keep: for each item in `building` or `validating`, its id, title, status, branch and worktree, its last `## Paused` note and the next step (from the note, else from the status), and a fourth line, `Criteria with no test: AC2, AC4 (2 of 5)` read from the item file (`none (5)` when every row is filled, `no section` for a bug or an item built before the section existed). Nothing is printed when no item is in progress. |
| **Every commit** | A guard refuses a `git commit` on the main branch in two cases: an item branch (`feature/F-<n>`, `bug/B-<n>`) is unmerged and checked out nowhere — the sign that an IDE switched the branch behind the session — or the commit carries an item file that is not `done` (refining, approved, building, validating), which belongs on the item branch. Claude tells you and switches back; if the commit really belongs on the main branch, you say so and Claude repeats it ending with the comment `# agile:main-ok`. |
| **Every shell command** | A second guard warns before a Bash or PowerShell command writes a repository file through the command's own text instead of the Write and Edit tools — a heredoc, `echo`/`printf`, a PowerShell `Set-Content`/`Out-File`/`Add-Content`, a Python `open(..., 'w'/'a')`, an inline Node or `.js` write whose literal carries a backslash, an escaped quote or an embedded newline, or a `sed -i` on a tracked file — or edits a GitHub issue or PR with an inline `--body`/`-b` instead of a whole one from a file. It exits 2 naming the rule and the file or command; a path outside the repository (temp, the session's scratchpad) and the plugin's own generated files (`warnings-baseline.json`, `.claude/agile/sync.json`, `scripts/delivered.json`, `.claude/agile/sync-base/**`) are silent. With your yes, Claude repeats the command ending with the comment `# agile:literal-ok`. |
| **Every file tool of a plugin agent** | A third guard refuses a Read, Glob, Grep, Write, Edit, MultiEdit or NotebookEdit made from inside one of the plugin's agents (`frontend`, `ux-designer`, `system-design`, `architect`, `reviewer`) when its target — the file, or the folder of a search; a search with no folder runs in the session's folder — is in the main checkout of your project while an item worktree (`feature/F-<n>`, `bug/B-<n>`) exists. It exits 2 naming the item worktrees and telling the agent to repeat the call with one of them as the path. Claude Code names these agents `agile:<name>` (measured); Claude's own calls, built-in agents (Explore, general-purpose), a project's own agent with the same bare name, a target inside a worktree or outside the repository, a project with no item worktree and a repository with no `docs/agile/` are never refused, and any doubt lets the call through. The only bypass, with your yes, is restarting Claude Code with `AGILE_HOOKS=off`, which turns off every agile hook (the commit guard and the gate too). |
| **Every edit** | Nothing is built. The edited file is only remembered, under the git root it belongs to — so an edit inside a worktree is gated in that worktree, not in the folder where the session started. |
| **End of turn** (only if code changed, or a document a test cites) | Rebuilds the changed projects (`--no-incremental`) and runs only the test projects that reference them, directly or indirectly. It never runs the whole suite. |
| **Ship** | `gate.js ship`: full rebuild, full suite, architecture tests; then no tracked file may be left changed (a generated file the run rewrote is committed with the item), and the evals run when the project has them: `evals/compare.js` compares the result with the baseline and its exit code is the verdict (a drop or a missing case fails the ship; a partial run, or a run of another model or ablation than the baseline's, is "not measured" and stops it). After the app manual, `gate.js docs` runs the docs command the repository declares — or, with nothing declared, the single `tools/*.DocGen` it finds (below). Before the docs commit Claude reads the item's diff under `docs/` (outside `docs/manual/`) and adds a glossary row, on the item branch, for each technical term it brings with no row; the report says `Glossary: N rows added: <terms>` or `Glossary: nothing missing`. |

Details:
- **New warnings only.** Warnings are compared with `.claude/agile/warnings-baseline.json`, a committed file. Existing warnings do not fail the gate; a new one does, listed with file, line and message. The baseline is rewritten only by a green ship (or by `gate.js baseline`, with your yes).
- **The verdict is the last line.** Every report ends with `agile gate GREEN`, `agile gate RED: <what failed>` (the new warnings, the failing tests, the blocked build) or `agile gate SKIPPED: <why>` (nothing was edited, no solution, not a git repository — when no solution is found, it says to name one as `solution` in `.claude/agile/build.json`), or `agile gate OFF: <why>` in a repository whose `build.json` says `engine: none`. As a hook, a turn with no code change stays silent. Run by hand, `stop` cannot see the hook's marks (they belong to the session), so it asks git which code files changed since the main branch `CLAUDE.md` names (`develop` if that is what it says; it also tries `origin/<name>`, and says `main branch `main` assumed: CLAUDE.md names none` when nothing is named, or `the main branch `<name>` named in CLAUDE.md is not in git` before falling back to the last commit) — committed, uncommitted and new — builds and tests those, and always prints its verdict. It never reads standard input: only the Stop hook, called as `gate.js stop --hook`, reads the hook event. Before 0.0.68 a stop by hand waited forever on a standard input left open, as in Claude's Bash tool. Claude saves the whole output to a file and quotes from it, never filtering it with `grep`, `head` or `tail`: a filtered view once hid the only list of new warnings.
- **Wide changes.** If a change reaches more than 6 test projects, only the ones that reference it directly run; the rest waits for ship (`AGILE_GATE_MAX_TESTS`). A change to `.props`, `.targets` or the solution builds the whole solution and leaves the tests for ship.
- **Solution lookup.** The solution is searched at the git root and one folder down (`repo/App.slnx`, `src/App.sln`).
- **What the turn gate sees.** A code file is `.cs`, `.vb`, `.fs`, `.fsi`, `.razor`, `.cshtml`, `.xaml`, `.resx`, a project file (`.csproj`, `.vbproj`, `.fsproj`) or a build file (`.props`, `.targets`, `.sln`, `.slnx`); an edit to anything else is not remembered and starts no build, except a document: a `.md` edit is remembered, and the test projects whose `.cs`, `.vb` or `.fs` sources name the file (`glossary.md` in a test that reads `docs/glossary.md`) are built and run (0.31.0). No test cites it: nothing runs, the hook stays silent and a run by hand says `agile gate SKIPPED: no code or cited document changed since ...`. The project that owns a file is the nearest `.csproj`, `.vbproj` or `.fsproj` above it, and the project graph holds every one of those under the root, so a C# test project that references a VB.NET library runs when a `.vb` file changes. A test project is one whose file carries `Microsoft.NET.Test.Sdk`, `Sdk="MSTest.Sdk` (with or without a version), the `MSTest` metapackage that `dotnet new mstest` references (0.31.0), `<IsTestProject>true` or `xunit`, whatever its language. Until 0.0.90 a `.vb` or `.fs` edit was invisible to the turn gate (SKIPPED, while `ship` built and tested the whole solution), and a project on `MSTest.Sdk` was built but never tested. DocGen is still looked for as a `tools/*.DocGen` folder with a `.csproj`: it is the plugin's C# template. `.sqlproj` and `.fsx` are not code for the gate.
- **The solution's SDK.** Every `dotnet` call runs from the solution's folder (the git root when there is none), so the `global.json` beside the solution chooses the SDK and the test runner, exactly as it does for someone building in that folder. Every report opens with `sdk <version> (<folder>)`; `sdk unknown` means asking `dotnet` for its version failed there (a pinned SDK that is not installed), and the build failure that follows says why. The baseline records the SDK it was taken with (`"#sdk"`). When a later build uses another one, the report adds `baseline taken with <a>, this build used <b>: warning counts may differ; ...`. That line is information, not a failure: retaking the baseline is your call. With the Microsoft.Testing.Platform runner, the report quotes its totals (`Test run summary`, `total`, `failed`, `succeeded`, `skipped`), and the gate passes a solution to `dotnet test` with `--solution` and a test project with `--project` (SDK 10.0.1xx refuses a solution after `--project`).
- **An adopted repository.** A code base that existed before the workflow (its `CLAUDE.md` says `Profile: adopted`, written by hand or by another plugin, such as legacy-lens's adoption) records how it is really built in `.claude/agile/build.json`: `engine` (`dotnet`, `msbuild` or `none`), `solution` (a path from the root), `scope` (the projects worth building when the whole solution is not), `testCommand`, `notes` and, under `msbuild`, the optional `msbuildPath` and `restoreCommand`. The gate reads `solution`, so a solution deep in the tree is still built, and `engine: none` turns it off with the reason in `notes`; `scope` is for people and is not read. The engine is trimmed and case does not matter, and a value outside the three is RED (`agile gate RED: unknown engine "MSBuild2" in .claude/agile/build.json (legal: dotnet, msbuild, none)`) instead of quietly behaving like `dotnet` — which is how `msbuild` went unhonored until 0.0.69. `/agile:sync` refreshes rules, templates and the workflow of such a repository but leaves its profile alone: there is no plugin profile to refresh it from.
- **`engine: msbuild`.** For a solution the .NET SDK cannot build — typically an old project type whose targets only Visual Studio ships, such as an ASP.NET web application importing `$(VSToolsPath)\WebApplications\Microsoft.WebApplication.targets`, where `dotnet build` stops with `error MSB4019`. The gate then drives MSBuild.exe instead of the `dotnet` CLI; the warning parser, the baseline, the locked-output hint and the RED/GREEN verdict are the same. What differs:
  - **Finding MSBuild:** `msbuildPath` from `build.json` (absolute, or from the repository root) when the file is there, then `msbuild` on the PATH (a Developer Command Prompt puts it there), then `vswhere.exe` at its fixed location under `Program Files (x86)`, which ships with every Visual Studio install. A declared `msbuildPath` that exists is used as it is: the gate never falls through to a different MSBuild than the one you named. When none works, `agile gate RED: MSBuild not found`, listing every place it looked. `vswhere` is Windows-only, so off Windows there are two places, not three.
  - **The report head** is `msbuild 18.10.1.42706 (.)` instead of `sdk <version> (<folder>)`, and the baseline keeps it in the same `#sdk` key as `msbuild 18.10.1.42706`. A `dotnet` baseline still stores the bare SDK version, so no baseline written before 0.0.69 has to be retaken.
  - **Restore**, on `ship` and `baseline` only: the build carries `-restore`, unless `restoreCommand` is declared, in which case that runs first instead — `msbuild -restore` does nothing for `packages.config`, which is what a legacy repository usually has. A `restoreCommand` that fails is `agile gate RED: restore failed (<command>)`, and nothing is built.
  - **Tests** are the `testCommand`, run whole through the shell, at `ship` only: `dotnet test` cannot reach a .NET Framework test assembly, and the gate's test-project detection does not recognise a `packages.config` MSTest or NUnit project. The turn gate builds the affected projects and says `tests skipped: engine msbuild runs the whole suite at ship`; `baseline` never ran tests. With no `testCommand`, the ship says `tests skipped: no testCommand in .claude/agile/build.json` and the build alone decides the verdict — a missing suite is debt the adoption recorded, not a gate nobody can clear. A `testCommand` that fails is RED; one that runs longer than 1800 seconds (`AGILE_TESTCMD_TIMEOUT`) is RED as a timeout.
  - Since 0.0.69 `testCommand` is **executed**, not just documentation: it has to run unattended, never opening an IDE or waiting for a key.
- **A declared docs command.** `build.json` may also carry `"docs": { "command": "...", "check": "...", "paths": ["docs/legacy/inventory"] }`, written by whoever prepared the repository (legacy-lens's adoption declares its inventory map there), so a living code map is refreshed with every item. At ship, after the app manual, `gate.js docs` runs `command` and then `check` (optional) through the shell, from the root of the item's worktree, whatever the `engine`. `command` and `paths` are required. It ends with `agile docs GREEN: <n> file(s) changed under <paths>`, and those files go into the item's docs commit. `agile docs SKIPPED: no docs command declared` means there is no `docs` block and no DocGen either, and the ship goes on as before. `agile docs RED: <what failed>` stops the ship like a red gate: a command or check that failed, is not installed, or ran longer than 600 seconds (`AGILE_DOCS_TIMEOUT`), a field that is missing, or a file changed outside `paths`. Such a file is listed and never committed.
- **The implicit DocGen command** (0.0.72). A project with DocGen has a docs command whether it declared one or not. With no `docs` block, `gate.js docs` looks for `tools/*.DocGen` folders holding a project file: exactly one is run as if it were declared — `dotnet run` on the `tools/<App>.DocGen` project, then again with `--check`, with `paths: ["docs/architecture/"]` — and the run prints `implicit DocGen docs command: declare it with /agile:sync (tools/<App>.DocGen)` just before the verdict. That closes the window between a project getting DocGen and declaring it: before 0.0.72 the ship ran DocGen in a step of its own, and a project with the block ran it twice. **Several** `tools/*.DocGen` and nothing declared is RED naming them (`declare which one in .claude/agile/build.json`): running the wrong one would leave the other stale in silence. A declared `docs` always wins, and DocGen is not run behind it. `/agile:sync` offers the block as a plan row (section "Updating the plugin", example 14.10); until then every ship repeats the line.
- **The security scan** (`scan.js`, #83). The end of the build (step 17b, before the validation script) and the ship (step 4d, after the gate) run it; nothing runs in the turn gate or in a hook. Three tools, each on its own line: the `dotnet` SDK's vulnerability report (`list package`, with its vulnerable and transitive options, read from its JSON, because it exits 0 with a vulnerability present, and it sees what the warnings baseline lets pass in silence), `gitleaks v8.30.1` over the files and the git history with `--redact`, and `semgrep 1.179.0` with `p/csharp`; the last two run through Docker (images pinned, never `latest`, the code mounted at `/src` because semgrep's image aborts without it). gitleaks and semgrep read a copy of what git tracks or does not ignore, so a local `appsettings.local.json` or `bin/` is never a finding, and a worktree is scanned from a clone (inside a worktree gitleaks answers "no leaks" after reading 0 commits). Each finding is triaged by severity into `docs/security/findings.md`, committed, one row per finding (id, severity, tool, rule, where, status, reason, who, date) and never a secret value; raw reports stay in the git-ignored `.claude/agile/scan/raw/`. The id hashes tool, rule, file and line: a finding whose line moves is a new open row, never a second finding hidden behind an accepted one. Severity is the tool's own: a gitleaks finding is critical, a NuGet advisory keeps its level (Moderate is medium), a semgrep finding takes `metadata.impact`, else `severity` (ERROR high, WARNING medium). The verdict is the last line: `agile scan GREEN`; `agile scan BLOCKED: <n> critical, <m> high open` (exit 1: a critical or high stops the ship like a red gate, medium and low are only listed); `agile scan PARTIAL: <tools> did not run (<why>)` (exit 2: Docker missing or stopped, a missing image, no NuGet source, a gitleaks history that read 0 commits, semgrep erroring on every file, a `scan.tools` that is empty or misspelled; you decide whether to go on); `agile scan SKIPPED: no .NET solution` (exit 0; gitleaks and semgrep still run). The plan names each image and its size (77 MB and 1.56 GB) and the first pull needs one yes from you; `scan.tools` in `build.json` (default all three) turns a tool off, and `gitleaks off by build.json` is not PARTIAL. A row keeps its status across runs; an open row the tools stop reporting becomes `fixed`, and comes back open if the finding returns; a triaged row comes back open if its severity rises. You triage with Claude, one finding at a time (no reason, no change; a false positive needs a concrete reason), never an automatic baseline. A secret found only in the git history stays open until you say the credential was rotated. A blocking finding is fixed like a bug: a test that fails, the fix, a rescan without it and the suite green, then the `reviewer` agent checks the diff; two attempts at most, then Claude asks you. A fixture with a fake secret uses gitleaks's own `gitleaks:allow` comment or `.gitleaksignore`, visible in the diff. Example 14.72.
- **Locked build output.** A running app host, preview or debugger keeps the DLLs open, and so does another build of the same folder (the compiler then says `CS2012`, `MSB3021`, `MSB3026`, `MSB3027` or `MSB3061`). The gate waits 15 seconds (`AGILE_GATE_LOCK_RETRY_SECS`) and builds that project once more; the report says `after one retry on a locked output` when that was enough (a red report that comes later keeps a line `<project> was built again after a locked output (15 s)`). Still locked, it reports "build blocked" and names both causes — another build or test in this folder (an IDE, a terminal, another session) or an app host, preview or debugger started from it — instead of a plain build failure. Claude stops whatever it started before the turn ends, and asks you to close yours.
- **One gate at a time per folder** (0.31.0). A run that builds (`stop`, `ship`, `baseline`) takes a lock for its git root, a small file (`agile-gate.lock`) in the git folder of the root, so a worktree has its own and a hook and a shell command see the same file; it names the mode and the process. Only a turn that really builds takes it. The Stop hook never waits for it: with the root held it says `agile gate deferred: ship (pid 4812) holds <root> since <time>; if that pid is not a gate run, delete <the lock file>; the marked files build on the next turn`, exits 0, keeps the marks and counts neither a red nor a green (when another root of the same turn is red, the deferral is listed under it). `ship`, `baseline` and `stop` by hand wait up to 120 seconds (`AGILE_GATE_LOCK_WAIT`) and then end `agile gate RED: gate busy: <who holds it>`. A lock whose process has ended, or that is more than two hours old, is taken over (a killed hook leaves its file behind, which is why the message names it); a lock file no one can read yet counts as held while it is a few seconds old, and a lock the gate cannot make never stops a run. Before this, a ship in the background and the Stop hook built the same `obj/` folders and two of three runs failed with `CS2012`.
- **The whole output of a RED** (0.31.0). A red build, restore or test run saves its raw output in `<temp>/agile-canary/gate-logs/<date>-<time>-<mode>.log` (the newest 20 are kept) and the verdict names it: `agile gate RED: tests failed (App.slnx): Fails1, Fails2 — full output: <path>`. The report still shows the last 25 lines; the file holds the message and the stack of every failing test. A folder that cannot be written is said (`the full output could not be saved: ...`) and the verdict stays as before.
- **Not this item's** (0.31.0). When the output names the failing test assemblies and every one of them is a test project that reaches none of the projects the branch changed since the main branch, the report adds, before the verdict, `every failing test is in tests/Other.Tests/Other.Tests.csproj, which reaches none of the projects this branch changed (src/Lib/Lib.csproj): probably not this item's; run it on the main branch to confirm`. It says nothing when it cannot tell, because a wrong silence is cheaper than blaming another item for a failure the item caused: a global file (`.props`, `.targets`, the solution) is in the diff, nothing changed, an assembly is not a project of the graph, a changed document is one a failing project cites, or a data file (json, sql, a fixture) changed in a failing project or in no project.
- **In the ship and the build** (0.31.0). The ship runs `gate.js ship` in the foreground, never in the background; its merge needs an `agile gate GREEN` printed by a `gate.js ship` run of that ship after the last commit that changed code, never one quoted from the building session; when a test project reads the version (it cites `Directory.Build.props` or `Version`), the ship runs `gate.js ship` again after the version bump and before committing it, because such a test goes red on the first bump. The build reads `git status` before every commit and stages files by name, never the whole tree at once.
- **No .NET SDK.** With no `dotnet` on the PATH the report says `the .NET SDK was not found on the PATH (...)` and the verdict `agile gate RED: .NET SDK not found`, instead of `build failed`.
- **Honest counts.** An incremental build skips the compile of a project that did not change, and MSBuild does not repeat the warnings of a skipped compile. So every gate build is a `--no-incremental` rebuild of what it measures, at the turn as at ship. Before 0.0.67, a new warning found in one turn disappeared in the next, and the gate went GREEN with nothing fixed. The rebuild costs a few seconds per turn: it was measured at +0.6 s and +2.2 s on two small solutions, and each `built` line shows it. Claude quotes a warning count only from the gate or a `--no-incremental` build, and builds again after `git stash` or a branch switch before running tests: `--no-build` would run the other tree's binaries.
- **Hung tests.** A test that runs for more than 120 seconds (`AGILE_GATE_HANG_TIMEOUT`) counts as hung: the run fails with its name instead of blocking the turn for minutes.
- **No loops.** After 3 red gates in a row the turn ends and you see the failure; the pending check stays for the next turn.
- **Known limit.** Only edits made with the editing tools are tracked. Changes made by a shell command (`dotnet format`, a merge) are caught at ship.
- `AGILE_HOOKS=off` disables the hooks for a session.
- `AGILE_TESTCMD_TIMEOUT` (1800 s) caps the `testCommand` of an `engine: msbuild` repository, as `AGILE_DOCS_TIMEOUT` caps the docs command.
- **A command past its limit is ended whole** (0.37.0, #150). The docs command and check, the `restoreCommand` and the `testCommand` run inside a small helper (`scripts/run-limited.js`) that handles the deadline while the shell is still alive and ends every process the command started (`taskkill /T` on Windows; on Linux and macOS the command runs in its own process group and the group is killed). Before, only the shell died: the build tool it had started kept running, held the folder, and the ship's worktree removal then failed. A `restoreCommand` past its limit now says `restore ran longer than 1800 s (AGILE_TESTCMD_TIMEOUT)` instead of `restore failed`. If the tree could not be ended, the verdict is the same RED and one line is added: `a process the command started may still be running`.
- `AGILE_GATE_LOCK_WAIT` (120 s) is how long `ship`, `baseline` and `stop` by hand wait for another run of the same folder; `AGILE_GATE_LOCK_RETRY_SECS` (15 s) is the pause before a locked build is tried once more.

## 10. Board

`CLAUDE.md` has a `Board:` line: GitHub Issues + Projects (`gh`), Azure Boards (`az boards`), or none (then `docs/agile/backlog.md` is used). Mapping: epic → feature. No tasks per role. The feature file keeps the board id; the merge closes the work item.

Closing the issue is not trusted alone: at ship, after closing, Claude also sets the board's Status field to Done explicitly and reads it back, because a project's own automation for that can miss (seen once, cause unknown). A read-back that still shows something else is retried once, then reported with the exact command to fix it by hand; the ship does not stop or undo the merge over it — the file already says `done`. An item not yet on the project board is added first. Azure Boards gets the same read-back on `System.State`.

A duplicate cancelled at `/agile:refine` is neither deleted nor Done: GitHub closes the issue with `gh issue close`, naming the other issue as the original, and archives its project item, Azure Boards sets `Removed` with a comment naming the other item, and with no board the backlog row is struck through with `cancelled, duplicate of <id>`.

An issue body is only ever replaced whole. `gh issue edit` with an inline body replaces the entire body with what it is given, so a partial text there silently deletes the rest of the item (it happened in legacy-lens B-1). Claude writes the full item text to a file, sends it with the command below, and reads the body back to compare:

```powershell
gh issue edit <id> --body-file <file>
```

```bash
gh issue edit <id> --body-file <file>
```

An Azure Boards description is likewise always sent whole, from a file.

A GitHub Projects board with no "Ready" option no longer just gets a note about it: the first time `/agile:refine` mirrors a `refining` or `approved` item, Claude creates it — right after whatever option currently sits first (usually "Backlog"/"Todo"), so the flow stays Backlog → Ready → In Progress → Done. GitHub only lets you replace a Status field's whole option list, never add one option to it, and replacing the list gives every option a new id, which silently unsets the Status of every other item on the board — that cost 30 items their status by hand once (2026-09-27). Claude avoids it by reading every item's current Status **by name** first, replacing the list, then re-applying each captured name to its new id; an item that had no Status keeps having none. Any other missing option — "Ready" itself once `/agile:build` or `/agile:ship` run, or a missing done option — is still only reported, never invented.

## 11. Languages and the app manual

- Code, docs, commits and identifiers are in English.
- The app ships in **pt-BR, pt-PT and en** from day one: no hardcoded UI strings, one resource file per module per language, `IStringLocalizer`. Adding a language means adding resource files only.
- The **app manual** lives in `docs/manual/<locale>/` and is updated when each feature ships, in the locales the profile names: pt-BR, pt-PT and en by default, pt-BR and en for `web-api`.
- `web-api` has no screens: its responses carry codes only, and the three-language resource files (`Resources/`) appear with the first feature that sends text to people, such as an e-mail. The lines about UI text, the language switch and mockups do not apply to it.
- Only the conversation between you and Claude is in Portuguese.

## 12. Architecture profiles

The quiz picks one profile; `CLAUDE.md` points to it.

| Profile | When |
|---|---|
| `modular-monolith` | One deployable, several business modules with clear boundaries |
| `monolith` | One deployable, one business area |
| `web-app` | Mostly screens over simple data; thin API or server-side UI |
| `website` | A public site for visitors (search engines, content edited by the owner) that may grow into an app: `web-app` plus a public layer |
| `web-api` | An app that is only an API for the owner's own clients (mobile, a single-page app in other repositories) and, when the bootstrap says so, machine clients: `monolith` without `Web`, plus consumer authentication, per-consumer rate limiting and a published contract |
| `microservices` | Independent deployables owned separately; only with a real reason |
| `mobile` | Native or MAUI client, with its own API profile, or on an existing API in another repository (`Backend: external`) |
| `desktop` | Standalone XAML client on the user's machine (WinUI 3, Avalonia 12 or .NET MAUI), behind an API or reaching the database directly |

Each profile defines the folder layout, where business rules live, the test strategy and its time budget, and the architecture tests. The profile files are in `profiles/` in the plugin; bootstrap copies the chosen one to `docs/agile/profile.md`.

What every profile shares:
- **Simple code.** CRUD is a thin vertical slice (endpoint or page → `DbContext`): no repository, no mediator, no mapping library. A rule lives in its entity, or in the feature handler when it needs data. Structure is added on the second use.
- **One project per module.** In `modular-monolith` a module is one project plus a `Contracts` project. A module with a rich domain can use the Clean Architecture variant (five projects: Domain, Application, Infrastructure, Api, Contracts); Claude suggests it with a reason, you decide per module, and an ADR records it.
- **Time budget.** Unit < 30 s, integration < 2 min per test project, full suite < 5 min (smaller for `web-app` and `monolith`, < 10 min for `microservices`). **One PostgreSQL container per test project**, never one per test class.
- **Architecture tests** guard the boundaries and the English vocabulary in every assembly, and every rule of absence is paired with a rule of presence, so an empty assembly fails. A layout test keeps every project in the folder its profile assigns.

**Folder layout per profile.** Bootstrap creates it, and every project a feature adds later goes into the folder its kind has here; the solution folders mirror the disk folders, and a layout test fails when a project lands elsewhere. `<App>` is your app's name.

`modular-monolith` — hosts, technical building blocks and business modules in separate groups:
```
src/
├── Hosts/
│   ├── <App>.AppHost/                local orchestration (Aspire)
│   ├── <App>.ServiceDefaults/        telemetry, health, resilience
│   ├── <App>.Api/                    host only: composition, auth, OpenAPI
│   └── <App>.Web/                    Blazor UI, typed clients to the API
├── BuildingBlocks/
│   ├── <App>.SharedKernel/           Entity, TenantEntity, Result/Error, AppJson
│   └── <App>.<Block>/                Persistence, Email, Storage, Ai — by the first feature that needs it
└── Modules/
    └── <Module>/
        ├── <App>.<Module>/           one project per module (or five with Clean Architecture)
        │   ├── Features/<Feature>/   vertical slices
        │   ├── Domain/               entities with rules
        │   ├── Data/                 DbContext, own schema, migrations
        │   └── Resources/            .resx in en, pt-BR, pt-PT
        └── <App>.<Module>.Contracts/ what other modules may see
tests/
├── Hosts/<App>.Web.Tests/            bUnit
├── BuildingBlocks/<App>.<Block>.Tests/
├── Modules/<Module>/<App>.<Module>.Tests/
├── <App>.ArchitectureTests/          boundaries, vocabulary, layout
└── <App>.Testing/                    shared test helpers
```

`monolith` — one deployable with the business code inside it:
```
src/
├── <App>.AppHost/                    optional
├── <App>.Api/                        host + business code
│   ├── Features/<Feature>/
│   ├── Domain/
│   ├── Data/                         one DbContext, migrations
│   ├── Contracts/                    records shared with the UI
│   ├── Common/                       Result/Error, AppJson, helpers
│   └── Resources/
└── <App>.Web/                        Blazor UI, typed clients to the API
tests/
├── <App>.Tests/                      unit + integration
└── <App>.Web.Tests/                  bUnit
```

`web-api` — `monolith` without `Web`: one deployable that is only an API, and its contract:
```
src/
├── <App>.AppHost/                    optional
└── <App>.Api/                        the only deployable
    ├── Features/<Feature>/
    ├── Domain/
    ├── Data/                         one DbContext, migrations
    ├── Contracts/                    records for the API's consumers
    ├── Auth/                         scheme selector, consumer claims, API key handler: what question 10a needs
    ├── Common/                       Result/Error, AppJson, the helper that reads the caller
    └── Resources/                    only when a feature sends text to people
tests/
└── <App>.Tests/                      unit + integration, the foundation's HTTP tests
docs/api/openapi.json                 the contract, written by the integration tests
```
The bootstrap generates a foundation proven on a scratch solution (21 HTTP tests, then run by hand with each status code read). Every handler adds the same two claims, `consumer_kind` (`user` or `client`) and `consumer_id`, and code reads the caller only through them. A new endpoint accepts users only: machine clients enter where a feature names the `Clients` policy, and an anonymous endpoint must be on an allowlist an architecture test checks. Rate limiting answers 429 with `Retry-After` and a code, per user or client and per IP when anonymous, with a stricter limit on login and a separate one on refresh. CORS is an allowlist from configuration and never allows `X-Api-Key`. `/health` is public, `/health/ready` answers only on internal hosts, and neither is in the contract. OpenAPI and Scalar are not served in Production: consumers read the committed `docs/api/openapi.json`, which every feature's HTTP tests rewrite. Problems the framework writes itself (a 404, a 405, a malformed body) carry a `code` too. The Identity bearer answer lasts 1 hour for access and 14 days for refresh; a security-stamp change stops the next refresh, but an access token already issued lives until it expires, and `docs/infra.md` says so, with where the key ring (a secret folder shared by every instance) lives. Packages are approved with the profile; `Microsoft.AspNetCore.Authentication.JwtBearer` only when the external IdP is chosen. A second business area means an ADR and `modular-monolith` without `Web`.

`web-app` — one Blazor project is the whole app:
```
src/
└── <App>.Web/                        the only deployable
    ├── Pages/<Area>/                 page + code-behind
    ├── Components/                   shared UI components
    ├── Features/<Feature>/           only when an operation has rules
    ├── Domain/
    ├── Data/                         one DbContext, migrations
    ├── Api/                          endpoints only for external clients
    ├── Common/                       Result/Error, AppJson, helpers
    └── Resources/
tests/
└── <App>.Tests/                      unit + integration + bUnit
```

`website` — the `web-app` layout, plus the public pages, the content area and the islands:
```
src/
└── <App>.Web/                        the only deployable
    ├── Pages/<Area>/                 page + code-behind
    ├── Pages/Public/                 public pages, static, /{culture}/...
    ├── Pages/Content/                content area: Editor and Admin
    ├── Pages/Admin/                  users, e-mail signups, settings
    ├── Components/Islands/           interactive only where something changes live
    └── ...                           the rest as in web-app
tests/
└── <App>.Tests/                      unit + integration + bUnit + HTTP tests of public pages
```
Public pages render on the server with no circuit (no `@rendermode`), which is what search engines read and what keeps an anonymous visitor from holding a server connection; a form (the e-mail signup, the contact) is a plain form post. Every public address starts with the language, with English slugs (`/pt-BR/about`, `/en/about`), and `/` sends the visitor to their browser's language. Each page has its title, description, canonical address and `hreflang`; `/sitemap.xml` is built from the pages themselves, so a new page cannot be left out. A text with no translation shows the default language. The content area has two fixed roles: `Editor` changes content, `Admin` also sees users, signups and settings. There is no page cache: once the content area's interactive mode is on, ASP.NET Core marks every page as not cacheable (measured, #36), so the content read from the database is cached instead, and a save clears it. E-mail signups stay in the app's own table, with a confirmation link and an unsubscribe link that needs no sign-in (LGPD). When logged-in features start sharing rules, the profile says to move to `monolith`, as `web-app` does.

`microservices` — one solution, one folder per service, each a small monolith:
```
src/
├── <App>.AppHost/                    every service, broker, databases
├── <App>.ServiceDefaults/
├── <App>.Gateway/                    routing and auth at the edge (YARP)
├── <App>.Web/                        Blazor UI, calls the gateway only
├── BuildingBlocks/
│   └── <App>.Messaging/              outbox, inbox, broker setup
└── Services/
    └── <Service>/
        ├── <App>.<Service>/          Features/, Domain/, Data/ (own database)
        └── <App>.<Service>.Contracts/ records and integration events
tests/
├── <App>.<Service>.Tests/            unit + integration + contract
├── <App>.Web.Tests/                  bUnit
├── <App>.ArchitectureTests/
└── <App>.SystemTests/                a few end-to-end flows
```

`mobile` — a testable core, a thin MAUI head, and the backend under its own profile:
```
src/
├── <App>.Api/                        backend, per its own profile
├── <App>.Contracts/                  records and error codes shared with the app
├── <App>.Mobile.Core/                plain library: everything testable
│   ├── Features/<Feature>/           view models, feature services
│   ├── Api/                          typed clients, AppJson
│   ├── Storage/                      local cache, offline queue
│   └── Resources/
└── <App>.Mobile/                     MAUI head: XAML pages, shell, platform code
tests/
├── <App>.Mobile.Core.Tests/          view models, services, offline queue
└── <App>.Tests/                      backend tests, per its profile
```

**`mobile` on an existing API (`Backend: external`).** The app is its own repository and its API is another system. There is no `Api` project: the app's own `Directory.Build.props` carries the only `<Version>` (seeded at `0.1.0`), and `/agile:ship` bumps it alone. `<App>.Contracts` holds records you write by hand, and a pinned copy of the API's OpenAPI document sits in `docs/api/backend-openapi.json` (an agile API: its committed `docs/api/openapi.json` at a named commit; another API: its URL), with `docs/api/backend.md` saying where it came from. A test reads that document and fails when a record, a route, the `X-App-Version` header or the `426` response no longer match it (a generator was weighed and not adopted: the typed clients stay over one `HttpClient`); refreshing the copy is an explicit step of the item that needs it, and `/agile:sync` only reports when the API moved past the pinned commit. The app signs in at the API's `login`, adds the token and its version (`X-App-Version`) to every call, refreshes once on `401` (concurrent calls share one refresh) and, on `426` `app.update_required`, shows "update required" without trying to refresh. The screens are XAML, or MAUI Blazor Hybrid when the site shares its components as a package (next paragraph). Tests use a stub `HttpMessageHandler`; the API's own endpoints are tested in the API's repository, and each validation script has a device run against the API's development instance (`10.0.2.2` from the Android emulator).
```
src/
├── <App>.Contracts/                  hand-written records and error codes, checked against the pinned document
├── <App>.Mobile.Core/                view models, typed clients, token and version handler
└── <App>.Mobile/                     MAUI head: XAML pages, shell, platform code
tests/
└── <App>.Mobile.Core.Tests/          view models, the contract test, clients over a stub handler
docs/api/
├── backend-openapi.json              pinned copy of the API's document
└── backend.md                        where it came from, when, which API version
```
The API's side is the API's own session's work. For an agile site, an epic "API for the mobile app" (below) plans it; for an agile `web-api` or any other API, **the forced-update gate** is a line of its profile: the app sends `X-App-Version`, a middleware answers `426` with the code `app.update_required` when it is below a configured minimum or not a version (`2.0` equals `2.0.0`; no header passes; an empty minimum turns the gate off, a malformed one stops the app at start), it runs before authentication, and the OpenAPI document declares the header and the `426` on every `/api/v1` operation. It is a compatibility gate, not a security control.

**Hybrid across repositories (the site's package).** The site keeps its shared components in `<App>.Shared` (a Razor Class Library) and its records in `<App>.Contracts`; they are the only projects with `<IsPackable>true</IsPackable>`, and `/agile:publish` packs both at the site's `<Version>` and pushes them to the GitHub Packages feed of the `origin` owner (private; the token is the classic personal access token in the environment variable `GITHUB_PACKAGES_TOKEN`, never written anywhere). The app has no `<App>.Contracts` of its own: `Mobile.Core` references the `<App>.Shared` package, which brings the site's records, and implements the components' data interfaces over `HttpClient`; the head hosts each component in a page of its own (route, `[Authorize]`, no render mode) and builds the `AuthenticationStateProvider` from `GET /api/v1/auth/me`. A `nuget.config` at the root lists nuget.org and the site's feed with a `packageSourceMapping` (`<App>.*` on the feed, `*` on nuget.org; without it a machine with source mapping on never looks at the feed) and `Directory.Packages.props` pins the exact `<App>.Shared` version, from the same site release as the pinned OpenAPI document. Moving to a newer package is a step of the app item that needs a newer component, committed with the refreshed document; `/agile:sync` only says when the site's latest release is past the pin. A site's breaking change to a component's parameters or a data interface is a MAJOR release. Texts come from `<App>.Shared/Resources/` in pt-BR, pt-PT and en, and the head sets the culture from the device.

**The `mobile-client` complement.** A running `web-app` or `website` can gain a mobile app without changing profile. `mobile-client` is not a profile of its own: it adds the client half of `mobile` on top of the site's profile, and the project records it with one line under `Profile:` in `CLAUDE.md` (``- Complement: `mobile-client` — see `docs/agile/profile-mobile-client.md`.``). You start it with `/agile:epic` (example 14.24): Claude asks where the app lives (the site's own repository and solution, recommended, or a new repository — below) and its screens (MAUI Blazor Hybrid, recommended, reuses the site's Razor components; MAUI XAML gives the native look), then lists the site's features — the `done` items and the areas under `Pages/` — for you to tick; `Pages/Public/` of a `website` is never offered. The epic starts with "Mobile foundation" (the projects below, the app's sign-in with a bearer token on the site's own `/api/v1/auth/login` and `/api/v1/auth/refresh`, the app version) and then one "<X> on mobile" per ticked feature, with an "API for <X>" before it when the feature's logic still sits in a page used by several pages or holds a rule. One source of truth: the rule lives in one class in `Features/`, used in process by the site and by the endpoint the app calls. A shared screen is a component with no `@page` and no render mode, hosted by a page on each side. The app is online by default; offline comes per feature. A mobile client alone does not move the site to `monolith`.

**The app in a new repository.** Answering "a new repository" still asks the screens question (Hybrid recommended; in a new repository it works through the `<App>.Shared` package above) and goes on with the list to tick, but the site's epic is "API for the mobile app" (examples 14.30 and 14.33): "Mobile API foundation" first — the app's sign-in on the site, the forced-update gate, an OpenAPI document (`Microsoft.AspNetCore.OpenApi`, only the `/api/v1` routes, not served in Production, written to `docs/api/openapi.json` and committed with every API change) and, with Hybrid, the packable `Contracts` and `Shared` — then one item per ticked feature, always, because the app cannot extract a page's logic across repositories: "API for <X>" with XAML, "API and shared screen for <X>" with Hybrid (the component moves to `Shared` and its endpoints ship in the same release the app pins). The ticked list goes into the epic as you ticked it, and the epic ends telling you the next step: a new repository whose `product/brief.md` names this one, then `/agile:bootstrap` there, which reads the list. The site records ``- Complement: `mobile-client` — app in its own repository: <repository>; see `docs/agile/profile-mobile-client.md`.`` (with Hybrid: ``... <repository>; shared screens in the `<App>.Shared` package; see ...``); its version stays on `<App>.Web.csproj` alone, and the packages carry it. Claude writes nothing in the app's repository, and the app's session writes nothing in the site's.

**Born with the site.** A `web-app` or `website` bootstrapped with question 2e yes already has the epic "Mobile app", `<App>.Contracts` and, with Hybrid, `<App>.Shared` with the UI kit and the screens of the ticked features. `/agile:epic` does not create the epic again: it says so and names the next idea. "Mobile foundation" there creates neither `Contracts` nor `Shared` and moves no component; it waits for the UI kit (Hybrid) and the site's sign-in. Everything else is as for a running site: the complement copied to `docs/agile/profile-mobile-client.md`, the `Complement:` line, the ADR, `Mobile.Core` and `Mobile`, bearer sign-in, the head started at the site's current version with `ApplicationVersion` 1, the key ring. Each "<X> on mobile" adds the `/api/v1/` endpoint over the class already in `Features/`, the HTTP implementation in `Mobile.Core` and the host page (or the XAML page); no "API for <X>" is suggested, because no ticked feature keeps its logic in a page. `Shared` reads its texts through `IStringLocalizer`, which a Razor Class Library gets only from the `Microsoft.Extensions.Localization.Abstractions` package.
```
src/
├── <App>.Web/                        the site; Api/ gains the app's endpoints
├── <App>.Contracts/                  records and error codes shared with the app
├── <App>.Shared/                     Hybrid only: shared components, their data interfaces, texts
├── <App>.Mobile.Core/                plain library: HTTP implementations, token handler
└── <App>.Mobile/                     MAUI head: host pages or XAML pages, sign-in, platform code
tests/
├── <App>.Tests/                      the site's tests, plus bUnit for the shared components
└── <App>.Mobile.Core.Tests/          the app against the site in memory, with a bearer token
```

**Push notifications** (`mobile`, and a site with the `mobile-client` complement; only when the brief or an item asks for them). One provider: Firebase Cloud Messaging for Android and iOS (it relays to APNs), sent by the API through `FirebaseAdmin` behind an `IPushSender` interface. Registration is the API's: `PUT /api/v1/devices/{installationId}` with the device token, platform, culture and app version, and `DELETE` at sign-out, both bearer-only (`401` anonymous, `400` with `devices.invalid-token` for an empty token, `204` otherwise); a device belongs to one user and a token the provider reports as unregistered is deleted. A push carries a code and ids, never text: the app renders it from its resources in the device's culture. The app side is an interface in `Mobile.Core` implemented in the head with `Plugin.Firebase.CloudMessaging`; the permission prompt comes after sign-in. Tests need no device or Firebase account (fake sender, fake registration); the real delivery is a step of the validation script. The service-account key is a secret outside the repository. Store publishing is `/agile:publish`, below. Example 14.31.

**Offline-first** (`mobile`, and a site with the `mobile-client` complement when its epic part (c) said offline-first; the question is 2f). One SQLite file per signed-in user (`sqlite-net-pcl`; `sqlite-net-sqlcipher` instead when the brief marks the data as sensitive), kept out of Android's backup and deleted at sign-out. Reads show the cache first, with when it was fetched, and refresh when online without touching a row that has pending writes. A save goes to the cache and to a queue; the queue is sent one write at a time, in order, each with an `Idempotency-Key` and, for an update or delete, `If-Match`, at app start, when the network comes back, after a save and on pull-to-refresh. A network error keeps the write and retries later; a `401` goes through the refresh; a `426` stops the queue ("update required"); a `409` (someone else changed the record) moves the write to a conflict list where the user sends theirs again over the current version or discards it, and only the later writes on that record wait. Signing out with unsent changes asks: send now or discard. On Android the queue is also sent with the app closed: a unique one-time WorkManager request with the "network connected" constraint, scheduled when the queue stops being empty and when the app leaves the foreground, behind `IBackgroundSync` in `Mobile.Core` (a no-op on other heads; iOS is a separate item). A background run never signs out and never deletes a write; it records "sign in again" (`401` whose refresh fails) or "update required" (`426`) for the next open, shares one lock with the foreground sender, and a user who force-stops the app waits until the next open. Nothing is shown with the app closed. A conflict over an update also offers **Merge** (Example 14.42): each pending update keeps `base`, the record as it was before your first edit, so the screen compares three versions per field. A field only you or only the other person changed is kept without asking; a field changed on both sides to different values is a pick (a list is one pick, whole); nothing is preselected and "Send merged" waits until every pick is made. It replaces the record's pending writes with one update on the current version; a `404`, `400`, `422`, a conflict over a delete and a record whose `## Offline` turns the merge off keep "Send mine again" and "Discard". Run on a scratch app: 13 Core tests green. Example 14.41. One kit component, `OfflineStatus`, shows the banner, the pending count and the conflict list on every screen. The tests run on a real SQLite file with a fake connectivity; the device run (airplane mode on, two saves, network on) is a step of the validation script. The API's side is **Offline writes (the API)**, the same text in `web-api`, `mobile`, `mobile-client` and `desktop`: a write with an `Idempotency-Key` is recorded in the same transaction as the write and replayed (not run again) when the key returns, `422` when the key comes back with another body; an entity written offline carries a version (`xmin` on PostgreSQL), and its update and delete require `If-Match` (`428` without it, `409` with the current record when it is stale). Keys are kept 30 days. Both blocks were run on a scratch API and a scratch app on the Android emulator.

**Staff backoffice** (`mobile` with `- Backoffice: staff`; the question is 2g, example 14.54). The app is the product and the people who manage the data it shows get web pages for it, served by the API's own host under `/backoffice` (Blazor Server; in a modular monolith each module brings its own pages), never in the backend profile's `<App>.Web`, so what `/agile:publish` packages does not change. There is one Identity for both populations and two doors: the app's bearer token opens only `/api/v1/`, and the backoffice's own cookie opens only `/backoffice` (a cookie on the API answers `401`, a bearer token on a backoffice page goes to sign-in). Staff accounts belong to the backoffice: the app's login refuses them (`auth.staff_account`) and never hands out a cookie, so a staff member uses the app with another account. Who enters: only accounts with a staff role, `Admin` or `Staff` (question 12 decides fixed roles or permissions), and an unknown address, an app user, a wrong password and a locked account all get the same "sign-in failed". An `Admin` must also enrol an authenticator app (TOTP) and keeps 10 single-use recovery codes: until then no data page opens for them. The first `Admin` comes from `Backoffice:FirstAdminEmail` (user-secrets or environment, never committed): the app e-mails a single-use link to set a password, sent again at every start until one is set; from then on an `Admin` invites others from the staff page, sends a pending invite again, resets another member's authenticator when a phone is lost, and removes a member from staff, which ends their cookie at the next check and their open page within a minute. There is no open sign-up. With several tenants the staff see all of them, only through the backoffice's own feature classes, each tested; the app's API stays filtered. Screens use a MudBlazor kit built from the app's identity tokens, with texts in every language of the app. A project without the backoffice adds it later with the item "Backoffice foundation", which `/agile:sync` proposes until `- Backoffice: staff` or `none` exists; the plugin ships the text and the note, and the code is that item's. With an external identity provider the sign-in is decided in that item's refinement.

**Audit log of the backoffice** (`mobile` with `- Backoffice: staff`, always on; `- Backoffice audit: on | none` in `CLAUDE.md` tells `/agile:sync` where the project stands; example 14.63). Staff see every customer's data, so each create, update and delete made from the backoffice leaves a row: who (the staff member), when (UTC), which entity and id, and the fields that changed, old and new. The row is written in the same transaction as the data, so a failed audit insert undoes the change. A field marked sensitive shows `***` on both sides (the row says it changed, never what it held). Four staff actions are recorded by name, never by diffing the accounts, so no password hash, authenticator key or recovery code ever reaches the log: invite, send the invite again, reset an authenticator, remove a member. Sign-ins, denied access and writes by app users through `/api/v1/` are not in this log (the quiz's auditing answer, question 9, is the trail for app users). An `Admin` with the second factor reads the rows on `/backoffice/audit`: newest first, 25 per page, filtered by member, entity and period, with each row opening to its old → new fields; `Staff` gets "denied". A job deletes rows older than `Backoffice:AuditRetentionDays` (730 by default; question 9b decides the number). The log is append-only for the app, but a person with direct SQL access to the database, or a code path using `ExecuteUpdate`/`ExecuteDelete`, is not stopped: only a database trigger or revoked grants would, and that is not built here (the later answer is a hash chain over the rows, agile-canary#106). The plugin ships the text, the `/agile:sync` note and the quiz line; the code is the project's own item. The text was rebuilt from itself by a fresh-context engineer on a monolith and on a modular monolith, and each rebuild's gaps were folded back.

**Store publishing** (`mobile`, and a site with the `mobile-client` complement). `/agile:publish` also builds `<App>.Mobile` as an Android App Bundle, a `dotnet publish` in Release for its android target framework with the signing as MSBuild properties, and copies only the signed file to `artifacts/publish/v<Version>/<App>.Mobile/<ApplicationId>-Signed.aab`. Signing comes from four environment variables, never from a file in the repository: `ANDROID_SIGNING_KEYSTORE` (an absolute path outside it), `ANDROID_SIGNING_ALIAS`, `ANDROID_SIGNING_STORE_PASS`, `ANDROID_SIGNING_KEY_PASS`; the passwords reach MSBuild as `env:` references, so no command line or log holds one, and `docs/infra.md` names the variables and where the keystore lives, never the values. The key is an **upload key** under Play App Signing (Google holds the app signing key; a lost upload key is reset through Play support, so keep a backup outside the machine). The Mobile head is left out, with the reason, and the server side is still published when: a variable is unset, the keystore is inside the repository or missing, `ApplicationId` still starts with the template's `com.companyname.` (Play makes the id permanent at the first upload), or the MAUI Android workload is missing. It is never built unsigned. A failed Android build stops the whole package step before any commit or tag. With a mobile head the release commit also carries `docs/releases/v<Version>-store.md`: the `.aab` path and both version numbers, the Play Console steps (testing track, then promote), the first-release-only steps (create the app, store listing, privacy policy URL, Data safety form from quiz 9b, content rating) and the iOS steps for a Mac, marked as not run by the plugin. The plugin uploads nothing to either store and builds nothing iOS. Play rejects a `versionCode` it has already seen: the ship raises `ApplicationVersion` at every release, and the checklist says so. The `.gitignore` template ignores `*.keystore`, `*.jks`, `*.p12`, `*.p8` and `*.mobileprovision`; a project whose file lacks them gets a warning from the plan, never an edit. First release with no keystore: Claude gives you the `keytool -genkeypair` command to run in your own terminal (it asks for the passwords there) and lists the variables to set. Example 14.32.

`desktop` — the same shape as `mobile`, with the API optional. The quiz asks the technology (Avalonia when more than one operating system, WinUI 3 for Windows only with the native look, MAUI when mobile is in the same product) and, for PostgreSQL or SQL Server, whether the client goes through an API or straight to the database (direct only for a single-user app or a closed network: the credential then lives on every machine). The app runs standalone (a self-contained `dotnet publish`, which `/agile:publish` runs once per runtime; a WinUI 3 head needs `<EnableMsixTooling>true</EnableMsixTooling>` or the published exe crashes on start); with an update source declared, the installed app updates itself ("Desktop updates", below). With an API it can also be offline-first (question 2f, below). The Stop gate never builds the head; only Avalonia has automated screen tests (`Avalonia.Headless`), WinUI 3 and MAUI go through the validation script:
```
src/
├── <App>.Api/                        only with an API, per its own profile
├── <App>.Contracts/                  only with an API
├── <App>.Desktop.Core/               plain library: everything testable
│   ├── Features/<Feature>/           view models, feature services
│   ├── Api/                          with an API: typed clients, AppJson
│   ├── Data/                         direct access only: DbContext, or the SQLite store
│   ├── Platform/                     interfaces the head implements
│   └── Resources/
└── <App>.Desktop/                    the head: XAML views, shell, platform code
tests/
├── <App>.Desktop.Core.Tests/         view models, services
├── <App>.Desktop.Tests/              Avalonia only: headless screen tests
└── <App>.Tests/                      backend tests, only with an API
```

**Desktop updates** (`desktop`; the source is quiz question 26b, a line of `docs/infra.md`). The updater is Velopack, for the three heads on Windows and for an Avalonia head on Linux (below), a stable channel and an opt-in beta channel (Windows only), unsigned unless `docs/infra.md` declares Code signing (below). `docs/infra.md` says `Update source:` a network share, an https URL, a public GitHub repository (below) or `not declared` (then the app stays a zip). The app reads it from `Updates:Source` in its `appsettings.json`; run from the IDE or a publish folder it checks nothing. At start the app checks the source in the background and downloads what is new (only the changed part, a delta, when one exists); then it asks "Update now / Later". "Update now" restarts on the new version; "Later" applies it when the app is closed; an app killed after a download applies it at its next start. A failed check is logged and shown to nobody. With an API, a `426` opens "Update required" with "Update now" only. `/agile:publish` packs each Windows runtime with `vpk` (Setup.exe, a portable zip, the full package, the delta, the feed index), sends the feed to a share after the tag, and for an https URL lists the files to copy by hand; the GitHub Release carries Setup.exe and the portable zip. The first release says: install once with Setup.exe (a zip copy does not update itself) and SmartScreen warns because the installer is unsigned ("More info", then "Run anyway"). The bootstrap writes the updater when the source is declared; an older project is told by `/agile:sync` ("Desktop updates") and captures an item. A head packed for more than one Windows runtime puts each on its own channel (`win-x64`, `win-arm64`), so their feeds do not overwrite one another; with one runtime the channel stays Velopack's default and installed copies are not touched. Example 14.44.

**Desktop updates from a public GitHub repository** (`desktop`; `- Update source: https://github.com/<owner>/<repo>`, quiz question 26b). The installed apps read the releases of a **public** repository, with no token: the app's own when its code is public, or a separate repository that holds only the releases while the code stays private. A private repository is never the source: reading its release files needs the permission that also reads the code, and that token would sit in every installed copy. `/agile:publish` first checks, before anything is built or tagged, that `gh` is installed, logged in and sees the repository as public; a private, internal or missing one stops it. It downloads the previous release from that repository for the delta, creates the GitHub Release there with the notes only (even when your code lives somewhere else, such as Azure DevOps: the code repository keeps its own tag), and attaches Setup.exe, the portable zip, the packages and the feed index with `vpk upload github`, using your own `gh` login, which is never shown. A failed upload leaves the release and the tag; the report lists the files already attached, one `gh release delete-asset` line for each, and the upload again. Installed apps see a release up to a minute after it is attached. Without a token GitHub allows 60 checks per hour per IP address (one per app start), so many users behind one address use the share or the https source. One Windows runtime per head, and no beta channel. A source that turned private answers `404`: the app opens normally and logs it. The bootstrap writes the head with a `GithubSource` when the line starts with `https://github.com/`. Example 14.53.

**Beta channel** (`desktop` with a share; quiz question 26c, asked only after a share). `docs/infra.md` says `- Beta channel: every merge` or `off` (no line is off). With `every merge`, every `/agile:ship` ends by packing `main` as `<Version>-beta` (`0.5.1-beta`) on Velopack's beta channel and sending it to the share: no tag, no GitHub Release, no notes. A tester installs `<App>-beta-Setup.exe` from the share once; from then on that PC takes each new beta, by a delta, and never a stable release, even a newer one. The beta install replaces the stable one on that PC (one channel per PC); running the stable `<App>-win-Setup.exe` takes it back to stable. App users on stable never see a beta. The share keeps the five newest betas (each is 50-120 MB); a tester away for more than five merges downloads the whole package once. A beta already on the share is not sent again. A failed beta (share unreachable, a `vpk` error) never undoes the merge: the ship report shows the error and the rerun, `/agile:publish --beta`. An https source or a GitHub repository refuses the beta (the plugin sends nothing to the first; the apps read no prereleases from the second), and the step runs on Windows only. With an API, a beta reports `0.5.1` in `X-App-Version`, so the forced-update gate treats it as that number. An older project with a share is told by `/agile:sync` ("Desktop updates, beta channel") and adds the line itself: the app needs no change. Example 14.50.

**Desktop updates on Linux** (`desktop` with an Avalonia head that lists `linux-x64`). `/agile:publish`, still on your Windows machine, packs `linux-x64` as `<App>.AppImage` with `vpk` for its `[linux]` target, on Velopack's channel `linux`, beside the Windows feed in the same share; the GitHub Release carries the AppImage too. The Linux app reads its own line, `- Update source (linux): <the share's path as mounted on Linux, or https://...>`, which `/agile:publish` writes into the Linux package's `appsettings.json`: a Windows share name means nothing on Linux. With no such line an https source serves both; a share stops the publish with the exact line to add. The head needs a PNG icon at `Assets/app-icon.png` (an AppImage requires one; the foundation makes it from the visual identity); without it the publish stops naming the path. A Linux user puts the AppImage in `~/Applications`, runs `chmod +x` and starts it; from then on it updates itself with the same dialogs, "Later" and kill behavior as on Windows. The first update downloads the full package, the next ones only what changed. The machine needs `libfuse3`, and Avalonia needs `libice6` and `libsm6` (a desktop distribution has them; a minimal WSL Ubuntu does not). The first release with an AppImage says all this in its notes. `linux-arm64` stays a zip; WinUI 3 and MAUI have no Linux; macOS is a later item (it packs only on a Mac); there is no Linux beta. The bootstrap asks nothing new. Example 14.51.

**Code signing** (`desktop`; a section of `docs/infra.md`, no quiz question). Unsigned, Windows names the installer's publisher "Unknown publisher", Smart App Control on Windows 11 may block it, and SmartScreen reputation starts from zero at every release. `## Code signing` says `- Code signing:` `artifact-signing` (Microsoft's Artifact Signing: organizations in the USA, Canada, EU and UK, individuals in the USA and Canada), `signtool` (a certificate from a certificate authority, on a token or a cloud HSM), `template` (a vendor's own signing command) or `none` (no line is `none`: unsigned, as before). Each mode reads one environment variable of the shell you publish from: `VPK_AZURE_TRUSTED_SIGN_FILE` (the path of Artifact Signing's `metadata.json`, outside the repository), `VPK_SIGN_PARAMS` (signtool's parameters, the certificate by `/sha1 <thumbprint>`, never `/p` with a password, nor a token PIN in `/kc`) or `VPK_SIGN_TEMPLATE` (the command, with `{{file}}`). The value is never written in `docs/infra.md` or shown in the chat. `vpk pack` signs Setup.exe, the app's files and `Update.exe`; `/agile:publish` gives it only the declared variable (a variable left in your shell for another project, or for a project that says `none`, signs nothing here), then checks Setup.exe, the app's `.exe` and `Update.exe` with Windows' own signature check and names the publisher in its report. It stops before anything is committed or tagged when the variable is unset, holds `/p`, the `metadata.json` is missing or inside the repository, the machine is not Windows, or a file is not validly signed: a declared mode is never published unsigned. `artifact-signing` also needs `- Signing account: <tenant id>/<subscription id>`, whose `az` login lives in the same folder per account as a deploy's (`~/.agile/azure/<tenant>/<subscription>`, section 5, `## Cloud accounts`): the publisher is the project's, your company for your apps, the client's for a client's app. A beta is signed the same way. An app with no update source (a plain zip) is signed too, in its Windows zips: the plugin packs the publish folder with `vpk` only to have its files signed, keeps the signed files and checks every `.exe` and `.dll` of the zip (not only the main one), because an unsigned `.dll` beside a signed `.exe` is what Smart App Control blocks. That needs `vpk` as a local tool of the project, which such a project does not have yet: the publish stops, asks "install vpk 1.2.161?" and, on yes, installs it, commits and pushes the tool manifest before going on. The release step checks the main `.exe` of each signed zip again, and the zips of other systems and the AppImage are never signed. The zip itself still stays in `artifacts/publish/<tag>/` for you to send. Signing does not make SmartScreen silent at once: the dialog shows your name but may still say "unrecognized app" for some weeks and hundreds of installs (Microsoft: EV certificates included), so the first release signed by a publisher says so in its notes. Examples 14.52 and 14.57.

**Offline-first on desktop** (`desktop` with an API; the question is 2f). The same design as the mobile one, in `Desktop.Core/Storage/`: one SQLite file per signed-in user under `LocalApplicationData` (never the roaming folder; `sqlite-net-pcl`, or `sqlite-net-sqlcipher` with its key in the operating system's protected store when the brief marks the data as sensitive), deleted at sign-out, no backup exclusion because a desktop has none like Android's. Reads, the queue, `Idempotency-Key`, `If-Match`, the conflict list, the field-by-field merge and the sign-out question are the mobile lines word for word. What differs is the head: `IConnectivity` is implemented with `Connectivity.Current` on MAUI and with the BCL's `NetworkChange` on WinUI 3 and Avalonia; because "a network is available" does not mean the API answers, a failed send marks the API unreachable until the next connectivity change or the next successful request, and a Refresh command (F5) always tries; the queue is not sent with the app closed (no tray icon or service: installers and services are outside the profile). Run on a scratch Avalonia app in a Linux container with its network cut and restored: 19 Core tests green, `NetworkAvailabilityChanged` raised both ways, two saves sent once each and in order, and an API stopped with the network up cleared by the Refresh. The WinUI 3 and MAUI heads are measured on your machine, in the validation script of the first "Offline foundation". The API's side is the same **Offline writes (the API)** block, now word for word in four profiles.

**App version.** The app carries one `SemVer` version: a single `<Version>` property in the `Directory.Build.props` beside the solution file. Every project inherits it, so every DLL, libraries included, reports the release and the commit it was built from (`0.4.0+3f2a9c1…`), and no `.csproj` carries `<Version>`. `/agile:publish` packages the profile's deployable project(s): `<App>.Api` (`monolith`, `modular-monolith`, `web-api`), `<App>.Web` (`web-app`, `website`), `<App>.Api` and `<App>.Mobile` (`mobile`; `<App>.Mobile` alone with `Backend: external`), `<App>.Desktop` (`desktop`), `<App>.Web` and `<App>.Mobile` (the `mobile-client` complement). Bootstrap seeds `0.1.0`. Every `/agile:ship` looks at it first and asks nothing: with none anywhere it adds `0.1.0` and does not bump (that item ships as `0.1.0`); with one on one or more `.csproj` (the layout before 0.0.104) it moves it to `Directory.Build.props`, removes it from every project (the highest wins when they differ) and then bumps; with it already there it just bumps. The bump is MINOR for a feature (PATCH resets), PATCH for a bug, or MAJOR when the item's `## Decisions` records a breaking change (MINOR and PATCH reset), and the ship report says which of the three cases ran. In `mobile`, the `mobile-client` head and a `desktop` MAUI head, the same ship also sets the head's `ApplicationDisplayVersion` to that string and increments its `ApplicationVersion` — the store's ever-increasing integer — by 1, a counter that never resets. `/agile:sync` only reports the state ("the next `/agile:ship` adds it" or "moves it") and never writes it. There is one number for the whole app, not one per project: replacing a single DLL would leave a machine with a mix the gate never tested; downloading only what changed belongs to an updater with delta packages (#58). When the app is in its own repository (`Backend: external`, or a Hybrid app), its solution has its own `Directory.Build.props` and its own number. `microservices` is out of scope: one version does not map cleanly to independently deployable services.

Shared rules live in `rules/core/` and are copied to `.claude/rules/agile/` at bootstrap: `workflow`, `naming`, `git`, `definition-of-done` and `output-style` are always loaded; `i18n` and `api-contracts` load only when Claude works on code files, `ui` only on screen files (`.razor`, `.xaml`), and `build-config` only on project and build files. One rule per line, at most 30 lines per file. Core rules are generic: they hold for every profile. Anything that depends on a profile, a stack or a UI library lives in the profile file, in `templates/dotnet/` or in a rule scoped by file type, and the plugin applies it to every profile it concerns.

**Rules the build checks.** Bootstrap copies `templates/dotnet/` to the solution root, in every profile: `Directory.Build.props` (shared settings, code style enforced in the build, `NeutralLanguage` `en` for the neutral resource set, and `net10.0`, the LTS release; `net11.0` is a one-line change when the project chooses it), `Directory.Packages.props` (every package version in one place), `.editorconfig` (the owner's rules: naming, braces on every block, pattern matching, expression-bodied members, formatting), `BannedSymbols.txt` (forbidden APIs such as `new JsonSerializerOptions`, `DateTime.Now`, `Thread.Sleep`) and `global.json`. A broken rule becomes a build warning, and the gate fails on new warnings — so the rule holds even when nobody remembers to read it. `TreatWarningsAsErrors` stays off. When a retro lesson can be checked by the build, it goes there first.

**Stack lessons.** A lesson from a real item that depends on the stack goes to the rule scoped to those files or to the profiles it concerns, never to an always-loaded rule. Examples from the first app: `api-contracts` says a custom middleware resolves an optional dependency inside the branch that needs it (an `InvokeAsync` parameter is resolved on every request) and that a client asks the API for the user's claims instead of decoding its access token (it may be encrypted); `build-config` says a version-dependent investigation reads the version resolved in `obj/project.assets.json`, not the pinned one; the profiles with an Aspire AppHost say code reaches Redis, PostgreSQL or a broker through the Aspire client integration, because local resources run with TLS by default, and that the generated `ServiceDefaults` turns off retries for POST (a retried POST replays single-use tokens); the profiles with a Blazor UI say that, with Interactive Server, state that changes during a session lives in a server-side store keyed by an id in the cookie, because a circuit cannot rewrite the cookie; and the test strategy of the profiles says a bUnit test waits for what an async click handler does (`WaitForAssertion`), and a test host created per test class clears its Npgsql pool on dispose, or the test database runs out of connections; and, with Interactive Server, data from the first request that a circuit needs (the visitor's address, a header) is read in `App` and passed to the interactive root, because a circuit has no `HttpContext`; the profiles with bUnit also say every new page and dialog gets a test that renders it, because a Razor attribute mistake compiles (a string parameter without `@` is literal text); the profiles with EF Core say the delete that closes a unit of work goes outside the `try` that handles its failure, because a failed save keeps the entry `Deleted`; with ASP.NET Core Identity, only the last step of a multi-step sign-in clears the failure count; and in `modular-monolith` a row a request must not lose (a queued job, an outbox message) is written by the same save as the data that justifies it. `api-contracts` also says an anonymous endpoint that must give the same answer on every path runs every input check before the lookup that tells the paths apart. The profiles with a Blazor UI also say a page that peeks a single-use ticket (an invite, a confirmation link) in `OnInitialized` sees it twice — prerender runs before the interactive circuit — so it reads the ticket there but spends it only on success, and a ticket that must not be replayed is bound to the browser with an HttpOnly cookie, never carried by the URL alone; and, for a project with the technical-docs generator, the data dictionary shows the `WHERE` clause of a partial index next to it. The test strategy of every profile also says the SDK template still generates xUnit 2.x, where `TestContext.Current.CancellationToken` does not exist: that member is v3, and adopting v3 is a decision that gets pinned and written down.

**One behavior on every screen.** The `ui` rule keeps screens from drifting apart: a pattern is defined once, in a UI kit shown on a dev-only gallery page, and pages use the kit instead of raw library components. One icon family behind semantic names (`AppIcons.Edit`), one way to edit an item (row actions in the last column; dialog for simple entities, own page for complex ones; a name link only opens a read-only detail page), the same hover and focus everywhere, one confirmation dialog for destructive actions, the same feedback, list states, form layout and action vocabulary. Architecture tests forbid raw icons and raw tables outside the kit, and the kit comes before the first screen. A kit parameter whose right value depends on what the page means (autocomplete, a destructive action's wording) is required, so a page that forgets it does not build. A colour or contrast problem is checked in both themes and on every surface the component sits on, computed from the theme and then measured on screen, and a test over the theme tokens keeps every colour pair honest, including the ones no screen has rendered yet. The project records its choices (icon family, declared exceptions) in its own rules.

Claude writes every file (code, tests, docs) with its editing tools, never through the text of a script: escapes such as `\t` or `\b` turn into control characters, and a test can pass while checking nothing.

**Keeping a project up to date.** Bootstrap copies plugin files into the project (rules, templates, this workflow, the profile, the build files), so a plugin update does not reach them by itself. Update the plugin in a terminal, then open a new session:

```powershell
claude plugin marketplace update canary
claude plugin update agile@canary
```

```bash
claude plugin marketplace update canary
claude plugin update agile@canary
```

Then run `/agile:sync` in the project. Claude shows a table of what changed and copies only what you approve: a copy you never edited is replaced; a file you edited (usually the profile) is merged by hand, keeping your sections; build files (`Directory.Build.props`, `.editorconfig`, `global.json`...) are never copied over — each difference is proposed as an edit; your own rules (`project.md`, `*-project.md`) are never touched, and `CLAUDE.md` only gets what you approve: a section the template gained, or a `Worktrees:` line when it has none (the bootstrap recommendation, `D:\wt\<repository>` or `C:\`; existing worktrees keep their names). If build files or checked rules changed, Claude builds, runs the full suite and refreshes the warnings baseline. A project with no warnings baseline at all (an adopted repository, an old bootstrap) gets a row offering to take the first one: a full rebuild, run only with your yes, committed with the sync. A ⏳ plugin note in the retro log that cites an `agile-canary#N` the plugin has delivered gets a row too: with your yes, `sync.js notes` marks it ✅ with the version and merge commit. Notes almost never cite their issue, so the plugin also keeps a map from the text of a note to the issue that delivered it (`scripts/delivered-notes.json`, from 0.29.2): a ⏳ `agile` note whose text is in the map gets the same row and the same mark, and the row says whether the note was cited or mapped. When you edited a file the sync also copies (a manual, a profile), the merge is `sync.js merge <path>`, which keeps the manual's links pointing at your two local manuals. The sync never proposes a `<Version>` for `Directory.Build.props` (the version is yours and `/agile:ship`'s), and does not offer the DocGen tool catalogue to a project whose `docgen.json` says `"tools": false`. Example 14.75. The project's own session is the only one that writes those marks; the plugin session never writes in your repository. The version and what was copied are recorded in `.claude/agile/sync.json`. Run it between features, not in the middle of one. `/agile:version` shows the plugin version running in the session next to the project's, and says whether a sync or a plugin update is the next step. The sync also names what only `/agile:bootstrap` installs and your project does not have — a tool of its own, such as the technical-docs generator — and offers to capture a feature for it; it never installs it behind your back. A project with screens and no `docs/design/identity.tokens.json` gets the same kind of note (from 0.0.86): the sync writes nothing, since the identity is your project's file and not a copy of the plugin, and suggests `/agile:identity`. A compose environment of the Aspire recipe whose AppHost declares a database container with no password argument gets the row "Database password" (from 0.30.1): it names the file, the edit and what to do with an environment already deployed, and offers to capture a bug; the sync never edits the AppHost (example 14.77). Every run also shows one row with the size of what is loaded in every session (from 0.0.103): the words of `CLAUDE.md` (limit ~900) and the always-loaded estimate, `CLAUDE.md` plus every rule without `paths:`, in tokens (words × 1.3, budget ~4.5k). When either is over, the row says which and names the two largest sections of `CLAUDE.md` as where to trim. Nothing blocks on size and the sync writes nothing: you trim, or the retro proposes it before adding a line. Example 14.37. From 0.1.0 the plugin numbers its versions by what changed: a feature raises the middle number, a fix the last one (before 1.0; 1.0.0 is the owner's decision).

`output-style` sets how Claude talks to you: in pt-BR, answer first, step reports of at most 10 lines, details in the file instead of the chat, one recommendation with its reason, no narration of the work, "not verified" said in those words, and bad news first. Long answers only when a gate failed, a question needs context, or you ask. Every question to you, from any command, is set apart from the report: a card when the session has the question tool, otherwise a quoted block marked `❓` with the options, the recommendation and the reason; the report stays outside it and the block is the last thing in the message (example 14.65). Every question, doubt or challenge carries a recommendation and its reason, in a command or in plain conversation. When you answer with another option, Claude scores it against the recommended one after the round: 3 to 5 criteria (effort now, cost over time, risk, reversibility, adherence to the brief and decisions), 0 to 10 for each option, one reason per line, the average at the end. Your answer stands; only a gap of 2.0 or more asks once whether to keep it or switch, and the choice is one line in `## Decisions` (example 14.70). A command you are to run or copy, in a message or in a document, comes in two code blocks, PowerShell first and Git Bash second, with made-up or `<placeholder>` values and never a real id, path or account of yours (example 14.71).

## 13. Sessions and pauses

- Start: Claude checks the branch, the board and the feature in progress before doing anything.
- Resume: `/agile:build <id>` on the item that is `building` continues from the last `wip` commit.
- Pause mid-feature: `/agile:pause`, or just say you are stopping. Claude writes the note (where we stopped, what is next, who decides, what is still running) both in the chat and as `## Paused` in the item file, replacing an earlier one, then commits on the feature branch with a `wip(F-<n>):` prefix; nothing stays only on disk. The note stays until the next pause replaces it; its date says how old it is. Closing the session without a pause is fine too: the next session commits the leftover work as `wip` first.
- Compaction: when the conversation is compacted, the item in progress, its branch, its worktree, its `## Paused` note and the next step are kept in the summary, and the session that continues prints them again (section 9, example 14.68). The criterion → test table is not kept: it lives in the chat (#118 would keep it in the item).
- End: a short note (where we stopped, what is next, who decides).
- Before removing a worktree at ship: Claude no longer asks whether you closed the IDE or app host — typing `/agile:ship` counts as closed. From 0.30.2 the removal is one script, `scripts/worktree.js remove <path>`, and nothing else removes a worktree (also an abandoned item and a refinement cancelled as a duplicate). In one process it runs `dotnet build-server shutdown` (only when the worktree tracks a `.sln`, `.slnx` or `.csproj`), then a lock probe (the folder renamed to `<folder>.probe` and back), then `git worktree remove` (never `--force`) and `git worktree prune`. A step that fails ends the run before the next one starts, so a removal can no longer run on a held folder (before, Claude joined the probe and the removal with `;` and a failed probe still unregistered the worktree and left the folder on disk). When a file is held the script stops with `HELD` and one message that names what it found: the processes whose command line has the folder path, what `handle.exe` (Sysinternals) lists when it is on your `PATH` and you already accepted its licence on this machine (the script never accepts it for you), and a `.vs` folder ("Visual Studio may hold it"). A program that only has the folder as its working directory shows by open handle only, so with no `handle.exe` the message says no holder was found and names the tool. Claude asks you to close the holder and runs the same command again. A folder git no longer lists but that is still on disk (an earlier removal that failed halfway) is an orphan: the script lists what is inside, deletes nothing, and Claude asks one yes per folder before running it again with `--orphan`. Example 14.78.

**One folder per item.** Every item gets its own worktree — a separate folder outside the repository (the root is the `Worktrees:` line of `CLAUDE.md`, asked at bootstrap — recommended `D:\wt\<repository>`, or `C:\wt\<repository>` without a D: drive; without the line, `<repository parent>/wt/<repository>/`, and `/agile:sync` offers the line. The folder is `f-<n>-<desc>` or `b-<n>-<desc>`, `<desc>` being up to 20 characters of the slug, cut at a hyphen: `f-3-exam-board`. Kept short because of Windows path limits; a worktree created before this keeps its `<type>-<n>` name until the merge) — created by `/agile:refine` before it writes anything and used until the merge. That is what keeps an item's documents on its own branch: a session working in another item's folder used to write the feature file and the mockup there, and they ended up on an unrelated branch. The build reuses that folder and never makes a second one.

**Two items in parallel.** The default is still one item at a time in `building`. When you really want a second one running — a long feature in one session and a bug in another — type `/agile:build <id> --worktree`. The flag is your request for parallel work: it lifts the one-at-a-time limit (and, for an item approved before this existed, creates the missing folder). Claude first tells you whether the two items can collide (same module schema, same screen, same contract) and recommends sequence when they do. One writer per worktree. The item status lives in that worktree until the merge, and `/agile:status` and the session start read every worktree. Only one app host runs at a time (the ports collide); ignored local files are not carried over. The app host of a worktree is started from that worktree in a background shell and opened in the preview by its URL; no launch configuration is pointed at a worktree. Anything a step starts inside a worktree (an app host, a server, a watcher) is stopped by that step, so nothing holds the folder at ship; a mockup is shown to you as a file, never through a server. On ship, the full check runs in the worktree and the merge in the main checkout; after the merge the worktree and the branch are removed, never with `--force` without asking, and the other item in progress is brought up to date. Recommended limit: two items — you validate each one on screen, and that is the real bottleneck.

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
rules/core/workflow.md: "Write files (code, tests, docs) with the Write and Edit tools, never through
a script's string literals": escapes turn into control characters or stray text, and a test can compile
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

## 15. Quick reference

You only type the commands below. Each one loads a skill with the full procedure (for example, `/agile:bootstrap` loads `bootstrap-quiz`); the skills are hidden from the `/` menu and Claude loads them.

| Command | Use |
|---|---|
| `/agile:bootstrap` | Quiz from the brief (a mobile app also asks 2g, a staff backoffice; an Aspire app asks 3a, fixed local ports and passwords) → `CLAUDE.md`, ADR, profile, skeleton |
| `/agile:discuss "<idea>"` | Explore an idea: options, decisions, items captured |
| `/agile:epic "<name>"` | Break an epic into prioritized features |
| `/agile:idea "<text>"` | Capture an epic, feature or bug, unrefined, after looking for a similar item (improve it instead, or capture anyway) |
| `/agile:refine <feature>` | Looks for a similar item (carry over and cancel this one, keep both, or fold), then creates the item's branch and worktree and runs the refinement round → feature file for approval |
| `/agile:screen <feature>` | Screen details and HTML mockup during refinement, by the `ux-designer` agent |
| `/agile:build <feature> [--worktree]` | Find the item's worktree by its branch, then implement an approved feature there (one at a time; `--worktree` for a second one in parallel) |
| `/agile:review <feature>` | Fresh-context review of a risky change |
| `/agile:change <feature> [text \| --block "<reason>" \| --unblock [--worktree]]` | Record a change of mind during build; or set the item `blocked` (reason and who unblocks it) and bring it back |
| `/agile:script <feature> [what changed]` | Rewrite the validation script of an item that is `building` or `validating`: only the steps your text reaches, or the whole script with no text; file, commit and issue body, no change note |
| `/agile:ship <feature>` | Full suite, app version (added or moved to `Directory.Build.props` when missing, then bumped), merge (typing it is the authorization), branch and worktree removed, board, app manual, and the declared docs command; a desktop app with `- Beta channel: every merge`: `main` sent to the share as `<Version>-beta` |
| `/agile:retro` | Turn lessons into rules or skills |
| `/agile:pause [note]` | Stop for now: wip commit on the item branch and a note of where we stopped |
| `/agile:status` | Feature in progress, backlog head, open questions, what is blocked and on whom |
| `/agile:sync` | After a plugin update: refresh the project's copies of rules, templates, workflow and profile; reports the capabilities the project lacks, such as the "Staff backoffice" and the "Staff audit log" of a `mobile` project |
| `/agile:identity` | Record the app's visual identity (a file, a website, an image or three questions) in `docs/design/`, or review the one recorded |
| `/agile:autopilot <feature> [--assume] [--worktree]` | One item from idea to done in a single run with two stops: the questions (your answers approve it) and the validation script ("validado e autorizo o merge de F-n" ships it; "validado" alone stops at validating). `--assume` skips the questions except new packages |
| `/agile:version` | Plugin version running in this session, the version the project's copies came from, and the next step when they differ |
| `/agile:publish <environment> --park` | An `aca` environment nobody uses for a while: shows the plan, asks a yes, sets every container app to minimum 0 and stops its PostgreSQL server (marked `agile-parked`); Azure starts the server by itself after 7 days, `--park` again parks it again |
| `/agile:publish <environment> --resume` | The same environment, back in use: shows the plan, asks a yes, starts the server, removes the mark, gives each app its minimum back and polls the Check URL; a deploy of a parked environment starts the database first on its own |
| `/agile:publish <environment> --cost` | An `aca` environment's spend since the first day of the month, from Azure Cost Management: one line with the amount, the subscription's currency and the share of the row's ceiling (figures lag 8 to 24 hours). A read: no yes, nothing written; `--plan` shows the same as `Spend:` after `Budget:` |
| `/agile:publish <environment> --restore-drill` | An `aca` environment's PostgreSQL backup, proven: shows the plan, asks a yes, restores the backup into a throwaway server for a few minutes, checks it (and reads the data with `psql` when you have it) and deletes it; the closing line is `PASSED`, `PASSED, data not read` or `FAILED; the backup is not proven`. The throwaway server is billed per hour until removed; a parked environment is refused (`--resume` first); `--plan` only shows the plan |
| `sync.js schedule [write]` (the plugin's script, run from the project root) | The `## Schedule` table of `docs/infra.md` rendered into `.github/workflows/park.yml`: without `write`, what it renders and whether the file matches; with it, writes `park.yml` and `.github/scripts/agile-park.js` so an `aca` environment is parked, resumed and re-parked on those times, with no yes (the row is it) |
| `sync.js merge <path>` (the plugin's script, run from the project root) | Prints the three-way merge of a file you edited (your copy, the base copy of the last sync, the plugin text as the sync would copy it, the manual's links included) and writes nothing; exit 1 means conflict markers to resolve, keeping your sections. `/agile:sync` runs it, you rarely will |
| `/agile:publish --beta` | A desktop app's beta of `main` only (`<Version>-beta` to the share; no tag, no Release): the rerun of a ship's failed beta step |
| `/agile:publish [<environment> [v<x.y.z>]]` | The app version on `main` as a release: package and zip per deployable project (a desktop head with an update source: Velopack packages and feed, an AppImage for an Avalonia head's `linux-x64`, attached to a public GitHub repository's release when that is the source; a Hybrid app's site: its two NuGet packages too), notes, annotated tag, push and GitHub Release; with an environment, also its deploy by the command `docs/infra.md` declares (`v<x.y.z>`: a rollback to that tag); the tag it pushes also starts the deploy pipeline when the project has one; the deploy lands in the client's cloud account the row of `## Cloud accounts` declares (an Azure, AWS or Google Cloud login is checked first). Typing it is the authorization |

### When to use each command

| Command | Use it when | Example in section 14 |
|---|---|---|
| `/agile:bootstrap` | A new app with a filled `product/brief.md` and no `CLAUDE.md` of agile yet; `/agile:bootstrap <round>` to resume a quiz you stopped | 14.1, 14.40, 14.54, 14.55 |
| `/agile:discuss` | You have an idea with no shape and want options before deciding anything | 14.7 |
| `/agile:epic` | An epic is too big for one session and you want it broken into features | 14.7 |
| `/agile:idea` | You thought of something mid-work and want it on the board without stopping | 14.2, 14.5, 14.48 |
| `/agile:refine` | An item is an idea and you are ready to answer its questions and approve it | 14.2, 14.16 |
| `/agile:screen` | The feature has a new or complex screen; simple forms and lists do not need it | 14.8, 14.40 |
| `/agile:build` | The item is approved; `--worktree` only when a second item must run while another is building | 14.2, 14.9, 14.46, 14.82 |
| `/agile:review` | A change is risky (data, money, permissions) and you want eyes with no context before validating | 14.8 |
| `/agile:change` | You changed your mind on an approved or building item; a done item gets a new item instead; something outside the work stops the item (`--block`) | 14.3, 14.40, 14.67 |
| `/agile:script` | A step of the validation script is wrong or outdated after a fix; a text that changes a criterion goes to `/agile:change` instead | 14.62 |
| `/agile:ship` | You validated the item and authorize the merge | 14.2, 14.21 |
| `/agile:retro` | After a ship or at the end of a session, to keep at most 3 lessons as rules; with an item id or `session` to pick the source | 14.37, 14.40 |
| `/agile:pause` | You are stopping for now; nothing stays only on disk | 14.6, 14.40 |
| `/agile:status` | At the start of a session, or to see what is blocked and on whom | 14.6, 14.20 |
| `/agile:sync` | After a plugin update, before the next item | 14.10, 14.54 |
| `/agile:identity` | The app has screens and no visual identity recorded, or you want to review the one there is | 14.22 |
| `/agile:autopilot` | A small, well-understood item you want in one run with two stops; `--assume` when the recommendations are fine with you | 14.13 |
| `/agile:version` | You want to know which plugin version this session runs and whether the project's copies are behind | 14.10 |
| `/agile:publish` | `main` holds a version you want packaged as a release; with an environment, also deployed; with `v<x.y.z>`, a rollback; with `--beta`, a desktop beta the ship could not send | 14.29, 14.35, 14.44, 14.50, 14.51, 14.55, 14.59, 14.60, 14.64 |

## 16. Command flows

One diagram per command: what you do, what Claude does, and where it stops to ask you. Rectangles are Claude's steps, diamonds are your decisions, rounded boxes are the result.

### /agile:bootstrap
```mermaid
flowchart TD
    A["product/brief.md exists?"] -->|no| A1(["Template copied; fill it and run again"])
    A -->|yes| B["Read the brief and the code base it names"]
    B --> C["Rounds 1 to 8, one message each:<br/>questions with recommendation and reason"]
    C --> C1["mobile on an existing API (2d): skip what the API decides;<br/>propose the app's ideas from the site's ticked list, or the gate issue on the API's board"]
    C1 --> C1b["mobile, or desktop with an API (2f): online or offline-first;<br/>offline-first writes the Offline line and the Offline foundation idea"]
    C1b --> C2["web-app or website with an app from day one (2e): the brief's features to untick;<br/>later Contracts (+ Shared with Hybrid) and the epic 'Mobile app', no app project yet"]
    C2 --> D{"Your answers ('ok' accepts)"}
    D --> E["Closing question: which domain concept worries you most?"]
    E --> F["Summary of every decision"]
    F --> G{"'confirmo'?"}
    G -->|no| C
    G -->|yes| H["Generate: CLAUDE.md, ADR-0001, profile, workflow copies,<br/>templates, rules, glossary, infra, manual index"]
    H --> I{"Epics on the board (website: 'Website sections')? Skeleton?"}
    I -->|yes| J["Create, build, test once, warnings baseline, sync record"]
    J --> K{"Authorize the first commit?"}
    K -->|yes| L(["Committed on main; next: /agile:epic or /agile:refine"])
```

### /agile:idea
```mermaid
flowchart TD
    A["Text from the chat"] --> B["Classify: epic, feature or bug"]
    B --> B1["Look for a similar item, before any number is taken"]
    B1 -->|candidate| B2{"Card: improve the existing item,<br/>capture anyway, or already delivered"}
    B2 -->|improve, or nothing to create| B3(["No number, no file, no board item"])
    B2 -->|capture anyway| B5{"Class"}
    B1 -->|"none: 'No similar item (N items read)'"| B5
    B5 -->|epic| D
    B5 -->|feature or bug| C["Read the open epics (board, docs/epics, backlog, headers);<br/>a caller that names the epic skips the question"]
    C --> C1{"Ask: fitting epics first, then New epic, then No epic"}
    C1 -->|"New epic: yes"| C2["Create the epic on the board and in docs/epics, status draft"]
    C2 --> D
    C1 -->|"an epic or none"| D["Next id, English slug, file from the template<br/>with status: idea; header, Summary and Start<br/>(depends on, waits on to start, needed to validate, path, parallel — unknown if nobody said);<br/>a cause named with its evidence, or 'Cause not verified'"]
    D --> E["Mirror on the board; board id in the header"]
    D -.-> D0["epic and discuss search the whole list once, one card for every collision"]
    E --> F(["Nothing else until /agile:refine <id>"])
```

### /agile:discuss
```mermaid
flowchart TD
    A["An idea with no shape yet"] --> B["Context: brief, decisions, the code it touches"]
    B --> C["Options with trade-offs, one recommendation"]
    C --> D{"Your choice, or more questions"}
    D -->|decide| E["docs/discussions/D-n: decisions, parked points"]
    D -->|park| E
    E --> F["Items that came out are captured as idea"]
    F --> G(["No spec, no code; next: /agile:refine or /agile:epic"])
```

### /agile:epic
```mermaid
flowchart TD
    A["An epic to plan"] --> B["Read brief, existing items and code"]
    B --> B1{"The profile or a complement it names<br/>has an Epic section for it? (mobile app on a site)"}
    B1 -->|yes| B2["Follow it: where the app lives, its UI,<br/>the site's features to tick, Mobile foundation first"]
    B1 -->|no| C
    B2 -->|new repository| B3["Skip the UI question; the site's epic is 'API for the mobile app':<br/>Mobile API foundation, then API for each ticked feature"]
    B3 --> C
    B2 --> C["Session-sized features: value, priority, size,<br/>depends on, waits on (and from whom), screen design yes/no"]
    C --> D["Execution plan: order, suggested path, what runs in parallel;<br/>what waits outside the epic; first release cut"]
    D --> E{"Agree with the breakdown?"}
    E -->|change| C
    E -->|yes| F["docs/epics/<slug>.md; each feature captured as idea<br/>with its Start, and mirrored on the board"]
    F --> G(["Next: /agile:refine <first feature that waits on nothing>"])
```

### /agile:refine
```mermaid
flowchart TD
    A["Item in idea or refining"] --> A0["Look for a similar item, before the branch exists"]
    A0 -->|candidate| A00{"Card: carry the improvement over and cancel this one,<br/>keep both, or fold the other in"}
    A00 -->|"carry over and cancel"| A01(["status: cancelled; no branch or worktree<br/>(a resume removes them); board by the mapping"])
    A00 -->|"keep both, or fold"| A1
    A0 -->|"none: 'No similar item (N items read)'"| A1["Branch and worktree of the item, before any writing;<br/>an uncommitted item file is moved in"]
    A1 --> B["status: refining in the worktree; read brief, profile, code it touches"]
    B --> C["Check every premise in the code"]
    C -->|a premise is false| C1["Say so first"]
    C1 --> D
    C -->|a bug| C2["Cause in the code, file:line; a business rule:<br/>search the solution for the same rule implemented again"]
    C2 --> C3["Write ## Cause with every occurrence; re-read the file"]
    C3 --> D
    C --> D["One round as quiz cards, one per topic, recommended option first:<br/>use cases, rules (duplicates: fix now or debt), permissions, states, screens, data,<br/>packages for code and tests, out of scope"]
    D --> E{"Your answers"}
    E --> F["Fill the file: UC, BR, screens and API,<br/>acceptance criteria, decisions, out of scope"]
    F --> G{"At most one follow-up round needed?"}
    G -->|yes| D
    G -->|no| H["Summary; ask you to read the file"]
    H --> I{"'aprovo F-n' and no open question?"}
    I -->|no| H
    I -->|yes| J(["status: approved; board and commit; next: /agile:build"])
```

### /agile:screen
```mermaid
flowchart TD
    A["Feature in refining with a new or complex screen"] --> B["Delegate to the ux-designer agent: paths only,<br/>alone in the item's worktree"]
    B --> C["It writes the screen section: layout, states, actions, messages, permissions"]
    C --> D["and the standalone HTML mockup: every state,<br/>three languages, kit patterns only"]
    D --> D1["Claude reads both files and says what it verified"]
    D1 --> E{"Its open points, then your questions and changes"}
    E -->|change| C
    E -->|fine| F(["Approved together with the feature"])
```

### /agile:build
```mermaid
flowchart TD
    A0["Find the item's worktree by branch (refs/heads/branch in git worktree list --porcelain);<br/>none: create it, branch without -b when it exists; say: Worktree of id: path [branch]"] --> A["Item approved, read in that worktree?"]
    A -->|already building| A2["Resume: branch, last wip commit, next step of the plan"]
    A2 --> D
    A -->|no| A1(["Stop: refine and approve first"])
    A -->|yes| B["Another item building or validating?"]
    B -->|yes, no --worktree| B1(["Stop and name it"])
    B -->|yes, --worktree| B2{"Can the two collide? Confirm parallel work"}
    B2 -->|yes| C2["The item's worktree, from the refinement; work only there"]
    B -->|no| C["The item's worktree; status: building; plan in 8 steps"]
    C2 --> D
    C --> P0{"Creates a project, a contract,<br/>a message between modules or a schema change?"}
    P0 -->|yes| P1["system-design proposes slice, contracts, data, risks;<br/>architect reviews it; both read-only, nothing written"]
    P1 --> P2["Claude verifies both, writes the plan from them,<br/>records what it accepted in ## Decisions"]
    P2 --> C3
    P0 -->|no| C3{"Mockup approved with the item?"}
    C3 -->|yes| C4["The frontend agent implements that screen and its tests,<br/>in the foreground and alone in the worktree, after Claude removes what the plan lists;<br/>Claude runs the whole suite, reads it, fixes in one round, answers its stops"]
    C4 --> D
    C3 -->|no| D["Code by the profile and rules; tests per criterion;<br/>a bug: one regression test per occurrence fixed;<br/>a feature that states 'fails today' on a named fixture:<br/>build and run it first, show the failure, then the fix passing;<br/>affected tests only; small commits, branch checked first"]
    D --> E{"False premise or impossible criterion?"}
    E -->|yes| E1["Options A/B → /agile:change"]
    E1 --> D
    E -->|no| F["App-host check; screen check in both themes"]
    F --> G["Stop app hosts you started; build with no new warnings"]
    G --> H["Coverage table: criterion → test through the path a user reaches;<br/>a method nothing calls is a gap"]
    H --> I{"Risky change?"}
    I -->|yes| I1["/agile:review"]
    I1 --> J
    I -->|no| J["Validation script, at most 8 steps"]
    J --> K(["status: validating; you validate on screen"])
```

### /agile:review
```mermaid
flowchart TD
    A["Risky change before validation"] --> B["Base commit = merge base with main"]
    B --> C["Reviewer agent, fresh context, review model:<br/>item file, profile, rules, diff — paths only"]
    C --> D["Findings: blocker / major / minor, file:line"]
    D --> E["Claude checks every blocker and major in the code"]
    E --> F["Table: finding, verdict, action"]
    F --> G{"Your triage"}
    G --> H["Fix confirmed blockers and majors; tests; rerun"]
    H --> I(["One line per finding in ## Decisions; build continues"])
```

### /agile:change
```mermaid
flowchart TD
    A["Change of mind, or a wrong premise, on an approved or building item<br/>(status read in the item's worktree, found by branch; none: stop, never create)"] --> B["Change note: what changed, why,<br/>which criteria are affected"]
    B --> C["New version of the file; affected criteria rewritten"]
    C --> D{"Re-approve the affected criteria only"}
    D -->|yes| E(["Work continues; the rest stays approved"])
    A -.->|"--block with reason and who"| F(["status: blocked + Blocked line + label; no commit"])
    F -.->|"--unblock, slot free"| G(["Back to the status it left; line and label removed"])
```

### /agile:script
```mermaid
flowchart TD
    A["Item id + optional text<br/>(status read in the item's worktree: building or validating)"] --> B{"Text changes a criterion?"}
    B -->|yes| C(["Nothing written: /agile:change is printed"])
    B -->|no| D["Only the steps the text reaches, or the whole script with no text;<br/>changed terminal steps run in Git Bash and PowerShell 7"]
    D --> E(["File + commit docs(F-n): validation script + issue body sent whole and read back"])
```

### /agile:ship
```mermaid
flowchart TD
    A["status: validating, read in the item's worktree (found by branch; none: stop, never create),<br/>and you said 'validado'?"] -->|no| A1(["Ask"])
    A -->|yes| B["Branch up to date with main; typing ship counts as IDE and app host closed"]
    B --> C["gate.js ship: full rebuild, full suite, architecture tests;<br/>output saved whole, last line GREEN or RED: what failed"]
    C -->|red| C1["Fix on the branch"]
    C1 --> C
    C -->|green| D["App manual in pt-BR, pt-PT, en; DocGen references and the hand-written overview;<br/>gate.js docs: the declared docs command, or DocGen implicitly;<br/>RED stops like the gate; infra.md; baseline"]
    D --> D1["App version in Directory.Build.props: added (0.1.0, no bump) when missing, moved from a .csproj when it sits there,<br/>then MINOR (feature), PATCH (bug) or MAJOR (breaking-change decision);<br/>a MAUI head also gets ApplicationDisplayVersion and ApplicationVersion + 1"]
    D1 --> E2["The command is the merge authorization; current branch is main; read main..branch:<br/>stop when a commit carries another item's id"]
    E2 --> F["Main moved during the ship? Merge it in, full check, bump from main's, go on (conflict or red stops);<br/>merge --no-ff, read its exit status, then push; lock probe, then verify 0 0, worktree gone,<br/>branch deleted (on origin only when ls-remote lists it)"]
    F --> G["Decisions naming a file are true in that file; ## Delivery; status: done"]
    G --> H["Close the board item with evidence;<br/>set Status to Done explicitly and read it back;<br/>not stuck after one retry: report the command by hand"]
    H --> H2["Desktop with '- Beta channel: every merge': publish.js beta from the main checkout,<br/>main as Version-beta to the share; a failure keeps the merge and names /agile:publish --beta"]
    H2 --> I["Retro: at most 3 lessons"]
    I --> J(["Next item at the top of the backlog"])
```

### /agile:retro
```mermaid
flowchart TD
    A["A shipped item, or a session"] --> B["At most 3 lessons that change future work;<br/>a cause shows its evidence, or says 'cause not verified'"]
    B --> B2["Score each lesson twice, 0 to 10: plugin and project, each with a reason;<br/>7 or more cites its evidence, else it stops at 6"]
    B2 --> C["Classify each: project rule, project setting,<br/>build check, template tweak, plugin note, nothing"]
    C --> D["Show lessons by score with destination and exact line;<br/>both scores 2 or less: last, 'not recommended'"]
    D --> E{"Approve each one"}
    E --> F["Apply; CLAUDE.md under ~900 words; one line per rule"]
    F --> F2["A new rule about a test pattern: sweep the existing tests;<br/>fix each hit or capture it as a bug"]
    F2 --> G["retro-log.md entry; a plugin note (plugin: name, from the Plugins: line)<br/>gets a ⏳ row with its plugin, scope and score in the text; an old table gains the Plugin column;<br/>it cites agile-canary#N once it has one, and gets its ✅ at /agile:sync"]
    G --> H(["Commit on main: your pick of lessons authorizes it under a ship; run alone, it asks"])
```

### /agile:pause
```mermaid
flowchart TD
    A["/agile:pause, or 'vamos parar' in any words"] --> B["git status in the checkout and every worktree"]
    B -->|nothing to save| B1(["One line: nothing in progress"])
    B --> C{"Uncommitted work on an item branch?"}
    C -->|on main| C1(["Stop and show git status"])
    C -->|yes| P["## Paused in the item file: where we stopped,<br/>Next, pending decisions, what is running"]
    P --> D["Branch checked; commit wip(F-n): what is half done"]
    D --> E["The same pause note in the chat"]
    E --> F(["Status unchanged; resume with /agile:build <id>"])
```

### /agile:status
```mermaid
flowchart TD
    A["Read-only"] --> B["Git: branch, uncommitted files,<br/>every worktree and its item status"]
    B --> C["Files: item in progress, approved items with open questions,<br/>items whose Start depends on an item not done or waits on something to start"]
    C --> D["Board: top 5, drift from the files"]
    D --> E["Retro log: ⏳ plugin notes, count per plugin"]
    E --> F(["Short report: in progress, next step, waiting on you, blocked and on whom,<br/>uncommitted work, backlog head (first one that can start), board drift,<br/>plugin notes waiting per plugin"])
```

### /agile:sync
```mermaid
flowchart TD
    A["Plugin updated; working tree clean; no item in progress?"] -->|no| A1{"Continue anyway?"}
    A1 -->|no| A2(["Wait for the item to finish"])
    A -->|yes| B["sync.js plan: recorded version → plugin version;<br/>one state per file; a Complement: line adds its own copy<br/>(docs/agile/profile-mobile-client.md)"]
    A1 -->|yes| B
    B -->|all same| B1(["Nothing to do"])
    B --> C["Table: new, update, edited, manual; a proposal per file"]
    C --> D{"Approve per file"}
    D --> E["Copy new and update; merge edited by hand;<br/>propose each manual edit (a missing Worktrees: or Plugins: line too);<br/>never touch project rules; no warnings baseline: offer the first one, gate.js baseline with your yes"]
    E --> E2["Something only bootstrap installs and this project lacks:<br/>name it and offer to capture a feature"]
    E2 --> E3["DocGen with no docs command declared:<br/>offer the block, sync.js docs writes it into build.json"]
    E3 --> E4["Backend: external and the API reachable:<br/>report when its openapi.json moved past the pinned commit;<br/>on the site's shared package: report when its latest release is past the pinned version"]
    E4 --> F{"Build files or checked rules changed?"}
    F -->|yes| G["Build, full suite, fix findings, baseline"]
    F -->|no| H
    G --> G2["Delivered plugin notes, when approved:<br/>sync.js notes marks them ✅"]
    G2 --> H["sync.js record; retro-log entry"]
    H --> I{"Authorize the commit on main?"}
    I -->|yes| J(["chore: sync with agile@canary <version>"])
```

### /agile:identity
```mermaid
flowchart TD
    A["Identity already in docs/design/?"] -->|yes| A1{"Table of it: what changes?"}
    A -->|no| B{"23a: file, URL, image or none?"}
    A1 --> B
    B -->|file| C["Validate DTCG, or convert DESIGN.md to tokens"]
    B -->|URL or image| D["Read the page or the image; image colors marked estimated"]
    B -->|none| E["Primary color, font, light/dark/both; derive the palette"]
    C --> F["Table: roles, fonts, radius, icon family, WCAG AA of every pair, both themes"]
    D --> F
    E --> F
    F --> G{"Failing pair: nearest passing value; 'confirm'?"}
    G -->|change| F
    G -->|yes| H["Write docs/design/identity.tokens.json, then DESIGN.md front matter from it; compare the two"]
    H --> I["ADR-0001 line; UI kit exists: capture 'Apply the identity to the theme'"]
    I --> J(["No application code touched"])
```

### /agile:autopilot
```mermaid
flowchart TD
    A["Item not done"] --> R{"Autopilot: line in the file?"}
    R -->|yes| R1["Resume from that step"]
    R -->|no| A1{"Start: depends on an item not done,<br/>or waits on something to start?<br/>(Needed to validate never stops it)"}
    A1 -->|yes| A2(["Says what it waits on and from whom; stops"])
    A1 -->|no| B["Refinement: code read, premises verified;<br/>screen-design when there is a new or complex screen"]
    B --> B1{"--assume?"}
    B1 -->|no| C(["STOP 1: question cards, packages,<br/>draft criteria, mockup, 'Aprovo F-n?'"])
    B1 -->|"yes, new package needed"| C2(["STOP 1 with the package question only"])
    B1 -->|"yes, no new package"| D["Recommended answers recorded<br/>as 'assumed by autopilot'"]
    C --> C1{"Your answer"}
    C1 -->|"other answers"| C
    C1 -->|"Aprovo F-n, screen as recommended"| E
    C1 -->|"Aprovo F-n, screen changed"| C3(["Extra stop: regenerated mockup"])
    C3 --> E
    C2 --> D
    D --> E["status: approved"]
    E --> F["Build: branch, code, tests, gate,<br/>app host and screen checks"]
    F --> F0{"Risky change?"}
    F0 -->|yes| F1["change-review by itself;<br/>fix confirmed blockers and majors"]
    F1 --> G
    F0 -->|no| G(["STOP 2: status validating; coverage table,<br/>findings, assumptions, validation script"])
    G --> H{"Your answer"}
    H -->|"a defect"| H1["Fix, regression test"] --> G
    H -->|"validado"| H2(["Stays in validating; /agile:ship F-n later"])
    H -->|"validado e autorizo o merge de F-n"| I["feature-ship: full suite, manual in 3 languages,<br/>merge --no-ff, board, retro"]
    I --> J(["done"])
    F -->|"package, schema, money, permissions, shared data,<br/>false premise, gate red 3 times"| S(["Ends: where it stopped, what is done,<br/>the command to continue"])
    I -->|"red suite, merge conflict"| S
```

### /agile:version
```mermaid
flowchart TD
    A["Read the running version (plugin.json),<br/>the installed one (claude plugin list)<br/>and the project's (sync.json, workflow header)"] --> B{"Same version?"}
    B -->|yes| B1(["Up to date"])
    B -->|running is newer| C(["Next step: /agile:sync, between features"])
    B -->|project is newer| D(["Update the plugin, then a new session"])
```

### /agile:publish
```mermaid
flowchart TD
    A["Plan: main checkout, on main, clean, level with origin;<br/>one version on the projects; the tag of that version free (a release only: a promotion or a rollback of a tag already released skips this)"] -->|"a check fails"| A1(["Stops with the reason, nothing written"])
    A -->|ok| B["Shows version, previous tag, items since it, projects and runtimes<br/>(typing the command is the authorization: no question)"]
    B --> C["dotnet publish -c Release per project and runtime;<br/>drops appsettings.Development.json and the .xml; one zip each"]
    C -->|"publish or zip fails"| C1(["Stops: nothing committed or tagged"])
    C --> C2["A Hybrid app's site: dotnet pack of Contracts and Shared at the release's version into packages/<br/>(no GITHUB_PACKAGES_TOKEN or origin off github.com: left out, with the reason)"]
    C2 -->|"pack fails"| C1
    C2 --> D["Writes the notes in docs/releases/ (one file per version, a glossary link under the title)"]
    D --> D2["A mobile head: signed Android .aab (signing from four environment variables)<br/>and docs/releases/v<Version>-store.md, the store checklist"]
    D2 --> E["Commit on main (notes, checklist, glossary rows the notes needed), annotated tag with the notes, push main and tag"]
    E -->|"push fails"| E1(["Names the command to rerun; commit and tag stay"])
    E --> E2(["With .github/workflows/deploy.yml: the pushed tag starts the pipeline,<br/>which runs the same Deploy command on its target (the plan said so before)"])
    E --> F{"origin on github.com and gh installed?"}
    F -->|yes| G(["GitHub Release with the notes, no binaries"])
    F -->|no| H(["One line saying why; the tag stands"])
    G --> I(["Packages pushed to GitHub Packages with --skip-duplicate;<br/>a failure names the push command to rerun"])
    H --> I
    I --> J{"An environment named?"}
    J -->|no| J0(["Done: the release only"])
    J -->|yes| K["Checks docs/infra.md: the environment, its command (not declared: says how to declare it),<br/>the tag (a rollback needs it to exist), every secret set by name, Aspire packages = CLI version"]
    K -->|"a check fails"| K1(["Stops with the reason, nothing ran"])
    K --> L["Runs the command in worktree root/deploy, detached at the tag;<br/>output saved outside the repository; Check URL polled for 200 (60 s)"]
    L -->|"exit not 0, or no 200"| L1(["Nothing recorded, nothing rolled back;<br/>gives /agile:publish for the version recorded before"])
    L --> M(["Records v<x.y.z> (date) in docs/infra.md: commit on main and push"])
```

## 17. Glossary

The technical terms this manual uses, with the pt-BR word Claude uses when talking to you. A project receives this manual as `docs/agile/workflow.md`, so the meanings are one click from the text; the project's own terms live in its `docs/glossary.md`.

| Term | pt-BR | Meaning |
|---|---|---|
| ADR (architecture decision record) | ADR (registro de decisão de arquitetura) | A short file that records a decision expensive to reverse, with its reason; ADR-0001 holds every quiz answer. |
| worktree | worktree (cópia de trabalho) | A second folder of the same repository on its own branch, so two items never share files. |
| merge base | base do merge | The commit where the item branch left the main branch; a review compares from it. |
| gate | gate (portão) | An automatic check that must pass before the work goes on: build, tests, no new warnings. |
| warnings baseline | baseline de avisos | The build warnings accepted so far; the gate fails only on new ones. |
| acceptance criterion (AC) | critério de aceite | A Given/When/Then sentence that a test proves. |
| profile | perfil | The architecture chosen at bootstrap (monolith, web-app, mobile...), copied to `docs/agile/profile.md`. |
| Mermaid | Mermaid | A text format for diagrams that the board and the editor draw. |
| OpenAPI | OpenAPI | A JSON document that describes every endpoint of an API; tests and tools read it. |
| DocGen | DocGen | The project's own tool that generates `docs/architecture/` from the code. |
| DbContext | DbContext (contexto do banco) | The Entity Framework class that maps tables to code for one module. |
| .NET Aspire (app host) | .NET Aspire (host da app) | Microsoft's tool that starts the app and its services (database, mail catcher) together for local work. |
| MAUI | MAUI | Microsoft's framework for one mobile or desktop app on several platforms. |
| Blazor Hybrid | Blazor Híbrido | Web screens written once and shown inside a MAUI app. |
| Velopack | Velopack | The tool that packs a desktop app and lets installed copies update themselves. |
| SmartScreen | SmartScreen | The Windows warning shown for an installer that is not code-signed, or signed but downloaded by few people yet ("unrecognized app", with the publisher's name). |
| code signing | assinatura de código | A certificate's signature on the installer and the app's files: Windows shows the publisher's name instead of "Unknown publisher", and the reputation of the name carries from one release to the next. |
| beta channel | canal beta | A second line of updates of a desktop app, fed by every merge, that only the PCs installed from the beta Setup.exe follow. |
| AppImage | AppImage | A Linux app in one file: made executable with `chmod +x`, it runs with nothing installed, and Velopack updates it in place. |
| WSL (WSLg) | WSL (WSLg) | Linux running inside Windows; WSLg shows its windows on the Windows desktop. |
| SemVer | versionamento semântico | The version number `MAJOR.MINOR.PATCH`: a breaking change, a feature, a fix. |
| DTCG | DTCG (tokens de design) | The W3C format for design tokens, used for the app's visual identity. |
| WCAG | WCAG | The accessibility guidelines; AA is the contrast level the identity is checked against. |
| CI pipeline | pipeline de CI | A job that runs on the server when something is pushed, such as the deploy of a tag. |
