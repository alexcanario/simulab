using Bunit;
using MudBlazor;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-105 AC18, owner decision of 2026-10-17: the shared reason dialog shows "{n}/500" as the user types, so it
/// applies to F-101's annul and reinstate dialogs, to the rejection and to the approval note. Here it is seen on
/// the annul dialog of the question form; the field itself is tested apart (<c>AppTextFieldCounterTests</c>).
/// </summary>
public sealed class QuestionReasonCounterTests : QuestionPageTestContext
{
    private const string ReasonId = "question-status-reason";

    private static Task TypeReasonAsync(IRenderedComponent<MudDialogProvider> dialogs, string reason) =>
        dialogs.InvokeAsync(() => dialogs.FindComponents<AppTextField<string>>()
            .Single(field => field.Instance.Id == ReasonId)
            .Instance.ValueChanged.InvokeAsync(reason));

    private async Task<IRenderedComponent<MudDialogProvider>> OpenAnnulDialogAsync()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Published);
        var page = Render<QuestionForm>(parameters => parameters.Add(form => form.Id, Published.Id));
        page.WaitForAssertion(() => RichText.Editors.Keys.Should().Contain("question-statement"));
        page.WaitForElement("#question-annul").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll($"#{ReasonId}").Should().ContainSingle());
        await Task.CompletedTask;

        return providers.Dialogs;
    }

    // AC18: the dialog opens with "0/500" in the description the field points at.
    [Fact]
    public async Task ReasonDialog_OpensWithTheCounterAtZero_InsideTheFieldsDescription()
    {
        var dialogs = await OpenAnnulDialogAsync();

        var description = dialogs.Find($"#{ReasonId}-description");
        description.QuerySelector(".app-field-counter")!.TextContent.Trim().Should().Be("0/500");
        dialogs.Find($"#{ReasonId}").GetAttribute("aria-describedby").Should().Be($"{ReasonId}-description");
    }

    // AC18: the count follows what is typed.
    [Fact]
    public async Task ReasonDialog_CountsWhatIsTyped()
    {
        var dialogs = await OpenAnnulDialogAsync();

        await TypeReasonAsync(dialogs, "A banca anulou o item 12.");

        dialogs.WaitForAssertion(() =>
            dialogs.Find($"#{ReasonId}-description .app-field-counter").TextContent.Trim().Should().Be("25/500"));
    }

    // AC18: an error text appears beside the counter and never replaces it.
    [Fact]
    public async Task ReasonDialog_AnEmptyReason_ShowsTheErrorBesideTheCounter()
    {
        var dialogs = await OpenAnnulDialogAsync();

        dialogs.Find("#question-status-reason-confirm").Click();

        dialogs.WaitForAssertion(() =>
        {
            var description = dialogs.Find($"#{ReasonId}-description");
            description.QuerySelector(".app-field-error-text")!.TextContent.Should().Contain("Write the reason.");
            description.QuerySelector(".app-field-counter")!.TextContent.Trim().Should().Be("0/500");
        });
    }
}
