using System.Text.Json;
using Simulab.Ai.Contracts;

namespace Simulab.Ai.Tests;

/// <summary>F-41: the gateway's rules, through the path a production call takes.</summary>
public class AnthropicAiGatewayTests
{
    private static AiRequest Ask(string prompt = "Say ok") => new(AiPurposes.Diagnostics, prompt);

    [Fact]
    public async Task CompleteAsync_WithSignedInUserUnderTheLimit_AnswersAndRecordsTheCall()
    {
        await using var host = await AiTestHost.StartAsync(nameof(CompleteAsync_WithSignedInUserUnderTheLimit_AnswersAndRecordsTheCall));
        host.Anthropic.Answers("Hello from the model", inputTokens: 1_000, outputTokens: 200);

        var result = await host.Gateway.CompleteAsync(Ask());

        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().Be("Hello from the model");
        result.Value.Model.Should().Be("claude-opus-5");
        result.Value.InputTokens.Should().Be(1_000);
        result.Value.OutputTokens.Should().Be(200);

        var calls = await host.CallsAsync();
        calls.Should().ContainSingle();
        calls[0].Succeeded.Should().BeTrue();
        calls[0].UserId.Should().Be(host.User.UserId!.Value);
        calls[0].Purpose.Should().Be(AiPurposes.Diagnostics);
        calls[0].ErrorCode.Should().BeNull();
    }

    [Fact]
    public async Task CompleteAsync_WithAJsonSchema_AsksForThatOutputFormat()
    {
        await using var host = await AiTestHost.StartAsync(nameof(CompleteAsync_WithAJsonSchema_AsksForThatOutputFormat));
        host.Anthropic.Answers("""{"name":"Cebraspe"}""", inputTokens: 10, outputTokens: 5);

        var schema = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["type"] = JsonSerializer.SerializeToElement("object"),
            ["properties"] = JsonSerializer.SerializeToElement(new { name = new { type = "string" } }),
            ["required"] = JsonSerializer.SerializeToElement(new[] { "name" })
        };

