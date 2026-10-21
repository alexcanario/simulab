using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Simulab.QuestionBank.Application.Images;
using Simulab.QuestionBank.Application.Content;
using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-102 BR3, BR4, BR6 under concurrency. A question save or publish that reads its base text holds a shared row
/// lock until its own save; a base text edit or delete takes the row for update first. Each race is made
/// deterministic by a store that, right after the question side read the base text, starts the competing request
/// and checks that it is waiting, before the question side goes on and saves.
/// </summary>
public sealed class BaseTextRaceTests : OriginApiTests
{
    private const string BaseTexts = "/api/v1/question-bank/base-texts";
    private const string Passage = "<p>O rio corria devagar entre as pedras.</p>";

    private static SaveQuestionRequest Linked(Guid topicId, Guid baseTextId) =>
        Complete(topicId) with { BaseTextId = baseTextId };

    private static async Task<BaseTextResponse> CreatedTextAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            BaseTexts, new SaveBaseTextRequest($"Texto {Marker()}", "pt-BR", Passage), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<BaseTextResponse>(AppJson.Options))!;
    }

    // BR6: a delete that starts between the link check and the question's save waits, then finds the link.
    [Fact]
    public async Task Delete_StartedWhileAQuestionSaveHoldsTheBaseText_WaitsForIt_AndThenAnswersInUse()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var text = await CreatedTextAsync(admin);
        Task<HttpResponseMessage>? delete = null;
        await using var scope = Factory.Services.CreateAsyncScope();
        var store = new CompetingStore(
            scope.ServiceProvider.GetRequiredService<IQuestionStore>(),
            async () =>
            {
                delete = admin.DeleteAsync($"{BaseTexts}/{text.Id}");
                await Task.Delay(TimeSpan.FromSeconds(1));
                delete.IsCompleted.Should().BeFalse("the delete waits for the row the save is holding");
            });
        var handler = new SaveQuestionHandler(
            store,
            scope.ServiceProvider.GetRequiredService<IQuestionQueries>(),
            scope.ServiceProvider.GetRequiredService<QuestionHtmlSanitizer>(),
            scope.ServiceProvider.GetRequiredService<QuestionImageOwnership>());
        var request = Linked(topic.Id, text.Id);

        var outcome = await handler.HandleAsync(null, request);

        outcome.IsSuccess.Should().BeTrue(outcome.Error?.Code);
        var refused = await delete!;
        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await refused.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.BaseTextInUse);
        (await GetAsync(admin, outcome.Question!.Id)).BaseText!.Id.Should().Be(text.Id);
    }

    // BR4: an edit that starts while a publish holds the base text waits, then versions the question it just published.
    [Fact]
    public async Task Edit_StartedWhileAPublishHoldsTheBaseText_WaitsForIt_AndThenVersionsTheNewlyPublishedQuestion()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var text = await CreatedTextAsync(admin);
        var draft = await CreatedAsync(admin, Linked(topic.Id, text.Id));
        const string edited = "<p>O rio corria depressa entre as pedras.</p>";
        Task<HttpResponseMessage>? edit = null;
        await using var scope = Factory.Services.CreateAsyncScope();
        var store = new CompetingStore(
            scope.ServiceProvider.GetRequiredService<IQuestionStore>(),
            async () =>
            {
                edit = admin.PutAsJsonAsync(
                    $"{BaseTexts}/{text.Id}", new SaveBaseTextRequest(text.Title, "pt-BR", edited), AppJson.Options);
                await Task.Delay(TimeSpan.FromSeconds(1));
                edit.IsCompleted.Should().BeFalse("the edit waits for the row the publish is holding");
            });
        var handler = new PublishQuestionHandler(store, scope.ServiceProvider.GetRequiredService<IQuestionQueries>());

        var published = await handler.HandleAsync(draft.Id);

        published.IsSuccess.Should().BeTrue(published.Error?.Code);
        var answer = await edit!;
        answer.StatusCode.Should().Be(HttpStatusCode.OK, await answer.Content.ReadAsStringAsync());
        var versions = (await admin.GetFromJsonAsync<List<QuestionVersionListItemResponse>>(
            $"{Questions}/{draft.Id}/versions", AppJson.Options))!;
        versions.Select(version => version.Number).Should().Equal(2, 1);
        (await admin.GetFromJsonAsync<QuestionVersionResponse>($"{Questions}/{draft.Id}/versions/1", AppJson.Options))!
            .BaseTextHtml.Should().Be(Passage);
        (await admin.GetFromJsonAsync<QuestionVersionResponse>($"{Questions}/{draft.Id}/versions/2", AppJson.Options))!
            .BaseTextHtml.Should().Be(edited);
    }

    // The store the handlers are given: the real one, except that right after the base text is read it lets a
    // competing request start and be seen waiting.
    private sealed class CompetingStore(IQuestionStore inner, Func<Task> afterFindBaseText) : IQuestionStore
    {
        public async Task<LinkedBaseText?> FindBaseTextAsync(Guid id, CancellationToken cancellationToken)
        {
            var found = await inner.FindBaseTextAsync(id, cancellationToken);
            await afterFindBaseText();

            return found;
        }

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

        public void AddVersion(QuestionVersion version) => inner.AddVersion(version);

        public void AddReviewEntry(QuestionReviewEntry entry) => inner.AddReviewEntry(entry);

        public void GuardStatus(Question question) => inner.GuardStatus(question);

        public void Add(Question question) => inner.Add(question);

        public void Remove(Question question) => inner.Remove(question);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => inner.SaveChangesAsync(cancellationToken);

        public Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken) =>
            inner.TrySaveChangesAsync(cancellationToken);
    }
}
