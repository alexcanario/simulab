using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Simulab.ExamEngine.Contracts;
using Simulab.ExamEngine.Infrastructure.Persistence;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Testing.ApiHost;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-167 UC2 through HTTP: "download my data" carries the <c>examEngine</c> section with every attempt and answer of the
/// caller (BR7-BR9). The real section is wrapped by a recorder, so a test also sees which user id it was asked for.
/// </summary>
public sealed class AttemptDataExportTests : ExamEngineApiTests
{
    private const string ExportRoute = "/api/v1/identity/data-exports";

    private readonly ConcurrentQueue<Guid> _askedFor = new();

    protected override void ConfigureHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDataExportSection>();
            services.AddScoped<ExamEngineDataExportSection>();
            services.AddScoped<IDataExportSection>(provider =>
                new RecordingSection(provider.GetRequiredService<ExamEngineDataExportSection>(), _askedFor));
        });

    private static AnswerPayload AnswerOf(AttemptQuestionType type, string marker) => type switch
    {
        AttemptQuestionType.SingleChoice => new SingleChoicePayload(2),
        AttemptQuestionType.TrueFalse => new TrueFalsePayload(false),
        AttemptQuestionType.MultipleAnswer => new MultipleAnswerPayload([0, 1]),
        AttemptQuestionType.Matching => new MatchingPayload([new(0, 2), new(1, 0)]),
        AttemptQuestionType.FillInTheBlanks => new FillInTheBlanksPayload([new(1, $"{marker}-B1"), new(2, $"{marker}-B2")]),
        AttemptQuestionType.ShortAnswer => new ShortAnswerPayload($"{marker}-S"),
        AttemptQuestionType.OpenAnswer => new OpenAnswerPayload($"{marker}-O"),
        AttemptQuestionType.Essay => new EssayPayload($"{marker}-E1\n{marker}-E2"),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private async Task<(HttpClient Client, Guid Id)> NewStudentAsync() => await SignedInWithIdAsync(IdentityRoles.Student);

    private static async Task<JsonElement> ExportedAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(ExportRoute, new DataExportRequest(TestAccounts.ValidPassword), AppJson.Options);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        return JsonDocument.Parse(body).RootElement.Clone();
    }

    /// <summary>A submitted attempt with every question answered, then, a minute later, one in progress with the first question answered.</summary>
    private async Task<(Guid Submitted, Guid InProgress)> TwoAttemptsAsync(HttpClient student, CatalogEdition edition, IReadOnlyList<PaperQuestion> questions, string marker)
    {
        var submitted = await StartedAsync(student, edition.Id);
        foreach (var question in questions)
        {
            await SavedAsync(student, submitted.Id, question.Id, AnswerOf(question.Type, marker));
        }

        await SubmittedAsync(student, submitted.Id);
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        var inProgress = await StartedAsync(student, edition.Id);
        await SavedAsync(student, inProgress.Id, questions[0].Id, AnswerOf(questions[0].Type, marker));

        return (submitted.Id, inProgress.Id);
    }

    // AC1: both attempts, oldest first, with every member of BR7; the answers exactly as saved, null when blank.
    [Fact]
    public async Task Export_HasEveryAttemptAndAnswerOfTheStudent_OldestFirst_InPaperOrder()
    {
        var admin = await AdminAsync();
        var (edition, questions) = await PublishedEditionWithEveryTypeAsync(admin);
        var (student, _) = await NewStudentAsync();
        var (submittedId, inProgressId) = await TwoAttemptsAsync(student, edition, questions, "MINE");
        var submitted = await ReadAsync(student, submittedId);
        var inProgress = await ReadAsync(student, inProgressId);

        var file = await ExportedAsync(student);

        var attempts = file.GetProperty("examEngine").GetProperty("attempts").EnumerateArray().ToList();
        attempts.Select(attempt => attempt.GetProperty("id").GetGuid()).Should().Equal(submittedId, inProgressId);
        foreach (var (exported, read) in attempts.Zip(new[] { submitted, inProgress }))
        {
            exported.GetProperty("examEditionId").GetGuid().Should().Be(edition.Id);
            exported.GetProperty("status").GetString().Should().Be(read.Status.ToString());
            exported.GetProperty("startedAt").GetDateTimeOffset().Should().Be(read.StartedAt);
            var exportedQuestions = exported.GetProperty("questions").EnumerateArray().ToList();
            exportedQuestions.Select(question => question.GetProperty("paperNumber").GetInt32()).Should().BeInAscendingOrder().And.HaveCount(8);
            foreach (var (question, expected) in exportedQuestions.Zip(read.Questions))
            {
                question.GetProperty("questionId").GetGuid().Should().Be(expected.QuestionId);
                question.GetProperty("paperNumber").GetInt32().Should().Be(expected.PaperNumber);
                question.GetProperty("versionNumber").GetInt32().Should().Be(expected.VersionNumber);
                question.GetProperty("type").GetString().Should().Be(expected.Type.ToString());
                question.TryGetProperty("answer", out var answer).Should().BeTrue("a blank answer is written as null, not left out");
                answer.GetRawText().Should().Be(expected.Answer is null ? "null" : Json(expected.Answer), $"the {expected.Type} answer as saved");
            }
        }

        attempts[0].GetProperty("submittedAt").GetDateTimeOffset().Should().Be(submitted.SubmittedAt!.Value);
        attempts[1].TryGetProperty("submittedAt", out var notYet).Should().BeTrue("written as null while in progress");
        notYet.ValueKind.Should().Be(JsonValueKind.Null);
        attempts[1].GetProperty("questions")[1].GetProperty("answer").ValueKind.Should().Be(JsonValueKind.Null);
    }

    // AC2: [sec] T1. Nothing of another student, and the section is asked only for the caller.
    [Fact]
    public async Task Export_HoldsNothingOfAnotherStudent_AndTheSectionIsAskedOnlyForTheCaller()
    {
        var admin = await AdminAsync();
        var (mine, myQuestions) = await PublishedEditionWithEveryTypeAsync(admin);
        var (theirs, theirQuestions) = await PublishedEditionWithEveryTypeAsync(admin);
        var (student, studentId) = await NewStudentAsync();
        var (other, otherId) = await NewStudentAsync();
        await TwoAttemptsAsync(student, mine, myQuestions, "MINE");
        var (otherSubmitted, otherInProgress) = await TwoAttemptsAsync(other, theirs, theirQuestions, "THEIRS");
        _askedFor.Clear();

        var file = await ExportedAsync(student);

        var section = file.GetProperty("examEngine").GetRawText();
        section.Should().NotContainAny(
            [otherId.ToString(), otherSubmitted.ToString(), otherInProgress.ToString(), theirs.Id.ToString(), "THEIRS",
                .. theirQuestions.Select(question => question.Id.ToString())]);
        file.GetProperty("examEngine").GetProperty("attempts").GetArrayLength().Should().Be(2);
        _askedFor.Should().Equal(studentId);
    }

    // AC3: [sec] T2. No answer key, explanation, model answer, statement, option text or tenant id.
    [Fact]
    public async Task Export_SectionCarriesNoKeyExplanationModelAnswerStatementOrTenant()
    {
        var admin = await AdminAsync();
        var (edition, questions) = await PublishedEditionWithEveryTypeAsync(admin);
        var (student, _) = await NewStudentAsync();
        await TwoAttemptsAsync(student, edition, questions, "MINE");

        var section = (await ExportedAsync(student)).GetProperty("examEngine").GetRawText();

        section.Should().NotContainAny("SECRET-EXPLANATION", "SECRET-RUBRIC", "Enunciado", "Paris", "Lyon", "Brasília", "<p>");
        section.ToUpperInvariant().Should().NotContainAny(
            "ISCORRECT", "EXPLANATION", "RUBRIC", "ACCEPTEDANSWERS", "TRUEFALSEKEY", "STATEMENT", "OPTIONS\"", "TENANTID");
    }

    // AC4: no attempt is an empty list; the rest of the file is F-32's.
    [Fact]
    public async Task Export_StudentWithNoAttempt_HasAnEmptyList_AndTheRestOfTheFileAsBefore()
    {
        var (student, studentId) = await NewStudentAsync();

        var file = await ExportedAsync(student);

        file.EnumerateObject().Select(member => member.Name).Should().Equal("format", "version", "exportedAt", "identity", "examEngine");
        file.GetProperty("format").GetString().Should().Be(DataExportResponse.FormatName);
        file.GetProperty("version").GetInt32().Should().Be(DataExportResponse.CurrentVersion);
        var attempts = file.GetProperty("examEngine").GetProperty("attempts");
        attempts.ValueKind.Should().Be(JsonValueKind.Array);
        attempts.GetArrayLength().Should().Be(0);
        var identity = file.GetProperty("identity").Deserialize<IdentityDataResponse>(AppJson.Options)!;
        identity.Account.Id.Should().Be(studentId);
        identity.Roles.Should().Equal(IdentityRoles.Student);
    }

    /// <summary>The real section, remembering every user id it was asked for.</summary>
    private sealed class RecordingSection(IDataExportSection inner, ConcurrentQueue<Guid> askedFor) : IDataExportSection
    {
        public string Name => inner.Name;

        public Task<JsonElement> GetAsync(Guid userId, CancellationToken cancellationToken)
        {
            askedFor.Enqueue(userId);
            return inner.GetAsync(userId, cancellationToken);
        }
    }
}
