using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-100 `/admin/questions`: the filters of the list - what each picker offers, what the list asks the server for,
/// the address that holds them, the chips, "Clear all", the load errors and the limit of picked ids (AC1 to AC3 and
/// AC5 to AC7 screen part, AC9 to AC12, AC15). The rules themselves are the Api's and have their own tests.
/// </summary>
public sealed class QuestionsFilterPageTests : QuestionPageTestContext
{
    private const string ListPath = "/api/v1/question-bank/questions";

    private static readonly OrganizerLookupResponse Cebraspe = new(Guid.Parse("0198f5a0-0000-7000-8000-0000000000c1"), "CEBRASPE", "Cebraspe");
    private static readonly OrganizerLookupResponse Fgv = new(Guid.Parse("0198f5a0-0000-7000-8000-0000000000c2"), "FGV", "Fundação Getulio Vargas");

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    private IRenderedComponent<Questions> RenderPage() => Render<Questions>();

    private void Seed()
    {
        Api.Organizers.Add(Cebraspe);
        Api.Organizers.Add(Fgv);
        Api.Questions.Add(Draft);
        Api.Questions.Add(Published with { Difficulty = 5 });
        Api.Questions.Add(FromGuard with { Type = QuestionType.TrueFalse, Status = QuestionStatus.Published });
    }

