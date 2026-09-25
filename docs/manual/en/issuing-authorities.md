---
page: issuing-authorities
locale: en
features: [F-34]
updated: 2026-09-25
---
# Issuing authorities

An issuing authority is who publishes the notice and sets the positions, the syllabus and the rules of an exam:
a city hall, a state government, a ministry, a university, a company. Every exam in the catalog belongs to one
of them.

Do not confuse it with the [organizer](organizers.md): the board is hired to write, apply and mark the paper,
and it can change from one edition to the next.

## Who can use it
Only an account with the permission "Manage the catalog" — the Admin role has it — can open the Issuing
authorities page. Everyone else gets Page not found.

## How to
### See the issuing authorities
1. Choose **Issuing authorities**, in the **Content** section of the side menu.
2. The list shows the name and the acronym, in name order. Click a column title to sort by it.
3. Type in the search box to filter by name or acronym. Case and accents do not matter.

### Add an issuing authority
1. Choose **Add**.
2. Type the **name**, as the body signs its notices, and the **acronym**. The acronym is stored in capitals.
3. The **official website** and the **description** are optional. The website is a full address, starting with
   `https://`.
4. Choose **Save**.

### Change an issuing authority
1. Choose **Edit** on its row.
2. Change what you need and choose **Save**. The exams that already belong to it keep belonging to it.

### Delete an issuing authority
1. Choose **Delete** on its row and confirm.
2. If it still has exams in the catalog, the deletion is refused and a notice appears at the top of the list.
   Delete those exams first, on the [Exams](exams.md) page — filter the list by this body to find them.
3. With no exams left, the body leaves the catalog. Its name and acronym stay taken, so nobody adds a second one
   with the same name by mistake.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Name | How the body appears everywhere | Required, 2 to 150 characters, cannot repeat another body's name, a deleted one included. Case and accents do not make a name different |
| Acronym | The short name (PMF, MEC, UFC) | Required, 2 to 20 characters, stored in capitals, unique the same way the name is |
| Official website | Where to read about the body | Optional, a full address starting with `http://` or `https://` |
| Description | Free text about the body | Optional, at most 500 characters |

## Messages
| Message | What it means | What to do |
|---|---|---|
| Another issuing authority already has this name. | The name is taken, possibly by a deleted body. | Choose another name. |
| Another issuing authority already has this acronym. | The acronym is taken, in any combination of capitals. | Choose another acronym. |
| Type a name with at least 2 characters. | The name is empty or too short. | Type the full name. |
| The name is too long: at most 150 characters. | The name is over the limit. | Shorten the name. |
| Type an acronym with at least 2 characters. | The acronym is empty or too short. | Type the acronym. |
| The acronym is too long: at most 20 characters. | The acronym is over the limit. | Shorten the acronym. |
| The description is too long: at most 500 characters. | The description is over the limit. | Shorten the description. |
| Type a full address, starting with http:// or https:// | The website is not a full address. | Add `https://` in front, or leave the field empty. |
| This issuing authority has exams in the catalog. Delete those exams first, on the Exams page. | The body still owns at least one exam. | Go to Exams, filter by this body, delete the exams and come back. |
| This issuing authority no longer exists. Refresh the list. | Someone deleted the body while your screen was open. | Reload the page. |
| Page not found | Your account does not manage the catalog. | Talk to an admin if you think it should. |

## Related pages
- [Exams](exams.md): each issuing authority's exams (Admins)
- [Organizers](organizers.md): who writes and applies the papers (Admins)
- [Getting around](getting-around.md): menu, light and dark mode, language and keyboard
- [Roles and permissions](roles.md): who can manage the catalog
- [Simulab](index.md)
