# UI map

<!-- Read by the writers of `## Validation script` (references/validation-steps.md in the plugin). One row per term and per screen; `Verified` is the short commit the row was read at, or `no`. A row whose Source changed after Verified is stale: `ui-map.js check` says so at the ship. -->

## Terms
| Term | Meaning (pt-BR) |
|---|---|
| sidebar | menu lateral esquerdo |
| base text | texto-base: um texto que várias questões usam, escrito uma vez |
| picker | seletor: o campo em que se escolhe um texto-base ao editar a questão |
| type card | cartão de tipo: cada um dos cartões do campo "Question type" (Single choice, True/false, Multiple answer) |
| checklist | lista "Before publishing": o que falta para a questão poder ser publicada, ao lado do formulário |

## Screens
| Screen | Path | Control | Source | Verified |
|---|---|---|---|---|
| Base texts list | sidebar > Content > Base texts | the table; Add at the top right; Edit and Delete in each row | src/Hosts/Simulab.Web/Components/Pages/QuestionBank/BaseTexts.razor | 31cb0606 |
| Base text form | Base texts list > Add, or Edit on a row | Save at the bottom of the first card; the linked questions and Delete in the cards below | src/Hosts/Simulab.Web/Components/Pages/QuestionBank/BaseTextForm.razor | 31cb0606 |
| Question form, base text section | sidebar > Content > Questions > Add (or Edit on a row) > Base text card, after Statement | the picker field; Open and Show the text under it | src/Hosts/Simulab.Web/Components/Pages/QuestionBank/QuestionForm.razor | bc7db0cb |
| Question form, type and options | sidebar > Content > Questions > Add > "Question type" cards at the top of the Classification card; the Options card below Base text | the three type cards; in the Options card one checkbox per row for the Multiple answer type, the count "{n} of 5 options" under the list, and the checklist "Before publishing" in the aside | src/Hosts/Simulab.Web/Components/Pages/QuestionBank/QuestionForm.razor | bc7db0cb |
| Question preview | sidebar > Content > Questions > Edit on a row > Preview as a student (Publication card, below the form) | the dialog: the line "Mark N options.", Show answer under the options | src/Hosts/Simulab.Web/Components/Pages/QuestionBank/QuestionPreviewDialog.razor | bc7db0cb |
