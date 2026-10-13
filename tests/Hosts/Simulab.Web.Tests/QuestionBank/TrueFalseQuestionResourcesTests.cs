using System.Globalization;
using System.Resources;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-97 AC15: every new text and the four error code texts exist in en, pt-BR and pt-PT with the words the item
/// gives. ResourceParityTests checks that no key is missing in a language; this pins what each one says.
/// </summary>
public sealed class TrueFalseQuestionResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string, string, string> Texts() => new()
    {
        { "Questions.Field.Type", "Question type", "Tipo de questão", "Tipo de questão" },
        { "Questions.Type.SingleChoice", "Single choice", "Múltipla escolha", "Escolha múltipla" },
        {
            "Questions.Type.SingleChoice.Description",
            "One statement with two to five options; exactly one is correct.",
            "Um enunciado com duas a cinco alternativas; só uma é correta.",
            "Um enunciado com duas a cinco opções; só uma está correta."
        },
        { "Questions.Type.TrueFalse", "True/false", "Certo/errado", "Certo/errado" },
        {
            "Questions.Type.TrueFalse.Description",
            "One statement the student judges as true or false.",
            "Um enunciado que o aluno julga como certo ou errado.",
            "Um enunciado que o aluno julga como certo ou errado."
        },
        {
            "Questions.Type.ReadOnlyHint",
            "The type cannot be changed after the question is created.",
            "O tipo não pode ser alterado depois que a questão é criada.",
            "O tipo não pode ser alterado depois de a questão ser criada."
        },
        { "Questions.Section.TrueFalseKey", "Answer key", "Gabarito", "Gabarito" },
        {
            "Questions.Section.TrueFalseKey.Subtitle",
            "Mark the statement as true or false. Needed only to publish.",
            "Marque o enunciado como certo ou errado. Só é exigido para publicar.",
            "Marque o enunciado como certo ou errado. Só é exigido para publicar."
        },
        { "Questions.TrueFalseKey.Label", "Correct answer", "Resposta correta", "Resposta correta" },
        {
            "Questions.TrueFalseKey.Hint",
            "The penalty for a wrong answer comes from the scoring rules of the edition, never from the question.",
            "A penalidade por resposta errada vem das regras de pontuação da edição, nunca da questão.",
            "A penalidade por resposta errada vem das regras de pontuação da edição, nunca da questão."
        },
        { "Questions.TrueFalse.True", "True", "Certo", "Certo" },
        { "Questions.TrueFalse.False", "False", "Errado", "Errado" },
        { "Questions.Preview.Answers", "Answers", "Respostas", "Respostas" },
        {
            "Questions.Preview.NoKey.TrueFalse",
            "This draft has no answer key yet.",
            "Este rascunho ainda não tem gabarito.",
            "Este rascunho ainda não tem gabarito."
        },
        { "Questions.Checklist.TrueFalseKey", "Answer key set", "Gabarito definido", "Gabarito definido" },
        { "Questions.Form.Aside.Type", "Type", "Tipo", "Tipo" },
        { "Questions.Column.Type", "Type", "Tipo", "Tipo" },
        {
            QuestionBankErrorCodes.QuestionTypeChanged,
            "The type of a question cannot be changed.",
            "O tipo de uma questão não pode ser alterado.",
            "O tipo de uma questão não pode ser alterado."
        },
        {
            QuestionBankErrorCodes.QuestionOptionsNotAllowed,
            "A true/false question has no options.",
            "Uma questão certo/errado não tem alternativas.",
            "Uma questão certo/errado não tem opções."
        },
        {
            QuestionBankErrorCodes.QuestionKeyNotAllowed,
            "A single-choice question has no true/false key.",
            "Uma questão de múltipla escolha não tem gabarito certo/errado.",
            "Uma questão de escolha múltipla não tem gabarito certo/errado."
        },
        {
            QuestionBankErrorCodes.QuestionTrueFalseKeyRequired,
            "Set the answer key (true or false) to publish.",
            "Defina o gabarito (certo ou errado) para publicar.",
            "Defina o gabarito (certo ou errado) para publicar."
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
