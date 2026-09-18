using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Registration;
using Simulab.Identity.Application.Verification;
using Simulab.Identity.Domain.Entities;
using Simulab.Identity.Infrastructure.Content;
using Simulab.Identity.Infrastructure.Email;
using Simulab.Identity.Infrastructure.Persistence;
using Simulab.Persistence;

namespace Simulab.Identity.Infrastructure;

/// <summary>Everything the Identity module needs, registered by the host in one call.</summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<IdentityModuleDbContext>((provider, options) =>
            ((DbContextOptionsBuilder<IdentityModuleDbContext>)options)
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(IdentityModuleDbContext.SchemaName))
                .UseModuleConventions(provider));

        // Identity core only: no cookies and no sign-in manager yet, those arrive with F-5.
        services.AddIdentityCore<User>(identity =>
            {
                // BR2: Simulae's policy, already in use there.
                identity.Password.RequiredLength = 12;
                identity.Password.RequireUppercase = true;
                identity.Password.RequireDigit = true;
                identity.Password.RequireNonAlphanumeric = true;
                identity.User.RequireUniqueEmail = true;
                identity.SignIn.RequireConfirmedEmail = true;
                identity.Lockout.MaxFailedAccessAttempts = 5;
                identity.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddUserStore<UserOnlyStore<User, IdentityModuleDbContext, Guid>>();

        services.AddScoped<IEmailVerificationTokenStore, EmailVerificationTokenStore>();
        services.AddScoped<IConsentRecordStore, ConsentRecordStore>();
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddScoped<IVerificationMailer, VerificationMailer>();
        services.AddSingleton<ILegalDocumentProvider, LegalDocumentProvider>();

        // The verification email is written from this module's own resources.
        services.AddLocalization();

        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<VerifyEmailHandler>();
        services.AddScoped<ResendVerificationHandler>();

        services.AddOptions<LegalContentOptions>().Bind(configuration.GetSection(LegalContentOptions.SectionName));
        services.AddOptions<VerificationEmailOptions>().Bind(configuration.GetSection(VerificationEmailOptions.SectionName));

        return services;
    }

    /// <summary>Applies the module's migrations. Development only; a release applies them from the pipeline.</summary>
    public static async Task MigrateIdentityModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
