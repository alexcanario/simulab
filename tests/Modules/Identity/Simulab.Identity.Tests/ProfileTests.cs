using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>F-8 AC1-AC6 and AC10: the signed-in user's own profile, through the real HTTP pipeline.</summary>
public sealed class ProfileTests : IdentityApiTests
{
    private const string ProfileRoute = "/api/v1/identity/profile";
    private const string LanguageRoute = "/api/v1/identity/profile/preferred-language";

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string route, string? accessToken, object? body = null)
    {
        using var request = new HttpRequestMessage(method, route);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: AppJson.Options);
        }

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request);
    }

    private async Task<(string Email, string AccessToken)> SignedInAsync(HttpClient client)
    {
        var email = await ActiveUser.CreateAsync(client, Emails);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        return (email, session.AccessToken!);
    }

    [Fact]
    public async Task Get_SignedIn_ReturnsTheCallersOwnProfile()
    {
        var client = Client("pt-BR");
        var (email, token) = await SignedInAsync(client);

        using var response = await SendAsync(client, HttpMethod.Get, ProfileRoute, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>(AppJson.Options);
        profile.Should().Be(new ProfileResponse(email, SignUpForm.Valid(email).FullName, "pt-BR"));
    }

    [Fact]
    public async Task Get_WithoutToken_IsUnauthorized()
    {
        using var get = await SendAsync(Client(), HttpMethod.Get, ProfileRoute, accessToken: null);
        using var put = await SendAsync(Client(), HttpMethod.Put, ProfileRoute, accessToken: null, new UpdateProfileRequest("Ana", "en"));
        using var language = await SendAsync(Client(), HttpMethod.Put, LanguageRoute, accessToken: null, new UpdatePreferredLanguageRequest("en"));

        get.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        put.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        language.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_SavesTheTrimmedNameAndLanguage_OfTheCallerOnly()
    {
        var client = Client();
        var (email, token) = await SignedInAsync(client);
        var (otherEmail, _) = await SignedInAsync(client);

        using var response = await SendAsync(client, HttpMethod.Put, ProfileRoute, token, new UpdateProfileRequest("  Ana Souza  ", "pt-PT"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var user = await QueryAsync(context => context.Users.SingleAsync(u => u.Email == email));
        user.FullName.Should().Be("Ana Souza");
        user.PreferredLanguage.Should().Be("pt-PT");
        var other = await QueryAsync(context => context.Users.SingleAsync(u => u.Email == otherEmail));
        other.FullName.Should().Be(SignUpForm.Valid(otherEmail).FullName);
        other.PreferredLanguage.Should().Be("en");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Update_BlankName_IsStoredAsNull(string? fullName)
    {
        var client = Client();
        var (email, token) = await SignedInAsync(client);

        using var response = await SendAsync(client, HttpMethod.Put, ProfileRoute, token, new UpdateProfileRequest(fullName, "en"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await QueryAsync(context => context.Users.SingleAsync(u => u.Email == email))).FullName.Should().BeNull();
    }

    [Fact]
    public async Task Update_NameOver120Characters_IsRefusedAndChangesNothing()
    {
        var client = Client();
        var (email, token) = await SignedInAsync(client);

        using var longest = await SendAsync(client, HttpMethod.Put, ProfileRoute, token, new UpdateProfileRequest(new string('a', 120), "en"));
        using var tooLong = await SendAsync(client, HttpMethod.Put, ProfileRoute, token, new UpdateProfileRequest(new string('b', 121), "pt-PT"));

        longest.StatusCode.Should().Be(HttpStatusCode.NoContent);
        tooLong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await tooLong.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.ProfileFullNameTooLong);
        var user = await QueryAsync(context => context.Users.SingleAsync(u => u.Email == email));
        user.FullName.Should().Be(new string('a', 120));
        user.PreferredLanguage.Should().Be("en");
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("pt")]
    public async Task Update_UnsupportedLanguage_IsRefusedAndChangesNothing(string? language)
    {
        var client = Client();
        var (email, token) = await SignedInAsync(client);

        using var profile = await SendAsync(client, HttpMethod.Put, ProfileRoute, token, new UpdateProfileRequest("Ana", language));
        using var alone = await SendAsync(client, HttpMethod.Put, LanguageRoute, token, new UpdatePreferredLanguageRequest(language));

        profile.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await profile.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.ProfileLanguageNotSupported);
        alone.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await alone.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.ProfileLanguageNotSupported);
        var user = await QueryAsync(context => context.Users.SingleAsync(u => u.Email == email));
        user.FullName.Should().Be(SignUpForm.Valid(email).FullName);
        user.PreferredLanguage.Should().Be("en");
    }

    [Fact]
    public async Task Update_LanguageInAnyCase_IsStoredInItsCanonicalForm()
    {
        var client = Client();
        var (email, token) = await SignedInAsync(client);

        using var response = await SendAsync(client, HttpMethod.Put, ProfileRoute, token, new UpdateProfileRequest(null, "PT-br"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await QueryAsync(context => context.Users.SingleAsync(u => u.Email == email))).PreferredLanguage.Should().Be("pt-BR");
    }

    [Fact]
    public async Task UpdatePreferredLanguage_ChangesTheLanguageAndKeepsTheName()
    {
        var client = Client("pt-BR");
        var (email, token) = await SignedInAsync(client);
        using (await SendAsync(client, HttpMethod.Put, ProfileRoute, token, new UpdateProfileRequest("Ana", "pt-BR")))
        {
        }

        using var response = await SendAsync(client, HttpMethod.Put, LanguageRoute, token, new UpdatePreferredLanguageRequest("en"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var user = await QueryAsync(context => context.Users.SingleAsync(u => u.Email == email));
        user.PreferredLanguage.Should().Be("en");
        user.FullName.Should().Be("Ana");
    }

    /// <summary>F-8 BR5, BR8: what the Web writes into the cookies at sign-in comes from the account as it is now.</summary>
    [Fact]
    public async Task Session_CarriesTheCurrentNameAndLanguage()
    {
        var client = Client();
        var (_, token) = await SignedInAsync(client);
        using (await SendAsync(client, HttpMethod.Put, ProfileRoute, token, new UpdateProfileRequest("Ana", "pt-PT")))
        {
        }

        using var response = await SendAsync(client, HttpMethod.Get, "/api/v1/identity/session", token);

        var session = await response.Content.ReadFromJsonAsync<SessionInfoResponse>(AppJson.Options);
        session!.DisplayName.Should().Be("Ana");
        session.PreferredLanguage.Should().Be("pt-PT");
    }

    /// <summary>F-8 AC10 (BR7): a language changed on the profile is the language of the next email.</summary>
    [Fact]
    public async Task ChangedLanguage_WritesTheNextEmailInIt()
    {
        var client = Client("en");
        var (email, token) = await SignedInAsync(client);
        using (await SendAsync(client, HttpMethod.Put, LanguageRoute, token, new UpdatePreferredLanguageRequest("pt-PT")))
        {
        }

        Emails.Clear();
        await PostAsync(Client("en"), "/api/v1/identity/password-reset-requests", new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);

        Emails.Last!.Subject.Should().StartWith("Redefina a sua palavra-passe");
    }
}
