---
page: account-events
locale: pt-PT
features: [F-21]
updated: 2026-09-23
---
# Eventos de conta

A página Eventos de conta mostra o que aconteceu às contas: quem iniciou sessão e a partir de onde, quem falhou ao iniciar sessão, que contas foram bloqueadas e quem alterou a palavra-passe ou desativou a verificação em duas etapas. Responde a perguntas como "alguém entrou nesta conta?".

## Quem pode utilizar
Só uma conta com a permissão "Gerir perfis e atribuições" — o perfil Administrador tem essa permissão.

## O que fica registado
- **Sessão iniciada** e **Falha ao iniciar sessão**, com a forma utilizada (palavra-passe, Google, código da aplicação ou código de recuperação) ou o motivo da falha.
- **Conta bloqueada**, quando uma sequência de falhas passa o limite. As tentativas seguintes, enquanto o bloqueio dura, ficam registadas como falhas.
- **Sessão terminada**, **Palavra-passe alterada**, **Reposição de palavra-passe pedida**, **Palavra-passe reposta**.
- **Verificação em duas etapas ativada**, **desativada** e **Códigos de recuperação gerados de novo**.
- **Conta eliminada**.

Uma tentativa de iniciar sessão com um e-mail que não pertence a nenhuma conta fica registada como "Conta desconhecida": o e-mail escrito nunca é guardado. A renovação silenciosa da sua sessão não é um início de sessão e não fica registada. Os registos são guardados e nunca alterados.

## Como fazer
### Ver os eventos
1. Escolha **Eventos de conta**, na secção **Administração** do menu lateral.
2. Os eventos mais recentes vêm primeiro. Escolha o cabeçalho da coluna **Quando** para ver os mais antigos primeiro.

### Ver os eventos de uma conta
1. Na página [Utilizadores](users.md), escolha **Eventos de segurança** na linha da conta.
2. A página abre já filtrada e uma etiqueta "Conta: …" mostra o filtro; escolha o × dela para ver tudo de novo.

### Ver tudo o que veio de um endereço
Escolha o endereço na coluna **De**. Aparece uma etiqueta "De: …" e a lista passa a mostrar todos os eventos desse endereço, de qualquer conta. Escolha o × dela para a retirar.

### Filtrar a lista
- **Evento**: apenas esse tipo de evento.
- **Período**: os últimos 7, 30 ou 90 dias, ou todo o período.
- A caixa de pesquisa encontra a conta por e-mail ou nome.

Os filtros ficam no endereço da página, por isso recarregar ou partilhar a ligação mantém o que escolheu.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Quando | Data e hora do evento | Mostrada no seu fuso horário |
| Conta | A conta a que o evento diz respeito | "Conta eliminada" se foi eliminada depois; "Conta desconhecida" se a tentativa não encontrou nenhuma |
| Evento | O que aconteceu | |
| Detalhes | Como a sessão foi iniciada, ou porque falhou | Vazio quando nenhum dos dois se aplica |
| De | O endereço de onde veio o pedido | Escolha-o para ver todos os eventos dele; vazio quando não é conhecido, ou quando a conta foi eliminada |

## O que não é guardado
O evento não guarda e-mail, nome, nem informação sobre o navegador ou o aparelho. O e-mail mostrado é lido da conta no momento em que abre a página, por isso uma conta eliminada depois aparece como "Conta eliminada". Quando uma conta é eliminada, os endereços de todos os eventos dela são apagados; os eventos ficam, para que a eliminação continue a poder ser seguida.

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Ainda não foi registado nenhum evento de conta. | Nada aconteceu desde que esta página existe. | Nada; os eventos aparecem aqui à medida que acontecem. |
| Nenhum evento corresponde a estes filtros. | Os filtros ou a pesquisa não deixam nada. | Limpe um filtro ou a pesquisa. |
| Conta eliminada | A conta foi eliminada depois do evento; o e-mail dela já não é guardado. | Nada; o registo fica. |
| Conta desconhecida | Alguém tentou iniciar sessão com um e-mail que não pertence a nenhuma conta. | Esteja atento a muitas ocorrências do mesmo endereço. |
| Página não encontrada | A sua conta não gere perfis. | Fale com um Administrador se acha que devia. |

## Páginas relacionadas
- [Utilizadores](users.md): encontrar uma conta e mudar os seus perfis
- [Histórico de perfis](role-history.md): quem alterou que perfil ou os perfis de quem, e quando
- [Iniciar e terminar sessão](sign-in-and-sign-out.md): iniciar sessão, terminar sessão, bloqueio
- [Início de sessão em duas etapas](two-factor.md): um código do telemóvel depois da palavra-passe, e os códigos de recuperação
- [Simulab](index.md)
