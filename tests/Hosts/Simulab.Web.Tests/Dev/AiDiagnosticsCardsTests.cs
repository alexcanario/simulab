using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Simulab.Web.Components.Pages.Dev;
using Simulab.Web.Services;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Dev;

/// <summary>
/// F-134 AC11 (BR7): the diagnostics page is two section cards, "Question" and, after an answer, "Answer" with the text and
/// a grid of model, tokens, cost and duration whose numbers use the mono font. A failed call shows the error alert and no
/// answer card. AC12 (the page answers not-found outside Development) stays in AiDiagnosticsPageTests and DevPagesHostTests.
/// </summary>
public sealed class AiDiagnosticsCardsTests : KitTestContext
{
    // F-183: the prompt is a text area with a counter, so it follows the typing (input) after the counter's pause.
    private void TypePrompt(IRenderedComponent<AiDiagnostics> page)
    {
        page.Find("#ai-prompt").Input("Say hello");
        AdvanceDebounce(250);
        page.WaitForAssertion(() => page.Find(".app-field-counter").TextContent.Trim().Should().StartWith("9/"));
    }

    private const string WebSessionId = "web-ai-cards";

    private AnswerOrFailure Api { get; } = new();

    public AiDiagnosticsCardsTests()
    {
        Services.AddSingleton(new AiApiClient(new HttpClient(Api) { BaseAddress = new Uri("https://api.test") }));
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Environments.Development));

        var store = new InMemoryWebSessionStore();
        store.SaveAsync(WebSessionId, new WebSession("jti-1", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddMinutes(10), [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)));
        Services.AddSingleton<FakeAuthApi>();
        Services.AddSingleton(provider => new WebSessionTokenAccessor(
            store,
            new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())),
            new SessionRefreshGate(),
            TimeProvider.System));

        Authorization.SetAuthorized("ana@exemplo.com")
            .SetClaims(new Claim(WebAuthClaims.WebSessionId, WebSessionId));
    }

    [Fact]
    public void BeforeACall_ShowsOnlyTheQuestionCard_WithThePromptAndTheSendButton()
    {
        var page = Render<AiDiagnostics>();

        page.Find("#ai-question-title").TextContent.Trim().Should().Be("Question");
        page.Find(".app-section-card #ai-prompt").Should().NotBeNull();
        page.FindAll("#ai-answer-title").Should().BeEmpty();
        page.FindAll(".app-section-card").Should().ContainSingle();
    }

    [Fact]
    public async Task AfterASuccessfulCall_ShowsTheAnswerCardWithTheTextAndTheMetricsInTheMonoFont()
    {
        Api.Answer = new AiDiagnosticsAnswer("Hello from the model", "claude-opus-5", 12, 40, 0.000123m, 850);

        var page = Render<AiDiagnostics>();
        TypePrompt(page);
        page.Find("button").Click();

        await page.WaitForAssertionAsync(() => page.Find("#ai-answer-title").TextContent.Trim().Should().Be("Answer"));

        page.Find("#ai-question-title").TextContent.Trim().Should().Be("Question");
        page.Markup.Should().Contain("Hello from the model");
        var values = page.FindAll(".ai-metric-value").Select(value => value.TextContent.Trim()).ToArray();
        values.Should().Equal("claude-opus-5", "12 / 40", "0.000123", "850 ms");
        page.FindAll(".ai-metric-label").Select(label => label.TextContent.Trim())
            .Should().Equal("Model", "Tokens in / out", "Cost (US$)", "Duration");
        AppCssColours.Declaration(".ai-metric-value", "font-family").Should().Contain("--app-font-mono");
    }

    [Fact]
    public async Task AfterAFailedCall_ShowsTheErrorAlertAndNoAnswerCard()
    {
        Api.Failure = (HttpStatusCode.UnprocessableEntity, "ai.quota_exceeded");

        var page = Render<AiDiagnostics>();
        TypePrompt(page);
        page.Find("button").Click();

        await page.WaitForAssertionAsync(() => page.Find(".app-alert").TextContent.Should().Contain("no more AI calls this month"));

        page.Find("#ai-question-title").TextContent.Trim().Should().Be("Question");
        page.FindAll("#ai-answer-title").Should().BeEmpty();
        page.FindAll(".ai-metric-value").Should().BeEmpty();
    }

    private sealed class AnswerOrFailure : HttpMessageHandler
    {
        public AiDiagnosticsAnswer? Answer { get; set; }

        public (HttpStatusCode Status, string Code)? Failure { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Failure is { } failure)
            {
                return Task.FromResult(new HttpResponseMessage(failure.Status)
                {
                    Content = new StringContent(
                        $$"""{"title":"{{failure.Code}}","status":{{(int)failure.Status}},"code":"{{failure.Code}}"}""",
                        Encoding.UTF8,
                        "application/problem+json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Answer, options: SharedKernel.Serialization.AppJson.Options)
            });
        }
    }
}
