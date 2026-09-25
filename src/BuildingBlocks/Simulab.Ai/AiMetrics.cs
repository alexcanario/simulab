using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Simulab.Ai;

/// <summary>
/// The tokens and cost counters of ADR-0001 decision 31, tagged with the purpose, the model and
/// whether the call succeeded (F-41, BR10). The Aspire dashboard shows them locally.
/// </summary>
public sealed class AiMetrics
{
    /// <summary>The meter name a host subscribes to.</summary>
    public const string MeterName = "Simulab.Ai";

    private readonly Counter<long> _calls;
    private readonly Counter<long> _inputTokens;
    private readonly Counter<long> _outputTokens;
    private readonly Counter<double> _costUsd;

    public AiMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        var meter = meterFactory.Create(MeterName);
        _calls = meter.CreateCounter<long>("simulab.ai.calls", "{call}", "Calls that reached the model.");
        _inputTokens = meter.CreateCounter<long>("simulab.ai.input_tokens", "{token}", "Input tokens spent.");
        _outputTokens = meter.CreateCounter<long>("simulab.ai.output_tokens", "{token}", "Output tokens spent.");
        _costUsd = meter.CreateCounter<double>("simulab.ai.cost_usd", "USD", "What the calls cost.");
    }

    /// <summary>Records one call that reached the model, answered or failed.</summary>
    public void Record(AiCall call)
    {
        ArgumentNullException.ThrowIfNull(call);

        var tags = new TagList
        {
            { "purpose", call.Purpose },
            { "model", call.Model },
            { "succeeded", call.Succeeded }
        };

        _calls.Add(1, tags);
        _inputTokens.Add(call.InputTokens, tags);
        _outputTokens.Add(call.OutputTokens, tags);
        _costUsd.Add((double)call.CostUsd, tags);
    }
}
