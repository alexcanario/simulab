using Bunit;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-109 `/admin/questions/new` and `/admin/questions/{id}`: the seventh and eighth type cards, the rubric section that
/// replaces the options (one empty row, up to ten, total of the points), the size fields of each type, what the form
/// sends, the checklist, the "Model answer" label, the refusals under the fields, the key-change reason and the frozen
/// state (AC14, AC15, AC19 as far as bUnit reaches).
/// </summary>
public sealed class RubricQuestionFormTests : QuestionPageTestContext
{
    private const string ThemeStatement = "<p>Discuta o tema proposto.</p>";

    private static readonly QuestionResponse EssayDraft = new(
        Guid.Parse("0198f5a0-0000-7000-8000-000000000210"),
        QuestionType.Essay,
        QuestionStatus.Draft,
        Crase.Id,
        Crase.Name,
        Portuguese.Id,
        Portuguese.Name,
        "pt-BR",
        ThemeStatement,
        null,
        null,
        3,
        [],
        Rubric: [new(1, "Theme", "Meets the prompt", 30), new(2, "Argument", null, 40)],
        AnswerSizeMin: 7,
        AnswerSizeMax: 30);

    private IRenderedComponent<QuestionForm> RenderAdd() => Render<QuestionForm>();

    private IRenderedComponent<QuestionForm> RenderEdit(Guid id) =>
        Render<QuestionForm>(parameters => parameters.Add(form => form.Id, id));

    private void WaitForEditors(IRenderedComponent<QuestionForm> page, params string[] ids) =>
        page.WaitForAssertion(() => RichText.Editors.Keys.Should().Contain(ids));

    private Task TypeAsync(IRenderedComponent<QuestionForm> page, string id, string html) =>
        page.InvokeAsync(() => RichText.TypeAsync(id, html));

    private static Task PickAsync(IRenderedComponent<QuestionForm> page, string id, AppLookupOption? option) =>
        page.InvokeAsync(() => page.FindComponents<AppLookupField>()
            .Single(component => component.Instance.Id == id).Instance.ValueChanged.InvokeAsync(option));

    private static AppTextField<string> NameField(IRenderedComponent<QuestionForm> page, int criterion) =>
        page.FindComponents<AppTextField<string>>().Single(component => component.Instance.Id == $"question-criterion-{criterion}-name").Instance;

    private static AppTextField<string> DescriptionField(IRenderedComponent<QuestionForm> page, int criterion) =>
        page.FindComponents<AppTextField<string>>().Single(component => component.Instance.Id == $"question-criterion-{criterion}-description").Instance;

    private static AppTextField<int?> PointsField(IRenderedComponent<QuestionForm> page, int criterion) =>
        page.FindComponents<AppTextField<int?>>().Single(component => component.Instance.Id == $"question-criterion-{criterion}-points").Instance;

    private static AppTextField<int?> SizeField(IRenderedComponent<QuestionForm> page, string id) =>
        page.FindComponents<AppTextField<int?>>().Single(component => component.Instance.Id == id).Instance;

    private static Task NameAsync(IRenderedComponent<QuestionForm> page, int criterion, string text) =>
        page.InvokeAsync(() => NameField(page, criterion).ValueChanged.InvokeAsync(text));

    private static Task PointsAsync(IRenderedComponent<QuestionForm> page, int criterion, int? points) =>
        page.InvokeAsync(() => PointsField(page, criterion).ValueChanged.InvokeAsync(points));

    private static Task SizeAsync(IRenderedComponent<QuestionForm> page, string id, int? value) =>
        page.InvokeAsync(() => SizeField(page, id).ValueChanged.InvokeAsync(value));

    private static int CriterionRows(IRenderedComponent<QuestionForm> page) =>
        page.FindComponents<AppTextField<string>>().Count(component => component.Instance.Id is { } id
            && id.StartsWith("question-criterion-", StringComparison.Ordinal) && id.EndsWith("-name", StringComparison.Ordinal));

    private static IReadOnlyList<string> ChecklistTexts(IRenderedComponent<QuestionForm> page) =>
        [.. page.FindAll(".app-form-aside-checklist li").Select(item => item.TextContent)];

    private async Task StartAsync(IRenderedComponent<QuestionForm> page, QuestionType type, string statement = ThemeStatement)
    {
        page.Find($"#question-type-{type}").Click();
        await PickAsync(page, "question-subject", new AppLookupOption(Portuguese.Id, Portuguese.Name));
        await PickAsync(page, "question-topic", new AppLookupOption(Crase.Id, Crase.Name));
        WaitForEditors(page, "question-statement");
        await TypeAsync(page, "question-statement", statement);
    }

