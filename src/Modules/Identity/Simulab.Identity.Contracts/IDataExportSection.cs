using System.Text.Json;

namespace Simulab.Identity.Contracts;

/// <summary>
/// One module's part of the file a user downloads (F-16 BR3, F-167 BR9): a top-level member named <see cref="Name"/>
/// beside <c>identity</c>. A module that keeps data about a person implements it in its own project and registers it;
/// Identity asks every registered section and never references a module's records.
/// </summary>
public interface IDataExportSection
{
    /// <summary>The member the section is written under, named after the module (<c>examEngine</c>).</summary>
    string Name { get; }

    /// <summary>
    /// The section for <paramref name="userId"/>, the token subject of the request and nothing else (F-16 BR1). A section
    /// that cannot be built throws, and the export fails as a whole (F-167 BR10).
    /// </summary>
    Task<JsonElement> GetAsync(Guid userId, CancellationToken cancellationToken);
}
