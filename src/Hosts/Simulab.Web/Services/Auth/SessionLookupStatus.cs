namespace Simulab.Web.Services.Auth;

/// <summary>What <see cref="AuthClient.LookUpSessionAsync"/> found out (B-3, BR3).</summary>
public enum SessionLookupStatus
{
    /// <summary>The Api knows the session; its claims came back.</summary>
    Alive,

    /// <summary>The Api answered 401: the session was revoked or its token is no longer valid.</summary>
    Ended,

    /// <summary>No usable answer (network, 5xx). Never read as a sign-out.</summary>
    Unavailable
}
