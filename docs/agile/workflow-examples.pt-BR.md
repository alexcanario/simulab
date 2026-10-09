# agile@canary — Exemplos de workflow (pt-BR)

> Parte do [manual](workflow.pt-BR.md). English: [en](workflow-examples.md).

## 14. Exemplos de workflow

Todos os exemplos usam um app fictício, o **ExamPrep**: uma plataforma em que curadores montam um banco de questões de concursos e alunos praticam com elas. `>` marca o que você digita; `Claude:` marca a resposta (resumida). Os diálogos estão em português, como na conversa real; os arquivos do projeto ficam em inglês.

### 14.1 Bootstrap de um app novo

**Antes:** um repositório vazio, com o `product/brief.md`:

```markdown
# ExamPrep
Problem: candidates study with scattered PDFs of past exams.
Users: curators (build the question bank), students (practice).
Capabilities: exam boards and exams, question bank (8 question types), practice tests, progress reports.
Constraints: Brazil and Portugal; web first; one developer.
Out of scope (v1): payments, mobile app.
```

```
> /agile:bootstrap
```

```
Claude: Li o product/brief.md. Rodada 1 de 7 — Forma.

1. Tipo de app → Recomendação: app web com API (o brief diz "web first").
2. Perfil de arquitetura → Recomendação: modular-monolith.
   Motivo: 4 áreas de negócio (catálogo, banco de questões, prática, relatórios) com
   fronteiras claras, mas um único desenvolvedor — um só deploy simplifica a operação.
3. Onde vai rodar → Recomendação: containers, local com .NET Aspire, Azure depois.

Responda cada uma ("ok" aceita a recomendação) ou diga o que mudar.
```

```
> 1 ok. 2 ok. 3 ok, mas sem decidir Azure ainda.
```

```
Claude: Registrado: onde vai rodar = containers + Aspire; provedor de nuvem adiado.

Rodada 2 de 7 — Dados.
4. Banco → Recomendação: PostgreSQL.
5. Multi-tenancy → Pergunta: escolas ou cursinhos terão bancos de questões privados?
   O brief não diz. Se sim, o isolamento por tenant precisa existir desde o primeiro dia.
...
```

A rodada 6 sempre traz uma pergunta que o brief não responde — onde ficam as pastas dos itens:

```
Claude: 28c. Raiz das worktrees → Recomendação: D:\wt\examprep (esta máquina tem drive D:;
    sem ele seria C:\wt\examprep). Fora do repositório; cada item ganha uma pasta como
    f-3-exam-board — o id e no máximo 20 caracteres do slug.
> ok
```

Depois da rodada 7, o Claude faz a pergunta de fechamento, resolve o que ela levantar, mostra um resumo de todas as decisões e pede confirmação. Então cria:

```
CLAUDE.md                                  52 linhas, aponta para o perfil
docs/decisions/ADR-0001-foundation.md      cada decisão, com o motivo
docs/agile/profile.md                      perfil modular-monolith
src/ExamPrep.AppHost, src/ExamPrep.Api, src/ExamPrep.Web, src/Modules/...
tests/...                                  unitários + integração (Postgres compartilhado)
Resources/*.pt-BR.resx, *.pt-PT.resx, *.en.resx
docs/manual/{pt-BR,pt-PT,en}/index.md
```

e os épicos do brief no board: *Catalog*, *Question bank*, *Practice tests*, *Progress reports*.

### 14.2 Uma feature da ideia ao fim

**Ideia.** Numa conversa sobre o catálogo:

```
> Os curadores precisam cadastrar bancas (nome e sigla). Registra isso.
```

```
> /agile:idea "Exam board registration: name and acronym, list and edit"
```

```
Claude: Épico para F-3 "Exam board registration"?
        1. Catalog (Recomendado) - o objetivo do épico é cadastrar o que o candidato pesquisa
        2. Content - conteúdo das provas
        3. Novo épico: "Exam boards" - cadastro e manutenção de bancas
        4. Sem épico
Você: 1
Claude: Criei a feature F-3 "Exam board registration" (board #12), no épico "Catalog".
Status: idea. Nada mais acontece até a refinarmos.
```

**Refinamento.** Quando chegar a vez desta feature:

```
> /agile:refine F-3
```

```
Claude: Pasta deste item: D:/wt/examprep/f-3-exam-board [feature/F-3] — criada agora,
fora do repositório; o arquivo foi movido para lá e tudo da F-3 é escrito ali.
Conferi o código: o módulo Catalog existe, mas ainda não tem entidades.
O modelo de usuários e papéis do bootstrap tem Curator e Admin.
```

Um item sobre evals é a exceção a "perguntar tudo de uma vez": o número de execuções por caso e o teto de custo só são propostos depois que o Claude roda um caso uma vez e cita quanto custou (exemplo 14.26).

As perguntas vêm como cartões de quiz, um por tema, com a opção recomendada primeiro. No app desktop você clica; sem a ferramenta de perguntas (um terminal, uma execução sem interface) cada pergunta chega como um bloco citado com o marcador `❓`, respondido com "ok" ou uma letra. O primeiro cartão, "Regras":

```
[Regras]
1. A sigla é única?
   ● Sim, sem diferenciar maiúsculas (Recomendado) — "FGV" e "fgv" são a mesma banca
   ○ Sim, diferenciando maiúsculas
   ○ Não
2. Uma banca pode ser excluída?
   ● Só desativada (Recomendado) — os concursos vão referenciá-la
   ○ Exclusão definitiva
3. Tamanho máximo?
   ● Nome 120, sigla 20 (Recomendado)
   ○ Outro (digite)
```

Depois "Permissões" e "Telas":

```
[Permissões]
1. Quem pode editar?
   ● Curator e Admin (Recomendado) — os curadores são donos do catálogo
   ○ Só Admin
[Telas]
1. A lista tem paginação ou busca?
   ● Busca por nome/sigla, 20 por página (Recomendado)
   ○ Lista simples, sem busca
```

```
> Regras: tudo recomendado. Permissões: só Admin. Telas: recomendado.
```

O Claude escreve `docs/features/F-3-exam-board-registration.md`:

```markdown
---
feature: F-3
status: refining
board: 12
---
# Exam board registration

## Goal
Let admins keep the list of exam boards used by exams and questions.

## Users and use cases
- UC1 Admin registers an exam board (name, acronym).
- UC2 Admin edits an exam board.
- UC3 Admin deactivates / reactivates an exam board.
- UC4 Admin and curators search the list.

## Business rules
- BR1 Acronym is unique, case-insensitive.
- BR2 Deactivated boards cannot be chosen for new exams but keep existing links.
- BR3 Name ≤ 120, acronym ≤ 20 characters.

## Screens and API
- /catalog/exam-boards (list, search, 20 per page)
- /catalog/exam-boards/new and /{id}/edit
- GET/POST/PUT /api/v1/catalog/exam-boards; POST .../{id}/deactivate|reactivate

## Acceptance criteria
- AC1 Given a new name and acronym, when saved, then the board appears in the list.
- AC2 Given an existing acronym in another case, when saved, then error `exam_board.acronym_taken`.
- AC3 Given a curator, when opening the new/edit screens, then access is denied.
- AC4 All texts appear in pt-BR, pt-PT and en.

## Decisions
- 2026-09-20 — Admin only edits (not curators) — owner decision.

## Out of scope
- Import from a file.

## Open questions
- (none)
```

```
Claude: A F-3 está pronta para aprovação. Leia o arquivo e responda "aprovo F-3" ou diga o que mudar.
> aprovo F-3
```

O status passa a `approved` (**Portão 1**).

**Build.**

```
> /agile:build F-3
```

```
Claude: Nenhuma outra feature em andamento. Branch feature/F-3 criada. Status: building.
...
Pronto:
- Entidade ExamBoard + configuração + migration AddExamBoards
- Endpoints com códigos de erro exam_board.acronym_taken / exam_board.not_found
- Telas List / Form, textos nos 3 arquivos de recursos
- 14 testes (Catalog.Tests 3,1 s, Web.Tests 1,8 s) — verdes

Critério → teste:
| AC1 lista       | ExamBoardsPage_Open_ShowsActiveBoards                        |
| AC3 única       | CreateExamBoard_DuplicateAcronym_Returns409                  |
| AC5 desativar   | ExamBoardsPage_Deactivate_ShowsInactive                      |
| AC7 idiomas     | roteiro de validação, passo 7                                |
A AC5 era uma lacuna: ExamBoard.Deactivate tinha um teste verde, mas nenhuma página
nem endpoint o chamava. Acrescentei o endpoint e o botão, depois o teste de página acima.

Roteiro de validação (F-3):
1. Suba o AppHost e entre como admin.
2. Menu Catalog → Exam boards: lista vazia.
3. Nova: "Fundação Getulio Vargas" / "FGV" → salva e aparece na lista.
4. Nova: "Outra" / "fgv" → mensagem "Sigla já em uso".
5. Edite a FGV e mude o nome → a lista mostra o nome novo.
6. Desative a FGV → status "Inativa"; reative.
7. Troque o idioma para pt-PT e en → os textos mudam.
8. Entre como curador → Nova/Editar não aparecem.
Status: validating.
```

Quando um passo precisa de terminal (um limite, um endpoint ainda sem tela), ele vem para os dois shells, já rodado pelo Claude:

```
4. Seis tentativas de cadastro seguidas do mesmo endereço: a 6ª é recusada.
   Git Bash:     for i in 1 2 3 4 5 6; do curl -sk -o /dev/null -w "%{http_code}\n" -X POST https://localhost:7101/api/v1/auth/sign-up -H "Content-Type: application/json" -d '{}'; done
   PowerShell 7: 1..6 | % { (Invoke-WebRequest -Method Post https://localhost:7101/api/v1/auth/sign-up -ContentType application/json -Body '{}' -SkipCertificateCheck -SkipHttpErrorCheck).StatusCode }
   Esperado: 400 cinco vezes, depois 429. Para repetir: espere um minuto.
```

**Validação** (**Portão 2**):

```
> O passo 4 mostra a mensagem, mas em inglês mesmo com pt-BR.
Claude: Faltava a chave no arquivo pt-BR. Corrigido, e acrescentei um teste para o AC4. Refaça o passo 4.
> ok, validado.
```

**Ship** (**Portão 3**):

```
> /agile:ship F-3
```

```
Claude: Suíte completa: 212 testes verdes (2 min 10 s). Testes de arquitetura verdes.
A execução reescreveu docs/api/ExamPrep.Api.json (uma rota nova): commitado com a F-3.
Merge de feature/F-3 em main feito (--no-ff), push feito; branch e worktree removidas. Board #12 fechado, Status Done (lido de volta). Manual atualizado: página "Exam boards" em pt-BR, pt-PT e en.
Retro: uma lição — "conferir todos os arquivos de idioma ao criar uma mensagem". Viro regra?
> sim
Claude: Regra acrescentada em core/i18n. Status: done.
```

Quando a escrita do Status não fixa — um erro de permissão, ou um board sem opção Done nesse campo — a linha de fechamento nomeia o problema e o comando exato, em vez de seguir em silêncio:

```
Claude: Mergeado. Board #12 fechado; Status ainda está "In Progress" depois de uma nova tentativa —
ajuste à mão: gh project item-edit --id PVTI_xxx --field-id PVTF_xxx --project-id PVT_xxx --single-select-option-id <done>.
Manual atualizado: página "Exam boards" em pt-BR, pt-PT e en.
```

### 14.3 Mudando de ideia durante o build

Com a F-7 "Question registration" em `building`:

```
> Quero que o campo "ano" seja opcional em questão sem concurso.
> /agile:change F-7
```

```
Claude: Nota de mudança acrescentada à F-7:
- O quê: Ano passa a ser opcional quando não há concurso vinculado.
- Por quê: questão autoral não tem ano de prova.
- Afeta: BR4 (ano obrigatório) e AC5. Os demais critérios não mudam.
Reaprova a BR4 e o AC5 como reescritos abaixo? (o resto do trabalho continua)
> aprovo
```

O build continua de onde estava. Nada é refeito.

### 14.4 Premissa errada descoberta no build

```
Claude: Parei a F-9 "Edit question exam". A feature pressupõe que a tela de edição já
permite trocar o assunto, mas ela não tem campos de assunto, banca nem ano, e a API de
atualização não os aceita. Opções:
A. Incluir a edição de assunto/banca/ano na F-9 (maior, mas funciona de ponta a ponta).
B. Deixar a F-9 só com o vínculo ao concurso e registrar uma feature nova para os campos.
Recomendação: A, porque a regra BR3 ("escolher um assunto coberto") é impossível sem isso.
> A
Claude: Nota de mudança acrescentada; escopo e critérios atualizados para sua aprovação.
```

### 14.5 Um bug depois do ship

```
> A lista de bancas não encontra "FGV" quando busco "fgv".
> /agile:idea "Bug: exam board search is case-sensitive"
Claude: Criei o bug B-2 (board #31), ligado à F-3.
> /agile:refine B-2
Claude: Causa confirmada na query (igualdade simples). Uma pergunta: a busca também deve
ignorar acentos ("Fundacao" encontra "Fundação")? Recomendação: sim.
> sim
```

O bug segue o mesmo ciclo, com um arquivo curto: causa, comportamento esperado, um teste que reproduz o problema e o roteiro de validação.

Quando a causa é uma regra de negócio, o refinamento também procura na solução inteira a mesma regra implementada de novo em outro lugar (comum em apps que cresceram sem uma camada de regras compartilhada), escreve todas as ocorrências em `## Cause` antes de perguntar qualquer coisa e pergunta quais corrigir agora e quais registrar como dívida:

```
> /agile:refine B-4
Claude: Causa confirmada: Enrollment/CpfValidator.cs:41 ignora o segundo dígito verificador.
A mesma conferência está implementada de novo em outros dois lugares (não são chamadas do validador):
  - Import/StudentCsvReader.cs:88 (cópia própria, mesmo defeito)
  - Web/Pages/Enroll.razor.cs:132 (cópia própria, correta)
As três estão em ## Cause, em docs/bugs/B-4-cpf-check-digit.md.
[cartão: Regras de negócio]
  Quais ocorrências o B-4 corrige?
  ● As três: o leitor e a página passam a chamar o CpfValidator (Recomendado)
    Só o validador e o leitor; a cópia da página fica como dívida registrada
    Só o validador
> (escolhe a primeira)
Claude: B-4 aprovado. O build acrescenta um teste de regressão por ocorrência corrigida.
```

### 14.6 Pausa e retomada

Você para quando quiser, com as suas palavras ou com o comando; os dois fazem a mesma coisa. No meio da F-7:

```
> /agile:pause
Claude: Pausado. Commit "wip(F-7): question form without type editor" em feature/F-7 (local, sem push).
Próximo passo: editor dos tipos T1-T3. Decisão pendente com você: nenhuma. Nada rodando.
```

"Vamos parar por hoje" ou "continuo amanhã" faz exatamente o mesmo. Se você só fechar a sessão, nada se perde: a sessão seguinte começa mostrando o trabalho sem commit, e o Claude o commita como `wip` antes de qualquer outra coisa, com o seu ok.

Na sessão seguinte:

```
> /agile:status
```

```
Claude:
Em andamento: F-7 Question registration — building (branch feature/F-7, 1 commit wip)
Próximo passo: editor dos tipos T1-T3
Perguntas em aberto: nenhuma
Topo do backlog: F-8 Question review queue (idea), B-3 Search accents (idea)
```

Para continuar, rode o build de novo no mesmo item; o Claude retoma em vez de recomeçar:

```
> /agile:build F-7
Claude: Retomando a F-7 a partir do commit wip "question form without type editor" em feature/F-7.
Próximo passo: editor dos tipos T1-T3 (passo 4 do plano). Continuando.
```

### 14.7 De uma ideia solta a um épico

```
> Estou pensando em simulados que se adaptam ao nível do aluno. Não sei bem como.
> /agile:discuss "adaptive practice tests"
```

```
Claude: O que já sabemos: o brief cita simulados; as questões ainda não têm dificuldade.
Opções:
A — Simulados fixos escolhidos pelo curador (P). Simples; sem adaptação.
B — Níveis de dificuldade nas questões + simulados que sorteiam por nível (M). Precisa dos níveis.
C — Motor adaptativo que ajusta durante a prova (G). Precisa de histórico e calibragem.
Recomendação: B agora, C depois — o B cria os dados de dificuldade que o C vai precisar.
Perguntas:
1. Quem define a dificuldade? → Recomendação: o curador, com 3 níveis.
```

```
> B, 3 níveis, o curador define. Adia o C.
Claude: D-1 registrada (decidida; C adiado). Registrei F-12 "Question difficulty" e
F-13 "Practice test by level". Elas pertencem a um épico novo, "Practice tests" — planejo agora?
> /agile:epic "Practice tests"
```

```
Claude:
| Id   | Feature                 | Prioridade | Tamanho | Depende de | Espera por                      | Design de tela |
| F-12 | Question difficulty     | Must       | P       | -          | -                               | não            |
| F-13 | Practice test by level  | Must       | M       | F-12       | -                               | sim            |
| F-14 | Practice test results   | Must       | M       | F-13       | regras de nota do curador       | sim            |
| F-15 | Retry wrong answers     | Should     | P       | F-14       | -                               | não            |
Plano de execução:
1. F-12 — primeiro: as outras precisam do dado de dificuldade — /agile:autopilot F-12 (pequena e clara)
2. F-13 — /agile:refine com /agile:screen, depois /agile:build — ao lado: nada
3. F-14 — /agile:refine quando o curador mandar as regras de nota — ao lado: F-15 num worktree
4. F-15 — /agile:refine → /agile:build
Esperando fora do épico: regras de nota para a F-14, do curador; quem pede é você.
Corte da primeira versão: F-12 a F-14.
1. A F-15 entra na primeira versão? → Recomendação: não.
> ok
Claude: docs/epics/practice-tests.md acordado. F-14 e F-15 registradas, cada uma com o seu Start.
Próximo: /agile:refine F-12 (não espera por nada).
```

Depois, o `/agile:status` mostra a espera até ela ser resolvida:

```
Bloqueados: F-14 — espera as regras de nota, do curador | F-15 — depende da F-14 (idea)
Topo do backlog: F-12 (done), F-13 (approved) — pode começar, F-14 (idea), F-15 (idea)
```

No arquivo, o tamanho é gravado como S / M / L, em inglês.

### 14.8 Design de tela e revisão

Durante o refinamento da F-13:

```
> /agile:screen F-13
Claude: Desenho delegado ao agente ux-designer, sozinho na worktree do item.
Ele escreveu a seção de tela e o mockup; eu li os dois arquivos antes de te mostrar:
7 estados, cada texto nos três idiomas, só padrões do kit,
cor nova com 4,8:1 no card e 5,1:1 na página.
Mockup enviado: docs/features/mockups/F-13-practice-test-by-level.html
Decisões: uma questão por página (bom no celular); cronômetro só quando o simulado tem limite.
Ponto aberto dele — 1. O aluno pode voltar a uma questão anterior? → Recomendação: sim, até concluir.
> sim
> aprovo F-13
```

O agente não fala com você. Ele desenha; o Claude lê os dois arquivos que ele escreveu, faz como suas as perguntas abertas dele e recolhe a sua aprovação. No build da mesma feature, um segundo agente, o `frontend`, implementa esse mockup aprovado e os testes daquela tela, sozinho na worktree; depois o Claude lê o que ele escreveu, roda o gate e cita os números reais. Domínio, API e migrações continuam com o Claude, e uma feature sem mockup aprovado é construída exatamente como antes, sem agente. Os dois modelos são sua escolha, na seção "Models" do `CLAUDE.md`.

No fim do build da F-14 (ela grava resultados e mexe nos dados dos alunos):

```
Claude: Esta mudança mexe em dados de alunos. Rodando /agile:review antes da validação.
| Gravidade  | Achado                                      | Veredito   | Ação                     |
| bloqueador | O endpoint de resultados não confere o dono | confirmado | corrigido + teste        |
| leve       | Mapeamento duplicado em ResultsPage         | confirmado | corrigido                |
| grave      | "A nota pode ficar negativa"                | rejeitado  | a BR2 permite penalidade |
Critério → teste: AC1 a AC6 cobertos; AC7 (troca de idioma) no roteiro de validação.
Status: validating. Roteiro de validação: ...
```

### 14.9 Dois itens em paralelo

A F-6 é um build longo numa sessão. Aparece um bug, e o dono quer corrigi-lo enquanto isso, em outra sessão:

```
> /agile:build B-2 --worktree
Claude: A F-6 está em build na worktree dela (feature/F-6). O B-2 mexe só em Identity/Login;
a F-6 mexe em Catalog. Sem schema nem tela em comum: podem andar em paralelo.
O B-2 já tem a worktree do refinamento: D:\wt\examprep\b-2-exam-board-search. Sigo nela?
> sim
Claude: B-2 em D:\wt\examprep\b-2-exam-board-search, status building (nessa branch).
Só um app host por vez: pare o do checkout principal antes de eu abrir a tela aqui.
```

No início de qualquer sessão, o status mostra os dois:

```
[agile] branch feature/F-6
In progress: F-6 Practice test by level — building (docs/features/F-6-...)
In progress in a worktree: B-2 — building [bug/B-2] D:/wt/examprep/b-2-exam-board-search
```

No ship do bug, a checagem completa roda na worktree e o merge no checkout principal:

```
> /agile:ship B-2
Claude: Desligando o build server do .NET, depois rodando a suíte completa na worktree.
Suíte completa verde (212 testes, 1 min 58 s). Merge de bug/B-2 em main feito. Sonda de lock em
D:\wt\examprep\b-2-exam-board-search passou. Worktree removida, branch apagada, 0 0 contra origin/main.
remote branch: never pushed, nothing to delete.
A F-6 ficou 1 commit atrás da main: vou atualizá-la antes da próxima rodada de testes.
```

A branch do item só existe na sua máquina, a menos que alguém tenha feito push dela, então normalmente não há nada a apagar no remoto. O Claude pergunta ao remoto antes com `git ls-remote` e só roda `git push` com `--delete` quando a branch está lá; antes da 0.0.71 o ship tentava sempre e terminava com `error: unable to delete 'bug/B-2': remote ref does not exist`.

### 14.10 Atualizando o plugin e o projeto

Uma lição para um plugin nasce na retro de um projeto. O `CLAUDE.md` do projeto lista os plugins que ele usa na linha `Plugins:`: o bootstrap escreve `Plugins: agile`, e a adoção de outro plugin acrescenta o nome curto dele (`Plugins: agile, lens`). Cada nota de plugin diz o plugin ao lado do escopo, e vira uma linha ⏳ em "Plugin notes — status" no `docs/agile/retro-log.md`, com uma coluna Plugin. Num projeto que usa os dois:

```
Claude: Lições do F-12:
1. Nota de plugin → plugin: lens [generic] — o mapa de pontos de entrada não viu uma chamada feita
   por uma classe base genérica de repositório (Orders/OrderService.cs:88).
2. Regra do projeto → project.md: "Um endpoint novo ganha o teste de autorização no mesmo commit."
Aprova?
> aprovo as duas
Claude: Retro log: linha ⏳ do lens acrescentada. A sessão do legacy-lens a recolhe; o agile não mexe nela.
```

Uma lição que cita uma causa mostra como a causa foi medida, ou diz que não foi. Um item capturado dela leva a mesma linha:

```
Claude: Lições do F-6:
1. Nota de plugin → plugin: agile [stack: dotnet] — o gate compilou com o preview 11.0, não com o SDK
   que a solução fixa. Evidência: `dotnet build adapters/dotnet/Lens.DotNet.slnx -getProperty:NETCoreSdkVersion`
   dá 11.0.100-rc.1 a partir da raiz e 10.0.401 a partir de adapters/dotnet.
2. Item novo → "Limpar os 911 avisos do baseline": causa não verificada (visto: 911 avisos aceitos desde
   a adoção). O Start dele diz "Cause not verified: measure it at /agile:refine".
Aprova?
```

Cada lição também vem com duas notas de 0 a 10 (Exemplo 14.66): `plugin`, quanto ela importa para o plugin e para todos os projetos que o usam, e `project`, quanto importa para este app. A escala é fixa (plugin: 0 um acaso único, 3-4 uma stack, 5-6 um perfil, 7-8 todos os perfis, 9-10 uma resposta errada silenciosa ou vista duas vezes; project: 0 nada muda, 5-6 voltaria a acontecer neste app em breve, 9-10 já causou um defeito), cada número tem um motivo de uma linha, e uma nota 7 ou mais cita a evidência; sem ela para em 6 e diz `no evidence yet`. As notas só ordenam e recomendam: o teto de 3 lições continua, quem decide é você, e uma lição com as duas notas em 2 ou menos aparece por último como `not recommended`, nunca some sem você ver. Uma nota de plugin leva as notas no próprio texto (`⏳ plugin: agile [generic] score 8/5 — ...`, plugin primeiro), então a tabela mantém as colunas e uma nota antiga simplesmente não tem nota.

Sem linha `Plugins:`, ou só com `agile`, toda nota é `plugin: agile` e nada é perguntado. Uma tabela antiga sem coluna Plugin ganha a coluna, com `agile` em todas as linhas, na próxima retro que escrever nela. O `/agile:status` conta as notas em espera por plugin ("Plugin notes waiting: 2 agile, 1 lens"). A sessão do plugin agile recolhe só as linhas `agile`, e nunca escreve no repositório do projeto (desde a 0.0.68). Ela comenta no issue que entregou e lista esse issue no `scripts/delivered.json` do plugin. A própria sessão do projeto marca a nota com ✅ no próximo `/agile:sync`. Na sessão do plugin:

