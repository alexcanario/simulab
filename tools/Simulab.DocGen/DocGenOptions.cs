using System.Text.Json;

namespace Simulab.DocGen;

/// <summary>Which documents to produce (docgen.json). Edit the file to add or drop one.</summary>
internal sealed record DocGenOptions(bool Entities, bool DataDictionary, bool Routes, bool Modules)
{
    public static DocGenOptions Read(string file)
    {
        if (!File.Exists(file))
        {
            return new(true, true, true, true);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(file));
        var json = document.RootElement;
        bool Get(string name) => !json.TryGetProperty(name, out var value) || value.GetBoolean();
        return new(Get("entities"), Get("dataDictionary"), Get("routes"), Get("modules"));
    }
}
