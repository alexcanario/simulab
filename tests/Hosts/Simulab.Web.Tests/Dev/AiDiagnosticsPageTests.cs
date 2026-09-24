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
/// F-41 UC2: the diagnostics page with a fake Api behind its typed client. The test says what the Api
/// answers and reads what the page shows and what it sent.
/// </summary>
public class AiDiagnosticsPageTests : KitTestContext
{
    private const string WebSessionId = "web-ai-1";

    private FakeAiApi Api { get; } = new();

    public AiDiagnosticsPageTests()
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
    public void Page_InDevelopment_ShowsThePromptAndTheSendButton()
    {
        var page = Render<AiDiagnostics>();

        page.Markup.Should().Contain("AI gateway");
        page.Find("#ai-prompt").Should().NotBeNull();
        page.FindAll("button").Should().Contain(button => button.TextContent.Contains("Send", StringComparison.Ordinal));
    }

    [Fact]
    public void Page_OutsideDevelopment_RendersNothing()
    {
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Environments.Production));
        using var context = new BunitContext();

        var page = Render<AiDiagnostics>();

        page.Markup.Should().NotContain("ai-prompt");
    }

    [Fact]
    public async Task Send_WhenTheApiAnswers_ShowsTheAnswerTheTokensAndTheCost()
    {
        Api.Answer = new AiDiagnosticsAnswer("Hello from the model", "claude-opus-5", 1000, 200, 0.01m, 1234);

        var page = Render<AiDiagnostics>();
        page.Find("#ai-prompt").Change("Say hello");
        page.Find("button").Click();

        await page.WaitForAssertionAsync(() =>
        {
            page.Markup.Should().Contain("Hello from the model");
            page.Markup.Should().Contain("claude-opus-5");
            page.Markup.Should().Contain("1000");
            page.Markup.Should().Contain("200");
            page.Markup.Should().Contain("1234");
        });

        Api.Received.Should().ContainSingle();
        Api.Received[0].Should().Contain("diagnostics").And.Contain("Say hello");
    }

    [Fact]
    public async Task Send_WhenTheGatewayRefuses_ShowsTheTextOfThatCodeAndNoAnswer()
    {
        Api.Failure = (HttpStatusCode.UnprocessableEntity, "ai.quota_exceeded");

        var page = Render<AiDiagnostics>();
        page.Find("#ai-prompt").Change("Say hello");
        page.Find("button").Click();

        await page.WaitForAssertionAsync(() =>
            page.Markup.Should().Contain("no more AI calls this month"));

        page.Markup.Should().NotContain("Tokens:");
    }

    /// <summary>Answers the diagnostics route with what the test set, and records what it received.</summary>
    private sealed class FakeAiApi : HttpMessageHandler
    {
        public AiDiagnosticsAnswer? Answer { get; set; }

        public (HttpStatusCode Status, string Code)? Failure { get; set; }

        public List<string> Received { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Received.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

            if (Failure is { } failure)
            {
                return new HttpResponseMessage(failure.Status)
                {
                    Content = new StringContent(
                        $$"""{"title":"{{failure.Code}}","status":{{(int)failure.Status}},"code":"{{failure.Code}}"}""",
                        Encoding.UTF8,
                        "application/problem+json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Answer, options: SharedKernel.Serialization.AppJson.Options)
            };
        }
    }
}
