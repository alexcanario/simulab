using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.Web.Components.Layout;
using Simulab.Web.Components.Pages;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Layout;

/// <summary>
/// F-134 AC5 to AC9 (BR4, BR5, BR6, BR8): the error page and the not-found page share the kit's page state. The error
/// page has the one amber action, "Try again", which reloads the address the user was on; the not-found page, which also
/// answers a route the user may not open, has only "Go to home".
/// </summary>
public sealed class ErrorPageTests : KitTestContext
{
    private IRenderedComponent<Error> RenderError(string? requestId = "trace-123", int viewportWidth = 1280)
    {
        Services.AddSingleton<IBrowserViewportService>(new FixedWidthViewport(viewportWidth));
        return Render<Error>(parameters =>
        {
            if (requestId is not null)
            {
                parameters.AddCascadingValue(new DefaultHttpContext { TraceIdentifier = requestId });
            }
        });
    }

    [Fact]
    public void Error_ShowsTheIconTheTitleTheMessageAndTheRequestId()
    {
        var error = RenderError();

        error.Find(".app-page-state .mud-icon-root").Should().NotBeNull();
        error.Find("h1").TextContent.Trim().Should().Be("Something went wrong");
        error.Find(".app-page-state-message").TextContent.Trim().Should().Be("An error occurred while processing your request.");
        var requestId = error.Find(".app-page-state-request-id");
        requestId.TextContent.Should().Contain("trace-123");
        AppCssColours.Declaration(".app-page-state-request-id", "font-family").Should().Contain("--app-font-mono");
    }

    [Fact]
    public void Error_HasTheAmberTryAgainActionAndASecondaryGoToHomeLink()
    {
        var error = RenderError();

        error.FindAll(".app-action").Should().ContainSingle("one amber action per screen");
        error.Find(".app-action-key").TextContent.Trim().Should().Be("Try again");
        var home = error.Find(".app-page-state-actions a[href='/']");
        home.TextContent.Trim().Should().Be("Go to home");
        home.ClassList.Should().Contain("mud-button-outlined");
    }

    [Fact]
    public void TryAgain_ReloadsTheAddressTheUserWasOn()
    {
        var error = RenderError();
        var navigation = Services.GetRequiredService<BunitNavigationManager>();
        var address = navigation.Uri;

        error.Find(".app-action-key").Click();

        var entry = navigation.History.Should().ContainSingle().Which;
        entry.Uri.Should().Be(address);
        entry.Options.ForceLoad.Should().BeTrue("a reload, so the server answers the address again");
    }

    [Fact]
    public void Error_WithoutARequestId_ShowsNoRequestIdLine()
    {
        var error = RenderError(requestId: null);

        error.Find(".app-page-state h1").TextContent.Trim().Should().Be("Something went wrong");
        error.FindAll(".app-page-state-request-id").Should().BeEmpty();
        error.Markup.Should().NotContain("Request ID");
    }

    [Fact]
    public void Error_OnAViewportUnder360_TakesTheCompactActionAndWrapsItsActions()
    {
        var error = RenderError(viewportWidth: 320);

        error.WaitForAssertion(() => error.FindAll(".app-action-compact").Should().ContainSingle());
        AppCssColours.Declaration(".app-page-state-actions", "flex-wrap").Should().Be("wrap");
    }

    [Fact]
    public void Error_OnAWiderViewport_TakesTheFullAction()
    {
        var error = RenderError(viewportWidth: 1280);

        error.FindAll(".app-action-compact").Should().BeEmpty();
        error.FindAll(".app-action-ctrl").Should().ContainSingle();
    }
}

public sealed class NotFoundPageTests : KitTestContext
{
    [Fact]
    public void NotFound_ShowsTheIconTheTitleTheMessageAndOnlyGoToHome()
    {
        var page = Render<NotFound>();

        page.Find(".app-page-state .mud-icon-root").Should().NotBeNull();
        page.Find("h1").TextContent.Trim().Should().Be("Page not found");
        page.Find(".app-page-state-message").TextContent.Trim().Should().Be("The page you are looking for does not exist.");
        var actions = page.FindAll(".app-page-state-actions a, .app-page-state-actions button");
        actions.Should().ContainSingle();
        actions[0].GetAttribute("href").Should().Be("/");
        actions[0].TextContent.Trim().Should().Be("Go to home");
        actions[0].ClassList.Should().Contain("mud-button-filled");
        page.FindAll(".app-action").Should().BeEmpty("nothing restarts here");
        page.Markup.Should().NotContain("404");
    }

    [Fact]
    public void RouteWithoutPermission_RendersTheSameMarkupAsAPageThatDoesNotExist()
    {
        // Blazor numbers the event handlers of each render; the numbers are not part of what the user sees.
        var notFound = WithoutHandlerIds(Render<NotFound>().Find(".app-page-state").OuterHtml);
        var forbidden = WithoutHandlerIds(Render<NotFoundContent>().Find(".app-page-state").OuterHtml);

        forbidden.Should().Be(notFound);
        forbidden.Should().NotContainAny("permission", "authorized", "forbidden", "403");
    }

    [Fact]
    public void Router_AnswersARouteTheUserIsNotAuthorizedForWithTheNotFoundContent()
    {
        var routes = File.ReadAllText(Path.Combine(WebRoot(), "Components", "Routes.razor"));

        routes.Should().MatchRegex(@"<NotAuthorized>\s*<NotFoundContent\s*/>\s*</NotAuthorized>");
        routes.Should().Contain("NotFoundPage=\"typeof(Pages.NotFound)\"");
    }

    private static string WithoutHandlerIds(string markup) =>
        System.Text.RegularExpressions.Regex.Replace(markup, @"blazor:onclick=""\d+""", "blazor:onclick");

    private static string WebRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests must run inside the repository");
        return Path.Combine(directory!.FullName, "src", "Hosts", "Simulab.Web");
    }
}
