namespace Simulab.QuestionBank.Application.Questions;

/// <summary>What a question save and a publish read of the base text it links (F-102, BR3, BR4).</summary>
/// <param name="Id">The base text.</param>
/// <param name="ContentLanguage">The language it is written in, canonical.</param>
/// <param name="TextHtml">The sanitized HTML a version snapshots.</param>
public sealed record LinkedBaseText(Guid Id, string ContentLanguage, string TextHtml);
