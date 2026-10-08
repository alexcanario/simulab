using Simulab.SharedKernel.Entities;

namespace Simulab.Catalog.Domain.Entities;

/// <summary>
/// One entry of the fixed list of areas a subject may belong to (F-79, BR1). Rows come only from the
/// migration that seeds them, so there is no <c>Create</c>: a new area is a migration plus three resource
/// entries. Its name is never stored; the screen reads the resource key <c>Area.&lt;Code&gt;</c>.
/// </summary>
public sealed class Area : TenantEntity
{
    private Area()
    {
    }

    /// <summary>The stable code the resource keys are named after, such as <c>Law</c>.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Where the area sits in the list, starting at 1.</summary>
    public int DisplayOrder { get; private set; }
}
