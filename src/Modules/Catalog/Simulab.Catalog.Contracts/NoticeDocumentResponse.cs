namespace Simulab.Catalog.Contracts;

/// <summary>
/// The notice PDF an edition keeps (F-89): what a caller may know about it. Never the blob name or any storage
/// address (BR8): the file is downloaded through the API.
/// </summary>
public sealed record NoticeDocumentResponse(string FileName, long Size, DateTimeOffset UploadedAt);
