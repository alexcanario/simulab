namespace Simulab.Web.Components.Ui;

/// <summary>
/// One candidate of an <c>AppLookupField</c> (F-34): the line the reader picks, the smaller line under it,
/// and the id the page keeps. <paramref name="Secondary"/> is what tells two look-alike names apart.
/// </summary>
public sealed record AppLookupOption(Guid Id, string Text, string? Secondary = null);
