using Simulab.Identity.Contracts;

namespace Simulab.Identity.Tests;

/// <summary>A valid sign-up request, so each test changes only the one thing it is about.</summary>
public static class SignUpForm
{
    /// <summary>The one password every test signs in with; it lives with the shared accounts since F-33.</summary>
    public const string ValidPassword = Simulab.Testing.ApiHost.TestAccounts.ValidPassword;

    /// <summary>The terms version that ships in the content folder (F-71 BR7: unchanged).</summary>
    public const string TermsVersion = "2026-v1";

    /// <summary>The privacy version that ships in the content folder (F-71 BR1).</summary>
    public const string PrivacyVersion = "2026-v2";

    public static RegisterRequest Valid(string? email = null) => new(
        email ?? $"ana.{Guid.CreateVersion7():N}@exemplo.com",
        ValidPassword,
        DeclaresAdult: true,
        AcceptsTerms: true,
        AcceptsPrivacy: true,
        TermsVersion: TermsVersion,
        PrivacyVersion: PrivacyVersion,
        FullName: "Ana Ribeiro");
}
