using Simulab.SharedKernel.Entities;

namespace Simulab.QuestionBank.Domain.Entities;

/// <summary>
/// One blank of a fill-in-the-blanks question (F-107, BR1): a marker <c>[[n]]</c> of the statement and the answers it
/// accepts. Like an option it has no rules of its own: the question owns the list, writes it only through its own
/// methods, keeps one row per marker (a draft marker with no answer included) and keeps the number unique.
/// </summary>
public sealed class QuestionBlank : TenantEntity
{
    private List<string> _acceptedAnswers = [];

    private QuestionBlank()
    {
    }

    internal QuestionBlank(int number, int position, IEnumerable<string> acceptedAnswers)
    {
        Number = number;
        Position = position;
        _acceptedAnswers = [.. acceptedAnswers];
    }

    public Guid QuestionId { get; private set; }

    /// <summary>The number of its marker, 1 to 99; unique for the question.</summary>
    public int Number { get; private set; }

    /// <summary>Where its marker sits among the markers of the statement, from 0: the order the blanks are shown in.</summary>
    public int Position { get; private set; }

    /// <summary>The accepted answers, trimmed, in the order the curator wrote them; empty on a draft blank with none yet.</summary>
    public IReadOnlyList<string> AcceptedAnswers => _acceptedAnswers;

    internal void Set(int position, IEnumerable<string> acceptedAnswers)
    {
        Position = position;
        _acceptedAnswers = [.. acceptedAnswers];
    }
}
