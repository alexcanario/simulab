namespace Simulab.Web.Services.Auth;

/// <summary>Server-side storage for <see cref="WebSession"/>, keyed by the id the auth cookie carries (B-3, BR1).</summary>
public interface IWebSessionStore
{
    Task<WebSession?> GetAsync(string webSessionId, CancellationToken cancellationToken = default);

    /// <summary>Writes the entry; it expires at <see cref="WebSession.ExpiresAt"/>, with the cookie that points at it.</summary>
    Task SaveAsync(string webSessionId, WebSession session, CancellationToken cancellationToken = default);

    Task RemoveAsync(string webSessionId, CancellationToken cancellationToken = default);
}
