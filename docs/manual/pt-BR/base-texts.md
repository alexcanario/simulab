---
page: base-texts
locale: pt-BR
features: [F-102]
updated: 2026-10-15
---
# Textos-base

Um texto-base é um trecho, uma tabela ou um caso a que várias questões se referem, como num bloco de
interpretação de texto. Você o escreve uma vez, dá um título e liga cada questão a ele; toda questão ligada
mostra o texto antes do enunciado. Corrigir uma palavra no texto-base corrige para todas elas.

## Quem pode usar
Uma conta com a permissão "Gerenciar o banco de questões". Curadores e Administradores a têm. As outras veem
Página não encontrada, e o menu não tem o item Textos-base.

## Como fazer
### Ver os textos-base
1. Escolha **Textos-base**, na seção **Conteúdo** do menu lateral, logo depois de **Questões**.
2. A lista mostra o título, o idioma e quantas questões usam cada texto, em ordem de título.
3. Digite na caixa de pesquisa para achar uma palavra do título (maiúsculas e acentos não importam), ou use
   **Idioma** para ver os textos de um idioma.

### Escrever um texto-base
1. Escolha **Adicionar**.
2. Digite o **título**, até 200 caracteres. Ele serve só para você achar o texto; o aluno não o vê.
3. Escolha o **idioma do conteúdo**: o idioma em que o texto está escrito. Só questões do mesmo idioma podem
   usá-lo.
4. Escreva o **texto** no editor. A barra oferece negrito, itálico, sublinhado, tachado, sobrescrito,
   subscrito, listas, citação, link e limpar formatação. Imagens ainda não são aceitas.
5. Escolha **Salvar**. A página vira a página de edição do texto.

### Ligar uma questão a um texto-base
1. Abra a questão (**Questões**, depois **Adicionar** ou **Editar** numa linha).
2. No cartão **Texto-base**, depois do enunciado, clique no campo e digite parte do título. Só textos do idioma
   da questão são oferecidos, cada um com o número de questões que já o usam.
3. Escolha um e aperte **Salvar**. **Abrir o texto-base** o mostra em outra aba e **Mostrar o texto** o lê sem
   sair da questão.
4. Para tirar o vínculo, aperte o **x** do campo e salve. O texto continua existindo.
5. O texto que você quer não está lá? Escolha **Criar um texto-base**. Ele abre em outra aba, então a questão
   que você escreve fica como está: salve o texto lá, volte e escolha-o.

Se você mudar o idioma do conteúdo de uma questão que tem um texto-base de outro idioma, a escolha é limpa e
um aviso diz qual texto foi retirado.

### Mudar um texto-base
1. Escolha **Editar** na linha, mude o título ou o texto e escolha **Salvar**.
2. Cada questão **publicada** que usa o texto ganha uma nova versão (veja **Versões** em
   [Questões](questions.md)): alunos e tentativas de prova guardam o que viram, e as novas veem a redação
   nova. Rascunhos acompanham o texto e não registram nada; questões anuladas não mudam.
3. Mudar só o título não registra nada.
4. O idioma não pode mudar enquanto uma questão usa o texto.

### Ver quais questões usam um texto
Abra o texto. O cartão **Questões que usam este texto** as lista, da mudada mais recentemente para a mais
antiga, com enunciado, origem e situação. Escolha **Editar** numa linha para abrir a questão.

### Excluir um texto-base
1. Só um texto que nenhuma questão usa pode ser excluído. Enquanto alguma usa, **Excluir** fica desabilitado e
   diz quantas questões usam o texto.
2. Abra cada questão, retire o vínculo e salve. Depois escolha **Excluir** na linha (ou no cartão **Excluir
   este texto-base** do texto) e confirme.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Título | O nome pelo qual você acha o texto | Obrigatório, até 200 caracteres |
| Idioma do conteúdo | O idioma em que o texto está escrito | Obrigatório, um dos idiomas do app. Travado enquanto uma questão usa o texto |
| Texto | O trecho, a tabela ou o caso | Obrigatório, até 20.000 caracteres |

Só a formatação simples é mantida: scripts, estilos e qualquer link que não seja `http` ou `https` são
removidos quando o texto é salvo, e a página mostra o que foi mantido.

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Escreva o título. | O título está vazio. | Digite um título. |
| O título passa de 200 caracteres. | O título passou do limite. | Encurte-o. |
| Escreva o texto. | O texto está vazio. | Escreva o trecho. |
| O texto passa de 20.000 caracteres. | O texto passou do limite. | Encurte-o. Nada é cortado por você. |
| Escolha um dos idiomas da lista. | O idioma não está na lista. | Escolha um idioma da lista. |
| Ainda há questões usando este texto-base. Remova os vínculos delas antes de excluir. | Uma ou mais questões ainda ligam o texto. | Retire os vínculos primeiro. |
| Há questões usando este texto-base, então o idioma dele não pode mudar. | Você tentou mudar o idioma de um texto em uso. | Retire os vínculos ou crie outro texto. |
| Este texto-base não existe mais. | Ele foi excluído enquanto sua tela estava aberta. | Volte à lista. |
| Este texto-base não existe mais. Escolha outro ou deixe o campo vazio. | A questão liga um texto que foi excluído nesse meio-tempo. | Escolha outro texto ou deixe o campo vazio. |
| O texto-base e a questão precisam estar no mesmo idioma. | O texto escolhido está em outro idioma. | Escolha um texto do idioma da questão. |
| Página não encontrada | Sua conta não gerencia o banco de questões. | Fale com um administrador se achar que deveria. |

## Páginas relacionadas
- [Questões](questions.md): escrever, publicar e achar questões, e ver suas versões
- [Como navegar](getting-around.md): menu, modo claro e escuro, idioma e teclado
- [Simulab](index.md)
