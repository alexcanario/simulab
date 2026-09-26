using Bunit;
using MudBlazor;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-43 BR1: the leading icon the three fields gained. It is decorative on purpose — the label is what names
/// the field — and it never takes the place of an adornment the page already set (the password field's reveal
/// button, the lookup's own magnifier when nothing else was asked for).
/// </summary>
public sealed class AppFieldLeadingIconTests : KitTestContext
{
    [Fact]
    public void TextField_WithALeadingIcon_ShowsItAtTheStart()
    {
        var field = Render<AppTextField<string>>(parameters => parameters
            .Add(text => text.Id, "exam-name")
            .Add(text => text.Label, "Name")
            .Add(text => text.LeadingIcon, AppIcons.Exams));

        field.Find(".mud-input-adornment-start").Should().NotBeNull();
    }

    /// <summary>An adornment the page set wins: replacing it would remove a control, not an ornament.</summary>
    [Fact]
    public void TextField_WithItsOwnAdornment_KeepsIt()
    {
        var field = Render<AppTextField<string>>(parameters => parameters
            .Add(text => text.Id, "exam-name")
            .Add(text => text.Label, "Name")
            .Add(text => text.LeadingIcon, AppIcons.Exams)
            .Add(text => text.Adornment, Adornment.End)
            .Add(text => text.AdornmentIcon, AppIcons.Search)
            .Add(text => text.AdornmentAriaLabel, "Search"));

        field.FindAll(".mud-input-adornment-start").Should().BeEmpty();
        field.Find(".mud-input-adornment-end").Should().NotBeNull();
    }

    [Fact]
    public void SelectField_WithoutALeadingIcon_HasNoAdornment()
    {
        var field = Render<AppSelectField<string>>(parameters => parameters
            .Add(select => select.Id, "exam-scope")
            .Add(select => select.Label, "Scope")
            .Add(select => select.Options, [new AppSelectOption<string>("National", "National")]));

        field.FindAll(".mud-input-adornment-start").Should().BeEmpty();
    }

    [Fact]
    public void SelectField_WithALeadingIcon_ShowsItAtTheStart()
    {
        var field = Render<AppSelectField<string>>(parameters => parameters
            .Add(select => select.Id, "exam-scope")
            .Add(select => select.Label, "Scope")
            .Add(select => select.LeadingIcon, AppIcons.Scope)
            .Add(select => select.Options, [new AppSelectOption<string>("National", "National")]));

        field.Find(".mud-input-adornment-start").Should().NotBeNull();
    }
}
