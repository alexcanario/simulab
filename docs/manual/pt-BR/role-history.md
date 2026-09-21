---
page: role-history
locale: pt-BR
features: [F-14]
updated: 2026-09-21
---
# Histórico de papéis

A página Histórico de papéis mostra cada mudança feita nos papéis e nos papéis de uma conta: quem fez, quando e o que mudou. Ela responde perguntas como "quem deu Administrador a esta pessoa, e quando?".

## Quem pode usar
Só uma conta com a permissão "Gerenciar papéis e atribuições" — o papel Administrador tem essa permissão.

## O que é registrado
- Um papel criado, alterado (renomeado ou com as permissões trocadas) ou excluído na página [Papéis](roles.md).
- Os papéis de uma conta alterados na página [Usuários](users.md).

As mudanças que o próprio Simulab faz não são registradas: o papel Estudante dado no cadastro e os papéis retirados quando alguém exclui a própria conta. Uma mudança recusada (por exemplo, uma que deixaria ninguém capaz de gerenciar papéis) ou um salvamento que não mudou nada não deixa registro. Os registros são guardados e nunca alterados.

## Como fazer
### Ver o histórico
1. Escolha **Histórico de papéis**, na seção **Administração** do menu lateral.
2. As mudanças mais recentes vêm primeiro. Escolha o título da coluna **Quando** para ver as mais antigas primeiro.

### Ver o histórico de um papel ou de uma conta
1. Na página [Papéis](roles.md) ou [Usuários](users.md), escolha **Histórico** na linha do papel ou da conta.
2. A página Histórico de papéis abre já filtrada. Para uma conta, uma etiqueta "Usuário: …" mostra o filtro; escolha o × dela para ver tudo de novo.

### Filtrar a lista
- **Papel**: mudanças nesse papel e mudanças que o deram a uma conta ou o tiraram dela. Papéis excluídos aparecem como "(excluído)".
- **Alterado por**: mudanças feitas por essa conta.
- **Período**: últimos 7, 30 ou 90 dias, ou todo o período.
- O campo de busca acha a conta cujos papéis mudaram, pelo e-mail ou pelo nome.

Os filtros ficam no endereço da página, então recarregar ou compartilhar o link os mantém.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Quando | Data e hora da mudança | Mostrada no seu fuso horário |
| Alterado por | A conta que fez a mudança | "Conta excluída" se essa conta foi excluída depois |
| Ação | Papel criado, Papel alterado, Papel excluído ou Papéis do usuário alterados | |
| Papel ou usuário | O papel (com o nome que tinha na época) ou a conta cujos papéis mudaram | |
| Alterações | O nome antigo e o novo, o que foi adicionado e o que foi removido | Listas longas são cortadas; aponte para ler tudo |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Nenhuma mudança de papel registrada ainda. | Ninguém mudou um papel desde que esta página existe. | Nada; as mudanças aparecem aqui conforme são feitas. |
| Nenhuma mudança corresponde a esses filtros. | Os filtros ou a busca não deixam nada. | Limpe um filtro ou a busca. |
| Conta excluída | A conta foi excluída depois da mudança; o e-mail dela não é mais guardado. | Nada; o registro continua. |
| Página não encontrada | Sua conta não gerencia papéis. | Fale com um Administrador se achar que deveria. |

## Páginas relacionadas
- [Papéis e permissões](roles.md): o que cada papel libera e como mudar um papel
- [Usuários](users.md): achar uma conta e mudar os papéis dela
- [Simulab](index.md)
