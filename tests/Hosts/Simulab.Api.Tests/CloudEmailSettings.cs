using Microsoft.AspNetCore.Hosting;

namespace Simulab.Api.Tests;

/// <summary>The email settings a cloud environment sets (F-66): Azure Communication Services, its endpoint, sender and identity.</summary>
internal static class CloudEmailSettings
{
    internal static void Apply(IWebHostBuilder builder)
    {
        builder.UseSetting("Email:Provider", "AzureCommunicationServices");
        builder.UseSetting("Email:AzureCommunicationServices:Endpoint", "https://simulab-test.communication.azure.com");
        builder.UseSetting("Email:FromAddress", "DoNotReply@test.azurecomm.net");
        builder.UseSetting("AZURE_CLIENT_ID", "00000000-0000-0000-0000-000000000001");
    }
}
