using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-89: the kit's file field in each of its states, the long file name that wraps, and the size text.</summary>
public sealed class AppFileFieldTests : KitTestContext
{
    private const string LongName =
        "edital-de-abertura-do-concurso-publico-para-provimento-de-vagas-e-formacao-de-cadastro-de-reserva-retificado-2026-versao-final.pdf";

    private IRenderedComponent<AppFileField> RenderField(Action<ComponentParameterCollectionBuilder<AppFileField>>? more = null) =>
        Render<AppFileField>(parameters =>
        {
            parameters
                .Add(field => field.Id, "doc")
                .Add(field => field.Label, "Notice PDF")
                .Add(field => field.Accept, ".pdf,application/pdf")
                .Add(field => field.DownloadText, "Download")
                .Add(field => field.DownloadLabel, "Download edital.pdf (PDF)")
                .Add(field => field.ChooseText, "Choose PDF")
                .Add(field => field.ReplaceText, "Replace")
                .Add(field => field.RemoveText, "Remove")
                .Add(field => field.DiscardText, "Discard choice")
                .Add(field => field.RemovePendingText, "The file will be removed when you save.")
                .Add(field => field.KeepText, "Keep file");
            more?.Invoke(parameters);
        });

    [Fact]
    public void Empty_ShowsTheLabelTheInputAndChooseOnly()
    {
        var field = RenderField(parameters => parameters.Add(component => component.Hint, "PDF, up to 20 MB."));

        field.Find("label.app-field-label").GetAttribute("for").Should().Be("doc");
        var input = field.Find("input#doc");
        input.GetAttribute("type").Should().Be("file");
        input.GetAttribute("accept").Should().Be(".pdf,application/pdf");
        input.GetAttribute("aria-describedby").Should().Be("doc-description");
        input.GetAttribute("aria-invalid").Should().BeNull();
        field.Find("#doc-choose").TextContent.Should().Be("Choose PDF");
        field.Find("#doc-choose").GetAttribute("for").Should().Be("doc", "the button is a label that opens the input");
        field.Find("#doc-description").TextContent.Should().Contain("PDF, up to 20 MB.");
        field.FindAll("#doc-name, #doc-chosen, #doc-removing, #doc-replace, #doc-remove").Should().BeEmpty();
    }

    [Fact]
    public void Stored_ShowsNameSizeDownloadReplaceAndRemove()
    {
        var field = RenderField(parameters => parameters
            .Add(component => component.StoredName, "edital.pdf")
            .Add(component => component.StoredSizeText, "2.5 MB")
            .Add(component => component.DownloadHref, "/files/edital"));

        field.Find("#doc-name").TextContent.Should().Be("edital.pdf");
        field.Find("#doc-size").TextContent.Should().Be("2.5 MB");
        var download = field.Find("a#doc-download");
        download.GetAttribute("href").Should().Be("/files/edital");
        download.HasAttribute("download").Should().BeTrue();
        download.GetAttribute("aria-label").Should().StartWith("Download");
        field.Find("#doc-replace").GetAttribute("for").Should().Be("doc");
        field.FindAll("#doc-remove").Should().ContainSingle();
        field.FindAll("#doc-choose").Should().BeEmpty();
    }

    [Fact]
    public void Chosen_ShowsTheSentenceAndDiscard_AndDiscardRaisesItsCallback()
    {
        var discarded = 0;
        var field = RenderField(parameters => parameters
            .Add(component => component.ChosenText, "Chosen file, not saved yet: notice.pdf")
            .Add(component => component.OnDiscard, EventCallback.Factory.Create(this, () => discarded++)));

        field.Find("#doc-chosen").TextContent.Should().Contain("notice.pdf");
        field.Find("#doc-discard").Click();

        discarded.Should().Be(1);
    }

    [Fact]
    public void RemovePending_ShowsTheSentenceAndKeep_AndKeepRaisesItsCallback()
    {
        var kept = 0;
        var field = RenderField(parameters => parameters
            .Add(component => component.StoredName, "edital.pdf")
            .Add(component => component.RemovePending, true)
            .Add(component => component.OnKeep, EventCallback.Factory.Create(this, () => kept++)));

        field.Find("#doc-removing").TextContent.Should().Contain("will be removed");
        field.FindAll("#doc-name, #doc-remove, #doc-replace").Should().BeEmpty();
        field.Find("#doc-keep").Click();

        kept.Should().Be(1);
    }

    [Fact]
    public void Remove_RaisesItsCallback()
    {
        var removed = 0;
        var field = RenderField(parameters => parameters
            .Add(component => component.StoredName, "edital.pdf")
            .Add(component => component.OnRemove, EventCallback.Factory.Create(this, () => removed++)));

        field.Find("#doc-remove").Click();

        removed.Should().Be(1);
    }

    [Fact]
    public void Error_ReplacesTheHintAndMarksTheInputInvalid()
    {
        var field = RenderField(parameters => parameters
            .Add(component => component.Hint, "PDF, up to 20 MB.")
            .Add(component => component.Error, "This file is not a PDF."));

        field.Find("#doc-error").TextContent.Should().Be("This file is not a PDF.");
        field.Find("#doc-description").TextContent.Should().NotContain("up to 20 MB");
        field.Find("input#doc").GetAttribute("aria-invalid").Should().Be("true");
        field.Find(".app-file-field").ClassList.Should().Contain("app-field-error");
    }

    // The long-text state (rule: ui): a file name wider than the field sits in the kit element that wraps.
    [Fact]
    public void LongFileName_SitsInTheElementThatWraps()
    {
        var field = RenderField(parameters => parameters
            .Add(component => component.StoredName, LongName)
            .Add(component => component.StoredSizeText, "19.8 MB"));

        var name = field.Find("#doc-name");
        name.TextContent.Should().Be(LongName);
        name.ClassList.Should().Contain("app-file-name");
        name.GetAttribute("style").Should().BeNull("no inline width or nowrap: the stylesheet wraps it");
    }

    [Fact]
    public void ChoosingAFile_RaisesTheChosenCallbackWithThatFile()
    {
        IBrowserFile? received = null;
        var field = RenderField(parameters => parameters
            .Add(component => component.OnChosen, EventCallback.Factory.Create<IBrowserFile>(this, file => received = file)));

        field.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary([1, 2, 3], "notice.pdf"));

        received.Should().NotBeNull();
        received!.Name.Should().Be("notice.pdf");
        received.Size.Should().Be(3);
    }

    [Theory]
    [InlineData(0L, "0 B")]
    [InlineData(512L, "512 B")]
    [InlineData(1024L, "1 KB")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(1_048_576L, "1 MB")]
    [InlineData(2_621_440L, "2.5 MB")]
    [InlineData(20_971_520L, "20 MB")]
    public void FileSizeText_WritesBytesKilobytesAndMegabytesWithOneDecimal(long bytes, string expected)
    {
        FileSizeText.Format(bytes, CultureInfo.InvariantCulture).Should().Be(expected);
    }

    [Fact]
    public void FileSizeText_UsesTheReadersDecimalSeparator()
    {
        FileSizeText.Format(2_621_440, new CultureInfo("pt-BR")).Should().Be("2,5 MB");
    }
}