    // AC14: eight cards, single choice still preselected.
    [Fact]
    public void Add_OffersTheSeventhAndEighthTypeCards_WithSingleChoiceStillPreselected()
    {
        var page = RenderAdd();

        page.Find("#question-type-SingleChoice").GetAttribute("aria-checked").Should().Be("true");
        page.Find("#question-type-OpenAnswer").GetAttribute("aria-checked").Should().Be("false");
        page.Find("#question-type-OpenAnswer").TextContent.Should().Contain("Open answer")
            .And.Contain("A written answer graded by a person against a rubric.");
        page.Find("#question-type-Essay").TextContent.Should().Contain("Essay")
            .And.Contain("A long written text graded by a person against a rubric.");
        page.Find("#question-type").QuerySelectorAll("[role='radio']").Should().HaveCount(8);
    }

    // AC14, BR12: the rubric section replaces the options, starts with one empty row and the essay size fields.
    [Fact]
    public async Task Add_ChoosingEssay_ShowsTheRubricWithOneEmptyRow_AndTheLineFields()
    {
        var page = RenderAdd();
        await StartAsync(page, QuestionType.Essay);

        page.WaitForAssertion(() => page.FindAll("#question-section-rubric-title").Should().ContainSingle());
        page.FindAll("#question-section-options-title").Should().BeEmpty();
        page.Find("#question-section-rubric").TextContent.Should().Contain("Rubric")
            .And.Contain("Write the criteria a grader scores. The question's grade is the points given divided by the total.")
            .And.Contain("1 of 10 criteria")
            .And.Contain("Total: 0 points");
        CriterionRows(page).Should().Be(1);
        NameField(page, 0).Value.Should().BeNullOrEmpty();
        NameField(page, 0).Label.Should().Be("Criterion 1 name");
        DescriptionField(page, 0).Label.Should().Be("Criterion 1 description");
        PointsField(page, 0).Label.Should().Be("Criterion 1 points");
        SizeField(page, "question-answer-size-min").Label.Should().Be("Minimum lines");
        SizeField(page, "question-answer-size-max").Label.Should().Be("Maximum lines");
    }

    [Fact]
    public async Task Add_ChoosingOpenAnswer_ShowsOnlyAMaximumOfCharacters()
    {
        var page = RenderAdd();
        await StartAsync(page, QuestionType.OpenAnswer);

        page.WaitForAssertion(() => page.FindAll("#question-section-rubric-title").Should().ContainSingle());
        SizeField(page, "question-answer-size-max").Label.Should().Be("Maximum characters");
        page.FindComponents<AppTextField<int?>>().Any(field => field.Instance.Id == "question-answer-size-min").Should().BeFalse();
    }

    // AC14: "Add criterion" goes up to 10 rows, the total follows, and a removal drops one.
    [Fact]
    public async Task Add_Rubric_AddCriterionGoesUpToTen_TheTotalFollowsThePoints_AndARemovalDropsOne()
    {
        var page = RenderAdd();
        await StartAsync(page, QuestionType.Essay);
        page.WaitForAssertion(() => CriterionRows(page).Should().Be(1));
        await PointsAsync(page, 0, 30);

        for (var index = 0; index < QuestionLimits.RubricCriteriaMax - 1; index++)
        {
            page.Find("#question-criterion-add").Click();
        }

        page.WaitForAssertion(() => CriterionRows(page).Should().Be(QuestionLimits.RubricCriteriaMax));
        page.Find("#question-criterion-add").HasAttribute("disabled").Should().BeTrue();
        page.Find("#question-section-rubric").TextContent.Should().Contain("10 of 10 criteria").And.Contain("Total: 30 points");
        NameField(page, 9).Label.Should().Be("Criterion 10 name");
        await PointsAsync(page, 9, 20);
        page.WaitForAssertion(() => page.Find("#question-section-rubric").TextContent.Should().Contain("Total: 50 points"));

        page.Find("#question-criterion-9-actions-delete").Click();

        page.WaitForAssertion(() => CriterionRows(page).Should().Be(9));
        page.Find("#question-criterion-add").HasAttribute("disabled").Should().BeFalse();
        page.Find("#question-section-rubric").TextContent.Should().Contain("Total: 30 points");
    }

