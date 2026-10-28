using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor;
using Simulab.ExamEngine.Contracts;
using Simulab.Web.Components.Layout;
using Simulab.Web.Components.Pages.Attempts;
using Simulab.Web.Services;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Attempts;

/// <summary>
/// F-169's two attempt screens, signed in as a Student holding <c>attempts.take</c>, with a fake Api behind the typed
/// client. The simulator is rendered inside its own layout, because the bar (the edition, the way out, the answer-sheet
/// button) belongs to the layout and the library's drawer needs the layout it sits in.
/// </summary>
public abstract class AttemptTestContext : KitTestContext
{
    private const string WebSessionId = "web-student";

    protected FakeExamEngineApi Api { get; } = new();

    /// <param name="viewportWidth">1280 is a desktop (the side panel); 390 a phone (the drawer).</param>
    protected AttemptTestContext(int viewportWidth = 1280)
    {
        Services.AddSingleton(new ExamEngineApiClient(new HttpClient(Api) { BaseAddress = new Uri("https://api.test") }));
        Services.AddSingleton<IBrowserViewportService>(new FixedWidthViewport(viewportWidth));
        Services.AddScoped<UserTimeZone>();

        var store = new InMemoryWebSessionStore();
        store.SaveAsync(WebSessionId, new WebSession("jti-1", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddMinutes(10), [ExamEnginePermissions.Take], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)));
        Services.AddSingleton<FakeAuthApi>();
        Services.AddSingleton(provider => new WebSessionTokenAccessor(
            store,
            new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())),
            new SessionRefreshGate(),
            TimeProvider.System));

        Authorization.SetAuthorized("ana@exemplo.com")
            .SetClaims(new Claim(WebAuthClaims.WebSessionId, WebSessionId), new Claim(WebAuthClaims.Permission, ExamEnginePermissions.Take));
    }

    protected NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    /// <summary>The simulator inside the exam session's layout, waiting until the first question shows.</summary>
    protected IRenderedComponent<ExamSessionLayout> RenderSimulator(Guid? id = null)
    {
        var layout = RenderSimulatorWithoutWaiting(id);
        layout.WaitForAssertion(() => layout.FindAll("#attempt-question-title").Should().ContainSingle());
        return layout;
    }

    protected IRenderedComponent<ExamSessionLayout> RenderSimulatorWithoutWaiting(Guid? id = null)
    {
        // The layout renders the popover, dialog and snackbar providers, as it does in the app.
        return Render<ExamSessionLayout>(parameters => parameters.Add(
            layout => layout.Body,
            (RenderFragment)(builder =>
            {
                builder.OpenComponent<TakeAttempt>(0);
                builder.AddAttribute(1, nameof(TakeAttempt.Id), id ?? AttemptSamples.AttemptId);
                builder.CloseComponent();
            })));
    }

    protected IRenderedComponent<AttemptSubmitted> RenderSubmitted(Guid? id = null) =>
        Render<AttemptSubmitted>(parameters => parameters.Add(page => page.Id, id ?? AttemptSamples.AttemptId));

    /// <summary>The number the question card is on, from its heading.</summary>
    protected static string Heading(IRenderedComponent<ExamSessionLayout> layout) =>
        layout.Find("#attempt-question-title").TextContent.Trim();

    /// <summary>One cell of the answer sheet by the number printed on it.</summary>
    protected static AngleSharp.Dom.IElement Cell(IRenderedComponent<ExamSessionLayout> layout, int number) =>
        layout.FindAll(".app-answer-cell").Single(cell => cell.TextContent.Trim() == number.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>One answer row by its position, in the answer area of the question card.</summary>
    protected static AngleSharp.Dom.IElement Option(IRenderedComponent<ExamSessionLayout> layout, int position) =>
        layout.Find($"#attempt-options-option-{position}");
}
