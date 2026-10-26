using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using Simulab.Catalog.Contracts;
using Simulab.ExamEngine.Contracts;
using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Testing.ApiHost;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// What the attempt tests share (F-166): signed-in accounts, a published edition with published questions of every
/// type made through the catalog and question bank routes the way a curator would, the five attempt routes, and a
/// plain SQL read of the module's tables. Tests of one class share a database, so each makes its own data.
/// </summary>
public abstract class ExamEngineApiTests : ApiHostTests
{
    protected const string Attempts = "/api/v1/exam-engine/attempts";
    protected const string Questions = "/api/v1/question-bank/questions";
    private const string CatalogExams = "/api/v1/catalog/exams";
    private const string Subjects = "/api/v1/catalog/subjects";

    /// <summary>A catalog edition and the exam it belongs to (the catalog routes are nested under the exam).</summary>
    protected sealed record CatalogEdition(Guid Id, Guid ExamId);

    /// <summary>A question made for an edition: its id, its number in the paper and its type.</summary>
    protected sealed record PaperQuestion(Guid Id, int PaperNumber, AttemptQuestionType Type);

    protected static string Marker() => Guid.CreateVersion7().ToString("N")[..12];

    /// <summary>The value as the wire carries it: what two attempts or answers are compared by, polymorphic payloads included.</summary>
    protected static string Json<T>(T value) => JsonSerializer.Serialize(value, AppJson.Options);

    protected async Task<(HttpClient Client, Guid UserId)> SignedInWithIdAsync(string role)
    {
        var account = await TestAccounts.CreateAsync(Factory.Services, roles: role);
        return (await TestAccounts.SignedInAsync(Client(), account.Email!), account.Id);
    }

    protected async Task<HttpClient> SignedInAsync(string role) => (await SignedInWithIdAsync(role)).Client;

    protected Task<HttpClient> AdminAsync() => SignedInAsync(IdentityRoles.Admin);

    protected Task<HttpClient> CuratorAsync() => SignedInAsync(IdentityRoles.Curator);

    protected Task<HttpClient> StudentAsync() => SignedInAsync(IdentityRoles.Student);

