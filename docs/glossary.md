<!--
Business vocabulary and its English identifier.
Add a row the first time a business term becomes code.
The UI labels come from resource files, not from this table.
Seeded from product/brief.md at bootstrap. pt-PT terms marked (?) need the owner's review when Portugal enters.
-->
# Glossary

| Business term (pt-BR) | pt-PT | English identifier | Meaning |
|---|---|---|---|
| Tipo de avaliação | Tipo de avaliação | `AssessmentType` | Public service exam, certification, university entrance exam, ENEM |
| Concurso público | Concurso público | `PublicServiceExam` | Assessment type: competitive exam for a public position |
| Certificação | Certificação | `Certification` | Assessment type: professional certification |
| Vestibular | Exame de acesso (?) | `UniversityEntranceExam` | Assessment type: university entrance exam |
| ENEM | — | `Enem` | Brazilian national secondary education exam, scored with Item Response Theory |
| Banca | Entidade organizadora | `Organizer` | The institution that elaborates, applies and marks a paper: an exam board, a certifying body or a university running its own entrance exam. It is what an edition points at (F-35); it never publishes a notice (F-34 v2) |
| Tipo de organizadora | Tipo de entidade organizadora (?) | `OrganizerKind` | What an organizer is: `ExamBoard`, `CertifyingBody` or `University` (F-33) |
| Sigla | Sigla | `Acronym` | The organizer's short name (CEBRASPE, FGV); unique, uppercase, at most 20 characters (F-33) |
| Órgão contratante | Entidade contratante (?) | `IssuingAuthority` | The body that publishes the notice and defines the positions, the syllabus, the schedule and the rules of an exam (a city hall, a state government, a ministry, a university, a company). Its own entity and table since F-34 v2; the exam belongs to it, and it never applies a paper |
| Abrangência | Abrangência | `ExamScope` | How far an exam reaches: `National`, `State` or `Municipal`. The state or the municipality is the `ScopeDetail` (F-34) |
| Idioma do conteúdo | Idioma do conteúdo | `ContentLanguage` | The language an exam and its questions are written in; never translated (ADR-0001 #27, F-34) |
| Prova / Concurso | Prova | `Exam` | An assessment its `IssuingAuthority` contracts and publishes a notice for; the board that applies each paper is on the edition (F-34) |
| Edição | Edição | `ExamEdition` | One exam actually applied: its notice, its year and the job it selects for, named in the edition itself. An edital that opens several jobs with different papers becomes one edition per paper (owner, 2026-09-20) |
| Edital | Aviso | `Notice` | The official document that opens an edition; its subjects are the `NoticeSubject` rows |
| Cargo | Posto de trabalho | `Position` | The job one edition's paper selects for, as free text on the edition; optional, there is no job entity (F-35) |
| Número do edital | Referência do aviso | `NoticeReference` | How the notice names itself ("Edital nº 01/2026"); editions cut from the same edital share it (F-35) |
| Data de aplicação | Data de aplicação | `AppliedOn` | The day the paper of an edition was applied; optional (F-35) |
| Rascunho / Publicada (edição) | Rascunho / Publicada | `ExamEditionStatus` (`Draft` / `Published`) | Whether students can see an edition; `InReview` joins with the AI import (F-35) |
| Caderno / Seção | Secção | `Section` | A part of a paper with its own questions, order and rules. Not decided: it exists only if the Exam Simulator reproduces the paper divided into booklets — settled when epic 695 is refined (owner, 2026-09-20) |
| Matéria / Disciplina | Disciplina | `Subject` | Top level of the canonical taxonomy: what a student studies (Constitutional Law, Portuguese, Logical Reasoning). Simulae's `KnowledgeDomain` becomes this |
| Assunto / Tópico | Tópico | `Topic` | Second and last level of the canonical taxonomy, inside a subject |
| Área | Área | `Area` | Optional grouping attribute of a subject (Law, Languages, Natural Sciences). Not a taxonomy level. Simulae's top-level `Subject` becomes this |
| Disciplina do edital | Disciplina do aviso (?) | `NoticeSubject` | A subject as one edition's notice names and groups it, with number of questions, weight and minimum; mapped to canonical subjects and topics |
| Apelido | Alias (?) | `SubjectAlias` / `TopicAlias` | A name an organizer uses for a canonical subject or topic; used to map imports |
| Conteúdo programático | Conteúdo programático | `NoticeSubject` (the rows of a notice) | What the notice says the edition covers: it is the list of notice subjects, not an entity of its own (owner, 2026-09-20). `SubjectCoverage` is retired before its first use |
| Questão | Questão | `Question` | An item a student answers, of one of the supported types |
| Tipo de questão | Tipo de questão | `QuestionType` | SingleChoice, MultipleAnswer, TrueFalse, Matching, FillInTheBlanks, ShortAnswer, OpenAnswer, Essay |
| Texto-base | Texto de apoio (?) | `BaseText` | Text or image shared by one or more questions |
| Gabarito | Chave de respostas (?) | `AnswerKey` | The official correct answers; may change after appeals |
| Comentário / Explicação | Explicação | `Explanation` | Why the answer is right or wrong |
| Questão anulada | Questão anulada | `AnnulledQuestion` | A question cancelled by the organizer; scoring follows the organizer's rule |
| Dificuldade | Dificuldade | `Difficulty` | Difficulty level of a question |
| Questão autoral | Questão de autor (?) | `AuthoredQuestion` | A question written by a curator or admin, not from a past exam |
| Rascunho / Revisão / Publicado | Rascunho / Revisão / Publicado | `Draft` / `InReview` / `Published` | Content workflow states |
| Importação de prova | Importação de prova | `ExamImport` | AI-assisted extraction of a past exam into a draft |
| Simulado de prova | Simulação de prova (?) | `ExamSimulation` | Exam Simulator session that reproduces a real edition |
| Simulado personalizado | Simulação personalizada (?) | `PracticeSession` | Question Bank Simulator session built from filters |
| Tentativa | Tentativa | `Attempt` | One run of a simulation by a student, with answers and times |
| Cartão-resposta | Folha de respostas | `AnswerSheet` | Where the candidate marks final answers |
| Regra de pontuação | Regra de pontuação | `ScoringRule` | How an organizer turns answers into a score |
| Penalidade por erro | Penalização por erro | `WrongAnswerPenalty` | Points lost for a wrong answer (for example Cebraspe) |
| Nota de corte | Nota mínima (?) | `CutOffScore` | The minimum score to pass or be classified |
| Desempenho | Desempenho | `Performance` | Analytics of a student's results |
| Recomendação de estudo | Recomendação de estudo | `StudyRecommendation` | Topics and practice sets suggested to a student |
| Coach | Coach | `Coach` | The conversational AI study coach |
| Plano de estudos | Plano de estudos | `StudyPlan` | The coach's plan toward a target exam and date |
| Prova-alvo | Prova-alvo | `TargetExam` | The exam and date a student prepares for |
| Estudante | Estudante | `Student` | Role: practices and follows progress |
| Curador | Curador | `Curator` | Role: imports, reviews, writes and publishes content |
| Administrador | Administrador | `Admin` | Role: manages catalog, users, roles, plans |
| Papel / Permissão | Perfil / Permissão (?) | `Role` / `Permission` | RBAC |
| Papel de sistema | Perfil de sistema (?) | `Role.IsSystem` | A seed role (Student, Curator, Admin): cannot be renamed or deleted, only its permissions change (F-9) |
| Atribuição de papel | Atribuição de perfil (?) | `UserRole` | A role given to a user; a user may hold several and gets the union of their permissions (F-9) |
| Mudança de papel / Histórico de papéis | Alteração de perfil / Histórico de perfis (?) | `RoleChange` | One recorded change to a role or to a user's roles: author, time, before and after (F-14) |
| Plano | Plano | `Plan` | What a user is entitled to: features and limits. Not to be confused with `StudyPlan` |
| Atribuição de plano | Atribuição de plano | `PlanAssignment` | A plan given to a user, with validity and source |
| Código promocional | Código promocional | `PromoCode` | A code that grants a target plan for a number of days |
| Campanha | Campanha | `Campaign` | A group of promo codes; one redemption per user per campaign |
| Resgate | Resgate (?) | `Redemption` | A user's use of a promo code |
| Cota / Consumo de IA | Quota / Consumo de IA | `AiQuota` / `AiUsage` | The AI limit of a plan and what a user has consumed in a period |
| Consentimento | Consentimento | `ConsentRecord` | A recorded acceptance (privacy policy, age declaration) |
| Cadastro / Criar conta | Registo / Criar conta | `Registration` | A visitor creating an account: sign-up form, endpoint and command |
| Usuário | Utilizador | `User` | An account that signs in; inherits `IdentityUser<Guid>` (declared exception to `TenantEntity`) |
| Situação da conta | Estado da conta | `AccountStatus` | `Pending` until the email is verified, then `Active` |
| Verificação de e-mail | Verificação de e-mail | `EmailVerification` / `EmailVerificationToken` | The single-use hashed token, valid 24 h, that activates an account |
| Reenvio da verificação | Reenvio da verificação | `ResendVerification` | Asking for a new verification email; throttled |
| Redefinição de senha / Esqueci a senha | Redefinição da palavra-passe | `PasswordReset` / `PasswordResetToken` | Asking for a link by email and choosing a new password with it; the single-use hashed token lives 1 h (F-7) |
| Troca de senha | Alteração da palavra-passe | `PasswordChange` | A signed-in user changing the password with the current one (F-7) |
| Senha | Palavra-passe | `Password` | The account's secret; policy 12 / uppercase / digit / symbol |
| Maioridade declarada | Maioridade declarada | `IsAdultDeclared` | The 18+ self-declaration made at sign-up (ADR-0001 #17) |
| Termos de Uso | Termos de Utilização | `LegalDocument` (topic `Terms`) | Versioned institutional document the user accepts |
| Política de Privacidade | Política de Privacidade | `LegalDocument` (topic `Privacy`) | Versioned privacy document the user accepts |
| Idioma preferido | Idioma preferido | `PreferredLanguage` | The user's locale: first culture source after sign-in and the language of their emails (F-8) |
| Minha conta | A minha conta | `Account` (page `/account`) | The signed-in user's own page: profile and link to change the password (F-8) |
| Perfil / Nome de exibição | Perfil / Nome de apresentação | `Profile` / `FullName` | The user's editable data: display name and preferred language (F-8) |
| Apagamento de conta | Eliminação da conta | `AccountErasure` | The user erasing their own account: the personal data in Identity is overwritten and the id stays as a pseudonym (F-10, ADR-0001 #9) |
| Conta apagada | Conta eliminada | `AccountStatus.Erased` | The state of an account after erasure: invisible to every lookup, with a tombstone address (F-10) |
| Baixar meus dados | Transferir os meus dados | `DataExport` | The user downloading, as one JSON file, everything the app holds about them (portability, F-16) |
| Verificação em duas etapas | Verificação em dois passos | `TwoFactor` / `Totp` | The second barrier at sign-in: a six-digit code from an authenticator app (F-11) |
| Código de recuperação | Código de recuperação | `RecoveryCode` | One of the ten single-use codes that replace the authenticator when the phone is gone (F-11) |
| Desafio de segunda etapa | Desafio de segundo passo | `TotpChallenge` | The single-use token the password step returns when the account asks for a code (F-11) |
| Entrar com o Google | Iniciar sessão com o Google | `GoogleSignIn` | Signing up or in with a Google account; the Api checks Google's ID token itself (F-20) |
| Vínculo com o Google | Ligação ao Google | `user_logins` row (provider `Google`) | The link between an account and a Google account's subject (`sub`), made on the first Google sign-in (F-20) |

## Forbidden terms in identifiers
Portuguese terms from Simulae that must not appear in code: `Banca`, `Concurso`, `Edital`, `Prova`, `Questao`, `Disciplina`, `Assunto`, `Gabarito`, `Simulado`, `Cadastro`, `Senha`, `Usuario`, `Plano`, `Cargo`.

## Technical terms
Terms used in reports, reviews and item files. They are not identifiers. The pt-BR column is the word to use when talking to the owner.

| Term | pt-BR | Meaning |
|---|---|---|
| blocker | bloqueador | Review finding that stops the merge until it is fixed. |
| major | grave | Review finding that is a real defect or a test gap; fixed before validation unless the owner decides otherwise. |
| minor | leve | Small review finding; fixed, accepted with a reason, or turned into a new item. |
| acceptance criterion (AC) | critério de aceite | A Given/When/Then sentence that a test proves. |
| app host | app host | The Aspire project (`Simulab.AppHost`) that starts the Web, the Api and the local containers (PostgreSQL, Redis, Mailpit) together. |
| claim | claim (dado da sessão) | A fact about the user kept in the cookie or the token: id, email, name, permissions. |
| concurrency failure | conflito de concorrência | Two saves of the same record at once; the second finds the record already changed. |
| last-write-wins | a última gravação vale | Concurrency rule where the latest save prevails. |
| audit trail | trilha de auditoria | Recorded history of who changed what and when, kept unchanged. |
| CSRF | CSRF (requisição forjada) | Cross-site request forgery: another site makes a signed-in user's browser trigger an action in the app. |
| SameSite=Lax | SameSite=Lax | Cookie setting: the browser does not send it on requests from another site, except a top-level GET navigation (a link). |
| Sec-Fetch-Site | Sec-Fetch-Site | Header the browser sends saying where a request came from (`same-origin` means from the app itself). |
| open redirect | redirecionamento aberto | A flaw where the app redirects to an outside address taken from the URL; avoided by accepting only paths inside the app. |
| connection pool | pool de conexões | Database connections kept open for reuse; when they are not released they exhaust the server's limit. |
| merge base | base do merge | The commit where the item branch left `main`; a review compares from it. |
| bUnit | bUnit | Library that tests Blazor components without a browser. |
| WebApplicationFactory | WebApplicationFactory | Starts the Api or the Web inside a test so it is called through real HTTP. |
| coverage gap | lacuna de cobertura | A criterion with no test through the path a user reaches. |
| validation script | roteiro de validação | At most 8 steps the owner follows on screen before the merge. |
| gate | gate (portão) | An automatic check that must pass before the work goes on: build, tests, no new warnings. |
| warnings baseline | baseline de avisos | The build warnings accepted so far; the gate fails only on new ones. |
| WCAG 2.2 AA | WCAG 2.2 AA | The accessibility level the app targets (ADR-0001 #29); among other things, text needs a contrast of at least 4.5:1 with its background (3:1 for large text). |
| contrast ratio | contraste | How far apart a text colour and its background are in brightness, from 1:1 (same colour) to 21:1 (black on white). |
| theme token | cor do tema | A named colour of the palette (`Primary`, `Surface`, `LinesInputs`...), defined once in `SimulabTheme` for each theme and used by every screen (F-17). |
| hover tone | tom ao passar o mouse | The darker shade (`<Colour>Darken`) a filled button takes while the pointer is over it (F-17). |
| anonymization | anonimização | Overwriting the data that names a person, keeping the rest of the record usable as statistics. |
| tombstone | lápide (valor-lápide) | The meaningless value written over a personal one so the column stays filled and unique (F-10: `erased-<id>@erased.invalid`). |
| danger zone | zona de risco | The part of a screen that holds the destructive actions, set apart and marked as such. |
| integration event | evento de integração | A past-tense record one module publishes so other modules can react, without either knowing the other (`IIntegrationEvent`). |
| job queue | fila de jobs | The `jobs` table plus the worker in the Api host (ADR-0001 #20, F-13): work a request stages and a background worker runs, retries and gives up on. |
| outbox | outbox (caixa de saida) | Writing the message to send in the same transaction as the data that justifies it, so the two can never disagree (F-13 BR2). |
| backoff | espera progressiva | Waiting longer before each retry (F-13: 1, 2, 4, 8, 16 minutes). |
| SKIP LOCKED | SKIP LOCKED | PostgreSQL clause that makes a reader skip rows another transaction has locked instead of waiting for them; it is what lets two workers share one queue (F-13 BR8). |
| at-least-once | pelo menos uma vez | Delivery guarantee where a message may arrive more than once but never zero times (F-13 BR10). |
| TOTP | TOTP (código temporário) | Time-based one-time password (RFC 6238): a six-digit code an authenticator app derives from a shared secret and the clock, valid for 30 seconds. |
| replay | reuso do código | Sending a code that was already accepted, while its 30-second window is still open; refused by storing the last accepted step. |
| AES-GCM | AES-GCM | The encryption used for the TOTP secret at rest: it both hides the value and detects a tampered one. |
| HMAC | hash com chave | A hash computed with a secret key (F-11: the recovery codes); without the key, a copy of the database cannot be used to guess the codes. |
| grant (OAuth2) | grant (tipo de concessão) | The kind of exchange the token endpoint performs: password, refresh token, and the `totp` one F-11 adds for the second step. |
| UI kit | kit de interface | The app's own components (`Components/Ui/`, shown at `/dev/ui`) that pages use instead of the library's raw ones, so every screen behaves the same. |
| C4 model | modelo C4 | A way to draw architecture in zoom levels: system context (who uses the system and what it talks to), containers, components, code (F-23). |
| container (C4) | contêiner | In the C4 model, a process or store that runs on its own (the Web, the Api, PostgreSQL, Redis); not necessarily a Docker container. |
| drift guard | teste contra divergência | A test that fails when a hand-written document no longer matches the code it describes (F-23: the overview against the app host). |
