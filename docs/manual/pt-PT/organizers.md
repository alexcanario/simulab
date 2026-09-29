---
page: organizers
locale: pt-PT
features: [F-33, F-34, F-35]
updated: 2026-09-29
---
# Entidades organizadoras

Uma entidade organizadora é quem elabora, aplica e corrige a prova: o júri contratado para um concurso público, a entidade que emite certificações ou a universidade com exame de acesso próprio. É escolhida em cada edição de um exame, e pode mudar de uma edição para a outra.

Quem publica o aviso e define as regras do exame é a [entidade contratante](issuing-authorities.md), não a entidade organizadora.

## Quem pode utilizar
Só uma conta com a permissão "Gerir o catálogo" — o perfil Administrador tem essa permissão — consegue abrir o ecrã Entidades organizadoras. Para as restantes, a página não é encontrada.

## Como fazer
### Ver as entidades organizadoras
1. Escolha **Entidades organizadoras**, na secção **Conteúdo** do menu lateral.
2. A lista mostra o nome, a sigla e o tipo, por ordem de nome. Clique no título de uma coluna para ordenar por ela.
3. Escreva na caixa de procura para filtrar por nome ou sigla. Acentos e maiúsculas não contam: "fundacao" encontra "Fundação".

### Registar uma entidade organizadora
1. Escolha **Adicionar**.
2. Escreva o **nome**, tal como a entidade assina os seus avisos de abertura, e a **sigla**. A sigla é guardada em maiúsculas.
3. Escolha o **tipo**: júri de exame, entidade certificadora ou universidade.
4. O **sítio oficial** e a **descrição** são opcionais. O sítio é o endereço completo, a começar por `https://`.
5. Escolha **Guardar**.

### Alterar uma entidade organizadora
1. Escolha **Editar** na respetiva linha.
2. Altere o que for preciso e escolha **Guardar**. Tudo o que já aponta para a entidade continua a apontar.

### Eliminar uma entidade organizadora
1. Escolha **Eliminar** na respetiva linha e confirme.
2. A entidade sai do catálogo e deixa de aparecer em qualquer parte da aplicação. O nome e a sigla continuam ocupados, para que ninguém registe uma segunda com o mesmo nome por engano.
3. Uma entidade que alguma [edição](exam-editions.md) indique não pode ser eliminada: altere ou elimine primeiro essas edições.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Nome | Como a entidade aparece em toda a aplicação | Obrigatório, de 2 a 150 caracteres, não pode repetir o nome de outra entidade, incluindo uma eliminada. Maiúsculas e acentos não fazem um nome diferente |
| Sigla | O nome curto (CEBRASPE, FGV) | Obrigatória, de 2 a 20 caracteres, guardada em maiúsculas, única da mesma forma que o nome |
| Tipo | Júri de exame, entidade certificadora ou universidade | Obrigatório |
| Sítio oficial | Onde ler sobre a entidade | Opcional, no máximo 300 caracteres, endereço completo a começar por `http://` ou `https://` |
| Descrição | Texto livre sobre a entidade | Opcional, no máximo 500 caracteres |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Outra entidade organizadora já tem este nome. | O nome está ocupado, talvez por uma entidade eliminada. | Escolha outro nome. |
| Outra entidade organizadora já tem esta sigla. | A sigla está ocupada, em qualquer combinação de maiúsculas. | Escolha outra sigla. |
| Escreva um nome com pelo menos 2 caracteres. | O nome está vazio ou é demasiado curto. | Escreva o nome completo. |
| Escreva uma sigla com pelo menos 2 caracteres. | A sigla está vazia ou é demasiado curta. | Escreva a sigla. |
| Escreva o endereço completo, a começar por http:// ou https:// | O sítio não é um endereço completo. | Acrescente `https://` à frente ou deixe o campo vazio. |
| Esta entidade organizadora já não existe. Atualize a lista. | Alguém a eliminou enquanto o seu ecrã estava aberto. | Recarregue a página. |
| Há edições que indicam esta entidade organizadora. Altere ou elimine primeiro essas edições. | A entidade é usada por uma edição. | Altere ou elimine essas edições. |
| Página não encontrada | A sua conta não gere o catálogo. | Fale com um administrador se acha que devia gerir. |

## Páginas relacionadas
- [Entidades contratantes](issuing-authorities.md): quem publica o aviso de cada exame (Administradores)
- [Exames](exams.md): os exames do catálogo (Administradores)
- [Navegar na aplicação](getting-around.md): menu, tema claro e escuro, idioma e teclado
- [Perfis e permissões](roles.md): quem pode gerir o catálogo
- [Simulab](index.md)
