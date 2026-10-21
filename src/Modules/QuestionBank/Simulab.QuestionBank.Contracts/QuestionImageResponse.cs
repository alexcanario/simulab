namespace Simulab.QuestionBank.Contracts;

/// <summary>An image as stored (F-103). The curator's editor puts <paramref name="Url"/> in the text; the Web serves it.</summary>
/// <param name="Id">Identifier of the image.</param>
/// <param name="Url">The address the text holds: <c>/question-images/{id}</c>.</param>
/// <param name="ContentType">The type decided from the file's first bytes: image/png, image/jpeg, image/webp or image/gif.</param>
/// <param name="SizeBytes">The size of the file, in bytes.</param>
public sealed record QuestionImageResponse(Guid Id, string Url, string ContentType, long SizeBytes);
