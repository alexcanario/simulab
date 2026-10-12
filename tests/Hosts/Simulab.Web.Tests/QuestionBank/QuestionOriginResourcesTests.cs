using System.Globalization;
using System.Resources;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-96 AC18: every text and every error code of the origin exists in en, pt-BR and pt-PT with the words the item
/// gives. ResourceParityTests checks that no key is missing in a language; this pins what each one says.
/// </summary>
public sealed class QuestionOriginResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string, string, string> Texts() => new()
    {
        { "Questions.Section.Origin", "Origin", "Origem", "Origem" },
        { "Questions.Field.Edition", "Edition", "Edição", "Edição" },
        { "Questions.Field.PaperNumber", "Number in the paper", "Número na prova", "Número na prova" },
        { "Questions.Field.NoticeSubject", "Notice subject", "Disciplina do edital", "Disciplina do edital" },
        { "Questions.Form.Aside.Source", "Source", "Fonte", "Fonte" },
        { "Questions.Source.Authored", "Authored", "Autoral", "Autoral" },
        { "Questions.Source.Number", "Question {0}", "Questão {0}", "Questão {0}" },
        { "Questions.Preview.Number", "Question {0}", "Questão {0}", "Questão {0}" },
        { "Questions.Column.Source", "Source", "Fonte", "Fonte" },
        { "EditionQuestions.Section.Title", "Questions", "Questões", "Questões" },
        {
            "EditionQuestions.Empty",
            "No questions from this edition yet.",
            "Nenhuma questão desta edição ainda.",
            "Ainda não há questões desta edição."
        },
        { "EditionQuestions.Add", "Add question", "Adicionar questão", "Adicionar questão" },
        {
            "question.paper_number_taken",
            "Number already used in this edition.",
            "Número já usado nesta edição.",
            "Número já usado nesta edição."
        },
        {
            "question.exam_edition_not_found",
            "The edition no longer exists. Pick another.",
            "A edição não existe mais. Escolha outra.",
            "A edição já não existe. Escolha outra."
        },
        {
            "question.notice_subject_not_found",
            "The notice subject does not belong to this edition, or no longer exists.",
            "A disciplina do edital não pertence a esta edição ou não existe mais.",
            "A disciplina do edital não pertence a esta edição ou já não existe."
        },
        {
            "question.paper_number_required",
            "Enter the number of the question in the paper.",
            "Informe o número da questão na prova.",
            "Indique o número da questão na prova."
        },
        {
            "question.paper_number_invalid",
            "The number is a whole number from 1 to 500.",
            "O número é um inteiro de 1 a 500.",
            "O número é um inteiro de 1 a 500."
        },
        {
            "question.origin_requires_edition",
            "Choose an edition to give a number or a notice subject.",
            "Escolha uma edição para informar um número ou uma disciplina do edital.",
            "Escolha uma edição para indicar um número ou uma disciplina do edital."
        },
        {
            "exam_edition.has_questions",
            "Questions of the bank came from this edition, so it cannot be deleted.",
            "Há questões do banco que vieram desta edição, então ela não pode ser excluída.",
            "Há questões do banco que vieram desta edição, por isso não pode ser eliminada."
        },
        {
            "notice_subject.has_questions",
            "Questions of the bank belong to this notice subject, so it cannot be deleted.",
            "Há questões do banco ligadas a esta disciplina do edital, então ela não pode ser excluída.",
            "Há questões do banco ligadas a esta disciplina do edital, por isso não pode ser eliminada."
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
