using System.Net;
using System.Net.Http.Json;
using Simulab.ExamEngine.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-166 UC3 through HTTP: submitting (BR6, T8, T9), and who may change an attempt at all (BR7, T3): the owner only, an
/// admin reads and changes nothing.
/// </summary>
public sealed class SubmitAttemptEndpointTests : ExamEngineApiTests
{
    private async Task<(HttpClient Student, AttemptResponse Attempt, IReadOnlyList<PaperQuestion> Questions)> StartedEveryTypeAsync()
    {
        var admin = await AdminAsync();
        var (edition, questions) = await PublishedEditionWithEveryTypeAsync(admin);
        var student = await StudentAsync();

        return (student, await StartedAsync(student, edition.Id), questions);
    }

    private static Guid Single(IReadOnlyList<PaperQuestion> questions) =>
        questions.Single(question => question.Type == AttemptQuestionType.SingleChoice).Id;

    // AC10
    [Fact]
    public async Task Submit_AnAttemptWithBlanks_IsSubmittedWithTheServersTime_AndTheBlanksStayBlank()
    {
        var (student, attempt, questions) = await StartedEveryTypeAsync();
        await SavedAsync(student, attempt.Id, Single(questions), new SingleChoicePayload(1));
        Factory.Clock.Advance(TimeSpan.FromMinutes(42));

        var submitted = await SubmittedAsync(student, attempt.Id);

        submitted.Status.Should().Be(AttemptStatus.Submitted);
        submitted.SubmittedAt.Should().Be(Factory.Clock.GetUtcNow());
        submitted.StartedAt.Should().Be(attempt.StartedAt);
        submitted.Questions.Count(question => question.Answer != null).Should().Be(1);
        submitted.Questions.Count(question => question.Answer == null).Should().Be(7);
        Json(await ReadAsync(student, attempt.Id)).Should().Be(Json(submitted));
    }

    // AC11: [sec] T9
    [Fact]
    public async Task Save_OrClear_OnASubmittedAttempt_Answers409_AndNothingChanges()
    {
        var (student, attempt, questions) = await StartedEveryTypeAsync();
        await SavedAsync(student, attempt.Id, Single(questions), new SingleChoicePayload(1));
        var submitted = await SubmittedAsync(student, attempt.Id);

        var save = await SaveAsync(student, attempt.Id, Single(questions), new SingleChoicePayload(2));
        var clear = await ClearAsync(student, attempt.Id, Single(questions));

        CodeOf(await BodyAsync(save, HttpStatusCode.Conflict)).Should().Be(ExamAttemptErrorCodes.AlreadySubmitted);
        CodeOf(await BodyAsync(clear, HttpStatusCode.Conflict)).Should().Be(ExamAttemptErrorCodes.AlreadySubmitted);
        Json(await ReadAsync(student, attempt.Id)).Should().Be(Json(submitted));
    }

    // AC11: [sec] T9. A retry of a lost response is no failure and does not move the time.
    [Fact]
    public async Task Submit_AgainAnAttemptAlreadySubmitted_Answers200WithTheOriginalTime()
    {
        var (student, attempt, _) = await StartedEveryTypeAsync();
        var first = await SubmittedAsync(student, attempt.Id);
        Factory.Clock.Advance(TimeSpan.FromHours(1));

        var again = await SubmittedAsync(student, attempt.Id);

        again.SubmittedAt.Should().Be(first.SubmittedAt);
        (await ReadAsync(student, attempt.Id)).SubmittedAt.Should().Be(first.SubmittedAt);
    }

