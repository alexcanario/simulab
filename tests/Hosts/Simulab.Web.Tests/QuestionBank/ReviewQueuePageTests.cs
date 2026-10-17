using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-105 `/admin/questions/review` (AC10 screen part, UC3, BR8): the questions waiting for a decision, the oldest
/// latest send first, paged on the server, with one Review action per row and every state the screen design lists.
/// </summary>
public sealed class ReviewQueuePageTests : QuestionPageTestContext
{
    private const string QueueRoute = "/api/v1/question-bank/review-queue";

    private static readonly DateTimeOffset First = new(2026, 10, 16, 14, 2, 0, TimeSpan.Zero);

    private static QuestionResponse InReviewOf(QuestionResponse source, int number, DateTimeOffset sentAt) =>
        source with
        {
            Id = Guid.Parse($"0198f5a0-0000-7000-8000-0000000009{number:00}"),
            Status = QuestionStatus.InReview,
            CurrentVersion = null,
            SubmittedAt = sentAt
        };

    // Sent second, listed second.
    private static readonly QuestionResponse Later = InReviewOf(Published, 2, First.AddHours(2));

    // Sent first, listed first, although it was added last.
    private static readonly QuestionResponse Earlier = InReviewOf(Draft, 1, First);

    private IRenderedComponent<ReviewQueue> RenderPage() => Render<ReviewQueue>();

