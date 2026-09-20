using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.Identity.Infrastructure.Persistence;
using Simulab.Persistence;

namespace Simulab.Identity.Tests;

/// <summary>Sign-up through the real HTTP pipeline: AC1 to AC5.</summary>
public sealed class RegistrationEndpointTests : IdentityApiTests
{
    private const string Route = "/api/v1/identity/registrations";

    [Fact]
    public async Task Register_ValidForm_CreatesPendingUserConsentAndToken()
    {
        var client = Factory.CreateClient("pt-BR");
        var request = SignUpForm.Valid("ana.pendente@exemplo.com");

        await PostAsync(client, Route, request, HttpStatusCode.Accepted);

        var user = await QueryAsync(context => context.Users.SingleAsync(u => u.Email == request.Email));
        user.Status.Should().Be(AccountStatus.Pending);
        user.EmailConfirmed.Should().BeFalse();
        user.EmailVerifiedAt.Should().BeNull();
        user.IsAdultDeclared.Should().BeTrue();
        user.PreferredLanguage.Should().Be("pt-BR");
        user.FullName.Should().Be("Ana Ribeiro");

        var consent = await QueryAsync(context => context.ConsentRecords.SingleAsync(c => c.UserId == user.Id));
        consent.TermsVersion.Should().Be(SignUpForm.CurrentVersion);
        consent.PrivacyVersion.Should().Be(SignUpForm.CurrentVersion);
        consent.DeclaresAdult.Should().BeTrue();
        consent.Locale.Should().Be("pt-BR");
        consent.AcceptedAt.Should().Be(Factory.Clock.GetUtcNow());

        var tokens = await QueryAsync(context => context.EmailVerificationTokens.Where(t => t.UserId == user.Id).ToListAsync());
        tokens.Should().ContainSingle();
        tokens[0].TokenHash.Should().NotBeNullOrWhiteSpace();
        tokens[0].ExpiresAt.Should().Be(Factory.Clock.GetUtcNow().AddHours(24));

        await RunJobsAsync();
        Emails.Messages.Should().ContainSingle();
        Emails.Last!.To.Should().Be(request.Email);

        // The raw token exists only in the email; the database keeps its hash.
        var raw = VerificationLink.TokenOf(Emails.Last.HtmlBody);
        tokens[0].TokenHash.Should().NotBe(raw);
    }

