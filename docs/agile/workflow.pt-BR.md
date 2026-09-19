# agile@canary — Manual (pt-BR)

> Versão 0.0.33 (rascunho). English: [en](workflow.md).

Sumário
1. Conceitos em dois minutos
2. Papéis
3. Preparando um projeto
4. Quiz do bootstrap
5. Ciclo de vida de uma feature
6. Mudando de ideia
7. O arquivo da feature
8. Definição de pronto
9. Gates de qualidade
10. Board
11. Idiomas e o manual da app
12. Perfis de arquitetura
13. Sessões e pausas
14. Exemplos de workflow
15. Referência rápida
16. Fluxo de cada comando

---

## 1. Conceitos em dois minutos

- **A conversa é onde as ideias amadurecem.** Você e o Claude conversam sobre épicos, features e casos de uso até ficarem claros. Nada é codificado enquanto a feature ainda está em discussão.
- **O disco é a fonte da verdade.** Toda decisão que importa termina num arquivo com cabeçalho `status:`. A conversa pode se perder; os arquivos, não.
- **Uma feature por vez.** Só uma feature fica em `building` ou `validating` a cada momento.
- **Três portões humanos.** A feature é **aprovada** antes do código, **validada na tela** antes do merge e **mergeada** só com o seu OK.
- **Código simples.** O perfil de arquitetura escolhido no bootstrap decide quanta estrutura cada módulo recebe. CRUD continua sendo uma fatia vertical fina.
- **Retorno rápido.** Os hooks compilam e testam só o que mudou. A suíte completa roda uma vez, no ship.

## 2. Papéis

| Você (dono do produto) | Claude (engenheiro) |
|---|---|
| Escreve o brief do produto | Conduz o quiz do bootstrap e registra as decisões |
| Propõe épicos e features na conversa | Registra no board, sem refinamento |
| Responde às perguntas de refinamento e aprova as features | Faz **todas** as perguntas numa rodada, cada uma com recomendação, depois de conferir o código |
| Valida cada feature na tela | Implementa, testa e entrega um roteiro de validação |
| Autoriza os merges | Faz o merge, atualiza o board e o manual da app, conduz a retro |

Subagentes são exceção: um revisor com contexto limpo para mudanças arriscadas, ou trabalho paralelo que não mexe nos mesmos arquivos. Não existe cadeia de papéis por feature.

**Modelos.** O modelo é escolhido por atividade, de propósito: a revisão independente roda no modelo mais forte, as buscas no código no menor, o trabalho mecânico em volume que os testes conferem num modelo intermediário, e a sessão principal no modelo que você escolher. O agente `reviewer` declara o seu modelo padrão; o seu projeto troca esse modelo na seção "Models" do `CLAUDE.md` (pergunta 34 do quiz), nunca editando o plugin.

## 3. Preparando um projeto

1. Instale o plugin:
   ```
   /plugin marketplace add D:\dev\agile-canary
   /plugin install agile@canary
   ```
2. Crie um repositório vazio e escreva `product/brief.md` com o template de brief: problema, usuários, capacidades principais, restrições e o que fica fora do escopo.
3. Rode `/agile:bootstrap`.

## 4. Quiz do bootstrap

O `/agile:bootstrap` lê o `product/brief.md` e faz perguntas em **rodadas por tema**. Para cada pergunta, o Claude traz uma recomendação baseada no brief e questiona respostas que conflitem com ele. As decisões são tomadas juntos; nada é presumido.

| Rodada | Temas |
|---|---|
| 1. Forma | Tipo de app (web, mobile, API), perfil de arquitetura, onde vai rodar |
| 2. Dados | Banco, multi-tenancy, soft delete, auditoria |
| 3. Acesso | Autenticação, RBAC, entitlements/planos (limites de uso, trials, concessões por prazo, códigos promocionais), back office administrativo |
| 4. Integração | Mensageria (nenhuma, em processo, broker), serviços externos, armazenamento de arquivos |
| 5. Experiência | Stack de UI, idiomas (padrão pt-BR, pt-PT e en), acessibilidade, família de ícones, como um item é editado, kit de UI e galeria |
| 6. Operação | Observabilidade, hospedagem, CI, board (GitHub ou Azure), ambientes |
| 7. Qualidade | Tempo máximo de teste por nível, expectativa de cobertura, testes de arquitetura, modelos por atividade |
| 8. Documentação | Documentação técnica gerada do código (diagramas de entidades, dicionário de dados, mapa de rotas, diagrama de módulos) e uma visão geral da arquitetura escrita à mão |

Quando o brief cita uma base de código existente para reaproveitar e você dá acesso a ela, o Claude lê esse código (nunca edita) e usa o que encontra como motivo das recomendações. Depois da rodada 8 vem uma **pergunta de fechamento** — "qual conceito do domínio mais preocupa você, ou o quiz não tocou?" — porque o vocabulário central de um domínio (uma taxonomia, um modelo de entitlement, uma regra de pontuação) raramente cabe numa lista fixa de perguntas. O que ela levantar é decidido como qualquer pergunta do quiz e registrado na ADR-0001.

Saídas, todas em inglês:
- `CLAUDE.md` — curto (até 60 linhas), apontando para um perfil.
- `docs/decisions/ADR-0001-foundation.md` — cada decisão do quiz, com o motivo.
- `docs/agile/profile.md` — cópia do perfil de arquitetura escolhido.
- `docs/agile/workflow.md` e `docs/agile/workflow.pt-BR.md` — este workflow nos dois idiomas, e `docs/agile/templates/` — os templates, copiados para o projeto.
- `docs/glossary.md` — termos de negócio e os identificadores em inglês, e os termos técnicos que o Claude usa em relatórios e revisões (as severidades da revisão, por exemplo) com a palavra em pt-BR que ele usa ao falar com você. Um termo técnico novo ganha uma linha na primeira vez que aparece.
- `docs/infra.md` — como rodar localmente, quais ambientes existem de fato (`provisioned` ou `planned`), os segredos esperados (só os nomes), os passos de release e os tempos medidos de build e testes. Atualizado no ship quando algo disso muda.
- Esqueleto da solução conforme o perfil, já com i18n e os projetos de teste.
- `docs/architecture/` — quando a rodada 8 escolheu algum documento: o `tools/<App>.DocGen` gera diagramas de entidades e dicionário de dados por módulo (do modelo EF), mapa de rotas por área (do documento OpenAPI) e diagrama de módulos (das referências entre projetos), tudo em Mermaid; o `/agile:ship` regenera e o `--check` reprova quando estão desatualizados. Opcionalmente uma visão geral de uma página escrita à mão (C4 contexto e containers).
- Os primeiros épicos no board, se o brief já os citar.

