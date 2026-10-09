using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;
using MudDialogProvider = MudBlazor.MudDialogProvider;
using MudSnackbarProvider = MudBlazor.MudSnackbarProvider;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-75 Screen 2: the field "Covers" of the notice subject dialog - what it offers, what it sends on Save (the whole
/// mapping, BR7), the comfort checks, the states of the taxonomy and the refusals of the Api (UC2, UC3, BR1 to BR5;
/// AC1, AC9, AC13, AC14). The Api is the authority; the checks here are for comfort.
/// </summary>
public sealed class NoticeSubjectCoversDialogTests : CatalogPageTestContext
{
    private static readonly Guid EditionId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000e1");
    private static readonly Guid ComputingId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000c6");
    private static readonly Guid MathId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000c1");
    private static readonly Guid EquationsId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000c3");
    private static readonly Guid FractionsId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000c2");
    private static readonly Guid LogicId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000c4");
    private static readonly Guid PropositionsId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000c5");

    private const string Covers = "notice-subject-covers";

    public NoticeSubjectCoversDialogTests() => SetTaxonomy(NormalTaxonomy());

    // The Api's order: subjects alphabetical, each with its topics alphabetical (AC13).
    private static List<TaxonomySubjectResponse> NormalTaxonomy() =>
    [
        new(ComputingId, "Informática", []),
        new(MathId, "Matemática", [new(EquationsId, "Equações"), new(FractionsId, "Frações")]),
        new(LogicId, "Raciocínio Lógico", [new(PropositionsId, "Proposições")])
    ];

    // F-79 BR10: a curator moved "Proposições" from Raciocínio Lógico to Matemática.
    private static List<TaxonomySubjectResponse> TaxonomyAfterTheMove() =>
    [
        new(ComputingId, "Informática", []),
        new(MathId, "Matemática", [new(EquationsId, "Equações"), new(FractionsId, "Frações"), new(PropositionsId, "Proposições")]),
        new(LogicId, "Raciocínio Lógico", [])
    ];

    private void SetTaxonomy(List<TaxonomySubjectResponse> taxonomy)
    {
        Api.Taxonomy.Clear();
        Api.Taxonomy.AddRange(taxonomy);
    }

    private static NoticeSubjectMappingResponse WholeEntry(Guid id, string name) => new(id, name, null, null);

    private static NoticeSubjectMappingResponse TopicEntry(Guid subjectId, string subject, Guid topicId, string topic) =>
        new(subjectId, subject, topicId, topic);

    private NoticeSubjectResponse AddRow(string label, params NoticeSubjectMappingResponse[] mappings)
    {
        var row = new NoticeSubjectResponse(Guid.CreateVersion7(), EditionId, null, label, 10, mappings);
        Api.NoticeSubjects.Add(row);
        return row;
    }

    private IRenderedComponent<NoticeSubjectsSection> RenderSection() =>
        Render<NoticeSubjectsSection>(parameters => parameters
            .Add(section => section.ExamId, AgentePf.Id)
            .Add(section => section.EditionId, EditionId));

    private static string Flat(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    private static SaveNoticeSubjectRequest SentBody(FakeCatalogApi api, HttpMethod method) =>
        FakeCatalogApi.Read<SaveNoticeSubjectRequest>(api.Received.Last(call => call.Method == method).Body);

    private List<string> FocusTargets() =>
        [.. JSInterop.Invocations
            .Where(invocation => invocation.Identifier == "simulabShell.focusElement")
            .Select(invocation => (string)invocation.Arguments[0]!)];

    private static string OptionId(string key) => $"#{Covers}-option-{key}";

    private static string RemoveId(string key) => $"#{Covers}-remove-{key}";

    private static void WaitForDialog(IRenderedComponent<MudDialogProvider> dialogs) =>
        dialogs.WaitForAssertion(() => dialogs.FindAll("#notice-subject-label").Should().ContainSingle());

    private static void WaitForTaxonomy(IRenderedComponent<MudDialogProvider> dialogs) =>
        dialogs.WaitForAssertion(() =>
        {
            dialogs.FindAll($"#{Covers}").Should().ContainSingle();
            dialogs.Find($"#{Covers}").HasAttribute("disabled").Should().BeFalse();
        });

    private static void OpenList(IRenderedComponent<MudDialogProvider> dialogs) => dialogs.Find($"#{Covers}").Click();

    private static IReadOnlyList<string> Live(IRenderedComponent<MudDialogProvider> dialogs) =>
        [.. dialogs.FindAll("[aria-live='polite']").Select(live => Flat(live.TextContent))];

    private static IReadOnlyList<string> ChipTexts(IRenderedComponent<MudDialogProvider> dialogs) =>
        [.. dialogs.FindAll("ul.app-mp-chips > li").Select(item => Flat(ChipText(item)))];

    // The visible form: a topic's "Subject › Topic" lives in the aria-hidden span; a whole subject is its name and marker.
    private static string ChipText(IElement item)
    {
        var text = item.QuerySelector(".app-chip-text")!;
        if (text.QuerySelector(".app-chip-secondary") is { } secondary)
        {
            var name = string.Concat(text.ChildNodes.Where(node => node.NodeType == NodeType.Text).Select(node => node.TextContent)).Trim();
            return $"{name} | {Flat(secondary.TextContent)}";
        }

        return text.QuerySelector("span[aria-hidden='true']")!.TextContent;
    }

    private (IRenderedComponent<NoticeSubjectsSection> Section, IRenderedComponent<MudDialogProvider> Dialogs, IRenderedComponent<MudSnackbarProvider> Snackbars) OpenAdd()
    {
        var (dialogs, snackbars) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());
        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);
        WaitForTaxonomy(dialogs);

        return (section, dialogs, snackbars);
    }

    private (IRenderedComponent<NoticeSubjectsSection> Section, IRenderedComponent<MudDialogProvider> Dialogs, IRenderedComponent<MudSnackbarProvider> Snackbars) OpenEdit(
        NoticeSubjectResponse row,
        bool waitForTaxonomy = true)
    {
        var (dialogs, snackbars) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll(".app-item-row").Should().NotBeEmpty());
        section.Find($"#notice-subject-actions-{row.Id}-edit").Click();
        WaitForDialog(dialogs);
        if (waitForTaxonomy)
        {
            WaitForTaxonomy(dialogs);
        }

        return (section, dialogs, snackbars);
    }

    // The dialog renders with its real parameters: the Covers field comes after the number of questions (UC2).
    [Fact]
    public void Dialog_Add_ShowsTheCoversFieldLastWithItsLabelHintPlaceholderAndCount_AndReadsTheTaxonomyOnce()
    {
        var (_, dialogs, _) = OpenAdd();

        dialogs.FindAll("input[id^='notice-subject-']").Select(input => input.Id).Should()
            .Equal("notice-subject-group", "notice-subject-label", "notice-subject-question-count", Covers);
        dialogs.Find($"label[for='{Covers}']").TextContent.Trim().Should().Be("Covers");
        var input = dialogs.Find($"#{Covers}");
        input.GetAttribute("role").Should().Be("combobox");
        input.GetAttribute("placeholder").Should().Be("Search a subject or a topic");
        dialogs.Find($"#{Covers}-description").TextContent.Should().Contain("Optional. The taxonomy subjects and topics this notice subject covers");
        dialogs.Find($"#{Covers}-count").TextContent.Should().Be("Picked: 0 of 50");
        dialogs.Find(".app-mp-none").TextContent.Should().Be("Nothing picked: the subject stays not mapped.");
        dialogs.FindAll(".app-mp-clear").Should().BeEmpty("clear is shown only with at least one pick");
        Api.TaxonomyReads.Should().Be(1, "one call per dialog opening");
    }

    // AC13 (screen side): the groups are the subjects in the Api's order, each starting with its whole-subject option.
    [Fact]
    public void List_GroupsAreTheSubjectsInTheApisOrder_EachStartingWithItsWholeSubjectOption()
    {
        var (_, dialogs, _) = OpenAdd();

        OpenList(dialogs);

        dialogs.FindAll("[role='group']").Select(group => group.GetAttribute("aria-label")).Should()
            .Equal("Informática", "Matemática", "Raciocínio Lógico");
        dialogs.FindAll("[role='option'] .app-mp-option-name").Select(name => name.TextContent.Trim()).Should()
            .Equal("Informática", "Matemática", "Equações", "Frações", "Raciocínio Lógico", "Proposições");
        dialogs.Find(OptionId($"s-{MathId}")).QuerySelector(".app-mp-option-secondary")!.TextContent.Should().Be("whole subject");
    }

    // AC1: a whole subject and a topic, picked in a row (the list stays open), are sent whole and the row shows both chips.
    [Fact]
    public void Add_AWholeSubjectAndATopic_AreSentTogetherAndTheRowShowsBothChips()
    {
        var (section, dialogs, snackbars) = OpenAdd();
        dialogs.Find("#notice-subject-label").Change("Raciocínio Lógico-Matemático");

        OpenList(dialogs);
        dialogs.Find(OptionId($"s-{MathId}")).Click();
        dialogs.Find(OptionId($"t-{PropositionsId}")).Click();

        dialogs.WaitForAssertion(() => dialogs.FindAll("ul.app-mp-chips > li").Should().HaveCount(2));
        dialogs.FindAll("[role='listbox']").Should().ContainSingle("several can be picked in a row");
        dialogs.Find($"#{Covers}-count").TextContent.Should().Be("Picked: 2 of 50");
        dialogs.Find(OptionId($"s-{MathId}")).GetAttribute("aria-selected").Should().Be("true");
        ChipTexts(dialogs).Should().Equal("Matemática | whole subject", "Raciocínio Lógico › Proposições");

        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => SentBody(Api, HttpMethod.Post).Mappings.Should().Equal(
            new NoticeSubjectMappingRequest(MathId, null),
            new NoticeSubjectMappingRequest(null, PropositionsId)));
        snackbars.WaitForAssertion(() => snackbars.Markup.Should().Contain("Notice subject saved."));
        section.WaitForAssertion(() => section.FindAll("ul.app-chip-list > li").Should().HaveCount(2));
    }

    [Fact]
    public void Add_NothingPicked_SendsAnEmptyListWhichIsNotMapped()
    {
        var (section, dialogs, _) = OpenAdd();
        dialogs.Find("#notice-subject-label").Change("Teoria Musical");

        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() =>
        {
            var sent = SentBody(Api, HttpMethod.Post);
            sent.Mappings.Should().NotBeNull();
            sent.Mappings!.Should().BeEmpty();
        });
        section.WaitForAssertion(() => section.FindAll(".app-status-chip-warning").Should().ContainSingle());
    }

    [Fact]
    public void Pick_AnnouncesEachToggle()
    {
        var (_, dialogs, _) = OpenAdd();
        OpenList(dialogs);

        dialogs.Find(OptionId($"s-{MathId}")).Click();
        dialogs.WaitForAssertion(() => Live(dialogs).Should().Contain("Picked: Matemática, whole subject."));

        dialogs.Find(OptionId($"t-{PropositionsId}")).Click();
        dialogs.WaitForAssertion(() => Live(dialogs).Should().Contain("Picked: Proposições, topic of Raciocínio Lógico."));

        dialogs.Find(OptionId($"t-{PropositionsId}")).Click();
        dialogs.WaitForAssertion(() => Live(dialogs).Should().Contain("Removed: Proposições, topic of Raciocínio Lógico."));
    }

    // AC9: mapped to A and B, saved with only C - the mapping sent is exactly C (UC3, BR7).
    [Fact]
    public void Edit_ClearingAndPickingOtherwise_SendsExactlyWhatIsPickedNow()
    {
        var row = AddRow("Direito", WholeEntry(MathId, "Matemática"), TopicEntry(MathId, "Matemática", FractionsId, "Frações"));
        var (section, dialogs, _) = OpenEdit(row);
        ChipTexts(dialogs).Should().Equal("Matemática | whole subject", "Matemática › Frações");
        dialogs.Find($"#{Covers}-count").TextContent.Should().Be("Picked: 2 of 50");

        dialogs.Find("button.app-mp-clear").Click();
        dialogs.WaitForAssertion(() => ChipTexts(dialogs).Should().BeEmpty());
        dialogs.WaitForAssertion(() => Live(dialogs).Should().Contain("All items were removed."));
        OpenList(dialogs);
        dialogs.Find(OptionId($"s-{ComputingId}")).Click();
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() =>
        {
            Api.Received.Should().ContainSingle(call => call.Method == HttpMethod.Put);
            SentBody(Api, HttpMethod.Put).Mappings.Should().Equal(new NoticeSubjectMappingRequest(ComputingId, null));
        });
        section.WaitForAssertion(() => section.FindAll("ul.app-chip-list > li").Should().ContainSingle());
    }

    // AC9: saved with an empty list, the row shows "Not mapped" (UC3, BR2).
    [Fact]
    public void Edit_ClearedAndSaved_SendsAnEmptyListAndTheRowShowsNotMapped()
    {
        var row = AddRow("Direito", WholeEntry(MathId, "Matemática"));
        var (section, dialogs, _) = OpenEdit(row);

        dialogs.Find("button.app-mp-clear").Click();
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => SentBody(Api, HttpMethod.Put).Mappings!.Should().BeEmpty());
        section.WaitForAssertion(() =>
        {
            section.FindAll("ul.app-chip-list").Should().BeEmpty();
            section.Find(".app-status-chip-warning").TextContent.Trim().Should().Be("Not mapped");
        });
    }

    // The picks change the row only on Save (BR7): closing with a discard sends nothing.
    [Fact]
    public void Cancel_AfterPicking_AsksToDiscardAndSendsNothing()
    {
        var (section, dialogs, _) = OpenAdd();
        OpenList(dialogs);
        dialogs.Find(OptionId($"s-{MathId}")).Click();

        dialogs.Find("button.app-form-cancel").Click();

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("Discard changes?"));
        dialogs.Find(".app-confirm-ok").Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#notice-subject-label").Should().BeEmpty());
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
        section.FindAll(".app-item-row").Should().BeEmpty();
    }

    [Fact]
    public void Cancel_WithOnlyTheRowsOwnPicks_ClosesAtOnce()
    {
        var row = AddRow("Direito", WholeEntry(MathId, "Matemática"));
        var (_, dialogs, _) = OpenEdit(row);

        dialogs.Find("button.app-form-cancel").Click();

        dialogs.WaitForAssertion(() => dialogs.FindAll("#notice-subject-label").Should().BeEmpty());
        dialogs.Markup.Should().NotContain("Discard changes?");
    }

    // The chips of an edit come from the row, so they show and can be removed without the taxonomy (Screen 2).
    [Fact]
    public void Edit_TaxonomyFailsToLoad_TheRowsChipsStayRemovableTryAgainReloadsAndSaveStillWorks()
    {
        Api.TaxonomyFailure = (HttpStatusCode.InternalServerError, "server_error");
        var row = AddRow("Direito", WholeEntry(MathId, "Matemática"), TopicEntry(LogicId, "Raciocínio Lógico", PropositionsId, "Proposições"));
        var (_, dialogs, _) = OpenEdit(row, waitForTaxonomy: false);

        dialogs.WaitForAssertion(() => dialogs.Find($"#{Covers}-description").TextContent.Should()
            .Contain("The taxonomy could not be loaded. The items already picked stay here."));
        dialogs.Find($"#{Covers}").HasAttribute("disabled").Should().BeTrue();
        ChipTexts(dialogs).Should().Equal("Matemática | whole subject", "Raciocínio Lógico › Proposições");
        dialogs.FindAll("button.app-chip-remove").Should().OnlyContain(button => !button.HasAttribute("disabled"));
        dialogs.FindAll("[role='alert']").Should().BeEmpty("the failure is under the field, not a banner");

        Api.TaxonomyFailure = null;
        dialogs.Find($"#{Covers}-description button").Click();

        WaitForTaxonomy(dialogs);
        Api.TaxonomyReads.Should().Be(2);
        dialogs.Find($"#{Covers}-description").TextContent.Should().Contain("Optional.");
    }

    [Fact]
    public void Edit_TaxonomyFailsToLoad_SaveStillSendsThePicksAlreadyThere()
    {
        Api.TaxonomyFailure = (HttpStatusCode.InternalServerError, "server_error");
        var row = AddRow("Direito", WholeEntry(MathId, "Matemática"));
        var (_, dialogs, _) = OpenEdit(row, waitForTaxonomy: false);
        dialogs.WaitForAssertion(() => dialogs.Find($"#{Covers}-description").TextContent.Should().Contain("could not be loaded"));
        Api.TaxonomyFailure = null;
        Api.Taxonomy.Clear();
        Api.Taxonomy.AddRange(NormalTaxonomy());

        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => SentBody(Api, HttpMethod.Put).Mappings.Should().Equal(new NoticeSubjectMappingRequest(MathId, null)));
    }

    // AC14 (screen side): a refusal of the taxonomy read (403, no catalog.manage) is the load-failed state, not a crash.
    [Fact]
    public void Taxonomy_ForbiddenForTheUser_ShowsTheLoadFailedStateWithTryAgain()
    {
        Api.TaxonomyFailure = (HttpStatusCode.Forbidden, "forbidden");
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());

        section.Find("#notice-subject-add").Click();

        WaitForDialog(dialogs);
        dialogs.WaitForAssertion(() => dialogs.Find($"#{Covers}-description button").TextContent.Trim().Should().Be("Try again"));
        dialogs.Find($"#{Covers}").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Taxonomy_Empty_DisablesTheFieldAndSaysWhereToAddSubjectsWithoutALink()
    {
        Api.Taxonomy.Clear();
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());

        section.Find("#notice-subject-add").Click();

        WaitForDialog(dialogs);
        dialogs.WaitForAssertion(() => dialogs.Find($"#{Covers}-description").TextContent.Should()
            .Contain("The taxonomy has no subject yet. Add them under Content › Subjects."));
        dialogs.Find($"#{Covers}").HasAttribute("disabled").Should().BeTrue();
        dialogs.FindAll($"#{Covers}-description a").Should().BeEmpty("a link would leave a dialog with unsaved changes");
    }

    // Comfort check (BR4 on screen): a topic whose subject is picked whole cannot be picked, and says why.
    [Fact]
    public void List_AWholeSubjectPicked_ItsTopicsAreDisabledWithTheReason()
    {
        var (_, dialogs, _) = OpenAdd();
        OpenList(dialogs);

        dialogs.Find(OptionId($"s-{MathId}")).Click();

        var topic = dialogs.Find(OptionId($"t-{FractionsId}"));
        topic.GetAttribute("aria-disabled").Should().Be("true");
        dialogs.Find($"#{topic.GetAttribute("aria-describedby")}").TextContent.Should().Be("Already covered by the whole subject");
        dialogs.Find(OptionId($"t-{PropositionsId}")).GetAttribute("aria-disabled").Should().BeNull("another subject's topic is free");
        topic.Click();
        dialogs.FindAll("ul.app-mp-chips > li").Should().ContainSingle("Space or Enter on a disabled option does nothing");
    }

    [Fact]
    public void List_ATopicPicked_ItsSubjectsWholeOptionIsDisabledWithTheReason()
    {
        var (_, dialogs, _) = OpenAdd();
        OpenList(dialogs);

        dialogs.Find(OptionId($"t-{FractionsId}")).Click();

        var whole = dialogs.Find(OptionId($"s-{MathId}"));
        whole.GetAttribute("aria-disabled").Should().Be("true");
        dialogs.Find($"#{whole.GetAttribute("aria-describedby")}").TextContent.Should()
            .Be("Remove its picked topics to pick the whole subject");
        dialogs.Find(OptionId($"t-{EquationsId}")).GetAttribute("aria-disabled").Should().BeNull("a sibling topic is free");
    }

    // BR4 and AC6: compared with the SAVED mapping, so an overlap a topic move created is never blocked.
    [Fact]
    public void List_AnOverlapTheSavedMappingAlreadyHas_IsKept_AndAnotherTopicOfThatSubjectIsBlocked()
    {
        SetTaxonomy(TaxonomyAfterTheMove());
        var row = AddRow("Direito", WholeEntry(MathId, "Matemática"), TopicEntry(MathId, "Matemática", PropositionsId, "Proposições"));
        var (_, dialogs, _) = OpenEdit(row);

        OpenList(dialogs);

        dialogs.Find(OptionId($"s-{MathId}")).GetAttribute("aria-disabled").Should().BeNull();
        dialogs.Find(OptionId($"t-{PropositionsId}")).GetAttribute("aria-disabled").Should().BeNull();
        var fractions = dialogs.Find(OptionId($"t-{FractionsId}"));
        fractions.GetAttribute("aria-disabled").Should().Be("true", "only the saved pair is kept: another topic of the whole subject would be a new overlap");
        dialogs.Find($"#{fractions.GetAttribute("aria-describedby")}").TextContent.Should().Be("Already covered by the whole subject");
        dialogs.Find(OptionId($"t-{PropositionsId}")).Click();
        dialogs.WaitForAssertion(() => ChipTexts(dialogs).Should().Equal("Matemática | whole subject"));
    }

    [Fact]
    public void List_FiftyPicked_EveryUnpickedOptionIsDisabledAndTheLimitReplacesTheHint()
    {
        var subjects = Enumerable.Range(1, 60)
            .Select(number => new TaxonomySubjectResponse(Guid.CreateVersion7(), $"Disciplina {number:00}", []))
            .ToList();
        SetTaxonomy(subjects);
        var row = AddRow("Legislação", [.. subjects.Take(50).Select(subject => WholeEntry(subject.Id, subject.Name))]);
        var (_, dialogs, _) = OpenEdit(row);

        OpenList(dialogs);

        dialogs.Find($"#{Covers}-count").TextContent.Should().Be("Picked: 50 of 50");
        dialogs.Find($"#{Covers}-description").TextContent.Should()
            .Contain("Limit of 50 items reached. Remove one to pick another.")
            .And.NotContain("Optional.");
        dialogs.FindAll("[role='option'][aria-selected='false']").Should().HaveCount(10)
            .And.OnlyContain(option => option.GetAttribute("aria-disabled") == "true");
        dialogs.FindAll("[role='option'][aria-selected='true']").Should().HaveCount(50)
            .And.OnlyContain(option => option.GetAttribute("aria-disabled") == null);
        dialogs.Find(OptionId($"s-{subjects[55].Id}")).Click();
        dialogs.Find($"#{Covers}-count").TextContent.Should().Be("Picked: 50 of 50");
    }

    // Removing a chip: the focus goes to the next chip's remove button, else the previous one, else the search input.
    [Fact]
    public void Remove_FocusGoesToTheNextChipsButton_ElseThePreviousOne_ElseTheInput()
    {
        var row = AddRow(
            "Direito",
            WholeEntry(MathId, "Matemática"),
            TopicEntry(MathId, "Matemática", FractionsId, "Frações"),
            TopicEntry(LogicId, "Raciocínio Lógico", PropositionsId, "Proposições"));
        var (_, dialogs, _) = OpenEdit(row);

        dialogs.Find(RemoveId($"t-{FractionsId}")).Click();
        dialogs.WaitForAssertion(() => FocusTargets().Should().Contain($"{Covers}-remove-t-{PropositionsId}"));
        dialogs.WaitForAssertion(() => Live(dialogs).Should().Contain("Removed: Frações, topic of Matemática."));

        dialogs.Find(RemoveId($"t-{PropositionsId}")).Click();
        dialogs.WaitForAssertion(() => FocusTargets().Should().Contain($"{Covers}-remove-s-{MathId}"));

        dialogs.Find(RemoveId($"s-{MathId}")).Click();
        dialogs.WaitForAssertion(() => FocusTargets().Should().Contain(Covers));
        dialogs.WaitForAssertion(() => dialogs.FindAll(".app-mp-none").Should().ContainSingle());
    }

    [Fact]
    public void Remove_ButtonsAreNamedAfterTheChipAndSayRemove()
    {
        var row = AddRow("Direito", TopicEntry(LogicId, "Raciocínio Lógico", PropositionsId, "Proposições"));
        var (_, dialogs, _) = OpenEdit(row);

        dialogs.Find(RemoveId($"t-{PropositionsId}")).GetAttribute("aria-label").Should()
            .Be("Remove Proposições, topic of Raciocínio Lógico");
    }

    [Fact]
    public void Clear_RemovesEverythingAnnouncesItAndFocusesTheInput()
    {
        var row = AddRow("Direito", WholeEntry(MathId, "Matemática"), WholeEntry(ComputingId, "Informática"));
        var (_, dialogs, _) = OpenEdit(row);

        dialogs.Find("button.app-mp-clear").Click();

        dialogs.WaitForAssertion(() => dialogs.FindAll("ul.app-mp-chips").Should().BeEmpty());
        dialogs.WaitForAssertion(() => Live(dialogs).Should().Contain("All items were removed."));
        dialogs.WaitForAssertion(() => FocusTargets().Should().Contain(Covers));
        dialogs.FindAll("button.app-mp-clear").Should().BeEmpty();
    }

    // While saving, the Covers input, clear and every remove button are disabled (States).
    [Fact]
    public void Saving_DisablesTheCoversInputClearAndRemoveButtons()
    {
        var row = AddRow("Direito", WholeEntry(MathId, "Matemática"));
        var (_, dialogs, _) = OpenEdit(row);
        var gate = new TaskCompletionSource();
        Api.WriteGate = gate;

        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() =>
        {
            dialogs.Find($"#{Covers}").HasAttribute("disabled").Should().BeTrue();
            dialogs.Find("button.app-mp-clear").HasAttribute("disabled").Should().BeTrue();
            dialogs.Find(RemoveId($"s-{MathId}")).HasAttribute("disabled").Should().BeTrue();
        });

        gate.SetResult();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#notice-subject-label").Should().BeEmpty());
    }

    // Refusal notice_subject.mapping_overlap: under the field, the topic chip marked, the taxonomy read again, the input focused.
    [Fact]
    public void Save_RefusedForAnOverlap_MarksTheTopicChipUnderTheFieldReloadsTheTaxonomyAndFocusesTheInput()
    {
        var (_, dialogs, _) = OpenAdd();
        dialogs.Find("#notice-subject-label").Change("Matemática e Lógica");
        OpenList(dialogs);
        dialogs.Find(OptionId($"s-{MathId}")).Click();
        dialogs.Find(OptionId($"t-{PropositionsId}")).Click();
        SetTaxonomy(TaxonomyAfterTheMove());
        Api.WriteFailure = (HttpStatusCode.BadRequest, CatalogErrorCodes.NoticeSubjectMappingOverlap);

        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Find($"#{Covers}-description").TextContent.Should()
            .Contain("A topic cannot be picked together with its whole subject; the items are marked below."));
        dialogs.WaitForAssertion(() => dialogs.FindAll("ul.app-mp-chips .app-chip.is-marked").Should().ContainSingle());
        var marked = dialogs.Find("ul.app-mp-chips .app-chip.is-marked");
        marked.QuerySelector(".app-chip-mark")!.TextContent.Should().Contain("repeats the whole subject");
        Flat(marked.QuerySelector("span[aria-hidden='true']")!.TextContent).Should().Be("Matemática › Proposições", "the topic shows under its new subject");
        dialogs.Find($"#{Covers}").GetAttribute("aria-invalid").Should().Be("true");
        dialogs.FindAll("[role='alert']").Should().BeEmpty("a field's refusal is not a banner");
        dialogs.FindAll("#notice-subject-label").Should().ContainSingle("the dialog stays open with what was picked");
        Api.TaxonomyReads.Should().Be(2);
        dialogs.WaitForAssertion(() => FocusTargets().Should().Contain(Covers));

        // Removing the topic takes the mark and the message away.
        dialogs.Find(RemoveId($"t-{PropositionsId}")).Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll(".app-chip.is-marked").Should().BeEmpty());
        dialogs.Find($"#{Covers}-description").TextContent.Should().NotContain("A topic cannot be picked");
    }

    // Refusal notice_subject.mapping_target_not_found: the taxonomy is read again, the gone chip marked, the options rebuilt.
    [Fact]
    public void Save_RefusedBecauseAnItemIsGone_MarksTheChipRebuildsTheOptionsAndCanBeFixedByRemovingIt()
    {
        var (_, dialogs, _) = OpenAdd();
        dialogs.Find("#notice-subject-label").Change("Noções de Matemática");
        OpenList(dialogs);
        dialogs.Find(OptionId($"t-{FractionsId}")).Click();
        dialogs.Find(OptionId($"t-{EquationsId}")).Click();
        SetTaxonomy(
        [
            new(ComputingId, "Informática", []),
            new(MathId, "Matemática", [new(EquationsId, "Equações")]),
            new(LogicId, "Raciocínio Lógico", [new(PropositionsId, "Proposições")])
        ]);
        Api.WriteFailure = (HttpStatusCode.BadRequest, CatalogErrorCodes.NoticeSubjectMappingTargetNotFound);

        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Find($"#{Covers}-description").TextContent.Should()
            .Contain("One of the picked items is no longer in the taxonomy; it is marked below."));
        dialogs.WaitForAssertion(() => dialogs.FindAll("ul.app-mp-chips .app-chip.is-marked").Should().ContainSingle());
        dialogs.Find("ul.app-mp-chips .app-chip.is-marked .app-chip-mark").TextContent.Should().Contain("no longer in the taxonomy");
        Api.TaxonomyReads.Should().Be(2);
        OpenList(dialogs);
        dialogs.FindAll(OptionId($"t-{FractionsId}")).Should().BeEmpty("the options were rebuilt from the new taxonomy");

        dialogs.Find(RemoveId($"t-{FractionsId}")).Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll(".app-chip.is-marked").Should().BeEmpty());
        dialogs.Find($"#{Covers}-description").TextContent.Should().NotContain("no longer in the taxonomy");
        Api.WriteFailure = null;
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => Api.Received.Count(call => call.Method == HttpMethod.Post).Should().Be(2));
        SentBody(Api, HttpMethod.Post).Mappings.Should().Equal(new NoticeSubjectMappingRequest(null, EquationsId));
    }

    [Theory]
    [InlineData(CatalogErrorCodes.NoticeSubjectMappingTooMany, "At most 50 items per notice subject.")]
    [InlineData(CatalogErrorCodes.NoticeSubjectMappingInvalid, "One of the picked items is not valid. Remove it and pick it again.")]
    public void Save_RefusedForTooManyOrInvalid_ShowsTheMessageAloneUnderTheField(string code, string message)
    {
        var (_, dialogs, _) = OpenAdd();
        dialogs.Find("#notice-subject-label").Change("Noções");
        Api.WriteFailure = (HttpStatusCode.BadRequest, code);

        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Find($"#{Covers}-description").TextContent.Should().Contain(message));
        dialogs.FindAll("[role='alert']").Should().BeEmpty();
        dialogs.FindAll(".app-chip.is-marked").Should().BeEmpty();
        Api.TaxonomyReads.Should().Be(1, "neither code needs the taxonomy");
    }

    // Other codes keep F-74's placement: a concurrent save of the same notice subject is the alert at the top.
    [Fact]
    public void Save_RefusedForAConcurrentSave_ShowsTheAlertAtTheTopAndKeepsThePicks()
    {
        var row = AddRow("Direito", WholeEntry(MathId, "Matemática"));
        var (_, dialogs, _) = OpenEdit(row);
        Api.WriteFailure = (HttpStatusCode.Conflict, CatalogErrorCodes.NoticeSubjectMappingConflict);

        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Find("[role='alert']").TextContent.Should()
            .Contain("Someone else saved this notice subject at the same time. Save again."));
        ChipTexts(dialogs).Should().Equal("Matemática | whole subject");
        dialogs.Find($"#{Covers}").GetAttribute("aria-invalid").Should().BeNull();
    }
}