    private static void WaitForRows(IRenderedComponent<ReviewQueue> page, int count) =>
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(count));

    private static IReadOnlyList<string> CellsOf(IRenderedComponent<ReviewQueue> page, int column) =>
        [.. page.FindAll("tbody tr").Select(row => row.QuerySelectorAll("td")[column].TextContent.Trim())];

    private static string Action(Guid id) => $"#review-queue-actions-{id}-review";

    private void SeedTwo()
    {
        Api.Questions.Add(Later);
        Api.Questions.Add(Earlier);
        Api.Questions.Add(Draft);
    }

    // The page renders with its real route parameters and the kit components it names (a Razor attribute mistake compiles).
    [Fact]
    public void Render_ShowsTheTitleTheBreadcrumbsAndTheTableWithoutSearchOrAPrimaryAction()
    {
        SeedTwo();

        var page = RenderPage();

        WaitForRows(page, 2);
        page.Find("h1").TextContent.Should().Be("Review queue");
        page.FindAll(".app-breadcrumbs").Should().ContainSingle();
        page.Find(".app-breadcrumbs").TextContent.Should().Contain("Content").And.Contain("Review queue");
        page.FindAll(".app-table-search").Should().BeEmpty("the queue has no search");
        page.FindAll(".app-primary-action").Should().BeEmpty("nothing is created from the queue");
        page.FindAll(".sortable-column-header").Should().BeEmpty("the order is the Api's: oldest latest send first");
    }

    // AC10: only the questions in review, oldest latest send first, with the five columns of the design.
    [Fact]
    public void Load_ShowsOnlyTheQuestionsInReview_OldestSendFirst_WithTheFiveColumns()
    {
        SeedTwo();

        var page = RenderPage();

        WaitForRows(page, 2);
        page.FindAll("thead th").Select(header => header.TextContent.Trim()).Take(5)
            .Should().Equal("Statement", "Topic", "Type", "Language", "Sent on");
        CellsOf(page, 0).Should().Equal("Qual frase usa a crase corretamente?", "Onde entra a vírgula?");

        var topicCell = page.FindAll("tbody tr")[0].QuerySelectorAll("td")[1];
        topicCell.TextContent.Should().Contain("Crase").And.Contain("Português");
        topicCell.QuerySelector(".app-cell-secondary")!.TextContent.Should().Contain("Português");

        CellsOf(page, 2).Should().Equal("Single choice", "Single choice");
        // The language is in its own name and marked with its own tag.
        CellsOf(page, 3)[0].Should().Be("Português (Brasil)");
        page.FindAll("tbody tr")[0].QuerySelectorAll("td")[3].QuerySelector("[lang]")!.GetAttribute("lang").Should().Be("pt-BR");
        page.FindAll("tbody tr")[1].QuerySelectorAll("td")[3].QuerySelector("[lang]")!.GetAttribute("lang").Should().Be("pt-PT");

        // The time of the latest send, in the culture's short format, as text and not as a chip.
        CellsOf(page, 4).Should().Equal(
            First.ToString("g", System.Globalization.CultureInfo.CurrentCulture),
            First.AddHours(2).ToString("g", System.Globalization.CultureInfo.CurrentCulture));
        page.FindAll("tbody tr td:nth-child(5) .mud-chip").Should().BeEmpty();
        page.FindAll("tbody tr td:nth-child(5) .app-cell-nowrap").Should().HaveCount(2);
    }

    // AC10: the queue is paged on the server with the kit's page size, and a later page is asked for.
    [Fact]
    public void Load_AsksTheServerForTheFirstPage_AndAskForTheNextOneWhenTheReaderPages()
    {
        foreach (var number in Enumerable.Range(1, 30))
        {
            Api.Questions.Add(InReviewOf(Draft, number, First.AddMinutes(number)));
        }

        var page = RenderPage();

        WaitForRows(page, 25);
        var first = Api.Received.First(call => call.Method == HttpMethod.Get && call.Path == QueueRoute);
        first.Query.Should().Contain("page=0").And.Contain("pageSize=25").And.NotContain("search=");

        var grid = page.FindComponent<MudBlazor.MudDataGrid<ReviewQueueItemResponse>>();
        page.InvokeAsync(() => grid.Instance.NavigateTo(MudBlazor.Page.Next));

        page.WaitForAssertion(() => Api.Received.Last(call => call.Path == QueueRoute).Query.Should().Contain("page=1").And.Contain("pageSize=25"));
        WaitForRows(page, 5);
    }

    // The row has one action, Review, with an accessible name that carries the question; no Edit and no Delete.
    [Fact]
    public void Row_HasOnlyTheReviewAction_NamedAfterTheQuestion()
    {
        SeedTwo();
        var page = RenderPage();
        WaitForRows(page, 2);

        var row = page.FindAll("tbody tr")[0];
        row.QuerySelectorAll(".app-row-action").Should().ContainSingle();
        var button = page.Find(Action(Earlier.Id));
        button.GetAttribute("aria-label").Should().Be("Review: question \"Qual frase usa a crase corretamente?\"");
        page.FindAll($"#review-queue-actions-{Earlier.Id}-edit, #review-queue-actions-{Earlier.Id}-delete").Should().BeEmpty();
    }

    [Fact]
    public void RowAction_Review_OpensTheReviewScreenOfThatQuestion()
    {
        SeedTwo();
        var page = RenderPage();
        WaitForRows(page, 2);

        page.Find(Action(Earlier.Id)).Click();

        page.WaitForAssertion(() =>
            Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith($"/admin/questions/review/{Earlier.Id}"));
    }

    // The excerpt is a link to the same screen, and the link is the cell's only tab stop.
    [Fact]
    public void Excerpt_IsALinkToTheReviewScreen_AndTheOnlyTabStopOfItsCell()
    {
        SeedTwo();
        var page = RenderPage();
        WaitForRows(page, 2);

        var cell = page.FindAll("tbody tr")[0].QuerySelectorAll("td")[0];
        var link = cell.QuerySelector("a.app-link")!;
        link.GetAttribute("href").Should().Be($"/admin/questions/review/{Earlier.Id}");
        link.GetAttribute("lang").Should().Be("pt-BR");
        cell.QuerySelectorAll("span[tabindex]").Should().BeEmpty("the link, not a focusable span, is the cell's tab stop");
    }

    // Long text: the excerpt and the topic are cut by the kit with a width cap, the whole text is the tooltip,
    // and the accessible name of the row's button stays short.
    [Fact]
    public void Row_LongText_IsTruncatedByTheKit_AndTheActionNameStaysShort()
    {
        var statement = string.Join(' ', Enumerable.Repeat("enunciado", 60));
        Api.Questions.Add(Earlier with { StatementHtml = $"<p>{statement}</p>", TopicName = new string('t', 200) });

        var page = RenderPage();
        WaitForRows(page, 1);

        var excerpt = page.Find("tbody tr td a.app-truncate");
        excerpt.GetAttribute("style").Should().Be("max-width: 26rem");
        excerpt.TextContent.Should().Be(statement);
        var topic = page.FindAll("tbody tr td")[1].QuerySelector(".app-truncate")!;
        topic.GetAttribute("style").Should().Be("max-width: 14rem");
        topic.TextContent.Should().HaveLength(200);

        var label = page.Find(Action(Earlier.Id)).GetAttribute("aria-label")!;
        label.Should().StartWith("Review: question \"enunciado enunciado").And.Contain("…");
        label.Length.Should().BeLessThan(100);
    }

    // AC10: an empty queue shows the empty state, with no action.
    [Fact]
    public void Load_NothingInReview_ShowsTheEmptyStateWithoutAnAction()
    {
        Api.Questions.Add(Draft);
        Api.Questions.Add(Published);

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Nothing is waiting for review."));
        page.FindAll("tbody tr").Where(row => row.QuerySelector("a.app-link") is not null).Should().BeEmpty();
        page.FindAll(".app-state button").Should().BeEmpty("nothing is created from the queue");
    }

    [Fact]
    public void Load_ApiFails_ShowsTheErrorStateWithTryAgain_AndTryingAgainAsksAgain()
    {
        Api.QueueFailure = (HttpStatusCode.InternalServerError, "common.unexpected_error");

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("We could not load this list."));
        var calls = Api.QueueCalls;
        Api.QueueFailure = null;
        Api.Questions.Add(Earlier);

        page.Find(".app-retry").Click();

        page.WaitForAssertion(() => Api.QueueCalls.Should().BeGreaterThan(calls));
        WaitForRows(page, 1);
    }
}
