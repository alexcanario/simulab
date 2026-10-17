using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// The store the review handlers are given in the race tests of F-105: the real one, except that right after the
/// question is read it lets a competing write go through another context first.
/// </summary>
public sealed class ReviewRacingStore(IQuestionStore inner, Func<Task>? afterFind = null) : IQuestionStore
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
