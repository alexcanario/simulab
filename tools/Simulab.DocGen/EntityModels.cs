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
        var tables = Tables(model);
        foreach (var table in tables)
        {
            sb.Append(CultureInfo.InvariantCulture, $"    {Id(table.Table.Name)} {{\n");
            foreach (var column in table.Columns)
            {
                var flags = column.Property?.IsPrimaryKey() == true ? " PK" : column.Property?.IsForeignKey() == true ? " FK" : "";
                sb.Append(CultureInfo.InvariantCulture, $"        {Id(column.Column.StoreType)} {Id(column.Column.Name)}{flags}\n");
            }

            sb.Append("    }\n");
        }

        // Foreign key constraints only: the key-to-key link between types sharing a table (table splitting, an owned
        // type, JSON) is not a constraint, so it never draws a table related to itself; a real self-reference is one (F-25).
        var relations = tables
            .SelectMany(table => table.Table.ForeignKeyConstraints
                .Select(fk => $"    {Id(fk.PrincipalTable.Name)} ||--{(fk.MappedForeignKeys.First().IsUnique ? "||" : "}o")} {Id(table.Table.Name)} : \"{Id(fk.Name)}\"\n"))
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
        foreach (var table in Tables(model))
        {
            var header = table.Entities.Count == 1 ? "Entity" : "Entities";
            sb.Append(CultureInfo.InvariantCulture, $"\n## {table.Table.Name}\n\n{header}: {string.Join(", ", table.Entities.Select(e => $"`{e.ClrType.Name}`"))}");
            if (table.Entities[0].GetComment() is { } comment)
            {
                sb.Append(CultureInfo.InvariantCulture, $" — {comment}");
            }

            sb.Append("\n\n| Column | Type | Null | Key | Default | Notes |\n|---|---|---|---|---|---|\n");
            foreach (var column in table.Columns)
            {
                sb.Append(CultureInfo.InvariantCulture, $"| {column.Column.Name} | {column.Column.StoreType} | {(column.Column.IsNullable ? "yes" : "no")} | {KeyText(column.Property)} | {DefaultText(column.Property)} | {Notes(column)} |\n");
            }

            var indexes = table.Table.Indexes.OrderBy(i => i.Name, StringComparer.Ordinal).ToList();
            if (indexes.Count > 0)
            {
                sb.Append("\nIndexes:\n");
                foreach (var index in indexes)
                {
                    var unique = index.IsUnique ? " (unique" + (index.MappedIndexes.First().GetAreNullsDistinct() == false ? ", NULLS NOT DISTINCT" : "") + ")" : "";
                    sb.Append(CultureInfo.InvariantCulture, $"- `{index.Name}` on {string.Join(", ", index.Columns.Select(c => c.Name))}{unique}\n");
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>A database table with the entity types stored in it (principal first) and its columns in document order.</summary>
    private sealed record TableDoc(ITable Table, IReadOnlyList<IEntityType> Entities, IReadOnlyList<ColumnDoc> Columns);

    /// <summary>
    /// A column with the property that documents it (null for a JSON container), where that property comes from when it is
    /// not the principal's (`Owned: Address`), and the type a JSON container stores.
    /// </summary>
    private sealed record ColumnDoc(IColumn Column, IProperty? Property, string? Origin, ITypeBase? Json);

    // One entry per database table of the relational model, the model the migrations are built from: types that share a
    // table (an owned type with or without ToJson, a complex type, table splitting) are one table with all its columns (F-25).
    private static List<TableDoc> Tables(IModel model) =>
    [
        .. model.GetRelationalModel().Tables
            .Select(Describe)
            .Where(t => t.Entities.Count > 0)
            .OrderBy(t => t.Table.Name, StringComparer.Ordinal)
    ];

    private static TableDoc Describe(ITable table)
    {
        var types = table.EntityTypeMappings.Select(m => m.TypeBase).OfType<IEntityType>().Where(e => !e.IsMappedToJson()).Distinct().ToList();
        // The principal is the entity type that is not owned and is not linked key to key to another type of the table.
        var entities = types.Where(e => !e.IsOwned())
            .OrderBy(e => table.GetRowInternalForeignKeys(e).Any() || e.BaseType is not null ? 1 : 0)
            .ThenBy(e => e.ClrType.Name, StringComparer.Ordinal)
            .ToList();
        if (entities.Count == 0)
        {
            return new TableDoc(table, entities, []);
        }

        var principal = entities[0];
        var json = types.SelectMany(JsonTypes).ToDictionary(j => j.Column, j => j.Type, StringComparer.Ordinal);
        var columns = table.Columns
            .Select(column => json.TryGetValue(column.Name, out var stored)
                ? new ColumnDoc(column, null, null, stored)
                : Documented(column, principal))
            .OrderBy(c => c.Json is not null ? 3 : c.Property!.IsPrimaryKey() ? 0 : c.Origin is null ? 1 : 2)
            .ThenBy(c => c.Origin, StringComparer.Ordinal)
            .ThenBy(c => c.Column.Name, StringComparer.Ordinal)
            .ToList();
        return new TableDoc(table, entities, columns);
    }

    // A column shared by several types (the key under table splitting) is documented once, as the principal's.
    private static ColumnDoc Documented(IColumn column, IEntityType principal)
    {
        var properties = column.PropertyMappings.Select(m => m.Property).ToList();
        var property = properties.FirstOrDefault(p => p.DeclaringType == principal)
            ?? properties.OrderBy(p => Origin(p, principal), StringComparer.Ordinal).First();
        return new ColumnDoc(column, property, Origin(property, principal), null);
    }

    private static string? Origin(IProperty property, IEntityType principal) => property.DeclaringType switch
    {
        IComplexType complex => $"Complex: {complex.ClrType.Name}",
        IEntityType owned when owned.IsOwned() => $"Owned: {owned.ClrType.Name}",
        IEntityType other when other != principal => $"Entity: {other.ClrType.Name}",
        _ => null
    };

    /// <summary>The JSON container columns of <paramref name="entity"/>, with the owned or complex type each one stores.</summary>
    private static IEnumerable<(string Column, ITypeBase Type)> JsonTypes(IEntityType entity) =>
        entity.GetNavigations()
            .Where(n => !n.IsOnDependent && n.ForeignKey.IsOwnership && n.TargetEntityType.IsMappedToJson())
            .Select(n => (n.TargetEntityType.GetContainerColumnName()!, (ITypeBase)n.TargetEntityType))
            .Concat(JsonComplexTypes(entity));

    // Complex types stored as columns can hold a complex type stored as JSON, so the walk goes down until it meets one.
    private static IEnumerable<(string Column, ITypeBase Type)> JsonComplexTypes(ITypeBase type) =>
        type.GetComplexProperties().SelectMany(p => p.ComplexType.IsMappedToJson()
            ? [(p.ComplexType.GetContainerColumnName()!, (ITypeBase)p.ComplexType)]
            : JsonComplexTypes(p.ComplexType));

    private static string KeyText(IProperty? property) =>
        property is null
            ? ""
            : property.IsPrimaryKey()
                ? "PK"
                : property.IsForeignKey()
                    ? "FK → " + string.Join(", ", property.GetContainingForeignKeys().Select(f => f.PrincipalEntityType.GetTableName()).Distinct().Order(StringComparer.Ordinal))
                    : "";

    private static string Notes(ColumnDoc column)
    {
        if (column.Json is not null)
        {
            return JsonNote(column.Json);
        }

        var notes = new List<string>();
        if (column.Origin is not null)
        {
            notes.Add(column.Origin);
        }

        if (column.Property!.GetMaxLength() is { } max)
        {
            notes.Add($"max {max}");
        }

        if (column.Property.GetComment() is { } note)
        {
            notes.Add(note);
        }

        return string.Join("; ", notes);
    }

    // The JSON property names EF writes, without the key it synthesizes for the collection; a nested JSON type by name only.
    private static string JsonNote(ITypeBase target)
    {
        var members = target.GetProperties()
            .Where(p => !p.IsKey() && !p.IsForeignKey())
            .Select(p => p.GetJsonPropertyName() ?? p.Name)
            .Concat(target is IEntityType entity ? entity.GetNavigations().Where(n => !n.IsOnDependent).Select(n => n.Name) : [])
            .Concat(target.GetComplexProperties().Select(p => p.Name));
        return $"JSON: {target.ClrType.Name} ({string.Join(", ", members)})";
    }

    // The configured SQL default or value; a CLR default (Guid.Empty, 0) is not a database default.
    private static string DefaultText(IProperty? property)
    {
        if (property is null)
        {
            return "";
        }

        if (property.GetDefaultValueSql() is { } sql)
        {
            return sql;
        }

        var value = property.GetDefaultValue();
        if (value is null)
        {
            return "";
        }

        var clr = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        return clr.IsValueType && Equals(value, Activator.CreateInstance(clr)) ? "" : value.ToString() ?? "";
    }

    private static string Id(string text) => NotIdentifier().Replace(text, "_");

    [GeneratedRegex("(Module)?(Db)?Context$")]
    private static partial Regex ContextSuffix();

    [GeneratedRegex("[^A-Za-z0-9_]")]
    private static partial Regex NotIdentifier();
}
