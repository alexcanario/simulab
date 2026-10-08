namespace Simulab.Catalog.Contracts;

/// <summary>
/// What a move sends (F-74, UC4). The direction travels as text so an unknown or missing value gets its own
/// 400 code, <c>notice_subject.move_invalid</c>, instead of an uncoded deserialization failure.
/// </summary>
/// <param name="Direction">"up" or "down", ignoring case.</param>
public sealed record MoveNoticeSubjectRequest(string? Direction = null)
{
    /// <summary>
    /// The direction as the enum, or null when it is blank or not one of the names. A method and not a property,
    /// so it stays out of the JSON schema. A number is not a name: "1" is refused.
    /// </summary>
    public NoticeSubjectMoveDirection? ParseDirection() =>
        Enum.TryParse<NoticeSubjectMoveDirection>(Direction, ignoreCase: true, out var value)
        && Enum.IsDefined(value)
        && !int.TryParse(Direction, out _)
            ? value
            : null;
}
