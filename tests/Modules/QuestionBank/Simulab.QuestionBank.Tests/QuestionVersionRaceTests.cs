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
/// F-98 AC14, BR10: two saves that both read version N and both make N+1. The race is made deterministic by a
/// store that, right after the handler reads the latest version, writes the competing N+1 through another
/// context: the handler's own save then meets the unique index. Exactly one version has the number, the loser
/// answers 409 <c>question.version_conflict</c> and its content change is rolled back with its version.
/// </summary>
public sealed class QuestionVersionRaceTests : QuestionBankApiTests
{
    private async Task InsertCompetingVersionAsync(Guid questionId, int number, string statementHtml) =>
        await QueryAsync(context => context.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO question_bank.question_versions
                (id, tenant_id, question_id, number, type, content_language, statement_html, options, answer_key_changed, created_at, is_deleted)
               VALUES ({Guid.CreateVersion7()}, NULL, {questionId}, {number}, 'SingleChoice', 'pt-BR', {statementHtml}, '[]'::jsonb, false, now(), false)"));

    private async Task<(HttpClient Admin, TopicResponse Topic, QuestionResponse Question)> PublishedAtVersionOneAsync()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var published = await PublishedAsync(admin, topic.Id);

        return (admin, topic, published);
    }

    private Task<List<(int Number, string Statement)>> VersionRowsAsync(Guid questionId) =>
        QueryAsync(async context =>
        {
            var numbers = await context.Database
                .SqlQuery<int>($"SELECT number AS \"Value\" FROM question_bank.question_versions WHERE question_id = {questionId} ORDER BY number")
                .ToListAsync();
            var statements = await context.Database
                .SqlQuery<string>($"SELECT statement_html AS \"Value\" FROM question_bank.question_versions WHERE question_id = {questionId} ORDER BY number")
                .ToListAsync();

            return numbers.Zip(statements).ToList();
        });

    [Fact]
    public async Task Save_WhenAnotherSaveTookTheNextNumberFirst_AnswersVersionConflict_AndStoresNothingOfItself()
    {
        var (admin, topic, question) = await PublishedAtVersionOneAsync();
        await using var scope = Factory.Services.CreateAsyncScope();
        var store = new RacingStore(
            scope.ServiceProvider.GetRequiredService<IQuestionStore>(),
            () => InsertCompetingVersionAsync(question.Id, 2, "<p>A outra edição.</p>"));
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
        outcome.Error.Kind.Should().Be(ErrorKind.Conflict);
        (await VersionRowsAsync(question.Id)).Should().Equal(
            [(1, "<p>Qual é a capital da França?</p>"), (2, "<p>A outra edição.</p>")]);
        (await GetAsync(admin, question.Id)).StatementHtml.Should().Be("<p>Qual é a capital da França?</p>");
    }

    [Fact]
    public async Task Publish_WhenAnotherSaveTookTheNextNumberFirst_AnswersVersionConflict_AndTheQuestionStaysADraft()
    {
        var (admin, topic, question) = await PublishedAtVersionOneAsync();
        (await admin.PostAsync($"{Questions}/{question.Id}/unpublish", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var edited = await admin.PutAsJsonAsync(
            $"{Questions}/{question.Id}",
            new SaveQuestionRequest(
                topic.Id,
                "pt-BR",
                "<p>Texto do rascunho.</p>",
                Options: [new SaveQuestionOptionRequest("<p>Paris</p>", true), new SaveQuestionOptionRequest("<p>Lyon</p>")]),
            AppJson.Options);
        edited.StatusCode.Should().Be(HttpStatusCode.OK, await edited.Content.ReadAsStringAsync());
        await using var scope = Factory.Services.CreateAsyncScope();
        var store = new RacingStore(
            scope.ServiceProvider.GetRequiredService<IQuestionStore>(),
            () => InsertCompetingVersionAsync(question.Id, 2, "<p>A outra edição.</p>"));
        var handler = new PublishQuestionHandler(store, scope.ServiceProvider.GetRequiredService<IQuestionQueries>());

        var result = await handler.HandleAsync(question.Id);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionVersionConflict);
        result.Error.Kind.Should().Be(ErrorKind.Conflict);
        (await VersionRowsAsync(question.Id)).Select(row => row.Number).Should().Equal(1, 2);
        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.Draft);
    }

    // The store the handlers are given: it is the real one, except that right after the latest version is read it
    // lets a competing save write the next number first.
    private sealed class RacingStore(IQuestionStore inner, Func<Task> competing) : IQuestionStore
    {
        public async Task<QuestionVersion?> FindLatestVersionAsync(Guid questionId, CancellationToken cancellationToken)
        {
            var latest = await inner.FindLatestVersionAsync(questionId, cancellationToken);
            await competing();

            return latest;
        }

        public Task<Question?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            inner.FindAsync(id, cancellationToken);

        public Task<LinkedBaseText?> FindBaseTextAsync(Guid id, CancellationToken cancellationToken) =>
            inner.FindBaseTextAsync(id, cancellationToken);

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