    /// <summary>The first column of every row the query returns, as text.</summary>
    protected async Task<List<string?>> ValuesAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string?>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.IsDBNull(0) ? null : Convert.ToString(reader.GetValue(0), System.Globalization.CultureInfo.InvariantCulture));
        }

        return values;
    }

    protected async Task<int> CountAsync(string sql) => int.Parse((await ValuesAsync(sql)).Single()!, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>A catalog edition, published when <paramref name="publish"/> is true; it is a draft otherwise.</summary>
    protected static async Task<CatalogEdition> EditionAsync(HttpClient admin, bool publish)
    {
        var marker = Marker();
        var authority = await admin.PostAsJsonAsync(
            "/api/v1/catalog/issuing-authorities", new SaveIssuingAuthorityRequest($"Orgao {marker}"), AppJson.Options);
        authority.StatusCode.Should().Be(HttpStatusCode.Created, await authority.Content.ReadAsStringAsync());
        var authorityId = (await authority.Content.ReadFromJsonAsync<IssuingAuthorityResponse>(AppJson.Options))!.Id;

        var exam = await admin.PostAsJsonAsync(
            CatalogExams,
            new SaveExamRequest(authorityId, $"Exame {marker}", nameof(AssessmentType.PublicServiceExam), nameof(ExamScope.National), null, "pt-BR"),
            AppJson.Options);
        exam.StatusCode.Should().Be(HttpStatusCode.Created, await exam.Content.ReadAsStringAsync());
        var examId = (await exam.Content.ReadFromJsonAsync<ExamResponse>(AppJson.Options))!.Id;

        var organizer = await admin.PostAsJsonAsync(
            "/api/v1/catalog/organizers",
            new SaveOrganizerRequest($"Banca {marker}", $"BN{marker[^10..]}".ToUpperInvariant(), nameof(OrganizerKind.ExamBoard)),
            AppJson.Options);
        organizer.StatusCode.Should().Be(HttpStatusCode.Created, await organizer.Content.ReadAsStringAsync());
        var organizerId = (await organizer.Content.ReadFromJsonAsync<OrganizerResponse>(AppJson.Options))!.Id;

        var edition = await admin.PostAsJsonAsync(
            $"{CatalogExams}/{examId}/editions",
            new SaveExamEditionRequest(organizerId, 2024, null, Status: nameof(ExamEditionStatus.Draft)),
            AppJson.Options);
        edition.StatusCode.Should().Be(HttpStatusCode.Created, await edition.Content.ReadAsStringAsync());
        var created = new CatalogEdition((await edition.Content.ReadFromJsonAsync<ExamEditionResponse>(AppJson.Options))!.Id, examId);
        if (publish)
        {
            await PublishEditionAsync(admin, created);
        }

        return created;
    }

    /// <summary>The edition is published the way an admin does it: the same form, with the status changed.</summary>
    protected static async Task PublishEditionAsync(HttpClient admin, CatalogEdition edition)
    {
        var current = (await admin.GetFromJsonAsync<ExamEditionResponse>(
            $"{CatalogExams}/{edition.ExamId}/editions/{edition.Id}", AppJson.Options))!;
        var response = await admin.PutAsJsonAsync(
            $"{CatalogExams}/{edition.ExamId}/editions/{edition.Id}",
            new SaveExamEditionRequest(
                current.OrganizerId,
                current.NoticeYear,
                current.Position,
                current.NoticeReference,
                current.NoticeUrl,
                current.AppliedOn,
                nameof(ExamEditionStatus.Published)),
            AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    /// <summary>A subject and a topic under it, made through the catalog the way a curator would.</summary>
    protected static async Task<Guid> TopicAsync(HttpClient admin)
    {
        var marker = Marker();
        var subject = await admin.PostAsJsonAsync(Subjects, new SaveSubjectRequest($"Português {marker}"), AppJson.Options);
        subject.StatusCode.Should().Be(HttpStatusCode.Created, await subject.Content.ReadAsStringAsync());
        var subjectId = (await subject.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!.Id;

        var topic = await admin.PostAsJsonAsync(
            $"{Subjects}/{subjectId}/topics", new SaveTopicRequest($"Crase {marker}"), AppJson.Options);
        topic.StatusCode.Should().Be(HttpStatusCode.Created, await topic.Content.ReadAsStringAsync());

        return (await topic.Content.ReadFromJsonAsync<TopicResponse>(AppJson.Options))!.Id;
    }

    /// <summary>A request that satisfies every rule of a published question of the type, in the edition at the paper number.</summary>
    protected static SaveQuestionRequest QuestionOfType(Guid topicId, Guid editionId, int paperNumber, AttemptQuestionType type)
    {
        var statement = $"<p>Enunciado {paperNumber}</p>";
        var request = new SaveQuestionRequest(topicId, "pt-BR", statement, ExamEditionId: editionId, PaperNumber: paperNumber);
        IReadOnlyList<SaveQuestionOptionRequest> rights = [new("<p>A</p>"), new("<p>B</p>"), new("<p>C</p>")];

        return type switch
        {
            AttemptQuestionType.SingleChoice => request with
            {
                ExplanationHtml = "<p>SECRET-EXPLANATION</p>",
                Options = [new("<p>Paris</p>", true), new("<p>Lyon</p>"), new("<p>Nice</p>")]
            },
            AttemptQuestionType.TrueFalse => request with { Type = QuestionType.TrueFalse, TrueFalseKey = true },
            AttemptQuestionType.MultipleAnswer => request with
            {
                Type = QuestionType.MultipleAnswer,
                Options = [new("<p>Paris</p>", true), new("<p>Roma</p>", true), new("<p>Cairo</p>"), new("<p>Lima</p>")]
            },
            AttemptQuestionType.Matching => request with
            {
                Type = QuestionType.Matching,
                Options = rights,
                MatchItems = [new("<p>1</p>", 2), new("<p>2</p>", 0), new("<p>3</p>", 1)]
            },
            AttemptQuestionType.FillInTheBlanks => request with
            {
                Type = QuestionType.FillInTheBlanks,
                StatementHtml = "<p>A capital é [[1]] e foi fundada em [[2]].</p>",
                Blanks = [new(1, ["Brasília"]), new(2, ["1960"])]
            },
            AttemptQuestionType.ShortAnswer => request with
            {
                Type = QuestionType.ShortAnswer,
                Blanks = [new(1, ["Brasília"])]
            },
            AttemptQuestionType.OpenAnswer => request with
            {
                Type = QuestionType.OpenAnswer,
                Rubric = [new("Theme", "SECRET-RUBRIC", 10)],
                AnswerSizeMax = 50
            },
            AttemptQuestionType.Essay => request with
            {
                Type = QuestionType.Essay,
                Rubric = [new("Theme", "SECRET-RUBRIC", 10)],
                AnswerSizeMin = 2,
                AnswerSizeMax = 5
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    /// <summary>Creates the question as a draft, then publishes it; returns it as the question bank stored it.</summary>
    protected static async Task<QuestionResponse> PublishedQuestionAsync(
        HttpClient admin, Guid topicId, Guid editionId, int paperNumber, AttemptQuestionType type)
    {
        var created = await admin.PostAsJsonAsync(
            Questions, QuestionOfType(topicId, editionId, paperNumber, type), AppJson.Options);
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var question = (await created.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;

        var published = await admin.PostAsync($"{Questions}/{question.Id}/publish", null);
        published.StatusCode.Should().Be(HttpStatusCode.OK, await published.Content.ReadAsStringAsync());

        return (await published.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
    }

    /// <summary>A draft question of the edition: it is made and never published.</summary>
    protected static async Task<QuestionResponse> DraftQuestionAsync(HttpClient admin, Guid topicId, Guid editionId, int paperNumber)
    {
        var created = await admin.PostAsJsonAsync(
            Questions, QuestionOfType(topicId, editionId, paperNumber, AttemptQuestionType.SingleChoice), AppJson.Options);
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());

        return (await created.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
    }

    /// <summary>A published edition with one published question of each type, numbered from 1 in the order of the enum.</summary>
    protected static async Task<(CatalogEdition Edition, IReadOnlyList<PaperQuestion> Questions)> PublishedEditionWithEveryTypeAsync(HttpClient admin)
    {
        var topicId = await TopicAsync(admin);
        var edition = await EditionAsync(admin, publish: false);
        var questions = new List<PaperQuestion>();
        var number = 1;
        foreach (var type in Enum.GetValues<AttemptQuestionType>())
        {
            var question = await PublishedQuestionAsync(admin, topicId, edition.Id, number, type);
            questions.Add(new PaperQuestion(question.Id, number, type));
            number++;
        }

        await PublishEditionAsync(admin, edition);

        return (edition, questions);
    }

    protected static Task<HttpResponseMessage> StartAsync(HttpClient client, Guid editionId) =>
        client.PostAsJsonAsync(Attempts, new StartAttemptRequest(editionId), AppJson.Options);

    /// <summary>Starts an attempt and expects it to be created (201).</summary>
    protected static async Task<AttemptResponse> StartedAsync(HttpClient client, Guid editionId)
    {
        var response = await StartAsync(client, editionId);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<AttemptResponse>(AppJson.Options))!;
    }

    protected static async Task<AttemptResponse> ReadAsync(HttpClient client, Guid attemptId)
    {
        var response = await client.GetAsync($"{Attempts}/{attemptId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<AttemptResponse>(AppJson.Options))!;
    }

    /// <summary>Saves an answer; the body is the payload as its base type, so the <c>type</c> member is written.</summary>
    protected static Task<HttpResponseMessage> SaveAsync(HttpClient client, Guid attemptId, Guid questionId, AnswerPayload answer) =>
        client.PutAsJsonAsync($"{Attempts}/{attemptId}/answers/{questionId}", answer, AppJson.Options);

    protected static Task<HttpResponseMessage> SaveRawAsync(HttpClient client, Guid attemptId, Guid questionId, string json) =>
        client.PutAsync(
            $"{Attempts}/{attemptId}/answers/{questionId}",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

    protected static async Task SavedAsync(HttpClient client, Guid attemptId, Guid questionId, AnswerPayload answer)
    {
        var response = await SaveAsync(client, attemptId, questionId, answer);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    protected static Task<HttpResponseMessage> ClearAsync(HttpClient client, Guid attemptId, Guid questionId) =>
        client.DeleteAsync($"{Attempts}/{attemptId}/answers/{questionId}");

    protected static Task<HttpResponseMessage> SubmitAsync(HttpClient client, Guid attemptId) =>
        client.PostAsync($"{Attempts}/{attemptId}/submit", null);

    protected static async Task<AttemptResponse> SubmittedAsync(HttpClient client, Guid attemptId)
    {
        var response = await SubmitAsync(client, attemptId);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<AttemptResponse>(AppJson.Options))!;
    }

    /// <summary>The <c>field</c> and <c>code</c> of every entry of the <c>errors</c> member of an answer-invalid body.</summary>
    protected static IReadOnlyList<(string Field, string Code)> ErrorsOf(string problemBody) =>
        [.. JsonDocument.Parse(problemBody).RootElement.GetProperty("errors").EnumerateArray()
            .Select(entry => (entry.GetProperty("field").GetString()!, entry.GetProperty("code").GetString()!))];

    protected static async Task<string> BodyAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(expected, body);

        return body;
    }
}