```
> vamos ajustar o plugin com as recomendações em retro-log.md
Claude: 2 notas em aberto (F-8), agile-canary#31 e #32. Feito na 0.0.29: espera do bUnit (4 perfis),
pool do Npgsql (5 perfis), manuais en/pt-BR. Os dois issues fechados com comentário e listados como entregues.
1. Push do plugin?  2. Atualizar o plugin instalado?
> pode fazer o push e atualizar o plugin
```

Cada um é pedido separadamente. O projeto só recebe os arquivos novos, e as marcas ✅, pelo `/agile:sync`, entre features, rodado pela própria sessão dele.

O plugin passou de 0.0.10 para 0.0.13. Primeiro o plugin em si (terminal, depois uma sessão nova):

```powershell
claude plugin marketplace update canary
claude plugin update agile@canary
```

```bash
claude plugin marketplace update canary
claude plugin update agile@canary
```

Numa sessão nova, o `/agile:version` diz onde você está:

```
> /agile:version
Claude: agile@canary 0.0.13 nesta sessão. Cópias do projeto: 0.0.10.
Próximo passo: /agile:sync, entre features.
```

Depois, no projeto, entre features:

```
> /agile:sync
Claude: agile@canary 0.0.10 → 0.0.13.
| Arquivo                                | Estado | Proposta                                                   |
| .claude/rules/agile/git.md             | update | copiar: conferir a branch antes de cada commit, hook de guarda |
| docs/agile/workflow.md                 | update | copiar: manual 0.0.13                                      |
| docs/agile/profile.md                  | edited | mesclar: duas linhas novas de teste; a sua seção Simulab fica |
| .editorconfig                          | manual | uma linha difere: CA1716 = none. Acrescento?              |
| CLAUDE.md                              | manual | sem linha Worktrees:. Acrescento "Worktrees: D:\wt\examprep"? |
| CLAUDE.md                              | manual | sem linha Plugins:. Acrescento "Plugins: agile"?               |
Nada mais mudou. Seus project.md e ui-project.md não são tocados; as worktrees que já existem mantêm o nome.
> ok
Claude: Copiados 2, mesclado 1 (sem conflitos), .editorconfig editado. Build com 0 avisos; 252 testes verdes (9 s).
O plugin também traz o DocGen (documentação técnica gerada do código, desde a 0.0.21), que o bootstrap instala e este projeto não tem. Registro como feature?
Registrei 0.0.13 em .claude/agile/sync.json. Commit "chore: sync with agile@canary 0.0.13" na main. Autoriza?
```

Um projeto sem baseline de avisos, como um repositório legado que o legacy-lens acabou de adotar, recebe a oferta de uma, porque enquanto o arquivo não existe o gate não chama nenhum aviso de novo:

```
> /agile:sync
Claude: agile@canary 0.0.64 → 0.0.65.
| Arquivo                                | Estado  | Proposta                                                         |
| .claude/rules/agile/git.md             | update  | copiar: uma linha sobre as worktrees dos itens                   |
| baseline de avisos                     | missing | tirar agora (rebuild completo de src/Billing.sln, pode levar minutos) |
> ok
Claude: Copiado 1. Baseline: compilei src/Billing.sln (212 s), 1.847 entradas, agile gate GREEN.
A partir do próximo item, só os avisos que ele acrescentar são novos. Commit "chore: sync with agile@canary 0.0.65"
(com a baseline) na main. Autoriza?
```

