using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Tests;

/// <summary>F-42 BR1, BR4, AC1, AC2, AC10: the list the form offers and the exam stores, and how it is filtered and shown.</summary>
public class BrazilianStatesTests
{
    // AC1: the 27 states, in the order of the approved list, each as "<name> (<acronym>)".
    [Fact]
    public void All_IsTheTwentySevenStatesOfTheApprovedList_InThatOrder()
    {
        BrazilianStates.All.Select(state => state.DisplayName).Should().Equal(
            "Acre (AC)",
            "Alagoas (AL)",
            "Amapá (AP)",
            "Amazonas (AM)",
            "Bahia (BA)",
            "Ceará (CE)",
            "Distrito Federal (DF)",
            "Espírito Santo (ES)",
            "Goiás (GO)",
            "Maranhão (MA)",
            "Mato Grosso (MT)",
            "Mato Grosso do Sul (MS)",
            "Minas Gerais (MG)",
            "Pará (PA)",
            "Paraíba (PB)",
            "Paraná (PR)",
            "Pernambuco (PE)",
            "Piauí (PI)",
            "Rio de Janeiro (RJ)",
            "Rio Grande do Norte (RN)",
            "Rio Grande do Sul (RS)",
            "Rondônia (RO)",
            "Roraima (RR)",
            "Santa Catarina (SC)",
            "São Paulo (SP)",
            "Sergipe (SE)",
            "Tocantins (TO)");
    }

    [Fact]
    public void All_HasNoRepeatedAcronym_AndEveryAcronymIsTwoUpperCaseLetters()
    {
        BrazilianStates.All.Select(state => state.Acronym).Should().OnlyHaveUniqueItems()
            .And.OnlyContain(acronym => acronym.Length == 2 && acronym.All(char.IsAsciiLetterUpper));
    }

    // AC2: typing narrows the list by name or acronym, ignoring accents and case.
    [Theory]
    [InlineData("sp", new[] { "ES", "SP" })] // ESPIRITO SANTO contains "sp" too: the list narrows, it does not guess
    [InlineData("SP", new[] { "ES", "SP" })]
    [InlineData("paulo", new[] { "SP" })]
    [InlineData("sao", new[] { "SP" })]
    [InlineData("  São  ", new[] { "SP" })]
    [InlineData("mato", new[] { "MT", "MS" })]
    [InlineData("ceara", new[] { "CE" })]
    [InlineData("zzz", new string[0])]
    public void Search_NarrowsByNameOrAcronym_IgnoringAccentsAndCase(string term, string[] expected)
    {
        BrazilianStates.Search(term).Select(state => state.Acronym).Should().Equal(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Search_BlankTerm_OffersTheWholeList(string? term)
    {
        BrazilianStates.Search(term).Should().HaveCount(27);
    }

    [Theory]
    [InlineData("SP", "São Paulo (SP)")]
    [InlineData("sp", "São Paulo (SP)")]
    [InlineData("Sampa", "Sampa")]
    [InlineData("São Paulo", "São Paulo")]
    [InlineData(null, null)]
    public void Display_ReadsAnAcronymAsNameAndAcronym_AndAnythingElseAsStored(string? stored, string? expected)
    {
        BrazilianStates.Display(stored).Should().Be(expected);
    }

    [Fact]
    public void FindByAcronym_IgnoresCaseAndSurroundingSpaces()
    {
        BrazilianStates.FindByAcronym("  rj ")!.Name.Should().Be("Rio de Janeiro");
        BrazilianStates.FindByAcronym("Rio de Janeiro").Should().BeNull();
        BrazilianStates.FindByAcronym(null).Should().BeNull();
    }
}
