---
page: exam-editions
locale: pt-BR
features: [F-35, F-74, F-75]
updated: 2026-10-09
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
2. Escolha **Excluir** na linha e confirme. A edição sai da lista, com as suas disciplinas do edital (a confirmação avisa). O ano, o cargo e a banca continuam reservados dentro do exame, para que a mesma prova não seja adicionada de novo por engano.

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

## Disciplinas do edital
No cartão **Disciplinas do edital**, abaixo do formulário da edição, você registra as disciplinas exatamente como o edital as apresenta: o grupo (por exemplo "Conhecimentos Básicos"), o nome da disciplina e o número de questões. O cartão só aparece depois que a edição é salva; numa edição nova ele pede para salvar primeiro. Cada disciplina é salva pelo seu próprio diálogo ou ação: o botão **Salvar** da edição nunca as salva. Funciona em edições em rascunho e publicadas.

### Adicionar uma disciplina
1. No cartão, escolha **Adicionar disciplina do edital**.
2. Se o edital agrupa as disciplinas, escolha um grupo já usado (a lista sugere ao digitar, sem diferenciar maiúsculas e acentos) ou digite um novo. Deixe vazio se o edital não agrupa. Ao adicionar várias seguidas, o grupo da última adicionada já vem preenchido.
3. Digite a **disciplina** como o edital a nomeia e, se o edital informa, o **número de questões** (de 1 a 500; vazio quer dizer que o edital não informa).
4. Escolha **Salvar**. A nova disciplina entra no fim do seu grupo, ou no fim da lista quando o grupo é novo.

### Ver, ordenar, alterar e excluir
- As disciplinas aparecem sob o título do seu grupo, na ordem do edital. Quando nenhuma tem grupo, não há título. Um traço (—) quer dizer que o número de questões não foi informado.
- Abaixo da lista, o total soma só os números informados e diz quantas disciplinas ficaram fora da soma por não terem número. O total não é guardado nem conferido com nada.
- **Mover para cima** e **Mover para baixo** trocam a disciplina com a vizinha do mesmo grupo. A primeira do grupo não sobe e a última não desce (o botão fica desativado e explica o motivo); uma disciplina nunca muda de grupo ao ser movida.
- **Editar** abre o mesmo diálogo preenchido. Mudar o grupo leva a disciplina para o fim do novo grupo; mudar só o nome ou o número mantém a posição.
- **Excluir** pede confirmação e remove a disciplina da edição. Ela pode ser adicionada de novo depois.

### Mapear uma disciplina do edital para a taxonomia
O edital nomeia as disciplinas com as próprias palavras ("Raciocínio Lógico-Matemático"). O campo **Abrange** diz quais [disciplinas e tópicos](subjects.md) da lista comum do Simulab aquele título quer dizer, para que todas as edições falem o mesmo vocabulário.
1. Escolha **Editar** na disciplina do edital (ou preencha ao adicioná-la).
2. Em **Abrange**, clique no campo ou aperte a seta para baixo. A lista mostra todas as disciplinas, cada uma começando pela opção "disciplina inteira", seguida dos seus tópicos. Digite para filtrar: maiúsculas e acentos não importam, e uma disciplina cujo nome combina mostra todos os seus tópicos.
3. Escolha a disciplina inteira quando o título cobre toda ela, ou só os tópicos que ele cobre. A lista continua aberta para você escolher vários; cada escolha aparece abaixo como um chip ("Disciplina · disciplina inteira" ou "Disciplina › Tópico"), e o contador diz quantos você escolheu de 50.
4. Retire uma escolha pelo X do chip, ou todas com **Limpar a seleção**. Escolha **Salvar**.

- Enquanto uma disciplina está escolhida inteira, os tópicos dela ficam acinzentados: a disciplina inteira já os cobre. Enquanto alguns tópicos dela estão escolhidos, a opção "disciplina inteira" fica acinzentada: retire esses tópicos primeiro. Apontar para uma opção acinzentada explica o motivo.
- A mesma disciplina ou tópico pode ser escolhido por várias disciplinas do edital da mesma edição, para uma lei que o edital lista sob dois títulos.
- O mapeamento é opcional. Uma linha sem nada escolhido mostra o chip **Não mapeada**, e o rodapé conta as linhas ainda não mapeadas. A edição continua podendo ser salva e publicada.
- Quando um curador move um tópico mapeado para outra disciplina, a linha o mostra sob a nova disciplina sem precisar ser editada.

