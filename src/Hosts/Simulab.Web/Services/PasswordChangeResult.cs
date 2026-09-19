namespace Simulab.Web.Services;

/// <summary>
/// What a password change gave back (F-7 BR8): nothing on success, otherwise the stable code, plus the
/// seconds left when the account is locked.
/// </summary>
public sealed record PasswordChangeResult(string? ErrorCode, int? RetryAfterSeconds)
{
    public bool IsSuccess => ErrorCode is null;
}
