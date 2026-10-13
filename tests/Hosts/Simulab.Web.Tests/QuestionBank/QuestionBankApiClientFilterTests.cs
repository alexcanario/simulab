using System.Net;
using System.Net.Http.Json;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Web.Services;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>F-100: the typed client writes the filters of the list as repeated keys and reads the two lookups.</summary>
public sealed class QuestionBankApiClientFilterTests
{
    private static readonly Guid TopicA = Guid.Parse("0198f5a0-0000-7000-8000-0000000000b1");
    private static readonly Guid TopicB = Guid.Parse("0198f5a0-0000-7000-8000-0000000000b2");
    private static readonly Guid Subject = Guid.Parse("0198f5a0-0000-7000-8000-0000000000a1");
    private static readonly Guid Organizer = Guid.Parse("0198f5a0-0000-7000-8000-0000000000c1");
    private static readonly Guid Edition = Guid.Parse("0198f5a0-0000-7000-8000-0000000000e1");

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string PathAndQuery, string? Authorization)> Received { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Received.Add((request.Method, request.RequestUri!.PathAndQuery, request.Headers.Authorization?.ToString()));

            return Task.FromResult(answer(request));
        }
    }

    private static (QuestionBankApiClient Client, Handler Handler) Create(Func<HttpRequestMessage, HttpResponseMessage> answer)
    {
        var handler = new Handler(answer);

        return (new QuestionBankApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test") }), handler);
    }

    private static HttpResponseMessage Json<T>(T value) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(value, options: AppJson.Options) };

    [Fact]
    public async Task List_EveryFilter_IsWrittenAsARepeatedKeyTheApiNames()
    {
        var (client, handler) = Create(_ => Json(new QuestionPageResponse([], 0)));
        var query = new QuestionListQuery(
            1,
            50,
            "crase",
            [QuestionStatus.Draft, QuestionStatus.Published],
            [QuestionType.TrueFalse],
            [2, 5],
            DifficultyNotSet: true,
            SubjectIds: [Subject],
            TopicIds: [TopicA, TopicB],
            OrganizerIds: [Organizer],
            ExamEditionId: Edition);

        var result = await client.ListQuestionsAsync("token", query);

        result.IsSuccess.Should().BeTrue();
        var call = handler.Received.Single().PathAndQuery;
        call.Should().StartWith("/api/v1/question-bank/questions?page=1&pageSize=50")
            .And.Contain("search=crase")
            .And.Contain("status=Draft").And.Contain("status=Published")
            .And.Contain("type=TrueFalse")
            .And.Contain("difficulty=2").And.Contain("difficulty=5").And.Contain("difficulty=none")
            .And.Contain($"subject={Subject}")
            .And.Contain($"topic={TopicA}").And.Contain($"topic={TopicB}")
            .And.Contain($"organizer={Organizer}")
            .And.Contain($"edition={Edition}");
    }

    [Fact]
    public async Task List_AHundredPickedIds_StayInsideTheRequestLineOfTheHosts()
    {
        var (client, handler) = Create(_ => Json(new QuestionPageResponse([], 0)));
        var ids = Enumerable.Range(0, 100).Select(_ => Guid.CreateVersion7()).ToList();

        await client.ListQuestionsAsync("token", new QuestionListQuery(
            0, 25, null, TopicIds: ids[..60], OrganizerIds: ids[60..80], SubjectIds: ids[80..]));

        // The Kestrel request line is 8 KB; a hundred ids and the keys have to leave room for the rest.
        var call = handler.Received.Single().PathAndQuery;
        call.Length.Should().BeLessThan(6_000);
        ids.Should().OnlyContain(id => call.Contains(id.ToString(), StringComparison.Ordinal), "every picked id is on the address");
    }

    [Fact]
    public async Task GetTaxonomy_ReadsTheSubjectsWithTheirTopics_FromTheQuestionBankRoute()
    {
        var taxonomy = new List<QuestionTaxonomySubjectResponse>
        {
            new(Subject, "Português", [new TopicLookupResponse(TopicA, "Crase")])
        };
        var (client, handler) = Create(_ => Json(taxonomy));

        var result = await client.GetTaxonomyAsync("token");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.Topics.Should().ContainSingle().Which.Name.Should().Be("Crase");
        var call = handler.Received.Single();
        call.Method.Should().Be(HttpMethod.Get);
        call.PathAndQuery.Should().Be("/api/v1/question-bank/taxonomy");
        call.Authorization.Should().Be("Bearer token");
    }

    [Fact]
    public async Task ListOrganizers_ReadsThemFromTheQuestionBankRoute()
    {
        var (client, handler) = Create(_ => Json(new List<OrganizerLookupResponse> { new(Organizer, "CEBRASPE", "Cebraspe") }));

        var result = await client.ListOrganizersAsync("token");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.Acronym.Should().Be("CEBRASPE");
        handler.Received.Single().PathAndQuery.Should().Be("/api/v1/question-bank/organizers");
    }

    [Fact]
    public async Task GetTaxonomy_ARefusal_IsTheCodeAndNoValue()
    {
        var (client, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = JsonContent.Create(new Dictionary<string, object> { ["status"] = 403, ["code"] = "identity.forbidden" }, options: AppJson.Options)
        });

        var result = await client.GetTaxonomyAsync("token");

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("identity.forbidden");
    }
}
