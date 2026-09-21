---
page: role-history
locale: pt-PT
features: [F-14]
updated: 2026-09-21
---
# Histórico de perfis

A página Histórico de perfis mostra cada alteração feita aos perfis e aos perfis de uma conta: quem a fez, quando e o que mudou. Responde a perguntas como "quem atribuiu o perfil Administrador a esta pessoa, e quando?".

## Quem pode utilizar
Só uma conta com a permissão "Gerir perfis e atribuições" — o perfil Administrador tem essa permissão.

## O que fica registado
- Um perfil criado, alterado (com outro nome ou outras permissões) ou eliminado na página [Perfis](roles.md).
- Os perfis de uma conta alterados na página [Utilizadores](users.md).

As alterações que o próprio Simulab faz não ficam registadas: o perfil Estudante atribuído no registo e os perfis retirados quando alguém elimina a própria conta. Uma alteração recusada (por exemplo, uma que deixaria ninguém capaz de gerir perfis) ou uma gravação que não mudou nada não deixa registo. Os registos são guardados e nunca alterados.

## Como fazer
### Ver o histórico
1. Escolha **Histórico de perfis**, na secção **Administração** do menu lateral.
2. As alterações mais recentes aparecem primeiro. Escolha o título da coluna **Quando** para ver as mais antigas primeiro.

### Ver o histórico de um perfil ou de uma conta
1. Na página [Perfis](roles.md) ou [Utilizadores](users.md), escolha **Histórico** na linha do perfil ou da conta.
2. A página Histórico de perfis abre já filtrada. Para uma conta, uma etiqueta "Utilizador: …" mostra o filtro; escolha o × para voltar a ver tudo.

### Filtrar a lista
- **Perfil**: alterações a esse perfil e alterações que o atribuíram a uma conta ou lho retiraram. Os perfis eliminados aparecem como "(eliminado)".
- **Alterado por**: alterações feitas por essa conta.
- **Período**: últimos 7, 30 ou 90 dias, ou todo o período.
- O campo de pesquisa encontra a conta cujos perfis mudaram, pelo e-mail ou pelo nome.

Os filtros ficam no endereço da página, por isso recarregar ou partilhar a ligação mantém-nos.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Quando | Data e hora da alteração | Apresentada no seu fuso horário |
| Alterado por | A conta que fez a alteração | "Conta eliminada" se essa conta foi eliminada depois |
| Ação | Perfil criado, Perfil alterado, Perfil eliminado ou Perfis do utilizador alterados | |
| Perfil ou utilizador | O perfil (com o nome que tinha na altura) ou a conta cujos perfis mudaram | |
| Alterações | O nome antigo e o novo, o que foi adicionado e o que foi retirado | Listas longas são cortadas; aponte para ler tudo |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Ainda não foi registada nenhuma alteração de perfil. | Ninguém alterou um perfil desde que esta página existe. | Nada; as alterações aparecem aqui à medida que são feitas. |
| Nenhuma alteração corresponde a estes filtros. | Os filtros ou a pesquisa não deixam nada. | Limpe um filtro ou a pesquisa. |
| Conta eliminada | A conta foi eliminada depois da alteração; o seu e-mail já não é guardado. | Nada; o registo mantém-se. |
| Página não encontrada | A sua conta não gere perfis. | Fale com um Administrador se achar que deveria. |

## Páginas relacionadas
- [Perfis e permissões](roles.md): o que cada perfil permite e como alterar um perfil
- [Utilizadores](users.md): encontrar uma conta e mudar os seus perfis
- [Simulab](index.md)
