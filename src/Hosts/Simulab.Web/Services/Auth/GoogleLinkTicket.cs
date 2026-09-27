namespace Simulab.Web.Services.Auth;

/// <summary>
/// F-29 BR2: what the Web host remembers between "connect Google" and the callback — the account that asked.
/// Nothing else: the ID token does not exist yet when this is minted, and the address is not known either.
/// <para>
/// It carries no binding hash, unlike <see cref="GoogleSignUpTicket"/>: that one's id travels in the URL of a
/// page, while this id lives in the challenge's protected properties and is consumed server-side inside the
/// same request that authenticated the external cookie.
/// </para>
/// </summary>
/// <param name="UserId">The account that started the link, compared with the session that finishes it.</param>
public sealed record GoogleLinkTicket(Guid UserId);
