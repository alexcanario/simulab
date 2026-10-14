using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Simulab.AppHost.Tests;

/// <summary>
/// F-94 BR2 (a): finds, by reflection, the options classes (a class with a <c>public const string SectionName</c>) that
/// have a <c>[Required]</c> property whose code default is null or empty. A property with a default of its own is its own
/// source and is not reported.
/// </summary>
internal static class RequiredOptionsScanner
{
    /// <summary>One options class and the keys, relative to its section, that nothing but configuration can fill.</summary>
    internal sealed record RequiredOption(Type Type, string Section, IReadOnlyList<string> Keys)
    {
        /// <summary>The keys as the configuration spells them, <c>Section:Property</c>.</summary>
        internal IEnumerable<string> FullKeys => Keys.Select(key => $"{Section}:{key}");
    }

    /// <summary>Every options class of <paramref name="assemblies"/> with at least one required key that has no default.</summary>
    internal static IReadOnlyList<RequiredOption> Find(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var found = new List<RequiredOption>();
        foreach (var type in assemblies.SelectMany(LoadableTypes).Where(type => type.IsClass && !type.IsAbstract))
        {
            var section = SectionOf(type);
            if (section is null || type.GetConstructor(Type.EmptyTypes) is null)
            {
                continue;
            }

            var keys = new List<string>();
            Collect(Activator.CreateInstance(type)!, prefix: string.Empty, keys, depth: 0);
            if (keys.Count > 0)
            {
                found.Add(new RequiredOption(type, section, keys));
            }
        }

        return found;
    }

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    private static string? SectionOf(Type type)
    {
        var field = type.GetField("SectionName", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        return field is { IsLiteral: true } && field.FieldType == typeof(string) ? (string?)field.GetRawConstantValue() : null;
    }

    private static void Collect(object instance, string prefix, List<string> keys, int depth)
    {
        foreach (var property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || !property.CanRead)
            {
                continue;
            }

            var key = prefix + property.Name;
            var value = property.GetValue(instance);

            if (property.GetCustomAttribute<RequiredAttribute>() is not null && IsEmpty(value))
            {
                keys.Add(key);
            }

            // A nested settings object (not a string, a collection or a framework type) is read one level at a time.
            if (value is not null && depth < 3 && IsNestedSettings(property.PropertyType))
            {
                Collect(value, key + ":", keys, depth + 1);
            }
        }
    }

    private static bool IsEmpty(object? value) => value is null || (value is string text && string.IsNullOrWhiteSpace(text));

    private static bool IsNestedSettings(Type type) =>
        type.IsClass
        && type != typeof(string)
        && !typeof(System.Collections.IEnumerable).IsAssignableFrom(type)
        && type.Namespace is not null
        && !type.Namespace.StartsWith("System", StringComparison.Ordinal);
}