Se esse build falhar, ou nenhuma solução for encontrada, o Claude cita a linha RED ou SKIPPED do gate, não grava baseline, termina o resto do sync e registra no retro log que a baseline continua faltando. Uma solução que só o MSBuild compila falha ali, porque o gate roda `dotnet build` (#20).

Uma nota de plugin ⏳ que cita um issue entregue pela versão nova recebe o ✅ no mesmo sync. Uma nota sem `agile-canary#N`, ou que cita um issue ainda aberto, continua ⏳:

```
> /agile:sync
Claude: agile@canary 0.0.66 → 0.0.68.
| Arquivo                                | Estado    | Proposta                                            |
| docs/agile/workflow.md                 | update    | copiar: manual 0.0.68                               |
| notas de plugin entregues              | 2 linhas  | marcar ✅: agile-canary#19 (0.0.66), linha e nota    |
> ok
Claude: Copiado 1. Notas: marquei a linha 10 (tabela) e a linha 47 (nota), agile@canary 0.0.66 (`6270ad6`).
Commit "chore: sync with agile@canary 0.0.68" (com o retro log) na main. Autoriza?
```

Um projeto que tem DocGen mas nunca o declarou como comando de docs recebe o bloco oferecido uma vez (0.0.72). O ship anterior a este sync já rodou o DocGen, como comando implícito, e avisou:

```
> /agile:sync
Claude: agile@canary 0.0.71 → 0.0.72.
| Arquivo / item                          | Estado    | Proposta                                          |
| docs/agile/workflow.md                  | update    | copiar: manual 0.0.72                             |
| comando de docs                         | ausente   | declarar o DocGen: dotnet run --project tools/Hutch.DocGen (check com -- --check, paths docs/architecture/) |
> ok
Claude: Copiei 1. Declarei o comando de docs: atualizei o .claude/agile/build.json
(docs.command = dotnet run --project tools/Hutch.DocGen). Do próximo ship em diante, o gate.js docs
roda como comando declarado e a linha "implicit DocGen" desaparece.
Commit "chore: sync with agile@canary 0.0.72" (com o build.json) na main. Autoriza?
```

Um projeto que já declara outro comando de docs recebe só uma linha — `docs already declared: lens-map --out docs/legacy/inventory` — e nada muda: juntar dois comandos é feito à mão. Duas pastas `tools/*.DocGen` também são nomeadas, porque o `gate.js docs` fica RED até você declarar uma.

A partir da 0.0.23 o template traz o `.editorconfig` definitivo. Num projeto que ainda tem o básico, a linha do `.editorconfig` lista os novos grupos de regras (chaves, pattern matching, membros com corpo de expressão, formatação) e o Claude propõe um grupo de cada vez; código que quebra uma regra aprovada aparece como aviso no build que vem depois do sync.

### 14.11 Quando um gate diz não

**Um app rodando trava o build.** O Claude abriu a tela para conferir e esqueceu o app host:

```
[agile gate] build blocked: ExamPrep.slnx
the build output is held by a running process (ExamPrep.Web (18344)): another build or test in this folder
(an IDE, a terminal, another session) or an app host, preview or debugger started from it. Stop it, then build again.
Claude: Meu app host ainda estava rodando. Encerrei; compilando de novo... verde.
```

**Um aviso novo no ship.** A lista e o veredito vêm por último, então sobrevivem a qualquer forma de ver a saída:

```
new warnings (fix them, or accept with `node gate.js baseline` after the owner agrees):
src/ExamPrep.Web/Pages/Results.razor.cs|RS0030 (0 -> 1)
  src/ExamPrep.Web/Pages/Results.razor.cs(42,19): RS0030 The symbol 'DateTime.Now' is banned in this project: ...
agile gate RED: 1 new warning(s): RS0030 in src/ExamPrep.Web/Pages/Results.razor.cs
Claude: Troquei DateTime.Now pelo TimeProvider injetado; ship de novo... agile gate GREEN.
```

**Um baseline tirado com outro SDK.** A solução fica em `src/`, com seu próprio `global.json`, e o baseline foi tirado antes de o gate compilar a partir dessa pasta:

```
sdk 10.0.401 (src)
baseline taken with SDK 11.0.100-rc.1.26425.128, this build used 10.0.401: warning counts may differ; retake it with gate.js baseline after the owner agrees
built src/ExamPrep.Web/ExamPrep.Web.csproj (6 s)
agile gate GREEN
Claude: O baseline veio do preview 11.0, que a raiz do repositório escolhia. Ele aceita avisos que este
SDK não levanta, e um aviso novo pode se esconder atrás deles. Tiro de novo agora (build completo, alguns minutos)?
```

**Um teste pendura.** Em vez de travar o turno por minutos:

```
[agile gate] a test hung (more than 120 s) or the test host crashed: tests/ExamPrep.Web.Tests
not finished: ExamPrep.Web.Tests.Ui.ConfirmDialogTests.Confirm_ReturnsTrue
Claude: O teste esperava o resultado de um diálogo dentro de InvokeAsync. Corrigido com o contexto de teste
compartilhado; 14 testes verdes (2 s).
```

**Um commit na branch errada.** A IDE trocou a branch para main no meio do build da F-6:

```
Claude: git commit -m "feat(F-6): add level filter"
[agile guard] This commit would land on `main`, but `feature/F-6` is not merged and is checked out nowhere.
The branch was probably switched outside this session (an IDE?). Run `git branch --show-current` ...
Claude: O checkout está na main; o lugar disto é a feature/F-6. Voltando para a feature/F-6 e fazendo
o commit lá. Nada entrou na main.
```

A mesma guarda recusa um commit de refinamento ou de build que colocaria o arquivo de um item na main:

```
Claude: git commit -m "docs(F-8): refine and approve"
[agile guard] This commit would land on `main` and carries `docs/features/F-8-question-review-queue.md` (approved).
An item that is not done is committed on its own branch ...
Claude: Criando a feature/F-8 a partir da main e fazendo o commit lá.
```

Quando um commit é mesmo da main com um item aberto (uma nota de retro, por exemplo), o Claude pergunta, e com o seu sim repete o comando terminando com `# agile:main-ok`. Um commit de retro carrega só regras, `CLAUDE.md`, templates e o retro-log; uma lição que precisa de código vira item, para que o código passe pelo gate na branch dele.

**O comando de docs mexeu num arquivo que não é dele.** Num repositório adotado cujo `.claude/agile/build.json` declara um mapa do código, o passo `gate.js docs` do ship o roda depois da verificação completa verde:

```
ran docs.command (48 s): lens-map --out docs/legacy/inventory
changed outside docs.paths (not committed):
  src/Billing/Billing.csproj
agile docs RED: 1 file(s) changed outside docs/legacy/inventory: src/Billing/Billing.csproj
Claude: O comando do mapa reescreveu um arquivo de projeto, que está fora de docs.paths. Não commitei
e o restaurei com git restore. Isso é da ferramenta que escreve o comando (legacy-lens), então anotei
como nota de plugin no retro log. Merge em espera: faço o ship sem o mapa desta vez, ou esperamos?
```

Uma execução verde lista os arquivos que mudou dentro de `docs.paths`, e eles entram no commit `docs(F-<n>)` junto com o manual:

```
ran docs.command (47 s): lens-map --out docs/legacy/inventory
ran docs.check (3 s): lens-map --check
to commit with the item:
  docs/legacy/inventory/entry-points.md
agile docs GREEN: 1 file(s) changed under docs/legacy/inventory
```

**O comando de docs que ninguém declarou** (0.0.72). Um projeto que ganhou o DocGen antes do bloco existir. O ship regenera o mapa do código de todo jeito, e diz quem declara:

```
ran docs.command (11 s): dotnet run --project tools/Hutch.DocGen
ran docs.check (4 s): dotnet run --project tools/Hutch.DocGen -- --check
to commit with the item:
  docs/architecture/modules.md
implicit DocGen docs command: declare it with /agile:sync (tools/Hutch.DocGen)
agile docs GREEN: 1 file(s) changed under docs/architecture/
Claude: O mapa do código foi regenerado e commitado com o manual. Este projeto roda o DocGen sem
declará-lo: rode /agile:sync entre features e eu escrevo o bloco no build.json uma vez.
```

Duas pastas `tools/*.DocGen` e nada declarado é RED, porque qualquer escolha deixaria a outra desatualizada sem avisar:

```
agile docs RED: 2 DocGen tools and no docs in .claude/agile/build.json: tools/Hutch.DocGen,
tools/Hutch.Legacy.DocGen — declare which one in .claude/agile/build.json
```

**Um repositório que só o MSBuild compila.** Um repositório adotado cuja solução tem uma aplicação web ASP.NET. Antes da 0.0.69 todo gate ali falhava no `dotnet build`, mesmo com `engine: msbuild` no `build.json`:

```
sdk 10.0.401 (.)
build failed: App.sln
src\Web\Web.csproj(19,3): error MSB4019: The imported project "...\sdk\11.0.100\Microsoft\VisualStudio\v18.0\WebApplications\Microsoft.WebApplication.targets" was not found.
agile gate RED: build failed (App.sln)
```

Da 0.0.69 em diante o mesmo repositório compila. Um turno que mexeu em um projeto:

```
agile gate (by hand): 1 code file(s) changed since the main branch
msbuild 18.10.1.42706 (.)
built src/Lib/Lib.csproj (0 s)
tests skipped: engine msbuild runs the whole suite at ship
agile gate GREEN
```

O ship restaura, recompila a solução e roda a suíte declarada:

```
msbuild 18.10.1.42706 (.)
restored (12 s): nuget restore App.sln
built App.sln (58 s)
tested (214 s): vstest.console.exe tests\**\bin\Debug\*.Tests.dll
  Total tests: 312. Passed: 312. Failed: 0.
warnings baseline: 41 entries -> .claude/agile/warnings-baseline.json
agile gate GREEN
```

Duas falhas que vale reconhecer. Um typo no engine, que antes passava calado como `dotnet`:

```
agile gate RED: unknown engine "MSBuild2" in .claude/agile/build.json (legal: dotnet, msbuild, none)
```

E uma máquina sem Visual Studio, que diz onde procurou em vez de culpar a solução:

```
MSBuild not found (engine msbuild in .claude/agile/build.json): tried no msbuildPath in .claude/agile/build.json, msbuild on the PATH, vswhere at C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe
agile gate RED: MSBuild not found
```

Claude: Esta máquina não tem Visual Studio, então o gate não consegue compilar este repositório. Instale o Build Tools, rode de um Developer Command Prompt, ou nomeie o MSBuild que você quer como `msbuildPath` no `build.json`. Qual deles?

**Uma escrita por heredoc bloqueada, e o bypass.** O Claude tenta corrigir o roadmap por um heredoc de shell em vez da ferramenta Edit:

```
Claude: cat > docs/ROADMAP.md <<'EOF'
...
EOF
[agile literal-guard] This command would write `docs/ROADMAP.md` through the command's own text.
rules/core/workflow.md: "Write or edit files (code, tests, docs; new or existing) with the Write and Edit
tools, never through a script's string literals": escapes turn into control characters or stray text, and a test can compile
and pass while matching nothing.
If this write really has to happen this way, ask the owner; with a yes, repeat the command ending with
the comment  # agile:literal-ok
Claude: Usando a ferramenta Edit em vez de heredoc para docs/ROADMAP.md.
```

Um `sed -i` num arquivo versionado é pego da mesma forma; quando o dono realmente quer a forma de shell (uma reescrita pontual em muitas linhas, feita por um script), o bypass repete o comando exato com o comentário:

```
Owner: sim, pode
Claude: sed -i 's/0\.0\.74/0.0.75/' docs/ROADMAP.md  # agile:literal-ok
```

### 14.12 A pergunta de fechamento do quiz

Depois da rodada 7, antes do resumo:

```
Claude: Pergunta de fechamento, sem recomendação: qual conceito do domínio mais preocupa você, ou o quiz não tocou?
> A taxonomia de assuntos. Cada banca dá um nome à matéria e algumas agrupam duas.
Claude: Fatos do código que você me deu: três níveis fixos, questões apontam para o mais baixo, sem apelidos.
Opções: A) manter três níveis; B) dois níveis canônicos + as disciplinas do edital mapeadas para eles + apelidos.
Recomendação: B — o edital mantém o vocabulário dele, e a analytics compara entre provas.
> B
Claude: Registrado na ADR-0001 em "After the quiz"; capacidade 2 do brief atualizada. Agora o resumo...
```

### 14.13 Um item numa tacada só

O `/agile:autopilot` leva um item da ideia até `done` com duas paradas (seção 5): a rodada de perguntas, em que as
suas respostas aprovam o item, e o roteiro de validação, em que você pode validar e autorizar o merge numa mensagem só.
Os exemplos abaixo usam a app ExamPrep do 14.1.

#### 14.13.1 Um item com tela, da ideia ao done

A F-17 acrescenta filtros à tela do banco de questões. Ela está em `idea` com o `## Start` vazio.

```
> /agile:autopilot F-17
Claude: F-17 (idea) — duas paradas: perguntas agora, roteiro de validação depois. Lendo o código do banco de questões.
O que existe: /questions lista 25 por página, sem filtros; Question tem Discipline, Difficulty, ExamBoard; a
permissão questions.view já protege a página. Nenhuma premissa falhou.

Critérios de aceite em rascunho (feitos com as opções recomendadas abaixo; as suas respostas podem mudá-los):
  AC1 Dado questões de três disciplinas, quando filtro por uma, então só as questões dela aparecem.
  AC2 Dado filtro por disciplina e por dificuldade, quando os dois estão marcados, então os dois valem (E).
  AC3 Dada uma lista filtrada, quando recarrego a página, então os filtros continuam (guardados na URL).
  AC4 Dado que nenhuma questão atende, quando filtro, então aparece o estado vazio "Nenhuma questão atende a estes filtros".
  AC5 Os textos existem em pt-BR, pt-PT e en.
Mockup: docs/features/mockups/F-17-question-bank-filters.html (estados: carregando, pronto, vazio, erro; 3 idiomas)

[cartão — Regras]    Vários filtros se combinam com: E (Recomendado) | OU
[cartão — Telas]     Os filtros ficam: numa barra acima da lista (Recomendado) | num painel lateral
                     Filtros mantidos ao recarregar: na URL (Recomendado) | não
[cartão — Pacotes]   Pacotes novos: nenhum necessário (Recomendado)
[cartão — Aprovação] Aprovar F-17 com estas respostas e critérios? Aprovo F-17 (Recomendado) | Mudar algo
```

Você abre o mockup, prefere um painel lateral e responde:

```
> Regras: E. Telas: painel lateral; URL sim. Pacotes: nenhum. Aprovo F-17.
Claude: A sua resposta de tela difere da recomendação (painel lateral), então o mockup mudou:
docs/features/mockups/F-17-question-bank-filters.html — o painel se fecha em telas estreitas. O resto está
registrado em ## Decisions e os critérios continuam. Esta parada extra é só da tela: o painel ficou certo?
> sim
Claude: F-17 aprovada (Autopilot: approved). Construindo na feature/F-17.
```

A execução segue sozinha: branch, código, testes afetados, gate, a tela aberta pelo app host nos dois temas. A
mudança não toca permissão, schema nem dado compartilhado, então não precisa de revisão. Depois vem a parada 2:

```
Claude: PARADA 2 — a F-17 está em validating (Autopilot: stop 2).
  8 arquivos; 12 testes verdes (2,9 s); gate GREEN, 0 warnings novos.
  Critério → teste: AC1 QuestionBankFilterTests.Filter_ByDiscipline_ListsOnlyIt; AC2 ...BothFilters_ApplyAnd;
  AC3 ...Reload_KeepsFiltersFromUrl; AC4 ...NoMatch_ShowsEmptyState; AC5 teste de chaves faltando + passo 6.
  Roteiro de validação: docs/features/F-17-question-bank-filters.md#validation-script (6 passos).
  Responda "validado e autorizo o merge de F-17" para entregar agora, ou "validado" para parar aqui.
> validado e autorizo o merge de F-17
Claude: Entregando a F-17 (Autopilot: shipping). Suíte completa: 214 testes verdes (41 s); manual atualizado em
pt-BR, pt-PT e en (question-bank.md); merge --no-ff em 3f9c2e1, push feito; main...origin/main 0 0; branch
removida. Board #31 fechado, Status Done (lido de volta). F-17 done. Retro: 1 lição proposta — mantém ou descarta?
```

O seu "validado e autorizo o merge" foi a autorização de merge do ship.

#### 14.13.2 Validado, merge depois

A mesma parada 2 da F-19, respondida só com "validado":

```
> validado
Claude: F-19 validada e mantida em validating; nada foi para a main. Quando quiser: /agile:ship F-19.
```

"ok", "pode seguir" ou "validado" nunca autorizam merge. Só uma mensagem que nomeia o merge autoriza, ou digitar `/agile:ship F-19` depois.

#### 14.13.3 Um defeito na parada 2

```
> passo 4 falhou: o filtro some quando troco de página
Claude: Reproduzido: o link de página perde a query string (QuestionList.razor:88). Corrigido; acrescentei
Paging_KeepsFilters (visto vermelho antes da correção). 13 testes verdes (3,0 s), gate GREEN.
PARADA 2 de novo — repita os passos 4 a 6 do roteiro.
```

#### 14.13.4 `--assume`, e o pacote que ele nunca assume

```
> /agile:autopilot F-18 --assume
Claude: F-18 (idea) — --assume: parada 1 pulada com as minhas recomendações, menos o que só você decide.
A F-18 exporta o banco de questões em PDF e precisa de um pacote novo. Essa pergunta é sua:
[cartão — Pacotes]  QuestPDF <última> (licença Community) (Recomendado) |
                    PdfSharpCore <última> (MIT) | sem PDF — só CSV
> QuestPDF
Claude: Registrado. 5 respostas assumidas (em ## Decisions como "assumed by autopilot"). F-18 aprovada. Construindo.
...
Claude: PARADA 2 — F-18 em validating. Assumi: A4 retrato; uma questão por bloco; respostas numa última página;
só admin e editor (permissão questions.export). Se alguma estiver errada, me diga: vira uma nota de mudança.
Mockup da página de exportação (você não o viu na parada 1): docs/features/mockups/F-18-question-export.html
```

#### 14.13.5 Uma mudança arriscada: a revisão roda sozinha

A F-20 deixa um editor arquivar questões (permissões e dados). Antes da parada 2 a execução chama o revisor de
contexto limpo sem que você peça, corrige o que ele confirma e mostra tudo:

```
Claude: PARADA 2 — F-20 em validating. Revisão (change-review): 1 grave confirmado e corrigido — o endpoint de
arquivar conferia questions.edit em vez de questions.archive (ArchiveQuestion.cs:21), teste acrescentado; 1 menor
aceito — a lista de arquivadas não tem limite de página (registrado em ## Decisions). Roteiro de validação: 7 passos.
```

#### 14.13.6 Uma parada reservada ao dono, e a retomada no dia seguinte

```
Claude: PARADA (dono) — a F-21 precisa de uma coluna que o arquivo não cita: questions.archived_at (mudança de schema).
Pronto: aprovada, 5 de 9 arquivos, testes verdes. Falta: a migration e 2 critérios.
Opções: A) acrescentar archived_at pelo /agile:change F-21 (Recomendado — a regra de arquivamento precisa da data);
B) manter um flag sem a data. Depois, /agile:autopilot F-21 continua.
> A
(a nota de mudança é registrada; você fecha a sessão)
```

No dia seguinte, numa sessão nova:

```
> /agile:autopilot F-21
Claude: F-21 retomada do arquivo (Autopilot: approved, build em andamento; nota de mudança 1 aplicada). Acrescentando a migration...
```

A execução nunca desfaz trabalho numa parada, e nunca faz merge, push ou marca `done` sem o merge nomeado por você.

### 14.14 Um quiz para um app que chama um modelo

O brief do ExamPrep ganha uma capacidade: "um tutor explica ao aluno uma resposta errada". A rodada 4 do quiz agora tem mais três perguntas, e a rodada 2 mais uma:

```
Claude: Rodada 2 — Dados.
9b. Dados pessoais e retenção → Recomendação: guardados, com prazo de retenção.
    O tutor vê as respostas do aluno. Respostas guardadas 2 anos; conversas com o tutor
    30 dias, apagadas por um job (uma flag de soft delete ainda guarda o texto). Podem
    sair da rede? Isso decide a pergunta 17b.

Rodada 4 — Integração.
17b. Provedor do LLM → Recomendação: IChatClient, provedor por configuração, hospedado.
     As respostas não levam nomes; se a 9b disser que não podem sair da rede, local.
17c. Teto de custo → Recomendação: limite por aluno + disjuntor global. Cada mensagem
     custa dinheiro e existe um plano grátis (pergunta 13): sem limite, nada o para.
17d. Modelo nos testes → Recomendação: um IChatClient fake roteirizado. Validação,
     permissões e limites ficam rápidos e determinísticos; o modelo real fica fora da suíte.
```

```
> 9b ok, podem sair. 17b ok. 17c ok, 50 mensagens por dia. 17d ok.
```

A rodada 7 ganha a pergunta 30b:

```
Claude: 30b. Evals → Recomendação: um nível de teste próprio. 40 respostas erradas com
    a explicação que um professor aceitaria, pontuadas contra o modelo real no ship e
    sob demanda, nunca no gate do turno. Taxa de acerto de base commitada; uma queda
    reprova o ship. Qual modelo roda os casos? → Recomendação: o que o tutor chama em
    produção. O comando o fixa (--model) e roda com --ablation none: o padrão rodaria
    cada caso duas vezes. O teto é definido depois, após uma execução medida.
> ok
```

A rodada 8 então faz a pergunta 38 — o prompt de sistema como arquivo versionado, revisado como código — e, se o tutor ganhar tools, a pergunta 37, do catálogo de tools.

Com o catálogo escolhido, o bootstrap copia o `ModelToolAttribute.cs` para o projeto que têm as tools. Uma tool passa a ser um método que declara o que o código sozinho não diz:

```csharp
[ModelTool(Permissions = ["EXAM_READ"], Reaches = ["Catalog.ExamService.GetAnswer"])]
[Description("Gets the official answer and rationale for a question.")]
public Task<Answer> GetOfficialAnswerAsync([Description("The question id.")] Guid questionId)
```

No ship seguinte, o `docs/architecture/tools.md` ganha uma linha por tool e uma seção com o schema de entrada dela; enquanto nenhum método tem o atributo, ele diz `0 tools.`, e uma app sem o atributo não ganha `tools.md`. O nome que o modelo vê é o do método, sem o `Async` final só quando o método devolve `Task`, `ValueTask` ou `IAsyncEnumerable<T>` (o `GetOfficialAnswerAsync` acima vira `GetOfficialAnswer`; um `GetExamAsync` que devolve `string` mantém o nome). Quando uma tool é adicionada sem dizer quem pode chamá-la, o ship para:

```
> /agile:ship F-30
Claude: Verificação completa GREEN (212 testes). Docs técnicos:

tool catalogue: 2 problem(s)
  ExamPrep.Ai.TutorTools.ExplainAsync: no Permissions. Name them, or say the tool is
  open with a value of its own (for example ["authenticated"]).
  ExamPrep.Ai.TutorTools.ResetProgressAsync: no Reaches. Name the systems and
  operations it reaches.

Duas tools estão sem declaração, então não regenerei o catálogo. A ExplainAsync lê uma
questão e a resposta do aluno: EXAM_READ, ou "authenticated" se qualquer aluno logado
pode usar? A ResetProgressAsync escreve — qual serviço ela alcança?
```

O dono responde, o código muda e o ship segue. Uma tool que escreve sem perguntar antes ao usuário é permitida, e aparece como `⚠ no` na tabela de resumo: a política é do app, a visibilidade é do catálogo.

### 14.15 As duas passagens antes do código

A F-21 cria um endpoint que outros módulos consomem e uma tabela nova, então o build roda as duas passagens somente leitura antes de planejar:

```
> /agile:build F-21
Claude: Este item cria contrato e tabela, então as duas passagens rodaram antes do plano.
system-design propôs: Catalog.Api recebe POST /api/v1/exams (códigos de erro exam.duplicate,
exam.invalid-board), a mensagem ExamPublished pertencente ao Catalog, e a tabela Exams com índice
único em (BoardId, Year). Risco: 312 linhas já duplicam esse par.
architect (bloqueador): o índice único sobre duplicatas existentes derruba a migração → limpar as
linhas na mesma migração, ou criar o índice depois do item de limpeza. Manter como proposto: o corte
e o contrato. Conferências que este projeto realmente tem: tests/Architecture, sem teste de layout.
Conferi as duas no código: as 312 linhas existem (contei), e a mensagem ainda não tem consumidor.
Aceitei o bloqueador e descartei o segundo índice, que nenhuma consulta usa — os dois em ## Decisions.
Plano (6 passos): 1. migração com limpeza, 2. entidade Exam, ...
```

Nada foi escrito pelos dois agentes: eles leram e propuseram, o Claude conferiu, decidiu e só então começou. Um item sem nenhum dos quatro gatilhos — um CRUD fino, uma mudança de texto — não passa por nenhuma das duas.

### 14.16 Uma feature reproduz a premissa antes do fix

O Summary da F-11 diz que a exportação de boletim quebra hoje em qualquer exame com mais de 2.000 alunos. O build segue a regra de reproduzir a fixture antes de escrever o fix, do mesmo jeito que um bug mostra o teste de regressão falhando primeiro:

```
> /agile:build F-11
Claude: Reproduzindo a premissa antes. Construí a ExamStub com 2.500 alunos e rodei a
exportação: System.OutOfMemoryException em TranscriptExporter.cs:41 (carrega toda linha
em memória antes de escrever). Confirmado — a fixture reproduz a falha declarada.
Aplicando o fix: streaming das linhas em vez de bufferizar tudo.
Claude: Mesma fixture, mesmo exame de 2.500 alunos, a exportação agora termina: 2.500
linhas escritas, 1,8s. O fix está provado contra a falha que ele diz resolver.
```

Uma feature sem premissa de "falha hoje" — uma capacidade nova, não uma alegação de que algo quebra — pula esse passo por completo: um teste por critério de aceitação, como antes.

### 14.17 Um board sem a opção "Ready" ainda

Um projeto bootstrapado antes de o board ter um estado distinto para Ready — o campo Status é só Backlog/In Progress/Done. A F-20 chega à aprovação e o primeiro espelho precisa de "Ready":

```
> /agile:refine F-20
...
Claude: aprovo F-20
Claude: F-20 aprovada. O campo Status do board não tinha a opção "Ready": criei logo
depois de "Backlog" (Backlog, Ready, In Progress, Done) e reapliquei o Status de todos
os outros itens pelo nome — nada mais no board se moveu. F-20 está agora em Ready.
Próximo: /agile:build F-20 (nada mais em building ou validating).
```

Um item seguinte no mesmo board só usa a opção que já está lá; o `/agile:build` e o `/agile:ship`, que nunca visam "Ready", continuam só reportando — nunca criando — qualquer opção que o espelho deles precise e não encontre.

### 14.18 Juntando um item aberto ao item em refinamento

Durante o refinamento da B-22, o dono propõe juntar o escopo da B-23 a ela, "já que mexem na mesma regra". O Claude confere a B-23 antes de juntar:

```
> Junta a B-23 nessa, é a mesma regra.
Claude: Conferindo a B-23 antes — git worktree list mostra D:/wt/examprep/b-23-ship-sets-status
[bug/B-23], e o arquivo dela começa com "Status: approved (owner, 2026-09-27; worktree ...)".
A B-23 já está approved na própria branch, não é só uma ideia: juntar ela aqui deixaria dois
itens corrigindo a mesma coisa. Junto a B-23 na B-22 mesmo assim, ou descarto a junção e
deixo a B-23 onde está?
> Descarta, entrega a B-23 sozinha.
Claude: A B-23 fica intocada e fora do escopo da B-22. Registrado em ## Decisions.
```

Uma proposta seguinte, de juntar a B-24 ("é só uma ideia, sem worktree ainda") na mesma rodada, não encontra nada além de `idea` em lugar nenhum, então a junção acontece sem pergunta:

```
> Junta a B-24 também, ainda é só ideia.
Claude: A B-24 não tem worktree e está idea em todo lugar — juntando ela na B-22 agora.
```

`epic-breakdown` e `discuss` não são afetadas: elas só capturam ideias novas, nunca absorvem o escopo de um item aberto já existente, então essa checagem roda só no `/agile:refine`.

### 14.19 Um app desktop no quiz

O brief descreve um app standalone para a recepção de uma clínica, Windows e macOS, vários usuários num PostgreSQL:

```
Claude: Pergunta 1, tipo de app. Recomendação: desktop (o brief diz "instalado nos PCs da recepção").
Pergunta 2, perfil. Recomendação: desktop.
> ok, ok
Claude: Pergunta 2a, tecnologia desktop. Windows e macOS, sem app mobile no brief.
Opções: A) Avalonia 12; B) WinUI 3 (só Windows: descartada pelo macOS); C) .NET MAUI.
Recomendação: A — uma base de código para os dois sistemas, sem controles de plataforma para conciliar.
> A
Claude: A pergunta 5 é PostgreSQL. Pergunta 2a2, como o cliente chega a ele.
Opções: A) por uma API; B) direto ao banco.
Recomendação: A — vários usuários dividem os dados, e B poria a credencial do banco em cada máquina.
> A
Claude: As perguntas 4 e 23 não são feitas em termos de MudBlazor aqui: a stack é XAML. Pergunta 23a, identidade visual: um arquivo,
um site, uma imagem ou três perguntas (veja 14.22). O kit de UI e a galeria deixaram de ser pergunta: estilos e
controles compartilhados com uma janela de galeria só de desenvolvimento, registrados como o primeiro item.
Pergunta 29: testes de view model, mais Avalonia.Headless para as telas; o gate compila Desktop.Core e a API, nunca a cabeça.
```

### 14.20 Pelo que um item espera para começar, e o que precisa para validar

O `## Start` de uma feature ou bug tem duas linhas: `Waits on (to start)` e `Needed to validate`. Só a primeira, junto com `Depends on`, pode impedir um começo. A F-21 precisa de uma exportação real da banca, mas só para ser validada:

```
## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: a real exam-board export (.csv) — from the curator.
```

```
> /agile:autopilot F-21
Claude: F-21 (idea) — dois stops: perguntas agora, roteiro de validação depois. Start: não depende de nada,
não espera por nada para começar. Precisa de uma exportação real para ser validada: peço no stop 2, não agora.
```

Antes, a linha única "Waits on" teria parado a execução aqui. No stop 2 o mesmo texto vem junto com o roteiro:

```
Claude: STOP 2 — F-21 está em validating (Autopilot: stop 2).
  Needed to validate: uma exportação real da banca (.csv), do curador — ainda não está no repositório.
  Roteiro de validação: docs/features/F-21-import-exam-board.md#validation-script (5 passos).
  Responda "validado e autorizo o merge de F-21" para entregar agora, ou "validado" para parar aqui.
```

Um item que espera por algo para começar (`Waits on (to start): a chave da API, do time de plataforma`) continua parando a execução, e o `/agile:status` continua listando como bloqueado. Um arquivo de item com a linha única antiga `Waits on:` é lido como "para começar", então nada já aberto muda sozinho.

### 14.21 Digitar ship é a autorização do merge

Você validou a F-35 e digita o comando. Não há pergunta "Autoriza?": o comando é a autorização, só para esse item.

```
> validado
> /agile:ship F-35
Claude: Suíte completa 231 testes verdes (2 min 4 s). A main andou (0.0.85, outra sessão): trazida para a branch,
suíte verde de novo, versão subida para 0.0.86. Merge de feature/F-35 em main feito (--no-ff), push feito; main...origin/main 0 0.
scripts/worktree.js remove D:\wt\agile-canary\f-35-ship-authorizes: build server desligado, sonda passou
(renomeada e de volta), worktree removida e podada (REMOVED). Branch apagada localmente (remoto: nunca enviada).
Board #35 fechado, Status Done (lido de volta). Retro: 1 lição proposta — qual você adota?
```

Quando algo ainda segura a pasta, a sonda para uma vez, antes de remover qualquer coisa:

```
Claude: Merge de feature/F-35 em main feito. Parei antes de remover a worktree: renomear
D:\wt\agile-canary\f-35-ship-authorizes falhou, um processo segura um arquivo nela (uma IDE ou um app host).
Feche e me avise; rodo o mesmo comando de novo. Nada foi removido (o exemplo 14.78 mostra o relatório).
> fechado
Claude: Sonda passou. Worktree removida, branch apagada. 0 0 contra origin/main.
```

### 14.22 A identidade visual do app, a partir de um site

Rodada 5 do bootstrap do app web de uma clínica. A identidade é perguntada antes da biblioteca do design system, e o kit de UI deixou de ser pergunta:

```
Claude: Pergunta 23a, identidade visual. Opções: A) um arquivo (DTCG *.tokens.json ou DESIGN.md); B) a URL de um site;
C) uma imagem (logo, prancha de marca); D) nenhuma, três perguntas derivam uma.
Recomendação: B — o brief cita o site da clínica. Então a 23 é só a escolha da biblioteca: MudBlazor.
> B https://clinica-exemplo.example
Claude: Li a página e as folhas de estilo. Nada foi escrito ainda. Encontrei:
| Papel | Claro | Escuro (derivado) | Texto sobre ele | Claro | Escuro |
| primary | #0B6E6E | #4FD1C5 | on-primary #FFFFFF | 6.1:1 | sobre #0B2A2A: 8.2:1 |
| secondary | #F2A93B | #F2B85F | texto #1A1A1A | 8.7:1 | texto #101010: 10.7:1 |
| background | #FAFAF7 | #121614 | texto #1A1A1A | 16.6:1 | texto #ECEFEC: 15.8:1 |
| error | #D92D20 | #F97066 | on-error #FFFFFF | 4.8:1 | sobre #1A0B09: 6.9:1 |
Fontes: Inter (títulos e corpo). Raio: 8 px. Família de ícones: Outlined. Origem: URL, 2026-09-28.
Dois pares não passam no AA: o link #3AA8A8 sobre o fundo claro dá 2.7:1 (o mais próximo que passa: #1F7F7F, 4.6:1) e
o secondary como texto sobre branco dá 2.0:1 (use só como preenchimento). Troco a cor do link para #1F7F7F?
> sim, confirmo
Claude: Escrevi docs/design/identity.tokens.json e docs/design/DESIGN.md (front matter gerado dos tokens, relidos e
comparados: 4 cores x 2 temas, fontes, raio e família de ícones iguais). Linha no ADR-0001: origem URL, 2026-09-28.
Primeiro item registrado: "Kit de UI e galeria, construídos a partir da identidade". Os dois arquivos entram no primeiro commit.
```

Um app criado antes desta versão recebe a mesma pergunta depois:

```
> /agile:sync
Claude: Falta também (só o /agile:identity adiciona): Identidade visual. O projeto tem telas e não tem
docs/design/identity.tokens.json. Não escrevo nada aqui.
> /agile:identity
Claude: Nenhuma identidade registrada. Pergunta 23a: um arquivo, um site, uma imagem ou três perguntas?
> nenhuma
Claude: Cor primária, fonte, claro, escuro ou os dois? ... (deriva a paleta, mostra a tabela, espera o "confirmo")
Claude: Escrevi docs/design/ e a linha do ADR-0001. O projeto já tem um kit de UI, então registrei a ideia
"Aplicar a identidade ao tema". Nenhum código do app foi alterado.
```

Com uma identidade já registrada, o `/agile:identity` mostra ela como tabela (origem, data, cores, fontes, raio, família de ícones) e pergunta o que muda.

### 14.23 As seções de um site no quiz

O brief descreve o site de um consultório de nutrição: quem atende, o que os pacientes dizem, os planos vendidos, uma newsletter e "talvez uma loja online depois":

```
Claude: Pergunta 2, perfil. Recomendação: website — um site público achado pelos buscadores, textos que você
mesmo edita, e ele pode virar app (área do paciente) sem trocar de perfil.
> ok
Claude: Pergunta 2c, as seções do site. Todas marcadas; responda "ok" ou os números a desmarcar.
Sempre no site (fora da lista): área de conteúdo, home, SEO básico, páginas 404 e de erro, política de
privacidade e consentimento de cookies.
Lançamento: 1 Sobre mim · 2 Depoimentos · 3 FAQ · 4 Cadastro de e-mail · 5 Produtos · 6 Contato e WhatsApp
Depois:     7 Serviços e preços · 8 Portfólio · 9 Blog com RSS · 10 Comentários moderados · 11 Galeria
            12 Eventos / agendamento · 13 Busca · 14 Localização · 15 Equipe · 16 Logos de parceiros
            17 Materiais para download · 18 Envio de newsletter · 19 Banner / pop-up · 20 Página de lançamento
            21 Link na bio · 22 Kit de imprensa · 23 Carreiras · 24 Declaração de acessibilidade
            25 Painel de visitas · 26 Site instalável (PWA)
> desmarca 4, 15, 23
Claude: O 18 (envio de newsletter) precisa do 4 (cadastro de e-mail): sem o 4 não há para quem enviar.
Mantém o 4, ou desmarca o 18 também?
> mantém o 4
Claude: Selecionadas: 24 seções (o 4 fica; 15 e 23 saem). A loja online do brief não é seção: é uma função de
app com pagamento, então registro como uma ideia de épico própria.
...
Claude: Pergunta 19, e-mail. Você escolheu "nenhum", mas as seções 4 e 6 precisam enviar (o link de
confirmação, a mensagem de contato). Recomendação: SMTP.
> SMTP
...
Claude: Board: épico "Website sections" com 29 ideias — as 5 da base (área de conteúdo primeiro), as
6 de lançamento, depois as 18 restantes — e a ideia de épico "Loja online". Crio?
```

### 14.24 Um app mobile para um site em funcionamento

O site da nutricionista (14.23) tem uma área do paciente, e ela quer que os pacientes a usem pelo celular:

```
> /agile:epic "Patient app"
Claude: O perfil é website, e um app mobile para ele segue o complemento mobile-client. Duas perguntas:
(a) Onde o app mora? Recomendação: neste repositório e nesta solução — o app reaproveita o código do site
    e sai com a mesma versão. (A outra resposta, um repositório novo, é o exemplo 14.30.)
(b) Telas? Recomendação: MAUI Blazor Hybrid — os componentes da área do paciente são reaproveitados no app.
    Alternativa: MAUI XAML, visual nativo, cada tela escrita de novo.
Quais funcionalidades do site vão para o app? Marque pelos números:
Itens done: 1 F-31 Meal plan · 2 F-34 Appointments · 3 F-37 Progress chart
Áreas:      4 Pages/Messages
(As páginas públicas ficam no site e não são oferecidas.)
> a: este repositório, b: Hybrid; 1, 2 e 4
Claude: O Mobile foundation vem primeiro (os projetos do app, login com token no site, a versão 1.8.0 que o site
tem hoje). Depois:
- Meal plan on mobile — o plano é lido numa página só: esta feature o extrai.
- Appointments on mobile — a regra de agendamento (dois no mesmo horário, não) está em duas páginas.
  Sugiro "API for appointments" antes. Pode ser?
- Messages on mobile — a página lê direto, sem regra: esta feature a extrai.
> pode
Claude: Épico "Patient app": Mobile foundation, API for appointments, Meal plan on mobile, Appointments on mobile,
Messages on mobile. A lista que você marcou vai para o arquivo do épico como você marcou. Crio?
```

Se ela tivesse respondido "um repositório novo" em (a), o épico seria só a metade do site: exemplo 14.30.

### 14.25 Uma lista que você aprovou termina no arquivo

Ao refinar "Export a report", o Claude propõe os formatos na rodada de perguntas e o dono edita a lista:

```
Claude: Formatos de exportação, nesta ordem:
1 PDF · 2 XLSX · 3 CSV · 4 JSON · 5 XML
> Tira o XML e põe DOCX no fim.
Claude: Pronto: PDF, XLSX, CSV, JSON, DOCX.
```

O Claude escreve o arquivo. Antes de pedir a aprovação, relê e acha uma linha, nos critérios de aceitação: "o relatório exporta nos formatos mostrados na refinação". Ele cola a lista e faz o critério apontar para ela:

```
## Approved list
Export formats
1. PDF
2. XLSX
3. CSV
4. JSON
5. DOCX

## Acceptance criteria
- AC2 Given a finished report, when the owner picks any format of `## Approved list`, then the file downloads in that format.
```

O Claude relê de novo, não acha nenhuma linha que mande o leitor para o chat, e só então mostra o resumo e pede o "aprovo F-n". O build, aberto dias depois em outra sessão, lê os cinco formatos do arquivo.

### 14.26 Evals: o modelo é fixado, o teto é medido, o veredito é código

Bootstrap do tutor de provas, pergunta 30b. O Claude pergunta também o modelo e diz o que o comando vai carregar:

```
Claude: 30b. Qual modelo roda os casos? → Recomendação: o que o tutor chama em
    produção. A linha Evals do CLAUDE.md ganha --model, --ablation none (o padrão
    rodaria cada caso duas vezes, com e sem o plugin), --max-cost-usd e --json.
    A base registra o modelo e a ablação.
> ok
```

Depois, ao refinar "Pontuar as explicações do tutor", o Claude ainda não fixa o número de execuções. Roda um caso uma vez e cita:

```
Claude: claude plugin eval . --case wrong-answer-1 --runs 1 --ablation none --model <modelo de produção>
    → custo US$ 0.21. Seis casos × 3 execuções = US$ 3.78; com 30% de margem, teto US$ 5.
    Registrado em ## Decisions. Execuções por caso: 3? Teto: US$ 5?
> ok
```

No ship, o Claude roda a suíte com `--json`, depois o comparador, e cita a saída inteira:

```powershell
node evals/compare.js result.json
```

```bash
node evals/compare.js result.json
```

```
ok       wrong-answer-1: 1 (baseline 1)
DROP     wrong-answer-2: 0.67 < baseline 1
cost US$ 3.61, model production-model, ablation none
evals RED            (exit 1)
```

O ship para. Se a execução tivesse usado outro modelo que o da base, a última linha seria `evals NOT MEASURED` (exit 2): nem queda, nem aprovação, e regravar a base (`--write`) precisa do seu sim.

### 14.27 Um repositório VB.NET: o gate de turno compila e testa uma mudança em `.vb`

Um repositório adotado escrito em VB.NET (`engine: dotnet`, projetos SDK-style): `src/VbLib`, `tests/VbLib.Tests` (MSTest com `Microsoft.NET.Test.Sdk`), `tests/Sdk.Tests` (um projeto VB em `MSTest.Sdk/3.6.4`), um `tests/CsApp.Tests` em C# que referencia `VbLib.vbproj`, e `src/FsLib` em F#. O Claude edita `Class1.vb`. Até a 0.0.90 o turno terminava em silêncio, e o `gate.js stop` à mão dizia:

```
agile gate SKIPPED: no code file changed since the main branch
```

Agora a mesma edição dá:

```
agile gate (by hand): 1 code file(s) changed since the main branch
sdk 8.0.131 (.)
built tests/CsApp.Tests/CsApp.Tests.csproj (4 s)
built tests/Sdk.Tests/Sdk.Tests.vbproj (3 s)
built tests/VbLib.Tests/VbLib.Tests.vbproj (2 s)
tested tests/CsApp.Tests/CsApp.Tests.csproj (2 s)
  Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: < 1 ms - CsApp.Tests.dll (net8.0)
tested tests/Sdk.Tests/Sdk.Tests.vbproj (1 s)
  Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 156ms
tested tests/VbLib.Tests/VbLib.Tests.vbproj (2 s)
  Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 36 ms - VbLib.Tests.dll (net8.0)
agile gate GREEN
```

O projeto de teste C# roda porque o grafo agora tem o `.vbproj` como nó; o `Sdk.Tests` roda porque `Sdk="MSTest.Sdk` conta como projeto de teste (um projeto C# em `MSTest.Sdk` é achado do mesmo jeito). Uma segunda variável local sem uso em `Class1.vb` é um aviso VB novo, RED a cada turno até ser corrigido:

```
new warnings (fix them, or accept with `node gate.js baseline` after the owner agrees):
src/VbLib/Class1.vb|BC42024 (1 -> 2)
  src/VbLib/Class1.vb(3,13): BC42024 Unused local variable: 'unused'.
  src/VbLib/Class1.vb(4,13): BC42024 Unused local variable: 'second'.
agile gate RED: 1 new warning(s): BC42024 in src/VbLib/Class1.vb
```

Uma asserção que falha em `Sdk.Tests` termina com `agile gate RED: tests failed (tests/Sdk.Tests/Sdk.Tests.vbproj)`. Uma mudança em `Library.fs` compila `src/FsLib/FsLib.fsproj` do mesmo jeito. Com `engine: msbuild` (um `.vbproj` antigo) o projeto afetado é recompilado com o MSBuild e o relatório diz `tests skipped: engine msbuild runs the whole suite at ship`, como faz para C#.

### 14.28 Uma app que é só uma API: a pergunta 10a

Bootstrap do "Rota", cujo brief diz que um app mobile e o sistema de um parceiro vão chamá-la e que ela não tem telas. Rodada 1:

```
Claude: 1. Tipo de app → Recomendação: só API. O brief não tem tela própria.
    2. Perfil de arquitetura → Recomendação: web-api (uma API para os seus clientes,
       sem telas). O monolith traria um projeto Blazor e o bUnit, que você não precisa.
    Já claro no brief: sem back office (14), sem telas (21, 22, 23a–23e).
> ok
```

Rodada 3, no lugar da pergunta 10:

```
Claude: 10a. Como os consumidores da API se autenticam? (pode marcar várias)
    · Identity bearer: os usuários entram na própria API
    · IdP externo: usuários e client credentials para clientes máquina
    · API key: só clientes máquina, nunca um navegador ou app mobile
    Recomendação: Identity bearer e API key. O app mobile faz o login dos usuários, e o
    sistema do parceiro é um cliente máquina que você controla; uma API key nunca vai
    para um app mobile, porque o código dele é público.
    11. Login social ou MFA na v1? → Recomendação: não (perguntada porque Identity bearer está ligado).
> ok
```

O esqueleto então tem o `Rota.Api` com login, refresh e `me`, o handler de API key e o seletor entre os dois, o `Rota.Tests` com os testes HTTP da fundação e sem o teste do template, o `docs/api/openapi.json` escrito pela primeira execução dos testes, e no `docs/infra.md` os prazos dos tokens (1 h, 14 dias) e onde fica o key ring. O primeiro endpoint que uma feature acrescenta aceita só usuários; o endpoint do parceiro nomeia a política `Clients`.

### 14.29 Publicando um release

O ExamPrep é um `web-app`; dois itens foram entregues desde o último release (`v0.1.0`), e a `main` tem `<Version>0.2.0</Version>` no `Directory.Build.props`. Você está no checkout principal.

```
> /agile:publish
Claude: Plano: v0.2.0, tag anterior v0.1.0, 2 itens desde ela.
  Feature F-3: question bank import
  Bug B-2: wrong total on the report
  Pacote: ExamPrep.Web (artifacts/publish/v0.2.0/ExamPrep.Web + ExamPrep.Web.zip).
  Seguindo: digitar o comando é a autorização para o commit das notas, a tag, o push e o GitHub Release.
Claude: ExamPrep.Web empacotado: 156 arquivos, zip de 4,7 MB; docs/releases/v0.2.0.md escrito.
Claude: Release v0.2.0: notas commitadas na main (docs(release): v0.2.0), tag anotada v0.2.0, main e tag enviadas, GitHub Release criado com as notas.
  Não empacotado: nada mais. Nenhum ambiente foi nomeado, então nada foi implantado (exemplo 14.35).
```

O mesmo comando um minuto depois para e não escreve nada, porque a tag agora existe:

```
> /agile:publish
Claude: Nada a publicar: a tag v0.2.0 já existe. O próximo release vem depois do próximo /agile:ship.
```

Um projeto com o complemento `mobile-client` e sem a assinatura configurada diz o que deixou de fora: `Não empacotado: ExamPrep.Mobile (a assinatura não está configurada: ANDROID_SIGNING_KEYSTORE, ... sem valor); só o ExamPrep.Web foi.` (exemplo 14.32). Um projeto desktop com `<RuntimeIdentifiers>win-x64;linux-x64</RuntimeIdentifiers>` ganha uma pasta e um zip por runtime, e o zip do linux vem com `feito no Windows: depois de descompactar, rode chmod +x no executável`. Se o `dotnet publish` falhar por erro de compilação, a execução termina com `nothing was committed or tagged` e a linha do erro, e a correção passa por um item. Se o push falhar depois da tag, o relatório diz `git push origin main` e `git push origin v0.2.0` para repetir; a tag fica.

### 14.30 Um app mobile em repositório próprio, sobre a API do site

A nutricionista (14.24) prefere o app num repositório só dele, com versão e release à parte do site. Primeiro, no repositório **do site**:

```
> /agile:epic "Patient app"
Claude: O perfil é website, e um app mobile para ele segue o complemento mobile-client.
(a) Onde o app mora? Recomendação: neste repositório (reaproveita o código e a versão do site).
    Ou um repositório novo, com versão própria.
Quais funcionalidades do site vão para o app? Marque pelos números:
Itens done: 1 F-31 Meal plan · 2 F-34 Appointments · 3 F-37 Progress chart
Áreas:      4 Pages/Messages
> a: um repositório novo; 1 e 2
Claude: Repositório novo significa telas MAUI XAML, então pulo essa pergunta. O épico do site é "API for the mobile app":
- Mobile API foundation — login do app, o portão de atualização obrigatória (versão mínima, resposta 426), o documento OpenAPI
- API for meal plan
- API for appointments
Meal plan e Appointments ficam em "Features taken to the app" como você marcou. Crio?
> ok
Claude: Próximo passo: crie um repositório novo cujo product/brief.md cite este (nutri-site) e rode /agile:bootstrap lá.
As ideias "<X> on mobile" do app são escritas lá, não aqui.
```

Depois, no repositório **do app**, com um brief que cita o site:

```
> /agile:bootstrap
Claude: Rodada 1. Pergunta 1: mobile sobre uma API existente. Pergunta 2: mobile. Pergunta 2d, onde mora o backend?
    · nesta solução · uma API existente em outro repositório (um site ou web-api seu, ou qualquer API com documento OpenAPI)
    Recomendação: uma API existente em outro repositório — o brief cita o nutri-site.
    Já está claro: 3 (hospedagem), a rodada de dados, 12, 13 e 14 — a API decide. A pergunta 10 é "o login da API": li no site, Identity bearer.
> ok
...
Claude: Quadro. O nutri-site tem o épico "API for the mobile app" com duas funcionalidades marcadas. Ideias propostas, nada escrito lá:
  1. Mobile foundation — login no login/refresh/me, o handler com X-App-Version, o documento fixado, o teste de contrato (espera o nutri-site#41 "Mobile API foundation")
  2. Meal plan on mobile (espera o nutri-site#42)
  3. Appointments on mobile (espera o nutri-site#43)
```

O `/agile:status` no app mostra as três como bloqueadas enquanto essas issues estiverem abertas no site, e como livres quando fecharem (a foundation também só quando o `docs/api/openapi.json` do site declara `X-App-Version`). Se a API fosse um `web-api` do agile ou de outra pessoa, as ideias saem do brief; quando essa API ainda não tem o portão, o Claude se oferece para abrir uma issue "Forced-update gate for <App>" no quadro dela, com o seu sim, e o "Mobile foundation" espera por ela. Depois do primeiro ship no repositório do app, só o `<App>.Mobile.csproj` vai de `0.1.0` para `0.2.0`.

### 14.31 Notificações push para um app mobile

O ExamPrep é um `website` com o complemento `mobile-client`. Você pede push numa feature, "Avisar quando um resultado ficar pronto", e o Claude refina contra o `docs/agile/profile-mobile-client.md`.

```
> /agile:refine F-9
Claude: O perfil descreve push (seção "Push notifications"). Duas decisões ficam com você:
  1. Provedor. Recomendação: Firebase Cloud Messaging com FirebaseAdmin 3.7.0 (Apache-2.0)
     para as duas plataformas; a cabeça do cliente soma Plugin.Firebase.CloudMessaging 4.0.1 (MIT).
  2. Cadastro de dispositivos. Recomendação: PUT/DELETE /api/v1/devices/{installationId} na classe
     Features/Devices do site, para a API saber a quem consegue chegar.
Você: ok
Claude: Os critérios cobrem 204 ao cadastrar, 401 anônimo, 400 devices.invalid-token, o dispositivo
  passando para o usuário que o cadastra, uma mensagem por dispositivo na cultura dele e o token
  não registrado apagado. Os testes usam um remetente falso; a entrega real é o passo 6 do roteiro
  de validação, que pede de você a chave de um projeto Firebase e um Android.
```

O gate continua verde sem aparelho e sem conta Firebase. Na validação o Claude entrega o roteiro: entrar no celular, aceitar o pedido de permissão, disparar o resultado, ver a notificação no idioma do celular, sair e disparar de novo para ver que nada chega.

### 14.32 Publicando um app mobile nas lojas

O ExamPrep é um `website` com o complemento `mobile-client`, na `0.2.0`, e esta é a primeira publicação nas lojas. As quatro variáveis de assinatura ainda não existem.

```
> /agile:publish
Claude: Plano: v0.2.0, tag anterior v0.1.0, 1 item desde ela.
  Pacote: ExamPrep.Web.
  Fica de fora: ExamPrep.Mobile: não publicado no Play Store, a assinatura não está configurada (ANDROID_SIGNING_KEYSTORE,
  ANDROID_SIGNING_ALIAS, ANDROID_SIGNING_STORE_PASS, ANDROID_SIGNING_KEY_PASS sem valor).
  Quer criar o keystore de upload agora? Diga onde ele deve ficar (um caminho absoluto fora do repositório).
Você: D:\keys\examprep-upload.keystore
Claude: Rode isto no seu terminal (ele pede as senhas lá, e eu nunca as vejo):
```

```powershell
keytool -genkeypair -keystore D:\keys\examprep-upload.keystore -alias upload -keyalg RSA -keysize 2048 -validity 10000
```

```bash
keytool -genkeypair -keystore /d/keys/examprep-upload.keystore -alias upload -keyalg RSA -keysize 2048 -validity 10000
```

```
Claude: Depois defina ANDROID_SIGNING_KEYSTORE (esse caminho), ANDROID_SIGNING_ALIAS (upload), ANDROID_SIGNING_STORE_PASS e
  ANDROID_SIGNING_KEY_PASS, nomeie as quatro no docs/infra.md e guarde uma cópia do keystore fora desta máquina:
  é a chave de upload, e o Play só a redefine pelo suporte.
```

Depois, com as variáveis definidas e o `ApplicationId` trocado do `com.companyname.exampreps` do template para `br.com.exampreps`:

```
> /agile:publish
Claude: Plano: v0.2.0 ... Pacote: ExamPrep.Web e ExamPrep.Mobile (android, aab).
Claude: Empacotados ExamPrep.Web (zip de 4,7 MB) e ExamPrep.Mobile: br.com.exampreps-Signed.aab, 31 MB, jarsigner: jar verified.
  docs/releases/v0.2.0.md e docs/releases/v0.2.0-store.md escritos.
Claude: Release v0.2.0: os dois arquivos commitados na main (docs(release): v0.2.0), tag, push, GitHub Release.
  Envie o .aab à mão: o plugin não envia nada. O docs/releases/v0.2.0-store.md lista os passos do Play Console
  (faixa de teste primeiro, depois promover), os da primeira publicação e, para um Mac, os do iOS.
```

Com o `ApplicationId` ainda `com.companyname.exampreps`, o mesmo comando deixa o head Mobile de fora (`o id é permanente depois do upload: troque antes`) e publica o site.

### 14.33 Um app Hybrid em repositório próprio, compartilhando os componentes do site por um pacote

O app do paciente da nutricionista (14.30) agora quer as telas do próprio site dentro do app. Primeiro, no repositório do **site**, a rodada do épico:

```
> /agile:epic "App do paciente"
Claude: ... (a) Onde o app mora? Recomendação: neste repositório. Ou num repositório novo, com versão própria.
(b) Telas? Recomendação: MAUI Blazor Hybrid — os componentes do site são reaproveitados no app; num repositório novo eles
    viajam como o pacote NutriSite.Shared. Ou MAUI XAML (visual nativo).
> a: um repositório novo, b: Hybrid; 1 e 2
Claude: O épico do site é "API for the mobile app":
- Mobile API foundation — login do app, o portão de atualização obrigatória, o documento OpenAPI, e NutriSite.Contracts e
  NutriSite.Shared como projetos empacotáveis (a linha Complement vai nomear o pacote; o docs/infra.md nomeia GITHUB_PACKAGES_TOKEN)
- API and shared screen for meal plan
- API and shared screen for appointments
```

Quando o site é publicado:

```
> /agile:publish
Claude: Plano: v0.4.0, tag anterior v0.3.0, 3 itens desde ela.
  Pacote: NutriSite.Web; NutriSite.Contracts (nuget 0.4.0); NutriSite.Shared (nuget 0.4.0).
Claude: Empacotei o NutriSite.Web (zip de 6,1 MB); packages/NutriSite.Contracts.0.4.0.nupkg e packages/NutriSite.Shared.0.4.0.nupkg
  (o Shared leva staticwebassets/ e depende do NutriSite.Contracts 0.4.0).
Claude: Release v0.4.0: notas commitadas, tag, push, GitHub Release; os dois pacotes enviados para
  https://nuget.pkg.github.com/<dono>/index.json com --skip-duplicate.
```

Com `GITHUB_PACKAGES_TOKEN` sem valor, o mesmo comando diz `Fica de fora: NutriSite.Contracts, NutriSite.Shared: não publicados como pacotes NuGet, GITHUB_PACKAGES_TOKEN não está definido` e publica o site. Se o envio dos pacotes falhar depois da tag, o relatório diz o comando `dotnet nuget push` para repetir, com `--skip-duplicate` e sem o token.

Depois, no repositório do **app**, com um brief que cita o site:

```
> /agile:bootstrap
Claude: Rodada 1 ... Pergunta 2d, onde mora o backend? Recomendação: uma API existente — o brief cita o nutri-site.
    Já está claro: as telas são MAUI Blazor Hybrid — o CLAUDE.md do nutri-site diz que as telas compartilhadas estão no pacote NutriSite.Shared.
> ok
...
Claude: Esqueleto: NutriApp.Mobile.Core (referencia o pacote NutriSite.Shared), NutriApp.Mobile (head com BlazorWebView),
  um nuget.config com o nuget.org e o feed do site (source mapping: NutriSite.* no feed, * no nuget.org; o token como
  %GITHUB_PACKAGES_TOKEN%), Directory.Packages.props fixando o NutriSite.Shared 0.4.0, docs/api/backend.md com o release v0.4.0.
  Sem projeto NutriApp.Contracts: os records vêm no pacote. O CLAUDE.md ganha "- Shared screens: package NutriSite.Shared ...".
```

Mais tarde, o `/agile:sync` no app, depois que o site lançou a `v0.5.0`:

```
Claude: Arquivos do plugin: todos iguais. O site tem o release v0.5.0, adiante do NutriSite.Shared 0.4.0 fixado; passar o pin e
  renovar o docs/api/backend-openapi.json a partir desse release é um item deste app. Nada foi escrito.
```

### 14.34 Um site novo que nasce com o app mobile

O brief de uma fisioterapeuta pede um site onde os pacientes veem os planos de exercício e marcam sessões, e "um app para os pacientes, desde o começo":

```
> /agile:bootstrap
Claude: Rodada 1.
2. Perfil de arquitetura — Recomendação: web-app. Motivo: telas sobre dados simples (planos, sessões), uma área de negócio,
   e o app mostra parte do site. mobile sobre monolith caberia se uma API fosse a principal consumidora; não é.
2e. Um app mobile desde o início? não · sim, MAUI Blazor Hybrid · sim, MAUI XAML
   Recomendação: sim, Hybrid. Motivo: o brief cita um app para os pacientes; com Hybrid as telas do site já nascem no
   PhysioSite.Shared e o app as reaproveita. Ele mora neste repositório e nesta solução.
   Quais funcionalidades do brief vão para o app? Todas marcadas; responda "ok" ou os números para desmarcar.
   Área do paciente: 1 Plano de exercícios · 2 Marcação de sessões · 3 Mensagens
   Administração:    4 Prontuários · 5 Configuração da agenda
> ok, desmarca 4 e 5
Claude: Vão para o app: 1 Plano de exercícios, 2 Marcação de sessões, 3 Mensagens.
...
Claude: Proposta para o quadro:
- UI kit and gallery, built from the identity — o kit no PhysioSite.Shared, a página da galeria no PhysioSite.Web
- Épico "Patient area": Exercise plan, Session booking, Messages — cada tela um componente no Shared hospedado por uma
  página no Web, a regra ou consulta em Features/ atrás de uma interface no Shared
- Épico "Back office": Patient records, Schedule settings
- Épico "Mobile app" (docs/epics/mobile-app.md, as três funcionalidades em "Features taken to the app"):
  Mobile foundation (depois do kit de UI e do login), Exercise plan on mobile, Session booking on mobile, Messages on mobile
  — cada "on mobile" depois do Mobile foundation e do item correspondente do site
Crio?
> ok
Claude: Esqueleto: PhysioSite.Web, PhysioSite.Contracts, PhysioSite.Shared (Razor Class Library, textos em
  pt-BR, pt-PT e en), PhysioSite.Tests com os testes de arquitetura e de layout. Ainda sem projeto do app e sem linha
  Complement: o Mobile foundation traz isso. Crio?
```

Meses depois, o `/agile:refine` do "Mobile foundation" lê o "Born with the site" do complemento: `Contracts` e `Shared` já estão lá, então o item acrescenta `PhysioSite.Mobile.Core` e `PhysioSite.Mobile`, o login do app, a linha `Complement:` e a cabeça na versão atual do site.

### 14.35 Fazendo deploy de um release

O ExamPrep (14.29) é um `web-app` com um AppHost na receita. O `docs/infra.md` dele tem staging (`http://localhost:5081`) e produção (`http://localhost:5080`) com o comando de deploy e uma Check URL, e `Parameters__apikey` em `## Expected secrets` como variável de ambiente dos dois. A `main` tem `<Version>0.2.0</Version>` e ainda não há tag. Antes de iniciar o Claude você definiu `Parameters__apikey` no seu próprio terminal; o valor nunca é digitado no chat.

```
> /agile:publish staging
Claude: Plano: release v0.2.0 (anterior v0.1.0, 2 itens), depois deploy dele em staging.
  Em staging agora: nada registrado.
  Comando: aspire deploy --apphost src/ExamPrep.AppHost/ExamPrep.AppHost.csproj -e Staging
           -o artifacts/deploy/staging --clear-cache --non-interactive --nologo
  Segredos, só os nomes: Parameters__apikey (definida).
  Seguindo: digitar o comando é a autorização. O deploy gera a imagem: leva cerca de um minuto.
Claude: Release v0.2.0 feito: notas commitadas, tag e main enviadas, GitHub Release criado.
Claude: v0.2.0 implantada em staging em 43 s. http://localhost:5081/health respondeu 200 após 0,1 s.
  O docs/infra.md diz "v0.2.0 (2026-10-02)" para staging: commit 7172abd, enviado.
  Saída salva em C:\Users\voce\AppData\Local\Temp\agile-deploy\staging-v0.2.0-....log.
  Sobrou 1 tag de imagem aspire-deploy-* (docker image ls --filter "reference=*:aspire-deploy-*"); aqui as antigas nunca são apagadas.
```

Você confere o staging e então:

```
> /agile:publish production
Claude: Plano: promoção. A v0.2.0 já está marcada: sem pacote, notas nem tag novos.
  Em produção agora: nada registrado. A implantar: v0.2.0. Segredos, só os nomes: Parameters__apikey (definida).
Claude: v0.2.0 implantada em produção em 18 s; http://localhost:5080/health respondeu 200. docs/infra.md atualizado (commit enviado).
  O staging não foi tocado: dois projetos compose rodam lado a lado.
```

Uma versão posterior, a 0.3.0, vai para produção e sai errada. O rollback precisa da tag e de nenhuma pergunta:

```
> /agile:publish production v0.2.0
Claude: Plano: rollback. Em produção agora: v0.3.0. A implantar: v0.2.0 (a tag existe). Segredos: Parameters__apikey (definida).
Claude: v0.2.0 implantada em produção em 17 s; /health respondeu 200. O docs/infra.md diz "v0.2.0 (2026-10-02)".
  Um projeto de produção está rodando (substituído, não duplicado); o seu checkout principal continuou na main.
```

Uma versão quebrada falha e diz como voltar; o segredo é conferido primeiro:

```
> /agile:publish staging v0.3.1
Claude: O comando de deploy saiu com 0, mas http://localhost:5081/health nunca respondeu 200 em 60 s (último: HTTP 503).
  Nada foi registrado e nada foi revertido. Fim da saída: ...
  Para voltar à versão registrada antes: /agile:publish staging v0.3.0

> /agile:publish production
Claude: A variável de ambiente Parameters__apikey não está definida: defina-a fora deste chat (o valor nunca é lido,
  mostrado nem pedido) e rode o comando de novo. Nada rodou.
```

Um erro de compilação na tag termina do mesmo jeito (`the deploy command exited 6`, as últimas linhas do build, a versão que estava no ar ainda respondendo). Sem ambiente, o `/agile:publish` depois de um ship lista: `local (not declared) · staging v0.3.0 · production v0.2.0`, e pergunta qual ou "nenhum" (só o release). Um ambiente cujo comando diz `not declared` não implanta nada: `Claude: production não declara comando de deploy: escreva-o na célula "Deploy command" da linha production do docs/infra.md.`

### 14.36 Uma tag enviada se implanta sozinha

O ExamPrep (14.35) tem `staging` e `production` declarados no `docs/infra.md`, `Parameters__apikey` como variável de ambiente dos dois e um `origin` no GitHub. Ele foi iniciado antes de o pipeline existir, então, depois de uma atualização do plugin, o `/agile:sync` avisa e não escreve nada:

```
> /agile:sync
Claude: Deploy pipeline (desde a 0.0.102) está ausente: um workflow do GitHub Actions que implanta uma tag v<Version>
  enviada pelo Deploy command do docs/infra.md. O sync nunca o instala; capturo um item para isso? (s/n)
> s
```

O item roda o gerador de pipeline do sync.js e mostra o que escreveu; o relatório antes dele mostra as escolhas que ele fez a partir da tabela:

```
wrote .github/workflows/deploy.yml (tag deploys to staging; secrets: Parameters__apikey) and .github/scripts/agile-deploy.js
```

Você adiciona `Parameters__apikey` aos environments `staging` e `production` do GitHub (Settings, Environments) e um revisor obrigatório em `production`. Daí em diante, um release local avisa antes de rodar:

```
> /agile:publish staging
Claude: Plano: release v0.3.0 (anterior v0.2.0, 2 itens), depois deploy dele em staging.
  O pipeline (.github/workflows/deploy.yml) também implanta a v0.3.0 em staging quando o release enviar a tag:
  os dois rodam o mesmo comando no mesmo ambiente ao mesmo tempo e não são serializados (o grupo de concorrência do pipeline só enfileira as execuções dele).
  ...
```

O push da tag inicia a execução. O resumo dela, para a tag:

```
## Deployed

v0.3.0 deployed to staging

It took 43 s.
Record it: the Version (deployed on) cell of staging in docs/infra.md is written only by a local /agile:publish.
Check: http://staging.examprep.example/health answered 200 in 0.4 s.
```

Produção é uma execução manual (Actions, Deploy, Run workflow: `environment` = `production`, `tag` = `v0.3.0`), que espera o revisor que você pôs. Um segredo que nunca foi adicionado para a execução antes do comando, e só o nome dele é impresso:

```
the secret Parameters__apikey is not set for the GitHub environment production: add it to the environment's secrets,
make sure .github/workflows/deploy.yml passes it (secrets.<name>), then run again; the command did not run
```

Uma versão que implanta mas nunca responde derruba a execução e diz como voltar; o rollback é a mesma execução manual com a tag antiga, ou `/agile:publish production v0.2.0` na sua máquina:

```
the deploy command exited 0 but http://staging.examprep.example/health never answered 200 in 60 s (last: HTTP 503)

Nothing was rolled back. To go back: /agile:publish staging v<the version recorded for staging in docs/infra.md> locally, or run this workflow by hand with that tag.
```

(As mensagens do pipeline ficam em inglês: são o log de uma execução do GitHub, não o chat.) Quando a tabela listar mais um segredo, o próximo `/agile:sync` mostra o `.github/workflows/deploy.yml` como diferença `manual` (a linha nova `secrets.<nome>`) e você mescla à mão.

### 14.37 O tamanho do que é sempre carregado

O `CLAUDE.md` do Simulab tem 553 palavras. No próximo `/agile:sync` a tabela tem uma linha a mais, seja qual for o resto:

```
| Arquivo / verificação | Estado | Ação |
| Tamanho do CLAUDE.md | 553 palavras (limite ~900); sempre carregado ~3,6k tokens (orçamento ~4,5k) | nenhuma |
```

Meses depois os retros engordaram `## Project-specific rules` e `## Models`; a linha agora marca:

```
| Tamanho do CLAUDE.md | 941 palavras (limite ~900); sempre carregado ~4,6k tokens (orçamento ~4,5k) | acima do limite e do orçamento: enxugue "Project-specific rules" ou "Models" |
```

O sync não pergunta nem escreve nada aqui. No próximo `/agile:retro`, antes de acrescentar uma linha ao `CLAUDE.md`, o Claude propõe mover uma primeiro:

```
Claude: o CLAUDE.md está com 941 palavras (limite ~900). Antes de acrescentar esta linha, mover "Use the shared JSON options" para .claude/rules/agile/project.md? (s/n)
```

Você decide; se disser não, a linha entra do mesmo jeito: nada bloqueia por tamanho.

### 14.38 A versão da app passa para o Directory.Build.props

O AndreaLisboa é um `website` do bootstrap anterior à 0.0.104: o `<Version>0.4.0</Version>` está no `AndreaLisboa.Web.csproj`, e o `AndreaLisboa.Core.dll` informa `1.0.0` porque nenhuma biblioteca leva número. Você atualiza o plugin, reinicia, roda `/agile:sync` (ele informa o estado e não oferece nada: `<Version> is on AndreaLisboa.Web.csproj (0.4.0): the next /agile:ship moves it to Directory.Build.props`) e, uma feature depois, digita `/agile:ship F-12`. O relatório do ship ganha duas linhas, e nenhuma pergunta veio antes:

```
app version: moved 0.4.0 from AndreaLisboa.Web.csproj to Directory.Build.props (highest wins: 0.4.0)
app version: 0.4.0 -> 0.5.0 in Directory.Build.props (feature)
```

A mudança é um commit próprio na branch do item, antes do merge. Dali em diante toda DLL da app informa `0.5.0+<commit>`: as propriedades do arquivo `AndreaLisboa.Core.dll`, em qualquer máquina, dizem o release e o commit de onde ele veio. Um projeto sem versão em lugar nenhum (o Simulab) lê `app version: added 0.1.0 to Directory.Build.props (no bump: this item ships as 0.1.0)`, e o `/agile:publish` lê o número do mesmo arquivo.

### 14.39 Offline-first desde o primeiro dia

O brief do FieldCheck diz que os inspetores preenchem checklists em subsolos e fábricas "sem sinal", e o app usa a API existente do repositório `ops-api` da empresa, um `web-api` do agile:

```
> /agile:bootstrap
Claude: Rodada 1. Pergunta 1: mobile numa API existente. Pergunta 2: mobile. Pergunta 2d, onde mora o backend?
   Recomendação: uma API existente. Motivo: o brief cita o ops-api.
2f. Online ou offline-first? online · offline-first
   Recomendação: offline-first. Motivo: o brief diz que os inspetores trabalham "sem sinal".
   O docs/api/openapi.json do ops-api não tem o cabeçalho Idempotency-Key nas operações de escrita: proponho a issue
   "Idempotent writes for FieldCheck" no quadro do ops-api, e "Offline foundation" espera por ela. Nunca escrevo naquele repositório.
> ok, cria a issue
Claude: Resumo: o CLAUDE.md ganha "- Offline: first", o ADR-0001 uma linha. Quadro: UI kit e galeria, Mobile foundation,
   Offline foundation (depois dos dois; espera ops-api#31), depois as funcionalidades.
```

Semanas depois, o `/agile:refine` de "Checklist no mobile" faz a rodada extra que o perfil exige, `## Offline`: quais checklists ficam no cache (os atribuídos ao inspetor), quais escritas entram na fila (respostas e notas; o envio de foto é uma escrita à parte) e quem ganha num conflito — o padrão (o servidor recusa a escrita velha e o usuário escolhe), a menos que você diga outra coisa. O arquivo da funcionalidade leva as suas respostas, e cada critério de aceite sobre salvar tem o seu gêmeo offline.


### 14.40 Os argumentos opcionais: retomar um quiz, uma tela pelo nome, uma mudança numa linha, uma nota de pausa, uma retro de um item

Cinco comandos aceitam um argumento que você pode deixar de fora. Cada exemplo abaixo é o mesmo comando que você já conhece, com o argumento escrito.

Retomar um quiz que você parou (`/agile:bootstrap [rodada]`). As respostas das rodadas já feitas estão em `docs/decisions/.quiz-draft.md`, então nada é perguntado de novo:

```
> /agile:bootstrap 4
Claude: Retomando da rodada 4. As rodadas 1 a 3 estão em docs/decisions/.quiz-draft.md (mobile, offline-first, PostgreSQL).
   Rodada 4. Pergunta 9: ...
```

Desenhar uma tela de uma feature que tem várias (`/agile:screen F-12 "Resumo do checkout"`). O nome diz ao Claude qual tela mandar para o `ux-designer`:

```
> /agile:screen F-12 "Resumo do checkout"
Claude: F-12 está em refinamento. Desenhando só "Resumo do checkout" com o ux-designer; "Carrinho" segue uma tela existente, então só a cito no arquivo.
```

Dizer a mudança na mesma linha (`/agile:change F-12 "o desconto é por item, não por pedido"`). O Claude ainda mostra os ids afetados e espera o seu "aprovo" antes de escrever a nota de mudança:

```
> /agile:change F-12 "o desconto é por item, não por pedido"
Claude: F-12 está em build. Afetados: BR-2 (desconto por pedido → por item), AC-3, AC-5; não afetados: UC-1, AC-1. Código já escrito: DiscountCalculator.cs e 2 testes a ajustar. Aprova?
> aprovo
Claude: Nota de mudança v2 escrita, F-12 continua em build; retomando no passo 4 do plano.
```

Deixar um recado para a próxima sessão (`/agile:pause "esperando a chave do sandbox de pagamento"`). A nota entra na mensagem de pausa, junto do commit wip e do próximo passo:

```
> /agile:pause "esperando a chave do sandbox de pagamento"
Claude: wip(F-12): desconto por item, testes do AC-3 pela metade (4e1f2a9). Próximo: terminar o AC-3, depois a tela.
   Pendente com você: a chave do sandbox de pagamento. Nada rodando.
```

Transformar as lições de um item, ou da sessão inteira, em regras (`/agile:retro F-12` ou `/agile:retro session`). Com o id de um item o Claude lê as notas de mudança, as decisões e as correções de validação dele; com `session` lê o que deu errado ou foi corrigido neste chat. No máximo 3 lições nos dois casos, cada uma com uma nota de plugin e uma de projeto, de 0 a 10 (Exemplo 14.66):

```
> /agile:retro session
Claude: 2 lições desta sessão, cada uma com as notas, o destino e a linha exata. Aprova?
```

### 14.41 A fila offline é enviada com o app fechado

A "Offline foundation" do FieldCheck está sendo construída (Exemplo 14.39). Nada extra é perguntado: o envio em segundo plano faz parte desse item, então os critérios de aceite já o carregam, e o roteiro de validação ganha uma passada para ele:

```
> /agile:build #12
Claude: ... IBackgroundSync (Schedule, Cancel) no Mobile.Core; a head Android o implementa com WorkManager
   (Xamarin.AndroidX.Work.Runtime 2.11.2.1, MIT AND Apache-2.0, só na head). Testes do Core com um IBackgroundSync falso: 14 passaram.
   Roteiro de validação, passo 6 (emulador): modo avião ligado, salve dois checklists, deslize o app para fora dos recentes,
   rede ligada. Sem abrir o app, a API recebe os dois, uma vez cada, em ordem. Abra o app: 0 pendentes.
   Passo 7: um checklist que outra pessoa mudou nesse meio tempo termina na lista de conflitos na próxima abertura; o outro foi enviado.
   Passo 8: Configurações, Forçar parada, rede ligada: nada é enviado até você abrir o app, e então a fila sai de uma vez.
```

Com o app fechado nada é mostrado: nem notificação nem permissão. Uma sessão expirada ou uma atualização obrigatória espera a próxima abertura, onde o banner diz "entre de novo" ou "atualização necessária"; uma execução em segundo plano nunca desconecta o usuário nem apaga uma escrita pendente. O iOS é outro item (agile-canary#55).

### 14.42 Um conflito é mesclado campo a campo

O FieldCheck é offline-first (Exemplos 14.39 e 14.41). Dois inspetores mudaram o mesmo checklist, um deles sem sinal. Nada é perguntado no refinamento: a mesclagem faz parte da "Offline foundation" para todo registro, e uma feature a desliga para um registro na sua seção `## Offline`, com o motivo (um valor em dinheiro, onde misturar campos está errado).

```
[Celular da inspetora Ana, sinal de volta] Lista de conflitos: "O checklist 12 foi alterado por outra pessoa"
   [Mesclar]  [Enviar a minha de novo]  [Descartar]
> Mesclar
   3 campos foram alterados nos dois lados — escolha um para cada
   Telefone   alterado nos dois   ( ) Seu 555-0101   ( ) Atual 555-0199
   Notas      alterado nos dois   ( ) Seu "porta consertada"   ( ) Atual "porta trocada"
   Contatos   lista inteira       ( ) Seu (3 pessoas)   ( ) Atual (4 pessoas)
   E-mail     só o seu mudou      ana@field.test
   Cidade     só o deles mudou    Porto
   [Cancelar]   [Enviar mesclado] (desabilitado até as três escolhas)
> escolhe Telefone: Seu, Notas: Atual, Contatos: Atual → Enviar mesclado
Claude (relatório do build): testes do Core: 13 passaram (cada linha da regra por campo, uma lista como uma escolha,
   "Enviar mesclado" desabilitado, uma atualização restante no registro com a versão atual, outros registros intactos,
   uma mesclagem velha volta para a lista).
   Roteiro de validação, passo 7 (emulador): dois usuários mudam um registro, o offline mescla, a API guarda os valores mesclados uma vez;
   passo 8: uma segunda mesclagem velha volta o registro para a lista.
```

"Cancelar" deixa o conflito como estava. Um conflito sobre uma exclusão, um `404`, um `400` ou um `422` não tem o que mesclar e mantém as duas ações antigas.

### 14.43 Um app desktop que funciona sem rede

O brief descreve um app desktop de vendas externas para Windows e macOS: os representantes precificam orçamentos no cliente, quase sempre sem conexão, e sincronizam depois. A pergunta 2a2 acabou de escolher uma API.

```
Claude: Pergunta 2f, online ou offline-first? online · offline-first
   Recomendação: offline-first. Motivo: o brief diz que os orçamentos são precificados "no cliente, quase sempre sem conexão".
> ok
Claude: Resumo: o CLAUDE.md ganha "- Offline: first" em Profile:, o ADR-0001 uma linha. Board: UI kit e galeria,
   a primeira feature, Offline foundation (depois das duas), depois as features. A fila não é enviada com o app fechado:
   isso está fora do perfil (sem ícone de bandeja nem serviço).
```

Com o mesmo brief, mas um app de um só usuário numa rede fechada que vai direto ao PostgreSQL, a 2f não é feita e o ADR-0001 diz: "Offline-first precisa de uma API; um armazenamento SQLite já é local."

Depois, o `/agile:refine` de "Lista de orçamentos" pergunta a seção `## Offline`: quais orçamentos ficam no cache (os abertos do representante), quais escritas entram na fila (edições de preço, anotações) e quem vence um conflito. O roteiro de validação da "Offline foundation" tem dois passos que só você executa, um por head escolhido: com o adaptador de rede desligado, salve duas vezes; o banner mostra "2 pendentes"; ligue o adaptador e a contagem esvazia em ordem, e a API guarda cada alteração uma vez.

### 14.44 Um app desktop que se atualiza sozinho

O app da recepção (Avalonia, Windows) é montado no bootstrap com uma pasta de rede da empresa. Na rodada 6:

```
Claude: Pergunta 26b, fonte de atualização? pasta de rede · URL https · repositório público do GitHub · não declarada
   Recomendação: pasta de rede. Motivo: o brief diz que os PCs ficam na rede da clínica.
> pasta de rede, o servidor de arquivos da clínica, pasta updates/recepcao
Claude: o docs/infra.md ganha "## Desktop updates" com a linha Update source; o head ganha o Velopack,
   o IAppUpdater com um fake para os testes, o Main que roda o Velopack primeiro e os dois diálogos em pt-BR, pt-PT e en.
```

O `/agile:publish` na 1.0.0 e, depois, na 1.1.0:

```
> /agile:publish
Claude: v1.1.0, App.Desktop (velopack, win-x64 -> a pasta de rede).
   Empacotado: App-win-Setup.exe 47 MB, App-1.1.0-full.nupkg 39 MB, App-1.1.0-delta.nupkg 62 KB
   (delta feito a partir da 1.0.0, baixada da pasta de rede). Tag v1.1.0, feed enviado para a pasta.
```

Num PC com a 1.0.0 instalada, o app abre normalmente; um instante depois aparece uma janela pequena:

```
Atualização disponível
A versão 1.1.0 está disponível.        [ Atualizar agora ]  [ Depois ]
```

"Atualizar agora" reinicia o app na 1.1.0. "Depois" deixa a recepcionista continuar trabalhando; a atualização é aplicada quando o app é fechado, mesmo fora do expediente. Na primeira release as notas dizem também: "Instale uma vez com o App-win-Setup.exe" e que o SmartScreen do Windows avisa porque o instalador não é assinado. Um PC que não alcança a pasta de rede segue com a versão que tem.

### 14.45 O glossário sob cada documento

O `docs/infra.md` do app da recepção diz "Bicep" e "ACR", e o glossário não tem linha para nenhum dos dois. No ship, o diff do item em `docs/` é lido:

```
> /agile:ship F-12
Claude: Suíte completa, 214 testes verdes (41 s). Glossário: 2 linhas acrescentadas: Bicep, ACR
   (o diff do docs/infra.md cita os dois; as linhas estão na branch, com o significado em pt-BR). Merge feito.
```

O `docs/glossary.md` agora tem, em "Technical terms":

```
| Bicep | Bicep | Azure's language for describing infrastructure as code. | A linguagem do Azure para descrever infraestrutura como código. |
| ACR | ACR (registro de contêineres) | Azure Container Registry, where the app's container images are kept. | Azure Container Registry, onde ficam guardadas as imagens de contêiner da app. |
```

Um ship cujo diff não traz termo novo diz `Glossário: nada faltando`. O `docs/infra.md` mostra, logo abaixo do título, `Technical terms: [glossary](glossary.md)`; um ADR ou um arquivo de item mostra `../glossary.md`.

Num projeto anterior à 0.0.111, o `/agile:sync`:

```
> /agile:sync
Claude: glossário, 3 propostas:
   1. a coluna "Meaning (pt-BR)" nas duas tabelas, escrita para as 36 linhas existentes;
   2. 3 linhas para termos sem nenhuma: Bicep, Azure Container Apps, Azure Key Vault (docs/infra.md, ADR-0001);
   3. a linha do glossário sob o título do docs/infra.md e de outros 14 documentos.
   Nada é escrito antes do seu OK.
```

### 14.46 O build acha a worktree do item primeiro

Você refinou o B-65 até `approved`; o arquivo dele vive só na worktree. Uma sessão nova está no checkout principal, que tem arquivos não commitados próprios, e você digita:

```
> /agile:build B-65
Claude: Worktree of B-65: D:\wt\examprep\b-65-search-index [bug/B-65]
   status: approved (lido ali; o checkout principal não tem cópia). Essa worktree está limpa;
   os arquivos não commitados do checkout principal não são assunto do build. Construindo.
```

Um item aprovado antes de existirem worktrees, ou cuja worktree foi removida, não tem nenhuma; a branch ainda pode existir:

```
> /agile:build B-12
Claude: Worktree of B-12: D:\wt\examprep\b-12-export-csv [bug/B-12] (created now)
   (a branch bug/B-12 já existia, então a worktree foi adicionada nela, sem -b).
```

O `/agile:ship` e o `/agile:change` leem o status na mesma worktree. Sem ela, param e dizem a branch que procuraram (`bug/B-12`); nunca criam uma.

### 14.47 Um deploy que cai no Azure do cliente, nunca em outro

O repositório e o board do Simulab são seus; o staging dele pertence ao cliente Acme, no tenant Azure deles. Na rodada 6 do bootstrap:

```
Claude: Pergunta 25a, conta de nuvem do staging? o nome do cliente e azure · aws · gcp · other · none
        Recomendado: azure (a pergunta 3 diz Azure Container Apps).
Você: Acme, azure. Ainda não tenho os ids.
Claude: o docs/infra.md ganha, em "## Cloud accounts":
        | staging | Acme | azure | | | | |
        Os ids ficam em branco até a Acme fornecê-los; o /agile:publish para até lá.
```

O administrador da Acme convida você como convidado e dá o papel Contributor na assinatura; você escreve os dois ids na linha. O primeiro publish para no login, antes de a tag ser enviada:

```
> /agile:publish staging
Claude: staging is in Acme's Azure (tenant 11111111-1111-4111-8111-111111111111, subscription
        22222222-2222-4222-8222-222222222222): no login in C:/Users/voce/.agile/azure/11111111-.../22222222-...;
        nothing ran. Log in once, in Git Bash:
        AZURE_CONFIG_DIR="C:/Users/voce/.agile/azure/11111111-.../22222222-..." az login --tenant 11111111-...
        No PowerShell (não fica definida):
        & { $env:AZURE_CONFIG_DIR = 'C:\Users\voce\.agile\azure\11111111-...\22222222-...'; az login --tenant 11111111-...; Remove-Item Env:AZURE_CONFIG_DIR }
```

Você roda uma das duas linhas no seu próprio terminal e digita o comando de novo. O plano agora mostra a conta e o que foi limpo:

```
Claude: staging: v0.4.0 (release, depois deploy) · cliente Acme · azure · pasta de login .agile/azure/1111.../2222...
        removidas do ambiente: AZURE_CLIENT_SECRET, ARM_SUBSCRIPTION_ID (só nomes; os valores da linha entram no lugar)
```

O seu login padrão do `az` (a assinatura de outro cliente) não é tocado. Se o login não enxergasse a assinatura da Acme, a parada diria a declarada e a encontrada: `... but the login in <pasta> is Basic subscription 2fae... (tenant ...) and cannot see it: ask the client for access to that subscription, or correct the row; nothing ran`. Quando o projeto tem o pipeline de deploy, o `/agile:sync` oferece o passo `azure/login` ao workflow dele, o administrador da Acme cria a credencial federada e você define `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` e `AZURE_SUBSCRIPTION_ID` como variáveis do ambiente `staging` do GitHub, com uma regra de proteção que só aceita tags `v*`.

### 14.48 Onde um item novo mora: a pergunta do épico

Dois épicos estão abertos, "Catalog" e "Content", e o que você digita a seguir cabe no primeiro. Nada é criado antes da sua resposta:

```
> /agile:idea "Exam board registration: name and acronym, list and edit"
```

```
Claude: Épico para F-3 "Exam board registration"?
        (também serve: "Admin" - cadastros de apoio; escolha por Outro)
        1. Catalog (Recomendado) - o objetivo do épico é cadastrar o que o candidato pesquisa
        2. Content - conteúdo das provas
        3. Novo épico: "Exam boards" - cadastro e manutenção de bancas
        4. Sem épico
```

Com um texto que não cabe em nenhum épico, "Novo épico" vem primeiro, como recomendação. Responder isso cria o épico e, depois, a feature debaixo dele:

```
> /agile:idea "Reports on candidate retention"
Claude: Épico para F-4 "Reports on candidate retention"?
        1. Novo épico: "Retention" - entender por que candidatos param de praticar (Recomendado)
        2. Sem épico
Você: 1
Claude: Criei o épico "Retention" (board #14, docs/epics/retention.md, draft) e a feature F-4 (board #15) debaixo dele.
```

"Sem épico" não cria épico e a saída diz `epic: none`. O `/agile:epic` nunca pergunta: ele já sabe o épico. No `/agile:refine F-3`, uma resposta fora do escopo que vira ideia nova lista "Catalog" primeiro, o épico do item em refinamento.

### 14.49 Uma duplicata achada antes de registrar, e outra cancelada no refinamento

O ExamPrep tem a F-12 "Exam board search" em `idea` e a F-15 "Search by acronym" em `refining` na própria worktree. Você digita:

```
> /agile:idea "buscar bancas por sigla"
Claude: 2 itens parecidos (14 itens lidos):
  1. F-12 Exam board search (idea) — a mesma busca, a sigla é só mais um campo.
  2. F-15 Search by acronym (refining, só na worktree) — o mesmo assunto com outras palavras.
  Recomendo: melhorar a F-12. [Melhorar a F-12 | Melhorar a F-15 | Registrar mesmo assim]
> Melhorar a F-12
Claude: o Summary da F-12 agora termina com "Added 2026-10-04: search also by board acronym".
Nenhum número foi pego e nada foi criado no board.
```

Se você tivesse escolhido a F-15, o Claude teria colocado o texto em `## Open questions` de `D:/wt/examprep/f-15-search-by-acronym/docs/features/F-15-search-by-acronym.md`, dizendo esse caminho. Para uma F-12 `approved` não escreveria nada e imprimiria `/agile:change F-12 "search also by board acronym"` para você digitar. Em outro momento nada se parece:

```
> /agile:idea "exportar os resultados das provas para planilha"
Claude: No similar item (14 items read).
F-16 Export results, épico Results, board #31, status idea. Nada mais acontece até /agile:refine F-16.
```

Depois você refina um item registrado à mão antes desta checagem existir:

```
> /agile:refine F-14
Claude: 1 item parecido (15 itens lidos):
  1. F-12 Exam board search (idea) — a F-14 pede a mesma busca por sigla.
  Recomendo: levar a melhoria à F-12 e cancelar a F-14.
  [Levar e cancelar a F-14 | Manter os dois | Absorver a F-12 na F-14]
> Levar e cancelar a F-14
Claude: o Summary da F-12 agora tem "Added 2026-10-04: ...". A F-14 está `cancelled` ("Duplicate of F-12
(2026-10-04): the Summary of F-12"); issue #29 fechado como duplicata do #27 e arquivado do projeto.
Nenhuma branch ou worktree foi criada. O próximo /agile:idea pega a F-17.
```

### 14.50 Cada merge chega aos testadores como beta

O app da recepção do 14.44 tem duas recepcionistas que testam cada mudança antes da clínica recebê-la. O `docs/infra.md` dele ganha uma linha em `## Desktop updates` (um projeto novo responde "every merge" na pergunta 26c):

```
- Update source: \\fs01\updates\reception
- Beta channel: every merge
```

Cada testadora roda uma vez o `Reception-beta-Setup.exe` da pasta de rede (ele substitui a instalação estável naquele PC). O ship da F-21 termina com uma linha a mais:

```
> /agile:ship F-21
Claude: ... merge feito (a1b2c3d), board #21 Done (lido de volta).
   Beta: 0.5.1-beta mandado para \\fs01\updates\reception (delta de 72 KB a partir do 0.5.0-beta);
   as testadoras instalam o Reception-beta-Setup.exe de lá.
```

No próximo início, os PCs das testadoras mostram "Version 0.5.1-beta is available."; os PCs da clínica, no estável, não veem nada. Quando a versão sai com `/agile:publish`, a clínica pega a 0.5.1; as testadoras continuam no beta e pegam a 0.5.2-beta depois do próximo ship.

Uma semana depois, o servidor de arquivos está fora do ar durante um ship:

```
> /agile:ship F-22
Claude: ... merge feito (d4e5f6a), board #22 Done (lido de volta).
   Beta: não mandado. vpk upload of 0.5.2-beta (beta) to \\fs01\updates\reception failed:
   "The network path was not found." O merge fica. Quando a pasta voltar: /agile:publish --beta
> /agile:publish --beta
Claude: Beta: 0.5.2-beta mandado para \\fs01\updates\reception (delta de 70 KB a partir do 0.5.1-beta).
```

Rodar de novo diz "0.5.2-beta is already on the share: nothing was packed or sent". Uma testadora que quer voltar ao estável roda o `Reception-win-Setup.exe`.

### 14.51 O mesmo app se atualiza no Linux

O novo balcão da clínica roda Ubuntu. O app da recepção do 14.44 é Avalonia, então o head passa a listar mais um runtime: `<RuntimeIdentifiers>win-x64;linux-x64</RuntimeIdentifiers>`. O primeiro `/agile:publish` depois disso para antes de construir qualquer coisa:

```
> /agile:publish
Claude: Não publicado. Reception.Desktop: a fonte de atualização é a pasta de rede \\fs01\updates\reception,
   que as máquinas Linux não leem por esse nome: acrescente
   `- Update source (linux): <the path where Linux mounts it, or https://...>` em `## Desktop updates`.
```

Os PCs Linux montam o servidor de arquivos em `/mnt/updates`, então o `docs/infra.md` ganha uma linha (num item na branch principal, como qualquer mudança):

```
- Update source: \\fs01\updates\reception
- Update source (linux): /mnt/updates/reception
```

Publicando de novo, ainda do PC Windows:

```
> /agile:publish
Claude: v1.3.0, Reception.Desktop (velopack, win-x64, linux-x64 -> \\fs01\updates\reception;
   o Linux lê /mnt/updates/reception).
   win-x64: delta de 74 KB feito a partir da 1.2.0. linux-x64: Reception.AppImage 49 MB, primeira release no canal linux
   nesta fonte: sem delta. Tag v1.3.0; os dois feeds enviados para a pasta; o GitHub Release leva o AppImage.
```

As notas da v1.3.0 terminam com as linhas de instalação no Linux: baixe o `Reception.AppImage`, ponha em `~/Applications`, `chmod +x`, abra; uma cópia de um zip antigo não se atualiza; a primeira atualização baixa o pacote completo, as seguintes só o que mudou. Da 1.4.0 em diante, o balcão Ubuntu vê a mesma janela "A versão 1.4.0 está disponível." que os PCs Windows, no idioma da sessão (`LANG`), e "Depois" aplica quando o app é fechado.

### 14.52 O instalador leva o nome da clínica

O app da recepção do 14.44 pergunta em todo PC novo "Deseja permitir que este aplicativo de um editor desconhecido...?". A empresa da clínica, em Portugal, abre uma conta do Microsoft Artifact Signing com um perfil de certificado de confiança pública, e o dono grava o `metadata.json` dela em `C:\signing\clinic\metadata.json`, fora do repositório. O `docs/infra.md` ganha, por um item como qualquer mudança:

```
## Code signing (a desktop app only)
- Code signing: artifact-signing
- Signing account: 0f3c.../7a1d...
```

A primeira publicação depois do merge para antes de construir qualquer coisa:

```
> /agile:publish
Claude: Não publicado. Code signing is artifact-signing in docs/infra.md but VPK_AZURE_TRUSTED_SIGN_FILE
   is not set: set it in the shell that runs /agile:publish (its value is never written in docs/infra.md).
   Um modo declarado nunca é publicado sem assinatura; nada foi construído.
```

O dono define a variável no próprio terminal (`$env:VPK_AZURE_TRUSTED_SIGN_FILE = 'C:\signing\clinic\metadata.json'`), reabre a sessão a partir dele e faz login uma vez na conta de assinatura, com a linha que a parada seguinte dá (`AZURE_CONFIG_DIR="C:/Users/dono/.agile/azure/0f3c.../7a1d..." az login --tenant 0f3c...`). Então:

```
> /agile:publish
Claude: v1.5.0, Reception.Desktop (velopack, win-x64 -> \\fs01\updates\reception, signed: artifact-signing;
   login em C:/Users/dono/.agile/azure/0f3c.../7a1d...).
   win-x64: delta de 81 KB a partir da 1.4.0. Assinado por Clinica Sorriso, Lda: Reception-win-Setup.exe,
   Reception.Desktop.exe e Update.exe conferidos, Valid. Tag v1.5.0; feed enviado para a pasta de rede.
```

As notas da v1.5.0 dizem (em inglês, como toda linha que o `publish.js` escreve nas notas): "The installer is signed by Clinica Sorriso, Lda. During the first weeks Windows may still show "unrecognized app" with that name: click "More info", then "Run anyway"." Os PCs instalados se atualizam para a 1.5.0 pelo delta de sempre e não percebem nada; a janela de download de um PC novo mostra o nome da clínica. As notas da v1.6.0 não repetem a linha. Se o perfil do certificado tivesse expirado, a publicação teria parado com `Reception-win-Setup.exe is not validly signed (status ...)`, antes das notas e da tag.

### 14.53 Os instaladores ficam públicos, o código continua privado

O app da recepção da clínica (14.44) passa do servidor de arquivos: uma segunda clínica, em outra cidade, não tem acesso a ele. O código mora no repositório privado do Azure DevOps da empresa, então o dono cria um repositório **público** vazio no GitHub, `clinica-sorriso/reception-releases`, só para guardar os releases, e muda uma linha do `docs/infra.md` (um item, como qualquer mudança):

```
- Update source: https://github.com/clinica-sorriso/reception-releases
```

O head lê a fonte com um `GithubSource` quando a linha começa com `https://github.com/`. O próximo `/agile:publish` confere o repositório primeiro:

```
> /agile:publish
Claude: Não publicado. Reception.Desktop: a fonte de atualização é https://github.com/clinica-sorriso/reception-releases,
   um repositório PRIVATE: um repositório privado exigiria um token dentro de cada app instalado, e esse token
   lê o código: use um repositório público, uma pasta de rede ou uma fonte https; nada foi construído.
```

O dono tinha criado o repositório privado por engano; depois de torná-lo público:

```
> /agile:publish
Claude: v2.1.0, Reception.Desktop (velopack, win-x64 -> GitHub Releases de clinica-sorriso/reception-releases).
   win-x64: delta de 64 KB feito a partir da 2.0.0 (baixada do repositório, sem token). Tag v2.1.0 no repositório do código.
   GitHub Release v2.1.0 criado em clinica-sorriso/reception-releases com as notas; Setup.exe, o zip portátil,
   os pacotes e o índice do feed anexados. Os apps instalados enxergam até 60 s depois do upload (cache da API do GitHub).
```

Um dia depois, o upload de um release é cortado no meio por uma conexão que caiu:

```
> /agile:publish
Claude: v2.2.0 tagueada e enviada. vpk upload to clinica-sorriso/reception-releases failed (win-x64): connection reset;
   o release v2.2.0 fica com Reception-2.2.0-full.nupkg, e os apps instalados não veem atualização até que o
   releases.win.json seja anexado (o vpk o envia por último). Para refazer:
   gh release delete-asset v2.2.0 Reception-2.2.0-full.nupkg --repo clinica-sorriso/reception-releases --yes
   dotnet vpk upload github --repoUrl https://github.com/clinica-sorriso/reception-releases --token "$(gh auth token)" --merge --tag v2.2.0 -o artifacts/publish/v2.2.0/velopack/win-x64
```

O token da última linha é o seu próprio login do `gh`, lido quando o comando roda; ele nunca aparece no chat. Os PCs da segunda clínica se atualizam por delta assim que o release está completo; um PC que abre o app mais de 60 vezes por hora atrás de um mesmo endereço do escritório vê uma checagem falhar no log e tenta de novo na próxima abertura.

### 14.54 A equipe gerencia os dados que o app mostra

O brief do FieldCheck (um app `mobile` sobre `monolith`, com a API na mesma solução) diz: "nossa equipe cadastra os checklists e vê as visitas de todos os inspetores". Logo depois da 2f:

```
Claude: Pergunta 2g, um backoffice web para a equipe? não · sim
   Recomendação: sim. Motivo: o brief cita "nossa equipe", que "cadastra os checklists e vê as visitas de todos os inspetores".
> ok
Claude: Resumo: o CLAUDE.md ganha "- Backoffice: staff" em Profile:, o ADR-0001 uma linha (uma área Blazor Server sob
   /backoffice no host da API, uma Identity, equipe por papel, app autenticador para o Admin, a equipe vê todos os clientes).
   Board: UI kit e galeria, a primeira feature, Backoffice foundation (depois do UI kit), depois as features.
   Sem mudança no esqueleto: o backoffice é construído pelo seu próprio item.
```

Com o mesmo brief, mas a API em outro repositório (`Backend: external`), a 2g não é feita e o ADR-0001 diz: "O backoffice mora junto dos dados, no repositório da API."

Mais tarde o "Backoffice foundation" é construído e validado. O roteiro de validação tem os passos que só você faz: o app sobe com `Backoffice:FirstAdminEmail` definido; o link do log define a senha do primeiro Admin; o Admin entra, lê o QR code, guarda os 10 códigos de recuperação e chega às páginas de dados; convida um colega como `Staff`; o colega entra e não consegue abrir a página da equipe; o Admin remove o colega e, em até um minuto, a página aberta do colega diz que ele não tem mais permissão.

Um projeto bootstrapado antes da 0.9.0 vê a capacidade no próximo sync:

```
> /agile:sync
Claude: Capacidade que falta: Staff backoffice (desde 0.9.0): uma área web para a equipe sob /backoffice no host da API,
   uma Identity, equipe por papel. Item a capturar: "Backoffice foundation", que constrói "Staff backoffice" de
   profiles/mobile.md e escreve "- Backoffice: staff" no CLAUDE.md com uma linha de ADR. Nada foi escrito.
   Capturo? Para recusar de vez, ponha "- Backoffice: none" em Profile: no CLAUDE.md e o aviso para.
```

### 14.55 Um deploy que cai no Azure Container Apps

A produção da Acme roda no Azure Container Apps, o staging dela fica na sua própria máquina. Na rodada 6 do bootstrap, depois da conta de nuvem de cada ambiente:

```
Claude: Pergunta 25b, receita da produção (nuvem azure)? aca · compose
        Recomendado: aca (banco gerenciado, nenhum host para atualizar, escala a zero).
Você: aca. O staging continua compose.
Claude: Teto mensal da produção (um número inteiro, o limite de um mês na moeda da cobrança da assinatura, ou none)?
Você: 80.
Claude: O AppHost é escrito a partir de "Deploy recipe (Azure Container Apps + Aspire)" do perfil: as duas formas num Program.cs.
        src/App.AppHost/appsettings.Production.json recebe { "Deploy": { "Target": "aca" } } e nenhum HostPort; o do staging tem o HostPort.
        docs/infra.md: URL e Check URL da produção ficam em branco (o endereço é gerado no primeiro deploy), e
        "## Cloud accounts" ganha | production | Acme | azure | | | | | 80 |: Tenant, Subscription, Resource group e Region
        são obrigatórios antes do primeiro deploy. O administrador da Acme concede Contributor e Role Based Access
        Control Administrator na assinatura.
```

A Acme te passa os dois ids e você os escreve, mas esquece as duas últimas colunas. O comando para antes de rodar qualquer coisa, dizendo o que está em branco:

```
> /agile:publish production
Claude: production: Deploy:Target is aca, but its ## Cloud accounts row leaves Resource group and Region blank; fill them (Azure__ResourceGroup, Azure__Location) — nothing ran
```

Você escreve `rg-acme-prod` e `brazilsouth` na linha e digita o comando de novo. O plano agora diz onde vai cair:

```
> /agile:publish production
Claude: production: v0.5.0 (release, then deploy) · client Acme · azure · login folder .agile/azure/1111.../2222...
        Target: aca (Azure Container Apps), resource group rg-acme-prod, region brazilsouth
        Budget: 80 per month on rg-acme-prod (billing currency of the subscription), alerts at 80 % and 100 % actual and 100 % forecast to Owner and Contributor
```

O primeiro deploy cria o conteúdo do resource group e imprime o endereço do app; você o escreve em URL e Check URL da linha da produção. O deploy seguinte, `v0.5.1`, mantém todo mundo logado, porque as chaves do Data Protection ficam num blob. Quando a tag `v0.5.1` é enviada, o log da pipeline começa com `Aspire CLI 13.6.0 installed (from Aspire.AppHost.Sdk of src/App.AppHost/App.AppHost.csproj)` antes do comando. Um projeto feito no bootstrap antes da 0.10.0 recebe a nova seção do perfil no próximo `/agile:sync`; a sessão dele muda o AppHost para as duas formas. O plugin nunca o edita.

### 14.56 Um deploy que cai na conta AWS do cliente, nunca em outra

O staging da Acme está na conta AWS dela, `123456789012`, região `us-east-1`; a sua própria máquina está logada na AWS de outro cliente. Você escreve as linhas com o id da conta do staging errado e, na produção, que está no Google Cloud, digita o nome do projeto em vez do id:

```
| staging | Acme | aws | | 1234 | | us-east-1 |
| production | Acme | gcp | | Acme Shop | | |

> /agile:publish staging --plan
Claude: the Subscription or account of staging in `## Cloud accounts` is "1234": an AWS row needs the 12-digit account id
        (aws sts get-caller-identity prints it); nothing ran
        (o /agile:publish production --plan para do mesmo jeito: ... of production ... is "Acme Shop": a GCP row needs the project id,
        not its name or number (gcloud projects list prints it); nothing ran)
```

Nenhuma CLI foi chamada. Você corrige o id (`123456789012`) e roda de novo; o plano para no login, antes de o tag ser enviado:

```
> /agile:publish staging --plan
Claude: staging is in Acme's AWS account 123456789012 (us-east-1): no login in C:/Users/you/.agile/aws/123456789012;
        nothing ran. Log in once, in Git Bash:
        AWS_CONFIG_FILE="C:/Users/you/.agile/aws/123456789012/config" AWS_SHARED_CREDENTIALS_FILE="C:/Users/you/.agile/aws/123456789012/credentials" aws configure sso
        (then `aws sso login` with the same two variables when the session expires; `aws configure` instead for access keys)
        No PowerShell (não fica definido):
        & { $env:AWS_CONFIG_FILE = 'C:\Users\you\.agile\aws\123456789012\config'; $env:AWS_SHARED_CREDENTIALS_FILE = 'C:\Users\you\.agile\aws\123456789012\credentials'; aws configure sso; Remove-Item Env:AWS_CONFIG_FILE, Env:AWS_SHARED_CREDENTIALS_FILE }
```

Você roda uma linha no seu próprio terminal, entrando com a conta que a Acme te deu, e digita o comando de novo:

```
Claude: staging: v0.4.0 (release, then deploy) · client Acme · aws · login folder .agile/aws/123456789012 · check ok
        removed from the environment: AWS_PROFILE, AWS_ACCESS_KEY_ID (names only; the row's AWS_CONFIG_FILE, AWS_SHARED_CREDENTIALS_FILE and AWS_REGION are set instead)
```

Se você tivesse entrado na conta errada nessa pasta, a parada nomearia as duas: `staging is declared in Acme's AWS account 123456789012, but the login in <pasta> is account 210987654321 (arn:aws:iam::210987654321:user/you): log in to the right account in that folder, or correct the row; nothing ran`. O login AWS da sua própria máquina (a conta de outro cliente) não é tocado. Na produção, com o id do projeto já certo, a parada por falta de login lista os dois logins do Google Cloud (`gcloud auth login` e `gcloud auth application-default login`, cada um com `CLOUDSDK_CONFIG` apontando para `.agile/gcp/<projeto>`), e uma identidade que não consegue ler o projeto é nomeada com a permissão que lhe falta: `... but you@acme.example in <pasta> cannot read it (needs resourcemanager.projects.get: Browser, Viewer, Editor or Owner): ask the client for access, or correct the row; nothing ran`. Com o pipeline, o `/agile:sync` mostra o passo `configure-aws-credentials` (ou `google-github-actions/auth`) para o workflow, o administrador da Acme cria o papel IAM, e você define `AWS_ROLE_ARN` e `AWS_REGION` como variáveis do ambiente `staging` do GitHub, com uma regra de proteção que só permite tags `v*`.

### 14.57 O zip também leva o nome da empresa

Um estúdio pequeno envia a sua ferramenta de estoque, `Stock.Desktop`, como um zip para poucos clientes; não há fonte de atualização, então não há instalador. Os PCs dos clientes dizem "Editor desconhecido". O estúdio compra um certificado de assinatura de código num token e, por um item como qualquer mudança, o `docs/infra.md` ganha:

```
## Code signing (a desktop app only)
- Code signing: signtool
```

Com `VPK_SIGN_PARAMS` definida no terminal do próprio dono (`/fd SHA256 /tr <a URL de carimbo de tempo da CA> /td SHA256 /sha1 <thumbprint>`), a primeira publicação para, porque o projeto não tem o `vpk`:

```
> /agile:publish
Claude: Not published. Code signing is signtool and Stock.Desktop is packed as a zip, which vpk signs, but this project
   has no vpk local tool in its tool manifest (.config/dotnet-tools.json or dotnet-tools.json); /agile:publish asks first and
   commits the manifest; nothing was built. The two steps, if you prefer to run them yourself:
```

```powershell
dotnet new tool-manifest
dotnet tool install vpk --version 1.2.161
```

```bash
dotnet new tool-manifest
dotnet tool install vpk --version 1.2.161
```

```
Claude: Instalo o vpk 1.2.161 neste projeto? Ele cria o manifesto de ferramentas (dotnet-tools.json), que eu commito e envio na main.
> sim
Claude: Instalado, commitado (build(release): vpk local tool for code signing) e enviado. Plano de novo:
   v1.2.0, Stock.Desktop (zip, win-x64, signed: signtool); linux-x64 packed as zips, which are not signed (Windows only).
   Stock.Desktop-win-x64.zip: 41 files, every .exe and .dll Valid, signed by Stock Studio Ltd. Tagged v1.2.0.
```

As notas da v1.2.0 dizem: "The app is signed by Stock Studio Ltd. During the first weeks Windows may still show "unrecognized app" with that name: click "More info", then "Run anyway"." As notas da v1.3.0 não repetem a linha. O zip fica em `artifacts/publish/v1.2.0/`; quem envia é o dono. Se uma biblioteca tivesse ficado sem assinatura (o perfil do certificado expirou no meio, por exemplo), a publicação teria parado com `Stock.Core.dll is not validly signed (status NotSigned)` (até cinco arquivos nomeados), antes das notas e do tag; e um zip empacotado antes de o modo ser declarado é recusado no `release`, com o `.exe` principal nomeado.

### 14.58 Um teto para o mês, e um e-mail antes da fatura

A produção da Acme roda no Azure Container Apps e foi feita no bootstrap antes de existir o teto mensal; depois da atualização você acrescentou a coluna `Monthly budget` em `## Cloud accounts`, mas deixou a célula da produção em branco (sem a coluna a parada diria `has no Monthly budget column; add it after Region and ...`). O próximo publish para antes de rodar qualquer coisa:

```
> /agile:publish production
Claude: production: Deploy:Target is aca, but its ## Cloud accounts row has no Monthly budget; write a whole number (the ceiling in the subscription's billing currency) or none — nothing ran
```

Você escreve `80 USD` na célula. O teto é um número, e a moeda é a da própria assinatura, então para de novo, nomeando a célula e o valor:

```
> /agile:publish production
Claude: production: Monthly budget "80 USD" is not a whole number or none — nothing ran
```

Você escreve `80` e digita o comando de novo. O plano acrescenta a linha depois de `Target:`:

```
> /agile:publish production
Claude: production: v0.5.0 (release, then deploy) · client Acme · azure · login folder .agile/azure/1111.../2222...
        Target: aca (Azure Container Apps), resource group rg-acme-prod, region brazilsouth
        Budget: 80 per month on rg-acme-prod (billing currency of the subscription), alerts at 80 % and 100 % actual and 100 % forecast to Owner and Contributor
```

Depois do comando de deploy o relatório diz o que foi escrito: `Budget agile-monthly-ceiling on rg-acme-prod: created, 80 USD per month from 2026-10-01`. O administrador da Acme (Owner) e você (Contributor, convidado no tenant deles) passam a receber o e-mail do Azure quando o gasto do mês chega a 80 % ou 100 % de 80 USD, ou quando a previsão diz que vai passar de 100 %. Nada é parado quando ele dispara; decidir o que fazer é seu. Em novembro o teto sobe para 120: você muda a célula e publica de novo, e o relatório diz `updated, 120 USD per month (from 2026-10-01, unchanged)`, porque o Azure guarda a data de início de um orçamento.

Quando o Azure recusa a escrita, o deploy e a release continuam valendo e a execução diz isso depois deles, com saída diferente de zero:

```
Claude: deploy ok; budget not written: Start date of budgets cannot be updated. Please delete and create a new budget.
```

Quando a produção não precisa mais de alerta você escreve `none`: nada é escrito nem apagado, e o plano nomeia o orçamento que continua lá: `a budget agile-monthly-ceiling still exists on rg-acme-prod; delete it by hand if the environment should have none`. Quando o ambiente acaba de vez, você apaga o resource group dele e depois o orçamento, porque o orçamento sobrevive ao grupo. O comando do orçamento é:

```powershell
az rest --method delete --url "/subscriptions/<subscription>/resourceGroups/<group>/providers/Microsoft.Consumption/budgets/agile-monthly-ceiling?api-version=2023-11-01"
```

```bash
az rest --method delete --url "/subscriptions/<subscription>/resourceGroups/<group>/providers/Microsoft.Consumption/budgets/agile-monthly-ceiling?api-version=2023-11-01"
```

### 14.59 Estacionando o staging entre as janelas de teste

O staging da Acme roda no Azure Container Apps, com um servidor PostgreSQL e um host de trabalho que envia e-mail (mínimo de 1 réplica). A janela de teste acabou na sexta, então você o estaciona. O Claude mostra o que mudaria e pergunta:

```
> /agile:publish staging --park
Claude: staging (rg-acme-staging):
        api: minimum 1 -> 0
        web: already 0
        postgres-xyz: Ready -> Stopped (mark agile-parked)
        Park staging now? (yes runs these changes)
Você: sim
Claude: Leva cerca de 5 minutos (a parada do servidor é a parte lenta).
        staging (rg-acme-staging): api minimum 1 -> 0; web already 0; postgres-xyz stopped (5 min 7 s). Azure starts postgres-xyz by itself on 2026-10-13 unless it is resumed first.
```

O registro de imagens, o workspace de logs e o armazenamento continuam custando, cerca de 8,7 por mês em USD mais o que o Log Analytics ingere; a computação do banco e o host de trabalho, cerca de 24 por mês juntos, param. Qualquer resposta que não seja sim e nada roda.

Na terça a próxima versão vai para o staging ainda estacionado. Você digita o comando de sempre; o plano diz o que acontece primeiro, e o deploy faz:

```
> /agile:publish staging
Claude: staging: v0.6.0 (release, then deploy) · client Acme · azure · login folder .agile/azure/1111.../2222...
        Target: aca (Azure Container Apps), resource group rg-acme-staging, region brazilsouth
        staging is parked: the deploy starts PostgreSQL postgres-xyz first (about 3 to 4 min)
        ...
Claude: staging was parked: PostgreSQL postgres-xyz started (3 min 27 s) before the deploy
        v0.6.0 deployed to staging ...
```

Implantar sobre um servidor parado teria falhado pela metade (o Azure recusa mudar um servidor PostgreSQL parado), então o início vem primeiro e a marca é tirada; os apps recebem os mínimos do AppHost pelo deploy. O pipeline faz o mesmo quando uma tag enviada o dispara. Você nunca digita um sim para isso: digitar `/agile:publish` já era a autorização, e o plano avisou.

Duas semanas depois você estaciona de novo e volta a ele para uma demonstração. O Azure tinha iniciado o servidor sozinho depois de 7 dias; a marca `agile-parked` continuava lá, então o segundo park o para outra vez. Para a demonstração:

```
> /agile:publish staging --resume
Claude: staging (rg-acme-staging):
        postgres-xyz: Stopped -> Ready (mark agile-parked removed)
        api: minimum 0 -> 1
        web: left at 0 (no agile-min-replicas tag)
        Resume staging now? (yes runs these changes)
Você: sim
Claude: staging: postgres-xyz started (3 min 27 s), mark removed; api minimum 0 -> 1; web left at 0 (no agile-min-replicas tag); Check URL https://staging.acme.example/health answered 200 in 12 s.
```

O `web` fica em 0 porque nunca foi mudado: só um app que o park mudou carrega a tag que o resume lê. Quando o Azure responde que um app ou o servidor está ocupado, o comando espera e tenta de novo por 5 minutos, depois para com `api: (ContainerAppOperationInProgress) ... (still busy after 5 min 0 s); already changed: nothing`; rodar de novo é seguro. Outro ambiente (um `compose`, ou uma linha AWS ou Google Cloud) para com `staging: --park works only on an aca environment (Deploy:Target is compose); nothing ran`.

### 14.60 Quanto o staging já gastou neste mês?

O staging da Acme tem um teto de 80 por mês na linha de `## Cloud accounts`, e o Azure manda um e-mail aos 80 % e aos 100 %. No décimo dia você não quer esperar o e-mail; quer o número:

```
> /agile:publish staging --cost
Claude: staging (aca, rg-acme-staging): Spend this month: 42.10 BRL of 80 (52 %) — figures from Azure Cost Management lag 8 to 24 hours
```

A moeda é a que o Azure usa para cobrar a assinatura, nunca presumida pelo plugin; 52 % é 42,10 de 80, arredondado para baixo. Os valores atrasam de 8 a 24 horas, então o que você gastou hoje cedo ainda não entrou, e um grupo publicado hoje lê `0.00`. Nada foi escrito: nenhuma tag, nenhum arquivo, nenhum commit. O próximo deploy mostra a mesma leitura no plano, depois da linha do orçamento:

```
> /agile:publish staging
Claude: staging: v0.6.0 (release, then deploy) · client Acme · azure · login folder .agile/azure/1111.../2222...
        Target: aca (Azure Container Apps), resource group rg-acme-staging, region brazilsouth
        Budget: 80 per month on rg-acme-staging (billing currency of the subscription), alerts at 80 % and 100 % actual and 100 % forecast to Owner and Contributor
        Spend: 42.10 BRL of 80 (52 %) — figures from Azure Cost Management lag 8 to 24 hours
        ...
```

O Azure Cost Management responde só a algumas consultas por minuto. Se você perguntar de novo logo em seguida, o comando espera e tenta até mais três vezes, e depois diz o que houve em vez de falhar em silêncio:

```
> /agile:publish staging --cost
Claude: staging: Azure Cost Management is rate-limited; try again in a minute
```

Num plano a mesma falha nunca para a release: a linha lê `Spend: not read (Azure Cost Management is rate-limited; try again in a minute)` e o deploy segue. Um ambiente `compose`, uma linha de AWS ou de Google Cloud, ou uma linha `aca` com Resource group em branco é recusado antes de qualquer pergunta ao Azure (`staging is not an aca environment (Deploy:Target is compose) — nothing ran`).

### 14.61 Em que item eu estou?

Você digita `/agile:refine F-12`, pensando na exportação em CSV. A primeira linha que o Claude escreve mostra o que esse id é, antes de qualquer outra checagem:

```
> /agile:refine F-12
Claude: F-12 — Export the monthly report as CSV [idea]
        No similar item (14 items read).
        Worktree of F-12: D:\wt\app\f-12-export-the-monthly [feature/F-12] (created now)
```

Se você queria outro item, a linha é a chance de parar antes de existir branch e worktree. Depois da aprovação, o build começa do mesmo jeito, e a linha agora vem da cópia do arquivo na worktree:

```
> /agile:build F-12
Claude: F-12 — Export the monthly report as CSV [approved]
        Worktree of F-12: D:\wt\app\f-12-export-the-monthly [feature/F-12]
```

Um id sem arquivo avisa, e o comando segue com a própria regra para item inexistente:

```
> /agile:refine F-999
Claude: F-999 — no item file found
        There is no item F-999. Nothing was created.
```

Digitar `/agile:ship F-13` para um item ainda `building` imprime `F-13 — Import the exam list [building]` acima da parada por status errado. O `/agile:autopilot F-14` imprime a linha uma vez no início, e a refinação, o build e o ship que ele executa não repetem.

### 14.62 Um passo do roteiro de validação ficou errado

O F-12 está `validating`. No passo 3 você descobre que o comando precisa de uma flag que o roteiro não deu. O Claude corrige o código, mas o roteiro ainda traz o comando antigo. Você digita o que mudou:

```
> /agile:script F-12 step 3 now needs --no-build
Claude: F-12 — Export the monthly report as CSV [validating]
        Step 3 changed: both forms run (Git Bash and PowerShell 7), the output quoted is the real one.
        Steps 1, 2, 4, 5 are unchanged. Commit: docs(F-12): validation script. The issue body was sent whole and read back: same.
        You repeat the script from step 3.
```

O arquivo ganhou o texto novo da seção e um commit na branch do item. Não há nota de mudança, nem `version` nova, nem reaprovação, e o status continua `validating`. Sem texto depois do id (`/agile:script F-12`), o roteiro inteiro é refeito a partir dos critérios e do diff da branch.

Se o seu texto muda de verdade o que um critério diz, nada é gravado:

```
> /agile:script F-12 export only the last 3 months
Claude: F-12 — Export the monthly report as CSV [validating]
        That changes AC2 ("the file holds every month of the year"), not only how it is checked.
        Nothing written. Type: /agile:change F-12 export only the last 3 months
```

Um item `approved` ou `done` para na hora, com uma linha dizendo por quê: o roteiro é escrito no build, e um item done nunca é reaberto.

### 14.63 Quem mudou aquele produto?

O app mobile da Acme tem backoffice da equipe desde a 0.9.0 e nenhuma auditoria. No próximo sync depois da atualização do plugin:

```
> /agile:sync
Claude: Capacidade que falta: Staff audit log (desde 0.18.0): quem mudou o quê no backoffice, na mesma transação,
   lido em /backoffice/audit. Item a capturar: "Backoffice audit", que constrói "Audit log" de "Staff backoffice" em
   profiles/mobile.md e escreve "- Backoffice audit: on" no CLAUDE.md. Nada foi escrito.
   Capturo? Para recusar de vez, ponha "- Backoffice audit: none" em Profile: no CLAUDE.md e o aviso para.
```

Um projeto novo não precisa disso: com `- Backoffice: staff` no bootstrap, a ideia "Backoffice foundation" que ele captura já inclui o registro de auditoria, sem pergunta extra.

Com o item construído, um membro da equipe altera o preço de um produto em `/backoffice/products`, e um membro com o papel `Admin` abre `/backoffice/audit`:

```
hora (UTC)        membro            ação     entidade  id   mudanças
2026-10-12 14:03  ana@acme.test     update   Product   41   Price: 19.90 → 24.90
2026-10-12 13:58  ana@acme.test     update   Product   41   ContactEmail: *** → ***
2026-10-12 09:10  rui@acme.test     invite             maya@acme.test
```

A segunda linha diz que o e-mail do fornecedor mudou e nunca qual era. A terceira é uma ação da equipe, registrada pelo nome com o alvo. Um membro com o papel `Staff` que abra a mesma página recebe "negado".

### 14.64 Colocando o staging no domínio do cliente

A Acme quer o staging em `https://staging.acme.com.br`. Você escreve isso como a URL da linha `staging` do `docs/infra.md` e faz o deploy como sempre. O DNS ainda não está pronto, então o deploy roda sem o domínio e diz o que pedir:

```
> /agile:publish staging
Claude: staging: v0.6.1 (deploy) · client Acme · azure · login folder .agile/azure/1111.../2222...
        Domain staging.acme.com.br: not bound yet; the client creates these records at their DNS provider, then deploy again:
          CNAME staging → web.happyfield-1a2b3c4d.brazilsouth.azurecontainerapps.io (found: nothing)
          TXT asuid.staging → 8F3C...D210 (found: nothing)
        v0.6.1 deployed to staging; https://web.happyfield-1a2b3c4d.brazilsouth.azurecontainerapps.io/health answered 200 in 9 s.
```

O app continua respondendo no endereço gerado. Você manda as duas linhas ao administrador da Acme; o `CNAME` precisa apontar direto para esse endereço (um proxy como a nuvem laranja do Cloudflare no meio bloquearia o certificado), e se a zona tem um registro CAA ele precisa permitir `digicert.com`. No dia seguinte os registros existem; o próximo deploy os encontra, adiciona o domínio, manda o Azure emitir o certificado e o vincula, sem nenhum passo no portal:

```
> /agile:publish staging --plan
Claude: ...
        Domain staging.acme.com.br: the deploy adds it, creates certificate mc-staging-acme-com-br (validation CNAME) and binds it
> /agile:publish staging
Claude: Domain staging.acme.com.br: added, certificate mc-staging-acme-com-br issued in 94 s and bound; https://staging.acme.com.br is live.
        v0.6.2 deployed to staging; https://staging.acme.com.br/health answered 200 in 3 s.
```

Todo deploy seguinte, seu ou do pipeline, diz `Domain staging.acme.com.br: certificate mc-staging-acme-com-br kept.` e nada mais muda. Se o Azure não tivesse terminado de emitir em 10 minutos, a execução terminaria com `deploy ok; certificate pending: the next deploy binds it` (saída 0), e o próximo deploy o vincularia. Um registro que a Acme mude depois (um proxy posto na frente, por exemplo) faz o próximo deploy listar esse registro com o que foi encontrado, e o domínio fica como estava: o plugin nunca desfaz um deploy.

### 14.65 Uma pergunta que não passa despercebida

Você roda o build do F-12 numa execução sem interface, onde não há a ferramenta de perguntas. O relatório vem primeiro, em texto simples; a pergunta é a última coisa da mensagem, num bloco só dela:

```
> /agile:build F-12
Claude: F-12 — Export the student list [approved]
        Worktree of F-12: D:\dev\wt\school\f-12-export-student-list [feature/F-12]
        Built: 3 files, 4 tests green (11 s). Stopped before the last step.

        > ❓ **Pergunta**
        > A exportação precisa de um pacote novo (CsvHelper 33.0.1, licença MS-PL). Posso adicionar?
        > a) Sim, adicionar o CsvHelper
        > b) Não, escrever o CSV à mão
        >
        > **Recomendação:** a) sim. **Motivo:** trata aspas e vírgulas dentro do campo, que o CSV à mão erraria.
```

Você responde "a" e o build continua. Onde a sessão tem a ferramenta de perguntas, a mesma pergunta é um cartão e o relatório acima dela é o mesmo. Duas perguntas na mesma mensagem são dois blocos, `**Pergunta 1**` e `**Pergunta 2**`; uma pergunta sem base no brief nem no código diz `**Sem recomendação:**` e o porquê.

### 14.66 Uma lição vem com nota

No fim do ship do F-12 a retro propõe três lições. Cada uma tem duas notas, `plugin` e `project`, com o motivo de cada número, e vêm ordenadas pela maior nota:

```
> /agile:ship F-12
Claude: ... Retro: 3 lições, por importância.
1. Nota de plugin → plugin: agile [generic] — nota plugin 8 / projeto 5
   plugin 8: um `grep` na saída do gate escondeu os avisos novos no F-12 e no F-9 (dois casos);
   projeto 5: voltaria a acontecer nos próximos itens deste app.
   Linha: ⏳ plugin: agile [generic] score 8/5 — save the whole gate output, never filter it.
2. Regra do projeto → .claude/rules/agile/project.md — nota plugin 1 / projeto 6
   plugin 1: hábito só deste app; projeto 6: três dos próximos cinco itens criam um endpoint.
   Linha: "Um endpoint novo ganha o teste de autorização no mesmo commit."
3. Ajuste de template → not recommended — nota plugin 2 / projeto 1
   plugin 2: a redação de um projeto só; projeto 1: custou um minuto.
Aprova?
> aprovo 1 e 2
Claude: Retro log: entrada com as três lições e suas notas; a linha ⏳ da lição 1 entra em
   "Plugin notes — status" com a nota na coluna Note (as mesmas cinco colunas). Lição 3 de fora.
```

A nota de plugin da lição 1 é 8 porque cita dois casos. Só com o do F-12 e sem um segundo caso ela pararia em 6 e diria `no evidence yet`: um palpite não vira 8. As notas recomendam e nunca decidem: você aprovou a 1 e a 2, e uma lição `not recommended` aparece, nunca fica escondida, então você ainda pode escolhê-la. A sessão do plugin agile lê a nota ao recolher as notas, e nada mais a lê.

### 14.67 O banco ainda não mandou a chave

O F-12 está `building` e espera a chave de sandbox do banco. Em vez de deixá-lo segurando a vaga:

```
> /agile:change F-12 --block "espera a chave de sandbox do banco"
Claude: F-12 — Bank sync [building]
        Quem destrava? (só o motivo não basta)
> o banco
Claude: F-12 está bloqueado: status: blocked, "Blocked (2026-10-06): waits on the bank's sandbox key — unblocked by the bank — returns to building",
        uma linha no ## Decisions, label `blocked` no issue (o campo Status continua In progress).
        Nada foi commitado: o arquivo fica na worktree até o próximo commit; o /agile:pause faz o commit wip do código pela metade.
```

A vaga está livre, então o F-13 começa na hora com `/agile:build F-13`. Digitar `/agile:build F-12` nesse meio tempo para:

```
> /agile:build F-12
Claude: F-12 — Bank sync [blocked]
        F-12 is blocked (waits on the bank's sandbox key), unblocked by the bank: /agile:change F-12 --unblock
```

O `/agile:status` o lista numa linha própria, `Blocked: F-12 Bank sync — waits on the bank's sandbox key — unblocked by the bank — returns to building`, separada de `Waits on:`, que continua sendo o que o `## Start` diz que o item espera. Quando a chave chega com o F-13 em `building`:

```
> /agile:change F-12 --unblock
Claude: F-12 pode voltar a `building`, mas o F-13 está em build neste checkout. Nada mudou.
        Termine o F-13, ou digite /agile:change F-12 --unblock --worktree para rodar os dois em paralelo.
```

Com a vaga livre (ou com `--worktree`), o `--unblock` põe `status: building`, remove a linha `Blocked` e o label e grava `2026-10-09: blocked (waits on the bank's sandbox key), unblocked (the key arrived)` no `## Decisions`. `/agile:change F-12 --block "depois"` num `idea`, num item `done` ou já bloqueado é recusado com o status dele.

### 14.68 A conversa foi compactada no meio do build

O F-13 está em `building`. Ontem você parou com `/agile:pause`, então o arquivo do item tem a seção `## Paused`. Hoje a sessão está longa e o Claude Code compacta a conversa:

```
> /compact
(o Claude Code compacta; o hook acrescenta ao que o resumo mantém:)
Keep in the summary (agile@canary), word for word: the item in progress and its next step.
Resume F-13 Exam board registration — building [feature/F-13] D:\wt\examprep\f-13-exam-board
  Paused: 2026-10-07: stopped at the endpoint tests, wip commit 4e1c2aa
  Next: write the 409 test for a duplicate acronym
(a sessão continua, e o início dela acrescenta:)
After a compact, resume here:
Resume F-13 Exam board registration — building [feature/F-13] D:\wt\examprep\f-13-exam-board
  Paused: 2026-10-07: stopped at the endpoint tests, wip commit 4e1c2aa
  Next: write the 409 test for a duplicate acronym
> continua
Claude: F-13 — Exam board registration [building]
        Escrevendo o teste do 409 para sigla duplicada, em D:\wt\examprep\f-13-exam-board.
```

Sem a nota `## Paused`, o próximo passo vem do status: `continue /agile:build F-13 from the last commit on feature/F-13` em `building`, ou `the owner runs the validation script of F-13, then /agile:ship F-13` em `validating`. Um início de sessão comum (não depois de uma compactação ou de um resume) mostra só as linhas de status de sempre.

### 14.69 O backup do staging restaura de verdade?

O staging da Acme roda há três semanas, e o Azure guarda o backup do banco por 7 dias, mas um backup é só uma esperança até alguém restaurá-lo. Você pede o drill:

```
> /agile:publish staging --restore-drill
Claude: Restore drill staging (aca, rg-acme-staging): PostgreSQL postgres-xyz, Standard_B1ms, 32 GB, version 16, backup retention 7 days, earliest restore point 2026-09-29 14:02 UTC
        Would create postgres-xyz-drill-2610062015 (restore to now), billed per hour until removed; the drill removes it
        psql found: the data will be read
        > ❓ **Pergunta**
        > Run the restore drill on staging now? (yes creates a PostgreSQL server billed per hour and removes it at the end)
        > a) Sim, rodar
        > b) Não, nada é criado
        >
        > **Recomendação:** a) sim. **Motivo:** é o único jeito de saber que o backup restaura antes do dia em que ele for preciso.
You:    a
```

Digitar o comando não iniciou nada: só mostrou o plano, porque o drill cria um servidor que custa dinheiro enquanto existe. Depois do seu sim o comando roda em segundo plano (uma restauração leva minutos) e então relata:

```
Claude: Source postgres-xyz: PASSED
          backup retention 7 days: the earliest point you can restore to is 2026-09-29 14:02 UTC
          restored in 7 min 12 s as postgres-xyz-drill-2610062015
          shape: Ready, version 16, Standard_B1ms, 32 GB; databases app
          database app: public.orders 1204 rows, public.products 87 rows, public.users 12 rows
          removed postgres-xyz-drill-2610062015
        Restore drill staging: PASSED (restored in 7 min 12 s, data read)
```

O servidor descartável sumiu (o Claude confere com `az postgres flexible-server list` que nenhum servidor `-drill-` sobrou no grupo), e o que você leu é evidência: as tabelas voltaram com linhas. Se o `psql` não está no seu PATH a execução ainda restaura, confere a forma e apaga, e a última linha diz isso em vez de passar em silêncio:

```
Claude: Restore drill staging: PASSED, data not read (psql not found on PATH)
```

Se algo falha, o servidor é removido do mesmo jeito e a linha diz que o backup não está provado, com o motivo do próprio Azure quando ele deu um:

```
Claude: Restore drill staging: FAILED: restore still running after 60 min; the backup is not proven
```

Um staging estacionado é recusado antes de qualquer coisa ser criada (`postgres-xyz is Stopped: run /agile:publish staging --resume first`), e um drill interrompido na semana passada deixa um servidor `postgres-xyz-drill-...` que o próximo plano lista (`Left over from an earlier drill: ..., billed per hour; the drill removes it first`) e que a próxima execução remove antes de restaurar. Se o próprio delete falha, o relatório diz isso na primeira linha, com o comando para você rodar, porque aquele servidor custa dinheiro até sumir.

### 14.70 Você escolhe a opção que o Claude não recomendou

Na rodada de refinamento do F-14, a pergunta 2 recomenda a) "Soft delete" e você responde b) "Hard delete". Depois que a rodada inteira é respondida, o Claude imprime uma tabela para essa resposta (o relatório fica fora de qualquer bloco de pergunta):

```
Claude: Avaliação: Exclusão de aluno — recomendado: Soft delete, escolhido: Hard delete
        | Critério                        | Recomendado | Escolhido | Motivo |
        | esforço agora                   | 6           | 8         | hard delete é um DELETE; soft delete pede filtro em toda consulta |
        | custo ao longo do tempo         | 7           | 4         | sem histórico, cada pedido de auditoria vira restauração de backup |
        | risco                           | 8           | 3         | brief.md:41 exige o histórico de provas do aluno; apagar perde as provas |
        | reversibilidade                 | 9           | 1         | soft delete desfaz com um UPDATE; o DELETE só volta do backup (brief.md:41) |
        | Média                           | 7.5         | 4.0       | diferença 3.5: confirmação abaixo |

        [cartão] Exclusão de aluno: a diferença é 3.5
        Trocar para a recomendada (Recommended) — Soft delete
        Manter a escolha — Hard delete
```

Você escolhe "Manter a escolha". Sua resposta vale, e o `## Decisions` do F-14 ganha uma linha:

```
2026-10-07: Exclusão de aluno — owner chose "Hard delete" over the recommended "Soft delete" (average 4.0 vs 7.5); kept — the school never needs the history
```

Com diferença abaixo de 2,0 não há cartão: a tabela termina com `Diferença abaixo de 2.0: a escolha fica.` Resposta recomendada, pergunta com `Sem recomendação`, pergunta de múltipla escolha e `--assume` não imprimem tabela.

### 14.71 O Claude te dá um comando para rodar

O `/agile:version` descobre que as cópias do projeto acme-shop estão mais velhas que o plugin, e o único passo seu é um comando num terminal. O Claude o escreve em dois blocos, PowerShell primeiro e Git Bash depois, um comando por linha, nunca como uma linha dentro de uma frase:

```
Claude: acme-shop: cópias da 0.24.0, plugin na 0.25.0. Atualize o plugin num terminal, abra uma sessão nova e rode /agile:sync.
```

```powershell
claude plugin marketplace update canary
claude plugin update agile@canary
```

```bash
claude plugin marketplace update canary
claude plugin update agile@canary
```

Um passo que precisa de um valor seu usa um placeholder como `<subscription>`, ou um valor inventado como `F-12` num exemplo; a mesma regra vale para os manuais, o README e os templates copiados para o seu projeto. O que o próprio Claude executa, como `git status` no meio de um passo, fica na frase: você não copia dali.


### 14.72 O scan de segurança: a primeira triagem e um ship bloqueado

O HR-Portal é um projeto mais antigo. No fim do build do F-21, antes do script de validação, o Claude roda o plano do scan e depois o scan.

```
Claude: Security scan: dotnet (native), gitleaks v8.30.1 (docker, image not present, 77 MB), semgrep 1.179.0 (docker, image not present, 1.56 GB)
        [card] Baixar as duas imagens (1,6 GB, uma vez)?
> sim
Claude: dotnet: 1 package with 1 advisory
        gitleaks: tree 0, history 214 commits read, 1 finding
        semgrep: 2 findings
        | id       | severity | where                          | rule                |
        | 3f9a12c0 | critical | src/Legacy/Mail.cs:14 @a41c9e2 | aws-access-token    |
        | 9be40d17 | high     | Newtonsoft.Json@9.0.1          | GHSA-5crp-9r3c-p9vr |
        | c27d5e88 | high     | src/Reports/Query.cs:12        | csharp-sqli         |
        | 70aa31f4 | medium   | src/Reports/Hash.cs:5          | warn-only           |
        Triaged: none
        agile scan BLOCKED: 1 critical, 2 high open
```

Nada é aceito sozinho: o Claude pergunta de cada achado, um por vez. Para o segredo, achado só num commit antigo, o card diz que apagar a linha o deixa em todo clone; você responde que a chave foi trocada hoje de manhã, e o Claude registra a linha como fixed com o motivo "rotated". O pacote é corrigido como um bug: um teste que lê a versão resolvida falha, a versão sobe, o novo scan não o lista mais, a suíte fica verde e o agente `reviewer` confirma o diff. A consulta vai para um card (corrigir agora, aceitar com motivo, falso positivo com motivo) e você escolhe "corrigir agora". No ship o scan roda de novo:

```
Claude: dotnet: 0 packages with 0 advisories
        gitleaks: tree 0, history 216 commits read, 0 findings
        semgrep: 1 finding
        | 5d01c3aa | high | src/Reports/Export.cs:30 | insecure-binaryformatter-deserialization |
        Triaged: 3 fixed
        agile scan BLOCKED: 0 critical, 1 high open
        Ship parado: um novo achado high (src/Reports/Export.cs:30, nunca o valor). [card] Corrigir · Aceitar com motivo
```

Sem o Docker rodando, o mesmo passo termina com `agile scan PARTIAL: gitleaks, semgrep did not run (docker daemon not running)` e um card pergunta se segue; a resposta vai para o `## Decisions` do item.


### 14.73 O staging dorme de noite sozinho

Você cansou de digitar `--park` na sexta à noite e `--resume` na segunda de manhã. No `docs/infra.md` da Acme você escreve, em `## Schedule`:

```
| Environment | Action | When | Timezone |
|---|---|---|---|
| staging | park | 0 20 * * 1-5 | America/Sao_Paulo |
| staging | resume | 0 8 * * 1-5 | America/Sao_Paulo |
| staging | re-park | 0 3 * * * | America/Sao_Paulo |
```

Depois, na raiz do projeto, você olha o que a tabela gera e escreve (o `sync.js` do plugin, na pasta da versão instalada):

```powershell
node "$env:USERPROFILE\.claude\plugins\cache\canary\agile\<versão>\scripts\sync.js" schedule
node "$env:USERPROFILE\.claude\plugins\cache\canary\agile\<versão>\scripts\sync.js" schedule write
```

```bash
node ~/.claude/plugins/cache/canary/agile/<versão>/scripts/sync.js schedule
node ~/.claude/plugins/cache/canary/agile/<versão>/scripts/sync.js schedule write
```

e eles imprimem:

```
Schedule (docs/infra.md): 3 rows, 1 environment (staging)
.github/workflows/park.yml: missing (run node sync.js schedule write)
wrote .github/workflows/park.yml (staging: park 0 20 * * 1-5, resume 0 8 * * 1-5, re-park 0 3 * * *, America/Sao_Paulo) and .github/scripts/agile-park.js
Next: add main to the deployment branches of the GitHub environment staging (Settings > Environments); the tag rule v* stays for the deploy
```

Você faz o commit dos dois arquivos, acrescenta `main` aos branches do environment `staging` no GitHub, e é só. Às 20:00 de sexta a página da execução no GitHub mostra (o plano primeiro, depois o que aconteceu; nada foi perguntado, a linha era o sim):

```
## Parked staging
staging (rg-acme-staging): api minimum 1 -> 0; web already 0; postgres-xyz stopped (5 min 7 s). Azure starts postgres-xyz by itself on 2026-10-16 unless it is resumed first.
Azure: Acme's subscription Acme staging (2222...), tenant 1111...; removed from the environment: nothing.
Scheduled park (0 20 * * 1-5, America/Sao_Paulo). Plan:
- api: minimum 1 -> 0
- web: already 0
- postgres-xyz: Ready -> Stopped (mark agile-parked)
```

Às 03:00 de toda noite a linha `re-park` olha o servidor: na sexta da outra semana o Azure o iniciou sozinho depois dos 7 dias, e a execução diz `PostgreSQL postgres-xyz re-parked (4 min 58 s); Azure starts it by itself on 2026-10-23 unless it is resumed first`. Se você tivesse retomado o staging na quinta para um teste tarde, a marca já tinha sumido e a execução diz `postgres-xyz: Ready, no agile-parked mark; left as it is`. Um horário digitado como `*/5` no minuto é recusado na tabela (`the minute "*/5" of "*/5 * * * *" must be a number from 0 to 59 (a list is allowed; a * or a step is not)`), e um deploy que esteja rodando às 20:00 faz o park esperar por ele. Quando uma execução não consegue terminar (um servidor que o Azure ainda responde como ocupado depois de 5 minutos), ela fica vermelha com o recurso e o que tinha mudado; a execução da noite seguinte, ou `/agile:publish staging --park`, pode ser repetida com segurança.

### 14.74 A branch principal é a `develop`

O seu projeto trabalha na `develop`, e o `CLAUDE.md` dele diz `- Main branch: `develop`.` (ou a linha do bootstrap, ``pushing or merging to `develop` ``). Um só leitor, o mesmo para o gate, o guard de commit e o `/agile:publish`, tira o nome só dessas duas linhas; uma frase como "a main branch é `develop`" não nomeia nada.

Tudo está commitado em `feature/F-6` e o Claude roda o gate à mão. Antes da 0.29.1 ele procurava só `main` ou `master` e respondia `agile gate SKIPPED: no code file changed since the last commit`. Agora:

```
agile gate (by hand): 2 code file(s) changed since the main branch `develop`
...
agile gate GREEN
```

Num projeto cujo `CLAUDE.md` não nomeia branch nenhuma, o gate segue com a `main` e avisa antes: ``main branch `main` assumed: CLAUDE.md names none``. O `/agile:publish` não adivinha, porque empurra uma tag e um release:

```
CLAUDE.md names no main branch: add the line - Main branch: `<name>`
```

e, quando as duas linhas discordam (`- Main branch: `develop`` e ``merging to `main` ``), `CLAUDE.md lines name different main branches (`develop` in the Main branch line, `main` in the merging to line): keep one`. Você corrige a linha e digita o comando de novo.

### 14.75 O sync marca notas que nunca citaram a issue e para de propor o que não é dele

O retro log do Toolbox tem notas ⏳ que o plugin entregou há tempo, nenhuma citando uma issue. A partir da 0.29.2 o plugin carrega um mapa do texto de uma nota para a sua issue, e a linha diz como cada uma foi achada:

```
> /agile:sync
Claude: agile@canary 0.29.1 → 0.29.2.
| Arquivo / item                   | Estado  | Proposta                                                                 |
| docs/agile/workflow.md           | edited  | merge: o plugin mudou a seção do sync; a sua edição está em outro ponto    |
| Directory.Build.props            | same    | (o <Version> 0.4.0 é seu; o 0.1.0 do template não é proposto)             |
| notas de plugin entregues        | 3 linhas | marcar ✅: linha 13 #21 (mapped), linha 15 #27 (mapped), linha 16 #29 (mapped) |
| catálogo de tools do DocGen      | -       | não oferecido: o docgen.json diz "tools": false                           |
> ok
Claude: Mesclei o docs/agile/workflow.md com o texto do plugin como o sync o copia: sem conflitos, os links do cabeçalho continuam apontando para o workflow.pt-BR.md.
Notas: marquei as linhas 13, 15 e 16, agile@canary 0.29.2.
Commit "chore: sync with agile@canary 0.29.2" na main. Autoriza?
```

Antes disso, o merge à mão trazia `../pt-BR/MANUAL.md` de volta ao cabeçalho, o sync propunha o `<Version>0.1.0</Version>` do template sobre o seu `0.4.0`, e o catálogo era oferecido de novo a cada sync. Um projeto que deixa `tools` fora do `docgen.json` continua recebendo a oferta do catálogo, com a linha que a recusa: ponha `"tools": false` no `docgen.json`. Um `docgen.json` que não pode ser lido é citado no relatório e nada é oferecido até você consertar.

### 14.76 A tela volta do agente com os testes conferidos, não confiados

A tela de blocos de texto do Toolbox troca um componente e muda o que um teste antigo afirma. O plano que o Claude escreveu no início do build listava `SectionOrnament` como removido e reservava uma rodada de conserto. O Claude removeu o tipo, com os dois testes dele atualizados antes, e só então chamou o agente `frontend` em primeiro plano.

```
Claude: Plano: 1 remover SectionOrnament (os 2 testes antes); 2 passar a tela ao frontend; 3 suíte inteira; 4 uma rodada de conserto; 5 cobertura; 6 roteiro de validação.
Relatório do agente: Nothing here was built or run.
  Existing tests that assert what I changed: TextsPageTests.ShowsBlocks (Texts.Tests): assertion changed under AC3.
  Removals for the session: none.
  Assumptions I could not verify: the new block card keeps the data-testid "block-card".
Claude: node gate.js ship, antes de ler os arquivos dele: 76 passaram, 21 falharam (2 padrões: o testid do card, o texto do estado vazio).
Claude: Li os 9 arquivos citados; a primeira suposição estava certa (testid "block-card"), a segunda não. Consertei as duas; suíte de novo: 97 passaram, 0 falharam. agile gate GREEN.
```

O relatório do build fecha com um bloco:

```
Frontend hand-off: first suite 76 passed, 21 failed; after the fix round 97 passed. Removed: SectionOrnament (2 tests updated first). Assertions changed: TextsPageTests.ShowsBlocks (AC3).
```

Antes, o agente podia ser chamado em segundo plano e o gate compilava arquivos pela metade; um tipo removido ficava como toco só de comentário; com apenas "não edite testes existentes", ele deixava de fazer uma conversão necessária; e as falhas só apareciam quando a primeira execução dos testes vinha depois de ler tudo.

### 14.77 A senha do banco para de mudar a cada deploy

O shop da Acme fez deploy do staging duas vezes com a receita antiga, e o segundo deixou o app sem conseguir entrar no banco. Depois da atualização do plugin o sync aponta a causa; ele não altera nada, porque o AppHost é código do projeto.

```
> /agile:sync
Claude: agile@canary 0.30.0 → 0.30.1.
Claude: Capacidade que falta: Database password (desde 0.30.1): o AppHost declara um contêiner de banco sem parâmetro de
   senha, então cada `aspire deploy` de um ambiente compose troca a senha e o app não entra a partir do segundo deploy.
   src/Shop.AppHost/Program.cs declara AddPostgres("postgres") para o ambiente compose Staging. Um ambiente já implantado
   mantém a senha do primeiro deploy: use-a como valor de Parameters__dbpassword se ainda souber qual é; senão o volume
   do banco precisa ser apagado (os dados se perdem); o sync não altera o AppHost.
   Item a capturar: um bug "A senha do banco muda a cada deploy". Nada foi escrito. Capturar?
Você: sim
Claude: Capturado como B-14, pronto para refinar.
```

O bug é construído no repositório do shop a partir das linhas do perfil: `AddPostgres("postgres", password: ...)` com o parâmetro secreto `dbpassword` só no modo publish, e a linha `Parameters__dbpassword` em `## Expected secrets`. O dono da Acme gera um valor com o par de comandos do texto da receita acima e o guarda no ambiente `Staging` do GitHub. Enquanto a variável não está definida, o deploy para pelo nome:

```
> /agile:publish staging
Claude: the environment variable Parameters__dbpassword is not set: set it outside this chat (the value is never read, printed or asked for), then run the command again; nothing ran
```

Com a variável definida no shell que iniciou o Claude, dois deploys seguidos deixam o mesmo contêiner rodando e a mesma senha entrando; os dados criados depois do primeiro continuam lá. A primeira senha do ambiente antigo tinha se perdido, então a Acme apagou o volume do staging uma vez, à mão, e recomeçou de um banco vazio.

### 14.78 Uma pasta presa para a remoção, e o relatório diz quem segura

O dono da Acme validou a F-21 e digitou o ship com uma janela do Visual Studio ainda aberta na worktree. O merge está feito; o script que remove a worktree para na sonda, antes de remover qualquer coisa:

```
> /agile:ship F-21
Claude: Merge de feature/F-21 em main feito (--no-ff), push feito; main...origin/main 0 0.
Claude: A worktree não foi removida. scripts/worktree.js parou na sonda, nada renomeado nem removido:
   Não consegui renomear D:\wt\shop\f-21-order-export (EBUSY): algo segura a pasta.
   Responsáveis (melhor esforço):
     process 18244 devenv.exe: "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe" D:\wt\shop\f-21-order-export\Shop.slnx
     Há uma pasta .vs dentro da worktree: o Visual Studio pode estar segurando.
   agile worktree HELD: D:\wt\shop\f-21-order-export
   Feche o Visual Studio nessa pasta e me avise; rodo o mesmo comando de novo.
> fechado
Claude: Sonda passou; worktree removida e podada (REMOVED). Branch apagada localmente (remoto: nunca enviada).
```

Quando o relatório não acha ninguém, ele diz isso e nomeia a ferramenta que acha: um programa que só tem a pasta como diretório de trabalho (um terminal com `cd` nela) só aparece por handle aberto, e o `handle.exe` da Sysinternals o encontra depois que você o rodou à mão e aceitou a licença; o plugin nunca aceita uma licença por você.

Uma remoção que falhou no meio antes da 0.30.2 deixa uma órfã, uma pasta que o git não lista mais. O script avisa e não apaga nada; a pasta só é removida depois do seu sim:

```
> /agile:ship F-22
Claude: Merge de feature/F-22 em main feito. A pasta D:\wt\shop\f-22-invoice-pdf não é uma worktree registrada mas continua no disco
   (uma remoção anterior falhou no meio). Sonda passou. 3 entradas no topo, 41 arquivos: src, tests, Shop.slnx.
   agile worktree ORPHAN: D:\wt\shop\f-22-invoice-pdf
   Nada foi apagado. Ela só tem o que já foi mesclado. Apagar a pasta?
> sim
Claude: Pasta apagada (REMOVED). Branch apagada localmente (remoto: nunca enviada).
```

### 14.79 O gate espera a sua vez e guarda o que falhou

A sessão do Simulab inicia `gate.js ship` para a F-28 e, enquanto a suíte roda, o turno termina. O hook Stop encontra a trava que o ship segura e não compila ao lado dele:

```
[agile gate]
agile gate deferred: ship (pid 4812) holds D:\dev\wt\simulab\f-28-exam-timer since 2026-10-08T14:02:11.402Z; if that pid is not a gate run, delete D:\dev\simulab\.git\worktrees\f-28-exam-timer\agile-gate.lock; the marked files build on the next turn
```

O ship termina vermelho. O relatório guarda as últimas linhas, nomeia os testes que falharam e, como a branch mudou só `Simulab.Exams`, diz onde a falha não está:

```
tests failed: Simulab.slnx
  Failed Fails_when_the_vocabulary_is_missing [12 ms]
  ...
every failing test is in tests/Simulab.Reports.Tests/Simulab.Reports.Tests.csproj, which reaches none of the projects this branch changed (src/Simulab.Exams/Simulab.Exams.csproj): probably not this item's; run it on the main branch to confirm
full output: C:\Users\me\AppData\Local\Temp\agile-canary\gate-logs\20261008-140433-210-ship.log
agile gate RED: tests failed (Simulab.slnx): Fails_when_the_vocabulary_is_missing — full output: C:\Users\me\AppData\Local\Temp\agile-canary\gate-logs\20261008-140433-210-ship.log
Claude: O teste que falhou está em Reports, que a F-28 não tocou. Rodei na main: falha lá também, então não é deste item. Mas o ship exige uma rodada verde e esta está vermelha, então paro aqui: conserto o Reports primeiro (uma mudança pequena num item próprio), ou espero quem cuida dele?
```

O arquivo tem a mensagem e o stack de todo teste que falhou, então um teste que falha uma vez e passa na rodada seguinte deixa o seu rastro. Antes disso, o ship em segundo plano e o hook Stop compilavam as mesmas pastas `obj/` e um deles falhava com `CS2012`, a dica culpava um app host que não estava rodando, e uma falha em outro projeto parecia ser do próprio item.

Em outro turno o Claude edita `docs/glossary.md` para a F-29: o projeto de teste que lê `glossary.md` roda ao fim desse turno; uma anotação que ele acrescenta em `docs/notes.md`, que nenhum teste nomeia, não roda nada e não imprime nada. Uma mudança feita fora das ferramentas de edição do Claude é vista pelo `gate.js stop` rodado à mão, que pergunta ao git, e pelo ship.

### 14.80 Um staging que faz deploy sem o Aspire

A loja da Acme é desenvolvida com o Aspire pelo dashboard, mas o host de staging deve rodar só as imagens da loja e um arquivo compose que o dono consiga ler. Na rodada 6 do bootstrap, depois da conta de nuvem de cada ambiente (`none`, um host da própria Acme):

```
Claude: Pergunta 25c, staging (compose no seu próprio host): com ou sem Aspire?
        a) com Aspire: aspire deploy, o AppHost descreve o ambiente
        b) sem Aspire: as imagens da própria loja e o deploy/compose.yaml; o AppHost continua uma ferramenta local
        Recomendação: a) com Aspire. Motivo: é o comportamento de hoje e não exige escrever mais nada.
Você: b.
Claude: staging faz deploy sem o Aspire. Escrito a partir da seção "Deploy recipe (containers without Aspire)" do perfil:
   docs/infra.md  Deploy command: dotnet publish src/Shop.Web/Shop.Web.csproj -c Release -t:PublishContainer && docker compose -f deploy/compose.yaml --env-file deploy/staging.env up -d --wait
                  Expected secrets: DB_PASSWORD (variável de ambiente)
   deploy/compose.yaml   postgres:17 com volume de dados, o serviço web em shop-web:${AGILE_VERSION}, um volume de chaves
   deploy/staging.env    ASPNETCORE_ENVIRONMENT=Staging, HOST_PORT=5081 (a porta da URL)
   src/Shop.Web/Shop.Web.csproj   ContainerRepository shop-web, ContainerImageTag $(Version)
   O AppHost não foi alterado.
```

Você gera um `DB_PASSWORD` aleatório (22 ou mais letras e dígitos) e o guarda no shell que inicia o Claude. Antes de defini-lo, o comando para e nada roda:

```
> /agile:publish staging
Claude: the environment variable DB_PASSWORD is not set: set it outside this chat (the value is never read, printed or asked for), then run the command again; nothing ran
```

Com a variável definida, o primeiro deploy publica `shop-web:0.1.0` e sobe os dois contêineres. Você abre o site, entra e guarda o cookie. A release `v0.2.0` faz deploy do mesmo jeito, com a mesma senha:

```
> /agile:publish staging
Claude: staging: v0.2.0 deployed · previous v0.1.0 · check http://localhost:5081/health 200 in 0.3 s
```

O contêiner web agora é `shop-web:0.2.0`, o contêiner do banco manteve os dados e a senha, e o cookie da `v0.1.0` continua aceito, porque o volume das chaves não foi tocado. `docker compose logs` no serviço web mostra o log das requisições; não há contêiner de dashboard. A produção, com o próprio `deploy/production.env` e a própria porta, roda ao lado do staging no mesmo host, com contêineres e volumes próprios.

### 14.81 Um banco que mantém o endereço, a senha e os dados

A loja da Acme é criada com Aspire e PostgreSQL. Na rodada 1, logo depois da pergunta 3, o Claude pergunta:

```
Claude: Pergunta 3a, serviços locais fixos: o banco mantém porta, senha e dados de um aspire run para o seguinte?
        a) sim: porta 5432, senha nos user secrets do AppHost, um volume de dados
        b) não: o AppHost como as receitas o escrevem (porta nova a cada execução)
        Recomendado: a) sim. Motivo: a conexão que você salva uma vez no DataGrip continua valendo e os dados sobrevivem a um reinício.
        A porta 5432 está livre nesta máquina.
Você: ok.
```

Com o esqueleto escrito, o relatório diz o que foi feito, nunca a senha:

```
Claude: Serviços locais fixos, a partir do bloco "Local services (fixed ports and passwords)" do perfil:
   Acme.AppHost/Program.cs   postgres na porta 5432 fora do modo publish, parâmetro de senha postgres-password, WithDataVolume()
   user secrets do AppHost   Parameters:postgres-password definido (aleatório, 35 caracteres; o valor não é mostrado)
   docs/infra.md   ## Local services: postgres · 5432 · Parameters:postgres-password · o volume que termina em -postgres-data
   CLAUDE.md   - Local services: fixed
   Os ambientes publicados não mudam.
```

Você lê a senha uma vez, nesta máquina, e salva a conexão do DataGrip com host `localhost`, porta 5432 e usuário `postgres`:

```powershell
dotnet user-secrets list --project src/Acme.AppHost
```

```bash
dotnet user-secrets list --project src/Acme.AppHost
```

No dia seguinte o `aspire run` sobe o banco na mesma porta, com a mesma senha e as linhas de ontem. Se quiser outra senha, troque o segredo e remova o volume citado no `docs/infra.md` (os dados dele se perdem), senão o banco recusa a nova com `password authentication failed`. Uma segunda máquina, ou um clone novo, define o seu valor com `dotnet user-secrets set` antes do primeiro `aspire run`; até lá os recursos que usam a senha ficam em `Waiting`.

Um projeto criado no mês passado tem um AppHost e nenhuma dessas linhas, então o `/agile:sync` o lista:

```
Claude: Novo nesta versão: Fixed local services (- Local services: fixed | none no CLAUDE.md).
        Item a registrar: "Fixed local services", que constrói o bloco "Local services (fixed ports and passwords)" do perfil; "- Local services: none" recusa e encerra este aviso.
```

### 14.82 Os dados do próprio item, e um agente mantido na worktree

O F-12 acrescenta uma coluna `Discount` aos pedidos da Acme, construído na worktree `D:\wt\shop\f-12-order-discount` enquanto o checkout principal fica no `main`. A primeira linha do roteiro de validação depende de onde o projeto guarda os dados locais. Três projetos, três passos de partida.

O AppHost declara `WithDataVolume()` sem nome (os perfis escrevem assim): o nome do volume carrega o caminho do AppHost, então a worktree já tem o seu próprio banco.

```
1. Inicie o app a partir da worktree: aspire run em D:\wt\shop\f-12-order-discount.
   Dados: esta worktree tem o seu próprio volume (o volume de dados do AppHost não tem nome); nada a ajustar.
   A migração do F-12 cai só ali; o banco do checkout principal não é tocado.
```

O projeto dá nome ao volume e mantém a sua própria chave em `docs/infra.md` (`Database__Name`):

```
1. Pare qualquer app host que esteja rodando e inicie o app a partir da worktree, num banco só dela:
```

```powershell
$env:Database__Name = "shop_f12"
aspire run
```

```bash
export Database__Name="shop_f12"
aspire run
```

```
   Esperado: o dashboard lista o banco shop_f12. Para repetir: as mesmas duas linhas num terminal novo.
```

O projeto não tem nenhum dos dois:

```
1. Inicie o app a partir da worktree: aspire run.
   Aviso: o banco local é compartilhado com o checkout principal; a migração do F-12 cai nele
   (registrado em ## Decisions com o seu sim de hoje).
```

Um passo que pede para você mudar código diz onde e o que falha, e um passo que compila para o host antes:

```
5. Pare o app host (Ctrl+C no terminal dele). Em src/Shop/Orders/Order.cs, dentro da classe Order, depois da
   propriedade Total, acrescente: public decimal Discount { get; init; } = -1m;
   Rode os testes: OrderTests.Discount_cannot_be_negative falha com "Discount must be zero or more".
   Remova a linha de novo.
```

No mesmo build, o agente `reviewer` uma vez buscou sem pasta. A guarda o mandou de volta:

```
[agile agent-guard] Grep from the `reviewer` agent targets the main checkout (D:/dev/shop), not an item's worktree.
The item's code and status live in its worktree: D:/wt/shop/f-12-order-discount [feature/F-12].
Repeat the call with the item's worktree (or a folder inside it) as the path; never report something as missing from a search rooted elsewhere.
Bypass, only with the owner's yes: restart Claude Code with AGILE_HOOKS=off (this turns off every agile hook).
```

O agente repetiu a busca na worktree e achou `Order.Discount`; você vê só os achados dele.
### 14.83 Quais critérios ainda não têm teste

O F-14 está em `building` e tem cinco critérios de aceite. Quando o build começa, o Claude acrescenta a seção ao arquivo da feature com todas as linhas `pending`, e cada commit que traz um teste preenche a sua linha. No meio do caminho, o arquivo diz:

```
## Criterion → test
| Criterion | Test |
| --- | --- |
| AC1 | ExamFeesPage_Open_ShowsFees |
| AC2 | pending |
| AC3 | CreateExamFee_Negative_Returns422 |
| AC4 | pending |
| AC5 | validation step 6 |
```

A conversa é compactada. O resumo guarda o bloco de retomada, que agora tem uma quarta linha:

```
Resume F-14 Exam fees — building [feature/F-14] D:/wt/examprep/f-14-exam-fees
  Paused: no pause note
  Next: continue `/agile:build F-14` from the last commit on feature/F-14 (git log in D:/wt/examprep/f-14-exam-fees)
  Criteria with no test: AC2, AC4 (2 of 5)
```

A sessão que continua vai direto ao AC2 e ao AC4, sem reler o código. A linha diz `none (5)` quando toda linha está preenchida, e `no section` para um bug ou um item feito antes da seção existir.

Depois você muda um critério no meio do build:

```
> /agile:change F-14 o AC3 também deve recusar uma taxa de zero
Claude: o AC3 muda (Dada uma taxa negativa, então 422 vira Dada uma taxa abaixo de um, então 422).
        A linha dele em ## Criterion → test volta a pending; AC1, AC2, AC4 e AC5 ficam como estão.
```

No fim do build toda linha está preenchida, e no dia do ship o Claude confere o arquivo em vez de confiar no relatório. Este ship tem uma lacuna:

```
> /agile:ship F-14
Claude: F-14 — Exam fees [validating]
        Criterion → test: parei antes do merge.
        O AC4 ainda está pending; o AC2 cita ExamFeeTests.Refund_Partial, que não está em nenhum projeto de teste.
        Acrescente os testes, ou me diga para mover um critério ao roteiro de validação (validation step <n>, registrado em ## Decisions).
```

Uma feature antiga, sem a seção, imprime `Criterion → test: no section (built before this rule)` e o ship segue; um bug não imprime nada.

### 14.84 Uma suíte que trava não deixa mais a ferramenta rodando

O repositório `legacy-shop` é `engine: msbuild`, e o `testCommand` dele roda uma suíte pelo `vstest.console.exe`. Um dia a suíte trava. No fim do ship o gate já esperou os 1800 segundos:

```
> /agile:ship F-31
Claude: F-31 — Refund by e-mail [validating]
        agile gate RED: tests timed out (vstest.console.exe LegacyShop.Tests.dll) — full output: C:/Users/you/AppData/Local/Temp/agile-canary/gate-logs/20261009-1815-ship.log
        tests ran longer than 1800 s (AGILE_TESTCMD_TIMEOUT): vstest.console.exe LegacyShop.Tests.dll
```

Antes da 0.37.0 só o shell era parado aqui: o `vstest.console.exe` continuava rodando, e quando você consertava a suíte e dava ship de novo a remoção do worktree falhava com a pasta em uso. Agora o gate encerrou a árvore inteira antes de imprimir o veredito, então a pasta está livre e o próximo ship começa limpo. Se o Windows tivesse recusado encerrar, o mesmo RED traria mais uma linha:

```
        a process the command started may still be running
```

Essa linha é o aviso para procurar no Gerenciador de Tarefas um `vstest.console.exe` ou `testhost.exe` sobrando antes de dar ship de novo.

### 14.85 Um deploy que trava é parado junto com tudo o que ele iniciou

A linha `staging` do `docs/infra.md` faz o deploy com `dotnet publish && az containerapp up ...`. Um dia o `az` trava num pedido de login. Trinta minutos depois:

```
> /agile:publish staging v0.9.0
Claude: deploy failed
        the deploy command ran longer than 1800 s (AGILE_DEPLOY_TIMEOUT) and was stopped: staging may be half deployed, check it before running again
        Output saved: C:/Users/you/AppData/Local/Temp/agile-deploy/staging-v0.9.0-2026-10-09T18-42-11-004Z.log
        Nothing was recorded and nothing was rolled back. To go back to the version recorded before: /agile:publish staging v0.8.0
```

Antes da 0.38.0 só o shell era parado: o `az` continuava rodando, segurava o worktree `deploy` e ainda podia terminar o deploy depois de a execução ter dito que falhou. Agora todo processo que o comando iniciou foi encerrado antes dessa mensagem; a pasta está livre e a próxima execução começa limpa. Parar os processos locais não desfaz o que o Azure já aceitou, e por isso a mensagem manda você olhar o ambiente primeiro. Se o Windows tivesse recusado encerrar a árvore, viria mais uma linha:

```
        a process the command started may still be running
```

Essa linha é o aviso para procurar no Gerenciador de Tarefas um `az.cmd` ou `dotnet.exe` sobrando. O limite é de 30 minutos, salvo se `AGILE_DEPLOY_TIMEOUT` (em segundos) disser outro; o orçamento de um ambiente `aca` continua sendo escrito depois de um estouro, como depois de qualquer deploy que falha.

### 14.86 O manual e seus exemplos são dois arquivos

A partir da 0.39.0 o manual guarda as regras e a referência, e os exemplos de workflow (este arquivo) ficam ao lado dele. Nada nos exemplos mudou: os mesmos números, o mesmo texto. No manual, "exemplo 14.54" é texto puro e quer dizer o exemplo com esse número aqui. O manual ficou com cerca de metade do tamanho antigo, então o que você abre para conferir uma regra é mais curto.

Um projeto que atualiza o plugin vê a divisão no próximo `/agile:sync`:

```
> /agile:sync
Claude: agile@canary 0.38.0 → 0.39.0.
| Arquivo                                 | Estado | Proposta                                                   |
| docs/agile/workflow.md                  | update | copiar: o manual, agora sem a seção 14                     |
| docs/agile/workflow.pt-BR.md            | update | copiar: o mesmo, em português                              |
| docs/agile/workflow-examples.md         | new    | copiar: os exemplos de workflow (14.1 a 14.86), com link no manual |
| docs/agile/workflow-examples.pt-BR.md   | new    | copiar: o mesmo, em português                              |
Copio os quatro?
> sim
Claude: 4 arquivos copiados. Os links entre os quatro apontam uns para os outros dentro de docs/agile.
```

Se você tivesse acrescentado um parágrafo seu ao `workflow.md`, a linha diria `edited` em vez de `update`, e o Claude mostraria a mesclagem de três vias: o seu parágrafo fica, e a seção 14 sai junto com o resto da mudança do plugin. Um exemplo novo entra no fim deste arquivo, nos dois idiomas, e o próximo número livre é o que o `plugin-version.js ship` imprime.

### 14.87 O build para antes de começar numa promessa que ninguém cumpriu

A F-12 "Monthly report screen" foi aprovada com o `## Start` dizendo que espera os wireframes que o dono ia desenhar. Ninguém os salvou ainda:

```
> /agile:build F-12
Claude: F-12 — Monthly report screen [approved]
        Worktree of F-12: D:/wt/acme-shop/f-12-monthly-report [feature/F-12]
        A F-12 não pode começar: o "Waits on (to start)" cita docs/specs/F-12-wireframes.md, e a worktree não tem esse arquivo.
        Quem resolve: você, salvando os wireframes ali. O status continua approved; nada foi escrito.
```

Antes da 0.40.0 o build marcava `building` e começava; o critério de tela da F-12 só voltaria aberto no ship. A mesma parada nomeia um item de "Depends on" que ainda não está `done`, com o status dele.

A F-14 "Download the report as CSV" foi refinada enquanto a F-13 ainda estava em build, e a BR1 dela diz que a F-13 acrescenta `ExportService.ToCsv`. A F-13 foi entregue depois, só com JSON:

```
> /agile:build F-14
Claude: F-14 — Download the report as CSV [approved]
        Worktree of F-14: D:/wt/acme-shop/f-14-csv-download [feature/F-14]
        A premissa da F-14 é falsa hoje: a BR1 diz que a F-13 acrescenta ExportService.ToCsv, e o src/Export.cs (F-13, done) só tem ToJson.
        O AC1 não pode ser cumprido como está escrito. Nada foi escrito; o status continua approved.
        Decida como a BR1 muda com /agile:change F-14
```

O build não escolhe a correção por você (escrever o método na F-14 ou esperar outro item): isso é uma nota de mudança, decisão sua, e o build recomeça depois dela. "Needed to validate" nunca para um build, e um build retomado de `building` pula essa checagem.

### 14.88 Uma tela de desktop é conferida na janela real, sem clicar por coordenada

Um item `desktop` com cabeça Avalonia chega ao fim do build. O painel do navegador não dirige um app desktop, então o passo 12 não o usa:

```
> /agile:build F-12
Claude: Conferência de tela (desktop, sessão Windows): iniciando Toolbox.Desktop pela worktree e lendo a janela pelo UI Automation.
        Janela "Conexões": filtros presentes, o DataGrid tem 0 filhos enquanto o rodapé diz "3 de 3 conexões".
        Achado: o tema do DataGrid não está incluído (avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml), a grade desenhou preta.
        Corrigido, com um teste headless que lê as linhas da grade. Tema escuro: alternado pelo botão de Configurações (UIA Invoke), mesmo resultado.
        Fora do alcance desta conferência, então vai para o seu roteiro de validação: ordem de tabulação, as setas e o Esc dentro do menu da linha.
        Processo encerrado. Nenhum mouse ou tecla foi enviado, e nada foi capturado por coordenada de tela.
```

A conferência pode apertar um botão pela árvore de automação do próprio app, porque isso não depende de qual janela está em foco. Ela nunca envia mouse ou tecla sintéticos: eles cairiam na janela da frente, em geral a sua IDE. Em Linux ou macOS, ou sem sessão de desktop, o relatório diz que a tela inteira vai para o seu roteiro de validação.

Um critério de teclado (Enter, Espaço, Alt+Seta) numa tela Avalonia vira teste headless, não fica para você; e todo componente que mostra texto do usuário ou do Core ganha um estado de texto longo: na galeria e num teste de quebra.
