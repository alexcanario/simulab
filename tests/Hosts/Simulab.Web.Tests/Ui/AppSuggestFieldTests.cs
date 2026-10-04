using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-74: the kit's free text with suggestions. The text is the value and a pick is never required; the suggestions
/// match ignoring case and accents, the way the Api tells two groups apart.
/// </summary>
public sealed class AppSuggestFieldTests : KitTestContext
{
    private static readonly string[] Groups = ["Conhecimentos Básicos", "Conhecimentos Específicos", "Legislação"];

    private IRenderedComponent<AppSuggestField> RenderField(string? value = null, string? error = null, string? hint = null)
    {
        Render<MudBlazor.MudPopoverProvider>();

        return Render<AppSuggestField>(parameters => parameters
            .Add(field => field.Id, "suggest")
            .Add(field => field.Label, "Group")
            .Add(field => field.Suggestions, Groups)
            .Add(field => field.SuggestionsLabel, "Groups already used")
            .Add(field => field.MaxLength, 100)
            .Add(field => field.Value, value)
            .Add(field => field.Hint, hint)
            .Add(field => field.Error, error));
    }

    [Theory]
    [InlineData("basicos", "Conhecimentos Básicos")]
    [InlineData("ESPECÍFICOS", "Conhecimentos Específicos")]
    [InlineData("  legislacao ", "Legislação")]
    public void Match_IgnoresCaseAndAccents(string typed, string expected)
    {
        AppSuggestField.Match(Groups, typed).Should().Equal(expected);
    }

    [Fact]
    public void Match_ASharedWord_KeepsThePagesOrder()
    {
        AppSuggestField.Match(Groups, "conhecimentos").Should().Equal("Conhecimentos Básicos", "Conhecimentos Específicos");
    }

    [Fact]
    public void Match_NothingTyped_OffersEverySuggestion()
    {
        AppSuggestField.Match(Groups, "").Should().Equal(Groups);
        AppSuggestField.Match(Groups, null).Should().Equal(Groups);
    }

    [Fact]
    public void Match_NoSuggestionContainsTheText_OffersNothingAndThatIsNotAnError()
    {
        AppSuggestField.Match(Groups, "física").Should().BeEmpty();
    }

    [Theory]
    [InlineData("Básicos", "basicos")]
    [InlineData("  AÇÃO ", "acao")]
    [InlineData(null, "")]
    public void Normalize_StripsAccentsAndCaseAndTrims(string? text, string expected)
    {
        AppSuggestField.Normalize(text).Should().Be(expected);
    }

    // The library renders a plain input, so the combobox pattern is the kit's own, the same as the lookup's.
    [Fact]
    public void Render_TheInputIsAClosedComboboxWithItsLimit()
    {
        var field = RenderField();

        var input = field.Find("#suggest");
        input.GetAttribute("role").Should().Be("combobox");
        input.GetAttribute("aria-expanded").Should().Be("false");
        input.GetAttribute("aria-autocomplete").Should().Be("list");
        input.GetAttribute("aria-haspopup").Should().Be("listbox");
        input.GetAttribute("aria-describedby").Should().Be("suggest-description");
        input.GetAttribute("aria-required").Should().BeNull("an optional field is not announced as required");
        input.GetAttribute("autocomplete").Should().Be("off");
        input.GetAttribute("maxlength").Should().Be("100");
        field.Find("label[for='suggest']").TextContent.Should().Contain("Group");
    }

    [Fact]
    public void Render_WithAValue_ShowsItInTheInput()
    {
        var field = RenderField(value: "Algo novo");

        field.Find("#suggest").GetAttribute("value").Should().Be("Algo novo");
    }

    [Fact]
    public void Render_WithAnError_ShowsItInsteadOfTheHintAndMarksTheInput()
    {
        var field = RenderField(error: "The group is too long.", hint: "How the notice groups its subjects.");

        field.Find(".app-field-error-text").TextContent.Should().Contain("The group is too long.");
        field.FindAll(".app-field-hint").Should().BeEmpty();
        field.Find("#suggest").GetAttribute("aria-invalid").Should().Be("true");
    }

    [Fact]
    public void Render_WithAHint_ShowsItUnderTheField()
    {
        var field = RenderField(hint: "How the notice groups its subjects.");

        field.Find(".app-field-hint").TextContent.Should().Contain("How the notice groups its subjects.");
    }

    // A text that matches no suggestion is the value all the same: nothing is coerced to a pick.
    [Fact]
    public void Typing_ATextThatMatchesNoSuggestion_IsReportedAsTheValue()
    {
        string? reported = null;
        Render<MudBlazor.MudPopoverProvider>();
        var field = Render<AppSuggestField>(parameters => parameters
            .Add(f => f.Id, "suggest")
            .Add(f => f.Label, "Group")
            .Add(f => f.Suggestions, Groups)
            .Add(f => f.SuggestionsLabel, "Groups already used")
            .Add(f => f.ValueChanged, (string? value) => reported = value));

        field.Find("#suggest").Input("Matemática");
        AdvanceDebounce();

        field.WaitForAssertion(() => reported.Should().Be("Matemática"));
    }
}
