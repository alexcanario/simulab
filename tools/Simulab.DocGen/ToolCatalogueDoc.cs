using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Simulab.DocGen;

/// <summary>A tool declaration that fails <c>--check</c>: one line per problem, none of them a file.</summary>
internal sealed class ToolCatalogueException(IReadOnlyList<string> problems)
    : Exception($"tool catalogue: {problems.Count} problem(s)")
{
    public IReadOnlyList<string> Problems { get; } = problems;
}

/// <summary>
/// docs/architecture/tools.md (F-49): the tools the app offers to a model, found as methods carrying an attribute named
/// ModelToolAttribute in any namespace. Name, description and input schema come from Microsoft.Extensions.AI's
/// AIFunctionFactory, reached by reflection from the assembly the types came from — the app's own version, so the
/// catalogue shows what the model really receives. DocGen references neither that package nor the attribute type.
/// The tool class is never constructed: an instance method gets a factory delegate that only runs on invoke.
/// </summary>
internal static class ToolCatalogueDoc
{
    private sealed record Tool(string Name, string Description, string Method, JsonElement Schema, string[] Permissions, string[] Reaches, bool Writes, bool Confirm);

    private const string Abstractions = "Microsoft.Extensions.AI.Abstractions";
    private static readonly string[] SkippedAssemblyPrefixes = ["Microsoft.", "System.", "Npgsql", "netstandard", "mscorlib"];

    /// <summary>The types of the app's assemblies next to the tool, plus the Microsoft.Extensions.AI factory the schemas come from.</summary>
    public static IReadOnlyList<Type> Load() =>
        [.. Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll")
            .Where(f => Path.GetFileNameWithoutExtension(f) == Abstractions
                || !SkippedAssemblyPrefixes.Any(s => Path.GetFileName(f).StartsWith(s, StringComparison.OrdinalIgnoreCase)))
            .Order(StringComparer.Ordinal)
            .SelectMany(EntityModels.LoadTypes)];

    /// <summary>The catalogue of the tools in <paramref name="types"/>; an empty one when there is none.</summary>
    /// <exception cref="ToolCatalogueException">A tool without description, permissions or reaches, or two with one name.</exception>
    public static string Render(IReadOnlyCollection<Type> types)
    {
        var found = types
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Select(m => (Method: m, Attribute: m.GetCustomAttributes().FirstOrDefault(a => a.GetType().Name == "ModelToolAttribute")))
            .Where(x => x.Attribute is not null)
            .OrderBy(x => x.Method.DeclaringType!.FullName, StringComparer.Ordinal)
            .ThenBy(x => x.Method.Name, StringComparer.Ordinal)
            .ToList();
        if (found.Count == 0)
        {
            return Write([]);
        }

        var factory = types.FirstOrDefault(t => t.FullName == "Microsoft.Extensions.AI.AIFunctionFactory")
            ?? throw new InvalidOperationException(
                $"{found.Count} tool(s) found, but {Abstractions} is not next to the tool. Reference it from the project that holds the tools, or set \"tools\": false in docgen.json.");

        var problems = new List<string>();
        var tools = new List<Tool>();
        var describe = new Descriptor(factory);
        foreach (var (method, attribute) in found)
        {
            var where = $"{method.DeclaringType!.FullName}.{method.Name}";
            var (name, description, schema) = describe.Describe(method, Text(attribute!, "Name"));
            var permissions = Strings(attribute!, "Permissions");
            var reaches = Strings(attribute!, "Reaches");
            if (string.IsNullOrWhiteSpace(description))
            {
                problems.Add($"{where}: no description. Add [Description(\"...\")] to the method.");
            }

            MissingParameterDescriptions(schema, where, problems);
            if (permissions.Length == 0)
            {
                problems.Add($"{where}: no Permissions. Name them, or say the tool is open with a value of its own (for example [\"authenticated\"]).");
            }

            if (reaches.Length == 0)
            {
                problems.Add($"{where}: no Reaches. Name the systems and operations it reaches.");
            }

            tools.Add(new(name, description, where, schema, permissions, reaches, Flag(attribute!, "Writes"), Flag(attribute!, "Confirm")));
        }

        foreach (var clash in tools.GroupBy(t => t.Name, StringComparer.Ordinal).Where(g => g.Count() > 1))
        {
            problems.Add($"two or more tools are called \"{clash.Key}\": {string.Join(", ", clash.Select(t => t.Method))}. The model could not tell them apart.");
        }

        return problems.Count > 0 ? throw new ToolCatalogueException(problems) : Write([.. tools.OrderBy(t => t.Name, StringComparer.Ordinal)]);
    }

