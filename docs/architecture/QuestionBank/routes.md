# QuestionBank — routes

Generated from `docs/api/Simulab.Api.json`. Do not edit.

| Verb | Route | Summary | Responses |
|---|---|---|---|
| `DELETE` | `/api/v1/question-bank/questions/{id}` | DeleteQuestion | 200 |
| `GET` | `/api/v1/question-bank/editions/{editionId}/notice-subjects` | ListQuestionEditionNoticeSubjects | 200 |
| `GET` | `/api/v1/question-bank/editions/{editionId}/questions` | ListEditionQuestions | 200 |
| `GET` | `/api/v1/question-bank/editions/{editionId}` | FindQuestionEdition | 200 |
| `GET` | `/api/v1/question-bank/editions` | SearchQuestionEditions | 200 |
| `GET` | `/api/v1/question-bank/questions/{id}` | FindQuestion | 200 |
| `GET` | `/api/v1/question-bank/questions` | ListQuestions | 200 |
| `GET` | `/api/v1/question-bank/subjects/{subjectId}/topics` | ListQuestionSubjectTopics | 200 |
| `GET` | `/api/v1/question-bank/subjects` | SearchQuestionSubjects | 200 |
| `POST` | `/api/v1/question-bank/questions/{id}/publish` | PublishQuestion | 200 |
| `POST` | `/api/v1/question-bank/questions/{id}/unpublish` | UnpublishQuestion | 200 |
| `POST` | `/api/v1/question-bank/questions` | CreateQuestion | 200 |
| `PUT` | `/api/v1/question-bank/questions/{id}` | UpdateQuestion | 200 |
