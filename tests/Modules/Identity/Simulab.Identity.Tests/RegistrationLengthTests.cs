using System.Net;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;

namespace Simulab.Identity.Tests;

/// <summary>B-7 AC1-AC3: sign-up refuses a name or an email longer than its column, instead of a 500.</summary>
public sealed class RegistrationLengthTests : IdentityApiTests
{
    private const string Route = "/api/v1/identity/registrations";

    [Fact]
    public async Task Register_NameOver120Characters_IsRefusedAndCreatesNothing()
    {
        var request = SignUpForm.Valid("nome.longo@exemplo.com") with { FullName = "  " + new string('a', 121) + "  " };

        var body = await PostAsync(Client(), Route, request, HttpStatusCode.BadRequest);

        CodeOf(body).Should().Be(IdentityErrorCodes.RegistrationFullNameTooLong);
        (await QueryAsync(context => context.Users.AnyAsync(u => u.Email == request.Email))).Should().BeFalse();
        await RunJobsAsync();
        Emails.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Register_NameOf120CharactersAfterTrimming_IsAccepted()
    {
        var request = SignUpForm.Valid("nome.limite@exemplo.com") with { FullName = "  " + new string('a', 120) + "  " };

        await PostAsync(Client(), Route, request, HttpStatusCode.Accepted);

        (await QueryAsync(context => context.Users.SingleAsync(u => u.Email == request.Email))).FullName.Should().Be(new string('a', 120));
    }

    [Fact]
    public async Task Register_EmailOver254Characters_IsRefusedAndCreatesNothing()
    {
        var email = new string('a', 243) + "@exemplo.com";
        email.Length.Should().Be(255);
        var request = SignUpForm.Valid(email);

        var body = await PostAsync(Client(), Route, request, HttpStatusCode.BadRequest);

        CodeOf(body).Should().Be(IdentityErrorCodes.EmailInvalid);
        (await QueryAsync(context => context.Users.CountAsync())).Should().Be(0);
        await RunJobsAsync();
        Emails.Messages.Should().BeEmpty();
    }

    /// <summary>BR3: the length check answers before the account lookup, so it says nothing about the address.</summary>
    [Fact]
    public async Task Register_ExistingAddressWithLongName_GetsTheSameAnswerAsANewOne()
    {
        var client = Client();
        var existing = SignUpForm.Valid("ja.existe@exemplo.com");
        await PostAsync(client, Route, existing, HttpStatusCode.Accepted);
        await RunJobsAsync();
        Emails.Clear();
        var longName = new string('a', 121);

        var forExisting = await PostAsync(client, Route, existing with { FullName = longName }, HttpStatusCode.BadRequest);
        var forNew = await PostAsync(client, Route, SignUpForm.Valid("nao.existe@exemplo.com") with { FullName = longName }, HttpStatusCode.BadRequest);

        CodeOf(forExisting).Should().Be(IdentityErrorCodes.RegistrationFullNameTooLong);
        CodeOf(forNew).Should().Be(CodeOf(forExisting));
        await RunJobsAsync();
        Emails.Messages.Should().BeEmpty();
    }
}
