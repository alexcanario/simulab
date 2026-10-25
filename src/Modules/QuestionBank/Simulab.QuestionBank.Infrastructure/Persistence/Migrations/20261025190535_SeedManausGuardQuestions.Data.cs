#nullable disable

namespace Simulab.QuestionBank.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// F-110: the frozen content of the seed, transcribed from the Manaus 2026 paper (booklet type 1) and checked against
    /// its pages; the keys are the final answer key. The HTML is what the sanitizer returns, the plain text what it reads
    /// back and the normalized text what <c>ComparableText</c> makes of it (BR2). Never edit a literal here: a change to a
    /// seeded question is a curator's edit, or a new migration.
    /// </summary>
    public partial class SeedManausGuardQuestions
    {
        private const string BaseTextTitle = "Calma, gente";
        private const string BaseTextNormalizedTitle = "CALMA, GENTE";

        private const string BaseTextHtml =
            """<p>Alguma coisa não vai bem entre mim e o tempo. Não o tempo de que tratam os filósofos, mas esse tempinho nosso de todo dia, medido em correrias, impaciências, tique-taques, calendários, reencontros (“Há quanto tempo!”), semáforos, luas cheias, aniversários, Natais e – ai – rugas. O tempo sobre o qual se conversa e no qual transitamos, transitórios.</p><p>Acontece que as pessoas têm pressa, e a pressa delas interfere no ritmo de outras. Na maioria das vezes, é uma agitação inútil e inexplicável. Tem gente que se assusta quando alguém propõe irem caminhando até um determinado lugar, perto: “A pé?!”. Não é pelo esforço, pois até atletas de academia reagem com espanto. Essas pessoas não suportam é “perder” tempo percorrendo uma distância que, de carro, levaria quatro minutos.</p><p>Em parte, foi essa pretensão de poder comprimir o tempo que derrotou o cavalo como transporte urbano, depois o bonde, o ônibus e promoveu o automóvel, maravilha que transformamos em problema. Ao volante, o raciocínio é: eu tenho o comando, eu decido a velocidade, eu me torno senhor do tempo no espaço.</p><p>Ilusão.</p><p>Quem pôde teve a mesma ideia e engarrafou as cidades.</p><p>O tempo já foi elástico, esticava-se segundo a vontade de quem dispunha dele. Dê tempo ao tempo, diziam umas pessoas para as outras, ralentando-se. Calma, que o Brasil ainda é nosso! – bradava-se, como quem diz: enquanto o país for nosso, vamos devagar. Fazíamos do tempo coisa nossa, como o samba, o futebol e outras bossas.</p><p>Leiam os romances antigos. Nenhum personagem diz para o outro: “Você tem um minuto?”. Havia muito mais do que um minuto para uma conversa. Vejam um filme clássico. Com que paciência era construída uma situação que iria depois desaguar em outra. John Ford, por exemplo, tinha tempo para contar uma boa história e sabia que também o tínhamos para apreciá-la. Hoje, no cinema pós-Spielberg, muitas vezes nem percebemos o que aconteceu, tal a rapidez da montagem.</p><p>A vida <em>on-line</em> traz, em segundos, o mundo. As imagens de um bombardeio da grande potência contra o Iraque depauperado chegaram à casa das pessoas no momento em que estava acontecendo. Chamam a isso “tempo real”. Como se fosse irreal o tempo dos cinejornais da II Guerra Mundial, que mostravam com meses de atraso centenas de milhares de soldados mortos. O tempo real trouxe também a globalização dos dinheiros aventureiros, que em segundos dão a volta ao mundo rapando economias, confrontando desiguais, espalhando o desemprego.</p><p>O que se faz com o tempo ganho com a pressa? Lembra-me o poeminha do pernambucano Ascenso Ferreira ironizando o gaúcho, que, diz ele, “riscando os cavalos” e tinindo as esporas sai de seus pagos em louca arrancada: “– Para quê? – Para nada”. Talvez para nada os apressados buzinam no trânsito, costuram, furam sinais; a pé, atropelam passantes nas ruas, empurram pessoas nas plataformas do metrô, impacientam-se com idosos, agridem garçons, trombam carrinhos de compras nos supermercados, reclamam do ritmo alheio. Entre a pressa e a falta de educação, a distância é curta.</p><p>É sábio um ditado russo que li citado pelo escritor Saul Bellow: “Quando estiver com pressa, vá devagar”. Mais ou menos é o que o historiador romano Suetônio, biógrafo dos césares, aconselhou ao imperador Adriano, 1.900 anos atrás: “Apressa-te devagar”. Sem nunca ter lido Suetônio, era quase o que minha mãe dizia quando eu moleque disparava pelas ruas do bairro: “Corre devagar, menino!”.</p><p>Suspeito que vem daí o meu descompasso com os apressados.</p><p><em>(ÂNGELO, Ivan. Revista Veja São Paulo. São Paulo: Editora Abril. Em: 10/09/2003.)</em></p>""";

        // The three pictures of the paper: cartogram 1 and 2 of question 16, the climogram of question 17 (sizes of the
        // embedded files).
        private static readonly SeedImage[] Images =
        [
            new(1, 16, 119641),
            new(2, 16, 143978),
            new(3, 17, 131836),
        ];

        private static SeedQuestion[] Questions => [.. Portuguese(), .. ComputingAndManaus(), .. RightsAndLaw(), .. ProcedureAndTraffic(), .. SpecificLegislation()];

        private static SeedQuestion[] Portuguese() =>
        [
            new(1, "Língua Portuguesa", "Leitura e Interpretação › Interpretação de textos", 'B',
                """<p>Acerca do gênero textual apresentado, analise as características a seguir.</p><p><strong>I.</strong> Proposição de reflexão a partir de situações da realidade observável.</p><p><strong>II.</strong> Objetivo comunicacional que permite indicar classificação do tipo de texto injuntivo.</p><p><strong>III.</strong> Estruturas linguísticas de conteúdo objetivo que permitem o reconhecimento de uma sequência linear temporal do início ao fim.</p><p>Está correto o que se afirma em</p>""",
                """Acerca do gênero textual apresentado, analise as características a seguir. I. Proposição de reflexão a partir de situações da realidade observável. II. Objetivo comunicacional que permite indicar classificação do tipo de texto injuntivo. III. Estruturas linguísticas de conteúdo objetivo que permitem o reconhecimento de uma sequência linear temporal do início ao fim. Está correto o que se afirma em""",
                """ACERCA DO GENERO TEXTUAL APRESENTADO, ANALISE AS CARACTERISTICAS A SEGUIR. I. PROPOSICAO DE REFLEXAO A PARTIR DE SITUACOES DA REALIDADE OBSERVAVEL. II. OBJETIVO COMUNICACIONAL QUE PERMITE INDICAR CLASSIFICACAO DO TIPO DE TEXTO INJUNTIVO. III. ESTRUTURAS LINGUISTICAS DE CONTEUDO OBJETIVO QUE PERMITEM O RECONHECIMENTO DE UMA SEQUENCIA LINEAR TEMPORAL DO INICIO AO FIM. ESTA CORRETO O QUE SE AFIRMA EM""",
                ["I, II e III.", "I, apenas.", "II, apenas.", "I e II, apenas."]),

            new(2, "Língua Portuguesa", "Leitura e Interpretação › Interpretação de textos", 'B',
                """<p>É possível observar que, em relação à referência ao conceito de tempo indicado no texto:</p>""",
                """É possível observar que, em relação à referência ao conceito de tempo indicado no texto:""",
                """E POSSIVEL OBSERVAR QUE, EM RELACAO A REFERENCIA AO CONCEITO DE TEMPO INDICADO NO TEXTO:""",
                [
                    "O narrador apresenta uma perspectiva pessoal pessimista predominante.",
                    "Há variáveis quanto à significação considerando-se o contexto apresentado.",
                    "Ocorre o estabelecimento de hipóteses que, em alguns momentos, apresentam-se contraditórias.",
                    "A efemeridade é uma ideia perseguida e defendida pelo narrador, baseando-se nesse conceito para construir toda a narrativa.",
                ]),

            new(3, "Língua Portuguesa", "Sintaxe › Sintaxe, Concordância e Regência", 'B',
                """<p>Observando-se as relações sintáticas estabelecidas, pode-se afirmar que o termo destacado que complementa a forma verbal empregada de modo indireto está indicado em:</p>""",
                """Observando-se as relações sintáticas estabelecidas, pode-se afirmar que o termo destacado que complementa a forma verbal empregada de modo indireto está indicado em:""",
                """OBSERVANDO-SE AS RELACOES SINTATICAS ESTABELECIDAS, PODE-SE AFIRMAR QUE O TERMO DESTACADO QUE COMPLEMENTA A FORMA VERBAL EMPREGADA DE MODO INDIRETO ESTA INDICADO EM:""",
                [
                    """“<em>Acontece que as pessoas têm <u>pressa</u>, e a pressa delas interfere no ritmo de outras</em>.” (2º§).""",
                    """“<em>Não o tempo de <u>que</u> tratam os filósofos, mas esse tempinho nosso de todo dia</em>, [...]” (1º§).""",
                    """“<em>Tem gente <u>que</u> se assusta quando alguém propõe irem caminhando até um determinado lugar, perto: ‘A pé?!’</em>.” (2º§).""",
                    """“<em>Ao volante, o raciocínio é: eu tenho <u>o comando</u>, eu decido a velocidade, eu me torno senhor do tempo no espaço</em>.” (3º§).""",
                ]),

            new(4, "Língua Portuguesa", "Leitura e Interpretação › Interpretação de textos", 'D',
                """<p>O 4º§ é composto por um único vocábulo – “<em>Ilusão</em>”. É correto afirmar que a escolha de tal composição denota:</p>""",
                """O 4º§ é composto por um único vocábulo – “Ilusão”. É correto afirmar que a escolha de tal composição denota:""",
                """O 4º§ E COMPOSTO POR UM UNICO VOCABULO – “ILUSAO”. E CORRETO AFIRMAR QUE A ESCOLHA DE TAL COMPOSICAO DENOTA:""",
                [
                    "Ruptura existente entre a introdução e o desenvolvimento do texto.",
                    "Objetivo do autor em demonstrar características textuais próprias da crônica.",
                    "Divisão organizacional do texto, que reflete o formato poético utilizado pelo autor em todo o texto.",
                    "Ênfase e relevância do efeito de sentido produzido pelo vocábulo, considerando-se o contexto estabelecido.",
                ]),

            new(5, "Língua Portuguesa", "Sintaxe › Sintaxe, Concordância e Regência", 'D',
                """<p>Em “<em>John Ford, por exemplo, tinha tempo para contar uma boa história e sabia que também o tínhamos para apreciá-la</em>.” (7º§), é possível observar o emprego de:</p>""",
                """Em “John Ford, por exemplo, tinha tempo para contar uma boa história e sabia que também o tínhamos para apreciá-la.” (7º§), é possível observar o emprego de:""",
                """EM “JOHN FORD, POR EXEMPLO, TINHA TEMPO PARA CONTAR UMA BOA HISTORIA E SABIA QUE TAMBEM O TINHAMOS PARA APRECIA-LA.” (7º§), E POSSIVEL OBSERVAR O EMPREGO DE:""",
                [
                    "Orações explicativas e subordinadas aditivas.",
                    "Oração explicativa separada por vírgulas do restante do período.",
                    "Período simples formado por orações subordinadas que se complementam.",
                    "Oração que, reconhecidamente, possui a mesma função exercida sintaticamente pelo objeto direto.",
                ]),

            new(6, "Língua Portuguesa", "Leitura e Interpretação › Interpretação de textos", 'B',
                """<p>Em relação ao título atribuído ao texto, pode-se afirmar que:</p>""",
                """Em relação ao título atribuído ao texto, pode-se afirmar que:""",
                """EM RELACAO AO TITULO ATRIBUIDO AO TEXTO, PODE-SE AFIRMAR QUE:""",
                [
                    "O tratamento dado ao interlocutor demonstra a suavização de uma mensagem que se mostra incisiva e irônica.",
                    "A interlocução direta demonstra que há o emprego de um recurso linguístico que tem como objetivo persuadir o interlocutor em relação ao assunto desenvolvido.",
                    "Embora haja uma evocação, é possível inferir distanciamento entre o enunciador e o interlocutor no desenvolvimento do texto a partir das escolhas linguísticas realizadas.",
                    """A escolha do vocábulo <em>“gente”</em> interfere na produção de sentido, à medida que fica explícito que o conteúdo tem como destinatário pessoas do convívio, próximas do enunciador.""",
                ]),

            new(7, "Língua Portuguesa", "Ortografia e Pontuação › Ortografia", 'A',
                """<p>Em “<em>O que se faz com o tempo ganho com a pressa? Lembra-me o poeminha do pernambucano Ascenso Ferreira ironizando o gaúcho, que, diz ele, ‘riscando os cavalos’ e tinindo as esporas sai de seus pagos em louca arrancada: ‘– Para <u>quê</u>? – Para nada’</em>.” (9º§), o vocábulo destacado recebe acento conforme a mesma justificativa vista de acordo com o emprego correto de “<em>por quê</em>” em:</p>""",
                """Em “O que se faz com o tempo ganho com a pressa? Lembra-me o poeminha do pernambucano Ascenso Ferreira ironizando o gaúcho, que, diz ele, ‘riscando os cavalos’ e tinindo as esporas sai de seus pagos em louca arrancada: ‘– Para quê? – Para nada’.” (9º§), o vocábulo destacado recebe acento conforme a mesma justificativa vista de acordo com o emprego correto de “por quê” em:""",
                """EM “O QUE SE FAZ COM O TEMPO GANHO COM A PRESSA? LEMBRA-ME O POEMINHA DO PERNAMBUCANO ASCENSO FERREIRA IRONIZANDO O GAUCHO, QUE, DIZ ELE, ‘RISCANDO OS CAVALOS’ E TININDO AS ESPORAS SAI DE SEUS PAGOS EM LOUCA ARRANCADA: ‘– PARA QUE? – PARA NADA’.” (9º§), O VOCABULO DESTACADO RECEBE ACENTO CONFORME A MESMA JUSTIFICATIVA VISTA DE ACORDO COM O EMPREGO CORRETO DE “POR QUE” EM:""",
                [
                    "Diante de tudo, você prefere se abster, por quê?",
                    "Os caminhos por quê andei, são caminhos de dores.",
                    "Por quê não podemos reencaminhar o trabalho realizado?",
                    "Por quê cada vez que te vejo as memórias voltam com intensidade cruel.",
                ]),

            new(8, "Língua Portuguesa", "Leitura e Interpretação › Interpretação de textos", 'D',
                """<p>De acordo com o texto, em relação ao tempo, é INCORRETO afirmar que:</p>""",
                """De acordo com o texto, em relação ao tempo, é INCORRETO afirmar que:""",
                """DE ACORDO COM O TEXTO, EM RELACAO AO TEMPO, E INCORRETO AFIRMAR QUE:""",
                [
                    "Há uma relação de relatividade entre a passagem do tempo e o contexto em que ele pode ser observado ou vivenciado.",
                    """A expressão “<em>tempo real</em>”, segundo o texto, não passa de uma realidade ilusória em uma tentativa de significar o que ocorre com o tempo atualmente.""",
                    "A comparação entre comportamentos históricos sociais exemplificados e atuais remetem à sustentação da perspectiva defendida pelo enunciador.",
                    "A valorização do tempo na atualidade é superior ao que era visto no passado, já que existe a consciência da velocidade de como os acontecimentos são divulgados.",
                ]),

            new(9, "Língua Portuguesa", "Produção Textual › Comunicação escrita", 'D',
                """<p>A redação oficial deve obedecer a certos atributos fundamentais para garantir compreensão e uniformidade na comunicação administrativa. De acordo com tal premissa, são consideradas características da redação oficial:</p>""",
                """A redação oficial deve obedecer a certos atributos fundamentais para garantir compreensão e uniformidade na comunicação administrativa. De acordo com tal premissa, são consideradas características da redação oficial:""",
                """A REDACAO OFICIAL DEVE OBEDECER A CERTOS ATRIBUTOS FUNDAMENTAIS PARA GARANTIR COMPREENSAO E UNIFORMIDADE NA COMUNICACAO ADMINISTRATIVA. DE ACORDO COM TAL PREMISSA, SAO CONSIDERADAS CARACTERISTICAS DA REDACAO OFICIAL:""",
                [
                    "Emoção; originalidade; pessoalidade; e prolixidade.",
                    "Ambiguidade; rebuscamento; e liberdade estilística.",
                    "Subjetividade; informalidade; regionalismo; e linguagem figurada.",
                    "Clareza; concisão; impessoalidade e uso do padrão culto da linguagem.",
                ]),

            new(10, "Língua Portuguesa", "Produção Textual › Comunicação escrita", 'D',
                """<p>Sobre o emprego e a concordância dos pronomes de tratamento na redação oficial, assinale a alternativa correta.</p>""",
                """Sobre o emprego e a concordância dos pronomes de tratamento na redação oficial, assinale a alternativa correta.""",
                """SOBRE O EMPREGO E A CONCORDANCIA DOS PRONOMES DE TRATAMENTO NA REDACAO OFICIAL, ASSINALE A ALTERNATIVA CORRETA.""",
                [
                    "Vossa Excelência estais convidado para a solenidade de posse.",
                    "Vossa Senhoria deveis encaminhar vosso relatório até amanhã.",
                    "Vossa Senhoria sois responsável por vossos atos administrativos.",
                    "Vossa Excelência deverá encaminhar seu parecer ao setor competente.",
                ]),
        ];
    }
}
