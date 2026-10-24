using System.Globalization;
using System.Resources;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-108 AC20: the new texts, including the short-answer texts of the reused error codes, exist in en, pt-BR and pt-PT
/// with the words the item gives. ResourceParityTests checks that no key is missing in a language; this pins what each
/// one says.
/// </summary>
public sealed class ShortAnswerQuestionResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string, string, string> Texts() => new()
    {
        { "Questions.Type.ShortAnswer", "Short answer", "Resposta curta", "Resposta curta" },
        {
            "Questions.Type.ShortAnswer.Description",
            "A short typed answer, checked against accepted answers.",
            "Uma resposta curta digitada, conferida com as respostas aceitas.",
            "Uma resposta curta escrita, conferida com as respostas aceites."
        },
        { "Questions.Section.ShortAnswer", "Accepted answers", "Respostas aceitas", "Respostas aceites" },
        {
            "Questions.Section.ShortAnswer.Subtitle",
            "Write the answers the student may type. Case and extra spaces are ignored; accents count.",
            "Escreva as respostas que o aluno pode digitar. Maiúsculas e espaços extras são ignorados; acentos contam.",
            "Escreva as respostas que o aluno pode escrever. Maiúsculas e espaços a mais são ignorados; os acentos contam."
        },
        { "Questions.ShortAnswer.Answer.Field", "Accepted answer {0}", "Resposta aceita {0}", "Resposta aceite {0}" },
        { "Questions.ShortAnswer.Answer.Name", "answer {0}", "resposta {0}", "resposta {0}" },
        {
            "Questions.ShortAnswer.Answer.Placeholder",
            "An answer the student may type",
            "Uma resposta que o aluno pode digitar",
            "Uma resposta que o aluno pode escrever"
        },
        { "Questions.ShortAnswer.Answer.Removed", "Answer removed. {0} left.", "Resposta removida. Restam {0}.", "Resposta removida. Restam {0}." },
        {
            "Questions.Checklist.ShortAnswer.Answers",
            "At least one accepted answer",
            "Pelo menos uma resposta aceita",
            "Pelo menos uma resposta aceite"
        },
        { "Questions.Preview.ShortAnswerLabel", "Your answer", "Sua resposta", "A sua resposta" },
        { "Questions.Preview.ShortAnswerInstruction", "Type your answer.", "Digite sua resposta.", "Escreva a sua resposta." },
        { "Questions.Preview.ShortAnswerKey", "Accepted answers: {0}", "Respostas aceitas: {0}", "Respostas aceites: {0}" },
        {
            "Questions.Preview.ShortAnswerNoKey",
            "Accepted answers: no key yet",
            "Respostas aceitas: ainda sem gabarito",
            "Respostas aceites: ainda sem solução"
        },
        {
            "Questions.ShortAnswer.Error.AnswersRequired",
            "Write at least one accepted answer.",
            "Escreva pelo menos uma resposta aceita.",
            "Escreva pelo menos uma resposta aceite."
        },
        {
            "Questions.ShortAnswer.Error.AnswersCount",
            "A short answer accepts at most 5 answers.",
            "Uma resposta curta aceita no máximo 5 respostas.",
            "Uma resposta curta aceita no máximo 5 respostas."
        },
        {
            "Questions.ShortAnswer.Error.AnswerRepeated",
            "Two answers are the same once case and extra spaces are ignored.",
            "Duas respostas são iguais quando se ignoram maiúsculas e espaços extras.",
            "Duas respostas são iguais quando se ignoram maiúsculas e espaços a mais."
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
