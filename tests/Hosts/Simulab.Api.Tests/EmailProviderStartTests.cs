using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulab.Email;

namespace Simulab.Api.Tests;

/// <summary>
/// F-66 AC2 and AC4: Development keeps SMTP to Mailpit; Staging and Production start only with the cloud email settings,
/// and a refusal names every key that is missing.
/// </summary>
public class EmailProviderStartTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public void Start_Development_RegistersTheSmtpSender()
    {
        using var host = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var scope = host.Services.CreateScope();

        scope.ServiceProvider.GetRequiredService<IEmailSender>().Should().BeOfType<SmtpEmailSender>();
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Start_CloudWithSmtp_RefusesAndNamesTheProvider(string environment)
    {
        using var host = factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment));

        var start = () => host.Services.GetRequiredService<IOptions<EmailOptions>>().Value;

        start.Should().Throw<OptionsValidationException>().Which.Message.Should().Contain("Email:Provider");
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Start_CloudWithAzureButNoEndpoint_RefusesAndNamesTheKey(string environment)
    {
        using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            CloudEmailSettings.Apply(builder);
            builder.UseSetting("Email:AzureCommunicationServices:Endpoint", string.Empty);
        });

        var start = () => host.Services.GetRequiredService<IOptions<EmailOptions>>().Value;

        start.Should().Throw<OptionsValidationException>().Which.Message
            .Should().Contain("Email:AzureCommunicationServices:Endpoint");
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Start_CloudWithEveryKey_RegistersTheAzureSender(string environment)
    {
        using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            CloudEmailSettings.Apply(builder);
        });
        using var scope = host.Services.CreateScope();

        scope.ServiceProvider.GetRequiredService<IEmailSender>().Should().BeOfType<AzureCommunicationServicesEmailSender>();
    }
}
