using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-89 on the edition form (<c>/admin/exams/{examId}/editions/{id}</c>): the "Notice PDF" field of the notice
/// section. The file is chosen on the form and sent only by Save, after the fields (BR5); the Api runs every rule
/// (BR3, BR4), so these tests say what the page shows, what it sends and in which order.
/// </summary>
public sealed class ExamEditionNoticeDocumentTests : CatalogPageTestContext
{
    private const string FieldId = "edition-notice-document";
    private const long TwentyMegabytes = 20L * 1024 * 1024;

    private static readonly Guid SavedEditionId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000f2");

    private static readonly DateTimeOffset UploadedAt = new(2026, 9, 28, 10, 0, 0, TimeSpan.Zero);

    private static readonly ExamEditionResponse EditionWithoutFile = new(
        SavedEditionId,
        AgentePf.Id,
        Cebraspe.Id,
        Cebraspe.Name,
        Cebraspe.Acronym,
        2026,
        "Guarda Municipal",
        null,
        null,
        null,
        ExamEditionStatus.Draft);

    private static readonly ExamEditionResponse EditionWithFile =
        EditionWithoutFile with { NoticeDocument = new NoticeDocumentResponse("edital.pdf", 2_621_440, UploadedAt) };

    private static string DocumentRoute(Guid editionId) => $"/api/v1/catalog/exams/{AgentePf.Id}/editions/{editionId}/notice-document";

    private static byte[] PdfBytes(long size = 64)
    {
        var bytes = new byte[size];
        "%PDF-1.7\n"u8.CopyTo(bytes);
        return bytes;
    }

    private IRenderedComponent<ExamEditionForm> RenderAdd() =>
        Render<ExamEditionForm>(parameters => parameters.Add(form => form.ExamId, AgentePf.Id));

    private IRenderedComponent<ExamEditionForm> RenderEdit(ExamEditionResponse edition)
    {
        Api.Editions.Add(edition);
        var page = Render<ExamEditionForm>(parameters => parameters
            .Add(form => form.ExamId, AgentePf.Id)
            .Add(form => form.Id, edition.Id));
        page.WaitForAssertion(() => page.FindAll("#edition-organizer").Should().ContainSingle());
        return page;
    }

