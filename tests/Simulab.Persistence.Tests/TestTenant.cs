using Simulab.SharedKernel.Security;

namespace Simulab.Persistence.Tests;

/// <summary>The tenant a test acts as. Null is the v1 default (global and B2C data).</summary>
public sealed class TestTenant : ICurrentTenant
{
    public Guid? TenantId { get; set; }
}
