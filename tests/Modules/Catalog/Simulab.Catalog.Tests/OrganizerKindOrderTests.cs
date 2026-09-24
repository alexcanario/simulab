using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Tests;

/// <summary>
/// B-15: `kindOrder` is optional and degrades to null (the pre-fix behavior) on anything but the known kinds,
/// once each. F-34 added a fourth kind, so the rule is "every kind of the enum", never a count of three — the
/// last test reads the enum instead of naming them, so a fifth kind cannot make it pass by accident.
/// </summary>
public sealed class OrganizerKindOrderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_BlankValue_IsNull(string? value) => OrganizerKindOrder.Parse(value).Should().BeNull();

    [Fact]
    public void Parse_OneUnknownName_IsNull() =>
        OrganizerKindOrder.Parse("ExamBoard,CertifyingBody,University,NotAKind").Should().BeNull();

    [Fact]
    public void Parse_ARepeatedKind_IsNull() =>
        OrganizerKindOrder.Parse("ExamBoard,ExamBoard,University,PublicBody").Should().BeNull();

    [Fact]
    public void Parse_FewerThanEveryKind_IsNull() =>
        OrganizerKindOrder.Parse("ExamBoard,CertifyingBody,University").Should().BeNull();

    [Fact]
    public void Parse_EveryKindIgnoringCaseAndSpacing_IsTheOrderGiven()
    {
        var order = OrganizerKindOrder.Parse(" university , examboard ,CertifyingBody, publicbody ");

        order.Should().Equal(
            OrganizerKind.University,
            OrganizerKind.ExamBoard,
            OrganizerKind.CertifyingBody,
            OrganizerKind.PublicBody);
    }

    // F-34 BR4: the parser must keep asking for every value of the enum, whatever their number.
    [Fact]
    public void Parse_EveryKindOfTheEnum_IsAccepted()
    {
        var every = string.Join(',', Enum.GetValues<OrganizerKind>());

        OrganizerKindOrder.Parse(every).Should().Equal(Enum.GetValues<OrganizerKind>());
    }
}
