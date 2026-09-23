using System.Text.RegularExpressions;

namespace Simulab.Web.Tests.Localization;

/// <summary>One failure of <see cref="ErrorCodeTextCheck"/>: what is wrong, and the line the report prints.</summary>
public sealed record ErrorCodeTextFailure(string Subject, string Detail)
{
    public override string ToString() => $"{Subject}: {Detail}";
}

/// <summary>
/// F-22: every API error code has a text in every language, and every code-shaped text belongs to a code.
/// A plain check over its inputs, so the rule itself can be tested with made-up data (BR5) and the test that
/// runs it over the real solution is one more case.
/// </summary>
public static partial class ErrorCodeTextCheck
{
    /// <summary>BR4: the Web's own messages live under this prefix and answer to no error code.</summary>
    public const string WebOwnPrefix = "common.";

    /// <summary>What the failures tell a developer to do (BR6).</summary>
    public const string WhatToDo =
        "Add the text to src/Hosts/Simulab.Web/Resources/SharedResources[.pt-BR|.pt-PT].resx, "
        + "or, when the app never shows this code to a person, add it to ErrorCodeTextTests.NeverShown with the reason.";

    /// <param name="codes">Every error code constant found in the solution (BR1).</param>
    /// <param name="textsByCulture">The resource keys of each culture, by culture name (BR2).</param>
    /// <param name="neverShown">The exempt codes, each with the reason it is never shown (BR3).</param>
    /// <returns>Every failure, in a stable order; empty when the rule holds.</returns>
    public static IReadOnlyList<ErrorCodeTextFailure> Run(
        IReadOnlyCollection<string> codes,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> textsByCulture,
        IReadOnlyDictionary<string, string> neverShown)
    {
        ArgumentNullException.ThrowIfNull(codes);
        ArgumentNullException.ThrowIfNull(textsByCulture);
        ArgumentNullException.ThrowIfNull(neverShown);

        var failures = new List<ErrorCodeTextFailure>();

        // BR3: an exemption with no reason is not an exemption, and one for a code that no longer exists is debt.
        foreach (var exempt in neverShown.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(exempt.Value))
            {
                failures.Add(new ErrorCodeTextFailure(exempt.Key, "is exempt with no reason; say why the app never shows it."));
            }

            if (!codes.Contains(exempt.Key))
            {
                failures.Add(new ErrorCodeTextFailure(exempt.Key, "is exempt but is no longer an error code; remove it from the list."));
            }
        }

        // BR2: every code that is shown has its text in every culture.
        foreach (var code in codes.Where(code => !neverShown.ContainsKey(code)).OrderBy(code => code, StringComparer.Ordinal))
        {
            var missing = textsByCulture
                .Where(culture => !culture.Value.Contains(code))
                .Select(culture => culture.Key)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            if (missing.Count > 0)
            {
                failures.Add(new ErrorCodeTextFailure(code, $"has no text in {string.Join(", ", missing)}. {WhatToDo}"));
            }
        }

        // BR4: the other direction, over every culture, so a text left in one file alone is found too.
        foreach (var culture in textsByCulture.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            var orphans = culture.Value
                .Where(IsCodeShaped)
                .Where(key => !key.StartsWith(WebOwnPrefix, StringComparison.Ordinal))
                .Where(key => !codes.Contains(key))
                .OrderBy(key => key, StringComparer.Ordinal);

            foreach (var orphan in orphans)
            {
                failures.Add(new ErrorCodeTextFailure(
                    orphan,
                    $"is a text in {culture.Key} that answers to no error code; remove it, or restore the code it belonged to."));
            }
        }

        return failures;
    }

    /// <summary>The shape of an error code (rule: naming): snake_case words, separated by dots.</summary>
    public static bool IsCodeShaped(string key) => key is not null && CodeShape().IsMatch(key);

    [GeneratedRegex("^[a-z][a-z0-9_]*(\\.[a-z][a-z0-9_]*)+$")]
    private static partial Regex CodeShape();
}
