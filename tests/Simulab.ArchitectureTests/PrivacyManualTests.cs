namespace Simulab.ArchitectureTests;

/// <summary>
/// F-112 BR8: the app manual page that describes the privacy policy names the real region and processors, and not the
/// region and the email company that policy 2026-v2 named. F-124 BR5: pt-BR is the only maintained manual, so only that
/// page is checked; the frozen pt-PT and en pages carry their notice instead.
/// </summary>
public class PrivacyManualTests
{
    [Fact]
    public void CreateAccount_NamesTheRealRegionAndProcessors()
    {
        var page = File.ReadAllText(Path.Combine(SolutionAssemblies.RepositoryRoot(), "docs", "manual", "pt-BR", "create-account.md"));

        page.Should().Contain("Central US")
            .And.Contain("Microsoft Azure Communication Services")
            .And.Contain("Anthropic")
            .And.NotContain("Brazil South")
            .And.NotContain("SendGrid");
    }
}
