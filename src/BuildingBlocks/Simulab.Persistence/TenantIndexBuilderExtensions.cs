using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Simulab.Persistence;

public static class TenantIndexBuilderExtensions
{
    /// <summary>
    /// A unique index that includes a nullable <c>TenantId</c> must treat two nulls as equal, or two
    /// global rows with the same key both pass (ADR-0001, decision 8; Simulae bug #671).
    /// </summary>
    public static IndexBuilder<TEntity> IsUniquePerTenant<TEntity>(this IndexBuilder<TEntity> index)
    {
        ArgumentNullException.ThrowIfNull(index);
        return index.IsUnique().AreNullsDistinct(false);
    }
}
