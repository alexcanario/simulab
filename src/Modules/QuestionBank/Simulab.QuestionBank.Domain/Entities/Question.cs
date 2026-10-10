using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Domain.Entities;

/// <summary>
/// A single-choice question in the bank (F-95, BR1 to BR12): a statement, an optional base text, from 0 to 5
/// options with at most one marked as the key once published, an optional explanation, a difficulty, a topic and
/// the language it is written in. Global data: <see cref="TenantEntity.TenantId"/> is null in v1.
/// <para>
/// The rules answer with a <see cref="Result"/> and never throw. A refused edit leaves the entity as it was:
/// the content is checked as a whole before anything is assigned (AC7).
/// </para>
/// </summary>
public sealed class Question : TenantEntity
{
    private readonly List<QuestionOption> _options = [];

    private Question()
    {
    }

    public QuestionType Type { get; private set; }

    public QuestionStatus Status { get; private set; }

    /// <summary>The topic the question belongs to; held by id, never by a foreign key (BR13).</summary>
    public Guid TopicId { get; private set; }

    /// <summary>The language the question is written in, canonical (BR7); never translated.</summary>
    public string ContentLanguage { get; private set; } = string.Empty;

    public string StatementHtml { get; private set; } = string.Empty;

    /// <summary>The statement without tags, for the list excerpt (BR10); derived on every write.</summary>
    public string StatementText { get; private set; } = string.Empty;

    /// <summary>The comparable form of <see cref="StatementText"/>: what the list search reads (BR10).</summary>
    public string NormalizedStatement { get; private set; } = string.Empty;

    public string? BaseTextHtml { get; private set; }

    public string? ExplanationHtml { get; private set; }

    public int? Difficulty { get; private set; }

    /// <summary>The options in position order.</summary>
    public IReadOnlyList<QuestionOption> Options => _options;

    public static Result<Question> Create(QuestionContent content) => throw new NotImplementedException();

    /// <summary>Replaces the content; a published question must stay complete (BR4).</summary>
    public Result Update(QuestionContent content) => throw new NotImplementedException();

    /// <summary>Draft to published; the status only (BR12).</summary>
    public Result Publish() => throw new NotImplementedException();

    /// <summary>Published to draft; the content is untouched (BR12).</summary>
    public Result Unpublish() => throw new NotImplementedException();

    /// <summary>Ok for a draft; <c>question.delete_requires_draft</c> for a published question (BR11).</summary>
    public Result EnsureDeletable() => throw new NotImplementedException();
}
