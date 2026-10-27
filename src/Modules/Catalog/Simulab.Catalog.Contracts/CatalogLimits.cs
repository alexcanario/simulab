namespace Simulab.Catalog.Contracts;

/// <summary>
/// The catalog's length limits: the width of their columns, read by the Api (the authority), the EF
/// mapping and the Web (comfort checks). One number per limit (F-33, BR6; rule from B-7 BR4).
/// </summary>
public static class CatalogLimits
{
    /// <summary>The organizer's full name. A longer one is refused, not cut.</summary>
    public const int OrganizerNameMaxLength = 150;

    /// <summary>The organizer's short name (CEBRASPE, FGV), stored uppercase.</summary>
    public const int OrganizerAcronymMaxLength = 20;

    /// <summary>Free text about the organizer.</summary>
    public const int OrganizerDescriptionMaxLength = 500;

    /// <summary>The organizer's official site, an absolute http or https address.</summary>
    public const int OrganizerWebsiteMaxLength = 300;

    /// <summary>The issuing authority's full name (F-34 BR18). Same widths as the organizer's: same shape.</summary>
    public const int IssuingAuthorityNameMaxLength = 150;

    /// <summary>Free text about the issuing authority.</summary>
    public const int IssuingAuthorityDescriptionMaxLength = 500;

    /// <summary>The issuing authority's official site, an absolute http or https address.</summary>
    public const int IssuingAuthorityWebsiteMaxLength = 300;

    /// <summary>The exam's name, inside its issuing authority (F-34, BR10). Notice titles are long.</summary>
    public const int ExamNameMaxLength = 200;

    /// <summary>Which state or which municipality an exam applies to (F-34, BR8).</summary>
    public const int ExamScopeDetailMaxLength = 120;

    /// <summary>The job an edition selects for (F-35, BR5).</summary>
    public const int ExamEditionPositionMaxLength = 200;

    /// <summary>How the notice names itself (F-35, BR6).</summary>
    public const int ExamEditionNoticeReferenceMaxLength = 100;

    /// <summary>The notice's official address, an absolute http or https URL (F-35, BR7).</summary>
    public const int ExamEditionNoticeUrlMaxLength = 300;

    /// <summary>The biggest notice PDF an edition keeps: 20 MB, exactly 20 × 1024 × 1024 bytes (F-89, BR4).</summary>
    public const long ExamEditionNoticeDocumentMaxBytes = 20L * 1024 * 1024;

    /// <summary>The notice PDF's file name as the curator gave it, cut of any folder part (F-89).</summary>
    public const int ExamEditionNoticeDocumentFileNameMaxLength = 255;

    /// <summary>The name the PDF is stored under: <c>exam-editions/{guid}/notice/{guid}.pdf</c>, 98 characters with the guids written with hyphens (F-89).</summary>
    public const int ExamEditionNoticeDocumentBlobNameLength = 98;

    /// <summary>The oldest notice year an edition may carry (F-35, BR4).</summary>
    public const int ExamEditionNoticeYearMin = 1990;

    /// <summary>How many digits the notice year input accepts (F-35, BR4).</summary>
    public const int ExamEditionNoticeYearDigits = 4;

    /// <summary>The subject's name (F-79, BR3): one name as typed, not translated.</summary>
    public const int SubjectNameMaxLength = 150;

    /// <summary>The topic's name, inside its subject (F-79, BR6).</summary>
    public const int TopicNameMaxLength = 200;

    /// <summary>The stable code of a seeded area (F-79, BR1).</summary>
    public const int AreaCodeMaxLength = 40;

    /// <summary>A notice subject's label, as the notice names it (F-74, BR2).</summary>
    public const int NoticeSubjectLabelMaxLength = 200;

    /// <summary>A notice subject's group, as the notice names it (F-74, BR3).</summary>
    public const int NoticeSubjectGroupMaxLength = 100;

    /// <summary>The fewest questions a notice subject may state (F-74, BR4).</summary>
    public const int NoticeSubjectQuestionCountMin = 1;

    /// <summary>The most questions a notice subject may state (F-74, BR4).</summary>
    public const int NoticeSubjectQuestionCountMax = 500;

    /// <summary>The most entries a notice subject's mapping may hold (F-75, BR1).</summary>
    public const int NoticeSubjectMappingMax = 50;

    /// <summary>The shortest paper an edition may state, in whole minutes (F-168, BR2).</summary>
    public const int ExamEditionDurationMinutesMin = 15;

    /// <summary>The longest paper an edition may state, in whole minutes: 8 hours (F-168, BR2).</summary>
    public const int ExamEditionDurationMinutesMax = 480;

    /// <summary>The lowest score a cut-off or an essay maximum may state, in points (F-168, BR5 and BR6).</summary>
    public const decimal ExamEditionScoreMin = 0.01m;

    /// <summary>The highest score a cut-off or an essay maximum may state, in points (F-168, BR5 and BR6).</summary>
    public const decimal ExamEditionScoreMax = 1000m;

    /// <summary>The fewest points a question of a notice subject may be worth (F-168, BR3).</summary>
    public const decimal NoticeSubjectPointsPerQuestionMin = 0.01m;

    /// <summary>The most points a question of a notice subject may be worth (F-168, BR3).</summary>
    public const decimal NoticeSubjectPointsPerQuestionMax = 100m;

    /// <summary>How many decimal places a score or the points per question may carry (F-168, BR3, BR5 and BR6).</summary>
    public const int ScoreDecimalPlaces = 2;

    /// <summary>The shortest a name may be (BR8).</summary>
    public const int NameMinLength = 2;
}
