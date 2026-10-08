<!--
Business vocabulary and its English identifier, then the technical terms used when talking to the owner.
Save as: docs/glossary.md. Add a business row the first time a business term becomes code,
and a technical row the first time a document or a report uses a term the owner may not know (a product or service name, an acronym, a protocol, a pattern, a library).
The documents of the project (infra, ADRs, item files, release notes) link here from the line under their title.
The last column of both tables is the meaning in Brazilian Portuguese: the one text in the repository that is not English.
The UI labels come from resource files, not from this file.
-->
# Glossary

| Business term (pt-BR) | pt-PT | English identifier | Meaning | Meaning (pt-BR) |
|---|---|---|---|---|
| Questão | Questão | `Question` | An item a student answers, of one of the supported types | Um item que o aluno responde, de um dos tipos suportados |

## Technical terms
Terms used in documents, reports, reviews and item files. They are not identifiers. The pt-BR column is the word to use when talking to the owner.

| Term | pt-BR | Meaning | Meaning (pt-BR) |
|---|---|---|---|
| blocker | bloqueador | Review finding that stops the merge until it is fixed. | Achado de revisão que impede o merge até ser corrigido. |
| major | grave | Review finding that is a real defect, a broken rule or a missing test for a criterion; fixed before validation unless the owner decides otherwise. | Achado de revisão que é um defeito real, uma regra quebrada ou um teste faltando para um critério; corrigido antes da validação, salvo decisão do dono. |
| minor | leve | Small review finding (clarity, a simplification); fixed, accepted with a reason, or turned into a new item. | Achado pequeno de revisão (clareza, uma simplificação); corrigido, aceito com um motivo ou transformado em um novo item. |
| acceptance criterion (AC) | critério de aceite | A Given/When/Then sentence that a test proves. | Uma frase Dado/Quando/Então que um teste comprova. |
| coverage gap | lacuna de cobertura | A criterion with no test through the path a user reaches. | Um critério sem teste pelo caminho que o usuário percorre. |
| validation script | roteiro de validação | At most 8 steps the owner follows on screen before the merge. | No máximo 8 passos que o dono segue na tela antes do merge. |
| gate | gate (portão) | An automatic check that must pass before the work goes on: build, tests, no new warnings. | Uma verificação automática que precisa passar antes de o trabalho seguir: build, testes, nenhum aviso novo. |
| warnings baseline | baseline de avisos | The build warnings accepted so far; the gate fails only on new ones. | Os avisos de build aceitos até agora; o gate só falha nos novos. |
| merge base | base do merge | The commit where the item branch left the main branch; a review compares from it. | O commit em que a branch do item saiu da principal; a revisão compara a partir dele. |
