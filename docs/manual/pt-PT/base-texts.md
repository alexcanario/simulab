---
page: base-texts
locale: pt-PT
features: [F-102]
updated: 2026-10-15
frozen: 2026-10-17 (v1.1.0)
---
# Textos de apoio

> Congelada em 2026-10-17 (v1.1.0). Esta página pode estar desatualizada; consulte a [versão em pt-BR](../pt-BR/base-texts.md).

Um texto de apoio é um excerto, uma tabela ou um caso a que várias questões se referem, como num bloco de
interpretação de texto. Escreve-o uma vez, dá-lhe um título e liga cada questão a ele; todas as questões
ligadas mostram o texto antes do enunciado. Corrigir uma palavra no texto de apoio corrige-a para todas elas.

## Quem pode usar
Uma conta com a permissão «Gerir o banco de questões». Os curadores e os administradores têm-na. As outras
veem Página não encontrada, e o menu não tem o item Textos de apoio.

## Como fazer
### Ver os textos de apoio
1. Escolha **Textos de apoio**, na secção **Conteúdo** do menu lateral, logo a seguir a **Questões**.
2. A lista mostra o título, o idioma e quantas questões usam cada texto, por ordem de título.
3. Escreva na caixa de pesquisa para encontrar uma palavra do título (maiúsculas e acentos não contam), ou use
   **Idioma** para ver os textos de um idioma.

### Escrever um texto de apoio
1. Escolha **Adicionar**.
2. Escreva o **título**, até 200 caracteres. Serve só para si encontrar o texto; o estudante não o vê.
3. Escolha o **idioma do conteúdo**: o idioma em que o texto está escrito. Só as questões do mesmo idioma o
   podem usar.
4. Escreva o **texto** no editor. A barra oferece negrito, itálico, sublinhado, rasurado, sobrescrito,
   subscrito, listas, citação, ligação e limpar formatação. As imagens ainda não são aceites.
5. Escolha **Guardar**. A página passa a ser a página de edição do texto.

### Ligar uma questão a um texto de apoio
1. Abra a questão (**Questões**, depois **Adicionar** ou **Editar** numa linha).
2. No cartão **Texto de apoio**, a seguir ao enunciado, clique no campo e escreva parte do título. Só são
   oferecidos os textos do idioma da questão, cada um com o número de questões que já o usam.
3. Escolha um e prima **Guardar**. **Abrir o texto de apoio** mostra-o noutro separador e **Mostrar o texto**
   lê-o sem sair da questão.
4. Para retirar a ligação, prima o **x** do campo e guarde. O texto continua a existir.
5. O texto que quer não está lá? Escolha **Criar um texto de apoio**. Abre noutro separador, por isso a
   questão que está a escrever fica como está: guarde o texto aí, volte e escolha-o.

Se mudar o idioma do conteúdo de uma questão que tem um texto de apoio de outro idioma, a escolha é limpa e um
aviso diz qual o texto retirado.

### Alterar um texto de apoio
1. Escolha **Editar** na linha, altere o título ou o texto e escolha **Guardar**.
2. Cada questão **publicada** que usa o texto ganha uma nova versão (veja **Versões** em
   [Questões](questions.md)): os estudantes e as tentativas de prova guardam o que viram, e as novas veem a
   redação nova. Os rascunhos acompanham o texto e não registam nada; as questões anuladas não mudam.
3. Alterar só o título não regista nada.
4. O idioma não pode mudar enquanto uma questão usa o texto.

### Ver que questões usam um texto
Abra o texto. O cartão **Questões que usam este texto** lista-as, da alterada há menos tempo para a mais
antiga, com enunciado, origem e estado. Escolha **Editar** numa linha para abrir a questão.

### Eliminar um texto de apoio
1. Só se elimina um texto que nenhuma questão usa. Enquanto alguma usa, **Eliminar** fica desativado e diz
   quantas questões usam o texto.
2. Abra cada questão, retire a ligação e guarde. Depois escolha **Eliminar** na linha (ou no cartão
   **Eliminar este texto de apoio** do texto) e confirme.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Título | O nome pelo qual encontra o texto | Obrigatório, até 200 caracteres |
| Idioma do conteúdo | O idioma em que o texto está escrito | Obrigatório, um dos idiomas da app. Bloqueado enquanto uma questão usa o texto |
| Texto | O excerto, a tabela ou o caso | Obrigatório, até 20 000 caracteres |

Só a formatação simples é mantida: scripts, estilos e qualquer ligação que não seja `http` ou `https` são
removidos quando o texto é guardado, e a página mostra o que foi mantido.

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Escreva o título. | O título está vazio. | Escreva um título. |
| O título excede 200 caracteres. | O título passou do limite. | Encurte-o. |
| Escreva o texto. | O texto está vazio. | Escreva o excerto. |
| O texto excede 20 000 caracteres. | O texto passou do limite. | Encurte-o. Nada é cortado por si. |
| Escolha um dos idiomas da lista. | O idioma não está na lista. | Escolha um idioma da lista. |
| Ainda há questões a usar este texto de apoio. Retire as ligações delas antes de o eliminar. | Uma ou mais questões ainda ligam o texto. | Retire primeiro as ligações. |
| Há questões a usar este texto de apoio, por isso o idioma dele não pode mudar. | Tentou mudar o idioma de um texto em uso. | Retire as ligações ou crie outro texto. |
| Este texto de apoio já não existe. | Foi eliminado enquanto o seu ecrã estava aberto. | Volte à lista. |
| Este texto de apoio já não existe. Escolha outro ou deixe o campo vazio. | A questão liga um texto que entretanto foi eliminado. | Escolha outro texto ou deixe o campo vazio. |
| O texto de apoio e a questão têm de estar no mesmo idioma. | O texto escolhido está noutro idioma. | Escolha um texto do idioma da questão. |
| Página não encontrada | A sua conta não gere o banco de questões. | Fale com um administrador se achar que devia. |

## Páginas relacionadas
- [Questões](questions.md): escrever, publicar e encontrar questões, e ver as suas versões
- [Como navegar](getting-around.md): menu, modo claro e escuro, idioma e teclado
- [Simulab](index.md)
