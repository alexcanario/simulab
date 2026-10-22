using System.Globalization;
using System.Resources;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-107 AC20: the new texts and the nine error code texts exist in en, pt-BR and pt-PT with the words the item gives.
/// ResourceParityTests checks that no key is missing in a language; this pins what each one says.
/// </summary>
public sealed class FillInTheBlanksQuestionResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string, string, string> Texts() => new()
    {
        { "Questions.Type.FillInTheBlanks", "Fill in the blanks", "Lacunas", "Lacunas" },
        {
            "Questions.Type.FillInTheBlanks.Description",
            "Blanks in the statement; the student types each answer.",
            "Lacunas no enunciado; o aluno digita cada resposta.",
            "Lacunas no enunciado; o aluno escreve cada resposta."
        },
        { "RichText.Blank", "Insert blank", "Inserir lacuna", "Inserir lacuna" },
        { "Questions.Section.Blanks", "Blanks", "Lacunas", "Lacunas" },
        {
            "Questions.Section.Blanks.Subtitle",
            "Insert a blank in the statement, then write the answers it accepts. Case and extra spaces are ignored; accents count.",
            "Insira uma lacuna no enunciado e escreva as respostas que ela aceita. Maiúsculas e espaços extras são ignorados; acentos contam.",
            "Insira uma lacuna no enunciado e escreva as respostas que ela aceita. Maiúsculas e espaços a mais são ignorados; os acentos contam."
        },
        {
            "Questions.Blanks.Empty",
            "No blank yet. Use Insert blank in the statement.",
            "Nenhuma lacuna ainda. Use Inserir lacuna no enunciado.",
            "Ainda não há lacunas. Use Inserir lacuna no enunciado."
        },
        { "Questions.Blanks.Blank", "Blank {0}", "Lacuna {0}", "Lacuna {0}" },
        { "Questions.Blanks.Answer.Field", "Accepted answer {0} of blank {1}", "Resposta aceita {0} da lacuna {1}", "Resposta aceite {0} da lacuna {1}" },
        { "Questions.Blanks.Answer.Name", "answer {0} of blank {1}", "resposta {0} da lacuna {1}", "resposta {0} da lacuna {1}" },
        { "Questions.Blanks.AddAnswer", "Add answer", "Adicionar resposta", "Adicionar resposta" },
        { "Questions.Blanks.AnswersCount", "{0} of 5 answers", "{0} de 5 respostas", "{0} de 5 respostas" },
        { "Questions.Checklist.Blanks.Count", "1 to 10 blanks in the statement", "De 1 a 10 lacunas no enunciado", "De 1 a 10 lacunas no enunciado" },
        {
            "Questions.Checklist.Blanks.Answers",
            "Every blank has an accepted answer",
            "Toda lacuna tem uma resposta aceita",
            "Cada lacuna tem uma resposta aceite"
        },
        { "Questions.Preview.BlanksInstruction", "Fill in each blank.", "Preencha cada lacuna.", "Preencha cada lacuna." },
        { "Questions.Preview.BlankKey", "Blank {0}: {1}", "Lacuna {0}: {1}", "Lacuna {0}: {1}" },
        { "Questions.Preview.BlankNoKey", "Blank {0}: no key yet", "Lacuna {0}: ainda sem gabarito", "Lacuna {0}: ainda sem solução" },
        {
            QuestionBankErrorCodes.QuestionBlanksCount,
            "A fill-in-the-blanks question has 1 to 10 blanks in the statement.",
            "Uma questão de lacunas tem de 1 a 10 lacunas no enunciado.",
            "Uma questão de lacunas tem de 1 a 10 lacunas no enunciado."
        },
        {
            QuestionBankErrorCodes.QuestionBlankRepeated,
            "Each blank number appears only once in the statement.",
            "Cada número de lacuna aparece uma só vez no enunciado.",
            "Cada número de lacuna aparece uma única vez no enunciado."
        },
        {
            QuestionBankErrorCodes.QuestionBlankMarkerMissing,
            "A blank has no marker in the statement.",
            "Uma lacuna não tem marcador no enunciado.",
            "Uma lacuna não tem marcador no enunciado."
        },
        {
            QuestionBankErrorCodes.QuestionBlankAnswersRequired,
            "Every blank needs an accepted answer.",
            "Toda lacuna precisa de uma resposta aceita.",
            "Cada lacuna precisa de uma resposta aceite."
        },
        {
            QuestionBankErrorCodes.QuestionBlankAnswersCount,
            "A blank accepts at most 5 answers.",
            "Uma lacuna aceita no máximo 5 respostas.",
            "Uma lacuna aceita no máximo 5 respostas."
        },
        {
            QuestionBankErrorCodes.QuestionBlankAnswerRequired,
            "Write the answer or remove it.",
            "Escreva a resposta ou remova-a.",
            "Escreva a resposta ou remova-a."
        },
        {
            QuestionBankErrorCodes.QuestionBlankAnswerTooLong,
            "An answer has more than 100 characters.",
            "Uma resposta passa de 100 caracteres.",
            "Uma resposta excede 100 caracteres."
        },
        {
            QuestionBankErrorCodes.QuestionBlankAnswerRepeated,
            "Two answers of this blank are the same once case and extra spaces are ignored.",
            "Duas respostas desta lacuna são iguais quando se ignoram maiúsculas e espaços extras.",
            "Duas respostas desta lacuna são iguais quando se ignoram maiúsculas e espaços a mais."
        },
        {
            QuestionBankErrorCodes.QuestionBlanksNotAllowed,
            "Only a fill-in-the-blanks question has blanks.",
            "Só uma questão de lacunas tem lacunas.",
            "Só uma questão de lacunas tem lacunas."
        }
    };

    [Theory]
    [MemberData(nameof(Texts))]
    public void EveryNewText_ExistsInTheThreeLanguagesWithTheItemsWords(string key, string en, string ptBr, string ptPt)
    {
        Manager.GetString(key, new CultureInfo("en")).Should().Be(en, $"'{key}' in en");
        Manager.GetString(key, new CultureInfo("pt-BR")).Should().Be(ptBr, $"'{key}' in pt-BR");
        Manager.GetString(key, new CultureInfo("pt-PT")).Should().Be(ptPt, $"'{key}' in pt-PT");
    }
}
