using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Simulab.DocGen;

/// <summary>Entity diagrams and data dictionaries from the EF model of every DbContext next to the tool.</summary>
internal static partial class EntityModels
{
    private static readonly string[] SkippedAssemblyPrefixes = ["Microsoft.", "System.", "Npgsql", "netstandard", "mscorlib"];

    /// <summary>
    /// Every DbContext in the assemblies next to the tool, built without opening a connection. A context with an
    /// <see cref="IDesignTimeDbContextFactory{TContext}"/> is built by it, as `dotnet ef` does, so the documented
    /// names follow the same conventions as the migrations (snake_case); otherwise with plain PostgreSQL options.
    /// </summary>
    public static IEnumerable<(string Module, IModel Model)> Load() =>
        // Every assembly next to the tool, except the framework and the providers: a referenced project the tool never
        // calls is not in GetReferencedAssemblies.
        Load(Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll")
            .Where(f => !SkippedAssemblyPrefixes.Any(s => Path.GetFileName(f).StartsWith(s, StringComparison.OrdinalIgnoreCase)))
            .Order(StringComparer.Ordinal)
            .SelectMany(LoadTypes)
            .ToList());

    /// <summary>The models of the DbContext types among <paramref name="types"/>, ordered by type name.</summary>
    public static IEnumerable<(string Module, IModel Model)> Load(IReadOnlyList<Type> types)
    {
        var contexts = types
            .Where(t => !t.IsAbstract && t != typeof(DbContext) && typeof(DbContext).IsAssignableFrom(t))
            .OrderBy(t => t.Name, StringComparer.Ordinal);
        foreach (var type in contexts)
        {
            using var context = Create(type, types);
            var model = context.GetService<IDesignTimeModel>().Model; // comments and other design-time facts live only here
            yield return (ModuleName(type.Name), model);
        }
    }

