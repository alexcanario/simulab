using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Simulab.QuestionBank.Application.BaseTexts;
using Simulab.QuestionBank.Application.Content;
using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-102 AC4, BR4: the second half, "a failure of either leaves the text and both questions as they were". A
/// store unpublishes one of the two linked questions after the fan-out read it and before its save: the guarded
/// status makes the whole save a lost race, so the text and both versions are rolled back together.
/// </summary>
public sealed class BaseTextFanOutAtomicityTests : OriginApiTests
{
    private const string BaseTexts = "/api/v1/question-bank/base-texts";
    private const string Passage = "<p>O rio corria devagar entre as pedras.</p>";

    [Fact]
    public async Task Edit_WhenALinkedQuestionWasUnpublishedMeanwhile_Answers409_AndRollsBackTheTextAndBothVersions()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var created = await admin.PostAsJsonAsync(
            BaseTexts, new SaveBaseTextRequest($"Texto {Marker()}", "pt-BR", Passage), AppJson.Options);
        var text = (await created.Content.ReadFromJsonAsync<BaseTextResponse>(AppJson.Options))!;
        var first = await PublishedLinkedAsync(admin, topic.Id, text.Id);
        var second = await PublishedLinkedAsync(admin, topic.Id, text.Id);
        await using var scope = Factory.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var store = new UnpublishingStore(
            provider.GetRequiredService<IQuestionStore>(),
            async () =>
            {
                // A competing request commits an unpublish of the first question while the fan-out holds it.
                var response = await admin.PostAsync($"{Questions}/{first.Id}/unpublish", null);
                response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            });
        var handler = new SaveBaseTextHandler(
            provider.GetRequiredService<IBaseTextStore>(),
            store,
            provider.GetRequiredService<IBaseTextQueries>(),
            provider.GetRequiredService<QuestionHtmlSanitizer>());

        var result = await handler.HandleAsync(
            text.Id, new SaveBaseTextRequest(text.Title, "pt-BR", "<p>O rio corria depressa.</p>"));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionVersionConflict);
        result.Error.Kind.Should().Be(ErrorKind.Conflict);
        (await admin.GetFromJsonAsync<BaseTextResponse>($"{BaseTexts}/{text.Id}", AppJson.Options))!.TextHtml
            .Should().Be(Passage);
        (await VersionCountAsync(first.Id)).Should().Be(1);
        (await VersionCountAsync(second.Id)).Should().Be(1);
    }

    private static async Task<QuestionResponse> PublishedLinkedAsync(HttpClient client, Guid topicId, Guid baseTextId)
    {
        var question = await CreatedAsync(client, Complete(topicId) with { BaseTextId = baseTextId });
        var response = await client.PostAsync($"{Questions}/{question.Id}/publish", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
    }

    private Task<int> VersionCountAsync(Guid questionId) =>
        QueryAsync(context => context.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM question_bank.question_versions WHERE question_id = {questionId}")
            .SingleAsync());

    // The question store the handler is given: the real one, except that the first version it is handed makes a
    // competing unpublish commit, after the fan-out read the questions and before it saves.
    private sealed class UnpublishingStore(IQuestionStore inner, Func<Task> competing) : IQuestionStore
    {
        private bool _done;

        public void AddVersion(QuestionVersion version)
        {
            inner.AddVersion(version);
            if (!_done)
            {
                _done = true;
                competing().GetAwaiter().GetResult();
            }
        }

        public Task<LinkedBaseText?> FindBaseTextAsync(Guid id, CancellationToken cancellationToken) =>
            inner.FindBaseTextAsync(id, cancellationToken);

        public Task<Question?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            inner.FindAsync(id, cancellationToken);

        public Task<bool> TopicExistsAsync(Guid topicId, CancellationToken cancellationToken) =>
            inner.TopicExistsAsync(topicId, cancellationToken);

        public Task<bool> ExamEditionExistsAsync(Guid examEditionId, CancellationToken cancellationToken) =>
            inner.ExamEditionExistsAsync(examEditionId, cancellationToken);

        public Task<bool> NoticeSubjectBelongsToAsync(
            Guid noticeSubjectId,
            Guid examEditionId,
            CancellationToken cancellationToken) =>
            inner.NoticeSubjectBelongsToAsync(noticeSubjectId, examEditionId, cancellationToken);

        public Task<bool> IsPaperNumberTakenAsync(
            Guid examEditionId,
            int paperNumber,
            Guid? exceptQuestionId,
            CancellationToken cancellationToken) =>
            inner.IsPaperNumberTakenAsync(examEditionId, paperNumber, exceptQuestionId, cancellationToken);

        public Task<QuestionVersion?> FindLatestVersionAsync(Guid questionId, CancellationToken cancellationToken) =>
            inner.FindLatestVersionAsync(questionId, cancellationToken);

        public void Add(Question question) => inner.Add(question);

        public void Remove(Question question) => inner.Remove(question);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => inner.SaveChangesAsync(cancellationToken);

        public Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken) =>
            inner.TrySaveChangesAsync(cancellationToken);
    }
}
