namespace Simulab.Identity.Contracts;

/// <summary>Custom claim types carried by the access and refresh tokens (BR4), shared by the Api and the Web.</summary>
public static class SessionClaims
{
    /// <summary>One per signed-in session; the key of its row in <c>IRefreshSessionStore</c> (BR4-BR7).</summary>
    public const string SessionJti = "session_jti";

    /// <summary>Always empty in v1 (ADR-0001 #7): no tenant is assigned yet.</summary>
    public const string TenantId = "tenant_id";
}
