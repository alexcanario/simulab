# Catalog — routes

Generated from `docs/api/Simulab.Api.json`. Do not edit.

| Verb | Route | Summary | Responses |
|---|---|---|---|
| `DELETE` | `/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects/{id}` | DeleteNoticeSubject | 200 |
| `DELETE` | `/api/v1/catalog/exams/{examId}/editions/{id}` | DeleteExamEdition | 200 |
| `DELETE` | `/api/v1/catalog/exams/{id}` | DeleteExam | 200 |
| `DELETE` | `/api/v1/catalog/issuing-authorities/{id}` | DeleteIssuingAuthority | 200 |
| `DELETE` | `/api/v1/catalog/organizers/{id}` | DeleteOrganizer | 200 |
| `DELETE` | `/api/v1/catalog/subjects/{id}` | DeleteSubject | 200 |
| `DELETE` | `/api/v1/catalog/topics/{id}` | DeleteTopic | 200 |
| `GET` | `/api/v1/catalog/areas` | ListAreas | 200 |
| `GET` | `/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects` | ListNoticeSubjects | 200 |
| `GET` | `/api/v1/catalog/exams/{examId}/editions/{id}` | FindExamEdition | 200 |
| `GET` | `/api/v1/catalog/exams/{examId}/editions` | ListExamEditions | 200 |
| `GET` | `/api/v1/catalog/exams/{id}` | FindExam | 200 |
| `GET` | `/api/v1/catalog/exams` | ListExams | 200 |
| `GET` | `/api/v1/catalog/issuing-authorities` | ListIssuingAuthorities | 200 |
| `GET` | `/api/v1/catalog/organizers` | ListOrganizers | 200 |
| `GET` | `/api/v1/catalog/published-exam-filters` | GetPublishedExamFilters | 200 |
| `GET` | `/api/v1/catalog/published-exams/{id}` | FindPublishedExam | 200 |
| `GET` | `/api/v1/catalog/published-exams` | ListPublishedExams | 200 |
| `GET` | `/api/v1/catalog/subjects/{id}` | FindSubject | 200 |
| `GET` | `/api/v1/catalog/subjects/{subjectId}/topics` | ListTopics | 200 |
| `GET` | `/api/v1/catalog/subjects` | ListSubjects | 200 |
| `POST` | `/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects/{id}/move` | MoveNoticeSubject | 200 |
| `POST` | `/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects` | CreateNoticeSubject | 200 |
| `POST` | `/api/v1/catalog/exams/{examId}/editions` | CreateExamEdition | 200 |
| `POST` | `/api/v1/catalog/exams` | CreateExam | 200 |
| `POST` | `/api/v1/catalog/issuing-authorities` | CreateIssuingAuthority | 200 |
| `POST` | `/api/v1/catalog/organizers` | CreateOrganizer | 200 |
| `POST` | `/api/v1/catalog/subjects/{subjectId}/topics` | CreateTopic | 200 |
| `POST` | `/api/v1/catalog/subjects` | CreateSubject | 200 |
| `PUT` | `/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects/{id}` | UpdateNoticeSubject | 200 |
| `PUT` | `/api/v1/catalog/exams/{examId}/editions/{id}` | UpdateExamEdition | 200 |
| `PUT` | `/api/v1/catalog/exams/{id}` | UpdateExam | 200 |
| `PUT` | `/api/v1/catalog/issuing-authorities/{id}` | UpdateIssuingAuthority | 200 |
| `PUT` | `/api/v1/catalog/organizers/{id}` | UpdateOrganizer | 200 |
| `PUT` | `/api/v1/catalog/subjects/{id}` | UpdateSubject | 200 |
| `PUT` | `/api/v1/catalog/topics/{id}` | UpdateTopic | 200 |
