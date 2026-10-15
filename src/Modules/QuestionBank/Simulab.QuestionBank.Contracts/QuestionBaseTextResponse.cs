namespace Simulab.QuestionBank.Contracts;

/// <summary>The base text a question links, with its sanitized HTML, as the question form and the preview read it (F-102, BR2).</summary>
/// <param name="Id">The base text.</param>
/// <param name="Title">The title the curator gave it.</param>
/// <param name="TextHtml">The sanitized HTML that was stored.</param>
public sealed record QuestionBaseTextResponse(Guid Id, string Title, string TextHtml);
