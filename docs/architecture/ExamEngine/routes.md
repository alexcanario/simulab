# ExamEngine — routes

Generated from `docs/api/Simulab.Api.json`. Do not edit.

| Verb | Route | Summary | Responses |
|---|---|---|---|
| `DELETE` | `/api/v1/exam-engine/attempts/{id}/answers/{questionId}` | ClearAnswer | 200 |
| `GET` | `/api/v1/exam-engine/attempts/{id}/content` | ReadAttemptContent | 200, 404 |
| `GET` | `/api/v1/exam-engine/attempts/{id}/images/{imageId}` | OpenAttemptImage | 200, 404 |
| `GET` | `/api/v1/exam-engine/attempts/{id}` | ReadAttempt | 200 |
| `POST` | `/api/v1/exam-engine/attempts/{id}/submit` | SubmitAttempt | 200 |
| `POST` | `/api/v1/exam-engine/attempts` | StartAttempt | 200 |
| `PUT` | `/api/v1/exam-engine/attempts/{id}/answers/{questionId}` | SaveAnswer | 200 |
