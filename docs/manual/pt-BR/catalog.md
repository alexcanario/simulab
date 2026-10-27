---
page: catalog
locale: pt-BR
features: [F-36, F-37, F-42, F-57, F-76, F-89, F-168, F-169]
updated: 2026-10-27
---
# Catálogo

O catálogo é onde você encontra o exame para o qual está se preparando. Ele lista os exames que têm pelo menos uma edição publicada, e a página de cada exame mostra as edições publicadas com um link para o edital oficial.

## Quem pode usar
Qualquer conta com login cujo papel tenha a permissão "Consultar o catálogo". Alunos, Curadores e Administradores a têm desde o início; um Administrador pode retirá-la de um papel em [Papéis e permissões](roles.md). Sem ela, não há item Catálogo no menu e o endereço mostra Página não encontrada.

O catálogo mostra o mesmo para todos, inclusive para Administradores: exames com apenas edições em rascunho não aparecem. Os rascunhos ficam na administração.

## Como fazer
### Encontrar um exame
1. Escolha **Catálogo** na seção **Estudo** do menu.
2. Digite na caixa de busca. As palavras podem ser parte do nome do exame, do nome do órgão contratante, ou do estado ou da cidade, em qualquer ordem, com ou sem acentos e maiúsculas. "guarda sp" encontra a "Guarda Municipal" de um órgão de São Paulo. Um exame estadual é encontrado pelo nome ou pela sigla do estado ("sp", "sao paulo" ou "São Paulo") e mostra o estado como "São Paulo (SP)", na lista e na página do exame.
3. Restrinja a lista com **Tipo de avaliação**, **Abrangência**, **Estado**, **Banca** e **Ano do edital**, sozinhos ou junto com o texto. A banca e o ano são comparados na mesma edição: um exame só aparece para "FGV" e "2025" quando tem uma edição publicada da FGV em 2025.
4. Para ver os exames de um estado, abra **Estado** (a lista inteira aparece; digite um nome ou uma sigla para reduzi-la) e escolha um, como "São Paulo (SP)". Só os exames estaduais desse estado aparecem, e **Abrangência** passa a Estadual. Escolher outra abrangência, ou todas, esvazia o estado; limpar só o estado mantém a abrangência. Exames nacionais e municipais nunca aparecem sob um estado.
5. Escolha **Limpar filtros** para remover os filtros, inclusive o estado. O texto da busca tem o próprio botão de limpar.

O endereço da página guarda sua busca, seus filtros e a página, então você pode marcá-lo ou compartilhá-lo. Ao voltar de um exame, a lista reaparece como você a deixou.

### Ver um exame e suas edições
1. Escolha o nome do exame na lista.
2. A página mostra as edições, da mais recente para a mais antiga, com a banca, o cargo, o número do edital e a data de aplicação quando existem. Quando o edital fixa as regras da prova, uma linha as resume, só com as que a edição tem: por exemplo "5 h · 100 pontos, aprovação com 60 · Redação de 20 pontos, aprovação com 10". Uma edição sem regras não mostra essa linha.
3. Escolha **Edital oficial** para abrir o edital no site do organizador. Ele abre em uma nova aba.
4. Quando a edição tem uma cópia do edital guardada no Simulab, ao lado de **Edital oficial** aparece o link **Edital (PDF)**: escolha-o para baixar o arquivo. Ele serve para o caso de o site oficial mudar de endereço ou sair do ar. Uma edição sem arquivo não mostra o link.
5. O cartão **Sobre este exame** traz o órgão contratante, o tipo de avaliação, a abrangência, o idioma do exame e quantas edições estão publicadas.
6. Cada edição publicada termina com o botão **Fazer este simulado**, que começa ou continua o seu simulado dessa edição (veja [Fazer um simulado](take-exam.md)). Ele aparece para quem tem a permissão **Fazer provas**.

O conteúdo do exame aparece no próprio idioma e nunca é traduzido.

