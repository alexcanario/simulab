using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Identity;

namespace Simulab.Web.Tests.Identity;

/// <summary>The public legal pages: the body, the version line and the draft notice.</summary>
public class LegalDocumentPageTests : IdentityPageTestContext
{
    [Fact]
    public void Terms_ShowsTitleVersionAndBody()
    {
        var page = Render<LegalDocumentPage>();

        page.Markup.Should().Contain("Terms of use");
        page.Markup.Should().Contain("Version 2026-v1");
        page.Markup.Should().Contain("<h1>Title</h1>");
    }

    [Fact]
    public void DraftVersion_ShowsTheDraftNotice()
    {
        Api.Terms = Document(LegalTopic.Terms, placeholder: true);

        var page = Render<LegalDocumentPage>();

        page.Markup.Should().Contain("Draft text");
    }

    [Fact]
    public void DocumentNotFound_ShowsTheErrorStateWithTryAgain()
    {
        Api.Terms = null;

        var page = Render<LegalDocumentPage>();

        page.Markup.Should().Contain("This document does not exist");
        page.Markup.Should().Contain("Try again");
    }
}