        var result = await host.Gateway.CompleteAsync(
            new AiRequest(AiPurposes.Diagnostics, "Name the board", JsonSchema: schema));

        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().Be("""{"name":"Cebraspe"}""");
        host.Anthropic.LastRequestBody.Should().Contain("json_schema");
        host.Anthropic.LastRequestBody.Should().Contain("\"name\"");
    }

    [Fact]
    public async Task CompleteAsync_WithNobodySignedIn_FailsWithoutCallingTheModel()
    {
        await using var host = await AiTestHost.StartAsync(nameof(CompleteAsync_WithNobodySignedIn_FailsWithoutCallingTheModel));
        host.User.UserId = null;

        var result = await host.Gateway.CompleteAsync(Ask());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AiErrorCodes.NoUser);
        host.Anthropic.RequestCount.Should().Be(0);
        (await host.CallsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteAsync_WhenTheMonthlyLimitIsReached_FailsWithoutCallingTheModel()
    {
        await using var host = await AiTestHost.StartAsync(nameof(CompleteAsync_WhenTheMonthlyLimitIsReached_FailsWithoutCallingTheModel));
        host.Entitlements.Limit = 2;
        await host.SeedSuccessfulCallsAsync(host.User.UserId!.Value, 2);

        var result = await host.Gateway.CompleteAsync(Ask());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AiErrorCodes.QuotaExceeded);
        host.Anthropic.RequestCount.Should().Be(0);
        (await host.CallsAsync()).Should().HaveCount(2, "the refused call adds no row");
    }

    [Fact]
    public async Task CompleteAsync_BelowTheMonthlyLimit_StillCallsTheModel()
    {
        await using var host = await AiTestHost.StartAsync(nameof(CompleteAsync_BelowTheMonthlyLimit_StillCallsTheModel));
        host.Entitlements.Limit = 3;
        await host.SeedSuccessfulCallsAsync(host.User.UserId!.Value, 2);
        host.Anthropic.Answers("ok", inputTokens: 1, outputTokens: 1);

        var result = await host.Gateway.CompleteAsync(Ask());

        result.IsSuccess.Should().BeTrue();
        host.Anthropic.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task CompleteAsync_WithNoApiKey_FailsWithoutCallingTheModelOrWritingARow()
    {
        await using var host = await AiTestHost.StartAsync(
            nameof(CompleteAsync_WithNoApiKey_FailsWithoutCallingTheModelOrWritingARow),
            apiKey: null);

        var result = await host.Gateway.CompleteAsync(Ask());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AiErrorCodes.NotConfigured);
        host.Anthropic.RequestCount.Should().Be(0);
        (await host.CallsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteAsync_WhenTheApiAnswersWithAnError_FailsWithoutThrowingAndRecordsIt()
    {
        await using var host = await AiTestHost.StartAsync(nameof(CompleteAsync_WhenTheApiAnswersWithAnError_FailsWithoutThrowingAndRecordsIt));
        host.Anthropic.Fails();

        var result = await host.Gateway.CompleteAsync(Ask());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AiErrorCodes.CallFailed);

        var calls = await host.CallsAsync();
        calls.Should().ContainSingle();
        calls[0].Succeeded.Should().BeFalse();
        calls[0].ErrorCode.Should().Be(AiErrorCodes.CallFailed);
        calls[0].CostUsd.Should().Be(0m, "no tokens are known, so nothing is charged");
    }

    [Fact]
    public async Task CompleteAsync_RecordsTheCostFromTheTokensAndThePricesOnTheRow()
    {
        await using var host = await AiTestHost.StartAsync(nameof(CompleteAsync_RecordsTheCostFromTheTokensAndThePricesOnTheRow));
        host.Anthropic.Answers("ok", inputTokens: 1_000_000, outputTokens: 100_000);

        var result = await host.Gateway.CompleteAsync(Ask());

        result.IsSuccess.Should().BeTrue();

        var call = (await host.CallsAsync()).Single();
        call.InputPricePerMillion.Should().Be(5.0m);
        call.OutputPricePerMillion.Should().Be(25.0m);
        // 1M input at $5 plus 100k output at $25 per million.
        call.CostUsd.Should().Be(7.5m);
        call.CostUsd.Should().Be(
            ((call.InputTokens * call.InputPricePerMillion) + (call.OutputTokens * call.OutputPricePerMillion)) / 1_000_000m);
        result.Value.CostUsd.Should().Be(call.CostUsd);
    }

    [Fact]
    public async Task CompleteAsync_ForAPurposeWithItsOwnModel_CallsAndRecordsThatModel()
    {
        await using var host = await AiTestHost.StartAsync(
            nameof(CompleteAsync_ForAPurposeWithItsOwnModel_CallsAndRecordsThatModel),
            settings: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Ai:Models:diagnostics"] = "claude-haiku-4-5",
                ["Ai:Prices:claude-haiku-4-5:InputPerMillion"] = "1.0",
                ["Ai:Prices:claude-haiku-4-5:OutputPerMillion"] = "5.0"
            });
        host.Anthropic.Answers("ok", inputTokens: 1_000_000, outputTokens: 0, model: "claude-haiku-4-5");

        var result = await host.Gateway.CompleteAsync(Ask());

        result.IsSuccess.Should().BeTrue();
        result.Value.Model.Should().Be("claude-haiku-4-5");
        host.Anthropic.LastRequestBody.Should().Contain("claude-haiku-4-5");

        var call = (await host.CallsAsync()).Single();
        call.Model.Should().Be("claude-haiku-4-5");
        call.CostUsd.Should().Be(1.0m);
    }

    [Fact]
    public async Task CompleteAsync_ForAPurposeWithNoModelOfItsOwn_UsesTheDefaultModel()
    {
        await using var host = await AiTestHost.StartAsync(nameof(CompleteAsync_ForAPurposeWithNoModelOfItsOwn_UsesTheDefaultModel));
        host.Anthropic.Answers("ok", inputTokens: 1, outputTokens: 1);

        var result = await host.Gateway.CompleteAsync(new AiRequest("another-purpose", "Say ok"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Model.Should().Be("claude-opus-5");
        host.Anthropic.LastRequestBody.Should().Contain("claude-opus-5");
    }
}