    // AC14, BR3: the form sends the type, the criteria in order and the size, nothing of the other types.
    [Fact]
    public async Task Add_Essay_SavesTheTypeTheRubricInOrderAndTheLineLimit()
    {
        var page = RenderAdd();
        await StartAsync(page, QuestionType.Essay);
        page.WaitForAssertion(() => CriterionRows(page).Should().Be(1));
        page.Find("#question-criterion-add").Click();
        await NameAsync(page, 0, "Theme");
        await page.InvokeAsync(() => DescriptionField(page, 0).ValueChanged.InvokeAsync("Meets the prompt"));
        await PointsAsync(page, 0, 30);
        await NameAsync(page, 1, "Argument");
        await PointsAsync(page, 1, 70);
        await SizeAsync(page, "question-answer-size-min", 7);
        await SizeAsync(page, "question-answer-size-max", 30);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post
            && call.Path == "/api/v1/question-bank/questions"));
        var sent = Api.LastSaved(HttpMethod.Post);
        sent.Type.Should().Be(QuestionType.Essay);
        sent.Options.Should().BeNullOrEmpty();
        sent.TrueFalseKey.Should().BeNull();
        sent.MatchItems.Should().BeNull();
        sent.Blanks.Should().BeNull();
        sent.Rubric!.Select(criterion => (criterion.Name, criterion.Description, criterion.MaxPoints))
            .Should().Equal(("Theme", (string?)"Meets the prompt", (int?)30), ("Argument", null, 70));
        (sent.AnswerSizeMin, sent.AnswerSizeMax).Should().Be((7, 30));
    }

    [Fact]
    public async Task Add_OpenAnswer_SavesTheCharacterLimitAndNoMinimum()
    {
        var page = RenderAdd();
        await StartAsync(page, QuestionType.OpenAnswer);
        page.WaitForAssertion(() => CriterionRows(page).Should().Be(1));
        await NameAsync(page, 0, "Accuracy");
        await PointsAsync(page, 0, 10);
        await SizeAsync(page, "question-answer-size-max", 500);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post));
        var sent = Api.LastSaved(HttpMethod.Post);
        sent.Type.Should().Be(QuestionType.OpenAnswer);
        sent.Rubric.Should().ContainSingle();
        (sent.AnswerSizeMin, sent.AnswerSizeMax).Should().Be((null, 500));
    }

    // AC14: picking another type and back keeps the typed rows.
    [Fact]
    public async Task Add_GoingToAnotherTypeAndBack_KeepsTheTypedRows()
    {
        var page = RenderAdd();
        await StartAsync(page, QuestionType.Essay);
        page.WaitForAssertion(() => CriterionRows(page).Should().Be(1));
        await NameAsync(page, 0, "Theme");
        await PointsAsync(page, 0, 30);

        page.Find("#question-type-ShortAnswer").Click();
        page.WaitForAssertion(() => page.FindAll("#question-section-rubric-title").Should().BeEmpty());
        page.Find("#question-type-Essay").Click();

        page.WaitForAssertion(() => NameField(page, 0).Value.Should().Be("Theme"));
        PointsField(page, 0).Value.Should().Be(30);
    }

    // AC14: on an existing question the type is read only and the stored rubric and size fill the fields.
    [Fact]
    public void Edit_Essay_ShowsTheTypeAsAReadOnlyValue_TheStoredRubricAndTheSize()
    {
        Api.Questions.Add(EssayDraft);

        var page = RenderEdit(EssayDraft.Id);

        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());
        page.Find("#question-type-value").TextContent.Should().Contain("Essay");
        page.WaitForAssertion(() => CriterionRows(page).Should().Be(2));
        NameField(page, 0).Value.Should().Be("Theme");
        DescriptionField(page, 0).Value.Should().Be("Meets the prompt");
        PointsField(page, 1).Value.Should().Be(40);
        SizeField(page, "question-answer-size-min").Value.Should().Be(7);
        SizeField(page, "question-answer-size-max").Value.Should().Be(30);
        page.Find("#question-section-rubric").TextContent.Should().Contain("Total: 70 points");
    }

    // AC15, BR9: the explanation is the model answer for these types.
    [Fact]
    public void Edit_Essay_LabelsTheExplanationAsTheModelAnswer_WithItsHint()
    {
        Api.Questions.Add(EssayDraft);

        var page = RenderEdit(EssayDraft.Id);

        page.WaitForAssertion(() => page.Markup.Should().Contain("Model answer")
            .And.Contain("The grader always sees it; a student sees it after answering."));
    }

    // AC15: the publish checklist has one item for these types.
    [Fact]
    public async Task Edit_Essay_ChecklistIsDoneOnlyWithACriterionNamedWithItsPoints()
    {
        Api.Questions.Add(EssayDraft with { Rubric = [new(1, "Theme", null, 30)] });
        var page = RenderEdit(EssayDraft.Id);
        page.WaitForAssertion(() => CriterionRows(page).Should().Be(1));

        ChecklistTexts(page).Should().ContainSingle(text => text.Contains("At least one rubric criterion"));
        page.FindAll(".app-form-aside-checklist li")
            .Single(item => item.TextContent.Contains("At least one rubric criterion"))
            .ClassList.Should().NotContain("app-checklist-pending");

        await PointsAsync(page, 0, null);

        page.WaitForAssertion(() => page.FindAll(".app-form-aside-checklist li")
            .Single(item => item.TextContent.Contains("At least one rubric criterion"))
            .ClassList.Should().Contain("app-checklist-pending"));
    }

    // AC14, AC19: a refusal about a criterion shows its text, announced, under the field of that criterion.
    [Theory]
    [InlineData(QuestionBankErrorCodes.QuestionCriterionNameRequired, "Write a name for the criterion.", "name")]
    [InlineData(QuestionBankErrorCodes.QuestionCriterionNameTooLong, "A criterion name has at most 100 characters.", "name")]
    [InlineData(QuestionBankErrorCodes.QuestionCriterionNameRepeated, "Two criteria have the same name.", "name")]
    [InlineData(QuestionBankErrorCodes.QuestionCriterionDescriptionTooLong, "A criterion description has at most 500 characters.", "description")]
    [InlineData(QuestionBankErrorCodes.QuestionCriterionPointsOutOfRange, "The points of a criterion are a whole number from 1 to 100.", "points")]
    public async Task Edit_Essay_WhenTheApiRefusesACriterion_TheTextIsUnderThatField(string code, string text, string field)
    {
        Api.Questions.Add(EssayDraft);
        Api.SaveFailure = (System.Net.HttpStatusCode.BadRequest, code, 2);
        var page = RenderEdit(EssayDraft.Id);
        page.WaitForAssertion(() => CriterionRows(page).Should().Be(2));
        await TypeAsync(page, "question-statement", "<p>Outro tema.</p>");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
        {
            var error = page.Find($"#question-criterion-1-{field}-description .app-field-error-text");
            error.TextContent.Trim().Should().Be(text);
        });
        page.FindAll("#question-criterion-0-name-description .app-field-error-text").Should().BeEmpty("only the criterion named by the position shows it");
    }

    // AC14: a refusal about the rubric as a whole or the size is under the section, announced.
    [Theory]
    [InlineData(QuestionBankErrorCodes.QuestionRubricCriteriaCount, "A rubric has at most 10 criteria.")]
    [InlineData(QuestionBankErrorCodes.QuestionRubricCriteriaRequired, "Write at least one criterion to publish.")]
    [InlineData(QuestionBankErrorCodes.QuestionAnswerSizeInvalid, "The size limit is out of range, or the minimum is above the maximum.")]
    public async Task Edit_Essay_WhenTheApiRefusesTheRubricOrTheSize_TheTextIsUnderTheSection(string code, string text)
    {
        Api.Questions.Add(EssayDraft);
        Api.SaveFailure = (System.Net.HttpStatusCode.BadRequest, code, null);
        var page = RenderEdit(EssayDraft.Id);
        page.WaitForAssertion(() => CriterionRows(page).Should().Be(2));
        await TypeAsync(page, "question-statement", "<p>Outro tema.</p>");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
        {
            var error = page.Find("#question-section-rubric .app-rubric-error");
            error.TextContent.Trim().Should().Be(text);
            error.GetAttribute("aria-live").Should().Be("polite");
        });
    }

    // AC14, UC7: a changed point on a published question asks for the reason; only a description does not.
    [Fact]
    public async Task Edit_PublishedEssay_ChangingThePoints_AsksForTheReason()
    {
        Api.Questions.Add(EssayDraft with { Status = QuestionStatus.Published });
        var page = RenderEdit(EssayDraft.Id);
        page.WaitForAssertion(() => CriterionRows(page).Should().Be(2));
        page.FindAll("#question-change-reason").Should().BeEmpty();

        await PointsAsync(page, 1, 45);

        page.WaitForElement("#question-change-reason");
    }

    [Fact]
    public async Task Edit_PublishedEssay_ChangingOnlyADescription_DoesNotAskForTheReason()
    {
        Api.Questions.Add(EssayDraft with { Status = QuestionStatus.Published });
        var page = RenderEdit(EssayDraft.Id);
        page.WaitForAssertion(() => CriterionRows(page).Should().Be(2));

        await page.InvokeAsync(() => DescriptionField(page, 0).ValueChanged.InvokeAsync("Now described"));

        page.WaitForAssertion(() => page.Find(".app-form-save").HasAttribute("disabled").Should().BeFalse());
        page.FindAll("#question-change-reason").Should().BeEmpty();
    }

    // F-101 BR5, F-105 BR3: a frozen question shows everything disabled.
    [Fact]
    public void Edit_AnnulledEssay_ShowsTheRubricDisabled_AndNoAddButton()
    {
        Api.Questions.Add(EssayDraft with { Status = QuestionStatus.Annulled });

        var page = RenderEdit(EssayDraft.Id);

        page.WaitForAssertion(() => CriterionRows(page).Should().Be(2));
        NameField(page, 0).Disabled.Should().BeTrue();
        PointsField(page, 0).Disabled.Should().BeTrue();
        SizeField(page, "question-answer-size-max").Disabled.Should().BeTrue();
        page.FindAll("#question-criterion-add").Should().BeEmpty();
    }
}
