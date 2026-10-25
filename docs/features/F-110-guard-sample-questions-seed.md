---
feature: F-110
epic: Question bank
status: refining
board: 156
version: 1
---
# Sample questions of the municipal guard editions

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: refining (2026-10-25)
- Stopped at: Refinement started: worktree created, premises being checked
- Next action: Question round, then approval
- Waiting on: nothing

## Summary
Seeded questions for the municipal guard editions of F-37, so local work and testers start with real questions without typing them. Simulae has no question seed (its `GuardaMunicipalContentSeed` holds only the taxonomy). Source: epic Question bank (E-4).

Added 2026-10-24: the source is the Manaus 2026 paper (Instituto Consulplan, Técnico Municipal I - Guarda Municipal, booklet type 1, white) and its final answer key, free to use (owner, D-6). The seed holds its 60 multiple-choice questions with their base texts, question 47 annulled (F-101). The question with an image comes from the migration with a fixed image id and its file is stored by an idempotent seeder at app start-up (F-103 BR11). Claude proposes the subject and topic of each question from the F-51 taxonomy; the owner reviews the table at refinement. The essay question waits for F-109. The other editions leave the seed in F-161.

## Start
- Depends on: F-96, F-97.
- Waits on (to start): nothing — the source paper and answer key are with the owner (OneDrive `Simulab/Guarda Municipal/Manaus/2026`), free to use (D-6).
- Needed to validate: the local app host — owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: any E-4 feature.

## Question classification
Proposed at refinement (2026-10-25), for the owner to review. Answer key: booklet type 1 (white), final. Notice subject = the Manaus 2026 notice subject of F-78 the question falls under. `?` marks a row with no close topic in the F-51 taxonomy.

