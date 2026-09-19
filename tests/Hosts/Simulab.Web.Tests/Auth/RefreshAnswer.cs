namespace Simulab.Web.Tests.Auth;

/// <summary>What <see cref="FakeAuthApi"/> answers to a refresh.</summary>
public enum RefreshAnswer
{
    /// <summary>A new access and refresh token pair.</summary>
    NewPair,

    /// <summary>The OAuth refusal of a used or unknown refresh token (F-5 BR5).</summary>
    Rejected,

    /// <summary>An OAuth error that is not about the token (a wrong client secret after a config change).</summary>
    InvalidClient,

    /// <summary>A 500 as the Api's exception handler writes it: problem details, no OAuth <c>error</c>.</summary>
    ServerError,

    /// <summary>No answer at all.</summary>
    Unreachable,

    /// <summary>The HTTP client gave up waiting (not the caller's cancellation).</summary>
    TimedOut
}
