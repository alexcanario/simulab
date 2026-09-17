using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Simulab.Email;

public static class EmailServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SMTP sender with the settings of the <c>Email</c> section. When the host provides an
    /// SMTP connection string (the Mailpit resource in development), its host and port win.
    /// </summary>
    public static IServiceCollection AddEmailSender(
        this IServiceCollection services,
        IConfiguration configuration,
        string? smtpConnectionString = null)
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
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        return services;
    }
}
