using System.ComponentModel.DataAnnotations;

namespace Simulab.AppHost.Tests;

/// <summary>F-94 BR2 (a), BR9: what the scanner reports on sample options classes.</summary>
public class RequiredOptionsScannerTests
{
    private static List<RequiredOptionsScanner.RequiredOption> FindIn(params Type[] types) =>
        RequiredOptionsScanner.Find([typeof(RequiredOptionsScannerTests).Assembly])
            .Where(option => types.Contains(option.Type))
            .ToList();

    /// <summary>AC3: a required property with no default is reported, with its full key.</summary>
    [Fact]
    public void Find_RequiredPropertyWithNoDefault_IsReportedWithItsKey()
    {
        var found = FindIn(typeof(SampleClientOptions));

        found.Should().ContainSingle().Which.FullKeys.Should().BeEquivalentTo("Sample:Client:ClientId");
    }

    /// <summary>AC4: a required property with a code default is its own source.</summary>
    [Fact]
    public void Find_RequiredPropertyWithADefault_IsNotReported()
    {
        FindIn(typeof(SampleDefaultedOptions)).Should().BeEmpty();
    }

    [Fact]
    public void Find_RequiredPropertyOfANestedObject_IsReportedWithTheNestedKey()
    {
        var found = FindIn(typeof(SampleNestedOptions));

        found.Should().ContainSingle().Which.FullKeys.Should().BeEquivalentTo("Sample:Nested:Inner:Endpoint");
    }

    [Fact]
    public void Find_ClassWithoutASectionName_IsNotAnOptionsClass()
    {
        FindIn(typeof(SampleNoSectionOptions)).Should().BeEmpty();
    }

    public sealed class SampleClientOptions
    {
        public const string SectionName = "Sample:Client";

        [Required]
        public string ClientId { get; set; } = string.Empty;

        public string? Optional { get; set; }
    }

    public sealed class SampleDefaultedOptions
    {
        public const string SectionName = "Sample:Defaulted";

        [Required]
        public string Host { get; set; } = "localhost";
    }

    public sealed class SampleNestedOptions
    {
        public const string SectionName = "Sample:Nested";

        public SampleInnerOptions Inner { get; set; } = new();
    }

    public sealed class SampleInnerOptions
    {
        [Required]
        public string? Endpoint { get; set; }
    }

    public sealed class SampleNoSectionOptions
    {
        [Required]
        public string Value { get; set; } = string.Empty;
    }
}
