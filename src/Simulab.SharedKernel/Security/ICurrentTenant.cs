namespace Simulab.SharedKernel.Security;

/// <summary>
/// The tenant of the current request. Null means global or B2C data, which is every row in v1
/// (ADR-0001, decision 7). The tenant query filter reads it.
/// </summary>
public interface ICurrentTenant
{
    Guid? TenantId { get; }
}
