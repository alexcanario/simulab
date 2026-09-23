using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Simulab.Identity.Contracts;

namespace Simulab.Identity.Tests;

/// <summary>The <c>google</c> grant's answer: tokens, or an OAuth error with the challenge or the Google identity (F-20).</summary>
public sealed record GoogleGrantResponse(
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("challenge")] string? Challenge,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("name")] string? Name);

/// <summary>
/// Stands in for Google (F-20): the Api reads Google's keys through <see cref="IConfigurationManager{T}"/>, and the
/// test host gets keys generated here instead, so every token below is signed the way Google signs, and no test calls
/// Google. The Api's own validator runs unchanged.
/// </summary>
public static class GoogleTokens
{
    public const string ClientId = "simulab-test.apps.googleusercontent.com";

    public const string Issuer = "https://accounts.google.com";

    private static readonly RsaSecurityKey GoogleKey = NewKey("google-test-key");

    /// <summary>A key Google never published: a token signed with it must be refused.</summary>
    private static readonly RsaSecurityKey ForeignKey = NewKey("foreign-key");

    /// <summary>Turns Google sign-in on and points the Api at the test keys.</summary>
    public static void Configure(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSetting("Identity:GoogleSignInEnabled", "true");
        builder.UseSetting("Authentication:Google:ClientId", ClientId);
        builder.ConfigureTestServices(services =>
        {
            var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
            configuration.SigningKeys.Add(GoogleKey);
            services.RemoveAll<IConfigurationManager<OpenIdConnectConfiguration>>();
            services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration));
        });
    }

    /// <summary>A token as Google issues it; each argument changes the one thing a test is about.</summary>
    public static string Issue(
        string subject,
        string email,
        bool emailVerified = true,
        string? name = "Ana Ribeiro",
        string audience = ClientId,
        string issuer = Issuer,
        DateTimeOffset? expires = null,
        bool foreignKey = false)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new Dictionary<string, object>
        {
            ["sub"] = subject,
            ["email"] = email,
            ["email_verified"] = emailVerified,
        };
        if (name is not null)
        {
            claims["name"] = name;
        }

        var end = expires ?? now.AddMinutes(30);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = claims,
            IssuedAt = end.AddHours(-1).UtcDateTime,
            NotBefore = end.AddHours(-1).UtcDateTime,
            Expires = end.UtcDateTime,
            SigningCredentials = new SigningCredentials(foreignKey ? ForeignKey : GoogleKey, SecurityAlgorithms.RsaSha256),
        });
    }

    /// <summary>A Google subject no test has used: the digits Google's own ids are made of.</summary>
    public static string NewSubject() => RandomNumberGenerator.GetInt32(100_000_000, int.MaxValue).ToString(System.Globalization.CultureInfo.InvariantCulture)
        + RandomNumberGenerator.GetInt32(100_000_000, int.MaxValue).ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The <c>google</c> grant, called the way the Web calls it.</summary>
    public static async Task<GoogleGrantResponse> GrantAsync(HttpClient client, string? idToken)
    {
        ArgumentNullException.ThrowIfNull(client);

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = GoogleSignInProtocol.GrantType,
            ["client_id"] = TestClient.ClientId,
            ["client_secret"] = TestClient.ClientSecret,
        };
        if (idToken is not null)
        {
            form[GoogleSignInProtocol.IdTokenParameter] = idToken;
        }

        using var response = await client.PostAsync("/connect/token", new FormUrlEncodedContent(form));
        return (await response.Content.ReadFromJsonAsync<GoogleGrantResponse>())!;
    }

    /// <summary>A confirmation with everything the page asks for; each test changes the one thing it is about.</summary>
    public static GoogleRegistrationRequest Registration(string idToken, string? fullName = "Ana Ribeiro") => new(
        idToken,
        DeclaresAdult: true,
        AcceptsTerms: true,
        AcceptsPrivacy: true,
        TermsVersion: SignUpForm.CurrentVersion,
        PrivacyVersion: SignUpForm.CurrentVersion,
        FullName: fullName);

    private static RsaSecurityKey NewKey(string keyId) => new(RSA.Create(2048)) { KeyId = keyId };
}
