---
page: account-events
locale: pt-BR
features: [F-21]
updated: 2026-09-23
---
# Eventos de conta

A página Eventos de conta mostra o que aconteceu com as contas: quem entrou e de onde, quem errou a senha, quais contas foram bloqueadas e quem trocou a senha ou desativou a verificação em duas etapas. Ela responde perguntas como "alguém entrou nesta conta?".

## Quem pode usar
Somente uma conta com a permissão "Gerenciar papéis e papéis de usuários" — o papel Admin tem essa permissão.

## O que é registrado
- **Sessão iniciada** e **Falha ao entrar**, com a forma usada (senha, Google, código do aplicativo ou código de recuperação) ou o motivo da falha.
- **Conta bloqueada**, quando uma sequência de falhas passa do limite. As tentativas seguintes, enquanto o bloqueio dura, são registradas como falhas.
- **Sessão encerrada**, **Senha alterada**, **Redefinição de senha solicitada**, **Senha redefinida**.
- **Verificação em duas etapas ativada**, **desativada** e **Códigos de recuperação gerados de novo**.
- **Conta excluída**.

Uma tentativa de entrar com um e-mail que não pertence a nenhuma conta é registrada como "Conta desconhecida": o e-mail digitado nunca é guardado. A renovação silenciosa da sua sessão não é uma entrada e não é registrada. Os registros são mantidos e nunca alterados.

## Como fazer
### Ver os eventos
1. Escolha **Eventos de conta**, na seção **Administração** do menu lateral.
2. Os eventos mais recentes vêm primeiro. Escolha o cabeçalho da coluna **Quando** para ver os mais antigos primeiro.

### Ver os eventos de uma conta
1. Na página [Usuários](users.md), escolha **Eventos de segurança** na linha da conta.
2. A página abre já filtrada, e uma etiqueta "Conta: …" mostra o filtro; escolha o × dela para ver tudo de novo.

### Ver tudo que veio de um endereço
Escolha o endereço na coluna **De**. Aparece uma etiqueta "De: …" e a lista passa a mostrar todos os eventos daquele endereço, de qualquer conta. Escolha o × dela para remover.

### Filtrar a lista
- **Evento**: apenas aquele tipo de evento.
- **Período**: os últimos 7, 30 ou 90 dias, ou todo o período.
- O campo de busca encontra a conta por e-mail ou nome.

Os filtros ficam no endereço da página, então recarregar ou compartilhar o link mantém o que você escolheu.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Quando | Data e hora do evento | Mostrada no seu fuso horário |
| Conta | A conta a que o evento se refere | "Conta excluída" se ela foi excluída depois; "Conta desconhecida" se a tentativa não achou nenhuma |
| Evento | O que aconteceu | |
| Detalhes | Como a entrada foi feita, ou por que falhou | Vazio quando nenhum dos dois se aplica |
| De | O endereço de onde veio a solicitação | Escolha para ver todos os eventos dele; vazio quando não é conhecido, ou quando a conta foi excluída |

## O que não é guardado
O evento não guarda e-mail, nome, nem informação sobre o navegador ou o aparelho. O e-mail mostrado é lido da conta no momento em que você abre a página, por isso uma conta excluída depois aparece como "Conta excluída". Quando uma conta é excluída, os endereços de todos os eventos dela são apagados; os eventos continuam lá, para que a exclusão possa ser rastreada.

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Nenhum evento de conta registrado ainda. | Nada aconteceu desde que esta página existe. | Nada; os eventos aparecem aqui conforme acontecem. |
| Nenhum evento corresponde a esses filtros. | Os filtros ou a busca não deixam nada. | Limpe um filtro ou a busca. |
| Conta excluída | A conta foi excluída depois do evento; o e-mail dela não é mais guardado. | Nada; o registro permanece. |
| Conta desconhecida | Alguém tentou entrar com um e-mail que não pertence a nenhuma conta. | Fique atento a muitas ocorrências vindas do mesmo endereço. |
| Página não encontrada | Sua conta não gerencia papéis. | Fale com um Admin se acha que deveria. |

## Páginas relacionadas
- [Usuários](users.md): encontrar uma conta e mudar os papéis dela
- [Histórico de papéis](role-history.md): quem mudou qual papel ou os papéis de quem, e quando
- [Entrar e sair](sign-in-and-sign-out.md): entrar, sair, bloqueio
- [Entrada em duas etapas](two-factor.md): um código do celular depois da senha, e os códigos de recuperação
- [Simulab](index.md)
