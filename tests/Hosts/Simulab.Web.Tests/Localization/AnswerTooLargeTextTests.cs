using System.Globalization;
using System.Resources;
using Simulab.ExamEngine.Contracts;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Localization;

/// <summary>
/// B-36 AC3: the code of a save body above the limit has its text in en, pt-BR and pt-PT, under a key equal to the
/// code. ErrorCodeTextTests checks that every code has a text; this pins the three texts the owner approved.
/// </summary>
public sealed class AnswerTooLargeTextTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string> Texts() => new()
    {
        { "en", "The answer is too large to be saved. Shorten the text and try again." },
        { "pt-BR", "A resposta é grande demais para ser salva. Encurte o texto e tente de novo." },
        { "pt-PT", "A resposta é demasiado grande para ser guardada. Encurte o texto e tente de novo." }
    };

    [Theory]
    [MemberData(nameof(Texts))]
    public void TheCodeOfAnOversizedAnswer_HasItsApprovedTextInEachLanguage(string culture, string expected)
    {
        Manager.GetString(ExamAttemptErrorCodes.AnswerTooLarge, new CultureInfo(culture)).Should().Be(expected);
    }
}
