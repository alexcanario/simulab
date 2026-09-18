using System.Text.Json;
using System.Text.Json.Serialization;

namespace Simulab.SharedKernel.Serialization;

/// <summary>
/// The one JSON configuration used by the API, by every HttpClient call and by the tests.
/// Never write <c>new JsonSerializerOptions</c> elsewhere.
/// </summary>
public static class AppJson
{
    // RS0030: this is the one place allowed to construct JsonSerializerOptions (rule: build-config).
#pragma warning disable RS0030
    public static JsonSerializerOptions Options { get; } = Configure(new JsonSerializerOptions(JsonSerializerDefaults.Web));
#pragma warning restore RS0030

    public static JsonSerializerOptions Configure(JsonSerializerOptions options)
    {
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        if (!options.Converters.OfType<JsonStringEnumConverter>().Any())
            options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
