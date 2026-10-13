using System.Globalization;
using System.Resources;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-98 AC17: every text and every error code of the history and of the reason exists in en, pt-BR and pt-PT.
/// ResourceParityTests checks that no key is missing in a language; this pins what each one says.
/// </summary>
public sealed class QuestionVersionResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string, string, string> Texts() => new()
    {
        { "Questions.Section.History", "History", "Histórico", "Histórico" },
        { "Questions.History.Empty", "The first version is recorded when the question is published.", "A primeira versão é registrada quando a questão é publicada.", "A primeira versão é registada quando a questão é publicada." },
        { "Questions.History.Column.Version", "Version", "Versão", "Versão" },
        { "Questions.History.Column.Date", "Recorded on", "Registrada em", "Registada em" },
        { "Questions.History.Column.Change", "Change", "Mudança", "Alteração" },
        { "Questions.History.Column.Reason", "Reason", "Motivo", "Motivo" },
        { "Questions.History.Current", "Current", "Atual", "Atual" },
        { "Questions.History.Change.First", "First publication", "Primeira publicação", "Primeira publicação" },
        { "Questions.History.Change.Edited", "Content edited", "Conteúdo editado", "Conteúdo editado" },
        { "Questions.History.Change.KeyChanged", "Answer key changed", "Gabarito alterado", "Chave de respostas alterada" },
        { "Questions.History.Action.View", "View version", "Ver versão", "Ver versão" },
        { "Questions.History.Version.Title", "Version {0}", "Versão {0}", "Versão {0}" },
        { "Questions.ChangeReason.Label", "Reason for the answer key change", "Motivo da troca de gabarito", "Motivo da alteração da chave de respostas" },
        { "question.key_change_reason_required", "Write the reason for the answer key change.", "Escreva o motivo da troca de gabarito.", "Escreva o motivo da alteração da chave de respostas." },
        { "question.change_reason_too_long", "The reason is too long (500 characters at most).", "O motivo é longo demais (no máximo 500 caracteres).", "O motivo é demasiado longo (no máximo 500 carateres)." },
        { "question.version_not_found", "This version does not exist.", "Esta versão não existe.", "Esta versão não existe." },
        { "question.version_conflict", "Another save changed this question's history first. Reload the page and try again.", "Outra gravação alterou o histórico desta questão antes. Recarregue a página e tente de novo.", "Outra gravação alterou antes o histórico desta questão. Recarregue a página e tente novamente." }
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
