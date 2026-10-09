using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.Identity.Infrastructure.Persistence;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-30: both sign-ups commit every write in one transaction, or none of it (AC1-AC9). AC3 and AC6, the
/// successful shapes, are already covered by <see cref="RegistrationEndpointTests"/> and
/// <see cref="GoogleSignInTests"/>; AC10 is the architecture test that already checks the Application
/// project references no EF Core type; AC12 is the missing-key test, unaffected since no UI text changed.
/// </summary>
public sealed class SignUpTransactionTests : IdentityApiTests
{
    private const string RegistrationsRoute = "/api/v1/identity/registrations";
    private const string GoogleRegistrationsRoute = "/api/v1/identity/google-registrations";

    private bool _breakTheConsentWrite;
    private bool _breakTheTokenWrite;
    private bool _hideEveryUser;
    private bool _skipEmailUniquenessPreCheck;

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        GoogleTokens.Configure(builder);
        builder.ConfigureServices(services =>
        {
            if (_breakTheConsentWrite)
            {
                services.RemoveAll<IConsentRecordStore>();
                services.AddScoped<IConsentRecordStore>(provider =>
                    new TooLongConsentRecordStore(new ConsentRecordStore(provider.GetRequiredService<IdentityModuleDbContext>())));
            }

            if (_breakTheTokenWrite)
            {
                services.RemoveAll<IEmailVerificationTokenStore>();
                services.AddScoped<IEmailVerificationTokenStore>(provider =>
                    new TooLongTokenStore(new EmailVerificationTokenStore(provider.GetRequiredService<IdentityModuleDbContext>())));
            }

            if (_hideEveryUser)
            {
                services.RemoveAll<IUserDirectory>();
                services.AddScoped<IUserDirectory>(provider => new NeverFindsAUserDirectory(new UserDirectory(
                    provider.GetRequiredService<IdentityModuleDbContext>(),
                    provider.GetRequiredService<ILookupNormalizer>())));
            }

            if (_skipEmailUniquenessPreCheck)
            {
                // AC8: with the handler's own lookup already hidden above, Identity's own pre-insert
                // uniqueness query (RequireUniqueEmail) would otherwise catch the race first, as an
                // IdentityResult, never letting it reach the database index this criterion is about.
                services.Configure<IdentityOptions>(options => options.User.RequireUniqueEmail = false);
            }
        });
    }

    // AC1.
    [Fact]
    public async Task Register_ConsentRecordWriteFails_CreatesNothingForThatAddress()
    {
        _breakTheConsentWrite = true;
        await using var factory = new IdentityApiFactory { ConfigureHost = ConfigureHost };
        await factory.PrepareAsync($"{nameof(SignUpTransactionTests)}_consent");
        var client = factory.CreateClient("en");
        var email = $"consentimento.{Guid.CreateVersion7():N}@exemplo.com";

        using var response = await client.PostAsJsonAsync(RegistrationsRoute, SignUpForm.Valid(email), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        (await context.Users.CountAsync(user => user.Email == email)).Should().Be(0, "AC1: no user either");
        (await context.ConsentRecords.CountAsync()).Should().Be(0);
        (await context.EmailVerificationTokens.CountAsync()).Should().Be(0);
    }

    // AC2.
    [Fact]
    public async Task Register_VerificationTokenWriteFails_CreatesNothingIncludingTheUser()
    {
        _breakTheTokenWrite = true;
        await using var factory = new IdentityApiFactory { ConfigureHost = ConfigureHost };
        await factory.PrepareAsync($"{nameof(SignUpTransactionTests)}_token");
        var client = factory.CreateClient("en");
        var email = $"token.{Guid.CreateVersion7():N}@exemplo.com";

        using var response = await client.PostAsJsonAsync(RegistrationsRoute, SignUpForm.Valid(email), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        (await context.Users.CountAsync(user => user.Email == email)).Should().Be(0, "AC2: not even the user Identity had already created");
        (await context.ConsentRecords.CountAsync()).Should().Be(0);
        (await context.EmailVerificationTokens.CountAsync()).Should().Be(0);
    }

    // AC7.
    [Fact]
    public async Task Register_StudentRoleMissing_ThrowsInsteadOfCommittingAHalfAccount()
    {
        await QueryAsync(async context =>
        {
            var role = await context.Roles.SingleAsync(candidate => candidate.Name == IdentityRoles.Student);
            context.Roles.Remove(role);
            return await context.SaveChangesAsync();
        });

        var client = Client("en");
        var email = $"semfuncao.{Guid.CreateVersion7():N}@exemplo.com";

        using var response = await client.PostAsJsonAsync(RegistrationsRoute, SignUpForm.Valid(email), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        (await QueryAsync(context => context.Users.CountAsync(user => user.Email == email))).Should().Be(0);
        (await QueryAsync(context => context.ConsentRecords.CountAsync())).Should().Be(0);
        (await QueryAsync(context => context.EmailVerificationTokens.CountAsync())).Should().Be(0);
    }

    // AC8.
    [Fact]
    public async Task Register_EmailInsertedByAnotherWriterAfterTheLookup_AnswersTheSameSuccessAndKeepsOne()
    {
        _hideEveryUser = true;
        _skipEmailUniquenessPreCheck = true;
        await using var factory = new IdentityApiFactory { ConfigureHost = ConfigureHost };
        await factory.PrepareAsync($"{nameof(SignUpTransactionTests)}_userrace");
        var client = factory.CreateClient("en");
        var email = $"corrida.{Guid.CreateVersion7():N}@exemplo.com";

        await PostAsync(client, RegistrationsRoute, SignUpForm.Valid(email), HttpStatusCode.Accepted);
        // The directory always answers "no one here", the way the loser of a real race would see it, and
        // with Identity's own pre-check off, only the database index is left to refuse the second insert (BR6).
        await PostAsync(client, RegistrationsRoute, SignUpForm.Valid(email) with { FullName = "Outra Pessoa" }, HttpStatusCode.Accepted);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        (await context.Users.CountAsync(user => user.Email == email)).Should().Be(1, "AC8: the second write rolled back");
        (await context.ConsentRecords.CountAsync()).Should().Be(1);
        (await context.EmailVerificationTokens.CountAsync()).Should().Be(1);
    }

    // AC8b: unlike AC8's RequireUniqueEmail, Identity's AddLoginAsync has no switch to turn off its own
    // pre-check, so the exact race cannot be forced through the HTTP pipeline without a second, genuinely
    // concurrent connection. This proves the same translation BR6 relies on directly against the real index,
    // and RegisterGoogleUserHandler's catch (verified above for AC4, AC5) applies it exactly as AC8 does.
    [Fact]
    public async Task TranslateWriteFailure_GoogleLoginIndex_MapsToGoogleLogin()
    {
        var subject = GoogleTokens.NewSubject();

        // A separate scope, and so a separate change tracker, commits the first link: two tracked
        // instances with the same (LoginProvider, ProviderKey) key in the same context would conflict in
        // memory before ever reaching the database, which is not what this test is about.
        await using (var seedScope = Factory.Services.CreateAsyncScope())
        {
            var seedContext = seedScope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
            var first = NewUser($"primeira.{Guid.CreateVersion7():N}@exemplo.com");
            seedContext.Users.Add(first);
            seedContext.UserLogins.Add(new IdentityUserLogin<Guid> { UserId = first.Id, LoginProvider = GoogleSignInProtocol.LoginProvider, ProviderKey = subject });
            await seedContext.SaveChangesAsync();
        }

        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IIdentityUnitOfWork>();
        var second = NewUser($"segunda.{Guid.CreateVersion7():N}@exemplo.com");
        context.Users.Add(second);
        await context.SaveChangesAsync();
        context.UserLogins.Add(new IdentityUserLogin<Guid> { UserId = second.Id, LoginProvider = GoogleSignInProtocol.LoginProvider, ProviderKey = subject });

        var save = async () => await context.SaveChangesAsync();
        var failure = await save.Should().ThrowAsync<DbUpdateException>();

        unitOfWork.TranslateWriteFailure(failure.Which).Should().Be(IdentityUniqueViolation.GoogleLogin);
    }

    private static User NewUser(string email) => new()
    {
        Id = Guid.CreateVersion7(),
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        PreferredLanguage = "en",
    };

    // AC4.
    [Fact]
    public async Task RegisterWithGoogle_NewAccountWhoseConsentWriteFails_CreatesNothing()
    {
        _breakTheConsentWrite = true;
        await using var factory = new IdentityApiFactory { ConfigureHost = ConfigureHost };
        await factory.PrepareAsync($"{nameof(SignUpTransactionTests)}_googleconsent");
        var client = factory.CreateClient("en");
        var subject = GoogleTokens.NewSubject();
        var email = $"semconsentimento.{Guid.CreateVersion7():N}@gmail.com";

        using var response = await client.PostAsJsonAsync(
            GoogleRegistrationsRoute, GoogleTokens.Registration(GoogleTokens.Issue(subject, email)), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        (await context.Users.CountAsync(user => user.Email == email)).Should().Be(0);
        (await context.UserRoles.CountAsync()).Should().Be(0);
        (await context.UserLogins.CountAsync()).Should().Be(0);
    }

    // AC5.
    [Fact]
    public async Task RegisterWithGoogle_TakeOverWhoseConsentWriteFailsAfterTheLink_LeavesThePendingAccountUntouched()
    {
        var client = Client("en");
        // Only a Gmail or Workspace address is authoritative enough for a Google take-over (BR9); anything
        // else always answers AccountExists() before any write, which is a different criterion (AC19).
        var email = $"pendente.{Guid.CreateVersion7():N}@gmail.com";
        await PostAsync(client, RegistrationsRoute, SignUpForm.Valid(email) with { FullName = "Nome Original" }, HttpStatusCode.Accepted);
        var before = await QueryAsync(context => context.Users.AsNoTracking().SingleAsync(user => user.Email == email));

        _breakTheConsentWrite = true;
        await using var factory = new IdentityApiFactory { ConfigureHost = ConfigureHost };
        await factory.PrepareExistingAsync(Factory.ConnectionString);
        var subject = GoogleTokens.NewSubject();

        using var response = await factory.CreateClient().PostAsJsonAsync(
            GoogleRegistrationsRoute,
            GoogleTokens.Registration(GoogleTokens.Issue(subject, email), fullName: "Nome do Google"),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var after = await QueryAsync(context => context.Users.AsNoTracking().SingleAsync(user => user.Id == before.Id));
        after.Status.Should().Be(AccountStatus.Pending, "AC5: still pending");
        after.PasswordHash.Should().Be(before.PasswordHash, "still has its password");
        after.FullName.Should().Be(before.FullName, "still has its old name");
        after.IsAdultDeclared.Should().Be(before.IsAdultDeclared, "still has its old 18+ declaration");
        (await QueryAsync(context => context.UserLogins.CountAsync(login => login.UserId == before.Id))).Should().Be(0, "and no link");
    }

    // AC9, BR3: what an aborted write staged never resurfaces in a later write of the same request.
    [Fact]
    public async Task Transaction_DisposedWithoutCommit_ClearsTheTrackerForALaterWriteInTheSameScope()
    {
        var email = $"limpeza.{Guid.CreateVersion7():N}@exemplo.com";
        await PostAsync(Client("en"), RegistrationsRoute, SignUpForm.Valid(email), HttpStatusCode.Accepted);
        var user = await QueryAsync(context => context.Users.AsNoTracking().SingleAsync(candidate => candidate.Email == email));

        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IIdentityUnitOfWork>();

        await using (await unitOfWork.BeginAsync())
        {
            context.ConsentRecords.Add(new ConsentRecord
            {
                UserId = user.Id,
                TermsVersion = "aborted",
                PrivacyVersion = "aborted",
                DeclaresAdult = true,
                Locale = "en",
                AcceptedAt = Factory.Clock.GetUtcNow(),
            });
            // Never committed.
        }

        context.ChangeTracker.Entries().Should().BeEmpty("BR3, AC9: the rollback cleared what the aborted write staged");

        context.ConsentRecords.Add(new ConsentRecord
        {
            UserId = user.Id,
            TermsVersion = SignUpForm.TermsVersion,
            PrivacyVersion = SignUpForm.PrivacyVersion,
            DeclaresAdult = true,
            Locale = "en",
            AcceptedAt = Factory.Clock.GetUtcNow(),
        });
        await context.SaveChangesAsync();

        (await context.ConsentRecords.CountAsync(record => record.UserId == user.Id))
            .Should().Be(2, "one from the sign-up, one written after the rollback in the same scope");
    }

    /// <summary>Writes a terms version longer than its column (F-30 AC1): a real database error, not the two BR6 translates.</summary>
    private sealed class TooLongConsentRecordStore(IConsentRecordStore inner) : IConsentRecordStore
    {
        public Task AddAsync(ConsentRecord record, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(record);
            return inner.AddAsync(
                new ConsentRecord
                {
                    UserId = record.UserId,
                    TermsVersion = new string('x', 100),
                    PrivacyVersion = record.PrivacyVersion,
                    DeclaresAdult = record.DeclaresAdult,
                    Locale = record.Locale,
                    AcceptedAt = record.AcceptedAt,
                    IpAddress = record.IpAddress,
                },
                cancellationToken);
        }
    }

    /// <summary>Writes a hash longer than its column (F-30 AC2): the same shape F-13's own break-the-write test uses.</summary>
    private sealed class TooLongTokenStore(IEmailVerificationTokenStore inner) : IEmailVerificationTokenStore
    {
        public Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(token);
            return inner.AddAsync(
                new EmailVerificationToken
                {
                    UserId = token.UserId,
                    TokenHash = new string('x', 100),
                    ExpiresAt = token.ExpiresAt
                },
                cancellationToken);
        }

        public Task<EmailVerificationToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            inner.FindByHashAsync(tokenHash, cancellationToken);

        public Task ConsumePendingForUserAsync(Guid userId, DateTimeOffset consumedAt, CancellationToken cancellationToken = default) =>
            inner.ConsumePendingForUserAsync(userId, consumedAt, cancellationToken);

        public Task<int> CountCreatedSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken = default) =>
            inner.CountCreatedSinceAsync(userId, since, cancellationToken);

        public Task<DateTimeOffset?> LastCreatedAtAsync(Guid userId, CancellationToken cancellationToken = default) =>
            inner.LastCreatedAtAsync(userId, cancellationToken);

        public Task ConsumeAsync(EmailVerificationToken token, DateTimeOffset consumedAt, CancellationToken cancellationToken = default) =>
            inner.ConsumeAsync(token, consumedAt, cancellationToken);
    }

    /// <summary>Always answers "no one here" (F-30 AC8, AC8b): what the loser of a real race would see.</summary>
    private sealed class NeverFindsAUserDirectory(IUserDirectory inner) : IUserDirectory
    {
        public Task<User?> FindByEmailIgnoringTenantAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<User?> FindByLoginIgnoringTenantAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<string?> FindLoginKeyAsync(Guid userId, string loginProvider, CancellationToken cancellationToken = default) =>
            inner.FindLoginKeyAsync(userId, loginProvider, cancellationToken);
    }
}