### Ver o conteúdo programático de uma edição
1. Na página do exame, escolha **Ver conteúdo programático** na linha da edição. O botão diz quantas disciplinas do edital ela tem.
2. O conteúdo abre sob a linha. As disciplinas aparecem na ordem do edital, sob o título do grupo quando o edital as agrupa, cada uma com o número de questões (um traço quando o edital não o informa).
3. Sob cada disciplina estão as disciplinas e os tópicos do produto que ela cobre, como "Matemática · disciplina inteira" ou "Raciocínio Lógico › Proposições". Quando ainda não há nenhum, a linha diz "Tópicos ainda não mapeados".
4. No fim, o total de questões informado no edital e quantas disciplinas ficaram fora da soma por não informarem número.
5. Escolha **Ocultar conteúdo programático** para fechar. Cada edição abre e fecha por conta própria, e várias podem ficar abertas ao mesmo tempo.

Uma edição sem disciplinas do edital cadastradas não tem botão e diz que o conteúdo programático ainda não foi cadastrado.

## O que um ambiente novo já tem
Todo ambiente começa com os exames de guarda municipal, então o catálogo nunca começa vazio. Quatro exames aparecem para os alunos, cada um com a edição publicada e o link do edital oficial: Guarda Municipal de Curitiba (Instituto AOCP, edital de 2025), Guarda Municipal de Manaus (Instituto Consulplan, 2026), Guarda Civil Municipal de Salvador (FGV, 2026) e Guarda Civil Municipal de Maceió (Copeve/Ufal, 2026). Os exames de Recife e Goiânia já estão cadastrados, mas ainda não têm edital, então ficam só na administração até um Administrador cadastrar e publicar uma edição. As edições publicadas não mostram data de aplicação, porque ela não foi confirmada nos sites oficiais.

Essas linhas são dados comuns: um Administrador pode editar ou excluir qualquer uma e a mudança permanece.

## Campos
| Campo | Significado | Regras |
|---|---|---|
| Busca | Parte do nome do exame, do nome do órgão contratante, ou do estado ou da cidade | Todas as palavras precisam combinar em algum lugar; maiúsculas e acentos não importam |
| Tipo de avaliação | Concurso público, certificação, vestibular ou ENEM | Um valor ou todos |
| Abrangência | Nacional, Estadual ou Municipal | Um valor ou todos |
| Estado | Um dos 27 estados, mostrado como "São Paulo (SP)" | Só aparecem estados com um exame estadual publicado, na ordem da lista oficial; escolher um define a Abrangência como Estadual |
| Banca | O organizador que aplicou uma edição publicada | Só aparecem bancas com algo publicado |
| Ano do edital | O ano do edital de uma edição publicada | Só aparecem anos com algo publicado |
| Edições | Quantas edições do exame estão publicadas | Conta todas, quaisquer que sejam os filtros |
| Último ano | O ano de edital mais recente entre as edições publicadas | — |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Ainda não há exames publicados no catálogo. | Nada foi publicado. | Volte mais tarde. |
| Nenhum exame publicado corresponde a "…". | A busca não achou nada. | Tente menos palavras ou palavras mais curtas. |
| Nenhum exame publicado corresponde aos filtros escolhidos. | Os filtros juntos não deixam nada. | Escolha **Limpar filtros**. |
| Não foi possível carregar as opções de banca, ano e estado. O resto da busca funciona. | As três listas de opções não carregaram. | Escolha **Tentar novamente** ou continue buscando sem elas. |
| Não foi possível carregar este exame. | A página do exame não carregou. | Escolha **Tentar novamente**. |
| Este exame não existe mais. | O exame não existe, foi removido ou não tem nada publicado. | Escolha **Voltar para o catálogo**. |
| O conteúdo programático desta edição ainda não foi cadastrado. | A edição não tem disciplinas do edital registradas. | Nada; ele aparece quando um Administrador o cadastrar. |
| Tópicos ainda não mapeados | A disciplina do edital ainda não foi ligada às disciplinas e tópicos do produto. | Nada; é só um aviso de que a lista ainda não existe. |

## Páginas relacionadas
- [Como navegar](getting-around.md)
- [Exames](exams.md) e [Edições de um exame](exam-editions.md): como os Administradores preparam o que o catálogo mostra
- [Papéis e permissões](roles.md): quem pode consultar o catálogo
