using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Domain.Entities;

/// <summary>
/// A passage, table or case that several questions refer to (F-102, BR1 to BR3): a title the curator finds it
/// by, the language it is written in and its sanitized HTML. Global data: <see cref="TenantEntity.TenantId"/>
/// is null in v1. The rules answer with a <see cref="Result"/> and never throw.
/// </summary>
public sealed class BaseText : TenantEntity
{
    private BaseText()
    {
    }

    /// <summary>What the curator calls the text, trimmed, 1 to 200 characters (BR1).</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>The comparable form of <see cref="Title"/>: what the list search reads (BR11).</summary>
    public string NormalizedTitle { get; private set; } = string.Empty;

    /// <summary>The language the text is written in, canonical; never translated (BR1).</summary>
    public string ContentLanguage { get; private set; } = string.Empty;

    /// <summary>The sanitized HTML, never empty (BR1, BR8).</summary>
    public string TextHtml { get; private set; } = string.Empty;

    public static Result<BaseText> Create(string? title, string? contentLanguage, SanitizedHtml text) =>
        throw new NotImplementedException();

    /// <summary>
    /// Replaces the content. <paramref name="hasLinkedQuestions"/> locks the language (BR3). Nothing changes
    /// when it fails.
    /// </summary>
    public Result Update(string? title, string? contentLanguage, SanitizedHtml text, bool hasLinkedQuestions) =>
        throw new NotImplementedException();
}
