using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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

    // B-26: a host that must fail at start is not started here. The framework races on a failed start and can surface
    // ObjectDisposedException instead of the validation error, so the host starts with every key and the test asks
    // the validator the host registered (what Program.cs wires: cloudEnvironment is !IsDevelopment()).
    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Validator_CloudWithSmtp_RefusesAndNamesTheProvider(string environment)
    {
        using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            CloudSettings.Apply(builder);
        });
        var options = new EmailOptions { Provider = EmailProvider.Smtp, FromAddress = "no-reply@simulab.app" };

        var result = RegisteredValidator(host).Validate(name: null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Email:Provider");
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Validator_CloudWithAzureButNoEndpoint_RefusesAndNamesTheKey(string environment)
    {
        using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            CloudSettings.Apply(builder);
        });
        var options = new EmailOptions
        {
            Provider = EmailProvider.AzureCommunicationServices,
            FromAddress = "DoNotReply@test.azurecomm.net",
            AzureCommunicationServices = { Endpoint = string.Empty, ManagedIdentityClientId = "00000000-0000-0000-0000-000000000001" },
        };

        var result = RegisteredValidator(host).Validate(name: null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Email:AzureCommunicationServices:Endpoint");
    }

    [Fact]
    public void Validator_DevelopmentWithSmtp_Passes()
    {
        using var host = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        var options = new EmailOptions { Provider = EmailProvider.Smtp, FromAddress = "no-reply@simulab.app" };

        var result = RegisteredValidator(host).Validate(name: null, options);

        result.Succeeded.Should().BeTrue();
    }

    private static EmailOptionsValidator RegisteredValidator(WebApplicationFactory<Program> host) =>
        host.Services.GetServices<IValidateOptions<EmailOptions>>().OfType<EmailOptionsValidator>().Single();

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Start_CloudWithEveryKey_RegistersTheAzureSender(string environment)
    {
        using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            CloudSettings.Apply(builder);
        });
        using var scope = host.Services.CreateScope();

        scope.ServiceProvider.GetRequiredService<IEmailSender>().Should().BeOfType<AzureCommunicationServicesEmailSender>();
    }
}
