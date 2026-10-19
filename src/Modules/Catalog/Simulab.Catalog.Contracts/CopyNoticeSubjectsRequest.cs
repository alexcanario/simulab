namespace Simulab.Catalog.Contracts;

/// <summary>What the copy dialog sends (F-86): the edition whose notice subjects are copied into the one of the route.</summary>
/// <param name="SourceEditionId">The source edition; null or missing is read as an edition that does not exist (BR7).</param>
public sealed record CopyNoticeSubjectsRequest(Guid? SourceEditionId = null);
