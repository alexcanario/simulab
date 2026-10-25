using System.Globalization;
using System.Resources;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-109 AC21: the new texts, including the texts of the ten new error codes, exist in en, pt-BR and pt-PT with the
/// words the item gives. ResourceParityTests checks that no key is missing in a language; this pins what each one says.
/// </summary>
public sealed class RubricQuestionResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string, string, string> Texts() => new()
    {
        { "Questions.Type.OpenAnswer", "Open answer", "Resposta aberta", "Resposta aberta" },
        {
            "Questions.Type.OpenAnswer.Description",
            "A written answer graded by a person against a rubric.",
            "Uma resposta escrita, corrigida por uma pessoa com base em uma rubrica.",
            "Uma resposta escrita, classificada por uma pessoa com base numa rubrica."
        },
        { "Questions.Type.Essay", "Essay", "Redação", "Redação" },
        {
            "Questions.Type.Essay.Description",
            "A long written text graded by a person against a rubric.",
            "Um texto longo, corrigido por uma pessoa com base em uma rubrica.",
            "Um texto longo, classificado por uma pessoa com base numa rubrica."
        },
        { "Questions.Section.Rubric", "Rubric", "Rubrica", "Rubrica" },
        {
            "Questions.Section.Rubric.Subtitle",
            "Write the criteria a grader scores. The question's grade is the points given divided by the total.",
            "Escreva os critérios que o corretor avalia. A nota da questão é a soma dos pontos dados dividida pelo total.",
            "Escreva os critérios que o avaliador classifica. A nota da questão é a soma dos pontos atribuídos a dividir pelo total."
        },
        { "Questions.Rubric.Criterion.Name", "Criterion {0} name", "Nome do critério {0}", "Nome do critério {0}" },
        { "Questions.Rubric.Criterion.Description", "Criterion {0} description", "Descrição do critério {0}", "Descrição do critério {0}" },
        { "Questions.Rubric.Criterion.Points", "Criterion {0} points", "Pontos do critério {0}", "Pontos do critério {0}" },
        { "Questions.Rubric.Criterion.ItemName", "criterion {0}", "critério {0}", "critério {0}" },
        { "Questions.Rubric.Criterion.Removed", "Criterion removed. {0} left.", "Critério removido. Restam {0}.", "Critério removido. Restam {0}." },
        { "Questions.Rubric.Add", "Add criterion", "Adicionar critério", "Adicionar critério" },
        { "Questions.Rubric.Count", "{0} of 10 criteria", "{0} de 10 critérios", "{0} de 10 critérios" },
        { "Questions.Rubric.Total", "Total: {0} points", "Total: {0} pontos", "Total: {0} pontos" },
        { "Questions.Section.AnswerSize", "Answer size (optional)", "Tamanho da resposta (opcional)", "Tamanho da resposta (opcional)" },
        { "Questions.AnswerSize.MinLines", "Minimum lines", "Mínimo de linhas", "Mínimo de linhas" },
        { "Questions.AnswerSize.MaxLines", "Maximum lines", "Máximo de linhas", "Máximo de linhas" },
        { "Questions.AnswerSize.MaxCharacters", "Maximum characters", "Máximo de caracteres", "Máximo de caracteres" },
        { "Questions.Explanation.ModelAnswer", "Model answer", "Resposta modelo", "Resposta modelo" },
        {
            "Questions.Explanation.ModelAnswer.Hint",
            "The grader always sees it; a student sees it after answering.",
            "O corretor sempre a vê; o aluno a vê depois de responder.",
            "O avaliador vê-a sempre; o aluno vê-a depois de responder."
        },
        {
            "Questions.Checklist.Rubric.Criteria",
            "At least one rubric criterion",
            "Pelo menos um critério da rubrica",
            "Pelo menos um critério da rubrica"
        },
        { "Questions.Preview.OpenAnswerLabel", "Your answer", "Sua resposta", "A sua resposta" },
        { "Questions.Preview.EssayLabel", "Your essay", "Sua redação", "A sua redação" },
        { "Questions.Preview.OpenAnswerInstruction", "Write your answer.", "Escreva sua resposta.", "Escreva a sua resposta." },
        { "Questions.Preview.EssayInstruction", "Write your essay.", "Escreva sua redação.", "Escreva a sua redação." },
        { "Questions.Preview.LimitCharacters", "Up to {0} characters.", "Até {0} caracteres.", "Até {0} caracteres." },
        { "Questions.Preview.LimitLinesBetween", "Between {0} and {1} lines.", "Entre {0} e {1} linhas.", "Entre {0} e {1} linhas." },
        { "Questions.Preview.LimitLinesAtLeast", "At least {0} lines.", "Pelo menos {0} linhas.", "Pelo menos {0} linhas." },
        { "Questions.Preview.LimitLinesUpTo", "Up to {0} lines.", "Até {0} linhas.", "Até {0} linhas." },
        { "Questions.Preview.RubricTitle", "Grading rubric", "Rubrica de correção", "Rubrica de classificação" },
        { "Questions.Preview.RubricTotal", "Total", "Total", "Total" },
        { "Questions.Preview.RubricNone", "no rubric yet", "ainda sem rubrica", "ainda sem rubrica" },
        { "question.rubric_criteria_count", "A rubric has at most 10 criteria.", "Uma rubrica tem no máximo 10 critérios.", "Uma rubrica tem no máximo 10 critérios." },
        {
            "question.rubric_criteria_required",
            "Write at least one criterion to publish.",
            "Escreva pelo menos um critério para publicar.",
            "Escreva pelo menos um critério para publicar."
        },
        { "question.criterion_name_required", "Write a name for the criterion.", "Escreva um nome para o critério.", "Escreva um nome para o critério." },
        {
            "question.criterion_name_too_long",
            "A criterion name has at most 100 characters.",
            "O nome de um critério tem no máximo 100 caracteres.",
            "O nome de um critério tem no máximo 100 caracteres."
        },
        {
            "question.criterion_description_too_long",
            "A criterion description has at most 500 characters.",
            "A descrição de um critério tem no máximo 500 caracteres.",
            "A descrição de um critério tem no máximo 500 caracteres."
        },
        {
            "question.criterion_points_out_of_range",
            "The points of a criterion are a whole number from 1 to 100.",
            "Os pontos de um critério são um número inteiro de 1 a 100.",
            "Os pontos de um critério são um número inteiro de 1 a 100."
        },
        { "question.criterion_name_repeated", "Two criteria have the same name.", "Dois critérios têm o mesmo nome.", "Dois critérios têm o mesmo nome." },
        {
            "question.rubric_not_allowed",
            "Only an open-answer or essay question has a rubric.",
            "Só uma questão de resposta aberta ou uma redação tem rubrica.",
            "Só uma questão de resposta aberta ou uma redação tem rubrica."
        },
        {
            "question.answer_size_invalid",
            "The size limit is out of range, or the minimum is above the maximum.",
            "O limite de tamanho está fora da faixa, ou o mínimo está acima do máximo.",
            "O limite de tamanho está fora do intervalo, ou o mínimo é superior ao máximo."
        },
        {
            "question.answer_size_not_allowed",
            "Only an open-answer or essay question has a size limit.",
            "Só uma questão de resposta aberta ou uma redação tem limite de tamanho.",
            "Só uma questão de resposta aberta ou uma redação tem limite de tamanho."
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
