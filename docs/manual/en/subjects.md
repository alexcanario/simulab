---
page: subjects
locale: en
features: [F-79, F-51, F-75, F-95, F-77]
updated: 2026-10-15
frozen: 2026-10-17 (v1.1.0)
---
# Subjects and topics

> Frozen on 2026-10-17 (v1.1.0). This page may be out of date; see the [pt-BR version](../pt-BR/subjects.md).

Subjects and topics are the one shared vocabulary of what a student studies: a subject is "Portuguese" or
"Constitutional Law", and a topic is something inside it, such as "Crase". Everything else that talks about
content points at this list.

## Who can use it
Only an account with the permission "Manage the catalog" — the Admin role has it — can open the Subjects
pages. Everyone else gets Page not found, and the menu has no Subjects entry.

## The list you start with
A new Simulab already holds 23 subjects and 70 topics for municipal guard exams, from language and
mathematics to criminal law and police procedures, each with its area. Names are content, so they stay in
Portuguese in every language. The subject **Conhecimentos locais** has
one topic per city of the catalog (Curitiba, Manaus, Salvador, Recife, Goiânia and Maceió). Treat them like any
other subject: rename, move or delete them and the change stays. If a subject with the same name already existed,
it is kept as it was and only the missing topics are added under it.

## How to
### See the subjects
1. Choose **Subjects**, in the **Content** section of the side menu.
2. The list shows the name, the area and how many topics each subject has, in name order.
3. Type in the search box to filter by name or by any of the subject's other names. Case and accents do not matter.
   A subject appears once, under its own name, even when several of its other names match.
4. Use **Area** to show only the subjects of one area, or only those with no area.

### Add a subject
1. Choose **Add**.
2. Type the **name**, as students will see it.
3. Pick an **area**, or leave it empty. The area only groups similar subjects.
4. Choose **Save**.

### Change a subject
1. Choose **Edit** on its row, or on the subject's own page.
2. Change the name or the area and choose **Save**.

### Give a subject or a topic other names
Organizers call the same subject by different names ("Português" for "Língua Portuguesa"). Record those names so a
notice's wording can be matched to the subject without creating a duplicate.
1. Open the **Add** or **Edit** window of a subject, or of a topic on the subject's page.
2. In **Other names**, type a name and press **Enter**. It appears as a chip under the field. The name is also added
   when you leave the field, so a name you typed is not lost when you choose **Save**.
3. To remove one, choose the **X** of its chip. There is no limit on how many other names an item has.
4. Choose **Save**. The set you see is the set that is saved: removing a chip removes the name.
5. Another name is only text. It follows its subject or topic: it goes with a topic that moves to another subject
   and it is deleted with the item. A name you removed is free again, for this item or for another.

### Delete a subject
1. A subject that still has topics cannot be deleted: its Delete button is disabled, and pointing at it says why.
   Delete the topics first, on the subject's page.
2. A subject that notice subjects map whole (see [Exam editions](exam-editions.md)) cannot be deleted either: its
   Delete button is disabled and says so. Remove it from those mappings first. When a subject has topics, that
   reason is the one shown.
3. With no topics and no mapping, choose **Delete** on its row and confirm. The subject leaves the list. Its name stays taken,
   so nobody adds a second one with the same name by mistake.

### Work with the topics of a subject
1. On the list, choose the subject's name. Its page shows the area and its topics, in alphabetical order.
2. Choose **Add topic**, type the name and choose **Save**.
3. Choose **Edit** on a topic to rename it. To move it to another subject, search and pick that subject in
   the same window.
4. Choose **Delete** on a topic and confirm. It leaves the page, and the subject's topic count drops by one.
   A topic that notice subjects map, or that a question uses (draft or published), cannot be deleted: its
   Delete button is disabled and says so. A mapping kept only by a deleted notice subject or a deleted edition
   does not count, and neither does a deleted question. Delete or move the questions first (see
   [Questions](questions.md)).
