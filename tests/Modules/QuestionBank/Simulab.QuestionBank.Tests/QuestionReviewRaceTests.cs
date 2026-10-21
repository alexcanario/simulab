using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Application.Images;
using Simulab.QuestionBank.Application.Content;
using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-105 AC7, AC3 and BR11: two writes that both read the same state. The race is made deterministic by a store
/// that, right after the handler reads, lets a competing write go through another context: the handler's own save
/// then meets the changed status. Exactly one write wins and the loser answers 409 <c>question.version_conflict</c>
/// with none of its rows written (the status is the concurrency token).
/// </summary>
public sealed class QuestionReviewRaceTests : QuestionReviewApiTests
{
    private Task<int> SetStatusAsync(Guid questionId, string status) =>
        QueryAsync(context => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE question_bank.questions SET status = {status} WHERE id = {questionId}"));

    private Task<List<int>> VersionNumbersAsync(Guid questionId) =>
        QueryAsync(context => context.Database
            .SqlQuery<int>($"SELECT number AS \"Value\" FROM question_bank.question_versions WHERE question_id = {questionId} ORDER BY number")
            .ToListAsync());

    private async Task<(HttpClient Admin, TopicResponse Topic, QuestionResponse Question)> InReviewQuestionAsync()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await InReviewAsync(admin, topic.Id);

        return (admin, topic, question);
    }

    // AC7, BR11: an approval that read InReview while a withdrawal got there first writes neither version nor entry.
    [Fact]
    public async Task Approve_WhenTheQuestionWasWithdrawnMeanwhile_AnswersVersionConflict_AndWritesNothing()
    {
        var (_, _, question) = await InReviewQuestionAsync();
        await using var scope = Factory.Services.CreateAsyncScope();
        var store = new ReviewRacingStore(
            scope.ServiceProvider.GetRequiredService<IQuestionStore>(),
            afterFind: () => SetStatusAsync(question.Id, "Draft"));
        var handler = new ApproveQuestionHandler(store, scope.ServiceProvider.GetRequiredService<IQuestionQueries>());

        var result = await handler.HandleAsync(question.Id, "Ok.");

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionVersionConflict);
        result.Error.Kind.Should().Be(ErrorKind.Conflict);
        (await StatusOfAsync(question.Id)).Should().Be("Draft");
        (await VersionNumbersAsync(question.Id)).Should().BeEmpty();
        (await StoredEntriesAsync(question.Id)).Should().ContainSingle();
    }

    // AC7, BR11: two decisions at once are one winner; the loser of a rejection against an approval answers 409.
    [Fact]
    public async Task Reject_WhenAnotherReviewerApprovedMeanwhile_AnswersVersionConflict_AndWritesNoEntry()
    {
        var (_, _, question) = await InReviewQuestionAsync();
        await using var scope = Factory.Services.CreateAsyncScope();
        var store = new ReviewRacingStore(
            scope.ServiceProvider.GetRequiredService<IQuestionStore>(),
            afterFind: () => SetStatusAsync(question.Id, "Published"));
        var handler = new RejectQuestionHandler(store, scope.ServiceProvider.GetRequiredService<IQuestionQueries>());

        var result = await handler.HandleAsync(question.Id, "Motivo.");

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionVersionConflict);
        (await StatusOfAsync(question.Id)).Should().Be("Published");
        (await StoredEntriesAsync(question.Id)).Should().ContainSingle();
    }

    // BR11: a withdrawal against a decision.
    [Fact]
    public async Task Withdraw_WhenAReviewerRejectedMeanwhile_AnswersVersionConflict_AndWritesNoEntry()
    {
        var (_, _, question) = await InReviewQuestionAsync();
        await using var scope = Factory.Services.CreateAsyncScope();
        var store = new ReviewRacingStore(
            scope.ServiceProvider.GetRequiredService<IQuestionStore>(),
            afterFind: () => SetStatusAsync(question.Id, "Draft"));
        var handler = new WithdrawQuestionHandler(store, scope.ServiceProvider.GetRequiredService<IQuestionQueries>());

        var result = await handler.HandleAsync(question.Id);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionVersionConflict);
        (await StoredEntriesAsync(question.Id)).Should().ContainSingle();
    }

    // The send against a save that annulled or changed the state meanwhile: one winner.
    [Fact]
    public async Task Submit_WhenTheDraftWasPublishedMeanwhile_AnswersVersionConflict_AndWritesNoEntry()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var draft = await CreatedAsync(admin, Complete(topic.Id));
        await using var scope = Factory.Services.CreateAsyncScope();
        var store = new ReviewRacingStore(
            scope.ServiceProvider.GetRequiredService<IQuestionStore>(),
            afterFind: () => SetStatusAsync(draft.Id, "Published"));
        var handler = new SubmitQuestionHandler(store, scope.ServiceProvider.GetRequiredService<IQuestionQueries>());

        var result = await handler.HandleAsync(draft.Id);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionVersionConflict);
        (await StatusOfAsync(draft.Id)).Should().Be("Published");
        (await StoredEntriesAsync(draft.Id)).Should().BeEmpty();
    }

    // AC3, BR3: a draft save that read "Draft" and meets a question already in review writes nothing, even when the
    // save changes no column of the question itself (only an option text): the status guard turns it into a lost race.
    [Fact]
    public async Task Save_OfOnlyAnOptionText_WhenTheQuestionWentIntoReviewMeanwhile_AnswersVersionConflict_AndTheOptionIsNotWritten()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var draft = await CreatedAsync(admin, Complete(topic.Id));
        await using var scope = Factory.Services.CreateAsyncScope();
        var store = new ReviewRacingStore(
            scope.ServiceProvider.GetRequiredService<IQuestionStore>(),
            afterFind: () => SetStatusAsync(draft.Id, "InReview"));
        var handler = new SaveQuestionHandler(
            store,
            scope.ServiceProvider.GetRequiredService<IQuestionQueries>(),
            scope.ServiceProvider.GetRequiredService<QuestionHtmlSanitizer>(),
            scope.ServiceProvider.GetRequiredService<QuestionImageOwnership>());

        var outcome = await handler.HandleAsync(
            draft.Id,
            Complete(topic.Id) with
            {
                Options = [new SaveQuestionOptionRequest("<p>Paris</p>", true), new SaveQuestionOptionRequest("<p>Marselha</p>")]
            });

        outcome.IsSuccess.Should().BeFalse();
        outcome.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionVersionConflict);
        var read = await GetAsync(admin, draft.Id);
        read.Options.Select(option => option.TextHtml).Should().Equal("<p>Paris</p>", "<p>Lyon</p>");
    }
}
