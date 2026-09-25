using System.Net;
using System.Text;

namespace Simulab.Ai.Tests;

/// <summary>
/// Stands in for api.anthropic.com. The gateway runs its real code — the SDK serializes the request,
/// sends it and reads the answer — and only the wire is replaced, so the tests exercise the path a
/// production call takes. It records every request, which is how "the model was not called" is proven.
/// </summary>
public sealed class StubAnthropicHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _answers = new();

    /// <summary>How many requests reached the wire.</summary>
    public int RequestCount { get; private set; }

    /// <summary>The body of the last request, as the SDK wrote it.</summary>
    public string LastRequestBody { get; private set; } = string.Empty;

    /// <summary>The next call answers with this message, these tokens and this model.</summary>
    public StubAnthropicHandler Answers(string text, int inputTokens, int outputTokens, string model = "claude-opus-5")
    {
        var body = $$"""
            {
              "id": "msg_test",
              "type": "message",
              "role": "assistant",
              "model": "{{model}}",
              "content": [{ "type": "text", "text": {{System.Text.Json.JsonSerializer.Serialize(text)}} }],
              "stop_reason": "end_turn",
              "usage": { "input_tokens": {{inputTokens}}, "output_tokens": {{outputTokens}} }
            }
            """;

        _answers.Enqueue(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
        return this;
    }

    /// <summary>The next call fails the way an overloaded or rejected request does.</summary>
    public StubAnthropicHandler Fails(HttpStatusCode status = HttpStatusCode.BadRequest)
    {
        var body = """{ "type": "error", "error": { "type": "invalid_request_error", "message": "test failure" } }""";

        _answers.Enqueue(() => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        RequestCount++;
        LastRequestBody = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return _answers.Count > 0
            ? _answers.Dequeue()()
            : new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "id": "msg_test", "type": "message", "role": "assistant", "model": "claude-opus-5",
                      "content": [{ "type": "text", "text": "ok" }],
                      "stop_reason": "end_turn",
                      "usage": { "input_tokens": 1, "output_tokens": 1 }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
    }
}
