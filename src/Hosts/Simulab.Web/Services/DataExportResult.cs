namespace Simulab.Web.Services;

/// <summary>
/// What a data export gave back (F-16): the file and its name on success, otherwise the stable code, plus the
/// seconds left when the account is locked (same password check as <see cref="AccountErasureResult"/>).
/// </summary>
public sealed record DataExportResult(string? ErrorCode, int? RetryAfterSeconds, byte[]? Content = null, string? FileName = null)
{
    public bool IsSuccess => ErrorCode is null;
}
