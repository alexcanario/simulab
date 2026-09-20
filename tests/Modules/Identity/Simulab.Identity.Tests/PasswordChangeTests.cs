using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.Jobs;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-7 AC8-AC12 for the signed-in change. Ported from Simulae's <c>ChangePasswordCommandHandlerTests</c> and
/// <c>ChangePasswordEndpointTests</c>.
/// </summary>
public sealed class PasswordChangeTests : IdentityApiTests
{
    private const string ChangeRoute = "/api/v1/identity/password-changes";
    private const string NewPassword = "Revisar#2026!x";

    private static async Task<HttpResponseMessage> ChangeAsync(HttpClient client, TokenResponse session, string current, string next)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, ChangeRoute)
        {
            Content = JsonContent.Create(new ChangePasswordRequest(current, next), options: AppJson.Options)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return await client.SendAsync(request);
    }

    // Simulae: Deve_Alterar_Senha_Quando_Atual_Confere_E_Nova_Valida, POST_password_change_retorna_200_e_permite_login_com_nova_senha.
    [Fact]
    public async Task Change_RightCurrentPassword_NewPasswordSignsIn()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];

        using var response = await ChangeAsync(client, session, SignUpForm.ValidPassword, NewPassword);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await TokenClient.SignInAsync(client, email, NewPassword)).AccessToken.Should().NotBeNull();
        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
    }

    // Simulae: Deve_Revogar_Demais_Sessoes_Mantendo_A_Atual.
    [Fact]
    public async Task Change_KeepsTheCallersSessionAndEndsTheOthers()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var sessions = await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 3);

        using var response = await ChangeAsync(client, sessions[0], SignUpForm.ValidPassword, NewPassword);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SignedInSessions.StatusOfAsync(client, sessions[0])).Should().Be(HttpStatusCode.OK);
        (await SignedInSessions.AnswerOfAsync(client, sessions[1].AccessToken!)).Should().Be((HttpStatusCode.Unauthorized, IdentityErrorCodes.TokenRevoked));
        (await SignedInSessions.AnswerOfAsync(client, sessions[2].AccessToken!)).Should().Be((HttpStatusCode.Unauthorized, IdentityErrorCodes.TokenRevoked));
        (await TokenClient.RefreshAsync(client, sessions[0].RefreshToken!)).AccessToken.Should().NotBeNull();
        (await TokenClient.RefreshAsync(client, sessions[1].RefreshToken!)).Error.Should().Be(IdentityErrorCodes.RefreshTokenInvalid);
    }

    // Simulae: Deve_Rejeitar_Quando_Senha_Atual_Incorreta, Deve_Rejeitar_Quando_Conta_Bloqueada_Por_Tentativas.
    [Fact]
    public async Task Change_WrongCurrentPassword_CountsTowardTheSignInLockout()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];

        for (var attempt = 1; attempt < 5; attempt++)
        {
            using var wrong = await ChangeAsync(client, session, "not-the-password", NewPassword);
            wrong.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            CodeOf(await wrong.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.PasswordChangeCurrentInvalid);
        }

        (await QueryAsync(context => context.Users.SingleAsync(u => u.Email == email))).AccessFailedCount.Should().Be(4);

        using var fifth = await ChangeAsync(client, session, "not-the-password", NewPassword);
        using var rightButLocked = await ChangeAsync(client, session, SignUpForm.ValidPassword, NewPassword);

        fifth.StatusCode.Should().Be(HttpStatusCode.Locked);
        rightButLocked.StatusCode.Should().Be(HttpStatusCode.Locked);
        var body = JsonDocument.Parse(await rightButLocked.Content.ReadAsStringAsync()).RootElement;
        body.GetProperty("code").GetString().Should().Be(IdentityErrorCodes.AccountLocked);
        body.GetProperty("retryAfterSeconds").GetInt32().Should().BePositive();
        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).Error.Should().Be(IdentityErrorCodes.AccountLocked);
    }

    // Simulae: Deve_Rejeitar_Nova_Senha_Fora_Da_Politica.
    [Theory]
    [InlineData("curta1!A", IdentityErrorCodes.PasswordChangeTooWeak, HttpStatusCode.BadRequest)]
    [InlineData(SignUpForm.ValidPassword, IdentityErrorCodes.PasswordChangeSameAsCurrent, HttpStatusCode.UnprocessableEntity)]
    public async Task Change_RefusedNewPassword_ChangesNothing(string next, string code, HttpStatusCode status)
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];

        using var response = await ChangeAsync(client, session, SignUpForm.ValidPassword, next);

        response.StatusCode.Should().Be(status);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(code);
        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).AccessToken.Should().NotBeNull();
    }

    [Fact]
    public async Task Change_Success_ClearsTheFailedCountAndSendsThePasswordChangedEmail()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        using (await ChangeAsync(client, session, "not-the-password", NewPassword))
        {
        }

        await RunJobsAsync();
        Emails.Clear();
        using var response = await ChangeAsync(client, session, SignUpForm.ValidPassword, NewPassword);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await QueryAsync(context => context.Users.SingleAsync(u => u.Email == email))).AccessFailedCount.Should().Be(0);
        await RunJobsAsync();
        Emails.Count.Should().Be(1);
        Emails.Last!.Subject.Should().StartWith("Your password was changed");
    }

    /// <summary>
    /// F-7 BR11 through F-13: the change never waits for the mail server, and a send that fails on the
    /// worker leaves the notice in the queue to be tried again instead of losing it in a log line.
    /// </summary>
    [Fact]
    public async Task Change_MailServerFails_TheChangeSucceedsAndTheNoticeIsKeptForARetry()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        await RunJobsAsync();
        Emails.Clear();
        Emails.FailNext = true;

        using var response = await ChangeAsync(client, session, SignUpForm.ValidPassword, NewPassword);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await TokenClient.SignInAsync(client, email, NewPassword)).AccessToken.Should().NotBeNull();

        await RunJobsAsync();
        Emails.Count.Should().Be(0, "the worker's send is the one that failed");
        (await PendingJobCountAsync()).Should().Be(1);

        Factory.Clock.Advance(JobPolicy.BackoffAfter(1));
        await RunJobsAsync();
        Emails.Count.Should().Be(1);
        Emails.Last!.Subject.Should().StartWith("Your password was changed");
    }

    // Simulae: POST_password_change_sem_token_retorna_401.
    [Fact]
    public async Task Change_Anonymous_Returns401()
    {
        var response = await Client().PostAsJsonAsync(ChangeRoute, new ChangePasswordRequest(SignUpForm.ValidPassword, NewPassword), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
