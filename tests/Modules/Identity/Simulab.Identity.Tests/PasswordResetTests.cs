using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Api;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-7 AC1-AC8, AC10, AC12 through the real HTTP pipeline. Ported from Simulae's <c>ForgotPasswordHandlerTests</c>,
/// <c>ForgotPasswordEndpointTests</c>, <c>ResetPasswordHandlerTests</c> and <c>ResetPasswordEndpointTests</c>.
/// </summary>
public sealed class PasswordResetTests : IdentityApiTests
{
    private const string RequestRoute = "/api/v1/identity/password-reset-requests";
    private const string CheckRoute = "/api/v1/identity/password-reset-token-checks";
    private const string ResetRoute = "/api/v1/identity/password-resets";
    private const string NewPassword = "Revisar#2026!x";

    private async Task<string> RequestLinkAsync(HttpClient client, string email)
    {
        Emails.Clear();
        await PostAsync(client, RequestRoute, new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);
        return ResetLink.TokenOf(Emails.Last!.HtmlBody);
    }

    // Simulae: Deve_gerar_token_e_enviar_email_para_conta_existente, Deve_invalidar_tokens_anteriores_ao_solicitar_novo.
    [Fact]
    public async Task RequestLink_ActiveAccount_StoresOneHashedTokenForAnHourAndInvalidatesTheOlderOne()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Emails);
        var first = await RequestLinkAsync(client, email);
        Factory.Clock.Advance(PasswordResetPolicy.Cooldown);

        var second = await RequestLinkAsync(client, email);

        Emails.Count.Should().Be(1);
        var tokens = await QueryAsync(context => context.PasswordResetTokens.OrderBy(token => token.CreatedAt).ToListAsync());
        tokens.Should().HaveCount(2);
        tokens.Should().NotContain(token => token.TokenHash == first || token.TokenHash == second, "only the hash is stored");
        tokens[0].IsConsumed.Should().BeTrue();
        tokens[1].IsConsumed.Should().BeFalse();
        tokens[1].ExpiresAt.Should().Be(Factory.Clock.GetUtcNow().Add(PasswordResetPolicy.Lifetime));
        await PostAsync(client, CheckRoute, new PasswordResetTokenCheckRequest(first), HttpStatusCode.BadRequest);
        await PostAsync(client, CheckRoute, new PasswordResetTokenCheckRequest(second), HttpStatusCode.NoContent);
    }

    // Simulae: Deve_retornar_mensagem_generica_para_email_inexistente, Deve_retornar_sucesso_generico_quando_email_vazio.
    [Theory]
    [InlineData("nao.existe@exemplo.com")]
    [InlineData("")]
    public async Task RequestLink_UnknownOrEmptyAddress_SameAnswerAndNothingStoredOrSent(string email)
    {
        var client = Client();

        await PostAsync(client, RequestRoute, new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);

        Emails.Count.Should().Be(0);
        (await QueryAsync(context => context.PasswordResetTokens.CountAsync())).Should().Be(0);
    }

    [Fact]
    public async Task RequestLink_PerAccountLimits_AreSilent()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Emails);
        await RequestLinkAsync(client, email);

        // Inside the cooldown: same 202, nothing sent.
        Emails.Clear();
        await PostAsync(client, RequestRoute, new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);
        Emails.Count.Should().Be(0);

        // Five in the hour: the sixth is refused silently too. The tokens are written directly, one a
        // minute apart: through HTTP the per-client cap (also five) would answer first in a test host,
        // where every request comes from the same client.
        var userId = (await QueryAsync(context => context.Users.SingleAsync(u => u.Email == email))).Id;
        for (var i = 1; i < PasswordResetPolicy.MaxEmailsPerWindow; i++)
        {
            Factory.Clock.Advance(PasswordResetPolicy.Cooldown);
            await QueryAsync(context =>
            {
                context.PasswordResetTokens.Add(new PasswordResetToken
                {
                    UserId = userId,
                    TokenHash = $"seeded-{i}",
                    ExpiresAt = Factory.Clock.GetUtcNow().Add(PasswordResetPolicy.Lifetime)
                });
                return context.SaveChangesAsync();
            });
        }

        Factory.Clock.Advance(PasswordResetPolicy.Cooldown);
        await PostAsync(client, RequestRoute, new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);
        Emails.Count.Should().Be(0);

        // An hour later the account may ask again.
        Factory.Clock.Advance(PasswordResetPolicy.Window);
        await PostAsync(client, RequestRoute, new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);
        Emails.Count.Should().Be(1);
    }

    [Fact]
    public async Task RequestLink_AboveTheHourlyCapForOneClient_IsRefused()
    {
        var client = Client();
        for (var i = 0; i < IdentityRateLimits.PasswordResetRequestsPerHour; i++)
        {
            await PostAsync(client, RequestRoute, new RequestPasswordResetRequest($"qualquer{i}@exemplo.com"), HttpStatusCode.Accepted);
        }

        var body = await PostAsync(client, RequestRoute, new RequestPasswordResetRequest("mais.um@exemplo.com"), HttpStatusCode.TooManyRequests);

        CodeOf(body).Should().Be(IdentityErrorCodes.PasswordResetRateLimited);
    }

    [Fact]
    public async Task Reset_AboveTheHourlyCapForOneClient_IsRefused()
    {
        var client = Client();
        for (var i = 0; i < IdentityRateLimits.PasswordResetsPerHour; i++)
        {
            await PostAsync(client, CheckRoute, new PasswordResetTokenCheckRequest($"token-{i}"), HttpStatusCode.BadRequest);
        }

        var body = await PostAsync(client, ResetRoute, new ResetPasswordRequest("token-x", NewPassword), HttpStatusCode.TooManyRequests);

        CodeOf(body).Should().Be(IdentityErrorCodes.PasswordResetRateLimited);
    }

    // Simulae: Deve_redefinir_senha_com_token_valido_1h, Deve_rejeitar_token_ja_consumido.
    [Fact]
    public async Task Reset_ValidToken_NewPasswordSignsInOldDoesNotAndTheLinkWorksOnce()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Emails);
        var token = await RequestLinkAsync(client, email);

        await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, NewPassword), HttpStatusCode.NoContent);

        (await TokenClient.SignInAsync(client, email, NewPassword)).AccessToken.Should().NotBeNull();
        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
        var again = await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, "Outra#Senha2026"), HttpStatusCode.BadRequest);
        CodeOf(again).Should().Be(IdentityErrorCodes.PasswordResetInvalid);
    }

    // Simulae: Deve_rejeitar_token_expirado_com_mensagem_generica, Deve_rejeitar_token_inexistente_com_mensagem_generica, Deve_rejeitar_quando_token_vazio.
    [Fact]
    public async Task CheckAndReset_UnknownOrExpiredToken_AnswerWithTheirOwnCodes()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Emails);
        var token = await RequestLinkAsync(client, email);

        var unknown = await PostAsync(client, CheckRoute, new PasswordResetTokenCheckRequest("not-a-token"), HttpStatusCode.BadRequest);
        var empty = await PostAsync(client, ResetRoute, new ResetPasswordRequest("", NewPassword), HttpStatusCode.BadRequest);
        await PostAsync(client, CheckRoute, new PasswordResetTokenCheckRequest(token), HttpStatusCode.NoContent);

        Factory.Clock.Advance(PasswordResetPolicy.Lifetime);
        var expiredCheck = await PostAsync(client, CheckRoute, new PasswordResetTokenCheckRequest(token), HttpStatusCode.Gone);
        var expiredReset = await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, NewPassword), HttpStatusCode.Gone);

        CodeOf(unknown).Should().Be(IdentityErrorCodes.PasswordResetInvalid);
        CodeOf(empty).Should().Be(IdentityErrorCodes.PasswordResetInvalid);
        CodeOf(expiredCheck).Should().Be(IdentityErrorCodes.PasswordResetExpired);
        CodeOf(expiredReset).Should().Be(IdentityErrorCodes.PasswordResetExpired);
    }

    // Simulae: Deve_rejeitar_senha_fora_da_politica.
    [Theory]
    [InlineData("curta1!A", IdentityErrorCodes.PasswordResetTooWeak, HttpStatusCode.BadRequest)]
    [InlineData(SignUpForm.ValidPassword, IdentityErrorCodes.PasswordResetSameAsCurrent, HttpStatusCode.UnprocessableEntity)]
    public async Task Reset_RefusedPassword_ChangesNothingAndKeepsTheLink(string password, string code, HttpStatusCode status)
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Emails);
        var token = await RequestLinkAsync(client, email);

        var body = await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, password), status);

        CodeOf(body).Should().Be(code);
        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).AccessToken.Should().NotBeNull();
        await PostAsync(client, CheckRoute, new PasswordResetTokenCheckRequest(token), HttpStatusCode.NoContent);
    }

    // Simulae: Deve_resetar_lockout_apos_redefinicao_bem_sucedida (which cleared the count but not the lockout).
    [Fact]
    public async Task Reset_LockedOutAccount_ClearsTheLockoutAndSignsInAtOnce()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Emails);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await TokenClient.SignInAsync(client, email, "not-the-password");
        }

        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).Error.Should().Be(IdentityErrorCodes.AccountLocked);
        var token = await RequestLinkAsync(client, email);

        await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, NewPassword), HttpStatusCode.NoContent);

        (await TokenClient.SignInAsync(client, email, NewPassword)).AccessToken.Should().NotBeNull();
        var user = await QueryAsync(context => context.Users.SingleAsync(u => u.Email == email));
        user.AccessFailedCount.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
    }

    [Fact]
    public async Task Reset_PendingAccount_ActivatesItAndConsumesItsVerificationLinks()
    {
        var client = Client();
        var request = SignUpForm.Valid();
        await client.PostAsJsonAsync("/api/v1/identity/registrations", request, AppJson.Options);
        var token = await RequestLinkAsync(client, request.Email);

        await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, NewPassword), HttpStatusCode.NoContent);

        var user = await QueryAsync(context => context.Users.SingleAsync(u => u.Email == request.Email));
        user.Status.Should().Be(AccountStatus.Active);
        user.EmailConfirmed.Should().BeTrue();
        user.EmailVerifiedAt.Should().Be(Factory.Clock.GetUtcNow());
        (await QueryAsync(context => context.EmailVerificationTokens.AllAsync(t => t.ConsumedAt != null))).Should().BeTrue();
        (await TokenClient.SignInAsync(client, request.Email, NewPassword)).AccessToken.Should().NotBeNull();
    }

    [Fact]
    public async Task Reset_EndsEverySessionOfTheAccount()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Emails);
        var sessions = await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 3);
        var token = await RequestLinkAsync(client, email);

        await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, NewPassword), HttpStatusCode.NoContent);

        foreach (var session in sessions)
        {
            (await SignedInSessions.StatusOfAsync(client, session)).Should().Be(HttpStatusCode.Unauthorized);
            (await TokenClient.RefreshAsync(client, session.RefreshToken!)).Error.Should().Be(IdentityErrorCodes.RefreshTokenInvalid);
        }
    }

    [Fact]
    public async Task Reset_SendsOnePasswordChangedEmailWithTheInstantAndNoToken()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Emails);
        var token = await RequestLinkAsync(client, email);
        Emails.Clear();

        await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, NewPassword), HttpStatusCode.NoContent);

        Emails.Count.Should().Be(1);
        var message = Emails.Last!;
        message.Subject.Should().StartWith("Your password was changed");
        message.HtmlBody.Should().Contain("https://localhost/forgot-password").And.Contain("UTC").And.NotContain("token=");
    }

    [Fact]
    public async Task Endpoints_ForTheLostPassword_AreAnonymous()
    {
        var client = Client();

        await PostAsync(client, RequestRoute, new RequestPasswordResetRequest("x@exemplo.com"), HttpStatusCode.Accepted);
        await PostAsync(client, CheckRoute, new PasswordResetTokenCheckRequest("x"), HttpStatusCode.BadRequest);
        await PostAsync(client, ResetRoute, new ResetPasswordRequest("x", NewPassword), HttpStatusCode.BadRequest);
    }
}
