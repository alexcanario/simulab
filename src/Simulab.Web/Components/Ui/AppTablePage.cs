namespace Simulab.Web.Components.Ui;

/// <summary>One page of table rows and the total across all pages.</summary>
public sealed record AppTablePage<TItem>(IReadOnlyList<TItem> Items, int TotalItems);
