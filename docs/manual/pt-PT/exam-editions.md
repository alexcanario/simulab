---
page: exam-editions
locale: pt-PT
features: [F-35]
updated: 2026-09-29
---
# Edições de um exame

Uma edição é uma prova efetivamente aplicada: o ano do aviso, o posto de trabalho a que se destina, a entidade organizadora que a aplicou e se os alunos a podem ver. Um exame tem tantas edições quantas as provas que teve. Quando um aviso abre vários postos de trabalho com provas diferentes, cada prova é uma edição.

As edições gerem-se na página do próprio exame, no cartão **Edições**, por baixo do formulário.

## Quem pode usar
Apenas uma conta com a permissão "Gerir o catálogo" — o perfil Admin tem-na. Para os restantes aparece Página não encontrada.

## Como fazer
### Ver as edições de um exame
1. Escolha **Exames**, na secção **Conteúdo**, e depois **Editar** no exame.
2. O cartão **Edições** lista-as da mais recente para a mais antiga. Cada linha mostra o ano, o posto de trabalho quando existe, a sigla da entidade organizadora, uma etiqueta (Rascunho ou Publicada) e a data de aplicação quando é conhecida.

### Adicionar uma edição
1. Guarde primeiro o exame: um exame novo ainda não pode ter edições.
2. No cartão **Edições**, escolha **Adicionar edição**. Abre numa página própria.
3. Em **Entidade organizadora**, escreva duas letras do nome ou da sigla e escolha na lista. Se ainda não existe, registe-a antes em [Entidades organizadoras](organizers.md).
4. Escreva o **ano do aviso**.
5. Se quiser, preencha o **posto de trabalho** (deixe vazio para ENEM, exames de acesso e certificações), a **referência do aviso** ("Aviso n.º 1/2026"), a **ligação do aviso** e a **data de aplicação**. Escreva a data no seu formato ou escolha-a no calendário.
6. Escolha **Rascunho** ou **Publicada** e **Guardar**. Uma edição nova é rascunho, a menos que escolha Publicada. Continua na página, agora a editar a edição que acabou de guardar.

### Alterar, publicar ou anular a publicação de uma edição
1. Escolha **Editar** na respetiva linha.
2. Altere o que for preciso. Para publicar ou anular a publicação, escolha o outro cartão em **Publicação**. Escolha **Guardar**.

### Eliminar uma edição
1. Só um rascunho pode ser eliminado. Se a edição está publicada, abra-a, escolha **Rascunho** e guarde primeiro: a ação de eliminar da linha publicada fica desativada.
2. Escolha **Eliminar** na linha e confirme. A edição sai da lista. O ano, o posto de trabalho e a entidade continuam ocupados dentro do exame, para que a mesma prova não volte a ser adicionada por engano.

Um exame que ainda tem edições não pode ser eliminado, nem uma entidade organizadora que alguma edição indique. Elimine ou altere primeiro as edições.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Entidade organizadora | Quem aplicou a prova, como o aviso a indica | Obrigatório. Escolha na lista |
| Ano do aviso | O ano do aviso | Obrigatório, de 1990 ao próximo ano |
| Posto de trabalho | O posto a que a prova se destina | Opcional, no máximo 200 caracteres |
| Referência do aviso | Como o aviso se identifica | Opcional, no máximo 100 caracteres. Edições do mesmo aviso repetem o texto |
| Ligação do aviso | O endereço oficial do aviso | Opcional, endereço completo a começar por `http://` ou `https://`, no máximo 300 caracteres |
| Data de aplicação | O dia em que a prova foi aplicada | Opcional, também para publicar. Não pode ser anterior a 1 de janeiro do ano do aviso |
| Estado | Rascunho fica só na administração; Publicada fica disponível para os alunos | Obrigatório. Começa em Rascunho |

Um exame não pode ter duas edições com o mesmo ano, posto de trabalho e entidade; maiúsculas e acentos não tornam um posto diferente, e uma edição eliminada ainda conta.

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Escolha a entidade organizadora. | Não foi escolhida nenhuma entidade. | Escreva duas letras e escolha uma na lista. |
| Indique um ano entre 1990 e o próximo ano. | O ano do aviso falta ou está fora do intervalo. | Escreva um ano válido. |
| Escreva o endereço completo, com no máximo 300 caracteres, a começar por http:// ou https:// | A ligação do aviso não é um endereço web completo. | Acrescente `https://` ou deixe o campo vazio. |
| A data de aplicação não pode ser anterior a 1 de janeiro do ano do aviso. | A data é anterior ao ano do aviso. | Corrija a data ou o ano. |
| Este exame já tem uma edição com este ano, posto de trabalho e entidade organizadora (uma eliminada também conta). | A mesma prova já existe. | Mude o ano, o posto ou a entidade. |
| Esta edição está publicada. Volte a colocá-la em Rascunho, guarde e depois elimine-a. | Só rascunhos podem ser eliminados. | Anule primeiro a publicação. |
| Este exame tem edições. Elimine primeiro as edições. | Um exame com edições não pode ser eliminado. | Elimine as edições e depois o exame. |
| Há edições que indicam esta entidade organizadora. Altere ou elimine primeiro essas edições. | Uma entidade em uso não pode ser eliminada. | Altere ou elimine essas edições. |
| Esta edição já não existe. | Alguém a eliminou com o seu ecrã aberto. | Volte ao exame. |

## Páginas relacionadas
- [Exames](exams.md): os exames do catálogo (Administradores)
- [Entidades organizadoras](organizers.md): as entidades que uma edição indica (Administradores)
- [Como navegar](getting-around.md): menu, modo claro e escuro, idioma e teclado
- [Simulab](index.md)
