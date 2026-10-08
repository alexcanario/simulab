namespace Simulab.Catalog.Contracts;

/// <summary>
/// One area of the fixed list a subject may belong to (F-79, BR1). The name is not here: the screen shows
/// the resource key <c>Area.&lt;Code&gt;</c> in the reader's language.
/// </summary>
public sealed record AreaResponse(Guid Id, string Code, int DisplayOrder);
