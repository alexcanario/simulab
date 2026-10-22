namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// One blank of a fill-in-the-blanks question as the form sends it (F-107, BR1): the number of its marker
/// <c>[[n]]</c> in the statement and the answers it accepts, plain text.
/// </summary>
/// <param name="Number">The marker's number, 1 to 99; the statement must hold <c>[[Number]]</c>.</param>
/// <param name="AcceptedAnswers">The accepted answers as typed; null counts as none. Each is trimmed and must not be empty.</param>
public sealed record SaveBlankRequest(int Number, IReadOnlyList<string>? AcceptedAnswers = null);
