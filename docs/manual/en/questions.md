---
page: questions
locale: en
features: [F-95]
updated: 2026-10-11
---
# Questions

The question bank is where curators write the questions that exams and practice are built from. A question
has a statement, an optional base text, from two to five options with one correct, an optional explanation, a
difficulty, a topic and the language it is written in. This page covers single-choice questions.

## Who can use it
An account with the permission "Manage the question bank". Curators and Admins have it. Everyone else gets
Page not found, and the menu has no Questions entry.

## How to
### See the questions
1. Choose **Questions**, in the **Content** section of the side menu.
2. The list shows the start of each statement, the topic with its subject, the difficulty, the language and
   the status, newest first.
3. Type in the search box to find a word of the statement. Case and accents do not matter.
4. Use **Status** to show only drafts or only published questions.

### Write a question
1. Choose **Add**.
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

A draft may be saved with no options and no correct one yet. The list "Before publishing", beside the form,
shows what is still missing.

### Publish and unpublish
1. Open the question and look at the **Publication** card, below the form.
2. **Publish** is available when the question is saved and the checklist is complete: topic, language,
   statement, two to five options, every option with text and exactly one correct. Choosing **Save** never
   publishes.
3. A published question can still be edited, but every edit must keep it complete.
4. **Unpublish** takes it back to a draft with the same content.

### Preview as a student
1. In the **Publication** card, choose **Preview as a student**.
2. The window shows the base text, the statement and the options as a student will see them.
3. Choose **Show answer** to see the correct option and the explanation. The preview always shows the saved
   version; save first to see your latest changes.

### Delete a question
1. Only a draft can be deleted. A published question has its Delete button disabled; unpublish it first.
2. Choose **Delete** on its row and confirm. The topic stays.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Subject | Used to pick the topic | Required on screen; the question stores only its topic |
| Topic | The topic the question belongs to | Required. A topic cannot be deleted while a question uses it |
| Content language | The language the question is written in | Required, one of the app's languages. Content is never translated |
| Difficulty | How hard the question is | Optional, a whole number from 1 to 5 |
| Statement | What the student is asked | Required, up to 10,000 characters |
| Base text | A text the question refers to | Optional, up to 20,000 characters |
| Options | The choices, letters A to E | Up to 5. To publish: 2 to 5, each with text, exactly one correct. Each up to 2,000 characters |
| Explanation | Why the key is right | Optional, up to 10,000 characters |

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
| Difficulty is a number from 1 to 5, or empty. | The difficulty is out of range. | Choose 1 to 5 or leave it empty. |
| Choose one of the supported languages. | The language is not on the list. | Pick a language from the list. |
| The topic no longer exists. Pick another. | The topic was deleted while you wrote. | Choose another topic. |
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
- [Roles and permissions](roles.md): who can manage the question bank
- [Getting around](getting-around.md): menu, light and dark mode, language and keyboard
- [Simulab](index.md)
