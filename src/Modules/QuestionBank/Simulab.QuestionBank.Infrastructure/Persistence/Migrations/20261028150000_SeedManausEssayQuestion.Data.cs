#nullable disable

namespace Simulab.QuestionBank.Infrastructure.Persistence.Migrations
{
    // F-163: the frozen literals of the essay seed, copied from pages 23 to 25 of the paper (booklet type 1).
    public partial class SeedManausEssayQuestion
    {
        private const string BaseTextTitle = "Prova discursiva: textos motivadores";
        private const string BaseTextNormalizedTitle = "PROVA DISCURSIVA: TEXTOS MOTIVADORES";

        private const string StatementHtml =
            """<p>Com base nos textos motivadores, redija uma redação sobre o tema:</p><p><strong>“A ressignificação da Guarda Municipal: da vigilância patrimonial à mediação social como estratégia de segurança urbana”.</strong></p>""";

        private const string StatementText =
            "Com base nos textos motivadores, redija uma redação sobre o tema: “A ressignificação da Guarda Municipal: da vigilância patrimonial à mediação social como estratégia de segurança urbana”.";

        private const string NormalizedStatement =
            "COM BASE NOS TEXTOS MOTIVADORES, REDIJA UMA REDACAO SOBRE O TEMA: “A RESSIGNIFICACAO DA GUARDA MUNICIPAL: DA VIGILANCIA PATRIMONIAL A MEDIACAO SOCIAL COMO ESTRATEGIA DE SEGURANCA URBANA”.";

        // The six criteria of the exam board's grid, in order: three that add points, then three whose maximum is the
        // points kept when no error is made, their description stating the deduction per error (BR3).
        private static readonly Criterion[] Rubric =
        [
            new(
                "Conhecimento e compreensão do conteúdo proposto",
                "Relevância e propriedade de resposta à temática e ao tipo de gênero textual solicitado.",
                4),
            new(
                "Desenvolvimento da argumentação, objetividade e informatividade",
                "Organização da argumentação dentro do tema proposto, considerando a macroestrutura do texto dissertativo: introdução, desenvolvimento e conclusão.",
                4),
            new(
                "Coerência",
                "Relação lógica entre as ideias e sua distribuição entre as partes do texto; encadeamento lógico e coerente das ideias: progressão textual.",
                3),
            new(
                "Estruturação sintática",
                "2 pontos, menos 0,50 por erro, sem ficar abaixo de 0. Truncamentos de períodos; justaposição de orações e/ou períodos; excesso, ausência ou duplicação de elementos sintáticos; uso dos recursos coesivos que tornam clara a relação entre frases, períodos e parágrafos.",
                2),
            new(
                "Morfossintaxe",
                "3 pontos, menos 0,25 por erro, sem ficar abaixo de 0. Colocação pronominal; concordância verbal e nominal; conectores; emprego de pronomes; paralelismo sintático; regência verbal e nominal; seleção vocabular; tempos e modos verbais; grau de formalidade.",
                3),
            new(
                "Desvios",
                "4 pontos, menos 0,25 por erro, sem ficar abaixo de 0. Acentuação; ortografia; translineação; uso de letras maiúsculas e minúsculas; pontuação.",
                4),
        ];

