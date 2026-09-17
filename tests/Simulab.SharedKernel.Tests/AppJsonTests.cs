using System.Text.Json;
using Simulab.SharedKernel.Results;
using Simulab.SharedKernel.Serialization;

namespace Simulab.SharedKernel.Tests;

public class AppJsonTests
{
    private sealed record Sample(string Name, ErrorKind Kind, Guid? ParentId = null);

    [Fact]
    public void Enums_travel_as_strings_and_names_are_camel_case()
    {
        var json = JsonSerializer.Serialize(new Sample("x", ErrorKind.BusinessRule), AppJson.Options);

        json.Should().Be("""{"name":"x","kind":"BusinessRule"}""");
    }

    [Fact]
    public void Round_trip_keeps_the_values()
    {
        var parent = Guid.CreateVersion7();
        var json = JsonSerializer.Serialize(new Sample("x", ErrorKind.NotFound, parent), AppJson.Options);

        var back = JsonSerializer.Deserialize<Sample>(json, AppJson.Options);

        back.Should().Be(new Sample("x", ErrorKind.NotFound, parent));
    }

    [Fact]
    public void Configure_does_not_add_the_enum_converter_twice()
    {
        var options = AppJson.Configure(AppJson.Configure(new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        options.Converters.OfType<System.Text.Json.Serialization.JsonStringEnumConverter>().Should().ContainSingle();
    }
}
