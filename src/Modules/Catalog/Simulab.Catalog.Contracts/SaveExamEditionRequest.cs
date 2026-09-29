namespace Simulab.Catalog.Contracts;

/// <summary>
/// What the edition form sends (F-35, UC2 and UC3). The status travels as text, not as an enum, so a caller
/// that sends an unknown value gets its own 400 code (BR9) instead of an uncoded deserialization failure —
/// the lesson F-33's review left on the organizer. Every struct is nullable with a default, so a missing
/// field is a validation error and never a silent zero.
/// </summary>
/// <param name="OrganizerId">The exam board that applied the paper (BR3). Required.</param>
/// <param name="NoticeYear">The year of the notice, from 1990 to the current year plus one (BR4). Required.</param>
/// <param name="Position">The job the paper selects for; blank is stored as null (BR5).</param>
/// <param name="NoticeReference">How the notice names itself, such as "Edital nº 01/2026" (BR6).</param>
/// <param name="NoticeUrl">The official address of the notice, absolute http or https (BR7).</param>
/// <param name="AppliedOn">The day the paper was applied; not before 1 January of the notice year (BR8).</param>
/// <param name="Status">One of Draft or Published, by name, ignoring case. Blank means Draft only when adding (BR9).</param>
public sealed record SaveExamEditionRequest(
    Guid? OrganizerId = null,
    int? NoticeYear = null,
    string? Position = null,
    string? NoticeReference = null,
    string? NoticeUrl = null,
    DateOnly? AppliedOn = null,
    string? Status = null)
{
    /// <summary>
    /// The status as the enum, or null when it is blank or not one of the names (BR9). A method and not a
    /// property, so it stays out of the JSON schema of the request. A number is not a name: "2" is refused,
    /// though <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/> would read it.
    /// </summary>
    public ExamEditionStatus? ParseStatus() =>
        Enum.TryParse<ExamEditionStatus>(Status, ignoreCase: true, out var value)
        && Enum.IsDefined(value)
        && !int.TryParse(Status, out _)
            ? value
            : null;
}
