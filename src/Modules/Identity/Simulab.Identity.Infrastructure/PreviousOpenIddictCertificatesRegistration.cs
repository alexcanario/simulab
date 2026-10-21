using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;

namespace Simulab.Identity.Infrastructure;

internal static class PreviousOpenIddictCertificatesRegistration
{
    /// <summary>
    /// F-84 BR1, BR2: besides the current certificates, OpenIddict also reads tokens made with the previous ones. They are added
    /// as plain credentials: OpenIddict puts the one that expires last first (it signs and encrypts) and keeps the others for
    /// validation and decryption, whatever the order they were registered in. The previous ones come from the version source
    /// the host registered, never from configuration (T5).
    /// </summary>
    internal static IServiceCollection AddPreviousOpenIddictCertificates(
        this IServiceCollection services,
        IConfiguration configuration,
        X509Certificate2 signing,
        X509Certificate2 encryption)
    {
        services.AddOptions<PreviousCertificateOptions>();
        services.AddSingleton(new CurrentOpenIddictCertificates(signing, encryption));
        services.AddSingleton(provider => PreviousOpenIddictCertificates.Load(
            configuration,
            provider.GetService<ICertificateVersionSource>(),
            provider.GetRequiredService<IOptions<PreviousCertificateOptions>>().Value,
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<PreviousOpenIddictCertificates>()));
        services.AddOptions<OpenIddictServerOptions>().Configure<PreviousOpenIddictCertificates>((options, previous) =>
        {
            if (previous.Signing is not null)
            {
                options.SigningCredentials.Add(new SigningCredentials(new X509SecurityKey(previous.Signing), SecurityAlgorithms.RsaSha256));
            }

            if (previous.Encryption is not null)
            {
                options.EncryptionCredentials.Add(new EncryptingCredentials(new X509SecurityKey(previous.Encryption), SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.Aes256CbcHmacSha512));
            }
        });
        services.AddHostedService<OpenIddictCertificateReporter>();
        return services;
    }
}
