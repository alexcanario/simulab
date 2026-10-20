using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Simulab.Identity.Infrastructure;

/// <summary>
/// F-84 BR11: loads the previous certificates at start (so the vault is read when the host starts, not on the first token) and
/// logs the thumbprint of every certificate each role trusts. Thumbprints are public; the values never reach the log.
/// </summary>
internal sealed class OpenIddictCertificateReporter(
    CurrentOpenIddictCertificates current,
    PreviousOpenIddictCertificates previous,
    ILogger<OpenIddictCertificateReporter> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "OpenIddict signing certificates: current {Current}, previous {Previous}",
            current.Signing.Thumbprint,
            previous.Signing?.Thumbprint ?? "none");
        logger.LogInformation(
            "OpenIddict encryption certificates: current {Current}, previous {Previous}",
            current.Encryption.Thumbprint,
            previous.Encryption?.Thumbprint ?? "none");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