## 5. Ciclo de vida de uma feature

```mermaid
stateDiagram-v2
    [*] --> idea: /agile:idea
    idea --> refining: /agile:refine
    refining --> approved: você diz "aprovo F-n"
    approved --> building: /agile:build
    building --> validating: tabela de cobertura + roteiro de validação
    validating --> building: um ajuste que você reportou
    validating --> done: você diz "validado", depois /agile:ship (merge autorizado)
    done --> [*]
    note right of approved: gate 1 — aprovada
    note right of validating: gate 2 — validada na tela
    note right of done: gate 3 — merge autorizado
```

| Status | O que acontece | Quem muda |
|---|---|---|
| `idea` | Registrada a partir da conversa com `/agile:idea`. Título e 2 ou 3 linhas. | Claude |
| `refining` | `/agile:refine`: o Claude lê o código relacionado e faz todas as perguntas abertas numa rodada, como cartões de quiz agrupados por tema (regras, permissões, estados, telas, dados, pacotes, escopo) com a opção recomendada primeiro; num terminal as mesmas perguntas vêm como lista numerada. A rodada inclui os pacotes novos de que o item precisa, para o código **e para os testes**, para que o seu sim seja dado uma vez e não no meio do build. Você responde; no máximo mais uma rodada. O arquivo da feature é escrito. | Claude |
| `approved` | Você aprova o arquivo da feature depois de lê-lo. Perguntas em aberto impedem a aprovação. **Portão 1.** | Você |
| `building` | `/agile:build`: branch, código e testes do que mudou. Antes da tabela de cobertura o Claude abre a tela pelo app host: os testes não enxergam como a biblioteca de componentes desenha os seus estados (um link ativo sem contraste, um link que não é link). As conferências por teclado ficam no seu roteiro de validação. O Claude nunca muda estado (cadastros, requisições contadas, dados) num app host que ele não abriu: pergunta antes, ou usa dados que ninguém mais usa e diz quais. Só uma feature pode estar aqui. | Claude |
| `validating` | O Claude entrega um roteiro de validação (até 8 passos). Você testa na tela. Um passo que precisa de terminal traz o comando para Git Bash e para PowerShell 7, com a saída esperada e como repetir, e o Claude já rodou os dois. **Portão 2.** | Você |
| `done` | `/agile:ship`: suíte completa, merge com o seu OK (**Portão 3**), board e manual da app atualizados, retro. | Claude |

Pequenas correções encontradas na validação são feitas na hora, sem sair de `validating`.

Antes de entregar o roteiro de validação, o Claude mostra uma tabela **critério → teste**: cada critério de aceite aponta para os seus testes, ou fica explicitamente para o roteiro de validação. O teste precisa passar pelo caminho que o usuário usa (página, endpoint, o handler que chama o código): um método escrito para um critério que nada na app chama é uma lacuna, mesmo que o seu próprio teste passe.

### Etapas opcionais

| Comando | Quando | Resultado |
|---|---|---|
| `/agile:discuss` | Uma ideia com vários caminhos possíveis, ou dúvidas que só você responde | `docs/discussions/D-<n>-<slug>.md` (opções, decisões, pontos adiados) e os itens registrados como `idea` |
| `/agile:epic` | Um épico novo para planejar | `docs/epics/<slug>.md` com features priorizadas, cada uma cabendo numa sessão e registrada como `idea` |
| `/agile:screen` | Uma feature em `refining` com tela nova ou complexa | Seção de tela detalhada no arquivo da feature e um mockup HTML (todos os estados, três idiomas), aprovados junto com a feature |
| `/agile:review` | Uma mudança arriscada (autenticação, permissões, isolamento por tenant, dados, contratos, dinheiro, ou mais de ~400 linhas), antes da validação | Achados por gravidade de um revisor só de leitura e com contexto limpo; os bloqueadores confirmados são corrigidos antes de você validar |

## 6. Mudando de ideia

- **Antes da aprovação:** mude o que quiser. É só conversa.
- **Durante o build:** o `/agile:change` acrescenta uma **nota de mudança** ao arquivo da feature (o que mudou, por quê e quais critérios de aceite são afetados). Você reaprova só esses critérios, e o trabalho continua.
- **Depois do ship:** é uma feature nova ou um bug, registrado com `/agile:idea`.
- **Premissa errada descoberta** (por exemplo, "a tela já tem esse campo" e não tem): o Claude para e pergunta antes de contornar o problema no código.

## 7. O arquivo da feature

`docs/features/F-<número>-<slug>.md`, um por feature, criado a partir de `docs/agile/templates/feature.md`. Bugs usam um arquivo mais curto, `docs/bugs/B-<número>-<slug>.md` (template `bug.md`): o que acontece, o esperado, a causa confirmada (com cada duplicata de uma regra de negócio), a correção (quais ocorrências agora, quais adiadas), um teste de regressão por ocorrência corrigida e o roteiro de validação.

Épicos ficam em `docs/epics/<slug>.md` (tabela de features, ordem, corte da primeira versão) e discussões em `docs/discussions/D-<n>-<slug>.md`. Mockups de tela vão para `docs/features/mockups/`.

Seções principais do arquivo da feature:

```markdown
---
feature: F-<número>
status: refining | approved | building | validating | done
board: <id do work item>
---
# <Nome da feature>

## Goal
## Users and use cases
## Business rules
## Screens and API
## Acceptance criteria
## Decisions (date — decision — reason)
## Out of scope
## Open questions
## Change notes
## Validation script
```

O arquivo é escrito em inglês, como todo o projeto.

## 8. Definição de pronto

- [ ] Critérios de aceite atendidos e cobertos por testes.
- [ ] Build sem avisos novos; testes afetados verdes.
- [ ] Todo texto de tela traduzido em pt-BR, pt-PT e en.
- [ ] Validado na tela por você.
- [ ] Suíte completa verde antes do merge.
- [ ] Documentação técnica gerada em dia (`DocGen --check`), quando o projeto a tem.
- [ ] Manual da app atualizado nos três idiomas.
- [ ] Board atualizado; o arquivo da feature reflete o que foi decidido.

## 9. Gates de qualidade

Os hooks rodam fora do modelo. São scripts Node (sem bash) e não fazem nada em um repositório sem projeto .NET.

