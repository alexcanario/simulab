using Microsoft.EntityFrameworkCore.Metadata;

namespace Simulab.DocGen;

/// <summary>Every generated document, keyed by its path relative to docs/architecture/.</summary>
internal static class DocSet
{
    public static SortedDictionary<string, string> Generate(
        string root,
        string appName,
        DocGenOptions options,
        IEnumerable<(string Module, IModel Model)> models,
        IReadOnlyCollection<Type> toolTypes)
    {
        var generated = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (options.Modules)
        {
            generated["modules.md"] = ModuleMapDoc.Render(root, appName);
        }

        if (options.Entities || options.DataDictionary)
        {
            foreach (var (module, model) in models)
            {
                if (options.Entities)
                {
                    generated[$"{module}/schema.dbml"] = EntityModels.RenderSchema(module, model);
                }

                if (options.DataDictionary)
                {
                    generated[$"{module}/data-dictionary.md"] = EntityModels.RenderDictionary(module, model);
                }
            }
        }

        if (options.Routes)
        {
            foreach (var (area, text) in RouteMapDoc.Render(root))
            {
                generated[$"{area}/routes.md"] = text;
            }
        }

        if (options.Tools)
        {
            generated["tools.md"] = ToolCatalogueDoc.Render(toolTypes);
        }

        generated["README.md"] = IndexDoc.Render(appName, generated.Keys);
        return generated;
    }
}
