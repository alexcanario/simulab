using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Application.Content;
using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-101 AC8, BR7 and the concurrency token of the status (design trade-off 8): two writes that both read the
/// same state. The race is made deterministic by a store that, right after the handler reads, lets a competing
/// write go through another context: the handler's own save then meets the unique version index or the
/// changed status. Exactly one write wins and the loser answers 409 <c>question.version_conflict</c>.
/// </summary>
public sealed class QuestionAnnulmentRaceTests : QuestionBankApiTests
{
    private Task<int> InsertCompetingVersionAsync(Guid questionId, int number) =>
        QueryAsync(context => context.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO question_bank.question_versions
                (id, tenant_id, question_id, number, type, content_language, statement_html, options, answer_key_changed, created_at, is_deleted)
               VALUES ({Guid.CreateVersion7()}, NULL, {questionId}, {number}, 'SingleChoice', 'pt-BR', '<p>Concorrente.</p>', '[]'::jsonb, false, now(), false)"));

    private Task<int> SetStatusAsync(Guid questionId, string status) =>
        QueryAsync(context => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE question_bank.questions SET status = {status} WHERE id = {questionId}"));

    private Task<string> StatusOfAsync(Guid questionId) =>
        QueryAsync(context => context.Database
            .SqlQuery<string>($"SELECT status AS \"Value\" FROM question_bank.questions WHERE id = {questionId}")
            .SingleAsync());

    private Task<List<int>> VersionNumbersAsync(Guid questionId) =>
        QueryAsync(context => context.Database
            .SqlQuery<int>($"SELECT number AS \"Value\" FROM question_bank.question_versions WHERE question_id = {questionId} ORDER BY number")
            .ToListAsync());

    private async Task<(HttpClient Admin, TopicResponse Topic, QuestionResponse Question)> PublishedAtVersionOneAsync()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var published = await PublishedAsync(admin, topic.Id);

        return (admin, topic, published);
    }

    [Fact]
    public async Task Annul_WhenAnotherWriteTookTheNextNumberFirst_AnswersVersionConflict_AndTheQuestionStaysPublished()
    {
        var (admin, _, question) = await PublishedAtVersionOneAsync();
        await using var scope = Factory.Services.CreateAsyncScope();
        var store = new RacingStore(
            scope.ServiceProvider.GetRequiredService<IQuestionStore>(),
            afterFindLatestVersion: () => InsertCompetingVersionAsync(question.Id, 2));
        var handler = new AnnulQuestionHandler(store, scope.ServiceProvider.GetRequiredService<IQuestionQueries>());

        var result = await handler.HandleAsync(question.Id, "Anulada.");

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionVersionConflict);
        result.Error.Kind.Should().Be(ErrorKind.Conflict);
        (await VersionNumbersAsync(question.Id)).Should().Equal(1, 2);
        (await StatusOfAsync(question.Id)).Should().Be("Published");
    }