    [Fact]
    public async Task Register_WithoutAgeDeclaration_IsRefusedAndCreatesNothing()
    {
        var client = Factory.CreateClient("en");
        var request = SignUpForm.Valid("sem.maioridade@exemplo.com") with { DeclaresAdult = false };

        var body = await PostAsync(client, Route, request, HttpStatusCode.BadRequest);

        CodeOf(body).Should().Be(IdentityErrorCodes.AgeDeclarationRequired);
        (await QueryAsync(context => context.Users.CountAsync(u => u.Email == request.Email))).Should().Be(0);
        await RunJobsAsync();
        Emails.Count.Should().Be(0);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Register_WithoutAnAcceptance_IsRefusedAndCreatesNothing(bool terms, bool privacy)
    {
        var client = Factory.CreateClient("en");
        var request = SignUpForm.Valid() with { AcceptsTerms = terms, AcceptsPrivacy = privacy };

        var body = await PostAsync(client, Route, request, HttpStatusCode.BadRequest);

        CodeOf(body).Should().Be(IdentityErrorCodes.ConsentRequired);
        (await QueryAsync(context => context.Users.CountAsync(u => u.Email == request.Email))).Should().Be(0);
    }

    [Theory]
    [InlineData("short#1A")]           // fewer than 12 characters
    [InlineData("estudarsemmaius#1")]  // no uppercase
    [InlineData("EstudarSemNumero#")]  // no digit
    [InlineData("EstudarSemSimbolo1")] // no symbol
    public async Task Register_WeakPassword_IsRefusedAndCreatesNothing(string password)
    {
        var client = Factory.CreateClient("en");
        var request = SignUpForm.Valid() with { Password = password };

        var body = await PostAsync(client, Route, request, HttpStatusCode.BadRequest);

        CodeOf(body).Should().Be(IdentityErrorCodes.PasswordTooWeak);
        (await QueryAsync(context => context.Users.CountAsync(u => u.Email == request.Email))).Should().Be(0);
    }

    [Fact]
    public async Task Register_CompliantPassword_IsAccepted()
    {
        var client = Factory.CreateClient("en");

        await PostAsync(client, Route, SignUpForm.Valid("senha.valida@exemplo.com"), HttpStatusCode.Accepted);

        (await QueryAsync(context => context.Users.CountAsync(u => u.Email == "senha.valida@exemplo.com"))).Should().Be(1);
    }

    [Fact]
    public async Task Register_EmailAlreadyRegistered_AnswersTheSameAndCreatesNothing()
    {
        var client = Factory.CreateClient("en");
        var request = SignUpForm.Valid("repetida@exemplo.com");
        await PostAsync(client, Route, request, HttpStatusCode.Accepted);
        await RunJobsAsync();
        Emails.Clear();

        // Same answer as a new account: the caller cannot tell the two apart (BR4).
        await PostAsync(client, Route, request with { FullName = "Outra Pessoa" }, HttpStatusCode.Accepted);

        (await QueryAsync(context => context.Users.CountAsync(u => u.Email == request.Email))).Should().Be(1);
        (await QueryAsync(context => context.EmailVerificationTokens.CountAsync())).Should().Be(1);
        await RunJobsAsync();
        Emails.Count.Should().Be(0);
    }

    [Fact]
    public async Task Register_EmailOfAnotherTenant_IsStillSeenAsTaken()
    {
        var client = Factory.CreateClient("en");
        var request = SignUpForm.Valid("b2b@exemplo.com");
        await PostAsync(client, Route, request, HttpStatusCode.Accepted);

        // The row is moved to a tenant, which the tenant filter hides from the current (null) tenant.
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
            var user = await context.Users.SingleAsync(u => u.Email == request.Email);
            user.TenantId = Guid.CreateVersion7();
            await context.SaveChangesAsync();

            (await context.Users.CountAsync(u => u.Email == request.Email)).Should().Be(0, "the filter hides it");
            (await context.Users.IgnoreQueryFilters([ModuleDbContext.TenantFilter])
                .CountAsync(u => u.Email == request.Email)).Should().Be(1);
        }

        await RunJobsAsync();
        Emails.Clear();
        await PostAsync(client, Route, request, HttpStatusCode.Accepted);

        var all = await QueryAsync(context => context.Users.IgnoreQueryFilters([ModuleDbContext.TenantFilter])
            .CountAsync(u => u.Email == request.Email));
        all.Should().Be(1, "no second account was created for an address that is already taken");
        await RunJobsAsync();
        Emails.Count.Should().Be(0);
    }

    [Fact]
    public async Task Register_OutdatedDocumentVersion_IsRefused()
    {
        var client = Factory.CreateClient("en");

        var body = await PostAsync(client, Route, SignUpForm.Valid() with { TermsVersion = "2025-v9" }, HttpStatusCode.Conflict);

        CodeOf(body).Should().Be(IdentityErrorCodes.TermsVersionOutdated);

        var privacyBody = await PostAsync(client, Route, SignUpForm.Valid() with { PrivacyVersion = "2025-v9" }, HttpStatusCode.Conflict);

        CodeOf(privacyBody).Should().Be(IdentityErrorCodes.TermsVersionOutdated);
    }

    [Fact]
    public async Task Register_InvalidEmail_IsRefused()
    {
        var client = Factory.CreateClient("en");

        var body = await PostAsync(client, Route, SignUpForm.Valid() with { Email = "nao-e-um-email" }, HttpStatusCode.BadRequest);

        CodeOf(body).Should().Be(IdentityErrorCodes.EmailInvalid);
    }
}
