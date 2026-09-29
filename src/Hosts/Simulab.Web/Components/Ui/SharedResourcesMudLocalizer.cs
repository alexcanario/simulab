using Microsoft.Extensions.Localization;
using MudBlazor;
using Simulab.Web.Resources;

namespace Simulab.Web.Components.Ui;

/// <summary>
/// F-35: the texts MudBlazor's own controls read (the date picker's calendar button and month arrows), taken from
/// <see cref="SharedResources"/> so they follow the reader's language like every other text of the app. A key
/// this map does not know is answered as not found, and MudBlazor falls back to its built-in English.
/// </summary>
public sealed class SharedResourcesMudLocalizer(IStringLocalizer<SharedResources> localizer) : MudLocalizer
{
    /// <summary>MudBlazor's resource key to the key of <see cref="SharedResources"/> that answers it.</summary>
    public static readonly IReadOnlyDictionary<string, string> Keys = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["MudBaseDatePicker_Open"] = "DateField.OpenCalendar",
        ["MudBaseDatePicker_PrevMonth"] = "DateField.PreviousMonth",
        ["MudBaseDatePicker_NextMonth"] = "DateField.NextMonth",
        ["MudBaseDatePicker_PrevYear"] = "DateField.PreviousYear",
        ["MudBaseDatePicker_NextYear"] = "DateField.NextYear"
    };

    public override LocalizedString this[string key] =>
        Keys.TryGetValue(key, out var own) ? localizer[own] : NotFound(key);

    public override LocalizedString this[string key, params object[] arguments] =>
        Keys.TryGetValue(key, out var own) ? localizer[own, arguments] : NotFound(key);

    private static LocalizedString NotFound(string key) => new(key, key, resourceNotFound: true);
}
