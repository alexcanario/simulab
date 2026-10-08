# Simulab

A study coach that runs realistic practice exams — public service exams, professional certifications, university entrance exams and ENEM — and tells each student what to study next.

## Problem
Candidates prepare with scattered PDFs of past exams and generic question banks. Practice rarely feels like the real exam (time limit, section order, answer sheet, scoring and penalty rules), so students are surprised on exam day. Nobody turns their results into a clear plan of what to study next. On the content side, building a reliable question bank from past exams by hand is slow and error-prone.

## Users
- **Student**: practices with realistic exams and custom question sets, follows progress, and gets guidance on what to study.
- **Curator**: imports past exams with AI help, reviews and corrects the extracted data, writes authored questions, and publishes content.
- **Admin**: manages the catalog (organizers, exams, subjects), users, roles, plans and content publishing.

## Capabilities
1. **Assessment catalog.** Assessment types: public service exam (concurso), certification, university entrance exam (vestibular), ENEM; Portuguese national exams later. Organizers: exam boards, certifying bodies, universities. Exams with editions, where an edition is one paper actually applied: its notice, its year and the job it selects for. An edital that opens several jobs with different papers becomes one edition per paper; there is no separate entity for the job or for a stage of the contest (owner, 2026-09-20). Each edition carries the subjects its notice lists, which are its syllabus.
2. **Subject taxonomy.**
   - Canonical taxonomy in two levels, Subject → Topic, shared across assessment types. A subject may carry an optional area (Law, Languages, Natural Sciences) used only for grouping.
   - Each exam edition keeps its own notice subjects: the label, grouping, number of questions and weight exactly as the notice states them, mapped to the canonical subjects and topics (for example "Raciocínio Lógico-Matemático" maps to Mathematics and Logical Reasoning).
   - Aliases link the different names organizers use to the same canonical subject or topic, and grow with every import.
3. **Question bank.**
   - Question types: single choice, multiple answer, true/false with penalty models, matching, fill in the blanks, short answer, open answer, essay.
   - Metadata: difficulty, base text and images, answer key and explanation.
   - Origin: a past exam edition, or authored by a curator or admin.
   - Workflow: draft, review, publish; annulled questions and changed answer keys are supported.
4. **AI-assisted exam import.** The curator uploads a scanned or digital past exam and its answer key. AI with OCR extracts the organizer, exam, edition, subjects, questions, answer types and answer key into a draft. The curator reviews and fixes the draft before anything is published.
5. **Exam Simulator.** Reproduces a real exam edition as closely as possible:
   - the same questions, order and sections;
   - the same time limit and rules (wrong-answer penalties, blank answers, answer sheet);
   - the score and cut-off calculated the way that organizer calculates them.
6. **Question Bank Simulator.** Custom practice built from the bank, using both exam questions and authored questions. Filters: subject or topic, organizer, assessment type, year, difficulty, unanswered or previously wrong. The student chooses the size, and whether it is timed.
7. **Performance analytics.** Per student:
   - score over time;
   - results by subject, topic, organizer and exam;
   - accuracy by difficulty and time per question;
   - strengths and weaknesses;
   - distance to the cut-off of the target exam.
8. **Study recommendations.** Topics to study next, and practice sets aimed at the subjects where the student needs to improve.
9. **AI coach.** A conversational coach for each student. It:
   - explains results and why an answer is right or wrong (grounded in the question's answer key and explanation);
   - builds a study plan toward a target exam and date;
   - adjusts the plan as results come in;
   - suggests the next practice session.
10. **Accounts and access.** Sign-up and sign-in, roles with permissions, plans that unlock features and AI usage, and an admin back office.
11. **Languages.** The app and its manual in pt-BR, pt-PT and en from day one.

## Constraints
- **Markets:** Brazil first (public service exams, certifications, vestibular, ENEM); Portugal next.
- **Team:** the product owner and Claude, working with the agile@canary workflow. Web app first.
- **Reuse from Simulae:** a large part of its code (.NET, Blazor, MudBlazor, Aspire, PostgreSQL) comes along. Mainly:
  - catalog of exam boards, exams and notices;
  - subject taxonomy;
  - question type editor (8 types);
  - identity, RBAC and plans;
  - seed data for municipal guard exams.
  Everything is renamed to English on the way in.
- **Privacy:** LGPD (Brazil) and GDPR (Portugal).
- **AI:**
  - every AI-extracted item is reviewed by a person before it is published;
  - the coach must not invent facts about exams, and it answers from the app's own data;
  - AI usage has limits per plan, and the cost per active student is tracked.
- **Content rights:** past exams are reproduced only within the rights that apply to each source.
- **Scoring rules differ per organizer** (for example, Cebraspe's wrong-answer penalty). ENEM uses Item Response Theory, which needs item parameters.

## Success
- A curator imports a past exam, reviews it and publishes it much faster than typing it by hand (target time to be set after the first import).
- Students who take an Exam Simulator session say it felt like the real exam.
- Students come back every week, and practice follows the recommendations.
- Scores in the subjects flagged as weak go up over the following weeks.
- The AI cost per active student stays within the margin of each plan.

## Out of scope (first version)
- Native mobile apps.
- Online payment and checkout (plans are assigned by an admin).
- Live classes and video content.
- Social features (forums, rankings between students).

## Open notes
- **First release order:** catalog + question bank + Question Bank Simulator first, or the Exam Simulator first? Where does AI import fit: before or after manual registration is solid?
- **Minors:** many ENEM and vestibular students are under 18. Simulae required 18+ by self-declaration. Keep that, or support parental consent?
- **Rights to reproduce past exams:** which sources are free to use, and what must be shown (source, organizer, year)?
- **First certifications:** which ones (IT, languages, finance)? Their formats and rules vary a lot.
- **ENEM scoring:** a realistic ENEM score needs IRT item parameters that may not be public. Use an approximate score, clearly labeled, or leave it out of v1?
- **Essays and open answers:** grading by a person, by AI, or out of v1?
- **AI:** which provider, and for what: import, coach, recommendations. Limits per plan.
- **Institutions:** will prep courses or schools have their own students and private question banks? This decides multi-tenancy.
- **Teachers and graders:** Simulae planned teacher and grader roles. Are they part of Simulab, and when?
- **Portugal:** which exams. When: after v1 (owner, 2026-10-04). Data stays in Brazil South, no EU region (ADR-0003).
