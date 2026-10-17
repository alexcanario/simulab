using System.Net;
using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-105 AC10 through HTTP: the review queue lists only the questions in review, oldest latest send first,
/// paginated. The tests of a class share a database, so each looks only at the rows it made.
/// </summary>
public sealed class ReviewQueueEndpointTests : QuestionReviewApiTests
{
    // AC10, BR8: only the ones in review, oldest latest send first; a question rejected and sent again goes behind.
    [Fact]
    public async Task Queue_ListsOnlyQuestionsInReview_OldestLatestSendFirst()
    {
        var admin = await AdminAsync();
        var reviewer = await HoldingOnlyAsync(QuestionBankPermissions.Review);
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var first = await InReviewAsync(admin, topic.Id, $"<p>Primeira {marker}</p>");
        Tick();
        var second = await InReviewAsync(admin, topic.Id, $"<p>Segunda {marker}</p>");
        Tick();
        var withdrawn = await InReviewAsync(admin, topic.Id, $"<p>Retirada {marker}</p>");
        Tick();
        await WithdrawAsync(admin, withdrawn.Id);
        var draft = await CreatedAsync(admin, Complete(topic.Id, $"<p>Rascunho {marker}</p>"));
        var published = await PublishedAsync(admin, topic.Id);
        Tick();
        (await RejectAsync(reviewer, first.Id, "Revise a chave.")).StatusCode.Should().Be(HttpStatusCode.OK);
        Tick();
        await SubmittedAsync(admin, first.Id);

        var page = await QueueAsync(reviewer, "?pageSize=100");

        var mine = page.Items.Where(item => new[] { first.Id, second.Id, withdrawn.Id, draft.Id, published.Id }.Contains(item.Id)).ToList();
        mine.Select(item => item.Id).Should().Equal(second.Id, first.Id);
        mine.Select(item => item.SubmittedAt).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        page.Total.Should().BeGreaterThanOrEqualTo(2);
        var latest = await GetAsync(admin, first.Id);
        mine[1].SubmittedAt.Should().Be(latest.SubmittedAt);
    }

    // AC10: the row carries what the table draws.
    [Fact]
    public async Task Queue_Row_CarriesTheExcerptTheTopicTheSubjectTheTypeAndTheLanguage()
    {
        var admin = await AdminAsync();
        var reviewer = await HoldingOnlyAsync(QuestionBankPermissions.Review);
        var topic = await TopicAsync(admin);
        var question = await InReviewAsync(admin, topic.Id, "<p>Qual é a <strong>capital</strong> da França?</p>");

        var row = (await QueueAsync(reviewer, "?pageSize=100")).Items.Single(item => item.Id == question.Id);

        row.StatementExcerpt.Should().Be("Qual é a capital da França?");
        row.TopicName.Should().Be(topic.Name);
        row.SubjectName.Should().Be(question.SubjectName);
        row.Type.Should().Be(QuestionType.SingleChoice);
        row.ContentLanguage.Should().Be("pt-BR");
        row.Difficulty.Should().BeNull();
    }

    // AC10, BR8: paginated, zero-based, the size capped at 100.
    [Fact]
    public async Task Queue_IsPaginated_AndThePageSizeIsCappedAt100()
    {
        var admin = await AdminAsync();
        var reviewer = await HoldingOnlyAsync(QuestionBankPermissions.Review);
        var topic = await TopicAsync(admin);
        for (var index = 0; index < 3; index++)
        {
            await InReviewAsync(admin, topic.Id, $"<p>Paginada {index} {Marker()}</p>");
            Tick();
        }

        var firstPage = await QueueAsync(reviewer, "?page=0&pageSize=2");
        var secondPage = await QueueAsync(reviewer, "?page=1&pageSize=2");
        var huge = await QueueAsync(reviewer, "?page=0&pageSize=1000");

        firstPage.Items.Should().HaveCount(2);
        secondPage.Items.Should().NotBeEmpty();
        firstPage.Total.Should().BeGreaterThanOrEqualTo(3).And.Be(secondPage.Total);
        firstPage.Items.Select(item => item.Id).Should().NotIntersectWith(secondPage.Items.Select(item => item.Id));
        huge.Items.Count.Should().BeLessThanOrEqualTo(QuestionListQuery.MaxPageSize);
    }
}
