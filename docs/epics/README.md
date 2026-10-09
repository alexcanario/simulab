# Epics

Technical terms: [glossary](../glossary.md)

Created on the board at bootstrap (2026-09-17). Each epic gets its own file `docs/epics/<slug>.md` when `/agile:epic` breaks it into features. Board: GitHub Issues + Projects `alexcanario/simulab`.

| Order | Epic | Slug | Board id | Status |
|---|---|---|---|---|
| 1 | Foundation and identity | `foundation-and-identity` | 690 | agreed (F-1 to F-11) |
| 2 | Assessment catalog | `assessment-catalog` | 691 | draft (F-33 to F-37) |
| 3 | Subject taxonomy | `subject-taxonomy` | 692 (GitHub #3) | agreed (F-51, F-74 to F-79) |
| 4 | Question bank | `question-bank` | 693 | idea |
| 5 | Question Bank Simulator | `question-bank-simulator` | 694 | idea |
| 6 | Exam Simulator | `exam-simulator` | 695 | idea |
| 7 | Plans and entitlements | `plans-and-entitlements` | 696 | idea |
| 8 | Entitlements and promo codes | `entitlements-and-promo-codes` | 697 | idea |
| 9 | AI-assisted exam import | `ai-assisted-exam-import` | 698 | idea |
| 10 | Performance analytics | `performance-analytics` | 699 | idea |
| 11 | Study recommendations | `study-recommendations` | 700 | idea |
| 12 | AI coach | `ai-coach` | 701 | idea |
| — | Parental consent for minors | `parental-consent-for-minors` | 702 | idea, not ordered |
| — | Institutions | `institutions` | 703 | idea, not ordered |
| — | Portugal exams | `portugal-exams` | 704 | idea, not ordered |
| — | Cloud hosting and operations | `cloud-hosting-and-operations` | GitHub #138 | idea, not ordered (F-62, F-64 to F-68, F-81, F-82, F-87, F-88) |

The order of epics 2 to 6 and 9 is a proposal for the brief's open note "first release order"; the owner may change it.

## Execution order

Agreed with the owner on 2026-10-09. Each item is scored 0 to 10 for its importance to the project: how directly it leads to the product core (catalog, taxonomy, question bank, simulators), or unblocks testers and real users with legal safety. Items with a worktree but a low score (F-68, F-77, F-86, F-89) stay paused until step 8, to keep work in progress low.

| Step | Item | Score | Why now |
|---|---|---|---|
Steps 1 to 3 release the staging to testers: real people's data must not go into it before them.

| Step | Item | Score | Releases testers | Why now |
|---|---|---|---|---|
| 1 | F-93 Users' data in Azure Central US meets LGPD and GDPR | 9 | yes | Central US is real (owner, 2026-10-09); ADR-0003 says Brazil South; F-71 depends on it |
| 2 | F-71 Privacy policy names where data lives and who processes it | 8 | yes | The privacy policy must name the region and the processors before real users |
| 3 | F-85 Existing users accept a new legal document version | 8 | yes | F-71 changes the policy version; users already signed up must see it |
| 4 | F-94 Every setting a host requires has a source in the publish | 6 | no | The next missing setting fails the build, not the staging (F-64 retro) |
| 5 | F-76 Syllabus for students | 7 | no | First student-visible value of the taxonomy; F-75 is done |
| 6 | F-78 Notice subjects of the municipal guard editions | 6 | no | Real data for testers to look at |
| 7 | E-4 Question bank (`/agile:epic`) | 10 | no | Next block of the product core |
| 8 | F-65 A GitHub Actions workflow deploys a release | 6 | no | Automates the manual deploy |
| 9 | F-73 Shared OpenIddict keys across Api replicas | 5 | no | One replica works; a park already signs testers out (`docs/infra.md`) |
| 10 | F-40 Eval suite for model calls | 5 | no | Must exist before any E-9 item (rises to 8 then) |
| 11 | E-5 Question Bank Simulator, then E-6 Exam Simulator | 9 | no | The product the student uses |

Remaining items, by score:

| Score | Items |
|---|---|
| 7 | E-9, E-10, E-11 |
| 6 | E-7, E-12 |
| 5 | F-87, E-13 |
| 4 | F-77, F-89, F-86, F-88, F-82 |
| 3 | F-68, F-90, F-92, F-84, F-81, E-8, E-15 |
| 2 | F-83, F-80, F-91, F-72, E-14 |
