using System.Globalization;
using System.Resources;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Identity;

/// <summary>
/// F-167 AC14 (BR11): the "Your data" card of <c>/account</c> names the attempts and answers among what the file holds,
/// in each UI locale, beside what F-16 and F-32 already listed.
/// </summary>
public sealed class DataExplanationResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(IdentityResources).FullName!, typeof(IdentityResources).Assembly);

    [Theory]
    [InlineData("en", "attempts", "answers", "security events")]
    [InlineData("pt-BR", "tentativas", "respostas", "eventos de segurança")]
    [InlineData("pt-PT", "tentativas", "respostas", "eventos de segurança")]
    public void Explanation_NamesTheAttemptsAndAnswers(string culture, string attempts, string answers, string kept)
    {
        var text = Manager.GetString("Account.Data.Explanation", new CultureInfo(culture));

        text.Should().Contain(attempts).And.Contain(answers).And.Contain(kept, "what the card already listed stays");
    }
}
