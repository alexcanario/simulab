namespace Simulab.Web.Services;

/// <summary>
/// What a call guarded by the sign-in lockout gave back (F-11, as F-7 BR8): the value, or the stable code, plus
/// the seconds left when the account is locked.
/// </summary>
public sealed record LockableResult<T>(T? Value, string? ErrorCode, int? RetryAfterSeconds)
{
    public bool IsSuccess => ErrorCode is null;
}