| Quando | O que acontece |
|---|---|
| **Início da sessão** | Mostra a branch, o item em andamento, o topo do backlog, os arquivos aprovados com perguntas em aberto e o trabalho sem commit em **todas** as worktrees. |
| **A cada commit** | Uma guarda recusa um `git commit` na branch principal em dois casos: uma branch de item (`feature/F-<n>`, `bug/B-<n>`) está sem merge e não está aberta em nenhum lugar — o sinal de que uma IDE trocou a branch por trás da sessão — ou o commit carrega o arquivo de um item que não está `done` (refining, approved, building, validating), que pertence à branch do item. O Claude avisa você e volta para a branch certa; se o commit for mesmo da branch principal, você confirma e o Claude repete o comando terminando com o comentário `# agile:main-ok`. |
| **A cada edição** | Nada é compilado. O arquivo editado só é anotado, sob a raiz git a que pertence — assim, uma edição dentro de uma worktree é verificada naquela worktree, e não na pasta onde a sessão começou. |
| **Fim do turno** (só se houve mudança de código) | Compila os projetos alterados e roda só os projetos de teste que os referenciam, direta ou indiretamente. Nunca roda a suíte inteira. |
| **Ship** | `gate.js ship`: rebuild completo, suíte completa e testes de arquitetura. |

Detalhes:
- **Só avisos novos.** Os avisos são comparados com `.claude/agile/warnings-baseline.json`, um arquivo versionado. Avisos que já existiam não reprovam o gate; um aviso novo, sim, listado com arquivo, linha e mensagem. A baseline só é reescrita por um ship verde (ou por `gate.js baseline`, com o seu sim).
- **O veredito é a última linha.** Todo relatório termina com `agile gate GREEN` ou `agile gate RED: <o que falhou>` (os avisos novos, os testes que falharam, o build bloqueado). O Claude grava a saída inteira num arquivo e cita dali, sem nunca filtrá-la com `grep`, `head` ou `tail`: uma vez, uma saída filtrada escondeu a única lista de avisos novos.
- **Mudanças amplas.** Se uma mudança alcança mais de 6 projetos de teste, rodam só os que a referenciam diretamente; o resto fica para o ship (`AGILE_GATE_MAX_TESTS`). Uma mudança em `.props`, `.targets` ou na solução compila a solução inteira e deixa os testes para o ship.
- **Busca da solução.** A solução é procurada na raiz git e uma pasta abaixo (`repo/App.slnx`, `src/App.sln`).
- **Saída de build travada.** Um app host, preview ou depurador rodando mantém as DLLs abertas. O gate então informa "build blocked" e o nome do processo, em vez de uma falha de build genérica; o Claude encerra o que ele mesmo iniciou antes do fim do turno e pede que você feche o seu.
- **Testes pendurados.** Um teste que roda por mais de 120 segundos (`AGILE_GATE_HANG_TIMEOUT`) conta como pendurado: a execução falha com o nome dele, em vez de travar o turno por minutos.
- **Sem loops.** Depois de 3 gates vermelhos seguidos, o turno termina e você vê a falha; a verificação pendente fica para o próximo turno.
- **Limite conhecido.** Só as edições feitas com as ferramentas de edição são rastreadas. Mudanças feitas por comando de shell (`dotnet format`, um merge) são pegas no ship.
- `AGILE_HOOKS=off` desliga os hooks em uma sessão.

## 10. Board

O `CLAUDE.md` tem uma linha `Board:`: GitHub Issues + Projects (`gh`), Azure Boards (`az boards`) ou nenhum (nesse caso, usa `docs/agile/backlog.md`). Mapeamento: épico → feature. Sem Tasks por papel. O arquivo da feature guarda o id do board, e o merge fecha o work item.

## 11. Idiomas e o manual da app

- Código, documentação, commits e identificadores em inglês.
- A app sai em **pt-BR, pt-PT e en** desde o primeiro dia: nenhum texto de tela fixo no código, um arquivo de recursos por módulo e por idioma, e `IStringLocalizer`. Acrescentar um idioma é só acrescentar arquivos de recursos.
- O **manual da app** fica em `docs/manual/<idioma>/` (pt-BR, pt-PT, en) e é atualizado no ship de cada feature.
- Só a conversa entre você e o Claude é em português.

## 12. Perfis de arquitetura

O quiz escolhe um perfil, e o `CLAUDE.md` aponta para ele.

| Perfil | Quando usar |
|---|---|
| `modular-monolith` | Um único deploy, com vários módulos de negócio e fronteiras claras |
| `monolith` | Um único deploy, com uma só área de negócio |
| `web-app` | Principalmente telas sobre dados simples; API fina ou UI no servidor |
| `microservices` | Deploys independentes, com donos separados; só com motivo real |
| `mobile` | Cliente nativo ou MAUI, com o seu próprio perfil de API |

Cada perfil define o layout de pastas, onde ficam as regras de negócio, a estratégia de testes com o tempo máximo e os testes de arquitetura. Os arquivos de perfil ficam em `profiles/` no plugin; o bootstrap copia o escolhido para `docs/agile/profile.md`.

O que todos os perfis têm em comum:
- **Código simples.** CRUD é uma fatia vertical fina (endpoint ou página → `DbContext`): sem repository, sem mediator, sem biblioteca de mapeamento. A regra fica na entidade, ou no handler da feature quando precisa de dados. Estrutura só entra no segundo uso.
- **Um projeto por módulo.** No `modular-monolith`, um módulo é um projeto mais um projeto `Contracts`. Um módulo com domínio rico pode usar a variante Clean Architecture (cinco projetos: Domain, Application, Infrastructure, Api, Contracts); o Claude sugere com o motivo, você decide por módulo, e uma ADR registra.
- **Tempo máximo.** Unitários < 30 s, integração < 2 min por projeto de teste, suíte completa < 5 min (menos em `web-app` e `monolith`, < 10 min em `microservices`). **Um container PostgreSQL por projeto de teste**, nunca um por classe de teste.
- **Testes de arquitetura** protegem as fronteiras e o vocabulário em inglês em todos os assemblies, e toda regra de ausência vem com uma regra de presença, para que um assembly vazio reprove. Um teste de layout mantém cada projeto na pasta que o perfil lhe dá.

**Estrutura de pastas por perfil.** O bootstrap cria a estrutura, e todo projeto que uma feature acrescenta depois nasce na pasta que o tipo dele tem aqui; as pastas da solução espelham as pastas do disco, e um teste de layout reprova quando um projeto cai em outro lugar. `<App>` é o nome do seu app.

