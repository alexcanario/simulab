using System.Globalization;
using System.Resources;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-104 AC17: every new text and the two error code texts exist in en, pt-BR and pt-PT with the words the item
/// gives. ResourceParityTests checks that no key is missing in a language; this pins what each one says.
/// </summary>
public sealed class MultipleAnswerQuestionResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string, string, string> Texts() => new()
    {
        { "Questions.Type.MultipleAnswer", "Multiple answer", "Múltiplas respostas", "Respostas múltiplas" },
        {
            "Questions.Type.MultipleAnswer.Description",
            "One statement with three to five options; two or more are correct.",
            "Um enunciado com três a cinco alternativas; duas ou mais são corretas.",
            "Um enunciado com três a cinco opções; duas ou mais estão corretas."
        },
        {
            "Questions.Section.Options.Multiple.Subtitle",
            "3 to 5 options to publish. Mark every correct one: at least two, and at least one wrong.",
            "De 3 a 5 alternativas para publicar. Marque todas as corretas: ao menos duas, e ao menos uma errada.",
            "De 3 a 5 opções para publicar. Assinale todas as corretas: pelo menos duas, e pelo menos uma errada."
        },
        { "Questions.Checklist.OptionsCount.Multiple", "3 to 5 options", "De 3 a 5 alternativas", "De 3 a 5 opções" },
        {
            "Questions.Checklist.Key.Multiple",
            "At least two correct and one wrong",
            "Ao menos duas corretas e uma errada",
            "Pelo menos duas corretas e uma errada"
        },
        { "Questions.Preview.SelectCount", "Mark {0} options.", "Marque {0} alternativas.", "Assinale {0} opções." },
        {
            QuestionBankErrorCodes.QuestionMultipleOptionsCount,
            "A multiple-answer question is published with 3 to 5 options.",
            "Uma questão de múltiplas respostas é publicada com 3 a 5 alternativas.",
            "Uma questão de respostas múltiplas é publicada com 3 a 5 opções."
        },
        {
            QuestionBankErrorCodes.QuestionMultipleKeyInvalid,
            "Mark at least two options as correct, and leave at least one wrong.",
            "Marque ao menos duas alternativas como corretas e deixe ao menos uma errada.",
            "Assinale pelo menos duas opções como corretas e deixe pelo menos uma errada."
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
