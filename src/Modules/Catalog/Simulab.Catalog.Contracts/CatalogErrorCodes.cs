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

    public const string IssuingAuthorityNotFound = "issuing_authority.not_found";
    public const string IssuingAuthorityNameRequired = "issuing_authority.name_required";
    public const string IssuingAuthorityNameTooLong = "issuing_authority.name_too_long";
    public const string IssuingAuthorityNameTaken = "issuing_authority.name_taken";
    public const string IssuingAuthorityDescriptionTooLong = "issuing_authority.description_too_long";
    public const string IssuingAuthorityWebsiteInvalid = "issuing_authority.website_invalid";

    /// <summary>
    /// F-34 BR12 (v2): the body has exams, so it cannot leave the catalog. The text carries no count on
    /// purpose — <c>ErrorText.For</c> maps a code to a text and takes no argument.
    /// </summary>
    public const string IssuingAuthorityHasExams = "issuing_authority.has_exams";

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

    /// <summary>F-42 BR2: a State exam names something that is not an acronym of the 27 Brazilian states.</summary>
    public const string ExamScopeDetailUnknownState = "exam.scope_detail_unknown_state";

    /// <summary>F-35 BR12: the exam has editions, so it cannot leave the catalog. Carries no count, like its siblings.</summary>
    public const string ExamHasEditions = "exam.has_editions";

    /// <summary>F-35 BR12: some edition names this board, so it cannot leave the catalog.</summary>
    public const string OrganizerHasEditions = "organizer.has_editions";

    public const string ExamEditionNotFound = "exam_edition.not_found";
    public const string ExamEditionOrganizerRequired = "exam_edition.organizer_required";
    public const string ExamEditionNoticeYearInvalid = "exam_edition.notice_year_invalid";
    public const string ExamEditionPositionTooLong = "exam_edition.position_too_long";
    public const string ExamEditionNoticeReferenceTooLong = "exam_edition.notice_reference_too_long";
    public const string ExamEditionNoticeUrlInvalid = "exam_edition.notice_url_invalid";
    public const string ExamEditionAppliedOnBeforeNoticeYear = "exam_edition.applied_on_before_notice_year";
    public const string ExamEditionStatusInvalid = "exam_edition.status_invalid";
    public const string ExamEditionDuplicate = "exam_edition.duplicate";
    public const string ExamEditionPublished = "exam_edition.published";

    public const string SubjectNotFound = "subject.not_found";
    public const string SubjectNameRequired = "subject.name_required";
    public const string SubjectNameTooLong = "subject.name_too_long";
    public const string SubjectNameTaken = "subject.name_taken";
    public const string SubjectAreaInvalid = "subject.area_invalid";

    /// <summary>F-79 BR8: the subject still has topics, so it cannot leave the catalog. Carries no count.</summary>
    public const string SubjectHasTopics = "subject.has_topics";

    public const string TopicNotFound = "topic.not_found";
    public const string TopicNameRequired = "topic.name_required";
    public const string TopicNameTooLong = "topic.name_too_long";
    public const string TopicNameTaken = "topic.name_taken";

    public const string NoticeSubjectNotFound = "notice_subject.not_found";
    public const string NoticeSubjectLabelRequired = "notice_subject.label_required";
    public const string NoticeSubjectLabelTooShort = "notice_subject.label_too_short";
    public const string NoticeSubjectLabelTooLong = "notice_subject.label_too_long";
    public const string NoticeSubjectGroupTooLong = "notice_subject.group_too_long";
    public const string NoticeSubjectQuestionCountInvalid = "notice_subject.question_count_invalid";
    public const string NoticeSubjectDuplicate = "notice_subject.duplicate";

    /// <summary>F-74 BR6: the row is first or last in its group, or the direction is not up or down.</summary>
    public const string NoticeSubjectMoveInvalid = "notice_subject.move_invalid";
}
