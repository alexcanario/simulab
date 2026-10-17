---
page: questions
locale: en
features: [F-95, F-96, F-97, F-98, F-99, F-100, F-101, F-102, F-104, F-105]
updated: 2026-10-17
frozen: 2026-10-17 (v1.1.0)
---
# Questions

> Frozen on 2026-10-17 (v1.1.0). This page may be out of date; see the [pt-BR version](../pt-BR/questions.md).

The question bank is where curators write the questions that exams and practice are built from. A question
has a statement, an optional base text, from two to five options with one correct, an optional explanation, a
difficulty, a topic and the language it is written in. A question is single choice, true/false (right or wrong)
or multiple answer; a true/false question has one answer key, true or false, instead of options, and a
multiple-answer question has several correct options.

## Who can use it
An account with the permission "Manage the question bank". Curators and Admins have it. Everyone else gets
Page not found, and the menu has no Questions entry. Sending a question for review and withdrawing it from
review also belong to whoever manages the bank. Approving or rejecting belongs to whoever has the permission
"Review questions" (Curators and Admins have it): see [Review queue](review-queue.md).

## How to
### See the questions
1. Choose **Questions**, in the **Content** section of the side menu.
2. The list shows the start of each statement, the topic with its subject, the source (the organizer, exam
   and year of the paper the question came from, with its number; a dash means the question is authored), the
   difficulty, the language, the status and the type (single choice or true/false), newest first.
3. Type in the search box to find a word of the statement. Case and accents do not matter.
4. Narrow the list with the filters above the table. Each one lets you pick several values; a question is
   kept when it matches at least one value of every filter you used:
   - **Status**: drafts, questions in review, published questions, annulled questions, or any mix.
   - **Type**: single choice, true/false, or both.
   - **Difficulty**: 1 to 5, and **Not set** for questions with no difficulty.
   - **Subject and topic**: pick a whole subject (it means all its topics, even topics added later) or
     single topics.
   - **Organizer**: the organizers whose past exams the questions came from. Authored questions have no
     organizer and are left out while this filter is used.
   - **Edition**: one past exam edition; start typing its exam, organizer or year.
5. Every value you pick shows as a chip under its filter; remove it with its **x**, or use **Clear** on that
   filter. **Clear all** removes every filter and the search at once.
6. You can pick up to 100 subjects, topics and organizers in total.
7. The filters are kept in the address of the page: open a question, save or cancel, and you come back to the
   same filtered list, and you can send the link to someone else.

### Write a question
1. Choose **Add**. The first field, **Question type**, offers **Single choice** (the default),
   **True/false** and **Multiple answer**. The type is chosen now and cannot be changed after the question is created; on a saved
   question it is shown as a value.
2. Pick the **subject**, then the **topic** (the topic list shows only the topics of that subject). The
   **content language** starts as your own language; change it to the language the question is written in.
   Add a **difficulty** from 1 (easiest) to 5 (hardest) if you want.
3. Write the **statement**. The toolbar offers bold, italic, underline, strikethrough, superscript, subscript,
   lists, a quote, a link and clear formatting. Images are not supported yet.
4. If the question refers to a passage, pick its **base text** in the **Base text** card (see
   [Base texts](base-texts.md)); only texts in the question's language are offered. It is shown before the
   statement, and the same text can serve several questions.
5. Under **Options**, choose **Add option** for each one (up to five), type its text and mark the correct one.
   Use the arrows to move an option; the letters A to E follow the order.
6. Optionally write the **explanation**, shown with the answer key.
7. Choose **Save**. The question is saved as a **draft** and the page becomes its edit page.

For a **true/false** question the **Options** card is replaced by **Answer key**: choose **True** or
**False** for the statement. The key is needed only to publish, so a draft may be saved without it. The
penalty for a wrong answer is not part of the question: it comes from the scoring rules of the edition. If you
change the type before saving, what you typed for the other type stays on the page but is not saved.

For a **multiple-answer** question the **Options** card has a checkbox on each option: mark every correct one.
To publish it needs three to five options with text, at least two correct and at least one wrong; a draft may
have any marking. The student is told how many to mark ("Mark 2 options."), counted from the options you
marked. How a partly right answer scores is not part of the question: it comes from the scoring rules of the
edition. Going back to single choice keeps only the first option you marked.

A draft may be saved with no options and no correct one yet, or with no key. The list "Before publishing",
beside the form, shows what is still missing; for a true/false question it has one item, "Answer key set",
and for a multiple-answer question it asks for "At least two correct and one wrong".

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

