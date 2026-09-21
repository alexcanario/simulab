namespace Simulab.Identity.Contracts;

/// <summary>
/// What the role history asks for (F-14, UC2, BR6). <paramref name="Page"/> is zero-based. The filters combine
/// with AND: <paramref name="RoleId"/> also matches user entries that added or removed that role;
/// <paramref name="Search"/> matches the target user's email or name; <paramref name="Days"/> is one of
/// <see cref="Periods"/>, or null for all time. Newest first unless <paramref name="Ascending"/>.
/// </summary>
public sealed record RoleChangeListQuery(
    int Page,
    int PageSize,
    Guid? RoleId = null,
    Guid? UserId = null,
    Guid? AuthorId = null,
    string? Search = null,
    int? Days = null,
    bool Ascending = false)
{
    public const int MaxPageSize = 100;

    public static readonly IReadOnlyList<int> Periods = [7, 30, 90];
}
