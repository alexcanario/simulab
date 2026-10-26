---
page: users
locale: pt-BR
features: [F-9, F-14, F-21, F-150]
updated: 2026-10-26
---
# Usuários

A página Usuários lista as contas do Simulab e é onde você dá ou tira um papel e liga uma conta a um tenant (a organização parceira a que ela pertence).

## Quem pode usar
Só uma conta com a permissão "Gerenciar papéis e atribuições" — o papel Administrador tem essa permissão.

## Como fazer
### Encontrar uma conta
1. Escolha **Usuários**, na seção **Administração** do menu lateral.
2. Digite parte do e-mail ou do nome no campo de busca.
3. Ou escolha um papel em **Papel** para ver só as contas que o possuem. Clicar no número de contas na página [Papéis](roles.md) abre esta página já filtrada.

### Mudar os papéis ou o tenant de alguém
1. Escolha **Editar papéis** na linha da conta. Abre o diálogo **Acesso de …**.
2. Marque todos os papéis que a conta deve ter e desmarque os outros. Uma conta pode ter vários papéis, e as permissões se somam. O papel **Corretor** é para quem corrige redações e respostas abertas.
3. Em **Tenant**, cole o id do tenant da conta, ou deixe o campo vazio para nenhum.
4. Escolha **Salvar**. Os papéis são salvos primeiro e o tenant depois, cada um só se mudou. A conta recebe as novas permissões em segundos e o menu dela muda no próximo carregamento de página; o novo tenant chega a ela em até 15 minutos.
5. Se os papéis forem salvos e o tenant não, o diálogo continua aberto com o erro e o aviso "Papéis de … salvos."; corrija e salve de novo — só o tenant é enviado.

Toda troca de tenant aparece no [Histórico de papéis](role-history.md) como **Tenant do usuário alterado**, com o id antigo e o novo.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| E-mail | O endereço com que a conta entra | Mostrado como foi cadastrado |
| Nome | O nome de exibição escolhido pela pessoa | Pode estar vazio |
| Situação | **Pendente** enquanto o e-mail não foi confirmado, **Ativa** depois disso | Conta em qualquer situação pode ter papéis; a pendente ainda não consegue entrar |
| Papéis | Os papéis que a conta tem | Nenhum, um ou vários |
| Tenant | A organização parceira da conta; a lista mostra os 8 primeiros caracteres do id, ou "—" quando não há | Um id no formato 3f2a9c10-7b1e-4c55-9d2e-0a1b2c3d4e5f, ou vazio; qualquer conta pode ter um |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Nenhum usuário corresponde a "…". | Nenhuma conta tem esse texto no e-mail ou no nome. | Confira a escrita ou limpe a busca. |
| Assim ninguém mais poderia gerenciar papéis. | A mudança tiraria a última conta que gerencia papéis. | Dê essa permissão a outra conta ativa antes. |
| Este usuário não existe mais. | A conta foi removida enquanto a página estava aberta. | Recarregue a página. |
| Cole um id de tenant como 3f2a9c10-7b1e-4c55-9d2e-0a1b2c3d4e5f. | O texto do campo Tenant não é um id. | Cole o id inteiro, ou deixe o campo vazio. |
| O id do tenant não é válido. Cole o id de um tenant ou deixe o campo vazio. | O id enviado foi recusado. | Confira o id e salve de novo. |
| Um dos papéis não existe mais. Recarregue a página e tente de novo. | Um papel foi excluído enquanto o diálogo estava aberto. | Recarregue a página. |
| Página não encontrada | Sua conta não gerencia papéis. | Fale com um Administrador se achar que deveria. |

## Páginas relacionadas
- [Papéis e permissões](roles.md): o que cada papel libera
- [Histórico de papéis](role-history.md): cada mudança nos papéis de uma conta — escolha **Histórico** na linha dela
- [Eventos de conta](account-events.md): entradas e mudanças de uma conta — escolha **Eventos de segurança** na linha dela
- [Como navegar](getting-around.md): menu, modo claro e escuro, idioma e teclado
- [Simulab](index.md)