    // BR2: every parameter the model fills is described. A CancellationToken never reaches the schema.
    private static void MissingParameterDescriptions(JsonElement schema, string where, List<string> problems)
    {
        if (!schema.TryGetProperty("properties", out var properties) || properties.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in properties.EnumerateObject())
        {
            if (!property.Value.TryGetProperty("description", out var text) || string.IsNullOrWhiteSpace(text.GetString()))
            {
                problems.Add($"{where}: no description on parameter \"{property.Name}\". Add [Description(\"...\")] to it.");
            }
        }
    }

    private static string[] Strings(Attribute attribute, string property) =>
        attribute.GetType().GetProperty(property)?.GetValue(attribute) as string[] ?? [];

    private static string? Text(Attribute attribute, string property) =>
        attribute.GetType().GetProperty(property)?.GetValue(attribute) as string;

    private static bool Flag(Attribute attribute, string property) =>
        attribute.GetType().GetProperty(property)?.GetValue(attribute) is true;

    /// <summary>AIFunctionFactory reached by reflection: the app's assembly, never a reference of DocGen's own.</summary>
    private sealed class Descriptor
    {
        private readonly MethodInfo _createWithTarget;
        private readonly MethodInfo _createWithFactory;
        private readonly Type _optionsType;
        private readonly Delegate _neverCalled;

        public Descriptor(Type factory)
        {
            var overloads = factory.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == "Create").ToList();
            _createWithTarget = overloads.First(m => m.GetParameters() is [{ ParameterType.Name: "MethodInfo" }, { ParameterType.Name: "Object" }, _]);
            _createWithFactory = overloads.First(m => m.GetParameters() is [{ ParameterType.Name: "MethodInfo" }, { ParameterType.Name: "Func`2" }, _]);
            _optionsType = factory.Assembly.GetType("Microsoft.Extensions.AI.AIFunctionFactoryOptions")!;
            var argumentsType = factory.Assembly.GetType("Microsoft.Extensions.AI.AIFunctionArguments")!;
            var funcType = typeof(Func<,>).MakeGenericType(argumentsType, typeof(object));
            var parameter = Expression.Parameter(argumentsType, "arguments");
            var throws = Expression.Throw(Expression.New(typeof(InvalidOperationException)), typeof(object));
            _neverCalled = Expression.Lambda(funcType, throws, parameter).Compile();
        }

