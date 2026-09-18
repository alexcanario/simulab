namespace Simulab.Web.Components.Ui;

/// <summary>What a table asks the server for.</summary>
/// <param name="Page">Zero-based page index.</param>
/// <param name="PageSize">Items per page.</param>
/// <param name="Search">Search term from the table toolbar, or null.</param>
/// <param name="SortBy">Sorted column's property name, or null.</param>
/// <param name="Descending">Sort direction.</param>
public sealed record AppTableQuery(int Page, int PageSize, string? Search, string? SortBy, bool Descending);