`modular-monolith` — hosts, blocos técnicos e módulos de negócio em grupos separados:
```
src/
├── Hosts/
│   ├── <App>.AppHost/                orquestração local (Aspire)
│   ├── <App>.ServiceDefaults/        telemetria, health, resiliência
│   ├── <App>.Api/                    só host: composição, auth, OpenAPI
│   └── <App>.Web/                    UI Blazor, clientes tipados para a API
├── BuildingBlocks/
│   ├── <App>.SharedKernel/           Entity, TenantEntity, Result/Error, AppJson
│   └── <App>.<Block>/                Persistence, Email, Storage, Ai — pela primeira feature que precisar
└── Modules/
    └── <Module>/
        ├── <App>.<Module>/           um projeto por módulo (ou cinco com Clean Architecture)
        │   ├── Features/<Feature>/   fatias verticais
        │   ├── Domain/               entidades com regras
        │   ├── Data/                 DbContext, schema próprio, migrations
        │   └── Resources/            .resx em en, pt-BR, pt-PT
        └── <App>.<Module>.Contracts/ o que os outros módulos podem ver
tests/
├── Hosts/<App>.Web.Tests/            bUnit
├── BuildingBlocks/<App>.<Block>.Tests/
├── Modules/<Module>/<App>.<Module>.Tests/
├── <App>.ArchitectureTests/          fronteiras, vocabulário, layout
└── <App>.Testing/                    auxiliares de teste compartilhados
```

`monolith` — um deployable com o código de negócio dentro dele:
```
src/
├── <App>.AppHost/                    opcional
├── <App>.Api/                        host + código de negócio
│   ├── Features/<Feature>/
│   ├── Domain/
│   ├── Data/                         um DbContext, migrations
│   ├── Contracts/                    records compartilhados com a UI
│   ├── Common/                       Result/Error, AppJson, auxiliares
│   └── Resources/
└── <App>.Web/                        UI Blazor, clientes tipados para a API
tests/
├── <App>.Tests/                      unitários + integração
└── <App>.Web.Tests/                  bUnit
```

`web-app` — um projeto Blazor é o app inteiro:
```
src/
└── <App>.Web/                        o único deployable
    ├── Pages/<Area>/                 página + code-behind
    ├── Components/                   componentes de UI compartilhados
    ├── Features/<Feature>/           só quando uma operação tem regras
    ├── Domain/
    ├── Data/                         um DbContext, migrations
    ├── Api/                          endpoints só para clientes externos
    ├── Common/                       Result/Error, AppJson, auxiliares
    └── Resources/
tests/
└── <App>.Tests/                      unitários + integração + bUnit
```

`microservices` — uma solução, uma pasta por serviço, cada um um monolito pequeno:
```
src/
├── <App>.AppHost/                    todos os serviços, broker, bancos
├── <App>.ServiceDefaults/
├── <App>.Gateway/                    roteamento e auth na borda (YARP)
├── <App>.Web/                        UI Blazor, chama só o gateway
├── BuildingBlocks/
│   └── <App>.Messaging/              outbox, inbox, configuração do broker
└── Services/
    └── <Service>/
        ├── <App>.<Service>/          Features/, Domain/, Data/ (banco próprio)
        └── <App>.<Service>.Contracts/ records e eventos de integração
tests/
├── <App>.<Service>.Tests/            unitários + integração + contrato
├── <App>.Web.Tests/                  bUnit
├── <App>.ArchitectureTests/
└── <App>.SystemTests/                alguns fluxos ponta a ponta
```

`mobile` — um núcleo testável, uma cabeça MAUI fina e o backend no perfil dele:
```
src/
├── <App>.Api/                        backend, pelo perfil dele
├── <App>.Contracts/                  records e códigos de erro compartilhados com o app
├── <App>.Mobile.Core/                biblioteca simples: tudo que é testável
│   ├── Features/<Feature>/           view models, serviços de feature
│   ├── Api/                          clientes tipados, AppJson
│   ├── Storage/                      cache local, fila offline
│   └── Resources/
└── <App>.Mobile/                     cabeça MAUI: páginas XAML, shell, código de plataforma
tests/
├── <App>.Mobile.Core.Tests/          view models, serviços, fila offline
└── <App>.Tests/                      testes do backend, pelo perfil dele
```

As regras comuns ficam em `rules/core/` e são copiadas para `.claude/rules/agile/` no bootstrap: `workflow`, `naming`, `git`, `definition-of-done` e `output-style` são sempre carregadas; `i18n` e `api-contracts` só são carregadas quando o Claude trabalha em arquivos de código, `ui` só em arquivos de tela (`.razor`, `.xaml`), e `build-config` só em arquivos de projeto e de build. Uma regra por linha, no máximo 30 linhas por arquivo. As regras core são genéricas: valem para todos os perfis. O que depende de um perfil, de uma stack ou de uma biblioteca de UI fica no arquivo do perfil, em `templates/dotnet/` ou numa regra limitada por tipo de arquivo, e o plugin aplica isso a todos os perfis a que diz respeito.

**Regras que o build confere.** O bootstrap copia `templates/dotnet/` para a raiz da solução, em todos os perfis: `Directory.Build.props` (configurações comuns, estilo de código cobrado no build), `Directory.Packages.props` (todas as versões de pacote em um só lugar), `.editorconfig` (as regras do dono: nomes, chaves em todo bloco, pattern matching, membros com corpo de expressão, formatação), `BannedSymbols.txt` (APIs proibidas, como `new JsonSerializerOptions`, `DateTime.Now`, `Thread.Sleep`) e `global.json`. Uma regra quebrada vira aviso de build, e o gate reprova avisos novos — assim a regra vale mesmo quando ninguém lembra de ler. O `TreatWarningsAsErrors` fica desligado. Quando uma lição de retro pode ser conferida pelo build, ela vai para lá primeiro.

