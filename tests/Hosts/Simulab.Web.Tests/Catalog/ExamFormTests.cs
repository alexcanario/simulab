using System.Net;
using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-34 `/admin/exams/new` and `/admin/exams/{id}`: the form page (AC4, AC5, AC6, AC12, AC12b, AC18b, AC18c).
/// </summary>
public sealed class ExamFormTests : CatalogPageTestContext
{
    private IRenderedComponent<ExamForm> RenderAdd() => Render<ExamForm>();

    private IRenderedComponent<ExamForm> RenderEdit(Guid id) =>
        Render<ExamForm>(parameters => parameters.Add(form => form.Id, id));

    private static SaveExamRequest SentBody(FakeCatalogApi api, HttpMethod method) =>
        FakeCatalogApi.Read<SaveExamRequest>(api.Received.Last(call => call.Method == method).Body);

    /// <summary>
    /// Sets a field by its id, whichever kit component holds it. F-43 turned the scope into
    /// <see cref="AppRadioCards{TValue}"/>; the tests below say what the form does with a value, not which
    /// control carries it, so the helper looks for both and their assertions are unchanged.
    /// </summary>
    private static void Set<T>(IRenderedComponent<ExamForm> page, string id, T value)
    {
        var selects = page.FindComponents<AppSelectField<T>>()
            .Where(component => component.Instance.Id == id)
            .Select(component => component.Instance.ValueChanged);
        var cards = page.FindComponents<AppRadioCards<T>>()
            .Where(component => component.Instance.Id == id)
            .Select(component => component.Instance.ValueChanged);

        var changed = selects.Concat(cards).Single();
        page.InvokeAsync(() => changed.InvokeAsync(value)).GetAwaiter().GetResult();
    }

    private static void PickAuthority(IRenderedComponent<ExamForm> page, IssuingAuthorityResponse authority)
    {
        var lookup = page.FindComponents<AppLookupField>().Single(component => component.Instance.Id == "exam-authority");
        page.InvokeAsync(() => lookup.Instance.ValueChanged.InvokeAsync(
            new AppLookupOption(authority.Id, $"{authority.Name} ({authority.Acronym})"))).GetAwaiter().GetResult();
    }