5. Moving a mapped topic to another subject is allowed: the notice subjects that map it follow it.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Subject name | The subject as students see it | Required, 2 to 150 characters, cannot repeat another subject's name, a deleted one included. Case and accents do not make a name different |
| Area | A group of similar subjects, such as Law or Languages | Optional. The areas are a fixed list: Languages, Mathematics, Logical Reasoning, Natural Sciences, Human Sciences, Law, Information Technology, Administration and Management, Specific Knowledge |
| Topic name | The topic inside its subject | Required, 2 to 200 characters, cannot repeat another topic's name in the same subject, a deleted one included. The same name may exist in two different subjects |
| Subject (on a topic) | Where the topic sits | Only on edit, to move the topic. The subject must exist |
| Other names (subject) | Names organizers use for the subject | Optional, any number, 2 to 150 characters each. A name typed twice (case and accents aside) counts once. It cannot equal the subject's own name, another subject's name or another subject's other name |
| Other names (topic) | Names organizers use for the topic | Optional, any number, 2 to 200 characters each. It cannot equal the topic's own name, nor the name or other name of another topic of the same subject. The same other name may exist under two subjects |

Names are shown exactly as typed, in every language of the app.

## Messages
| Message | What it means | What to do |
|---|---|---|
| Another subject already has this name, even if it was deleted. Pick a different name. | The name is taken, possibly by a deleted subject. | Choose another name. |
| This subject already has a topic with this name, even if it was deleted. Pick a different name. | The topic name is taken inside this subject. | Choose another name, or put the topic in another subject. |
| Type a name with at least 2 characters. | The name is empty or too short. | Type the full name. |
| The name is too long. Use at most 150 characters. / Use at most 200 characters. | The name is over the limit. | Shorten the name. |
| Each other name needs at least 2 characters. | An other name is empty or too short. | Type the full name or remove it. |
| An other name is too long. Use at most 150 characters. / Use at most 200 characters. | An other name is over the limit. | Shorten it. |
| An other name is the same as the subject's name. Remove it. / ... the topic's name. | The other name repeats the item's own name. | Remove it: the name already finds the item. |
| Another subject already uses one of these names, as its name or as an other name. Remove it or pick a different one. | An other name is the name, or an other name, of another subject. Also shown on a subject's own name when it equals another subject's other name. | Remove it or choose another. |
| Another topic of this subject already uses one of these names, as its name or as an other name. Remove it or pick a different one. | An other name is the name, or an other name, of another topic of the same subject. Moving a topic can cause it too. | Remove it, choose another, or move the topic elsewhere. |
| This area is not on the list. Pick one of the areas offered. | The area is not one of the fixed list. | Pick an area from the list, or none. |
| This subject has topics. Delete them first, on the subject's page. | The subject still has at least one topic. | Open the subject, delete its topics and come back. |
| Notice subjects map this whole subject. Remove it from their mappings before deleting it. | Shown on the disabled Delete: notice subjects map the subject whole. | Remove it from those notice subjects, then delete. |
| Notice subjects map this topic or questions use it. Remove those links before deleting it. | Shown on the disabled Delete: notice subjects map the topic or a question uses it. | Remove it from those notice subjects and delete or change the questions, then delete. |
| Notice subjects map this subject, so it cannot be deleted. The list was reloaded. / Notice subjects map this topic or questions use it, so it cannot be deleted. The list was reloaded. | Someone mapped it or wrote a question on it while your screen was open. | Remove the mapping or the questions first. |
| This subject no longer exists. Refresh the list. | Someone deleted the subject while your screen was open. | Reload the page. |
| This topic no longer exists. Refresh the list. | Someone deleted the topic while your screen was open. | Reload the page. |
| Page not found | Your account does not manage the catalog. | Talk to an admin if you think it should. |

## Related pages
- [Issuing authorities](issuing-authorities.md): who publishes each exam's notice (Admins)
- [Exams](exams.md): the exams of the catalog (Admins)
- [Getting around](getting-around.md): menu, light and dark mode, language and keyboard
- [Roles and permissions](roles.md): who can manage the catalog
- [Simulab](index.md)