    private static DbContext Create(Type contextType, IEnumerable<Type> types)
    {
        var factoryInterface = typeof(IDesignTimeDbContextFactory<>).MakeGenericType(contextType);
        var factory = types.FirstOrDefault(t => !t.IsAbstract && factoryInterface.IsAssignableFrom(t));
        if (factory is not null)
        {
            var instance = Activator.CreateInstance(factory)!;
            return (DbContext)factoryInterface.GetMethod(nameof(IDesignTimeDbContextFactory<DbContext>.CreateDbContext))!.Invoke(instance, [Array.Empty<string>()])!;
        }

        var optionsType = typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType);
        var builder = (DbContextOptionsBuilder)Activator.CreateInstance(optionsType)!;
        builder.UseNpgsql("Host=localhost;Database=docgen;Username=docgen;Password=docgen");
        var constructor = contextType.GetConstructors().OrderBy(c => c.GetParameters().Length).First();
        var arguments = constructor.GetParameters()
            .Select(p => p.ParameterType.IsInstanceOfType(builder.Options) ? builder.Options : p.HasDefaultValue ? p.DefaultValue : null)
            .ToArray();
        return (DbContext)constructor.Invoke(arguments);
    }

    private static IEnumerable<Type> LoadTypes(string file)
    {
        Assembly assembly;
        try
        {
            assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
        }
        catch (BadImageFormatException)
        {
            return [];
        }
        catch (FileLoadException)
        {
            return [];
        }

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            types = [.. e.Types.OfType<Type>()];
        }

        return types;
    }

    /// <summary>The DbContext type name without `DbContext`, `Context` and a trailing `Module`: IdentityModuleDbContext -> Identity.</summary>
    public static string ModuleName(string contextTypeName) =>
        ContextSuffix().Replace(contextTypeName, "") is { Length: > 0 } name ? name : contextTypeName;

    public static string RenderEntities(string module, IModel model)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"# {module} — entities\n\nGenerated from the EF model. Do not edit.\n\n```mermaid\nerDiagram\n");
        foreach (var entity in Tables(model))
        {
            sb.Append(CultureInfo.InvariantCulture, $"    {Id(entity.GetTableName()!)} {{\n");
            foreach (var property in Columns(entity))
            {
                var flags = property.IsPrimaryKey() ? " PK" : property.IsForeignKey() ? " FK" : "";
                sb.Append(CultureInfo.InvariantCulture, $"        {ColumnType(property)} {Id(property.GetColumnName())}{flags}\n");
            }

            foreach (var json in JsonColumns(entity))
            {
                sb.Append(CultureInfo.InvariantCulture, $"        {Id(json.Column.StoreType)} {Id(json.Column.Name)}\n");
            }

            sb.Append("    }\n");
        }

        var relations = Tables(model)
            .SelectMany(entity => entity.GetForeignKeys()
                .Where(fk => fk.PrincipalEntityType.GetTableName() is not null)
                .Select(fk => $"    {Id(fk.PrincipalEntityType.GetTableName()!)} ||--{(fk.IsUnique ? "||" : "}o")} {Id(entity.GetTableName()!)} : \"{Id(fk.GetConstraintName() ?? "fk")}\"\n"))
            .Distinct()
            .OrderBy(line => line, StringComparer.Ordinal);
        foreach (var line in relations)
        {
            sb.Append(line);
        }

        sb.Append("```\n");
        return sb.ToString();
    }

    public static string RenderDictionary(string module, IModel model)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"# {module} — data dictionary\n\nGenerated from the EF model. Do not edit. Schema: `{model.GetDefaultSchema() ?? "public"}`.\n");
        foreach (var entity in Tables(model))
        {
            sb.Append(CultureInfo.InvariantCulture, $"\n## {entity.GetTableName()}\n\nEntity: `{entity.ClrType.Name}`");
            if (entity.GetComment() is { } comment)
            {
                sb.Append(CultureInfo.InvariantCulture, $" — {comment}");
            }

            sb.Append("\n\n| Column | Type | Null | Key | Default | Notes |\n|---|---|---|---|---|---|\n");
            foreach (var property in Columns(entity))
            {
                var key = property.IsPrimaryKey()
                    ? "PK"
                    : property.IsForeignKey()
                        ? "FK → " + string.Join(", ", property.GetContainingForeignKeys().Select(f => f.PrincipalEntityType.GetTableName()).Distinct().Order(StringComparer.Ordinal))
                        : "";
                var notes = new List<string>();
                if (property.GetMaxLength() is { } max)
                {
                    notes.Add($"max {max}");
                }

                if (property.GetComment() is { } note)
                {
                    notes.Add(note);
                }

                sb.Append(CultureInfo.InvariantCulture, $"| {property.GetColumnName()} | {property.GetColumnType()} | {(property.IsNullable ? "yes" : "no")} | {key} | {property.GetDefaultValueSql() ?? DefaultText(property)} | {string.Join("; ", notes)} |\n");
            }

            foreach (var json in JsonColumns(entity))
            {
                sb.Append(CultureInfo.InvariantCulture, $"| {json.Column.Name} | {json.Column.StoreType} | {(json.Column.IsNullable ? "yes" : "no")} |  |  | {JsonNote(json.Target)} |\n");
            }

            var indexes = entity.GetIndexes().OrderBy(i => i.GetDatabaseName(), StringComparer.Ordinal).ToList();
            if (indexes.Count > 0)
            {
                sb.Append("\nIndexes:\n");
                foreach (var index in indexes)
                {
                    var unique = index.IsUnique ? " (unique" + (index.GetAreNullsDistinct() == false ? ", NULLS NOT DISTINCT" : "") + ")" : "";
                    sb.Append(CultureInfo.InvariantCulture, $"- `{index.GetDatabaseName()}` on {string.Join(", ", index.Properties.Select(p => p.GetColumnName()))}{unique}\n");
                }
            }
        }

        return sb.ToString();
    }

    // An owned type mapped with ToJson reports its owner's table name but is stored in one of the owner's columns (B-12).
    // Leaving it out also drops the ownership foreign key, which would draw the owner's table related to itself.
    private static IEnumerable<IEntityType> Tables(IModel model) =>
        model.GetEntityTypes()
            .Where(e => e.GetTableName() is not null && !e.IsMappedToJson())
            .OrderBy(e => e.GetTableName(), StringComparer.Ordinal);

    private static IEnumerable<IProperty> Columns(IEntityType entity) =>
        entity.GetProperties().OrderBy(p => p.IsPrimaryKey() ? 0 : 1).ThenBy(p => p.GetColumnName(), StringComparer.Ordinal);

    /// <summary>The JSON container columns of <paramref name="entity"/>'s table, with the owned type each one stores.</summary>
    private static IEnumerable<(IColumn Column, IEntityType Target)> JsonColumns(IEntityType entity)
    {
        var table = entity.GetTableMappings().First().Table;
        return entity.GetNavigations()
            .Where(n => !n.IsOnDependent && n.ForeignKey.IsOwnership && n.TargetEntityType.IsMappedToJson())
            .Select(n => (Column: table.FindColumn(n.TargetEntityType.GetContainerColumnName()!)!, Target: n.TargetEntityType))
            .OrderBy(json => json.Column.Name, StringComparer.Ordinal);
    }

    // The JSON property names EF writes, without the key it synthesizes for the collection; a nested JSON type by name only.
    private static string JsonNote(IEntityType target)
    {
        var members = target.GetProperties()
            .Where(p => !p.IsKey() && !p.IsForeignKey())
            .Select(p => p.GetJsonPropertyName() ?? p.Name)
            .Concat(target.GetNavigations().Where(n => !n.IsOnDependent).Select(n => n.Name));
        return $"JSON: {target.ClrType.Name} ({string.Join(", ", members)})";
    }

    // A CLR default (Guid.Empty, 0) is not a database default: show only values that were configured.
    private static string DefaultText(IProperty property)
    {
        var value = property.GetDefaultValue();
        if (value is null)
        {
            return "";
        }

        var clr = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        return clr.IsValueType && Equals(value, Activator.CreateInstance(clr)) ? "" : value.ToString() ?? "";
    }

    private static string ColumnType(IProperty property) => NotIdentifier().Replace(property.GetColumnType() ?? property.ClrType.Name, "_");

    private static string Id(string text) => NotIdentifier().Replace(text, "_");

    [GeneratedRegex("(Module)?(Db)?Context$")]
    private static partial Regex ContextSuffix();

    [GeneratedRegex("[^A-Za-z0-9_]")]
    private static partial Regex NotIdentifier();
}
