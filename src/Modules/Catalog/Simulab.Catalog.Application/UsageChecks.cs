namespace Simulab.Catalog.Application;

/// <summary>
/// Asks every registered usage check of one kind whether an id is still used by a live row of its module
/// (F-96, BR8). The delete handlers of editions and notice subjects share it; each check interface has its own
/// <c>InUseAsync</c>, so the call is passed in and no base interface is added to the contracts.
/// </summary>
public static class UsageChecks
{
    /// <summary>True when any check says <paramref name="id"/> is in use.</summary>
    public static async Task<bool> AnyAsync<TCheck>(
        IEnumerable<TCheck> checks,
        Guid id,
        Func<TCheck, IReadOnlyCollection<Guid>, CancellationToken, Task<IReadOnlySet<Guid>>> inUse,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(checks);
        ArgumentNullException.ThrowIfNull(inUse);

        foreach (var check in checks)
        {
            if ((await inUse(check, [id], cancellationToken)).Contains(id))
            {
                return true;
            }
        }

        return false;
    }
}
