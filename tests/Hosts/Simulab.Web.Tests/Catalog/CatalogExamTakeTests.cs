using System.Net;
using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.ExamEngine.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Tests.Attempts;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-169 `/catalog/exams/{id}`: the start action on each published edition (UC1, BR1). Present for who holds
/// <c>attempts.take</c>, absent for a curator; it starts the attempt, or the one in progress, and opens it.
/// </summary>
public sealed class CatalogExamTakeTests : StudentCatalogTestContext
{
    private IRenderedComponent<CatalogExam> RenderPage()
    {
        Api.Details[GuardaMunicipal.Id] = new PublishedExamDetailResponse(
            GuardaMunicipal.Id,
            GuardaMunicipal.Name,
            GuardaMunicipal.IssuingAuthorityName,
            GuardaMunicipal.AssessmentType,
            GuardaMunicipal.Scope,
            GuardaMunicipal.ScopeDetail,
            GuardaMunicipal.ContentLanguage,
            2,
            2025,
            [Edition2025, Edition2023]);

        var page = Render<CatalogExam>(parameters => parameters.Add(exam => exam.Id, GuardaMunicipal.Id));
        page.WaitForAssertion(() => page.FindAll("li.app-item-row").Should().HaveCount(2));
        return page;
    }

    private void SignInAsAStudent() =>
        Authorization.SetPolicies(PermissionPolicy.Prefix + ExamEnginePermissions.Take);

    // AC17 [sec]: a curator (catalog.browse only) sees the rows as they are today, with no start action at all.
    [Fact]
    public void ForAUserWithoutTake_NoEditionHasAStartAction()
    {
        var page = RenderPage();

        page.FindAll(".app-edition-take").Should().BeEmpty();
        page.FindAll("li.app-item-row button").Should().BeEmpty();
        Attempts.Received.Should().BeEmpty();
    }

    // AC17 [sec] and BR1: a student sees one start action on each published edition, named by the edition.
    [Fact]
    public void ForAHolderOfTake_EachEditionHasAStartActionNamedByTheEdition()
    {
        SignInAsAStudent();

        var page = RenderPage();

        var buttons = page.FindAll("li.app-item-row button.app-edition-take");
        buttons.Should().HaveCount(2);
        buttons.Select(button => button.TextContent.Trim()).Should().OnlyContain(text => text == "Take this exam");
        buttons[0].GetAttribute("aria-label").Should().Be(
            "Take this exam: 2025, Guarda Municipal de 3a Classe, Instituto Consulplan (CONSULPLAN)");
        buttons[1].GetAttribute("aria-label").Should().Be("Take this exam: 2023, Fundacao Getulio Vargas (FGV)");
        buttons[0].GetAttribute("aria-label")!.Should().StartWith(buttons[0].TextContent.Trim());
        buttons[0].ClassList.Should().Contain("mud-button-outlined", "the page has no primary action");
        page.FindAll(".app-primary-action, .mud-button-filled-primary").Should().BeEmpty();
    }

    // AC1: the action starts an attempt on that edition and opens it; the Api decides whether it is new or in progress.
    [Fact]
    public void TheStartAction_StartsAnAttemptOnThatEdition_AndOpensIt()
    {
        SignInAsAStudent();
        var page = RenderPage();

        page.FindAll("button.app-edition-take")[1].Click();

        page.WaitForAssertion(() =>
        {
            var start = Attempts.Writes.Should().ContainSingle().Which;
            start.Method.Should().Be(HttpMethod.Post);
            start.Path.Should().Be("/api/v1/exam-engine/attempts");
            start.Body.Should().Contain(Edition2023.Id.ToString());
            Navigation.Uri.Should().EndWith($"/attempts/{AttemptSamples.AttemptId}");
        });
    }

    // The start in flight: the clicked button is disabled, shows progress in place of its icon and says it is busy; the
    // other row's button stays enabled.
    [Fact]
    public async Task WhileTheStartIsInFlight_TheClickedButtonIsBusy_AndTheOtherStaysEnabled()
    {
        SignInAsAStudent();
        var hold = new TaskCompletionSource();
        Attempts.HoldStart = hold;
        var page = RenderPage();

        page.FindAll("button.app-edition-take")[0].Click();

        page.WaitForAssertion(() =>
        {
            var buttons = page.FindAll("button.app-edition-take");
            buttons[0].HasAttribute("disabled").Should().BeTrue();
            buttons[0].GetAttribute("aria-busy").Should().Be("true");
            buttons[0].QuerySelector("[role=progressbar]")!.ParentElement!.GetAttribute("aria-hidden").Should().Be("true");
            buttons[0].QuerySelector(".mud-button-icon-start").Should().BeNull("the progress takes the place of the icon");
            buttons[1].HasAttribute("disabled").Should().BeFalse();
        });

        await page.InvokeAsync(() => hold.SetResult());
        page.WaitForAssertion(() => Navigation.Uri.Should().EndWith($"/attempts/{AttemptSamples.AttemptId}"));
    }

    // The start refused with a code the student can act on: the alert at the top of the card, in that code's words, and
    // the button returns to normal.
    [Theory]
    [InlineData(ExamAttemptErrorCodes.EditionNotFound, "This exam is not available.")]
    [InlineData(ExamAttemptErrorCodes.EditionWithoutQuestions, "This exam has no questions yet.")]
    [InlineData("common.server_error", "The exam could not be started. Try again.")]
    public void AStartThatFails_ShowsAnAlertAtTheTopOfTheCard_AndTheButtonIsBackToNormal(string code, string text)
    {
        SignInAsAStudent();
        Attempts.StartFailure = (HttpStatusCode.UnprocessableEntity, code);
        var page = RenderPage();

        page.FindAll("button.app-edition-take")[0].Click();

        page.WaitForAssertion(() => page.Find(".app-alert").TextContent.Should().Contain(text));
        page.Find(".app-alert").GetAttribute("role").Should().Be("alert");
        var button = page.FindAll("button.app-edition-take")[0];
        button.HasAttribute("disabled").Should().BeFalse();
        button.HasAttribute("aria-busy").Should().BeFalse();
        Navigation.Uri.Should().NotContain("/attempts/");
    }
}
