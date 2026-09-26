using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-43 BR1: the field another choice reveals. What matters is the way back — hiding it tells the page to drop
/// the value, so a value nobody can see is never sent (F-34 AC18b, now the kit's job).
/// </summary>
public sealed class AppConditionalFieldTests : KitTestContext
{
    private int _cleared;

    private IRenderedComponent<AppConditionalField> Render(bool visible) =>
        Render<AppConditionalField>(parameters => parameters
            .Add(field => field.Visible, visible)
            .Add(field => field.OnHidden, () => _cleared++)
            .Add(field => field.ChildContent, builder => builder.AddMarkupContent(0, "<p>the state</p>")));

    [Fact]
    public void Render_Visible_TheFieldIsThereAndChangesAreAnnouncedPolitely()
    {
        var field = Render(true);

        field.Find(".app-conditional-field").GetAttribute("aria-live").Should().Be("polite");
        field.Markup.Should().Contain("the state");
    }

    [Fact]
    public void Render_Hidden_NothingIsRendered()
    {
        Render(false).Markup.Should().NotContain("the state");
    }

    [Fact]
    public void Hiding_AfterBeingVisible_AsksThePageToDropTheValue()
    {
        var field = Render(true);

        field.Render(parameters => parameters.Add(component => component.Visible, false));

        _cleared.Should().Be(1);
    }

    /// <summary>A field that was never on screen has nothing to clear; clearing anyway would wipe a loaded value.</summary>
    [Fact]
    public void Render_HiddenFromTheStart_NothingIsCleared()
    {
        Render(false);

        _cleared.Should().Be(0);
    }

    [Fact]
    public void Showing_Again_DoesNotClear()
    {
        var field = Render(false);

        field.Render(parameters => parameters.Add(component => component.Visible, true));

        _cleared.Should().Be(0);
    }
}
