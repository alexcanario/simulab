---
page: organizers
locale: en
features: [F-33]
updated: 2026-09-23
---
# Organizers

An organizer is who runs an exam: an exam board hired for a public service exam, a body that issues certifications, or a university with its own entrance exam. Every exam in the catalog hangs on one of them.

## Who can use it
Only an account with the permission "Manage the catalog" — the Admin role has it — can open the Organizers page. Everyone else gets Page not found.

## How to
### See the organizers
1. Choose **Organizers**, in the **Content** section of the side menu.
2. The list shows the name, the acronym and the kind, sorted by name. Click a column header to sort by it.
3. Type in the search box to filter by name or acronym. Accents and capitals do not matter: "fundacao" finds "Fundação".

### Add an organizer
1. Choose **Add**.
2. Type the **name**, as the organizer signs its notices, and its **acronym**. The acronym is saved in capitals.
3. Choose the **kind**: exam board, certifying body or university.
4. The **official website** and the **description** are optional. The website is the full address, starting with `https://`.
5. Choose **Save**.

### Change an organizer
1. Choose **Edit** on its line.
2. Change what you need and choose **Save**. Everything that already points at the organizer keeps pointing at it.

### Delete an organizer
1. Choose **Delete** on its line and confirm.
2. The organizer leaves the catalog and stops appearing anywhere in the app. Its name and acronym stay taken, so nobody registers a second one under the same name by mistake.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Name | How the organizer is shown everywhere | Required, 2 to 150 characters, cannot repeat another organizer's name, including deleted ones. Capitals and accents do not make a different name |
| Acronym | The short name (CEBRASPE, FGV) | Required, 2 to 20 characters, saved in capitals, unique in the same way as the name |
| Kind | Exam board, certifying body or university | Required |
| Official website | Where to read about the organizer | Optional, at most 300 characters, a full address starting with `http://` or `https://` |
| Description | Free text about the organizer | Optional, at most 500 characters |

## Messages
| Message | What it means | What to do |
|---|---|---|
| Another organizer already has this name. | The name is taken, possibly by a deleted organizer. | Pick a different name. |
| Another organizer already has this acronym. | The acronym is taken, in any capitalization. | Pick a different acronym. |
| Type a name with at least 2 characters. | The name is missing or too short. | Type the full name. |
| Type an acronym with at least 2 characters. | The acronym is missing or too short. | Type the acronym. |
| Type a full address, starting with http:// or https:// | The website is not a complete address. | Add `https://` in front, or leave the field empty. |
| This organizer no longer exists. Refresh the list. | Someone deleted it while your page was open. | Reload the page. |
| Page not found | Your account does not manage the catalog. | Ask an Admin if you believe you should. |

## Related pages
- [Getting around](getting-around.md): menu, light and dark mode, language and keyboard
- [Roles and permissions](roles.md): who may manage the catalog
- [Simulab](index.md)
