---
page: organizers
locale: pt-BR
features: [F-33, F-34, F-35]
updated: 2026-09-29
---
# Bancas

Uma banca é quem elabora, aplica e corrige a prova: a banca contratada para um concurso público, a entidade que
emite certificações ou a universidade com vestibular próprio. Ela é escolhida em cada edição de um exame, e pode
mudar de uma edição para a outra.

Quem publica o edital e define as regras do exame é o [órgão contratante](issuing-authorities.md), não a banca.

## Quem pode usar
Só uma conta com a permissão "Gerenciar o catálogo" — o papel Administrador tem essa permissão — consegue abrir a tela Bancas. Para as demais, a página não é encontrada.

## Como fazer
### Ver as bancas
1. Escolha **Bancas**, na seção **Conteúdo** do menu lateral.
2. A lista mostra o nome, a sigla e o tipo, em ordem de nome. Clique no título de uma coluna para ordenar por ela.
3. Digite na caixa de busca para filtrar por nome ou sigla. Acento e maiúscula não importam: "fundacao" encontra "Fundação".

### Cadastrar uma banca
1. Escolha **Adicionar**.
2. Digite o **nome**, como a banca assina seus editais, e a **sigla**. A sigla é salva em maiúsculas.
3. Escolha o **tipo**: banca examinadora, certificadora ou universidade.
4. O **site oficial** e a **descrição** são opcionais. O site é o endereço completo, começando com `https://`.
5. Escolha **Salvar**.

### Alterar uma banca
1. Escolha **Editar** na linha dela.
2. Mude o que precisar e escolha **Salvar**. Tudo o que já aponta para a banca continua apontando.

### Excluir uma banca
1. Escolha **Excluir** na linha dela e confirme.
2. A banca sai do catálogo e deixa de aparecer em qualquer lugar do app. O nome e a sigla continuam ocupados, para ninguém cadastrar uma segunda com o mesmo nome por engano.
3. Uma banca que alguma [edição](exam-editions.md) indica não pode ser excluída: altere ou exclua antes essas edições.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Nome | Como a banca aparece em todo lugar | Obrigatório, de 2 a 150 caracteres, não pode repetir o nome de outra banca, inclusive de uma excluída. Maiúscula e acento não fazem um nome diferente |
| Sigla | O nome curto (CEBRASPE, FGV) | Obrigatória, de 2 a 20 caracteres, salva em maiúsculas, única do mesmo jeito que o nome |
| Tipo | Banca examinadora, certificadora ou universidade | Obrigatório |
| Site oficial | Onde ler sobre a banca | Opcional, no máximo 300 caracteres, endereço completo começando com `http://` ou `https://` |
| Descrição | Texto livre sobre a banca | Opcional, no máximo 500 caracteres |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Outra banca já tem esse nome. | O nome está ocupado, talvez por uma banca excluída. | Escolha outro nome. |
| Outra banca já tem essa sigla. | A sigla está ocupada, em qualquer combinação de maiúsculas. | Escolha outra sigla. |
| Digite um nome com pelo menos 2 caracteres. | O nome está vazio ou curto demais. | Digite o nome completo. |
| Digite uma sigla com pelo menos 2 caracteres. | A sigla está vazia ou curta demais. | Digite a sigla. |
| Digite o endereço completo, começando com http:// ou https:// | O site não é um endereço completo. | Acrescente `https://` na frente ou deixe o campo vazio. |
| Esta banca não existe mais. Atualize a lista. | Alguém excluiu a banca enquanto sua tela estava aberta. | Recarregue a página. |
| Há edições que indicam esta banca. Altere ou exclua essas edições primeiro. | A banca é usada por uma edição. | Altere ou exclua essas edições. |
| Página não encontrada | Sua conta não gerencia o catálogo. | Fale com um administrador se você acha que deveria gerenciar. |

## Páginas relacionadas
- [Órgãos contratantes](issuing-authorities.md): quem publica o edital de cada exame (Administradores)
- [Exames](exams.md): os exames do catálogo (Administradores)
- [Navegando pelo app](getting-around.md): menu, tema claro e escuro, idioma e teclado
- [Papéis e permissões](roles.md): quem pode gerenciar o catálogo
- [Simulab](index.md)
