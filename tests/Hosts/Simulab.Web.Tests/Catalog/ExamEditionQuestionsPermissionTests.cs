using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Services;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-96 AC16 and BR12 on the edition page: the "Questions" section is drawn only for who holds
/// <c>questions.manage</c>, besides the <c>catalog.manage</c> the page itself needs.
/// </summary>
public sealed class ExamEditionQuestionsPermissionTests : CatalogPageTestContext
{
    private static readonly Guid SavedEditionId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000f9");

    private static readonly ExamEditionResponse Saved = new(
        SavedEditionId,
        AgentePf.Id,
        Cebraspe.Id,
        Cebraspe.Name,
        Cebraspe.Acronym,
        2026,
        null,
        null,
        null,
        null,
        ExamEditionStatus.Draft);

    private IRenderedComponent<ExamEditionForm> RenderEdit()
    {
        Api.Editions.Add(Saved);

        var page = Render<ExamEditionForm>(parameters => parameters
            .Add(form => form.ExamId, AgentePf.Id)
            .Add(form => form.Id, SavedEditionId));
        page.WaitForAssertion(() => page.FindAll("#edition-organizer").Should().ContainSingle());

        return page;
    }

    [Fact]
    public void WithOnlyCatalogManage_TheQuestionsSectionIsNotDrawn_AndTheNoticeSubjectsAre()
    {
        var page = RenderEdit();

        page.FindAll("#edition-section-questions").Should().BeEmpty();
        page.FindAll("#edition-section-questions-title").Should().BeEmpty();
        page.FindAll("#edition-section-notice-subjects-title").Should().ContainSingle();
    }

    [Fact]
    public void WithQuestionsManageToo_TheQuestionsSectionIsDrawnBelowTheNoticeSubjects()
    {
        Services.AddSingleton(new QuestionBankApiClient(
            new HttpClient(new EmptyQuestionBank()) { BaseAddress = new Uri("https://api.test") }));
        Authorization.SetPolicies(PermissionPolicy.Prefix + QuestionBankPermissions.Manage);

        var page = RenderEdit();

        page.WaitForAssertion(() => page.FindAll("#edition-section-questions-title").Should().ContainSingle());
        var markup = page.Markup;
        markup.IndexOf("edition-section-questions-title", StringComparison.Ordinal)
            .Should().BeGreaterThan(markup.IndexOf("edition-section-notice-subjects-title", StringComparison.Ordinal));
        page.WaitForAssertion(() => page.Markup.Should().Contain("No questions from this edition yet."));
    }

    /// <summary>Answers every question bank read with an empty page.</summary>
    private sealed class EmptyQuestionBank : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new EditionQuestionPageResponse([], 0), options: AppJson.Options)
            });
    }
}
