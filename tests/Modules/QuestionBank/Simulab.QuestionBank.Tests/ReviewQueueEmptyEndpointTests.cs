using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Tests;

/// <summary>F-105 AC10, UC3: with nothing in review the queue is empty (a class of its own, so its database is).</summary>
public sealed class ReviewQueueEmptyEndpointTests : QuestionReviewApiTests
{
    [Fact]
    public async Task Queue_WithNothingInReview_IsEmpty()
    {
        var reviewer = await HoldingOnlyAsync(QuestionBankPermissions.Review);

        var page = await QueueAsync(reviewer);

        page.Items.Should().BeEmpty();
        page.Total.Should().Be(0);
    }
}