    private static void WaitForRows(IRenderedComponent<Questions> page, int count) =>
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(count));

    private static void Pick(IRenderedComponent<Questions> page, string field, string key)
    {
        page.WaitForAssertion(() => page.FindAll($"#{field}").Should().ContainSingle());
        page.Find($"#{field}").Click();
        page.WaitForAssertion(() => page.FindAll($"#{field}-option-{key}").Should().ContainSingle());
        page.Find($"#{field}-option-{key}").Click();
    }

    private IReadOnlyList<string> ListQueries() =>
        [.. Api.Received.Where(call => call.Method == HttpMethod.Get && call.Path == ListPath).Select(call => call.Query ?? string.Empty)];

    private string LastListQuery() => ListQueries()[^1];

    private static string ChipsOf(IRenderedComponent<Questions> page, string field) =>
        page.Find($"#{field}-field .app-mp-chips, #{field}-field .app-mp-none").TextContent;

    // -- what the page offers --------------------------------------------------------------------------------

    [Fact]
    public void Load_OffersTheSixFiltersInOrder_AndTheTextSearchStaysTheTablesOwn()
    {
        Seed();

        var page = RenderPage();
        WaitForRows(page, 3);

        var order = new[]
        {
            "questions-filter-status", "questions-filter-type", "questions-filter-difficulty",
            "questions-filter-subject", "questions-filter-organizer", "questions-filter-edition"
        };
        var markup = page.Markup;
        var positions = order.Select(id => markup.IndexOf($"id=\"{id}\"", StringComparison.Ordinal)).ToList();
        positions.Should().OnlyContain(position => position >= 0, "every filter field is on the page");
        positions.Should().BeInAscendingOrder();
        page.FindAll(".app-table-search input").Should().ContainSingle();
        page.Find("label[for='questions-filter-subject']").TextContent.Should().Be("Subject and topic");
        page.Find("label[for='questions-filter-organizer']").TextContent.Should().Be("Organizer");
    }

    [Fact]
    public void Load_NoFilter_AsksForThePlainFirstPage_AndShowsNoClearAll()
    {
        Seed();

        var page = RenderPage();
        WaitForRows(page, 3);

        page.FindAll("#questions-filter-type").Should().ContainSingle("the type filter is part of the toolbar");
        ListQueries().Single().Should().Contain("page=0").And.Contain("pageSize=25")
            .And.NotContain("status=").And.NotContain("type=").And.NotContain("difficulty=")
            .And.NotContain("topic=").And.NotContain("subject=").And.NotContain("organizer=").And.NotContain("edition=");
        page.FindAll("#questions-filter-clear").Should().BeEmpty();
    }

    [Fact]
    public void Subject_OffersEachSubjectAsAGroupWithItsTopics_AndTheOrganizerItsAcronymFirst()
    {
        Seed();
        var page = RenderPage();
        WaitForRows(page, 3);

        page.WaitForAssertion(() => page.Find("#questions-filter-subject").HasAttribute("disabled").Should().BeFalse());
        page.Find("#questions-filter-subject").Click();

        var groups = page.FindAll("#questions-filter-subject-list [role='group']");
        groups.Select(group => group.GetAttribute("aria-label")).Should().Equal("Direito Constitucional", "Português");
        var portuguese = groups.Single(group => group.GetAttribute("aria-label") == "Português");
        portuguese.QuerySelectorAll("[role='option'] .app-mp-option-name").Select(name => name.TextContent.Trim())
            .Should().Equal("Português", "Crase", "Pontuação");

        page.Find("#questions-filter-organizer").Click();
        page.FindAll("#questions-filter-organizer-list [role='option'] .app-mp-option-name")
            .Select(name => name.TextContent.Trim()).Should().Equal("CEBRASPE", "FGV");
    }

    // -- what the list asks for ------------------------------------------------------------------------------

    // AC5, BR4: several values of one filter are several keys.
    [Fact]
    public void Filter_TypeAndStatus_AreSentAsRepeatedKeys_FromTheFirstPage()
    {
        Seed();
        var page = RenderPage();
        WaitForRows(page, 3);

        Pick(page, "questions-filter-status", "Draft");
        Pick(page, "questions-filter-status", "Published");
        Pick(page, "questions-filter-type", "TrueFalse");

        page.WaitForAssertion(() =>
            LastListQuery().Should().Contain("status=Draft").And.Contain("status=Published")
                .And.Contain("type=TrueFalse").And.Contain("page=0"));
        WaitForRows(page, 1);
    }

    // AC6, BR4: "Not set" is the word none.
    [Fact]
    public void Filter_DifficultyNotSetAndFive_AreSentAsNoneAndFive()
    {
        Seed();
        var page = RenderPage();
        WaitForRows(page, 3);

        Pick(page, "questions-filter-difficulty", "none");
        Pick(page, "questions-filter-difficulty", "5");

        page.WaitForAssertion(() => LastListQuery().Should().Contain("difficulty=none").And.Contain("difficulty=5"));
        WaitForRows(page, 1); // only the published question has difficulty 5; none of the three has no difficulty
        page.Find("#questions-filter-difficulty").Click();
        page.Find("#questions-filter-difficulty-option-none .app-mp-option-name").TextContent.Should().Be("Not set");
    }

    // AC1: a topic of the taxonomy is sent by its id.
    [Fact]
    public void Filter_ATopic_IsSentAsTopic()
    {
        Seed();
        var page = RenderPage();
        WaitForRows(page, 3);

        Pick(page, "questions-filter-subject", $"t-{Punctuation.Id}");

        page.WaitForAssertion(() => LastListQuery().Should().Contain($"topic={Punctuation.Id}").And.NotContain("subject="));
        WaitForRows(page, 1);
    }

    // AC2: a whole subject is sent as the subject, and its chip says it means all its topics.
    [Fact]
    public void Filter_AWholeSubject_IsSentAsSubject_AndItsChipSaysAllTopics()
    {
        Seed();
        var page = RenderPage();
        WaitForRows(page, 3);

        Pick(page, "questions-filter-subject", $"s-{Portuguese.Id}");

        page.WaitForAssertion(() => LastListQuery().Should().Contain($"subject={Portuguese.Id}").And.NotContain("topic="));
        page.WaitForAssertion(() => ChipsOf(page, "questions-filter-subject").Should().Contain("Português (all topics)"));
    }

    // AC3: an organizer is sent by its id.
    [Fact]
    public void Filter_AnOrganizer_IsSentAsOrganizer()
    {
        Seed();
        Api.Questions[2] = FromGuard with { Status = QuestionStatus.Published };
        var page = RenderPage();
        WaitForRows(page, 3);

        Pick(page, "questions-filter-organizer", Cebraspe.Id.ToString());

        page.WaitForAssertion(() => LastListQuery().Should().Contain($"organizer={Cebraspe.Id}"));
        WaitForRows(page, 1);
    }

    // UC3, BR3: one edition, chosen by search, is sent as edition.
    [Fact]
    public async Task Filter_AnEdition_IsSentAsEdition()
    {
        Seed();
        Api.Editions.Add(GuardEdition);
        var page = RenderPage();
        WaitForRows(page, 3);

        await page.InvokeAsync(() => page.FindComponents<AppLookupField>()
            .Single(component => component.Instance.Id == "questions-filter-edition").Instance.ValueChanged
            .InvokeAsync(new AppLookupOption(GuardEdition.Id, "CONSULPLAN · Guarda Municipal · 2024", "Guarda")));

        page.WaitForAssertion(() => LastListQuery().Should().Contain($"edition={GuardEdition.Id}").And.Contain("page=0"));
        WaitForRows(page, 1);
    }

    // AC7: filters and the table's own search go together.
    [Fact]
    public void Filter_AndSearch_AreSentTogether()
    {
        Seed();
        var page = RenderPage();
        WaitForRows(page, 3);
        Pick(page, "questions-filter-status", "Published");
        page.WaitForAssertion(() => LastListQuery().Should().Contain("status=Published"));

        page.Find(".app-table-search input").Input("vírgula");
        AdvanceDebounce();

        page.WaitForAssertion(() => LastListQuery().Should().Contain("status=Published").And.Contain("search=v%C3%ADrgula"));
    }

    // -- the address ----------------------------------------------------------------------------------------

    // AC9, BR8: the filters are written to the address without a new history entry.
    [Fact]
    public void Filter_IsWrittenToTheAddress()
    {
        Seed();
        var page = RenderPage();
        WaitForRows(page, 3);

        Pick(page, "questions-filter-status", "Published");
        Pick(page, "questions-filter-subject", $"t-{Crase.Id}");

        page.WaitForAssertion(() =>
            Navigation.Uri.Should().Contain("status=Published").And.Contain($"topic={Crase.Id}"));
    }

    // AC9: a list opened from an address applies it and shows it in the pickers.
    [Fact]
    public void Address_WithFilters_IsAppliedAndShownInThePickers()
    {
        Seed();
        Navigation.NavigateTo($"/admin/questions?status=Published&type=TrueFalse&organizer={Cebraspe.Id}&difficulty=none&search=guarda");

        var page = RenderPage();

        page.WaitForAssertion(() =>
            ListQueries().Should().Contain(query => query.Contains("status=Published", StringComparison.Ordinal)
                && query.Contains("type=TrueFalse", StringComparison.Ordinal)
                && query.Contains($"organizer={Cebraspe.Id}", StringComparison.Ordinal)
                && query.Contains("difficulty=none", StringComparison.Ordinal)
                && query.Contains("search=guarda", StringComparison.Ordinal)));
        page.WaitForAssertion(() =>
        {
            ChipsOf(page, "questions-filter-status").Should().Contain("Published");
            ChipsOf(page, "questions-filter-type").Should().Contain("True/false");
            ChipsOf(page, "questions-filter-organizer").Should().Contain("CEBRASPE");
            ChipsOf(page, "questions-filter-difficulty").Should().Contain("Not set");
        });
        page.Find(".app-table-search input").GetAttribute("value").Should().Be("guarda");
    }

    // AC10, BR8: a topic that no longer exists is dropped without an error.
    [Fact]
    public void Address_WithATopicThatNoLongerExists_LoadsAndDropsIt()
    {
        Seed();
        var gone = Guid.CreateVersion7();
        Navigation.NavigateTo($"/admin/questions?topic={gone}&topic={Crase.Id}");

        var page = RenderPage();

        page.WaitForAssertion(() => ChipsOf(page, "questions-filter-subject").Should().Contain("Crase"));
        ChipsOf(page, "questions-filter-subject").Should().NotContain(gone.ToString());
        page.WaitForAssertion(() => LastListQuery().Should().Contain($"topic={Crase.Id}").And.NotContain(gone.ToString()));
        page.FindAll(".app-alert, [role='alert']").Should().BeEmpty();
    }

    // -- chips, clear all, errors, limit --------------------------------------------------------------------

    // AC11: one chip per picked value; removing one reloads from the first page without it.
    [Fact]
    public void Chips_OnePerPickedValue_AndRemovingOneReloadsWithoutIt()
    {
        Seed();
        var page = RenderPage();
        WaitForRows(page, 3);
        Pick(page, "questions-filter-status", "Draft");
        Pick(page, "questions-filter-status", "Published");
        page.WaitForAssertion(() => page.FindAll("#questions-filter-status-field .app-mp-chips li").Should().HaveCount(2));

        page.Find($"#{AppMultiPickField.RemoveButtonId("questions-filter-status", "Draft")}").Click();

        page.WaitForAssertion(() =>
        {
            page.FindAll("#questions-filter-status-field .app-mp-chips li").Should().ContainSingle();
            LastListQuery().Should().Contain("status=Published").And.NotContain("status=Draft").And.Contain("page=0");
        });
    }

    // AC11: the page-level "Clear all" removes every filter and the search.
    [Fact]
    public void ClearAll_RemovesEveryFilterAndTheSearch_AndHidesItself()
    {
        Seed();
        var page = RenderPage();
        WaitForRows(page, 3);
        Pick(page, "questions-filter-status", "Published");
        Pick(page, "questions-filter-type", "TrueFalse");
        page.Find(".app-table-search input").Input("guarda");
        AdvanceDebounce();
        page.WaitForAssertion(() => LastListQuery().Should().Contain("status=Published").And.Contain("search=guarda"));
        page.Find("#questions-filter-clear").TextContent.Should().Contain("Clear all");

        page.Find("#questions-filter-clear").Click();

        page.WaitForAssertion(() =>
        {
            LastListQuery().Should().NotContain("status=").And.NotContain("type=").And.NotContain("search=");
            page.Find(".app-table-search input").GetAttribute("value").Should().BeNullOrEmpty();
            page.FindAll("#questions-filter-clear").Should().BeEmpty();
            Navigation.Uri.Should().NotContain("status=").And.NotContain("search=");
        });
    }

    [Fact]
    public void ClearAll_IsShownWhileOnlyTheSearchIsSet()
    {
        Seed();
        var page = RenderPage();
        WaitForRows(page, 3);

        page.Find(".app-table-search input").Input("guarda");
        AdvanceDebounce();

        page.WaitForAssertion(() => page.FindAll("#questions-filter-clear").Should().ContainSingle());
    }

    // The empty state says it is the filters, not an empty bank (F-95 text, F-100 wording).
    [Fact]
    public void Empty_WithFilters_SaysNothingMatchesTheFilters()
    {
        Api.Questions.Add(Draft);
        var page = RenderPage();
        WaitForRows(page, 1);

        Pick(page, "questions-filter-type", "TrueFalse");

        page.WaitForAssertion(() => page.Markup.Should().Contain("No question matches the chosen filters."));
    }

    // AC12: a taxonomy that does not load shows its error with try again; the list still loads.
    [Fact]
    public void Taxonomy_FailsToLoad_ShowsItsErrorWithTryAgain_AndTheListStillLoads()
    {
        Seed();
        Api.TaxonomyFails = true;

        var page = RenderPage();

        WaitForRows(page, 3);
        page.WaitForAssertion(() =>
        {
            page.Find("#questions-filter-subject-description").TextContent.Should().Contain("did not load");
            page.Find("#questions-filter-subject-description button").TextContent.Should().Contain("Try again");
        });

        Api.TaxonomyFails = false;
        page.Find("#questions-filter-subject-description button").Click();

        page.WaitForAssertion(() => page.Find("#questions-filter-subject").HasAttribute("disabled").Should().BeFalse());
        page.FindAll("#questions-filter-status").Should().ContainSingle("the other filters kept working");
    }

    // AC15, change note v2: a hundred picked ids across subject, topic and organizer is the limit.
    [Fact]
    public void Limit_AtAHundredPickedIds_TheOtherOptionsAreDisabledAndTheHintSaysWhy()
    {
        Seed();
        var topics = Enumerable.Range(0, 101)
            .Select(index => new TopicResponse(Guid.CreateVersion7(), Portuguese.Id, $"Tópico {index:000}"))
            .ToList();
        Api.Topics.AddRange(topics);
        var picked = topics.Take(100).Select(topic => $"topic={topic.Id}");
        Navigation.NavigateTo($"/admin/questions?{string.Join('&', picked)}");

        var page = RenderPage();
        page.WaitForAssertion(() => page.Find("#questions-filter-subject").HasAttribute("disabled").Should().BeFalse());
        page.Find("#questions-filter-subject").Click();

        var next = page.Find($"#questions-filter-subject-option-t-{topics[100].Id}");
        next.GetAttribute("aria-disabled").Should().Be("true");
        page.Find($"#questions-filter-subject-option-t-{topics[0].Id}").GetAttribute("aria-disabled").Should().BeNull();
        page.Find($"#questions-filter-subject-option-s-{Law.Id}").GetAttribute("aria-disabled").Should().Be("true");
        page.Find("#questions-filter-subject-description").TextContent.Should().Contain("100");
        page.Find("#questions-filter-organizer").Click();
        page.Find($"#questions-filter-organizer-option-{Cebraspe.Id}").GetAttribute("aria-disabled").Should().Be("true");
        page.Markup.Should().NotContain("aria-disabled=\"true\" aria-describedby=\"\"");
    }

    // -- the form keeps the filters --------------------------------------------------------------------------

    // AC9: Add and Edit go to the form carrying the filters; the form's Cancel brings them back.
    [Fact]
    public void Add_FromAFilteredList_CarriesTheFiltersToTheForm()
    {
        Seed();
        Navigation.NavigateTo($"/admin/questions?status=Published&topic={Crase.Id}");
        var page = RenderPage();
        WaitForRows(page, 1); // the guard question: published, in the topic Crase

        page.Find(".app-primary-action").Click();

        page.WaitForAssertion(() =>
            Navigation.Uri.Should().Contain("/admin/questions/new").And.Contain("status=Published").And.Contain($"topic={Crase.Id}"));
    }

    [Fact]
    public void Edit_FromAFilteredList_CarriesTheFiltersToTheForm()
    {
        Seed();
        Navigation.NavigateTo($"/admin/questions?status=Draft");
        var page = RenderPage();
        WaitForRows(page, 1); // the only draft left after the seed

        page.Find($"#question-actions-{Draft.Id}-edit").Click();

        page.WaitForAssertion(() =>
            Navigation.Uri.Should().Contain($"/admin/questions/{Draft.Id}").And.Contain("status=Draft"));
    }

    [Fact]
    public void Form_Cancel_ReturnsToTheListWithTheFiltersItWasOpenedWith()
    {
        Api.Questions.Add(Draft);
        Navigation.NavigateTo($"/admin/questions/{Draft.Id}?status=Draft&topic={Crase.Id}&utm=1");
        var form = Render<QuestionForm>(parameters => parameters.Add(component => component.Id, Draft.Id));
        form.WaitForAssertion(() => form.FindAll(".app-form-cancel").Should().ContainSingle());

        form.Find(".app-form-cancel").Click();

        form.WaitForAssertion(() =>
        {
            var target = new Uri(Navigation.Uri);
            target.AbsolutePath.Should().Be("/admin/questions");
            target.Query.Should().Contain("status=Draft").And.Contain($"topic={Crase.Id}").And.NotContain("utm");
        });
    }
}
