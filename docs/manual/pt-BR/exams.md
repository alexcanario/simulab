---
page: exams
locale: pt-BR
features: [F-34]
updated: 2026-09-25
---
# Exames

Um exame é a avaliação que se repete ao longo dos anos: o concurso da Prefeitura de Fortaleza, o ENEM, uma
certificação, o vestibular de uma universidade. Cada exame pertence ao **órgão contratante** que publica o
edital. As edições de cada ano, com a banca que aplica a prova, vêm depois.

## Quem pode usar
Só uma conta com a permissão "Gerenciar o catálogo" — o papel Administrador tem essa permissão — consegue abrir
a tela Exames. Para as demais, a página não é encontrada.

## Como fazer
### Ver os exames
1. Escolha **Exames**, na seção **Conteúdo** do menu lateral.
2. A lista mostra o nome, o órgão contratante, o tipo de avaliação e a abrangência, em ordem de nome. Clique no
   título de uma coluna para ordenar por ela.
3. Digite na caixa de busca para filtrar por nome. Acento e maiúscula não importam: "publica" encontra "Pública".
4. Os três filtros acima da lista — órgão contratante, tipo de avaliação e abrangência — podem ser combinados.
   No filtro de órgão contratante, digite duas letras do nome ou da sigla e escolha na lista que aparece.

### Cadastrar um exame
1. Escolha **Adicionar**. O formulário abre em uma página própria.
2. Em **Órgão contratante**, digite duas letras do nome ou da sigla e escolha na lista. Se o órgão ainda não
   existe, cadastre-o antes em [Órgãos contratantes](issuing-authorities.md).
3. Digite o **nome** do exame, como ele aparece no edital.
4. Escolha o **tipo de avaliação** e a **abrangência**.
5. Se a abrangência for Estadual ou Municipal, aparece um campo a mais para dizer **onde** o exame se aplica.
   Voltar a abrangência para Nacional esconde o campo e descarta o que você digitou.
6. Escolha o **idioma do conteúdo**. Ele começa em Português (Brasil). Esse é o idioma em que o exame e suas
   questões estão escritos; o conteúdo nunca é traduzido.
7. Escolha **Salvar**. Você continua na página, agora editando o exame que acabou de criar.

### Alterar um exame
1. Escolha **Editar** na linha dele.
2. Mude o que precisar e escolha **Salvar**. Se sair da página com alterações não salvas, o app pergunta antes.

### Excluir um exame
1. Escolha **Excluir** na linha dele e confirme.
2. O exame sai do catálogo. O nome continua reservado dentro daquele órgão contratante, para ninguém cadastrar
   um segundo exame com o mesmo nome por engano.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Órgão contratante | Quem publica o edital e define as regras do exame | Obrigatório. A banca que aplica cada prova não é escolhida aqui: ela pertence à edição |
| Nome | Como o exame aparece em todo lugar | Obrigatório, de 2 a 200 caracteres. Não pode repetir o nome de outro exame do mesmo órgão contratante, inclusive de um excluído. Maiúscula e acento não fazem um nome diferente |
| Tipo de avaliação | Concurso público, certificação, vestibular ou ENEM | Obrigatório |
| Abrangência | Nacional, estadual ou municipal | Obrigatório |
| Estado / Município | Onde o exame se aplica | Obrigatório quando a abrangência é Estadual ou Municipal, no máximo 120 caracteres. Não aparece quando é Nacional |
| Idioma do conteúdo | O idioma em que o exame e suas questões estão escritos | Obrigatório, um entre Português (Brasil), Português (Portugal) e English. Começa em Português (Brasil) |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Escolha o órgão contratante. | Nenhum órgão foi escolhido no campo de busca. | Digite duas letras e escolha um da lista. |
| Informe um nome com ao menos 2 caracteres. | O nome está vazio ou curto demais. | Digite o nome do exame. |
| O nome é muito longo: no máximo 200 caracteres. | O nome passou do limite. | Encurte o nome. |
| Este órgão já tem um exame com este nome. | O nome está ocupado dentro daquele órgão, talvez por um exame excluído. | Escolha outro nome, ou outro órgão contratante. |
| Escolha um tipo de avaliação. | O tipo não foi escolhido. | Escolha um dos quatro tipos. |
| Escolha a abrangência. | A abrangência não foi escolhida. | Escolha nacional, estadual ou municipal. |
| Informe onde este exame se aplica. | A abrangência é estadual ou municipal e o campo ficou vazio. | Digite o estado ou o município. |
| Este campo é muito longo: no máximo 120 caracteres. | O estado ou município passou do limite. | Encurte o texto. |
| Escolha o idioma do conteúdo. | O idioma não foi escolhido. | Escolha um dos três idiomas. |
| Este exame não existe mais. | Alguém excluiu o exame enquanto sua tela estava aberta. | Recarregue a página. |
| Nenhum exame corresponde aos filtros escolhidos. | A combinação de busca e filtros não encontrou nada. | Limpe um filtro e tente de novo. |
| Página não encontrada | Sua conta não gerencia o catálogo. | Fale com um administrador se você acha que deveria gerenciar. |

## Páginas relacionadas
- [Órgãos contratantes](issuing-authorities.md): quem publica o edital de cada exame (Administradores)
- [Bancas](organizers.md): quem elabora e aplica as provas (Administradores)
- [Navegando pelo app](getting-around.md): menu, tema claro e escuro, idioma e teclado
- [Papéis e permissões](roles.md): quem pode gerenciar o catálogo
- [Simulab](index.md)
