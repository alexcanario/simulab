using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Tests;

/// <summary>
/// B-15: `kindOrder` is optional and degrades to null (the pre-fix behavior) on anything but the known kinds,
/// once each. The rule is "every kind of the enum", never a count: the last test reads the enum instead of
/// naming them, so adding a kind cannot make it pass by accident (a lesson from F-34, which added one and
/// then, in v2, took it back out).
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
        OrganizerKindOrder.Parse("ExamBoard,CertifyingBody,NotAKind").Should().BeNull();

    [Fact]
    public void Parse_ARepeatedKind_IsNull() =>
        OrganizerKindOrder.Parse("ExamBoard,ExamBoard,University").Should().BeNull();

    [Fact]
    public void Parse_FewerThanEveryKind_IsNull() =>
        OrganizerKindOrder.Parse("ExamBoard,CertifyingBody").Should().BeNull();

    [Fact]
    public void Parse_EveryKindIgnoringCaseAndSpacing_IsTheOrderGiven()
    {
        var order = OrganizerKindOrder.Parse(" university , examboard ,CertifyingBody");

        order.Should().Equal(OrganizerKind.University, OrganizerKind.ExamBoard, OrganizerKind.CertifyingBody);
    }

    // The parser must keep asking for every value of the enum, whatever their number.
    [Fact]
    public void Parse_EveryKindOfTheEnum_IsAccepted()
    {
        var every = string.Join(',', Enum.GetValues<OrganizerKind>());

        OrganizerKindOrder.Parse(every).Should().Equal(Enum.GetValues<OrganizerKind>());
    }
}
