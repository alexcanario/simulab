using System.Reflection;

namespace Simulab.AppHost.Tests;

/// <summary>
/// F-94 BR2 and BR3: what each deployed host needs outside Development. Settings that live in an options class are found
/// by <see cref="RequiredOptionsScanner"/> (every <c>[Required]</c> property with no default); this file names the options
/// classes each host binds, and lists by hand what is read outside options or has a development default that is wrong in
/// the cloud. A new required option that no host names fails <c>RequiredSettingsTests</c>.
/// </summary>
internal static class RequiredSettingsCatalog
{
    private const string VaultReason = "the owner keeps it in the vault (docs/infra.md); the Api refuses to start without it";

    /// <summary>The options classes each host binds, by full name (a class can be internal).</summary>
    internal static readonly IReadOnlyDictionary<SettingsHost, string[]> OptionTypes = new Dictionary<SettingsHost, string[]>
    {
        [SettingsHost.Api] =
        [
            "Simulab.Email.EmailOptions",
        ],
        [SettingsHost.Web] =
        [
            "Simulab.Web.Services.Auth.OpenIddictClientOptions",
        ],
    };

    /// <summary>
    /// Options classes with a required property and no default that no host binds as such, each with the reason (BR3). An
    /// entry here is a decision; a new class is never added to this list to make the test pass.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> ExemptOptionTypes = new Dictionary<string, string>();

    /// <summary>What is read outside an options class, or has a development default that is wrong in the cloud (BR2 b).</summary>
    internal static readonly IReadOnlyList<RequiredSetting> Explicit =
    [
        Api("ConnectionStrings:simulab", "Program.cs: the host stops without it"),
        Api("ConnectionStrings:redis", "Program.cs: AddRedisClient(\"redis\"), the sessions and the revocation set"),
        Api("ConnectionStrings:keys", "the Data Protection key ring is a blob in the cloud (F-64 BR4)"),
        Api("ConnectionStrings:keyvault", "the Api reads its secrets from the vault as configuration (F-64 BR7)"),
        Api("DataProtection:KeyVaultKeyId", "the key that encrypts the key ring at rest (F-64 D9)"),
        Api("Authentication:OpenIddict:ClientSecret", "IdentityModule.cs: the same secret the Web sends (F-5)"),
        Api("Identity:VerificationUrl", "its development default is a localhost link, silently wrong in the cloud (F-4)"),
        Api("Identity:PasswordResetUrl", "its development default is a localhost link, silently wrong in the cloud (F-7)"),
        Api("Identity:ForgotPasswordUrl", "its development default is a localhost link, silently wrong in the cloud (F-7)"),
        Api("Identity:SignUpUrl", "its development default is a localhost link, silently wrong in the cloud (F-10)"),
        Api("Email:Provider", "outside Development it must be AzureCommunicationServices (F-66 BR3)"),
        Api("Email:AzureCommunicationServices:Endpoint", "the address of the Communication Service, set from the Bicep output (F-66)"),

        Web("ConnectionStrings:redis", "Program.cs: AddRedisClient(\"redis\"), the web sessions (B-3)"),
        Web("ConnectionStrings:keys", "the Data Protection key ring is a blob in the cloud (F-64 BR4)"),
        Web("DataProtection:KeyVaultKeyId", "the key that encrypts the key ring at rest (F-64 D9)"),
        Web("Authentication:OpenIddict:ClientSecret", "the client secret the Web sends to the token endpoint (F-5)"),
        Web("services:api:https:0", "service discovery: the address of the Api behind 'https+http://api'"),

        new(SettingsHost.Api, "OpenIddict:SigningCertificate", SettingOrigin.KeyVault, VaultReason),
        new(SettingsHost.Api, "OpenIddict:EncryptionCertificate", SettingOrigin.KeyVault, VaultReason),
    ];

    /// <summary>The settings of <paramref name="host"/>: the explicit list plus the required keys of its options classes.</summary>
    internal static IReadOnlyList<RequiredSetting> For(SettingsHost host, IReadOnlyList<RequiredOptionsScanner.RequiredOption> scanned)
    {
        ArgumentNullException.ThrowIfNull(scanned);

        var named = OptionTypes[host];
        var fromOptions = scanned
            .Where(option => named.Contains(option.Type.FullName))
            .SelectMany(option => option.FullKeys.Select(key =>
                new RequiredSetting(host, key, SettingOrigin.Deployed, $"[Required] on {option.Type.Name}, no default")));

        return [.. Explicit.Where(setting => setting.Host == host), .. fromOptions];
    }

    /// <summary>
    /// The assemblies the two hosts load of this solution: <c>Simulab.Api</c> and <c>Simulab.Web</c> and every
    /// <c>Simulab.*</c> assembly they reference, loaded by name.
    /// </summary>
    internal static IReadOnlyList<Assembly> HostAssemblies()
    {
        var loaded = new Dictionary<string, Assembly>(StringComparer.Ordinal);
        var pending = new Queue<AssemblyName>([new AssemblyName("Simulab.Api"), new AssemblyName("Simulab.Web")]);
        while (pending.Count > 0)
        {
            var name = pending.Dequeue();
            if (name.Name is null || loaded.ContainsKey(name.Name))
            {
                continue;
            }

            var assembly = Assembly.Load(name);
            loaded[name.Name] = assembly;
            foreach (var reference in assembly.GetReferencedAssemblies().Where(reference => reference.Name?.StartsWith("Simulab.", StringComparison.Ordinal) == true))
            {
                pending.Enqueue(reference);
            }
        }

        return [.. loaded.Values];
    }

    private static RequiredSetting Api(string key, string reason) => new(SettingsHost.Api, key, SettingOrigin.Deployed, reason);

    private static RequiredSetting Web(string key, string reason) => new(SettingsHost.Web, key, SettingOrigin.Deployed, reason);
}
