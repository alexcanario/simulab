using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Testing.ApiHost;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-105 through HTTP, on a real PostgreSQL: approving (AC5), rejecting (AC6), the refusals of a decision and the
/// send the reviewer saw (AC7, change note v2), the person who sent also deciding (AC8), and the review detail (AC10).
/// </summary>
public sealed class QuestionReviewDecisionEndpointTests : QuestionReviewApiTests
{
    private static async Task<IReadOnlyList<QuestionVersionListItemResponse>> VersionsAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<List<QuestionVersionListItemResponse>>($"{Questions}/{id}/versions", AppJson.Options))!;

    private async Task<(HttpClient Admin, HttpClient Reviewer, TopicResponse Topic, QuestionResponse Question)> InReviewWithAReviewerAsync()
    {
        var admin = await AdminAsync();
        var reviewer = await HoldingOnlyAsync(QuestionBankPermissions.Review);
        var topic = await TopicAsync(admin);
        var question = await InReviewAsync(admin, topic.Id);
        Tick();

        return (admin, reviewer, topic, question);
    }

    // AC5, UC4, BR5: published, version 1 holds the content, the entry holds the trimmed note.
    [Fact]
    public async Task Approve_AQuestionInReview_PublishesIt_WithVersionOneAndTheTrimmedNote()
    {
        var (admin, reviewer, _, question) = await InReviewWithAReviewerAsync();

        var approved = await AsQuestionAsync(await ApproveAsync(reviewer, question.Id, "  Conferida com o edital.  "));

        approved.Status.Should().Be(QuestionStatus.Published);
        approved.CurrentVersion.Should().Be(1);
        approved.SubmittedAt.Should().BeNull();
        approved.StatementHtml.Should().Be(question.StatementHtml);
        var versions = await VersionsAsync(admin, question.Id);
        versions.Should().ContainSingle();
        versions[0].Number.Should().Be(1);
        var entries = await ReviewsAsync(admin, question.Id);
        entries.Select(entry => entry.Kind).Should().Equal(QuestionReviewKind.Submitted, QuestionReviewKind.Approved);
        entries[1].Reason.Should().Be("Conferida com o edital.");
        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.Published);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Approve_WithNoBodyOrABlankNote_Publishes_AndTheEntryHoldsNoNote(string? note)
    {
        var (admin, reviewer, _, question) = await InReviewWithAReviewerAsync();

        var approved = await AsQuestionAsync(await ApproveAsync(reviewer, question.Id, note));

        approved.Status.Should().Be(QuestionStatus.Published);
        (await ReviewsAsync(admin, question.Id))[1].Reason.Should().BeNull();
    }

    // AC5: 500 characters are accepted, 501 answer 400 and change nothing.
    [Fact]
    public async Task Approve_ANoteOf500Characters_IsAccepted_And501AreRefused_ChangingNothing()
    {
        var (admin, reviewer, topic, question) = await InReviewWithAReviewerAsync();
        var other = await InReviewAsync(admin, topic.Id);

        await AssertRefusedAsync(
            await ApproveAsync(reviewer, question.Id, new string('a', QuestionLimits.ChangeReasonMaxLength + 1)),
            HttpStatusCode.BadRequest,
            QuestionBankErrorCodes.QuestionReviewReasonTooLong);

        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.InReview);
        (await ReviewsAsync(admin, question.Id)).Should().ContainSingle();
        (await VersionsAsync(admin, question.Id)).Should().BeEmpty();
        (await ApproveAsync(reviewer, other.Id, new string('a', QuestionLimits.ChangeReasonMaxLength))).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // OP1 of the design pass, BR5: the note is the reason of the version when the answer key moved since the last one.
    [Fact]
    public async Task Approve_WhenTheKeyDiffersFromTheLatestVersion_NeedsTheNoteAsTheReason()
    {
        var admin = await AdminAsync();
        var reviewer = await HoldingOnlyAsync(QuestionBankPermissions.Review);
        var topic = await TopicAsync(admin);
        var published = await PublishedAsync(admin, topic.Id);
        (await admin.PostAsync($"{Questions}/{published.Id}/unpublish", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.PutAsJsonAsync(
            $"{Questions}/{published.Id}",
            Complete(topic.Id) with { Options = [new SaveQuestionOptionRequest("<p>Paris</p>"), new SaveQuestionOptionRequest("<p>Lyon</p>", true)] },
            AppJson.Options)).StatusCode.Should().Be(HttpStatusCode.OK);
        await SubmittedAsync(admin, published.Id);

        await AssertRefusedAsync(
            await ApproveAsync(reviewer, published.Id, null),
            HttpStatusCode.BadRequest,
            QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);

        (await GetAsync(admin, published.Id)).Status.Should().Be(QuestionStatus.InReview);
        var approved = await AsQuestionAsync(await ApproveAsync(reviewer, published.Id, "A chave estava errada."));
        approved.CurrentVersion.Should().Be(2);
        var versions = await VersionsAsync(admin, published.Id);
        versions[0].AnswerKeyChanged.Should().BeTrue();
        versions[0].Reason.Should().Be("A chave estava errada.");
    }

    // AC6, UC5, BR6: a draft again, with the trimmed reason in the history.
    [Fact]
    public async Task Reject_AQuestionInReview_BecomesADraft_WithTheTrimmedReasonInTheHistory()
    {
        var (admin, reviewer, _, question) = await InReviewWithAReviewerAsync();

        var rejected = await AsQuestionAsync(await RejectAsync(reviewer, question.Id, "  A chave da C contradiz a explicação.  "));

        rejected.Status.Should().Be(QuestionStatus.Draft);
        rejected.SubmittedAt.Should().BeNull();
        var entries = await ReviewsAsync(admin, question.Id);
        entries.Select(entry => entry.Kind).Should().Equal(QuestionReviewKind.Submitted, QuestionReviewKind.Rejected);
        entries[1].Reason.Should().Be("A chave da C contradiz a explicação.");
        (await VersionsAsync(admin, question.Id)).Should().BeEmpty();
    }

    // AC6: a missing body, a blank reason and 501 characters answer 400 and the question stays in review; 500 is accepted.
    [Fact]
    public async Task Reject_WithAMissingBlankOrOverLongReason_IsRefused_AndItStaysInReview()
    {
        var (admin, reviewer, topic, question) = await InReviewWithAReviewerAsync();
        var other = await InReviewAsync(admin, topic.Id);

        await AssertRefusedAsync(await RejectAsync(reviewer, question.Id, null), HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionReviewReasonRequired);
        await AssertRefusedAsync(await RejectAsync(reviewer, question.Id, "   "), HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionReviewReasonRequired);
        await AssertRefusedAsync(
            await RejectAsync(reviewer, question.Id, new string('a', QuestionLimits.ChangeReasonMaxLength + 1)),
            HttpStatusCode.BadRequest,
            QuestionBankErrorCodes.QuestionReviewReasonTooLong);

        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.InReview);
        (await ReviewsAsync(admin, question.Id)).Should().ContainSingle();
        (await RejectAsync(reviewer, other.Id, new string('a', QuestionLimits.ChangeReasonMaxLength))).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // AC7, BR6: a question that is not in review answers 409 not_in_review, before the reason is looked at.
    [Fact]
    public async Task ApproveAndReject_AQuestionNotInReview_AreRefusedWithNotInReview_Or404WhenUnknown()
    {
        var admin = await AdminAsync();
        var reviewer = await HoldingOnlyAsync(QuestionBankPermissions.Review);
        var topic = await TopicAsync(admin);
        var draft = await CreatedAsync(admin, Complete(topic.Id));
        var published = await PublishedAsync(admin, topic.Id);

        await AssertRefusedAsync(await ApproveAsync(reviewer, draft.Id, null), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionNotInReview);
        await AssertRefusedAsync(await ApproveAsync(reviewer, published.Id, new string('a', 501)), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionNotInReview);
        await AssertRefusedAsync(await RejectAsync(reviewer, draft.Id, null), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionNotInReview);
        await AssertRefusedAsync(await RejectAsync(reviewer, published.Id, "Motivo."), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionNotInReview);
        await AssertRefusedAsync(await ApproveAsync(reviewer, Guid.CreateVersion7(), null), HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);
        await AssertRefusedAsync(await RejectAsync(reviewer, Guid.CreateVersion7(), "Motivo."), HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);

        (await GetAsync(admin, draft.Id)).Status.Should().Be(QuestionStatus.Draft);
        (await GetAsync(admin, published.Id)).Status.Should().Be(QuestionStatus.Published);
    }

    // AC7, change note v2: a decision on a send other than the latest is a lost race; the one the reviewer saw is accepted.
    [Fact]
    public async Task Decisions_ThatSendASubmittedAtOtherThanTheLatestSend_AnswerVersionConflict_AndTheOneTheReviewerSawIsAccepted()
    {
        var (admin, reviewer, _, question) = await InReviewWithAReviewerAsync();
        var seen = (await reviewer.GetFromJsonAsync<QuestionResponse>($"{ReviewQueue}/{question.Id}", AppJson.Options))!.SubmittedAt!.Value;
        (await WithdrawAsync(admin, question.Id)).StatusCode.Should().Be(HttpStatusCode.OK);
        Tick();
        var resent = await SubmittedAsync(admin, question.Id);

        resent.SubmittedAt.Should().BeAfter(seen);
        await AssertRefusedAsync(await ApproveAsync(reviewer, question.Id, "Ok.", seen), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionVersionConflict);
        await AssertRefusedAsync(await RejectAsync(reviewer, question.Id, "Motivo.", seen), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionVersionConflict);

        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.InReview);
        (await ReviewsAsync(admin, question.Id)).Select(entry => entry.Kind).Should().Equal(
            QuestionReviewKind.Submitted, QuestionReviewKind.Withdrawn, QuestionReviewKind.Submitted);
        var approved = await AsQuestionAsync(await ApproveAsync(reviewer, question.Id, "Ok.", resent.SubmittedAt));
        approved.Status.Should().Be(QuestionStatus.Published);
    }

    // AC8, BR5, BR7: the person who sent it may approve it, and both entries carry that person's id.
    [Fact]
    public async Task Approve_ByThePersonWhoSentTheQuestion_Publishes_AndBothEntriesCarryTheirId()
    {
        var account = await TestAccounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        var self = await TestAccounts.SignedInAsync(Client(), account.Email!);
        var topic = await TopicAsync(self);
        var question = await InReviewAsync(self, topic.Id);

        var approved = await AsQuestionAsync(await ApproveAsync(self, question.Id, null));

        approved.Status.Should().Be(QuestionStatus.Published);
        var stored = await StoredEntriesAsync(question.Id);
        stored.Select(entry => entry.Kind).Should().Equal(QuestionReviewKind.Submitted, QuestionReviewKind.Approved);
        stored.Select(entry => entry.CreatedBy).Should().OnlyContain(by => by == account.Id);
    }

    // AC10: the review detail is a question in review with the time of the latest send; any other state answers 409.
    [Fact]
    public async Task ReviewDetail_OfAQuestionInReview_ReturnsItWithItsSubmittedAt_AndAnyOtherStateAnswers409()
    {
        var (admin, reviewer, topic, question) = await InReviewWithAReviewerAsync();
        var draft = await CreatedAsync(admin, Complete(topic.Id));
        var published = await PublishedAsync(admin, topic.Id);

        var detail = (await reviewer.GetFromJsonAsync<QuestionResponse>($"{ReviewQueue}/{question.Id}", AppJson.Options))!;

        detail.Status.Should().Be(QuestionStatus.InReview);
        detail.SubmittedAt.Should().Be(question.SubmittedAt);
        detail.Options.Should().Contain(option => option.IsCorrect);
        detail.StatementHtml.Should().Be(question.StatementHtml);
        await AssertRefusedAsync(await reviewer.GetAsync($"{ReviewQueue}/{draft.Id}"), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionNotInReview);
        await AssertRefusedAsync(await reviewer.GetAsync($"{ReviewQueue}/{published.Id}"), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionNotInReview);
        await AssertRefusedAsync(await reviewer.GetAsync($"{ReviewQueue}/{Guid.CreateVersion7()}"), HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);
    }
}
