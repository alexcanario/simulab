---
page: exams
locale: en
features: [F-34, F-43, F-35, F-42]
updated: 2026-10-02
---
# Exams

An exam is the assessment that comes back year after year: a city hall's public service exam, ENEM, a
certification, a university's entrance exam. Every exam belongs to the **issuing authority** that publishes its
notice. Each paper actually applied is an [edition](exam-editions.md), managed on the exam's own page.

## Who can use it
Only an account with the permission "Manage the catalog" — the Admin role has it — can open the Exams page.
Everyone else gets Page not found.

## How to
### See the exams
1. Choose **Exams**, in the **Content** section of the side menu.
2. The list shows the name, the issuing authority, the assessment type and the scope, in name order. Click a
   column title to sort by it.
3. Type in the search box to filter by name. Case and accents do not matter: "publica" finds "Pública".
4. The three filters above the list — issuing authority, assessment type and scope — can be combined. In the
   issuing authority filter, type two letters of the name and pick from the list that appears.

### Add an exam
1. Choose **Add**. The form opens on a page of its own.
2. In **Issuing authority**, type two letters of the name and pick from the list. If the body is
   not there yet, add it first on [Issuing authorities](issuing-authorities.md).
3. Type the exam's **name**, as it appears in the notice.
4. Choose the **assessment type**. Choose the **scope** from the three cards — each one explains in a line
   what it means.
5. If the scope is State or Municipal, one more field appears, for **where** the exam applies.
   - **State**: the field is a list of the 27 Brazilian states (26 states and the Federal District). Click it to
     see them all, each as "São Paulo (SP)"; type part of the name or the acronym ("sp", "paulo", "sao") to narrow
     the list, with or without accents, and pick one.
   - **Municipal**: type the municipality's name, as free text.
   - Setting the scope back to National hides the field and drops what you chose. Moving between State and
     Municipal starts the field empty.
6. Choose the **content language**. It starts at Portuguese (Brazil). This is the language the exam and its
   questions are written in; content is never translated.
7. Choose **Save**. You stay on the page, now editing the exam you just created.

The form is read in three titled blocks — identification, classification and where the exam applies — and the
column on the right shows what is already filled and what is still missing. If you save with something missing,
a notice at the top of the card lists every missing field; clicking one takes you straight to it.

### Change an exam
1. Choose **Edit** on its row.
2. Change what you need and choose **Save**. If you leave the page with unsaved changes, the app asks first.

### Delete an exam
1. Choose **Delete** on its row and confirm.
2. The exam leaves the catalog. Its name stays reserved inside that issuing authority, so nobody adds a second
   exam with the same name by mistake.
3. An exam that still has editions cannot be deleted: delete its [editions](exam-editions.md) first.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Issuing authority | Who publishes the notice and sets the exam's rules | Required. The board that applies each paper is not chosen here: it belongs to the edition |
| Name | How the exam appears everywhere | Required, 2 to 200 characters. It cannot repeat the name of another exam of the same issuing authority, a deleted one included. Case and accents do not make a name different |
| Assessment type | Public service exam, certification, university entrance exam or ENEM | Required |
| Scope | National, state or municipal | Required |
| State / Municipality | Where the exam applies | Required when the scope is State or Municipal. For a State exam, one of the 27 states of the list; it is shown everywhere as "São Paulo (SP)" and the same list is offered whatever the exam's content language. For a Municipal exam, free text of at most 120 characters. Hidden when it is National |
| Content language | The language the exam and its questions are written in | Required, one of Portuguese (Brazil), Portuguese (Portugal) and English. Starts at Portuguese (Brazil) |

## Messages
| Message | What it means | What to do |
|---|---|---|
| Choose the issuing authority. | No body was picked in the search field. | Type two letters and pick one from the list. |
| Enter a name with at least 2 characters. | The name is empty or too short. | Type the exam's name. |
| The name is too long: at most 200 characters. | The name is over the limit. | Shorten the name. |
| This issuing authority already has an exam with this name. | The name is taken inside that body, possibly by a deleted exam. | Choose another name, or another issuing authority. |
| Choose an assessment type. | The type was not chosen. | Pick one of the four types. |
| Choose a scope. | The scope was not chosen. | Pick national, state or municipal. |
| Say where this exam applies. | The scope is state or municipal and the field was left empty. | Pick the state from the list, or type the municipality. |
| Choose one of the 27 states of the list. | The state was refused because it is not one of the 27 of the list. | Open the list and pick the state. |
| This exam was saved with “…”, which is not a state of the list. Choose the right one. | An exam saved before the state list existed, with a text no state matches (for example "Sampa"), was opened. The field is empty and the hint quotes the old text. | Pick the right state and save; saving without a state is refused. |
| This field is too long: at most 120 characters. | The state or municipality is over the limit. | Shorten the text. |
| Choose the language of the content. | The language was not chosen. | Pick one of the three languages. |
| This exam no longer exists. | Someone deleted the exam while your screen was open. | Reload the page. |
| No exam matches the chosen filters. | The search and filters together found nothing. | Clear one filter and try again. |
| This exam has editions. Delete its editions first. | The exam still has editions. | Delete the editions, then the exam. |
| Page not found | Your account does not manage the catalog. | Talk to an admin if you think it should. |

## Related pages
- [Issuing authorities](issuing-authorities.md): who publishes each exam's notice (Admins)
- [Organizers](organizers.md): who writes and applies the papers (Admins)
- [Getting around](getting-around.md): menu, light and dark mode, language and keyboard
- [Roles and permissions](roles.md): who can manage the catalog
- [Simulab](index.md)
