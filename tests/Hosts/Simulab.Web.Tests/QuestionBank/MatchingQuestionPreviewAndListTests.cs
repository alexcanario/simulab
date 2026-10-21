using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-106 AC12 and AC14: the list names the fourth type, and the preview shows the numbered and the lettered columns
/// with the task line, the key and the explanation hidden until "show answer"; the version preview carries the left
/// items of the version (architect #2).
/// </summary>
public sealed class MatchingQuestionPreviewAndListTests : QuestionPageTestContext
{
    private static readonly DateTimeOffset Recorded = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    // 1→C, 2→A; B is extra.
    private static readonly QuestionResponse MatchingPublished = new(
        Guid.Parse("0198f5a0-0000-7000-8000-000000000080"),
        QuestionType.Matching,
        QuestionStatus.Published,
        Crase.Id,
        Crase.Name,
        Portuguese.Id,
        Portuguese.Name,
        "pt-BR",
        "<p>Associe cada país à sua capital.</p>",
        null,
        "<p>Capitais europeias.</p>",
        3,
        [
            new(0, "<p>Lisboa</p>", false),
            new(1, "<p>Madri</p>", false),
            new(2, "<p>Paris</p>", false)
        ],
        CurrentVersion: 1,
        MatchItems:
        [
            new(0, "<p>França</p>", 2),
            new(1, "<p>Portugal</p>", 0)
        ]);

    private async Task<IRenderedComponent<MudDialogProvider>> OpenAsync(QuestionResponse question)
    {
        Render<MudPopoverProvider>();
        var provider = Render<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<QuestionPreviewDialog>
        {
            { dialog => dialog.Question, question },
            { dialog => dialog.UnsavedChanges, false }
        };

        await provider.InvokeAsync(async () => await dialogs.ShowAsync<QuestionPreviewDialog>(
            "Preview", parameters, new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseOnEscapeKey = true }));
        provider.WaitForAssertion(() => provider.FindAll("#question-preview-statement").Should().ContainSingle());

        return provider;
    }

    // AC14, BR13: the task line, then the numbered items and the lettered matches in authored order.
    [Fact]
    public async Task Preview_Matching_ShowsTheTaskLineAndBothColumns()
    {
        var dialog = await OpenAsync(MatchingPublished);

        dialog.Find("#question-preview-match-instruction").TextContent.Trim().Should().Be("Match each number with one letter.");
        dialog.FindAll("#question-preview-match-items li .app-match-number").Select(number => number.TextContent.Trim())
            .Should().Equal("1", "2");
        dialog.FindAll("#question-preview-match-items li").Select(item => item.TextContent)
            .Should().SatisfyRespectively(
                first => first.Should().Contain("França"),
                second => second.Should().Contain("Portugal"));
        dialog.FindAll("ol.app-question-options .app-option-letter").Select(letter => letter.TextContent)
            .Should().Equal("A", "B", "C");
        var markup = dialog.Markup;
        markup.IndexOf("question-preview-match-instruction", StringComparison.Ordinal)
            .Should().BeGreaterThan(markup.IndexOf("question-preview-statement", StringComparison.Ordinal));
    }

    // AC14: nothing of the key shows before the toggle.
    [Fact]
    public async Task Preview_Matching_BeforeShowAnswer_HidesTheKeyAndTheExplanation()
    {
        var dialog = await OpenAsync(MatchingPublished);

        dialog.FindAll("#question-preview-match-items li").Should().HaveCount(2);
        dialog.FindAll(".is-correct").Should().BeEmpty();
        dialog.FindAll(".app-match-key").Should().BeEmpty();
        dialog.Markup.Should().NotContain("Correct answer").And.NotContain("Capitais europeias.").And.NotContain("→");
    }

    // AC14: after the reveal each number shows its letter and each matched right item is marked; an extra one is not.
    [Fact]
    public async Task Preview_Matching_ShowAnswer_ShowsTheKeyAndMarksTheMatchedItemsWithIconTextAndBorder()
    {
        var dialog = await OpenAsync(MatchingPublished);

        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.FindAll("ol.app-question-options li.is-correct").Should().HaveCount(2));
        dialog.FindAll("ol.app-question-options li.is-correct")
            .Select(item => item.QuerySelector(".app-option-letter")!.TextContent).Should().Equal("A", "C");
        dialog.FindAll("ol.app-question-options li.is-correct").Should().OnlyContain(item =>
            item.TextContent.Contains("Correct answer") && item.QuerySelector(".app-correct-label svg") != null);
        dialog.FindAll(".app-match-key").Select(key => key.TextContent.Trim()).Should().Equal("1 → C", "2 → A");
        dialog.Find("#question-preview-key").TextContent.Should().Contain("1 → C").And.Contain("2 → A");
        dialog.Find("#question-preview-explanation").TextContent.Should().Contain("Capitais europeias.");
    }

    [Fact]
    public async Task Preview_MatchingDraftWithNoMatches_SaysThereIsNoKeyYet()
    {
        var unmatched = MatchingPublished with
        {
            Status = QuestionStatus.Draft,
            MatchItems = [.. MatchingPublished.MatchItems!.Select(item => item with { MatchPosition = null })]
        };

        var dialog = await OpenAsync(unmatched);
        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.FindAll(".app-preview-no-key").Should().ContainSingle());
        dialog.Find(".app-preview-no-key").TextContent.Should().Be("This draft has no match set yet.");
        dialog.FindAll("#question-preview-match-items li").Should().HaveCount(2);
        dialog.FindAll(".is-correct").Should().BeEmpty();
    }

    // AC9, AC14 (architect #2): the version preview shows the left items the version recorded.
    [Fact]
    public void VersionPreview_Matching_ShowsTheLeftItemsOfTheVersion()
    {
        var providers = RenderProviders();
        Api.Questions.Add(MatchingPublished);
        Api.Versions[MatchingPublished.Id] =
        [
            new QuestionVersionResponse(
                Guid.Parse("0198f5a0-0000-7000-8000-000000000981"),
                MatchingPublished.Id,
                1,
                QuestionType.Matching,
                "pt-BR",
                MatchingPublished.StatementHtml,
                null,
                null,
                MatchingPublished.Options,
                null,
                false,
                null,
                Recorded,
                false,
                [new(0, "<p>Itália</p>", 2), new(1, "<p>Espanha</p>", 1)])
        ];
        var page = Render<QuestionForm>(parameters => parameters.Add(form => form.Id, MatchingPublished.Id));
        page.WaitForElement("#question-version-actions-1-view");

        page.Find("#question-version-actions-1-view").Click();

        providers.Dialogs.WaitForAssertion(() =>
            providers.Dialogs.FindAll("#question-preview-match-items li").Should().HaveCount(2));
        providers.Dialogs.Find("#question-preview-match-items").TextContent.Should().Contain("Itália").And.Contain("Espanha");
    }

    // AC12, BR11: the type chip of the list says it in text.
    [Fact]
    public void List_NamesTheMatchingType()
    {
        Api.Questions.Add(Draft);
        Api.Questions.Add(MatchingPublished);

        var page = Render<Questions>();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));
        page.FindAll("tbody tr")
            .Select(row => row.QuerySelector("td .app-chip")!.TextContent.Trim())
            .Should().Equal("Single choice", "Matching");
    }
}
