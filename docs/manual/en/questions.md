---
page: questions
locale: en
features: [F-95, F-96, F-97, F-98]
updated: 2026-10-13
---
# Questions

The question bank is where curators write the questions that exams and practice are built from. A question
has a statement, an optional base text, from two to five options with one correct, an optional explanation, a
difficulty, a topic and the language it is written in. A question is either single choice or true/false
(right or wrong); a true/false question has one answer key, true or false, instead of options.

## Who can use it
An account with the permission "Manage the question bank". Curators and Admins have it. Everyone else gets
Page not found, and the menu has no Questions entry.

## How to
### See the questions
1. Choose **Questions**, in the **Content** section of the side menu.
2. The list shows the start of each statement, the topic with its subject, the source (the organizer, exam
   and year of the paper the question came from, with its number; a dash means the question is authored), the
   difficulty, the language, the status and the type (single choice or true/false), newest first.
3. Type in the search box to find a word of the statement. Case and accents do not matter.
4. Use **Status** to show only drafts or only published questions.

### Write a question
1. Choose **Add**. The first field, **Question type**, offers **Single choice** (the default) and
   **True/false**. The type is chosen now and cannot be changed after the question is created; on a saved
   question it is shown as a value.
2. Pick the **subject**, then the **topic** (the topic list shows only the topics of that subject). The
   **content language** starts as your own language; change it to the language the question is written in.
   Add a **difficulty** from 1 (easiest) to 5 (hardest) if you want.
3. Write the **statement**. The toolbar offers bold, italic, underline, strikethrough, superscript, subscript,
   lists, a quote, a link and clear formatting. Images are not supported yet.
4. Write a **base text** if the question refers to one. It is shown before the statement.
5. Under **Options**, choose **Add option** for each one (up to five), type its text and mark the correct one.
   Use the arrows to move an option; the letters A to E follow the order.
6. Optionally write the **explanation**, shown with the answer key.
7. Choose **Save**. The question is saved as a **draft** and the page becomes its edit page.

For a **true/false** question the **Options** card is replaced by **Answer key**: choose **True** or
**False** for the statement. The key is needed only to publish, so a draft may be saved without it. The
penalty for a wrong answer is not part of the question: it comes from the scoring rules of the edition. If you
change the type before saving, what you typed for the other type stays on the page but is not saved.

A draft may be saved with no options and no correct one yet, or with no key. The list "Before publishing",
beside the form, shows what is still missing; for a true/false question it has one item, "Answer key set".

### Record where a question came from
A question from a past exam keeps the edition it came from, its number in that paper and, if you like, its
notice subject. A question with no edition is an authored question.
1. In the **Origin** card, under **Edition**, type the exam, the organizer or the year and pick from the list.
   Draft and published editions are listed; each shows the organizer's acronym, the exam and the year.
2. Enter the **number in the paper**, from 1 to 500. It is required when there is an edition, and each number
   is used once per edition: the number of a deleted (draft) question is free again.
3. If you like, choose the **notice subject**, among those of the chosen edition.
4. Choose **Save**. Changing the edition, or emptying it, clears the number and the notice subject.
5. To make the question authored again, empty **Edition** and save. The number becomes free.

An origin never stops you from publishing: a published question may come from an edition that is still a draft.
The side of the form shows the **Source** (the "organizer · exam · year" line, or "Authored"), and the preview
as a student shows it above the statement, with the number.

The edition lists its questions too: on the edition page (**Exams**, the exam, **Edit** the edition) the
**Questions** card shows, in paper order, the questions that came from it, with **Add question**. The card is
shown to people who also manage the question bank.

### Publish and unpublish
1. Open the question and look at the **Publication** card, below the form.
2. **Publish** is available when the question is saved and the checklist is complete: topic, language,
   statement, two to five options, every option with text and exactly one correct (for a true/false question:
   the answer key set). Choosing **Save** never publishes.
3. A published question can still be edited, but every edit must keep it complete.
4. **Unpublish** takes it back to a draft with the same content.

### Versions and answer key changes
Every state a published question was shown in is kept, so an exam attempt can always be tied to the exact text
and key that graded it. The **History** card, below the **Publication** card, lists the versions of a saved
question, newest first, with the date, what changed and the reason.
1. The first time a question is published it becomes **version 1**, "First publication". A draft that was
   never published has no version.
2. Saving a published question records a new version only when what students see or are graded on changed:
   the statement, the base text, the options and the key, the explanation or the language. Changing only the
   topic, the difficulty or the origin does not. Saving with no change records nothing.
3. If you unpublish, edit and publish again, a new version is recorded at the publish when the content differs
   from the latest version.
4. To change the answer key of a published question (after an appeal, for example), mark another option as
   correct or flip True/False. A field **Reason for the answer key change** appears under the options (or the
   key); it is required, up to 500 characters. **Save** records the version as "Answer key changed" with your
   reason and the date. Moving the correct option by reordering counts as a key change.
5. If the key changed while the question was a draft, **Publish** is refused the first time with "Write the
   reason for the answer key change." The reason field appears in the **Publication** card; type it and choose
   **Publish** again.
6. In the **History** card, choose **View** on a row to open that version as it was recorded, read only. The
   correct answer stays hidden until **Show answer**. The current version is marked **Current**.
7. A draft that has versions can be deleted; its versions stay in the history, which is not shown once the
   question is gone. A published question still cannot be deleted.

Versions are never edited or removed. The name of the person who made a change is stored, but not shown yet.