        private const string BaseTextHtml =
            """<p><strong>Texto I</strong></p><p><strong>O perfil profissional do Guarda Municipal</strong></p><p>Pode-se elencar o perfil e as habilidades do profissional da Guarda Municipal que se pretende formar:</p><ul><li>Atuar de forma a garantir a ocupação e a utilização democrática do espaço público;</li><li>Garantir o respeito dos direitos fundamentais do cidadão na vida cotidiana;</li><li>Proteger o meio ambiente e o patrimônio ecológico;</li><li>Detectar todo tipo de deficiências e panes que impedem a livre circulação do cidadão e a correta utilização dos serviços públicos urbanos; e</li><li>Ser reconhecido pela população com o objetivo de prevenir e mediar pequenos conflitos, assim como participar do acesso dos cidadãos aos seus direitos, sendo capaz de orientá-los quando necessário.</li></ul><p><em>(PEREIRA, M. Matriz Curricular Nacional para Formação das Guardas Municipais. Revisada, contendo formato dos cursos, concepção, metodologia, público-alvo, carga horária, modelo avaliativo, referência bibliográficas, sugestão de corpo docente, ementas entre outros. Brasília: UNDP, 2013. Fragmento.)</em></p><p><strong>Texto II</strong></p><p><strong>Lei nº 13.675, de 11 de junho de 2018 (atualizada pela Lei nº 14.751, de 12 de dezembro de 2023)</strong></p><p>Art. 4º - São princípios da PNSPDS (Política Nacional de Segurança Pública e Defesa Social):</p><p><strong>I.</strong> Respeito ao ordenamento jurídico e aos direitos e garantias individuais e coletivos;</p><p><strong>II.</strong> Proteção, valorização e reconhecimento dos profissionais de segurança pública;</p><p><strong>III.</strong> Proteção dos direitos humanos, respeito aos direitos fundamentais e promoção da cidadania e da dignidade da pessoa humana;</p><p><strong>IV.</strong> Eficiência na prevenção e no controle das infrações penais;</p><p><strong>V.</strong> Eficiência na repressão e na apuração das infrações penais;</p><p><strong>VI.</strong> Eficiência na prevenção e na redução de riscos em situações de emergência e desastres que afetam a vida, o patrimônio e o meio ambiente;</p><p><strong>VII.</strong> Participação e controle social;</p><p><strong>VIII.</strong> Resolução pacífica de conflitos;</p><p><strong>IX.</strong> Uso comedido e proporcional da força pelos agentes da segurança pública, pautado nos documentos internacionais de proteção aos direitos humanos de que o Brasil seja signatário;</p><p><strong>X.</strong> Proteção da vida, do patrimônio e do meio ambiente;</p><p><strong>XI.</strong> Publicidade das informações não sigilosas;</p><p><strong>XII.</strong> Promoção da produção de conhecimento sobre segurança pública;</p><p><strong>XIII.</strong> Otimização dos recursos materiais, humanos e financeiros das instituições;</p><p><strong>XIV.</strong> Simplicidade, informalidade, economia procedimental e celeridade no serviço prestado à sociedade;</p><p><strong>XV.</strong> Relação harmônica e colaborativa entre os Poderes; e</p><p><strong>XVI.</strong> Transparência, responsabilização e prestação de contas.</p><p><em>(BRASIL. Lei nº 13.675, de 11 de junho de 2018. Institui o Sistema Único de Segurança Pública (Susp). Brasília: Presidência da República, 2018/2023.)</em></p><p><strong>Texto III</strong></p><p><strong>Guarda Municipal: segurança pública e o direito coletivo ao bem-estar</strong></p><p>A segurança pública, muitas vezes tratada como um problema a ser resolvido com policiamento e prisões, deve ser entendida como um direito coletivo que envolve educação, prevenção e o acesso igualitário aos direitos fundamentais. Nesse cenário, a Guarda Municipal (GCM) emerge como uma peça central na construção de uma sociedade mais segura e justa, desempenhando um papel estratégico ao estar diretamente conectada com a população e seus desafios cotidianos.</p><p>As Guardas Municipais têm uma função vital na mediação de conflitos locais e na proteção da população. Suas atribuições vão além da proteção dos bens, serviços e instalações municipais, estendendo-se para ações preventivas e educativas em segurança. Um exemplo claro dessa atuação é o trabalho das Patrulhas Maria da Penha, que monitoram mulheres sob medida protetiva, garantindo sua segurança e evitando a escalada da violência doméstica.</p><p>Com uma atuação de caráter comunitário, as GCMs são especialmente eficazes em situações em que a mediação é necessária, como conflitos entre vizinhos, fiscalização de áreas públicas e prevenção de delitos de menor potencial ofensivo. Essas ações não apenas protegem a população, mas também aliviam a sobrecarga das Polícias Militar e Civil, permitindo que elas concentrem esforços em ocorrências mais graves.</p><p>É comum pensarmos que a segurança pública depende exclusivamente de ações repressivas, como prisões e policiamento ostensivo. No entanto, a verdadeira segurança começa com a prevenção. A primeira etapa para construir uma sociedade mais segura é evitar que crimes aconteçam. As Guardas Municipais, posicionadas nas comunidades, têm o potencial de intervir antes que pequenos conflitos se transformem em grandes problemas, tornando-se uma força essencial de prevenção.</p><p>Esse papel preventivo reforça a necessidade de uma guarda verdadeiramente popular, ou seja, uma instituição que atue de forma integrada com a sociedade, compreendendo as necessidades locais e respondendo com soluções que priorizem o bem-estar e a segurança coletiva. Além disso, valorizar o treinamento contínuo e capacitar os guardas para lidar com situações de mediação e diálogo é fundamental para uma atuação preventiva eficaz.</p><p><em>(GOMES, N. Guarda Municipal: segurança pública e o direito coletivo ao bem-estar. Brasília: CONFETAM, 2024. Adaptado.)</em></p>""";
    }
}
