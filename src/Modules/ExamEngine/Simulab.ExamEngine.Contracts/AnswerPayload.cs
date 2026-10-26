using System.Text.Json.Serialization;

namespace Simulab.ExamEngine.Contracts;

/// <summary>
/// The answer to one question, with the shape of its type (F-166, BR5). The <c>type</c> member names the shape. Options
/// and matching items are told by their position in the question's version, counted from 0.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SingleChoicePayload), nameof(AttemptQuestionType.SingleChoice))]
[JsonDerivedType(typeof(TrueFalsePayload), nameof(AttemptQuestionType.TrueFalse))]
[JsonDerivedType(typeof(MultipleAnswerPayload), nameof(AttemptQuestionType.MultipleAnswer))]
[JsonDerivedType(typeof(MatchingPayload), nameof(AttemptQuestionType.Matching))]
[JsonDerivedType(typeof(FillInTheBlanksPayload), nameof(AttemptQuestionType.FillInTheBlanks))]
[JsonDerivedType(typeof(ShortAnswerPayload), nameof(AttemptQuestionType.ShortAnswer))]
[JsonDerivedType(typeof(OpenAnswerPayload), nameof(AttemptQuestionType.OpenAnswer))]
[JsonDerivedType(typeof(EssayPayload), nameof(AttemptQuestionType.Essay))]
public abstract record AnswerPayload;

/// <summary>One option of the version.</summary>
public sealed record SingleChoicePayload(int? OptionPosition = null) : AnswerPayload;

/// <summary>True ("certo") or false ("errado").</summary>
public sealed record TrueFalsePayload(bool? Value = null) : AnswerPayload;

/// <summary>One or more options of the version.</summary>
public sealed record MultipleAnswerPayload(IReadOnlyList<int>? OptionPositions = null) : AnswerPayload;

/// <summary>For each left item, at most one right option.</summary>
public sealed record MatchingPayload(IReadOnlyList<MatchingPairPayload>? Pairs = null) : AnswerPayload;

/// <summary>A left item and the right option the student matched to it.</summary>
public sealed record MatchingPairPayload(int? ItemPosition = null, int? OptionPosition = null);

/// <summary>For each blank number of the version, a text.</summary>
public sealed record FillInTheBlanksPayload(IReadOnlyList<BlankTextPayload>? Blanks = null) : AnswerPayload;

/// <summary>The text typed in one blank.</summary>
public sealed record BlankTextPayload(int? Number = null, string? Text = null);

/// <summary>A short text.</summary>
public sealed record ShortAnswerPayload(string? Text = null) : AnswerPayload;

/// <summary>A text of at most the question's size limit, in characters.</summary>
public sealed record OpenAnswerPayload(string? Text = null) : AnswerPayload;

/// <summary>A text of at most the question's size limit, in lines.</summary>
public sealed record EssayPayload(string? Text = null) : AnswerPayload;
