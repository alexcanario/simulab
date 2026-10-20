namespace Simulab.Catalog.Domain.Entities;

/// <summary>
/// The notice PDF an edition keeps (F-89): where it is stored, the name the curator gave it, its size and when it
/// was uploaded. A value of the edition, not an entity: an edition holds at most one, all four fields or none.
/// </summary>
public sealed record NoticeDocument(string BlobName, string FileName, long Size, DateTimeOffset UploadedAt);
