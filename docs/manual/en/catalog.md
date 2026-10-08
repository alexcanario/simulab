---
page: catalog
locale: en
features: [F-36, F-37, F-42, F-57]
updated: 2026-10-06
---
# Catalog

The catalog is where you find the exam you are preparing for. It lists the exams that have at least one published edition, and each exam's page shows its published editions with a link to the official notice.

## Who can use it
Any signed-in account whose role has the permission "Browse the catalog". Students, Curators and Admins have it from the start; an Admin can take it away from a role on [Roles and permissions](roles.md). Without it there is no Catalog item in the menu and the address shows Page not found.

The catalog shows the same to everyone, Admins included: exams with only draft editions are not listed. Drafts stay in the administration.

## How to
### Find an exam
1. Choose **Catalog** in the **Study** section of the menu.
2. Type in the search box. Words can be part of the exam's name, of the issuing authority's name, or of the state or city, in any order, with or without accents and capitals. "guarda sp" finds "Guarda Municipal" of a São Paulo authority. A state exam is found by its state's name or acronym ("sp", "sao paulo" or "São Paulo") and shows its state as "São Paulo (SP)", in the list and on the exam page.
3. Narrow the list with **Assessment type**, **Scope**, **State**, **Board** and **Notice year**, alone or together with the text. The board and the year are matched on the same edition: an exam only appears for "FGV" and "2025" when it has a published FGV edition in 2025.
4. To see the exams of one state, open **State** (the whole list appears; type a name or an acronym to narrow it) and pick one, such as "São Paulo (SP)". Only the state exams of that state are listed, and **Scope** changes to State. Choosing another scope, or all scopes, empties the state; clearing only the state keeps the scope. National exams and municipal exams are never listed under a state.
5. Choose **Clear filters** to remove the filters, the state included. The search text has its own clear button.

The address of the page keeps your search, filters and page, so you can bookmark or share it. Going back from an exam returns to the list as you left it.

### See an exam and its editions
1. Choose the exam's name in the list.
2. The page shows its editions, newest notice year first, with the board, the position, the notice reference and the application date when they exist.
3. Choose **Official notice** to open the notice on the organizer's own site. It opens in a new tab.
4. The **About this exam** card gives the issuing authority, the assessment type, the scope, the language of the exam, and how many editions are published.

Exam content is shown in its own language and is never translated.

## What a new environment already has
Every environment starts with the municipal guard exams, so the catalog is never empty at the beginning. Four exams appear for students, each with its published edition and the link to the official notice: Guarda Municipal of Curitiba (Instituto AOCP, notice 2025), Guarda Municipal of Manaus (Instituto Consulplan, 2026), Guarda Civil Municipal of Salvador (FGV, 2026) and Guarda Civil Municipal of Maceió (Copeve/Ufal, 2026). The exams of Recife and Goiânia are already registered but have no notice yet, so they stay in the administration until an Admin adds and publishes an edition. The published editions show no application date, because it was not confirmed on the official sites.

These rows are ordinary data: an Admin can edit or delete any of them and the change stays.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Search | Part of the exam's name, its issuing authority's name, or its state or city | Every word must match somewhere; capitals and accents do not matter |
| Assessment type | Public service exam, certification, university entrance exam or ENEM | One value or all |
| Scope | National, State or Municipal | One value or all |
| State | One of the 27 states, shown as "São Paulo (SP)" | Only states with a published state exam are offered, in the order of the official list; picking one sets Scope to State |
| Board | The organizer that applied a published edition | Only boards with something published are offered |
| Notice year | The year of a published edition's notice | Only years with something published are offered |
| Editions | How many editions of the exam are published | Counts all of them, whatever the filters |
| Latest year | The most recent notice year among the published editions | — |

## Messages
| Message | What it means | What to do |
|---|---|---|
| No exam is published in the catalog yet. | Nothing has been published. | Come back later. |
| No published exam matches "…". | The search found nothing. | Try fewer or shorter words. |
| No published exam matches the chosen filters. | The filters together leave nothing. | Choose **Clear filters**. |
| We could not load the board, year and state options. The rest of the search works. | The three lists of options failed to load. | Choose **Try again**, or keep searching without them. |
| We could not load this exam. | The exam page failed to load. | Choose **Try again**. |
| This exam no longer exists. | The exam does not exist, was removed or has nothing published. | Choose **Back to the catalog**. |

## Related pages
- [Getting around](getting-around.md)
- [Exams](exams.md) and [Exam editions](exam-editions.md): how Admins prepare what the catalog shows
- [Roles and permissions](roles.md): who can browse the catalog
