namespace Simulab.ArchitectureTests;

/// <summary>
/// F-112 BR8: the app manual page that describes the privacy policy names the real region and processors, in the three
/// languages, and not the region and the email company that policy 2026-v2 named.
/// </summary>
public class PrivacyManualTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("pt-BR")]
    [InlineData("pt-PT")]
    public void CreateAccount_NamesTheRealRegionAndProcessors(string locale)
    {
        var page = File.ReadAllText(Path.Combine(SolutionAssemblies.RepositoryRoot(), "docs", "manual", locale, "create-account.md"));

        page.Should().Contain("Central US")
            .And.Contain("Microsoft Azure Communication Services")
            .And.Contain("Anthropic")
            .And.NotContain("Brazil South")
            .And.NotContain("SendGrid");
    }
}
