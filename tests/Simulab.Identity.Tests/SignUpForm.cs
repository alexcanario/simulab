using Simulab.Identity.Contracts;

namespace Simulab.Identity.Tests;

/// <summary>A valid sign-up request, so each test changes only the one thing it is about.</summary>
public static class SignUpForm
{
    public const string ValidPassword = "Estudar#2026!";

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