### Transcribe a whole paper
Use this to enter every question of a past paper in one sitting, without reopening the form for each one.
1. Open a saved exam edition and, in its **Questions** card, choose **Transcribe**.
2. The form opens with the edition fixed and the next number in the paper already filled in (1 when the edition has no questions). You can change the number.
3. Fill in the question and choose **Save and continue**. The question is saved as a draft, the counter "Question N of this session" goes up and the form clears for the next one.
4. The edition, the notice subject, the language and the base text stay as they were; everything else is blank again. A reading block with several questions needs the text picked only once; change or remove it when the block ends. Change the notice subject whenever the paper moves to another one.
5. Choose **Finish** when you are done. Every question already saved stays in the bank as a draft.

All three question types can be transcribed. The counter lives only on the screen: if you leave and come back, it starts again at 1 and the suggested number follows what is already saved.

### Publish and unpublish
1. Open the question and look at the **Publication** card, below the form.
2. **Publish** is available when the question is saved and the checklist is complete: topic, language,
   statement, two to five options, every option with text and exactly one correct (for a true/false question:
   the answer key set; for a multiple-answer question: three to five options, at least two correct and at
   least one wrong). Choosing **Save** never publishes.
3. A published question can still be edited, but every edit must keep it complete.
4. **Unpublish** takes it back to a draft with the same content.

### Send for review
Publishing directly stays as it is. Sending for review is a second way: the question is published only after
someone with the permission "Review questions" approves it.
1. Open a saved draft and, in the **Publication** card, choose **Send for review**. The button is disabled for
   the same reasons as **Publish**: unsaved changes and an incomplete "Before publishing" list; the reason
   line below applies to both.
2. The question becomes **In review**: a yellow notice says it cannot be edited, every field becomes
   read-only and the card shows since when it has been waiting for a reviewer.
3. While it is in review, the question cannot be saved, published, unpublished or deleted, and the list offers
   neither Publish nor Unpublish for it. **Preview as a student** and the history keep working. A topic used
   only by a question in review cannot be deleted either.
4. To change something, choose **Withdraw from review**. It asks for no reason: the question goes back to a
   draft with the same content and can be edited.
5. If the reviewer **rejects** it, the question goes back to a draft and a blue notice in the **Publication**
   card shows the date and the reason for the rejection. Fix it and send it again; the notice goes away with
   the next send.
6. If the reviewer **approves** it, the question is published at once (version 1), and the reviewer's note, if
   any, is kept in the history.
7. The **Review history** card, between the **Publication** and **History** cards, lists every send,
   withdrawal, approval and rejection, newest first, with the date and the reason or note. It appears only
   once the question has had one of these steps. The name of who acted is stored, but not shown yet.

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

### Annul and reinstate a question
When the organizer of an exam annuls a question, record it so practice stops using it. The question stays in
the bank, in its edition and in the history; only its status changes.
1. Open a **published** question and look at the **Publication** card. A draft cannot be annulled.
2. Choose **Annul question**. If the form has changes you did not save, the button is disabled and the card
   says "Save your changes first."
3. In the window, write the **reason** (up to 500 characters; it is required) and choose **Annul**. The
   question becomes **Annulled**: a yellow notice at the top says it cannot be edited, the card shows the
   date and the reason, every field is read only, and the History card gets a new row "Annulled" with the
   reason.
4. While a question is annulled it cannot be saved, published, unpublished or deleted, and the list offers
   neither Publish nor Unpublish for it. **Preview as a student** still works and shows the date and the
   reason above the statement. It keeps its topic, its edition and its number in the paper.
5. To undo it (a mistake, or the organizer reversed the decision), choose **Reinstate question**, write the
   reason and confirm. The question is **Published** again, can be edited, and the History card shows a row
   "Reinstated" with that reason.
6. In the list, filter the **Status** by **Annulled** to see them. Questions in practice are the published
   ones; how an annulled question counts in a full exam simulation is the edition's scoring rule, not set
   here.

The date shown is the day you recorded the annulment, not the organizer's own date; the reason is the place to
cite the official act.

### Preview as a student
1. In the **Publication** card, choose **Preview as a student**.
2. The window shows the base text, the statement and the options as a student will see them; a true/false
   question shows two lines, True and False, and a multiple-answer question starts with "Mark N options."
   above the options.
3. Choose **Show answer** to see the correct option (or the key) and the explanation. The preview always shows the saved
   version; save first to see your latest changes.

### Delete a question
1. Only a draft can be deleted. A published question has its Delete button disabled; unpublish it first. An
   annulled question cannot be deleted at all; reinstate it, unpublish it, and then delete it if you must.
