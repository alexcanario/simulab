---
page: exam-editions
locale: en
features: [F-35, F-74]
updated: 2026-10-04
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
2. Choose **Delete** on the row and confirm. The edition leaves the list, with its notice subjects (the confirmation says so). Its year, position and board stay taken inside the exam, so the same paper cannot be added again by mistake.

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

## Notice subjects
In the **Notice subjects** card, below the edition form, you record the subjects exactly as the notice states them: the group (for example "Basic knowledge"), the subject name and the number of questions. The card shows only once the edition is saved; on a new edition it asks you to save first. Each subject is saved by its own dialog or action: the edition's **Save** button never saves them. It works on Draft and Published editions.

### Add a notice subject
1. In the card, choose **Add notice subject**.
2. If the notice groups its subjects, pick a group already used (the list suggests as you type, ignoring case and accents) or type a new one. Leave it empty when the notice does not group. When you add several in a row, the group of the last one added comes pre-filled.
3. Type the **subject** as the notice names it and, if the notice states it, the **number of questions** (1 to 500; empty means the notice does not say).
4. Choose **Save**. The new subject goes last in its group, or last in the list when the group is new.

### View, order, change and delete
- Subjects appear under their group heading, in the notice's order. When none has a group there is no heading. A dash (—) means the number of questions is not stated.
- Below the list, the total adds up only the stated numbers and says how many subjects are left out of the sum for having no number. The total is neither stored nor checked against anything.
- **Move up** and **Move down** swap the subject with its neighbour in the same group. The first of a group cannot go up and the last cannot go down (the button is disabled and says why); a subject never changes group by being moved.
- **Edit** opens the same dialog filled in. Changing the group moves the subject to the end of the new group; changing only the name or the number keeps its position.
- **Delete** asks for confirmation and removes the subject from the edition. It can be added again later.

### Notice subject fields
| Field | Meaning | Rules |
|---|---|---|
| Group | The heading under which the notice lists the subject | Optional, up to 100 characters. Empty is "No group" |
| Subject | The subject as the notice names it | Required, 2 to 200 characters |
| Number of questions | How many questions the notice gives the subject | Optional, whole number from 1 to 500 |

In one edition, a subject name cannot repeat inside the same group (capitals and accents do not count; "no group" is one group). A deleted subject does not count.

### Notice subject messages
| Message | What it means | What to do |
|---|---|---|
| Type the subject as the notice names it. | The name is empty. | Type the name. |
| The subject needs at least 2 characters. | The name has 1 character. | Type the full name. |
| The subject is too long: at most 200 characters. | The name is over 200 characters. | Shorten the name. |
| The group is too long: at most 100 characters. | The group is over 100 characters. | Shorten the group. |
| Type a whole number from 1 to 500, or leave it empty. | The number is not whole or is out of range. | Fix it or leave it empty. |
| This group already has a subject with this name (capitals and accents do not count). | The same subject is already in the group. | Change the name or the group. |
| This subject is no longer in the edition. The list was reloaded. | Someone deleted it while your screen was open. | Check the refreshed list. |
| This subject cannot move that way. The list was reloaded. | Someone else already moved it. | Check the current order. |

## Related pages
- [Exams](exams.md): the exams in the catalog (Admins)
- [Organizers](organizers.md): the boards an edition names (Admins)
- [Getting around](getting-around.md): menu, light and dark mode, language and keyboard
- [Simulab](index.md)
