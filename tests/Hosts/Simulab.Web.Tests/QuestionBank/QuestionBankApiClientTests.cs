using System.Net;
using System.Net.Http.Json;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Web.Services;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>F-95: the typed client's routes, query string and the way it reads a refusal and its option position.</summary>
public sealed class QuestionBankApiClientTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string PathAndQuery, string? Authorization, string? Language)> Received { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Received.Add((
                request.Method,
                request.RequestUri!.PathAndQuery,
                request.Headers.Authorization?.ToString(),
                request.Headers.AcceptLanguage.ToString()));

            return Task.FromResult(answer(request));
        }
    }

    private static (QuestionBankApiClient Client, Handler Handler) Create(Func<HttpRequestMessage, HttpResponseMessage> answer)
    {
        var handler = new Handler(answer);

        return (new QuestionBankApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test") }), handler);
    }

    private static HttpResponseMessage Problem(HttpStatusCode status, string code, int? position = null)
    {
        var body = new Dictionary<string, object> { ["status"] = (int)status, ["code"] = code };
        if (position is { } at)
        {
            body["position"] = at;
        }

        return new HttpResponseMessage(status) { Content = JsonContent.Create(body, options: AppJson.Options) };
    }

    [Fact]
    public async Task List_SendsPageSizeSearchAndStatusAsTheApiNamesThem()
    {
        var (client, handler) = Create(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new QuestionPageResponse([], 0), options: AppJson.Options)
        });

        var result = await client.ListQuestionsAsync("token", new QuestionListQuery(2, 10, "vírgula & crase", [QuestionStatus.Published]));

        result.IsSuccess.Should().BeTrue();
        var call = handler.Received.Single();
        call.Method.Should().Be(HttpMethod.Get);
        call.PathAndQuery.Should().StartWith("/api/v1/question-bank/questions?page=2&pageSize=10")
            .And.Contain("search=v%C3%ADrgula%20%26%20crase")
            .And.Contain("status=Published");
        call.Authorization.Should().Be("Bearer token");
    }

    [Fact]
    public async Task List_NoSearchNoStatus_SendsOnlyThePage()
    {
        var (client, handler) = Create(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new QuestionPageResponse([], 0), options: AppJson.Options)
        });

        await client.ListQuestionsAsync("token", new QuestionListQuery());

        handler.Received.Single().PathAndQuery.Should().Be("/api/v1/question-bank/questions?page=0&pageSize=25");
    }

    [Fact]
    public async Task Routes_PublishUnpublishDeleteFindAndSave_GoWhereTheApiMapsThem()
    {
        var id = Guid.Parse("0198f5a0-0000-7000-8000-000000000001");
        var (client, handler) = Create(request => request.Method == HttpMethod.Delete
            ? new HttpResponseMessage(HttpStatusCode.NoContent)
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { id }, options: AppJson.Options) });

        await client.FindQuestionAsync("t", id);
        await client.CreateQuestionAsync("t", new SaveQuestionRequest());
        await client.UpdateQuestionAsync("t", id, new SaveQuestionRequest());
        await client.PublishQuestionAsync("t", id);
        await client.UnpublishQuestionAsync("t", id);
        var deleted = await client.DeleteQuestionAsync("t", id);

        deleted.IsSuccess.Should().BeTrue();
        handler.Received.Select(call => (call.Method.Method, call.PathAndQuery)).Should().Equal(
            ("GET", $"/api/v1/question-bank/questions/{id}"),
            ("POST", "/api/v1/question-bank/questions"),
            ("PUT", $"/api/v1/question-bank/questions/{id}"),
            ("POST", $"/api/v1/question-bank/questions/{id}/publish"),
            ("POST", $"/api/v1/question-bank/questions/{id}/unpublish"),
            ("DELETE", $"/api/v1/question-bank/questions/{id}"));
    }

    [Fact]
    public async Task Refusal_CarriesTheStableCode_AndTheOptionPositionWhenTheProblemHasOne()
    {
        var (client, _) = Create(_ => Problem(HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionOptionTextRequired, 3));

        var result = await client.CreateQuestionAsync("t", new SaveQuestionRequest());

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("question.option_text_required");
        result.OptionPosition.Should().Be(3);
    }

    [Fact]
    public async Task Refusal_WithoutAPosition_HasNone()
    {
        var (client, _) = Create(_ => Problem(HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionDeleteRequiresDraft));

        var result = await client.DeleteQuestionAsync("t", Guid.NewGuid());

        result.ErrorCode.Should().Be("question.delete_requires_draft");
        result.OptionPosition.Should().BeNull();
    }

    [Fact]
    public async Task BrokenCall_IsTheGenericCode_NotAnException()
    {
        var (client, _) = Create(_ => throw new HttpRequestException("down"));

        var result = await client.FindQuestionAsync("t", Guid.NewGuid());

        result.ErrorCode.Should().Be("common.unexpected_error");
    }

    [Fact]
    public async Task ABodyThatIsNotAProblem_IsTheGenericCode()
    {
        var (client, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("boom") });

        var result = await client.FindQuestionAsync("t", Guid.NewGuid());

        result.ErrorCode.Should().Be("common.unexpected_error");
    }
}
