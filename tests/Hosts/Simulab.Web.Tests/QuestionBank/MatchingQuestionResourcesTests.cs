using System.Globalization;
using System.Resources;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-106 AC19: the new texts and the six error code texts exist in en, pt-BR and pt-PT with the words the item gives.
/// ResourceParityTests checks that no key is missing in a language; this pins what each one says.
/// </summary>
public sealed class MatchingQuestionResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string, string, string> Texts() => new()
    {
        { "Questions.Type.Matching", "Matching", "Associação", "Associação" },
        {
            "Questions.Type.Matching.Description",
            "Two columns: match each numbered item with a lettered one.",
            "Duas colunas: associe cada item numerado a um item com letra.",
            "Duas colunas: associe cada item numerado a um item com letra."
        },
        {
            "Questions.Section.Matching.Subtitle",
            "2 to 8 items on the left. The right column needs at least as many items; extra items match nothing. Pick the letter each number matches.",
            "De 2 a 8 itens à esquerda. A coluna da direita precisa de ao menos o mesmo número de itens; os itens extras não se associam a nenhum. Escolha a letra de cada número.",
            "De 2 a 8 itens à esquerda. A coluna da direita precisa de pelo menos o mesmo número de itens; os itens a mais não correspondem a nenhum. Escolha a letra de cada número."
        },
        { "Questions.Matching.Item.Field", "Text of item {0}", "Texto do item {0}", "Texto do item {0}" },
        { "Questions.Matching.Item.Match", "Letter matched by item {0}", "Letra associada ao item {0}", "Letra associada ao item {0}" },
        { "Questions.Matching.Match.Field", "Text of match {0}", "Texto da correspondência {0}", "Texto da correspondência {0}" },
        { "Questions.Matching.UsedBy", "Used by item {0}", "Usada pelo item {0}", "Usada pelo item {0}" },
        { "Questions.Matching.Extra", "Extra", "Extra", "A mais" },
        { "Questions.Matching.ItemsCount", "{0} of 8 items", "{0} de 8 itens", "{0} de 8 itens" },
        { "Questions.Matching.MatchesCount", "{0} of 8 matches", "{0} de 8 correspondências", "{0} de 8 correspondências" },
        { "Questions.Checklist.Matching.ItemsCount", "2 to 8 items on the left", "De 2 a 8 itens à esquerda", "De 2 a 8 itens à esquerda" },
        {
            "Questions.Checklist.Matching.MatchesCount",
            "At least as many matches on the right",
            "Ao menos o mesmo número de correspondências à direita",
            "Pelo menos o mesmo número de correspondências à direita"
        },
        {
            "Questions.Checklist.Matching.Key",
            "Every item has text and a match",
            "Todo item tem texto e uma correspondência",
            "Cada item tem texto e uma correspondência"
        },
        {
            "Questions.Preview.MatchInstruction",
            "Match each number with one letter.",
            "Associe cada número a uma letra.",
            "Associe cada número a uma letra."
        },
        {
            QuestionBankErrorCodes.QuestionMatchingItemsCount,
            "A matching question has 2 to 8 items on the left and, on the right, at least as many and at most 8.",
            "Uma questão de associação tem de 2 a 8 itens à esquerda e, à direita, ao menos o mesmo número e no máximo 8.",
            "Uma questão de associação tem de 2 a 8 itens à esquerda e, à direita, pelo menos o mesmo número e no máximo 8."
        },
        {
            QuestionBankErrorCodes.QuestionMatchingKeyInvalid,
            "Every item needs a match.",
            "Todo item precisa de uma correspondência.",
            "Cada item precisa de uma correspondência."
        },
        {
            QuestionBankErrorCodes.QuestionMatchingKeyRepeated,
            "Each letter can match only one item.",
            "Cada letra só pode ser associada a um item.",
            "Cada letra só pode corresponder a um item."
        },
        {
            QuestionBankErrorCodes.QuestionMatchItemTextRequired,
            "Write the text of the item.",
            "Escreva o texto do item.",
            "Escreva o texto do item."
        },
        {
            QuestionBankErrorCodes.QuestionMatchItemTooLong,
            "An item has more than 2,000 characters.",
            "Um item passa de 2.000 caracteres.",
            "Um item excede 2 000 caracteres."
        },
        {
            QuestionBankErrorCodes.QuestionMatchItemsNotAllowed,
            "Only a matching question has items on the left.",
            "Só uma questão de associação tem itens à esquerda.",
            "Só uma questão de associação tem itens à esquerda."
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
