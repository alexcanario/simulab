using Bunit;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// B-28 BR5: the question form's subject and topic lookups use the question bank's own routes only. The fake
/// Api answers every route of the catalog with 403, as it does for a curator who holds <c>questions.manage</c>
/// and no <c>catalog.manage</c>, so a form that reaches for the catalog cannot offer a candidate.
/// </summary>
public sealed class QuestionFormLookupTests : QuestionPageTestContext
{
    private const string CatalogPrefix = "/api/v1/catalog/";

    private AppLookupField Field(IRenderedComponent<QuestionForm> page, string id) =>
        page.FindComponents<AppLookupField>().Single(component => component.Instance.Id == id).Instance;

    // AC5
    [Fact]
    public async Task SubjectSearch_OffersWhatTheQuestionBankLookupReturned_AndCallsNoCatalogRoute()
    {
        var page = Render<QuestionForm>();

        var offered = await Field(page, "question-subject").SearchAsync("direito", CancellationToken.None);

        offered.Should().Equal(new AppLookupOption(Law.Id, Law.Name));
        Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Get
            && call.Path == "/api/v1/question-bank/subjects"
            && call.Query!.Contains("search=direito", StringComparison.Ordinal));
        Api.Received.Should().NotContain(call => call.Path.StartsWith(CatalogPrefix, StringComparison.Ordinal));
    }

    // AC5
    [Fact]
    public async Task TopicSearch_OffersTheTopicsTheQuestionBankLookupReturned_AndCallsNoCatalogRoute()
    {
        var page = Render<QuestionForm>();
        await page.InvokeAsync(() => Field(page, "question-subject").ValueChanged
            .InvokeAsync(new AppLookupOption(Portuguese.Id, Portuguese.Name)));

        var offered = await Field(page, "question-topic").SearchAsync(string.Empty, CancellationToken.None);

        offered.Select(option => option.Text).Should().BeEquivalentTo("Crase", "Pontuação");
        Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Get && call.Path == $"/api/v1/question-bank/subjects/{Portuguese.Id}/topics");
        Api.Received.Should().NotContain(call => call.Path.StartsWith(CatalogPrefix, StringComparison.Ordinal));
    }
}
