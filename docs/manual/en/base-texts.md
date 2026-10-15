---
page: base-texts
locale: en
features: [F-102]
updated: 2026-10-15
---
# Base texts

A base text is a passage, a table or a case that several questions refer to, as in a reading comprehension
block. You write it once, give it a title and link each question to it; every linked question shows the text
before its statement. Fixing a word in the base text fixes it for all of them.

## Who can use it
An account with the permission "Manage the question bank". Curators and Admins have it. Everyone else gets
Page not found, and the menu has no Base texts entry.

## How to
### See the base texts
1. Choose **Base texts**, in the **Content** section of the side menu, right after **Questions**.
2. The list shows the title, the language and how many questions use each text, sorted by title.
3. Type in the search box to find a word of the title (case and accents do not matter), or use **Language**
   to see the texts of one language.

### Write a base text
1. Choose **Add**.
2. Type the **title**, up to 200 characters. It is only for you to find the text; students do not see it.
3. Choose the **content language**: the language the text is written in. Only questions in the same language
   can use it.
4. Write the **text** in the editor. The toolbar offers bold, italic, underline, strikethrough, superscript,
   subscript, lists, a quote, a link and clear formatting. Images are not supported yet.
5. Choose **Save**. The page becomes the edit page of the text.

### Link a question to a base text
1. Open the question (**Questions**, then **Add** or **Edit** on a row).
2. In the **Base text** card, after the statement, click the field and type part of the title. Only texts in
   the question's language are offered, each with the number of questions that already use it.
3. Pick one and choose **Save**. **Open the base text** shows it in a new tab and **Show the text** reads it
   without leaving the question.
4. To take the link away, choose the **x** of the field and save. The text itself stays.
5. The text you want is not there? Choose **Create a base text**. It opens in a new tab, so the question you are
   writing stays as it is: save the text there, come back and pick it.

If you change the content language of a question that has a base text of another language, the choice is
cleared and a notice tells you which text was removed.

### Change a base text
1. Choose **Edit** on its row, change the title or the text and choose **Save**.
2. Every **published** question that uses the text gets a new version (see **Versions** in
   [Questions](questions.md)): students and exam attempts keep what they were shown, and the new ones see the
   new wording. Drafts follow the text and record nothing; annulled questions are not changed.
3. Changing only the title records nothing.
4. The language cannot be changed while a question uses the text.

### See which questions use a text
Open the text. The **Questions that use this text** card lists them, most recently changed first, with the
statement, the source and the status. Choose **Edit** on a row to open the question.

### Delete a base text
1. Only a text that no question uses can be deleted. While one does, **Delete** is disabled and says how many
   questions use the text.
2. Open each question, remove the link and save. Then choose **Delete** on the row (or in the **Delete this
   base text** card of the text) and confirm.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Title | The name you find the text by | Required, up to 200 characters |
| Content language | The language the text is written in | Required, one of the app's languages. Locked while a question uses the text |
| Text | The passage, table or case | Required, up to 20,000 characters |

Only plain formatting is kept: scripts, styles and any link that is not `http` or `https` are removed when the
text is saved, and the page shows what was kept.

## Messages
| Message | What it means | What to do |
|---|---|---|
| Write the title. | The title is empty. | Type a title. |
| The title has more than 200 characters. | The title is over its limit. | Shorten it. |
| Write the text. | The text is empty. | Write the passage. |
| The text has more than 20,000 characters. | The text is over its limit. | Shorten it. Nothing is cut for you. |
| Choose one of the listed languages. | The language is not on the list. | Pick a language from the list. |
| Questions still use this base text. Remove their links before deleting it. | One or more questions still link the text. | Remove the links first. |
| Questions use this base text, so its language cannot change. | You tried to change the language of a used text. | Remove the links, or create another text. |
| This base text no longer exists. | It was deleted while your screen was open. | Go back to the list. |
| This base text no longer exists. Pick another or leave the field empty. | The question links a text that was deleted meanwhile. | Pick another text or leave the field empty. |
| The base text and the question must be in the same language. | The text you picked is in another language. | Pick a text in the question's language. |
| Another save changed this question's history first. Reload the page and try again. | Your save of the text lost a race with another one. | Reload the page and save again; nothing was written. |
| Page not found | Your account cannot manage the question bank. | Talk to an admin if you think it should. |

## Related pages
- [Questions](questions.md): write, publish and find questions, and see their versions
- [Getting around](getting-around.md): menu, light and dark mode, language and keyboard
- [Simulab](index.md)
