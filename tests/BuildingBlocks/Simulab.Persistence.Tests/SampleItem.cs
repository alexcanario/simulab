using Simulab.SharedKernel.Entities;

namespace Simulab.Persistence.Tests;

/// <summary>A module entity, as a module would write it: no filter and no audit code of its own.</summary>
public sealed class SampleItem : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public void PlaceInTenant(Guid? tenantId) => TenantId = tenantId;
}