    private static void Choose(IRenderedComponent<ExamEditionForm> page, string fileName, byte[]? bytes = null) =>
        page.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(bytes ?? PdfBytes(), fileName));

    private static void FillTheRequired(IRenderedComponent<ExamEditionForm> page)
    {
        var lookup = page.FindComponents<Simulab.Web.Components.Ui.AppLookupField>().Single(component => component.Instance.Id == "edition-organizer");
        page.InvokeAsync(() => lookup.Instance.ValueChanged.InvokeAsync(
            new Simulab.Web.Components.Ui.AppLookupOption(Cebraspe.Id, $"{Cebraspe.Name} ({Cebraspe.Acronym})"))).GetAwaiter().GetResult();
        page.Find("#edition-notice-year").Change("2026");
    }

    private IReadOnlyList<(HttpMethod Method, string Path)> Calls() =>
        [.. Api.Received.Where(call => call.Method != HttpMethod.Get).Select(call => (call.Method, call.Path))];

    private IReadOnlyList<string> DocumentCalls() =>
        [.. Api.Received.Where(call => call.Path.EndsWith("/notice-document", StringComparison.Ordinal)).Select(call => $"{call.Method} {call.Path}")];

    // The field sits in the notice section, beside the link: a hint, one way to choose, nothing stored yet.
    [Fact]
    public void Render_WithoutAFile_ShowsTheFieldInTheNoticeSectionWithItsHintAndChoose()
    {
        var page = RenderEdit(EditionWithoutFile);

        var notice = page.Find("section[aria-labelledby='edition-section-notice-title']");
        notice.QuerySelectorAll($"#{FieldId}").Should().ContainSingle();
        notice.QuerySelector($"#{FieldId}")!.GetAttribute("type").Should().Be("file");
        notice.QuerySelector($"#{FieldId}")!.GetAttribute("accept").Should().Contain(".pdf");
        notice.TextContent.Should().Contain("Notice PDF");
        page.Find($"#{FieldId}-description").TextContent.Should().Contain("PDF, up to 20 MB");
        page.Find($"#{FieldId}-choose").TextContent.Should().Be("Choose PDF");
        page.FindAll($"#{FieldId}-name, #{FieldId}-download, #{FieldId}-remove, #{FieldId}-discard").Should().BeEmpty();
    }

    // UC1 and UC3: with a stored file the form shows its name and size, a download link, Replace and Remove.
    [Fact]
    public void Render_WithAStoredFile_ShowsNameSizeDownloadReplaceAndRemove()
    {
        var page = RenderEdit(EditionWithFile);

        page.Find($"#{FieldId}-name").TextContent.Should().Contain("edital.pdf");
        page.Find($"#{FieldId}-size").TextContent.Should().Contain("2.5 MB");
        var download = page.Find($"a#{FieldId}-download");
        download.GetAttribute("href").Should().Be($"/catalog/exams/{AgentePf.Id}/editions/{SavedEditionId}/notice-document");
        download.GetAttribute("aria-label").Should().Contain("edital.pdf").And.Contain("PDF");
        page.Find($"#{FieldId}-replace").TextContent.Should().Be("Replace");
        page.Find($"#{FieldId}-remove").TextContent.Should().Be("Remove");
        page.FindAll($"#{FieldId}-choose").Should().BeEmpty();
    }

    // BR5: a chosen file is a change of the form, shown with its name and a way back; nothing is sent by choosing.
    [Fact]
    public void Choose_AFile_ShowsItsNameAndDiscard_AndSendsNothing()
    {
        var page = RenderEdit(EditionWithoutFile);
        var before = Api.Received.Count;

        Choose(page, "notice.pdf");

        page.WaitForAssertion(() => page.Find($"#{FieldId}-chosen").TextContent.Should().Contain("notice.pdf"));
        page.FindAll($"#{FieldId}-discard").Should().ContainSingle().Which.TextContent.Should().Be("Discard choice");
        Api.Received.Skip(before).Should().BeEmpty("choosing a file calls no endpoint");
    }

    [Fact]
    public void Discard_TheChosenFile_ShowsChooseAgainAndSendsNothing()
    {
        var page = RenderEdit(EditionWithoutFile);
        Choose(page, "notice.pdf");
        page.WaitForAssertion(() => page.FindAll($"#{FieldId}-discard").Should().ContainSingle());

        page.Find($"#{FieldId}-discard").Click();

        page.WaitForAssertion(() => page.FindAll($"#{FieldId}-chosen").Should().BeEmpty());
        page.FindAll($"#{FieldId}-choose").Should().ContainSingle();
        DocumentCalls().Should().BeEmpty();
    }

    // AC8 (UC1, BR5): a new edition with a chosen file - the edition is created first, then the file goes to it.
    [Fact]
    public void Save_ANewEditionWithAChosenFile_CreatesTheEditionThenUploadsTheFileToIt()
    {
        RenderProviders();
        var page = RenderAdd();
        page.WaitForAssertion(() => page.FindAll("#edition-organizer").Should().ContainSingle());
        FillTheRequired(page);
        Choose(page, "notice.pdf");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => DocumentCalls().Should().ContainSingle());
        var created = Api.Editions.Should().ContainSingle().Which;
        Calls().Select(call => $"{call.Method} {call.Path}").Should().Equal(
            $"POST /api/v1/catalog/exams/{AgentePf.Id}/editions",
            $"PUT {DocumentRoute(created.Id)}");
        Api.Received.Last(call => call.Method == HttpMethod.Put).Body.Should().Contain("filename=notice.pdf");
        page.WaitForAssertion(() => page.Find($"#{FieldId}-name").TextContent.Should().Contain("notice.pdf"));
        page.FindAll($"#{FieldId}-chosen").Should().BeEmpty("the file is stored now, no longer only chosen");
    }

    // AC3 (UC2): replacing sends the new file on Save, after the fields.
    [Fact]
    public void Save_AReplacementFile_SendsTheFieldsThenTheNewFile()
    {
        RenderProviders();
        var page = RenderEdit(EditionWithFile);
        Choose(page, "new.pdf");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => DocumentCalls().Should().ContainSingle());
        Calls().Select(call => $"{call.Method} {call.Path}").Should().Equal(
            $"PUT /api/v1/catalog/exams/{AgentePf.Id}/editions/{SavedEditionId}",
            $"PUT {DocumentRoute(SavedEditionId)}");
        page.WaitForAssertion(() => page.Find($"#{FieldId}-name").TextContent.Should().Contain("new.pdf"));
    }

    // AC4 (UC2): "Remove" only marks the file; Save deletes it, and nothing is uploaded.
    [Fact]
    public void Save_AfterRemove_DeletesTheFileAndUploadsNothing()
    {
        RenderProviders();
        var page = RenderEdit(EditionWithFile);

        page.Find($"#{FieldId}-remove").Click();
        page.WaitForAssertion(() => page.FindAll($"#{FieldId}-removing").Should().ContainSingle());
        DocumentCalls().Should().BeEmpty("Remove is a change of the form, not a call");
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => DocumentCalls().Should().ContainSingle());
        DocumentCalls().Single().Should().Be($"DELETE {DocumentRoute(SavedEditionId)}");
        page.WaitForAssertion(() => page.FindAll($"#{FieldId}-name").Should().BeEmpty());
        page.FindAll($"#{FieldId}-choose").Should().ContainSingle();
    }

    [Fact]
    public void Remove_ThenKeepTheFile_SendsNothingOnSave()
    {
        RenderProviders();
        var page = RenderEdit(EditionWithFile);
        page.Find($"#{FieldId}-remove").Click();
        page.WaitForAssertion(() => page.FindAll($"#{FieldId}-removing").Should().ContainSingle());

        page.Find($"#{FieldId}-keep").Click();

        page.WaitForAssertion(() => page.FindAll($"#{FieldId}-removing").Should().BeEmpty());
        page.Find($"#{FieldId}-name").TextContent.Should().Contain("edital.pdf");
    }

    // BR5: saving the fields alone never touches the stored file.
    [Fact]
    public void Save_OnlyTheFields_DoesNotCallTheNoticeDocumentRoutes()
    {
        RenderProviders();
        var page = RenderEdit(EditionWithFile);
        page.Find("#edition-position").Change("Guarda Municipal de 2ª Classe");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Calls().Should().ContainSingle());
        DocumentCalls().Should().BeEmpty();
        page.Find($"#{FieldId}-name").TextContent.Should().Contain("edital.pdf");
    }

    // AC7 (BR5): cancelling with a chosen file uploads nothing and leaves the stored file as it was.
    [Fact]
    public void Cancel_WithAChosenFile_UploadsNothingAndLeavesTheStoredFile()
    {
        var (dialogs, _) = RenderProviders();
        var page = RenderEdit(EditionWithFile);
        Choose(page, "new.pdf");
        page.WaitForAssertion(() => page.FindAll($"#{FieldId}-chosen").Should().ContainSingle());

        // A chosen file is an unsaved change, so leaving asks first (rule: ui); answering yes goes back.
        page.Find(".app-form-cancel").Click();
        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("Discard changes?"));
        dialogs.Find(".app-confirm-ok").Click();

        page.WaitForAssertion(() =>
            Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith($"/admin/exams/{AgentePf.Id}"));
        Calls().Should().BeEmpty("nothing was written");
        Api.Editions.Single().NoticeDocument!.FileName.Should().Be("edital.pdf");
    }

    // The Api refuses the file (BR3): the reason is on the field and in the panel, and the choice stays for another try.
    [Fact]
    public void Save_TheApiRefusesTheFileAsNotAPdf_ShowsItOnTheFieldAndInThePanelAndKeepsTheChoice()
    {
        RenderProviders();
        Api.Custom = (method, path, _, _) => method == HttpMethod.Put && path.EndsWith("/notice-document", StringComparison.Ordinal)
            ? FakeCatalogApi.Problem((HttpStatusCode.BadRequest, CatalogErrorCodes.ExamEditionNoticeDocumentNotPdf))
            : null;
        var page = RenderEdit(EditionWithoutFile);
        Choose(page, "notice.pdf");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find($"#{FieldId}-error").TextContent.Should().Contain("This file is not a PDF."));
        page.Find($"#{FieldId}").GetAttribute("aria-invalid").Should().Be("true");
        page.FindAll("#edition-error-summary .app-error-summary-list a").Select(link => link.GetAttribute("href"))
            .Should().Equal($"#{FieldId}");
        page.Find($"#{FieldId}-chosen").TextContent.Should().Contain("notice.pdf");
    }

    // BR4: a file over 20 MB is refused on the form before any byte leaves for the Api (comfort; the Api is the authority).
    [Fact]
    public void Choose_AFileOverTwentyMegabytes_IsRefusedOnTheFieldAndNothingIsSent()
    {
        var page = RenderEdit(EditionWithoutFile);
        var before = Api.Received.Count;

        Choose(page, "huge.pdf", PdfBytes(TwentyMegabytes + 1));

        page.WaitForAssertion(() => page.Find($"#{FieldId}-error").TextContent.Should().Contain("The file is over 20 MB."));
        page.FindAll($"#{FieldId}-chosen").Should().BeEmpty();
        Api.Received.Skip(before).Should().BeEmpty();
    }
}