**Lições da stack.** Uma lição de um item real que depende da stack vai para a regra restrita àqueles arquivos ou para os perfis a que diz respeito, nunca para uma regra sempre carregada. Exemplos da primeira app: a `api-contracts` diz que um middleware próprio resolve uma dependência opcional dentro do ramo que precisa dela (um parâmetro do `InvokeAsync` é resolvido em toda requisição) e que um cliente pede à API os claims do usuário em vez de decodificar o seu access token (ele pode ser criptografado); a `build-config` diz que uma investigação que depende de versão lê a versão resolvida em `obj/project.assets.json`, não a fixada; os perfis com AppHost do Aspire dizem que o código chega a Redis, PostgreSQL ou a um broker pela integração de cliente do Aspire, porque os recursos locais rodam com TLS por padrão, e que o `ServiceDefaults` gerado desliga as novas tentativas de POST (um POST repetido reusa tokens de uso único); os perfis com UI Blazor dizem que, no Interactive Server, estado que muda durante a sessão fica num store no servidor, com a chave num id do cookie, porque um circuito não consegue reescrever o cookie; e a estratégia de testes dos perfis diz que um teste bUnit espera o que um handler assíncrono de clique faz (`WaitForAssertion`), e que um host de teste criado por classe de teste limpa o pool do Npgsql ao ser descartado, senão o banco de teste fica sem conexões; e, no Interactive Server, um dado da primeira requisição que o circuito vai precisar (o endereço do visitante, um cabeçalho) é lido no `App` e passado ao componente raiz interativo, porque o circuito não tem `HttpContext`. A `api-contracts` também diz que um endpoint anônimo que precisa dar a mesma resposta em todos os caminhos faz todas as conferências de entrada antes da consulta que diferencia esses caminhos.

**Um comportamento só em todas as telas.** A regra `ui` impede que as telas se afastem umas das outras: um padrão é definido uma vez, num kit de UI mostrado numa página de galeria só de desenvolvimento, e as páginas usam o kit em vez dos componentes crus da biblioteca. Uma família de ícones atrás de nomes semânticos (`AppIcons.Edit`), um jeito só de editar um item (ações de linha na última coluna; diálogo para entidades simples, página própria para as complexas; o link no nome só abre uma página de detalhe para leitura), o mesmo hover e o mesmo foco em todo lugar, um diálogo único de confirmação para ações destrutivas, e o mesmo feedback, estados de lista, layout de formulário e vocabulário de ações. Testes de arquitetura proíbem ícones e tabelas crus fora do kit, e o kit vem antes da primeira tela. Um parâmetro do kit cujo valor certo depende do que a página significa (autocomplete, o texto de uma ação destrutiva) é obrigatório, então uma página que o esquece não compila. Um problema de cor ou contraste é conferido nos dois temas e em toda superfície onde o componente aparece, calculado pelo tema e depois medido na tela. O projeto registra as suas escolhas (família de ícones, exceções declaradas) nas suas próprias regras.

O Claude escreve todo arquivo (código, testes, docs) com as suas ferramentas de edição, nunca pelo texto de um script: escapes como `\t` ou `\b` viram caracteres de controle, e um teste pode passar sem conferir nada.

**Mantendo um projeto atualizado.** O bootstrap copia arquivos do plugin para dentro do projeto (regras, templates, este workflow, o perfil, os arquivos de build), então uma atualização do plugin não chega a eles sozinha. Atualize o plugin (`claude plugin marketplace update canary`, `claude plugin update agile@canary`, sessão nova) e rode `/agile:sync` no projeto. O Claude mostra uma tabela do que mudou e só copia o que você aprovar: uma cópia que você nunca editou é substituída; um arquivo que você editou (normalmente o perfil) é mesclado à mão, mantendo as suas seções; os arquivos de build (`Directory.Build.props`, `.editorconfig`, `global.json`...) nunca são copiados por cima — cada diferença é proposta como uma edição; as suas regras próprias (`project.md`, `*-project.md`) e o `CLAUDE.md` nunca são tocados. Se arquivos de build ou regras conferidas pelo build mudaram, o Claude compila, roda a suíte completa e refaz a baseline de avisos. A versão e o que foi copiado ficam registrados em `.claude/agile/sync.json`. Rode entre features, não no meio de uma. O `/agile:version` mostra a versão do plugin em uso na sessão ao lado da versão do projeto, e diz se o próximo passo é um sync ou uma atualização do plugin.

A `output-style` define como o Claude fala com você: em pt-BR, com a resposta primeiro, relatórios de passo com no máximo 10 linhas, detalhes no arquivo e não no chat, uma recomendação com o motivo, sem narrar o trabalho, "não verificado" dito com essas palavras e a má notícia primeiro. Respostas longas só quando um gate falhou, quando uma pergunta precisa de contexto ou quando você pedir.

## 13. Sessões e pausas

- **Início:** o Claude confere a branch, o board e a feature em andamento antes de qualquer ação.
- **Retomada:** `/agile:build <id>` no item que está em `building` continua a partir do último commit `wip`.
- **Pausa no meio da feature:** `/agile:pause`, ou só diga que vai parar. O Claude faz o commit na branch da feature com o prefixo `wip(F-<n>):` e escreve uma nota (onde paramos, o que vem a seguir, quem decide); nada fica só no disco. Fechar a sessão sem pausar também não perde nada: a sessão seguinte commita o que sobrou como `wip` antes de tudo.
- **Fim:** uma nota curta (onde paramos, o que vem a seguir, quem decide).
- **Antes do merge a partir de uma worktree:** feche a IDE e o app host que estiverem rodando daquela pasta.

