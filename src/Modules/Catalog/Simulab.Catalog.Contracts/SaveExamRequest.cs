namespace Simulab.Catalog.Contracts;

/// <summary>
/// What the exam form sends (F-34, UC2 and UC3). The assessment type, the scope and the content language
/// travel as text, not as enums, so a caller that sends an unknown value gets its own 400 code (BR6, BR7,
/// BR9) instead of an uncoded deserialization failure — the lesson F-33's review left on the organizer.
/// </summary>
/// <param name="IssuingAuthorityId">The organizer that publishes the notice and sets the rules (BR2). Required.</param>
/// <param name="Name">The exam's name, 2 to 200 characters, unique inside its issuing authority (BR10).</param>
/// <param name="AssessmentType">One of PublicServiceExam, Certification, UniversityEntranceExam or Enem, by name, ignoring case (BR6).</param>
/// <param name="Scope">One of National, State or Municipal, by name, ignoring case (BR7).</param>
/// <param name="ScopeDetail">Which state or which municipality; required for State and Municipal, dropped for National (BR8).</param>
/// <param name="ContentLanguage">One of the app's languages, canonicalized (BR9).</param>
public sealed record SaveExamRequest(
    Guid? IssuingAuthorityId,
    string? Name,
    string? AssessmentType,
    string? Scope,
    string? ScopeDetail = null,
    string? ContentLanguage = null)
{
    /// <summary>
    /// The assessment type as the enum, or null when it is blank or not one of the names (BR6). A method and
    /// not a property, so it stays out of the JSON schema of the request.
    /// </summary>
    public AssessmentType? ParseAssessmentType() =>
        Enum.TryParse<AssessmentType>(AssessmentType, ignoreCase: true, out var value) && Enum.IsDefined(value) ? value : null;

    /// <summary>The scope as the enum, or null when it is blank or not one of the names (BR7).</summary>
    public ExamScope? ParseScope() =>
        Enum.TryParse<ExamScope>(Scope, ignoreCase: true, out var value) && Enum.IsDefined(value) ? value : null;
}
