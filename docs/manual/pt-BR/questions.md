---
page: questions
locale: pt-BR
features: [F-95, F-96, F-97, F-98, F-99, F-100, F-101, F-102, F-103, F-104, F-105, F-106, F-107, F-108, F-109, F-110]
updated: 2026-10-25
---
# Questões

O banco de questões é onde os curadores escrevem as questões de que provas e treinos são feitos. Uma questão
tem enunciado, texto-base opcional, de duas a cinco alternativas com uma correta, explicação opcional,
dificuldade, tópico e o idioma em que está escrita. Uma questão é de múltipla escolha, certo/errado,
múltiplas respostas, associação, lacunas, resposta curta, resposta aberta ou redação; a questão certo/errado tem um gabarito, certo ou errado, no lugar das
alternativas, a de múltiplas respostas tem várias alternativas corretas, a de associação liga cada item
numerado da esquerda a uma correspondência com letra da direita, e a de lacunas tem espaços numerados no
enunciado que o aluno preenche digitando, e a de resposta curta tem uma caixa em que o aluno digita a resposta.
A de resposta aberta e a de redação são escritas pelo aluno e corrigidas por uma pessoa; o gabarito delas é uma
rubrica, uma lista de critérios com pontos máximos. Este banco só guarda a rubrica: corrigir e dar a nota é de
etapas futuras.

## Quem pode usar
Uma conta com a permissão "Gerenciar o banco de questões". Curadores e Administradores a têm. Todas as outras
veem Página não encontrada, e o menu não tem o item Questões. Enviar uma questão para revisão e retirá-la da
revisão também são de quem gerencia o banco. Aprovar ou rejeitar é de quem tem a permissão "Revisar questões"
(Curadores e Administradores a têm): veja [Fila de revisão](review-queue.md).

## Como fazer
### Ver as questões
1. Escolha **Questões**, na seção **Conteúdo** do menu lateral.
2. A lista mostra o início de cada enunciado, o tópico com a disciplina, a fonte (a banca, o exame e o ano da
   prova de onde a questão veio, com o número dela; um traço quer dizer que a questão é autoral), a
   dificuldade, o idioma, a situação e o tipo (múltipla escolha ou certo/errado), da mais nova para a mais antiga.
3. Digite na caixa de busca para achar uma palavra do enunciado. Maiúsculas e acentos não importam.
4. Restrinja a lista com os filtros acima da tabela. Cada um aceita vários valores; uma questão é mantida
   quando combina com pelo menos um valor de cada filtro usado:
   - **Situação**: rascunhos, questões em revisão, questões publicadas, questões anuladas ou qualquer combinação.
   - **Tipo**: múltipla escolha, certo/errado ou os dois.
   - **Dificuldade**: de 1 a 5, e **Não definida** para questões sem dificuldade.
   - **Disciplina e tópico**: escolha uma disciplina inteira (vale todos os tópicos dela, até os que forem
     criados depois) ou tópicos avulsos.
   - **Banca**: as bancas de cujas provas as questões vieram. Questão autoral não tem banca e fica de fora
     enquanto este filtro estiver em uso.
   - **Edição**: uma edição de prova; comece a digitar o exame, a banca ou o ano.
5. Cada valor escolhido aparece como um chip embaixo do filtro; remova-o no **x**, ou use **Limpar** naquele
   filtro. **Limpar tudo** remove todos os filtros e a busca de uma vez.
6. Dá para escolher até 100 disciplinas, tópicos e bancas ao todo.
7. Os filtros ficam no endereço da página: abra uma questão, salve ou cancele, e você volta para a mesma lista
   filtrada, e pode mandar o link para outra pessoa.

### Questões que já vêm no banco
Um banco novo já traz as 60 questões de múltipla escolha da prova de 2026 da Guarda Municipal de Manaus
(Instituto Consulplan, caderno tipo 1, gabarito definitivo), na edição **CONSULPLAN · Guarda Municipal · 2026**,
numeradas como na prova e com tópico, disciplina do edital e gabarito. As questões 01 a 08 usam o texto-base
"Calma, gente", as questões 16 e 17 trazem as imagens da prova, e a questão 47 vem anulada, como a banca fez.
A redação da prova vem como a questão 61, do tipo Essay (redação): publicada, com os três textos motivadores como
texto-base, o tema no enunciado, limite de 20 a 30 linhas e a grade da banca como rubric (critérios de correção,
20 pontos no total). Nos três últimos critérios (estrutura sintática, morfossintaxe e desvios) a pontuação é a que
se mantém quando não há erro, e a descrição diz quanto se desconta por erro: 0,50 na estrutura sintática e 0,25 nos
outros dois. Essas questões são como as outras: dá para alterar, despublicar, anular ou excluir, e a mudança fica.
Quando a edição já tinha alguma questão, ou foi excluída, nada é trazido; a redação só deixa de vir se o número 61
já estiver em uso.

### Escrever uma questão
1. Escolha **Adicionar**. O primeiro campo, **Tipo de questão**, oferece **Múltipla escolha** (o padrão),
   **Certo/errado**, **Múltiplas respostas**, **Associação**, **Lacunas**, **Resposta curta**, **Resposta aberta** e **Redação**. O tipo é escolhido agora e não pode ser alterado depois que a questão é criada; numa
   questão salva ele aparece como um valor.
2. Escolha a **disciplina** e depois o **tópico** (a lista mostra só os tópicos daquela disciplina). O
   **idioma do conteúdo** começa com o seu idioma; troque pelo idioma em que a questão está escrita. Informe
   uma **dificuldade** de 1 (mais fácil) a 5 (mais difícil), se quiser.
