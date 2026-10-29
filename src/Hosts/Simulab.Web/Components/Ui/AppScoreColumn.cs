using Microsoft.AspNetCore.Components;

namespace Simulab.Web.Components.Ui;

/// <summary>F-181 BR7: a score column. It is the number column with the decimals the meaning asks for, so every table
/// shows a score the same way.</summary>
public class AppScoreColumn<TItem, TProperty> : AppNumberColumn<TItem, TProperty>
{
    /// <summary>Decimal places shown; they come from the exam rule, so there is no default.</summary>
    [Parameter, EditorRequired] public int Decimals { get; set; }

    protected override void OnParametersSet()
    {
#pragma warning disable BL0005 // The kit sets its own inherited column parameter once, before the base reads it.
        Format = "N" + Decimals.ToString(System.Globalization.CultureInfo.InvariantCulture);
#pragma warning restore BL0005
        base.OnParametersSet();
    }
}