2. Choose **Delete** on its row and confirm. The topic stays.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Question type | How the question is answered | Single choice, true/false or multiple answer. Chosen when the question is created; it cannot be changed afterwards |
| Subject | Used to pick the topic | Required on screen; the question stores only its topic |
| Topic | The topic the question belongs to | Required. A topic cannot be deleted while a question uses it |
| Content language | The language the question is written in | Required, one of the app's languages. Content is never translated |
| Difficulty | How hard the question is | Optional, a whole number from 1 to 5 |
| Statement | What the student is asked | Required, up to 10,000 characters |
| Base text | A passage the question refers to, written once and shared by every question that links it | Optional. Picked from the base texts in the question's language; see [Base texts](base-texts.md) |
| Options | The choices, letters A to E | Up to 5. To publish: 2 to 5, each with text, exactly one correct (multiple answer: 3 to 5, at least two correct and at least one wrong). Each up to 2,000 characters |
| Answer key | Whether the statement of a true/false question is true or false | Only for true/false. May be empty in a draft; required to publish. A published question cannot lose it |
| Explanation | Why the key is right | Optional, up to 10,000 characters |
| Reason for the answer key change | Why the key of a published question changed | Required when a save or a publish records a version whose key differs from the previous one; 1 to 500 characters. A reason on any other new version is kept as a note |
| Reason (annul or reinstate) | Why a published question was annulled, or an annulled one taken back | Required, 1 to 500 characters. Kept in the History card with the date |
| Edition | The past paper the question came from | Optional. With no edition the question is authored. An edition cannot be deleted while a question uses it |
| Number in the paper | The question's position in the edition's paper | Required with an edition, a whole number from 1 to 500, unique among the edition's questions |
| Notice subject | The subject of the edition's notice the question belongs to | Optional, only with an edition, and of that same edition. It cannot be deleted while a question uses it |

Only plain formatting is kept: scripts, styles and any link that is not `http` or `https` are removed when
the question is saved, and the page shows what was kept.

## Messages
| Message | What it means | What to do |
|---|---|---|
| Write the statement. | The statement is empty. | Write what the student is asked. |
| The statement has more than 10,000 characters. (also the explanation and option limits) | The text is over its limit. | Shorten it. Nothing is cut for you. |
| A question is published with 2 to 5 options. | Fewer than two options, or more than five. | Add or remove options. |
| Every option needs text. | A published question has an empty option. | Write the text or remove the option. |
| Mark exactly one option as correct. | No correct option, or more than one, on a single-choice question. | Mark one. |
| A multiple-answer question is published with 3 to 5 options. | Fewer than three options. | Add options. |
| Mark at least two options as correct, and leave at least one wrong. | A multiple-answer question has fewer than two correct options, or every option is marked. | Mark two or more, and leave one wrong. |
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
| Write the reason. | The reason of an annulment or a reinstatement is empty. | Type the reason, up to 500 characters. |
| This question is annulled and cannot be changed. Reinstate it first. | You tried to save, publish, unpublish or delete an annulled question. | Choose **Reinstate question** first, if the annulment was a mistake. |
| This question is already annulled. / This question is not annulled. | Someone else changed it first. | Reload the page. |
| Only a draft can be deleted. Unpublish the question first. | The question is published. | Unpublish it, then delete. |
| This question is already published. / This question is not published. | Someone else changed it first. | Reload the page. |
| This question is in review and cannot be changed. Withdraw it from review first. | You tried to save, publish, unpublish or delete a question in review. | Choose **Withdraw from review** and try again. |
| This question is already in review. | It was sent earlier, by you or someone else. | Reload the page. |
| This question is not in review. | Someone else withdrew it or decided it first. | Reload the page. |
| This question is in review. Withdraw it before deleting. | Delete is disabled for a question in review. | Withdraw it from review, then delete. |
| This question no longer exists. | Someone deleted it while your screen was open. | Go back to the list. |
| Only http and https addresses. | The link you typed is not a web address. | Type an address that starts with https://. |
| Page not found | Your account cannot manage the question bank. | Talk to an admin if you think it should. |

## Using the editor with the keyboard
- The toolbar is one tab stop: use Left and Right to move between its buttons, Home and End to jump.
- Tab from the toolbar enters the text; Tab again leaves the editor, it never traps the focus.
- Ctrl+B, Ctrl+I and Ctrl+U toggle bold, italic and underline.
- A field with an error says so when you leave it, and a failed save moves the focus to the list of errors.

## Related pages
- [Review queue](review-queue.md): review, approve and reject questions sent for review
- [Base texts](base-texts.md): the passages several questions share
- [Subjects and topics](subjects.md): the subjects and topics a question points at
- [Exam editions](exam-editions.md): the papers questions come from
- [Roles and permissions](roles.md): who can manage the question bank
- [Getting around](getting-around.md): menu, light and dark mode, language and keyboard
- [Simulab](index.md)
