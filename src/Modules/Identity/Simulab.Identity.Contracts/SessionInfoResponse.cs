namespace Simulab.Identity.Contracts;

/// <summary>
/// What the Web needs to build its own cookie right after sign-in (F-5): read from the access token's
/// own claims by the Api, which validates the token anyway, instead of the Web trying to decode it
/// (the token may be encrypted, and decoding it is not the Web's job either way).
/// </summary>
public sealed record SessionInfoResponse(string Subject, string Email, string SessionJti);
