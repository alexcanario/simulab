namespace Simulab.AppHost.Tests;

/// <summary>Where a required setting is meant to come from in the cloud (F-94 BR4).</summary>
internal enum SettingOrigin
{
    /// <summary>The publish model or the host's committed settings; the check looks for it there.</summary>
    Deployed,

    /// <summary>A secret the owner keeps in Key Vault. The model cannot see the vault, so it is listed and not verified.</summary>
    KeyVault,
}