    [Fact]
    public async Task Reinstate_WhenAnotherWriteTookTheNextNumberFirst_AnswersVersionConflict_AndTheQuestionStaysAnnulled()
    {
        var (admin, _, question) = await PublishedAtVersionOneAsync();
        var annulled = await admin.PostAsJsonAsync(
            $"{Questions}/{question.Id}/annul", new AnnulQuestionRequest("Anulada."), AppJson.Options);
        annulled.StatusCode.Should().Be(HttpStatusCode.OK, await annulled.Content.ReadAsStringAsync());
        await using var scope = Factory.Services.CreateAsyncScope();
        var store = new RacingStore(
            scope.ServiceProvider.GetRequiredService<IQuestionStore>(),
            afterFindLatestVersion: () => InsertCompetingVersionAsync(question.Id, 3));
        var handler = new ReinstateQuestionHandler(store, scope.ServiceProvider.GetRequiredService<IQuestionQueries>());

        var result = await handler.HandleAsync(question.Id, "Revertida.");

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionVersionConflict);
        (await VersionNumbersAsync(question.Id)).Should().Equal(1, 2, 3);
        (await StatusOfAsync(question.Id)).Should().Be("Annulled");
    }

    // Trade-off 8: the status is a concurrency token, so an annulment racing an unpublish leaves one winner.
    [Fact]
    public async Task Annul_WhenTheQuestionWasUnpublishedMeanwhile_AnswersVersionConflict_AndStoresNoVersion()
    {
        var (_, _, question) = await PublishedAtVersionOneAsync();
        await using var scope = Factory.Services.CreateAsyncScope();
        var store = new RacingStore(
            scope.ServiceProvider.GetRequiredService<IQuestionStore>(),
            afterFind: () => SetStatusAsync(question.Id, "Draft"));
        var handler = new AnnulQuestionHandler(store, scope.ServiceProvider.GetRequiredService<IQuestionQueries>());

        var result = await handler.HandleAsync(question.Id, "Anulada.");

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionVersionConflict);
        (await StatusOfAsync(question.Id)).Should().Be("Draft");
        (await VersionNumbersAsync(question.Id)).Should().Equal(1);
    }

    [Fact]
    public async Task Unpublish_WhenTheQuestionWasAnnulledMeanwhile_AnswersVersionConflict_AndTheQuestionStaysAnnulled()
    {
        var (_, _, question) = await PublishedAtVersionOneAsync();
        await using var scope = Factory.Services.CreateAsyncScope();
        var store = new RacingStore(
            scope.ServiceProvider.GetRequiredService<IQuestionStore>(),
            afterFind: () => SetStatusAsync(question.Id, "Annulled"));
        var handler = new UnpublishQuestionHandler(store, scope.ServiceProvider.GetRequiredService<IQuestionQueries>());

        var result = await handler.HandleAsync(question.Id);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionVersionConflict);
        result.Error.Kind.Should().Be(ErrorKind.Conflict);
        (await StatusOfAsync(question.Id)).Should().Be("Annulled");
    }

    [Fact]
    public async Task Delete_WhenTheQuestionWasPublishedMeanwhile_AnswersVersionConflict_AndTheQuestionIsNotDeleted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var draft = await CreatedAsync(admin, Complete(topic.Id));
        await using var scope = Factory.Services.CreateAsyncScope();
        var store = new RacingStore(
            scope.ServiceProvider.GetRequiredService<IQuestionStore>(),
            afterFind: () => SetStatusAsync(draft.Id, "Annulled"));
        var handler = new DeleteQuestionHandler(store);

        var result = await handler.HandleAsync(draft.Id);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionVersionConflict);
        (await GetAsync(admin, draft.Id)).Id.Should().Be(draft.Id);
    }

    [Fact]
    public async Task Save_WhenTheQuestionWasAnnulledMeanwhile_AnswersVersionConflict_AndTheContentIsNotWritten()
    {
        var (admin, topic, question) = await PublishedAtVersionOneAsync();
        await using var scope = Factory.Services.CreateAsyncScope();
        var store = new RacingStore(
            scope.ServiceProvider.GetRequiredService<IQuestionStore>(),
            afterFind: () => SetStatusAsync(question.Id, "Annulled"));
        var handler = new SaveQuestionHandler(
            store,
            scope.ServiceProvider.GetRequiredService<IQuestionQueries>(),
            scope.ServiceProvider.GetRequiredService<QuestionHtmlSanitizer>());

        var outcome = await handler.HandleAsync(
            question.Id,
            new SaveQuestionRequest(
                topic.Id,
                "pt-BR",
                "<p>Minha edição.</p>",
                Options: [new SaveQuestionOptionRequest("<p>Paris</p>", true), new SaveQuestionOptionRequest("<p>Lyon</p>")]));

        outcome.IsSuccess.Should().BeFalse();
        outcome.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionVersionConflict);
        (await VersionNumbersAsync(question.Id)).Should().Equal(1);
        (await GetAsync(admin, question.Id)).StatementHtml.Should().Be(question.StatementHtml);
    }

    // The store the handlers are given: the real one, except that right after a read it lets a competing
    // write go through another context first.
    private sealed class RacingStore(
        IQuestionStore inner,
        Func<Task>? afterFind = null,
        Func<Task>? afterFindLatestVersion = null) : IQuestionStore
    {
        public async Task<Question?> FindAsync(Guid id, CancellationToken cancellationToken)
        {
            var question = await inner.FindAsync(id, cancellationToken);
            if (afterFind is not null)
            {
                await afterFind();
            }

            return question;
        }

        public async Task<QuestionVersion?> FindLatestVersionAsync(Guid questionId, CancellationToken cancellationToken)
        {
            var latest = await inner.FindLatestVersionAsync(questionId, cancellationToken);
            if (afterFindLatestVersion is not null)
            {
                await afterFindLatestVersion();
            }

            return latest;
        }

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

        public void AddVersion(QuestionVersion version) => inner.AddVersion(version);

        public void Add(Question question) => inner.Add(question);

        public void Remove(Question question) => inner.Remove(question);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => inner.SaveChangesAsync(cancellationToken);

        public Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken) =>
            inner.TrySaveChangesAsync(cancellationToken);
    }
}
