using MudBlazor;

namespace Simulab.Web.Components.Ui;

/// <summary>A date column: short date in the user's culture, unless a format is given.</summary>
public class AppDateColumn<TItem, TProperty> : PropertyColumn<TItem, TProperty>
{
    public const string CssClass = "app-cell-date";

    protected override void OnParametersSet()
    {
#pragma warning disable BL0005 // The kit sets its own inherited column parameters once, before the base reads them.
        Format ??= "d";
#pragma warning restore BL0005
        AppNumberColumn<TItem, TProperty>.ApplyKitDefaults(this, CssClass);
        base.OnParametersSet();
    }
}
