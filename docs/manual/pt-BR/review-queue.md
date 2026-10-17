---
page: review-queue
locale: pt-BR
features: [F-105]
updated: 2026-10-17
---
# Fila de revisão

A fila de revisão reúne as questões que um curador enviou para revisão. Quem revisa lê a questão como o aluno a
verá, com o gabarito e a explicação, e a **aprova** (ela é publicada na hora) ou a **rejeita** com um motivo
(ela volta a rascunho). Quem escreve a questão a envia pela página dela; veja [Questões](questions.md).

## Quem pode usar
Uma conta com a permissão "Revisar questões". Curadores e Administradores a têm. Todas as outras veem Página
não encontrada, e o menu não tem o item Fila de revisão. A permissão é independente de "Gerenciar o banco de
questões": uma conta só com "Revisar questões" vê a fila e a tela de revisão, mas não edita nem envia questões.
Quem enviou a questão pode aprová-la, se tiver as duas permissões.

## Como fazer
### Ver a fila
1. Escolha **Fila de revisão**, na seção **Conteúdo** do menu lateral.
2. A lista mostra só as questões **Em revisão**, da que espera há mais tempo para a mais nova (conta o último
   envio): o início do enunciado, o tópico com a disciplina, o tipo, o idioma e a data do envio.
3. Sem nada esperando, a página diz "Nada aguardando revisão."
4. Escolha o enunciado, ou **Revisar** no menu da linha, para abrir a tela de revisão.

### Revisar uma questão
1. Na tela de revisão você vê o texto-base, o enunciado e as alternativas, e sempre o gabarito, a explicação e a
   classificação da questão (disciplina, tópico, idioma, dificuldade, fonte e a data do envio). Nada aqui se
   edita.
2. Escolha **Aprovar** para publicar. A janela pede uma **observação**, opcional, de até 500 caracteres, que
   fica no histórico de revisão da questão. Confirme com **Aprovar**.
3. Ou escolha **Rejeitar**. A janela pede o **motivo**, obrigatório, de 1 a 500 caracteres. O curador o vê na
   página da questão. Confirme com **Rejeitar**.
4. Os campos de texto das janelas mostram quantos caracteres você digitou, como "12/500". O contador aparece
   também ao lado de uma mensagem de erro.
5. Depois de decidir, você volta à fila e vê a confirmação "Questão aprovada e publicada." ou "Questão
   rejeitada. Voltou a ser rascunho."

Se outra pessoa decidiu antes de você, ou o curador retirou a questão da revisão, a tela mostra "Esta questão
não está mais em revisão: outro revisor decidiu, ou ela foi retirada." e o link **Voltar para a fila**. Se a
questão foi retirada e enviada de novo enquanto você a lia, a decisão é recusada para que você a releia.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Observação (aprovar) | Um recado do revisor, guardado no histórico de revisão | Opcional, texto simples, até 500 caracteres |
| Motivo (rejeitar) | Por que a questão foi rejeitada | Obrigatório, texto simples, de 1 a 500 caracteres |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Escreva o motivo da rejeição. | O motivo está vazio. | Digite o motivo, até 500 caracteres. |
| O motivo ou a observação passa de 500 caracteres. | O texto passou do limite. | Encurte-o. |
| Esta questão não está em revisão. | Outra pessoa decidiu antes de você, ou a questão foi retirada. | Volte à fila. |
| Esta questão não existe mais. | Alguém a excluiu com a tela aberta. | Volte à fila. |
| Outra gravação alterou esta questão antes. | Duas decisões foram enviadas juntas, ou a questão foi retirada e enviada de novo. | Reabra a questão e decida de novo. |
| Página não encontrada | A sua conta não tem a permissão "Revisar questões". | Fale com um administrador se achar que deveria. |

## Páginas relacionadas
- [Questões](questions.md): escrever uma questão e enviá-la para revisão
- [Papéis e permissões](roles.md): quem pode revisar questões
- [Como navegar](getting-around.md): menu, modo claro e escuro, idioma e teclado
- [Simulab](index.md)
