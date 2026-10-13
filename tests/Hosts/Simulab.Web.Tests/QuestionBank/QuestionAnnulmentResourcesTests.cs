using System.Globalization;
using System.Resources;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-101 AC18: every text and every error code of the annulment exists in en, pt-BR and pt-PT.
/// ResourceParityTests checks that no key is missing in a language; this pins what each one says.
/// </summary>
public sealed class QuestionAnnulmentResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string, string, string> Texts() => new()
    {
        { "Questions.Status.Annulled", "Annulled", "Anulada", "Anulada" },
        { "Questions.Action.Annul", "Annul question", "Anular questão", "Anular questão" },
        { "Questions.Action.Reinstate", "Reinstate question", "Reintegrar questão", "Reintegrar questão" },
        { "Questions.Publication.Annulled", "Annulled. Out of practice and frozen until it is reinstated.", "Anulada. Fora da prática e congelada até ser reintegrada.", "Anulada. Fora da prática e congelada até ser reintegrada." },
        { "Questions.Annulment.Date", "Annulled on {0}", "Anulada em {0}", "Anulada em {0}" },
        { "Questions.Annulment.Reason", "Reason: {0}", "Motivo: {0}", "Motivo: {0}" },
        { "Questions.Annulled.Alert", "This question is annulled and cannot be edited. Reinstate it to change it.", "Esta questão está anulada e não pode ser editada. Reintegre-a para alterá-la.", "Esta questão está anulada e não pode ser editada. Reintegre-a para a alterar." },
        { "Questions.Annul.Dialog.Title", "Annul this question?", "Anular esta questão?", "Anular esta questão?" },
        { "Questions.Annul.Dialog.Message", "Practice will stop using it. You can reinstate it later; the history is kept.", "A prática deixará de usá-la. Você pode reintegrá-la depois; o histórico é mantido.", "A prática deixará de a usar. Pode reintegrá-la depois; o histórico é mantido." },
        { "Questions.Reinstate.Dialog.Title", "Reinstate this question?", "Reintegrar esta questão?", "Reintegrar esta questão?" },
        { "Questions.Reinstate.Dialog.Message", "It becomes published again and practice can use it.", "Ela volta a ser publicada e a prática pode usá-la.", "Volta a ser publicada e a prática pode usá-la." },
        { "Questions.StatusReason.Label", "Reason", "Motivo", "Motivo" },
        { "Questions.StatusReason.Hint", "Required. Plain text, up to 500 characters.", "Obrigatório. Texto simples, até 500 caracteres.", "Obrigatório. Texto simples, até 500 carateres." },
        { "Questions.StatusReason.Cancel", "Cancel", "Cancelar", "Cancelar" },
        { "Questions.Annulled", "Question annulled.", "Questão anulada.", "Questão anulada." },
        { "Questions.Reinstated", "Question reinstated.", "Questão reintegrada.", "Questão reintegrada." },
        { "Questions.History.Change.Annulled", "Annulled", "Anulada", "Anulada" },
        { "Questions.History.Change.Reinstated", "Reinstated", "Reintegrada", "Reintegrada" },
        { "Questions.Preview.Annulled", "Annulled on {0}: {1}", "Anulada em {0}: {1}", "Anulada em {0}: {1}" },
        { "Questions.Delete.DisabledAnnulled", "This question is annulled and cannot be deleted.", "Esta questão está anulada e não pode ser excluída.", "Esta questão está anulada e não pode ser eliminada." },
        { "question.annulled", "This question is annulled and cannot be changed. Reinstate it first.", "Esta questão está anulada e não pode ser alterada. Reintegre-a primeiro.", "Esta questão está anulada e não pode ser alterada. Reintegre-a primeiro." },
        { "question.already_annulled", "This question is already annulled.", "Esta questão já está anulada.", "Esta questão já está anulada." },
        { "question.not_annulled", "This question is not annulled.", "Esta questão não está anulada.", "Esta questão não está anulada." },
        { "question.status_reason_required", "Write the reason.", "Escreva o motivo.", "Escreva o motivo." },
        { "question.status_reason_too_long", "The reason is too long (500 characters at most).", "O motivo é longo demais (no máximo 500 caracteres).", "O motivo é demasiado longo (no máximo 500 carateres)." }
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
