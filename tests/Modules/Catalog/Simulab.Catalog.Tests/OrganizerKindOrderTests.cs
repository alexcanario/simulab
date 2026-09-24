using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Tests;

/// <summary>B-15: `kindOrder` is optional and degrades to null (the pre-fix behavior) on anything but the three known kinds, once each.</summary>
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
    public void Parse_FewerThanTheThreeKinds_IsNull() =>
        OrganizerKindOrder.Parse("ExamBoard,CertifyingBody").Should().BeNull();

    [Fact]
    public void Parse_TheThreeKindsIgnoringCaseAndSpacing_IsTheOrderGiven()
    {
        var order = OrganizerKindOrder.Parse(" university , examboard ,CertifyingBody");

        order.Should().Equal(OrganizerKind.University, OrganizerKind.ExamBoard, OrganizerKind.CertifyingBody);
    }
}
