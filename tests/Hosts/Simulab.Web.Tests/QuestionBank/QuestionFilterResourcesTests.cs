using System.Globalization;
using System.Resources;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-100 AC14: every text of the list's filters exists in en, pt-BR and pt-PT. ResourceParityTests checks that
/// no key is missing in a language; this pins what each one says.
/// </summary>
public sealed class QuestionFilterResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string, string, string> Texts() => new()
    {
        { "Questions.Filter.Type", "Type", "Tipo", "Tipo" },
        { "Questions.Filter.Difficulty", "Difficulty", "Dificuldade", "Dificuldade" },
        { "Questions.Filter.Subject", "Subject and topic", "Disciplina e tópico", "Disciplina e tópico" },
        { "Questions.Filter.Organizer", "Organizer", "Banca", "Banca" },
        { "Questions.Filter.Edition", "Edition", "Edição", "Edição" },
        { "Questions.Filter.Status.Placeholder", "Status name", "Nome da situação", "Nome do estado" },
        { "Questions.Filter.Type.Placeholder", "Type name", "Nome do tipo", "Nome do tipo" },
        { "Questions.Filter.Difficulty.Placeholder", "A number from 1 to 5", "Um número de 1 a 5", "Um número de 1 a 5" },
        { "Questions.Filter.Subject.Placeholder", "Subject or topic name", "Nome da disciplina ou do tópico", "Nome da disciplina ou do tópico" },
        { "Questions.Filter.Organizer.Placeholder", "Organizer name", "Nome da banca", "Nome da banca" },
        { "Questions.Filter.Edition.Placeholder", "Exam, organizer or year", "Concurso, banca ou ano", "Concurso, banca ou ano" },
        { "Questions.Filter.Options", "{0}: options", "{0}: opções", "{0}: opções" },
        { "Questions.Filter.Picked", "{0}: picked", "{0}: escolhidos", "{0}: escolhidos" },
        { "Questions.Filter.Count", "Picked: {0}", "Escolhidos: {0}", "Escolhidos: {0}" },
        { "Questions.Filter.None", "No filter", "Sem filtro", "Sem filtro" },
        { "Questions.Filter.ClearField", "Clear", "Limpar", "Limpar" },
        { "Questions.Filter.Clear", "Clear all", "Limpar tudo", "Limpar tudo" },
        { "Questions.Filter.SubjectAll", "{0} (all topics)", "{0} (todos os tópicos)", "{0} (todos os tópicos)" },
        { "Questions.Filter.Loading", "Loading options…", "Carregando opções…", "A carregar opções…" },
        { "Questions.Filter.LoadFailed", "The list of options did not load.", "A lista de opções não carregou.", "A lista de opções não carregou." },
        { "Questions.Filter.Limit", "Up to 100 subjects, topics and organizers can be picked together.", "Dá para escolher até 100 disciplinas, tópicos e bancas ao todo.", "É possível escolher até 100 disciplinas, tópicos e bancas no total." },
        { "Questions.Empty.Filters", "No question matches the chosen filters.", "Nenhuma questão corresponde aos filtros escolhidos.", "Nenhuma questão corresponde aos filtros escolhidos." }
    };

    [Theory]
    [MemberData(nameof(Texts))]
    public void EveryFilterText_ExistsInTheThreeLanguagesWithTheItemsWords(string key, string en, string ptBr, string ptPt)
    {
        Manager.GetString(key, new CultureInfo("en")).Should().Be(en, $"'{key}' in en");
        Manager.GetString(key, new CultureInfo("pt-BR")).Should().Be(ptBr, $"'{key}' in pt-BR");
        Manager.GetString(key, new CultureInfo("pt-PT")).Should().Be(ptPt, $"'{key}' in pt-PT");
    }
}
