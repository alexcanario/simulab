namespace Simulab.SharedKernel.Results;

public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
    BusinessRule,
    Forbidden
}

/// <summary>
/// A failure with a stable code (<c>entity.reason</c>, snake_case). The code is also the resource key the UI translates.
/// <see cref="Detail"/> is English, for logs only, and is never shown to the user.
/// </summary>
public sealed record Error(string Code, ErrorKind Kind, string? Detail = null);
