using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-95 AC17 as far as bUnit reaches: the rich text field's anatomy, the attributes it asks the editor to put on
/// the editable region, the toolbar's accessible names and the way a typed value travels. The editor itself is a
/// fake; Quill's own DOM and the keyboard are checked on screen (gallery, <c>unverified (gallery DOM)</c>).
/// </summary>
public sealed class AppRichTextFieldTests : KitTestContext
{
    private const string FieldId = "sample-statement";

    private IRenderedComponent<AppRichTextField> RenderField(Action<ComponentParameterCollectionBuilder<AppRichTextField>>? more = null) =>
        Render<AppRichTextField>(parameters =>
        {
            parameters
                .Add(field => field.Id, FieldId)
                .Add(field => field.Label, "Statement")
                .Add(field => field.Placeholder, "Write the statement");
            more?.Invoke(parameters);
        });

    private void WaitForEditor(IRenderedComponent<AppRichTextField> field, string id = FieldId) =>
        field.WaitForAssertion(() => RichText.Editors.Should().ContainKey(id));

    [Fact]
    public void Render_Field_HasALabelThatIsNotForAnInputAndAHintThatDescribesTheEditor()
    {
        var field = RenderField(parameters => parameters
            .Add(f => f.Required, true)
            .Add(f => f.Hint, "Up to 10,000 characters."));

        // The editable is a div, so the label is tied by aria-labelledby (the editor sets it), not by `for`.
        var label = field.Find($"label#{FieldId}-label");
        label.TextContent.Should().Contain("Statement");
        label.HasAttribute("for").Should().BeFalse();
        field.Find(".app-field-required").GetAttribute("aria-hidden").Should().Be("true");
        field.Find($"#{FieldId}-description").TextContent.Should().Contain("Up to 10,000 characters.");
        field.FindAll(".app-field-error-text").Should().BeEmpty();
    }

    [Fact]
    public void Render_Field_AsksTheEditorForTheNameTheStateAndTheLanguageOfTheEditableRegion()
    {
        var field = RenderField(parameters => parameters
            .Add(f => f.Required, true)
            .Add(f => f.Lang, "pt-BR"));

        WaitForEditor(field);
        var options = RichText.Editors[FieldId];
        options.LabelId.Should().Be($"{FieldId}-label");
        options.DescriptionId.Should().Be($"{FieldId}-description");
        options.Lang.Should().Be("pt-BR", "a screen reader switches voice for content in another language");
        options.Required.Should().BeTrue();
        options.Invalid.Should().BeFalse();
        options.Disabled.Should().BeFalse();
        options.Placeholder.Should().Be("Write the statement");
    }

    [Fact]
    public void Render_ToolbarIsOneTabStopWithAButtonThatNamesItselfPerFormat()
    {
        var field = RenderField();

        var toolbar = field.Find("[role='toolbar']");
        toolbar.GetAttribute("aria-label").Should().Be("Formatting");
        toolbar.GetAttribute("aria-controls").Should().Be(FieldId);

        var buttons = field.FindAll("[role='toolbar'] button");
        buttons.Select(button => button.GetAttribute("aria-label")).Should().Equal(
            "Bold (Ctrl+B)", "Italic (Ctrl+I)", "Underline (Ctrl+U)", "Strikethrough",
            "Superscript", "Subscript", "Bulleted list", "Numbered list", "Quote", "Link", "Clear formatting");
        buttons.Should().OnlyContain(button => button.GetAttribute("type") == "button", "no toolbar button submits a form");
        buttons.Should().OnlyContain(button => button.GetAttribute("title") == button.GetAttribute("aria-label"));
        buttons.Count(button => button.GetAttribute("tabindex") == "0").Should().Be(1, "roving tabindex: one tab stop");

        // Toggles say whether they are pressed; "clear formatting" is an action and does not.
        buttons.Where(button => button.HasAttribute("aria-pressed")).Should().HaveCount(10);
        buttons[^1].HasAttribute("aria-pressed").Should().BeFalse();
    }

    [Fact]
    public void Render_Error_ReplacesTheHintAndIsAnnouncedAndMarksTheEditorInvalid()
    {
        var field = RenderField(parameters => parameters
            .Add(f => f.Hint, "Up to 10,000 characters.")
            .Add(f => f.Error, "Write the statement."));

        var error = field.Find($"#{FieldId}-description .app-field-error-text");
        error.TextContent.Should().Be("Write the statement.");
        error.GetAttribute("aria-live").Should().Be("polite", "an error that appears on blur is spoken");
        field.Find(".app-rich-text").ClassList.Should().Contain("app-field-error");
        field.FindAll(".app-field-hint").Should().BeEmpty();
        field.WaitForAssertion(() => RichText.Editors[FieldId].Invalid.Should().BeTrue());
    }

    [Fact]
    public void Render_ErrorAppearingLater_IsSentToTheEditorAsInvalid()
    {
        var field = RenderField();
        WaitForEditor(field);

        field.Render(parameters => parameters.Add(f => f.Error, "Write the statement."));

        field.WaitForAssertion(() => RichText.Editors[FieldId].Invalid.Should().BeTrue());
    }

    [Fact]
    public void Render_Compact_HasInlineFormatsOnlyAndAVisuallyHiddenLabel()
    {
        var field = RenderField(parameters => parameters
            .Add(f => f.Compact, true)
            .Add(f => f.LabelVisuallyHidden, true));

        field.Find(".app-rich-text").ClassList.Should().Contain("app-rich-text-compact");
        field.Find($"#{FieldId}-label").ClassList.Should().Contain("app-visually-hidden");
        field.FindAll("[role='toolbar'] button").Select(button => button.GetAttribute("aria-label")).Should().Equal(
            "Bold (Ctrl+B)", "Italic (Ctrl+I)", "Underline (Ctrl+U)", "Strikethrough",
            "Superscript", "Subscript", "Link", "Clear formatting");
        WaitForEditor(field);
        RichText.Editors[FieldId].Compact.Should().BeTrue();
    }

