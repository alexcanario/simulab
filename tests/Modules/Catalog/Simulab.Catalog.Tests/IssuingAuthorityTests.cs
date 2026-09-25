using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Tests;

/// <summary>F-34 BR18 (v2): the rules the entity owns, answered with a Result and never thrown.</summary>
public class IssuingAuthorityTests
{
    private static Result<IssuingAuthority> Create(
        string? name = "Prefeitura Municipal de Guarulhos",
        string? acronym = "pmg",
        string? description = null,
        string? website = null) =>
        IssuingAuthority.Create(name, acronym, description, website);

    [Fact]
    public void Create_ValidData_TrimsAndUppercasesTheAcronym()
    {
        var result = Create(name: "  Prefeitura Municipal de Guarulhos  ", acronym: "  pmg ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Prefeitura Municipal de Guarulhos");
        result.Value.Acronym.Should().Be("PMG");
        result.Value.Id.Should().NotBe(Guid.Empty);
        result.Value.TenantId.Should().BeNull("catalog rows are global in v1");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    public void Create_BlankOrTooShortName_FailsWithNameRequired(string? name)
    {
        var result = Create(name: name);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(CatalogErrorCodes.IssuingAuthorityNameRequired);
    }

    [Fact]
    public void Create_NameLongerThanTheColumn_FailsWithNameTooLong()
    {
        var result = Create(name: new string('a', CatalogLimits.IssuingAuthorityNameMaxLength + 1));

        result.Error!.Code.Should().Be(CatalogErrorCodes.IssuingAuthorityNameTooLong);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("X")]
    public void Create_BlankOrTooShortAcronym_FailsWithAcronymRequired(string? acronym)
    {
        var result = Create(acronym: acronym);

        result.Error!.Code.Should().Be(CatalogErrorCodes.IssuingAuthorityAcronymRequired);
    }

    [Fact]
    public void Create_AcronymLongerThanTheColumn_FailsWithAcronymTooLong()
    {
        var result = Create(acronym: new string('a', CatalogLimits.IssuingAuthorityAcronymMaxLength + 1));

        result.Error!.Code.Should().Be(CatalogErrorCodes.IssuingAuthorityAcronymTooLong);
    }

    [Fact]
    public void Create_DescriptionLongerThanTheColumn_FailsWithDescriptionTooLong()
    {
        var result = Create(description: new string('a', CatalogLimits.IssuingAuthorityDescriptionMaxLength + 1));

        result.Error!.Code.Should().Be(CatalogErrorCodes.IssuingAuthorityDescriptionTooLong);
    }

    [Theory]
    [InlineData("guarulhos.sp.gov.br")]
    [InlineData("/guarulhos")]
    [InlineData("mailto:contato@guarulhos.sp.gov.br")]
    [InlineData("ftp://guarulhos.sp.gov.br")]
    public void Create_WebsiteThatIsNotAnAbsoluteWebAddress_FailsWithWebsiteInvalid(string website)
    {
        var result = Create(website: website);

        result.Error!.Code.Should().Be(CatalogErrorCodes.IssuingAuthorityWebsiteInvalid);
    }

    [Theory]
    [InlineData("https://www.guarulhos.sp.gov.br")]
    [InlineData("http://guarulhos.sp.gov.br/concursos")]
    public void Create_AnAbsoluteWebAddress_IsAccepted(string website)
    {
        var result = Create(website: website);

        result.IsSuccess.Should().BeTrue();
        result.Value.Website.Should().Be(website);
    }

    [Fact]
    public void Create_BlankOptionalFields_AreStoredAsNull()
    {
        var result = Create(description: "   ", website: "  ");

        result.Value.Description.Should().BeNull();
        result.Value.Website.Should().BeNull();
    }

    [Fact]
    public void Create_StoresTheNormalizedFormsTheIndexesRead()
    {
        var result = Create(name: "Prefeitura de São Paulo", acronym: "psp");

        result.Value.NormalizedName.Should().Be(CatalogText.Normalize("PREFEITURA DE SAO PAULO"));
        result.Value.NormalizedAcronym.Should().Be("PSP");
    }

    [Fact]
    public void Update_NewValues_ReplacesThemAndTheNormalizedForms()
    {
        var authority = Create().Value;

        var result = authority.Update("Ministério da Educação", "mec", "Since 1930", "https://www.gov.br/mec");

        result.IsSuccess.Should().BeTrue();
        authority.Name.Should().Be("Ministério da Educação");
        authority.Acronym.Should().Be("MEC");
        authority.NormalizedName.Should().Be(CatalogText.Normalize("MINISTERIO DA EDUCACAO"));
    }

    [Fact]
    public void Update_Refused_ChangesNothing()
    {
        var authority = Create(name: "Prefeitura Municipal de Guarulhos").Value;

        var result = authority.Update("A", "pmg", null, null);

        result.IsFailure.Should().BeTrue();
        authority.Name.Should().Be("Prefeitura Municipal de Guarulhos");
    }
}
