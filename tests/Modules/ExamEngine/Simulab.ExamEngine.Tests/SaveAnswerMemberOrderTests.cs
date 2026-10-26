using System.Net;
using Simulab.ExamEngine.Contracts;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-166 BR5 for a client that writes the members of the answer in another order: JSON objects are unordered, so the
/// <c>type</c> member does not have to come first (review finding 1).
/// </summary>
public sealed class SaveAnswerMemberOrderTests : ExamEngineApiTests
{
    [Theory]
    [InlineData(AttemptQuestionType.ShortAnswer, """{"text":"Brasília","type":"ShortAnswer"}""")]
    [InlineData(AttemptQuestionType.SingleChoice, """{"optionPosition":1,"type":"SingleChoice"}""")]
    [InlineData(AttemptQuestionType.MultipleAnswer, """{"optionPositions":[0,1],"type":"MultipleAnswer"}""")]
    public async Task Save_TheTypeMemberAfterTheOthers_IsSavedAndReadBack(AttemptQuestionType type, string body)
    {
        var admin = await AdminAsync();
        var (edition, questions) = await PublishedEditionWithEveryTypeAsync(admin);
        var student = await StudentAsync();
        var attempt = await StartedAsync(student, edition.Id);
        var questionId = questions.Single(question => question.Type == type).Id;

        var response = await SaveRawAsync(student, attempt.Id, questionId, body);

        await BodyAsync(response, HttpStatusCode.OK);
        var reread = await ReadAsync(student, attempt.Id);
        reread.Questions.Single(question => question.QuestionId == questionId).Answer
            .Should().NotBeNull().And.BeAssignableTo<AnswerPayload>();
    }
}
