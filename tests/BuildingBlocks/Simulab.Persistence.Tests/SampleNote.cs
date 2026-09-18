using Simulab.SharedKernel.Entities;

namespace Simulab.Persistence.Tests;

/// <summary>A second entity, added without touching the context: the filters must reach it too.</summary>
public sealed class SampleNote : TenantEntity
{
    public string Text { get; set; } = string.Empty;

    public void PlaceInTenant(Guid? tenantId) => TenantId = tenantId;
}
