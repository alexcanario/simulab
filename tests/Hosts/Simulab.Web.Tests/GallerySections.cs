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
        // F-74: free text with suggestions.
        "gallery-suggest-field",
        // F-75: the read-only chip and the multi-pick field.
        "gallery-chip", "gallery-multi-pick-field",
        // F-77: the chips field (the other names of a subject or a topic).
        "gallery-chips-field",
        // F-114: the three field kinds and their states.
        "gallery-field-kinds",
        // F-89: the file field in every state.
        "gallery-file-field",
        // F-95: the rich text field in every state, and the option row built from it.
        "gallery-rich-text",
        // F-125: the identity — button variants, the type scale and the palette of the theme in use.
        "gallery-buttons", "gallery-type", "gallery-palette",
        // F-169: the exam simulator's answer area, answer sheet and save status.
        "gallery-answer-options", "gallery-answer-sheet", "gallery-save-status",
        // F-185: the date range, the relative time and the date text.
        "gallery-dates",
        // F-184: the choice controls and their texts.
        "gallery-choices",
        // F-181: the number controls and their texts.
        "gallery-numbers",
    ];
}