3. Escreva o **enunciado**. A barra oferece negrito, itálico, sublinhado, tachado, sobrescrito, subscrito,
   listas, citação, link, imagem e limpar formatação. Para pôr uma figura no enunciado, veja
   [Pôr imagens no texto](#por-imagens-no-texto).
4. Se a questão se refere a um trecho, escolha o **texto-base** dela no cartão **Texto-base** (veja
   [Textos-base](base-texts.md)); só textos do idioma da questão são oferecidos. Ele aparece antes do
   enunciado, e o mesmo texto pode servir a várias questões.
5. Em **Alternativas**, escolha **Adicionar alternativa** para cada uma (até cinco), digite o texto e marque a
   correta. Use as setas para mover uma alternativa; as letras A a E seguem a ordem.
6. Se quiser, escreva a **explicação**, mostrada junto do gabarito.
7. Escolha **Salvar**. A questão fica salva como **rascunho** e a página vira a de edição dela.

Numa questão **certo/errado** o cartão **Alternativas** dá lugar ao **Gabarito**: escolha **Certo** ou
**Errado** para o enunciado. O gabarito só é exigido para publicar, então um rascunho pode ser salvo sem ele.
A penalidade por resposta errada não faz parte da questão: vem das regras de pontuação da edição. Se trocar o
tipo antes de salvar, o que você digitou para o outro tipo fica na página, mas não é salvo.

Numa questão de **múltiplas respostas** o cartão **Alternativas** tem uma caixa de seleção em cada uma: marque
todas as corretas. Para publicar, ela precisa de três a cinco alternativas com texto, ao menos duas corretas e
ao menos uma errada; um rascunho aceita qualquer marcação. O aluno é avisado de quantas marcar ("Marque 2
alternativas."), contadas a partir das que você marcou. Como uma resposta parcialmente certa pontua não faz
parte da questão: vem das regras de pontuação da edição. Ao voltar para múltipla escolha, só a primeira
alternativa marcada é mantida.

Numa questão de **associação** o cartão **Alternativas** dá lugar ao cartão **Associações**, com duas listas:
1. Em **Correspondências** (à direita), escolha **Adicionar correspondência** para cada item com letra (até 8).
2. Em **Itens** (à esquerda), escolha **Adicionar item** para cada item numerado (até 8) e digite o texto.
3. No seletor de letra de cada item, escolha a correspondência certa. Uma letra já usada por outro item não é
   oferecida: cada letra serve a um item só.
4. Ao lado de cada correspondência aparece "Usada pelo item N" ou "Extra". Correspondências extras, que não
   servem a nenhum item, são permitidas e confundem quem chuta.

Mover uma correspondência leva a letra junto com o texto: o item que a usava passa a mostrar a nova letra.
Remover uma correspondência usada deixa aquele item sem letra. Um rascunho aceita itens sem letra; para
publicar, são de 2 a 8 itens, ao menos tantas correspondências quanto itens, todos com texto e todo item com
a sua letra. Como uma resposta parcialmente certa pontua não faz parte da questão: vem das regras de
pontuação da edição.

Numa questão de **lacunas** a barra do enunciado ganha o botão **Inserir lacuna** e o cartão **Alternativas**
dá lugar ao cartão **Lacunas**:
1. No enunciado, ponha o cursor onde vai a lacuna e escolha **Inserir lacuna**. O editor escreve o marcador
   `[[1]]`; a próxima vez, `[[2]]`, e assim por diante. Você também pode digitar o marcador à mão. Os números
   vão de 1 a 99, não precisam ser seguidos e cada um aparece uma vez só no enunciado.
2. O cartão **Lacunas** mostra uma linha "Lacuna N" para cada marcador, na ordem do enunciado. Em cada uma,
   escolha **Adicionar resposta** e digite uma resposta que a lacuna aceita (até 5 por lacuna, cada uma até
   100 caracteres, texto simples).
3. Maiúsculas e espaços a mais não contam na hora de comparar a resposta do aluno; acentos contam. Para aceitar
   "Brasilia" além de "Brasília", cadastre as duas grafias. Duas respostas iguais nessa comparação na mesma
   lacuna são recusadas.

Apagar um marcador do enunciado tira a linha dele e as respostas que você digitou para ela; digitá-lo de novo
traz uma linha vazia. Marcadores só valem no enunciado: no texto-base, nas alternativas ou na explicação, `[[1]]`
é texto comum. Um rascunho aceita lacunas sem resposta; para publicar, são de 1 a 10 lacunas, cada uma com ao
menos uma resposta. Na lista de questões e na busca, cada marcador aparece como `____`. Como uma resposta
parcialmente certa pontua não faz parte da questão: vem das regras de pontuação da edição.

Numa questão de **resposta curta** o cartão **Alternativas** dá lugar ao cartão **Respostas aceitas**, que começa
com um campo vazio. A barra do enunciado não tem **Inserir lacuna**, e `[[1]]` no enunciado é texto comum:
1. Digite no campo uma resposta que o aluno pode escrever (até 100 caracteres, texto simples). Escolha
   **Adicionar resposta** para cada variante (até 5) e use o botão de remover de cada uma para tirá-la. Um
   campo deixado vazio é ignorado.
2. Maiúsculas e espaços a mais não contam na hora de comparar a resposta do aluno; acentos contam. Para aceitar
   "Brasilia" além de "Brasília", cadastre as duas grafias. Duas respostas iguais nessa comparação são recusadas.
3. Um rascunho aceita uma questão sem resposta; para publicar, é preciso ao menos uma resposta aceita. Se trocar
   o tipo antes de salvar, as respostas que você digitou continuam na página.

Numa questão de **resposta aberta** ou de **redação** o cartão **Alternativas** dá lugar ao cartão **Rubrica**, que
começa com uma linha vazia. Numa redação o enunciado é o tema. A nota da questão é a soma dos pontos dados pelo
corretor dividida pelo total dos pontos máximos; quanto a questão vale numa prova é regra de pontuação da
edição, e não fica guardado aqui. `[[1]]` no enunciado é texto comum:
1. Em cada linha, escreva o **nome do critério** (até 100 caracteres; dois critérios não podem ter o mesmo nome,
   sem contar maiúsculas e espaços a mais), uma **descrição** opcional (até 500 caracteres) e os **pontos** do
   critério, um número inteiro de 1 a 100. Escolha **Adicionar critério** para cada nova linha (até 10) e use o
   botão de remover de cada uma para tirá-la. O cartão mostra "n de 10 critérios" e "Total: n pontos"; o total
   não precisa ser 100. Uma linha deixada vazia é ignorada.
2. Abaixo das linhas, **Tamanho da resposta (opcional)**. Numa redação: **Mínimo de linhas** e **Máximo de linhas**,
   cada um de 1 a 100, o mínimo não acima do máximo. Numa resposta aberta: **Máximo de caracteres**, de 1 a
   5.000, sem mínimo. O limite é uma informação para o simulado; esta tela não o aplica a nenhum aluno.
3. O campo da explicação dessas duas questões se chama **Resposta modelo**: o corretor sempre a vê, e o aluno
   a vê depois de responder.
4. Um rascunho aceita uma rubrica sem critério; para publicar, é preciso ao menos um critério. Se trocar o tipo
   antes de salvar, as linhas que você digitou continuam na página.

Um rascunho pode ser salvo sem alternativas e sem correta, ou sem gabarito. A lista "Antes de publicar", ao
lado do formulário, mostra o que ainda falta; numa questão certo/errado ela tem um item, "Gabarito definido",
numa de múltiplas respostas pede "Ao menos duas corretas e uma errada", numa de associação pede "De 2 a 8
itens à esquerda", "Ao menos o mesmo número de correspondências à direita" e "Todo item tem texto e uma
correspondência", numa de lacunas pede "De 1 a 10 lacunas no enunciado" e "Toda lacuna tem uma resposta
aceita", numa de resposta curta pede "Pelo menos uma resposta aceita", e numa de resposta aberta ou de redação pede
"Pelo menos um critério da rubrica".

### Pôr imagens no texto
O enunciado, o texto-base, cada alternativa e cada item e correspondência de uma associação aceitam imagens; a
explicação não.
1. Na barra do editor, escolha **Inserir imagem** (o botão com o desenho de uma paisagem). A barra pequena das
   alternativas tem o mesmo botão.
2. Em **Escolher imagem**, escolha um arquivo PNG, JPEG, WebP ou GIF de até 2 MB. O tipo é reconhecido pelo
   conteúdo do arquivo, não pelo nome: um texto renomeado para `.png` é recusado, e SVG não é aceito.
3. Escreva o **texto alternativo**, a descrição lida em voz alta para quem não vê a imagem (até 250
   caracteres), ou marque **Imagem decorativa** se ela não traz informação.
4. Escolha **Inserir**. A imagem aparece no editor, no lugar do cursor.
5. Para tirar uma imagem, apague-a no editor e salve.

Colar ou arrastar um arquivo de imagem para dentro do editor não insere nada: só o diálogo insere.

A imagem é enviada quando você a escolhe, antes de salvar a questão. Uma imagem que nenhum texto salvo
guarda é apagada sozinha depois de 24 horas. Cada questão tem no máximo 10 imagens (enunciado, alternativas e
itens juntos), e cada texto-base também. Uma alternativa só com uma imagem conta como preenchida.

Ao apagar uma imagem de uma questão **publicada**, o arquivo continua guardado enquanto alguma versão anterior
da questão ainda o usa: quem fez a prova com aquela versão continua vendo a imagem. Num rascunho, o arquivo é
apagado de vez. A imagem aparece na visualização como aluno, na fila de revisão e no texto-base, reduzida à
largura da coluna, e só quem gerencia ou revisa o banco de questões a vê.

### Registrar de onde a questão veio
Uma questão de prova anterior guarda a edição de onde veio, o número dela na prova e, se quiser, a disciplina do
edital. Uma questão sem edição é uma questão autoral.
1. No cartão **Origem**, em **Edição**, digite o exame, a banca ou o ano e escolha na lista. Edições em
   rascunho e publicadas aparecem; a lista mostra a sigla da banca, o exame e o ano.
2. Informe o **número na prova**, de 1 a 500. Ele é obrigatório quando há edição, e cada número é usado uma vez
   por edição: um número de uma questão excluída (rascunho) volta a ficar livre.
3. Se quiser, escolha a **disciplina do edital**, entre as da edição escolhida.
4. Escolha **Salvar**. Trocar a edição, ou esvaziá-la, limpa o número e a disciplina do edital.
5. Para tornar a questão autoral de novo, esvazie a **Edição** e salve. O número fica livre.

A origem não impede de publicar: uma questão publicada pode vir de uma edição ainda em rascunho. O lado direito
do formulário mostra a **Fonte** (a linha "banca · exame · ano" ou "Autoral"), e a visualização como aluno a
mostra acima do enunciado, com o número.

A edição também lista as suas questões: na página da edição (**Exames**, o exame, **Editar** a edição) o cartão
**Questões** mostra, na ordem da prova, as questões que vieram dela, com **Adicionar questão**. O cartão
aparece para quem também gerencia o banco de questões.

### Transcrever uma prova inteira
Use para digitar todas as questões de uma prova anterior de uma vez, sem reabrir o formulário a cada uma.
1. Abra uma edição de prova já salva e, no cartão **Questões** dela, escolha **Transcrever**.
2. O formulário abre com a edição fixa e o próximo número na prova já preenchido (1 quando a edição não tem questões). Você pode alterar o número.
3. Preencha a questão e escolha **Salvar e continuar**. A questão é salva como rascunho, o contador "Questão N desta sessão" aumenta e o formulário é limpo para a próxima.
4. A edição, a disciplina do edital, o idioma e o texto-base continuam como estavam; o resto volta em branco. Um bloco de leitura com várias questões só precisa do texto escolhido uma vez; troque ou retire-o quando o bloco acabar. Troque a disciplina do edital sempre que a prova passar para outra.
5. Escolha **Concluir** ao terminar. Toda questão já salva continua no banco como rascunho.

Os cinco tipos de questão podem ser transcritos. O contador existe apenas na tela: se você sair e voltar, ele recomeça em 1 e o número sugerido segue o que já está salvo.

### Publicar e despublicar
1. Abra a questão e veja o bloco **Publicação**, na coluna **Resumo** ao lado do formulário (abaixo do formulário,
   quando a janela tem menos de 1280 px). As seções do formulário são numeradas de 1 a 6.
2. **Publicar** é o botão âmbar da tela, só em um rascunho salvo. Fica disponível quando a questão está salva e a lista está completa: tópico, idioma,
   enunciado, de duas a cinco alternativas, todas com texto e exatamente uma correta (numa questão
   certo/errado: o gabarito definido; numa de múltiplas respostas: de três a cinco alternativas, ao menos duas
   corretas e ao menos uma errada; numa de associação: de 2 a 8 itens, ao menos tantas correspondências, todos
   com texto e com letra; numa de lacunas: de 1 a 10 lacunas, cada uma com ao menos uma resposta; numa de resposta curta: ao menos
   uma resposta aceita; numa de resposta aberta ou de redação: ao menos um critério com nome e pontos).
   **Salvar** nunca publica.
3. Uma questão publicada ainda pode ser editada, mas toda edição precisa mantê-la completa.
4. **Despublicar** devolve a questão a rascunho com o mesmo conteúdo.

### Enviar para revisão
A publicação direta continua como está. Enviar para revisão é um segundo caminho: a questão só é publicada
depois que outra pessoa, com a permissão "Revisar questões", a aprova.
1. Abra um rascunho salvo e, no bloco **Publicação**, escolha **Enviar para revisão**. O botão fica
   desabilitado pelos mesmos motivos de **Publicar**: alterações não salvas e lista "Antes de publicar"
   incompleta; a linha de motivo embaixo vale para os dois.
2. A questão passa a **Em revisão**: um aviso amarelo diz que ela não pode ser editada, todos os campos ficam
   só para leitura e o bloco mostra desde quando ela espera um revisor.
3. Enquanto estiver em revisão, a questão não pode ser salva, publicada, despublicada nem excluída, e a lista
   não oferece Publicar nem Despublicar para ela. **Visualizar como aluno** e o histórico continuam
   funcionando. Um tópico usado só por uma questão em revisão também não pode ser excluído.
4. Para mudar algo, escolha **Retirar da revisão**. Não pede motivo: a questão volta a rascunho, com o mesmo
   conteúdo, e pode ser editada.
5. Se o revisor **rejeitar**, a questão volta a rascunho e um aviso azul no bloco **Publicação** mostra a data
   e o motivo da rejeição. Corrija e envie de novo; o aviso some no próximo envio.
6. Se o revisor **aprovar**, a questão é publicada na hora (versão 1), com a observação do revisor, se houver,
   guardada no histórico.
7. O cartão **Histórico de revisão**, abaixo do formulário e antes do cartão **Histórico**, lista da mais nova para
   a mais antiga cada envio, retirada, aprovação e rejeição, com a data e o motivo ou a observação. Ele só
   aparece quando a questão já teve alguma dessas etapas. O nome de quem agiu é guardado, mas ainda não é
   exibido.

### Versões e trocas de gabarito
Todo estado em que uma questão publicada foi exibida fica guardado, de modo que uma tentativa de prova sempre
possa ser ligada ao texto e ao gabarito exatos que a corrigiram. O cartão **Histórico**, abaixo do cartão
**Publicação**, lista as versões de uma questão salva, da mais nova para a mais antiga, com a data, o que
mudou e o motivo.
1. Na primeira vez que a questão é publicada, ela vira a **versão 1**, "Primeira publicação". Um rascunho que
   nunca foi publicado não tem versão.
2. Salvar uma questão publicada registra uma nova versão somente quando mudou o que o aluno vê ou como é
   corrigido: o enunciado, o texto-base, as alternativas e o gabarito, a explicação ou o idioma. Mudar só o
   tópico, a dificuldade ou a origem não registra. Salvar sem mudança não registra nada.
3. Se você despublica, edita e publica de novo, uma nova versão é registrada na publicação quando o conteúdo
   difere da última versão.
4. Para trocar o gabarito de uma questão publicada (após um recurso, por exemplo), marque outra alternativa
   como correta ou inverta Certo/Errado. Aparece o campo **Motivo da troca de gabarito** sob as alternativas
   (ou sob o gabarito); ele é obrigatório, até 500 caracteres. **Salvar** registra a versão como "Gabarito
   alterado" com o seu motivo e a data. Mover a alternativa correta por reordenação também conta como troca.
   Numa associação, trocar a letra de um item, acrescentar ou remover um item, ou mover uma correspondência
   usada para outra letra conta como troca; mudar só o texto de um item não. Numa de lacunas, acrescentar ou
   remover uma lacuna, ou acrescentar, remover ou trocar uma resposta aceita, conta como troca; mudar o texto em
   volta dos marcadores, a ordem deles, a ordem das respostas ou só as maiúsculas de uma resposta não. Numa de
   resposta curta, acrescentar ou remover uma resposta aceita conta como troca; mudar o enunciado, a ordem das
   respostas ou só as maiúsculas de uma resposta não. Numa de resposta aberta ou de redação, acrescentar,
   remover ou renomear um critério, ou mudar os pontos dele, conta como troca; mudar só a descrição de um
   critério, a ordem dos critérios, o enunciado, a resposta modelo ou o limite de tamanho não, embora tudo isso
   registre uma versão.
5. Se o gabarito mudou enquanto a questão era rascunho, **Publicar** é recusado na primeira vez com "Escreva o
   motivo da troca de gabarito." O campo de motivo aparece no bloco **Publicação**, acima dos botões; digite-o e escolha
   **Publicar** de novo.
6. No cartão **Histórico**, escolha **Ver** numa linha para abrir aquela versão como foi registrada, só para
   leitura. A resposta correta fica oculta até **Mostrar resposta**. A versão atual leva a marca **Atual**.
7. Um rascunho que tem versões pode ser excluído; as versões ficam guardadas, e o histórico não é mais exibido
   depois que a questão some. Uma questão publicada continua sem poder ser excluída.

Versões nunca são editadas nem removidas. O nome de quem fez a mudança é guardado, mas ainda não é exibido.

### Anular e reintegrar uma questão
Quando a banca de uma prova anula uma questão, registre isso para que a prática deixe de usá-la. A questão
continua no banco, na edição e no histórico; só a situação dela muda.
1. Abra uma questão **publicada** e olhe o bloco **Publicação**. Um rascunho não pode ser anulado.
2. Escolha **Anular questão**. Se o formulário tem alterações não salvas, o botão fica desabilitado e o bloco
   diz "Salve suas alterações primeiro."
3. Na janela, escreva o **motivo** (até 500 caracteres; é obrigatório) e escolha **Anular**. A questão passa a
   **Anulada**: um aviso amarelo no alto diz que ela não pode ser editada, o bloco mostra a data e o motivo,
   todos os campos ficam só para leitura e o cartão Histórico ganha uma linha "Anulada" com o motivo.
4. Enquanto estiver anulada, a questão não pode ser salva, publicada, despublicada nem excluída, e a lista não
   oferece Publicar nem Despublicar para ela. **Visualizar como aluno** continua funcionando e mostra a data e
   o motivo acima do enunciado. Ela mantém o tópico, a edição e o número na prova.
5. Para desfazer (um engano, ou a banca reverteu a decisão), escolha **Reintegrar questão**, escreva o motivo e
   confirme. A questão volta a ser **Publicada**, pode ser editada, e o cartão Histórico mostra uma linha
   "Reintegrada" com esse motivo.
6. Na lista, filtre a **Situação** por **Anulada** para vê-las. As questões em prática são as publicadas;
   como uma questão anulada conta num simulado completo é regra de pontuação da edição, que não é definida
   aqui.

A data mostrada é o dia em que você registrou a anulação, não a data oficial da banca; o motivo é o lugar de
citar o ato oficial.

### Visualizar como aluno
1. No cartão **Publicação**, escolha **Visualizar como aluno**.
2. A janela mostra o texto-base, o enunciado e as alternativas como o aluno vai ver; uma questão certo/errado
   mostra duas linhas, Certo e Errado, uma de múltiplas respostas começa com "Marque N alternativas." acima
   das alternativas, uma de associação mostra "Associe cada número a uma letra.", os itens numerados e as
   correspondências com letra, na ordem em que foram escritos, e uma de lacunas mostra no enunciado uma caixa
   sublinhada com o número de cada lacuna, sem pista do tamanho da resposta, e "Preencha cada lacuna.", e uma de resposta curta mostra uma caixa vazia e
   desabilitada, "Sua resposta", com a linha "Digite sua resposta.", e uma de resposta aberta ou de redação
   mostra uma caixa de várias linhas vazia e desabilitada, "Sua resposta" ou "Sua redação", a linha "Escreva sua
   resposta." ou "Escreva sua redação." e, se houver limite, a linha dele ("Até 500 caracteres.", "Entre 7 e 30
   linhas.", "Pelo menos 7 linhas." ou "Até 30 linhas.").
3. Escolha **Mostrar resposta** para ver a alternativa correta (ou o gabarito; numa associação, "1 → C" ao lado
   de cada item e a marca em cada correspondência usada; numa de lacunas, a primeira resposta aceita dentro de
   cada caixa e a lista "Lacuna N: resposta, resposta"; numa de resposta curta, a lista "Respostas aceitas:
   resposta, resposta"; numa de resposta aberta ou de redação, a tabela "Rubrica de correção" com cada critério,
   a descrição e os pontos máximos, e o total, ou "ainda sem rubrica" num rascunho sem critério) e a explicação
   (a resposta modelo, nessas duas). A visualização sempre mostra a
   versão salva; salve antes para ver as últimas alterações.

### Excluir uma questão
1. Só um rascunho pode ser excluído. Numa questão publicada o botão Excluir fica desabilitado; despublique antes.
   Uma questão anulada não pode ser excluída; reintegre-a, despublique e só então exclua, se precisar.
2. Escolha **Excluir** na linha e confirme. O tópico permanece.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Tipo de questão | Como a questão é respondida | Múltipla escolha, certo/errado, múltiplas respostas, associação, lacunas, resposta curta, resposta aberta ou redação. Escolhido ao criar a questão; não pode ser alterado depois |
| Disciplina | Serve para escolher o tópico | Obrigatória na tela; a questão guarda só o tópico |
| Tópico | O tópico a que a questão pertence | Obrigatório. Um tópico não pode ser excluído enquanto uma questão o usa |
| Idioma do conteúdo | O idioma em que a questão está escrita | Obrigatório, um dos idiomas do app. O conteúdo nunca é traduzido |
| Dificuldade | Quão difícil é a questão | Opcional, um número inteiro de 1 a 5 |
| Motivo (anular ou reintegrar) | Por que uma questão publicada foi anulada, ou uma anulada foi retomada | Obrigatório, de 1 a 500 caracteres. Fica no cartão Histórico com a data |
| Edição | A prova anterior de onde a questão veio | Opcional. Sem edição a questão é autoral. Uma edição não pode ser excluída enquanto uma questão a usa |
| Número na prova | A posição da questão na prova da edição | Obrigatório com edição, um número inteiro de 1 a 500, único entre as questões da edição |
| Disciplina do edital | A disciplina do edital da edição a que a questão pertence | Opcional, só com edição, e da mesma edição. Não pode ser excluída enquanto uma questão a usa |
| Enunciado | O que se pede ao aluno | Obrigatório, até 10.000 caracteres |
| Texto-base | Um trecho a que a questão se refere, escrito uma vez e compartilhado por toda questão que o liga | Opcional. Escolhido entre os textos-base do idioma da questão; veja [Textos-base](base-texts.md) |
| Alternativas | As escolhas, letras A a E | Até 5. Para publicar: de 2 a 5, todas com texto, exatamente uma correta (múltiplas respostas: de 3 a 5, ao menos duas corretas e ao menos uma errada). Cada uma até 2.000 caracteres |
| Itens (associação) | Os itens numerados da esquerda, cada um com a letra da sua correspondência | Até 8. Para publicar: de 2 a 8, todos com texto e com letra; cada letra serve a um item só. Cada um até 2.000 caracteres |
| Correspondências (associação) | Os itens com letra da direita | Até 8. Para publicar: ao menos tantos quanto os itens, todos com texto; os que nenhum item usa são extras. Cada um até 2.000 caracteres |
| Lacunas | Os marcadores `[[N]]` do enunciado, um por lacuna | Só em lacunas. Até 10, números de 1 a 99, cada um uma vez. Para publicar: de 1 a 10, cada uma com resposta |
| Respostas aceitas | O que uma lacuna aceita como certo | Até 5 por lacuna, texto simples, cada uma até 100 caracteres. Maiúsculas e espaços a mais não contam; acentos contam |
| Respostas aceitas (resposta curta) | O que o aluno pode digitar como certo | De 0 a 5, texto simples, cada uma até 100 caracteres. Maiúsculas e espaços a mais não contam; acentos contam. Para publicar: ao menos uma |
| Critérios da rubrica | O que o corretor avalia numa resposta aberta ou numa redação | Até 10, na ordem escrita. Nome: de 1 a 100 caracteres, único na rubrica sem contar maiúsculas e espaços a mais. Descrição: opcional, até 500. Pontos: inteiro de 1 a 100. Para publicar: ao menos um critério |
| Tamanho da resposta | O limite de escrita de um aluno | Opcional. Redação: mínimo e máximo de linhas, de 1 a 100, o mínimo não acima do máximo. Resposta aberta: máximo de caracteres, de 1 a 5.000, sem mínimo |
| Gabarito | Se o enunciado de uma questão certo/errado está certo ou errado | Só em certo/errado. Pode ficar vazio no rascunho; obrigatório para publicar. Uma questão publicada não pode perdê-lo |
| Explicação | Por que o gabarito está certo (numa resposta aberta ou numa redação, a **Resposta modelo**: o corretor sempre a vê, o aluno depois de responder) | Opcional, até 10.000 caracteres |
| Motivo da troca de gabarito | Por que o gabarito de uma questão publicada mudou | Obrigatório quando um salvamento ou uma publicação registra uma versão cujo gabarito difere da anterior; de 1 a 500 caracteres. Um motivo em qualquer outra versão nova fica como nota |

Só a formatação simples é mantida: scripts, estilos e qualquer link que não seja `http` ou `https` são
removidos ao salvar, e a página mostra o que foi mantido.

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Escreva o enunciado. | O enunciado está vazio. | Escreva o que se pede ao aluno. |
| O arquivo não é uma imagem PNG, JPEG, WebP ou GIF. | O conteúdo do arquivo não é de um desses formatos. | Escolha outro arquivo. |
| A imagem tem mais de 2 MB. | O arquivo passou do limite. | Reduza a imagem ou escolha outra. |
| Escreva o texto alternativo ou marque a imagem como decorativa. | O diálogo foi confirmado sem texto alternativo. | Descreva a imagem ou marque **Imagem decorativa**. |
| Toda imagem precisa de texto alternativo ou deve ser marcada como decorativa. | Uma imagem do texto ficou sem alternativo. | Reinsira a imagem com o texto alternativo. |
| O texto alternativo de uma imagem tem no máximo 250 caracteres. | O texto alternativo passou do limite. | Encurte-o. |
| Uma imagem do texto não existe mais. Insira-a de novo. | A imagem foi apagada (por exemplo, a espera de 24 horas passou). | Insira a imagem de novo. |
| Uma questão tem no máximo 10 imagens. | Enunciado e alternativas somam mais de 10. | Tire algumas. |
| O enunciado passa de 10.000 caracteres. (e os limites da explicação e da alternativa) | O texto passou do limite. | Encurte-o. Nada é cortado por você. |
| Uma questão é publicada com 2 a 5 alternativas. | Menos de duas alternativas, ou mais de cinco. | Adicione ou remova alternativas. |
| Toda alternativa precisa de texto. | Uma questão publicada tem alternativa vazia. | Escreva o texto ou remova a alternativa. |
| Marque exatamente uma alternativa como correta. | Nenhuma correta, ou mais de uma, numa questão de múltipla escolha. | Marque uma. |
| Uma questão de múltiplas respostas é publicada com 3 a 5 alternativas. | Menos de três alternativas. | Acrescente alternativas. |
| Marque ao menos duas alternativas como corretas e deixe ao menos uma errada. | Menos de duas corretas, ou todas as alternativas marcadas. | Marque duas ou mais e deixe uma errada. |
| Uma questão de associação tem de 2 a 8 itens à esquerda e, à direita, ao menos o mesmo número e no máximo 8. | Itens ou correspondências demais, ou poucos para publicar. | Acrescente ou remova itens ou correspondências. |
| Todo item precisa de uma correspondência. | Um item ficou sem letra, ou com uma letra que não existe. | Escolha a letra de cada item. |
| Cada letra só pode ser associada a um item. | Dois itens escolheram a mesma letra. | Troque a letra de um deles. |
| Escreva o texto do item. | Uma associação publicada tem item vazio. | Escreva o texto ou remova o item. |
| Um item passa de 2.000 caracteres. | O texto do item passou do limite. | Encurte-o. |
| Só uma questão de associação tem itens à esquerda. | Um salvamento de outro tipo trouxe itens. | Recarregue a página e salve de novo. |
| Uma questão de lacunas tem de 1 a 10 lacunas no enunciado. | Mais de 10 marcadores, ou nenhum ao publicar. | Insira ou apague marcadores. |
| Cada número de lacuna aparece uma só vez no enunciado. | O mesmo marcador foi escrito duas vezes. | Apague um deles ou troque o número. |
| Uma lacuna não tem marcador no enunciado. | Uma lacuna foi enviada sem o marcador dela. | Recarregue a página e salve de novo. |
| Toda lacuna precisa de uma resposta aceita. | Uma lacuna ficou sem resposta ao publicar. | Escreva ao menos uma resposta naquela lacuna. |
| Uma lacuna aceita no máximo 5 respostas. | Respostas demais numa lacuna. | Remova algumas. |
| Escreva a resposta ou remova-a. | Uma resposta ficou vazia. | Escreva o texto ou remova a resposta. |
| Uma resposta passa de 100 caracteres. | A resposta passou do limite. | Encurte-a. |
| Duas respostas desta lacuna são iguais quando se ignoram maiúsculas e espaços extras. | Duas respostas da mesma lacuna só diferem em maiúsculas ou espaços. | Remova uma delas. |
| Só uma questão de lacunas tem lacunas. | Um salvamento de outro tipo trouxe lacunas. | Recarregue a página e salve de novo. |
| Escreva pelo menos uma resposta aceita. | Uma questão de resposta curta ficou sem resposta ao publicar. | Escreva ao menos uma resposta. |
| Uma resposta curta aceita no máximo 5 respostas. | Respostas demais numa questão de resposta curta. | Remova algumas. |
| Duas respostas são iguais quando se ignoram maiúsculas e espaços extras. | Duas respostas de uma questão de resposta curta só diferem em maiúsculas ou espaços. | Remova uma delas. |
| Uma rubrica tem no máximo 10 critérios. | Critérios demais numa resposta aberta ou numa redação. | Remova alguns. |
| Escreva pelo menos um critério para publicar. | Uma questão de resposta aberta ou de redação ficou sem critério ao publicar. | Escreva ao menos um critério, com nome e pontos. |
| Escreva um nome para o critério. | Um critério tem os pontos ou a descrição, mas não o nome. | Escreva o nome ou apague a linha. |
| O nome de um critério tem no máximo 100 caracteres. / A descrição de um critério tem no máximo 500 caracteres. | O texto passou do limite. | Encurte-o. |
| Os pontos de um critério são um número inteiro de 1 a 100. | Os pontos faltam, ou estão fora da faixa. | Digite um número inteiro de 1 a 100. |
| Dois critérios têm o mesmo nome. | Dois nomes só diferem em maiúsculas ou espaços. | Renomeie ou remova um deles. |
| O limite de tamanho está fora da faixa, ou o mínimo está acima do máximo. | O limite de caracteres ou de linhas não cabe na faixa do tipo, ou há um mínimo numa resposta aberta. | Use de 1 a 100 linhas (redação) ou de 1 a 5.000 caracteres (resposta aberta), sem mínimo na resposta aberta. |
| Só uma questão de resposta aberta ou uma redação tem rubrica. / ...tem limite de tamanho. | Um salvamento de outro tipo trouxe uma rubrica ou um limite. | Recarregue a página e salve de novo. |
| Defina o gabarito (certo ou errado) para publicar. | A questão certo/errado não tem gabarito. | Escolha Certo ou Errado em Gabarito. |
| O tipo de uma questão não pode ser alterado. | Um salvamento citou outro tipo que o guardado. | Recarregue a página; crie uma questão nova para o outro tipo. |
| Uma questão certo/errado não tem alternativas. / Uma questão de múltipla escolha não tem gabarito certo/errado. | Um salvamento trouxe uma resposta que o tipo não tem. | Recarregue a página e salve de novo. |
| A dificuldade é um número de 1 a 5, ou vazia. | A dificuldade está fora da faixa. | Escolha de 1 a 5 ou deixe vazia. |
| Escolha um dos idiomas suportados. | O idioma não está na lista. | Escolha um idioma da lista. |
| O tópico não existe mais. Escolha outro. | O tópico foi excluído enquanto você escrevia. | Escolha outro tópico. |
| A edição não existe mais. Escolha outra. | A edição foi excluída enquanto você escrevia. | Escolha outra edição. |
| A disciplina do edital não pertence a esta edição ou não existe mais. | A disciplina foi excluída ou é de outra edição. | Escolha outra disciplina ou deixe vazio. |
| Informe o número da questão na prova. | Há edição e falta o número. | Digite o número. |
| O número é um inteiro de 1 a 500. | O número está fora da faixa. | Digite um número de 1 a 500. |
| Número já usado nesta edição. | Outra questão da edição tem este número. | Escolha outro número, ou corrija a outra questão. |
| Escolha uma edição para informar um número ou uma disciplina do edital. | Há número ou disciplina e nenhuma edição. | Escolha a edição ou esvazie os campos. |
| Escreva o motivo da troca de gabarito. | O gabarito mudou e nenhum motivo foi informado. | Digite o motivo, até 500 caracteres, e salve ou publique de novo. |
| O motivo é longo demais (no máximo 500 caracteres). | O motivo tem mais de 500 caracteres. | Encurte-o. |
| Esta versão não existe. | Pediu-se a versão por um número que a questão não tem. | Recarregue a página. |
| Outra gravação alterou o histórico desta questão antes. Recarregue a página e tente de novo. | Dois salvamentos tentaram registrar o mesmo número de versão. | Recarregue a página e salve de novo. |
| Escreva o motivo. | O motivo de uma anulação ou de uma reintegração está vazio. | Digite o motivo, até 500 caracteres. |
| Esta questão está anulada e não pode ser alterada. Reintegre-a primeiro. | Você tentou salvar, publicar, despublicar ou excluir uma questão anulada. | Escolha **Reintegrar questão** primeiro, se a anulação foi um engano. |
| Esta questão já está anulada. / Esta questão não está anulada. | Outra pessoa a alterou antes. | Recarregue a página. |
| Só um rascunho pode ser excluído. Despublique a questão primeiro. | A questão está publicada. | Despublique e depois exclua. |
| Esta questão já está publicada. / Esta questão não está publicada. | Outra pessoa mudou antes de você. | Recarregue a página. |
| Esta questão está em revisão e não pode ser alterada. Retire-a da revisão primeiro. | Você tentou salvar, publicar, despublicar ou excluir uma questão em revisão. | Escolha **Retirar da revisão** e tente de novo. |
| Esta questão já está em revisão. | Ela foi enviada antes, por você ou por outra pessoa. | Recarregue a página. |
| Esta questão não está em revisão. | Outra pessoa a retirou ou decidiu antes de você. | Recarregue a página. |
| Esta questão está em revisão. Retire-a da revisão antes de excluir. | Excluir está desabilitado para uma questão em revisão. | Retire-a da revisão e depois exclua. |
| Esta questão não existe mais. | Alguém a excluiu com a sua tela aberta. | Volte à lista. |
| Só endereços http e https. | O link digitado não é um endereço da web. | Digite um endereço que comece com https://. |
| Página não encontrada | A sua conta não gerencia o banco de questões. | Fale com um administrador se achar que deveria. |

## Usar o editor pelo teclado
- A barra de ferramentas é uma parada só: use as setas esquerda e direita entre os botões, Home e End para saltar.
- Tab na barra entra no texto; Tab de novo sai do editor, o foco nunca fica preso.
- Ctrl+B, Ctrl+I e Ctrl+U alternam negrito, itálico e sublinhado.
- Numa questão de lacunas, **Inserir lacuna** fica na barra depois de **Inserir imagem**: chegue até ele com
  as setas e use Espaço ou Enter para escrever o marcador onde está o cursor.
- Um campo com erro avisa ao sair dele, e um salvamento que falha leva o foco à lista de erros.

## Páginas relacionadas
- [Fila de revisão](review-queue.md): revisar, aprovar e rejeitar questões enviadas para revisão
- [Textos-base](base-texts.md): os trechos que várias questões compartilham
- [Disciplinas e tópicos](subjects.md): as disciplinas e os tópicos a que uma questão aponta
- [Edições de um exame](exam-editions.md): as provas de onde as questões vêm
- [Papéis e permissões](roles.md): quem pode gerenciar o banco de questões
- [Como navegar](getting-around.md): menu, modo claro e escuro, idioma e teclado
- [Simulab](index.md)
