using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Simulab.Web.Services.Auth;

/// <summary>Everything the completion endpoint needs to write the auth cookie, carried by one ticket.</summary>
public sealed record SignInTicket(
    string Subject,
    string Email,
    string? DisplayName,
    string SessionJti,
    string AccessToken,
    string RefreshToken,
    TimeSpan AccessTokenLifetime,
    IReadOnlyList<string> Permissions,
    string? PreferredLanguage = null);

/// <summary>
/// A single-use, short-lived relay between the interactive sign-in page and the plain endpoint that
/// writes the auth cookie (F-5, build decision: a Blazor Interactive Server circuit has no open HTTP
/// response to call <c>HttpContext.SignInAsync</c> on). Tokens never sit in a URL or a log: the ticket
/// id is the only thing that travels there, and it is consumed at most once.
/// </summary>
public sealed class SignInTicketStore(TimeProvider timeProvider)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(30);

    private readonly ConcurrentDictionary<string, (SignInTicket Ticket, DateTimeOffset ExpiresAt)> _tickets = new(StringComparer.Ordinal);

    public string Issue(SignInTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        var id = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        _tickets[id] = (ticket, timeProvider.GetUtcNow().Add(Lifetime));
        Sweep();
        return id;
    }

    public bool TryConsume(string id, out SignInTicket ticket)
    {
        if (_tickets.TryRemove(id, out var entry) && entry.ExpiresAt > timeProvider.GetUtcNow())
        {
            ticket = entry.Ticket;
            return true;
        }

        ticket = null!;
        return false;
    }

    private void Sweep()
    {
        var now = timeProvider.GetUtcNow();
        foreach (var (id, entry) in _tickets)
        {
            if (entry.ExpiresAt <= now)
            {
                _tickets.TryRemove(id, out _);
            }
        }
    }
}
