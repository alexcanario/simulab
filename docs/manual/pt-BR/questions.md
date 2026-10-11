---
page: questions
locale: pt-BR
features: [F-95]
updated: 2026-10-11
---
# Questões

O banco de questões é onde os curadores escrevem as questões de que provas e treinos são feitos. Uma questão
tem enunciado, texto-base opcional, de duas a cinco alternativas com uma correta, explicação opcional,
dificuldade, tópico e o idioma em que está escrita. Esta página trata das questões de escolha única.

## Quem pode usar
Uma conta com a permissão "Gerenciar o banco de questões". Curadores e Administradores a têm. Todas as outras
veem Página não encontrada, e o menu não tem o item Questões.

## Como fazer
### Ver as questões
1. Escolha **Questões**, na seção **Conteúdo** do menu lateral.
2. A lista mostra o início de cada enunciado, o tópico com a disciplina, a dificuldade, o idioma e a situação,
   da mais nova para a mais antiga.
3. Digite na caixa de busca para achar uma palavra do enunciado. Maiúsculas e acentos não importam.
4. Use **Situação** para ver só rascunhos ou só questões publicadas.

### Escrever uma questão
1. Escolha **Adicionar**.
2. Escolha a **disciplina** e depois o **tópico** (a lista mostra só os tópicos daquela disciplina). O
   **idioma do conteúdo** começa com o seu idioma; troque pelo idioma em que a questão está escrita. Informe
   uma **dificuldade** de 1 (mais fácil) a 5 (mais difícil), se quiser.
3. Escreva o **enunciado**. A barra oferece negrito, itálico, sublinhado, tachado, sobrescrito, subscrito,
   listas, citação, link e limpar formatação. Imagens ainda não são aceitas.
4. Escreva um **texto-base** se a questão se refere a um. Ele aparece antes do enunciado.
5. Em **Alternativas**, escolha **Adicionar alternativa** para cada uma (até cinco), digite o texto e marque a
   correta. Use as setas para mover uma alternativa; as letras A a E seguem a ordem.
6. Se quiser, escreva a **explicação**, mostrada junto do gabarito.
7. Escolha **Salvar**. A questão fica salva como **rascunho** e a página vira a de edição dela.

Um rascunho pode ser salvo sem alternativas e sem correta. A lista "Antes de publicar", ao lado do formulário,
mostra o que ainda falta.

### Publicar e despublicar
1. Abra a questão e veja o cartão **Publicação**, abaixo do formulário.
2. **Publicar** fica disponível quando a questão está salva e a lista está completa: tópico, idioma,
   enunciado, de duas a cinco alternativas, todas com texto e exatamente uma correta. **Salvar** nunca publica.
3. Uma questão publicada ainda pode ser editada, mas toda edição precisa mantê-la completa.
4. **Despublicar** devolve a questão a rascunho com o mesmo conteúdo.

### Visualizar como aluno
1. No cartão **Publicação**, escolha **Visualizar como aluno**.
2. A janela mostra o texto-base, o enunciado e as alternativas como o aluno vai ver.
3. Escolha **Mostrar resposta** para ver a alternativa correta e a explicação. A visualização sempre mostra a
   versão salva; salve antes para ver as últimas alterações.

### Excluir uma questão
1. Só um rascunho pode ser excluído. Numa questão publicada o botão Excluir fica desabilitado; despublique antes.
2. Escolha **Excluir** na linha e confirme. O tópico permanece.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Disciplina | Serve para escolher o tópico | Obrigatória na tela; a questão guarda só o tópico |
| Tópico | O tópico a que a questão pertence | Obrigatório. Um tópico não pode ser excluído enquanto uma questão o usa |
| Idioma do conteúdo | O idioma em que a questão está escrita | Obrigatório, um dos idiomas do app. O conteúdo nunca é traduzido |
| Dificuldade | Quão difícil é a questão | Opcional, um número inteiro de 1 a 5 |
| Enunciado | O que se pede ao aluno | Obrigatório, até 10.000 caracteres |
| Texto-base | Um texto a que a questão se refere | Opcional, até 20.000 caracteres |
| Alternativas | As escolhas, letras A a E | Até 5. Para publicar: de 2 a 5, todas com texto, exatamente uma correta. Cada uma até 2.000 caracteres |
| Explicação | Por que o gabarito está certo | Opcional, até 10.000 caracteres |

Só a formatação simples é mantida: scripts, estilos e qualquer link que não seja `http` ou `https` são
removidos ao salvar, e a página mostra o que foi mantido.

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Escreva o enunciado. | O enunciado está vazio. | Escreva o que se pede ao aluno. |
| O enunciado passa de 10.000 caracteres. (e os limites do texto-base, da explicação e da alternativa) | O texto passou do limite. | Encurte-o. Nada é cortado por você. |
| Uma questão é publicada com 2 a 5 alternativas. | Menos de duas alternativas, ou mais de cinco. | Adicione ou remova alternativas. |
| Toda alternativa precisa de texto. | Uma questão publicada tem alternativa vazia. | Escreva o texto ou remova a alternativa. |
| Marque exatamente uma alternativa como correta. | Nenhuma correta, ou mais de uma. | Marque uma. |
| A dificuldade é um número de 1 a 5, ou vazia. | A dificuldade está fora da faixa. | Escolha de 1 a 5 ou deixe vazia. |
| Escolha um dos idiomas suportados. | O idioma não está na lista. | Escolha um idioma da lista. |
| O tópico não existe mais. Escolha outro. | O tópico foi excluído enquanto você escrevia. | Escolha outro tópico. |
| Só um rascunho pode ser excluído. Despublique a questão primeiro. | A questão está publicada. | Despublique e depois exclua. |
| Esta questão já está publicada. / Esta questão não está publicada. | Outra pessoa mudou antes de você. | Recarregue a página. |
| Esta questão não existe mais. | Alguém a excluiu com a sua tela aberta. | Volte à lista. |
| Só endereços http e https. | O link digitado não é um endereço da web. | Digite um endereço que comece com https://. |
| Página não encontrada | A sua conta não gerencia o banco de questões. | Fale com um administrador se achar que deveria. |

## Usar o editor pelo teclado
- A barra de ferramentas é uma parada só: use as setas esquerda e direita entre os botões, Home e End para saltar.
- Tab na barra entra no texto; Tab de novo sai do editor, o foco nunca fica preso.
- Ctrl+B, Ctrl+I e Ctrl+U alternam negrito, itálico e sublinhado.
- Um campo com erro avisa ao sair dele, e um salvamento que falha leva o foco à lista de erros.

## Páginas relacionadas
- [Disciplinas e tópicos](subjects.md): as disciplinas e os tópicos a que uma questão aponta
- [Papéis e permissões](roles.md): quem pode gerenciar o banco de questões
- [Como navegar](getting-around.md): menu, modo claro e escuro, idioma e teclado
- [Simulab](index.md)
