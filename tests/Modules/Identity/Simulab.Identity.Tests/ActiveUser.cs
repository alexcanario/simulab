using System.Net.Http.Json;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>Signs a fresh user up and verifies the email through the real endpoints, the way a real account gets to `Active`.</summary>
public static class ActiveUser
{
    public static async Task<string> CreateAsync(HttpClient client, IdentityApiFactory factory, string? email = null)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var request = SignUpForm.Valid(email);
        await client.PostAsJsonAsync("/api/v1/identity/registrations", request, AppJson.Options);

        // F-13: the verification email is a job; it has to run before its link exists.
        await factory.RunJobsAsync();

        var raw = VerificationLink.TokenOf(factory.Emails.Last!.HtmlBody);
        await client.PostAsJsonAsync("/api/v1/identity/email-verifications", new VerifyEmailRequest(raw), AppJson.Options);

        return request.Email;
    }
}
