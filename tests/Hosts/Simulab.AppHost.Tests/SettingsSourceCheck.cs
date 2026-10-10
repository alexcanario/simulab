namespace Simulab.AppHost.Tests;

/// <summary>
/// F-94 BR4 and BR5: whether each required setting of a host has a source in the publish or in the host's committed
/// settings. Pure: it takes the two maps, so a failure is shown on a small fake without the real model.
/// </summary>
internal static class SettingsSourceCheck
{
    /// <summary>What a run found: the keys with no source (they fail the test) and the vault keys it cannot see.</summary>
    internal sealed record Outcome(IReadOnlyList<string> Problems, IReadOnlyList<string> NotVerified);

    /// <param name="host">The host the settings belong to.</param>
    /// <param name="environment">The environment of the publish model, for the message.</param>
    /// <param name="required">The settings of every host; the ones of <paramref name="host"/> are checked.</param>
    /// <param name="publishVariables">The host's environment variables in the publish model (<c>A__B</c> spelling).</param>
    /// <param name="hostSettings">The host's committed settings, flattened to <c>A:B</c> keys.</param>
    internal static Outcome Check(
        SettingsHost host,
        string environment,
        IEnumerable<RequiredSetting> required,
        IReadOnlyDictionary<string, string?> publishVariables,
        IReadOnlyDictionary<string, string?> hostSettings)
    {
        ArgumentNullException.ThrowIfNull(required);
        ArgumentNullException.ThrowIfNull(publishVariables);
        ArgumentNullException.ThrowIfNull(hostSettings);

        var published = publishVariables.ToDictionary(
            pair => pair.Key.Replace("__", ":", StringComparison.Ordinal), pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var committed = new Dictionary<string, string?>(hostSettings, StringComparer.OrdinalIgnoreCase);

        var problems = new List<string>();
        var notVerified = new List<string>();
        foreach (var setting in required.Where(setting => setting.Host == host).DistinctBy(setting => setting.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (setting.Origin == SettingOrigin.KeyVault)
            {
                notVerified.Add($"{host} {environment}: '{setting.Key}' is read from Key Vault and is not verified ({setting.Reason}).");
                continue;
            }

            if (HasValue(published, setting.Key) || HasValue(committed, setting.Key))
            {
                continue;
            }

            problems.Add(
                $"{host} {environment}: '{setting.Key}' has no value in the publish model and none in the host's committed "
                + $"appsettings.json / appsettings.{environment}.json ({setting.Reason}).");
        }

        return new Outcome(problems, notVerified);
    }

    private static bool HasValue(Dictionary<string, string?> map, string key) =>
        map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value);
}