| # | Notice subject | Subject › Topic | Key | Note |
|---|---|---|---|---|
| 01 | Língua Portuguesa | Leitura e Interpretação › Interpretação de textos | B | base text "Calma, gente" |
| 02 | Língua Portuguesa | Leitura e Interpretação › Interpretação de textos | B | base text |
| 03 | Língua Portuguesa | Sintaxe › Sintaxe, Concordância e Regência | B | base text |
| 04 | Língua Portuguesa | Leitura e Interpretação › Interpretação de textos | D | base text |
| 05 | Língua Portuguesa | Sintaxe › Sintaxe, Concordância e Regência | D | base text |
| 06 | Língua Portuguesa | Leitura e Interpretação › Interpretação de textos | B | base text |
| 07 | Língua Portuguesa | Ortografia e Pontuação › Ortografia | A | base text; accent of "por quê" |
| 08 | Língua Portuguesa | Leitura e Interpretação › Interpretação de textos | D | base text |
| 09 | Língua Portuguesa | Produção Textual › Comunicação escrita | D | official writing |
| 10 | Língua Portuguesa | Produção Textual › Comunicação escrita | D | forms of address |
| 11 | Informática | Informática › Sistemas operacionais Windows | D | |
| 12 | Informática | Informática › Pacotes Microsoft Office | D | Word |
| 13 | Informática | Informática › Pacotes Microsoft Office | A | Excel |
| 14 | Informática | Informática › Conceitos de hardware ? | C | cloud computing |
| 15 | Informática | Informática › Segurança na internet | D | denial of service |
| 16 | Geografia e História de Manaus | Conhecimentos locais › Manaus (AM) | B | two cartograms (images) |
| 17 | Geografia e História de Manaus | Conhecimentos locais › Manaus (AM) | B | climogram (image) |
| 18 | Geografia e História de Manaus | Conhecimentos locais › Manaus (AM) | D | |
| 19 | Geografia e História de Manaus | Conhecimentos locais › Manaus (AM) | A | |
| 20 | Geografia e História de Manaus | Conhecimentos locais › Manaus (AM) | A | |
| 21 | Ética e Direitos Humanos | Direitos Humanos › Proteção de direitos fundamentais | A | use of force at a peaceful protest |
| 22 | Ética e Direitos Humanos | Direitos Humanos › Legislação de direitos humanos | D | international guidelines |
| 23 | Ética e Direitos Humanos | Direitos Humanos › Legislação de direitos humanos | B | racial discrimination |
| 24 | Ética e Direitos Humanos | Direitos Humanos › Legislação de direitos humanos | A | abandoned elderly person |
| 25 | Ética e Direitos Humanos | Direitos Humanos › Direitos humanos e cidadania ? | D | public-service ethics, offered advantage |
| 26 | Direito Constitucional | Direito Constitucional › Direitos fundamentais | C | nationality |
| 27 | Direito Constitucional | Direito Constitucional › Direitos fundamentais | C | ADPF, structural racism |
| 28 | Direito Constitucional | Direito Constitucional › Princípios constitucionais ? | C | state of defense |
| 29 | Direito Constitucional | Direito Constitucional › Princípios constitucionais ? | D | tenure and reinstatement of a civil servant |
| 30 | Direito Constitucional | Direito Constitucional › Constituição Federal (art. 144) | D | public security |
| 31 | Direito Penal | Direito Penal › Crimes específicos | C | |
| 32 | Direito Penal | Direito Penal › Crimes específicos | C | |
| 33 | Direito Penal | Direito Penal › Tipificação de crimes | B | crimes against honor |
| 34 | Direito Penal | Direito Penal › Crimes específicos | C | crimes against the public administration |
| 35 | Direito Penal | Direito Penal › Crimes específicos | D | theft of livestock |
| 36 | Direito Processual Penal | Direito Penal › Procedimentos processuais | D | |
| 37 | Direito Processual Penal | Direito Penal › Processo penal | C | in-the-act arrest |
| 38 | Direito Processual Penal | Direito Penal › Procedimentos processuais | D | crime scene |
| 39 | Direito Processual Penal | Direito Penal › Processo penal | D | unlawful evidence |
| 40 | Direito Processual Penal | Direito Penal › Processo penal | D | |
| 41 | Legislação de trânsito | Legislação Federal › Código de Trânsito | C | |
| 42 | Legislação de trânsito | Legislação Federal › Código de Trânsito | D | |
| 43 | Legislação de trânsito | Legislação Federal › Código de Trânsito | A | |
| 44 | Legislação de trânsito | Legislação Federal › Código de Trânsito | D | |
| 45 | Legislação de trânsito | Legislação Federal › Código de Trânsito | D | |
| 46 | Legislação específica | Estatuto do Desarmamento › Lei 10.826/2003 (Estatuto do Desarmamento) | C | |
| 47 | Legislação específica | Estatuto das Guardas Municipais › Lei 13.022/2014 (Estatuto das Guardas) ? | # | annulled; national public-security system |
| 48 | Legislação específica | Direito Penal › Crimes específicos | D | drug trafficking |
| 49 | Legislação específica | Legislação Municipal › Legislação municipal específica | C | |
| 50 | Legislação específica | Estatuto das Guardas Municipais › Lei 13.022/2014 (Estatuto das Guardas) | D | complaint against the guard |
| 51 | Legislação específica | Direito Penal › Crimes específicos | C | abuse of authority |
| 52 | Legislação específica | Legislação Federal › ECA (Lei 8.069/1990) | D | |
| 53 | Legislação específica | Direito Penal › Crimes específicos | C | torture law |
| 54 | Legislação específica | Direito Penal › Crimes específicos | D | |
| 55 | Legislação específica | Legislação Federal › Lei Maria da Penha (Lei 11.340/2006) | B | |
| 56 | Legislação específica | Direito Administrativo › Legislação administrativa | B | administrative misconduct |
| 57 | Legislação específica | Legislação Municipal › Organização municipal | C | |
| 58 | Legislação específica | Direito Administrativo › Legislação administrativa | B | access to information; true/false items inside multiple choice |
| 59 | Legislação específica | Direito Administrativo › Legislação administrativa | B | administrative procedure |
| 60 | Legislação específica | Legislação Municipal › Estatuto dos Funcionários Públicos Municipais | B | Lei Municipal 1.118/1971 |
