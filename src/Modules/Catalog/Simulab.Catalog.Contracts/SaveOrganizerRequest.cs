namespace Simulab.Catalog.Contracts;

/// <summary>
/// What the add and the edit dialog send (F-33, UC2 and UC3). The kind travels as text, not as the enum,
/// so a caller that sends an unknown kind gets 400 <c>organizer.kind_invalid</c> (BR7) instead of an
/// uncoded deserialization failure.
/// </summary>
/// <param name="Name">The organizer's full name, 2 to 150 characters (BR8).</param>
/// <param name="Acronym">The short name, 2 to 20 characters, stored uppercase (BR8).</param>
/// <param name="Kind">One of ExamBoard, CertifyingBody or University, by name and ignoring case (BR7).</param>
/// <param name="Description">Free text, optional.</param>
/// <param name="Website">An absolute http or https address, optional (BR10).</param>
public sealed record SaveOrganizerRequest(
    string? Name,
    string? Acronym,
    string? Kind,
    string? Description = null,
    string? Website = null)
{
    /// <summary>
    /// The kind as the enum, or null when it is blank or not one of the three names (BR7). A method and
    /// not a property, so it stays out of the JSON schema of the request.
    /// </summary>
    public OrganizerKind? ParseKind() =>
        Enum.TryParse<OrganizerKind>(Kind, ignoreCase: true, out var kind) && Enum.IsDefined(kind) ? kind : null;
}
