using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Simulab.Email.Tests;

/// <summary>F-66 AC1 (BR3), AC2 and AC4 (BR4): the provider is picked by configuration, and the cloud refuses a bad one at start.</summary>
public class EmailProviderRegistrationTests
{
    private static ServiceProvider Build(Dictionary<string, string?> settings, bool cloudEnvironment = false)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddEmailSender(configuration, smtpConnectionString: null, cloudEnvironment);
        return services.BuildServiceProvider();
    }

    private static Dictionary<string, string?> Acs(string? endpoint = "https://simulab.communication.azure.com", string? from = "DoNotReply@abc.azurecomm.net") => new()
    {
        ["Email:Provider"] = "AzureCommunicationServices",
        ["Email:AzureCommunicationServices:Endpoint"] = endpoint,
        ["Email:FromAddress"] = from,
        ["AZURE_CLIENT_ID"] = "00000000-0000-0000-0000-000000000001",
    };

    [Theory]
    [InlineData(null)]
    [InlineData("Smtp")]
    public void AddEmailSender_ProviderAbsentOrSmtp_RegistersTheSmtpSender(string? provider)
    {
        var settings = new Dictionary<string, string?> { ["Email:Provider"] = provider, ["Email:FromAddress"] = "no-reply@simulab.app" };
        using var services = Build(settings);
        using var scope = services.CreateScope();

        scope.ServiceProvider.GetRequiredService<IEmailSender>().Should().BeOfType<SmtpEmailSender>();
        services.GetService<EmailClient>().Should().BeNull();
    }

    [Fact]
    public void AddEmailSender_ProviderAzureCommunicationServices_RegistersTheAzureSender()
    {
        using var services = Build(Acs());
        using var scope = services.CreateScope();

        scope.ServiceProvider.GetRequiredService<IEmailSender>().Should().BeOfType<AzureCommunicationServicesEmailSender>();
        services.GetRequiredService<EmailClient>().Should().NotBeNull();
    }

    [Fact]
    public void Options_ClientIdOfTheIdentity_IsReadFromAzureClientId()
    {
        using var services = Build(Acs());

        services.GetRequiredService<IOptions<EmailOptions>>().Value.AzureCommunicationServices.ManagedIdentityClientId
            .Should().Be("00000000-0000-0000-0000-000000000001");
    }

    [Fact]
    public void Validate_CloudWithSmtp_NamesTheProvider()
    {
        var settings = new Dictionary<string, string?> { ["Email:FromAddress"] = "no-reply@simulab.app" };
        using var services = Build(settings, cloudEnvironment: true);

        var read = () => services.GetRequiredService<IOptions<EmailOptions>>().Value;

        read.Should().Throw<OptionsValidationException>().Which.Message.Should().Contain("Email:Provider");
    }

    [Fact]
    public void Validate_CloudWithoutEndpointSenderAndIdentity_NamesEveryMissingKeyAtOnce()
    {
        var settings = new Dictionary<string, string?> { ["Email:Provider"] = "AzureCommunicationServices" };
        using var services = Build(settings, cloudEnvironment: true);

        var read = () => services.GetRequiredService<IOptions<EmailOptions>>().Value;

        read.Should().Throw<OptionsValidationException>().Which.Message
            .Should().Contain("Email:AzureCommunicationServices:Endpoint")
            .And.Contain("Email:FromAddress")
            .And.Contain("AZURE_CLIENT_ID");
    }

    [Fact]
    public void Validate_EndpointWithoutHttps_IsRefused()
    {
        using var services = Build(Acs(endpoint: "http://simulab.communication.azure.com"), cloudEnvironment: true);

        var read = () => services.GetRequiredService<IOptions<EmailOptions>>().Value;

        read.Should().Throw<OptionsValidationException>().Which.Message.Should().Contain("Email:AzureCommunicationServices:Endpoint");
    }

    [Fact]
    public void Validate_CloudWithEveryKey_Passes()
    {
        using var services = Build(Acs(), cloudEnvironment: true);

        services.GetRequiredService<IOptions<EmailOptions>>().Value.Provider.Should().Be(EmailProvider.AzureCommunicationServices);
    }

    [Fact]
    public void Validate_DevelopmentWithSmtp_Passes()
    {
        var settings = new Dictionary<string, string?> { ["Email:FromAddress"] = "no-reply@simulab.app" };
        using var services = Build(settings, cloudEnvironment: false);

        services.GetRequiredService<IOptions<EmailOptions>>().Value.Provider.Should().Be(EmailProvider.Smtp);
    }
}
