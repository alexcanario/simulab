# agile@canary — Exemplos de workflow (pt-BR)

> Parte do [manual](workflow.pt-BR.md).

## 14. Exemplos de workflow

Todos os exemplos usam um app fictício, o **ExamPrep**: uma plataforma em que curadores montam um banco de questões de concursos e alunos praticam com elas. `>` marca o que você digita; `Claude:` marca a resposta (resumida). Os diálogos estão em português, como na conversa real; os arquivos do projeto ficam em inglês. Nos roteiros de validação mostrados aqui, a caixa `- [ ]` é uma ilustração: no arquivo do item ela fica sempre vazia, e as suas marcas ficam na sua máquina, fora do Git.

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

Um item sobre evals é a exceção a "perguntar tudo de uma vez": o número de execuções por caso e o `limitUsd` (o teto de cada chamada o runner deriva) só são propostos depois que o Claude roda um caso uma vez e cita quanto custou (exemplo 14.26).

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
| AC7 idiomas     | roteiro de validação, passo 4                                |
A AC5 era uma lacuna: ExamBoard.Deactivate tinha um teste verde, mas nenhuma página
nem endpoint o chamava. Acrescentei o endpoint e o botão, depois o teste de página acima.

Roteiro de validação (F-3):
1. Abrir a lista de bancas
   - [ ] 1.1 Suba o AppHost e entre como admin.
   - [ ] 1.2 Menu Catalog → Exam boards: lista vazia.
2. Cadastrar bancas
   - [ ] 2.1 Nova: "Fundação Getulio Vargas" / "FGV" → salva e aparece na lista.
   - [ ] 2.2 Nova: "Outra" / "fgv" → mensagem "Sigla já em uso".
3. Editar e desativar
   - [ ] 3.1 Edite a FGV e mude o nome → a lista mostra o nome novo.
   - [ ] 3.2 Desative a FGV → status "Inativa"; reative.
4. [ ] Troque o idioma para pt-PT e en → os textos mudam.
5. [ ] Entre como curador → Nova/Editar não aparecem.
Status: validating.
```

Quando um passo precisa de terminal (um limite, um endpoint ainda sem tela), ele vem para os dois shells, já rodado pelo Claude:

```
4. [ ] Seis tentativas de cadastro seguidas do mesmo endereço: a 6ª é recusada.
   Git Bash:     for i in 1 2 3 4 5 6; do curl -sk -o /dev/null -w "%{http_code}\n" -X POST https://localhost:7101/api/v1/auth/sign-up -H "Content-Type: application/json" -d '{}'; done
   PowerShell 7: 1..6 | % { (Invoke-WebRequest -Method Post https://localhost:7101/api/v1/auth/sign-up -ContentType application/json -Body '{}' -SkipCertificateCheck -SkipHttpErrorCheck).StatusCode }
   Esperado: 400 cinco vezes, depois 429. Para repetir: espere um minuto.
```

**Validação** (**Portão 2**):

```
> O passo 2.2 mostra a mensagem, mas em inglês mesmo com pt-BR.
Claude: Faltava a chave no arquivo pt-BR. Corrigido, e acrescentei um teste para o AC4. Refaça o passo 2.2.
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
| # | Gravidade  | Achado                                      | Veredito   | Ação                     |
| 1 | bloqueador | O endpoint de resultados não confere o dono | confirmado | corrigido + teste        |
| 2 | leve       | Mapeamento duplicado em ResultsPage         | confirmado | corrigido                |
| 3 | grave      | "A nota pode ficar negativa"                | rejeitado  | a BR2 permite penalidade |
Conferido sem achado: dados, contratos, localização; não conferido: nenhum.
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

A guarda também lê o que o mesmo comando vai levar: um commit escrito na mesma linha de um `git add` do arquivo do item (ou de uma pasta, ou de tudo), e um commit que nomeia o arquivo, recebem a mesma recusa, com a mesma mensagem.

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
   releases.win.json seja anexado (o vpk o envia por último). Para refazer, rode estes dois comandos, na ordem:
```

```powershell
gh release delete-asset v2.2.0 Reception-2.2.0-full.nupkg --repo clinica-sorriso/reception-releases --yes
dotnet vpk upload github --repoUrl https://github.com/clinica-sorriso/reception-releases --token "$(gh auth token)" --merge --tag v2.2.0 -o artifacts/publish/v2.2.0/velopack/win-x64
```

```bash
gh release delete-asset v2.2.0 Reception-2.2.0-full.nupkg --repo clinica-sorriso/reception-releases --yes
dotnet vpk upload github --repoUrl https://github.com/clinica-sorriso/reception-releases --token "$(gh auth token)" --merge --tag v2.2.0 -o artifacts/publish/v2.2.0/velopack/win-x64
```

O token do segundo comando é o seu próprio login do `gh`, lido quando o comando roda; ele nunca aparece no chat. Os PCs da segunda clínica se atualizam por delta assim que o release está completo; um PC que abre o app mais de 60 vezes por hora atrás de um mesmo endereço do escritório vê uma checagem falhar no log e tenta de novo na próxima abertura.

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

O F-13 está em `building`. Ontem você parou com `/agile:pause`, então o arquivo do item tem o bloco `## Resume here` com o ponto em que paramos e a próxima ação. Hoje a sessão está longa e o Claude Code compacta a conversa:

```
> /compact
(o Claude Code compacta; o hook acrescenta ao que o resumo mantém:)
Keep in the summary (agile@canary), word for word: the item in progress and its next step.
Resume F-13 Exam board registration — building [feature/F-13] D:\wt\examprep\f-13-exam-board
  Paused: the endpoint tests, wip commit 4e1c2aa
  Next: write the 409 test for a duplicate acronym
(a sessão continua, e o início dela acrescenta:)
After a compact, resume here:
Resume F-13 Exam board registration — building [feature/F-13] D:\wt\examprep\f-13-exam-board
  Paused: the endpoint tests, wip commit 4e1c2aa
  Next: write the 409 test for a duplicate acronym
> continua
Claude: F-13 — Exam board registration [building]
        Escrevendo o teste do 409 para sigla duplicada, em D:\wt\examprep\f-13-exam-board.
```

Sem o bloco `## Resume here` (um item antigo ainda mostra a nota `## Paused`, se tiver uma), o próximo passo vem do status: `continue /agile:build F-13 from the last commit on feature/F-13` em `building`, ou `the owner runs the validation script of F-13, then /agile:ship F-13` em `validating`. Um início de sessão comum (não depois de uma compactação ou de um resume) mostra só as linhas de status de sempre.

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
1. [ ] Inicie o app a partir da worktree: aspire run em D:\wt\shop\f-12-order-discount.
   Dados: esta worktree tem o seu próprio volume (o volume de dados do AppHost não tem nome); nada a ajustar.
   A migração do F-12 cai só ali; o banco do checkout principal não é tocado.
```

O projeto dá nome ao volume e mantém a sua própria chave em `docs/infra.md` (`Database__Name`):

```
1. [ ] Pare qualquer app host que esteja rodando e inicie o app a partir da worktree, num banco só dela:
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
1. [ ] Inicie o app a partir da worktree: aspire run.
   Aviso: o banco local é compartilhado com o checkout principal; a migração do F-12 cai nele
   (registrado em ## Decisions com o seu sim de hoje).
```

Um passo que pede para você mudar código diz onde e o que falha, e um passo que compila para o host antes:

```
5. [ ] Pare o app host (Ctrl+C no terminal dele). Em src/Shop/Orders/Order.cs, dentro da classe Order, depois da
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

### 14.89 O ship abre um pull request e você faz o merge

O `CLAUDE.md` do acme-shop tem `- Merge: pr`, porque a branch principal é protegida. A F-12 está validada:

```
> validado
> /agile:ship F-12
Claude: F-12 — Exportar o relatório mensal em CSV [validating]
        Verificação completa: agile gate GREEN (212 passaram, 0 falharam, 1 min 48 s). Scan GREEN. Manual atualizado. Versão 0.7.0 -> 0.8.0.
        O origin é github.com e merge commit é permitido. ## Delivery e status: done estão na branch.
        Enviei feature/F-12. PR #31 aberto: corpo = resumo, roteiro de validação como checklist, "Validated by the owner on 2026-10-09 (chat)",
        tabela critério -> teste, a última linha do gate, Closes #12. Gravei "PR: #31" no arquivo do item e enviei de novo.
        PR #31 open: merge it on GitHub when you are ready
```

Digitar ship autorizou só o pull request. Você lê o diff no celular e aperta "Create a merge commit". Outro item pode ser construído nesse meio-tempo: o próximo início de sessão diz

```
Waiting on PR: F-12 #31 MERGED — close not run: /agile:ship F-12
```

```
> /agile:ship F-12
Claude: F-12 — Exportar o relatório mensal em CSV [done]
        O PR #31 está MERGED (commit de merge 4e1c0aa). Checkout principal: pull --ff-only, 0 0 contra o remoto, o commit de merge não está vazio.
        Branch remota: já apagada pelo GitHub. Worktree D:/wt/acme-shop/f-12-csv-export removida, branch local apagada.
        Board #12 fechado, Status Done (lido de volta). Nada foi commitado na main: done e Delivery vieram com o PR.
        Retro: 1 lição proposta.
```

Se a F-13 tivesse ido para a main enquanto o PR #31 esperava, e as duas tivessem incrementado a versão, o GitHub mostraria o PR em conflito ("dirty") e você não conseguiria mesclar; o segundo `/agile:ship F-12` então mesclaria a main na branch, rodaria o gate de novo, incrementaria a versão a partir da da main, enviaria e diria `PR #31 open` outra vez, com o mesmo número. Se você tivesse fechado o PR sem merge, o item voltaria para `validating` e a linha `PR:` seria removida. O GitHub não deixa você aprovar o próprio PR: se a regra da branch exige aprovação, o bypass de admin, uma segunda conta ou um bot ficam por sua conta.

### 14.90 Um segredo é barrado na porta, antes de sair da sessão

No acme-shop, o Claude termina a F-14 (uma conexão com o banco de relatórios) e faz o commit num comando só, como sempre:

```
Claude: git add src/Acme.Reports/appsettings.json && git commit -m "feat(F-14): connect to the reporting database"
        [agile secret-guard] Possible secret in the commit: src/Acme.Reports/appsettings.json:7 (password).
        Remove it, or name the setting instead (${DB_PASSWORD}, <password>); for fake test data put agile:allow-secret in a comment on that line. The value is not shown here.
        If this is a mistake, ask the owner; with a yes, repeat the command ending with the comment  # agile:secret-ok
Claude: O commit foi recusado: a linha 7 tem a senha real do banco de relatórios. Nada foi commitado.
        Movi a senha para os user secrets (ConnectionStrings:Reporting) e deixei `Password=<password>` no arquivo. Commitando de novo.
```

A guarda lê o que o comando está prestes a commitar, mesmo quando `git add` e `git commit` são um comando só e nada está preparado (staged) ainda. Ela nomeia o arquivo, a linha e o tipo de segredo, e nunca imprime o valor. A mesma porta vigia o `git push` (os commits que o remoto ainda não tem) e o texto de `gh issue|pr create|edit|comment`, `gh release create|edit` e `az boards work-item create|update`, inline ou vindo de um arquivo:

```
Claude: gh issue create --title "Login fails" --body-file body.md
        [agile secret-guard] Possible secret in the body of gh issue create: body.md:3 (GitHub token).
Claude: O relato cita um token do log. Troquei por `<token>` e criei a issue.
```

Um fixture de teste que precisa de uma chave falsa leva `agile:allow-secret` num comentário da própria linha:

```
var fakeKey = "AKIA<the fake key>"; // agile:allow-secret
```

O marcador fica no código, então o revisor vê cada exceção no diff. Marcadores de lugar (`<password>`, `${DB_PASSWORD}`, `changeme`) passam sozinhos; `var password = request.Password;` é código que lê uma senha, não um segredo, e também passa. Se o segredo já está num commit que não foi enviado, o push é recusado e a mensagem diz que reescrever esse commit precisa do seu sim: nada sofre force-push. A guarda é uma checagem barata em cada porta; o scan antes do ship (gitleaks e semgrep) continua lendo o projeto inteiro e o histórico. Só projetos agile são vigiados, e qualquer dúvida (um diff acima de 4 MB, um comando que ela não consegue ler) deixa a chamada passar.


### 14.91 O id do ticket do cliente é o id do item

O acme-shop corrige bugs que chegam como tickets do Jira. Você quer que o ticket, a branch e a worktree tenham o mesmo nome:

```
> /agile:idea bug --ticket PRJ-123 --url https://acme.atlassian.net/browse/PRJ-123 "Erro ao salvar um pedido sem linhas"
Claude: No similar item (31 items read).
        Card: qual épico? Pedidos (Recomendado) / Novo épico / Sem épico
> Pedidos
Claude: PRJ-123 — Erro ao salvar um pedido sem linhas [idea]
        Criei docs/bugs/PRJ-123-crash-saving-order.md com ticket-url, board #44. Nenhum número B-<n> foi usado.
```

O mesmo ticket não vira um segundo item, e um id errado não escreve nada:

```
> /agile:idea bug --ticket PRJ-123 "Erro em pedido vazio, de novo"
Claude: PRJ-123 já é um item: PRJ-123 — Erro ao salvar um pedido sem linhas [idea]. Nada foi escrito.
        Trabalho novo no mesmo ticket é /agile:change PRJ-123; outro ticket tem o próprio id.
> /agile:idea bug --ticket B-12 "Total errado"
Claude: B-12 não é um id de ticket: um id de ticket é uma chave de dois ou mais caracteres (uma letra, depois letras, dígitos ou _),
        um hífen e dígitos, como PRJ-123 ou TFS-4521. Nada foi escrito.
```

Daí em diante cada passo leva o nome do ticket:

```
> /agile:refine PRJ-123
Claude: PRJ-123 — Erro ao salvar um pedido sem linhas [idea]
        Worktree: D:/wt/acme-shop/b-prj-123-crash-saving-order, branch bug/PRJ-123
```

Os commits ficam `fix(PRJ-123): rejeitar pedido sem linhas`, a mensagem do merge cita `bug/PRJ-123` e as notas da versão listam `Bug PRJ-123`. Itens sem `--ticket` (F-12, B-3) seguem numerados e funcionam como antes.

### 14.92 Um projeto muito atrasado é avisado de quanto, e o que chegou é lido em blocos

O acme-shop sincronizou pela última vez com o plugin em 0.28.0; o plugin agora está em 0.46.0. O sync continua indo direto para a versão atual (ele compara cada arquivo com o texto de hoje, nunca versão por versão), mas, passando de 2 versões menores, o relatório diz quanto falta e agrupa o que tem versão pela série menor em que chegou:

```
> /agile:sync
Claude: 0.28.0 -> 0.46.0: 18 versões menores atrás; o sync vai direto para 0.46.0. O que chegou, por versão:

        0.30 (1 capacidade, 0 notas)
          - Database password: o AppHost declara um banco sem parâmetro de senha ...
        0.35 (0 capacidades, 1 nota)
          - linha 14 do retro-log: "gate.js lê o stdin ..." (#25), entregue na 0.35.0
        Antes da sua versão registrada 0.28.0 (ainda pendente)
          - DocGen: documentação técnica gerada a partir do código ...

        Arquivos, uma tabela por estado (nada data um arquivo, então não são agrupados):
        | arquivo                           | estado | proposta |
        | .claude/rules/agile/git.md        | update | copiar: ... |
        | docs/agile/workflow.md            | edited | mesclar: ... |
        Aprove as linhas, uma a uma ou "ok" para todas.
```

Um projeto com até 2 versões menores de atraso, ou cujo `.claude/agile/sync.json` não registra versão, recebe exatamente o relatório de antes: sem aviso e sem blocos. A aprovação não muda: um OK por linha, não um por bloco.

### 14.93 Os testes vêm primeiro e ficam travados enquanto o item é construído

O F-12 da acme-shop, o total de um pedido, está aprovado. O build não escreve os testes junto com o código: ele roda uma fase de testes, trava os testes num commit e só então escreve o código.

```
> /agile:build F-12
Claude: F-12 — Order total [approved]
        Worktree of F-12: D:/wt/acme-shop/f-12-order-total [feature/F-12]
        Fase de testes: 3 testes para AC1-AC3 e o esqueleto de Order (assinaturas que lançam NotImplementedException).
        Testes: Failed: 3, Passed: 0 — cada um falha em Order.Add ou Order.Total, no esqueleto; nenhum é erro de compilação.
        test(F-12): testes de aceite do total do pedido commitados.
        Tests locked: 9f89899c0d1e... (1 file); unlocks: 0
        A linha está em docs/features/F-12-order-total.md, sob ## Criterion → test.
```

Daí em diante um hook recusa toda escrita nesse arquivo, pelas ferramentas de edição e pelo shell, e uma reescrita de histórico (rebase, amend, reset hard) na branch do item:

```
Claude: [agile test-lock] this command writes tests/Orders.Tests/OrderTests.cs, locked by the acceptance
        tests of F-12 (commit 9f89899): the code is made to satisfy them, never the reverse.
        If the test is wrong, stop and ask the owner.
```

Se o teste está mesmo errado, o build para e pergunta a você; a sessão nunca destrava sozinha:

```
Claude: ❓ Um teste travado parece errado
        O AC2 espera o total sem imposto, e a regra BR2 diz que inclui.
        a) Destravar e reescrever o teste
        b) Manter o teste e corrigir o código
        Recomendação: a). Motivo: a regra do arquivo diz que o total inclui imposto; o teste a contradiz.
> a
Claude: Tests unlocked: F-12 (AC2 contradiz BR2; o dono escolheu reescrever); unlocks: 1.
        Reescrito, commitado, travado de novo: Tests locked: 3b7c21aa4e90... (1 file); unlocks: 1
        Escrito em ## Decisions: tests unlocked (AC2 contradiz BR2).
```

No ship, o lock é conferido de novo: os arquivos são iguais ao commit do lock e cada destravamento tem a sua linha em `## Decisions`. Um arquivo alterado, um destravamento sem decisão ou um arquivo de lock perdido para o ship como um build vermelho:

```
> /agile:ship F-12
Claude: Tests lock: 3b7c21aa4e90... (1 files) unchanged — F-12; unlocks: 1
          unlock: 2026-10-10T14:02:11Z AC2 contradiz BR2; o dono escolheu reescrever
```

Os testes de uma tela que o agente `frontend` escreve com o mockup aprovado não são travados. Um item que chega ao ship sem lock (construído antes desta regra) imprime `Tests lock: none (built before this rule)` e segue.

### 14.94 A board é lida em pt-BR, o arquivo continua em inglês

O acme-shop mantém os arquivos dos itens em inglês (por exemplo `docs/features/F-12-export-exam-board-csv.md`); a board do GitHub é o que você lê. Desta versão em diante, o que o plugin escreve ali é pt-BR.

```
> /agile:idea feature "exportar a lista de bancas para CSV"
Claude: F-12 — Export the exam board list to CSV [idea]
        Arquivo: docs/features/F-12-export-exam-board-csv.md (inglês)
        Board: issue #31, título "Exportar a lista de bancas para CSV", corpo em pt-BR
```

O que o issue mostra (o corpo é o arquivo inteiro, gerado de novo a cada escrita):

```
## Resumo
Exportar a lista de bancas examinadoras para CSV, a partir da tela de bancas.

## Início
- Depende de: nada.

Original (English): docs/features/F-12-export-exam-board-csv.md
```

No ship, o comentário de evidência também é pt-BR e, com `Merge: pr`, o pull request é `F-12 — Exportar a lista de bancas para CSV`, com o resumo e a checklist de validação em pt-BR. Os commits e a mensagem de merge ficam em inglês. Um item que já tinha corpo em inglês na board o mantém até o plugin enviá-lo inteiro de novo (por exemplo `/agile:script F-9`). Um projeto no Azure Boards recebe o mesmo texto sem acentos (`Resumo`, `Criterios de aceite`).

### 14.95 Um item registra o que aconteceu com ele, e a retro conta

O F-12 da acme-shop, o total de um pedido, percorre o seu ciclo. Ninguém pede ao Claude que anote o que acontece: o arquivo do item junta isso, uma linha por evento, em `## Events`, logo antes de `## Delivery`. O gate escreve a sua linha quando uma sequência de vermelhos termina; destravar um teste também escreve a sua:

```
## Events

- 2026-10-10 question — asked 4, from context 0
- 2026-10-10 override — Total with tax — chose "per line" over "on the sum"
- 2026-10-10 test-fail — build failed (src/Orders/Order.cs), 2 reds
- 2026-10-11 blocked — test wrong: AC2 contradicted BR2
- 2026-10-11 rework — reviewer: tax rounded per item, not on the sum
- 2026-10-11 rework — reviewer: tax rounded per item again, in the export
- 2026-10-11 question — asked 1, from context 0
```

Os dois vermelhos do gate são uma linha `test-fail`: ela abriu no primeiro vermelho, fechou no verde seguinte e nomeia a causa do primeiro. No ship, o relatório conta o arquivo e compara com os últimos cinco itens entregues:

```
> /agile:ship F-12
Claude: ...
        Events: override 1, question 2 (asked 5, from context 0), assumed 0, rework 2, test-fail 1, blocked 1, change 0, correction 0, stop 0
        Last 5 shipped (5 items): override 0.2, question 1.8, rework 0.6, test-fail 0.4, blocked 0.2
```

A retro parte dessas duas linhas, e uma linha de evento é a evidência que as notas pedem:

```
Claude: Lição 1 (nota plugin 7 / projeto 8): uma regra que vale para total, imposto e arredondamento foi
        decidida no chat e não no arquivo. Evidência: as duas linhas `rework` de 2026-10-11 nomeiam a
        mesma causa (onde o imposto é arredondado): visto duas vezes num item.
```

Um repositório sem arquivo de item (o do próprio plugin, sem `docs/agile`) não escreve nada, e o `events.js add` avisa; o checkout principal de um projeto também, onde o arquivo do item é só uma cópia:

```
agile events: no item file for F-12 in D:/dev/acme-shop (docs/features or docs/bugs): nothing was written
agile events: this worktree is on main, not on the branch of F-12: nothing was written (run it from the item's worktree)
```

### 14.96 O autopilot embarca na etapa em que o item está

A acme-shop tem a F-12 aprovada à mão ontem (`/agile:refine F-12`, depois "Aprovo F-12") e a F-13 ainda ideia. A flag vai no comando que serve ao item.

```
> /agile:build F-12 --autopilot
Claude: F-12 — Exportar a lista de bancas para CSV [approved]
        Worktree de F-12: D:/dev/wt/acme-shop/f-12-export-exam [feature/F-12]
        Embarca no build: sem rodada de perguntas. Autopilot: approved.
        ...
        PARADA 2 — F-12 [validating]. Testes: 41 passaram, 0 falharam. Responda "validado e autorizo o merge de F-12"
        para entregar, "validado" sozinho para parar aqui.
```

O comando errado é visto antes de qualquer coisa acontecer: a F-13 é uma ideia, então o build não a aceita.

```
> /agile:build F-13 --autopilot
Claude: F-13 — Importar a lista de bancas [idea]
        F-13 está em idea: /agile:refine F-13 --autopilot embarca neste status
```

Nenhuma branch, worktree, arquivo ou item do quadro foi mexido. Digitado de novo num item depois da validação, o mesmo comando mostra a parada 2 e não entrega nada:

```
> /agile:autopilot F-12
Claude: F-12 — Exportar a lista de bancas para CSV [validating]
        A linha Autopilot: dizia approved; o item está em validating, reescrita para stop 2.
        PARADA 2 — arquivos, testes, ## Criterion → test, roteiro de validação (mostrado de novo).
        Nada é entregue até você escrever "validado e autorizo o merge de F-12".
```

### 14.97 As features do épico andam uma de cada vez, cada uma pelo autopilot

A acme-shop tem o épico "exam board" concordado, com três features: F-20 (a lista), F-21 (a exportação para CSV, depende de F-20) e F-22 (o layout de impressão, caminho no plano `/agile:discuss` primeiro).

```
> /agile:epic "exam board" --autopilot
Claude: Épico exam-board [agreed]: 0/3 done. Fila: F-20, F-21, F-22.
        F-20 — Lista de bancas [idea]
        ...
        PARADA 1 — F-20: rodada de perguntas (3 perguntas, recomendações primeiro).
```

Você responde, aprova F-20 e a valida. Só a frase do merge a entrega, e F-21 começa na mesma execução:

```
> validado e autorizo o merge de F-20
Claude: F-20 entregue (0.31.0). Retro pulada: 2 candidatos a lição acrescentados em ## Retro notes do épico.
        Épico exam-board: Autopilot: F-21 stop 1 (2026-10-12). 1/3 done.
        F-21 — Exportar a lista de bancas para CSV [idea]
        PARADA 1 — F-21: rodada de perguntas (2 perguntas; responde a partir do que F-20 decidiu).
```

"validado" sozinho encerra a execução, e o mesmo comando a retoma:

```
> validado
Claude: Execução terminou em F-21 [validating]. Nada foi mesclado. 1/3 done, Autopilot: F-21 stop 2.
        Retomar: /agile:epic "exam board" --autopilot   (ou /agile:ship F-21)
```

O caminho de F-22 no plano é `/agile:discuss` primeiro, então a execução nunca a começa sozinha:

```
> /agile:epic "exam board" --autopilot
Claude: F-21 entregue antes; 2/3 done.
        Execução terminou antes de F-22: o caminho dela no plano é /agile:discuss primeiro. Rode /agile:discuss
        no layout de impressão, mude o caminho dela em docs/epics/exam-board.md e digite este comando de novo.
```

Depois que a última feature é entregue, a retro do épico conta primeiro e propõe no máximo três lições:

```
Claude: Épico exam-board: 3/3 done.
        Events (3 items): override 1, question 4 (asked 7, from context 0), assumed 0, rework 1, test-fail 2, blocked 0, change 0, correction 1, stop 2
        Last 5 shipped (5 items): override 0.4, question 1.6, rework 0.2 ...
        1. plugin 4 / project 6 — faltou uma chave de recurso em pt-BR em duas features: acrescentá-la às regras do projeto. Aprova?
```

Só as lições aprovadas são aplicadas; então o épico fica `done` e a linha `Autopilot:` é removida.

### 14.98 F-13 espera F-12 ser entregue, e se libera sozinho

F-12 (Bank sync) está em `building` na worktree dele. F-13 (Card limit) diz `- Depends on: F-12` no `## Start`, e a refinação dele acabou de terminar:

```
> aprovo F-13
Claude: F-13 aprovado, e bloqueado: depende de F-12 (building, D:/dev/wt/app/f-12-bank-sync); libera sozinho no primeiro comando depois da entrega de F-12.
        status: blocked, "Blocked (2026-10-12): depends on F-12 (building) — unblocked by the delivery of F-12 — returns to approved",
        a aprovação e o bloqueio no ## Decisions, um evento blocked, label `blocked` no issue (o campo Status fica Ready).
```

Um comando em F-13 antes da entrega de F-12 para depois da linha do item e não escreve nada; o `--unblock` também é recusado:

```
> /agile:build F-13
Claude: F-13 — Card limit [blocked]
        Worktree of F-13: D:/dev/wt/app/f-13-card-limit [feature/F-13]
        F-13 está bloqueado: depende de F-12 (building, D:/dev/wt/app/f-12-bank-sync); libera sozinho no primeiro comando depois da entrega de F-12.
> /agile:change F-13 --unblock
Claude: F-13 — Card limit [blocked]
        Recusado: F-12 ainda não foi entregue (building). Se F-13 não precisa mais de F-12, corrija o Depends on com /agile:change F-13 <o que mudou>.
```

F-12 é entregue. O relatório do ship tem uma linha a mais, só leitura (F-14 depende de F-12 e de F-15):

```
> /agile:ship F-12
Claude: ...
        Dependents: F-13 free (released at its next command); F-14 waits on F-15 (building)
```

A sessão seguinte começa com `Ready to release: F-13 Card limit — the first command on it releases it`. Nada foi escrito ainda na worktree de F-13: a liberação acontece quando você mexe nele, e o comando segue:

```
> /agile:build F-13
Claude: F-13 — Card limit [blocked]
        Worktree of F-13: D:/dev/wt/app/f-13-card-limit [feature/F-13]
        F-12 foi entregue: F-13 voltou para approved (a linha Blocked e o label removidos,
        "2026-10-14: blocked (depends on F-12 (building)), unblocked (F-12 delivered)" no ## Decisions).
        ...
```

Entregue quer dizer `status: done` na branch principal: com `Merge: pr`, F-12 ainda esperando o pull request não está entregue. Um item em `idea` ou `refining` nunca é bloqueado assim (só mostra `Waits on:`), e um item que você bloqueou à mão (14.67) nunca se libera sozinho.

### 14.99 Arquivando o que está finalizado: `/agile:compact`

O acme-shop entregou 36 itens e o retro-log tem 32 entradas, 6.400 palavras. O início da sessão imprimiu uma linha (ela só avisa; nada se move):

```
Compact: 36 finished items (~74800 words), retro-log 6400 words: /agile:compact
```

Entre features, você digita o comando e lê o plano:

```
> /agile:compact
Claude: Plano, nada foi escrito ainda:
        | Item | Título | Status | De → para | Palavras |
        | F-1 | Sign in | done | docs/features/F-1-sign-in.md → docs/archive/F-1-sign-in.md | 2.140 |
        | ... mais 34: F-2 a F-31, B-1 a B-4 | | | | |
        Mockups que vão junto: 3 (docs/archive/mockups/)
        Ficam: F-32 (done, uma worktree segura a branch: o pull request está aberto), F-33 (approved), F-34 (idea)
        Retro-log: 21 das 32 entradas movem; as 10 mais recentes ficam, e uma entrada antiga fica pela nota ⏳ aberta
        Links reescritos: docs/agile/backlog.md 29, docs/epics/shop.md 6
        Palavras que uma sessão relê: 82.900 → 8.700
        Aplico este plano? (ok / deixa)
> ok
Claude: agile compact APPLIED: 35 items, 3 mockups, 21 retro-log entries, 35 links; nothing was committed
        Status da árvore: 35 renomeados, docs/archive/INDEX.md e docs/archive/retro-log.md novos,
        docs/agile/backlog.md, docs/epics/shop.md e docs/agile/retro-log.md modificados
        Commit proposto: chore: archive 35 finished items and 21 retro-log entries (/agile:compact)
        Autoriza o commit na main?
> ok
Claude: Commitado. Rodar de novo não encontra nada para mover.
```

O que o arquivo muda depois: um bug novo ganha `B-5`, porque `B-1` a `B-4` só estão no índice e os números deles continuam tomados; e uma ideia posterior encontra o assunto já entregue no índice, sem abrir nenhum arquivo arquivado:

```
> /agile:idea "baixar os resultados em um arquivo CSV"
Claude: Item parecido: F-12 — Export results to CSV [done], lido de docs/archive/INDEX.md.
        Already delivered in F-12 (0.14.0). Crio um item novo mesmo assim, ou deixo?
```

Com um item em `building`, o mesmo comando para e não move nada:

```
> /agile:compact
Claude: F-33 está em building em D:/dev/wt/acme-shop/f-33-print-layout: o compact roda entre features.
        Nada foi movido. Termine ou pause o F-33 e digite o comando de novo.
```

### 14.100 A ordem do que implementar: `/agile:order`

O acme-shop tem quatro itens em aberto: F-12 (Must, em construção), F-13 (bloqueado até o F-12 ser entregue), F-14 (Should) e B-2, um bug. O épico `shop` lista `F-12, F-13` em `## First release cut`. Você digita o comando uma vez:

```
> /agile:order
Claude: agile order WRITTEN: 4 items, 11 delivered
        | # | Id | Título | Status | Épico | Depende de | Libera |
        | 1 | B-2 | Total errado em reembolsos | approved | - | - | - |
        | 2 | F-12 | Export results to CSV | building | shop | - | F-13 (unblocks), first release of shop |
        | 3 | F-14 | Print layout | approved | shop | - | - |
        | 4 | F-13 | Share by link | blocked | shop | F-12 | - |
        First release (shop): 0 of 2 delivered; missing: F-12, F-13
        Escrito a partir de 4 itens. git status --short: docs/agile/order.md novo
        Commit proposto: chore(order): refresh the ordered list
        Autoriza o commit na main?
> ok
Claude: Commitado. Daqui em diante o ship atualiza este arquivo a cada entrega.
```

O B-2 vem primeiro: um bug vai antes das features do nível dele. O F-13 fica um nível abaixo do F-12 porque espera por ele. Quando você entrega o F-12, o ship atualiza o arquivo no commit que põe o F-12 em `done` e reporta uma linha:

```
> /agile:ship F-12
Claude: ...
        Order: F-13 liberado; primeira versão (shop): falta 1.
```

A linha diz o que a entrega liberou (o F-13 saiu do bloqueio na mesma hora) e o quanto falta da primeira versão. Um projeto que nunca digitou `/agile:order` vê `Order: sem lista (/agile:order cria).` e nada é escrito.

### 14.101 Em quais idiomas o manual da app é escrito: `Manual locales`

O acme-shop é um projeto novo. Na rodada 5 do `/agile:bootstrap`, depois dos idiomas da UI, o Claude pergunta pelo manual:

```
> /agile:bootstrap
Claude: ❓ Em quais idiomas o manual do app será escrito?
        a) pt-BR (padrão)   b) outras tags (vários podem ser escolhidos; o primeiro é a referência)
        Recomendação: pt-BR. Motivo: só o que você vai ler e manter; cada idioma extra é traduzido a cada ship.
> a e, em outras tags, en
Claude: CLAUDE.md em "## Languages": - Manual locales: pt-BR, en
        docs/manual/pt-BR/index.md e docs/manual/en/index.md (locale: pt-BR e locale: en); nenhuma outra pasta.
```

A lista não tem relação com a pergunta 20: a app pode sair com a UI numa lista própria de idiomas. Algumas features depois você decide que o manual também precisa de espanhol, edita a linha para `- Manual locales: pt-BR, en, es` e dá o ship do próximo item:

```
> /agile:ship F-9
Claude: Manual: adicionando es, 7 páginas a partir de pt-BR
        docs/manual/es/ escrito a partir de docs/manual/pt-BR/ (mesmos slugs, locale: es).
        Commit isolado: docs(F-9): add es to the app manual
        Depois as páginas do próprio F-9, em pt-BR, en e es.
```

Um projeto antigo não tem a linha, só as pastas `pt-BR`, `pt-PT` e `en`. O ship dele trabalha com as três e diz uma vez que a linha falta; o `/agile:sync` propõe a linha. O relatório do sync para esse projeto traz a chave `manualLocales`:

```
"manualLocales": { "line": null, "found": ["pt-BR", "pt-PT", "en"], "proposed": "pt-BR, pt-PT, en" }
```

```
> /agile:sync
Claude: | arquivo | estado | o que proponho |
        | manual locales | sem linha | escrever `- Manual locales: pt-BR, pt-PT, en` (substitui a linha `- App manual:`) |
        Posso escrever?
> ok
Claude: Escrito em "## Languages". Nenhuma página do manual foi tocada.
```

Tire `pt-PT` dessa linha e o próximo ship deixa `docs/manual/pt-PT/` como está e avisa uma vez: `docs/manual/pt-PT não está em Manual locales: mantido como está`. Nada é apagado.

### 14.102 Os idiomas do manual do plugin: o `workflow.md` antigo e a linha do `CLAUDE.md`

O acme-shop foi bootstrapped antes de o plugin ligar só o pt-BR em `docs/manual/languages.json`: ele tem `docs/agile/workflow.md` e `docs/agile/workflow-examples.md` (em inglês), e a linha `Workflow:` do `CLAUDE.md` aponta para o primeiro. Depois de atualizar o plugin e reiniciar, você digita o sync:

```
> /agile:sync
Claude: 0.55.0 -> 0.56.0. Plano:
        | Arquivo | Estado | Proposta |
        | docs/agile/workflow.pt-BR.md | new | copiar |
        | docs/agile/workflow-examples.pt-BR.md | new | copiar |
        | docs/agile/workflow-examples.md | aposentado | o plugin não mantém mais o manual em en; a cópia fica como está (apague se não quiser) |
        | docs/agile/workflow.md | aposentado | o plugin não mantém mais o manual em en; a cópia fica como está (apague se não quiser) |
        | CLAUDE.md | linha Workflow | trocar docs/agile/workflow.md por docs/agile/workflow.pt-BR.md? (ok ou não) |
        Aplico? (ok ou não)
> ok
Claude: Copiei os dois arquivos pt-BR. Os dois antigos ficam como estão; troquei só a linha Workflow: do CLAUDE.md.
        Commit proposto: chore: sync with agile@canary 0.56.0
```

O relatório do plano é o que o script imprime, na chave `manualCopies`:

```json
{
 "retired": [
  { "file": "docs/agile/workflow-examples.md", "locale": "en" },
  { "file": "docs/agile/workflow.md", "locale": "en" }
 ],
 "pointer": { "from": "docs/agile/workflow.md", "to": "docs/agile/workflow.pt-BR.md" }
}
```

Um projeto que nunca teve esses dois arquivos não vê nenhuma das duas linhas, e uma linha `Manual locales: en, pt-BR` no `CLAUDE.md` não muda nada disso: o manual da app e o manual do plugin são configurações separadas. Com `/agile:version` o Claude lê então o cabeçalho de `workflow.pt-BR.md`; antes do sync, num projeto que só tem o `workflow.md`, ele lê o desse.

### 14.103 Em quais idiomas as telas da app são escritas: `UI locales`

A acme-shop é um projeto novo. Na rodada 5 do `/agile:bootstrap`, a pergunta 20 pede os idiomas da UI:

```
> /agile:bootstrap
Claude: ❓ Em quais idiomas o app terá textos de tela?
        a) pt-BR (padrão)   b) outras tags (vários podem ser escolhidos; a ordem da resposta é a ordem da linha, o primeiro é o idioma base)
        Recomendação: pt-BR. Motivo: cada idioma extra é um conjunto de textos a escrever e manter em toda tela; o primeiro da lista é o idioma base do app.
> a
Claude: CLAUDE.md em "## Languages": - UI locales: pt-BR
        Directory.Build.props: <NeutralLanguage>pt-BR</NeutralLanguage>
        Resources/: App.resx (pt-BR, o arquivo neutro); nenhum App.pt-BR.resx e nenhum seletor de idioma.
```

Algumas features depois a app precisa de espanhol. Você edita a linha para `- UI locales: pt-BR, es` e começa o próximo item. Antes do trabalho do próprio item, o build atualiza o idioma novo em um commit só dele (esta é a saída da execução real num projeto de teste com quatro chaves):

```
> /agile:build F-9
Claude: UI: adicionando es, 4 chaves em 1 arquivo
        src/App/Resources/App.es.resx escrito a partir do arquivo neutro (mesmas chaves e placeholders).
        Commit só dele: feat(F-9): add es UI texts
        Depois o trabalho do F-9, com cada texto novo em pt-BR e es.
```

Um projeto antigo não tem a linha `- UI locales:`. O build dele trabalha com `pt-BR` e avisa uma vez `UI locales: sem linha, usando pt-BR` que a linha falta; o `/agile:sync` a propõe. O relatório do sync desse projeto traz a chave `uiLocales` (impressa pelo script no projeto de teste):

```
"uiLocales": { "line": null, "proposed": "pt-BR" }
```

```
> /agile:sync
Claude: | arquivo | estado | o que proponho |
        | ui locales | sem linha | escrever `- UI locales: pt-BR` |
        Posso escrever?
> ok
Claude: Escrito em "## Languages". Nenhum arquivo de recursos foi tocado.
```

Um projeto que já tem a linha, mesmo na forma antiga com texto depois da lista (`- UI locales: pt-BR, pt-PT, en. No hardcoded UI strings...`), não ganha linha na tabela nem reescrita. Tire `pt-PT` da linha e o próximo build deixa os arquivos `.pt-PT.resx` como estão e avisa uma vez: `pt-PT UI texts não estão em UI locales: mantidos como estão`. Nada é apagado, e o seletor de idioma deixa de oferecer `pt-PT`.

### 14.104 A lista de caixas da ordem: `/agile:order` mostra o que marcar

Com os mesmos quatro itens em aberto do exemplo 14.100 (o acme-shop; o exemplo 14.104 é este), o `/agile:order` mostra na conversa a lista de caixas em vez da tabela. Você marca as que já implementou enquanto lê, no app do Claude (a saída abaixo é a do script no projeto de teste):

```
> /agile:order
Claude: agile order WRITTEN: 4 items, 11 delivered
        - [ ] 1. B-2 — Total errado em reembolsos [approved]
        - [ ] 2. F-12 — Export results to CSV [building] — unlocks F-13 (unblocks)
        - [ ] 3. F-14 — Print layout [approved]
        - [ ] 4. F-13 — Share by link [blocked] — waits on F-12
        As marcas ficam salvas só nesta máquina, fora do Git: o widget salva a cada clique, ou diga "marque F-3".
        First release (shop): 0 of 2 delivered; missing: F-12, F-13
        Escrito a partir de 4 itens. git status --short: docs/agile/order.md novo
        Commit proposto: chore(order): refresh the ordered list
        Autoriza o commit na main?
```

O mesmo texto está em `docs/agile/order.md`, na seção `## Checklist`, entre a tabela e o `## Summary`; a tabela continua no arquivo. A linha de cada item traz o número da tabela, o id, o título e o status, e depois `waits on` e `unlocks` só quando a célula tem algo. No arquivo as caixas são sempre `[ ]`, e uma marca (que o exemplo 14.107 mostra salva) não muda status, ordem nem arquivo. Um arquivo escrito por uma versão antiga ganha a seção na primeira execução, num diff só dela.

### 14.105 A lista de caixas da ordem como widget: `/agile:order` no app

Com os mesmos quatro itens em aberto do exemplo 14.104, numa sessão do app do Claude que oferece a ferramenta `show_widget`, o `/agile:order` mostra a lista como um widget interativo em vez de colar as linhas `- [ ]`. O widget é montado a partir do JSON que o script imprime (`order.js checklist --json`, que não grava nada); o Claude não redigita nenhuma linha:

```
> /agile:order
Claude: agile order WRITTEN: 4 items, 11 delivered
        [widget] Ordem de implementação
          ☐ 1. B-2   Total errado em reembolsos            approved
          ☐ 2. F-12  Export results to CSV                 building   unlocks F-13 (unblocks)
          ☐ 3. F-14  Print layout                          approved
          ☐ 4. F-13  Share by link                         blocked    waits on F-12
          0 de 4 marcados    [Limpar marcas]
          As marcas ficam salvas a cada clique, só nesta máquina, fora do Git.
        As marcas ficam salvas só nesta máquina, fora do Git: o widget salva a cada clique, ou diga "marque F-3".
        Não marque as caixas ao abrir docs/agile/order.md no painel de arquivos do app: lá o clique grava no arquivo.
        First release (shop): 0 of 2 delivered; missing: F-12, F-13
        Escrito a partir de 4 itens. git status --short: docs/agile/order.md novo
        Commit proposto: chore(order): refresh the ordered list
        Autoriza o commit na main?
```

A saída de `order.js checklist --json` no projeto de teste, de onde o widget sai:

```
{
  "items": [
    {
      "n": 1,
      "id": "B-2",
      "title": "Total errado em reembolsos",
      "status": "approved",
      "epic": "shop",
      "waitsOn": [],
      "unlocks": [],
      "cycle": false,
      "ticked": false
    },
    {
      "n": 2,
      "id": "F-12",
      "title": "Export results to CSV",
      "status": "building",
      "epic": "shop",
      "waitsOn": [],
      "unlocks": [
        "F-13 (unblocks)"
      ],
      "cycle": false,
      "ticked": false
    },
    {
      "n": 3,
      "id": "F-14",
      "title": "Print layout",
      "status": "approved",
      "epic": "shop",
      "waitsOn": [],
      "unlocks": [],
      "cycle": false,
      "ticked": false
    },
    {
      "n": 4,
      "id": "F-13",
      "title": "Share by link",
      "status": "blocked",
      "epic": "shop",
      "waitsOn": [
        "F-12"
      ],
      "unlocks": [],
      "cycle": false,
      "ticked": false
    }
  ],
  "listed": 4,
  "delivered": 11
}
```

Marcar duas caixas no widget risca e escurece as linhas e muda o contador para `2 de 4 marcados`; `Limpar marcas` as desmarca na página; cada clique manda a mensagem que grava o conjunto (exemplos 14.107 e 14.108). O `docs/agile/order.md` continua como o script o escreveu, com todas as caixas vazias. É no painel de arquivos do app que o clique numa caixa `- [ ]` grava `[x]` em `docs/agile/order.md` (o `git diff` mostra o arquivo alterado), por isso o Claude pede para não marcar lá. Num terminal, ou numa sessão sem `show_widget`, o `/agile:order` mostra as linhas `- [x]` e `- [ ]` como no exemplo 14.104. O `/agile:ship` só atualiza o arquivo (a linha `Order:`) e nunca mostra o widget.

### 14.106 `/agile:order` na sessão do plugin: a lista sai dos issues e nada é gravado

No repositório do próprio plugin os itens são issues do GitHub, não arquivos em `docs/features`. Ali o `/agile:order` lê os issues com uma única chamada do `gh`, aplica a mesma regra de ordem de sempre e só mostra a lista: não escreve arquivo, não propõe commit e o ship do plugin não atualiza nada. O exemplo usa um repositório de teste com um épico (#10), dois itens dele, um bug, um item adiado (`deferred`) e um que depende dele:

```
> /agile:order
Claude: [widget] Ordem de implementação
          ☐ 1. #13  Ship misses the version in the README   idea
          ☐ 2. #11  Refine prints the item line             approved   unlocks #12
          ☐ 3. #12  Order shows what each item unlocks      idea       waits on #11
          ☐ 4. #15  Report mailed to the owner              idea       waits on #14 (parked)
          0 de 4 marcados    [Limpar marcas]
          Adiados (fora da ordem):
            #14  Weekly report of delivered items           idea       unlocks #15
        As marcas ficam salvas só nesta máquina, fora do Git: o widget salva a cada clique, ou diga "marque F-3".
        4 itens na ordem, 1 adiado, 2 entregues. Nada foi escrito e nenhum commit é proposto.
```

Num terminal, ou numa sessão sem `show_widget`, a saída é o texto do script, com as linhas `- [ ]` e o grupo `## Parked` depois delas (a saída do script no mesmo repositório de teste):

```
# Order of implementation

| # | Id | Title | Status | Epic | Depends on | Unlocks |
|---|---|---|---|---|---|---|
| 1 | #13 | Ship misses the version in the README | idea | - | - | - |
| 2 | #11 | Refine prints the item line | approved | #10 Item workflow | - | #12 |
| 3 | #12 | Order shows what each item unlocks | idea | #10 Item workflow | #11 | - |
| 4 | #15 | Report mailed to the owner | idea | - | #14 (parked) | - |

## Checklist

- [ ] 1. #13 — Ship misses the version in the README [idea]
- [ ] 2. #11 — Refine prints the item line [approved] — unlocks #12
- [ ] 3. #12 — Order shows what each item unlocks [idea] — waits on #11
- [ ] 4. #15 — Report mailed to the owner [idea] — waits on #14 (parked)

## Parked

Open items labelled `deferred`: outside the order of implementation, with no position. What depends on one of them waits.

| # | Id | Title | Status | Epic | Depends on | Unlocks |
|---|---|---|---|---|---|---|
| - | #14 | Weekly report of delivered items | idea | - | - | #15 |

## Summary

- Delivered: 2
- Parked: 1

agile order SHOWN: 4 items, 1 parked, 2 delivered
```

O que a lista lê: os issues com o rótulo `feature` ou `bug` são as linhas (o épico, rótulo `epic`, só dá nome ao grupo na coluna `Epic`); o status é a palavra depois de `Status:` na primeira linha do corpo; as dependências são os `#n` da linha `- Depends on:` de `## Start` (um `#n` entre crases não conta); entregue é issue fechado como concluído, e um fechado como não planejado aparece como `(cancelled)` e nunca sobe o nível. O item `#14` está `deferred`, então fica no grupo `## Parked`, sem posição e fora de qualquer `Unlocks` de uma linha da ordem; o `#15`, que depende dele, espera e mostra `#14 (parked)`: tirar o `#14` do adiamento é o que o libera. Se o `gh` não está logado ou falha, a saída é `agile order FAILED: <motivo>` e nenhuma lista parcial; fora do repositório do plugin é `REFUSED`, e num projeto o comando continua escrevendo `docs/agile/order.md` como no exemplo 14.104.

### 14.107 As marcas da lista da ordem ficam salvas: o widget guarda e a próxima sessão as mostra

Num projeto com quatro itens em aberto (B-2, F-12 em `building`, F-14 e F-13, que espera o F-12), você marca no widget o F-12 e o F-14 ("quero estes agora"). Cada clique manda uma mensagem com o conjunto inteiro (aqui, a do segundo clique) e o Claude roda o script uma vez por mensagem; nada mais é mostrado, nenhum `git status`, nenhum commit:

```
> /agile:order ticks F-12 F-14
Claude: agile order TICKED: 2 ticks
        Marcas salvas só nesta máquina, fora do Git.
```

Na sessão seguinte, `/agile:order` mostra as duas caixas já marcadas (no widget, riscadas e escurecidas, com o contador `2 de 4 marcados`; num terminal, as linhas abaixo, a saída do script no projeto de teste):

```
- [ ] 1. B-2 — Total errado em reembolsos [approved]
- [x] 2. F-12 — Export results to CSV [building] — unlocks F-13
- [x] 3. F-14 — Print layout [approved]
- [ ] 4. F-13 — Share by link [approved] — waits on F-12
```

O arquivo das marcas é `agile-order-ticks.json`, dentro da pasta `.git` do projeto, e tem só os ids:

```
{
  "version": 1,
  "ticks": [
    "F-12",
    "F-14"
  ]
}
```

O `docs/agile/order.md` não muda: a seção `## Checklist` dele continua com todas as caixas `- [ ]`, então uma marca nunca aparece num `git status` nem num commit, e a máquina de outra pessoa (ou o seu outro clone) tem as marcas dela. Um id que não é um item em aberto da lista é recusado pelo nome e nada é gravado:

```
> /agile:order ticks F-9
Claude: agile order REFUSED: F-9 is not an open listed item: nothing was written
```

Quando o F-12 for entregue, a marca dele é descartada na execução seguinte do `/agile:order` e some da lista; `Limpar marcas` (ou `/agile:order ticks` sem ids) esvazia o conjunto. No repositório do plugin é igual, com os ids `#n`, e um issue `deferred` não pode ser marcado.

### 14.108 O widget da ordem salva a cada clique: marcar e trocar de sessão não perde nada

Antes, uma marca só ia para o disco quando você apertava um botão de salvar, e quem trocava de sessão sem apertar perdia tudo. Agora não há botão: cada clique numa caixa (e o `Limpar marcas`) manda na hora a mensagem com o conjunto inteiro das caixas marcadas, na ordem da lista, sem espera. Marcando F-12, F-14 e F-13, um clique de cada vez, a conversa recebe três mensagens e o Claude roda o script uma vez por mensagem, mostrando só a última linha:

```
> /agile:order ticks F-12
Claude: agile order TICKED: 1 tick
> /agile:order ticks F-12 F-14
Claude: agile order TICKED: 2 ticks
> /agile:order ticks F-12 F-14 F-13
Claude: agile order TICKED: 3 ticks
```

Como toda mensagem leva o conjunto inteiro, e nunca só o id que mudou, uma rajada de cliques durante uma resposta em andamento termina sempre no que a última mensagem disse. Numa sessão nova, sem apertar nada, a lista aparece assim (a saída do script no projeto de teste):

```
- [ ] 1. B-2 — Total errado em reembolsos [approved]
- [x] 2. F-12 — Export results to CSV [building] — unlocks F-13
- [x] 3. F-14 — Print layout [approved]
- [x] 4. F-13 — Share by link [approved] — waits on F-12
```

O custo é um turno do Claude por clique, cada um respondendo uma linha. O rodapé do widget diz `As marcas ficam salvas a cada clique, só nesta máquina, fora do Git.`; o `docs/agile/order.md` continua com todas as caixas vazias.

### 14.109 Sem widget, a lista da ordem aparece num bloco de código com o motivo

Antes, numa sessão em que a `show_widget` estava só nomeada (deferida), o Claude não a carregava e colava as linhas da lista soltas na conversa. O chat desenha `- [ ]` solto como caixas, e elas não clicam: você via uma lista que parecia o widget e não fazia nada. Agora a ferramenta deferida é carregada e o widget abre. Só quando a ferramenta não existe (um terminal, uma execução sem interface) ou a chamada falha, as linhas vêm num bloco de código, seguidas de uma linha com o motivo e de como marcar. A saída abaixo é a do script no projeto de teste, com um item em andamento e um que espera por ele:

````
> /agile:order
Claude: agile order WRITTEN: 4 items, 0 delivered

```
- [ ] 1. B-2 — Total errado em reembolsos [approved]
- [ ] 2. F-12 — Export results to CSV [building] — unlocks F-13
- [ ] 3. F-14 — Print layout [approved]
- [ ] 4. F-13 — Share by link [approved] — waits on F-12
```

sem show_widget nesta sessão
diga "marque F-3" para marcar
As marcas ficam salvas só nesta máquina, fora do Git: o widget salva a cada clique, ou diga "marque F-3".
Não marque as caixas ao abrir docs/agile/order.md no painel de arquivos do app: lá o clique grava no arquivo.
````

Se a chamada da `show_widget` falhar, a linha do motivo é `o widget não abriu: <erro>` e o resto é igual. Dentro do bloco as caixas são texto: nada nele parece clicável e nenhuma frase pede para clicar. Para marcar, diga "marque F-3"; um tick salvo aparece como `- [x]` na próxima execução, e o `docs/agile/order.md` continua com todas as caixas vazias.

### 14.110 O release roda no GitHub Actions: o Claude dispara o workflow e espera

O projeto `shop` (perfil `web-app`, versão `0.4.0` na `main`) tem o `.github/workflows/release.yml`, escrito pelo `pipeline write` do `sync.js` junto com o pipeline de deploy. Digitar `/agile:publish` não empacota nada na sua máquina: o Claude confere o plano aqui (as paradas de hoje: árvore limpa, em dia com o `origin`, tag livre, uma versão), escreve as linhas do glossário e entrega o resto ao runner.

```
> /agile:publish
Claude: Plano: v0.4.0 (primeiro release), 1 projeto: Shop.Web. O release roda no GitHub Actions (release.yml); depois dele o pipeline implanta v0.4.0 em staging.
        Glossário: nada faltando.
        Disparando Release (deploy=true) e esperando a run...
        Release v0.4.0 feito no GitHub Actions.
        Run:    https://github.com/acme/shop/actions/runs/4242
        Release: https://github.com/acme/shop/releases/tag/v0.4.0
        Pacotes: nenhum neste projeto. Deploy: a run dispara o Deploy de v0.4.0 em staging.
```

O que o script respondeu nessa execução (o relatório inteiro de `publish.js ci`, de um projeto de teste com `gh` simulado):

```
{
  "ok": true,
  "mode": "ci",
  "version": "0.4.0",
  "tag": "v0.4.0",
  "steps": [
    "dispatched Release (release.yml --ref main -f deploy=true)",
    "run 4242: https://github.com/acme/shop/actions/runs/4242",
    "main brought level with origin (the notes commit and v0.4.0)"
  ],
  "run": {
    "id": 4242,
    "url": "https://github.com/acme/shop/actions/runs/4242"
  },
  "release": "https://github.com/acme/shop/releases/tag/v0.4.0",
  "pulled": true,
  "ci": {
    "workflow": ".github/workflows/release.yml",
    "target": "staging",
    "deploy": true,
    "line": "the pipeline deploys v0.4.0 to staging after the release",
    "dispatch": "gh workflow run release.yml --ref main -f deploy=true"
  },
  "warnings": []
}
```

Quando o ambiente que você digita é o destino do pipeline, o Claude não deixa o deploy acontecer duas vezes: o disparo leva `deploy=false` e o deploy local que segue roda uma vez.

```
> /agile:publish staging
Claude: ... deploy=false: staging is the pipeline's target and the local deploy follows, so it is not deployed twice
        Disparando Release (deploy=false) e esperando a run...
        Release v0.4.0 feito. Agora o deploy de v0.4.0 em staging, daqui, pelo Deploy command do docs/infra.md.
```

Um projeto cujo plano tem um head que o runner não empacota para antes de qualquer commit, tag ou disparo:

```
> /agile:publish
Claude: Não dá para fazer esse release no GitHub Actions: release in CI does not cover Acme.Desktop (Velopack) yet: run /agile:publish --local
        Nada foi escrito, commitado nem disparado. Para fazer o release aqui na sua máquina: /agile:publish --local
```

Pelo botão "Run workflow" do Actions o resultado é o mesmo (as notas commitadas, a tag anotada, o GitHub Release), sem o passo do glossário: o resumo da run diz `glossary not updated`. Uma `main` protegida que recusa o push do bot do Actions faz a run parar com a resposta do git, sem tag no `origin`, e o relatório diz qual regra relaxar ou que `/agile:publish --local` empurra como você.

### 14.111 A revisão diz o que conferiu e deixa você escolher onde há escolha

A F-1 do projeto `orders` (lista e criação de pedidos) está em `validating`, e a mudança mexe em permissões e em isolamento por tenant. O dono digita `/agile:review F-1`. O revisor devolve um relatório numerado: os achados, uma linha `Simplicity:`, as escolhas com custo, o que conferiu sem achado e o que não conferiu. O Claude confere cada bloqueador e grave no código e mostra a tabela com os números do relatório; a escolha com custo não entra na tabela, vira uma pergunta.

```
> /agile:review F-1
Claude: F-1 · Orders list and create · validating
        Todos os achados conferem com o código. Tabela para decidir:

| # | Severidade | Achado                                                                    | Veredito   | Ação      |
|---|------------|---------------------------------------------------------------------------|------------|-----------|
| 1 | blocker    | POST /api/v1/orders sem RequireAuthorization nem checagem de orders:write | confirmado | corrigir  |
| 2 | blocker    | OrdersStore.Add grava tudo no tenant fixo "t1"                            | confirmado | corrigir  |
| 3 | major      | o id devolvido no 201 nunca é salvo na linha                              | confirmado | corrigir  |
| 6 | major      | o teste Forbidden é Assert.True(true), nunca falha                        | confirmado | corrigir  |

        Escolha com custo nº 9 (cache de 60 s no GET: um pedido novo pode levar até 1 min para aparecer):
        - Sugestão do revisor (Recomendado): ler direto do store, sem cache. O item não pede desempenho e quem acabou de criar espera ver o pedido.
        - B: manter o cache como está, aceitando o atraso.
        - Manter como construído.

        Conferido sem achado: contratos, localização, dados, simplicidade, regras além dos achados 1 e 2;
        não conferido: lock de testes (o item não tem lock), o registro de DI do host (não há Program.cs na worktree).
        Simplicity: none
```

Cada linha de `Conferido sem achado` vem do relatório do revisor com o que ele leu (`contracts: none — ambas as rotas sob /api/v1/orders, Guid? Id = null`); o relatório inteiro, com o que foi lido, vai para `## Decisions` numa linha `Review coverage`. Uma lista vazia de achados não é mais uma revisão rasa: ela vem com as verificações que o revisor fez. Você responde pelo número ("corrigir 1, 2 e 6"; "manter o cache"). A sessão nunca responde a escolha com custo por você, nem no `--autopilot`: lá o código fica como construído e as escolhas aparecem no segundo ponto de parada, com as opções.

O diálogo acima é um trecho (4 dos 8 achados, em pt-BR) de uma execução real de `/agile:review F-1`, sem terminal interativo, sobre um projeto de teste com um endpoint sem autorização, um cache e áreas limpas; nenhum arquivo foi alterado.

### 14.112 Uma regra que já responde vira confirmação com a fonte, e no ship as decisões são classificadas

O projeto `invoicing` tem em `.claude/rules/agile/project.md` duas regras: "Money is rounded half-up to cents (F-12, 2026-10-07)" e "Every list endpoint paginates with page and size (F-8, 2026-10-01)". A F-2 (desconto percentual na fatura) tem uma pergunta aberta, "como arredondar o total com desconto?". Antes, o Claude via que a regra respondia e não perguntava: dizia "não vou perguntar de novo, só registrar", e você só descobria no fim. Agora a decisão é perguntada num cartão de confirmação, antes dos cartões de tema, com a fonte:

```
> /agile:refine F-2
Claude: F-2 · Invoice total with a percentage discount · refining
        Duas regras do projeto já respondem parte do que o arquivo deixa em aberto:

> Confirmação 1
> O total com desconto é arredondado para cima a partir de 0,5 centavo (round half-up),
> como decidido em project.md (regra válida desde F-12). Continua valendo aqui?
> a) Sim (Recomendado)   b) Não, mudar

> Confirmação 2
> A listagem de faturas pagina por page e size, como decidido em project.md (F-8). Continua valendo aqui?
> a) Sim (Recomendado)   b) Não, mudar

        Depois, os cartões de tema (regras de negócio, permissões, estados, telas, pacotes, fora do escopo).
```

Você responde "ok" e o arquivo termina assim: as duas respostas Sim entram em `## Decisions` com a fonte e o evento diz quantas perguntas o contexto já respondeu.

```
## Decisions
- 2026-10-12 — Money is rounded half-up to cents — the project rule answers it — confirmed: .claude/rules/agile/project.md
- 2026-10-12 — The invoice list paginates by page and size — the project rule answers it — confirmed: .claude/rules/agile/project.md
- 2026-10-12 — The discount is a whole number from 0 to 100 — keeps validation simple — asked

## Events
- 2026-10-12 question — asked 8, from context 2
```

No ship da F-3 (exportar faturas para CSV), a retro lê as `## Decisions` do item e as classifica por tipo. A F-3 tem uma decisão que mudou a regra de paginação ("o export não pagina: faz streaming"), um fato que só quem leu o código sabe (o export lê a view `invoice_audit`, nunca a tabela, que guarda faturas apagadas) e uma lacuna do plugin (o refinamento não perguntou quem lê o arquivo exportado). A regra de paginação já tinha sido quebrada de novo dentro do mesmo item (eventos `correction`, `rework` e `test-fail`), então a retro não propõe outra linha:

```
> /agile:retro F-3
Claude: Decisões do F-3, classificadas (as já confirmadas não entram de novo):

| # | Decision                                   | Kind        | Destination                                  | Source                                      |
|---|--------------------------------------------|-------------|----------------------------------------------|---------------------------------------------|
| 1 | O export não pagina, faz streaming         | project rule| um teste que separa lista de export primeiro | changed: Every list endpoint paginates (project.md) |
| 2 | O export lê a view invoice_audit           | hidden fact | .claude/rules/agile/export-project.md        | asked                                       |
| 3 | O refinamento não perguntou quem lê o CSV  | plugin gap  | docs/agile/retro-log.md, nota ⏳ plugin: agile | asked                                       |
Item decisions: 2, stay in the item

        1. score plugin 8 / project 8 — a regra quebrou de novo no mesmo item (correction, rework, test-fail):
           não uma linha nova; um teste de arquitetura primeiro, e a linha antiga fica até ele existir.
        2. score plugin 7 / project 3 — nota de plugin.
        3. score plugin 3 / project 6 — fato escondido, carregado só quando alguém lê a pasta do export.
```

Com o seu OK, o fato escondido é escrito pelo `rule-line.js`, com a fonte, num arquivo que só carrega quando a pasta do export é lida, e a linha antiga de paginação fica onde estava:

```
---
paths:
  - "src/**/Export*/**"
---
# Project rules

- The export reads the invoice_audit view, never the invoices table — the table keeps soft-deleted rows the view filters out (F-3, 2026-10-12)
```

O diálogo acima é um trecho (em pt-BR, abreviado) de duas execuções reais, `/agile:refine F-2` e `/agile:retro F-3` com o plugin da branch, sobre um projeto de teste com as regras e o item F-3 descritos; a mesma `/agile:refine F-2` com a 0.65.0 não perguntou a regra, e a mesma `/agile:retro F-3` propôs uma linha nova na regra de paginação, sem tipo nem fonte. Custo de cada execução: de US$ 0,22 a US$ 1,26.

### 14.113 O modelo mais forte só entra quando a tarefa é complexa, e a linha do projeto continua mandando

Antes de chamar `system-design`, `architect`, `reviewer` ou `ux-designer`, o Claude lê a régua (`references/model-choice.md`), diz uma linha `Model:` com o motivo e passa o modelo na chamada. Sem nenhum sinal de risco a chamada vai no modelo intermediário; com um sinal, ou na dúvida, no mais forte. Dois itens de um projeto de teste, o F-2 (uma coluna opcional `Note` na tabela de pedidos) e o F-3 (um webhook de pagamento com assinatura):

```
> /agile:build F-2
Claude: Model: sonnet (no signal)
        (system-design e architect chamados com model sonnet)

> /agile:build F-3
Claude: Model: opus (signal: a webhook, a public API contract)
        (system-design e architect chamados com model opus)
```

A linha "Models" do `CLAUDE.md` do projeto continua valendo. Com `Design and architecture passes: always opus`, o F-2 também roda no modelo mais forte, e a linha vira `Model: opus (project line: always opus)`. Com `Design and architecture passes: sonnet`, o F-3 roda em `sonnet` apesar do sinal, porque o plugin nunca passa um modelo acima do que a linha nomeia. Na revisão (`/agile:review`) o mesmo vale: um diff de duas linhas num arquivo de recurso roda o `reviewer` em `sonnet`; um diff que mexe em permissão e tenant, em `opus`. Na tela (`/agile:screen`), uma lista que repete o padrão do kit vai em `sonnet`; um estado novo, um componente fora do kit e uma ação destrutiva com confirmação vão em `opus`.

O diálogo acima é um trecho (abreviado) de execuções reais de `/agile:build F-2`, `/agile:build F-3`, `/agile:review` e `/agile:screen` com o plugin da branch, sobre um projeto de teste; os modelos são os `model` lidos das chamadas do agente.

### 14.114 O ship imprime quanto tempo o item gastou no gate, e nada é cortado por isso

A F-12 do acme-shop, o total do pedido, passou por nove rodadas do gate e do scan enquanto era construída e validada. Ninguém escreveu esses números: cada script anotou a sua rodada, em segundos, num caderno local (`agile-gate-passes.jsonl`, dentro da pasta `.git`), vermelhas incluídas. No ship, o `gate-passes.js summary F-12` soma o que o caderno guardou do item, imprime uma linha só e a escreve em `## Delivery`:

```
Gate time: 9 passes, 14 min 52 s — turn 5 (4 min 4 s), ship 2 (6 min 2 s), baseline 1 (3 min 10 s), scan 1 (1 min 36 s); 2 red
```

O relatório do ship mostra a linha ao lado de `Events:`, exatamente como o script a imprimiu, e o `/agile:retro` a lê primeiro: é a evidência para a pergunta "o ciclo deste item foi caro?". As linhas da F-12 saem do caderno e as de outros itens ficam; rodar o resumo de novo diz `Gate time: no pass recorded` e não escreve nada. A linha só mede: nenhuma rodada deixa de acontecer por causa dela.

A linha acima é a saída real de `gate-passes.js summary F-12` sobre um caderno de teste com as nove rodadas (cinco `turn`, dois `ship` e um deles vermelho, um `baseline`, um `scan`); `## Delivery` do arquivo do item ficou com a mesma linha e o caderno ficou sem linhas.

### 14.115 A mesma árvore verde não roda o gate de novo, e `--fresh` roda de propósito

A F-12 do acme-shop, o total do pedido, terminou o build com o gate verde. O ship vai rodar o gate de novo, mas nada do código mudou entre uma rodada e outra. A primeira rodada é inteira e guarda um selo da árvore (o conteúdo dos arquivos, sem `bin/`, `obj/` e sem o arquivo do próprio item); a segunda reconhece a mesma árvore e não compila nem testa nada:

```
sealed: gate green on this tree at 2026-10-13T11:09:27.894Z, 3 s not spent; --fresh runs it again
sdk 11.0.100-rc.1.26425.128 (.)
built Shop.slnx (2 s)
tested Shop.slnx (1 s)
agile gate GREEN
```

A linha `sealed:` vem antes e diz quantos segundos não foram gastos; os totais são os da rodada selada e o veredito é o de uma rodada real, com código de saída 0. Nada foi escrito: nem o baseline, nem o `docs/security/findings.md`, nem um evento de sequência vermelha. O ship aceita essa rodada como o GREEN do ship, porque o script a provou sobre a mesma árvore. O scan funciona igual, com um prazo: o selo dele vale 24 horas, porque uma vulnerabilidade publicada depois precisa ser achada.

Qualquer outro arquivo mudado invalida o selo, um byte que seja; o arquivo do próprio item não, porque o ciclo o escreve entre o scan do build e o do ship. Quando o dono não confia num resultado selado (a suíte é instável, um pacote flutua), `gate.js ship --fresh` e `scan.js run --fresh` rodam inteiros e a rodada verde troca o selo. No ship, `gate-passes.js summary F-12` conta cada acerto, fora do que foi gasto:

```
Gate time: 1 pass, 3 s — ship 1 (3 s), sealed 1 (3 s saved); 0 red
```

As duas saídas acima são a saída real de `gate.js ship` rodado duas vezes e de `gate-passes.js summary F-12` sobre um projeto de teste com a solução `Shop.slnx` e o item F-12 na branch `feature/F-12`; a primeira rodada imprimiu as mesmas linhas `built` e `tested` sem a linha `sealed:`.

### 14.116 O architect diz o que conferiu e deixa a escolha com custo para você

A F-2 do projeto de pedidos troca o campo `Notes` por `NoteText` (máximo de 500 caracteres) e cria um resumo por tenant. Muda o schema e cria uma rota, então o `/agile:build F-2` roda as duas passagens somente leitura antes do plano. O Claude diz o modelo da chamada e, depois que o `architect` devolve o relatório, confere cada linha nos arquivos. Antes desta versão, o relatório era uma tabela de quatro colunas (`Severity`, `What`, `Why it matters`, `What to do instead`): nove linhas (três bloqueadores, três graves, três leves), sem número, sem dizer o que foi conferido e sem separar nenhuma escolha. Agora o relatório vem assim (trecho, no inglês do agente):

```
## Findings
| # | Check | Severity | What | Why it matters | What to do instead |
| 1 | data | blocker | ... Any of the ~40 notes over 500 characters loses its text ... | ... against BR4 ... | write 002 with the guard as the first statement, as a real stop ... |
| 2 | scope | minor | Some parts of the proposal reopen what the item file already settles ... | The owner gets questions that are already answered ... | Remove Risk 2's and Risk 6's alternatives from the proposal ... |

## Trade-offs
| # | The choice | As proposed | Alternative | Architect's pick and why |
| 3 | The list cache after a create (Risk 5) | Leave `OrdersCache` alone. The summary is current right away, and the list can be up to 60 s old. | Clear the tenant's cache entry on create. The list and the summary match, but the POST handler becomes coupled to `OrdersCache` ... | **Recommended: as proposed.** The 60 s staleness is a choice F-1 already wrote down (`src/Orders/OrdersCache.cs:3`) ... |

## Checked and fine
- layout: none. Everything stays in `src/Orders/` (`Orders.csproj:1`, a single web project with no package references). ...
- rules: none. Read `.claude/rules/agile/api.md`: `/api/v1/orders/summary` is under `/api/v1/` and declares `.RequireAuthorization()` ...
- contracts: none. Adding `string? NoteText = null` at the end of `CreateOrder` (`OrdersEndpoints.cs:3`) is additive. ...
- simplicity: none. Read `OrdersStore.cs`, `OrdersCache.cs` and `OrdersEndpoints.cs`. ...

## Not checked
Not checked: none
```

Os números 1 a 3 são uma só sequência, achados primeiro. As quatro conferências sem achado dizem o que foi lido (arquivo, rota, contagem): uma lista vazia deixa de poder ser confundida com uma revisão rasa. A conferência que teve achado (`data` e `scope`) não se repete ali. Para você, o Claude resume numa linha:

```
Conferido sem achado: layout, rules, contracts, simplicity. Não conferido: nenhum.
```

e grava em `## Decisions` uma linha `Design coverage` com a lista inteira e o que foi lido. A escolha 3 não é um defeito: o resumo fica sempre atual e a lista, com cache de 60 segundos, pode ficar atrás. Quem decide é você, e ela chega como cartão, com a sugestão do `architect` primeiro, na mesma rodada das paradas que só o dono resolve (pacote de banco, tenant fixo, projeto de testes):

```
> ❓ Pergunta 4 — Cache da lista (60s) ficar fora de sincronia com o resumo
> a) Deixar como está — nenhum critério deste item compara lista com resumo.
> b) Limpar a entrada do cache do tenant a cada create, para os dois baterem sempre.
> Recomendação: a) Motivo: essa escolha de cache já é do F-1; mexer nela agora é mudança fora do que este item pede.
```

A sua resposta vira uma linha `design trade-off 3 ... owner chose as proposed | alternative` em `## Decisions`, e o plano dos 8 passos parte dela. No `/agile:autopilot` e com `--assume` nada disso para a corrida: ela segue a sugestão do `architect`, grava a linha como `assumed by autopilot` e o stop 2 lista a escolha com as duas opções e a que foi seguida; mudar depois é uma nota de mudança. Um achado que quebra o arquivo do item, uma regra, o perfil ou uma conferência do projeto nunca entra como escolha com custo: o Claude o move para os achados, com a gravidade da quebra.

As saídas acima são o relatório e as mensagens reais de uma execução de `/agile:build F-2` com o plugin da branch (modelo opus, 35 turnos, US$ 1,23) sobre um projeto de teste com a F-1 entregue e a F-2 aprovada; a mesma execução com o plugin anterior produziu a tabela de quatro colunas descrita no início (US$ 1,15, 38 turnos).

### 14.117 O item lista as páginas técnicas que toca, o build as escreve e o ship confere cada uma

A F-1 do acme-shop, o resumo do dia, acrescenta um comportamento (a dona abre o resumo e vê os totais) e dois campos num contrato que já tem página. No refinamento o Claude escreve em `## Docs` do arquivo do item uma linha por página, e é isso que você lê junto com o resto antes de aprovar:

```
## Docs
- flow: new docs/flows/order-summary.md — the owner opens the summary and sees the day's totals
- reference: update docs/reference/orders-api.md#Fields — the summary adds `total` and `count`
```

Um bug que muda esse fluxo depois lista só `update`; uma página `new` num bug é recusada. Um item que não toca nenhuma página diz `- none — <motivo>`. No build, as páginas são escritas junto com o código, a partir de `docs/agile/templates/tech-page.md`, e o Claude roda a conferência antes de passar o item para `validating`. Com a página de fluxo ainda por escrever, a página de referência existe mas nenhum índice foi escrito:

```
RED: docs/flows/order-summary.md: the page does not exist
RED: docs/reference/orders-api.md: docs/reference.md is not current (run techdocs.js index)
```

Cada linha `RED:` nomeia a página e o que falta; o ship para do mesmo jeito que para num gate vermelho. Com a página de fluxo escrita e os índices gerados, a mesma conferência fecha:

```
agile techdocs INDEX WRITTEN: 2 file(s)
agile techdocs GREEN: 2 page(s)
```

Uma segunda geração não muda nada (`agile techdocs INDEX UNCHANGED`), porque o índice é derivado do cabeçalho das páginas, sem data nem commit. O arquivo que a dona ou o time abre, `docs/flows.md`, começa com uma linha que diz que é gerado e que ninguém o edita à mão, e tem uma tabela `Page | Summary | Items` com uma linha por página, ordenada pelo título: o título da página como link para `flows/order-summary.md`, o resumo do cabeçalho e `F-1` na coluna dos itens.

As saídas acima são as que o `techdocs.js` imprimiu sobre um projeto de teste com o item F-1 e as duas páginas deste exemplo. O ship repete `index` e `check`, commita as páginas e os índices com o manual e diz `Technical pages: 2`.

### 14.118 Um item grande demais para uma sessão é cortado no refinamento, e o build para antes de escrever código

A F-1 do acme-shop, a área de gestão de pedidos, descreve num só resumo cinco coisas que se usam sozinhas, cada uma com tela e dados próprios: a lista com filtros, a exportação em CSV, o e-mail a cada mudança de status, a aprovação de um gerente e a trilha de auditoria. No refinamento o Claude lê o código antes de perguntar qualquer coisa, julga o tamanho (pelos sinais, sem contar nada) e mostra o corte, depois um cartão:

```
F-1 — Order management area [refining]

Proposta de corte (parte 1 é o próprio F-1, reduzido):
| Parte  | Título                          | Tamanho | Depende de |
| 1 (F-1)| Order management area           | M       | -          |
| 2      | Order status notification e-mails | S     | F-1        |
| 3      | Order approval workflow         | M       | F-1        |
| 4      | Order audit trail               | S       | F-1        |

Como seguir com o tamanho do F-1?
a) Dividir como proposto (Recomendado)
b) Manter inteiro
c) Outro corte (descreva)
```

Com "dividir", o F-1 continua com o mesmo id, a mesma branch e o mesmo épico, com o escopo reduzido à lista e à exportação, e a decisão fica em `## Decisions` (`size L judged, split as proposed`). As outras três partes viram ideias novas no mesmo épico, cada uma com `Depends on: F-1` e, no começo do `## Summary`, `Split from F-1 (2026-10-13)`; a tabela do épico ganha uma linha por parte e o plano de execução também. O refinamento segue com as perguntas já do F-1 reduzido.

No build o mesmo julgamento roda de novo, no plano do passo 5, depois das passadas de design e antes do primeiro teste. Um item aprovado antes da regra, cujo plano passa por três módulos com contrato e migração próprios, para sem escrever uma linha:

```
Build parado: o plano aciona o sinal de item grande (L) antes da primeira linha de código.
F-2 fica inteiro ou é cortado em partes antes de continuar o build?
a) Dividir como proposto: F-2 fica só com a fatura (Billing); dois itens novos, dependentes de F-2.
b) Manter inteiro: grava "size L judged, kept whole" e o build segue com os três módulos.
```

"Manter inteiro" grava a linha em `## Decisions` com o motivo e o build segue sem perguntar o tamanho de novo; "dividir" passa pelo `/agile:change` (nota de mudança com cada critério que saiu e o id que o recebe). Com `/agile:autopilot F-3 --assume` o corte nunca é assumido: a execução para na proposta com a frase `--assume não decide corte de item — essa pergunta é sempre do dono`.

As saídas acima são as mensagens reais de três execuções não interativas com o plugin da branch, sobre projetos de teste: o refinamento da F-1 (modelo opus, US$ 0,74 até o cartão), o build da F-2 (US$ 1,48 até a parada) e o autopilot da F-3 com `--assume` (US$ 0,59).

### 14.119 Toda tela ganha um wireframe em texto, e só a tela complexa leva também o mockup HTML

A F-1 do acme-shop, o resumo do dia, cria uma tela. No refinamento, junto com as perguntas, o Claude escreve em `## Screens and API` do arquivo um bloco `### Wireframe` com a tela inteira, e você lê o desenho antes de aprovar, no mesmo arquivo:

```
## Screens and API
- /summary — the day's totals

### Wireframe: Order summary
+--------------------------------------+
| Order summary          [Today v]     |
|--------------------------------------|
| Orders      12        [new]          |
| Total       US$ 1,240 [new]          |
| [ Export ]                           |
+--------------------------------------+
```

`[new]` marca o que o item acrescenta. Se a feature só acrescentasse um campo a uma tela que já existe, o desenho seria o da tela inteira com `[new]` no campo, para quem lê ver onde ele fica. Uma tela simples, como esta, fica só com o texto. Esta aqui tem vários estados (vazio, carregando, erro), então é complexa pela mesma linha do `/agile:screen`: ela também ganha o mockup HTML, e o `## Docs` lista a página de fluxo que vai guardar os dois.

No build, o Claude copia o desenho inteiro para a seção `## Wireframe` da página de fluxo e, como existe mockup, acrescenta a linha `Mockup:` com o caminho. Enquanto a página só tem `## Trigger` e `## Steps`, a conferência para:

```
RED: docs/flows/order-summary.md: no "Wireframe" section (the item draws a screen)
```

Com o desenho copiado mas sem a linha do mockup, ela ainda para, nomeando o arquivo que falta:

```
RED: docs/flows/order-summary.md: does not link the mockup docs/features/mockups/F-1-order-summary.html
```

Com as duas coisas na página, fecha:

```
agile techdocs GREEN: 1 page(s)
```

Quem abre `docs/flows/order-summary.md` num wiki vê a tela desenhada na hora; o HTML, que o wiki não abre, fica a um link de distância. Um bug que muda o layout depois lista `update docs/flows/order-summary.md#Wireframe`, e o Claude corrige o desenho na mesma página.

As saídas acima são as que o `techdocs.js` imprimiu sobre um projeto de teste com o item F-1, a página de fluxo e o mockup deste exemplo.

### 14.120 O desenho e a revisão dizem a armadilha: a escolha atraente que custa depois

A F-2 do projeto de pedidos cria a rota `GET /api/v1/orders/summary` e uma tabela de contagem, então o `/agile:build F-2` roda as duas passagens somente leitura. O relatório do `system-design` agora tem uma seção entre os riscos e os pontos em aberto, com até 3 linhas no formato `T<n> | a escolha atraente | onde ela tenta | o custo escondido | taken ou avoided` (trecho, no inglês do agente):

```
## Traps

T1 | Keep a counter table OrderSummaries updated by the create handler | F-2 Design, line 21 | It drifts on any path that skips the handler and needs a backfill and an atomic upsert, while BR3 (at most 200 orders) makes an indexed COUNT cheap | taken (owner's design; Risk 2, Open points 2 and 3)
T2 | Cut at 500 and drop Notes in the same migration | F-2 Design, line 20; 001_create_orders.sql:5,7 | Anything past 500 characters is lost silently and the down cannot restore it, against BR4 | taken (owner's design; Risk 1, Open point 1)
T3 | Answer the summary with OrdersCache.ListFor(tenant).Count | OrdersCache.cs:3-15, use case AC2 | The count is up to 60 s late right after a create and can disagree with the counter | avoided
```

Uma armadilha não é um risco (um jeito de a própria proposta falhar), não é um achado (um defeito) e não é uma escolha com custo dos dois lados: é o caminho que parece certo e que o item poderia tomar na confiança. A T3 é a que a proposta recusou; a T1 e a T2 são as que ela tomou, porque o desenho é seu, e por isso também aparecem nos riscos e nos pontos em aberto. A T2 toca a BR4 (nenhuma nota pode perder texto), e nesta execução o Claude a aceitou como mitigada pela guarda de tamanho que a migração ganhou; se a mitigação não fechasse a BR4, ela seria movida para os achados. O Claude confere o que cada linha cita no código (o cache está mesmo em `OrdersCache.cs:3`), descarta a que tem premissa falsa e move para os achados a que quebra regra ou critério. Para você, ele diz uma linha e grava outra em `## Decisions`; nenhuma vira cartão:

```
Armadilhas: T1 (tabela de contagem) e T2 (corte em 500 + drop de `Notes`) aceitas e mitigadas; T3 (resumo pelo cache) evitada.
```

```
2026-10-13: traps — T1 counter table updated by the create handler: taken (owner's design), mitigated with the migration 002 back-fill ...; T2 cut at 500 and drop `Notes` in the same migration: taken (owner's design), mitigated with the length guard above; T3 answer the summary from `OrdersCache`: avoided (would read up to 60s stale, could disagree with the counter).
```

No `/agile:review F-3`, outra mudança, o relatório do revisor ganha `## Traps` entre as escolhas com custo e o conferido sem achado. A armadilha do revisor é uma escolha que passa nas nove conferências hoje e custa depois, com número `T<n>` próprio, que nunca entra na sequência `#` dos achados, para os cartões e as linhas de `## Decisions` não mudarem de número:

```
## Traps
| T# | File:line | The attractive choice | Hidden cost | Why it passes the nine checks |
| T1 | src/Orders/OrdersEndpoints.cs:12 | Count with `store.ListFor(tenant.Id).Count`, reusing the existing list method. | Every summary call builds the tenant's full customer list just to count it. The cost grows with the number of orders ... (`SELECT` all rows instead of `COUNT(*)`). | It is correct, filtered by tenant, and reuses an existing helper with no new code. At today's volumes it is not a defect. |
| T2 | src/Orders/OrdersEndpoints.cs:12 | The summary reads `OrdersStore` directly, while the list endpoint (line 10) reads `OrdersCache` (60 s). | For up to 60 s after an order is created, the count and the list can disagree ... | BR1 and AC1 only ask for the right tenant's count. ... |
```

e o Claude mostra a linha `Armadilhas: T1 contar monta lista inteira; T2 vitrine em cache (60s) vs leitura direta podem discordar`, depois dos dois achados de teste, que continuam achados. Sem nenhuma, o relatório diz `Traps: none`, resposta válida: o agente não enche a lista para parecer completo, e o teto é 3. No `/agile:autopilot` e com `--assume` nada disso para a corrida; o stop 2 lista as armadilhas com as escolhas com custo.

As saídas acima são as de duas execuções reais com o plugin da branch (`/agile:build F-2` e `/agile:review F-3` sobre dois projetos de teste); com o plugin anterior, o mesmo desenho e a mesma revisão não tinham onde dizer isso: a escolha atraente ficava de fora ou virava um achado leve. A passagem `system-design` custou 17 878 tokens, contra 17 040 antes (+838).

### 14.121 O build sobe a escada de reuso antes de escrever e diz o que reaproveitou

No build da F-1 ("total do pedido com imposto"), a tarefa pede arredondar como o endpoint de pedidos já arredonda e conferir o tenant. O Claude lê o código que a mudança toca (passo 6) e, antes de escrever um helper, sobe a escada de reuso: o código já tem um arredondamento (`Money.Round2`) e uma checagem de tenant (`TenantGuard.Allows`), então nenhum dos dois é reescrito. O que a biblioteca base já faz (`decimal.TryParse`) também não vira helper. O relatório do build ganha uma linha `Reuse:`, copiada para `## Decisions` do item:

```
- Reuse: `Money.Round2` and `TenantGuard.Allows` (`src/Seed.Api/Program.cs`, `src/Seed.Api/Money.cs`, `src/Seed.Api/TenantGuard.cs`), the same helpers the existing order endpoint uses; `decimal.TryParse` (BCL) for the tax-rate check, no new helper written.
```

Quando nada serve, a linha diz o que foi procurado, para você e para o revisor conferirem: `Reuse: nothing found (<o que foi procurado>)`. O piso de segurança (validação nas fronteiras de confiança, autorização, isolamento de tenant, perda de dados, i18n, acessibilidade) nunca é cortado pela escada, e ela nunca adiciona pacote.

As linhas acima são de execuções reais do `/agile:build` com o plugin da branch sobre a seed do `benchmarks/` (a tarefa `total-with-tax` do #130), três execuções, em que duas escreveram a linha `Reuse:` no arquivo do item (a terceira usou os mesmos helpers e não a escreveu): a média foi de 16,3 linhas de fonte contra 21 da linha de base anterior, gate verde nas três, `Money.Round2` e `TenantGuard.Allows` chamados em todas, US$ 9,12 no total. Com três execuções a diferença de linhas está dentro do ruído; o que a medida prova é que o piso se manteve.

### 14.122 O plugin no Copilot CLI: a instalação e a tabela do que funciona

Você usa o Copilot CLI e quer o ciclo nele. A seção "Usar no Copilot CLI" do manual (seção 3) dá a instalação a partir de um clone local: o plugin registra a pasta como marketplace `canary` e responde que está carregado ao vivo, sem copiar nada. `copilot plugin list` passa a mostrar `agile@canary (v0.77.0) (enabled)`. A mesma tabela diz, parte por parte, o que foi medido: as 22 skills e os 5 agentes carregam e são listados; os comandos, os 6 eventos de hook, a variável `CLAUDE_PLUGIN_ROOT` (21 arquivos) e `AskUserQuestion` (1 arquivo) estão como `não medido`, e a seção diz que nada ali afirma que funcionam.

O check confere que a tabela e o repositório contam o mesmo. Esta é a saída real dele, sobre os arquivos do plugin desta versão:

```
ok   AC5: the report states the files that use CLAUDE_PLUGIN_ROOT (42)
ok   AC5: the report states the files that use AskUserQuestion (1)
ok   AC5: the report states the hook events (8)
ok   AC5: the report states the skills (27) and the agents (11)
```

Se um item futuro usar a variável em mais um arquivo, a linha `CLAUDE_PLUGIN_ROOT` fica vermelha até alguém atualizar o número da tabela, depois de reler a linha.

As medidas da tabela vêm de uma execução real do Copilot CLI 1.0.83 sobre a pasta do plugin (uma chamada de 8,28 AI Credits); a instalação local foi executada e desfeita depois, e a forma `<owner>/<repo>` do GitHub não foi.

### 14.123 O refino de um bug lista os chamadores da função da causa e o build refaz a lista antes de editar

Um bug: "meio centavo arredonda para baixo no total do pedido" (10,045 aparece como 10,04). A causa está em `Money.Round2`, que chama `Math.Round` sem dizer o modo de arredondamento, e a função é usada de três lugares. O relato nomeia só um deles. Antes de qualquer pergunta, o `/agile:refine` escreve em `## Cause` a linha `Callers:` com cada chamador e seu veredito. Esta é a saída real de uma execução do refino num projeto de teste com três chamadores:

```
Callers of Money.Round2:
- src/Orders/OrderTotal.cs:8 — same defect (the reported path ...)
- src/Reports/Weekly.cs:8 — same defect (its comment says "a half cent rounds up like the order screen")
- src/Invoices/InvoiceLine.cs:8 — relies on old behavior (its comment says invoice lines are rounded to even on purpose, to match the bank file)

Not searched: none (small solution, no reflection or DI-resolved rounding).
```

O chamador `relies on old behavior` vira pergunta do tema de regras, junto das duplicatas: corrigir a função e adaptar o chamador agora, ou corrigir só o caminho do relato e registrar o resto como dívida. Você escolheu o parâmetro com valor padrão, que mantém o resultado da fatura.

No build, antes da primeira edição de `Money.Round2`, o Claude refaz o grep. Se a lista é a mesma, escreve em `## Decisions` a linha `Callers (build): 3 checked, same as the refinement` e segue. Se outro item acrescentou um chamador depois do refino, ele para. Esta é a parada real do build num projeto de teste em que `src/Reports/Weekly.cs` entrou depois do refino:

```
Antes do primeiro edit em Money.Round2 ... reconferi as chamadoras.
Achado: apareceu uma 3ª chamadora que não estava na linha Callers: do arquivo do item:
- src/Reports/Weekly.cs:8 — Weekly.Total, veredito same defect
Isso é uma diferença em relação à refinação, então a regra manda parar aqui com um card;
a decisão vira nota de mudança (/agile:change B-1).
```

O conserto nunca é decidido dentro do build: você responde ao cartão e o `/agile:change` registra a nota. O custo das execuções foi de US$ 2,71 no total (quatro chamadas, uma delas parada por uma permissão de pasta).

### 14.124 O roteiro de validação em dois idiomas

O F-12 chega ao fim do build. O Claude escreve `## Validation script` em duas subseções, `### English` e `### pt-BR`, e mostra no chat só a versão pt-BR; a board recebe só ela, sob `Roteiro de validação`. Antes de entregar, ele confere o par. Esta é a saída real do conferidor num arquivo de teste em que o passo 3 existe só em inglês e o comando do passo 2 diz `--no-build` só em pt-BR:

```
step count differs: English 3, pt-BR 2
code block 1 differs
```

O Claude corrige as duas subseções numa edição, roda o conferidor de novo e ele diz `OK`. O código de saída é 0 e só então o roteiro é entregue. Um item antigo, com um idioma só, não dá erro: o conferidor diz `legacy: one language`, e o próximo `/agile:script` reescreve o roteiro como o par.

### 14.125 O roteiro de validação diz o caminho até o recurso, e o mapa de telas evita reler o código

No ExamPrep, a feature "transcrever questão" acrescenta o botão **Transcrever** ao cartão de questões. O build não escreve "no cartão de questões, clique em Transcrever": o roteiro diz como chegar lá, e explica o termo uma vez só.

```
1. [ ] Abra o app e entre com o usuário curador.
2. Abrir a edição
   - [ ] 2.1 No sidebar (menu lateral esquerdo), clique em Exames; a lista de edições aparece.
   - [ ] 2.2 Escolha a edição clicando nela.
3. [ ] Clicando em Questões, no sidebar, clique no botão "Transcrever" no canto superior direito.
```

O sidebar foi explicado no passo 2.1 e aparece curto no passo 3. O caminho vem da linha da tela no mapa `docs/agile/ui-map.md`; o Claude só abriu o código para a linha que o diff tocou. Quando a linha não existe e o código não confirma o menu, o passo diz `(caminho não conferido: nome da entrada de Questões no sidebar)` e a linha fica com `Verified: no`.

No ship, o mapa é conferido. Saída real do script sobre o cenário do exemplo, com o cartão de questões alterado depois da linha ser lida:

```
STALE: Question card: src/Exams/QuestionCard.razor changed since ccdebe5
agile uimap STALE: 1 stale, 0 unverified of 2 row(s)
```

O relatório do ship traz `Map: 1 stale` e nomeia a linha; o ship continua. Com todas as linhas em dia a última linha é `agile uimap GREEN: 2 row(s)`, e sem mapa nada é dito.

### 14.126 O corte do MVP: `/agile:mvp` propõe, você aprova epic por epic

No acme-shop há dois epics, `checkout` (corte atual: só o F-9, já entregue) e `catalogo`, sem corte. Quatro itens em aberto: o F-12 (Pagar com cartão, Must, depende do F-14), o F-13 (Cupom, Could), o F-14 (Carrinho, Should, do epic `catalogo`) e o F-15 (Lista de desejos, Could). Você digita `/agile:mvp checkout`. O Claude lê o brief e os epics e roda a lista de candidatos; a saída real, sobre esse cenário:

```
| Id | Epic | Priority | Status | Title | Waits on |
|---|---|---|---|---|---|
| F-14 | catalogo | should | approved | Carrinho | - |
| F-13 | checkout | could | approved | Cupom de desconto | - |
| F-15 | catalogo | could | approved | Lista de desejos | - |
| F-12 | checkout | must | approved | Pagar com cartao | F-14 |
cut catalogo: no section
cut checkout: F-9
agile mvp CANDIDATES: 4 open, 1 must, 2 epics
```

O brief pede pagar com cartão e nada sobre cupom ou lista de desejos; a semente é o F-12. O fecho das dependências é calculado pelo script, não de cabeça:

```
F-14 needed by F-12
F-12 seed
agile mvp CLOSURE: 2 ids
```

O F-14 é de outro epic e entra mesmo assim, porque o F-12 não funciona sem ele. O Claude mostra a tabela (MVP: F-12 e F-14, com o motivo `needed by F-12` no segundo; depois: F-13 e F-15; fora: nenhum) e a diferença contra o corte gravado:

```
enters: F-14, F-12
leaves: -
kept: F-9
agile mvp DIFF: checkout 2 enter, 0 leave, 1 kept
```

Um card pergunta "Escrever o corte do checkout?", com `Escrever (Recomendado)`, `Mudar a lista` e `Pular`. Você escolhe escrever. O arquivo `docs/epics/checkout.md` passa a ter `F-9, F-14, F-12` em `## First release cut` (o F-9 entregue fica; a ordem é a do `/agile:order`) e uma linha em `## Decisions`; o `catalogo` não é tocado:

```
chore(epic): first release cut of checkout
agile mvp WRITTEN: checkout 3 ids
- 2026-10-15 — first release cut written by /agile:mvp: 3 items (criterion: the brief, priority Must, dependency closure); from other epics: F-14 (catalogo)
```

O Claude propõe o commit `chore(epic): first release cut of checkout` e só o faz com a sua autorização. Depois, o `/agile:order` passa a mostrar `First release (checkout): 1 of 3 delivered; missing: F-14, F-12`. Quando o F-14 for entregue e você rodar `/agile:mvp checkout` de novo, a proposta parte dos itens ainda abertos, mantém o F-9 e o F-14 como entregues e mostra só o que entra ou sai. Publicar a versão continua sendo o `/agile:publish`.

### 14.127 O Copilot CLI: a variável de root e os hooks medidos, com o que funciona e o que não

Você tem crédito do Copilot e quer saber se o ciclo roda nele com a guarda de commit no lugar. A seção "Usar no Copilot CLI" do manual (seção 3) agora tem as linhas `CLAUDE_PLUGIN_ROOT` e `Hooks` medidas, cada uma com o comando, a versão do CLI (1.0.99) e o custo: oito chamadas `-p` de 0,23 a 0,46 AI credits, 2,31 no total. A leitura da tabela:

- `CLAUDE_PLUGIN_ROOT`: `parcial`. Num hook, o Copilot entrega `CLAUDE_PLUGIN_ROOT`, `COPILOT_PLUGIN_ROOT` e `PLUGIN_ROOT` e expande `"${CLAUDE_PLUGIN_ROOT}"` no texto do comando, então os 6 hooks do plugin acham seus scripts. Em texto de skill o que o modelo faz com a variável não foi medido.
- `Hooks`: `parcial`. `SessionStart`, `PreToolUse`, `PostToolUse`, `Stop` e `SubagentStart` disparam; `PreCompact` não foi alcançado. O `guard.js` real recusou um commit de item `approved` em `main`, mas o Copilot mostrou só `hook errored`, sem o motivo nem o desvio `# agile:main-ok`; e o matcher `^(Explore|Plan)$` de `SubagentStart` não casa com o agente `explore` em minúsculas.
- `Comandos` e `AskUserQuestion` seguem `não medido`, e a seção diz qual item os mede.

A seção também diz o que isso quer dizer para o ciclo e não converte nada: cada falha vira uma ideia à parte (a recusa com motivo e desvio, o matcher em minúsculas).

O check confere a tabela. Esta é a saída real dele sobre o estado deste exemplo (as linhas dos novos casos):

```
ok   #200 AC4: the CLAUDE_PLUGIN_ROOT row is measured (a state, not a bare "não medido")
ok   #200 AC4: the Hooks row is measured (a state, not a bare "não medido")
ok   #200 AC2: the report says whether PreCompact fired
ok   #200 AC2: the report says whether SubagentStart fired
ok   #200 AC3: the report says what guard.js did in Copilot (recusou, permitiu or falhou fechado)
ok   #200 BR6: the Comandos row stays "não medido"
```

Se alguém voltar uma dessas linhas para `não medido` sem dizer o motivo, ou tirar a versão ou o custo, o check fica vermelho. Para repetir só a sonda da variável de root, o manual traz a pasta de teste e um par de comandos (cerca de 0,2 AI credits).

### 14.128 O MVP por um objetivo: `/agile:mvp` com rótulo e texto

No acme-sql, um IDE de SQL, há dois epics: `conexao` (sem corte; o F-19, a tela inicial, já entregue) e `relatorios`, com um corte antigo que só tem o F-20 (Relatório mensal). Seis itens em aberto: o F-22 (Configurar o servidor), o F-21 (Listar os bancos do servidor, depende do F-22), o F-25 (Conectar no banco escolhido, depende do F-21), o F-23 (Executar consulta do editor, depende do F-25), o F-20 e o F-24 (Exportar resultado em CSV, Must). Você digita:

```
/agile:mvp mvp-conexao "o MVP precisa listar todos os bancos do servidor configurado, conectar no escolhido e rodar consultas digitadas no IDE ou abertas de um arquivo de script SQL"
```

A primeira linha diz o que foi lido: `label: mvp-conexao · epic: all · goal: o MVP precisa listar todos os bancos...`. Uma palavra sem aspas que não é slug de epic vira rótulo; se você tivesse errado o slug de um epic, veria isso aqui. O Claude roda a lista de candidatos; a saída real, sobre esse cenário:

```
| Id | Epic | Priority | Status | Title | Waits on |
|---|---|---|---|---|---|
| F-24 | relatorios | must | approved | Exportar resultado em CSV | - |
| F-22 | conexao | should | approved | Configurar o servidor | - |
| F-20 | relatorios | could | approved | Relatorio mensal | - |
| F-21 | conexao | should | approved | Listar os bancos do servidor | F-22 |
| F-25 | conexao | should | approved | Conectar no banco escolhido | F-21 |
| F-23 | conexao | should | approved | Executar consulta do editor | F-25 |
cut conexao: no section
cut relatorios: F-20
agile mvp CANDIDATES: 6 open, 1 must, 2 epics
```

O objetivo pede listar os bancos (F-21) e rodar consultas (F-23): são as sementes. O fecho das dependências vem do script:

```
F-22 needed by F-21
F-21 seed
F-25 needed by F-23
F-23 seed
agile mvp CLOSURE: 4 ids
```

A tabela põe F-22, F-21, F-25 e F-23 no MVP, cada um com a parte do objetivo que atende (ou `needed by`), e o F-24 em depois com `Must, not needed by the goal`: com objetivo, a prioridade Must não basta. O F-20 também vai para depois. Abrir a consulta de um arquivo de script SQL não tem item nenhum: é uma lacuna. Antes dos cards dos epics vem um card só dela, "Abrir script SQL de um arquivo: o que fazer?", com `Capturar como ideia (Recomendado)`, `Sugerir uma discussão` e `Ignorar`. Se você captura, o `/agile:idea` roda com esse texto, pergunta o epic e o item novo entra no corte nesta mesma rodada; se pede a discussão, o Claude escreve `/agile:discuss "abrir script SQL de um arquivo"` para você digitar quando quiser, e a lacuna fica contada no corte. Aqui você pede a discussão.

Depois, um card por epic. O do `conexao` mostra a diferença:

```
enters: F-22, F-21, F-25, F-23
leaves: -
kept: -
label: none → mvp-conexao
agile mvp DIFF: conexao 4 enter, 0 leave, 0 kept
```

O `relatorios` tem um corte gravado que o objetivo não usa, então recebe o card do corte vazio, para o MVP antigo não se misturar com o novo:

```
enters: -
leaves: F-20
kept: -
label: none → mvp-conexao
agile mvp DIFF: relatorios 0 enter, 1 leave, 0 kept
```

Você escolhe escrever os dois. A saída real das duas gravações:

```
commit: chore(epic): first release cut of conexao
agile mvp WRITTEN: conexao 4 ids
commit: chore(epic): first release cut of relatorios
agile mvp WRITTEN: relatorios 0 ids
```

O `docs/epics/conexao.md` passa a ter o rótulo no título e só os ids no corpo, e o objetivo na linha de `## Decisions`:

```
## First release cut: mvp-conexao
F-22, F-21, F-25, F-23

- 2026-10-15 — first release cut written by /agile:mvp: 4 items (criterion: the goal, dependency closure); label: mvp-conexao; goal: "o MVP precisa listar todos os bancos do servidor configurado, conectar no escolhido e rodar consultas digitadas no IDE ou abertas de um arquivo de script SQL"; gaps left: 1
```

O `/agile:order` passa a mostrar o rótulo na linha de cada epic:

```
- First release (conexao, mvp-conexao): 0 of 4 delivered; missing: F-22, F-21, F-25, F-23
- First release (relatorios, mvp-conexao): cut not defined
```

Semanas depois, você digita só `/agile:mvp mvp-conexao`. O Claude não pede o texto de novo: lê o último objetivo gravado para esse rótulo (`agile mvp GOAL: 2026-10-15 "o MVP precisa listar..."`), a primeira linha diz `goal: reused from 2026-10-15` e a proposta mostra só o que mudou contra os cortes gravados. Um rótulo sem objetivo gravado é recusado: `no goal recorded for <rótulo>: type it once with the text`.

### 14.129 A lista ordenada abre com o MVP: `/agile:order` põe antes o que o corte pede e o que ele precisa

No acme-shop há dois epics. O `checkout` tem o corte `F-12, F-14`; o `catalog` não tem corte. Cinco itens em aberto: F-11 (Product search, Should, do `catalog`), F-12 (Checkout with card, Must), F-14 (Order confirmation email, Should, depende do F-11), F-15 (Product reviews, Must, do `catalog`) e F-16 (Wishlist, Could, do `catalog`). Antes, o F-15 (Must, nível 1) vinha na frente do F-11 e do F-14, embora ele não esteja em corte algum. Agora você digita `/agile:order` e a lista abre com o que o MVP precisa: os dois itens do corte e o F-11, que o F-14 espera e que está fora do corte. O F-15 e o F-16 ficam em `## Later`.

Esta é a saída real do script sobre essa fixture (o arquivo, sem o cabeçalho de comentário):

```
# Order of implementation

## MVP

| # | Id | Title | Status | Epic | Depends on | Unlocks |
|---|---|---|---|---|---|---|
| 1 | F-12 | Checkout with card | approved | checkout | - | - |
| 2 | F-11 | Product search | approved | catalog | - | F-14 |
| 3 | F-14 | Order confirmation email | approved | checkout | F-11 | - |

## Later

| # | Id | Title | Status | Epic | Depends on | Unlocks |
|---|---|---|---|---|---|---|
| 4 | F-15 | Product reviews | approved | catalog | - | - |
| 5 | F-16 | Wishlist | approved | catalog | - | - |

## Checklist

### MVP

- [ ] 1. F-12 — Checkout with card [approved]
- [ ] 2. F-11 — Product search [approved] — unlocks F-14
- [ ] 3. F-14 — Order confirmation email [approved] — waits on F-11

### Later

- [ ] 4. F-15 — Product reviews [approved]
- [ ] 5. F-16 — Wishlist [approved]

## Summary

- Delivered: 0
- MVP: 3 open (2 in the cuts, 1 their dependencies)
- First release (checkout): 0 of 2 delivered; missing: F-12, F-14
```

Três coisas para notar. A numeração é uma só, de 1 a 5, e dentro de cada grupo vale a mesma regra de antes (o F-12 e o F-11 estão no nível 1 e o Must vem primeiro; o F-14 espera o F-11 e fica no nível 2). A linha `- MVP:` diz de onde vem o grupo: 2 itens estão nos cortes e 1 entrou porque o F-14 depende dele. E a linha `First release (checkout)` continua a de sempre, por épico: a união só ordena a lista, nunca é um release, e publicar continua sendo o `/agile:publish`.

Quando o F-12, o F-14 e o F-11 forem entregues, não sobra item de corte em aberto e a lista volta ao formato anterior, sem grupos e sem a linha `- MVP:`. No widget do app os grupos aparecem como cabeçalhos `MVP` e `Depois` sobre as mesmas linhas.

### 14.130 O ship imprime quanto do uso do modelo o item gastou, por etapa, e o status mostra o uso até agora

A F-12 do acme-shop, o total do pedido, foi refinada, construída e shippada numa sessão só. O Claude Code guarda no seu computador um arquivo por sessão com os tokens de cada resposta; o `usage.js` lê esses arquivos (só o modelo, o id, a contagem de tokens, o horário e as chamadas de skill e de agente, nunca o texto) e pesa cada token pelo preço relativo da API: a entrada vale 1, a leitura do cache 0,1, a escrita do cache 2 e a saída 5, vezes o peso da família do modelo. A etapa de cada resposta é a da última skill do agile carregada antes dela, e o item é o que a skill nomeou:

```
Usage: 3009.3k weighted (estimate) — refine 11% · build 62% · ship 27% — agents 1% — context per turn 118k → 235k — 1 session, 73 turns
```

O número é uma **estimativa**: o plano Max pesa os tokens de um jeito que a Anthropic não publica, então o preço é a melhor aproximação, e serve para dizer qual etapa pesa mais, não quanto falta do seu limite. A leitura aqui é a de sempre: quase todo o custo é o histórico lido de novo a cada resposta (o contexto por resposta cresce de 118k no refino para 235k no ship), e o build é a etapa mais pesada. O agente `architect` ficou com 1% do total. A linha da F-13, que a mesma sessão começou depois, não entra na da F-12:

```
Usage: 107.5k weighted (estimate) — build 100% — context per turn 124k — 1 session, 5 turns
```

O ship escreve a linha em `## Delivery`, ao lado de `Gate time:`, exatamente como o script a imprimiu, e o `/agile:retro` abre com as duas. Enquanto o item está em `building` ou `validating`, o `/agile:status` (e o começo de cada sessão) mostra uma linha `Usage (F-12): ...` com o que já foi gasto, e nada se não há transcrição. Sem a pasta de transcrições, ou sem nenhuma sessão que cite o item, a linha é `Usage: no transcript found`; uma linha ilegível é pulada e contada, `(1 lines skipped)`. Nada disso para o ship nem muda um gate: é só medida, e os cortes que ela justifica são de outros itens.

As duas linhas acima são a saída real de `usage.js summary F-12` e `summary F-13` sobre uma sessão de teste com os 73 turnos da F-12, os 5 da F-13 e uma resposta do `architect`.

### 14.131 O Claude propõe uma skill companheira uma vez e o catálogo recusa a que faz commit

No ExamPrep, a feature "importar gabarito" muda o contrato público de `/api/v1/exams` (um sinal da régua de modelos: contrato consumido fora do módulo). No refino, antes de fechar o desenho, o Claude escreve uma única proposta:

```
Esta feature mexe num contrato público, então vale uma segunda leitura de segurança.
O catálogo tem `safety-pal:review-safety` (só lê; para conferir o piso de segurança de um diff).
Rodo agora nesta worktree? Se disser não, não pergunto de novo neste item.
```

Você responde "sim"; a skill roda, o Claude confere o `git status` da worktree e do checkout principal (nada mudou fora dela) e relata em pt-BR o que ela achou. Numa tarefa sem nenhum sinal (um texto de tela, uma coluna a mais), nenhuma proposta aparece.

Uma linha do catálogo que nomeia uma skill que faz commit é recusada pela conferência. Saída real sobre um catálogo de teste com a linha de `agent-protocols:build`:

```
row 1 (agent-protocols@marketplace:build) names no record of benchmarks/companions.json
row 1 (agent-protocols@marketplace:build) names an excluded skill (it commits, merges or changes a status)
```

Com o catálogo vazio, que é o estado enquanto nenhum companheiro foi medido, a conferência passa e a linha fixa das regras ainda vale para as skills que a sessão oferece.

### 14.132 O agente worker existe, custa mais para uma espera em primeiro plano e a guarda o cobre

No ExamPrep, durante o ship, o Claude pensa em entregar a espera do `gate.js ship` (uma suíte de 150 s) ao agente `worker`, que roda em `haiku`. A régua de modelos (`references/model-choice.md`, seção "Mechanical tasks: worker") já responde: medido em três pares com e sem o agente, ele gastou de 9 a 11% mais, porque esperar em primeiro plano não custa nada à sessão e ela lê o arquivo de saída de qualquer jeito. O Claude então roda o gate ele mesmo, como sempre, e o ship segue igual.

O `worker` fica pronto para a busca do #217 (`find`: linhas `caminho:linha` ou `none`). Como os outros agentes do plugin, ele não pode procurar no checkout principal enquanto existe uma worktree de item. Saída real da guarda sobre um projeto de teste com a worktree `feature/F-7`, numa busca do `worker` feita no checkout principal:

```
[agile agent-guard] Grep from the `worker` agent targets the main checkout (D:/dev/_scratch/216/gd), not an item's worktree.
The item's code and status live in its worktree: D:/dev/_scratch/216/gd-wt [feature/F-7].
Repeat the call with the item's worktree (or a folder inside it) as the path; never report something as missing from a search rooted elsewhere.
Bypass, only with the owner's yes: restart Claude Code with AGILE_HOOKS=off (this turns off every agile hook).
```

A chamada termina com código 2; um agente com o mesmo nome vindo de outro plugin (`worker` sem o prefixo `agile:`) nunca é recusado.

### 14.133 Um plugin do Claude Code pelo bootstrap: a rodada do plugin e o esqueleto

O brief da Ana descreve um plugin que, no começo de cada sessão, lembra quem revisa dos pull requests que esperam por ele. Rodada 1:

```
Claude: 1. Tipo de app → Recomendação: Claude Code plugin. O brief fala de um comando
       e de um lembrete no começo da sessão, não de telas nem de uma API.
    2. Perfil de arquitetura → Recomendação: plugin (o único para um plugin).
> ok
```

No lugar das rodadas 2 a 8 vem uma só:

```
Claude: P1. Nome → Recomendação: pr-reminders (kebab-case; vira o nome do marketplace
       e o prefixo dos comandos, /pr-reminders:<verbo>).
    P2. Descrição → Recomendação: "Reminds the reviewer of open pull requests at the
       start of a session."
    P3. Autor → Recomendação: Ana Souza.  P4. Licença → Recomendação: MIT.
    P5. O que ele contém → Recomendação: skills e comandos; hooks quando o lembrete
       vier no começo da sessão (o brief pede, então: skills, comandos e hooks).
    27. Board → Recomendação: GitHub Issues + Projects (o código está no GitHub).
    28c. Raiz das worktrees → Recomendação: D:\wt\pr-reminders.
    28d. Merge → Recomendação: direct.  34. Modelos → as linhas de sempre, sem as de tela.
    20a. Idiomas do manual → Recomendação: pt-BR.
    Já está claro: um plugin não tem banco, telas, ambientes nem idiomas de UI;
    branch principal main, fluxo trunk.
> ok
```

Depois da pergunta de fechamento, do resumo e do "confirmo", o Claude descreve o esqueleto e, com o seu sim, copia o `templates/plugin/`: os dois manifestos em `0.1.0`, `skills/`, `commands/` e `hooks/hooks.json` sem hooks (a pasta `agents/` fica de fora, porque a P5 não a escolheu), o `README.md` com a instalação no Claude Code e no Copilot CLI e as três ferramentas em `tools/`. Não há solução, `Directory.Build.props` nem baseline de avisos. As duas checagens rodam uma vez sobre o esqueleto novo, antes do primeiro commit; saída real sobre o esqueleto do exemplo:

```
structure GREEN: 0 skills, 0 commands, 0 hooks (limits: 60 skill lines, 5 command lines, 250 description characters)
hooks GREEN: 0 hooks
```

O `CLAUDE.md` tem `Profile: plugin`, nenhuma linha de idiomas de UI (um plugin não tem texto de tela), e o `.claude/agile/build.json` declara as duas checagens como o comando de docs, o scan só com o gitleaks e os limites. O relatório final lembra que o `/agile:publish` lança um plugin da sua máquina (#222): a tag `v<versão>` e um GitHub Release com as notas, sem pacote.

### 14.134 O ship de um plugin parado pela checagem de estrutura, e depois a versão

No pr-reminders, a F-1 acrescenta o comando `/pr-reminders:remind` e, para ele, uma skill chamada `remind`. O build rodou as checagens à mão no primeiro arquivo e não depois do último. No ship, o passo de docs as roda de novo; saída real sobre o repositório do exemplo:

```
RED skill-command-name: skills/remind/SKILL.md:2: a skill named "remind" shares the name of commands/remind.md
structure RED: 1 problem
agile docs RED: docs.command exited 1: node tools/structure.check.js
```

O ship para ali, como para num gate vermelho: um comando e uma skill dividem o mesmo `/pr-reminders:<nome>`, e o Claude Code carregaria só um. Nada foi commitado pelo ship. A correção é renomear a skill para `pr-list` (a pasta e o `name` do frontmatter juntos) e apontar o comando para ela. Na segunda rodada:

```
ran docs.command (0 s): node tools/structure.check.js
ran docs.check (0 s): node tools/hooks.check.js
agile docs GREEN: 0 file(s) changed under tools/
agile gate OFF: .claude/agile/build.json says engine none (profile plugin: the structure check runs as the docs command)
```

O OFF mais o `agile docs GREEN` são o verde do merge. Então a versão, com a branch do item já com a `main` mergeada:

```
app version: profile plugin, node tools/plugin-version.js ship feature
plugin version: main 0.1.0 + feature -> 0.2.0
write .claude-plugin/plugin.json: 0.1.0 -> 0.2.0
write .claude-plugin/marketplace.json (pr-reminders): 0.1.0 -> 0.2.0
plugin version: both places read back as 0.2.0
```

Os dois arquivos entram no commit da versão, antes do merge. Se a sua skill precisasse de 80 linhas, o número a subir seria o `skillLines` do bloco `structure` do `build.json`, num commit próprio; a checagem passaria a aceitar 80.

### 14.135 O `/agile:screen` oferece o Stitch uma vez, com o motivo, e só age no seu sim

No ExamPrep, a feature F-12 "montar prova" tem uma tela com muitos estados e nenhuma tela parecida no app. O Stitch está conectado nesta sessão. Você digita `/agile:screen F-12`; o `ux-designer` volta, o Claude lê os dois arquivos e, antes de te mostrar o mockup, mostra um cartão:

```
Stitch
Por quê: não há tela parecida no app e esta tem seis estados.
Enviado ao Stitch: o texto da tela "Montar prova" do item F-12 e docs/design/DESIGN.md.
Baixado: F-12-exam-builder.stitch.html e .stitch.png em docs/features/mockups/ (worktree do item).
A geração leva alguns minutos e usa a sua conta do Stitch.

( ) Referência ao lado do mockup (recomendado)
( ) Usar o visual no mockup
( ) Não
```

Você responde "Referência ao lado do mockup". O Claude acha (ou cria) o projeto "ExamPrep" no Stitch, reaproveita o design system feito do `DESIGN.md`, gera uma tela, baixa os dois arquivos e escreve no bloco da tela a linha `Stitch: projects/<p>/screens/<s> (<data>)`. No relatório ele lista o que o HTML baixado referencia fora dele (por exemplo uma fonte remota) e te envia os dois mockups, o do `ux-designer` e a referência. Em `## Decisions` fica uma linha `Stitch offered` com `reference`.

Se você tivesse respondido "Usar o visual no mockup", o `ux-designer` seria chamado de novo com o caminho do HTML do Stitch e refaria o mockup mantendo todos os estados e o seletor de idioma, com as cores da identidade e sem nenhuma referência externa. Com "Não", nada é enviado nem criado e o cartão não volta neste item, a não ser que você digite `/agile:screen F-12 --stitch`. Sem o Stitch conectado nada disso aparece.


### 14.136 O Claude oferece uma sessão nova só quando o contexto passa do limite, e nada para por isso

No acme-shop, a F-12 (o total do pedido) é refinada e construída na mesma sessão. Logo depois do "Aprovo F-12", o Claude roda `usage.js fresh --at approval --id F-12`: o script lê a transcrição mais nova da pasta (a sessão em andamento), pega o contexto da última resposta e compara com o limite da parada, 120k na aprovação. Esta sessão está com 140k por resposta, então a linha sai no fim da mensagem:

```
Contexto desta sessão: 140k por turno; uma sessão nova abre em ~65k. Para economizar uso: /clear e depois /agile:build F-12.
```

A linha só é mostrada. Você pode digitar `/clear` e `/agile:build F-12`, e o build começa do item no disco com o mesmo rigor (o ship ainda confere cada critério nos arquivos), ou pode seguir na mesma sessão e pagar o que pagaria antes: nenhuma parada entra ou sai e nenhum gate muda. Numa sessão com 90k o mesmo comando não imprime nada e sai com código 0, porque uma sessão nova abre em cerca de 65k ponderados e perderia ao trocar.

Quando a F-12 chega a `validating`, o relatório do build termina com a linha da validação (limite de 80k, porque a sua espera costuma passar de uma hora e o cache de 1 h expira). Aqui o contexto é de 252k:

```
Contexto desta sessão: 252k por turno; uma sessão nova abre em ~65k. Para economizar uso: /clear e depois /agile:ship F-12.
```

Em `/agile:epic "Checkout" --autopilot`, depois de cada feature entregue o Claude roda `usage.js fresh --at epic`; com uma linha, a execução termina ali (evento `stop`, `fresh context between features`) com o comando de retomada `/agile:epic "Checkout" --autopilot`, e o estado está no disco; sem linha, ela segue para a próxima feature. O merge continua por item.

Para ver o número por trás da oferta, `usage.js fresh --replay <transcrição>` reprecifica uma sessão medida como se cada etapa depois da primeira abrisse nova. A saída real sobre uma sessão de teste com a forma medida da #201 (12 respostas de refino a partir de 119k, 30 de build a partir de 181k, 20 de ship a partir de 234k):

```
stage    turns      kept     fresh    saving
other        1     12.8k     12.8k      0.0k
refine      12    166.2k    196.4k    -30.2k
build       30    621.0k    413.0k    208.0k
ship        20    504.0k    281.0k    223.0k
total       63   1304.0k    903.2k    400.8k
Estimate, weights of references/usage-weights.json. Each stage after the first opens a fresh session (~65k weighted to open, a context of 90k = 77k open + 13k item read) and then grows as it did; the 1 h cache expiry is not counted, which favors fresh.
```

É uma estimativa, como o `Usage:`: o refino perde ao abrir nova (a sessão ainda era pequena), o build e o ship ganham, e é por isso que a oferta só aparece depois que o contexto passa do limite.


### 14.137 O plano do build: fatias verticais, na ordem das dependências, cada uma com a sua checagem

No acme-shop, a F-1 (o desconto do cliente na fatura) está aprovada. Ela cria uma tabela nova, `Billing.Discounts`, e um handler que a lê, `GetInvoiceHandler`, e o arquivo do item lista o handler antes da tabela (UC1, depois BR1). Você digita `/agile:build F-1`. Antes da primeira linha de código o Claude lê `references/build-plan.md`, monta o plano de acordo com ele e o grava numa só linha de `## Decisions`. A linha abaixo é a que a execução real escreveu na F-1 de teste (numa execução de verdade, não escrita à mão; o texto do arquivo é em inglês):

```
- 2026-10-16: plan — 1 `Billing.Discounts` migration (BR1) → AC3; 2 `IDiscounts`/`IInvoices` contracts and `GetInvoiceHandler` applying BR2 → AC1; 3 no-row path returns the full total → AC2; 4 unknown invoice answers `invoices.not_found` (BR3) → build
```

Repare nas três regras do método. A tabela vem em primeiro, mesmo estando em segundo no arquivo, porque o handler a lê (ordem das dependências). Cada passo é um caminho que funciona, não uma camada (nenhum passo diz "todas as entidades"). E cada passo termina com a sua checagem: o `AC` cujo teste ele deixa verde, ou `build` quando o passo é só o andaime ou a regra não tem critério próprio.

Se o item cria um projeto novo ou uma mensagem entre módulos, o passo 4b roda antes e o plano nasce do corte que o `system-design` propôs. O Claude passa a ele, e ao `architect`, o caminho absoluto de `build-plan.md`. Na F-2 de teste (o módulo `Loyalty` e a mensagem `InvoicePaid`), o `Slice` voltou em passos ordenados, por exemplo:

```
1. `Loyalty.Points` migration (BR1) → check: build.
2. Read path (UC1) → AC1. This step needs step 1.
4. `InvoicePaid` contract owned by Billing (BR3) → check: build. Step 5 reads it, so it comes before.
5. Consumer adds truncated points (UC2, BR2) → AC2.
```

Para ver o `architect` pegar uma ordem errada, a mesma F-2 foi dada a ele com um corte plantado ao contrário (os handlers antes da tabela que leem, o consumidor antes da mensagem que ele lê, e um passo que era uma camada inteira). Ele devolveu, sem criar uma sétima conferência, três achados da conferência `layout`:

```
| 1 | layout | major | Step 1 builds "all entities and all handlers of src/Loyalty at once (a layer alone, no criterion)" ...
| 2 | layout | major | The order is reversed. The step 1 handlers read `Loyalty.Points`, which only exists in step 4. ...
| 3 | layout | minor | No step names its check. ...
```

Quando você retoma um build parado (`/agile:build F-1` com a F-1 já em `building`), o Claude lê essa linha de `plan`, diz em que passo parou pelo último commit `wip` e continua dali; ele nunca reescreve a linha. Se o plano mudar no meio, acrescenta uma linha `plan v2` com o motivo. Se o plano passaria de 8 passos, o item é de tamanho L e o build para antes de qualquer código (`references/item-size.md`).

Se esse plano gasta menos tokens do que o plano solto de antes é uma hipótese, não um número prometido: a linha `plan` de cada item é a evidência daqui em diante.

### 14.138 O benchmark da companheira só mede o que serve a uma API .NET e para na primeira execução sem a skill

Você quer medir uma skill que não tem nada com um app .NET (aqui, a da pasta de fixtures do plugin `safety-pal`) e digita `--go`. O plano, sem o `--go`, já avisa o escopo em uma linha; com o `--go`, uma chamada barata pergunta ao haiku se a skill ajuda a construir uma API ASP.NET Core com testes xUnit. A resposta foi NÃO:

```text
$ node benchmarks/run.js --go --companion scripts/fixtures/benchmark-companion:review-safety
benchmark PROBE: ok (base arm sees 0 agile: skills, plugin arm 2)
benchmark PROBE: ok (companion arm lists safety-pal:review-safety)
benchmark REFUSED: safety-pal:review-safety does not apply to a .NET app (It creates plugins, it does not help build .NET code.)
exit 1
```

Você gastou centavos e nenhuma execução paga começou. Se a sonda errou numa skill de fronteira, `--applies-anyway` pula a pergunta e o relatório registra "applicability overridden by the owner". Se mesmo assim o modelo nunca chama a skill na primeira execução da companheira, o benchmark para ali, em vez de gastar o teto de US$ 80:

```text
$ node benchmarks/run.js --go --companion scripts/fixtures/benchmark-companion:review-safety --applies-anyway --task order-summary --runs 2
benchmark PROBE: skipped (--applies-anyway)
benchmark PARTIAL: benchmark-<stamp>.md (US$ 0.50 of US$ 25.00; some runs not measured)
benchmark ABORT: safety-pal:review-safety was not invoked in its first run (companion not invoked (safety-pal:review-safety; the run invoked no skill)); no further run started
exit 2
```

O relatório é parcial, tem `aborted` com o motivo, e o script de veredito da companheira sobre ele dá "não medido" (saída 2). Uma execução não invocada depois de uma medida não para nada: fica "não medida" e o laço continua. Uma tarefa de plugin (`--task plugin-skill`) não faz a pergunta de .NET, porque ela existe para skills que criam plugins.

### 14.139 Um plugin lançado pelo `/agile:publish`: a parada por versões diferentes e depois a tag e o GitHub Release

O acme-tools é um plugin do Claude Code criado com o perfil `plugin` (exemplo 14.133). O ship da F-3 subiu a versão para 0.2.0 em `.claude-plugin/plugin.json`, mas alguém editou o `marketplace.json` à mão antes e a entrada do plugin ficou em 0.1.0. Você digita `/agile:publish` e o plano para, sem escrever nada. Saída real do `publish.js plan` sobre o repositório do exemplo:

```
{
  "ok": false,
  "mode": "plan",
  "stop": [
    ".claude-plugin/marketplace.json says 0.1.0 for acme-tools but .claude-plugin/plugin.json says 0.2.0: one release has one version"
  ],
  "profile": "plugin",
  "projects": [],
  "pluginName": "acme-tools",
  "warnings": []
}
```

Claude: o plano parou: o `marketplace.json` diz 0.1.0 para o acme-tools e o `plugin.json` diz 0.2.0, e um release tem uma versão só. Corrija a entrada do `marketplace.json` para 0.2.0 (o `tools/plugin-version.js` do próximo ship faz isso sozinho), faça o commit, o push e digite `/agile:publish` de novo. Nada foi escrito.

Corrigida a entrada, você digita `/agile:publish` outra vez. O plano agora passa; o Claude o mostra numa linha e segue, porque digitar o comando é a autorização:

```
{
  "ok": true,
  "mode": "plan",
  "profile": "plugin",
  "projects": [],
  "pluginName": "acme-tools",
  "version": "0.2.0",
  "branch": "main",
  "tag": "v0.2.0",
  "notesFile": "docs/releases/v0.2.0.md",
  "previousTag": null,
  "mergeCount": 0,
  "warnings": []
}
```

```
Claude: plugin acme-tools 0.2.0 (tag v0.2.0, no package) · primeiro release · nenhum item desde a tag anterior
```

O `package` de um plugin escreve só as notas (`docs/releases/v0.2.0.md`) e não cria a pasta `artifacts/`; como o projeto não tem `docs/glossary.md`, as notas não trazem a linha do glossário. O `release` faz o resto:

```
{
  "ok": true,
  "mode": "release",
  "version": "0.2.0",
  "tag": "v0.2.0",
  "steps": [
    "committed docs/releases/v0.2.0.md",
    "tagged v0.2.0 (annotated, with the notes)",
    "pushed main and v0.2.0",
    "GitHub Release v0.2.0 created: https://github.com/acme/acme-tools/releases/tag/v0.2.0"
  ],
  "warnings": []
}
```

Claude: release v0.2.0 feito: commit das notas, tag anotada, push e GitHub Release só com as notas (um plugin não tem binários: o marketplace instala a partir do git). Nada foi empacotado. A próxima versão vem do próximo `/agile:ship`.

Um plugin não tem ambiente, loja nem nuvem. Se você digitar `/agile:publish staging`, o script para antes de ler qualquer outra coisa:

```
{
  "ok": false,
  "mode": "deploy",
  "stop": [
    "a plugin has no environment to deploy to: it is released by its tag, run /agile:publish --local"
  ]
}
```

O mesmo vale para `--beta`, `--park`, `--resume`, `--cost` e `--restore-drill` (cada um com o seu motivo) e para o release no GitHub Actions: um plugin é sempre lançado da sua máquina, e nenhum desses modos escreve nada. Se o `gh` não estiver instalado, ou o `origin` não for do GitHub, a tag é feita e enviada do mesmo jeito, e o relatório diz `GitHub Release skipped` com o motivo.

### 14.140 O modelo sugerido de cada item: gravado no refinamento, mostrado em `/agile:order` e dito no `/agile:build`

No acme-shop você refina a F-12, "Add a webhook for paid orders", e a F-13, "Show the paid date in the order list". Logo antes de pedir o "aprovo", o Claude decide o modelo de cada uma com a seção "Main session" da régua e diz isso no resumo:

Claude: Modelo sugerido para construir a F-12: opus (porta: webhook). Para a F-13: sonnet (nenhum sinal: sem porta, sem contrato público, sem esquema, sem módulo novo, tamanho M). A linha fica no `## Start` de cada item, depois de `Needed to validate`; quem troca o modelo da sessão é você.

```
- Needed to validate: nothing
- Model: opus (door: webhook)
```

A F-14, "Export orders to CSV", foi aprovada antes dessa mudança e não tem a linha. Você digita `/agile:order` e a lista mostra os três, a F-14 com `-`. Saída real do `order.js` sobre esses três itens:

```
| # | Id | Title | Status | Model | Epic | Depends on | Unlocks |
|---|---|---|---|---|---|---|---|
| 1 | F-12 | Add a webhook for paid orders | approved | opus | checkout | - | - |
| 2 | F-13 | Show the paid date in the order list | approved | sonnet | checkout | - | - |
| 3 | F-14 | Export orders to CSV | approved | - | checkout | - | - |

- [ ] 1. F-12 — Add a webhook for paid orders [approved] — model opus
- [ ] 2. F-13 — Show the paid date in the order list [approved] — model sonnet
- [ ] 3. F-14 — Export orders to CSV [approved]
```

Você abre uma sessão em `opus` e digita `/agile:build F-13`. O item sugere `sonnet`, e o Claude diz uma linha e segue, sem perguntar nada:

Claude: Model: item suggests sonnet, session is opus

Com `/agile:build F-12` na mesma sessão `opus` o Claude não diz nada: o modelo é o mesmo. A ordem da lista não mudou por causa da coluna, e nenhum item sem a linha foi alterado.

### 14.141 O ship das evals paga só o caso que mudou, estima antes e recusa acima do limite

O app tem quatro casos de eval e um `evals/evals.json` com o modelo `claude-haiku-5-5`, `runs: 1`, `ablation: "none"`, `limitUsd: 3` e um mapa de entradas: `prompts/system.md` vale para todos os casos e `prompts/refusal.md` só para `refuses-off-topic`. Ninguém mediu o custo de um caso ainda. No ship, o Claude roda o runner:

```powershell
node evals/run.js
```

```bash
node evals/run.js
```

```
no measured cost for cites-source: running it first (the only paid call before the estimate)
estimate: US$ 1.23 (3 case(s) × 1 run(s))
ok       grounded-answer: 1 (baseline 1)
ok       refuses-off-topic: 1 (baseline 1)
ok       cites-source: 1 (baseline 1)
ok       short-reply: 1 (baseline 1)
cost US$ 1.64, model claude-haiku-5-5, ablation none
evals GREEN
```

Um caso rodou sozinho para medir o custo; os outros três foram estimados com ele, ficaram abaixo do limite e rodaram, cada chamada com o teto da própria estimativa × 1,5 (você não digita teto nenhum). Os quatro casos verdes foram selados. No ship seguinte nada mudou nas entradas:

```
sealed: 4 case(s) green on an equal key, US$ 1.64 and 0 s not spent; --fresh runs them again
ok       grounded-answer: 1 (baseline 1)
ok       refuses-off-topic: 1 (baseline 1)
ok       cites-source: 1 (baseline 1)
ok       short-reply: 1 (baseline 1)
cost US$ 0.00, model claude-haiku-5-5, ablation none
evals GREEN
```

Nenhuma chamada paga; o veredito e o código de saída são os de uma rodada inteira. (Os segundos são 0 porque o runner deste exemplo é um stand-in que responde na hora; numa rodada real o selo guarda o tempo de verdade de cada caso.) Depois a feature muda `prompts/refusal.md`, e só o caso que lê esse arquivo roda:

```
estimate: US$ 0.41 (1 case(s) × 1 run(s))
sealed: 3 case(s) green on an equal key, US$ 1.23 and 0 s not spent; --fresh runs them again
ok       grounded-answer: 1 (baseline 1)
ok       refuses-off-topic: 1 (baseline 1)
ok       cites-source: 1 (baseline 1)
ok       short-reply: 1 (baseline 1)
cost US$ 0.41, model claude-haiku-5-5, ablation none
evals GREEN
```

Se `prompts/system.md` mudar (vale para os quatro) com o `limitUsd` baixado para 1, a estimativa passa do limite e nada é pago:

```
estimate: US$ 1.64 (4 case(s) × 1 run(s))
REFUSED: estimate US$ 1.64 (4 case(s) × 1 run(s)) over the limit US$ 1.00; nothing was paid
```

O código de saída é 4 e o ship para até você decidir (subir o `limitUsd`). Um caso que caiu abaixo da base (`DROP`), uma execução parcial ou um caso sem base (`NEW`) nunca é selado, e o próximo ship o roda de novo. `--fresh` roda tudo sem olhar os selos.

### 14.142 O roteiro com subpassos e caixas: marcar na tela, reabrir, e a evidência no ship

O F-12 chega ao fim do build. O roteiro tem 3 passos e, ao todo, 5 unidades que você executa: os subpassos `1.1`, `1.2`, `2.1`, `2.2` e o passo `3`, que tem uma unidade só e por isso leva a própria caixa. No arquivo todas as caixas estão vazias (`[ ]`); o Claude mostra o roteiro no chat e, logo abaixo, o widget, uma lista clicável com o contador `0 de 5 marcados`. Sem o widget (a sessão não oferece `show_widget`, ou a chamada falha) ele mostra as mesmas linhas num bloco de código, com uma linha dizendo por quê.

Você testa o passo 1 e o passo 3 e clica nas caixas. Cada clique manda uma mensagem com o conjunto inteiro:

```
> /agile:script F-12 ticks 1.1 1.2 3
Claude: agile validation TICKED: 3 of 5
```

Nada é gravado no arquivo do item, nenhum commit, nenhum envio à board: as marcas ficam em um arquivo da sua máquina, fora do Git, e valem para todas as worktrees do repositório. Dito em palavras, "marque 2.1" ou "limpe as marcas" fazem o mesmo. Se você pedir o roteiro de novo, em outra sessão, as caixas continuam marcadas:

```
1. Abrir o relatório   (done)
   [x] 1.1 No menu lateral esquerdo (sidebar) clique em Relatórios → a lista aparece.
   [x] 1.2 Clique no relatório mensal → a página abre.
2. Exportar
   [ ] 2.1 Clique em Exportar CSV → o arquivo baixa.
   [ ] 2.2 Abra o arquivo → tem 12 linhas.
3. [x] Troque o idioma para en → os rótulos mudam.
agile validation TICKED: 3 of 5
```

O passo 1 aparece como feito porque os seus dois subpassos estão marcados; esse estado é derivado e nunca guardado. O corpo do issue na board mostra o roteiro com as marcas que valiam no último envio do Claude; marcar uma caixa na página do GitHub não vale, e o próximo envio a sobrescreve.

Depois você acha um problema no passo 2.1 e o Claude reescreve só esse subpasso com `/agile:script F-12 step 2.1 ...`. Os subpassos que não mudaram, palavra por palavra, mantêm a marca; o que mudou a perde e você o valida de novo.

No ship, antes do commit, o Claude escreve a evidência em `## Delivery` e a posta como comentário no issue entregue. Saída real do script sobre o cenário do exemplo, com o passo 2 nunca testado:

```
1. Open the report
   [x] 1.1 (2026-10-17) In the left sidebar (menu lateral esquerdo) click Reports → the list appears.
   [x] 1.2 (2026-10-17) Click the monthly report → the page opens.
2. Export it
   [ ] 2.1 not tested Click Export CSV → the file downloads.
   [ ] 2.2 not tested Open the file → it has 12 rows.
3. [x] (2026-10-17) Switch the language to en → the labels change.
Evidence: 3 of 5 tested
```

Um caminho sem marca vira `not tested` e nunca impede o ship: digitar o ship é a autorização. Só depois de escrever a evidência o Claude apaga as marcas do item; se a evidência ou a limpeza falhar, o ship continua e diz isso em uma linha. Um roteiro antigo, de passos soltos ou de um idioma só, continua válido e aparece sem caixas.


### 14.143 As skills de companhia usadas: o evento `companion` e a proposta de absorção na retro

Durante o trabalho, o Claude propôs uma skill de outro plugin e você disse sim. Ao terminar o uso, ele registra um evento no item, no worktree dele:

```powershell
node "$env:CLAUDE_PLUGIN_ROOT/scripts/events.js" add F-9 companion "acme-tools@acme-market:design-review — design review of the export contract"
```

```bash
node "${CLAUDE_PLUGIN_ROOT}/scripts/events.js" add F-9 companion "acme-tools@acme-market:design-review — design review of the export contract"
```

Uma nota fora do formato `<plugin@marketplace>:<skill> — <para que serviu>` é recusada e nada é escrito. Uma proposta que você recusou, ou uma skill só lida, não é uso.

Na retro, o Claude roda `events.js companions` antes de reunir as lições. Num projeto com a skill `design-review` usada em F-3, F-7 e F-9, e `pair-test` usada duas vezes só em F-4, a saída real é:

```
Absorb: acme-tools@acme-market:design-review — uses 3, items 3 (F-3, F-7, F-9)
Companion: acme-tools@acme-market:pair-test — uses 2, items 1 (F-4)
```

O limite conta itens diferentes, não usos: `pair-test` tem 2 usos em 1 item e não é proposta. Para `design-review` a retro mostra, depois das lições e fora do teto de 3, a nota `⏳ plugin: agile [generic] score 6/3 — absorb acme-tools@acme-market:design-review: used in F-3, F-7, F-9`. Você aprova como qualquer nota de plugin; nada é absorvido sozinho. Depois que a nota entra em "Plugin notes — status", a mesma saída diz `already proposed` e a proposta não se repete, mesmo que você a recuse:

```
Companion: acme-tools@acme-market:design-review — uses 3, items 3 (F-3, F-7, F-9) — already proposed
Companion: acme-tools@acme-market:pair-test — uses 2, items 1 (F-4)
```


### 14.144 A lista de skills de companhia do projeto: a proposta do sync e a linha `Companions:` do início da sessão

Depois de atualizar o plugin, o `/agile:sync` vê que o projeto ainda não tem `docs/agile/companions.md` e acrescenta uma linha à tabela: "companions list — missing — create it with 1 installed companion". Nesta máquina só `design-review@acme-market` está instalado, então a proposta traz uma linha só, a do catálogo:

```
| design-review@acme-market | review | design review of an API contract | Reads | acme/acme-market |
```

Você diz ok e o Claude escreve o arquivo (o cabeçalho do template mais essa linha); o `CLAUDE.md` não muda, e um arquivo que já existisse seria deixado como está:

```powershell
node "$env:CLAUDE_PLUGIN_ROOT/scripts/companions.js" write
```

```bash
node "${CLAUDE_PLUGIN_ROOT}/scripts/companions.js" write
```

Mais tarde, numa outra tarefa, o Claude propõe uma skill que não está na lista, `pair-test@acme-market:tests`, e você diz sim. Ele pergunta se ela só lê ou também escreve; você responde que escreve, e a linha entra no arquivo, sem commit, para o próximo commit do item:

```powershell
node "$env:CLAUDE_PLUGIN_ROOT/scripts/companions.js" add pair-test@acme-market tests writes "writes test skeletons in the worktree" --source acme/acme-market
```

```bash
node "${CLAUDE_PLUGIN_ROOT}/scripts/companions.js" add pair-test@acme-market tests writes "writes test skeletons in the worktree" --source acme/acme-market
```

Numa máquina onde `pair-test` não está instalado, o início da sessão (e o `/agile:status`) imprime esta linha. O marketplace `acme-market` já é conhecido ali, então não há comando de `marketplace add`; o trabalho segue sem a skill, e o comando é seu para digitar:

````
Companions: 1 da lista de acompanhantes (docs/agile/companions.md) falta nesta máquina; o trabalho segue sem ele. Os comandos são seus para digitar:
- pair-test@acme-market (tests): não instalado
```powershell
claude plugin install pair-test@acme-market
```
```bash
claude plugin install pair-test@acme-market
```
````

Se `pair-test` estivesse instalado mas desativado, a linha trocaria o `install` por `enable`. Numa máquina sem o marketplace, os blocos trariam antes um `marketplace add` com o valor da coluna `Source`, `acme/acme-market`; sem `Source` na linha, ela diria para adicionar o marketplace antes. Com tudo instalado, ou sem o arquivo `installed_plugins.json`, nada é impresso.

O `companions.js check` avisa, sem editar, de uma linha cuja skill o catálogo exclui, de uma sem entrada no catálogo (`no catalogue`) e de uma sem `Reads/Writes`. Neste projeto ele não imprime nada, porque as duas linhas estão no catálogo e marcadas.

### 14.145 Senha de exemplo em arquivo de desenvolvimento passa; a mesma linha em arquivo de produção, não

No acme-shop, o Claude termina a F-15 (a conexão com o banco local) e commita só o `appsettings.Development.json`, que tem uma senha de exemplo. O commit passa, sem marcador e sem nada no stderr, porque o caminho está na lista de isentos. No mesmo trabalho ele também colocou a linha no `appsettings.json` e o commit seguinte, com os dois arquivos, é recusado só por esse:

```
Claude: git add src/App/appsettings.json && git commit -m "feat(F-15): local database connection"
        [agile secret-guard] Possible secret in the commit: src/App/appsettings.json:2 (password).
        Remove it, or name the setting instead (${DB_PASSWORD}, <password>); for fake test data put agile:allow-secret in a comment on that line. The value is not shown here.
        Exempt paths (a password or a URL with credentials only; never under Production or Prod): **/*.Development.*, **/*.Staging.*, **/*.Local.*, **/.env.local, **/.env.development, **/.env.staging, tests/**, **/*.Tests/**, **/fixtures/**, docs/features/mockups/**. Add yours with "- Secret-guard paths: <glob>" in CLAUDE.md.
        If this is a mistake, ask the owner; with a yes, repeat the command ending with the comment  # agile:secret-ok
Claude: O commit foi recusado: o `appsettings.json` não está na lista de isentos. Troquei a senha por `Password=<password>` e o `appsettings.Development.json` ficou como estava.
```

A lista padrão vale para senha e URL com credenciais; um token de GitHub, uma chave da AWS ou uma chave privada bloqueiam até dentro de `appsettings.Development.json`, porque um token colado ali pode ser real. Um caminho com `Production` ou `Prod` (`tests/appsettings.Production.json`) nunca é isento. Se o projeto guarda dados de exemplo em `config/seed/`, o dono acrescenta ao `CLAUDE.md` do projeto a linha `- Secret-guard paths: config/seed/**` e esses arquivos passam também; o plugin não escreve essa linha. A mensagem de commit e o texto de um `gh issue create` nunca são isentos: uma senha ali bloqueia mesmo que o arquivo de corpo se chame `appsettings.Development.json`.

### 14.146 A passada larga do `/agile:discuss --wide`: cinco ramos isolados, o crítico e as armadilhas

Num SaaS de lavanderias de bairro, você digita:

```
/agile:discuss --wide "Como reduzir o prejuízo das lavanderias com clientes que não aparecem no horário de retirada e pagamentos cobrados por telefone?"
```

O Claude faz o enquadramento de sempre (o brief, o `Order { Id, TenantId, CustomerId, PickupSlot, Total, Paid }` do código, o que está em aberto) e confere os três sinais. A pergunta é aberta, mexe com dinheiro e dados e não é um sim/não, então nenhum card aparece. Na mesma mensagem que chama os ramos, ele escreve:

```
Sinais de pré-voo (aberto, alto risco — toca dinheiro e dados —, pergunta não binária): os três batem.
Passada larga: 5 ramos — regulador, concorrente, orçamento zero, primeiro uso, tirar a premissa que sustenta tudo.
```

Os 5 agentes `diverge` saem numa mensagem só, cada um com o enquadramento da discussão e o seu enquadramento cognitivo por último. Cada um volta em cerca de 7 segundos com 5 ideias, sem ferramenta usada e sem nota. O ramo da premissa removida, por exemplo, escolheu "o pagamento tem que acontecer na retirada" e devolveu ideias como cobrar já na entrega da roupa. O do concorrente trouxe superlotar os horários como uma companhia aérea. Você não vê as 25 ideias cruas; vê o que o crítico tirou delas:

```
Opções (do conjunto largo)
A — Cobrar no drop-off, não na retirada: o Pix ou o dinheiro no momento da entrega (o campo Paid já existe), e um lembrete algumas horas antes do PickupSlot avisando a consequência de não aparecer. Esforço: S. Risco: o valor precisa ser conhecido na entrega.
B — Link de cobrança Pix automático na criação do pedido (não óbvia), com uma taxa de no-show limitada e divulgada antes, e um registro de eventos por pedido para sustentar uma disputa. Esforço: M. Risco: depende de um gateway que hoje não existe; exige texto de consentimento explícito.
C — Depósito no drop-off, o saldo na retirada, mais lembrete e taxa de no-show. Esforço: M. Risco: a cobrança do saldo por telefone continua.
Armadilhas:
- T1 — Superlotar os horários como companhia aérea: a lavanderia tem capacidade física fixa, e não há dado de taxa de no-show para calibrar.
- T2 — Abandonar o horário de retirada: elimina o problema por definição, mas destrói a agenda, que é o valor central do produto.
- T3 — Guardar a roupa como garantia e cobrar armazenagem: passivo jurídico (até quando reter o bem do cliente?) e não resolve quem nunca volta.
Recomendo: A — ataca o no-show e a cobrança por telefone só com o que já existe, é reversível e gera o dado real antes de investir em gateway.
```

Depois vêm as perguntas numeradas do passo 3, como sempre. Quando você decide, o arquivo `docs/discussions/D-<n>-<slug>.md` ganha, entre `## What we already know` e `## Options`:

```
## Wide pass
- Frames: regulator, competitor, zero budget, first-time user, remove the load-bearing assumption (payment at pickup); no branch failed
- Clusters: charge before pickup (drop-off Pix, deposit, payment link); remind and confirm (WhatsApp, one-tap confirm); absorb the loss (surcharge, overbooking, storage fee)
- Traps: T1 overbooking — fixed physical capacity, no no-show rate to calibrate; T2 drop the pickup slot — destroys the core scheduling value; T3 hold items as collateral — legal liability, does not reach the customer who never returns
- Non-obvious pick: automatic Pix charge link at order creation — viable once a gateway exists, with consent text and an event log
```

Esta passada custou US$ 0,48 na execução de validação do #120 (a sessão principal mais os 5 ramos, todos em `sonnet`). Se a sua pergunta fosse "Devemos aceitar cartão, sim ou não?", o Claude diria numa linha que falhou o sinal de pergunta aberta e mostraria o card `Run wide anyway` / `Normal step 2` antes de chamar qualquer ramo. E sem o `--wide`, o passo 2 seria o de sempre, sem nenhuma menção à passada larga.

### 14.147 A suíte da passada larga: um caso medido primeiro, depois a suíte com limite

Você quer saber se o `--wide` dá opções melhores que o passo 2 de hoje. Antes, lê e reescreve os 4 problemas de `benchmarks/wide/problems/` (e os hashes de `suite.json`). Sem argumento, o script só mostra o plano e não chama nada:

```text
$ node benchmarks/wide.js
wide suite v1: 4 problems x 3 runs x 2 arms (step2, wide) on sonnet, judge opus (another family)
  import-records  How should a clinic that joins us bring in the patient records and the future appointments it keeps
  clinic-leaves  What should happen when a clinic stops paying or decides to leave us? It holds years of health recor
  no-shows  How could we help clinics lose less money to patients who book and never show up? It is the complain
  offline-desk  How should the front desk keep working when the clinic's internet line drops for an hour in the midd
Nothing started and nothing paid. First one measured case: node benchmarks/wide.js --measure; then the suite: node benchmarks/wide.js --go --max-cost-usd <n>
```

Se você pular direto para a suíte, ela recusa sem gastar nada:

```text
$ node benchmarks/wide.js --go --seed 42
wide suite REFUSED: --go needs --max-cost-usd <n>: run --measure first, it prints the limit to use (BR7)
```

O caso medido roda os dois braços uma vez no primeiro problema e depois o juiz, sem limite, e sugere o limite da suíte. Saída do script sobre o `claude` falso do check (cada chamada custa US$ 0,37 ali; numa execução real, os valores são os que a CLI do Claude informar):

```text
$ node benchmarks/wide.js --measure --seed 3
import-records run 1: step2 US$ 0.37, wide US$ 0.37; judge US$ 0.37
results: benchmarks/results/wide-measure-<stamp>.json and .md (seed 3)
measured: US$ 1.11 for one case (2 arms + judge); suggested --max-cost-usd 17 (x 4 problems x 3 runs + 25%)
wide suite DONE (total US$ 1.11)
```

O relatório traz a tabela do problema, com a coluna "Shown as" dizendo qual resposta o juiz viu como X:

```text
| Run | Arm | Shown as | breadth | novelty | traps | Cost US$ | Judge US$ | Traps named (judge) |
|---|---|---|---|---|---|---|---|---|
| 1 | step2 | Y | 5 | 1 | 2 | 0.37 | 0.37 | none named |
| 1 | wide | X | 10 | 6 | 4 | 0.37 | 0.37 | names the lock-in of a custom format |
```

Com o limite em mãos, você roda a suíte inteira com ele. Se um problema for editado sem subir a `version` de `suite.json`, o script recusa nomeando o problema; se você pedir um juiz da mesma família dos braços (por exemplo `claude-sonnet-5-5`), também recusa, antes de qualquer chamada.

### 14.148 Um tema embutido a mais e um par de contraste recusado

O app desktop Cais já tem a identidade com chaves próprias (`Surface`, `Surface.Page`, `Text`, `Text.Muted`, `Accent`, `Focus`, `Env.Production.Bg`, `Scrim`), os pares de contraste declarados e a pasta `docs/design/themes/`, criada pelo item do kit de UI. Você quer um segundo tema, âmbar:

```
> /agile:identity
Claude: Identidade registrada: tema Cais (claro e escuro), 9 chaves, 5 pares de contraste, 2 chaves bloqueadas
(Env.Production.Bg, Focus). O que muda? A) uma cor  B) adicionar um tema embutido  C) fontes ou raio  D) nada
> B, amber
Claude: As mesmas 9 chaves, claro e escuro. Quais as cores do tema amber? (ou eu derivo de uma cor base)
> deriva de #9A4A00, e o texto do escuro quero #8F7D5F
Claude: Nada foi escrito ainda. Tabela do tema amber, claro e escuro... Um par não passa:
Text sobre Surface no escuro dá 4.49:1, abaixo de 4.5:1 (o mais próximo que passa: #907E5F). Troco?
> não, confirmo assim
Claude: Escrevi docs/design/identity.amber.tokens.json e rodei a geração dos temas embutidos:
```

```
amber-dark Text on Surface 4.49 < 4.5 nearest #907E5F
REFUSED: 1 contrast pair below the minimum
```

```
Claude: A geração recusou: um tema embutido que falha um par nunca é escrito, e os arquivos do tema Cais
continuam como estavam. Text sobre Surface no escuro do amber: 4.49 contra o mínimo 4.5. Opções: A) usar
#907E5F, a cor mais próxima que passa (recomendado: a diferença não se vê); B) outra cor sua.
> A
Claude: Troquei o texto do escuro do amber para #907E5F e rodei de novo:
```

```
high contrast: 1 colors adjusted, 0 of them backgrounds
WROTE 7 files
```

```
Claude: docs/design/themes/ tem agora cais-light.json, cais-dark.json, cais-hc-light.json, cais-hc-dark.json,
amber-light.json, amber-dark.json e o index.json, com o Cais como padrão. Os dois temas de alto contraste
são do tema padrão: o amber não tem um. Nenhum código do app foi alterado.
```

Os seis arquivos de tema e o índice são gerados, nunca editados à mão; o formato está em `references/theme-format.md` do plugin. A chave `Accent` marcada como bloqueada no arquivo do amber não entra na lista de bloqueadas: só o tema padrão bloqueia chaves.

### 14.149 Um atalho deliberado: marcado no build, sem gatilho corrigido, oferecido como ideia na retro

Você está construindo o F-12, a busca de pedidos do app Cais. O Claude usa uma varredura linear e um lock global, de propósito. Cada um ganha uma linha de comentário com o limite e o gatilho para revisitar: `// shortcut(F-12): linear scan of the rows — revisit when a tenant passes 5,000 rows` na busca e `// shortcut(F-12): one global lock` no cache. A do lock ficou sem gatilho.

Antes de marcar `validating`, o build confere os marcadores do item. O script lista só os que não têm gatilho e sai com 1:

```text
src/Orders/Cache.cs:4 | one global lock | no-trigger
Shortcuts: 2 marked (1 no-trigger: src/Orders/Cache.cs:4)
```

```
Claude: Um marcador não diz quando revisitar: src/Orders/Cache.cs:4, "one global lock". Sem gatilho ele nunca
é revisitado. Qual é o gatilho? (recomendo "throughput matters", o que você disse na refinação)
> throughput matters
Claude: Completei o gatilho no marcador e conferi de novo: nenhum no-trigger. O relatório do build traz a linha
Shortcuts: 2 marked (src/Orders/Cache.cs:4, src/Orders/OrderSearch.cs:4), copiada para ## Decisions.
```

Você valida e dá o `/agile:ship F-12`. Depois do merge, no passo da retro, o ship lê os marcadores que os commits do item acrescentaram, do commit de merge contra o primeiro pai:

```text
src/Orders/Cache.cs:4 | one global lock | throughput matters
src/Orders/OrderSearch.cs:4 | linear scan of the rows | a tenant passes 5,000 rows
Shortcuts: 2 marked (src/Orders/Cache.cs:4, src/Orders/OrderSearch.cs:4)
```

```
Claude: Lições do F-12: nenhuma nova. Atalhos deixados (fora do limite de 3 lições):
  1. src/Orders/Cache.cs:4 — um lock global — revisitar quando throughput matters
  2. src/Orders/OrderSearch.cs:4 — varredura linear das linhas — revisitar quando um tenant passar de 5.000 linhas
Quais viram ideia? (nenhum vira sem a sua escolha)
> 2
Claude: Em qual épico? (sugiro o do F-12, "Busca", para o lote todo)
> Busca
Claude: Capturei a ideia F-31 no épico Busca: "Trocar a varredura linear de OrderSearch". O Summary cita
src/Orders/OrderSearch.cs:4, o limite e o gatilho, e traz a linha "Source: shortcut marker of F-12".
```

Um item que não tomou atalho nenhum fecha com a linha `Shortcuts: none` no relatório e a retro não oferece nada. O marcador não derruba nenhum gate: ele só garante que cada atalho tenha o dia de ser revisitado escrito ao lado.

### 14.150 O plugin oferece só pt-BR: o padrão da pergunta 20, outros idiomas como tags e o seletor só com dois

A acme-shop é um projeto novo. Na pergunta 20 do `/agile:bootstrap` só `pt-BR` aparece como opção; qualquer outro idioma é digitado como tag em "outras tags". O dono quer espanhol também e digita `es`:

```
> /agile:bootstrap
Claude: ❓ Em quais idiomas o app terá textos de tela?
        a) pt-BR (padrão)   b) outras tags (vários podem ser escolhidos; a ordem da resposta é a ordem da linha, o primeiro é o idioma base)
        Recomendação: pt-BR. Motivo: cada idioma extra é um conjunto de textos a escrever e manter em toda tela; o primeiro da lista é o idioma base do app.
> a e, em outras tags, es
Claude: CLAUDE.md em "## Languages": - UI locales: pt-BR, es
        Directory.Build.props: <NeutralLanguage>pt-BR</NeutralLanguage>
        Resources/: App.resx (pt-BR, o arquivo neutro) e App.es.resx.
```

Com dois idiomas, o mockup de uma tela e o roteiro de validação trazem um seletor de idioma, como sempre. Se o dono tivesse respondido só `a`, a linha seria `- UI locales: pt-BR` e nem o mockup nem o roteiro teriam seletor: com um idioma só não há o que trocar.

Três projetos que já existiam, lidos pelo `/agile:sync` (a saída é a do script em cada um):

```
"uiLocales": { "line": ["pt-BR","pt-PT","en"], "proposed": "pt-BR" }   <- projeto com a forma antiga: três idiomas
"uiLocales": { "line": ["pt-BR","es"], "proposed": "pt-BR" }           <- projeto da acme-shop acima
"uiLocales": { "line": null, "proposed": "pt-BR" }                     <- projeto sem a linha
```

Os dois primeiros não ganham linha na tabela do sync nem reescrita: a lista que eles têm continua valendo, `pt-PT` e `en` incluídos. Só o terceiro ganha a proposta, e o dono a aceita:

```
> /agile:sync
Claude: | arquivo | estado | o que proponho |
        | ui locales | sem linha | escrever `- UI locales: pt-BR` |
        Posso escrever?
> ok
Claude: Escrito em "## Languages". Nenhum arquivo de recursos foi tocado.
```

Até o dono aceitar, o build desse projeto diz uma vez `UI locales: sem linha, usando pt-BR` e exige só o idioma neutro. Um `.pt-PT.resx` ou `.en.resx` que ele já tenha fica fora da lista: nada é apagado, nada é exigido.

### 14.151 Uma feature com porta ganha a seção Security e um critério [sec] por ameaça; uma troca de texto não ganha nada

Você refina o F-1 do app Loja, "Partner sign-in and order webhook": o parceiro entra com e-mail e senha, o sistema dele posta atualizações de pedido em `POST /api/v1/webhooks/orders` e o app guarda o nome e o e-mail do contato. O Claude lê o código (passo 6) e vê uma porta pelo sinal 1 de `references/model-choice.md`: login, uma chamada de entrada de outro sistema e dado pessoal. Antes de qualquer pergunta ele lê `references/security-section.md` (passo 7d) e a rodada ganha um tópico de segurança, depois de permissões. A rodada continua uma só.

A retenção do dado pessoal não é perguntada: a linha da 9b do `docs/decisions/ADR-0001-foundation.md` já diz 24 meses depois do último pedido, apagado por um job de retenção, lido só pela equipe. O Claude diz isso na linha de confirmações e pergunta só o que a 9b deixou em aberto (a base legal, por exemplo). Com as respostas, o arquivo ganha a seção, entre `## Screens and API` e `## Docs` (trecho):

```text
## Security
### Surfaces
- S1 POST /api/v1/webhooks/orders — the partner's own order-management system, over the internet, with no signed-in session
- S2 POST /api/v1/partners/sign-in — anyone on the internet
### Threats
- T1 (S1, S) Another party sends a status update claiming to be a partner's system it is not → AC5 [sec]
- T3 (S2, S) Repeated password guesses against one partner's e-mail (online brute force) → AC7 [sec]
- T4 (S1/S2, I) A sign-in or webhook failure reveals which part was wrong (that the e-mail exists, or that the order exists) → AC8 [sec]
### Personal data
| Datum | Lives in | Read by | Kept for | Legal basis |
| --- | --- | --- | --- | --- |
| Partner contact name and e-mail | Partners table | Staff only; never leaves the network | 24 months after the partner's last order, then deleted by a retention job | Legitimate interest — needed to reach the partner about an order |
```

Cada ameaça tem o seu critério em `## Acceptance criteria`, no formato `AC5 [sec] (T1) Given ..., when ..., then ...`. No build eles viram linhas de `## Criterion → test` como qualquer outro, o teste é travado e o ship confere que nenhuma ficou `pending`. Uma ameaça sem critério não fica: ou o critério é escrito, ou a ameaça é riscada com o motivo em `## Decisions`. Depois, `/agile:review` lê a seção; uma ameaça sem critério `[sec]` ou sem teste é um `blocker`.

Já no F-2, "Empty-state wording", que troca duas frases em arquivos de recurso, sem rota, dado nem permissão, o Claude não acha porta: a rodada não tem tópico de segurança, o arquivo não tem `## Security` e a seção do template é removida. Um bug também não ganha a seção; o revisor cobre a porta de um bug no diff.

### 14.152 O zip de cada runtime do head desktop vai junto ao GitHub Release, com a linha de `chmod +x`

A acme-stock é um app desktop sem fonte de atualização no `docs/infra.md`: ele continua sendo um zip, e a lista de runtimes do `.csproj` é `win-x64;linux-x64`. O dono digita `/agile:publish --local` numa máquina Windows. O empacotamento faz um zip por runtime; no `release`, o Claude cita a lista de arquivos que o relatório traz e a chamada do `gh` (a saída é a do script no projeto de teste, com o caminho da pasta encurtado):

```
package: ok
  artifacts/publish/v1.2.0/Stock.Desktop-win-x64.zip
  artifacts/publish/v1.2.0/Stock.Desktop-linux-x64.zip  (note: the zip was made on Windows and lost the execute bit: after unzipping on linux-x64, run chmod +x on the executable)

release: ok
  "releaseAssets": [
    "artifacts/publish/v1.2.0/Stock.Desktop-win-x64.zip",
    "artifacts/publish/v1.2.0/Stock.Desktop-linux-x64.zip"
  ]
  committed docs/releases/v1.2.0.md
  tagged v1.2.0 (annotated, with the notes)
  pushed main and v1.2.0
  GitHub Release v1.2.0 created: https://github.com/acme/shop/releases/tag/v1.2.0
```

Na chamada que criou o GitHub Release, os dois zips vêm depois do arquivo de notas, na mesma chamada.

As notas do release ganham uma seção só para quem baixa o zip do Linux, porque o zip feito no Windows perde o bit de execução:

```
## Install

- linux-x64: the zip was made on Windows and lost the execute bit: after unzipping, run `chmod +x` on the executable.
```

Um zip do `win-x64` não ganha essa linha, e um zip feito no Linux (o runner do `release.yml`) também não. O mesmo projeto, liberado pelo `release.yml`, deixa no GitHub Release os dois zips sem assinatura que o runner empacotou; antes, eles sumiam com o runner. Um head Velopack que lista também `linux-arm64` leva o Setup.exe, o zip portátil e o `Stock.Desktop-linux-arm64.zip`, e nenhum zip do `win-x64`. Os arquivos de um head web, de uma API ou do Mobile (o `.aab`) nunca vão ao release. Se um dos zips sumir entre o `package` e o `release`, o `release` para antes do commit e da tag, dizendo qual zip falta. Um repositório privado recebe os zips como qualquer arquivo; quem baixa precisa estar logado.

### 14.153 A pergunta 23f do bootstrap só aparece para um kit com módulo de temas, e um sim cria a pasta dos temas

A acme-stock é um app `web-app` com MudBlazor. Na rodada 5, depois de a 23a gravar a identidade e de a 23c ser respondida, o bootstrap pergunta, com sim como recomendação:

```
Pergunta 23f — Temas personalizáveis, no padrão do VS Code?
a) sim   b) não
Recomendação: sim. Motivo: quem usa o app escolhe o tema do modo claro e do escuro do sistema e a fonte, e o kit aplica isso em tempo de execução.
```

O dono responde sim. O ADR-0001 ganha a linha `Customizable themes | yes | VS Code pattern (references/theme-format.md)`, e o bootstrap roda o script de temas logo depois da identidade, no mesmo projeto (o script diz também, antes do veredito, quantas cores ajustou nos dois temas de alto contraste):

```
WROTE 7 files
```

Isso cria `docs/design/themes/` com `index.json`, um arquivo por tema e modo e os dois temas de alto contraste do tema padrão, que é o que liga o módulo do kit. No passo 13, a ideia "Run-time themes in the MudBlazor" entra no quadro logo depois da ideia do kit de UI e depende dela.

Se o dono responde não, o ADR-0001 só ganha `Customizable themes | no | one identity`: nenhuma pasta, nenhuma ideia. Se o script recusasse por um par de cores sem contraste, o bootstrap mostraria a recusa, manteria o sim e capturaria antes a ideia "Built-in themes from the identity".

Um app `desktop` recebe a pergunta com qualquer das três cabeças, Avalonia, WinUI 3 ou MAUI (o exemplo 14.176 mostra a do WinUI 3). Quem não recebe é `web-api` e `plugin`, que não têm telas: o ADR-0001 ganha `Customizable themes | not offered`, e a mensagem do bootstrap lista isso em "Já claro".

### 14.154 O head Velopack sem assinatura é lançado no GitHub Actions: o runner é Windows e o feed https vira artefato

A acme-stock é um app desktop (Avalonia, `win-x64;linux-x64`) com `Update source` no `docs/infra.md` e `release.yml` escrito antes da fonte de atualização existir. A fonte é o GitHub deste repositório, mas o `release.yml` ainda diz `ubuntu-latest`. O dono digita `/agile:publish`. O Claude roda o plano do release em CI, e o script para antes de qualquer disparo (a saída é a do script num projeto de teste):

```
{
  "ok": false,
  "stop": [
    "release.yml runs on ubuntu-latest: set runs-on: windows-latest"
  ],
  "note": "nothing was committed, tagged or dispatched"
}
```

O Claude cita a frase e diz o único próximo passo: trocar `runs-on` do job `release` para `windows-latest` (o `release.yml` escrito é do projeto: o `/agile:sync` não o muda; um projeto novo que declara a fonte já nasce com `windows-latest`, e o job `deploy` fica no `ubuntu-latest`). Com a troca feita, o runner empacota o que o `/agile:publish --local` empacotaria (Setup.exe, Portable.zip, pacotes completo e delta e o índice do feed do `win-x64`, o AppImage do `linux-x64`), cria o GitHub Release e anexa o feed com o `GITHUB_TOKEN` da run, sem segredo novo.

Num projeto cuja fonte é uma URL https, o runner não tem para onde enviar o feed. O release termina bem e os arquivos do feed ficam como o artefato `velopack-v1.2.0` da run (a saída é a do script, com `GITHUB_STEP_SUMMARY` e `GITHUB_OUTPUT` de teste):

```
## Release v1.2.0

- Tag: v1.2.0 (annotated, with the notes)
- Files: docs/releases/v1.2.0.md
- GitHub Release: https://github.com/acme/shop/releases/tag/v1.2.0
- Glossary: not updated (the rows are added by /agile:publish before it dispatches this run; a run started by hand skips them)
- Velopack feed: artifact velopack-v1.2.0 (kept 30 days): copy its files to https://updates.acme.example/stock; installed apps see no update until you do
```

O relatório de `/agile:publish` diz o mesmo e o Claude repete ao dono: baixe o artefato `velopack-v1.2.0` em 30 dias e copie os arquivos para `https://updates.acme.example/stock`; até lá os apps instalados não veem a atualização. Uma fonte numa pasta de rede, o GitHub de outro repositório, um modo de assinatura declarado ou um head Mobile continuam parando antes de qualquer commit, cada um com o próprio motivo, e `/agile:publish --local` os libera da sua máquina.

### 14.155 O release no GitHub Actions é assinado: o ambiente `release-signing`, a parada antes de qualquer commit e o resumo com o publisher

A acme-shop é um app desktop (Avalonia, `win-x64`) com a fonte de atualização no GitHub deste repositório e `- Code signing: signtool` no `docs/infra.md`. O `release.yml` já tem `environment: release-signing` e os passos do `signtool`, mas o ambiente do GitHub foi criado aberto a qualquer branch e sem o segredo do certificado. O dono digita `/agile:publish`. O Claude roda o plano; na máquina dele nenhuma checagem de assinatura roda (não há variável aqui): o script só lê o `release.yml` e os nomes no GitHub.

```
{
  "ok": false,
  "stop": [
    "the GitHub environment release-signing is open to every branch: a workflow edited on any branch could read the signing keys; limit it to main (Settings > Environments > release-signing > Deployment branches and tags > Selected branches)",
    "the GitHub environment release-signing has no secret SIGNING_PFX_BASE64 (signtool needs it; Settings > Environments > release-signing)"
  ],
  "note": "nothing was committed, tagged or dispatched"
}
```

O Claude cita as duas frases e diz o único próximo passo: no GitHub, em Settings, Environments, `release-signing`, escolher "Selected branches" com só a `main` e criar o segredo `SIGNING_PFX_BASE64` (o certificado em base64) ao lado de `SIGNING_PFX_PASSWORD`; nenhum valor passa pelo chat. Nada foi commitado, marcado nem disparado. Com o ambiente limitado à `main` e os dois segredos e a variável `SIGNING_TIMESTAMP_URL` no lugar, o mesmo comando segue: mostra o plano, dispara o `Release` e espera a run. Se o `gh` não conseguir ler o ambiente, o plano diz `could not check ...` como aviso e a própria run para se um nome faltar.

No runner o job `release` importa o certificado, assina por `/sha1`, confere cada assinatura e só então cria a tag. O resumo da run, que o Claude cita ao dono, nomeia quem assinou, a run e o commit das notas:

```
## Release v1.0.0
- Tag: v1.0.0 (annotated, with the notes)
- Files: docs/releases/v1.0.0.md
- GitHub Release: https://github.com/acme/shop/releases/tag/v1.0.0
- Glossary: not updated (the rows are added by /agile:publish before it dispatches this run; a run started by hand skips them)
- Signed: signtool
- Publisher: Acme Software Ltd
- Run: 8123456789 (https://github.com/acme/shop/actions/runs/8123456789)
- Commit: 85cea08b027fb8b604ebbd3e4773f97f5eff0884
```

Se um dos arquivos não estivesse validamente assinado, a run pararia antes da tag, nomeando o arquivo e o status (`Setup.exe is not validly signed (status NotSigned)`), e nenhum release sairia sem assinatura.

### 14.156 MVPs nomeados e ranqueados: `/agile:order` abre com um grupo por MVP e põe antes o item que fecha uma feature

No ExamPrep há dois epics, e cada um tem um corte com nome: o `cadastro` tem `mvp1-cadastro` (o cadastro de questões completo), com `Rank: 1`, e o `provas` tem `mvp2-provas` (a correção das provas), ainda sem rank. Você quer que o `mvp2-provas` seja o segundo e que o F-2, que os dois cortes pedem, fique no primeiro. Para gravar o rank sem editar o epic à mão:

```
/agile:mvp provas mvp2-provas --rank 2
```

Antes de pedir o seu ok, o Claude roda o `mvp.js diff` com o rótulo, o rank e os ids, e mostra a saída (o corte antigo não tinha rank):

```
enters: F-2
leaves: -
kept: -
rank: none → 2
agile mvp DIFF: provas 1 enter, 0 leave, 0 kept
```

Com o seu ok, o `mvp.js write` grava o título, a linha `Rank: 2` e os ids (a linha `Rank:` de um corte que já tinha rank continua onde está, a menos que você passe `--rank`; um valor que não seja um inteiro de 1 a 99 é recusado e nada é escrito). Depois, o `/agile:order` escreve a lista. Esta é a saída real do script sobre essa fixture (o arquivo, sem o cabeçalho de comentário); o `F-6` e o `F-7` já foram entregues e o `F-10` é a última parte aberta da funcionalidade que eles começaram (`Split from F-6` na primeira linha do `## Summary`):

```
# Order of implementation

## MVP: mvp1-cadastro

| # | Id | Title | Status | Model | Epic | Depends on | Unlocks |
|---|---|---|---|---|---|---|---|
| 1 | F-1 | Question bank | approved | - | cadastro | - | F-2 |
| 2 | F-3 | Image attachments | approved | - | cadastro | - | F-2 |
| 3 | F-2 | Question editor | approved | - | cadastro | F-1, F-3 | - |

## MVP: mvp2-provas

| # | Id | Title | Status | Model | Epic | Depends on | Unlocks |
|---|---|---|---|---|---|---|---|
| 4 | B-1 | Exam printed without questions | approved | - | provas | - | - |
| 5 | F-4 | Exam correction | approved | - | provas | - | - |

## Later

| # | Id | Title | Status | Model | Epic | Depends on | Unlocks |
|---|---|---|---|---|---|---|---|
| 6 | F-10 | Answer key import: error report | approved | - | provas | - | - |
| 7 | F-9 | Dark theme | approved | - | cadastro | - | - |

## Checklist

### MVP: mvp1-cadastro

- [ ] 1. F-1 — Question bank [approved] — unlocks F-2
- [ ] 2. F-3 — Image attachments [approved] — unlocks F-2
- [ ] 3. F-2 — Question editor [approved] — waits on F-1, F-3

### MVP: mvp2-provas

- [ ] 4. B-1 — Exam printed without questions [approved]
- [ ] 5. F-4 — Exam correction [approved]

### Later

- [ ] 6. F-10 — Answer key import: error report [approved]
- [ ] 7. F-9 — Dark theme [approved]

## Summary

- Delivered: 2
- MVP mvp1-cadastro: 3 open (2 in the cuts, 1 their dependencies)
- MVP mvp2-provas: 2 open (2 in the cuts, 0 their dependencies)
- First release (cadastro, mvp1-cadastro): 0 of 2 delivered; missing: F-1, F-2
- First release (provas, mvp2-provas): 0 of 3 delivered; missing: B-1, F-4, F-2
```

Quatro coisas para notar. Os grupos seguem o `Rank:`: `mvp1-cadastro` (rank 1) vem antes de `mvp2-provas` (rank 2), e sem nenhum `Rank:` a ordem alfabética dos nomes decidiria; um MVP com rank vem sempre antes de um sem. O F-2 está nos dois cortes e fica só no primeiro, e o F-3, que o F-2 precisa e nenhum corte cita, entra no primeiro como dependência (`2 in the cuts, 1 their dependencies`); nada num MVP espera por um MVP posterior. Dentro de cada grupo a chave é: nível, bug, se o item fecha uma feature, Must, Should, Could, tamanho, id. Por isso o B-1 vem antes do F-4 no `mvp2-provas`, e no `Later` o F-10 (Could) vem antes do F-9 (Must): o F-10 é a última parte aberta de uma família (`Split from F-6`) com partes entregues. Um irmão ainda aberto, bloqueado ou adiado desfaz esse empurrão, uma família sem parte entregue não ganha nenhum, e um `Split from` de um id que não existe ou um laço não dão erro nem empurrão. Por fim, as linhas `First release` continuam por épico: o F-2 aparece como faltando nas duas, porque o corte de cada épico não muda, e só a lista é a união. Com um corte só, o arquivo é o de antes (`## MVP`, `- MVP: <k> open (...)`); no widget os cabeçalhos são `MVP: <nome>` (ou `MVP`) e `Depois`.

### 14.157 O `.aab` Android sai assinado no GitHub Actions: os nomes no ambiente `release-signing`, a parada antes de qualquer commit e o resumo com o signatário

A acme-shop é um app mobile (MAUI, `Shop.Api` e `Shop.Mobile`, com `ApplicationId` `com.acme.shop`) cujo `release.yml` o `pipeline write` escreveu já com os passos Android. O dono criou o ambiente `release-signing` no GitHub, mas ele ficou aberto a qualquer branch e sem o segredo do keystore. O dono digita `/agile:publish`. O Claude roda o plano; nesta máquina não há variável `ANDROID_SIGNING_*`, nem keystore, nem a workload do Android, e isso não importa: o script só lê o `release.yml` e, no GitHub, os nomes.

```
{
  "ok": false,
  "stop": [
    "the GitHub environment release-signing is open to every branch: a workflow edited on any branch could read the signing keys; limit it to main (Settings > Environments > release-signing > Deployment branches and tags > Selected branches)",
    "the GitHub environment release-signing has no secret ANDROID_SIGNING_KEYSTORE_BASE64 (the Android signing needs it; Settings > Environments > release-signing)"
  ],
  "note": "nothing was committed, tagged or dispatched"
}
```

O Claude cita as duas frases e diz o único próximo passo: no GitHub, em Settings, Environments, `release-signing`, escolher "Selected branches" com só a `main` e criar o segredo `ANDROID_SIGNING_KEYSTORE_BASE64` (a seção `## Store signing` do `docs/infra.md` traz os comandos `gh`, em PowerShell e em Bash: o keystore é lido do arquivo e as senhas são pedidas pelo `gh`, então nenhum valor passa pelo chat nem por uma linha de comando). Nada foi commitado, marcado nem disparado. Com o ambiente limitado à `main` e a variável `ANDROID_SIGNING_ALIAS` e os três segredos no lugar, o mesmo comando segue: mostra o plano com a linha do deploy, dispara o `Release` e espera a run.

No runner o job `release` instala a workload, grava o keystore num arquivo temporário fora do checkout, empacota o `.aab` assinado, confere a assinatura com o `jarsigner`, lê o certificado com o `keytool` e só então commita as notas e cria a tag. O `.aab` vai como arquivo do GitHub Release, e o resumo da run, que o Claude cita ao dono, nomeia quem assinou, a run e o commit das notas:

```
## Release v0.4.0

- Tag: v0.4.0 (annotated, with the notes)
- Files: docs/releases/v0.4.0.md, docs/releases/v0.4.0-store.md
- GitHub Release: https://github.com/acme/shop/releases/tag/v0.4.0
- Glossary: not updated (the rows are added by /agile:publish before it dispatches this run; a run started by hand skips them)
- Android bundle: com.acme.shop-Signed.aab (attached to the GitHub Release)
- Android signer: CN=Acme Shop Upload, O=Acme Software Ltd, C=BR
- Android SHA-256: 1E:08:1A:32:7A:93:B8:AB:EE:17:4C:75:00:AF:36:5D:81:BB:95:16:E9:DE:72:7E:71:4C:B4:05:10:25:49:92
- Run: 8123456789 (https://github.com/acme/shop/actions/runs/8123456789)
- Commit: 96324db95e235e6cfcb71641f08beb9988a67d3f
```

O checklist da loja que a run commitou na `main` já não aponta um caminho de `artifacts/`, que o dono não tem depois de uma run: nomeia o arquivo do Release e o endereço para baixá-lo, e o resto do checklist é o de sempre (o envio à Play Console continua sendo do dono):

```
# Store checklist v0.4.0

Written by `/agile:publish` for this release. The plugin uploads nothing to either store: every step below is yours.

## Android, this release
- The signed bundle: the asset `com.acme.shop-Signed.aab` of the GitHub Release v0.4.0: https://github.com/acme/shop/releases/download/v0.4.0/com.acme.shop-Signed.aab (application id `com.acme.shop`).
```

Se o `.aab` não tivesse assinatura (o `jarsigner` sai com código 0 até para `jar is unsigned.`, então o Claude e o script leem o texto), a run pararia antes de commitar qualquer coisa, nomeando o arquivo, e nenhum release sairia com um pacote sem assinatura. O keystore, as duas senhas e o base64 não aparecem em relatório, resumo nem log: o arquivo temporário é apagado por um passo que roda mesmo quando a run falha, e um valor que vazasse numa mensagem de erro de uma ferramenta sai como `***`.

### 14.158 Os releases ficam em outro repositório: o token de escopo fino `RELEASES_TOKEN` e a prova do alcance antes de qualquer commit

A acme-shop é um app desktop (Avalonia, `win-x64`) cujo código é privado (`acme/shop`) e cuja fonte de atualização é um repositório público só de releases, `https://github.com/acme/shop-releases`. Até aqui o `/agile:publish` não a lançava no GitHub Actions: o `GITHUB_TOKEN` da run alcança só `acme/shop`. O dono digita `/agile:publish`, e o `release.yml` dele, escrito antes desta mudança, ainda não tem as duas linhas do token. O Claude mostra o plano (`ci --plan`), que para antes de qualquer coisa:

```
{
  "ok": false,
  "stop": [
    "release in CI does not cover Shop.Desktop (Velopack, GitHub source acme/shop-releases is not this repository) yet: run /agile:publish --local (to release it in CI: add environment: release-signing to the release job and AGILE_RELEASES_TOKEN: ${{ secrets.RELEASES_TOKEN }} to the env of the Release step of release.yml (/agile:sync never changes a release.yml), and keep a fine-grained token (Contents: read and write on that repository only) in the secret RELEASES_TOKEN of the GitHub environment release-signing (Desktop updates in docs/infra.md))"
  ],
  "note": "nothing was committed, tagged or dispatched"
}
```

O Claude cita a frase e diz o único próximo passo: acrescentar `environment: release-signing` ao job `release` e `AGILE_RELEASES_TOKEN: ${{ secrets.RELEASES_TOKEN }}` ao `env` do passo `Release` (um projeto novo já nasce com as duas linhas, que o `pipeline write` escreve), e criar o token. Ele não cria nada: no GitHub, em Settings, Developer settings, Fine-grained tokens, o dono escolhe como dono do recurso o dono do repositório de releases, só o repositório `shop-releases`, a permissão Contents: read and write e uma data de vencimento, e grava o valor no ambiente `release-signing` (limitado à `main`, o mesmo das chaves de assinatura) com o `gh secret set` da seção `## Desktop updates` do `docs/infra.md`, que lê o valor de um prompt. Com isso, o `ci --plan` confere o ambiente, o limite à `main` e o nome do segredo (nunca o valor) e passa.

No runner, antes de empacotar, commitar ou criar a tag, a run prova o que o token alcança. Se o dono tivesse criado um token clássico (`ghp_`), que alcança todos os repositórios da conta, a run pararia ali, sem nada commitado:

```
{
  "ok": false,
  "stop": [
    "RELEASES_TOKEN is a classic personal access token (ghp_): it reaches every repository of the account, not only acme/shop-releases; create a fine-grained token on acme/shop-releases only (Contents: read and write) and set it with gh secret set RELEASES_TOKEN --env release-signing"
  ],
  "note": "nothing was packaged, committed or tagged"
}
```

Um token vencido ou revogado para do mesmo jeito, com `GitHub refused RELEASES_TOKEN for acme/shop-releases (HTTP 401): the token expired or was revoked`; um token sem `push` no repositório de releases, com `cannot write to`; e um token que consegue ler o `acme/shop` privado, com `the token reaches acme/shop`. Com o token certo, a run escreve em `acme/shop-releases` (o `gh release create` e o `vpk upload github` são as únicas chamadas que o recebem; o `dotnet publish`, o `vpk pack`, o `git` e o NuGet começam sem ele), a leitura de `acme/shop` dá 404 e o resumo da run, que o Claude cita ao dono, diz o que foi provado:

```
## Release v1.0.0

- Tag: v1.0.0 (annotated, with the notes)
- Files: docs/releases/v1.0.0.md
- GitHub Release: https://github.com/acme/shop-releases/releases/tag/v1.0.0
- Glossary: not updated (the rows are added by /agile:publish before it dispatches this run; a run started by hand skips them)
- Releases token: writes to acme/shop-releases; acme/shop: not found (the token does not reach it)
```

O código fica com a tag `v1.0.0` e sem Release; o Release e o feed ficam no repositório público. Se o repositório de código fosse público, a prova não conseguiria mostrar que o token não escreve nele (ler não prova a ausência de escrita): a run seguiria com um aviso dizendo isso. O valor do token não aparece em relatório, resumo, saída de passo nem log: se uma ferramenta o repetisse numa mensagem de erro, ele sairia como `***`.

### 14.159 `/agile:docs` gera o site da documentação: o manual e as páginas técnicas viram HTML só de leitura, e uma página com o front matter aberto é pulada

A acme-shop tem o manual do app em pt-BR (`inicio` e `pedidos`) e páginas técnicas: um fluxo do checkout com um diagrama, a referência da API de pedidos e um how-to de carga de dados. O dono quer ler tudo no navegador, sem abrir os `.md` um a um, e digita `/agile:docs`. O Claude roda o script na raiz do projeto e cita as linhas que ele imprimiu, inteiras:

```
SKIPPED: docs/how-to/seed-data.md: front matter is not closed
agile docs GREEN: 7 page(s) (manual 2, technical 5) -> D:\dev\acme-shop\.agile-site
file:///D:/dev/acme-shop/.agile-site/index.html
```

A página `seed-data.md` abre o front matter com `---` e nunca o fecha: o script a nomeia e a pula, constrói o resto e sai com código 1. As sete páginas são as duas do manual, o fluxo do checkout, a referência da API e os três arquivos de índice (`docs/flows.md`, `docs/how-to.md` e `docs/reference.md`), cada um virando uma página na raiz do site. A pasta ficou assim:

```
.agile-site/.generated
.agile-site/assets/mermaid.min.js
.agile-site/flows.html
.agile-site/flows/checkout.html
.agile-site/how-to.html
.agile-site/index.html
.agile-site/manual/pt-BR/inicio.html
.agile-site/manual/pt-BR/pedidos.html
.agile-site/reference.html
.agile-site/reference/orders-api.html
```

O Claude diz, em pt-BR, o que o site é: só leitura e derivado dos `.md`, que continuam sendo a fonte; a pasta `.agile-site/` entrou no `.gitignore` (a linha `.agile-site/`, uma vez) e não vai para o Git; o `assets/mermaid.min.js` está ali porque o fluxo do checkout tem um diagrama, e o navegador o desenha sem nenhuma chamada de rede, então a pasta inteira vai junto se for enviada. O dono abre o `index.html` com um clique duplo: a home lista o manual por idioma e cada tipo de página técnica, e uma página tem os atalhos das suas seções no topo.

O dono fecha o front matter de `seed-data.md` e pede de novo. O script apaga só os arquivos que a marca `.generated` listava, escreve tudo outra vez e agora não pula nada:

```
agile docs GREEN: 8 page(s) (manual 2, technical 6) -> D:\dev\acme-shop\.agile-site
file:///D:/dev/acme-shop/.agile-site/index.html
```

Um arquivo que o dono tivesse posto na pasta continuaria lá. Se `.agile-site/` já existisse com arquivos que o script não escreveu (sem a marca `.generated`), ele pararia com `REFUSED:` nomeando a pasta e não escreveria nada; o Claude então deixa o dono escolher outra pasta com `--out` ou limpar a existente, e nunca a apaga. Num projeto sem nenhuma página de manual nem técnica, a resposta é só `agile docs NOTHING TO RENDER`.

### 14.160 A inclinação primeiro, rodadas até "decidido" e um resumo antes de gravar: o `/agile:discuss` como conversa

No Clinic Desk, um app de agenda para clínicas pequenas, você digita:

```
/agile:discuss "Como o Clinic Desk deve tratar o paciente que marca e não aparece?"
```

O Claude lê o brief e o que já existe e escreve o enquadramento (a ideia em duas linhas, o que o brief diz: o paciente nunca entra no app, ninguém paga pelo app, a meta do ano inclui perder menos clínicas; o que está em aberto). A mensagem termina aí, sem nenhuma opção, com uma pergunta só:

> ❓ **Pergunta**
> Qual é a sua inclinação sobre até onde o Clinic Desk deve ir: só lembrar o paciente, ou também agir depois do não comparecimento (uma taxa, por exemplo)? O que te preocupa em ir além?
> **Recomendação:** responda numa linha, ou `sem inclinação` para ir direto às opções.

Você responde `sem inclinação`. Agora vêm as opções: A, só o lembrete (S); B, o lembrete mais uma taxa que o app só registra (M); C, o lembrete mais o acompanhamento dos não comparecimentos, sem dinheiro (S/M). Com uma inclinação dita, uma linha diria como cada opção a pesa, e uma recomendação diferente dela diria por quê. Depois vem a rodada 1, com 4 perguntas, cada uma com recomendação e motivo (quando o lembrete sai, quem marca o não comparecimento, quem fixa a taxa, como o paciente fica sabendo dela). Você aceita todas as recomendações e diz `decidido`. Se a conversa passasse da segunda rodada, a primeira linha de cada rodada seguinte lembraria `diga decidido ou estacionar para encerrar`; nada a encerra além de você.

Antes de gravar qualquer coisa, o Claude mostra o resumo: as cinco decisões (data, decisão, motivo), nenhum ponto adiado e os itens a criar, cada um com o épico que vai receber:

| Tipo | Título | Épico |
|---|---|---|
| Feature | Lembrete antes da consulta | Novo épico: No-shows (recomendado, nenhum existente serve) |
| Feature | Taxa de não comparecimento | Novo épico: No-shows |

> ❓ **Pergunta**
> a) Record and capture (Recommended)
> b) Change the list
> c) Park, capture nothing

Você escolhe `Change the list` e tira a taxa: "preço está fora do escopo". Nenhum número foi pego e nenhum arquivo foi escrito. O resumo aparece de novo com uma linha só na tabela (o lembrete), as decisões que existiam só para a taxa saem da lista, e o card se repete. Você escolhe `Record and capture` e responde "sem épico" para o item que sobrou.

Só agora o arquivo `docs/discussions/D-1-<slug>.md` é gravado, com a linha `dropped at the gate: No-show fee — the owner does not want it; pricing is out of scope` em `## Decisions`, a busca de itens parecidos roda (`No similar item (0 items read)`) e só o lembrete vira a feature F-1, no status `idea`, com o épico que você respondeu no resumo, sem perguntar de novo. A taxa nunca teve número nem issue. Se em vez do card você tivesse dito `estacionar` na rodada, o arquivo sairia `parked`, sem card e sem item. Numa execução sem ninguém para responder, a pergunta da inclinação não é feita e o card toma a opção recomendada.


### 14.161 O Claude fecha um falso positivo do scan e você reabre

No F-14 do HR-Portal, o scan do fim do build para com quatro achados critical. O Claude lê o código de cada um antes de decidir. A linha `src/Db.cs:13` é uma consulta que concatena uma constante, sem entrada de usuário: um falso positivo. Ele chama o agente `reviewer` com o achado, o lugar e a evidência; o `reviewer` confirma, e só então o Claude fecha a linha.

```
Claude: agile scan BLOCKED: 4 critical, 0 high open
        a1b2c3d4: false-positive (the query takes a constant (evidence: Db.cs:13 concatenates a literal, no input reaches it) (reviewer confirmed))
```

Os outros achados ele não toca, e o script recusa quando o Claude tenta fechá-los:

```
Claude: c3d4e5f6 is a secret found in the git history: only the owner closes it, after saying the credential was rotated: nothing changed
        --by claude closes only critical or high findings (e5f6a7b8 is medium): nothing changed
```

O segredo do histórico e o aviso medium vão para você como sempre. O novo scan bloqueia por menos um, sem nenhuma parada nova:

```
Claude: Triaged: 1 false positive
        Closed by Claude: 1 (node scan.js closed lists them)
        agile scan BLOCKED: 3 critical, 0 high open
```

O script de validação ganha um passo com o que o Claude fechou, exatamente como o script imprime:

```
Closed by Claude:
| id | severity | where | reason |
|---|---|---|---|
| a1b2c3d4 | critical | src/Db.cs:13 | the query takes a constant (evidence: Db.cs:13 concatenates a literal, no input reaches it) (reviewer confirmed) |
```

Você abre o `src/Db.cs` e vê que o literal vem de um campo de formulário. Você digita o comando de reabrir do passo, com o motivo, e a linha volta para aberta com `who: owner`. O Claude não a fecha de novo e o scan volta a bloquear:

```
Claude: a1b2c3d4: reopened (the literal comes from a form field)
        Triaged: none
        agile scan BLOCKED: 4 critical, 0 high open
```

No ship, o passo 4d imprime o mesmo bloco `Closed by Claude:` logo depois do scan e antes do merge, e o relatório o cita sem resumir. Quem aceitar um risco continua sendo você: o `accept` sem `--by` vale mais que o do Claude.

### 14.162 O painel de papéis: o plano, o caso medido e o veredito

Você quer saber se três papéis, cada um num contexto novo, dão opiniões melhores que um contexto só, antes de pedir o `--panel` no `/agile:discuss` (#271). Sem argumento, o script só mostra o plano e não chama nada. O plano diz qual é a régua do veredito, fixada antes de qualquer rodada:

```text
$ node benchmarks/panel.js
panel suite v1: 4 problems x 3 runs x 2 arms (self, panel) on sonnet, judge opus (another family)
Each case: one /agile:discuss step 2, then the arm self (one call: the three roles in one context, with the recommendation) and the arm panel (three calls, one per role, each in a fresh context, with no recommendation: product, architect, contrarian).
  import-records  How should a clinic that joins us bring in the patient records and the future appointments it keeps 
  clinic-leaves  What should happen when a clinic stops paying or decides to leave us? It holds years of health recor
  no-shows  How could we help clinics lose less money to patients who book and never show up? It is the complain
  offline-desk  How should the front desk keep working when the clinic's internet line drops for an hour in the midd
The bar for "beats" (BR9, fixed on 2026-10-20 before any run): all three of a mean gap of at least 1.0 (the mean of challenge, traps and decision value over all judged cases, panel over self), the panel higher on at least 3 of 4 problems, and a panel arm that costs at most 3 times the self arm per case. A partial run is INCONCLUSIVE.
Nothing started and nothing paid. First one measured case: node benchmarks/panel.js --measure; then the suite: node benchmarks/panel.js --go --max-cost-usd <n>
```

Se você pular direto para a suíte, ela recusa sem gastar nada:

```text
$ node benchmarks/panel.js --go --seed 42
panel suite REFUSED: --go needs --max-cost-usd <n>: run --measure first, it prints the limit to use (BR8)
```

O caso medido faz uma rodada completa de um problema, sem limite: a chamada do passo 2, o braço `self` (uma chamada), o braço `panel` (três chamadas, uma por papel) e o juiz. Saída do script sobre o `claude` falso do check (cada chamada custa US$ 0,37 ali; numa execução real, os valores são os que a CLI do Claude informa). O custo de cada braço é a soma das suas chamadas, e o limite sugerido é o custo do caso × 4 problemas × 3 rodadas + 25%, arredondado para cima:

```text
$ node benchmarks/panel.js --measure --seed 3
import-records run 1: step 2 US$ 0.37; self US$ 0.37, panel US$ 1.11; judge US$ 0.37
results: benchmarks/results/panel-measure-<stamp>.json and .md (seed 3)
measured: US$ 2.22 for one case (step 2, self, 3 roles, judge); suggested --max-cost-usd 34 (x 4 problems x 3 runs + 25%)
panel verdict: INCONCLUSIVE (a complete run is every case measured and judged (1 of 12 cases are))
panel suite DONE (total US$ 2.22)
```

Um caso só nunca dá veredito: `INCONCLUSIVE`. O relatório traz a tabela do problema, com a coluna "Shown as" dizendo qual conjunto o juiz viu como X (o juiz não sabe qual é o `self` e qual é o `panel`):

```text
| Run | Arm | Shown as | challenge | traps | decision | Cost US$ | Step 2 US$ | Judge US$ | Traps named (judge) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | self | Y | 4 | 4 | 4 | 0.37 | 0.37 | 0.37 | names the lock-in of a custom format |
| 1 | panel | X | 7 | 7 | 7 | 1.11 | 0.37 | 0.37 | names the lock-in of a custom format |
```

Você autoriza a suíte com o limite sugerido (é uma despesa sua, não do script). Na rodada completa, o relatório fecha com uma linha só, os números atrás dela e a régua ao lado. No exemplo do check, o `panel` tira 3 pontos a mais, ganha nos 4 problemas e custa 3 vezes o `self`, no limite exato da régua:

```text
panel verdict: BEATS (mean gap +3.00 [bar +1.0], panel higher on 4 of 4 problems [bar 3], cost ratio 3.00 [bar 3.0]; self US$ 0.37 and panel US$ 1.11 a case)
```

Se qualquer caso não tiver custo informado ou o juiz responder fora da escala, a rodada sai com `PARTIAL`, o veredito é `INCONCLUSIVE` e o código de saída é 2. Um `BEATS` não abre nada sozinho: o #271 só começa quando você o abrir.

### 14.163 A página HTML do roteiro, para validar no celular

O F-12 está em `validating` e você vai testar os passos longe do computador: no celular, no caminho do trabalho. Você digita o comando com `--html` e a pasta de uma pasta sincronizada com o aparelho:

```text
> /agile:script F-12 --html --out C:\Users\ana\Sync
Claude: F-12 — Bulk import of exams [validating]
agile validation page WRITTEN: C:\Users\ana\Sync\F-12.html
```

O Claude não leu o status nem escreveu nada no item, no board ou no arquivo das marcas: a página é só uma vista do roteiro. Sem `--out`, o arquivo vai para `agile-validation-pages/F-12.html` dentro da pasta `.git` e nunca é versionado. A página é um arquivo só, sem rede: abre no celular direto da pasta sincronizada.

No aparelho você vê o roteiro em pt-BR (o botão `EN` troca para o inglês, com as mesmas marcas). Cada subpasso tem a sua caixa; os blocos de comando têm um botão `Copiar` cada. A página nasceu com as marcas que a máquina já guardava, `1.1` e `1.2`, e por isso o passo 1 já aparece como `(feito)`:

```text
Roteiro de validação                                [pt-BR | EN]
Gerada em 2026-10-20
2 de 4 marcados  [=========-------]                [Limpar marcas]
Se o roteiro mudar, gere de novo.

1. Iniciar o app   (feito)
   [x] 1.1 Suba o app host → a página de login abre.
   [x] 1.2 Entre como o usuário demo → a home abre.
2. Rodar a conferência da importação
   [ ] 2.1 Rode o comando do seu shell → 12 passaram.
       dotnet test --filter Name~Import                [Copiar]
       dotnet test --filter Name~Import                [Copiar]
3. [ ] Troque o idioma para inglês → os textos mudam.

/agile:script F-12 ticks 1.1 1.2                       [Copiar comando]
As marcas ficam neste navegador; para o Claude, copie o comando e cole no chat.
```

Você roda o passo 2 e marca `2.1`: o contador vira `3 de 4 marcados`, a linha riscada mostra a unidade feita e o comando passa a ser `/agile:script F-12 ticks 1.1 1.2 2.1`. Se você fechar a aba e abrir de novo, as marcas estão como ficaram (elas ficam no navegador daquele aparelho; sem esse armazenamento, a página funciona do mesmo jeito, só não lembra). `Limpar marcas` desmarca tudo.

De volta ao computador, você toca em `Copiar comando` (ou seleciona o texto do campo, se o navegador bloquear a área de transferência) e cola no chat:

```text
> /agile:script F-12 ticks 1.1 1.2 2.1
Claude: agile validation TICKED: 3 of 4
```

Daí em diante as marcas valem como as do widget: aparecem nele, no corpo do issue e na evidência do ship. A página não sabe disso: ela é uma foto. Se o roteiro mudar, você pede `--html` outra vez, e a página nova começa das marcas da máquina.

Um item que ainda não tem roteiro é recusado e nada é escrito:

```text
> /agile:script F-5 --html
Claude: F-5 — No script yet [building]
agile validation page REFUSED: C:\dev\app\docs\features\F-5-none.md has no readable ## Validation script (no ## Validation script section)
```

O texto do item só aparece como texto: um passo que contenha `<script>` ou `</script>` é mostrado do jeito que foi escrito, e a página continua funcionando.

### 14.164 Dois temas de alto contraste saem da identidade e o teste de contraste do app lê o mínimo deles

O app desktop Cais já tem os temas embutidos (claro, escuro e o âmbar). Você ajusta o texto secundário do tema claro, e a geração escreve também os dois temas de alto contraste do tema padrão:

```
> /agile:identity
Claude: Identidade registrada: tema Cais (claro e escuro), 9 chaves, 5 pares de contraste, 2 chaves bloqueadas
(Env.Production.Bg, Focus). O que muda? A) uma cor  B) adicionar um tema embutido  C) fontes ou raio  D) nada
> A, Text.Muted do claro: #5E6875
Claude: Nada foi escrito ainda. Text.Muted sobre Surface no claro dá 5.65:1, acima do mínimo 4.5:1. Confirma?
> confirmo
Claude: Escrevi docs/design/identity.tokens.json e rodei a geração dos temas embutidos:
```

```
high contrast: 1 colors adjusted, 0 of them backgrounds
WROTE 7 files
```

```
Claude: Além dos quatro temas de antes, saíram cais-hc-light.json e cais-hc-dark.json, os temas de alto contraste do
tema padrão (o âmbar não tem um). Uma cor foi ajustada, e nenhuma delas era fundo: no claro, Text.Muted passou de
#5E6875 (5.65:1 sobre Surface) para #515965 (7.08:1, o mínimo de texto em alto contraste é 7:1). Todas as outras cores
já chegavam ao mínimo e ficaram iguais às do tema claro. Nenhum código do app foi alterado.
```

O mínimo de alto contraste é 7:1 para texto e 4.5:1 para o resto (a WCAG 2.2 só define o nível AAA para texto, então a parte de interface usa o do texto grande). Cada par do `index.json` traz os dois, `min` e `minHighContrast`, e o teste de contraste do app confere cada arquivo de tema com o mínimo certo:

```csharp
// para cada tema listado no index.json
var minimum = theme.Type.StartsWith("hc-") ? pair.MinHighContrast : pair.Min;
Assert.True(Contrast(colors[pair.Foreground], colors[pair.Background]) >= minimum);
```

Em um app que adotou o kit de temas antes desta versão, a verificação aponta `STALE` para os dois arquivos novos até a próxima geração; o kit só passa a listar "Cais High Contrast Light" entre os temas claros e "Cais High Contrast Dark" entre os escuros depois do `/agile:sync` e de refazer o módulo do kit. Trocar sozinho para o alto contraste quando o sistema operacional pede esse modo não faz parte disto: é a #273. Se uma identidade tem um par que nenhuma cor de primeiro plano leva ao mínimo, o fundo anda e o relatório conta esse fundo; se nem assim o par fecha em dez passadas, a geração recusa pelo nome do par e nada é escrito.

### 14.165 O `/agile:order` puxa a cadeia de um bug de nível alto para o topo

O acme-shop tem cinco itens em aberto: F-20 (catálogo de produtos), F-21 (totais do carrinho, depende do F-20), F-22 (layout de impressão), F-23 (compartilhar por link, depende do F-22) e B-5, um bug no total dos reembolsos que depende do F-21. O B-5 fica no nível 3, e antes desta versão a lista o punha depois de todas as features dos níveis 1 e 2, inclusive as que nada têm a ver com ele:

```
| # | Id | Title | Status | Model | Epic | Depends on | Unlocks |
| 1 | F-20 | Product catalog | approved | - | shop | - | F-21 |
| 2 | F-22 | Print layout | approved | - | shop | - | F-23 |
| 3 | F-21 | Cart totals | approved | - | shop | F-20 | B-5 |
| 4 | F-23 | Share by link | approved | - | shop | F-22 | - |
| 5 | B-5 | Total wrong on refunds | approved | - | shop | F-21 | - |
```

Agora o `/agile:order` puxa o bug e o que ele espera para o topo; o resto fica na ordem de sempre (a saída abaixo é a do script no projeto de teste):

```
> /agile:order
Claude: agile order WRITTEN: 5 items, 0 delivered
        | # | Id | Title | Status | Model | Epic | Depends on | Unlocks |
        | 1 | F-20 | Product catalog | approved | - | shop | - | F-21 |
        | 2 | F-21 | Cart totals | approved | - | shop | F-20 | B-5 |
        | 3 | B-5 | Total wrong on refunds | approved | - | shop | F-21 | - |
        | 4 | F-22 | Print layout | approved | - | shop | - | F-23 |
        | 5 | F-23 | Share by link | approved | - | shop | F-22 | - |
```

A cadeia do B-5 (F-20, F-21 e o próprio bug) vem primeiro, e o F-22 e o F-23, que o bug não precisa, vêm depois. Nada fica antes do que espera: o F-20 abre a lista porque o F-21 depende dele. Um bug bloqueado à mão, um adiado ou um que está num ciclo não puxa nada. Com épicos em corte, a puxada vale dentro de cada grupo: o bug de `Later` leva a cadeia para o topo de `Later` e não muda nenhum item de grupo.

### 14.166 Um objetivo vira quatro marcos em sequência: `/agile:mvp --milestones`

No acme-shop há dois epics, `catalogo` e `checkout`, e seis itens em aberto: o F-20 (Lista de produtos), o F-21 (Busca de produtos), o F-22 (Carrinho, depende do F-20), o F-23 (Pagar com cartão, depende do F-22), o F-24 (Cupom de desconto, Could) e o F-25 (Painel de pedidos, depende do F-23). Nenhum epic tem corte. Você quer ver o caminho até uma loja que vende e acompanha pedidos, não um corte só. Você digita:

```
/agile:mvp loja "vender produtos online com pagamento por cartão e acompanhar os pedidos" --milestones
```

A primeira linha da resposta diz o que foi lido: `label: loja · epic: all · goal: vender produtos online com pagamento por cartão e acompanhar os pedidos`. O Claude lê o brief, os epics e os itens e analisa o objetivo no chat: o que o usuário faz (ver, buscar, montar o carrinho, pagar, acompanhar), o que o técnico impõe (o pagamento depende do carrinho, o painel depende do pagamento), o que o negócio pede (cartão) e o que ninguém escreveu mas o objetivo exige: fechar um pedido precisa de um valor de frete, e nenhum item cobre isso. Essa é uma lacuna, e vem com quatro campos num card:

- Nome curto: Cálculo de frete.
- Descrição objetiva: informar o custo de entrega do carrinho pelo CEP.
- Dependências: F-22.
- Impacto no objetivo: sem o frete o pedido não fecha; entra no marco do pagamento.

O card pergunta o que fazer com a lacuna: `Capturar como ideia (Recomendado)`, `Sugerir uma discussão` ou `Ignorar`. Você escolhe capturar. O `/agile:idea` cria o F-26 (Cálculo de frete, depende do F-22), que passa a ser semente do marco do pagamento. O Claude grava as sementes de cada marco num arquivo de plano, fora do repositório, e nunca digita os ids de um corte:

```
{
  "milestones": [
    { "name": "vitrine", "purpose": "see and search products", "seeds": ["F-20", "F-21"] },
    { "name": "carrinho", "purpose": "fill a cart", "seeds": ["F-22"] },
    { "name": "pagamento", "purpose": "pay by card with shipping", "seeds": ["F-23", "F-26"] },
    { "name": "pedidos", "purpose": "follow the orders", "seeds": ["F-25"] }
  ]
}
```

O script calcula o resto, e a saída real dele sobre esse cenário é a diferença contra o que está gravado (nada, ainda):

```
cut loja-1-vitrine (rank 1, catalogo): enters: F-20, F-21; leaves: -
cut loja-2-carrinho (rank 2, catalogo): enters: F-22; leaves: -
cut loja-3-pagamento (rank 3, checkout): enters: F-23, F-26; leaves: -
cut loja-4-pedidos (rank 4, checkout): enters: F-25; leaves: -
agile mvp MILESTONES DIFF: 4 milestones, 6 items
```

O F-24 (cupom, Could) não é pedido pelo objetivo e fica em `Later`. O Claude mostra a tabela com o motivo de cada marco e faz uma pergunta só:

| Marco | Rank | Epic | Itens | Motivo |
|---|---|---|---|---|
| vitrine | 1 | catalogo | F-20, F-21 | ver e buscar produtos é o começo de qualquer venda |
| carrinho | 2 | catalogo | F-22 | juntar o que se quer comprar |
| pagamento | 3 | checkout | F-23, F-26 | pagar por cartão e fechar o pedido com o frete |
| pedidos | 4 | checkout | F-25 | acompanhar o que foi vendido |

O card diz `Escrever (Recomendado)`, `Mudar a lista` ou `Pular`. Nada foi escrito até aqui; `Pular` não toca em arquivo algum, e `Mudar a lista` refaz o plano e mostra uma nova diferença antes de perguntar de novo. Você escolhe escrever. O script grava um corte por marco, cada um no epic que tem mais dos seus ids, com a linha `Rank:` igual ao número do marco, e uma linha em `## Decisions` de cada arquivo:

```
cut loja-1-vitrine (rank 1, catalogo): enters: F-20, F-21; leaves: -
cut loja-2-carrinho (rank 2, catalogo): enters: F-22; leaves: -
cut loja-3-pagamento (rank 3, checkout): enters: F-23, F-26; leaves: -
cut loja-4-pedidos (rank 4, checkout): enters: F-25; leaves: -
files: docs/epics/catalogo.md, docs/epics/checkout.md
commit: chore(epic): milestones of loja
agile mvp WRITTEN: 4 milestones
```

```
## First release cut: loja-3-pagamento
Rank: 3
F-23, F-26

## First release cut: loja-4-pedidos
Rank: 4
F-25
```

```
- 2026-10-20 — milestones of loja written by /agile:mvp: 4 milestones (6 items); label: loja; goal: "vender produtos online com pagamento por cartão e acompanhar os pedidos"; gaps left: 0; milestones: loja-1-vitrine (see and search products), loja-2-carrinho (fill a cart), loja-3-pagamento (pay by card with shipping), loja-4-pedidos (follow the orders)
```

O Claude propõe o commit `chore(epic): milestones of loja`, com os dois epics por nome, e só o faz com a sua autorização. A análise e o motivo de cada marco ficaram no chat; nos cortes só há ids. O `/agile:order` passa a listar os marcos em sequência, e o F-24 em `Later`; este é o trecho real dos grupos:

```
## MVP: loja-1-vitrine

| # | Id | Title | Status | Model | Epic | Depends on | Unlocks |
|---|---|---|---|---|---|---|---|
| 1 | F-20 | Lista de produtos | approved | - | catalogo | - | F-22 |
| 2 | F-21 | Busca de produtos | approved | - | catalogo | - | - |

## MVP: loja-2-carrinho

| # | Id | Title | Status | Model | Epic | Depends on | Unlocks |
|---|---|---|---|---|---|---|---|
| 3 | F-22 | Carrinho | approved | - | catalogo | F-20 | F-23, F-26, first release of catalogo |

## MVP: loja-3-pagamento

| # | Id | Title | Status | Model | Epic | Depends on | Unlocks |
|---|---|---|---|---|---|---|---|
| 4 | F-23 | Pagar com cartao | approved | - | checkout | F-22 | F-24, F-25 |
| 5 | F-26 | Calculo de frete | idea | - | checkout | F-22 | - |

## MVP: loja-4-pedidos

| # | Id | Title | Status | Model | Epic | Depends on | Unlocks |
|---|---|---|---|---|---|---|---|
| 6 | F-25 | Painel de pedidos | approved | - | checkout | F-23 | first release of checkout |

## Later

| # | Id | Title | Status | Model | Epic | Depends on | Unlocks |
|---|---|---|---|---|---|---|---|
| 7 | F-24 | Cupom de desconto | approved | - | checkout | F-23 | - |
```

Semanas depois o F-20 e o F-21 foram entregues, e você digita `/agile:mvp loja --milestones`, sem o texto. O Claude não pede o objetivo de novo: lê o último gravado para o rótulo (`agile mvp GOAL: 2026-10-20 "vender produtos online com pagamento por cartão e acompanhar os pedidos"`) e a primeira linha diz `goal: reused from 2026-10-20`. Ele reavalia só o que está em aberto e propõe três marcos, juntando o cupom ao painel. A saída real do script, sobre esse novo estado:

```
cut loja-1-vitrine (kept, rank 1, catalogo): F-20, F-21
cut loja-2-carrinho (rank 2, catalogo): enters: -; leaves: -
cut loja-3-pagamento (rank 3, checkout): enters: -; leaves: -
cut loja-4-pedidos-e-cupom (rank 4, checkout): enters: F-25, F-24; leaves: -
cut loja-4-pedidos (removed, checkout): leaves: F-25
files: docs/epics/catalogo.md, docs/epics/checkout.md
commit: chore(epic): milestones of loja
agile mvp WRITTEN: 3 milestones
```

O corte `loja-1-vitrine` tem todos os ids entregues: fica como estava, com o número 1. Os outros são trocados, de qualquer epic, e os três novos recebem os números 2, 3 e 4. Se um item de um marco de número menor dependesse de um que você pôs num marco maior, o script o traria para o marco menor e diria `moved <id> from <corte> to <corte> (needed by <id>)`, para nenhum item esperar por um marco posterior. Rodar o mesmo plano de novo termina em `UNCHANGED`, sem escrever nada. Publicar uma versão continua sendo o `/agile:publish`.

### 14.167 Só o que um objetivo ainda precisa: `/agile:order <rótulo>` e o porquê de cada linha

No acme-shop o `/agile:mvp loja --milestones` gravou dois marcos para o objetivo "Vender online com pagamento por cartão": `loja-1-catalogo` (Rank 1, no épico `catalog`, com o F-12 e o F-13) e `loja-2-pagamento` (Rank 2, no `checkout`, com o F-14). O F-14 depende do F-15, que está no corte `admin`; o F-16 (lista de desejos) não está em corte nenhum. Você quer ver só o caminho até a loja, sem o resto do backlog:

```
> /agile:order loja
```

A saída real do script nesse projeto (no app, o mesmo vem como um widget só de leitura, com o objetivo no topo e o porquê embaixo de cada título):

```
# Order of implementation: loja

Goal: "Vender online com pagamento por cartão"

## MVP: loja-1-catalogo

| # | Id | Title | Status | Model | Epic | Depends on | Unlocks | Why |
|---|---|---|---|---|---|---|---|---|
| 1 | F-12 | Product list with filters | approved | - | catalog | - | F-13 | in cut loja-1-catalogo; level 1; feature; Must; size M |
| 2 | F-13 | Product page | idea | - | catalog | F-12 | - | in cut loja-1-catalogo; level 2: waits on F-12; feature; Must; size S |

## MVP: loja-2-pagamento

| # | Id | Title | Status | Model | Epic | Depends on | Unlocks | Why |
|---|---|---|---|---|---|---|---|---|
| 3 | F-15 | Payment provider settings | approved | - | admin | - | F-14, first release of admin | needed by F-14; level 1; feature; Should; size S |
| 4 | F-14 | Card payment | approved | - | checkout | F-15 | first release of checkout | in cut loja-2-pagamento; level 2: waits on F-15; feature; Must; size L |

## Summary

- loja-1-catalogo: 2 open (2 in the cut, 0 their dependencies)
- loja-2-pagamento: 2 open (1 in the cut, 1 their dependencies)
- Delivered in these cuts: 0

agile order SHOWN: 4 items for loja
```

O F-16 não aparece: nenhum corte de `loja` o pede. O F-15 entrou no segundo marco porque o F-14 precisa dele (`needed by F-14`), embora o corte dele seja o `admin`. A coluna `Why` diz por que cada linha está ali: o corte ou quem a puxou, o nível e o que ela espera, feature ou bug, e a prioridade e o tamanho da tabela do épico. O Claude resume em português: 4 itens, o objetivo, e que o primeiro é o F-12, nível 1 e Must. Nada foi gravado: nem o `docs/agile/order.md`, nem as marcas, nem commit (a seção `## Checklist` da saída foi omitida aqui).

Dias depois o F-12 é entregue e você digita o mesmo comando. A visão é recalculada dos arquivos, não guardada (da saída real, o primeiro grupo e o resumo):

```
## MVP: loja-1-catalogo

| # | Id | Title | Status | Model | Epic | Depends on | Unlocks | Why |
|---|---|---|---|---|---|---|---|---|
| 1 | F-13 | Product page | idea | - | catalog | - | first release of catalog | in cut loja-1-catalogo; level 1; feature; Must; size S |

## Summary

- loja-1-catalogo: 1 open (1 in the cut, 0 their dependencies)
- loja-2-pagamento: 2 open (1 in the cut, 1 their dependencies)
- Delivered in these cuts: 1

agile order SHOWN: 3 items for loja
```

O F-12 saiu, e o F-13 não espera mais nada (`level 1`, sem `waits on`). Se você digitar um rótulo que não existe, nada é mostrado nem gravado, e a resposta diz quais cortes há:

```
> /agile:order loj
agile order REFUSED: no cut named loj; cuts found: loja-1-catalogo, loja-2-pagamento, admin
```

O `/agile:order` sem argumento continua escrevendo a lista inteira, agora também com a coluna `Why`. Um rótulo como `ticks` ou `write` não pode ser usado num corte: o `/agile:mvp` o recusa, porque o `/agile:order` o leria como outra coisa.

### 14.168 Quem usa o app importa um tema e a página conta o que ficou de fora

O app web Cais já tem a página de aparência com a importação (o segundo item, "Import and generate themes in the MudBlazor kit", está entregue). Uma pessoa recebeu de um colega o arquivo `night-owl.json`, feito à mão: ele traz uma cor escrita como `red`, uma chave `Mystery` que o app não conhece e o foco (`Focus`, travado) em preto. Ela abre a página, toca em "Importar tema..." e escolhe o arquivo. A página aplica as mesmas regras do oráculo de importação do plugin, que dá, para esse arquivo (o mesmo conteúdo da fixture `report.json` do plugin):

```
themes.js import report.json
{
  "formatVersion": 1,
  "id": "night-owl",
  "name": "Night Owl",
  "type": "light",
  "colors": {
    "Accent": "#0B57D0",
    "Surface": "#FFFFFF",
    "Text.Muted": "#555F6D"
  }
}
ignored bad color: Text "red"
ignored unknown key: Mystery
ignored locked key: Focus
ACCEPTED night-owl
```

e a área de resultado da página mostra, em pt-BR:

```
Seus temas
  Night Owl                         [Baixar] [Remover]
[Importar tema...]  [Gerar a partir das minhas personalizações]

Resultado
  Importado "Night Owl" como night-owl
  ignoradas: 1 chave desconhecida, 1 chave travada, 1 cor inválida
```

"Night Owl" já aparece na lista de temas claros, sem recarregar a página, e fica no `localStorage` do navegador, na chave `appearance.themes`: outro navegador não o vê, e a pasta de temas do servidor não muda. O texto e o foco que faltaram vêm do tema claro padrão, Cais Light, e nenhum par de contraste falhou, então não há aviso. Se ela importar o mesmo arquivo de novo, o tema novo vira `night-owl-2`, com o nome "Night Owl (2)"; se escolher um tema do próprio VS Code, a página diz `Não importado: nenhuma chave de cor deste app` e nada é guardado.

### 14.169 A leitura de segurança de um item: `/agile:review F-7 security`

O item F-7 reembolsa um pedido e vai guardar uma porta. Antes de validar, você pede a leitura de segurança. Ela não escreve nada: o agente `security` só lê e devolve uma tabela. Sem o argumento `security` o comando é o revisor de sempre. Sem id, ou com um id sem branch e sem worktree (um app já pronto), o comando para e diz o motivo (o app pronto é do legacy-lens), e nenhum agente roda.

```
/agile:review F-7 security
```

O Claude acha a worktree do item, usa o `docs/security/findings.md` como está (a varredura roda antes só quando o arquivo é mais velho que o último commit da branch) e diz o modelo de cada modo antes de chamar, um modo por chamada:

```
Model: sonnet (leaks: no signal)
Model: opus (pentest: reasons about attacks)
```

O `leaks` achou a senha na string de conexão (mostrada como `[redacted: ...]`, nunca o valor) e o e-mail no log; o `pentest` achou a rota sem autorização e disse o ataque e o teste que o prova, sem carga de exploração. O Claude confere cada bloqueador e grave no código e mostra a tabela de verdade, tirada da execução real sobre uma cópia de teste:

```
| # | Severity | Finding | Verdict | Action |
|---|---|---|---|---|
| 1 | blocker | `POST /api/v1/orders/{id}/refund` has no `.RequireAuthorization()` (`Program.cs:3-7`). Anonymous callers can refund any order. | confirmed | fix now |
| 2 | blocker | The handler checks neither the manager role nor that the order belongs to the caller's tenant (`Program.cs:3-7`). Suggested tests: `Refund_WithoutManagerRole_Returns403`, `Refund_OrderOfOtherTenant_Returns403`. | confirmed | fix now |
| 3 | blocker | A production connection string with a plaintext password is committed in `appsettings.Production.json:3`. | confirmed | fix now: move it to a secret store and rotate the password |
| 4 | major | `body.CustomerEmail` (personal data) is written to the Information log (`Program.cs:5`). | confirmed | fix now |
```

Os três bloqueadores confirmados impedem o merge, como os do revisor. Você decide cada linha; o Claude corrige as que você mandar, grava uma linha por achado e a linha `Security coverage` em `## Decisions`. No build, um item que guarda uma porta recebe a proposta uma vez, depois da varredura: "Quer a leitura de segurança do agente (só lê, não escreve, custa uma chamada)?", e só roda com o seu sim; no piloto automático ela não é proposta e o relatório traz a linha `Agente de segurança: não rodou (autopilot); /agile:review F-7 security`.

### 14.170 Uma segunda biblioteca de UI: o AdminLTE 4 na pergunta 23

O brief pede um app web só para a equipe, em .NET, telas de tabela e formulário. Na rodada 5 a pergunta 4 já saiu como Blazor Server, então a 23 oferece o AdminLTE 4 ao lado do MudBlazor, que continua a recomendação. Você responde "AdminLTE 4". A mensagem da rodada, tirada de uma execução de verdade do bootstrap num app de teste (o teste rodou em inglês):

```
Round 5 — Experience.
- Questions 20 and 20a: pt-BR, the recommendation. Already clear: 21 (one locale, so no switch) and 22 (WCAG 2.2 AA).
- Question 23a: skipped for this test, so nothing goes under `docs/design`.
- Question 23: AdminLTE 4 is offered, because this is a `web-app` on Blazor Server.
- Question 23b: Outlined, which is Bootstrap Icons' base name.
- Question 23c: row action (pencil).
- Question 23f: no (when this example ran, AdminLTE had no run-time theme module and the question was not asked; since #248 it is, see example 14.174).
```

No `CLAUDE.md` entra a linha `- UI library: adminlte` embaixo de `Profile:`, e o ADR-0001 ganha a linha da biblioteca (`AdminLTE 4 (package ColorlibHQ.AdminLTE.AspNetCore 0.3.1; sidebar stays the package's dark #343A40 in both themes)`); a linha de temas vem da 23f (hoje também feita para o AdminLTE, exemplo 14.174). A ideia do kit de UI passa a seguir a seção `## AdminLTE kit` do perfil.

Outro app, mesmo brief, mas você responde Blazor WebAssembly na pergunta 4. A 23 não oferece o AdminLTE e a mensagem diz o porquê (texto da execução):

```
- **23 (biblioteca):** o AdminLTE 4 não é oferecido com Blazor WebAssembly, porque o pacote falha o build `browser-wasm` (NETSDK1082). Vale o MudBlazor.
```

O `CLAUDE.md` não ganha a linha `UI library`, e quem chegou aqui tem o MudBlazor como sempre.

### 14.171 O registro de execuções de um item: o que cada agente custou e como terminou

O F-7 foi construído numa terça e revisado na quarta, em duas sessões. Na terça, o build chamou o `system-design` e o `architect`; na quarta, a primeira chamada do `reviewer` voltou com a resposta vazia e a segunda trouxe o relatório. Ninguém escreveu nada à mão: os hooks gravaram duas linhas por agente no registro do F-7, na pasta comum do git. Este é o registro (os ids são do exemplo):

```jsonl
{"v":1,"ev":"start","item":"F-7","agent":"a3f1c09e2b7d4410","session":"11111111-1111-4111-8111-111111111111","role":"system-design","stage":"build","at":"2026-10-21T14:02:11.000Z"}
{"v":1,"ev":"end","item":"F-7","agent":"a3f1c09e2b7d4410","at":"2026-10-21T14:04:03.000Z","status":"done","model":"claude-opus-5-5","tokens":{"in":38,"out":4120,"cacheRead":412300,"cacheWrite":61200}}
{"v":1,"ev":"start","item":"F-7","agent":"b81d77c2e0a94f31","session":"11111111-1111-4111-8111-111111111111","role":"architect","stage":"build","at":"2026-10-21T14:05:40.000Z"}
{"v":1,"ev":"end","item":"F-7","agent":"b81d77c2e0a94f31","at":"2026-10-21T14:07:10.000Z","status":"done","model":"claude-opus-5-5","tokens":{"in":22,"out":2890,"cacheRead":298100,"cacheWrite":40800},"findings":{"blocker":0,"major":1,"minor":1}}
{"v":1,"ev":"start","item":"F-7","agent":"c47e02a9d1f86b55","session":"22222222-2222-4222-8222-222222222222","role":"reviewer","stage":"review","at":"2026-10-22T09:15:00.000Z"}
{"v":1,"ev":"end","item":"F-7","agent":"c47e02a9d1f86b55","at":"2026-10-22T09:15:20.000Z","status":"failed","model":"claude-opus-5-5","tokens":{"in":9,"out":0,"cacheRead":51000,"cacheWrite":18300}}
{"v":1,"ev":"start","item":"F-7","agent":"d920f3b6a7c14e88","session":"22222222-2222-4222-8222-222222222222","role":"reviewer","stage":"review","at":"2026-10-22T09:16:02.000Z"}
{"v":1,"ev":"end","item":"F-7","agent":"d920f3b6a7c14e88","at":"2026-10-22T09:18:16.000Z","status":"done","model":"claude-opus-5-5","tokens":{"in":31,"out":3650,"cacheRead":502700,"cacheWrite":55900},"findings":{"blocker":1,"major":0,"minor":2}}
```

Para ler, numa sessão qualquer do repositório (o checkout principal ou uma worktree):

```powershell
node "$env:CLAUDE_PLUGIN_ROOT/scripts/run-log.js" show F-7
```

```bash
node "${CLAUDE_PLUGIN_ROOT}/scripts/run-log.js" show F-7
```

A saída real sobre esse registro:

```text
Run log of F-7: 4 runs (3 done, 1 failed, 0 running, 0 lost)
1. system-design (build), try 1: done in 1 min 52 s — claude-opus-5-5, tokens in 38, out 4120, cache read 412300, cache write 61200
2. architect (build), try 1: done in 1 min 30 s — claude-opus-5-5, tokens in 22, out 2890, cache read 298100, cache write 40800; findings 0 blocker, 1 major, 1 minor
3. reviewer (review), try 1: failed in 20 s — claude-opus-5-5, tokens in 9, out 0, cache read 51000, cache write 18300
4. reviewer (review), try 2: done in 2 min 14 s — claude-opus-5-5, tokens in 31, out 3650, cache read 502700, cache write 55900; findings 1 blocker, 0 major, 2 minor
Totals: 5 min 56 s of finished runs; tokens in 100, out 10660, cache read 1264100, cache write 176200; findings 1 blocker, 1 major, 3 minor
```

A terceira linha é a revisão que voltou vazia (`failed`), e a quarta é a segunda tentativa do mesmo papel (`try 2`), com um bloqueador e dois menores. O registro não guarda nenhum texto: nem o prompt, nem o relatório, só ids, papéis, horários, o modelo e as contagens. Um agente ainda rodando aparece como `running for <tempo>`, e um que começou há mais de 2 horas sem fim, como `lost`. Com `--json` a mesma lista sai como dados, para o módulo de UI do #240.

### 14.172 O ship limpa as scratches do item e mostra a pasta órfã

O item #268 foi mergeado e a worktree dele saiu. Na raiz de scratches estão `268`, `268-live`, `2680` e `other`; na raiz das worktrees sobrou `old-f-200`, que o git não lista. O ship roda a limpeza na checkout principal (saída de uma execução de verdade do script nesse cenário, sem nada a pedir antes porque digitar `/agile:ship` já autorizou as scratches do item):

```
removed: S:\scratch\268
removed: S:\scratch\268-live
scratches: 2 removed for item 268 under S:\scratch.
ORPHAN: S:\wt\old-f-200
  build.log
  notes.txt
orphans: 1 under S:\wt.
agile cleanup ORPHANS: 1
```

`2680` e `other` ficam: não são do item 268. Nada foi apagado da pasta órfã. O Claude pergunta:

```
Pergunta: apagar a pasta órfã S:\wt\old-f-200 (build.log, notes.txt)?
Recomendação: não apagar sem olhar — Motivo: o git não a lista e o ship não sabe de quem é.
```

Você responde "sim": o ship roda `worktree.js remove S:\wt\old-f-200 --orphan` e a linha do relatório fecha assim:

```
Cleanup: 2 scratches removidas (268, 268-live), 1 pasta órfã (removida)
```

Com "não" a pasta fica e a linha diz `(mantida)`. Sem nada a limpar a linha é `Cleanup: nada a limpar`. À mão, `--dry-run` mostra o que sairia (saída do mesmo cenário com uma scratch `268-b` a mais):

```
would remove: S:\scratch\268-b
scratches: 1 would go for item 268 under S:\scratch.
would remove (after a yes): S:\wt\old-f-200
orphans: 1 under S:\wt.
agile cleanup DRY-RUN
```

### 14.173 O rodapé acima do prompt e o Process board: onde o tempo e os tokens do item foram

O F-12 está em `building`. O `architect` rodou às 13:18, a primeira revisão achou um bloqueador e dois maiores, a segunda revisão (`try 2`) voltou só com dois menores, e agora o agente `security` está rodando. Você não pergunta nada: o rodapé acima do prompt, que o módulo do agile desenha num cliente que carrega módulos de plugin, mostra a trilha do item, lida do `status:` do arquivo, e o que roda agora:

```text
F-12  ●idea ─●refining ─●approved ─◉building ┄validating ┄done  security running
```

Você digita `/process-board` e o painel lateral abre com uma linha por execução, lidas do registro do item:

```text
Process board — F-12   elapsed 1h 12m   76k in / 5.6k out   1.5M read / 109k written
#  Task    Agent (model)                  In / Out    Start  Duration  Verdict
1  build   architect (claude-sonnet-5-5)  12k / 1.1k  13:18  1 min     0 blocker, 0 major, 1 minor
2  review  reviewer (claude-opus-5-5)     30k / 2.4k  14:00  3 min     1 blocker, 2 major, 0 minor
3  review  reviewer (claude-opus-5-5)     34k / 2.1k  14:18  2 min     0 blocker, 0 major, 2 minor try 2
4  review  security (-)                   - / -       14:29  1 min     running
```

O topo soma os tokens das execuções terminadas (o cache à parte); o `security` ainda não tem modelo nem tokens, porque eles chegam na linha de fim. Os horários são os do seu computador. Neste repositório do plugin, onde o item é um issue e não há arquivo com `status:`, o rodapé mostra só o id e o que roda (`F-240 · security running`), sem trilha. Num cliente sem módulos de plugin nada aparece, e com `AGILE_HOOKS=off` o rodapé some e o comando responde que o módulo está desligado.

### 14.174 Temas personalizáveis no kit AdminLTE: a pergunta 23f deixa de ser pulada

O brief é o mesmo do exemplo 14.170 (um app web só para a equipe, tabelas e formulários) e você escolhe o AdminLTE 4 na 23. Até o #248 a 23f não era feita para esse kit; agora ela aparece na rodada 5, com sim como recomendação. A mensagem da rodada, tirada de uma execução de verdade do bootstrap num app de teste:

```
## Rodada 5 — Experiência
Já claro: AdminLTE 4 é oferecido porque o perfil é `web-app` e a pergunta 4 será Blazor Server.

| # | Pergunta | Recomendação | Motivo |
|---|---|---|---|
| 23 | Biblioteca | MudBlazor (padrão) ou AdminLTE 4 | AdminLTE se o brief quiser painel nesse visual |
| 23b | Família de ícones | Outlined | Telas densas |
| 23c | Edição de item | Ação na linha (lápis) | Um jeito só |
| 23f | Temas personalizados (padrão VS Code) | Sim | O kit AdminLTE tem módulo de temas em tempo de execução |

**Resposta do dono:**
- 23: AdminLTE 4, que foi oferecido.
- 23f: sim.
```

Um **sim** grava no ADR-0001 a linha da execução:

```
| 23f | Customizable themes | yes | VS Code pattern (references/theme-format.md) |
```

Nesse teste a identidade (23a) foi pulada, então o `themes.js write --init` não rodou; num app com identidade ele roda logo depois dela e cria `docs/design/themes/`. No passo 13 o bootstrap captura a ideia "Run-time themes in the AdminLTE kit", que segue a seção `## AdminLTE run-time themes` do perfil: a página `/appearance`, o `appearance.js` no `head` e o botão claro/escuro do pacote desligado.

### 14.175 Todos os MVPs numa olhada: `/agile:mvp --list`

No acme-shop há três epics. O `catalog` tem dois marcos: `loja-1-vitrine` (posição 1, F-1 e F-2, os dois entregues) e `loja-2-carrinho` (posição 2: o F-3 entregue, o F-4 em aberto e o F-5 cancelado). O `checkout` tem um corte sem rótulo e sem posição com o F-6 entregue, o F-7 e o F-8 em aberto (o F-8 depende do F-9, do `admin`). O `admin` tem a seção do corte, mas vazia. Você quer saber onde cada MVP está, sem mudar nada, e digita `/agile:mvp --list`. O Claude roda a lista na branch principal; a saída real, sobre esse cenário:

```text
- loja-1-vitrine · rank 1 · catalog · open 0 · delivered 2 · delivered
- loja-2-carrinho · rank 2 · catalog · open 1: F-4 · delivered 1
- checkout · rank none · checkout · open 2: F-7, F-8 · delivered 1
empty cuts not listed: admin
agile mvp LIST: 3 MVPs, 1 delivered
```

E diz em pt-BR: três MVPs, um já entregue (`loja-1-vitrine`); o `loja-2-carrinho` espera só o F-4 (o F-5 cancelado não conta); o `checkout`, sem posição, vem depois dos que têm e espera o F-7 e o F-8; o corte do `admin` está vazio e por isso não é MVP. O F-9 não aparece na linha do `checkout`, embora o F-8 dependa dele: ele não está no corte, e o grupo inteiro, com as dependências, é o `/agile:order checkout`. Nada foi gravado, nenhum card, nenhum commit. Se você digitar `/agile:mvp --list loja`, o Claude responde que o `--list` não aceita outro argumento e que um MVP sozinho é o `/agile:order <rótulo>`.

### 14.176 A pergunta 23f num app desktop com cabeça WinUI 3: o kit dele tem módulo de temas, e o dono vira o modo do Windows uma vez

A acme-ponto é um app `desktop` com cabeça WinUI 3, só para Windows, com o visual nativo. Na rodada 5, depois de a 23a gravar a identidade, o bootstrap pergunta, com sim como recomendação (antes do #251 o kit do WinUI 3 não tinha módulo e a pergunta nem era feita):

```
Pergunta 23f — Temas personalizáveis, no padrão do VS Code?
a) sim   b) não
Recomendação: sim. Motivo: quem usa o app escolhe o tema do modo claro e do escuro do Windows e a fonte, e o kit aplica isso em tempo de execução.
```

O dono responde sim. O ADR-0001 ganha a linha `Customizable themes | yes | VS Code pattern (references/theme-format.md)`, e o bootstrap roda o script de temas logo depois da identidade (a saída é a do script no projeto de teste desta seção):

```
high contrast: 1 colors adjusted, 0 of them backgrounds
WROTE 7 files
```

A pasta `docs/design/themes/` é o que liga o módulo. No passo 13 entra no quadro a ideia "Run-time themes in the WinUI 3 kit", com prioridade Must e tamanho M, logo depois da ideia do kit de UI e dependendo dela; o resumo aponta para o bloco `### WinUI 3` do `## Desktop run-time themes` do perfil.

Quando o item do kit chega ao `validating`, o roteiro de validação tem um passo que só o dono faz: abrir a janela de configurações do app (a que traz a `AppearanceView`), pôr o modo em Sistema, escolher um tema para o claro e outro para o escuro, e então virar a configuração claro e escuro do próprio Windows uma vez. O app troca para o tema preferido da nova variante sem reiniciar, porque a cabeça WinUI 3 acompanha o Windows pelo evento de mudança de cores do sistema. Tudo o mais (o modo escolhido à mão, a troca de tema na mesma variante, a fonte e o tamanho, o aviso de contraste por par) o Claude já leu na janela em execução, pela árvore de automação do Windows e por uma captura da própria janela.

### 14.177 O quadro do GitHub atrás dos cortes: `/agile:mvp --list` e `/agile:mvp --mirror`

A acme-shop usa GitHub (`Board: GitHub Issues + Projects (acme/acme-shop) via gh`). Os cortes do exemplo 14.175 já estavam escritos antes de o plugin espelhar os MVPs, e no repositório só existe um marco posto à mão, `v1`, na issue do F-7. Você roda `/agile:mvp --list` e, ao final de cada linha, vê o estado do marco (a saída é a do script na acme-shop deste exemplo):

```text
- loja-1-vitrine · rank 1 · catalog · open 0 · delivered 2 · delivered · milestone missing
- loja-2-carrinho · rank 2 · catalog · open 1: F-4 · delivered 1 · milestone missing
- checkout · rank none · checkout · open 2: F-7, F-8 · delivered 1 · milestone missing
item F-7 has milestone v1, the cut asks checkout
empty cuts not listed: admin
agile mvp LIST: 3 MVPs, 1 delivered
```

O Claude diz em pt-BR que os três MVPs ainda não têm marco no GitHub e que `/agile:mvp --mirror` resolve; o F-7 tem o `v1` posto à mão e o espelho não vai mexer nele. Você roda `/agile:mvp --mirror`: o Claude não lê o brief nem grava corte, só mostra o plano em um bloco:

```text
milestone loja-1-vitrine: create
milestone loja-2-carrinho: create
milestone checkout: create
item F-7 has milestone v1, the cut asks checkout
item F-1: none → loja-1-vitrine
item F-2: none → loja-1-vitrine
item F-3: none → loja-2-carrinho
item F-4: none → loja-2-carrinho
item F-6: none → checkout
item F-8: none → checkout
agile mvp MIRROR PLAN: 3 MVPs, 9 changes
```

Ele explica: três marcos serão criados, seis issues levam o marco do MVP (nove mudanças ao todo) (as entregues F-1, F-2, F-3 e F-6 também, para o progresso de cada marco bater), e a issue do F-7 fica com o `v1`. O card pergunta: **escrever (recomendado)** ou pular. Você escolhe escrever, o Claude roda `mirror --write` e a última linha é `agile mvp MIRRORED: 3 MVPs, 9 changes`. Um `/agile:mvp --list` agora termina cada linha com `milestone ok`, e um segundo `/agile:mvp --mirror` diz `agile mvp MIRROR PLAN: 3 MVPs, 0 changes` (o aviso do F-7 continua, porque o marco dele é seu). Os marcos têm no GitHub a descrição de duas linhas, `Objetivo: vender pelo site` (o objetivo gravado do rótulo `loja`) e `Espelho de /agile:mvp; o corte no arquivo do épico é a fonte.`; o `checkout`, sem objetivo gravado, leva `Primeira entrega do épico checkout: brief e prioridade Must`.

Se o `gh` tivesse falhado na hora de um `/agile:mvp` com escrita, o corte estaria no arquivo e o script diria `milestone pending: <a primeira linha do erro>`; o Claude avisa que o quadro ficou atrás e que `--mirror` o acerta, sem refazer a análise.

### 14.178 Uma extensão do Visual Studio pelo bootstrap: a rodada da extensão, o esqueleto e o `.vsix`

O brief da Ana descreve uma extensão que lista, no Visual Studio, os pull requests à espera de revisão. Rodada 1:

```
Claude: 1. Tipo de app → Recomendação: Visual Studio extension. O brief fala de um
       comando dentro do Visual Studio, não de telas do app nem de uma API.
    2. Perfil de arquitetura → Recomendação: vs-extension (o único para uma extensão).
> ok
```

No lugar das rodadas 2 a 8 vem uma só:

```
Claude: V1. Nome → Recomendação: ReviewHelper (PascalCase; vira o namespace, a solução
       e o prefixo dos três projetos).
    V2. Nome de exibição e descrição → Recomendação: "Review Helper" e "Shows the pull
       requests waiting for a review."
    V3. Publicador → Recomendação: Ana Souza.  V4. Licença → Recomendação: MIT.
    V5. Repositório → Recomendação: ana-souza/review-helper (o `origin` já o dá).
    27. Board → Recomendação: GitHub Issues + Projects (o código está no GitHub).
    28c. Raiz das worktrees → Recomendação: D:\wt\review-helper.
    28d. Merge → Recomendação: direct.  34. Modelos → as linhas de sempre, sem as de tela.
    20a. Idiomas do manual → Recomendação: pt-BR.
    Já está claro: uma extensão não tem banco, ambientes, idiomas de UI nem telas do
    próprio agile; branch principal main, fluxo trunk.
> ok
```

Depois da pergunta de fechamento, do resumo e do "confirmo", o Claude descreve o esqueleto e, com o seu sim, copia primeiro os arquivos de `templates/dotnet/` (o `Directory.Build.props` com `<Version>0.1.0</Version>`, o `.editorconfig`, o `BannedSymbols.txt`, o `global.json`) e por cima o `templates/vs-extension/`: `ReviewHelper.slnx`, `src/ReviewHelper.Extension` (o ponto de entrada e o comando "Say hello" no menu Extensions), `src/ReviewHelper.Core`, `tests/ReviewHelper.Core.Tests`, o `Directory.Packages.props` com os cinco pacotes e o `README.md`. O Claude confere no nuget.org se as versões ainda são as últimas estáveis, e depois roda o build e o teste uma vez sobre o esqueleto novo, antes do primeiro commit; saída real sobre o esqueleto do exemplo:

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 46 ms - ReviewHelper.Core.Tests.dll (net8.0)
src/ReviewHelper.Extension/bin/Debug/net8.0-windows8.0/ReviewHelper.Extension.vsix
```

O `CLAUDE.md` tem `Profile: vs-extension` e nenhuma linha de idiomas de UI; não há `build.json` (o motor `dotnet` é o padrão) nem `extension.vsixmanifest` no repositório: o build o gera ao lado do `.vsix`, com a versão do `Directory.Build.props`. O relatório final diz como ver a extensão: instalar a carga de trabalho "Visual Studio extension development", abrir a solução, F5 no projeto `ReviewHelper.Extension` e procurar "Say hello" no menu Extensions da Experimental Instance. Se você pedir `/agile:publish` agora, a resposta é uma linha só e nada é gravado: `a Visual Studio extension is not published by /agile:publish yet: the upload to the Visual Studio Marketplace is #264; take the .vsix from the build output`.

### 14.179 O corte das regras sempre carregadas: o `/agile:sync` mostra o tamanho antes e depois

O Simulab tem um `CLAUDE.md` de 530 palavras e as cinco regras sempre carregadas (`workflow`, `git`, `output-style`, `definition-of-done`, `naming`) como o plugin as trazia antes do corte. Neste `/agile:sync` a linha de tamanho marca:

```
| Tamanho do CLAUDE.md | 530 palavras (limite ~900); sempre carregado ~4,2k tokens (orçamento ~4,5k) | nenhuma |
```

Os números vêm do `sync.js plan` (`alwaysLoadedWords` 3202, `alwaysLoadedTokens` 4163). As cinco cópias nunca foram editadas, então a tabela as lista como `update`, mais uma regra nova, `naming-dotnet.md`. Você aprova, e o `apply` responde:

```
copied   .claude/rules/agile/workflow.md
copied   .claude/rules/agile/git.md
copied   .claude/rules/agile/output-style.md
copied   .claude/rules/agile/definition-of-done.md
copied   .claude/rules/agile/naming.md
copied   .claude/rules/agile/naming-dotnet.md
```

No sync seguinte a mesma linha marca:

```
| Tamanho do CLAUDE.md | 530 palavras (limite ~900); sempre carregado ~3,3k tokens (orçamento ~4,5k) | nenhuma |
```

Foram 880 tokens a menos (`alwaysLoadedTokens` 3283): as cinco regras passaram de 2.672 para 1.995 palavras, e o `naming-dotnet.md` não entra na conta porque só carrega quando o Claude lê um `.cs`, um `.razor` ou um `.sql`. A sobra de cerca de 1,2k tokens é dos retros do Simulab (`project.md`). Um projeto de perfil `plugin` recebe os mesmos cinco arquivos e nenhuma linha de C#, de banco ou de `WithDataVolume`. O `CLAUDE.md` do Simulab não muda: o sync nunca o escreve, e só um projeto novo ganha as linhas mais curtas do template (as evals, os plugins, os modelos, sem a linha dos três portões, que a regra de workflow já diz).

### 14.180 Onde as instruções pesam: `/agile:doctor` num projeto

Entre duas features do Agenda, a Ana acha que as sessões começam pesadas e digita `/agile:doctor`. O Claude roda o `doctor.js` na raiz do projeto; nada é gravado. Última linha do script sobre o projeto do exemplo:

```
agile doctor REPORTED: 4 files, 2 causes, 4 proposals
```

O relatório, com os números do JSON:

```
Claude: Orçamento: 463 palavras (602 tokens) carregadas em toda sessão, de 4.500 tokens;
       sobram 3898.
    Arquivos mais pesados:
      CLAUDE.md                    386 palavras  502 tokens  maior linha 381  always
      .claude/rules/agile/git.md    77 palavras  100 tokens  maior linha  66  always
      docs/agile/retro-log.md        8 palavras   10 tokens               per command
      docs/agile/profile.md          4 palavras    5 tokens               per command
    Causas:
    1. Limite de linhas escondendo crescimento: .claude/rules/agile/git.md:3 tem 66
       palavras (teto 60) num arquivo de 3 linhas, dentro do teto de 30.
    2. História de incidente em regra: .claude/rules/agile/git.md:3, 66 palavras, com
       número de issue (#14) e data (2026-09-14).
    Nota: histórico mais curto que 30 dias nos 4 arquivos; sem número de crescimento.
    Julgamento: nenhum par de regras dizendo o mesmo com outras palavras.
    Cortes de conteúdo:
    - .claude/rules/agile/git.md:3 — o script diz "shorten" (6 palavras acima do teto);
      proponho reduzir à regra, cerca de 20 palavras ("depois de um rebase pós-revisão,
      rode a suíte inteira antes do merge (#14)"): a linha é uma história.
    Mudanças no plugin:
    - Um teto de palavras por linha nos arquivos de regra, no relatório de tamanho do
      /agile:sync.
    - A forma de lição do CLAUDE.md: a regra e as issues, sem a história.
```

O card, com escolha múltipla:

```
Claude: ❓ Quais correções capturar como ideia?
    Cortes:  [ ] git.md:3 reduzir à regra (#14)
    Plugin:  [ ] teto de palavras por linha no /agile:sync   [ ] forma de lição
> marca "git.md:3 reduzir à regra" e "forma de lição"
```

O corte vira o item `F-7` do projeto, pela captura de ideias (status `idea`, com as linhas de evidência). A mudança no plugin vira uma nota `⏳ plugin: agile` no `docs/agile/retro-log.md`, com a linha na tabela "Plugin notes — status": quem a entrega é a sessão do plugin, e depois da entrega o próximo `/agile:sync` do Agenda marca a nota ✅. O Claude mostra o `git status` com os dois arquivos e propõe o commit; só com o "ok" da Ana ele faz o commit. Se a Ana não marcar nada, a resposta é `Nada capturado.` e o `git status` fica igual.

### 14.181 Uma regra só de SQL: o `/agile:retro` escreve em `project-sql.md` e a linha velha sai do `project.md`

No acme-reports o F-31 decidiu "todo arquivo `.sql` começa com `SET NOCOUNT ON`", e a tabela da retro classifica a linha como **Language rule** (vale para os arquivos de uma só linguagem da lista: `dotnet`, `sql`, `javascript`, `css`). O Claude propõe o destino `.claude/rules/agile/project-sql.md` e, como o `project.md` já tem uma linha que vale só para SQL ("uma migration nunca apaga uma coluna na mesma versão que para de lê-la", F-9), propõe também movê-la. Você escolhe as duas e o Claude roda o `scripts/rule-line.js`, nunca à mão: a linha velha entra no arquivo novo com a sua fonte e a sua data, e só então sai do `project.md`; saída real sobre o fixture do exemplo:

```
- A migration never drops a column in the same release that stops reading it (F-9, 2026-09-01)
agile rule-line ADDED: .claude/rules/agile/project-sql.md
- A migration never drops a column in the same release that stops reading it (F-9, 2026-09-01)
agile rule-line REMOVED: line 4 of .claude/rules/agile/project.md
- Every .sql file starts with SET NOCOUNT ON (F-31, 2026-10-22)
agile rule-line ADDED: .claude/rules/agile/project-sql.md
```

O `project-sql.md` não existia: a primeira regra o criou, com `paths:` igual a `**/*.sql`, então ele só carrega quando um arquivo `.sql` é lido ou escrito; o `project.md` ficou só com a regra do dinheiro, que vale para tudo. Uma regra que vale para .NET e SQL ao mesmo tempo continuaria no `project.md`, sem cópia nos dois. Um nome de linguagem fora da lista não grava nada: `agile rule-line REFUSED: unknown language "cobol": the known ones are dotnet, sql, javascript, css`.

### 14.182 As seções compartilhadas do perfil: o `/agile:sync` traz os quatro arquivos novos

O Simulab é um projeto `web-app` sincronizado pela última vez na 0.151.0, com o `docs/agile/profile.md` nunca editado. Neste `/agile:sync` a tabela lista o perfil como `update` e quatro arquivos que o projeto ainda não tem como `new`; o `sync.js plan` diz:

```
update   docs/agile/profile.md
new      docs/agile/profile-sections/local-services.md
new      docs/agile/profile-sections/deploy-aspire.md
new      docs/agile/profile-sections/deploy-aca.md
new      docs/agile/profile-sections/deploy-compose.md
```

Você aprova, e o `apply` responde:

```
copied   docs/agile/profile.md
copied   docs/agile/profile-sections/deploy-aspire.md
copied   docs/agile/profile-sections/deploy-aca.md
copied   docs/agile/profile-sections/deploy-compose.md
copied   docs/agile/profile-sections/local-services.md
```

O `profile.md` passou de 12.641 para 8.215 palavras: as quatro seções agora são uma linha cada, o ponteiro com o `Read when`, e o texto delas está nos arquivos novos (715, 2.479, 875 e 543 palavras). No próximo `/agile:build` de um item que acrescenta um campo numa tela, o Claude lê o perfil e nenhum desses arquivos; num item que muda a porta do PostgreSQL no AppHost, ele lê também `local-services.md`.

Se você tivesse acrescentado uma linha sua ao `profile.md` (na seção `## Layout`, por exemplo), a tabela o listaria como `edited`, o `apply` recusaria (`refused  docs/agile/profile.md: edited, merge by hand`) e o Claude faria o merge de três vias: a sua linha fica, cada seção movida vira o ponteiro, e os quatro arquivos chegam como `new` do mesmo jeito.

### 14.183 As seções de tema do perfil: o `/agile:sync` traz os três arquivos de um `web-app`

O Simulab é um projeto `web-app` sincronizado na 0.156.0, com o `docs/agile/profile.md` nunca editado e os quatro arquivos de AppHost já copiados. Neste `/agile:sync` o `sync.js plan` lista o perfil como `update`, os três arquivos de tema como `new` e os quatro de AppHost como `same`:

```
update   docs/agile/profile.md
new      docs/agile/profile-sections/runtime-themes-mudblazor.md
new      docs/agile/profile-sections/adminlte-kit.md
new      docs/agile/profile-sections/adminlte-themes.md
same     docs/agile/profile-sections/local-services.md
same     docs/agile/profile-sections/deploy-aspire.md
same     docs/agile/profile-sections/deploy-aca.md
same     docs/agile/profile-sections/deploy-compose.md
```

Você aprova, e o `apply` responde:

```
copied   docs/agile/profile.md
copied   docs/agile/profile-sections/runtime-themes-mudblazor.md
copied   docs/agile/profile-sections/adminlte-kit.md
copied   docs/agile/profile-sections/adminlte-themes.md
```

O `profile.md` passou de 8.193 para 2.109 palavras: as três seções de tema agora são uma linha cada, o ponteiro com o `Read when`, e o texto delas está nos arquivos novos (1.956, 1.755 e 2.476 palavras). O Simulab usa MudBlazor (o `CLAUDE.md` não tem a linha `- UI library: adminlte`), então os dois arquivos de AdminLTE estão na pasta mas não são lidos. No próximo `/agile:build` de um item que acrescenta um campo numa tela, o Claude lê o perfil e nenhum arquivo de tema; num item que acrescenta um seletor de tema à página `/appearance`, ele lê também `runtime-themes-mudblazor.md`. Num projeto com `- UI library: adminlte`, o primeiro item lê `adminlte-kit.md` e o segundo, `adminlte-themes.md`.

No Bolso, um projeto `mobile` sincronizado na mesma versão, o `sync.js plan` lista `update` para o `docs/agile/profile.md` e `new` para um único arquivo de tema, e o `apply` responde:

```
copied   docs/agile/profile.md
copied   docs/agile/profile-sections/runtime-themes-maui.md
```

O `profile.md` do Bolso passou de 12.392 para 10.163 palavras (o arquivo novo tem 2.289, e a frase sobre ler o ponteiro só quando o `Read when` combina entrou no primeiro parágrafo). Um item que muda a tela de pedidos não lê o `runtime-themes-maui.md`; um que muda o módulo de temas da cabeça MAUI lê.

Se você tivesse acrescentado uma linha sua ao `profile.md`, a tabela o listaria como `edited`, o `apply` recusaria e o Claude faria o merge de três vias: a sua linha fica, cada seção de tema vira o ponteiro, e os três arquivos chegam como `new` do mesmo jeito.

### 14.184 O corpo de issue quebrado, parado antes do envio: a guarda de corpo do repositório do plugin

Na sessão do plugin, o Claude monta o corpo da issue #303 e o envia. Uma substituição caiu no começo da linha 1 e a linha de status ficou para trás. A guarda lê o arquivo antes de o `gh issue edit` sair e responde:

```
Claude: gh issue edit 303 --body-file <scratch>/body-303.md
[agile body-guard] line 1 is not a status line: "| AC16 | check-all.js GREEN | pending |" (<scratch>/body-303.md).
This body would replace the item's issue as it is. Write the whole body with the Write tool and read line 1 back before the send (.claude/rules/ship.md, "Editing").
If this body is meant to be sent as it is, ask the owner; with a yes, repeat the command ending with the comment  # agile:body-ok
```

Nada foi enviado ao GitHub. O Claude reescreve o corpo inteiro com a ferramenta Write, lê a linha 1 (`Status: approved (...)`) e envia de novo. Num corpo de item aberto cuja tabela `## Criterion → test` deixou critérios sem linha, a recusa nomeia todos de uma vez:

```
Claude: gh issue edit 303 --body-file <scratch>/body-ac.md
[agile body-guard] AC2, AC3 have no row in ## Criterion → test (<scratch>/body-ac.md).
This body would replace the item's issue as it is. Write the whole body with the Write tool and read line 1 back before the send (.claude/rules/ship.md, "Editing").
If this body is meant to be sent as it is, ask the owner; with a yes, repeat the command ending with the comment  # agile:body-ok
```

Linhas agrupadas (`AC1-AC4`, `AC1, AC2`) contam como cobertura; um corpo com prefixo `Deferred (` ou `Cancelled (` seguido de `Status:`, um corpo `done` e uma seção só com prosa passam sem uma palavra. O comentário de bypass só entra no comando depois do sim do dono.

### 14.185 Três projetos de teste lado a lado no fim do turno, e um RED que nomeia todos

O Claude muda uma classe de `LibA`, uma biblioteca que três projetos de teste referenciam (`T1`, `T2`, `T3`, cada um com um teste de 5 s). No fim do turno o gate compila os três e os roda ao mesmo tempo:

```
agile gate
sdk 11.0.100-rc.2.26504.105 (.)
built tests/T1/T1.csproj (2 s)
built tests/T2/T2.csproj (2 s)
built tests/T3/T3.csproj (2 s)
tested tests/T1/T1.csproj (7 s)
  Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 5 s - T1.dll (net11.0)
tested tests/T2/T2.csproj (7 s)
  Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 5 s - T2.dll (net11.0)
tested tests/T3/T3.csproj (7 s)
  Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 5 s - T3.dll (net11.0)
tests side by side: 3 project(s), 3 at once (7 s)
agile gate GREEN
```

O turno levou 12 s; com `"testParallel": 1` no `.claude/agile/build.json` o mesmo turno levou 25 s, os três `tested` um depois do outro e sem a linha `tests side by side`. Cada linha `tested` guarda os segundos do próprio projeto, e a linha de baixo diz quanto o passo de testes levou de verdade.

No turno seguinte, um teste de `T1` falha e um de `T2` trava (o gate rodou com `AGILE_GATE_HANG_TIMEOUT` em 15 s). `T3` roda até o fim, e o RED traz os dois projetos quebrados, cada um com o seu corpo (as linhas da pilha de chamadas foram cortadas aqui):

```
[agile gate] [<scratch>\repo]
sdk 11.0.100-rc.2.26504.105 (.)
tests failed: tests/T1/T1.csproj
Test run for <scratch>\repo\tests\T1\bin\Debug\net11.0\T1.dll (.NETCoreApp,Version=v11.0)
A total of 1 test files matched the specified pattern.
Data collector 'Blame' message: All tests finished running, Sequence file will not be generated.
  Failed Probe_T1 [5 s]
  Error Message:
   Assert.Fail failed. planted failure of T1

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 5 s - T1.dll (net11.0)
a test hung (more than 15 s) or the test host crashed: tests/T2/T2.csproj
not finished: T.Probe.Probe_T2
full output: <scratch>\tmp\agile-canary\gate-logs\20261023-141013-434-stop.log
agile gate RED: a test hung (tests/T2/T2.csproj); tests failed (tests/T1/T1.csproj: Probe_T1) — full output: <scratch>\tmp\agile-canary\gate-logs\20261023-141013-434-stop.log
```

O veredito cita o travamento primeiro; o log salvo tem a saída inteira dos três projetos, cada uma sob `===== tests/T<n>/T<n>.csproj (...) =====`. O Claude corrige os dois no mesmo turno. Com `"testParallel": 1` o gate faz como antes deste item: para em `T1`, o primeiro vermelho (`agile gate RED: tests failed (tests/T1/T1.csproj): Probe_T1`), e `T2` só aparece no turno seguinte.

### 14.186 O plugin largou duas regras e um template: o `/agile:sync` pergunta, apaga a intacta e só avisa da editada

O plugin atualizou e deixou de ter `rules/core/old-rule.md`, `rules/core/old-two.md` e `templates/old.md`. O projeto ainda tem as três cópias, gravadas no `.claude/agile/sync.json`, e editou uma delas (`old-two.md`). O `plan` as lista em `removedCopies`, cada uma com o arquivo do plugin que sumiu:

```
removed  .claude/rules/agile/old-rule.md   (plugin file gone: rules/core/old-rule.md)
edited   .claude/rules/agile/old-two.md   (plugin file gone: rules/core/old-two.md)
removed  docs/agile/templates/old.md   (plugin file gone: templates/old.md)
```

O Claude mostra uma linha por cópia, em pt-BR: `.claude/rules/agile/old-rule.md — o plugin removeu esta regra/template; apagar? (ok ou não)`, o mesmo para o template, e para a editada `.claude/rules/agile/old-two.md — removido no plugin e editado no projeto: mover as escolhas para <nome>-project.md ou apagar à mão; nada é apagado`. Você diz ok só para a regra intacta e o Claude passa os caminhos ao `remove` (o terceiro, `docs/agile/profile.md`, entra aqui só para mostrar a recusa):

```
removed  .claude/rules/agile/old-rule.md
refused  .claude/rules/agile/old-two.md: edited, decide by hand
skip     docs/agile/profile.md: not a removed copy
```

A regra intacta foi apagada e saiu do `sync.json`; a editada ficou, porque o script nunca apaga o que o projeto mexeu. O Claude roda o `record`, e a linha que você não aprovou volta:

```
recorded agile@canary 0.169.0: 2 files -> .claude/agile/sync.json
edited   .claude/rules/agile/old-two.md   (plugin file gone: rules/core/old-two.md)
removed  docs/agile/templates/old.md   (plugin file gone: templates/old.md)
```

No sync seguinte o Claude pergunta de novo pelo template; a regra editada continua só como aviso até você mover as suas escolhas para um `old-two-project.md` e apagar o arquivo à mão. A apagada vai no commit do sync, e o retro-log diz quais cópias saíram, ficaram ou estão editadas.

### 14.187 Antes do `reviewer`, a sessão grande para e oferece uma sessão nova

No acme-shop, a F-3 está em `validating` e a sessão que a construiu já passou de 130k por resposta. Você digita `/agile:review F-3`. Logo antes de chamar o `reviewer`, o Claude roda `usage.js fresh --at review --id F-3`; o contexto está acima do limite de 120k, então a resposta termina com esta linha, depois de um commit `wip(F-3): before review` com o que ainda não estava commitado:

```
Contexto desta sessão: 130k por turno; uma sessão nova abre em ~65k. Para economizar uso: /clear e depois /agile:review F-3. Ou responda segue para continuar nesta sessão.
```

Não há cartão nem evento. Você digita `/clear` e depois `/agile:review F-3`, e a revisão recomeça do item no disco numa sessão de cerca de 90k; ou responde `segue`, e o `reviewer` é chamado nesta mesma sessão, como antes. Numa sessão abaixo de 120k o comando não imprime nada e a revisão segue sem parar.

O mesmo vale antes das passadas de desenho e do `frontend` no `/agile:build` (o comando da linha é `/agile:build F-3`) e antes do `ux-designer` no `/agile:screen`. Num `/agile:autopilot F-3`, a mesma sessão imprime:

```
Contexto desta sessão: 130k por turno; uma sessão nova abre em ~65k. Para economizar uso: /clear e depois /agile:autopilot F-3.
```

A execução termina ali com o evento `stop` `fresh context before review`; `/agile:autopilot F-3` numa sessão limpa continua do disco.

### 14.188 O `/agile:order` agrupado por benefício, na sessão do plugin

Você digita `/agile:order` no repositório do plugin, com seis issues abertos. O Claude lê as linhas de `order.js plugin --json` (título e primeira frase do resumo de cada uma), escreve numa pasta temporária um arquivo com os grupos, um motivo e dois modelos sugeridos por item, e roda `order.js grouped` com esse arquivo. O script refaz as linhas a partir dos issues, confere que nenhuma linha fica antes de algo de que depende e imprime as tabelas, que o Claude copia sem mudar:

```
### Relatórios mais úteis — cerca de 2 sessões (~3 h)

| # | Id | Title | Status | Refine | Build | Waits on | Reason |
|---|---|---|---|---|---|---|---|
| 1 | #10 | Export the report as PDF | idea | ~sonnet | ~sonnet | - | exportar é o pedido mais comum |
| 2 | #12 | Faster report page | refining | ~sonnet | ~sonnet | - | a página de relatório fica rápida |
| 3 | #11 | Email the report on a schedule | idea | ~sonnet | ~sonnet | #10 | envio automático, depende do PDF |

### Integrações — cerca de 1 sessão (~2 h)

| # | Id | Title | Status | Refine | Build | Waits on | Reason |
|---|---|---|---|---|---|---|---|
| 4 | #13 | Webhook for paid invoices | approved | - | opus | - | uma porta nova: o webhook |

### Outros — cerca de 1 sessão (~1 h)

| # | Id | Title | Status | Refine | Build | Waits on | Reason |
|---|---|---|---|---|---|---|---|
| 5 | #14 | Dark theme | idea | ~sonnet | ~sonnet | - | aparência, sem urgência |
| 6 | #15 | Rename the settings menu | idea | ~sonnet | ~sonnet | - | ajuste de texto |

agile order GROUPED: 6 items in 3 groups
```

O `~` marca o modelo como sugestão do Claude, lida do título e da frase do resumo; a coluna `Refine` vira `-` quando o item já está aprovado, e a coluna `Build` mostra o modelo decidido, sem `~`, quando o item tem a linha `- Model:` (aqui o #13, `opus`). Depois das tabelas o Claude diz, em português, por que a ordem é essa (o #11 espera o #10, então vêm nessa ordem no mesmo grupo), que a estimativa de cada grupo é um palpite e não uma medida, e o próximo passo: `/agile:refine #10`, com o modelo sugerido para ele, `sonnet`.

Se o Claude colocar o #11 num grupo desenhado antes do #10, o script recusa com `agile order REFUSED: #11 waits on #10 but is drawn before it`, sem tabela nenhuma, e o Claude corrige o arquivo uma vez e roda de novo. Um grupo com mais de cinco linhas que não seja o último grupo `Outros`, um id que falta, um id desconhecido ou repetido, e um modelo que não seja `opus` ou `sonnet` são recusados do mesmo jeito, com o nome de cada um.

### 14.189 O widget do `/agile:order` com os grupos por benefício

Você digita `/agile:order` no repositório do plugin, no app do Claude, com três issues abertos. O Claude escreve o arquivo com os grupos, o motivo e os modelos de cada item, como no exemplo 14.188, mas, como a sessão oferece `show_widget`, roda o `grouped` com `--json` e monta o widget a partir da saída, sem redigitar uma linha:

```
{
  "groups": [
    {
      "name": "Relatórios mais úteis",
      "estimate": "cerca de 2 sessões (~3 h)",
      "rest": false,
      "section": null,
      "mvp": null,
      "items": [
        "#10",
        "#11"
      ]
    },
    {
      "name": "Integrações",
      "estimate": "cerca de 1 sessão (~2 h)",
      "rest": false,
      "section": null,
      "mvp": null,
      "items": [
        "#13"
      ]
    }
  ],
  "items": [
    {
      "n": 1,
      "id": "#10",
      "title": "Export the report as PDF",
      "status": "idea",
      "model": null,
      "epic": "-",
      "waitsOn": [],
      "unlocks": [
        "#11"
      ],
      "cycle": false,
      "group": null,
      "mvp": null,
      "ticked": false,
      "why": "level 1; feature",
      "summary": "Export any report as a PDF file.",
      "reason": "exportar é o pedido mais comum",
      "refine": "~sonnet",
      "build": "~sonnet"
    },
    {
      "n": 2,
      "id": "#11",
      "title": "Email the report on a schedule",
      "status": "idea",
      "model": null,
      "epic": "-",
      "waitsOn": [
        "#10"
      ],
      "unlocks": [],
      "cycle": false,
      "group": null,
      "mvp": null,
      "ticked": false,
      "why": "level 2: waits on #10; feature",
      "summary": "Send the PDF by email every week.",
      "reason": "envio automático, depende do PDF",
      "refine": "~sonnet",
      "build": "~sonnet"
    },
    {
      "n": 3,
      "id": "#13",
      "title": "Webhook for paid invoices",
      "status": "approved",
      "model": "opus",
      "epic": "-",
      "waitsOn": [],
      "unlocks": [],
      "cycle": false,
      "group": null,
      "mvp": null,
      "ticked": false,
      "why": "level 1; feature",
      "summary": "Tell other systems when an invoice is paid.",
      "reason": "uma porta nova: o webhook",
      "refine": "-",
      "build": "opus"
    }
  ],
  "parked": [],
  "listed": 3,
  "delivered": 0
}
```

O widget desenha um cabeçalho por grupo em `groups` (`Relatórios mais úteis`, `cerca de 2 sessões (~3 h)` e `2 itens`; depois `Integrações`, `cerca de 1 sessão (~2 h)` e `1 itens`) e as linhas de `items`, nesta ordem, três linhas cada. A do #11: a caixa, o 2, o **#11** e o título; o status `idea`, `refinar` com o selo tracejado `~sonnet`, `construir` com o selo tracejado `~sonnet` e `waits on #10`; e, em letra pequena, `envio automático, depende do PDF`. A do #13 mostra `refinar` com o selo neutro `-` (já está aprovado) e `construir` com o selo cheio `opus`, o modelo decidido na linha `- Model:`. Sob a lista, a legenda `tracejado = sugestão do Claude · cheio = decidido no item`. Marcar a caixa do #10 salva a marca na hora (o `/agile:order ticks #10` do exemplo 14.107) e o contador mostra `1 de 3 marcados`. Como o plugin não tem cortes, `section` e `mvp` são `null` e não há cabeçalho de MVP; num projeto com cortes eles viriam antes dos grupos. Depois do widget o Claude escreve, no chat, por que a ordem é essa, a estimativa total (um palpite) e o próximo passo.

Numa sessão sem `show_widget`, ou se o widget não abrir, o Claude roda o `grouped` sem `--json`, copia as tabelas do exemplo 14.188 e escreve logo depois a linha `sem show_widget nesta sessão` (ou `o widget não abriu: <erro>`) e `diga "marque F-3" para marcar`; um arquivo que o script recusa não gera JSON nenhum, só a linha `agile order REFUSED: <motivo>`, igual com ou sem `--json`.

### 14.190 Um run do resolve com o guarda ligado: o autoteste, uma recusa, a sua autorização e a parada

Na acme-shop, o `/agile:resolve` leva a issue 41 (o item F-12) até a validação sem você acompanhar cada passo. Antes de qualquer trabalho ele liga o guarda do run, junta a branch do item e faz as duas escritas proibidas de propósito; cada entrada de hook recusa a sua e a anota no arquivo do run. Só então o `check` ativa o run. A saída abaixo é a do script num repositório de teste da acme-shop (os caminhos encurtados):

```text
agile run-guard BEGUN: run 41 on main, armed; next the two self-test probes, then check 41
agile run-guard ADDED: feature/F-12 to run 41
[agile run-guard] Write refused: the run is armed: every write waits for the self-test. Resolve run 41 is on (C:\dev\acme-shop\.git\agile\resolve\41.json).
[agile run-guard] Bash refused: git push with a force, a +refspec, --mirror, --all or --prune rewrites history, refused for the whole run. Resolve run 41 is on (C:\dev\acme-shop\.git\agile\resolve\41.json).
agile run-guard ACTIVE: run 41 on main; both hook entries refused their probe
```

Durante o run o guarda falha fechado também no texto que não consegue ler. Um push forçado escondido numa variável, ou escrito com um prefixo da opção que o git aceita, é recusado do mesmo jeito:

```text
[agile run-guard] Bash refused: the word $F of git push cannot be read. Resolve run 41 is on (C:\dev\acme-shop\.git\agile\resolve\41.json).
[agile run-guard] Bash refused: git push with a force, a +refspec, --mirror, --all or --prune rewrites history, refused for the whole run. Resolve run 41 is on (C:\dev\acme-shop\.git\agile\resolve\41.json).
```

Os dois comandos recusados, nessa ordem:

```powershell
$F = '--force'; git push $F origin feature/F-12
git push --force-w origin feature/F-12
```

```bash
F=--force; git push $F origin feature/F-12
git push --force-w origin feature/F-12
```

A mensagem de commit lida de um heredoc entre aspas, dentro de uma substituição que só tem o heredoc, continua passando.

Se uma das duas entradas não estivesse carregada (o plugin sem atualizar, um hook desligado), a escrita dela não seria recusada nem anotada, e o `check` pararia em `agile run-guard REFUSED: self-test shell not blocked`: o run não começa sem guarda. A sonda do shell leva `--dry-run`, então nem nesse caso a `main` seria sobrescrita.

No meio do trabalho, o modelo tenta o merge do item na `main` antes de você validar. O hook recusa, com código 2, e diz qual é o desvio:

```text
[agile run-guard] Bash refused: a merge into main waits for the owner's authorization (validado e autorizo o merge de 41). Resolve run 41 is on (C:\dev\acme-shop\.git\agile\resolve\41.json).
Bypass, only by the owner: node "<plugin>\scripts\run-guard.js" end 41 in a terminal, or restart Claude Code with AGILE_HOOKS=off.
```

Você valida o F-12 pelo roteiro e digita, sozinha no prompt:

> validado e autorizo o merge de F-12

O hook de prompt anota a autorização e não escreve nada no chat; o merge, o push na `main` e as escritas do ship no checkout principal passam a valer. Se a mesma frase chegasse de outro jeito (com mais texto, citada, dentro do relatório de um subagente ou de uma mensagem de outra sessão), nada seria anotado; e uma ferramenta que tentasse levar a frase, como uma sessão do Claude aberta por um comando ou um prompt de agente, seria recusada. O reescrever do histórico continua recusado até o fim do run.

Em outro run, algo parece errado e você quer parar tudo. Você digita `pare o resolve` (num run sem ninguém digitando, o mesmo é o `stop` do script num terminal). Daí em diante só leituras passam:

```text
[agile run-guard] Bash refused: the run is stopped. Resolve run 41 is on (C:\dev\acme-shop\.git\agile\resolve\41.json).
agile run-guard STOPPED: run 41; every write and shell call is refused until run-guard.js end 41
```

Quando terminar de olhar, você encerra o run num terminal, na pasta da acme-shop:

```powershell
node "<plugin>/scripts/run-guard.js" end 41
```

```bash
node "<plugin>/scripts/run-guard.js" end 41
```

```text
agile run-guard ENDED: run 41; every hook is back to its normal behavior
```

Sem o arquivo do run, todos os hooks voltam ao normal na hora: as ferramentas MCP, as escritas no checkout principal e o resto do trabalho do dia a dia funcionam como antes.

### 14.191 O plano de um ambiente `aca` numa região que a assinatura não pode usar

Na acme-shop, a linha do `staging` em `## Cloud accounts` diz Region `eastus`, e a assinatura do cliente é nova. Antes de qualquer tag ou comando, o plano de `/agile:publish staging` lê o que a Region aguenta (duas consultas de leitura ao Azure, nada é escrito). Aqui o PostgreSQL Flexible está bloqueado nessa região, e o plano para, com o motivo que o próprio Azure dá e as duas saídas:

```text
staging: Azure cannot host this environment in eastus: PostgreSQL Flexible (Provisioning is restricted in this region. Please choose a different region. For exceptions to this rule please open a support request with Issue type of 'Service and subscription limits'.). Change Region in docs/infra.md, or ask Azure for the quota (a support request of issue type "Service and subscription limits") — nothing ran
```

Você troca a Region para `eastus2` na linha do `staging` e pede o plano de novo. Agora a região aguenta, e o plano mostra a linha logo depois do `Budget:` e do `Spend:`:

```text
Capacity: ok in eastus2 (Container Apps environments 0 of 2; PostgreSQL Flexible Standard_B1ms offered)
```

Se o login não puder ler as cotas (ou o Azure responder algo que o plugin não entende), o plano não para por isso: a linha diz `Capacity: not read (<motivo>)` e o deploy segue. Num deploy seguinte, quando o resource group já tem o ambiente do Container Apps e o servidor PostgreSQL, a linha diz `Capacity: ok in eastus2 (already in rg-acme)` e nenhuma cota é consultada. O deploy local e o pipeline fazem a mesma leitura antes do comando, e uma falta provada os para do mesmo jeito.

### 14.192 A pausa grava o bloco `Resume here` e o início da sessão seguinte o lê

O F-13 está em `building` e você precisa parar. Você digita `/agile:pause` (dizer que vai parar, com quaisquer palavras, dá no mesmo). O Claude imprime a linha do item, grava o bloco `## Resume here` no arquivo do item, na worktree (o script lê o status e a data do próprio arquivo), faz o commit `wip` e escreve a nota curta do chat, que ainda diz o que fechar:

```text
> /agile:pause
Claude: F-13 — Exam board registration [building]
        agile resume SET: docs/features/F-13-exam-board.md (building)
        wip(F-13): endpoint tests half done (feature/F-13, 4e1c2aa)
        Paramos nos testes do endpoint. Próximo: escrever o teste do 409 para sigla duplicada.
        Pendente com você: nada. Rodando: o app host; feche antes de voltar.
```

O arquivo do item agora abre assim, logo depois do título e da linha `Technical terms:`, antes de `## Summary`:

```text
## Resume here
- Status: building (2026-10-07)
- Stopped at: the endpoint tests, wip commit 4e1c2aa
- Next action: write the 409 test for a duplicate acronym
- Waiting on: nothing
```

No dia seguinte você abre uma sessão nova no mesmo repositório e a conversa foi compactada. O início da sessão mostra o ponto em que paramos e a próxima ação, lidos do bloco:

```text
After a compact, resume here:
Resume F-13 Exam board registration — building [feature/F-13] D:\wt\examprep\f-13-exam-board
  Paused: the endpoint tests, wip commit 4e1c2aa
  Next: write the 409 test for a duplicate acronym
  Criteria with no test: no section
```

O `/agile:status` mostra a mesma coisa em `Next step:`, lida do bloco. O mesmo bloco serve a quem abre o arquivo: as quatro linhas dizem em que estado o item está e o que fazer, sem ler o resto. Um item feito antes do bloco existir não é erro; a verificação diz isso e o primeiro passo que mexe no item grava o bloco:

```text
legacy: no block
```

### 14.193 Uma issue sua até a validação com o `/agile:resolve`: o seu texto, uma parada grave e a retomada

Na acme-shop você abriu a issue 52, "Exportar pedidos em CSV", com o rótulo `feature`, e comentou o separador. O Bruno, que também tem acesso ao repositório, comentou pedindo outra coisa. Você digita:

> /agile:resolve acme/acme-shop#52

O Claude lê a issue pelo script. A saída abaixo é a do script num repositório de teste da acme-shop (os caminhos encurtados):

```text
issue: https://github.com/acme/acme-shop/issues/52
owner: ana
kind: feature (label)
capture: board #52 (the issue is the item's board issue; no second issue)
snapshot: C:\dev\acme-shop\.git\agile\issues\acme-acme-shop-52\text.md sha256 3a7ee4facf1b65b235c4139c961a60ce9c6650d0eb2aefada0c83c3ed4c1deab (new)
not followed: bruno https://github.com/acme/acme-shop/issues/52#issuecomment-901 (another author)
agile resolve-issue READ: acme/acme-shop#52 by ana, kind feature, snapshot 3a7ee4facf1b
agile resolve-issue NONE: no item has board #52: a new run captures it
```

O snapshot guarda o corpo e o seu comentário; o do Bruno fica só como autor e link, e o texto dele não entra em nada que o Claude lê. Nenhum item parecido existe: o Claude registra o F-14 com a própria issue 52 como issue do board e refina com todas as respostas assumidas. Assim que a worktree do F-14 existe, o guarda do run começa, faz o autoteste e recebe a branch do item:

```text
agile run-guard BEGUN: run 52 on main, armed; next the two self-test probes, then check 52
[agile run-guard] Write refused: the run is armed: every write waits for the self-test. Resolve run 52 is on (C:\dev\acme-shop\.git\agile\resolve\52.json).
[agile run-guard] Bash refused: git push with a force, a +refspec, --mirror, --all or --prune rewrites history, refused for the whole run. Resolve run 52 is on (C:\dev\acme-shop\.git\agile\resolve\52.json).
agile run-guard ACTIVE: run 52 on main; both hook entries refused their probe
agile run-guard ADDED: feature/F-14 to run 52
agile resolve-check conflict: OK: HEAD merges into main and origin/main with no conflict
```

O build segue sem perguntas. Antes de ir para `validating`, a conferência do contrato acha uma operação do OpenAPI que o item tirou, e isso é uma parada grave:

```text
agile resolve-check contract: STOP: another module's contract changes: GET /api/v1/orders/{id} removed from docs/api/openapi.json
agile resolve-issue POSTED: stop comment on acme/acme-shop#52 (https://github.com/acme/acme-shop/issues/52#issuecomment-955)
agile run-guard STOPPED: run 52; every write and shell call is refused until run-guard.js end 52
```

O comentário que aparece na issue:

```text
**/agile:resolve — Parado numa parada grave** (F-14, branch `feature/F-14`)

Parou em: o contrato de outro módulo.
Motivo: GET /api/v1/orders/{id} removed from docs/api/openapi.json

Para retomar: remova a causa, rode `run-guard.js end 52` num terminal na pasta do projeto e digite `/agile:resolve acme/acme-shop#52`.

**Assumido pelo resolve**
- formato da data: ISO 8601 (recomendado)

**Comentários não seguidos** (outro autor: o texto não foi lido)
- bruno: https://github.com/acme/acme-shop/issues/52#issuecomment-901

**Anexos não lidos**
- nenhum
```

A sessão não consegue encerrar o run sozinha antes da sua autorização. Você devolve a operação ao OpenAPI na branch do item e encerra o run num terminal, na pasta da acme-shop:

```powershell
node "<plugin>/scripts/run-guard.js" end 52
```

```bash
node "<plugin>/scripts/run-guard.js" end 52
```

```text
agile run-guard ENDED: run 52; every hook is back to its normal behavior
```

Você digita o mesmo comando de novo. O snapshot é reaproveitado, o item é achado pela issue do board, e um run novo começa com um autoteste novo, a partir da linha `Autopilot:`:

```text
agile resolve-issue FOUND: F-14 building at C:\dev\acme-shop-f-14\docs\features\F-14-export-orders-csv.md; Autopilot: approved
agile run-guard ACTIVE: run 52 on main; both hook entries refused their probe
agile resolve-check migration: OK: no migration deletes data
agile resolve-check contract: OK: no contract removed or changed (no Contracts: line in the profile)
agile resolve-check package: OK: no package added
agile resolve-issue POSTED: validation comment on acme/acme-shop#52 (https://github.com/acme/acme-shop/issues/52#issuecomment-955)
```

A parada 2 mostra o roteiro de validação com tudo o que foi assumido (o formato da data e o nome do arquivo), e o comentário de validação diz o mesmo na issue. Depois de validar, você digita sozinha no prompt:

> validado e autorizo o merge de F-14

O ship roda, um comentário de entrega vai para a issue e o run termina. Só "validado" deixaria o F-14 em `validating` com o run ligado.


### 14.194 Uma extensão do VS Code pelo bootstrap: a rodada da extensão, o esqueleto e o `.vsix`

O brief da Ana descreve uma extensão que lista, no VS Code, os pull requests à espera de revisão. Rodada 1:

```
Claude: 1. Tipo de app → Recomendação: VS Code extension. O brief fala de um comando
       dentro do VS Code, não de telas do app nem de uma API.
    2. Perfil de arquitetura → Recomendação: vscode-extension (o único para uma extensão do VS Code).
> ok
```

No lugar das rodadas 2 a 8 vem uma só:

```
Claude: X1. Nome → Recomendação: review-helper (kebab-case; vira o name do package.json
       e o prefixo dos comandos, review-helper.hello).
    X2. Nome de exibição e descrição → Recomendação: "Review Helper" e "Shows the pull
       requests waiting for a review."
    X3. Publicador → Recomendação: ana-souza.  X4. Licença → Recomendação: MIT.
    X5. Repositório → Recomendação: ana-souza/review-helper (o `origin` já o dá).
    27. Board → Recomendação: GitHub Issues + Projects (o código está no GitHub).
    28c. Raiz das worktrees → Recomendação: D:\wt\review-helper.
    28d. Merge → Recomendação: direct.  34. Modelos → as linhas de sempre, sem as de tela.
    20a. Idiomas do manual → Recomendação: pt-BR.
    Já está claro: uma extensão não tem banco, ambientes, idiomas de UI nem telas do
    próprio agile; branch principal main, fluxo trunk.
> ok
```

Depois da pergunta de fechamento, do resumo e do "confirmo", o Claude descreve o esqueleto e, com o seu sim, copia o `templates/vscode-extension/` com os tokens trocados (`package.json`, `tsconfig.json`, `.vscodeignore`, `.gitignore`, `README.md`, `LICENSE`, `.vscode/launch.json` e `tasks.json`, `src/extension.ts`, `src/core/greeter.ts`, os dois testes e o `.claude/agile/build.json`), confere no registro do npm as versões dos quatro pacotes e roda o `npm install`, que escreve o `package-lock.json`. Depois roda `npm ci`, `npm test` e `npm run package` e mostra o que o npm respondeu:

```
> review-helper@0.1.0 test
> tsc -p . && node --test "out/test/**/*.test.js"

✔ src/core imports nothing from vscode (2.4235ms)
✔ hello greets by name (0.6187ms)
ℹ tests 2
ℹ pass 2
ℹ fail 0
 DONE  Packaged: C:\dev\review-helper\review-helper-0.1.0.vsix (7 files, 3.68 KB)
```

O `CLAUDE.md` tem `Profile: vscode-extension` e nenhuma linha de idiomas de UI; não há solução, `templates/dotnet/` nem baseline de avisos. O `.vsix` leva `out/extension.js` e `out/core/greeter.js` e deixa `src/` e `out/test/` de fora. Para ver a extensão rodando, a Ana abre a pasta no VS Code e aperta F5 com "Run Extension": o Extension Development Host abre com o comando "Review Helper: Say hello" na paleta. Ao fim, o Claude lembra que o `package.json` é o manifesto, escrito à mão, que a versão mora nele e que o `/agile:publish` recusa o perfil até o item do Marketplace (#264) existir.

### 14.195 As seções offline do perfil: o `/agile:sync` traz os arquivos de um `mobile` e de um `web-api`

O Bolso é um projeto `mobile` sincronizado na 0.176.0, com o `docs/agile/profile.md` nunca editado e o `runtime-themes-maui.md` já copiado. Neste `/agile:sync` o `sync.js plan` lista o perfil como `update`, os dois arquivos offline como `new` e o de tema como `same`:

```
update   docs/agile/profile.md
new      docs/agile/profile-sections/offline-writes-api.md
new      docs/agile/profile-sections/offline-first-mobile.md
same     docs/agile/profile-sections/runtime-themes-maui.md
```

Você aprova, e o `apply` responde:

```
copied   docs/agile/profile.md
copied   docs/agile/profile-sections/offline-writes-api.md
copied   docs/agile/profile-sections/offline-first-mobile.md
```

O `profile.md` do Bolso passou de 10.163 para 8.034 palavras: `## Offline-first` e `## Offline writes (the API)` viraram uma linha cada, o ponteiro com o `Read when`, e o texto delas está nos arquivos novos (1.759 e 447 palavras). Um item que acrescenta um campo numa tela existente não lê nenhum dos dois; um item que muda o merge de uma atualização pendente lê o `offline-first-mobile.md`.

No API do Bolso, um projeto `web-api` na mesma versão, o `sync.js plan` lista `update` para o `docs/agile/profile.md` e `new` para os dois arquivos abaixo, o segundo com 314 palavras, as sete linhas do portão:

```
new      docs/agile/profile-sections/offline-writes-api.md
new      docs/agile/profile-sections/forced-update-gate.md
```

O perfil passou de 3.583 para 2.891 palavras e o `## Forced update` continua com a introdução dele, agora seguida de um `### Forced update (the gate)` que só tem o ponteiro. Um item que muda o portão de versão (`X-App-Version`, `426`) lê esse arquivo; um que só acrescenta um endpoint, não.

Se você tivesse acrescentado uma linha sua ao `profile.md`, a tabela o listaria como `edited`, o `apply` recusaria e o Claude faria o merge de três vias: a sua linha fica, cada seção vira o ponteiro, e os arquivos chegam como `new` do mesmo jeito.

### 14.196 O ship mostra uma lista de 8 itens e uma tabela de 2 colunas como NOTE

Você escreveu as regras do F-12 numa lista só e juntou duas colunas numa tabela. Antes de fechar o ship, o Claude roda a contagem no arquivo do item:

```powershell
node "<plugin>scriptsdensity.js" check docs/features/F-12-exam-board.md
```

```bash
node "<plugin>/scripts/density.js" check docs/features/F-12-exam-board.md
```

O arquivo tem estas seções (o resto dele cabe nos limites):

```markdown
# Exam board registration

## Rules
- BR1: an exam board has a name
- BR2: the acronym is unique
- BR3: the acronym has at most 8 letters
- BR4: a board can be archived
- BR5: an archived board has no new exam
- BR6: the list is sorted by name
- BR7: the name is trimmed
- BR8: a deleted board stays in reports

## Fields
| Field | Rule |
|---|---|
| Name | required |
```

A saída traz um achado por linha:

```text
list Rules 8 > 7
table Fields 2 columns, a list in disguise
```

O relatório do ship mostra as duas linhas como `NOTE docs/features/F-12-exam-board.md: list Rules 8 > 7` e `NOTE docs/features/F-12-exam-board.md: table Fields 2 columns, a list in disguise`. O ship segue: nada é parado e nada é escrito no arquivo.

Para tirar as notas, divida as regras em duas seções e troque a tabela por uma lista. Num template do plugin, ou no manual acima do número guardado em `scripts/density-baseline.json`, o mesmo achado seria uma falha do `check-all`.

### 14.197 Um run do `/agile:resolve` passa de 75% do teto de custo e depois entra no estado de limite

Na acme-shop, o run da issue 52 (o F-14) segue sem você. A cada chamada de ferramenta o guarda do run soma o uso de todas as sessões do run. Quando a soma passa de 75% do teto de custo, você vê um aviso, e o run segue:

```text
Resolve #52: 75% do teto de custo (7.5M de 10M ponderado, estimativa) · 186 de 400 turnos.
```

Você quer ver onde a contagem está e roda o `status` do guarda num terminal, na pasta da acme-shop:

```powershell
node "<plugin>/scripts/run-guard.js" status
```

```bash
node "<plugin>/scripts/run-guard.js" status
```

```text
agile run-guard STATUS: run 52 on main, active; branches: feature/F-14 (C:\dev\acme-shop\.git\agile\resolve\52.json); count 7.5M of 10M weighted, 186 of 400 turns
```

O build continua e a soma chega ao teto. A próxima edição é recusada, e você vê:

```text
Resolve #52: limite atingido (teto de custo: 10.0M de 10M ponderado); a run só posta o comentário de parada. Encerre com run-guard.js end 52 no terminal.
```

A sessão recebe a recusa com o que ainda passa (os caminhos encurtados):

```text
[agile run-guard] Edit refused: the run reached its cost ceiling, its limit state (count 10.0M of 10M weighted, 248 of 400 turns, limit reached (ceiling)): only Read, Glob, Grep, mcp__ccd_turn__*, a Write in the scratchpad, resolve-issue.js comment 52 --kind stop --file <f>, events.js add <id> stop "<text>" and run-guard.js stop 52 pass. Resolve #52: limite atingido (teto de custo: 10.0M de 10M ponderado); a run só posta o comentário de parada. Encerre com run-guard.js end 52 no terminal. Resolve run 52 is on (C:\dev\acme-shop\.git\agile\resolve\52.json).
Bypass, only by the owner: node "<plugin>\scripts\run-guard.js" end 52 in a terminal, or restart Claude Code with AGILE_HOOKS=off.
```

Ela posta o comentário de parada na issue (o motivo é o teto de custo), grava o evento `stop` e para o run. O `end` é seu: você decide se a issue vale outro run, que começa com a contagem do zero, ou se o F-14 segue no fluxo normal com o `/agile:autopilot`.

### 14.198 O `/agile:resolve --driver` leva a issue até a validação pelo terminal, retoma depois de uma sessão morta e recebe a sua autorização

Na acme-shop, a issue 52 já virou o F-12 (Exportar pedidos), aprovado, com o run do guarda ligado. Você quer que o resto rode sem uma sessão só carregando tudo. Numa sessão do projeto você digita:

```text
> /agile:resolve acme/acme-shop#52 --driver
```

O Claude confere a issue sem escrever nada e mostra o comando do driver, com a pasta do plugin que está rodando:

```powershell
node "C:\Users\ana\.claude\plugins\cache\canary\agile\<versão>\scripts\resolve.js" 'acme/acme-shop#52'
```

```bash
node "C:/Users/ana/.claude/plugins/cache/canary/agile/<versão>/scripts/resolve.js" 'acme/acme-shop#52'
```

Você roda o comando num terminal, na pasta da acme-shop. Cada linha é uma sessão nova do Claude Code, sem janela; a primeira termina na linha de contexto novo antes do `frontend`, a segunda passa da sua hora de limite com o `OrdersExport.cs` pela metade, e a terceira chega à parada 2 (a saída do script num repositório de teste da acme-shop, os caminhos encurtados):

```text
resolve acme/acme-shop#52 session 1: approved -> approved, ok, run 1.4M of 10M, 61 of 400 turns, US$ 1.9412
resolve acme/acme-shop#52 session 2: approved -> approved, timeout, run 2.6M of 10M, 118 of 400 turns, US$ 0.0000
resolve acme/acme-shop#52 session 3: approved -> stop 2, ok, run 3.9M of 10M, 172 of 400 turns, US$ 2.4407
agile resolve VALIDATION: F-12 está em validação; o roteiro está em C:\dev\acme-shop-f-12\docs\features\F-12-export-orders.md (## Validation script), com os passos não conferidos sem tela. Valide, rode o mesmo comando e digite a frase de autorização
```

A sessão 2 não se perdeu: antes da 3, o driver passou o `secret-guard.js` no que estava na worktree e gravou o commit `wip(F-12): interrupted at approved` na branch `feature/F-12`, e a sessão 3 retomou dali. Se você tivesse fechado o terminal no meio, a sessão morreria junto, e o mesmo comando faria esse mesmo commit antes de seguir.

Você valida o app pelo roteiro, roda o mesmo comando no terminal e digita a frase quando ele pede:

```text
Digite a frase de autorização de F-12 (validado e autorizo o merge de F-12):
> validado e autorizo o merge de F-12
resolve acme/acme-shop#52 session 4: stop 2 -> stop 2, ok, run 3.9M of 10M, 172 of 400 turns, US$ 0.0061
resolve acme/acme-shop#52 session 5: stop 2 -> shipping, ok, run not begun, US$ 0.8823
agile resolve SHIPPED: F-12 entregue; a worktree C:\dev\acme-shop-f-12 ficou: feche o que a prende e remova com worktree.js remove
```

A sessão 4 não tem nenhuma ferramenta: ela só recebe a frase, que o hook do guarda registra no arquivo do run. O driver lê o registro de volta e só então roda a sessão 5, a do ship, que faz o merge, posta o comentário de entrega e encerra o run (daí o `run not begun` na linha dela).

A worktree do F-12 ficou porque um programa ainda a usava; o driver a nomeia em vez de tentar de novo. Um `validado` sozinho, ou a frase de outro item, não registra nada e encerra com `REFUSED`.

### 14.199 Um épico inteiro pelo `/agile:resolve`: três features num `epic/<slug>`, uma parada de conflito e a autorização pelo slug

Na acme-shop, a issue 60, "Frete por região", tem o rótulo `epic`. Você digita:

```text
> /agile:resolve #60
```

Antes do guarda, o Claude cria `epic/frete-regiao` com a worktree dela e commita ali o arquivo do épico em `draft`. A quebra sai sem perguntas (saída do script num repositório de teste da acme-shop):

```text
agile resolve-check epic: OK: 3 features, at most 6
```

São F-20 (tabela de regiões), F-21 (frete por região, depende da F-20) e F-22 (frete no carrinho). Cada uma entra em `epic/frete-regiao` sem parar e sem subir versão. A F-21 conta a F-20 como entregue na branch do épico:

```text
F-20 delivered on epic/frete-regiao
agile deps FREE
```

A F-22 mexe na mesma linha de `src/Freight.cs` que a F-21. Antes do merge:

```text
agile resolve-check conflict: STOP: a merge conflict with epic/frete-regiao: src/Freight.cs
```

A execução comenta na issue e para o guarda. Você resolve o conflito na branch da F-22 e encerra o run num terminal:

```text
agile run-guard ENDED: run 60; every hook is back to its normal behavior
```

O mesmo `/agile:resolve #60` retoma da linha `Autopilot:` com um autoteste novo. Com as três dentro, o corpo da issue 60 recebe um roteiro só: uma seção por feature, os passos de ponta a ponta e o que foi assumido. Você valida e digita, por engano, o id de uma feature:

```text
> validado e autorizo o merge de F-21
```

Nada é registrado. Com o slug, o `status` do guarda passa a dizer `authorized` (o caminho encurtado):

```text
> validado e autorizo o merge de frete-regiao
agile run-guard STATUS: run 60 on main, active, authorized; branches: epic/frete-regiao, feature/F-22 (...\.git\agile\resolve\60.json); count 0.0M of 10M weighted, 0 of 400 turns
```

Daí o ship do épico: uma subida de versão, o merge em `main`, as quatro issues fechadas em Done, a retro com você e a limpeza das branches.

### 14.200 O `/agile:resolve --driver` num repositório legado: a etapa do lens leva a issue até a parada 2, e uma decisão que precisa de você para o run

A acme-shop é um sistema legado que o lens mapeia. O `CLAUDE.md` dela tem a linha `- Lens workspace: ../acme-shop-lens`, e o lens está instalado (`claude plugin install`). A issue 52 diz: "Um pedido de exatamente 100 não recebe o desconto de 10%." Numa sessão do projeto você digita `/agile:resolve acme/acme-shop#52 --driver` e roda num terminal, na pasta da acme-shop, o comando que ele mostra. Antes do guarda do run, o driver roda o lens:

```text
resolve acme/acme-shop#52 session 1: lens adopt -> lens adopt: DONE, ok, run not begun, US$ 0.0500
resolve acme/acme-shop#52 session 2: lens map -> lens map: OPEN 1, ok, run not begun, US$ 0.0900
resolve acme/acme-shop#52 session 3: idea -> idea, ok, run not begun, US$ 0.0500
resolve acme/acme-shop#52 session 4: lens decide -> Arquivo: `necropsies/resolve-52/decision.json`, ok, run not begun, US$ 0.2300
resolve acme/acme-shop#52 session 5: idea -> stop 2, ok, run not begun, US$ 1.1200
agile resolve VALIDATION: B-12 está em validação; o roteiro está em C:\dev\acme-shop-f-12\docs\bugs\B-12-discount-at-100.md (## Validation script), com os passos não conferidos sem tela. Valide, rode o mesmo comando e digite a frase de autorização
```

O workspace ainda não existia, então o `lens adopt` o criou ao lado do projeto. O `lens map` terminou em `OPEN 1`: o mapa está completo, e o item aberto vai para a parada 2.

A sessão 3 é do agile: ela capturou o B-12, escolheu no inventário a semente que o seu texto alcança (o endpoint `OrdersController.Price`), gravou-a com `resolve-lens.js seeds`, registrou a escolha como `assumed by resolve` e terminou em `lens decide`. O lens então escreveu o `decision.json`. O refinamento usou a decisão pelo `resolve-lens.js feed`:

```text
approach: - 2026-10-25: approach — In Discount.Apply change total > 100m to total >= 100m. — assumed by resolve — lens decision resolve-52: The comment at line 7 says 100 or more. (src/OrdersController.cs:7, src/OrdersController.cs:8)
exists: src/OrdersController.cs (src/OrdersController.cs:8, deterministic)
criterion: the contract OrdersController stays as it is (src/OrdersController.cs:16)
risk: - 2026-10-25: risk — The endpoint result changes from 100 to 90 (src/OrdersController.cs:8) — assumed by resolve
agile resolve-lens FEED: decided resolve-52, 1 files, 1 contracts, 0 tests, 1 risks
```

O comentário de validação na issue traz a parte do lens:

```text
**Etapa do lens**
- Workspace: `C:\dev\acme-shop-lens`
- Sementes: `http-endpoint:src/App.csproj:OrdersController.Price@src/OrdersController.cs:16`
- Abordagem: In Discount.Apply change total > 100m to total >= 100m.
- Descartadas:
  - Change the comment to say more than 100 (It would document the bug.)
- Riscos:
  - The endpoint result changes from 100 to 90 (src/OrdersController.cs:8)
- Itens abertos do map:
  - permission-checks-not-declared
- Sessões do lens: lens adopt US$ 0.0500; lens map US$ 0.0900; lens decide US$ 0.2300
```

Na mesma situação, se o lens não decide sozinho, o run para antes de começar:

```text
resolve acme/acme-shop#52 session 4: lens decide -> NEEDS-OWNER resolve-52: 1 questions, ok, run not begun, US$ 0.2300
agile resolve STOPPED: lens needs the owner: 1 question; answer in lens: /lens:decide --name resolve-52 --request .git/agile/issues/acme-acme-shop-52/text.md --seed http-endpoint:src/App.csproj:OrdersController.Price@src/OrdersController.cs:16 C:\dev\acme-shop-lens; o driver postou o comentário de parada; remova a causa e rode o mesmo comando
```

O comentário de parada traz a pergunta do lens com a recomendação e o motivo, e a linha que você roda:

```text
- Perguntas do lens (responda no lens, não na issue):
  - Should an order of exactly 100 get the discount? Recomendação: Yes, as the comment at line 7 says. Motivo: The comment and the code disagree.
- Para responder: numa sessão interativa do Claude Code na pasta do projeto, rode `/lens:decide --name resolve-52 --request .git/agile/issues/acme-acme-shop-52/text.md --seed http-endpoint:src/App.csproj:OrdersController.Price@src/OrdersController.cs:16 C:\dev\acme-shop-lens`; depois rode de novo o comando do driver.
```

Você responde no lens, que reescreve o `decision.json` como `decided`. Quando você roda o comando do driver de novo, ele lê o arquivo, não roda o lens outra vez e segue para o refinamento. Uma resposta escrita na issue não chega ao run: é texto depois do snapshot. Essa parada acontece antes do run, então não há `run-guard.js end` para rodar.

### 14.201 Um item da acme-shop entregue com `Merge: pr-auto`: o merge sozinho, um check vermelho e o auto-merge depois de 30 minutos

A acme-shop está no `github.com`, roda o CI em todo pull request e o `CLAUDE.md` diz `- Merge: pr-auto`. Você valida a F-9 e digita:

```text
> /agile:ship F-9
```

O ship faz as mesmas verificações do `pr`, abre o PR 41 e espera os checks dele. As saídas abaixo são do script num repositório de teste com um `gh` falso:

```text
$ node scripts/pr-wait.js watch 41
PASSED
```

O ship faz o merge com um merge commit, lê o PR de volta como `MERGED` e fecha o item na mesma execução: worktree, branches, board e retro. Você não abriu o GitHub.

Na F-10 um teste falha no CI:

```text
$ node scripts/pr-wait.js watch 42
test: https://github.com/acme/shop/actions/runs/88/job/test
FAILED: test
```

O ship para com `PR #42 open: test failed` e o link. Nada foi mesclado e o auto-merge não foi ligado. Você pede a correção na branch e digita `/agile:ship F-10` de novo: o gate roda, a branch sobe e a espera recomeça.

Na F-11 o CI demora mais que o limite. Depois de 30 minutos:

```text
$ node scripts/pr-wait.js watch 44
TIMEOUT: test
$ node scripts/pr-wait.js auto 44
ARMED
```

O repositório permite auto-merge e a `main` exige o check `test`, então o ship para com `PR #44 open, auto-merge on: GitHub merges it when the required checks pass; /agile:ship F-11 closes the item`. O início da sessão seguinte mostra `Waiting on PR: F-11 #44 open — auto-merge on`. Sem um check exigido na `main`, o auto-merge não seria ligado:

```text
$ node scripts/pr-wait.js auto 44
NOT ARMED: no required check on the main branch, and --auto would merge at once with checks pending
```

e o ship pararia com `PR #44 open: checks still running after 30 minutes (test); /agile:ship F-11 waits again`. Um PR sem nenhum check, depois de 2 minutos, dá `NO CHECKS`, e o ship pede `Merge: pr` ou `direct`.

### 14.202 O `/agile:resolve --driver` leva um épico até o ship: uma sessão por feature, uma sessão morta no meio e a autorização pelo slug

Na acme-shop, a issue 60 tem o rótulo `epic`, e o run do guarda está aberto. Você roda no terminal o comando do `--driver` (como em 14.198). A primeira sessão faz a quebra em F-1, F-2 e F-3; cada outra leva uma feature até o merge em `epic/shop`, e todas terminam com `agile resolve headless: fresh epic`. A terceira passa do tempo com a F-2 pela metade (a saída do script no repositório de teste, a issue renomeada):

```text
resolve acme/acme-shop#60 session 1: agreed -> F-1 approved, ok, run 0.0M of 10M, 0 of 400 turns, US$ 0.0123
resolve acme/acme-shop#60 session 2: F-1 approved -> F-2 approved, ok, run 0.0M of 10M, 0 of 400 turns, US$ 0.0123
resolve acme/acme-shop#60 session 3: F-2 approved -> F-2 approved, timeout, run 0.0M of 10M, 0 of 400 turns, US$ 0.0000
resolve acme/acme-shop#60 session 4: F-2 approved -> F-3 approved, ok, run 0.0M of 10M, 0 of 400 turns, US$ 0.0123
resolve acme/acme-shop#60 session 5: F-3 approved -> shop stop 2, ok, run 0.0M of 10M, 0 of 400 turns, US$ 0.0123
agile resolve VALIDATION: o épico shop está em validação; o roteiro está no corpo da issue acme/acme-shop#60 (## Validation script), com os passos não conferidos sem tela. Valide, rode o mesmo comando e digite a frase de autorização; para um defeito, digite /agile:resolve acme/acme-shop#60 numa sessão do Claude Code
```

O passo de cada linha é a linha `Autopilot:` do épico. Antes da sessão 4, o driver gravou `wip(F-2): interrupted at F-2 approved` na branch `feature/F-2`, e a sessão 4 retomou dali. O `VALIDATION` só saiu porque as três features estão `done` nos próprios arquivos em `epic/shop`; com a F-2 ainda `approved`, o driver rodaria mais uma sessão em vez de pedir a frase.

Você valida pelo roteiro no corpo da issue 60, roda o mesmo comando e digita a frase do épico:

```text
Digite a frase de autorização do épico shop (validado e autorizo o merge de shop, ou validado e autorizo o merge de 60):
> validado e autorizo o merge de shop
resolve acme/acme-shop#60 session 6: shop stop 2 -> shop stop 2, ok, run 0.0M of 10M, 0 of 400 turns, US$ 0.0123
resolve acme/acme-shop#60 session 7: shop stop 2 -> shop stop 2, ok, run 0.0M of 10M, 0 of 400 turns, US$ 0.0123
agile resolve SHIPPED: epic shop entregue; o driver postou o comentário de entrega e encerrou o run; a worktree C:\dev\acme-shop-e-shop ficou: feche o que a prende e remova com worktree.js remove; a retro do épico é sua: digite /agile:retro epic shop numa sessão do Claude Code
```

A sessão 6 só recebe a frase, que o hook do guarda registra; a 7 faz o ship do épico. Ela não roda a retro, que precisa de você: as lições ficam em `## Retro notes`, e o `SHIPPED` pede o `/agile:retro epic shop`. A frase com `F-2` terminaria em `REFUSED`, sem nada registrado: uma feature nunca abre a `main` para o épico inteiro.

### 14.203 Uma extensão do VS Code publicada pelo `/agile:publish`: sem o token, o envio que falha depois da tag e o reenvio seguro

O acme-tools é uma extensão do VS Code (exemplo 14.194), na versão 0.1.0. Você digita `/agile:publish` sem o token: o plano passa e avisa que o Marketplace fica de fora (a saída do script no repositório de teste, sem `project`, `remote` e `notes`):

```text
{
  "ok": true,
  "mode": "plan",
  "profile": "vscode-extension",
  "projects": [],
  "version": "0.1.0",
  "extension": {
    "name": "acme-tools",
    "publisher": "acme-co",
    "vsix": "acme-tools-0.1.0.vsix",
    "marketplace": "vscode"
  },
  "leftOut": [
    "acme-tools: not sent to the VS Code Marketplace, VSCE_PAT is not set"
  ],
  "branch": "main",
  "tag": "v0.1.0",
  "notesFile": "docs/releases/v0.1.0.md",
  "previousTag": null,
  "mergeCount": 0,
  "warnings": [
    "acme-tools: not sent to the VS Code Marketplace, VSCE_PAT is not set"
  ]
}
```

Claude: `VSCE_PAT` não está definido. O release (tag, notas e GitHub Release com o `.vsix`) sai e o envio ao VS Code Marketplace fica de fora. Para enviar, defina a variável no seu terminal e digite o comando de novo; eu não leio o valor.

Você define `VSCE_PAT` e digita o comando outra vez. O plano traz `planLine` e nenhum `leftOut`; o pacote lê o `.vsix` de volta:

```text
{
  "ok": true,
  "mode": "package",
  "profile": "vscode-extension",
  "projects": [],
  "version": "0.1.0",
  "extension": {
    "name": "acme-tools",
    "publisher": "acme-co",
    "vsix": "acme-tools-0.1.0.vsix",
    "marketplace": "vscode"
  },
  "planLine": "acme-tools 0.1.0 (vscode marketplace)",
  "branch": "main",
  "tag": "v0.1.0",
  "notesFile": "docs/releases/v0.1.0.md",
  "previousTag": null,
  "mergeCount": 0,
  "warnings": [],
  "notesWritten": "docs/releases/v0.1.0.md",
  "vsix": "acme-tools-0.1.0.vsix",
  "vsixBytes": 587
}
```

O Marketplace recusa o token (vencido). O envio é o último passo, então o release fica de pé:

```text
{
  "ok": false,
  "mode": "release",
  "tag": "v0.1.0",
  "steps": [
    "committed docs/releases/v0.1.0.md",
    "tagged v0.1.0 (annotated, with the notes)",
    "pushed main and v0.1.0",
    "GitHub Release v0.1.0 created: https://github.com/acme/acme-tools/releases/tag/v0.1.0"
  ],
  "stop": [
    "the release of v0.1.0 stands, but the upload to the VS Code Marketplace failed: ERROR  Failed request: (401) Unauthorized (token ***)"
  ],
  "rerun": [
    "npm exec -- vsce publish --packagePath acme-tools-0.1.0.vsix --skip-duplicate"
  ],
  "note": "the commit and the tag stay; nothing was deleted"
}
```

Claude: a tag e o GitHub Release existem; só o envio falhou (401), e o token aparece como `***`. Gere outro, defina `VSCE_PAT` e rode o comando de `rerun`, sem o token na linha; não apago a tag.

Você roda o reenvio. A segunda vez, com a mesma versão, não envia nada (a saída do `vsce` de teste):

```text
INFO  Publishing 'acme-tools-0.1.0.vsix'...
DONE  Published 0.1.0.
INFO  Version 0.1.0 is already published. Skipping publish.
```

Num release sem falha o relatório termina com o endereço do item (a saída do script num repositório de teste):

```text
{
  "ok": true,
  "mode": "release",
  "version": "0.1.0",
  "tag": "v0.1.0",
  "steps": [
    "committed docs/releases/v0.1.0.md",
    "tagged v0.1.0 (annotated, with the notes)",
    "pushed main and v0.1.0",
    "GitHub Release v0.1.0 created: https://github.com/acme/acme-tools/releases/tag/v0.1.0",
    "acme-tools 0.1.0 uploaded to the VS Code Marketplace: https://marketplace.visualstudio.com/items?itemName=acme-co.acme-tools"
  ],
  "warnings": [],
  "releaseAssets": [
    "acme-tools-0.1.0.vsix"
  ],
  "marketplace": "published",
  "marketplaceUrl": "https://marketplace.visualstudio.com/items?itemName=acme-co.acme-tools"
}
```

O `/agile:publish` não despublica: use o `vsce unpublish` por conta própria.

### 14.204 O kit AdminLTE importa um tema: a página conta o que ficou de fora e o arquivo passa pelo circuito

O app web Cais usa o kit AdminLTE e já tem a página `/appearance`; o segundo item, "Import and generate themes in the AdminLTE kit", está entregue. Uma pessoa recebeu o `night-owl.json`, feito à mão, com uma cor escrita como `red`, uma chave `Mystery` desconhecida e o foco (`Focus`, travado) em preto. Ela toca em "Importar tema..." e escolhe o arquivo.

Como a página é Blazor Server, o circuito lê o texto, na memória do servidor, e aplica as regras do oráculo de importação do plugin, que dá:

```
themes.js import report.json
{
  "formatVersion": 1,
  "id": "night-owl",
  "name": "Night Owl",
  "type": "light",
  "colors": {
    "Accent": "#0B57D0",
    "Surface": "#FFFFFF",
    "Text.Muted": "#555F6D"
  }
}
ignored bad color: Text "red"
ignored unknown key: Mystery
ignored locked key: Focus
ACCEPTED night-owl
```

e a área de resultado mostra, em pt-BR:

```
Seus temas (1 de 20)
  Night Owl   night-owl   claro   [Baixar] [Remover]
[Escolher arquivo (.json)]   Nome [Cais Light (custom)]   [Gerar a partir das minhas personalizações]

Resultado
  Importado "Night Owl" como night-owl
  ignoradas: 1 chave desconhecida, 1 chave travada, 1 cor inválida
```

O texto do arquivo não é gravado nem vai para o log: só o tema guardado, com as cores que passaram, vai para o `localStorage`, na chave `appearance.themes`. "Night Owl" entra na lista de temas claros sem recarregar. Importado de novo, vira `night-owl-2`, "Night Owl (2)". Escolhido como preferido, pinta desde a primeira pintura, até numa página estática, pela cópia `appearance.css` e pelo `appearance.js`, que não mudou.

Duas abas não perdem a mudança uma da outra, porque cada gravação relê a chave. Se alguém editar `appearance.themes` pelas ferramentas do navegador e puser uma cor `red`, a próxima carga descarta o que a importação recusaria e mostra um aviso com o número descartado.

### 14.205 Uma rodada do `/agile:refine` com uma decisão tirada das regras, uma assumida e um cartão só

O app web da acme-shop (`Profile: web-app`) tem em `.claude/rules/agile/project.md` a regra "Só o gerente cria e edita preços e cupons". O item "Desconto por cupom" precisa de três respostas: quem cria um cupom, a ordem dos campos do formulário e o desconto máximo de um cupom.

A primeira resposta está na regra: é aplicada, sem pergunta. As outras duas ganham um id do catálogo e passam pelo script:

```
autonomy.js check field-order
DELEGATED field-order (profile web-app)
autonomy.js check business-limit
RESERVED business-limit (unknown)
```

A primeira mensagem da rodada traz as duas listas, fora de qualquer bloco de pergunta, antes do cartão:

```
Taken from rules: só o gerente cria e edita cupons (.claude/rules/agile/project.md)
Assumed, not asked: código, desconto, validade, nessa ordem (field-order)
```

e um cartão só, com a pergunta reservada:

```
Desconto máximo de um cupom?
  1. 30% (Recommended) — a margem média do catálogo é 35%
  2. 50%
  3. Sem limite
```

Você responde 1. No resumo da aprovação as duas listas aparecem de novo; você escreve "a ordem é desconto, código, validade; aprovo F-31". A ordem muda e as linhas de `## Decisions` ficam:

```
- 2026-10-27: Só o gerente cria e edita cupons — confirmed: .claude/rules/agile/project.md
- 2026-10-27: Os campos vêm na ordem desconto, código, validade — asked
- 2026-10-27: O desconto máximo de um cupom é 30% — asked
```

A rodada escreve o evento `question` `asked 1, from context 1` e o `assumed` `1 answers`; um `override` registra a ordem que você mudou.

### 14.206 O `/agile:sync` aplica as cópias e as notas sem perguntar e faz um cartão só da mesclagem do perfil

O acme-shop é um app do perfil `web-app`, sincronizado pela última vez na 0.190.0; o plugin está na 0.196.0. Você digita `/agile:sync`. Antes de montar a tabela, o sync pergunta à política quem decide as duas categorias que ele usa (a saída do script na pasta do acme-shop, sem a linha `- Autonomy:`):

```
DELEGATED plugin-copy (base)
DELEGATED docs-commit (base)
```

As duas são do Claude. O plano acha um arquivo `update` (`.claude/rules/agile/git.md`, que você nunca editou), um `new` (`.claude/rules/agile/autonomy.md`), uma nota ⏳ do `retro-log.md` que o plugin já entregou e o `docs/agile/profile.md`, que você editou e cuja versão do plugin mudou. O sync copia os dois arquivos, marca a nota com ✅ e mostra, antes de qualquer cartão e fora de um bloco de pergunta:

```
Applied, not asked
| # | What | Category | Where |
|---|------|----------|-------|
| 1 | update: .claude/rules/agile/git.md | plugin-copy | .claude/rules/agile/git.md |
| 2 | new: .claude/rules/agile/autonomy.md | plugin-copy | .claude/rules/agile/autonomy.md |
| 3 | nota ⏳ entregue marcada ✅ (agile-canary#318, 0.190.2) | plugin-copy | docs/agile/retro-log.md |
```

A tabela vem antes de qualquer cartão. Sobra uma decisão que a política guarda com você, a mesclagem do perfil que você editou:

> ❓ **Pergunta**
> O `docs/agile/profile.md` é seu e o plugin mudou a seção `## Autonomy` dele. Mesclo a seção nova mantendo as suas linhas?
> a) Sim, mesclar e mostrar o resultado antes de gravar
> b) Deixar como está
> **Recomendação:** a). **Motivo:** a mesclagem é de um arquivo que você editou e fica com você; a seção nova só soma linhas.

Você responde a). O sync mescla e grava; só então vem o commit `chore: sync with agile@canary 0.196.0` na `main`, sem pergunta (digitar o comando autoriza; sem push). A linha final fecha a tabela:

```
Committed 3f9a1c2; git revert 3f9a1c2 undoes it.
```

O retro-log do acme-shop recebe a entrada do sync com cada linha aplicada no formato `<o quê> — assumed: plugin-copy (base)`. Se você quiser o cartão de volta para as cópias, escreva no `CLAUDE.md`, sob `## Profile`, a linha `- Autonomy: reserve plugin-copy`: o sync passa a perguntar por linha, como antes, e as outras categorias seguem do Claude.

### 14.207 O `/agile:epic` com mais de 5 features: o padrão repetido vira uma feature e o cartão agrupa o resto

O épico "Controles por tipo de dado" do acme-lab já tem F-40 "Tipo texto", `done`. O texto pede 8 controles de entrada do mesmo padrão (número, data, hora, moeda, percentual, CEP, telefone, e-mail) e mais "Validação no servidor", "Máscara por idioma", "Importar valores", "Histórico de alterações" e "Exportar valores".

Você digita `/agile:epic "Controles por tipo de dado"`. Os 8 controles viram uma feature só, "Controles de entrada por tipo": repetição não é tamanho. Com F-40, a tabela teria 7 linhas, mais que o limite de 5. Antes do seu acordo vem um cartão:

> ❓ **Pergunta**
> São 7 features e o épico comporta 5, contando F-40. Como seguimos?
> a) Agrupar mais: a máscara entra nos controles; importar e exportar viram "Importar e exportar valores"
> b) Mover o resto para um épico seguinte: histórico e exportar vão para um épico `draft` novo, sem número
> **Recomendação:** a). **Motivo:** cada junção é do mesmo padrão ou do mesmo arquivo, e nenhuma passa de L.

Você responde a). A tabela fica com 5 linhas: F-40, "Controles de entrada por tipo", "Validação no servidor", "Importar e exportar valores" e "Histórico de alterações". As duas features agrupadas levam no `## Start` a linha:

```
- Grouped at breakdown (2026-10-27)
```

No `/agile:refine` de "Controles de entrada por tipo", se o Claude julgar a feature L, o cartão de tamanho recomenda **Manter inteiro**, porque o agrupamento foi escolha sua. Se você tivesse respondido b), o arquivo `docs/epics/controles-por-tipo-de-dado-2.md` nasceria `draft`, com as duas linhas sem id; o `/agile:epic` dele as registra depois.

### 14.208 O estado do run de um `/agile:resolve`: as etapas na lista de tarefas e o debrief

O acme-shop roda `/agile:resolve acme/shop#41`, a issue em que você pediu a exportação de pedidos. A execução cria o estado do run logo depois de ler a issue, e a sessão mostra as oito etapas como lista de tarefas; Breakdown e Foundation já entram como puladas, porque a issue é um item só:

```
☑ Briefing
☑ Breakdown (skipped)
☑ Foundation (skipped)
☑ Slices
▶ Hardening
☐ Docs
☐ Handover
☐ Debrief
```

Você pergunta onde o run está, e o Claude lê o estado em vez de lembrar:

```
acme/shop#41 (item)
    Briefing (done)
    Breakdown (skipped)
    Foundation (skipped)
    Slices (done)
  > Hardening (current)
    Docs (pending)
    Handover (pending)
    Debrief (pending)
  activity: F-12 build, test phase
  F-12 built, attempts 1 [feature/F-12]
  run file: none
agile resolve-state SHOW: acme/shop#41 stage Hardening, 1 features, 0 events, 0 sessions
```

A linha `Autopilot:` do item, na worktree da F-12, é reescrita a partir do estado (`Autopilot: built`). Na parada 2 chega a notificação `acme/shop#41: validar a F-12`, depois do comentário de validação na issue. Depois do seu `validado e autorizo o merge de F-12` e do ship, o debrief aparece no chat e no comentário de entrega:

```
**Debrief do run**
- Tentativas por feature: F-12 1 (reviewer 1, vermelhos do gate 1)
- Escalações: 0
- Tokens por papel: desconhecido
- Perguntas: 2 feitas, 5 respondidas pelo contexto
agile resolve-state DEBRIEF: acme/shop#41 1 features, 0 escalations, tokens unknown
```

Os tokens saem `desconhecido` quando a transcrição da sessão não está na máquina; nada é estimado. Duas perguntas feitas contra cinco respondidas pelo contexto mostram quanto o projeto já responde sozinho.

### 14.209 Os agentes especialistas de um `/agile:resolve`: um teste errado devolvido e uma fatia que para na terceira tentativa

O acme-shop roda `/agile:resolve acme/acme-shop#52`, a issue do desconto por cupom. No build, o Claude não escreve teste nem código: o `test-writer` escreve os testes de aceitação, o Claude os roda, mostra os cinco vermelhos pelo comportamento que falta e os trava. A fatia 1 (o desconto no domínio) vai ao `backend`; o Claude roda os testes ele mesmo e anota o ciclo:

```
agile resolve-state CYCLE: acme/acme-shop#52 F-12 slice 1 closed, 2/5 passing, 0 of 5 cycles without progress
```

Na fatia 2, o `backend` termina com `BLOCKED: TEST WRONG PedidoTests.Total — asserts 110, AC3 says 100 with the discount`. O Claude lê o teste contra o critério AC3, confirma, e o estado manda devolver ao `test-writer` uma vez:

```
agile resolve-state RESEND: acme/acme-shop#52 F-12 test-writer once more, after test-lock.js unlock with the reason — PedidoTests.Total: asserts 110, AC3 says 100 with the discount
```

O Claude destrava com o motivo, registra em `## Decisions` `tests unlocked (...) — assumed by resolve`, o `test-writer` reescreve só esse teste e ele é travado de novo; isso não conta como tentativa. A tela da fatia 3 iria ao `ui-kit` (o controle de cupom que o kit não tem) e depois ao `frontend`, mas a fatia 2 continua vermelha: uma execução vermelha, um retrabalho pedido pela revisão, outra vermelha:

```
agile resolve-state CYCLE: acme/acme-shop#52 F-12 slice 2 attempt 1 of 3, 3/5 passing, 0 of 5 cycles without progress
agile resolve-state CYCLE: acme/acme-shop#52 F-12 slice 2 attempt 2 of 3, 4/5 passing, 0 of 5 cycles without progress
agile resolve-state STOP: attempts: acme/acme-shop#52 F-12 slice 2 red after 3 attempts, 4/5 acceptance tests passing
```

A terceira tentativa é a parada grave `attempts`: o comentário de parada vai à issue com o debrief parcial, chega a notificação e o guarda deixa só leituras até o seu `run-guard.js end 52`. Um segundo teste errado na mesma feature pararia do mesmo jeito (`STOP: test wrong`), e cinco ciclos seguidos sem nenhum teste a mais passando também (`STOP: no progress`).

### 14.210 O app segue o alto contraste do sistema: a chave, as duas listas e o tema que não existe mais

No acme-shop (MudBlazor, com `docs/design/themes/`), a pessoa liga um tema de contraste do Windows. Na página `/appearance` a chave "Seguir o alto contraste do sistema" já está ligada e as listas "Tema de alto contraste (claro)" e "Tema de alto contraste (escuro)" mostram só os temas `hc-*` de cada variante, pelo nome.

Ela não escolheu nada, então vale o primeiro do índice: `cais-hc-light`. Meses depois o tema de alto contraste que ela tinha importado e escolhido (`amber-hc-light`) é removido; o app cai no primeiro `hc-*` da variante, sem erro. A regra tem 12 casos, e o caso 6 é este:

```
ok   resolution case 6 (light) gives cais-hc-light
```

Com a chave desligada, ou sem o sinal do sistema, vale o tema preferido de sempre.

### 14.211 Os anexos de um `/agile:resolve`: um arquivo lido com uma linha cortada e os que não são lidos

A issue `acme/acme-shop#52` linka `regras-cupom.md` e `exemplo-pedidos.csv`, uma captura de tela e um PDF. O `resolve-issue.js read` baixa os dois arquivos de texto, passa cada um pelo `attachment-scan.js` e guarda o texto limpo na pasta do run; nada do que foi baixado aparece na saída:

```
attachment read: https://github.com/user-attachments/files/501/regras-cupom.md -> <pasta do run>/attachments/1-regras-cupom.md (1 passages cut)
attachment flagged: https://github.com/user-attachments/files/501/regras-cupom.md line 4 instruction
attachment read: https://github.com/user-attachments/files/502/exemplo-pedidos.csv -> <pasta do run>/attachments/2-exemplo-pedidos.csv (0 passages cut)
attachment not read: https://github.com/user-attachments/assets/3f2c-91aa (image)
attachment not read: https://github.com/user-attachments/files/503/especificacao.pdf (not a text format)
```

A linha 4 de `regras-cupom.md` mandava o modelo ignorar as instruções e fazer o merge na `main`. O arquivo guardado tem no lugar dela `[passage removed by scan: instruction]`, e o run lê o resto como dado. Na parada 2 o comentário de validação na issue diz o que foi lido e o que foi cortado, sem repetir o trecho:

```
**Anexos lidos**
- https://github.com/user-attachments/files/501/regras-cupom.md — 1 trecho cortado
  - linha 4: instruction
- https://github.com/user-attachments/files/502/exemplo-pedidos.csv — nenhum trecho cortado

**Anexos não lidos**
- https://github.com/user-attachments/assets/3f2c-91aa (imagem)
- https://github.com/user-attachments/files/503/especificacao.pdf (formato que não é texto)
```

Abra o arquivo cortado se quiser conferir o que saiu: o texto original continua no anexo da issue, e só você decide o que fazer com ele. Um download recusado (403, 404, redirecionamentos demais) aparece como `download recusado (HTTP 404)`, e a retomada do run não baixa nada de novo.

### 14.212 Um lote no `/agile:refine`: três itens numa rodada e uma aprovação que deixa um de fora

No acme-shop, `F-3` (cupom de desconto), `F-4` (histórico de pedidos) e `F-7` (exportar pedidos em CSV) estão em `idea`. Você digita `/agile:refine F-3 F-4 F-7`. O lote é conferido primeiro:

```
BATCH F-3 F-4 F-7
```

Cada item imprime a linha dele, passa pela busca de itens parecidos e ganha a própria worktree (`D:/wt/acme-shop/f-3-coupon-discount`, `f-4-order-history`, `f-7-export-orders`). Depois vem uma rodada só, por tema, cada pergunta com o id do item:

```
[F-4] Assumed, not asked: the history lists 20 orders per page (layout-in-kit)
Regras — [F-3] Qual o desconto máximo de um cupom?
Regras — [F-4, F-7] Um pedido cancelado aparece no histórico e na exportação?
Permissões — [F-7] Quem pode exportar os pedidos?
```

A pergunta do pedido cancelado vale para os dois itens e vem uma vez. No fim, um resumo por item e a linha `Approve the batch with one reply naming each id: Aprovo F-3, F-4, F-7`. Você responde `Aprovo F-3 e F-7`, e a resposta é lida pelo script:

```
APPROVED F-3 F-7
NOT NAMED F-4
```

`F-3` e `F-7` vão para `approved`; `F-4` segue `refining` na worktree dele, e um `aprovo F-4` depois o aprova. O próximo passo é `/agile:build F-3`, com `F-7` esperando na ordem do lote.

### 14.213 O nível de risco que o build grava: um item só de documentação e um que toca o login

O item F-31 mudou só uma página do manual e um teste. Antes de pôr o item em `validating`, o build roda o `risk.js` na worktree:

```
agile risk LOW: docs and tests only
```

Depois o mesmo item ganhou `src/Modules/Identity/LoginEndpoint.cs`, num módulo que a `main` não tinha. O mesmo comando agora lista cada sinal com o caminho que o deu:

```
signal: door (src/Modules/Identity/LoginEndpoint.cs)
signal: new-module (src/Modules/Identity)
agile risk HIGH: door: src/Modules/Identity/LoginEndpoint.cs; new-module: src/Modules/Identity
```

O `write` grava a linha em `## Start`, logo abaixo de `- Model:`:

```
- Risk: high (door: src/Modules/Identity/LoginEndpoint.cs; new-module: src/Modules/Identity)
```

Se você achar que um item `low` merece mais cuidado, troque a linha por `- Risk: high (owner)`: o próximo `write` a mantém. Um `- Risk: low (owner)` num item com porta volta a `high`, e a linha diz de onde veio.

### 14.214 Um roteiro em passos numerados, e o que o conferidor diz quando falta o esperado

O build do F-12 (exportar pedidos) entrega o roteiro com o rótulo e o tempo em cada passo, e o resultado esperado numa linha só dele:

```
1. [ ] Passo 1 de 2 (cerca de 2 min): No menu lateral clique em Pedidos e depois no botão Exportar, acima da lista.
   Esperado: um arquivo `orders.csv` é baixado.
2. Passo 2 de 2 (cerca de 3 min): Abra o arquivo.
   - [ ] 2.1 Conte as linhas.
     Esperado: uma linha por pedido da lista, mais o cabeçalho.
   - [ ] 2.2 Leia a coluna Total.
     Esperado: os valores têm duas casas decimais.
```

Você sabe quantos passos faltam e quanto tempo reservar antes de começar; o `Esperado:` diz o que comparar sem reler a ação. O conferidor, rodado com `--file` no arquivo do item, responde `OK`. Num rascunho em que o passo 1 perdeu a linha `Esperado:` e o passo 2 do pt-BR perdeu o rótulo, ele nomeia cada defeito:

```
step 1 has no Expected: line
step 2 has no "Step 2 of 2 (about <k> min):" label
```

Um roteiro escrito antes do #347, sem rótulo no passo 1, continua valendo como está.

### 14.215 Um roteiro em duas partes: o que o Claude já verificou e o que só você pode julgar

O build do F-12 (exportar pedidos) rodou os testes e abriu a tela de pedidos no navegador embutido antes de escrever o roteiro. O que ele já conferiu vai, com a evidência e o commit, para a primeira parte; a segunda fica só com o que pede os seus olhos:

```
#### Verificado pelo Claude
- AC1-AC3: `node --test tests/Orders.Tests` imprimiu `14 passed` (em 3f2a9c1).
- O botão Exportar baixa `orders.csv` com uma linha por pedido: captura `docs/agile/evidence/F-12-export.png` (em 3f2a9c1).

#### Só você
1. [ ] Passo 1 de 1 (cerca de 3 min): Abra o `orders.csv` no Excel da sua máquina.
   Esperado: a coluna Total mostra duas casas decimais e os acentos dos nomes estão certos.
```

Você lê a primeira parte como relatório e só executa o passo 1. Se um rascunho tira o commit das duas linhas verificadas do pt-BR, o conferidor recusa e diz o que falta:

```
verified entry count differs: English 2, pt-BR 0
a verified entry with no "(at <commit>)": "AC1-AC3: `node --test tests/Orders.Tests` imprimiu `14 passe"
a verified entry with no "(at <commit>)": "O botão Exportar baixa `orders.csv` com uma linha por pedido"
```

### 14.216 A parada 2 de um item de risco baixo: só a autorização do merge

O F-14 só mudou a página de ajuda e um teste, e o build gravou `- Risk: low (docs and tests only)`. O roteiro dele tem a parte verificada e, na sua parte, uma linha só:

```
#### Verificado pelo Claude
- AC1-AC2: `node --test tests/Help.Tests` imprimiu `6 passed` (em 8c41d07).

#### Só você
Nada: o item só mudou texto e testes, e os dois rodaram aqui.
```

A parada 2 mostra essa evidência e pede uma resposta: `autorizo o merge de F-14`, que vale como validação e autorização. Se o mesmo roteiro tivesse um passo na sua parte, o conferidor recusaria:

```
a low-risk item's script is in two parts with one "Nothing:" line for the owner: the owner only authorizes the merge (#344)
```

Num item `medium` você segue só os passos de `#### Só você`; num `high`, segue esses passos e lê cada linha verificada. Para olhar mais de perto um item `low`, troque a linha por `- Risk: high (owner)` e peça o roteiro de novo.

### 14.217 A revisão do dia: três itens, um aprovado, um devolvido, um só autorizado

No fim do dia três itens estão em `validating`. `/agile:script --inbox` lista cada um com o nível de risco, o que o Claude verificou e o que fica com você, e manda a página HTML com os mesmos três cartões:

```
F-13 — Customer notes [risk medium]
   changed: A customer gets a free-text notes field on their page.
   verified: AC1-AC2: `dotnet test` imprimiu `9 passed` (em 77b0e21).
   yours: 1. Passo 1 de 1 (cerca de 2 min): Digite uma nota de três linhas num cliente e salve. — as três linhas voltam como digitadas.
   finding: 2026-10-28: review 1 found 1 major: the note is saved without a length limit — fixed with a 2,000-character limit.
   answer: validado e autorizo o merge de F-13  |  devolvo F-13: <nota>
```

O F-14 é `low`: a parte dele diz que não há nada para você fazer. Você clica nos botões da página e cola o texto que ela monta:

```
validado e autorizo o merge de F-12
devolvo F-13: a nota com acento some depois de salvar
autorizo o merge de F-14
```

O Claude passa a resposta pelo `review-inbox.js answer`:

```
approved: F-12
approved: F-14
sent back: F-13 — a nota com acento some depois de salvar
next: /agile:ship F-12, /agile:ship F-14
agile inbox ANSWER: 2 approved, 1 sent back
```

O F-13 continua em `validating` com a sua nota em `## Decisions`, à espera da correção. Uma linha como `aprovado` ou `autorizo todos`, que não nomeia um item, não aprova nada.

### 14.218 O rigor de um item de risco baixo

O F-14 só mudou uma página do manual. Antes da revisão, o build pergunta ao `risk.js rigor` quanto verificar:

```
level: low (computed)
review: no (the owner may still ask: /agile:review)
live run: no
ship gate: gate.js ship (a sealed result counts)
measures: only the numbers a criterion asserts
agile rigor LOW: docs and tests only
```

O build pula a revisão e segue para a parada 2. Se você escrever `- Risk: high (owner)` no `## Start`, a mesma chamada imprime `agile rigor HIGH: the item's high`, e a revisão e o gate `--fresh` voltam.

### 14.219 Os riscos de uma mudança: o que você ouve antes de validar

A revisão do F-21 achou dois riscos fora do diff e o Claude os escreveu em `## Risks`:

```
- The migration drops the old Notes column — its data is copied first (0007_Notes.cs:12) — not proven — high impact — one-way door
- The customer list sorts by name — the sort is the query's ORDER BY (Customers.cs:40) — proven — low impact — two-way door
```

Na parada 2, antes dos passos, o Claude roda `risks.js check` e diz o risco que ninguém provou:

```
say first: - The migration drops the old Notes column — its data is copied first (0007_Notes.cs:12) — not proven — high impact — one-way door
agile risks OK: 2 risks, 1 one-way door, 1 to say before the validation
```

Você decide se valida assim ou se pede um teste que prove a cópia antes. O corpo do PR traz as duas linhas, a porta de mão única primeiro.

### 14.220 O painel de itens: três itens em andamento, o que espera por você primeiro

Você tem uma feature esperando a sua validação, outra sendo construída em outra sessão e um bug aprovado que depende dela. Em vez de perguntar a cada sessão, você pede o painel no chat:

```
/agile:status --dashboard
```

A sessão gera a página, envia o arquivo e o app abre no painel lateral. A tabela tem o mesmo conteúdo da versão em texto, que você obtém com `--text`:

```
Itens em andamento (gerado 2026-10-28 12:00)
Item                                    Etapa                 Worktree                                    Espera por     Última checagem  Na etapa  Próxima ação
#12 Cadastro de banca examinadora       aguardando validação  f-12-exam-board · limpo · 24 h atrás        você (o dono)  —                1 d       rodar o roteiro e dizer validado ou o defeito
#14 Exportar notas em CSV               construindo           f-14-export-csv · 4 sem commit · 2 h atrás  nenhum         —                0 d       critério 4, a tela de exportação
#15 Corrigir o arredondamento da média  aprovado              —                                           #14            —                2 d       —
0 ideias no backlog
check-all rodando: pid 18420
```

O item 12 espera por você e vem primeiro, com o worktree limpo e o último commit de ontem. O item 14 está sendo construído, com quatro arquivos sem commit. O item 15 está aprovado e espera pelo 14, na coluna "Espera por".

A linha `check-all rodando` aparece quando uma checagem completa está em andamento, para você não disparar outra por cima. Um `—` é uma informação que ainda não existe, como a última checagem de um item que nunca rodou uma.

O painel só lê: nada do ciclo muda ao abri-lo, e para atualizar basta digitar o comando de novo.

### 14.221 Três itens construídos ao mesmo tempo com `/agile:build F-3 F-4 F-7`

Você aprovou F-3, F-4 e F-7 e vai sair. Digita `/agile:build --approved --max 2`. O Claude mostra o plano: F-8 espera o F-3 e fica de fora.

```
START F-3
START F-4
QUEUED F-7
SKIPPED F-8 (waits on F-3)
agile dispatch PLAN: 3 items, 2 at once
```

Duas sessões começam, cada uma na worktree do seu item; o F-7 entra quando uma delas termina. Nada é perguntado. Quando você volta, as linhas chegaram uma por item, na ordem em que cada um terminou:

```
F-4 STOPPED (building): Stopped: the schema change is not in the item file (owner-reserved stop).
F-3 READY (validating) in 4 s
F-7 READY (validating) in 3 s
agile dispatch DONE: 2 ready, 1 stopped, 0 failed in 6 s
```

O F-3 e o F-7 esperam a sua validação: `/agile:autopilot F-3` mostra o roteiro. O F-4 parou numa parada sua; resolvida a causa, `/agile:autopilot F-4` continua. Nenhum dos três foi mesclado.

### 14.222 O quadro em dois momentos: o que o board mostra ao longo de um item

Você acompanha um item de `/agile:refine` até `/agile:ship`. O campo Status do board só muda duas vezes: quando você aprova e quando o ship fecha. Nos outros passos o plugin não escreve nada no board (e não gasta o envio do corpo inteiro); a etapa está no arquivo do item.

```
refining: quadro não escrito
approved: quadro escrito
building: quadro não escrito
validating: quadro não escrito
done: quadro escrito
```

Se você abrir o board no meio do build, o item aparece em Ready: não é divergência. O `/agile:status` compara o board com os arquivos só em `approved` e `done`.

### 14.223 A retro uma vez por lote: três ships, uma retro

Você shippa o F-21 (filtro por data). No passo da retro o ship não pergunta nada: guarda as notas do item e segue.

```
Retro: notas de F-21 guardadas em docs/agile/retro-notes.md (1 item); a retro roda com 3.
```

No dia seguinte você shippa o F-22 e o B-7; as notas chegam a 3 itens, e o ship do B-7 roda a retro uma vez para os três: as contagens de eventos somadas, no máximo 3 lições, cada uma com as duas notas.

```
Events (3 items): rework 2, correction 1, test-fail 3
1. score 6/7 — os testes de data falham perto da meia-noite UTC: fixe o relógio do teste (F-21, B-7)
Retro notes: 3 itens lidos e removidos de docs/agile/retro-notes.md
```

Para fechar a retro antes do terceiro item, digite `/agile:retro notes`.

### 14.224 O roteiro numa língua só: só o `### pt-BR`

O build do F-31 (filtro de pedidos por data) termina e escreve o roteiro uma vez, na sua língua. No arquivo do item a seção fica assim:

```
## Validation script
### pt-BR
#### Verificado pelo Claude
- Os testes do filtro: `14 passed` (em 3f2a9c1)

#### Só você
1. Passo 1 de 1 (cerca de 2 min): Abra a lista de pedidos e filtre por ontem.
   - [ ] 1.1 Só os pedidos de ontem aparecem.
     Esperado: 3 pedidos.
```

O conferidor aceita o roteiro como está. Se um rascunho vier só com `### English`, ele recusa e diz:

```
missing ### pt-BR subsection (the owner's language, #388)
```

### 14.225 Opus com effort `high` num item extremo, só com o seu sim

No acme-shop você refina a F-40, "Refund a paid order through the payment webhook". O item tem três sinais: uma porta (dinheiro e webhook), uma mudança de esquema que não é só adição (a coluna `Status` do pedido ganha `refunded` e as linhas antigas migram) e três módulos tocados. Dois ou mais sinais fazem o item extremo, e o resumo da aprovação traz a proposta:

Claude: Modelo sugerido para construir a F-40: opus (porta: dinheiro e webhook; esquema não aditivo; três módulos). O item é extremo: proponho effort `high` na construção e nas passagens de desenho e revisão. Para aceitar, responda `aprovo F-40 effort high`; um `aprovo F-40` simples aprova sem effort.

Você responde `aprovo F-40 effort high`, e a linha fica no `## Start`:

```
- Needed to validate: a test key of the payment provider
- Model: opus, effort high (door: money, webhook; schema not additive; 3 modules; owner's yes)
```

Se você tivesse respondido só `aprovo F-40`, a linha seria `- Model: opus (door: money, webhook; schema not additive; 3 modules)`. Na F-41, "Show the refund date in the order list", sem nenhum sinal, o resumo não propõe effort nenhum.

Você abre uma sessão em `opus` e digita `/agile:build F-40`. Mesmo com o mesmo modelo, o Claude diz uma linha e segue, sem perguntar:

Claude: Model: item suggests opus, effort high, session is opus

Você digita `/effort high` e a construção continua. No passo 4b, as chamadas de `system-design` e `architect` vão em `opus` com `effort: high`, e o mesmo vale depois para o `reviewer` do `/agile:review`. Na lista do `/agile:order` a coluna `Model` da F-40 continua `opus`.

### 14.226 As faixas de um plano

Sete itens estão abertos. F-1 e F-2 mexem no mesmo arquivo, F-3 espera o F-2, F-4 espera um item que não está no plano, F-5 já está em validação, F-6 não nomeia nenhum arquivo do repositório e o branch do F-7 só mudou um arquivo. O plano das faixas, com `--used 40`, fica assim:

```
SKIPPED F-5 (validating)
SKIPPED F-4 (waits on F-9)
NOFILES F-6
LANE jussara: F-1 (opus), F-2 (opus), F-3 (sonnet) | files: 3
LANE olinda: F-6 (opus) | files: 0
LANE nanda: F-7 (opus) | files: 1
agile dispatch LANES: 5 items in 3 lanes
agile dispatch BUDGET: the week used 40%: ok (step down at 70%, ask at 80%)
```

A semana já usou 40% da sua cota: não há limite até 80%.

São três faixas: a bruna nem existe. Com `--write`, cada faixa vira uma linha de `lanes.json` no estado `planned`. Quando a nanda termina o F-7, a fila dela fica vazia e ela pega o F-6 da olinda:

```
STOLE F-6 from olinda
```

Os itens da jussara não saem dela enquanto dividem arquivos ou um espera o outro.

### 14.227 Um resolve de várias issues: as faixas abrem suas sessões

Você tem três issues prontas e digita:

```
/agile:resolve #41 #42 #43
```

O Claude captura as três como itens, F-1, F-2 e F-3, e planeja as faixas. A jussara fica com F-1 e F-2, que mexem nos mesmos arquivos; a olinda, com F-3. No app abrem duas sessões novas, com os nomes jussara e olinda. A nanda e a bruna não abrem: não têm item.

Na sessão da jussara, o primeiro passo pergunta o item:

```
WORK F-1 (sonnet)
```

Quando F-1 chega a `validating`, a faixa pede o próximo e segue com F-2. A olinda termina F-3 primeiro e fica com a fila vazia. Ela não pega F-2, que divide arquivos com F-1, e para:

```
IDLE olinda
```

Você valida F-1 e autoriza o merge na sessão da jussara. Antes do ship, a faixa pede a vaga do lote:

```
SLOT jussara F-1: yours
```

Se a olinda pedir a vaga ao mesmo tempo, ela espera a vez:

```
WAIT olinda F-3: 1 ahead
```

### 14.228 O painel ao vivo: o que espera por você, as lanes e a hora dos ships

Você deixa três lanes construindo e quer ver, sem perguntar a cada sessão, o que depende de você agora. Digita no chat:

```
/agile:status --dashboard --watch
```

A sessão inicia o painel em segundo plano e abre a página no painel do navegador do app. A página se reescreve a cada 30 segundos e recarrega sozinha. Os quatro blocos também saem em texto com `--text --all`:

```
O que espera por você
  #12 Cadastro de banca examinadora — validação e merge — diga: autorizo o merge de 12
  #15 Corrigir o arredondamento da média — aprovação — diga: aprovo 15
  lane nanda · #12 · aguardando você
Sessões
  orçamento: a semana usou 40% (reduz o modelo em 70%, pergunta em 80%) · ok
  jussara · opus · #14 · trabalhando · 12 min · US$ 2.25
  olinda · sonnet · #15 · trabalhando · 3 h · US$ 0.50 · tirado de bruna
  nanda · sonnet · #12 · aguardando você · 40 min · US$ 1.00
Itens em andamento (gerado 2026-10-28 12:00)
Item                                    Etapa                 Worktree                                    Espera por                      Última checagem  Na etapa  Próxima ação
#12 Cadastro de banca examinadora       aguardando validação  f-12-exam-board · limpo · 24 h atrás        você (o dono)                   —                1 d       rodar o roteiro e dizer validado ou o defeito
#14 Exportar notas em CSV               construindo           f-14-export-csv · 4 sem commit · 2 h atrás  nenhum                          —                0 d       critério 4, a tela de exportação
#15 Corrigir o arredondamento da média  aprovado              —                                           o dono precisa aprovar o corte  —                2 d       construir
0 ideias no backlog
Tempo e progresso
  ships (180 s):
    2026-10-28 09:10 UTC · 170 s · dentro
    2026-10-27 16:40 UTC · 410 s · acima
    2026-10-27 10:05 UTC · 95 s · dentro
  hoje: 1 lote, 2 itens
  por dia: 2026-10-28 2 · 2026-10-27 1
  noturno: VERDE · 8 h · 997 s
```

No topo está o que só você destrava: o item 12 pede a sua validação (a frase para copiar vem na mesma linha) e o 15 pede a aprovação do corte. Em Sessões, a olinda trabalha no 15 com um item tirado da bruna, e a nanda aguarda você. Em Tempo e progresso, o ship de 410 s ficou acima da meta de 180 s.

Nada sai da máquina, e fechar o painel não muda nada no ciclo.

### 14.229 A cota da semana chega a 80% numa execução em faixas

Duas faixas trabalham: a jussara em F-1 e F-2, a olinda em F-3. A sua cota semanal passa de 70%, e o próximo item da jussara começa um degrau mais barato:

```
WORK F-2 (sonnet, stepped down from opus: the week used 72%)
```

A faixa anota essa linha no `## Decisions` do F-2. Mais tarde a semana chega a 81%. Ao terminar F-3, a olinda não começa outro passo:

```
HOLD olinda: the week used 81% of a 80% ceiling: wait for the owner
```

Com as duas faixas paradas, o Claude faz uma pergunta só: a semana em 81%, o teto em 80% e os itens que faltam. Você responde "sim, até 90%". Cada faixa, no próximo passo, mostra a sua resposta antes do item e a anota no item:

```
AUTHORIZED 90%: "sim, até 90%"
```

Se você responder "não", as faixas param com um commit wip e a fila fica para depois.

### 14.230 Um arquivo, um dono: a olinda pede, a jussara concorda e a correção é copiada

A olinda precisa de `scripts/check-kit.js`, que a jussara está usando. Ela não pega o arquivo: pede.

```
dispatch.js ask olinda scripts/check-kit.js
ASKED scripts/check-kit.js from jussara
```

O pedido fica registrado e o arquivo continua com a jussara. Qualquer faixa vê a fila de pedidos:

```
dispatch.js asks
olinda asks scripts/check-kit.js from jussara
```

Quando a jussara termina o que fazia no arquivo, ela concorda e solta:

```
dispatch.js release jussara scripts/check-kit.js
GRANTED scripts/check-kit.js to olinda (released by jussara)
```

Mais tarde as duas esbarram no mesmo defeito. A jussara corrige primeiro e registra a correção:

```
dispatch.js fix register <commit> --lane jussara --defect trim
REGISTERED trim by jussara (0159dc4) fix(F-5): trim the input
```

A olinda não refaz: com o worktree limpo, roda `fix copy`, que aplica o commit da jussara, confere que cada arquivo ficou igual ao dela e commita com a mesma mensagem:

```
dispatch.js fix copy trim
COPIED trim from jussara (0159dc4) byte for byte
```

Os dois ramos têm a mesma correção, e juntá-los não dá conflito.

### 14.231 As paradas das faixas numa pergunta só

Três faixas param ao mesmo tempo: a jussara tem o #1 pronto para validar, a olinda quer dividir o #3 e a nanda precisa destravar um teste do #4. Nenhuma delas pergunta nada na própria sessão. O orquestrador junta as três numa pergunta só:

```
Round 1: 3 stops
#50 Shop:
- #1 (jussara) validation: #1 is ready: do you validate it and authorize its merge?
#60 Billing:
- #3 (olinda) split: split #3 in two parts?
no epic:
- #4 (nanda) unlock: unlock the AC2 test of #4?
Answer for each item, in your own words.
```

Você responde só sobre o #1 e o #3. O #4 fica para a rodada seguinte. A jussara recebe a sua resposta com a pergunta que ela respondeu e a linha que escreve no item:

```
RELAY #1 validation
question (round 1): - #1 (jussara) validation: #1 is ready: do you validate it and authorize its merge?
the owner, verbatim: "Valido o #1 e autorizo o merge; pode dividir o #3 — "como proposto""
- 2026-10-29: validated by the owner — Valido o #1 e autorizo o merge; pode dividir o #3 — "como proposto" (relayed by the orchestrator, round 1: "- #1 (jussara) validation: #1 is ready: do you validate it and authorize its merge?")
```

### 14.232 O resolve retoma as faixas depois de uma queda

A sessão que coordenava as faixas fechou no meio da execução. Você abre uma sessão nova e digita:

```
/agile:resolve --resume
```

O Claude lê o que ficou no disco e mostra cada faixa:

```
LANE jussara waiting owner F-1 session s1 queue 1 stops 1/1/0: reconnect
LANE olinda planned - session - queue 1 stops 0/0/1: open, relay
LANE nanda stopped - session - queue 0 stops 0/0/0: done
ROUND 2 unanswered:
Round 2: 1 stops
#9 Shop
- F-1 (jussara) validation: validation of F-1
Answer for each item, in your own words.
agile resume PLAN: 3 lanes, 1 to reconnect, 1 to open
```

A sessão da jussara ainda existe, e o Claude manda nela a ordem de seguir. A olinda não tem sessão: abre uma nova com o nome dela, que primeiro recebe a resposta que você já deu e depois segue a fila. A nanda terminou. A pergunta da rodada 2 volta para você como estava, sem rodada nova.

### 14.233 Uma autorização para o épico inteiro

Você vai deixar o épico #379 andando sozinho. Na sessão que faz o merge, você digita só isto:

```
Delego a validacao do epico #379
```

Sem acento ou com acento, a frase é a mesma; as suas palavras ficam guardadas como você as digitou. O `/agile:status` passa a mostrar:

```
[agile] branch main
In progress: nothing
Delegation: epic #379 — "Delego a validacao do epico #379" (2026-10-29)
```

Quando o épico fechar, ou quando você digitar `revogo a delegação`, a linha some, e o próximo item volta a parar na sua validação.

### 14.234 O sync mostra quanto o contexto pesa

Depois de atualizar o plugin, você digita `/agile:sync` num projeto com o `CLAUDE.md` de 2.900 palavras. Ao gravar o registro, o Claude repete a linha de contexto que o `sync.js` imprimiu:

```
context: 2903 of 3461 words (no earlier measure)
```

Em português: 2.903 de 3.461 palavras, e nenhuma medida anterior para comparar. Três semanas depois o `CLAUDE.md` tem 3.300 palavras, e o próximo sync mostra:

```
⚠ context: 3303 of 3461 words (+400 since the last sync) — run /agile:doctor
```

O aviso diz que passou de 95% do teto e cresceu mais de 150 palavras. O sync não abre card nenhum; o `/agile:doctor` continua sendo seu para digitar. Quando você digita, a primeira linha do relatório avisa quanto sobra:

```
agile doctor WARNING: always-loaded context at 95% of the cap (3303 of 3461 words, room 158): run the cuts first
```

### 14.235 Sob a delegação, o médio passa e o alto para

Você delegou o épico #378. Dois itens dele chegam à validação. O primeiro é de risco médio, com todos os passos marcados e sem achado grave; o Claude confere e ele segue sozinho para o ship:

```
agile delegation ELIGIBLE: F-71 under epic #378 — "delego a validação do épico #378" (2026-10-29)
```

O outro é de risco alto. Ele para na validação, como sempre, e o relatório diz por quê:

```
agile delegation STOP: F-74: risk high
```

### 14.236 O --delegate: um épico inteiro sem paradas

Você quer o épico #378 rodando até o fim sem precisar de você em cada item. Em vez de digitar a frase, você põe a delegação no próprio comando:

```
/agile:epic #378 --autopilot --delegate
```

A delegação fica registrada com o comando que você digitou:

```
agile delegation: epic #378 — "/agile:epic #378 --autopilot --delegate" (2026-10-29)
```

Cada item que chega à validação seguro o bastante passa sozinho. Um item de risco alto, um check vermelho, um achado grave, um corte de tamanho L ou os 80% da sua cota semanal ainda param em você. Sem o `--delegate`, o mesmo comando para em cada validação, como antes.
### 14.237 O painel de um projeto mostra o que o projeto tem

Você abre o painel de um projeto com três itens prontos e uma faixa trabalhando. Em vez de "histórico de ships indisponível", o bloco mostra o que existe:

```
Tempo e progresso
  hoje: 2 itens
  por dia: 2026-10-29 2 · 2026-10-28 1
  lane jussara: 2 de 4 itens prontos
```

Dois itens ficaram prontos hoje e um ontem. A faixa jussara recebeu quatro itens e já entregou dois.
### 14.238 O resolve abre o painel

Você digita `/agile:resolve` com três issues. Depois de abrir as faixas, o Claude mostra o painel. Na CLI ele inicia o painel em segundo plano; a primeira linha dele é:

```
agile dashboard WATCH: every 30 s -> D:/dev/_icontrol/agile-canary/.git/agile/dashboard.html
```

No app não há nada para iniciar: o painel agile do Code desktop abre sozinho quando uma faixa começa a trabalhar (exemplo 14.239). Na CLI, a página se recarrega sozinha a cada 30 segundos e o Claude escreve `Dashboard: D:/dev/_icontrol/agile-canary/.git/agile/dashboard.html` e você abre o arquivo onde quiser. Quando as faixas terminam, o Claude para o painel.

### 14.239 O painel agile no Code desktop

Três faixas trabalham e uma espera por você. Acima do prompt do Code desktop aparece a faixa:

```
⚠ 2 espera você · jussara F-5 aguardando você · olinda F-6 trabalhando · semana 40%
```

O painel abre sozinho quando a primeira faixa começa a trabalhar. Se você o fechar, digite `dashboard` para abri-lo de novo. Ele mostra os quatro blocos:

```
O que espera por você
  F-5 Cinco — validação e merge — diga: autorizo o merge de F-5
  lane jussara · F-5 · aguardando você
Sessões
  orçamento: a semana usou 40% (reduz o modelo em 70%, pergunta em 80%) · ok
  jussara · opus · F-5 · aguardando você · 1 h · US$ 1.50
  olinda · sonnet · F-6 · trabalhando · 30 min · US$ 0.50
Itens em andamento
  F-5 Cinco — aguardando validação · você (o dono) · o dono valida
Tempo e progresso
  hoje: 0 itens
  por dia: —
  lane jussara: 0 de 1 itens prontos
  lane olinda: 0 de 1 itens prontos
```

Nenhuma página é aberta e nada é gravado: o painel só lê os arquivos do repositório.

No app, a página abre no painel do navegador e se recarrega sozinha a cada 30 segundos. Na CLI, o Claude escreve `Dashboard: D:/dev/_icontrol/agile-canary/.git/agile/dashboard.html` e você abre o arquivo onde quiser. Quando as faixas terminam, o Claude para o painel.

### 14.240 Uma faixa abre num modelo abaixo do plano e o Claude a troca

O plano dá opus para o item da jussara, mas a sessão que coordena roda em sonnet, e o app abre a faixa em sonnet. O Claude confere cada faixa:

```
MODEL jussara s1 wants opus
MODEL olinda s2 wants haiku
```

A sessão da jussara está em sonnet, abaixo do que o item pede. O Claude troca o modelo dela para opus, e o app pede o seu sim, porque o opus custa mais. Você aceita, e a jussara segue em opus a partir da próxima resposta. Se você recusar, ela segue em sonnet, e o item anota isso nas suas decisões.

### 14.241 O board do épico atrasado, acertado antes da fila

Você roda `/agile:epic data-type-controls --autopilot`. O arquivo do épico tem as features F-189 e F-190 e a F-183 entregue, mas a issue do épico no board ficou para trás. Antes da primeira feature, a execução mostra:

```
MISSING F-189: in the file, not on the board
MISSING F-190: in the file, not on the board
STATUS F-183: file done, board idea
AUTOPILOT: the header names F-183, which is done
agile epic-mirror DRIFT: 4 differences
```

Nada disso é pergunta sua: o Claude acerta a linha `Autopilot:` para a primeira feature da fila, manda o corpo do épico de novo a partir do arquivo, confere, e a fila começa. Só uma linha a mais no board, que o arquivo não tem, para e pergunta.

### 14.242 O gasto de uma faixa visível, tirado da própria sessão

A jussara termina o F-1 e anota o gasto da sessão até ali:

```
agile lane jussara: US$ 0.02 spent (estimated from session aaaaaaaa-1111-2222-3333-444444444444)
```

O valor é uma estimativa pelo preço de cada modelo, lida da transcrição da própria sessão. No item seguinte o total cresce e substitui o anterior; ele nunca soma de novo o que já tinha contado. O painel e a cota da semana passam a ver esse gasto.

### 14.243 O painel redesenhado mostra o término e o botão da resposta

Você roda o `/agile:resolve` com oito issues em duas lanes. No Code desktop o painel `dashboard` mostra, no topo, o projeto e a execução, e logo abaixo o progresso:

```
simulab
/agile:resolve · 8 issues em 2 lanes · começou 15:15 · semana 8% de 80%
5 de 8 features · faltam 3 · 1 construindo · 1 com você · 1 na fila
~21:01 · término estimado · em 46 min
ritmo: 1 feature a cada 60 min
```

Em "Sua vez" aparece um cartão com um botão cujo texto é a resposta exata:

```
merge F-196 Color picker
validação e merge · esperando há 8 min · trava a lane nanda
[autorizo o merge de F-196]
```

Apertar o botão envia a frase ao agente na hora, como se você a digitasse: não há confirmação, então um merge autorizado assim acontece em seguida. Com vários cartões, os botões ficam alinhados à direita; se você estreitar o painel até um deles não caber, todos descem juntos para baixo do texto. Mais abaixo, cada lane mostra seus itens em ordem e quanto falta:

```
jussara  3 de 5  termina ~21:01
F-191 construindo há 22 min · média da lane 34 min
nanda    2 de 3  termina quando você validar
F-196 aguardando você há 8 min · validação e merge
```

O término da execução é o da lane mais lenta (a jussara); a nanda espera por você. Na CLI continuam os blocos de texto do painel.

### 14.244 Uma faixa começa o próximo item numa sessão nova

A jussara leva o F-1 até a validação e a olinda termina o F-3, o último da sua fila. As duas encerram o turno, e o Claude lê quais sessões trocar:

```
FRESH jussara s1 -> F-2 (sonnet)
DONE olinda s2
```

O Claude abre uma sessão nova para a jussara, em sonnet, para o F-2, registra essa sessão na faixa e arquiva a s1. A olinda não tem mais item, então a s2 só é arquivada. A sua parada do F-1 continua aberta e a resposta chega à sessão nova da jussara. O gasto da faixa passa a somar as duas sessões.

### 14.245 O teste de integração de uma extensão do VS Code: o `/agile:ship` abre um VS Code de verdade

A Ana tem a extensão `review-helper`, criada pelo bootstrap já com o teste de integração. No ship o `docs.check` roda `npm run package` e depois `npm run test:integration`: o `tsc` compila `src/integration/`, o `run.ts` abre um VS Code com a extensão carregada e o `suite.ts`, dentro dele, ativa a extensão, confere que `review-helper.hello` está registrado e o executa.

Na máquina da Ana o VS Code já está instalado, e ela aponta para ele na variável do shell (sem a variável, a primeira execução baixa uma vez o mínimo de `engines.vscode` para `.vscode-test/`). O fim da saída de uma execução:

```
Exit code:   0
integration attempt 1: passed
```

A janela do VS Code abre e fecha sozinha, em cerca de 10 s.

Se uma tentativa falhar, o `run.ts` imprime `integration attempt 1: failed` com o erro, repete uma vez e imprime `integration attempt 2: ...`; a segunda decide o código de saída. Quando a Ana renomeia o comando só em `src/extension.ts`, as duas tentativas falham com `the command review-helper.hello is registered` e o ship fica vermelho, o que os dois testes unitários, que não abrem o VS Code, não percebem.

Um projeto criado antes desta versão não ganha o `src/integration/`: o perfil manda copiar os arquivos, o script, o pacote, as duas linhas de ignorar e o `docs.check` do template à mão.

### 14.246 As faixas respondem em português

A regra `output-style` do seu projeto diz "Talk to the owner in Brazilian Portuguese (pt-BR)". O `/agile:resolve` abre a jussara, e o prompt da faixa agora traz, logo depois da linha que apresenta a faixa:

```
The owner reads this session: write every message to the owner in Brazilian Portuguese (pt-BR), though this prompt is English (#416). Files, commits, issues and the texts you give the orchestrator stay English, as the rules say.
```

A jussara escreve "F-181 está em `idea`; confiro se a F-168, de que ele depende, já foi entregue." em vez da frase em inglês, enquanto o arquivo do item e os commits seguem em inglês. Quando o orquestrador manda a faixa rodar o `relay`, a mensagem termina com "write to the owner in Brazilian Portuguese (pt-BR)".

### 14.247 Um passo abre o Claude Code na worktree e manda fechá-lo antes do ship

O F-7 muda o plugin, e o roteiro pede que você teste a versão da worktree numa sessão do Claude Code aberta nela:

```bash
claude --plugin-dir D:/wt/agile-canary/f-7-sample
```

Na primeira versão do roteiro o passo não dizia para fechar a sessão. O validador que o Claude roda sobre o roteiro aponta o passo:

```
step 1 opens a Claude Code session (`claude --plugin-dir D:/wt/agile-canary/f-7-sample`) and no line at or after it tells the owner to close it before the ship: an open session holds the worktree and worktree.js remove answers HELD (#349)
```

O Claude acrescenta o subpasso "1.2 Feche essa sessão do Claude Code antes do `/agile:ship`.", com o esperado "nenhuma janela do Claude Code aberta na worktree", e o validador responde `OK`. Sem esse subpasso, a sua sessão ainda aberta seguraria a pasta, e o ship pararia em `HELD` ao remover a worktree. Um `claude -p` ou um `claude plugin ...` termina sozinho e não pede o subpasso.

### 14.248 O corpo que perdeu uma seção da issue, parado antes do envio

Na sessão do plugin, o Claude troca a tabela de testes do corpo salvo da issue #298 com um script inline. O `$1` dentro de aspas duplas foi expandido pelo bash e levou junto o título `## Validation script`; a linha 1 continua certa. Antes de o `gh issue edit` sair, a guarda de corpo lê o corpo atual da issue no GitHub, compara os títulos `## ` e responde:

```
Claude: gh issue edit 298 --body-file <scratch>/body-298.md
[agile body-guard] #298 lost ## Validation script (the issue's current body has it) (<scratch>/body-298.md).
This body would replace the item's issue as it is. Write the whole body with the Write tool and read line 1 back before the send (.claude/rules/ship.md, "Editing").
If this body is meant to be sent as it is, ask the owner; with a yes, repeat the command ending with the comment  # agile:body-ok
```

Nada foi enviado. O Claude busca o corpo da issue de novo, refaz a troca com a ferramenta Write e envia; o corpo novo tem todos os títulos do atual e passa sem uma palavra. Uma seção nova no corpo não é perda, e uma seção removida de propósito só sai com o seu sim e o comentário de bypass. Se o GitHub não responde em 5 s, a guarda não compara e o envio segue.

### 14.249 A revisão de um item para na terceira rodada e pede outra abordagem

O F-7 (códigos de desconto) roda no `/agile:autopilot`, sem ninguém olhando. Antes de cada chamada do `reviewer`, o Claude pergunta ao script em que rodada está; depois do relatório, grava a rodada:

```
ROUND 1 of 3: whole diff
- 2026-10-30: review round 1 at 4f1c2a9 — 1 blocker, 2 major, 1 minor
ROUND 2 of 3: since 4f1c2a9
- 2026-10-30: review round 2 at 8b03d17 — 0 blocker, 1 major, 2 minor
- 2026-10-30: review round 3 at c91e5a0 — 0 blocker, 1 major, 0 minor
```

Na rodada 2 o revisor lê só o que mudou desde `4f1c2a9` e confere se os achados da rodada 1 foram corrigidos. A rodada 3 lista só bloqueadores e graves; como sobrou um grave, o relatório termina numa linha `Rethink:`, com o padrão que se repete e outra abordagem. Um defeito vem sempre com todos os lugares da mesma classe, para um conserto só.

Na quarta vez o script não deixa chamar o revisor:

```
CAP: 3 review rounds done (4f1c2a9, 8b03d17, c91e5a0); rethink the approach; a 4th round only after the owner's reset
```

O autopilot para aí (no `/agile:resolve`, é uma parada grave). Num `/agile:review` com você, vem um cartão: `Rethink with /agile:change` ou `One more round`. Só com as suas palavras o Claude abre mais uma contagem, e a linha `- 2026-10-30: review rounds reset — owner: "mais uma rodada"` fica em `## Decisions`.

### 14.250 O `docs/agile/REVIEW.md` diz ao revisor o que é falha e o que ignorar

O F-2 conserta o total de pedidos dentro de `legacy/total.js`, e a regra do projeto diz "nunca `var`". O código antigo de `legacy/` usa `var` em tudo, e reescrever `legacy/` está fora do escopo. O projeto ainda não tem `docs/agile/REVIEW.md`: o `/agile:review F-2` o cria vazio a partir de `docs/agile/templates/review.md`, e o revisor relata o `var` novo:

```
| 1 | rules | major | legacy/total.js:3 | The new loop declares its counter with `var i`. ...
Criteria without a test: none
Ignored by REVIEW.md: none
```

Você rejeita o achado pela segunda vez (já tinha rejeitado o mesmo num item anterior), e o Claude propõe uma linha; com o seu sim, a seção `## Ignore` do `docs/agile/REVIEW.md` ganha:

```
- `var` in files under legacy/ (the old style stays until legacy/ is rewritten).
```

Na revisão seguinte, o mesmo `var` não vira linha da tabela; só é contado:

```
Criteria without a test: none
Ignored by REVIEW.md: 1 (`var` in files under legacy/, at `legacy/total.js:2` and `:3`)
```

Uma linha de `## Failures` faz o contrário: o que ela descreve vem no mínimo como grave. Nenhuma linha cala um bloqueador de segurança ou de trava, nem um critério sem teste. No `/agile:autopilot` a proposta da linha não é feita: quem rejeita é você.

### 14.251 A limpeza do ship lê só o caminho da linha `Worktrees:`

O `CLAUDE.md` do projeto tem a linha que o bootstrap escreve, com o caminho entre crases e uma frase depois. Na raiz das worktrees sobrou `f-12-old-report`, que o git não lista. Antes desta versão, a limpeza lia a frase junto com o caminho e não olhava pasta nenhuma (saída de uma execução de verdade do script nesse cenário):

```
orphans: D:\dev\_scratch\ship-317\fx\wt\app` — one folder per item does not exist, nothing to look at.
agile cleanup DRY-RUN
```

Agora ela lê só o caminho entre as crases e mostra a pasta órfã (mesmo cenário):

```
would remove (after a yes): D:\dev\_scratch\ship-317\fx\wt\app\f-12-old-report
orphans: 1 under D:\dev\_scratch\ship-317\fx\wt\app.
agile cleanup DRY-RUN
```

Sem crases, o caminho termina no primeiro ` (`, travessão entre espaços, `, `, `; ` ou ponto seguido de espaço. Um caminho com espaço no meio é lido inteiro.

### 14.252 Um script inline que o shell mudaria, parado antes de rodar

Na sessão do plugin, o Claude vai filtrar as linhas da tabela de testes do corpo salvo de uma issue com um `node -e` no Bash. As expressões regulares do script têm barras invertidas, e a ferramenta Bash entregaria ao node outra coisa: um `\\` chega como `\`, e o `node --check` continua verde no texto mudado. A guarda de comandos responde antes de o comando rodar:

```
Claude: node -e "const rows = body.split(/\r?\n/).filter(l => /^\| AC/.test(l))"
[agile literal-guard] This command hands node a script text (node -e) holding a backslash: the shell, or the Bash tool, changes it before node reads it (`\\` arrives as `\`, even in single quotes and a quoted heredoc; a backtick or `$(` in double quotes runs as a command), and node --check stays green on the changed text.
rules/core/workflow.md (the Write and Edit tools): write the script with the Write tool to a file and run node <file>.
If it really has to run this way, ask the owner; with a yes, repeat the command ending with the comment  # agile:literal-ok
```

Nada rodou. O Claude escreve o script com a ferramenta Write num arquivo da pasta de rascunho e roda `node <arquivo>`; o arquivo guarda cada barra invertida como foi escrita. Um `node -e` sem barra invertida (nem crase ou `$(` entre aspas duplas) passa sem uma palavra, e no PowerShell um texto entre aspas simples ou num here-string `@'...'@` também.
