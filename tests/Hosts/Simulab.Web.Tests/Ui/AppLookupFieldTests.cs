using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-34 BR16: the kit's searchable picker. The states a page cannot show on its own — nothing found, the
/// search itself failing, and what the field announces to a screen reader — live here.
/// </summary>
public sealed class AppLookupFieldTests : KitTestContext
{
    private static readonly AppLookupOption Cebraspe =
        new(Guid.Parse("0198f0a3-0000-7000-8000-000000000001"), "Centro Brasileiro de Pesquisa em Avaliacao (CEBRASPE)", "Exam board");

    private IRenderedComponent<AppLookupField> Render(
        Func<string, CancellationToken, Task<IReadOnlyList<AppLookupOption>>> search,
        string? error = null) =>
        Render<AppLookupField>(parameters => parameters
            .Add(field => field.Id, "lookup")
            .Add(field => field.Label, "Issuing authority")
            .Add(field => field.Placeholder, "Name or acronym")
            .Add(field => field.Required, true)
            .Add(field => field.SearchAsync, search)
            .Add(field => field.Error, error));

    private static Task<IReadOnlyList<AppLookupOption>> Found(string term, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AppLookupOption>>([Cebraspe]);

    private static Task<IReadOnlyList<AppLookupOption>> Nothing(string term, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AppLookupOption>>([]);

    private static Task<IReadOnlyList<AppLookupOption>> Broken(string term, CancellationToken cancellationToken) =>
        Task.FromException<IReadOnlyList<AppLookupOption>>(new HttpRequestException("the search failed"));

    // The library renders a plain text input, so the combobox pattern is the kit's own (found on screen).
    [Fact]
    public void Render_TheInputIsAClosedCombobox()
    {
        var field = Render(Found);

        var input = field.Find("#lookup");
        input.GetAttribute("role").Should().Be("combobox");
        input.GetAttribute("aria-expanded").Should().Be("false");
        input.GetAttribute("aria-autocomplete").Should().Be("list");
        input.GetAttribute("aria-haspopup").Should().Be("listbox");
        input.GetAttribute("aria-required").Should().Be("true");
        input.GetAttribute("aria-describedby").Should().Be("lookup-description");
        input.GetAttribute("autocomplete").Should().Be("off", "a picker must never be autofilled by the browser");
    }

    [Fact]
    public void Render_WithoutATerm_InvitesTheReaderToTypeTheMinimum()
    {
        var field = Render(Found);

        field.Find(".app-field-description").TextContent.Should().Contain("Type at least 2 characters");
    }

    [Fact]
    public void Render_WithAnError_ShowsItInsteadOfTheHintAndMarksTheInput()
    {
        var field = Render(Found, error: "Choose the issuing authority.");

        field.Find(".app-field-error-text").TextContent.Should().Contain("Choose the issuing authority.");
        field.Find("#lookup").GetAttribute("aria-invalid").Should().Be("true");
    }

    // BR16: nothing found says so with the term, and is not confused with a broken search.
    [Fact]
    public void Search_FindsNothing_AnnouncesTheEmptyResultWithTheTerm()
    {
        var field = Render(Nothing);

        field.Find("#lookup").Input("zzz");
        AdvanceDebounce();

        field.WaitForAssertion(() =>
            field.Find("[aria-live='polite']").TextContent.Should().Contain("Nothing found for \"zzz\"."));
        field.Find(".app-field-description").TextContent.Should().NotContain("The search failed.");
    }

    // BR16: a broken call is not "no results": the description says so and offers another go.
    [Fact]
    public void Search_Fails_ShowsTheFailureAndTryAgain()
    {
        var field = Render(Broken);

        field.Find("#lookup").Input("ceb");
        AdvanceDebounce();

        field.WaitForAssertion(() => field.Find(".app-field-error-text").TextContent.Should().Contain("The search failed."));
        field.Markup.Should().Contain("Try again");
        field.Find("[aria-live='polite']").TextContent.Should().Contain("The search failed.");
    }

    [Fact]
    public void Search_Fails_ThenTryAgain_ClearsTheFailure()
    {
        var field = Render(Broken);
        field.Find("#lookup").Input("ceb");
        AdvanceDebounce();
        field.WaitForAssertion(() => field.Markup.Should().Contain("The search failed."));

        field.FindAll("button").First(button => button.TextContent.Contains("Try again", StringComparison.Ordinal)).Click();

        field.WaitForAssertion(() => field.Markup.Should().NotContain("The search failed."));
    }

    // F-48 AC2: the picker debounces on the container's clock - typing alone searches nothing, the interval passing searches once.
    [Fact]
    public void Search_Typed_SearchesOnlyWhenTheDebounceElapses()
    {
        var terms = new List<string>();
        var field = Render((term, cancellationToken) =>
        {
            terms.Add(term);
            return Found(term, cancellationToken);
        });

        field.Find("#lookup").Input("ceb");
        terms.Should().BeEmpty("the term is still waiting out the debounce");

        AdvanceDebounce();

        field.WaitForAssertion(() => terms.Should().Equal("ceb"));
    }

    [Fact]
    public void Search_Finds_AnnouncesHowMany()
    {
        var field = Render(Found);

        field.Find("#lookup").Input("ceb");
        AdvanceDebounce();

        // "Results: 1", not "1 results": the count is read aloud, and no language here has one plural form.
        field.WaitForAssertion(() => field.Find("[aria-live='polite']").TextContent.Should().Contain("Results: 1"));
    }

    // F-42: a search over the catalog keeps its defaults - nothing opens before the reader types, and the list is cut at 20.
    [Fact]
    public void Render_ByDefault_DoesNotOpenOnFocus_AndShowsAtMostTwentyCandidates()
    {
        var field = Render(Found);

        var autocomplete = field.FindComponent<MudBlazor.MudAutocomplete<AppLookupOption>>().Instance;
        autocomplete.OpenOnFocus.Should().BeFalse();
        autocomplete.MaxItems.Should().Be(AppLookupField.MaxCandidates).And.Be(20);
    }

    // F-42: a short fixed list (the 27 states) opens on focus, with no minimum of characters and room for every item.
    [Fact]
    public void Render_ForAFixedList_OpensOnFocusWithoutATermAndKeepsEveryItem()
    {
        var field = Render<AppLookupField>(parameters => parameters
            .Add(component => component.Id, "lookup")
            .Add(component => component.Label, "State")
            .Add(component => component.Placeholder, "Choose")
            .Add(component => component.SearchAsync, Found)
            .Add(component => component.MinChars, 0)
            .Add(component => component.MaxItems, 27)
            .Add(component => component.OpenOnFocus, true));

        var autocomplete = field.FindComponent<MudBlazor.MudAutocomplete<AppLookupOption>>().Instance;
        autocomplete.OpenOnFocus.Should().BeTrue();
        autocomplete.MinCharacters.Should().Be(0);
        autocomplete.MaxItems.Should().Be(27);

        field.Find("#lookup").Focus();

        field.WaitForAssertion(() => field.Find("[aria-live='polite']").TextContent.Should().Contain("Results: 1"));
    }
}
