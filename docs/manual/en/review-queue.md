---
page: review-queue
locale: en
features: [F-105]
updated: 2026-10-17
---
# Review queue

The review queue gathers the questions a curator sent for review. A reviewer reads the question as the student
will see it, with the answer key and the explanation, and either **approves** it (it is published at once) or
**rejects** it with a reason (it goes back to a draft). Whoever writes the question sends it from its own
page; see [Questions](questions.md).

## Who can use it
An account with the permission "Review questions". Curators and Admins have it. Everyone else gets Page not
found, and the menu has no Review queue entry. The permission is independent of "Manage the question bank": an
account with only "Review questions" sees the queue and the review screen, but cannot edit or send questions.
Whoever sent the question can approve it, if they hold both permissions.

## How to
### See the queue
1. Choose **Review queue**, in the **Content** section of the side menu.
2. The list shows only questions **In review**, from the one waiting longest to the newest (the latest send
   counts): the start of the statement, the topic with its subject, the type, the language and the date it
   was sent.
3. With nothing waiting, the page says "Nothing is waiting for review."
4. Choose the statement, or **Review** in the row menu, to open the review screen.

### Review a question
1. The review screen shows the base text, the statement and the options, and always the answer key, the
   explanation and the classification of the question (subject, topic, language, difficulty, source and the
   date it was sent). Nothing here can be edited.
2. Choose **Approve** to publish. The dialog asks for an optional **note**, up to 500 characters, which is
   kept in the question's review history. Confirm with **Approve**.
3. Or choose **Reject**. The dialog asks for the **reason**, which is required, from 1 to 500 characters. The
   curator sees it on the question's page. Confirm with **Reject**.
4. The text fields in the dialogs show how many characters you have typed, like "12/500". The counter stays
   beside an error message too.
5. After you decide, you return to the queue and see "Question approved and published." or "Question
   rejected. It is a draft again."

If someone else decided before you, or the curator withdrew the question from review, the screen shows "This
question is no longer in review: another reviewer decided it, or it was withdrawn." and the link **Back to the
queue**. If the question was withdrawn and sent again while you were reading it, your decision is refused so
that you read it again.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Note (approve) | A message from the reviewer, kept in the review history | Optional, plain text, up to 500 characters |
| Reason (reject) | Why the question was rejected | Required, plain text, from 1 to 500 characters |

## Messages
| Message | What it means | What to do |
|---|---|---|
| Write the reason for the rejection. | The reason is empty. | Type the reason, up to 500 characters. |
| The reason or note has more than 500 characters. | The text went over the limit. | Shorten it. |
| This question is not in review. | Someone else decided before you, or the question was withdrawn. | Go back to the queue. |
| This question no longer exists. | Someone deleted it while the screen was open. | Go back to the queue. |
| Another save changed this question first. | Two decisions were sent together, or the question was withdrawn and sent again. | Reopen the question and decide again. |
| Page not found | Your account does not have the permission "Review questions". | Ask an administrator if you think it should. |

## Related pages
- [Questions](questions.md): write a question and send it for review
- [Roles and permissions](roles.md): who can review questions
- [Getting around](getting-around.md): menu, light and dark mode, language and keyboard
- [Simulab](index.md)
