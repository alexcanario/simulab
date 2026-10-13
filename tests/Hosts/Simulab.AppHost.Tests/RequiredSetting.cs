namespace Simulab.AppHost.Tests;

/// <summary>A setting <paramref name="Host"/> needs outside Development, spelled as the configuration spells it (<c>A:B:C</c>).</summary>
internal sealed record RequiredSetting(SettingsHost Host, string Key, SettingOrigin Origin, string Reason);
