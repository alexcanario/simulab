using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-168 AC10 and UC4: the card of each published edition on <c>/catalog/exams/{id}</c> shows its exam rules in one
/// line, with only the rules it has, and no line when it has none.
/// </summary>
public sealed class CatalogExamRulesLineTests : StudentCatalogTestContext
{
    private static PublishedExamDetailResponse Detail(params PublishedExamEditionResponse[] editions) =>
        new(
            GuardaMunicipal.Id,
            GuardaMunicipal.Name,
            GuardaMunicipal.IssuingAuthorityName,
            GuardaMunicipal.AssessmentType,
            GuardaMunicipal.Scope,
            GuardaMunicipal.ScopeDetail,
            GuardaMunicipal.ContentLanguage,
            editions.Length,
            editions.Select(edition => edition.NoticeYear).DefaultIfEmpty(0).Max(),
            editions);

    private IRenderedComponent<CatalogExam> RenderWith(params PublishedExamEditionResponse[] editions)
    {
        Api.Details[GuardaMunicipal.Id] = Detail(editions);
        var page = Render<CatalogExam>(parameters => parameters.Add(exam => exam.Id, GuardaMunicipal.Id));
        page.WaitForAssertion(() => page.FindAll("li.app-item-row").Should().HaveCount(editions.Length));

        return page;
    }

    [Fact]
    public void Render_AnEditionWithEveryRule_ShowsThemInOneLine()
    {
        var manaus = Edition2025 with
        {
            DurationMinutes = 300,
            ObjectiveMaxScore = 100m,
            ObjectiveCutOffScore = 60m,
            EssayMaxScore = 20m,
            EssayCutOffScore = 10m
        };

        var page = RenderWith(manaus);

        var lines = page.FindAll("li.app-item-row .app-edition-rules");
        lines.Should().ContainSingle();
        lines[0].TextContent.Should().Be("5 h · 100 points, pass at 60 · Essay 20 points, pass at 10");
    }

    [Fact]
    public void Render_AnEditionWithSomeRules_ShowsOnlyThoseItHas()
    {
        var partial = Edition2025 with { DurationMinutes = 270, ObjectiveCutOffScore = 60.5m };

        var page = RenderWith(partial);

        page.Find("li.app-item-row .app-edition-rules").TextContent.Should().Be("4 h 30 min · Pass at 60.5 points");
    }

    [Fact]
    public void Render_AnEditionWithNoRule_ShowsNoLine_AndTheOtherEditionKeepsItsOwn()
    {
        var withRules = Edition2025 with { ObjectiveMaxScore = 100m };

        var page = RenderWith(withRules, Edition2023);

        var rows = page.FindAll("li.app-item-row");
        rows[0].QuerySelector(".app-edition-rules")!.TextContent.Should().Be("100 points");
        rows[1].QuerySelectorAll(".app-edition-rules").Should().BeEmpty();
    }
}
