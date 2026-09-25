---
page: issuing-authorities
locale: pt-PT
features: [F-34]
updated: 2026-09-25
---
# Entidades contratantes

Uma entidade contratante é quem publica o aviso e define os lugares, o conteúdo e as regras de um exame: uma
câmara municipal, um governo, um ministério, uma universidade, uma empresa. Todos os exames do catálogo
pertencem a uma delas.

Não confunda com a [entidade organizadora](organizers.md): o júri é contratado para elaborar, aplicar e corrigir
a prova, e pode mudar de uma edição para a outra.

## Quem pode utilizar
Só uma conta com a permissão "Gerir o catálogo" — o perfil Administrador tem essa permissão — consegue abrir o
ecrã Entidades contratantes. Para as restantes, a página não é encontrada.

## Como fazer
### Ver as entidades contratantes
1. Escolha **Entidades contratantes**, na secção **Conteúdo** do menu lateral.
2. A lista mostra o nome e a sigla, por ordem de nome. Clique no título de uma coluna para ordenar por ela.
3. Escreva na caixa de pesquisa para filtrar pelo nome ou pela sigla. Acentos e maiúsculas não contam.

### Registar uma entidade contratante
1. Escolha **Adicionar**.
2. Escreva o **nome**, tal como a entidade assina os seus avisos, e a **sigla**. A sigla é guardada em
   maiúsculas.
3. O **sítio oficial** e a **descrição** são opcionais. O sítio é o endereço completo, a começar por `https://`.
4. Escolha **Guardar**.

### Alterar uma entidade contratante
1. Escolha **Editar** na linha dela.
2. Altere o que precisar e escolha **Guardar**. Os exames que já lhe pertencem continuam a pertencer-lhe.

### Eliminar uma entidade contratante
1. Escolha **Eliminar** na linha dela e confirme.
2. Se ainda tiver exames no catálogo, a eliminação é recusada e aparece um aviso no topo da lista. Elimine antes
   esses exames, na página [Exames](exams.md) — filtre a lista por esta entidade para os encontrar.
3. Sem exames, a entidade sai do catálogo. O nome e a sigla continuam ocupados, para ninguém registar uma
   segunda com o mesmo nome por engano.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Nome | Como a entidade aparece em todo o lado | Obrigatório, entre 2 e 150 caracteres, não pode repetir o nome de outra entidade, incluindo uma eliminada. Maiúsculas e acentos não fazem um nome diferente |
| Sigla | O nome curto (CML, DGES, UC) | Obrigatória, entre 2 e 20 caracteres, guardada em maiúsculas, única da mesma forma que o nome |
| Sítio oficial | Onde ler sobre a entidade | Opcional, endereço completo a começar por `http://` ou `https://` |
| Descrição | Texto livre sobre a entidade | Opcional, no máximo 500 caracteres |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Outra entidade contratante já tem este nome. | O nome está ocupado, talvez por uma entidade eliminada. | Escolha outro nome. |
| Outra entidade contratante já tem esta sigla. | A sigla está ocupada, em qualquer combinação de maiúsculas. | Escolha outra sigla. |
| Indique um nome com pelo menos 2 caracteres. | O nome está vazio ou demasiado curto. | Escreva o nome completo. |
| O nome é demasiado longo: no máximo 150 caracteres. | O nome passou do limite. | Encurte o nome. |
| Indique uma sigla com pelo menos 2 caracteres. | A sigla está vazia ou demasiado curta. | Escreva a sigla. |
| A sigla é demasiado longa: no máximo 20 caracteres. | A sigla passou do limite. | Encurte a sigla. |
| A descrição é demasiado longa: no máximo 500 caracteres. | A descrição passou do limite. | Encurte a descrição. |
| Indique um endereço completo, a começar por http:// ou https:// | O sítio não é um endereço completo. | Acrescente `https://` à frente ou deixe o campo vazio. |
| Esta entidade contratante tem exames no catálogo. Elimine esses exames primeiro, na página Exames. | A entidade ainda é dona de pelo menos um exame. | Vá a Exames, filtre por esta entidade, elimine os exames e volte. |
| Esta entidade contratante já não existe. Atualize a lista. | Alguém eliminou a entidade enquanto o seu ecrã estava aberto. | Recarregue a página. |
| Página não encontrada | A sua conta não gere o catálogo. | Fale com um administrador se acha que devia gerir. |

## Páginas relacionadas
- [Exames](exams.md): os exames de cada entidade contratante (Administradores)
- [Entidades organizadoras](organizers.md): quem elabora e aplica as provas (Administradores)
- [Como navegar](getting-around.md): menu, modo claro e escuro, idioma e teclado
- [Perfis e permissões](roles.md): quem pode gerir o catálogo
- [Simulab](index.md)
