---
page: exam-editions
locale: pt-PT
features: [F-35, F-74]
updated: 2026-10-04
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
2. Escolha **Eliminar** na linha e confirme. A edição sai da lista, com as suas disciplinas do aviso (a confirmação avisa). O ano, o posto de trabalho e a entidade continuam ocupados dentro do exame, para que a mesma prova não volte a ser adicionada por engano.

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

## Disciplinas do aviso
No cartão **Disciplinas do aviso**, abaixo do formulário da edição, regista as disciplinas tal como o aviso as apresenta: o grupo (por exemplo "Conhecimentos gerais"), o nome da disciplina e o número de questões. O cartão só aparece depois de a edição ser guardada; numa edição nova pede para guardar primeiro. Cada disciplina é guardada pelo seu próprio diálogo ou ação: o botão **Guardar** da edição nunca as guarda. Funciona em edições em rascunho e publicadas.

### Adicionar uma disciplina
1. No cartão, escolha **Adicionar disciplina do aviso**.
2. Se o aviso agrupa as disciplinas, escolha um grupo já usado (a lista sugere ao escrever, sem distinguir maiúsculas e acentos) ou escreva um novo. Deixe vazio se o aviso não agrupa. Ao adicionar várias seguidas, o grupo da última adicionada já vem preenchido.
3. Escreva a **disciplina** como o aviso a designa e, se o aviso o indica, o **número de questões** (de 1 a 500; vazio quer dizer que o aviso não o indica).
4. Escolha **Guardar**. A nova disciplina entra no fim do seu grupo, ou no fim da lista quando o grupo é novo.

### Ver, ordenar, alterar e eliminar
- As disciplinas aparecem sob o título do seu grupo, pela ordem do aviso. Quando nenhuma tem grupo, não há título. Um traço (—) quer dizer que o número de questões não foi indicado.
- Abaixo da lista, o total soma só os números indicados e diz quantas disciplinas ficaram fora da soma por não terem número. O total não é guardado nem comparado com nada.
- **Mover para cima** e **Mover para baixo** trocam a disciplina com a vizinha do mesmo grupo. A primeira do grupo não sobe e a última não desce (o botão fica desativado e explica o motivo); uma disciplina nunca muda de grupo ao ser movida.
- **Editar** abre o mesmo diálogo preenchido. Mudar o grupo passa a disciplina para o fim do novo grupo; mudar só o nome ou o número mantém a posição.
- **Eliminar** pede confirmação e retira a disciplina da edição. Pode voltar a ser adicionada mais tarde.

### Campos das disciplinas do aviso
| Campo | Significado | Regras |
|---|---|---|
| Grupo | O título sob o qual o aviso lista a disciplina | Opcional, no máximo 100 caracteres. Vazio é "Sem grupo" |
| Disciplina | A disciplina como o aviso a designa | Obrigatório, de 2 a 200 caracteres |
| Número de questões | Quantas questões o aviso reserva à disciplina | Opcional, número inteiro de 1 a 500 |

Na mesma edição, o nome da disciplina não pode repetir-se dentro do mesmo grupo (maiúsculas e acentos não contam; "sem grupo" é um grupo). Uma disciplina eliminada não conta.

### Mensagens das disciplinas do aviso
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Indique a disciplina tal como o aviso a designa. | O nome está vazio. | Escreva o nome. |
| A disciplina precisa de pelo menos 2 caracteres. | O nome tem 1 caractere. | Escreva o nome completo. |
| A disciplina é demasiado longa: no máximo 200 caracteres. | O nome passa de 200 caracteres. | Encurte o nome. |
| O grupo é demasiado longo: no máximo 100 caracteres. | O grupo passa de 100 caracteres. | Encurte o grupo. |
| Indique um número inteiro de 1 a 500, ou deixe vazio. | O número não é inteiro ou está fora do intervalo. | Corrija ou deixe vazio. |
| Este grupo já tem uma disciplina com este nome (maiúsculas e acentos não contam). | A mesma disciplina já está no grupo. | Mude o nome ou o grupo. |
| Esta disciplina já não está na edição. A lista foi recarregada. | Alguém eliminou-a com o seu ecrã aberto. | Confira a lista atualizada. |
| Esta disciplina não pode ir nesse sentido. A lista foi recarregada. | Outra pessoa já a moveu. | Confira a ordem atual. |

## Páginas relacionadas
- [Exames](exams.md): os exames do catálogo (Administradores)
- [Entidades organizadoras](organizers.md): as entidades que uma edição indica (Administradores)
- [Como navegar](getting-around.md): menu, modo claro e escuro, idioma e teclado
- [Simulab](index.md)
