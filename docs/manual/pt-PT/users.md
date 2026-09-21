---
page: users
locale: pt-PT
features: [F-9, F-14]
updated: 2026-09-21
---
# Utilizadores

A página Utilizadores lista as contas do Simulab e é onde atribui ou retira um perfil.

## Quem pode utilizar
Só uma conta com a permissão "Gerir perfis e atribuições" — o perfil Administrador tem essa permissão.

## Como fazer
### Encontrar uma conta
1. Escolha **Utilizadores**, na secção **Administração** do menu lateral.
2. Escreva parte do e-mail ou do nome no campo de pesquisa.
3. Ou escolha um perfil em **Perfil** para ver só as contas que o têm. Clicar no número de contas na página [Perfis](roles.md) abre esta página já filtrada.

### Mudar os perfis de alguém
1. Escolha **Editar perfis** na linha da conta.
2. Assinale todos os perfis que a conta deve ter e desmarque os restantes. Uma conta pode ter vários perfis, e as permissões somam-se.
3. Escolha **Guardar**. A conta recebe as novas permissões em segundos; o menu dela muda no carregamento de página seguinte.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| E-mail | O endereço com que a conta inicia sessão | Apresentado como foi registado |
| Nome | O nome de apresentação escolhido pela pessoa | Pode estar vazio |
| Estado | **Pendente** enquanto o e-mail não está confirmado, **Ativa** depois disso | Uma conta em qualquer estado pode ter perfis; a pendente ainda não consegue iniciar sessão |
| Perfis | Os perfis que a conta tem | Nenhum, um ou vários |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Nenhum utilizador corresponde a "…". | Nenhuma conta tem esse texto no e-mail ou no nome. | Verifique a escrita ou limpe a pesquisa. |
| Assim ninguém mais poderia gerir perfis. | A alteração retiraria a última conta que gere perfis. | Dê essa permissão a outra conta ativa primeiro. |
| Este utilizador já não existe. | A conta foi removida enquanto a página estava aberta. | Recarregue a página. |
| Um dos perfis já não existe. Recarregue a página e tente novamente. | Um perfil foi eliminado enquanto a caixa estava aberta. | Recarregue a página. |
| Página não encontrada | A sua conta não gere perfis. | Fale com um Administrador se achar que deveria. |

## Páginas relacionadas
- [Perfis e permissões](roles.md): o que cada perfil permite
- [Histórico de perfis](role-history.md): cada alteração aos perfis de uma conta — escolha **Histórico** na linha dela
- [Como navegar](getting-around.md): menu, modo claro e escuro, idioma e teclado
- [Simulab](index.md)
