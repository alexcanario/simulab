---
page: exam-editions
locale: en
features: [F-35]
updated: 2026-09-29
---
# Exam editions

An edition is one paper actually applied: the year of its notice, the job it selects for, the board that applied it and whether students can see it. An exam has as many editions as it had papers. When one notice opens several jobs with different papers, each paper is its own edition.

Editions are managed on the exam's own page, in the **Editions** card below the exam form.

## Who can use it
Only an account with the permission "Manage the catalog" — the Admin role has it. Everyone else gets Page not found.

## How to
### See the editions of an exam
1. Choose **Exams** in the **Content** section, then **Edit** on the exam.
2. The **Editions** card lists them, newest notice year first. Each row shows the year, the position when there is one, the board's acronym, a chip (Draft or Published) and the application date when it is known.

### Add an edition
1. Save the exam first: a new exam cannot have editions yet.
2. In the **Editions** card, choose **Add edition**. It opens on a page of its own.
3. In **Board**, type two letters of the name or the acronym and pick from the list. If the board is not there yet, add it first on [Organizers](organizers.md).
4. Type the **notice year**.
5. Optionally fill the **position** (the job the paper selects for; leave it empty for ENEM, entrance exams and certifications), the **notice reference** ("Edital nº 01/2026"), the **notice link** and the **application date**. Type the date in your format or pick it from the calendar.
6. Choose **Draft** or **Published** and **Save**. A new edition is a draft unless you choose Published. You stay on the page, now editing the edition you just saved.

### Change, publish or unpublish an edition
1. Choose **Edit** on its row.
2. Change what you need. To publish or unpublish, choose the other card in **Publication**. Choose **Save**.

### Delete an edition
1. Only a draft can be deleted. If the edition is published, open it, choose **Draft** and save first: the delete action of a published row is disabled.
2. Choose **Delete** on the row and confirm. The edition leaves the list. Its year, position and board stay taken inside the exam, so the same paper cannot be added again by mistake.

An exam that still has editions cannot be deleted, and neither can a board that some edition names. Delete or change the editions first.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Board | Who applied the paper, as the notice names it | Required. Pick from the list |
| Notice year | The year of the notice | Required, from 1990 to next year |
| Position | The job the paper selects for | Optional, at most 200 characters |
| Notice reference | How the notice names itself | Optional, at most 100 characters. Editions cut from one notice share it |
| Notice link | The official address of the notice | Optional, a full address starting with `http://` or `https://`, at most 300 characters |
| Application date | The day the paper was applied | Optional, also to publish. Not before 1 January of the notice year |
| Status | Draft is kept in the administration; Published is offered to students | Required. Starts at Draft |

An exam cannot have two editions with the same year, position and board; capitals and accents do not make a position different, and a deleted edition still counts.

## Messages
| Message | What it means | What to do |
|---|---|---|
| Choose the board. | No board was picked. | Type two letters and pick one from the list. |
| Type a year from 1990 to next year. | The notice year is missing or out of range. | Type a valid year. |
| Type a full address of at most 300 characters, starting with http:// or https:// | The notice link is not a complete web address. | Add `https://`, or leave the field empty. |
| The application date cannot be before 1 January of the notice year. | The date is earlier than the notice year. | Fix the date or the year. |
| This exam already has an edition with this year, position and board (a deleted one counts too). | The same paper exists. | Change the year, position or board. |
| This edition is published. Set it back to Draft, save, then delete it. | Only drafts can be deleted. | Unpublish it first. |
| This exam has editions. Delete its editions first. | An exam with editions cannot be deleted. | Delete the editions, then the exam. |
| Some editions name this board. Change or delete those editions first. | A board in use cannot be deleted. | Change or delete those editions. |
| This edition no longer exists. | Someone deleted it while your screen was open. | Go back to the exam. |

## Related pages
- [Exams](exams.md): the exams in the catalog (Admins)
- [Organizers](organizers.md): the boards an edition names (Admins)
- [Getting around](getting-around.md): menu, light and dark mode, language and keyboard
- [Simulab](index.md)
