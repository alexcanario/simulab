namespace Simulab.Identity.Contracts;

/// <summary>The names both hosts use for the <c>google</c> grant of the token endpoint (F-20).</summary>
public static class GoogleSignInProtocol
{
    /// <summary>The custom grant type: <see cref="IdTokenParameter"/> in, tokens out.</summary>
    public const string GrantType = "google";

    /// <summary>The token-request parameter that carries the Google ID token.</summary>
    public const string IdTokenParameter = "id_token";

    /// <summary>With <see cref="IdentityErrorCodes.GoogleSignUpRequired"/>: the Google address, for the confirmation page.</summary>
    public const string EmailParameter = "email";

    /// <summary>With <see cref="IdentityErrorCodes.GoogleSignUpRequired"/>: the Google name, for the confirmation page.</summary>
    public const string NameParameter = "name";

    /// <summary>The login provider name in <c>user_logins</c>.</summary>
    public const string LoginProvider = "Google";
}
