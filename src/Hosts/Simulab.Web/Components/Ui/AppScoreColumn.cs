using Microsoft.AspNetCore.Components;

namespace Simulab.Web.Components.Ui;

public class AppScoreColumn<TItem, TProperty> : AppNumberColumn<TItem, TProperty>
{
    [Parameter, EditorRequired] public int Decimals { get; set; }
}