**Dois itens em paralelo.** O padrão é um item por vez. Quando você quiser mesmo um segundo item andando — uma feature longa numa sessão e um bug em outra — digite `/agile:build <id> --worktree`. A flag é o seu pedido de trabalho em paralelo; o Claude nunca cria uma worktree por conta própria. Antes, ele diz se os dois itens podem colidir (mesmo schema de módulo, mesma tela, mesmo contrato) e recomenda fazer em sequência quando colidem. Depois cria a branch numa pasta separada, fora do repositório (`Worktrees:` no `CLAUDE.md`; padrão `<pasta pai do repositório>/wt/<repositório>/<tipo>-<n>`, curta por causa do limite de caminho do Windows) e trabalha só ali: um escritor por worktree. O status do item fica nessa worktree até o merge, e o `/agile:status` e o início de sessão leem todas as worktrees. Só um app host roda por vez (as portas colidem); arquivos locais ignorados pelo git não vêm junto. No ship, a checagem completa roda na worktree e o merge no checkout principal; depois do merge a worktree e a branch são removidas, nunca com `--force` sem perguntar, e o outro item em andamento é atualizado com a branch principal. Limite recomendado: dois itens — você valida cada um na tela, e esse é o gargalo de verdade.

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
Claude: Criei a feature F-3 "Exam board registration" (board #12), no épico "Catalog".
Status: idea. Nada mais acontece até a refinarmos.
```

**Refinamento.** Quando chegar a vez desta feature:

```
> /agile:refine F-3
```

```
Claude: Conferi o código: o módulo Catalog existe, mas ainda não tem entidades.
O modelo de usuários e papéis do bootstrap tem Curator e Admin.
```

As perguntas vêm como cartões de quiz, um por tema, com a opção recomendada primeiro. No app desktop você clica; num terminal as mesmas perguntas chegam como lista numerada, respondida com "ok" ou uma letra. O primeiro cartão, "Regras":

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
Pronto para o merge de feature/F-3 em main. Autoriza?
> sim
Claude: Mergeado. Board #12 fechado. Manual atualizado: página "Exam boards" em pt-BR, pt-PT e en.
Retro: uma lição — "conferir todos os arquivos de idioma ao criar uma mensagem". Viro regra?
> sim
Claude: Regra acrescentada em core/i18n. Status: done.
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
| Id   | Feature                 | Prioridade | Tamanho | Depende de | Design de tela |
| F-12 | Question difficulty     | Must       | P       | -          | não            |
| F-13 | Practice test by level  | Must       | M       | F-12       | sim            |
| F-14 | Practice test results   | Must       | M       | F-13       | sim            |
| F-15 | Retry wrong answers     | Should     | P       | F-14       | não            |
Ordem: F-12, F-13, F-14, F-15. Corte da primeira versão: F-12 a F-14.
1. A F-15 entra na primeira versão? → Recomendação: não.
> ok
Claude: docs/epics/practice-tests.md acordado. F-14 e F-15 registradas. Próximo: /agile:refine F-12.
```

No arquivo, o tamanho é gravado como S / M / L, em inglês.

### 14.8 Design de tela e revisão

Durante o refinamento da F-13:

```
> /agile:screen F-13
Claude: Mockup enviado: docs/features/mockups/F-13-practice-test-by-level.html
Estados: carregando, sem questões no nível, pronto, respondendo, tempo esgotado, concluído, erro.
Decisões: uma questão por página (bom no celular); cronômetro só quando o simulado tem limite.
1. O aluno pode voltar a uma questão anterior? → Recomendação: sim, até concluir.
> sim
> aprovo F-13
```

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
Claude: A F-6 está em build neste checkout (feature/F-6). O B-2 mexe só em Identity/Login;
a F-6 mexe em Catalog. Sem schema nem tela em comum: podem andar em paralelo.
Worktree: D:\dev\wt\examprep\bug-2 (fora do repositório). Posso criar?
> sim
Claude: Criada bug/B-2 em D:\dev\wt\examprep\bug-2, status building (nessa branch).
Só um app host por vez: pare o do checkout principal antes de eu abrir a tela aqui.
```

No início de qualquer sessão, o status mostra os dois:

```
[agile] branch feature/F-6
In progress: F-6 Practice test by level — building (docs/features/F-6-...)
In progress in a worktree: B-2 — building [bug/B-2] D:/dev/wt/examprep/bug-2
```

No ship do bug, a checagem completa roda na worktree e o merge no checkout principal:

```
> /agile:ship B-2
Claude: Feche a IDE e o app host que estiverem rodando de D:\dev\wt\examprep\bug-2 e diga "fechado".
> fechado
Claude: Suíte completa verde na worktree (212 testes, 1 min 58 s). Pronto para o merge de bug/B-2 em main. Autoriza?
> sim
Claude: Merge feito. Worktree removida, branch apagada, 0 0 contra origin/main. A F-6 ficou 1 commit atrás
da main: vou atualizá-la antes da próxima rodada de testes.
```

### 14.10 Atualizando o plugin e o projeto

Uma lição para o plugin nasce na retro de um projeto: vira uma linha ⏳ em "Plugin notes — status" no `docs/agile/retro-log.md`. Na sessão do plugin:

```
> vamos ajustar o plugin com as recomendações em retro-log.md
Claude: 2 notas em aberto (F-8). Feito na 0.0.29: espera do bUnit (4 perfis), pool do Npgsql (5 perfis),
manuais en/pt-BR. Linhas marcadas ✅ no retro-log.md (sem commit).
1. Push do plugin?  2. Atualizar o plugin instalado?  3. Commit no projeto?
> pode fazer o push, atualizar o plugin e o commit no simulab
```

Cada um dos três é pedido separadamente, assim como o push do projeto. O projeto só recebe os arquivos novos pelo `/agile:sync`, entre features.

O plugin passou de 0.0.10 para 0.0.13. Primeiro o plugin em si (terminal, depois uma sessão nova):

```
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
Nada mais mudou. Seus project.md, ui-project.md e CLAUDE.md não são tocados.
> ok
Claude: Copiados 2, mesclado 1 (sem conflitos), .editorconfig editado. Build com 0 avisos; 252 testes verdes (9 s).
Registrei 0.0.13 em .claude/agile/sync.json. Commit "chore: sync with agile@canary 0.0.13" na main. Autoriza?
```

A partir da 0.0.23 o template traz o `.editorconfig` definitivo. Num projeto que ainda tem o básico, a linha do `.editorconfig` lista os novos grupos de regras (chaves, pattern matching, membros com corpo de expressão, formatação) e o Claude propõe um grupo de cada vez; código que quebra uma regra aprovada aparece como aviso no build que vem depois do sync.

### 14.11 Quando um gate diz não

**Um app rodando trava o build.** O Claude abriu a tela para conferir e esqueceu o app host:

```
[agile gate] build blocked: ExamPrep.slnx
the build output is locked by a running process (ExamPrep.Web (18344)): stop the app host, preview or
debugger started from this folder, then build again.
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

## 15. Referência rápida

Você só digita os comandos abaixo. Cada um carrega uma skill com o procedimento completo (por exemplo, `/agile:bootstrap` carrega a `bootstrap-quiz`); as skills ficam ocultas do menu `/` e é o Claude quem as carrega.

| Comando | Uso |
|---|---|
| `/agile:bootstrap` | Quiz a partir do brief → `CLAUDE.md`, ADR, perfil e esqueleto |
| `/agile:discuss "<ideia>"` | Explora uma ideia: opções, decisões, itens registrados |
| `/agile:epic "<nome>"` | Quebra um épico em features priorizadas |
| `/agile:idea "<texto>"` | Registra um épico, feature ou bug, sem refinamento |
| `/agile:refine <feature>` | Rodada de refinamento → arquivo da feature para aprovação |
| `/agile:screen <feature>` | Detalhe de tela e mockup HTML durante o refinamento |
| `/agile:build <feature> [--worktree]` | Implementa uma feature aprovada (uma por vez; `--worktree` para uma segunda em paralelo) |
| `/agile:review <feature>` | Revisão com contexto limpo de uma mudança arriscada |
| `/agile:change <feature>` | Registra uma mudança de ideia durante o build |
| `/agile:ship <feature>` | Suíte completa, merge, board e manual da app |
| `/agile:retro` | Transforma lições em regras ou skills |
| `/agile:pause [nota]` | Parar por agora: commit wip na branch do item e uma nota de onde paramos |
| `/agile:status` | Feature em andamento, topo do backlog e perguntas em aberto |
| `/agile:sync` | Depois de atualizar o plugin: renova as cópias de regras, templates, workflow e perfil dentro do projeto |
| `/agile:version` | Versão do plugin em uso nesta sessão, a versão de onde vieram as cópias do projeto, e o próximo passo quando diferem |

## 16. Fluxo de cada comando

Um diagrama por comando: o que você faz, o que o Claude faz e onde ele para para perguntar. Retângulos são passos do Claude, losangos são decisões suas, caixas arredondadas são o resultado.

### /agile:bootstrap
```mermaid
flowchart TD
    A["product/brief.md existe?"] -->|não| A1(["Template copiado; preencha e rode de novo"])
    A -->|sim| B["Ler o brief e a base de código que ele cita"]
    B --> C["Rodadas 1 a 8, uma mensagem cada:<br/>perguntas com recomendação e motivo"]
    C --> D{"Suas respostas ('ok' aceita)"}
    D --> E["Pergunta de fechamento: qual conceito do domínio mais preocupa?"]
    E --> F["Resumo de todas as decisões"]
    F --> G{"'confirmo'?"}
    G -->|não| C
    G -->|sim| H["Gerar: CLAUDE.md, ADR-0001, perfil, cópias do workflow,<br/>templates, regras, glossário, infra, índice do manual"]
    H --> I{"Épicos no board? Esqueleto?"}
    I -->|sim| J["Criar, compilar, testar uma vez, baseline de avisos, registro do sync"]
    J --> K{"Autoriza o primeiro commit?"}
    K -->|sim| L(["Commit na main; próximo: /agile:epic ou /agile:refine"])
```

### /agile:idea
```mermaid
flowchart TD
    A["Texto da conversa"] --> B["Classificar: épico, feature ou bug"]
    B --> C["Achar o épico"]
    C -->|nenhum serve| C1{"Propor um épico; concorda?"}
    C1 -->|sim| D
    C --> D["Próximo id, slug em inglês, arquivo do template<br/>com status: idea; só cabeçalho e Summary"]
    D --> E["Espelhar no board; id do board no cabeçalho"]
    E --> F(["Nada mais até /agile:refine <id>"])
```

### /agile:discuss
```mermaid
flowchart TD
    A["Uma ideia ainda sem forma"] --> B["Contexto: brief, decisões, o código que ela toca"]
    B --> C["Opções com trade-offs, uma recomendação"]
    C --> D{"Sua escolha, ou mais perguntas"}
    D -->|decidir| E["docs/discussions/D-n: decisões, pontos estacionados"]
    D -->|estacionar| E
    E --> F["Itens que surgiram são capturados como idea"]
    F --> G(["Sem spec, sem código; próximo: /agile:refine ou /agile:epic"])
```

### /agile:epic
```mermaid
flowchart TD
    A["Um épico para planejar"] --> B["Ler brief, itens existentes e código"]
    B --> C["Features do tamanho de uma sessão: valor, prioridade,<br/>tamanho, dependências, design de tela sim/não"]
    C --> D["Ordem e corte da primeira release"]
    D --> E{"Concorda com a quebra?"}
    E -->|mudar| C
    E -->|sim| F["docs/epics/<slug>.md; cada feature capturada como idea<br/>e espelhada no board"]
    F --> G(["Próximo: /agile:refine <primeira feature>"])
```

### /agile:refine
```mermaid
flowchart TD
    A["Item em idea ou refining"] --> B["status: refining; ler brief, perfil, código que toca"]
    B --> C["Conferir cada premissa no código"]
    C -->|premissa falsa| C1["Dizer isso primeiro"]
    C1 --> D
    C -->|um bug| C2["Causa no código, arquivo:linha; regra de negócio:<br/>procurar na solução a mesma regra implementada de novo"]
    C2 --> C3["Escrever ## Cause com todas as ocorrências; reler o arquivo"]
    C3 --> D
    C --> D["Uma rodada como cartões de quiz, um por tema, opção recomendada primeiro:<br/>casos de uso, regras (duplicatas: corrigir agora ou dívida), permissões, estados, telas, dados,<br/>pacotes do código e dos testes, fora de escopo"]
    D --> E{"Suas respostas"}
    E --> F["Preencher o arquivo: UC, BR, telas e API,<br/>critérios de aceite, decisões, fora de escopo"]
    F --> G{"Precisa de no máximo mais uma rodada?"}
    G -->|sim| D
    G -->|não| H["Resumo; pedir que você leia o arquivo"]
    H --> I{"'aprovo F-n' e nenhuma pergunta aberta?"}
    I -->|não| H
    I -->|sim| J(["status: approved; board e commit; próximo: /agile:build"])
```

### /agile:screen
```mermaid
flowchart TD
    A["Feature em refining com tela nova ou complexa"] --> B["Ler o arquivo da feature, o kit de UI e a galeria"]
    B --> C["Seção de tela: layout, estados, ações, mensagens, permissões"]
    C --> D["Mockup HTML autônomo: todos os estados,<br/>três idiomas, só padrões do kit"]
    D --> E{"Suas perguntas e mudanças"}
    E -->|mudar| C
    E -->|ok| F(["Aprovado junto com a feature"])
```

### /agile:build
```mermaid
flowchart TD
    A["Item aprovado?"] -->|já em building| A2["Retomar: branch, último commit wip, próximo passo do plano"]
    A2 --> D
    A -->|não| A1(["Parar: refinar e aprovar antes"])
    A -->|sim| B["Outro item em building ou validating?"]
    B -->|sim, sem --worktree| B1(["Parar e dizer qual"])
    B -->|sim, com --worktree| B2{"Os dois podem colidir? Confirmar o paralelo"}
    B2 -->|sim| C2["Worktree fora do repositório; trabalhar só lá"]
    B -->|não| C["Branch feature/F-n; status: building; plano em 8 passos"]
    C2 --> D
    C --> D["Código pelo perfil e pelas regras; testes por critério;<br/>um bug: um teste de regressão por ocorrência corrigida;<br/>só testes afetados; commits pequenos, branch conferida antes"]
    D --> E{"Premissa falsa ou critério impossível?"}
    E -->|sim| E1["Opções A/B → /agile:change"]
    E1 --> D
    E -->|não| F["Conferência pelo app host; tela nos dois temas"]
    F --> G["Encerrar app hosts que abriu; build sem avisos novos"]
    G --> H["Tabela de cobertura: critério → teste pelo caminho que o usuário usa;<br/>um método que nada chama é lacuna"]
    H --> I{"Mudança arriscada?"}
    I -->|sim| I1["/agile:review"]
    I1 --> J
    I -->|não| J["Roteiro de validação, no máximo 8 passos"]
    J --> K(["status: validating; você valida na tela"])
```

### /agile:review
```mermaid
flowchart TD
    A["Mudança arriscada antes da validação"] --> B["Commit base = merge base com a main"]
    B --> C["Agente revisor, contexto limpo, modelo de revisão:<br/>arquivo do item, perfil, regras, diff — só caminhos"]
    C --> D["Achados: bloqueador / grave / leve, arquivo:linha"]
    D --> E["Claude confere cada bloqueador e grave no código"]
    E --> F["Tabela: achado, veredito, ação"]
    F --> G{"Sua triagem"}
    G --> H["Corrigir bloqueadores e graves confirmados; testes; rodar de novo"]
    H --> I(["Uma linha por achado em ## Decisions; o build continua"])
```

### /agile:change
```mermaid
flowchart TD
    A["Mudança de ideia, ou premissa errada, em item aprovado ou em build"] --> B["Nota de mudança: o que mudou, por quê,<br/>quais critérios são afetados"]
    B --> C["Nova versão do arquivo; critérios afetados reescritos"]
    C --> D{"Reaprovar só os critérios afetados"}
    D -->|sim| E(["O trabalho continua; o resto segue aprovado"])
```

### /agile:ship
```mermaid
flowchart TD
    A["status: validating e você disse 'validado'?"] -->|não| A1(["Perguntar"])
    A -->|sim| B["Branch atualizada com a main; worktree: fechar IDE e app host"]
    B --> C["gate.js ship: rebuild completo, suíte completa, testes de arquitetura;<br/>saída gravada inteira, última linha GREEN ou RED: o que falhou"]
    C -->|vermelho| C1["Corrigir na branch"]
    C1 --> C
    C -->|verde| D["Manual da app em pt-BR, pt-PT, en; docs técnicas regeneradas; infra.md; baseline"]
    D --> E{"Autoriza o merge na main?"}
    E -->|não| E1(["Esperar"])
    E -->|sim| F["Merge --no-ff; push; verificar 0 0,<br/>branch e worktree removidas"]
    F --> G["Decisões que citam arquivo estão nele; ## Delivery; status: done"]
    G --> H["Fechar o item do board com evidência"]
    H --> I["Retro: no máximo 3 lições"]
    I --> J(["Próximo item no topo do backlog"])
```

### /agile:retro
```mermaid
flowchart TD
    A["Um item shippado, ou uma sessão"] --> B["No máximo 3 lições que mudam o trabalho futuro"]
    B --> C["Classificar cada uma: regra do projeto, configuração,<br/>verificação de build, ajuste de template, nota do plugin, nada"]
    C --> D["Mostrar lições com destino e linha exata"]
    D --> E{"Aprovar cada uma"}
    E --> F["Aplicar; CLAUDE.md abaixo de 60 linhas; uma linha por regra"]
    F --> G["Entrada no retro-log.md; nota do plugin ganha linha ⏳<br/>na tabela de status, com o escopo"]
    G --> H(["Commit com a sua autorização se for na main"])
```

### /agile:pause
```mermaid
flowchart TD
    A["/agile:pause, ou 'vamos parar' com quaisquer palavras"] --> B["git status no checkout e em cada worktree"]
    B -->|nada a salvar| B1(["Uma linha: nada em andamento"])
    B --> C{"Trabalho sem commit numa branch de item?"}
    C -->|na main| C1(["Parar e mostrar o git status"])
    C -->|sim| D["Branch conferida; commit wip(F-n): o que ficou pela metade"]
    D --> E["Nota de pausa: onde paramos, próximo passo,<br/>decisões pendentes, o que fechar"]
    E --> F(["Status inalterado; retomar com /agile:build <id>"])
```

### /agile:status
```mermaid
flowchart TD
    A["Somente leitura"] --> B["Git: branch, arquivos sem commit,<br/>cada worktree e o status do item dela"]
    B --> C["Arquivos: item em andamento, aprovados com perguntas abertas"]
    C --> D["Board: top 5, divergência com os arquivos"]
    D --> E["Retro log: notas ⏳ do plugin, contagem"]
    E --> F(["Relatório curto: em andamento, próximo passo, esperando você,<br/>trabalho sem commit, topo do backlog, divergência do board"])
```

### /agile:sync
```mermaid
flowchart TD
    A["Plugin atualizado; árvore limpa; nenhum item em andamento?"] -->|não| A1{"Continuar mesmo assim?"}
    A1 -->|não| A2(["Esperar o item terminar"])
    A -->|sim| B["sync.js plan: versão registrada → versão do plugin;<br/>um estado por arquivo"]
    A1 -->|sim| B
    B -->|tudo igual| B1(["Nada a fazer"])
    B --> C["Tabela: new, update, edited, manual; uma proposta por arquivo"]
    C --> D{"Aprovar por arquivo"}
    D --> E["Copiar new e update; mesclar edited à mão;<br/>propor cada edição manual; nunca tocar nas regras do projeto"]
    E --> F{"Arquivos de build ou regras conferidas mudaram?"}
    F -->|sim| G["Build, suíte completa, corrigir achados, baseline"]
    F -->|não| H
    G --> H["sync.js record; entrada no retro-log"]
    H --> I{"Autoriza o commit na main?"}
    I -->|sim| J(["chore: sync with agile@canary <versão>"])
```

### /agile:version
```mermaid
flowchart TD
    A["Ler a versão em uso (plugin.json),<br/>a instalada (claude plugin list)<br/>e a do projeto (sync.json, cabeçalho do workflow)"] --> B{"Mesma versão?"}
    B -->|sim| B1(["Em dia"])
    B -->|a em uso é mais nova| C(["Próximo passo: /agile:sync, entre features"])
    B -->|a do projeto é mais nova| D(["Atualizar o plugin, depois uma sessão nova"])
```
