---
adr: 0001
status: accepted
date: 2026-09-17
---
# ADR-0001: Foundation

Technical terms: [glossary](../glossary.md)

## Context
Simulab is described in `product/brief.md`: a study coach that runs realistic practice exams and tells each student what to study next. The team is the product owner and Claude, working with the agile@canary workflow.

A large part of the code comes from Simulae (`D:\dev\_icontrol\simulae`, branch `develop`, read on 2026-09-17). Facts verified there and used as reasons below:
- .NET 10, Central Package Management, modular monolith with five Clean Architecture projects in every module: Identity (157 `.cs` files), ContentCatalog (130), ExamEngine (48), Billing (19).
- Separate `Api` and `Web` hosts, Aspire AppHost, ServiceDefaults. Blazor Interactive Server on every page (71 `.razor` files), MudBlazor 9.9, custom theme.
- No localization at all: no `.resx`, UI text hardcoded in Portuguese, Portuguese page and identifier names.
- PostgreSQL with one schema and one `DbContext` per module; `TenantEntity` with nullable `TenantId`, audit fields and soft delete; global catalog rows already use `AreNullsDistinct(false)` (Simulae bug #671).
- ASP.NET Identity (`User : IdentityUser<Guid>`) + OpenIddict local server (password, refresh token and authorization code flows), Google sign-in, TOTP, consent records, RBAC (`Permission`, `RolePermission`, `IPermissionQueryService`).
- Billing holds `Plan` (features and limits as columns) and `PlanAssignment` (no validity period, no source). No payment code, no usage meter, no promo codes.
- Integration events abstraction in `Common`, implemented with MassTransit 8.5.10 + RabbitMQ, used once. Redis used for the exam timer, the attempt lock, refresh tokens and the SignalR backplane. `IEmailSender` with SendGrid. No file storage code, no AI code.
- About 1,360 tests (xUnit, AwesomeAssertions, NSubstitute, bUnit, Testcontainers); 42 test files create or reference their own PostgreSQL container. Architecture tests exist. The build has no warnings-as-errors and no analyzers.
- Hosted on GitHub (`anthropics-usable/simulae`) with a develop / main flow and Bicep for Azure Container Apps (Brazil South).

Everything imported from Simulae is renamed to English and localized on the way in. Code is imported feature by feature, never in bulk.

## Decision

### Round 1 — Shape
| # | Question | Decision | Reason |
|---|---|---|---|
| 1 | Type of app | Web app + API | Brief: "Web app first"; native mobile is out of scope |
| 2 | Architecture profile | modular-monolith | Several business areas with clear boundaries, team of two; same as Simulae |
| 2b | Module structure | Five projects (Clean Architecture) for modules imported from Simulae: Identity, Catalog (ContentCatalog), ExamEngine, Plans (Billing). One project + `Contracts` for new CRUD-like or orchestration modules. Decided per module | Flattening about 350 imported files is rework without gain; five projects for simple new modules is ceremony. Consequence: two module shapes coexist and the architecture tests know both |
| 3 | Hosts | Separate `Api` and `Web` hosts + Aspire AppHost | Simulae's Web already consumes the API this way; keeps the door open for mobile |
| 4 | Runtime and UI | .NET 10, Blazor Interactive Server, MudBlazor | Same as Simulae; admin-heavy app. A dropped connection during a timed exam is handled with server-side autosave of answers and reconnection |
| 5 | Deployment target | Containers + Aspire locally. Azure Container Apps is the planned target (Simulae Bicep reusable), created only near the first release | No environment cost before there is something to publish |

### Round 2 — Data
| # | Question | Decision | Reason |
|---|---|---|---|
| 6 | Database | PostgreSQL 15+, one database, one schema and one `DbContext` per module | Same as Simulae; keeps module isolation |
| 7 | Multi-tenancy | Shared database with nullable `TenantId` kept in the model and dormant (option B). No institution features in v1; every user is B2C (`TenantId` null). Tenant filters are tested from the start | The brief leaves institutions open. Keeping the column costs almost nothing; removing and re-adding it later is a migration of every table |
| 8 | Global data | Yes: catalog, taxonomy and published questions are global (`TenantId` null). Every unique index that includes `TenantId` is `NULLS NOT DISTINCT` | Known trap, already paid for in Simulae bug #671 |
| 9 | Deletion | Soft delete. LGPD/GDPR erasure anonymizes the student's personal data and keeps unidentified attempts for question statistics | Questions and exams are referenced by attempt history |
| 10 | Auditing | Created/updated by and at through interceptors everywhere. Full history (versions) only for question content | Brief requires annulled questions and changed answer keys; an attempt must know which version graded it |
| 11 | Migrations | One fresh initial migration per module, English names, no Simulae migration history. Assumption: no Simulae production data to migrate besides the municipal guard seed | Everything is renamed on the way in |

### Round 3 — Access
| # | Question | Decision | Reason |
|---|---|---|---|
| 12 | Authentication | ASP.NET Identity + OpenIddict local server | Self-service sign-up; separate hosts need tokens; external IdPs charge per active user or add a service to run, and would discard tested code. Rule: the password flow is first-party only; a future mobile app uses authorization code + PKCE |
| 13 | Social login / MFA | Google sign-in and TOTP code come along; both switched off by configuration in v1 | Owner's decision; nothing is deleted, turning them on is configuration |
| 14 | Authorization | RBAC with configurable permissions. Seed roles: Student, Curator, Admin. Pages, menus and endpoints check permissions, never role names | Brief asks for roles with permissions; many back-office screens |
| 15 | Plans module | Import Simulae Billing renamed `Plans`; plans are separate from roles | It has no payment code; it is exactly "plans assigned by an admin" |
| 15a | Entitlement model | Effective entitlement = base plan (Free by default) + at most one active time-bound grant. `PlanAssignment` gains `ValidFrom`, `ValidUntil` (null = open-ended) and `Source` (Admin, PromoCode). Falling back to the base plan is computed at read time | Smallest change to existing code; also lets an admin grant a plan for a fixed period |
| 15b | Promo codes | A code grants a target plan for N days, not loose feature toggles. AI limits come from that plan | One source of truth for what the user can do; a new campaign is a new trial plan, not new code |
| 15c | Promo code rules | Campaign label, target plan, duration, redemption window, maximum redemptions, one redemption per user per campaign, verified email required, rate-limited attempts. Admin creates, pauses and sees redemptions | A leaked code must not become unbounded AI cost |
| 15d | AI quota | Usage meter per user and period; the AI gateway checks it against the effective plan before each call; trial plans carry a hard cap | Brief: AI cost per active student stays within the plan margin; a trial has no revenue |
| 15e | Entitlement check | One `IEntitlementService` in `Plans.Contracts` (`HasFeature`, `GetLimit`, `GetRemaining`). Nobody reads plan columns directly | RBAC decides who may; the plan decides what is included. One place applies expiry |
| 15f | Trial expiry | Banner with days left. After expiry the coach history stays readable and the study plan stays visible but frozen; new AI calls are blocked with an upgrade or contact message (no checkout in v1) | Hiding what the student built punishes those who tried the product |
| 16 | Back office | Admin area in the same Web app, guarded by permissions | A second app duplicates layout, auth, i18n and deployment |
| 17 | Age / consent | v1 keeps the 18+ self-declaration and consent records. Parental consent is captured as an epic idea, to be resolved before marketing to ENEM and vestibular students | Verifiable parental consent is expensive and differs between LGPD and GDPR. Accepted consequence: much of the ENEM/vestibular audience is formally excluded until then |
| 18 | Teacher / grader roles | Not in v1 | They depend on institutions and essay grading; RBAC lets them be added as data |

Known exception: `User` inherits `IdentityUser<Guid>`, so it cannot inherit `TenantEntity`; it repeats the tenant, audit and soft-delete fields.

### Round 4 — Integration
| # | Question | Decision | Reason |
|---|---|---|---|
| 19 | Messaging | Keep the integration events abstraction with an in-process implementation. MassTransit and RabbitMQ are not imported | One process, one event in use; MassTransit 8.5.10 is the last free line and is frozen |
| 20 | Background work | Job table in PostgreSQL + hosted worker in the `Api` host | AI import takes minutes; volume is low; jobs survive a restart and show progress |
| 21 | Redis | Kept: exam timer, attempt lock, refresh tokens, SignalR backplane | Core of the Exam Simulator |
| 22 | AI provider | Claude API behind `IAiGateway` in a new `Ai` building block. Import: PDF and scanned-image reading with structured output. Coach: tool calls over the app's own data. Every call records user, purpose, tokens and cost | Brief: the coach answers from the app's data and must not invent facts; per-call records enable plan limits and cost per active student; the abstraction avoids provider lock-in |
| 23 | File storage | Blob storage behind `IFileStorage`; Azurite through Aspire in dev; private containers; the original exam file is kept | The curator compares the draft with the original during review |
| 24 | Email | `IEmailSender`; Azure Communication Services Email over HTTP with the Api's managed identity in the cloud (amended by F-66, 2026-10-06: SendGrid retired its free plan and ACS lives in the ADR-0002 subscription), Mailpit through Aspire in dev; provider chosen by `Email:Provider`; templates in three languages | Code exists; local capture tests sign-up and password reset; staging sends to a real mailbox |

### Round 5 — Experience
| # | Question | Decision | Reason |
|---|---|---|---|
| 25 | Languages | pt-BR, pt-PT and en from day one, app and manual | Brief |
| 26 | Resource layout | `.resx` per functional area plus a shared set; `en` is the neutral resource; the API returns error codes and the Web translates | One file is a merge bottleneck; translating in the API duplicates languages |
| 27 | Content language | Question content is not translated; each exam and question carries its own language; the coach answers in the user's UI language | A translated exam is no longer the real exam |
| 28 | Default language | Per user (profile), then browser, then `en` | Several devices; emails need the user's language |
| 29 | Accessibility | WCAG 2.2 AA. The exam timer is announced accessibly without interrupting | Public service exams have candidates with disabilities |
| 30 | Design system | `SimulabTheme` starting from Simulae's theme (light and dark). Its values are recorded in `docs/design/identity.tokens.json` (source: `SimulabTheme`, F-63; `IdentityTokensTests` keeps both equal), primary `#216DB5` in light mode (`#2478C5` until B-8) | Theme is ready; revisit when Simulab has its own brand |

### Round 6 — Operations
| # | Question | Decision | Reason |
|---|---|---|---|
| 31 | Observability | Structured logs + OpenTelemetry traces and metrics; Aspire dashboard locally; a custom metric for AI tokens and cost | Code exists; brief asks to track AI cost |
| 32 | Environments | Dev only. Staging and production are `planned` (Azure Container Apps, Brazil South) | Nothing to publish yet |
| 33 | Code hosting and CI | GitHub, repository `alexcanario/simulab`. CI = build + tests on pull requests and `main` with GitHub Actions, no deploy stages yet | Centralized platform; GitHub Issues + Projects for board and tracking |
| 34 | Board | GitHub Issues + Projects: Epic → Feature → Bug, no tasks per role; ASCII English text | The file in `docs/` is the source of truth; board mirrors the files |
| 35 | Branching | Trunk: `main` + short `feature/F-<n>` and `bug/B-<n>` branches | Team of two, one item in progress; no environments for release branches to serve |

### Round 7 — Quality
| # | Question | Decision | Reason |
|---|---|---|---|
| 36 | Test levels | Unit + integration (Testcontainers) + bUnit for screens with logic. End-to-end later; first candidate is a full timed Exam Simulator session | Same stack as Simulae |
| 37 | Imported tests | Tests come with the code they cover, renamed to English, in the same feature. Code without its tests is not imported | The tests prove the rename broke nothing |
| 38 | Time budget | Unit < 30 s, integration < 2 min, full suite < 5 min. One PostgreSQL container per test project, a database per test class | Simulae's per-file containers are converted as they are imported |
| 39 | Coverage | Domain rules covered (scoring, penalties, cut-off, answer key versions, plan limits); no global percentage | A wrong penalty rule breaks the product's central promise |
| 40 | Architecture tests | Yes: module isolation for both module shapes, English vocabulary, `User` exception, no MassTransit reference | Decisions hold only if the build checks them |
| 41 | Build-enforced rules | English identifiers, no hardcoded UI strings, API error codes, `Guid? = null` in contracts, warnings baseline at zero | Workflow defaults; Simulab starts empty |
| 42 | AI tests | The AI gateway is faked in unit and integration tests. Import extraction is checked against a small set of golden exams, run on demand, not in CI | Real AI calls in CI are slow, cost money and are not deterministic |

### After the quiz — Subject taxonomy (owner, 2026-09-17)
Raised by the owner with a survey of municipal guard exams in seven capitals: organizers name subjects and topics differently, and some notices group subjects (Mathematics + Logic). The brief (capability 2) was updated.
| # | Question | Decision | Reason |
|---|---|---|---|
| 43 | Canonical taxonomy depth | Two levels: `Subject` → `Topic`. Simulae's `KnowledgeDomain` becomes `Subject`; Simulae's top-level `Subject` (LINGUAGEM, DIREITO) becomes an optional `Area` attribute used only for grouping. `Question` keeps only `TopicId` | Nobody studies "Law"; they study Constitutional Law. `Area` also fits ENEM (area → subject → topic). Less code than importing three levels |
| 44 | Notice vocabulary | Each exam edition has `NoticeSubject` rows: the label, grouping, number of questions, weight and minimum as the notice states them, mapped to N canonical subjects and topics | The Exam Simulator shows and scores by the notice's subjects (faithful to the real exam); analytics and recommendations use the canonical taxonomy (comparable across exams). Grouped subjects and laws placed under different headings stop being a problem |
| 45 | Name variations | `SubjectAlias` and `TopicAlias`. AI import looks a notice name up in the aliases first; otherwise it suggests the closest topic or a new draft topic; the curator's confirmation becomes a new alias | Stops duplicate topics; mapping gets more automatic with every import |
| 46 | Deeper topics | Not now. If large subjects need it, add an optional parent on `Topic` | Additive change, nothing breaks |

Consequences: one entity, one admin screen and one cascade level fewer than Simulae; new work is `NoticeSubject` with its mapping and the aliases; the municipal guard seed is remapped on import. Local content (city history, municipal law) lives in a "Local knowledge" subject with one topic per city. Screen detail is left to the refinement of the "Subject taxonomy" epic.

## Deferred decisions
Deferred by the owner on 2026-09-17; each returns when its epic is refined.
- First release order, and where AI import fits.
- Rights to reproduce past exams per source, and what must be shown.
- First certifications.
- ENEM scoring (IRT item parameters).
- Essay and open-answer grading.
- Institutions (activates the dormant `TenantId`).
- Portugal: which exams. When: after v1; data region: Brazil South for every user, no EU region (ADR-0003, 2026-10-04).
- Dedicated OCR service: decided after the first real import.
- Concrete trial values: durations, limits, list of plans.

## Consequences
- Importing from Simulae costs a rename to English, extraction of every UI text to three languages, and conversion of test containers to a shared fixture, per feature.
- Two module shapes coexist; the architecture tests and the profile describe both.
- The `Plans` module needs new work before promo codes exist: validity on assignments, a usage meter, the entitlement service.
- AI and file storage are entirely new code.
- One container fewer to run and test (no RabbitMQ).

## Revisit when
- An institution wants private students or question banks (multi-tenancy).
- A second process needs events (broker).
- A mobile client appears (OpenIddict flows, render mode).
- The first release is near (environments, deployment pipeline).
- Simulab has its own brand (theme).