    [Fact]
    public void Render_Disabled_DisablesTheToolbarAndTellsTheEditor()
    {
        var field = RenderField(parameters => parameters.Add(f => f.Disabled, true));

        field.FindAll("[role='toolbar'] button").Should().OnlyContain(button => button.HasAttribute("disabled"));
        field.Find(".app-rich-text").ClassList.Should().Contain("app-rich-text-disabled");
        field.WaitForAssertion(() => RichText.Editors[FieldId].Disabled.Should().BeTrue());
    }

    // The ui rule on long text: bUnit cannot measure layout, so it asserts the field sets no width of its own and
    // the text is held by the kit's wrapping elements; the gallery shows the real wrap.
    [Fact]
    public void Render_LongText_SetsNoFixedWidthOnTheFieldOrItsHost()
    {
        var longText = "<p>" + new string('a', 5000) + "</p>";

        var field = RenderField(parameters => parameters.Add(f => f.Value, longText));

        field.FindAll("[style]").Where(element => (element.GetAttribute("style") ?? string.Empty).Contains("width", StringComparison.OrdinalIgnoreCase))
            .Should().BeEmpty();
        field.Find($"#{FieldId}-editor").ClassList.Should().Contain("app-rt-frame");
        WaitForEditor(field);
        RichText.Editors[FieldId].Html.Should().Be(longText);
    }

    [Fact]
    public async Task Typing_RaisesValueChangedWithWhatTheEditorHolds()
    {
        string? received = null;
        var field = RenderField(parameters => parameters
            .Add(f => f.ValueChanged, EventCallback.Factory.Create<string?>(this, value => received = value)));
        WaitForEditor(field);

        await field.InvokeAsync(() => RichText.TypeAsync(FieldId, "<p><strong>Hi</strong></p>"));

        received.Should().Be("<p><strong>Hi</strong></p>");
    }

    [Fact]
    public async Task Value_ComingFromOutside_ReplacesTheEditorContent_ButAValueTheEditorRaisedIsNotPushedBack()
    {
        var field = RenderField(parameters => parameters.Add(f => f.Value, "<p>stored</p>"));
        WaitForEditor(field);
        RichText.Pushed.Should().BeEmpty("the starting value is attached with the editor, not pushed");

        // What the Api stored (sanitized) comes back and replaces what the editor holds.
        field.Render(parameters => parameters.Add(f => f.Value, "<p>sanitized</p>"));
        field.WaitForAssertion(() => RichText.Pushed.Should().ContainSingle().Which.Html.Should().Be("<p>sanitized</p>"));

        // What the editor itself raised comes back as the value: nothing to push.
        await field.InvokeAsync(() => RichText.TypeAsync(FieldId, "<p>typed</p>"));
        field.Render(parameters => parameters.Add(f => f.Value, "<p>typed</p>"));
        RichText.Pushed.Should().ContainSingle();
    }

    [Fact]
    public void AutoFocus_FocusesTheEditorOnceItIsAttached()
    {
        var field = RenderField(parameters => parameters.Add(f => f.AutoFocus, true));

        field.WaitForAssertion(() => RichText.Focused.Should().Contain(FieldId));
    }

    [Fact]
    public async Task Link_AnAddressThatIsNotHttpIsRefusedInTheDialog_AndAValidOneIsHandedToTheEditor()
    {
        var provider = Render<MudDialogProvider>();
        Render<MudPopoverProvider>();
        var field = RenderField();
        WaitForEditor(field);

        string? answer = null;
        var pending = field.InvokeAsync(async () => answer = await RichText.PressLinkAsync(FieldId));

        provider.WaitForAssertion(() => provider.FindAll("#richtext-link-url").Should().ContainSingle());
        provider.Find("#richtext-link-url").Change("javascript:alert(1)");
        provider.Find(".app-link-dialog-insert").Click();

        provider.WaitForAssertion(() => provider.Markup.Should().Contain("Only http and https addresses."));
        pending.IsCompleted.Should().BeFalse("a refused address keeps the dialog open");

        provider.Find("#richtext-link-url").Change("https://example.org/a");
        provider.Find(".app-link-dialog-insert").Click();

        await pending;
        answer.Should().Be("https://example.org/a");
    }

    [Fact]
    public async Task Link_Cancel_ChangesNothing()
    {
        var provider = Render<MudDialogProvider>();
        Render<MudPopoverProvider>();
        var field = RenderField();
        WaitForEditor(field);

        string? answer = "not asked";
        var pending = field.InvokeAsync(async () => answer = await RichText.PressLinkAsync(FieldId, "https://old.example"));
        provider.WaitForAssertion(() => provider.FindAll("#richtext-link-url").Should().ContainSingle());
        provider.Find("#richtext-link-url").GetAttribute("value").Should().Be("https://old.example");

        provider.Find(".app-link-dialog-cancel").Click();

        await pending;
        answer.Should().BeNull();
    }

    [Theory]
    [InlineData("https://example.org", true)]
    [InlineData("http://example.org/a?b=c", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:text/html;base64,AAAA", false)]
    [InlineData("ftp://example.org", false)]
    [InlineData("example.org", false)]
    [InlineData("", false)]
    public void LinkDialog_IsAllowed_OnlyAbsoluteHttpAndHttpsAddresses(string address, bool expected)
    {
        AppRichTextLinkDialog.IsAllowed(address).Should().Be(expected);
    }
}