    // AC11: [sec] T9. A save racing a submit: the save that answered 200 is stored, the one that answered 409 is not.
    [Fact]
    public async Task Save_RacingASubmit_IsEitherStoredBeforeItOrRefusedAfterIt_NeverStoredAfterIt()
    {
        var admin = await AdminAsync();
        var (edition, questions) = await PublishedEditionWithEveryTypeAsync(admin);
        var student = await StudentAsync();
        var questionId = Single(questions);

        for (var round = 0; round < 6; round++)
        {
            var attempt = await StartedAsync(student, edition.Id);

            var save = SaveAsync(student, attempt.Id, questionId, new SingleChoicePayload(1));
            var submit = SubmitAsync(student, attempt.Id);
            var responses = await Task.WhenAll(save, submit);

            responses[1].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[0].StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Conflict);
            var stored = (await ReadAsync(student, attempt.Id)).Questions.Single(question => question.QuestionId == questionId).Answer;
            if (responses[0].StatusCode == HttpStatusCode.OK)
            {
                stored.Should().Be(new SingleChoicePayload(1), $"round {round}: a save answered 200 must be there");
            }
            else
            {
                stored.Should().BeNull($"round {round}: a save refused as submitted must not be there");
            }
        }
    }

    // AC18: [sec] T8
    [Fact]
    public async Task Submit_WithATimeInTheBodyOrTheHeaders_StoresTheServersClock()
    {
        var (student, attempt, _) = await StartedEveryTypeAsync();
        Factory.Clock.Advance(TimeSpan.FromMinutes(5));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{Attempts}/{attempt.Id}/submit")
        {
            Content = JsonContent.Create(new { submittedAt = "2001-01-01T00:00:00Z", SubmittedAt = "2001-01-01T00:00:00Z" }, options: AppJson.Options)
        };
        request.Headers.TryAddWithoutValidation("Date", "Mon, 01 Jan 2001 00:00:00 GMT");
        request.Headers.TryAddWithoutValidation("X-Submitted-At", "2001-01-01T00:00:00Z");

        var response = await student.SendAsync(request);

        var submitted = (await BodyAsync(response, HttpStatusCode.OK));
        System.Text.Json.JsonSerializer.Deserialize<AttemptResponse>(submitted, AppJson.Options)!.SubmittedAt
            .Should().Be(Factory.Clock.GetUtcNow());
    }

    // AC13
    [Fact]
    public async Task AnAdmin_ReadsButNeverSavesClearsOrSubmits_AnAttemptOfAStudent()
    {
        var (student, attempt, questions) = await StartedEveryTypeAsync();
        var admin = await AdminAsync();
        await SavedAsync(student, attempt.Id, Single(questions), new SingleChoicePayload(1));
        var before = await ReadAsync(student, attempt.Id);

        var save = await SaveAsync(admin, attempt.Id, Single(questions), new SingleChoicePayload(2));
        var clear = await ClearAsync(admin, attempt.Id, Single(questions));
        var submit = await SubmitAsync(admin, attempt.Id);

        foreach (var response in new[] { save, clear, submit })
        {
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        Json(await ReadAsync(student, attempt.Id)).Should().Be(Json(before));
    }

    // AC15: [sec] T3. Another student's attempt and an id that does not exist answer alike.
    [Fact]
    public async Task AnotherStudent_CannotSaveClearOrSubmit_AndGetsTheAnswerOfAnIdThatDoesNotExist()
    {
        var (owner, attempt, questions) = await StartedEveryTypeAsync();
        var other = await StudentAsync();
        await SavedAsync(owner, attempt.Id, Single(questions), new SingleChoicePayload(1));
        var before = await ReadAsync(owner, attempt.Id);
        var unknown = Guid.CreateVersion7();

        var foreign = new[]
        {
            await SaveAsync(other, attempt.Id, Single(questions), new SingleChoicePayload(2)),
            await ClearAsync(other, attempt.Id, Single(questions)),
            await SubmitAsync(other, attempt.Id)
        };
        var missing = new[]
        {
            await SaveAsync(other, unknown, Single(questions), new SingleChoicePayload(2)),
            await ClearAsync(other, unknown, Single(questions)),
            await SubmitAsync(other, unknown)
        };

        for (var index = 0; index < foreign.Length; index++)
        {
            foreign[index].StatusCode.Should().Be(HttpStatusCode.NotFound);
            var foreignBody = await foreign[index].Content.ReadAsStringAsync();
            CodeOf(foreignBody).Should().Be(ExamAttemptErrorCodes.NotFound);
            (await missing[index].Content.ReadAsStringAsync()).Should().Contain(ExamAttemptErrorCodes.NotFound);
            missing[index].StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        Json(await ReadAsync(owner, attempt.Id)).Should().Be(Json(before));
    }

    // AC14: the write routes need the permission to take attempts as the start does.
    [Fact]
    public async Task TheWriteRoutes_AnAnonymousCallerIs401_AndACuratorIs403()
    {
        var (_, attempt, questions) = await StartedEveryTypeAsync();
        var curator = await CuratorAsync();

        foreach (var client in new[] { Client(), curator })
        {
            var expected = client == curator ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized;

            (await SaveAsync(client, attempt.Id, Single(questions), new SingleChoicePayload(1))).StatusCode.Should().Be(expected);
            (await ClearAsync(client, attempt.Id, Single(questions))).StatusCode.Should().Be(expected);
            (await SubmitAsync(client, attempt.Id)).StatusCode.Should().Be(expected);
        }
    }
}
