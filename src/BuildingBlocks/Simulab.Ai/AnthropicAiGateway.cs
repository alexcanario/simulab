using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulab.Ai.Contracts;
using Simulab.Ai.Persistence;
using Simulab.Plans.Contracts;
using Simulab.SharedKernel.Results;
using Simulab.SharedKernel.Security;

namespace Simulab.Ai;

/// <summary>
/// The one implementation of <see cref="IAiGateway"/>: the Claude API behind the checks of F-41.
/// The order matters — who, then how much is left, then whether there is a key — so that a call the
/// app must not make never leaves it and never becomes a row (BR2, BR3, BR4, BR5).
/// </summary>
public sealed class AnthropicAiGateway(
    AnthropicClient client,
    AiDbContext database,
    IEntitlementService entitlements,
    ICurrentUser currentUser,
    AiMetrics metrics,
    TimeProvider timeProvider,
    IOptions<AiOptions> options,
    ILogger<AnthropicAiGateway> logger) : IAiGateway
{
    private readonly AiOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public async Task<Result<AiCompletion>> CompleteAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (currentUser.UserId is not { } userId)
        {
            return Failure(AiErrorCodes.NoUser, ErrorKind.Forbidden, "No user is signed in.");
        }

        if (await IsOverQuotaAsync(userId, cancellationToken).ConfigureAwait(false))
        {
            return Failure(AiErrorCodes.QuotaExceeded, ErrorKind.BusinessRule, "The plan allows no more calls this month.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return Failure(AiErrorCodes.NotConfigured, ErrorKind.BusinessRule, "No API key is configured.");
        }

        var model = _options.ModelFor(request.Purpose);
        var startedAt = timeProvider.GetUtcNow();
        var started = timeProvider.GetTimestamp();

        try
        {
            var message = await client.Messages.Create(Parameters(request, model), cancellationToken).ConfigureAwait(false);
            var elapsed = timeProvider.GetElapsedTime(started);

            var call = AiCall.ForAnswer(
                userId,
                request.Purpose,
                model,
                (int)message.Usage.InputTokens,
                (int)message.Usage.OutputTokens,
                _options.PriceOf(model),
                elapsed,
                startedAt);

            await RecordAsync(call, cancellationToken).ConfigureAwait(false);

            return Result.Success(new AiCompletion(
                AnswerOf(message),
                model,
                call.InputTokens,
                call.OutputTokens,
                call.CostUsd,
                elapsed));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // BR8: a failure of the call is a result the caller can show, never an exception it must catch.
            var elapsed = timeProvider.GetElapsedTime(started);
            logger.LogError(exception, "The call to {Model} for {Purpose} failed.", model, request.Purpose);

            await RecordAsync(
                AiCall.ForFailure(userId, request.Purpose, model, AiErrorCodes.CallFailed, elapsed, startedAt),
                cancellationToken).ConfigureAwait(false);

            return Failure(AiErrorCodes.CallFailed, ErrorKind.BusinessRule, exception.Message);
        }
    }

    /// <summary>BR3: the plan's ceiling against what this user already spent this calendar month, in UTC.</summary>
    private async Task<bool> IsOverQuotaAsync(Guid userId, CancellationToken cancellationToken)
    {
        var limit = await entitlements
            .GetLimitAsync(userId, PlanFeatures.AiCallsPerMonth, cancellationToken)
            .ConfigureAwait(false);

        if (limit is not { } ceiling)
        {
            return false;
        }

        var now = timeProvider.GetUtcNow();
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var used = await database.Calls
            .Where(call => call.UserId == userId && call.Succeeded && call.StartedAt >= monthStart)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        return used >= ceiling;
    }

    private async Task RecordAsync(AiCall call, CancellationToken cancellationToken)
    {
        database.Calls.Add(call);
        await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        metrics.Record(call);
    }

    private MessageCreateParams Parameters(AiRequest request, string model) =>
        new()
        {
            Model = model,
            MaxTokens = _options.MaxTokens,
            Messages = [new() { Role = Role.User, Content = request.Prompt }],
            System = string.IsNullOrWhiteSpace(request.System)
                ? (MessageCreateParamsSystem?)null
                : request.System,
            OutputConfig = request.JsonSchema is { Count: > 0 } schema
                ? new OutputConfig
                {
                    Format = new JsonOutputFormat
                    {
                        Schema = new Dictionary<string, System.Text.Json.JsonElement>(schema)
                    }
                }
                : null
        };

    private static string AnswerOf(Message message) =>
        string.Concat(message.Content.Select(block => block.Value).OfType<TextBlock>().Select(text => text.Text));

    private static Result<AiCompletion> Failure(string code, ErrorKind kind, string detail) =>
        Result.Failure<AiCompletion>(new Error(code, kind, detail));
}
