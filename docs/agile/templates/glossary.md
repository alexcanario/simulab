<!--
Business vocabulary and its English identifier, then the technical terms used when talking to the owner.
Save as: docs/glossary.md. Add a business row the first time a business term becomes code,
and a technical row the first time a report, review or item file uses a term the owner may not know.
The UI labels come from resource files, not from this file.
-->
# Glossary

| Business term (pt-BR) | pt-PT | English identifier | Meaning |
|---|---|---|---|
| Questão | Questão | `Question` | An item a student answers, of one of the supported types |

## Technical terms
Terms used in reports, reviews and item files. They are not identifiers. The pt-BR column is the word to use when talking to the owner.

| Term | pt-BR | Meaning |
|---|---|---|
| blocker | bloqueador | Review finding that stops the merge until it is fixed. |
| major | grave | Review finding that is a real defect, a broken rule or a missing test for a criterion; fixed before validation unless the owner decides otherwise. |
| minor | leve | Small review finding (clarity, a simplification); fixed, accepted with a reason, or turned into a new item. |
| acceptance criterion (AC) | critério de aceite | A Given/When/Then sentence that a test proves. |
| coverage gap | lacuna de cobertura | A criterion with no test through the path a user reaches. |
| validation script | roteiro de validação | At most 8 steps the owner follows on screen before the merge. |
| gate | gate (portão) | An automatic check that must pass before the work goes on: build, tests, no new warnings. |
| warnings baseline | baseline de avisos | The build warnings accepted so far; the gate fails only on new ones. |
| merge base | base do merge | The commit where the item branch left the main branch; a review compares from it. |
