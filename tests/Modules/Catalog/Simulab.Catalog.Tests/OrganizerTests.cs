using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain;
using Simulab.Catalog.Domain.Entities;

namespace Simulab.Catalog.Tests;

/// <summary>F-33 BR8, BR10, BR13: the rules the entity owns, answered with a Result and never thrown.</summary>
public class OrganizerTests
{
    private static Simulab.SharedKernel.Results.Result<Organizer> Create(
        string? name = "Centro Brasileiro de Pesquisa em Avaliacao",
        string? acronym = "cebraspe",
        OrganizerKind kind = OrganizerKind.ExamBoard,
        string? description = null,
        string? website = null) =>
        Organizer.Create(name, acronym, kind, description, website);

    [Fact]
    public void Create_ValidData_TrimsAndUppercasesTheAcronym()
    {
        var result = Create(name: "  Fundacao Getulio Vargas  ", acronym: "  fgv ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Fundacao Getulio Vargas");
        result.Value.Acronym.Should().Be("FGV");
        result.Value.Id.Should().NotBe(Guid.Empty);
        result.Value.TenantId.Should().BeNull("catalog rows are global in v1 (BR5)");
    }

    // AC14: a blank name fails with its code instead of throwing.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    public void Create_BlankOrTooShortName_FailsWithNameRequired(string? name)
    {
        var result = Create(name: name);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(CatalogErrorCodes.OrganizerNameRequired);
    }

    [Fact]
    public void Create_NameLongerThanTheColumn_FailsWithNameTooLong()
    {
        var result = Create(name: new string('a', CatalogLimits.OrganizerNameMaxLength + 1));

        result.Error!.Code.Should().Be(CatalogErrorCodes.OrganizerNameTooLong);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("X")]
    public void Create_BlankOrTooShortAcronym_FailsWithAcronymRequired(string? acronym)
    {
        var result = Create(acronym: acronym);

        result.Error!.Code.Should().Be(CatalogErrorCodes.OrganizerAcronymRequired);
    }

    [Fact]
    public void Create_AcronymLongerThanTheColumn_FailsWithAcronymTooLong()
    {
        var result = Create(acronym: new string('a', CatalogLimits.OrganizerAcronymMaxLength + 1));

        result.Error!.Code.Should().Be(CatalogErrorCodes.OrganizerAcronymTooLong);
    }

    [Fact]
    public void Create_UnknownKind_FailsWithKindInvalid()
    {
        var result = Create(kind: (OrganizerKind)42);

        result.Error!.Code.Should().Be(CatalogErrorCodes.OrganizerKindInvalid);
    }

    [Fact]
    public void Create_DescriptionLongerThanTheColumn_FailsWithDescriptionTooLong()
    {
        var result = Create(description: new string('a', CatalogLimits.OrganizerDescriptionMaxLength + 1));

        result.Error!.Code.Should().Be(CatalogErrorCodes.OrganizerDescriptionTooLong);
    }

    // AC11, BR10: only an absolute http or https address is an official site.
    [Theory]
    [InlineData("cebraspe.org.br")]
    [InlineData("/organizers/1")]
    [InlineData("mailto:contact@cebraspe.org.br")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://cebraspe.org.br")]
    public void Create_WebsiteThatIsNotAnAbsoluteWebAddress_FailsWithWebsiteInvalid(string website)
    {
        var result = Create(website: website);

        result.Error!.Code.Should().Be(CatalogErrorCodes.OrganizerWebsiteInvalid);
    }

    [Fact]
    public void Create_WebsiteLongerThanTheColumn_FailsWithWebsiteInvalid()
    {
        var tooLong = "https://" + new string('a', CatalogLimits.OrganizerWebsiteMaxLength) + ".org";

        Create(website: tooLong).Error!.Code.Should().Be(CatalogErrorCodes.OrganizerWebsiteInvalid);
    }

    [Theory]
    [InlineData("https://www.cebraspe.org.br")]
    [InlineData("http://vunesp.com.br/inscricoes")]
    public void Create_AbsoluteWebAddress_IsAccepted(string website)
    {
        Create(website: website).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_BlankOptionalFields_StoresNull()
    {
        var result = Create(description: "   ", website: "  ");

        result.Value.Description.Should().BeNull();
        result.Value.Website.Should().BeNull();
    }

    // BR9: the stored comparable form is what the unique index and the search read.
    [Fact]
    public void Create_AccentedName_StoresAnAccentFreeUppercaseNormalizedForm()
    {
        var result = Create(name: "Fundação Getúlio Vargas", acronym: "fgv");

        result.Value.NormalizedName.Should().Be("FUNDACAO GETULIO VARGAS");
        result.Value.NormalizedAcronym.Should().Be("FGV");
    }

    [Fact]
    public void Update_ValidData_ReplacesEveryFieldAndKeepsTheId()
    {
        var organizer = Create().Value;
        var id = organizer.Id;

        var result = organizer.Update("Vunesp", "vnsp", OrganizerKind.University, "A description", "https://vunesp.com.br");

        result.IsSuccess.Should().BeTrue();
        organizer.Id.Should().Be(id);
        organizer.Name.Should().Be("Vunesp");
        organizer.Acronym.Should().Be("VNSP");
        organizer.Kind.Should().Be(OrganizerKind.University);
        organizer.NormalizedName.Should().Be("VUNESP");
    }

    [Fact]
    public void Update_InvalidData_ChangesNothing()
    {
        var organizer = Create(name: "Fundacao Getulio Vargas", acronym: "fgv").Value;

        var result = organizer.Update(string.Empty, "fgv", OrganizerKind.ExamBoard, null, null);

        result.IsFailure.Should().BeTrue();
        organizer.Name.Should().Be("Fundacao Getulio Vargas");
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    [InlineData("Raciocínio Lógico", "RACIOCINIO LOGICO")]
    [InlineData("  são paulo  ", "SAO PAULO")]
    public void Normalize_StripsAccentsCaseAndSurroundingSpace(string? value, string expected)
    {
        CatalogText.Normalize(value).Should().Be(expected);
    }
}
