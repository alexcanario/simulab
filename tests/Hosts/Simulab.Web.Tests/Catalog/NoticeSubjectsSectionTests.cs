using System.Net;
using System.Text.RegularExpressions;
using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-74 Screen 1 and Screen 2: the notice subjects section on the edition page and its add/edit dialog (AC1, AC2, AC17,
/// AC18, AC19 and the states of the item). The Api runs every rule (BR2 to BR9); these tests say what the screen shows,
/// what it sends and what it does with a refusal.
/// </summary>
public sealed class NoticeSubjectsSectionTests : CatalogPageTestContext
{
    private static readonly Guid EditionId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000e1");

    private const string Basic = "Conhecimentos Básicos";
    private const string Specific = "Conhecimentos Específicos";

    private NoticeSubjectResponse Add(string? group, string label, int? questions)
    {
        var row = new NoticeSubjectResponse(Guid.CreateVersion7(), EditionId, group, label, questions);
        Api.NoticeSubjects.Add(row);
        return row;
    }

    private IRenderedComponent<NoticeSubjectsSection> RenderSection(bool saved = true)
    {
        Guid? editionId = saved ? EditionId : null;

        return Render<NoticeSubjectsSection>(parameters => parameters
            .Add(section => section.ExamId, AgentePf.Id)
            .Add(section => section.EditionId, editionId));
    }

    private static string Flat(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    private static IReadOnlyList<string> Labels(IRenderedComponent<NoticeSubjectsSection> section) =>
        [.. section.FindAll(".app-item-row .app-truncate").Select(label => Flat(label.TextContent))];

    private static IReadOnlyList<string> Headings(IRenderedComponent<NoticeSubjectsSection> section) =>
        [.. section.FindAll("h3.app-notice-subjects-group-title").Select(heading => Flat(heading.TextContent))];

    private static IReadOnlyList<string> Counts(IRenderedComponent<NoticeSubjectsSection> section) =>
        [.. section.FindAll(".app-notice-subject-count").Select(count => Flat(count.TextContent))];

    private static string ButtonOf(NoticeSubjectResponse row, string key) => $"#notice-subject-actions-{row.Id}-{key}";

    private static void WaitForRows(IRenderedComponent<NoticeSubjectsSection> section, int count) =>
        section.WaitForAssertion(() => section.FindAll(".app-item-row").Should().HaveCount(count));

    private static string Announcement(IRenderedComponent<NoticeSubjectsSection> section) =>
        Flat(section.Find("div.app-visually-hidden[aria-live='polite']").TextContent);

    private static SaveNoticeSubjectRequest SentBody(FakeCatalogApi api, HttpMethod method) =>
        FakeCatalogApi.Read<SaveNoticeSubjectRequest>(api.Received.Last(call => call.Method == method).Body);

    private static void SetGroup(IRenderedComponent<MudBlazor.MudDialogProvider> dialogs, string? text) =>
        dialogs.InvokeAsync(() => dialogs.FindComponent<AppSuggestField>().Instance.ValueChanged.InvokeAsync(text))
            .GetAwaiter().GetResult();

    private static void WaitForDialog(IRenderedComponent<MudBlazor.MudDialogProvider> dialogs) =>
        dialogs.WaitForAssertion(() => dialogs.FindAll("#notice-subject-label").Should().ContainSingle());

    // AC1: two groups, in the order of their first row, each row with its number or the dash.
    [Fact]
    public void Section_TwoGroups_ListsThemInOrderWithEachRowsNumberOrTheDash()
    {
        Add(Basic, "Língua Portuguesa", 15);
        Add(Basic, "Raciocínio Lógico", null);
        Add(Specific, "Direito Penal", 1);

        var section = RenderSection();

        WaitForRows(section, 3);
        Headings(section).Should().Equal(Basic, Specific);
        Labels(section).Should().Equal("Língua Portuguesa", "Raciocínio Lógico", "Direito Penal");
        Counts(section).Should().Equal("15 questions", "—number of questions not stated", "1 question");
        section.FindAll(".app-notice-subject-count [aria-hidden='true']").Should().ContainSingle("the dash is hidden from a screen reader");
        section.FindAll(".app-notice-subject-count .app-visually-hidden").Should().ContainSingle("and its words are read instead");
        section.Find("#edition-section-notice-subjects-title").TextContent.Should().Be("Notice subjects");
    }

    // The list is labelled by its heading: a screen reader says "Conhecimentos Básicos, list, 2 items".
    [Fact]
    public void Section_EachGroupsListIsLabelledByItsHeading()
    {
        Add(Basic, "Língua Portuguesa", 15);
        Add(Specific, "Direito Penal", 10);

        var section = RenderSection();

        WaitForRows(section, 2);
        var lists = section.FindAll("ul.app-item-rows-list");
        lists.Should().HaveCount(2);
        for (var index = 0; index < lists.Count; index++)
        {
            var headingId = lists[index].GetAttribute("aria-labelledby");
            headingId.Should().NotBeNullOrEmpty();
            section.Find($"#{headingId}").TextContent.Trim().Should().Be(index == 0 ? Basic : Specific);
            section.Find($"#{headingId}").TagName.Should().Be("H3");
        }
    }

    // BR6 and the Decisions: one heading per run of rows with the same group, case and accents ignored, first text wins.
    [Fact]
    public void Section_RowsOfOneGroupSpelledTwoWays_ShareOneHeadingWithTheFirstSpelling()
    {
        Add("Conhecimentos Básicos", "Língua Portuguesa", 5);
        Add("conhecimentos basicos", "Informática", 5);
        Add(Specific, "Direito Penal", 5);

        var section = RenderSection();

        WaitForRows(section, 3);
        Headings(section).Should().Equal(Basic, Specific);
    }

    [Fact]
    public void Section_NoRowHasAGroup_DrawsOneListAndNoHeading()
    {
        Add(null, "Língua Portuguesa", 20);
        Add(null, "Teoria Musical", 20);

        var section = RenderSection();

        WaitForRows(section, 2);
        Headings(section).Should().BeEmpty("a 'No group' title over the whole list says nothing");
        section.FindAll("ul.app-item-rows-list").Should().ContainSingle();
        section.Markup.Should().NotContain("No group");
    }

    [Fact]
    public void Section_MixedGroups_DrawsNoGroupWhereItsFirstRowFalls()
    {
        Add(Basic, "Língua Portuguesa", 15);
        Add(null, "Ética no Serviço Público", 5);
        Add(Specific, "Direito Penal", 10);

        var section = RenderSection();

        WaitForRows(section, 3);
        Headings(section).Should().Equal(Basic, "No group", Specific);
    }

    // AC2: 10, 20 and a row with no number: the footer says 30 and that one row has no number.
    [Fact]
    public void Footer_TenTwentyAndOneWithoutNumber_ShowsThirtyAndTheUnstatedRow()
    {
        Add(Basic, "Língua Portuguesa", 10);
        Add(Basic, "Informática", 20);
        Add(Basic, "Conhecimentos Gerais", null);

        var section = RenderSection();

        WaitForRows(section, 3);
        var total = Flat(section.Find(".app-notice-subjects-total").TextContent);
        total.Should().Contain("Stated in the notice: 30 questions in total");
        total.Should().Contain("1 subject has no number and is left out of the sum.");
    }

    [Fact]
    public void Footer_SeveralRowsWithoutNumber_SaysHowManyAreLeftOut()
    {
        Add(Basic, "Língua Portuguesa", 1);
        Add(Basic, "Informática", null);
        Add(Basic, "Conhecimentos Gerais", null);

        var section = RenderSection();

        WaitForRows(section, 3);
        var total = Flat(section.Find(".app-notice-subjects-total").TextContent);
        total.Should().Contain("Stated in the notice: 1 question in total");
        total.Should().Contain("2 subjects have no number and are left out of the sum.");
    }

    [Fact]
    public void Footer_NoRowStatesANumber_SaysSoInsteadOfASum()
    {
        Add(null, "Teoria Musical", null);
        Add(null, "História da Música", null);

        var section = RenderSection();

        WaitForRows(section, 2);
        Flat(section.Find(".app-notice-subjects-total").TextContent).Should().Be("No subject states its number of questions.");
    }

    [Fact]
    public void Section_NoRows_ShowsTheEmptyMessageAndTheAddButtonAndNoSum()
    {
        var section = RenderSection();

        section.WaitForAssertion(() => section.Markup.Should().Contain("This edition has no notice subject yet."));
        var add = section.Find("#notice-subject-add");
        add.TagName.Should().Be("BUTTON", "adding opens a dialog and does not navigate");
        add.TextContent.Should().Contain("Add notice subject");
        section.FindAll(".app-notice-subjects-total").Should().BeEmpty();
        section.FindAll("a[href]").Should().BeEmpty("no link on this screen navigates");
    }

    // AC17: an edition not saved yet says to save it first and offers no add and no list.
    [Fact]
    public void Section_UnsavedEdition_SaysToSaveTheEditionFirstAndOffersNoAdd()
    {
        var section = RenderSection(saved: false);

        section.Markup.Should().Contain("Save the edition first. Its notice subjects are added here.");
        section.FindAll("#notice-subject-add").Should().BeEmpty();
        section.FindAll(".app-item-rows").Should().BeEmpty();
        section.FindAll(".app-notice-subjects-footer").Should().BeEmpty();
        Api.Received.Should().BeEmpty("there is no edition to ask about");
    }

    [Fact]
    public void Section_LoadFails_ShowsTheErrorStateWithTryAgainAndNoAddButton()
    {
        Api.ListNoticeSubjectsFailure = (HttpStatusCode.InternalServerError, "server_error");
        Add(Basic, "Língua Portuguesa", 15);

        var section = RenderSection();

        section.WaitForAssertion(() => section.Find(".app-state-error").TextContent.Should().Contain("We could not load this list."));
        section.FindAll("#notice-subject-add").Should().BeEmpty();

        Api.ListNoticeSubjectsFailure = null;
        section.Find(".app-retry").Click();

        WaitForRows(section, 1);
        section.FindAll(".app-state-error").Should().BeEmpty();
    }

    // AC19: the first row of a group cannot go up and the last cannot go down; the buttons stay and say why.
    [Fact]
    public void Section_FirstAndLastRowOfAGroup_HaveTheirMoveButtonDisabledWithTheReason()
    {
        var first = Add(Basic, "Língua Portuguesa", 15);
        var middle = Add(Basic, "Informática", 5);
        var last = Add(Basic, "Conhecimentos Gerais", 5);
        var alone = Add(Specific, "Direito Penal", 10);

        var section = RenderSection();

        WaitForRows(section, 4);
        section.Find(ButtonOf(first, "move-up")).HasAttribute("disabled").Should().BeTrue();
        section.Find(ButtonOf(first, "move-down")).HasAttribute("disabled").Should().BeFalse();
        section.Find(ButtonOf(middle, "move-up")).HasAttribute("disabled").Should().BeFalse();
        section.Find(ButtonOf(middle, "move-down")).HasAttribute("disabled").Should().BeFalse();
        section.Find(ButtonOf(last, "move-up")).HasAttribute("disabled").Should().BeFalse();
        section.Find(ButtonOf(last, "move-down")).HasAttribute("disabled").Should().BeTrue();
        section.Find(ButtonOf(alone, "move-up")).HasAttribute("disabled").Should().BeTrue("a group of one cannot move");
        section.Find(ButtonOf(alone, "move-down")).HasAttribute("disabled").Should().BeTrue();

        var actions = section.FindComponents<AppRowActions>();
        actions.Single(row => row.Instance.ItemName == $"Língua Portuguesa, {Basic}").Instance
            .MoveUpDisabledReason.Should().Be("Already first in its group");
        actions.Single(row => row.Instance.ItemName == $"Conhecimentos Gerais, {Basic}").Instance
            .MoveDownDisabledReason.Should().Be("Already last in its group");
        section.Find(ButtonOf(first, "move-up")).GetAttribute("aria-label").Should()
            .Be($"Move up: Língua Portuguesa, {Basic}", "the name carries the row and its group");
    }

    // UC4: moving sends the direction at once, with no confirmation, and announces the new position.
    [Fact]
    public void Move_Down_SendsTheDirectionReloadsTheListAndAnnouncesThePosition()
    {
        var a = Add(Basic, "Língua Portuguesa", 15);
        Add(Basic, "Informática", 5);
        Add(Basic, "Conhecimentos Gerais", 5);

        var section = RenderSection();
        WaitForRows(section, 3);

        section.Find(ButtonOf(a, "move-down")).Click();

        section.WaitForAssertion(() =>
        {
            var move = Api.Received.Should().ContainSingle(call => call.Method == HttpMethod.Post).Subject;
            move.Path.Should().Be($"/api/v1/catalog/exams/{AgentePf.Id}/editions/{EditionId}/notice-subjects/{a.Id}/move");
            FakeCatalogApi.Read<MoveNoticeSubjectRequest>(move.Body).Direction.Should().Be("down");
        });
        section.WaitForAssertion(() => Labels(section).Should().Equal("Informática", "Língua Portuguesa", "Conhecimentos Gerais"));
        Announcement(section).Should().Be($"Língua Portuguesa moved to position 2 of 3 in {Basic}.");
    }

    [Fact]
    public void Move_UpInAListWithNoGroup_AnnouncesWithoutAGroupName()
    {
        Add(null, "Teoria Musical", 20);
        var second = Add(null, "História da Música", 10);

        var section = RenderSection();
        WaitForRows(section, 2);

        section.Find(ButtonOf(second, "move-up")).Click();

        section.WaitForAssertion(() => Labels(section).Should().Equal("História da Música", "Teoria Musical"));
        Announcement(section).Should().Be("História da Música moved to position 1 of 2.");
        FakeCatalogApi.Read<MoveNoticeSubjectRequest>(Api.Received.Last(call => call.Method == HttpMethod.Post).Body)
            .Direction.Should().Be("up");
    }

    // A refusal (another admin moved the row first) shows the alert at the top of the section and reloads the list.
    [Fact]
    public void Move_Refused_ShowsTheAlertAndReloadsTheList()
    {
        var a = Add(Basic, "Língua Portuguesa", 15);
        Add(Basic, "Informática", 5);
        var section = RenderSection();
        WaitForRows(section, 2);
        Api.MoveFailure = (HttpStatusCode.BadRequest, CatalogErrorCodes.NoticeSubjectMoveInvalid);
        var gets = Api.Received.Count(call => call.Method == HttpMethod.Get);

        section.Find(ButtonOf(a, "move-down")).Click();

        section.WaitForAssertion(() => section.Find("[role='alert']").TextContent.Should()
            .Contain("This subject cannot move that way. The list was reloaded."));
        section.WaitForAssertion(() => Api.Received.Count(call => call.Method == HttpMethod.Get).Should().Be(gets + 1));
        Labels(section).Should().Equal("Língua Portuguesa", "Informática");
    }

    [Fact]
    public void Move_OfARowAlreadyGone_ShowsTheNotFoundAlertAndDropsTheRow()
    {
        var a = Add(Basic, "Língua Portuguesa", 15);
        Add(Basic, "Informática", 5);
        var section = RenderSection();
        WaitForRows(section, 2);
        Api.NoticeSubjects.Remove(a);

        section.Find(ButtonOf(a, "move-down")).Click();

        section.WaitForAssertion(() => section.Find("[role='alert']").TextContent.Should()
            .Contain("This subject is no longer in the edition. The list was reloaded."));
        section.WaitForAssertion(() => section.FindAll(".app-item-row").Should().ContainSingle());
    }

    // AC-add and BR12 (AC18): the dialog sends what was typed; the group of the last add is offered the next time.
    [Fact]
    public void Add_ValidData_SendsItShowsTheSnackbarAndTheRowAppearsInItsGroup()
    {
        var (dialogs, snackbars) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.Markup.Should().Contain("This edition has no notice subject yet."));

        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);
        dialogs.Markup.Should().Contain("Add notice subject");
        SetGroup(dialogs, "  Basic knowledge  ");
        dialogs.Find("#notice-subject-label").Change("  Portuguese  ");
        dialogs.Find("#notice-subject-question-count").Change("10");
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() =>
        {
            var created = Api.Received.Should().ContainSingle(call => call.Method == HttpMethod.Post).Subject;
            created.Path.Should().Be($"/api/v1/catalog/exams/{AgentePf.Id}/editions/{EditionId}/notice-subjects");
            var sent = FakeCatalogApi.Read<SaveNoticeSubjectRequest>(created.Body);
            sent.Group.Should().Be("Basic knowledge", "the dialog trims before it sends");
            sent.Label.Should().Be("Portuguese");
            sent.QuestionCount.Should().Be(10);
        });
        snackbars.WaitForAssertion(() => snackbars.Markup.Should().Contain("Notice subject saved."));
        section.WaitForAssertion(() =>
        {
            Headings(section).Should().Equal("Basic knowledge");
            Labels(section).Should().Equal("Portuguese");
        });
    }

    [Fact]
    public void Add_BlankGroupAndNumber_SendsThemAsNothing()
    {
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());

        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);
        dialogs.Find("#notice-subject-label").Change("Teoria Musical");
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() =>
        {
            var sent = SentBody(Api, HttpMethod.Post);
            sent.Group.Should().BeNull("blank is 'no group'");
            sent.QuestionCount.Should().BeNull("blank means the notice does not say");
        });
    }

    // AC18: the group of the last notice subject added on this page visit starts the next add; a fresh visit starts empty.
    [Fact]
    public void Add_AfterAnAdd_TheGroupFieldStartsWithTheLastGroupAddedOnThisVisit()
    {
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());
        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);
        dialogs.Find("#notice-subject-group").GetAttribute("value").Should().BeNullOrEmpty("the first add of a visit starts empty");
        SetGroup(dialogs, "Basic knowledge");
        dialogs.Find("#notice-subject-label").Change("Portuguese");
        dialogs.Find("button.app-form-save").Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#notice-subject-label").Should().BeEmpty());
        WaitForRows(section, 1);

        section.Find("#notice-subject-add").Click();

        WaitForDialog(dialogs);
        dialogs.WaitForAssertion(() => dialogs.Find("#notice-subject-group").GetAttribute("value").Should().Be("Basic knowledge"));
    }

    [Fact]
    public void Add_OnAFreshPageVisit_TheGroupFieldStartsEmptyEvenWhenGroupsExist()
    {
        Add(Basic, "Língua Portuguesa", 15);
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        WaitForRows(section, 1);

        section.Find("#notice-subject-add").Click();

        WaitForDialog(dialogs);
        dialogs.Find("#notice-subject-group").GetAttribute("value").Should().BeNullOrEmpty();
    }

    // The group field suggests the groups already used in the edition, in display order, each once.
    [Fact]
    public void Add_TheGroupFieldSuggestsTheGroupsAlreadyUsedInTheEdition()
    {
        Add(Basic, "Língua Portuguesa", 15);
        Add(Basic, "Informática", 5);
        Add(null, "Ética", 5);
        Add(Specific, "Direito Penal", 10);
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        WaitForRows(section, 4);

        section.Find("#notice-subject-add").Click();

        WaitForDialog(dialogs);
        dialogs.FindComponent<AppSuggestField>().Instance.Suggestions.Should().Equal(Basic, Specific);
    }

    // The dialog renders with real parameters: title, the three fields in order, required marker on the label, hints.
    [Fact]
    public void Dialog_Add_ShowsTheThreeFieldsInOrderWithTheirLimitsAndHints()
    {
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());

        section.Find("#notice-subject-add").Click();

        WaitForDialog(dialogs);
        dialogs.FindAll("input[id^='notice-subject-']").Select(input => input.Id).Should()
            .Equal("notice-subject-group", "notice-subject-label", "notice-subject-question-count");
        var group = dialogs.Find("#notice-subject-group");
        group.GetAttribute("role").Should().Be("combobox");
        group.GetAttribute("aria-autocomplete").Should().Be("list");
        group.GetAttribute("maxlength").Should().Be("100");
        dialogs.Find("#notice-subject-label").GetAttribute("maxlength").Should().Be("200");
        dialogs.Find("#notice-subject-label").GetAttribute("aria-required").Should().Be("true");
        dialogs.Find("#notice-subject-group").GetAttribute("aria-required").Should().BeNull("the group is optional");
        dialogs.Find("#notice-subject-question-count").GetAttribute("type").Should().Be("number");
        var text = Flat(dialogs.Markup);
        text.Should().Contain("How the notice groups its subjects.")
            .And.Contain("As the notice names it. From 2 to 200 characters.")
            .And.Contain("From 1 to 500. Leave it empty when the notice does not say.");
        dialogs.FindAll("button.app-form-save").Should().ContainSingle();
        dialogs.FindAll("button.app-form-cancel").Should().ContainSingle();
    }

    // UC3: the dialog opens filled and sends a PUT on that notice subject.
    [Fact]
    public void Edit_OpensFilledAndSendsAPutOnThatNoticeSubject()
    {
        Add(Basic, "Língua Portuguesa", 15);
        var informatics = Add(Basic, "Informática", 5);
        var (dialogs, snackbars) = RenderProviders();
        var section = RenderSection();
        WaitForRows(section, 2);

        section.Find(ButtonOf(informatics, "edit")).Click();

        WaitForDialog(dialogs);
        dialogs.Markup.Should().Contain("Edit notice subject");
        dialogs.Find("#notice-subject-group").GetAttribute("value").Should().Be(Basic);
        dialogs.Find("#notice-subject-label").GetAttribute("value").Should().Be("Informática");
        dialogs.Find("#notice-subject-question-count").GetAttribute("value").Should().Be("5");
        dialogs.Find("#notice-subject-label").Change("Noções de Informática");
        dialogs.Find("#notice-subject-question-count").Change("8");
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() =>
        {
            var saved = Api.Received.Should().ContainSingle(call => call.Method == HttpMethod.Put).Subject;
            saved.Path.Should().Be($"/api/v1/catalog/exams/{AgentePf.Id}/editions/{EditionId}/notice-subjects/{informatics.Id}");
            var sent = FakeCatalogApi.Read<SaveNoticeSubjectRequest>(saved.Body);
            sent.Label.Should().Be("Noções de Informática");
            sent.QuestionCount.Should().Be(8);
            sent.Group.Should().Be(Basic);
        });
        snackbars.WaitForAssertion(() => snackbars.Markup.Should().Contain("Notice subject saved."));
        section.WaitForAssertion(() => Labels(section).Should().Equal("Língua Portuguesa", "Noções de Informática"));
    }

    // BR7: a changed group sends the row to the end of the new group, and the hint says so.
    [Fact]
    public void Edit_GroupChanged_ReplacesTheHintWithTheNoteAndTheRowGoesToTheNewGroup()
    {
        var portuguese = Add(Basic, "Língua Portuguesa", 15);
        Add(Specific, "Direito Penal", 10);
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        WaitForRows(section, 2);

        section.Find(ButtonOf(portuguese, "edit")).Click();
        WaitForDialog(dialogs);
        dialogs.Markup.Should().Contain("How the notice groups its subjects.")
            .And.NotContain("Changing the group moves the subject to the end of the new group.");
        SetGroup(dialogs, Specific);

        dialogs.WaitForAssertion(() => dialogs.Markup.Should()
            .Contain("Changing the group moves the subject to the end of the new group.")
            .And.NotContain("How the notice groups its subjects."));
        dialogs.Find("button.app-form-save").Click();

        section.WaitForAssertion(() =>
        {
            Headings(section).Should().Equal(Specific);
            Labels(section).Should().Equal("Direito Penal", "Língua Portuguesa");
        });
    }

    // Comfort checks: nothing is sent while a field is wrong, and each message sits under its field.
    [Fact]
    public void Save_BlankLabel_IsRefusedUnderTheLabelWithoutCallingTheApi()
    {
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());
        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);

        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Find("#notice-subject-label-description").TextContent.Should()
            .Contain("Type the subject as the notice names it."));
        dialogs.Find("#notice-subject-label").GetAttribute("aria-invalid").Should().Be("true");
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    [Fact]
    public void Save_OneCharacterLabel_IsRefusedAsTooShort()
    {
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());
        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);
        dialogs.Find("#notice-subject-label").Change("P");

        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Find("#notice-subject-label-description").TextContent.Should()
            .Contain("The subject needs at least 2 characters."));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("501")]
    [InlineData("-3")]
    public void Save_NumberOutsideOneToFiveHundred_IsRefusedUnderTheNumberWithoutCallingTheApi(string typed)
    {
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());
        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);
        dialogs.Find("#notice-subject-label").Change("Português");
        dialogs.Find("#notice-subject-question-count").Change(typed);

        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Find("#notice-subject-question-count-description").TextContent.Should()
            .Contain("Type a whole number from 1 to 500, or leave it empty."));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    [Fact]
    public void Save_GroupOverOneHundredCharacters_IsRefusedUnderTheGroup()
    {
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());
        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);
        SetGroup(dialogs, new string('G', CatalogLimits.NoticeSubjectGroupMaxLength + 1));
        dialogs.Find("#notice-subject-label").Change("Português");

        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Find("#notice-subject-group-description").TextContent.Should()
            .Contain("The group is too long: at most 100 characters."));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    // The Api's refusal of one field lands under that field; the dialog stays open with what was typed.
    [Fact]
    public void Save_ApiRefusesTheNumber_ShowsItUnderTheNumberAndKeepsTheDialogOpen()
    {
        Api.WriteFailure = (HttpStatusCode.BadRequest, CatalogErrorCodes.NoticeSubjectQuestionCountInvalid);
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());
        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);
        dialogs.Find("#notice-subject-label").Change("Português");
        dialogs.Find("#notice-subject-question-count").Change("10");

        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Find("#notice-subject-question-count-description").TextContent.Should()
            .Contain("Type a whole number from 1 to 500, or leave it empty."));
        dialogs.FindAll("[role='alert']").Should().BeEmpty("a field's code is not a banner");
        dialogs.Find("#notice-subject-label").GetAttribute("value").Should().Be("Português");
    }

    // A duplicate concerns the group and the label together: the alert at the top, not a field.
    [Fact]
    public void Save_Duplicate_ShowsTheAlertAtTheTopAndKeepsWhatWasTyped()
    {
        Api.WriteFailure = (HttpStatusCode.Conflict, CatalogErrorCodes.NoticeSubjectDuplicate);
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());
        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);
        SetGroup(dialogs, "basicos");
        dialogs.Find("#notice-subject-label").Change("portugues");

        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Find("[role='alert']").TextContent.Should()
            .Contain("This group already has a subject with this name (capitals and accents do not count)."));
        dialogs.FindAll(".app-field-error").Should().BeEmpty();
        dialogs.Find("#notice-subject-label").GetAttribute("value").Should().Be("portugues");
        dialogs.Find("button.app-form-save").HasAttribute("disabled").Should().BeFalse("Save is enabled again after the refusal");
    }

    [Fact]
    public void Save_ServerError_ShowsTheGenericAlert()
    {
        Api.WriteFailure = (HttpStatusCode.InternalServerError, "server_error");
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());
        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);
        dialogs.Find("#notice-subject-label").Change("Português");

        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Find("[role='alert']").TextContent.Should().Contain("Something went wrong. Please try again."));
    }

    // Closing with changes asks first; closing without changes does not.
    [Fact]
    public void Cancel_WithChanges_AsksToDiscardAndDiscardingClosesWithoutSaving()
    {
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());
        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);
        dialogs.Find("#notice-subject-label").Change("Português");

        dialogs.Find("button.app-form-cancel").Click();

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("Discard changes?"));
        dialogs.Find(".app-confirm-ok").Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#notice-subject-label").Should().BeEmpty());
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    [Fact]
    public void Cancel_WithoutChanges_ClosesAtOnce()
    {
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());
        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);

        dialogs.Find("button.app-form-cancel").Click();

        dialogs.WaitForAssertion(() => dialogs.FindAll("#notice-subject-label").Should().BeEmpty());
        dialogs.Markup.Should().NotContain("Discard changes?");
    }

    // UC5: delete asks first, names the subject, sends a DELETE and the row leaves.
    [Fact]
    public void Delete_Confirmed_SendsTheDeleteAndTheRowLeaves()
    {
        Add(Basic, "Língua Portuguesa", 15);
        var informatics = Add(Basic, "Informática", 5);
        var providers = RenderProviders();
        var section = RenderSection();
        WaitForRows(section, 2);

        section.Find(ButtonOf(informatics, "delete")).Click();

        providers.Dialogs.WaitForAssertion(() =>
            Flat(providers.Dialogs.Markup).Should()
                .Contain("The subject Informática leaves this edition")
                .And.Contain("Delete subject Informática"));
        providers.Dialogs.Find(".app-confirm-ok").Click();

        section.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Delete
            && call.Path == $"/api/v1/catalog/exams/{AgentePf.Id}/editions/{EditionId}/notice-subjects/{informatics.Id}"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Notice subject deleted."));
        section.WaitForAssertion(() => Labels(section).Should().Equal("Língua Portuguesa"));
    }

    [Fact]
    public void Delete_Cancelled_SendsNothing()
    {
        var row = Add(Basic, "Língua Portuguesa", 15);
        var providers = RenderProviders();
        var section = RenderSection();
        WaitForRows(section, 1);

        section.Find(ButtonOf(row, "delete")).Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll(".app-confirm-cancel").Should().ContainSingle());
        providers.Dialogs.Find(".app-confirm-cancel").Click();

        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Delete);
        Labels(section).Should().Equal("Língua Portuguesa");
    }

    [Fact]
    public void Delete_OfARowAlreadyGone_ShowsTheAlertAndReloadsTheList()
    {
        var row = Add(Basic, "Língua Portuguesa", 15);
        var providers = RenderProviders();
        var section = RenderSection();
        WaitForRows(section, 1);
        Api.NoticeSubjects.Clear();

        section.Find(ButtonOf(row, "delete")).Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll(".app-confirm-ok").Should().ContainSingle());
        providers.Dialogs.Find(".app-confirm-ok").Click();

        section.WaitForAssertion(() => section.Find("[role='alert']").TextContent.Should()
            .Contain("This subject is no longer in the edition. The list was reloaded."));
        section.WaitForAssertion(() => section.Markup.Should().Contain("This edition has no notice subject yet."));
    }

    [Fact]
    public void Delete_FailsOnTheServer_ShowsTheGenericAlert()
    {
        var row = Add(Basic, "Língua Portuguesa", 15);
        var providers = RenderProviders();
        var section = RenderSection();
        WaitForRows(section, 1);
        Api.WriteFailure = (HttpStatusCode.InternalServerError, "server_error");

        section.Find(ButtonOf(row, "delete")).Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll(".app-confirm-ok").Should().ContainSingle());
        providers.Dialogs.Find(".app-confirm-ok").Click();

        section.WaitForAssertion(() => section.Find("[role='alert']").TextContent.Should().Contain("Something went wrong. Please try again."));
        Labels(section).Should().Equal("Língua Portuguesa");
    }

    // The page wiring: the section is a card of its own below the form, outside it, and Save stays the only primary button.
    [Fact]
    public void EditionPage_SavedEdition_ListsItsNoticeSubjectsInACardOutsideTheForm()
    {
        var edition = new ExamEditionResponse(
            EditionId, AgentePf.Id, Cebraspe.Id, Cebraspe.Name, Cebraspe.Acronym, 2026, "Guarda", null, null, null, ExamEditionStatus.Published);
        Api.Editions.Add(edition);
        Add(Basic, "Língua Portuguesa", 15);

        var page = Render<ExamEditionForm>(parameters => parameters
            .Add(form => form.ExamId, AgentePf.Id)
            .Add(form => form.Id, EditionId));

        page.WaitForAssertion(() => page.FindAll("#edition-section-notice-subjects-title").Should().ContainSingle());
        page.WaitForAssertion(() => page.FindAll(".app-item-row").Should().ContainSingle());
        page.FindAll(".app-form-layout #edition-section-notice-subjects-title").Should().BeEmpty("it sits below the form layout, not inside it");
        page.FindAll(".app-form-save").Should().ContainSingle("the edition's Save is the page's only primary button");
    }

    // AC17 on the page: the add route shows the "save first" state and never asks the Api for notice subjects.
    [Fact]
    public void EditionPage_NewEdition_SaysToSaveTheEditionFirstAndAsksNothing()
    {
        var page = Render<ExamEditionForm>(parameters => parameters.Add(form => form.ExamId, AgentePf.Id));

        page.WaitForAssertion(() => page.FindAll("#edition-organizer").Should().ContainSingle());
        page.Find("section[aria-labelledby='edition-section-notice-subjects-title']").TextContent.Should()
            .Contain("Save the edition first. Its notice subjects are added here.");
        page.FindAll("#notice-subject-add").Should().BeEmpty();
        Api.Received.Should().NotContain(call => call.Path.EndsWith("/notice-subjects", StringComparison.Ordinal));
    }
}
