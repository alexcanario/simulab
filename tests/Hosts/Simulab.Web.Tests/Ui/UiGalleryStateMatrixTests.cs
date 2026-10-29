using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor;
using Simulab.Web.Components.Pages.Dev;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-189: each text kind of the gallery shows every state that applies to it, in its own block.</summary>
public sealed class UiGalleryStateMatrixTests : KitTestContext
{
    private static readonly string[] AllStates = ["Empty", "Filled", "Error", "Disabled", "Read-only", "Long value"];
    private static readonly string[] SearchStates = ["Empty", "Filled", "Long value"];

    private static readonly GalleryStateMatrix.Expectation[] Kinds =
    [
        new("Text area (AppTextAreaField)", "app-text-area-field", AllStates),
        new("E-mail (AppEmailField)", "app-email-field", AllStates),
        new("Web address (AppUrlField)", "app-url-field", AllStates),
        new("Code (AppCodeField)", "app-code-field", AllStates),
        new("One-time code (AppOtpField)", "app-otp-field", AllStates),
        new("Search (AppSearchField)", "app-search-field", SearchStates),
    ];

    private IElement RenderSection()
    {
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Environments.Development));
        Render<MudPopoverProvider>();

        var gallery = Render<UiGallery>(parameters => parameters.Add(page => page.SourceDelay, TimeSpan.Zero));

        return gallery.Find("#gallery-text-kinds");
    }

    [Fact]
    public void Read_Development_EachKindHasABlockStartedByItsOwnTitle()
    {
        var blocks = GalleryStateMatrix.Read(RenderSection());

        blocks.Select(block => block.Title).Should().BeEquivalentTo(Kinds.Select(kind => kind.Title));
    }

    [Theory]
    [InlineData("Text area (AppTextAreaField)")]
    [InlineData("E-mail (AppEmailField)")]
    [InlineData("Web address (AppUrlField)")]
    [InlineData("Code (AppCodeField)")]
    [InlineData("One-time code (AppOtpField)")]
    public void Problems_Development_KindWithSixStatesHoldsEachOfThem(string title)
    {
        var kind = Kinds.Single(candidate => candidate.Title == title);

        GalleryStateMatrix.Problems(RenderSection(), [kind]).Should().BeEmpty();
        kind.States.Should().HaveCount(6);
    }

    [Fact]
    public void Problems_Development_SearchHoldsEmptyFilledAndLongValue()
    {
        var kind = Kinds.Single(candidate => candidate.Title == "Search (AppSearchField)");

        GalleryStateMatrix.Problems(RenderSection(), [kind]).Should().BeEmpty();
        GalleryStateMatrix.Read(RenderSection()).Single(block => block.Title == kind.Title).Cells
            .Select(cell => cell.Caption).Should().Equal(SearchStates);
    }

    [Fact]
    public void Problems_CaptionRemovedFromOneKind_NamesTheKindAndTheState()
    {
        var section = (IElement)RenderSection().Clone(true);
        var otp = GalleryStateMatrix.Read(section).Single(block => block.Title == "One-time code (AppOtpField)");
        otp.Cells.Should().Contain(cell => cell.Caption == "Read-only");
        var caption = section.QuerySelectorAll("p.gallery-state-caption")
            .Where(p => p.TextContent == "Read-only")
            .Single(p => p.NextElementSibling!.ClassList.Contains("app-otp-field"));
        caption.NextElementSibling!.Remove();
        caption.Remove();

        GalleryStateMatrix.Problems(section, Kinds).Should().Equal("One-time code (AppOtpField): missing state Read-only");
    }

    [Fact]
    public void Problems_ControlOfAnotherKindUnderACaption_NamesTheKindAndTheState()
    {
        var section = (IElement)RenderSection().Clone(true);
        var caption = section.QuerySelectorAll("p.gallery-state-caption")
            .Where(p => p.TextContent == "Disabled")
            .First(p => p.NextElementSibling!.ClassList.Contains("app-email-field"));
        caption.NextElementSibling!.ClassList.Remove("app-email-field");

        GalleryStateMatrix.Problems(section, Kinds).Should()
            .Equal("E-mail (AppEmailField): state Disabled is not followed by an app-email-field");
    }

    [Fact]
    public void Problems_ExpectedKindWithNoBlock_NamesTheKind()
    {
        var section = RenderSection();

        GalleryStateMatrix.Problems(section, [new("Date (AppDateField)", "app-date-field", AllStates)]).Should()
            .Equal("Date (AppDateField): no block with that title");
    }
}
