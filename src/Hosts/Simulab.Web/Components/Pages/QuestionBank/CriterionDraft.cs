namespace Simulab.Web.Components.Pages.QuestionBank;

/// <summary>
/// One criterion of the rubric as the form holds it before Save (F-109). The position is the row's index; nothing here is
/// stored until the question is saved.
/// </summary>
public sealed class CriterionDraft
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>The most points the criterion gives; null while the field is empty.</summary>
    public int? Points { get; set; }

    /// <summary>The refusals that belong to this row's fields, already translated.</summary>
    public string? NameError { get; set; }

    public string? DescriptionError { get; set; }

    public string? PointsError { get; set; }

    /// <summary>True when nothing was typed in the row: such a row is not sent.</summary>
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(Description) && Points is null;
}
