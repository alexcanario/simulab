namespace Simulab.Web.Tests;

/// <summary>Section ids the gallery must render.</summary>
internal static class GallerySections
{
    public static readonly string[] All =
    [
        "gallery-table", "gallery-row-actions", "gallery-states", "gallery-confirm",
        "gallery-form", "gallery-fields", "gallery-lookups", "gallery-alerts", "gallery-errors",
        "gallery-feedback", "gallery-icons",
        // F-35 BR16: the kit's only date input.
        "gallery-date-field",
        // F-43 BR1: the nine composition patterns. A screen mockup may use only what is shown here.
        "gallery-section-card", "gallery-form-grid", "gallery-radio-cards", "gallery-conditional-field",
        "gallery-leading-icons", "gallery-status-chips", "gallery-error-summary", "gallery-form-aside",
        "gallery-item-rows",
        // F-36 BR13: the optional parameters of the table, the truncated text and the page header.
        "gallery-kit-parameters",
    ];
}
