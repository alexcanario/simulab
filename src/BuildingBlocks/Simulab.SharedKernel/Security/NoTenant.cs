namespace Simulab.SharedKernel.Security;

/// <summary>No tenant: only global and B2C rows are visible. The v1 default.</summary>
public sealed class NoTenant : ICurrentTenant
{
    public Guid? TenantId => null;
}