        public (string Name, string Description, JsonElement Schema) Describe(MethodInfo method, string? name)
        {
            var options = Activator.CreateInstance(_optionsType)!;
            if (!string.IsNullOrWhiteSpace(name))
            {
                _optionsType.GetProperty("Name")!.SetValue(options, name);
            }

            var function = method.IsStatic
                ? _createWithTarget.Invoke(null, [method, null, options])!
                : _createWithFactory.Invoke(null, [method, _neverCalled, options])!;
            var type = function.GetType();
            return (
                (string)type.GetProperty("Name")!.GetValue(function)!,
                (string?)type.GetProperty("Description")!.GetValue(function) ?? "",
                (JsonElement)type.GetProperty("JsonSchema")!.GetValue(function)!);
        }
    }

    private static string Write(List<Tool> tools)
    {
        var sb = new StringBuilder();
        sb.Append("# Tools offered to the model\n\nGenerated from the code. Do not edit.\n\n");
        sb.Append(CultureInfo.InvariantCulture, $"{tools.Count} tool{(tools.Count == 1 ? "" : "s")}. \"Confirms\" is whether the app asks the user before running it.\n\n");
        sb.Append("| Tool | Writes | Confirms | Permissions | Reaches |\n|---|---|---|---|---|\n");
        foreach (var tool in tools)
        {
            var confirms = tool.Writes && !tool.Confirm ? "⚠ no" : tool.Confirm ? "yes" : "n/a";
            sb.Append(CultureInfo.InvariantCulture, $"| [{tool.Name}](#{Anchor(tool.Name)}) | {(tool.Writes ? "yes" : "no")} | {confirms} | {Cell(tool.Permissions)} | {Cell(tool.Reaches)} |\n");
        }

        foreach (var tool in tools)
        {
            sb.Append(CultureInfo.InvariantCulture, $"\n## {tool.Name}\n\n{tool.Description}\n\n");
            sb.Append(CultureInfo.InvariantCulture, $"- Method: `{tool.Method}`\n");
            sb.Append(CultureInfo.InvariantCulture, $"- {(tool.Writes ? "Writes" : "Reads only")}{(tool.Writes && !tool.Confirm ? " — ⚠ writes without confirmation" : tool.Confirm ? " — asks the user first" : "")}\n");
            sb.Append(CultureInfo.InvariantCulture, $"- Permissions: {Cell(tool.Permissions)}\n");
            sb.Append(CultureInfo.InvariantCulture, $"- Reaches: {Cell(tool.Reaches)}\n\n");
            sb.Append(Parameters(tool.Schema));
            sb.Append(CultureInfo.InvariantCulture, $"<details>\n<summary>Input schema</summary>\n\n```json\n{Pretty(tool.Schema)}\n```\n\n</details>\n");
        }

        return sb.ToString();
    }

    private static string Parameters(JsonElement schema)
    {
        if (!schema.TryGetProperty("properties", out var properties) || properties.ValueKind != JsonValueKind.Object)
        {
            return "No parameters.\n\n";
        }

        var required = schema.TryGetProperty("required", out var r) && r.ValueKind == JsonValueKind.Array
            ? r.EnumerateArray().Select(e => e.GetString()).ToHashSet(StringComparer.Ordinal)
            : [];
        var sb = new StringBuilder("| Parameter | Type | Required | Description | Default |\n|---|---|---|---|---|\n");
        foreach (var property in properties.EnumerateObject())
        {
            var type = property.Value.TryGetProperty("type", out var t) ? SchemaType(t) : "—";
            var description = property.Value.TryGetProperty("description", out var d) ? Escape(d.GetString() ?? "") : "—";
            var fallback = property.Value.TryGetProperty("default", out var f) ? $"`{f.GetRawText()}`" : "—";
            sb.Append(CultureInfo.InvariantCulture, $"| `{property.Name}` | {type} | {(required.Contains(property.Name) ? "yes" : "no")} | {description} | {fallback} |\n");
        }

        return sb.Append('\n').ToString();
    }

    // A schema type is a string, or a list when the parameter is nullable: ["string", "null"].
    private static string SchemaType(JsonElement type) => type.ValueKind == JsonValueKind.Array
        ? string.Join(" or ", type.EnumerateArray().Select(e => e.GetString()))
        : type.GetString() ?? "—";

    // Indented through a writer, not a serializer: `new JsonSerializerOptions()` is in BannedSymbols.txt. NewLine is set
    // so the text equals the file read back with its line endings normalised, or --check would never pass on Windows.
    private static string Pretty(JsonElement schema)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true, NewLine = "\n" }))
        {
            schema.WriteTo(writer);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static string Cell(string[] values) => values.Length == 0 ? "—" : string.Join(", ", values.Select(v => $"`{v}`"));

    private static string Escape(string text) => text.Replace("|", "\\|", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);

    private static string Anchor(string name) =>
        new([.. name.ToLowerInvariant().Select(c => c == ' ' ? '-' : c).Where(c => char.IsLetterOrDigit(c) || c is '-' or '_')]);
}
