using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-77 screen side: the "Other names" field of the subject and the topic dialogs (AC2, AC13, AC15, AC16, BR4,
/// BR13). The rules are the Api's and have their own tests; here the dialogs are checked to show the stored names,
/// to send the whole set only when it changed, and to put a refusal on the field.
/// </summary>
public sealed class SubjectAliasDialogTests : SubjectsTestContext
{
    private static readonly SubjectResponse WithAliases = Portuguese with { Aliases = ["Língua Portuguesa", "Gramática"] };

    private static readonly TopicResponse TopicWithAliases = Crase with { Aliases = ["Acento grave"] };

    private IRenderedComponent<Subjects> RenderList() => Render<Subjects>();

    private IRenderedComponent<SubjectDetail> RenderDetail() =>
        Render<SubjectDetail>(parameters => parameters.Add(page => page.Id, Portuguese.Id));

    private static void Enter(AngleSharp.Dom.IElement input, string text)
    {
        input.Input(text);
        input.KeyDown(new KeyboardEventArgs { Key = "Enter" });
    }

    private SaveSubjectRequest LastSubjectRequest(HttpMethod method) =>
        FakeCatalogApi.Read<SaveSubjectRequest>(Api.Received.Last(call => call.Method == method).Body);

    private SaveTopicRequest LastTopicRequest(HttpMethod method) =>
        FakeCatalogApi.Read<SaveTopicRequest>(Api.Received.Last(call => call.Method == method).Body);

    // AC2: the edit dialog shows one chip for each stored other name.
    [Fact]
    public void SubjectEdit_ShowsTheStoredOtherNamesAsChips()
    {
        Subjects = [WithAliases];
        var providers = RenderProviders();
        var page = RenderList();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(1));

        page.FindAll("tbody tr")[0].QuerySelectorAll("button")[0].Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#subject-aliases"));
        providers.Dialogs.FindAll("#subject-aliases-field .app-chip-text").Select(text => text.TextContent.Trim())
            .Should().Equal("Língua Portuguesa", "Gramática");
        providers.Dialogs.Find("label[for='subject-aliases']").TextContent.Trim().Should().Be("Other names");
    }

    // BR4 (trap T1): a save that did not touch the chips never sends the set, so it cannot wipe what the Api holds.
    [Fact]
    public void SubjectEdit_RenameOnly_SendsAliasesNull()
    {
        Subjects = [WithAliases];
        var providers = RenderProviders();
        var page = RenderList();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(1));

        page.FindAll("tbody tr")[0].QuerySelectorAll("button")[0].Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#subject-name"));
        providers.Dialogs.Find("#subject-name").Change("Língua Portuguesa II");
        providers.Dialogs.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Put));
        LastSubjectRequest(HttpMethod.Put).Aliases.Should().BeNull();
    }

    // AC2: a new name typed with Enter travels with the others, as the whole set.
    [Fact]
    public void SubjectEdit_AddingAName_SendsTheWholeSet()
    {
        Subjects = [WithAliases];
        var providers = RenderProviders();
        var page = RenderList();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(1));

        page.FindAll("tbody tr")[0].QuerySelectorAll("button")[0].Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#subject-aliases"));
        Enter(providers.Dialogs.Find("#subject-aliases"), "Port. Brasileiro");
        providers.Dialogs.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Put));
        LastSubjectRequest(HttpMethod.Put).Aliases.Should()
            .BeEquivalentTo(["Língua Portuguesa", "Gramática", "Port. Brasileiro"]);
    }

    // BR4: removing every chip sends an empty list - "clear the set" - and never null.
    [Fact]
    public void SubjectEdit_RemovingEveryName_SendsAnEmptyList()
    {
        Subjects = [Portuguese with { Aliases = ["Gramática"] }];
        var providers = RenderProviders();
        var page = RenderList();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(1));

        page.FindAll("tbody tr")[0].QuerySelectorAll("button")[0].Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#subject-aliases"));
        providers.Dialogs.Find("button[aria-label='Remove Gramática']").Click();
        providers.Dialogs.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Put));
        LastSubjectRequest(HttpMethod.Put).Aliases.Should().NotBeNull().And.BeEmpty();
    }

    // AC2: a create with no chips sends null, as before this item.
    [Fact]
    public void SubjectAdd_WithoutChips_SendsAliasesNull_AndWithChips_SendsThem()
    {
        var providers = RenderProviders();
        var page = RenderList();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.Find(".app-page-header button").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#subject-name"));
        providers.Dialogs.Find("#subject-name").Change("Informática");
        Enter(providers.Dialogs.Find("#subject-aliases"), "TI");
        providers.Dialogs.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post));
        LastSubjectRequest(HttpMethod.Post).Aliases.Should().Equal("TI");
    }

    // AC5 (screen side): a refusal about an other name shows on that field, not on the name.
    [Theory]
    [InlineData(CatalogErrorCodes.SubjectAliasTaken, "Another subject already uses one of these names")]
    [InlineData(CatalogErrorCodes.SubjectAliasSameAsName, "An other name is the same as the subject's name")]
    public void SubjectSave_AliasRefused_ShowsTheMessageOnTheAliasField(string code, string text)
    {
        TaxonomyWriteFailure = (code == CatalogErrorCodes.SubjectAliasTaken ? HttpStatusCode.Conflict : HttpStatusCode.BadRequest, code);
        var providers = RenderProviders();
        var page = RenderList();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.Find(".app-page-header button").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#subject-name"));
        providers.Dialogs.Find("#subject-name").Change("Informática");
        Enter(providers.Dialogs.Find("#subject-aliases"), "Português");
        providers.Dialogs.Find(".app-form-save").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain(text));
        providers.Dialogs.Find("#subject-aliases").GetAttribute("aria-invalid").Should().Be("true");
        providers.Dialogs.Find("#subject-name").GetAttribute("aria-invalid").Should().NotBe("true");
    }

    [Fact]
    public void SubjectDialog_ANameTypedAndNotConfirmed_IsNotLostBySave()
    {
        var providers = RenderProviders();
        var page = RenderList();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.Find(".app-page-header button").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#subject-name"));
        providers.Dialogs.Find("#subject-name").Change("Informática");
        providers.Dialogs.Find("#subject-aliases").Change("Tecnologia");
        providers.Dialogs.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post));
        LastSubjectRequest(HttpMethod.Post).Aliases.Should().Equal("Tecnologia");
    }

    // AC2 (topic side): the edit dialog shows the topic's stored names, and a rename alone sends null.
    [Fact]
    public void TopicEdit_ShowsTheStoredNames_AndARenameAloneSendsAliasesNull()
    {
        Topics.Clear();
        Topics.AddRange([TopicWithAliases, Punctuation]);
        var providers = RenderProviders();
        var page = RenderDetail();
        page.WaitForAssertion(() => page.FindAll(".app-item-row-content").Should().HaveCount(2));

        page.Find("button[aria-label='Edit: Crase']").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#topic-aliases"));
        providers.Dialogs.FindAll("#topic-aliases-field .app-chip-text").Select(text => text.TextContent.Trim())
            .Should().Equal("Acento grave");
        providers.Dialogs.Find("#topic-name").Change("Crase obrigatória");
        providers.Dialogs.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Put));
        LastTopicRequest(HttpMethod.Put).Aliases.Should().BeNull();
    }

    [Fact]
    public void TopicAdd_WithAName_SendsTheSet_AndARefusalShowsOnTheAliasField()
    {
        TaxonomyWriteFailure = (HttpStatusCode.Conflict, CatalogErrorCodes.TopicAliasTaken);
        var providers = RenderProviders();
        var page = RenderDetail();
        page.WaitForAssertion(() => page.FindAll(".app-item-row-content").Should().HaveCount(2));

        page.Find("#topic-add").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#topic-name"));
        providers.Dialogs.Find("#topic-name").Change("Acentuação");
        Enter(providers.Dialogs.Find("#topic-aliases"), "Acentos");
        providers.Dialogs.Find(".app-form-save").Click();

        providers.Dialogs.WaitForAssertion(() =>
            providers.Dialogs.Markup.Should().Contain("Another topic of this subject already uses one of these names"));
        providers.Dialogs.Find("#topic-aliases").GetAttribute("aria-invalid").Should().Be("true");
        LastTopicRequest(HttpMethod.Post).Aliases.Should().Equal("Acentos");
    }
}