### Preview as a student
1. In the **Publication** card, choose **Preview as a student**.
2. The window shows the base text, the statement and the options as a student will see them; a true/false
   question shows two lines, True and False.
3. Choose **Show answer** to see the correct option (or the key) and the explanation. The preview always shows the saved
   version; save first to see your latest changes.

### Delete a question
1. Only a draft can be deleted. A published question has its Delete button disabled; unpublish it first.
2. Choose **Delete** on its row and confirm. The topic stays.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Question type | How the question is answered | Single choice or true/false. Chosen when the question is created; it cannot be changed afterwards |
| Subject | Used to pick the topic | Required on screen; the question stores only its topic |
| Topic | The topic the question belongs to | Required. A topic cannot be deleted while a question uses it |
| Content language | The language the question is written in | Required, one of the app's languages. Content is never translated |
| Difficulty | How hard the question is | Optional, a whole number from 1 to 5 |
| Statement | What the student is asked | Required, up to 10,000 characters |
| Base text | A text the question refers to | Optional, up to 20,000 characters |
| Options | The choices, letters A to E | Up to 5. To publish: 2 to 5, each with text, exactly one correct. Each up to 2,000 characters |
| Answer key | Whether the statement of a true/false question is true or false | Only for true/false. May be empty in a draft; required to publish. A published question cannot lose it |
| Explanation | Why the key is right | Optional, up to 10,000 characters |
| Reason for the answer key change | Why the key of a published question changed | Required when a save or a publish records a version whose key differs from the previous one; 1 to 500 characters. A reason on any other new version is kept as a note |
| Edition | The past paper the question came from | Optional. With no edition the question is authored. An edition cannot be deleted while a question uses it |
| Number in the paper | The question's position in the edition's paper | Required with an edition, a whole number from 1 to 500, unique among the edition's questions |
| Notice subject | The subject of the edition's notice the question belongs to | Optional, only with an edition, and of that same edition. It cannot be deleted while a question uses it |

Only plain formatting is kept: scripts, styles and any link that is not `http` or `https` are removed when
the question is saved, and the page shows what was kept.

## Messages
| Message | What it means | What to do |
|---|---|---|
| Write the statement. | The statement is empty. | Write what the student is asked. |
| The statement has more than 10,000 characters. (also the base text, explanation and option limits) | The text is over its limit. | Shorten it. Nothing is cut for you. |
| A question is published with 2 to 5 options. | Fewer than two options, or more than five. | Add or remove options. |
| Every option needs text. | A published question has an empty option. | Write the text or remove the option. |
| Mark exactly one option as correct. | No correct option, or more than one. | Mark one. |
| Set the answer key (true or false) to publish. | A true/false question has no key. | Choose True or False under Answer key. |
| The type of a question cannot be changed. | A save named another type than the stored one. | Reload the page; create a new question for the other type. |
| A true/false question has no options. / A single-choice question has no true/false key. | A save carried an answer the type does not have. | Reload the page and save again. |
| Difficulty is a number from 1 to 5, or empty. | The difficulty is out of range. | Choose 1 to 5 or leave it empty. |
| Choose one of the supported languages. | The language is not on the list. | Pick a language from the list. |
| The topic no longer exists. Pick another. | The topic was deleted while you wrote. | Choose another topic. |
| The edition no longer exists. Pick another. | The edition was deleted while you wrote. | Choose another edition. |
| The notice subject does not belong to this edition, or no longer exists. | The subject was deleted or belongs to another edition. | Choose another subject or leave it empty. |
| Enter the number of the question in the paper. | There is an edition and no number. | Type the number. |
| The number is a whole number from 1 to 500. | The number is out of range. | Type a number from 1 to 500. |
| Number already used in this edition. | Another question of the edition has this number. | Choose another number, or fix the other question. |
| Choose an edition to give a number or a notice subject. | There is a number or a subject and no edition. | Choose the edition or empty the fields. |
| Write the reason for the answer key change. | The key changed and no reason was given. | Type the reason, up to 500 characters, and save or publish again. |
| The reason is too long (500 characters at most). | The reason has more than 500 characters. | Shorten it. |
| This version does not exist. | The version was asked for by a number the question does not have. | Reload the page. |
| Another save changed this question's history first. Reload the page and try again. | Two saves tried to record the same version number. | Reload the page and save again. |
| Only a draft can be deleted. Unpublish the question first. | The question is published. | Unpublish it, then delete. |
| This question is already published. / This question is not published. | Someone else changed it first. | Reload the page. |
| This question no longer exists. | Someone deleted it while your screen was open. | Go back to the list. |
| Only http and https addresses. | The link you typed is not a web address. | Type an address that starts with https://. |
| Page not found | Your account cannot manage the question bank. | Talk to an admin if you think it should. |

## Using the editor with the keyboard
- The toolbar is one tab stop: use Left and Right to move between its buttons, Home and End to jump.
- Tab from the toolbar enters the text; Tab again leaves the editor, it never traps the focus.
- Ctrl+B, Ctrl+I and Ctrl+U toggle bold, italic and underline.
- A field with an error says so when you leave it, and a failed save moves the focus to the list of errors.

## Related pages
- [Subjects and topics](subjects.md): the subjects and topics a question points at
- [Exam editions](exam-editions.md): the papers questions come from
- [Roles and permissions](roles.md): who can manage the question bank
- [Getting around](getting-around.md): menu, light and dark mode, language and keyboard
- [Simulab](index.md)
