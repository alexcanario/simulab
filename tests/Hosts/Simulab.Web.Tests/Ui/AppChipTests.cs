using Bunit;
using Microsoft.AspNetCore.Components;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-75 Screen 4: the read-only label chip of the kit, and its optional remove button.</summary>
public sealed class AppChipTests : KitTestContext
{
    private static string Flat(string text) => System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

    [Fact]
    public void Render_PlainChip_ShowsTheTextAndIsNotFocusableAndHasNoButton()
    {
        var chip = Render<AppChip>(parameters => parameters.Add(p => p.Text, "Matemática"));

        chip.Find(".app-chip-text").TextContent.Trim().Should().Be("Matemática");
        chip.FindAll("button, a, [tabindex]").Should().BeEmpty("a read-only chip takes no focus and links nowhere");
        chip.FindAll(".app-chip-icon").Should().BeEmpty();
    }

    [Fact]
    public void Render_WithIconAndSecondaryText_TheIconIsDecorativeAndTheMeaningIsInTheText()
    {
        var chip = Render<AppChip>(parameters => parameters
            .Add(p => p.Text, "Matemática")
            .Add(p => p.Icon, AppIcons.Subjects)
            .Add(p => p.Secondary, "whole subject"));

        chip.Find(".app-chip-icon").GetAttribute("aria-hidden").Should().Be("true");
        chip.Find(".app-chip-secondary").TextContent.Trim().Should().Be("whole subject");
        chip.Find(".app-chip-text > [aria-hidden='true']").TextContent.Should().Contain("·", "the dot between the two is only drawn");
        Flat(chip.Find(".app-chip-text .app-visually-hidden").TextContent).Should().Be(",", "a screen reader hears a comma instead");
    }

    [Fact]
    public void Render_WithSpokenText_TheVisibleFormIsHiddenFromAScreenReaderAndTheSpokenOneIsRead()
    {
        var chip = Render<AppChip>(parameters => parameters
            .Add(p => p.Text, "Raciocínio Lógico › Proposições")
            .Add(p => p.SpokenText, "Proposições, topic of Raciocínio Lógico"));

        chip.Find(".app-chip-text > span[aria-hidden='true']").TextContent.Should().Be("Raciocínio Lógico › Proposições");
        chip.Find(".app-chip-text > .app-visually-hidden").TextContent.Should().Be("Proposições, topic of Raciocínio Lógico");
    }

    [Fact]
    public void Render_MarkedChip_HasTheErrorStateAndSaysWhyInWords()
    {
        var chip = Render<AppChip>(parameters => parameters
            .Add(p => p.Text, "Proposições")
            .Add(p => p.Mark, "repeats the whole subject"));

        chip.Find(".app-chip").ClassList.Should().Contain("is-marked");
        chip.Find(".app-chip-mark").TextContent.Should().Contain("repeats the whole subject");
    }

    [Fact]
    public void Render_Removable_TheButtonIsNamedByThePageHasItsIdAndCallsBack()
    {
        var removed = false;
        var remove = new AppChipRemove("Remove Matemática", EventCallback.Factory.Create(this, () => removed = true), "chip-remove-1");
        var chip = Render<AppChip>(parameters => parameters
            .Add(p => p.Text, "Matemática")
            .Add(p => p.Remove, remove));

        chip.Find(".app-chip").ClassList.Should().Contain("is-removable");
        var button = chip.Find("button.app-chip-remove");
        button.GetAttribute("aria-label").Should().Be("Remove Matemática");
        button.Id.Should().Be("chip-remove-1");
        button.HasAttribute("disabled").Should().BeFalse();

        button.Click();
        removed.Should().BeTrue();
    }

    [Fact]
    public void Render_RemovableAndDisabled_TheButtonIsDisabled()
    {
        var remove = new AppChipRemove("Remove Matemática", EventCallback.Empty, Disabled: true);
        var chip = Render<AppChip>(parameters => parameters
            .Add(p => p.Text, "Matemática")
            .Add(p => p.Remove, remove));

        chip.Find("button.app-chip-remove").HasAttribute("disabled").Should().BeTrue();
    }
}
