namespace Simulab.Web.Tests.Localization;

/// <summary>
/// F-22 BR5: the guard itself, on made-up data. A check nobody has seen fail is not a check — F-14 shipped a
/// code with no text past two tests that were supposed to catch it.
/// </summary>
public sealed class ErrorCodeTextCheckTests
{
    private static readonly IReadOnlyDictionary<string, string> NothingExempt = new Dictionary<string, string>();

    private static Dictionary<string, IReadOnlyCollection<string>> Texts(params string[] keys) =>
        new()
        {
            ["en"] = keys,
            ["pt-BR"] = keys,
            ["pt-PT"] = keys
        };

    // AC1 shape: everything in place passes.
    [Fact]
    public void EveryCodeWithItsTexts_Passes() =>
        ErrorCodeTextCheck.Run(["role.not_found"], Texts("role.not_found", "common.unexpected_error"), NothingExempt)
            .Should().BeEmpty();

    // AC2: one language short is enough to fail, and the message names that language.
    [Fact]
    public void CodeMissingInOneCultureOnly_FailsNamingThatCulture()
    {
        var texts = new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["en"] = ["role.not_found"],
            ["pt-BR"] = ["role.not_found"],
            ["pt-PT"] = []
        };

        var failures = ErrorCodeTextCheck.Run(["role.not_found"], texts, NothingExempt);

        var failure = failures.Should().ContainSingle().Subject;
        failure.Subject.Should().Be("role.not_found");
        failure.Detail.Should().Contain("no text in pt-PT").And.NotContain("en,");
    }

    // AC3: no text anywhere names the three.
    [Fact]
    public void CodeMissingEverywhereAndNotExempt_FailsNamingTheThreeCultures()
    {
        var failures = ErrorCodeTextCheck.Run(["role.not_found"], Texts(), NothingExempt);

        failures.Should().ContainSingle().Which.Detail.Should().Contain("no text in en, pt-BR, pt-PT");
    }

    // AC4: an exempt code passes, and an exemption with no reason does not.
    [Fact]
    public void ExemptCode_Passes_AndAnExemptionWithNoReason_Fails()
    {
        var withReason = new Dictionary<string, string> { ["mfa_required"] = "read as a flow signal; never displayed" };
        var withoutReason = new Dictionary<string, string> { ["mfa_required"] = "   " };

        ErrorCodeTextCheck.Run(["mfa_required"], Texts(), withReason).Should().BeEmpty();

        ErrorCodeTextCheck.Run(["mfa_required"], Texts(), withoutReason)
            .Should().ContainSingle().Which.Detail.Should().Contain("no reason");
    }

    // AC4: an exemption left behind by a deleted code is debt, and says so.
    [Fact]
    public void ExemptionForACodeThatNoLongerExists_Fails() =>
        ErrorCodeTextCheck.Run(
                ["role.not_found"],
                Texts("role.not_found"),
                new Dictionary<string, string> { ["gone.code"] = "was never displayed" })
            .Should().ContainSingle().Which.Detail.Should().Contain("no longer an error code");

    // AC5: a code-shaped text with no code fails; the Web's own namespace never does.
    [Fact]
    public void OrphanText_Fails_AndTheWebsOwnPrefix_DoesNot()
    {
        var failures = ErrorCodeTextCheck.Run(
            ["role.not_found"],
            Texts("role.not_found", "role.renamed_away", "common.unexpected_error"),
            NothingExempt);

        failures.Select(failure => failure.Subject).Distinct().Should().Equal("role.renamed_away");
        failures.Should().HaveCount(3, "the text is in the three files, and each one is named");
        failures[0].Detail.Should().Contain("answers to no error code");
    }

    // AC5: a key that is not code-shaped is a label, not an orphan code.
    [Theory]
    [InlineData("RoleHistory.Title")]
    [InlineData("Nav.Roles")]
    [InlineData("Common.TryAgain")]
    public void KeyThatIsNotCodeShaped_IsNotAnOrphan(string key) =>
        ErrorCodeTextCheck.Run(["role.not_found"], Texts("role.not_found", key), NothingExempt).Should().BeEmpty();

    // The shape only decides what counts as an orphan text (BR4); a code without a dot, such as the protocol's
    // own `mfa_required`, is still checked for its text (BR2) because it comes from the constants, not the shape.
    [Theory]
    [InlineData("role.not_found", true)]
    [InlineData("account_event.period_invalid", true)]
    [InlineData("mfa_required", false)]
    [InlineData("RoleHistory.Title", false)]
    [InlineData("Permission.identity.roles.manage.Name", false)]
    public void CodeShape_IsSnakeCaseWordsSeparatedByDots(string key, bool expected) =>
        ErrorCodeTextCheck.IsCodeShaped(key).Should().Be(expected);

    // AC7: every failure says where to go.
    [Fact]
    public void MissingTextFailure_NamesTheResourceFileAndTheExemptList()
    {
        var failure = ErrorCodeTextCheck.Run(["role.not_found"], Texts(), NothingExempt).Should().ContainSingle().Subject;

        failure.Detail.Should().Contain("SharedResources").And.Contain("NeverShown");
        failure.ToString().Should().StartWith("role.not_found: ");
    }
}
