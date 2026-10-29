using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Simulab.Web.Components.Ui;

/// <summary>F-184 BR3: a yes/no column: an icon with the words, a dash for none.</summary>
public class AppBoolColumn<TItem> : PropertyColumn<TItem, bool?>
{
    private Func<TItem, bool?>? _getter;

    /// <summary>The word for true; "Yes" when not set.</summary>
    [Parameter] public string? TrueText { get; set; }

    /// <summary>The word for false; "No" when not set.</summary>
    [Parameter] public string? FalseText { get; set; }

    protected override void OnParametersSet()
    {
#pragma warning disable BL0005 // The kit sets its own inherited column parameter once, before the base reads it.
        CellTemplate ??= context => builder =>
        {
            _getter ??= Property.Compile();
            builder.OpenComponent<AppBoolText>(0);
            builder.AddComponentParameter(1, nameof(AppBoolText.Value), _getter(context.Item));
            builder.AddComponentParameter(2, nameof(AppBoolText.TrueText), TrueText);
            builder.AddComponentParameter(3, nameof(AppBoolText.FalseText), FalseText);
            builder.AddComponentParameter(4, nameof(AppBoolText.ShowIcon), true);
            builder.CloseComponent();
        };
#pragma warning restore BL0005
        base.OnParametersSet();
    }
}
