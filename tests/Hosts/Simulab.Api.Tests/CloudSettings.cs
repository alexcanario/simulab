using Microsoft.AspNetCore.Hosting;
using Simulab.Testing;

namespace Simulab.Api.Tests;

/// <summary>
/// What a cloud environment sets and a host outside Development refuses to start without: the email settings
/// (F-66: Azure Communication Services, its endpoint, sender and identity) and the OpenIddict certificates (F-64 BR4).
/// </summary>
internal static class CloudSettings
{
    private static readonly TestCertificates Certificates = TestCertificates.Create();

    internal static void ApplyEmail(IWebHostBuilder builder)
    {
        builder.UseSetting("Email:Provider", "AzureCommunicationServices");
        builder.UseSetting("Email:AzureCommunicationServices:Endpoint", "https://simulab-test.communication.azure.com");
        builder.UseSetting("Email:FromAddress", "DoNotReply@test.azurecomm.net");
        builder.UseSetting("AZURE_CLIENT_ID", "00000000-0000-0000-0000-000000000001");
    }

    internal static void ApplyCertificates(IWebHostBuilder builder)
    {
        builder.UseSetting("OpenIddict:SigningCertificate", Certificates.Signing);
        builder.UseSetting("OpenIddict:EncryptionCertificate", Certificates.Encryption);
    }

    /// <summary>Everything a Staging or Production host needs to start.</summary>
    internal static void Apply(IWebHostBuilder builder)
    {
        ApplyEmail(builder);
        ApplyCertificates(builder);
    }
}
