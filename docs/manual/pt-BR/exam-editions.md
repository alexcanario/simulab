---
page: exam-editions
locale: pt-BR
features: [F-35]
updated: 2026-09-29
---
# Edições de um exame

Uma edição é uma prova efetivamente aplicada: o ano do edital, o cargo a que ela se destina, a banca que a aplicou e se os alunos podem vê-la. Um exame tem tantas edições quantas provas teve. Quando um edital abre vários cargos com provas diferentes, cada prova é uma edição.

As edições são gerenciadas na página do próprio exame, no cartão **Edições**, abaixo do formulário.

## Quem pode usar
Apenas uma conta com a permissão "Gerenciar o catálogo" — o papel Admin a tem. Para os demais aparece Página não encontrada.

## Como fazer
### Ver as edições de um exame
1. Escolha **Exames**, na seção **Conteúdo**, e depois **Editar** no exame.
2. O cartão **Edições** lista todas, da mais recente para a mais antiga. Cada linha mostra o ano, o cargo quando há, a sigla da banca, uma etiqueta (Rascunho ou Publicada) e a data de aplicação quando é conhecida.

### Adicionar uma edição
1. Salve o exame antes: um exame novo ainda não pode ter edições.
2. No cartão **Edições**, escolha **Adicionar edição**. Ela abre em uma página própria.
3. Em **Banca**, digite duas letras do nome ou da sigla e escolha na lista. Se a banca ainda não existe, cadastre-a antes em [Bancas](organizers.md).
4. Digite o **ano do edital**.
5. Se quiser, preencha o **cargo** (deixe vazio para ENEM, vestibulares e certificações), a **identificação do edital** ("Edital nº 01/2026"), o **link do edital** e a **data de aplicação**. Digite a data no seu formato ou escolha no calendário.
6. Escolha **Rascunho** ou **Publicada** e **Salvar**. Uma edição nova é rascunho, a menos que você escolha Publicada. Você continua na página, agora editando a edição que acabou de salvar.

### Alterar, publicar ou despublicar uma edição
1. Escolha **Editar** na linha dela.
2. Mude o que precisar. Para publicar ou despublicar, escolha o outro cartão em **Publicação**. Escolha **Salvar**.

### Excluir uma edição
1. Só um rascunho pode ser excluído. Se a edição está publicada, abra-a, escolha **Rascunho** e salve antes: a ação de excluir da linha publicada fica desativada.
2. Escolha **Excluir** na linha e confirme. A edição sai da lista. O ano, o cargo e a banca continuam reservados dentro do exame, para que a mesma prova não seja adicionada de novo por engano.

Um exame que ainda tem edições não pode ser excluído, nem uma banca que alguma edição indica. Exclua ou altere as edições primeiro.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Banca | Quem aplicou a prova, como o edital a indica | Obrigatório. Escolha na lista |
| Ano do edital | O ano do edital | Obrigatório, de 1990 ao próximo ano |
| Cargo | O cargo a que a prova se destina | Opcional, até 200 caracteres |
| Identificação do edital | Como o edital se identifica | Opcional, até 100 caracteres. Edições do mesmo edital repetem o texto |
| Link do edital | O endereço oficial do edital | Opcional, endereço completo começando com `http://` ou `https://`, até 300 caracteres |
| Data de aplicação | O dia em que a prova foi aplicada | Opcional, também para publicar. Não pode ser anterior a 1º de janeiro do ano do edital |
| Situação | Rascunho fica só na administração; Publicada é oferecida aos alunos | Obrigatório. Começa em Rascunho |

Um exame não pode ter duas edições com o mesmo ano, cargo e banca; maiúsculas e acentos não tornam um cargo diferente, e uma edição excluída ainda conta.

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Escolha a banca. | Nenhuma banca foi escolhida. | Digite duas letras e escolha uma na lista. |
| Informe um ano entre 1990 e o próximo ano. | O ano do edital falta ou está fora do intervalo. | Digite um ano válido. |
| Digite o endereço completo, com até 300 caracteres, começando com http:// ou https:// | O link do edital não é um endereço web completo. | Acrescente `https://` ou deixe o campo vazio. |
| A data de aplicação não pode ser anterior a 1º de janeiro do ano do edital. | A data é anterior ao ano do edital. | Corrija a data ou o ano. |
| Este exame já tem uma edição com este ano, cargo e banca (uma excluída também conta). | A mesma prova já existe. | Mude o ano, o cargo ou a banca. |
| Esta edição está publicada. Volte-a para Rascunho, salve e depois exclua. | Só rascunhos podem ser excluídos. | Despublique antes. |
| Este exame tem edições. Exclua as edições primeiro. | Um exame com edições não pode ser excluído. | Exclua as edições e depois o exame. |
| Há edições que indicam esta banca. Altere ou exclua essas edições primeiro. | Uma banca em uso não pode ser excluída. | Altere ou exclua essas edições. |
| Esta edição não existe mais. | Alguém a excluiu com a sua tela aberta. | Volte ao exame. |

## Páginas relacionadas
- [Exames](exams.md): os exames do catálogo (Administradores)
- [Bancas](organizers.md): as bancas que uma edição indica (Administradores)
- [Como navegar](getting-around.md): menu, modo claro e escuro, idioma e teclado
- [Simulab](index.md)
