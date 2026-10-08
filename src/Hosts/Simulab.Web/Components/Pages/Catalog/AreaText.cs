using Microsoft.Extensions.Localization;

namespace Simulab.Web.Components.Pages.Catalog;

/// <summary>
/// The reader-facing name of an area (F-79, BR1). The area is a seeded row with a stable code and no name:
/// the name is the resource <c>Area.&lt;Code&gt;</c> in the reader's language, so a new area is a migration
/// plus three resource entries and never a column.
/// </summary>
public static class AreaText
{
    /// <summary>The area's name, or an empty text when the subject has no area.</summary>
    public static string Name(IStringLocalizer localizer, string? code)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        return code is null ? string.Empty : localizer[$"Area.{code}"].Value;
    }
}
