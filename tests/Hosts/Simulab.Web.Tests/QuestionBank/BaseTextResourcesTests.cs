using System.Globalization;
using System.Resources;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-102 AC14: every text and every error code of the base text exists in en, pt-BR and pt-PT with the words the
/// item gives. ResourceParityTests checks that no key is missing in a language; this pins what each one says.
/// </summary>
public sealed class BaseTextResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string, string, string> Texts() => new()
    {
        { "Nav.BaseTexts", "Base texts", "Textos-base", "Textos de apoio" },
        { "BaseTexts.Title", "Base texts", "Textos-base", "Textos de apoio" },
        { "BaseTexts.Search.Placeholder", "Search by title", "Pesquisar pelo título", "Pesquisar pelo título" },
        { "BaseTexts.Filter.Language", "Language", "Idioma", "Idioma" },
        { "BaseTexts.Filter.Language.All", "All languages", "Todos os idiomas", "Todos os idiomas" },
        { "BaseTexts.Column.Title", "Title", "Título", "Título" },
        { "BaseTexts.Column.Language", "Language", "Idioma", "Idioma" },
        { "BaseTexts.Column.Questions", "Questions", "Questões", "Questões" },
        { "BaseTexts.Empty", "No base text yet.", "Nenhum texto-base ainda.", "Ainda não há textos de apoio." },
        {
            "BaseTexts.Empty.Filters",
            "No base text in this language.",
            "Nenhum texto-base neste idioma.",
            "Nenhum texto de apoio neste idioma."
        },
        {
            "BaseTexts.Empty.Search",
            "No base text matches \"{0}\".",
            "Nenhum texto-base corresponde a \"{0}\".",
            "Nenhum texto de apoio corresponde a \"{0}\"."
        },
        { "BaseTexts.Row.Name", "base text \"{0}\"", "texto-base \"{0}\"", "texto de apoio \"{0}\"" },
        {
            "BaseTexts.Delete.DisabledInUse",
            "Questions use this text ({0}). Remove their links before deleting it.",
            "Há questões que usam este texto ({0}). Remova os vínculos delas antes de excluir.",
            "Há questões que usam este texto ({0}). Retire as ligações delas antes de o eliminar."
        },
        { "BaseTexts.Delete.Object", "this base text", "este texto-base", "este texto de apoio" },
        {
            "BaseTexts.Delete.Message",
            "\"{0}\" leaves the list and the question picker. No question uses it.",
            "\"{0}\" sai da lista e do seletor das questões. Nenhuma questão o usa.",
            "\"{0}\" sai da lista e do seletor das questões. Nenhuma questão o usa."
        },
        { "BaseTexts.Deleted", "Base text deleted.", "Texto-base excluído.", "Texto de apoio eliminado." },
        { "BaseTexts.Saved", "Base text saved.", "Texto-base salvo.", "Texto de apoio guardado." },
        { "BaseTexts.Form.AddTitle", "Add base text", "Adicionar texto-base", "Adicionar texto de apoio" },
        { "BaseTexts.Form.EditTitle", "Edit base text", "Editar texto-base", "Editar texto de apoio" },
        { "BaseTexts.BackToList", "Back to the base texts", "Voltar para os textos-base", "Voltar aos textos de apoio" },
        { "BaseTexts.Section.Text", "Base text", "Texto-base", "Texto de apoio" },
        {
            "BaseTexts.Section.Text.Subtitle",
            "A passage, table or case that several questions refer to. Written once, shown before each of their statements.",
            "Um trecho, tabela ou caso a que várias questões se referem. Escrito uma vez, aparece antes do enunciado de cada uma.",
            "Um excerto, tabela ou caso a que várias questões se referem. Escrito uma vez, aparece antes do enunciado de cada uma."
        },
        { "BaseTexts.Field.Title", "Title", "Título", "Título" },
        {
            "BaseTexts.Field.Title.Placeholder",
            "A name that is easy to find",
            "Um nome fácil de encontrar",
            "Um nome fácil de encontrar"
        },
        {
            "BaseTexts.Field.Title.Hint",
            "Up to 200 characters. It names the text in lists and in the question form.",
            "Até 200 caracteres. É o nome do texto nas listas e no formulário da questão.",
            "Até 200 caracteres. É o nome do texto nas listas e no formulário da questão."
        },
        { "BaseTexts.Field.ContentLanguage", "Content language", "Idioma do conteúdo", "Idioma do conteúdo" },
        {
            "BaseTexts.Field.ContentLanguage.Hint",
            "The language the text is written in. Only questions in this language can use it.",
            "O idioma em que o texto está escrito. Só questões neste idioma podem usá-lo.",
            "O idioma em que o texto está escrito. Só as questões neste idioma o podem usar."
        },
        {
            "BaseTexts.Field.ContentLanguage.Locked",
            "Locked while questions use this text ({0}). A question and its base text are always in the same language.",
            "Bloqueado enquanto houver questões usando este texto ({0}). Uma questão e seu texto-base estão sempre no mesmo idioma.",
            "Bloqueado enquanto houver questões a usar este texto ({0}). Uma questão e o seu texto de apoio estão sempre no mesmo idioma."
        },
        { "BaseTexts.Field.Text", "Text", "Texto", "Texto" },
        { "BaseTexts.Field.Text.Placeholder", "Paste or write the text", "Cole ou escreva o texto", "Cole ou escreva o texto" },
        { "BaseTexts.Field.Text.Hint", "Up to 20,000 characters.", "Até 20.000 caracteres.", "Até 20 000 caracteres." },
        {
            "BaseTexts.Versions.Notice",
            "Saving a change to the text records a new version of each published question that uses it; drafts and annulled questions get none. Changing only the title records nothing.",
            "Salvar uma mudança no texto registra uma nova versão de cada questão publicada que o usa; rascunhos e questões anuladas não ganham versão. Mudar só o título não registra nada.",
            "Guardar uma alteração ao texto regista uma nova versão de cada questão publicada que o usa; os rascunhos e as questões anuladas não recebem versão. Alterar só o título não regista nada."
        },
        {
            "BaseTexts.Section.Questions",
            "Questions that use this text ({0})",
            "Questões que usam este texto ({0})",
            "Questões que usam este texto ({0})"
        },
        {
            "BaseTexts.Section.Questions.Subtitle",
            "Most recently changed first. Open a question to change or remove its link.",
            "As alteradas mais recentemente primeiro. Abra uma questão para trocar ou remover o vínculo.",
            "As alteradas mais recentemente primeiro. Abra uma questão para mudar ou retirar a ligação."
        },
        {
            "BaseTexts.Questions.Empty",
            "No question uses this text yet. Pick it in the Base text section of a question.",
            "Nenhuma questão usa este texto ainda. Escolha-o na seção Texto-base de uma questão.",
            "Ainda nenhuma questão usa este texto. Escolha-o na secção Texto de apoio de uma questão."
        },
        { "BaseTexts.Section.Delete", "Delete this base text", "Excluir este texto-base", "Eliminar este texto de apoio" },
        {
            "BaseTexts.Section.Delete.Subtitle",
            "Only a text that no question uses can be deleted.",
            "Só um texto que nenhuma questão usa pode ser excluído.",
            "Só um texto que nenhuma questão usa pode ser eliminado."
        },
        {
            "Questions.BaseText.Option.Secondary",
            "Questions: {0} · changed on {1}",
            "Questões: {0} · alterado em {1}",
            "Questões: {0} · alterado em {1}"
        },
        { "Questions.BaseText.Open", "Open the base text", "Abrir o texto-base", "Abrir o texto de apoio" },
        { "Questions.BaseText.Show", "Show the text", "Mostrar o texto", "Mostrar o texto" },
        { "Questions.BaseText.Hide", "Hide the text", "Ocultar o texto", "Ocultar o texto" },
        {
            "Questions.BaseText.ReadOnlyNote",
            "Edited on its own page, for every question that uses it.",
            "É editado na página dele, para todas as questões que o usam.",
            "É editado na sua própria página, para todas as questões que o usam."
        },
        { "Questions.BaseText.Create.Lead", "Not in the list?", "Não está na lista?", "Não está na lista?" },
        { "Questions.BaseText.Create", "Create a base text", "Criar um texto-base", "Criar um texto de apoio" },
        {
            "Questions.BaseText.Create.Help",
            "It opens in a new tab, so this question stays as it is. Save the text there, come back and pick it here.",
            "Abre em uma nova aba, e esta questão fica como está. Salve o texto lá, volte e escolha-o aqui.",
            "Abre num novo separador, e esta questão fica como está. Guarde o texto lá, volte e escolha-o aqui."
        },
        {
            "Questions.BaseText.Cleared",
            "The base text \"{0}\" was removed: it is in {1}, and this question is now in {2}.",
            "O texto-base \"{0}\" foi removido: ele está em {1}, e esta questão agora está em {2}.",
            "O texto de apoio \"{0}\" foi retirado: está em {1}, e esta questão está agora em {2}."
        },
        {
            "Questions.BaseText.Kept",
            "Stays chosen for the next question. Change or remove it when the block of questions ends.",
            "Continua escolhido para a próxima questão. Troque ou remova quando o bloco de questões acabar.",
            "Continua escolhido para a próxima questão. Mude-o ou retire-o quando o bloco de questões terminar."
        },
        {
            "Common.LinkOpensNewTab",
            "{0} (opens in a new tab)",
            "{0} (abre em uma nova aba)",
            "{0} (abre num novo separador)"
        },
        {
            "Questions.Section.BaseText.Subtitle",
            "A text shared by several questions, shown before the statement. Optional.",
            "Um texto compartilhado por várias questões, mostrado antes do enunciado. Opcional.",
            "Um texto partilhado por várias questões, mostrado antes do enunciado. Opcional."
        },
        {
            "Questions.Field.BaseText.Placeholder",
            "Title of the base text",
            "Título do texto-base",
            "Título do texto de apoio"
        },
        {
            "Questions.Field.BaseText.Hint",
            "Only texts in {0}, the language of this question, are listed.",
            "Só aparecem textos em {0}, o idioma desta questão.",
            "Só aparecem textos em {0}, o idioma desta questão."
        },
        {
            QuestionBankErrorCodes.BaseTextNotFound,
            "This base text no longer exists.",
            "Este texto-base não existe mais.",
            "Este texto de apoio já não existe."
        },
        { QuestionBankErrorCodes.BaseTextTitleRequired, "Write the title.", "Escreva o título.", "Escreva o título." },
        {
            QuestionBankErrorCodes.BaseTextTitleTooLong,
            "The title has more than 200 characters.",
            "O título passa de 200 caracteres.",
            "O título excede 200 caracteres."
        },
        { QuestionBankErrorCodes.BaseTextTextRequired, "Write the text.", "Escreva o texto.", "Escreva o texto." },
        {
            QuestionBankErrorCodes.BaseTextTextTooLong,
            "The text has more than 20,000 characters.",
            "O texto passa de 20.000 caracteres.",
            "O texto excede 20 000 caracteres."
        },
        {
            QuestionBankErrorCodes.BaseTextContentLanguageInvalid,
            "Choose one of the listed languages.",
            "Escolha um dos idiomas da lista.",
            "Escolha um dos idiomas da lista."
        },
        {
            QuestionBankErrorCodes.BaseTextInUse,
            "Questions still use this base text. Remove their links before deleting it.",
            "Ainda há questões usando este texto-base. Remova os vínculos delas antes de excluir.",
            "Ainda há questões a usar este texto de apoio. Retire as ligações delas antes de o eliminar."
        },
        {
            QuestionBankErrorCodes.BaseTextLanguageInUse,
            "Questions use this base text, so its language cannot change.",
            "Há questões usando este texto-base, então o idioma dele não pode mudar.",
            "Há questões a usar este texto de apoio, por isso o idioma dele não pode mudar."
        },
        {
            QuestionBankErrorCodes.QuestionBaseTextNotFound,
            "This base text no longer exists. Pick another or leave the field empty.",
            "Este texto-base não existe mais. Escolha outro ou deixe o campo vazio.",
            "Este texto de apoio já não existe. Escolha outro ou deixe o campo vazio."
        },
        {
            QuestionBankErrorCodes.QuestionBaseTextLanguageMismatch,
            "The base text and the question must be in the same language.",
            "O texto-base e a questão precisam estar no mesmo idioma.",
            "O texto de apoio e a questão têm de estar no mesmo idioma."
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

    [Theory]
    [InlineData("en")]
    [InlineData("pt-BR")]
    [InlineData("pt-PT")]
    public void TheInlineBaseTextTooLongText_LeftWithTheInlineField(string culture)
    {
        Manager.GetString("question.base_text_too_long", new CultureInfo(culture)).Should().BeNull();
    }

    [Fact]
    public void TheTwoPortugueseVariants_AreDifferentTextsWhereTheItemSaysSo()
    {
        var ptBr = Manager.GetString("BaseTexts.Title", new CultureInfo("pt-BR"));
        var ptPt = Manager.GetString("BaseTexts.Title", new CultureInfo("pt-PT"));

        ptBr.Should().NotBe(ptPt, "pt-BR says \"textos-base\" and pt-PT says \"textos de apoio\"");
    }
}