### Campos das disciplinas do edital
| Campo | Significado | Regras |
|---|---|---|
| Grupo | O título sob o qual o edital lista a disciplina | Opcional, até 100 caracteres. Vazio é "Sem grupo" |
| Disciplina | A disciplina como o edital a nomeia | Obrigatório, de 2 a 200 caracteres |
| Número de questões | Quantas questões o edital reserva à disciplina | Opcional, número inteiro de 1 a 500 |
| Abrange | As disciplinas e os tópicos da taxonomia que a disciplina do edital cobre | Opcional, até 50 itens. Um tópico não pode ser escolhido junto com a disciplina inteira dele |

Na mesma edição, o nome da disciplina não pode se repetir dentro do mesmo grupo (maiúsculas e acentos não contam; "sem grupo" é um grupo). Uma disciplina excluída não conta.

### Mensagens das disciplinas do edital
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Informe a disciplina como o edital a nomeia. | O nome está vazio. | Digite o nome. |
| A disciplina precisa de ao menos 2 caracteres. | O nome tem 1 caractere. | Digite o nome completo. |
| A disciplina é longa demais: no máximo 200 caracteres. | O nome passa de 200 caracteres. | Encurte o nome. |
| O grupo é longo demais: no máximo 100 caracteres. | O grupo passa de 100 caracteres. | Encurte o grupo. |
| Informe um número inteiro de 1 a 500, ou deixe vazio. | O número não é inteiro ou está fora do intervalo. | Corrija ou deixe vazio. |
| Este grupo já tem uma disciplina com este nome (maiúsculas e acentos não contam). | A mesma disciplina já está no grupo. | Mude o nome ou o grupo. |
| Esta disciplina não está mais na edição. A lista foi recarregada. | Alguém a excluiu com a sua tela aberta. | Confira a lista atualizada. |
| Esta disciplina não pode ir nessa direção. A lista foi recarregada. | Outra pessoa já a moveu. | Confira a ordem atual. |
| Limite de 50 itens atingido. Remova um para escolher outro. | O campo Abrange já tem 50 escolhas. | Remova uma escolha antes. |
| No máximo 50 itens por disciplina do edital. | O salvamento enviou mais de 50 escolhas. | Remova escolhas e salve de novo. |
| Um tópico não pode ser escolhido junto com a disciplina inteira dele; os itens estão marcados abaixo. Remova o tópico ou a disciplina inteira. | Um tópico e a disciplina inteira dele foram escolhidos juntos. | Remova um dos chips marcados. |
| Um dos itens escolhidos não existe mais na taxonomia; ele está marcado abaixo. Remova-o e salve de novo. | Alguém excluiu uma disciplina ou tópico escolhido com a sua tela aberta. | Remova o chip marcado e salve. |
| Um dos itens escolhidos não é válido. Remova-o e escolha de novo. | Uma escolha não pôde ser lida. | Remova-a e escolha de novo. |
| Outra pessoa salvou esta disciplina ao mesmo tempo. Salve de novo. | Duas pessoas salvaram a mesma linha ao mesmo tempo. | Salve de novo. |
| Não foi possível carregar a taxonomia. Os itens já escolhidos continuam aqui. | A lista de disciplinas não carregou. | Recarregue a página e tente de novo. |

## Páginas relacionadas
- [Exames](exams.md): os exames do catálogo (Administradores)
- [Bancas](organizers.md): as bancas que uma edição indica (Administradores)
- [Disciplinas e tópicos](subjects.md): a lista comum para a qual uma disciplina do edital é mapeada (Administradores)
- [Como navegar](getting-around.md): menu, modo claro e escuro, idioma e teclado
- [Simulab](index.md)
