using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-105 through HTTP, on a real PostgreSQL: sending for review (AC1, AC2), the frozen state (AC3), withdrawing
/// (AC4), the history (AC11), the topic guard (AC12) and the direct paths that stay (AC16).
/// </summary>
public sealed class QuestionSubmitWithdrawEndpointTests : QuestionReviewApiTests
{
    private static Task<HttpResponseMessage> SaveAsync(HttpClient client, Guid id, SaveQuestionRequest request) =>
        client.PutAsJsonAsync($"{Questions}/{id}", request, AppJson.Options);

    private static async Task<IReadOnlyList<QuestionVersionListItemResponse>> VersionsAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<List<QuestionVersionListItemResponse>>($"{Questions}/{id}/versions", AppJson.Options))!;

    // AC1, UC1, BR1, BR2, BR7: the status, the entry, no version, and the time of the send travels with the question.
    [Fact]
    public async Task Submit_ACompleteDraft_BecomesInReview_WithOneSubmittedEntry_AndNoVersion()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var draft = await CreatedAsync(admin, Complete(topic.Id));
        var before = Factory.Clock.GetUtcNow();

        var submitted = await SubmittedAsync(admin, draft.Id);

        submitted.Status.Should().Be(QuestionStatus.InReview);
        submitted.SubmittedAt.Should().NotBeNull();
        submitted.SubmittedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(Factory.Clock.GetUtcNow());
        submitted.StatementHtml.Should().Be(draft.StatementHtml);
        submitted.CurrentVersion.Should().BeNull();
        var read = await GetAsync(admin, draft.Id);
        read.Status.Should().Be(QuestionStatus.InReview);
        read.SubmittedAt.Should().Be(submitted.SubmittedAt);
        var entries = await ReviewsAsync(admin, draft.Id);
        entries.Should().ContainSingle();
        entries[0].Kind.Should().Be(QuestionReviewKind.Submitted);
        entries[0].Reason.Should().BeNull();
        (await VersionsAsync(admin, draft.Id)).Should().BeEmpty();
        (await StoredEntriesAsync(draft.Id)).Should().ContainSingle();
    }

    // AC2, BR2: an incomplete draft is refused with the code publishing uses; nothing changes.
    [Fact]
    public async Task Submit_AnIncompleteDraft_IsRefusedWithThePublishCode_AndNothingChanges()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var noKey = await CreatedAsync(
            admin,
            Complete(topic.Id) with { Options = [new SaveQuestionOptionRequest("<p>Paris</p>"), new SaveQuestionOptionRequest("<p>Lyon</p>")] });
        var oneOption = await CreatedAsync(
            admin,
            Complete(topic.Id) with { Options = [new SaveQuestionOptionRequest("<p>Paris</p>", true)] });

        await AssertRefusedAsync(await SubmitAsync(admin, noKey.Id), HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionKeyInvalid);
        await AssertRefusedAsync(await SubmitAsync(admin, oneOption.Id), HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionOptionsCount);

        (await GetAsync(admin, noKey.Id)).Status.Should().Be(QuestionStatus.Draft);
        (await ReviewsAsync(admin, noKey.Id)).Should().BeEmpty();
        (await GetAsync(admin, oneOption.Id)).Status.Should().Be(QuestionStatus.Draft);
    }

    // AC2: a question already in review, published or annulled answers the code of its state; an unknown id answers 404.
    [Fact]
    public async Task Submit_InReviewPublishedOrAnnulled_IsRefusedWithTheCodeOfThatState()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var inReview = await InReviewAsync(admin, topic.Id);
        var published = await PublishedAsync(admin, topic.Id);
        var annulled = await PublishedAsync(admin, topic.Id);
        (await admin.PostAsJsonAsync($"{Questions}/{annulled.Id}/annul", new AnnulQuestionRequest("Anulada."), AppJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        await AssertRefusedAsync(await SubmitAsync(admin, inReview.Id), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionAlreadyInReview);
        await AssertRefusedAsync(await SubmitAsync(admin, published.Id), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionAlreadyPublished);
        await AssertRefusedAsync(await SubmitAsync(admin, annulled.Id), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionAnnulled);
        await AssertRefusedAsync(await SubmitAsync(admin, Guid.CreateVersion7()), HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);

        (await ReviewsAsync(admin, inReview.Id)).Should().ContainSingle();
        (await GetAsync(admin, published.Id)).Status.Should().Be(QuestionStatus.Published);
        (await GetAsync(admin, annulled.Id)).Status.Should().Be(QuestionStatus.Annulled);
    }

    // AC3, BR3: save, publish, unpublish and delete answer 409 question.in_review and change nothing.
    [Fact]
    public async Task InReview_SaveDeletePublishAndUnpublish_Answer409InReview_AndChangeNothing()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await InReviewAsync(admin, topic.Id);

        var save = await SaveAsync(admin, question.Id, Complete(topic.Id, "<p>Outro enunciado.</p>"));
        var saveOptionsOnly = await SaveAsync(
            admin,
            question.Id,
            Complete(topic.Id) with { Options = [new SaveQuestionOptionRequest("<p>Paris</p>", true), new SaveQuestionOptionRequest("<p>Marselha</p>")] });
        var saveToAnUnknownTopic = await SaveAsync(admin, question.Id, Complete(Guid.CreateVersion7()));
        var delete = await admin.DeleteAsync($"{Questions}/{question.Id}");
        var publish = await admin.PostAsync($"{Questions}/{question.Id}/publish", null);
        var unpublish = await admin.PostAsync($"{Questions}/{question.Id}/unpublish", null);

        foreach (var response in new[] { save, saveOptionsOnly, saveToAnUnknownTopic, delete, publish, unpublish })
        {
            await AssertRefusedAsync(response, HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionInReview);
        }

        var read = await GetAsync(admin, question.Id);
        read.Status.Should().Be(QuestionStatus.InReview);
        read.StatementHtml.Should().Be(question.StatementHtml);
        read.Options.Select(option => option.TextHtml).Should().Equal(question.Options.Select(option => option.TextHtml));
        (await ReviewsAsync(admin, question.Id)).Should().ContainSingle();
    }

    // AC3, BR3: reading, listing, the version history and the annul rule answer as before.
    [Fact]
    public async Task InReview_GetListVersionsAndAnnul_AnswerAsBefore()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await InReviewAsync(admin, topic.Id, $"<p>Questão em revisão {Marker()}</p>");

        var list = await ListAsync(admin, "?status=InReview&pageSize=100");
        var annul = await admin.PostAsJsonAsync($"{Questions}/{question.Id}/annul", new AnnulQuestionRequest("Anulada."), AppJson.Options);

        (await GetAsync(admin, question.Id)).Id.Should().Be(question.Id);
        list.Items.Should().Contain(item => item.Id == question.Id && item.Status == QuestionStatus.InReview);
        (await VersionsAsync(admin, question.Id)).Should().BeEmpty();
        await AssertRefusedAsync(annul, HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionNotPublished);
        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.InReview);
    }

    // AC4, UC2, BR4: a withdrawal by any holder of questions.manage makes it a draft again, with the same content.
    [Fact]
    public async Task Withdraw_AQuestionInReview_BecomesADraftAgain_WithAWithdrawnEntry_AndTheSameContent()
    {
        var admin = await AdminAsync();
        var curator = await CuratorAsync();
        var topic = await TopicAsync(admin);
        var question = await InReviewAsync(admin, topic.Id);
        Tick();

        var withdrawn = await AsQuestionAsync(await WithdrawAsync(curator, question.Id));

        withdrawn.Status.Should().Be(QuestionStatus.Draft);
        withdrawn.SubmittedAt.Should().BeNull();
        withdrawn.StatementHtml.Should().Be(question.StatementHtml);
        withdrawn.Options.Select(option => (option.TextHtml, option.IsCorrect))
            .Should().Equal(question.Options.Select(option => (option.TextHtml, option.IsCorrect)));
        var entries = await ReviewsAsync(admin, question.Id);
        entries.Select(entry => entry.Kind).Should().Equal(QuestionReviewKind.Submitted, QuestionReviewKind.Withdrawn);
        (await SaveAsync(admin, question.Id, Complete(topic.Id, "<p>Agora pode editar.</p>")))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Withdraw_ADraftPublishedOrUnknownQuestion_IsRefusedWithNotInReview_Or404()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var draft = await CreatedAsync(admin, Complete(topic.Id));
        var published = await PublishedAsync(admin, topic.Id);

        await AssertRefusedAsync(await WithdrawAsync(admin, draft.Id), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionNotInReview);
        await AssertRefusedAsync(await WithdrawAsync(admin, published.Id), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionNotInReview);
        await AssertRefusedAsync(await WithdrawAsync(admin, Guid.CreateVersion7()), HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);

        (await ReviewsAsync(admin, draft.Id)).Should().BeEmpty();
    }

    // AC11, UC6, BR7: send, rejection and a second send are three entries, oldest first, with their reasons.
    [Fact]
    public async Task Reviews_AfterSendRejectionAndSecondSend_AreThreeEntriesOldestFirst()
    {
        var admin = await AdminAsync();
        var reviewer = await HoldingOnlyAsync(QuestionBankPermissions.Review);
        var topic = await TopicAsync(admin);
        var question = await InReviewAsync(admin, topic.Id);
        Tick();
        (await RejectAsync(reviewer, question.Id, "  A chave da C contradiz a explicação.  ")).StatusCode.Should().Be(HttpStatusCode.OK);
        Tick();
        await SubmittedAsync(admin, question.Id);

        var entries = await ReviewsAsync(admin, question.Id);

        entries.Select(entry => entry.Kind).Should().Equal(
            QuestionReviewKind.Submitted, QuestionReviewKind.Rejected, QuestionReviewKind.Submitted);
        entries.Select(entry => entry.Reason).Should().Equal(null, "A chave da C contradiz a explicação.", null);
        entries.Select(entry => entry.At).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        await AssertRefusedAsync(
            await admin.GetAsync($"{Questions}/{Guid.CreateVersion7()}/reviews"),
            HttpStatusCode.NotFound,
            QuestionBankErrorCodes.QuestionNotFound);
    }

    // AC11: no route changes or deletes an entry.
    [Fact]
    public async Task Reviews_HaveNoRouteThatChangesOrDeletesAnEntry()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await InReviewAsync(admin, topic.Id);
        var path = $"{Questions}/{question.Id}/reviews";

        var responses = new[]
        {
            await admin.PostAsync(path, null),
            await admin.PutAsJsonAsync(path, new { }, AppJson.Options),
            await admin.DeleteAsync(path),
            await admin.DeleteAsync($"{path}/{Guid.CreateVersion7()}")
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().BeOneOf(HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound);
        }

        (await ReviewsAsync(admin, question.Id)).Should().ContainSingle();
        (await StoredEntriesAsync(question.Id)).Should().ContainSingle();
    }

    // AC12, BR12: the topic delete guard counts a question in review.
    [Fact]
    public async Task DeleteTopic_UsedOnlyByAQuestionInReview_IsRefusedWithTopicInUse()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        await InReviewAsync(admin, topic.Id);

        var response = await admin.DeleteAsync($"/api/v1/catalog/topics/{topic.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.TopicInUse);
    }

    // AC16, UC7, BR13, BR14: publishing a complete draft directly and editing a published question need no review.
    [Fact]
    public async Task PublishDirect_AndEditAPublishedQuestion_StillWorkWithNoReview()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var published = await PublishedAsync(admin, topic.Id);

        var edited = await SaveAsync(admin, published.Id, Complete(topic.Id, "<p>Enunciado corrigido.</p>"));

        edited.StatusCode.Should().Be(HttpStatusCode.OK, await edited.Content.ReadAsStringAsync());
        var read = await GetAsync(admin, published.Id);
        read.Status.Should().Be(QuestionStatus.Published);
        read.StatementHtml.Should().Be("<p>Enunciado corrigido.</p>");
        (await ReviewsAsync(admin, published.Id)).Should().BeEmpty();
        (await VersionsAsync(admin, published.Id)).Select(version => version.Number).Should().Equal(2, 1);
    }

    // AC9: with questions.manage only, send, withdraw and the history answer; the review routes answer 403.
    [Fact]
    public async Task ManageOnly_CanSendWithdrawAndReadTheHistory_ButNotTheReviewRoutes()
    {
        var admin = await AdminAsync();
        var manager = await HoldingOnlyAsync(QuestionBankPermissions.Manage);
        var topic = await TopicAsync(admin);
        var draft = await CreatedAsync(manager, Complete(topic.Id));

        var submitted = await SubmittedAsync(manager, draft.Id);
        (await ReviewsAsync(manager, draft.Id)).Should().ContainSingle();
        (await AsQuestionAsync(await WithdrawAsync(manager, draft.Id))).Status.Should().Be(QuestionStatus.Draft);
        await SubmittedAsync(manager, draft.Id);

        var forbidden = new[]
        {
            await manager.GetAsync(ReviewQueue),
            await manager.GetAsync($"{ReviewQueue}/{submitted.Id}"),
            await ApproveAsync(manager, submitted.Id, null),
            await RejectAsync(manager, submitted.Id, "Motivo.")
        };
        foreach (var response in forbidden)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        (await GetAsync(admin, draft.Id)).Status.Should().Be(QuestionStatus.InReview);
    }

    // AC9: with questions.review only, the review routes answer and send, withdraw, save and the history are 403.
    [Fact]
    public async Task ReviewOnly_CannotSendWithdrawSaveOrReadTheHistory()
    {
        var admin = await AdminAsync();
        var reviewer = await HoldingOnlyAsync(QuestionBankPermissions.Review);
        var topic = await TopicAsync(admin);
        var question = await InReviewAsync(admin, topic.Id);

        var forbidden = new[]
        {
            await SubmitAsync(reviewer, question.Id),
            await WithdrawAsync(reviewer, question.Id),
            await SaveAsync(reviewer, question.Id, Complete(topic.Id)),
            await reviewer.GetAsync($"{Questions}/{question.Id}/reviews"),
            await reviewer.GetAsync($"{Questions}/{question.Id}"),
            await reviewer.GetAsync(Questions)
        };

        foreach (var response in forbidden)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        (await reviewer.GetAsync(ReviewQueue)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await reviewer.GetAsync($"{ReviewQueue}/{question.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // AC9: neither permission, no route; a student too. A new Curator and the Admin hold both.
    [Fact]
    public async Task EveryRoute_ForAUserWithNeitherPermissionOrAStudent_Answers403_AndCuratorAndAdminHoldBoth()
    {
        var unknown = Guid.CreateVersion7();
        var neither = await HoldingOnlyAsync(CatalogPermissions.Manage);
        var student = await StudentAsync();

        foreach (var client in new[] { neither, student })
        {
            var responses = new[]
            {
                await SubmitAsync(client, unknown),
                await WithdrawAsync(client, unknown),
                await client.GetAsync($"{Questions}/{unknown}/reviews"),
                await client.GetAsync(ReviewQueue),
                await client.GetAsync($"{ReviewQueue}/{unknown}"),
                await ApproveAsync(client, unknown, null),
                await RejectAsync(client, unknown, "Motivo.")
            };
            foreach (var response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }
        }

        foreach (var client in new[] { await CuratorAsync(), await AdminAsync() })
        {
            (await client.GetAsync(ReviewQueue)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await SubmitAsync(client, unknown)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
