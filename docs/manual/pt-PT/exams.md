---
page: exams
locale: pt-PT
features: [F-34, F-43, F-35, F-42]
updated: 2026-10-02
---
# Exames

Um exame é a avaliação que se repete ao longo dos anos: o concurso de uma câmara municipal, o ENEM, uma
certificação, o exame de acesso de uma universidade. Cada exame pertence à **entidade contratante** que publica
o aviso. Cada prova efetivamente aplicada é uma [edição](exam-editions.md), gerida na página do próprio exame.

## Quem pode utilizar
Só uma conta com a permissão "Gerir o catálogo" — o perfil Administrador tem essa permissão — consegue abrir o
ecrã Exames. Para as restantes, a página não é encontrada.

## Como fazer
### Ver os exames
1. Escolha **Exames**, na secção **Conteúdo** do menu lateral.
2. A lista mostra o nome, a entidade contratante, o tipo de avaliação e a abrangência, por ordem de nome. Clique
   no título de uma coluna para ordenar por ela.
3. Escreva na caixa de pesquisa para filtrar pelo nome. Acentos e maiúsculas não contam: "publica" encontra
   "Pública".
4. Os três filtros acima da lista — entidade contratante, tipo de avaliação e abrangência — podem ser
   combinados. No filtro de entidade contratante, escreva duas letras do nome e escolha na lista que
   aparece.

### Registar um exame
1. Escolha **Adicionar**. O formulário abre numa página própria.
2. Em **Entidade contratante**, escreva duas letras do nome e escolha na lista. Se a entidade ainda
   não existe, registe-a antes em [Entidades contratantes](issuing-authorities.md).
3. Escreva o **nome** do exame, tal como aparece no aviso.
4. Escolha o **tipo de avaliação**. Escolha a **abrangência** entre os três cartões — cada um explica numa
   linha o que significa.
5. Se a abrangência for Estadual ou Municipal, aparece mais um campo para dizer **onde** o exame se aplica.
   - **Estadual**: o campo é uma lista com os 27 estados brasileiros (26 estados e o Distrito Federal). Clique nele
     para os ver todos, cada um como "São Paulo (SP)"; escreva parte do nome ou da sigla ("sp", "paulo", "sao") para
     filtrar a lista, com ou sem acentos, e escolha um.
   - **Municipal**: escreva o nome do município, em texto livre.
   - Voltar a abrangência para Nacional esconde o campo e descarta o que escolheu. Passar de Estadual para
     Municipal, ou o contrário, deixa o campo vazio.
6. Escolha o **idioma do conteúdo**. Começa em Português (Brasil). É o idioma em que o exame e as suas questões
   estão escritos; o conteúdo nunca é traduzido.
7. Escolha **Guardar**. Fica na página, agora a editar o exame que acabou de criar.

O formulário é lido em três blocos com título — identificação, classificação e onde o exame se aplica — e a
coluna da direita mostra o que já está preenchido e o que ainda falta. Se guardar com algo em falta, um aviso no
topo do cartão lista todos os campos em falta; clicar num deles leva-o diretamente ao campo.

### Alterar um exame
1. Escolha **Editar** na linha dele.
2. Altere o que precisar e escolha **Guardar**. Se sair da página com alterações por guardar, a aplicação
   pergunta primeiro.

### Eliminar um exame
1. Escolha **Eliminar** na linha dele e confirme.
2. O exame sai do catálogo. O nome continua reservado dentro daquela entidade contratante, para ninguém registar
   um segundo exame com o mesmo nome por engano.
3. Um exame que ainda tem edições não pode ser eliminado: elimine primeiro as [edições](exam-editions.md) dele.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Entidade contratante | Quem publica o aviso e define as regras do exame | Obrigatório. A entidade que aplica cada prova não é escolhida aqui: pertence à edição |
| Nome | Como o exame aparece em todo o lado | Obrigatório, entre 2 e 200 caracteres. Não pode repetir o nome de outro exame da mesma entidade contratante, incluindo um eliminado. Maiúsculas e acentos não fazem um nome diferente |
| Tipo de avaliação | Concurso público, certificação, exame de acesso ou ENEM | Obrigatório |
| Abrangência | Nacional, estadual ou municipal | Obrigatório |
| Estado / Município | Onde o exame se aplica | Obrigatório quando a abrangência é Estadual ou Municipal. Num exame Estadual, um dos 27 estados da lista; aparece em todo o lado como "São Paulo (SP)", e a mesma lista é oferecida seja qual for o idioma do conteúdo. Num exame Municipal, texto livre de no máximo 120 caracteres. Não aparece quando é Nacional |
| Idioma do conteúdo | O idioma em que o exame e as suas questões estão escritos | Obrigatório, um entre Português (Brasil), Português (Portugal) e English. Começa em Português (Brasil) |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Escolha a entidade contratante. | Nenhuma entidade foi escolhida no campo de pesquisa. | Escreva duas letras e escolha uma da lista. |
| Indique um nome com pelo menos 2 caracteres. | O nome está vazio ou demasiado curto. | Escreva o nome do exame. |
| O nome é demasiado longo: no máximo 200 caracteres. | O nome passou do limite. | Encurte o nome. |
| Esta entidade já tem um exame com este nome. | O nome está ocupado dentro daquela entidade, talvez por um exame eliminado. | Escolha outro nome, ou outra entidade contratante. |
| Escolha um tipo de avaliação. | O tipo não foi escolhido. | Escolha um dos quatro tipos. |
| Escolha a abrangência. | A abrangência não foi escolhida. | Escolha nacional, estadual ou municipal. |
| Indique onde este exame se aplica. | A abrangência é estadual ou municipal e o campo ficou vazio. | Escolha o estado na lista, ou escreva o município. |
| Escolha um dos 27 estados da lista. | O estado foi recusado porque não é um dos 27 da lista. | Abra a lista e escolha o estado. |
| Este exame foi guardado com “…”, que não é um estado da lista. Escolha o estado correto. | Foi aberto um exame guardado antes de existir a lista de estados, com um texto que nenhum estado reconhece (por exemplo "Sampa"). O campo fica vazio e a dica cita o texto antigo. | Escolha o estado correto e guarde; guardar sem estado é recusado. |
| Este campo é demasiado longo: no máximo 120 caracteres. | O estado ou município passou do limite. | Encurte o texto. |
| Escolha o idioma do conteúdo. | O idioma não foi escolhido. | Escolha um dos três idiomas. |
| Este exame já não existe. | Alguém eliminou o exame enquanto o seu ecrã estava aberto. | Recarregue a página. |
| Nenhum exame corresponde aos filtros escolhidos. | A pesquisa e os filtros juntos não encontraram nada. | Limpe um filtro e tente de novo. |
| Este exame tem edições. Elimine primeiro as edições. | O exame ainda tem edições. | Elimine as edições e depois o exame. |
| Página não encontrada | A sua conta não gere o catálogo. | Fale com um administrador se acha que devia gerir. |

## Páginas relacionadas
- [Entidades contratantes](issuing-authorities.md): quem publica o aviso de cada exame (Administradores)
- [Entidades organizadoras](organizers.md): quem elabora e aplica as provas (Administradores)
- [Como navegar](getting-around.md): menu, modo claro e escuro, idioma e teclado
- [Perfis e permissões](roles.md): quem pode gerir o catálogo
- [Simulab](index.md)
