namespace Simulab.Identity.Contracts;

/// <summary>
/// What the account event trail asks for (F-21, UC2, BR10). <paramref name="Page"/> is zero-based. The filters
/// combine with AND: <paramref name="UserId"/> is one account, <paramref name="Event"/> one of
/// <see cref="AccountEventTypes.All"/>, <paramref name="IpAddress"/> one client address,
/// <paramref name="Search"/> matches the account's email or name, and <paramref name="Days"/> is one of
/// <see cref="Periods"/>, or null for all time. Newest first unless <paramref name="Ascending"/>.
/// </summary>
public sealed record AccountEventListQuery(
    int Page,
    int PageSize,
    Guid? UserId = null,
    string? Event = null,
    string? IpAddress = null,
    string? Search = null,
    int? Days = null,
    bool Ascending = false)
{
    public const int MaxPageSize = 100;

    public static readonly IReadOnlyList<int> Periods = [7, 30, 90];
}
