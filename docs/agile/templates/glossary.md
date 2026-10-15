<!--
Business vocabulary and its English identifier, then the technical terms used when talking to the owner.
Save as: docs/glossary.md. Add a business row the first time a business term becomes code,
and a technical row the first time a document or a report uses a term the owner may not know (a product or service name, an acronym, a protocol, a pattern, a library).
The documents of the project (infra, ADRs, item files, release notes) link here from the line under their title.
The last column of both tables is the meaning in Brazilian Portuguese: the one text in the repository that is not English.
The UI labels come from resource files, not from this file.
-->
# Glossary

| Business term (pt-BR) | English identifier | Meaning | Meaning (pt-BR) |
|---|---|---|---|
| Questão | `Question` | An item a student answers, of one of the supported types | Um item que o aluno responde, de um dos tipos suportados |

## Technical terms
Terms used in documents, reports, reviews and item files. They are not identifiers. The pt-BR column is the word to use when talking to the owner.

| Term | pt-BR | Meaning | Meaning (pt-BR) |
|---|---|---|---|
| blocker | bloqueador | Review finding that stops the merge until it is fixed. | Achado de revisão que impede o merge até ser corrigido. |
| major | grave | Review finding that is a real defect, a broken rule or a missing test for a criterion; fixed before validation unless the owner decides otherwise. | Achado de revisão que é um defeito real, uma regra quebrada ou um teste faltando para um critério; corrigido antes da validação, salvo decisão do dono. |
| minor | leve | Small review finding (clarity, a simplification); fixed, accepted with a reason, or turned into a new item. | Achado pequeno de revisão (clareza, uma simplificação); corrigido, aceito com um motivo ou transformado em um novo item. |
| trade-off | escolha com custo | A valid point in a review or a design review (reviewer, architect) with a cost on both sides; the owner chooses, the session never does. A broken rule or criterion is a finding, not a trade-off. | Um ponto válido de uma revisão ou da revisão de desenho (reviewer, architect) com custo nos dois lados; o dono escolhe, a sessão nunca. Uma regra ou critério quebrado é um achado, não uma escolha com custo. |
| checked and fine | conferido sem achado | A check the reviewer or the architect ran that has no finding, with what it read. | Uma verificação que o revisor ou o architect rodou e não teve achado, com o que leu. |
| trap | armadilha | In a `system-design` or `reviewer` report, an attractive choice that looks right for this item and has a hidden cost (a false economy, will not scale, breaks a caller later); at most 3, shown and recorded, never asked. Not a risk, a finding or a trade-off. | Num relatório do `system-design` ou do `reviewer`, uma escolha atraente que parece certa para este item e tem um custo escondido (falsa economia, não escala, quebra um chamador depois); no máximo 3, mostrada e registrada, nunca perguntada. Não é risco, achado nem escolha com custo. |
| reuse ladder | escada de reuso | The order the build searches before writing code: not needed, already in the codebase, the base class library, the platform, a package already installed, then the minimum code. | A ordem em que o build procura antes de escrever código: nem precisa, já existe no código, a biblioteca base, a plataforma, um pacote já instalado, e só então o mínimo de código. |
| safety floor | piso de segurança | What the reuse ladder never cuts: validation at trust boundaries, authorization, tenant isolation, data loss, i18n and accessibility, plus the item's own criteria and tests. | O que a escada de reuso nunca corta: validação nas fronteiras de confiança, autorização, isolamento de tenant, perda de dados, i18n e acessibilidade, além dos critérios e testes do próprio item. |
| acceptance criterion (AC) | critério de aceite | A Given/When/Then sentence that a test proves. | Uma frase Dado/Quando/Então que um teste comprova. |
| coverage gap | lacuna de cobertura | A criterion with no test through the path a user reaches. | Um critério sem teste pelo caminho que o usuário percorre. |
| validation script | roteiro de validação | At most 8 steps the owner follows on screen before the merge. | No máximo 8 passos que o dono segue na tela antes do merge. |
| gate | gate (portão) | An automatic check that must pass before the work goes on: build, tests, no new warnings. | Uma verificação automática que precisa passar antes de o trabalho seguir: build, testes, nenhum aviso novo. |
| warnings baseline | baseline de avisos | The build warnings accepted so far; the gate fails only on new ones. | Os avisos de build aceitos até agora; o gate só falha nos novos. |
| merge base | base do merge | The commit where the item branch left the main branch; a review compares from it. | O commit em que a branch do item saiu da principal; a revisão compara a partir dele. |
| confirmation | confirmação | A decision a rule, an ADR or an earlier item already answers, asked in one click with its source: Yes keeps it, No changes it. | Uma decisão que uma regra, um ADR ou um item anterior já responde, perguntada em um clique com a sua fonte: Sim mantém, Não muda. |
| item decision | decisão do item | A decision that is true for this item only; it stays in the item file. | Uma decisão que vale só para este item; fica no arquivo do item. |
| project rule | regra do projeto | A decision that is true for later items: one line, with its source, in the project rules. | Uma decisão que vale para os próximos itens: uma linha, com a fonte, nas regras do projeto. |
| hidden fact | fato escondido | Something true in the code that is hard to find: a rule of its own area, loaded only when that area is read. | Algo verdadeiro no código e difícil de achar: uma regra da própria área, carregada só quando essa área é lida. |
| plugin gap | lacuna do plugin | Something the plugin should have known or done: a note for the plugin, not a rule of the project. | Algo que o plugin deveria saber ou fazer: uma nota para o plugin, não uma regra do projeto. |
| technical page | página técnica | A page for the developers in the project's `docs/`, written with the code of an item and listed in the item's `## Docs`: a flow, a how-to, a reference or an architecture page. | Uma página para os desenvolvedores em `docs/` do projeto, escrita junto com o código de um item e listada no `## Docs` dele: um fluxo, um passo a passo, uma referência ou uma arquitetura. |
| flow page | página de fluxo | A technical page that describes a behavior of the app from its trigger to its end. | Uma página técnica que descreve um comportamento do app, do gatilho até o fim. |
| how-to page | página de passo a passo | A technical page that tells the team how to run a procedure or operate something. | Uma página técnica que diz ao time como executar um procedimento ou operar algo. |
| reference page | página de referência | A technical page that lists a contract or a setting: a route, a message, a configuration key. | Uma página técnica que lista um contrato ou uma configuração: uma rota, uma mensagem, uma chave de configuração. |
| architecture page | página de arquitetura | A technical page that describes a module or an external dependency: its purpose, parts and dependencies. | Uma página técnica que descreve um módulo ou uma dependência externa: propósito, partes e dependências. |
| size L | tamanho L | An item the session judges too large to refine, build and validate in one session; it is proposed in smaller parts. | Um item que a sessão julga grande demais para refinar, construir e validar numa sessão; ele é proposto em partes menores. |
| split | corte | Dividing an item into parts the owner approves: the item stays as part 1 and each other part is a new item that says `Split from <id>`. | Dividir um item em partes que o dono aprova: o item fica como parte 1 e cada outra parte é um item novo que diz `Split from <id>`. |
| wireframe | wireframe | A text drawing of a screen in boxes, written at refinement in the item file and copied into the flow page; every screen an item adds or changes has one. | Um desenho em texto de uma tela, em caixas, escrito na refinada no arquivo do item e copiado para a página de fluxo; toda tela que um item cria ou muda tem um. |
| mockup | mockup | The HTML prototype of a complex screen with every state, made by `/agile:screen`; the flow page links it. | O protótipo HTML de uma tela complexa, com todos os estados, feito pelo `/agile:screen`; a página de fluxo aponta para ele. |
