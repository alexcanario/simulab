using Azure.Communication.Email;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Simulab.Email;

public static class EmailServiceCollectionExtensions
{
    /// <summary>
    /// Registers the sender chosen by <c>Email:Provider</c> (F-66 BR3): SMTP by default, or Azure Communication
    /// Services. When the host provides an SMTP connection string (the Mailpit resource in development), its host and
    /// port win. <paramref name="cloudEnvironment"/> (every environment but Development) makes a start fail when the
    /// provider or its settings are missing (BR4).
    /// </summary>
    public static IServiceCollection AddEmailSender(
        this IServiceCollection services,
        IConfiguration configuration,
        string? smtpConnectionString = null,
        bool cloudEnvironment = false)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .Configure(options =>
            {
                if (Uri.TryCreate(smtpConnectionString, UriKind.Absolute, out var smtp))
                {
                    options.Host = smtp.Host;
                    options.Port = smtp.IsDefaultPort ? options.Port : smtp.Port;
                }

                // The cloud sets AZURE_CLIENT_ID on a host that has its managed identity attached.
                options.AzureCommunicationServices.ManagedIdentityClientId ??= configuration["AZURE_CLIENT_ID"];
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<EmailOptions>>(new EmailOptionsValidator(cloudEnvironment)));

        var provider = configuration.GetSection(EmailOptions.SectionName).GetValue(nameof(EmailOptions.Provider), EmailProvider.Smtp);
        if (provider == EmailProvider.AzureCommunicationServices)
        {
            services.AddSingleton(serviceProvider =>
            {
                var azure = serviceProvider.GetRequiredService<IOptions<EmailOptions>>().Value.AzureCommunicationServices;
                var credential = new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(azure.ManagedIdentityClientId!));
                return new EmailClient(new Uri(azure.Endpoint!), credential);
            });
            services.AddScoped<IEmailSender, AzureCommunicationServicesEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }

        return services;
    }
}
