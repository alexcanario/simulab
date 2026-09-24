namespace Simulab.Catalog.Contracts;

/// <summary>
/// Every failure code this module can return (rule: api-contracts). A code is stable: it is the
/// resource key the UI translates, so it is never renamed or reused.
/// </summary>
public static class CatalogErrorCodes
{
    public const string OrganizerNotFound = "organizer.not_found";
    public const string OrganizerNameRequired = "organizer.name_required";
    public const string OrganizerNameTooLong = "organizer.name_too_long";
    public const string OrganizerNameTaken = "organizer.name_taken";
    public const string OrganizerAcronymRequired = "organizer.acronym_required";
    public const string OrganizerAcronymTooLong = "organizer.acronym_too_long";
    public const string OrganizerAcronymTaken = "organizer.acronym_taken";
    public const string OrganizerKindInvalid = "organizer.kind_invalid";
    public const string OrganizerDescriptionTooLong = "organizer.description_too_long";
    public const string OrganizerWebsiteInvalid = "organizer.website_invalid";

    /// <summary>
    /// F-34 BR12: the organizer is an exam's issuing authority, so it cannot leave the catalog. The text
    /// carries no count on purpose — <c>ErrorText.For</c> maps a code to a text and takes no argument.
    /// </summary>
    public const string OrganizerHasExams = "organizer.has_exams";

    public const string ExamNotFound = "exam.not_found";
    public const string ExamNameRequired = "exam.name_required";
    public const string ExamNameTooLong = "exam.name_too_long";
    public const string ExamNameTaken = "exam.name_taken";
    public const string ExamIssuingAuthorityRequired = "exam.issuing_authority_required";
    public const string ExamAssessmentTypeInvalid = "exam.assessment_type_invalid";
    public const string ExamScopeInvalid = "exam.scope_invalid";
    public const string ExamScopeDetailRequired = "exam.scope_detail_required";
    public const string ExamScopeDetailTooLong = "exam.scope_detail_too_long";
    public const string ExamContentLanguageInvalid = "exam.content_language_invalid";
}
