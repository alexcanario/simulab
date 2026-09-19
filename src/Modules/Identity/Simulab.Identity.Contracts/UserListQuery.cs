namespace Simulab.Identity.Contracts;

/// <summary>
/// What the user list asks for (F-9, UC5). <paramref name="Page"/> is zero-based; <paramref name="SortBy"/>
/// is <c>email</c> (the default) or <c>name</c>.
/// </summary>
public sealed record UserListQuery(
    int Page,
    int PageSize,
    string? Search = null,
    Guid? RoleId = null,
    string? SortBy = null,
    bool Descending = false)
{
    public const int MaxPageSize = 100;

    public const string SortByEmail = "email";

    public const string SortByName = "name";
}
