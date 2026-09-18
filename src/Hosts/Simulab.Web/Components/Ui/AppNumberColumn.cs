using MudBlazor;

namespace Simulab.Web.Components.Ui;

/// <summary>A number column: right-aligned, formatted with the user's culture.</summary>
public class AppNumberColumn<TItem, TProperty> : PropertyColumn<TItem, TProperty>
{
    public const string CssClass = "app-cell-number";

    protected override void OnParametersSet()
    {
        ApplyKitDefaults(this, CssClass);
        base.OnParametersSet();
    }

    internal static void ApplyKitDefaults(PropertyColumn<TItem, TProperty> column, string cssClass)
    {
#pragma warning disable BL0005 // The kit sets its own inherited column parameters once, before the base reads them.
        column.CellClass = Join(column.CellClass, cssClass);
        column.HeaderClass = Join(column.HeaderClass, cssClass);
#pragma warning restore BL0005
    }

    private static string Join(string? existing, string cssClass) =>
        existing is null ? cssClass
        : existing.Contains(cssClass, StringComparison.Ordinal) ? existing
        : $"{existing} {cssClass}";
}
