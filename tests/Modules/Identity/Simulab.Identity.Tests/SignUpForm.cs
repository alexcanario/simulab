using Simulab.Identity.Contracts;

namespace Simulab.Identity.Tests;

/// <summary>A valid sign-up request, so each test changes only the one thing it is about.</summary>
public static class SignUpForm
{
    /// <summary>The one password every test signs in with; it lives with the shared accounts since F-33.</summary>
    public const string ValidPassword = Simulab.Testing.ApiHost.TestAccounts.ValidPassword;

    public const string CurrentVersion = "2026-v1";

    public static RegisterRequest Valid(string? email = null) => new(
        email ?? $"ana.{Guid.CreateVersion7():N}@exemplo.com",
        ValidPassword,
        DeclaresAdult: true,
        AcceptsTerms: true,
        AcceptsPrivacy: true,
        TermsVersion: CurrentVersion,
        PrivacyVersion: CurrentVersion,
        FullName: "Ana Ribeiro");
}