    // AC4: a national exam is sent without a detail, and the page says "Add exam" until it is saved.
    [Fact]
    public void Add_NationalExam_SendsItWithoutADetail()
    {
        var page = RenderAdd();
        page.Markup.Should().Contain("Add exam");

        PickAuthority(page, PoliciaFederal);
        page.Find("#exam-name").Change("Agente de Policia Federal");
        Set<AssessmentType?>(page, "exam-assessment-type", AssessmentType.PublicServiceExam);
        Set<ExamScope?>(page, "exam-scope", ExamScope.National);
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post));
        var sent = SentBody(Api, HttpMethod.Post);
        sent.IssuingAuthorityId.Should().Be(PoliciaFederal.Id);
        sent.Name.Should().Be("Agente de Policia Federal");
        sent.ScopeDetail.Should().BeNull();
        // BR9: the form offers pt-BR, the language of the catalog's first market, until the reader changes it.
        sent.ContentLanguage.Should().Be(ExamForm.DefaultContentLanguage).And.Be("pt-BR");
    }

    // AC18b: the conditional field appears for the two scopes that need it, and its label follows the scope.
    [Fact]
    public void Scope_Municipal_ShowsTheMunicipalityFieldAndStateShowsTheStateOne()
    {
        var page = RenderAdd();

        page.FindAll("#exam-scope-detail").Should().BeEmpty("a national exam has no detail");

        Set<ExamScope?>(page, "exam-scope", ExamScope.Municipal);
        page.WaitForAssertion(() => page.Markup.Should().Contain("Municipality"));
        page.FindAll("#exam-scope-detail").Should().ContainSingle();

        Set<ExamScope?>(page, "exam-scope", ExamScope.State);
        page.WaitForAssertion(() => page.Markup.Should().Contain("State"));
    }

    // AC18b: leaving those scopes drops what was typed, so nothing stale reaches the request.
    [Fact]
    public void Scope_BackToNational_DropsTheDetailAndSendsItNull()
    {
        var page = RenderAdd();
        PickAuthority(page, PoliciaFederal);
        page.Find("#exam-name").Change("Guarda Municipal");
        Set<AssessmentType?>(page, "exam-assessment-type", AssessmentType.PublicServiceExam);
        Set<ExamScope?>(page, "exam-scope", ExamScope.Municipal);
        page.Find("#exam-scope-detail").Change("Guarulhos (SP)");

        Set<ExamScope?>(page, "exam-scope", ExamScope.National);
        page.FindAll("#exam-scope-detail").Should().BeEmpty();
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post));
        SentBody(Api, HttpMethod.Post).ScopeDetail.Should().BeNull();
    }

    // AC5: the two scopes that need a place refuse to be saved without one, before the Api is called.
    [Fact]
    public void Save_MunicipalWithoutItsDetail_ShowsTheMessageAndCallsNothing()
    {
        var page = RenderAdd();
        PickAuthority(page, PoliciaFederal);
        page.Find("#exam-name").Change("Guarda Municipal");
        Set<AssessmentType?>(page, "exam-assessment-type", AssessmentType.PublicServiceExam);
        Set<ExamScope?>(page, "exam-scope", ExamScope.Municipal);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Say where this exam applies."));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    [Fact]
    public void Save_WithoutAnIssuingAuthority_ShowsTheMessageAndCallsNothing()
    {
        var page = RenderAdd();
        page.Find("#exam-name").Change("Agente");
        Set<AssessmentType?>(page, "exam-assessment-type", AssessmentType.PublicServiceExam);
        Set<ExamScope?>(page, "exam-scope", ExamScope.National);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Choose the issuing authority."));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    // AC12b: after adding, the page stays open as the edit form of the exam that was just saved.
    [Fact]
    public void Add_Saved_StaysOnThePageAsTheEditForm()
    {
        var providers = RenderProviders();
        var page = RenderAdd();
        PickAuthority(page, PoliciaFederal);
        page.Find("#exam-name").Change("Agente de Policia Federal");
        Set<AssessmentType?>(page, "exam-assessment-type", AssessmentType.PublicServiceExam);
        Set<ExamScope?>(page, "exam-scope", ExamScope.National);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Edit exam"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Exam saved."));
    }

    // AC12: the edit form opens filled and sends a PUT on that exam.
    [Fact]
    public void Edit_OpensFilledAndSendsAPutOnThatExam()
    {
        var page = RenderEdit(Fuvest.Id);

        page.WaitForAssertion(() => page.Markup.Should().Contain("Edit exam"));
        page.Find("#exam-name").GetAttribute("value").Should().Be("FUVEST");
        page.Find("#exam-scope-detail").GetAttribute("value").Should().Be("Sao Paulo", "the state scope carries its detail");

        page.Find("#exam-name").Change("FUVEST 2027");
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Put && call.Path == $"/api/v1/catalog/exams/{Fuvest.Id}"));
        SentBody(Api, HttpMethod.Put).Name.Should().Be("FUVEST 2027");
    }

    // AC6: a name the authority already has shows on the name field, not only at the top.
    [Fact]
    public void Save_NameTaken_ShowsTheMessageOnTheNameField()
    {
        Api.WriteFailure = (HttpStatusCode.Conflict, CatalogErrorCodes.ExamNameTaken);
        var page = RenderAdd();
        PickAuthority(page, PoliciaFederal);
        page.Find("#exam-name").Change("Agente de Policia Federal");
        Set<AssessmentType?>(page, "exam-assessment-type", AssessmentType.PublicServiceExam);
        Set<ExamScope?>(page, "exam-scope", ExamScope.National);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("This issuing authority already has an exam with this name."));
        page.Find("#exam-name").GetAttribute("aria-invalid").Should().Be("true");
    }

    // The organizer left the catalog between the pick and the save: the field is cleared and says so.
    [Fact]
    public void Save_IssuingAuthorityGone_ClearsThePickerAndAsksForAnother()
    {
        Api.WriteFailure = (HttpStatusCode.NotFound, CatalogErrorCodes.IssuingAuthorityNotFound);
        var page = RenderAdd();
        PickAuthority(page, PoliciaFederal);
        page.Find("#exam-name").Change("Agente");
        Set<AssessmentType?>(page, "exam-assessment-type", AssessmentType.PublicServiceExam);
        Set<ExamScope?>(page, "exam-scope", ExamScope.National);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Choose the issuing authority."));
        page.Markup.Should().Contain("This issuing authority no longer exists. Refresh the list.");
    }

    // AC18c: an id that is not an exam shows the message and a way back, and no fields.
    [Fact]
    public void Edit_AnIdThatIsNotAnExam_ShowsNotFoundAndALinkBack()
    {
        Api.FindExamFailure = (HttpStatusCode.NotFound, CatalogErrorCodes.ExamNotFound);

        var page = RenderEdit(Guid.CreateVersion7());

        page.WaitForAssertion(() => page.Markup.Should().Contain("This exam no longer exists."));
        page.Markup.Should().Contain("Back to the exams");
        page.FindAll("#exam-name").Should().BeEmpty();
    }

    // AC18 (v2): the picker asks for the bodies that publish notices, never for the boards.
    [Fact]
    public void Authority_Typing_AsksTheIssuingAuthorityListWithTheTerm()
    {
        var page = RenderAdd();

        page.Find("#exam-authority").Input("gua");

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Path == "/api/v1/catalog/issuing-authorities"
            && call.Query!.Contains("search=gua", StringComparison.Ordinal)
            && call.Query.Contains($"pageSize={AppLookupField.MaxCandidates}", StringComparison.Ordinal)));
        Api.Received.Should().NotContain(call => call.Path == "/api/v1/catalog/organizers");
    }

    // BR9: the content language is offered in its own name, so it does not depend on the UI language.
    [Fact]
    public void ContentLanguage_IsOfferedInItsOwnName()
    {
        var page = RenderAdd();

        var field = page.FindComponents<AppSelectField<string>>().Single(component => component.Instance.Id == "exam-content-language");
        field.Instance.Options.Select(option => option.Text).Should().Contain("Português (Brasil)");
        field.Instance.Options.Select(option => option.Value).Should().Contain("pt-BR").And.Contain("pt-PT").And.Contain("en");
    }

    // F-43, AC3, BR1: the fields sit inside titled sections instead of on one plain column.
    [Fact]
    public void Render_TheFieldsAreGroupedInsideTitledSectionCards()
    {
        var page = RenderAdd();

        var sections = page.FindComponents<AppSectionCard>();
        sections.Select(section => section.Instance.Title).Should().Equal(
            "Identification", "Classification", "Where the exam applies");

        var identification = page.Find("section[aria-labelledby='exam-section-identification-title']");
        identification.QuerySelectorAll("#exam-authority").Should().ContainSingle();
        identification.QuerySelectorAll("#exam-name").Should().ContainSingle();

        var classification = page.Find("section[aria-labelledby='exam-section-classification-title']");
        classification.QuerySelectorAll("#exam-assessment-type").Should().ContainSingle();
        classification.QuerySelectorAll("#exam-content-language").Should().ContainSingle();

        var scope = page.Find("section[aria-labelledby='exam-section-scope-title']");
        scope.QuerySelectorAll("#exam-scope").Should().ContainSingle();
    }

    // F-43, AC5, UC3: one panel at the top lists every missing field, besides the message beside each field.
    [Fact]
    public void Save_AnIncompleteForm_ListsEveryMissingFieldAtTheTopOfTheCard()
    {
        var page = RenderAdd();

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
        {
            var summary = page.Find("#exam-error-summary");
            summary.GetAttribute("role").Should().Be("alert");
            var links = summary.QuerySelectorAll(".app-error-summary-list a");
            links.Select(link => link.GetAttribute("href")).Should().Equal(
                "#exam-authority", "#exam-name", "#exam-assessment-type", "#exam-scope");
        });
        // The field beside it still carries its own message (BR1, UC3).
        page.Markup.Should().Contain("Choose the issuing authority.");
    }

    // F-43, AC5: fixing a field on the form takes its line out of the summary without another save.
    [Fact]
    public void Save_ThenFixOneField_RemovesItFromTheSummary()
    {
        var page = RenderAdd();
        page.Find(".app-form-save").Click();
        page.WaitForAssertion(() => page.Find("#exam-error-summary").QuerySelectorAll(".app-error-summary-list a")
            .Select(link => link.GetAttribute("href")).Should().Contain("#exam-name"));

        page.Find("#exam-name").Change("Agente de Policia Federal");

        page.WaitForAssertion(() => page.Find("#exam-error-summary").QuerySelectorAll(".app-error-summary-list a")
            .Select(link => link.GetAttribute("href")).Should().NotContain("#exam-name"));
    }

    // F-43, AC7, BR4: the read-only aside shows what is filled and what is missing, and carries no button.
    [Fact]
    public void Render_TheAsideShowsWhatIsFilledAndCarriesNoButton()
    {
        var page = RenderAdd();

        var aside = page.Find(".app-form-aside");
        aside.QuerySelectorAll("button").Should().BeEmpty("the aside is read-only (BR4)");
        aside.TextContent.Should().Contain("Not filled");

        PickAuthority(page, PoliciaFederal);
        page.Find("#exam-name").Change("Agente de Policia Federal");

        page.WaitForAssertion(() => page.Find(".app-form-aside").TextContent.Should().Contain("Policia Federal"));
        page.Find(".app-form-aside").TextContent.Should().Contain("Agente de Policia Federal");
    }

    // F-43: the state/municipality summary in the aside follows the scope that is picked.
    [Fact]
    public void Aside_ScopeMunicipal_ShowsTheScopeAndItsPlaceTogether()
    {
        var page = RenderAdd();
        Set<ExamScope?>(page, "exam-scope", ExamScope.Municipal);
        page.Find("#exam-scope-detail").Change("Fortaleza/CE");

        page.WaitForAssertion(() => page.Find(".app-form-aside").TextContent.Should().Contain("Fortaleza/CE"));
    }
}
