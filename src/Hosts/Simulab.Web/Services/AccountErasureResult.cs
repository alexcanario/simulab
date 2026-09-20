namespace Simulab.Web.Services;

/// <summary>
/// What an account erasure gave back (F-10 BR2): nothing on success, otherwise the stable code, plus the
/// seconds left when the account is locked. Same two shapes as <see cref="PasswordChangeResult"/>,
/// because it is the same password check — one record each, so neither name lies about its call.
/// </summary>
public sealed record AccountErasureResult(string? ErrorCode, int? RetryAfterSeconds)
{
    public bool IsSuccess => ErrorCode is null;
}
