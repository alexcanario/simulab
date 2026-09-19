namespace Simulab.Identity.Application.Abstractions;

/// <summary>
/// The one email an erased account gets, in its own language (F-10, BR12). It goes to the real address
/// before the tombstone replaces it, so the caller reads the address first.
/// </summary>
public interface IErasureMailer
{
    Task SendAccountErasedAsync(string email, DateTimeOffset erasedAt, string locale, CancellationToken cancellationToken = default);
}
