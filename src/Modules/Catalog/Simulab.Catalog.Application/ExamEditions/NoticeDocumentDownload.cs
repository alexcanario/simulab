namespace Simulab.Catalog.Application.ExamEditions;

/// <summary>A notice PDF opened for download (F-89): its content and the name it is saved under. The caller disposes the stream.</summary>
public sealed record NoticeDocumentDownload(Stream Content, string FileName, long Size);
