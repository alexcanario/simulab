---
page: issuing-authorities
locale: pt-BR
features: [F-34]
updated: 2026-09-25
---
# Órgãos contratantes

Um órgão contratante é quem publica o edital e define os cargos, o conteúdo e as regras de um exame: uma
prefeitura, um governo estadual, um ministério, uma universidade, uma empresa. Todo exame do catálogo pertence a
um deles.

Não confunda com a [banca](organizers.md): a banca é contratada para elaborar, aplicar e corrigir a prova, e
pode mudar de uma edição para a outra.

## Quem pode usar
Só uma conta com a permissão "Gerenciar o catálogo" — o papel Administrador tem essa permissão — consegue abrir
a tela Órgãos contratantes. Para as demais, a página não é encontrada.

## Como fazer
### Ver os órgãos contratantes
1. Escolha **Órgãos contratantes**, na seção **Conteúdo** do menu lateral.
2. A lista mostra o nome e a sigla, em ordem de nome. Clique no título de uma coluna para ordenar por ela.
3. Digite na caixa de busca para filtrar por nome ou sigla. Acento e maiúscula não importam.

### Cadastrar um órgão contratante
1. Escolha **Adicionar**.
2. Digite o **nome**, como o órgão assina seus editais, e a **sigla**. A sigla é salva em maiúsculas.
3. O **site oficial** e a **descrição** são opcionais. O site é o endereço completo, começando com `https://`.
4. Escolha **Salvar**.

### Alterar um órgão contratante
1. Escolha **Editar** na linha dele.
2. Mude o que precisar e escolha **Salvar**. Os exames que já pertencem a ele continuam pertencendo.

### Excluir um órgão contratante
1. Escolha **Excluir** na linha dele e confirme.
2. Se ele ainda tiver exames no catálogo, a exclusão é recusada e um aviso aparece no topo da lista. Exclua
   antes esses exames, na página [Exames](exams.md) — filtre a lista por este órgão para achá-los.
3. Sem exames, o órgão sai do catálogo. O nome e a sigla continuam ocupados, para ninguém cadastrar um segundo
   com o mesmo nome por engano.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Nome | Como o órgão aparece em todo lugar | Obrigatório, de 2 a 150 caracteres, não pode repetir o nome de outro órgão, inclusive de um excluído. Maiúscula e acento não fazem um nome diferente |
| Sigla | O nome curto (PMF, MEC, UFC) | Obrigatória, de 2 a 20 caracteres, salva em maiúsculas, única do mesmo jeito que o nome |
| Site oficial | Onde ler sobre o órgão | Opcional, endereço completo começando com `http://` ou `https://` |
| Descrição | Texto livre sobre o órgão | Opcional, no máximo 500 caracteres |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Outro órgão contratante já tem esse nome. | O nome está ocupado, talvez por um órgão excluído. | Escolha outro nome. |
| Outro órgão contratante já tem essa sigla. | A sigla está ocupada, em qualquer combinação de maiúsculas. | Escolha outra sigla. |
| Informe um nome com ao menos 2 caracteres. | O nome está vazio ou curto demais. | Digite o nome completo. |
| O nome é muito longo: no máximo 150 caracteres. | O nome passou do limite. | Encurte o nome. |
| Informe uma sigla com ao menos 2 caracteres. | A sigla está vazia ou curta demais. | Digite a sigla. |
| A sigla é muito longa: no máximo 20 caracteres. | A sigla passou do limite. | Encurte a sigla. |
| A descrição é muito longa: no máximo 500 caracteres. | A descrição passou do limite. | Encurte a descrição. |
| Informe um endereço completo, começando com http:// ou https:// | O site não é um endereço completo. | Acrescente `https://` na frente ou deixe o campo vazio. |
| Este órgão contratante tem exames no catálogo. Exclua esses exames primeiro, na página Exames. | O órgão ainda é dono de pelo menos um exame. | Vá até Exames, filtre por este órgão, exclua os exames e volte. |
| Este órgão contratante não existe mais. Atualize a lista. | Alguém excluiu o órgão enquanto sua tela estava aberta. | Recarregue a página. |
| Página não encontrada | Sua conta não gerencia o catálogo. | Fale com um administrador se você acha que deveria gerenciar. |

## Páginas relacionadas
- [Exames](exams.md): os exames de cada órgão contratante (Administradores)
- [Bancas](organizers.md): quem elabora e aplica as provas (Administradores)
- [Navegando pelo app](getting-around.md): menu, tema claro e escuro, idioma e teclado
- [Papéis e permissões](roles.md): quem pode gerenciar o catálogo
- [Simulab](index.md)
