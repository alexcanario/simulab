using Microsoft.Extensions.Options;

namespace Simulab.Email;

/// <summary>
/// F-66 BR4: checks what the data annotations cannot, and names every missing key in one message so a deploy
/// shows the whole problem at once. <paramref name="cloudEnvironment"/> is every environment but Development
/// (Staging and Production today), the same reading <c>AddIdentityModule</c> has of its <c>IsDevelopment()</c> flag.
/// </summary>
public sealed class EmailOptionsValidator(bool cloudEnvironment) : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var problems = new List<string>();
        var azure = options.Provider == EmailProvider.AzureCommunicationServices;

        if (cloudEnvironment && !azure)
        {
            problems.Add("Email:Provider must be AzureCommunicationServices outside development (it is Smtp)");
        }

        if (cloudEnvironment && string.IsNullOrWhiteSpace(options.FromAddress))
        {
            problems.Add("Email:FromAddress is missing");
        }

        if (azure)
        {
            var endpoint = options.AzureCommunicationServices.Endpoint;
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                problems.Add("Email:AzureCommunicationServices:Endpoint is missing or is not an https address");
            }

            if (string.IsNullOrWhiteSpace(options.AzureCommunicationServices.ManagedIdentityClientId))
            {
                problems.Add("AZURE_CLIENT_ID is missing (the Api needs its managed identity attached)");
            }
        }

        return problems.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(string.Join("; ", problems) + ".");
    }
}
