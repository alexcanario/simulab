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
| review queue | fila de revisão: a lista das questões enviadas para revisão, da mais antiga para a mais recente |
| in review | em revisão: a questão foi enviada, está congelada e espera uma decisão |
| reviewer-only role | papel só de revisão: um papel criado na tela de papéis que tem apenas a permissão "Review questions" |
| app bar | barra superior: a faixa no alto de todas as páginas, com o menu, a marca e os botões de tema, idioma e usuário |
| theme switch | botão de tema: a lua (ou o sol, no tema escuro) no canto superior direito da barra superior |
| UI gallery | galeria do kit: a página de desenvolvimento que mostra cada peça do kit de interface |
| chip | etiqueta: uma pílula arredondada que mostra um item (aqui, uma entrada do mapeamento de uma disciplina do edital) |
| notice subject | disciplina do edital: uma matéria como o edital a nomeia e agrupa, com o número de questões |
| syllabus | conteúdo programático: a lista de disciplinas do edital que o aluno abre sob cada edição no catálogo |
| notice PDF | PDF do edital: o arquivo PDF do edital de uma edição, guardado dentro do Simulab, além do link oficial |
| file field | campo de arquivo: o campo "Notice PDF" do formulário da edição, com Choose PDF, Replace e Remove |

## Screens
| Screen | Path | Control | Source | Verified |
|---|---|---|---|---|
| App bar | every page with the main layout, at the top | from the left: menu button, brand symbol, "Simulab" over "Control Alt Systems" (the home link), theme switch, language switch (globe), user menu or Sign in | src/Hosts/Simulab.Web/Components/Layout/MainLayout.razor | 5078271 |
| UI gallery, identity sections | sidebar > Development > UI kit (/dev/ui, Development only) | the sections Buttons, Type and Palette at the end of the page | src/Hosts/Simulab.Web/Components/Pages/Dev/UiGallery.razor | 5078271 |
| Base texts list | sidebar > Content > Base texts | the table; Add at the top right; Edit and Delete in each row | src/Hosts/Simulab.Web/Components/Pages/QuestionBank/BaseTexts.razor | 31cb0606 |
| Base text form | Base texts list > Add, or Edit on a row | Save at the bottom of the first card; the linked questions and Delete in the cards below | src/Hosts/Simulab.Web/Components/Pages/QuestionBank/BaseTextForm.razor | 31cb0606 |
| Question form, base text section | sidebar > Content > Questions > Add (or Edit on a row) > Base text card, after Statement | the picker field; Open and Show the text under it | src/Hosts/Simulab.Web/Components/Pages/QuestionBank/QuestionForm.razor | bc7db0cb |
| Question form, type and options | sidebar > Content > Questions > Add > "Question type" cards at the top of the Classification card; the Options card below Base text | the three type cards; in the Options card one checkbox per row for the Multiple answer type, the count "{n} of 5 options" under the list, and the checklist "Before publishing" in the aside | src/Hosts/Simulab.Web/Components/Pages/QuestionBank/QuestionForm.razor | bc7db0cb |
| Question preview | sidebar > Content > Questions > Edit on a row > Preview as a student (Publication card, below the form) | the dialog: the line "Mark N options.", Show answer under the options | src/Hosts/Simulab.Web/Components/Pages/QuestionBank/QuestionPreviewDialog.razor | bc7db0cb |
| Review queue | sidebar > Content > Review queue | the table; the question text is a link, and Review is in the row menu | src/Hosts/Simulab.Web/Components/Pages/QuestionBank/ReviewQueue.razor | 203369c |
| Review screen | Review queue > the question text or Review in the row | Reject and Approve at the bottom right; the summary (aside) on the right | src/Hosts/Simulab.Web/Components/Pages/QuestionBank/ReviewQuestion.razor | 203369c |
| Question form, review | sidebar > Content > Questions > Edit on a row > Publication card (Send for review, Withdraw from review) and the Review history card below it | the buttons in the Publication card; the Review history table under it | src/Hosts/Simulab.Web/Components/Pages/QuestionBank/QuestionForm.razor | 203369c |
| Edition notice subjects | sidebar > Content > Exams > the exam > "Editions" section > the edition (/admin/exams/{exam}/editions/{edition}) | the section "Notice subjects": rows in the notice's order under group headings, chips under each label, Edit and the move controls in each row | src/Hosts/Simulab.Web/Components/Pages/Catalog/NoticeSubjectsSection.razor | no |
| Edition notice PDF | sidebar > Content > Exams > the exam > "Editions" section > the edition (/admin/exams/{exam}/editions/{edition}) > card "Notice and application" | the file field "Notice PDF" below Application date: Choose PDF when empty; with a file, its name and size, Download, Replace and Remove; Save at the bottom sends it | src/Hosts/Simulab.Web/Components/Pages/Catalog/ExamEditionForm.razor, src/Hosts/Simulab.Web/Components/Ui/AppFileField.razor | 5869cb8 |
| Student notice PDF | sidebar > Study > Catalog > the exam (/catalog/exams/{id}) | in each edition's line, beside "Official notice", the link "Notice (PDF)" when the edition has a file | src/Hosts/Simulab.Web/Components/Pages/Catalog/CatalogExam.razor | 5869cb8 |
| Student syllabus | sidebar > Study > Catalog > the exam (/catalog/exams/{id}) | under each edition the button "Show syllabus (n subjects)" opens its notice subjects with their mapping | src/Hosts/Simulab.Web/Components/Pages/Catalog/EditionSyllabus.razor | no |
