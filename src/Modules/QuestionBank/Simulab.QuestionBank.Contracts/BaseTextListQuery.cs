namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// What the base text list asks the server for (F-102, BR11). <paramref name="Page"/> is zero-based and
/// <paramref name="PageSize"/> is capped. A list is a read, so a value that is not understood is ignored.
/// </summary>
/// <param name="Page">The zero-based page.</param>
/// <param name="PageSize">The rows per page, capped at <see cref="MaxPageSize"/>.</param>
/// <param name="Search">A word of the title, case and accents ignored.</param>
/// <param name="Language">Keep only this content language; an unknown value is ignored.</param>
public sealed record BaseTextListQuery(
    int Page = 0,
    int PageSize = BaseTextListQuery.DefaultPageSize,
    string? Search = null,
    string? Language = null)
{
    public const int DefaultPageSize = 25;

    /// <summary>The cap the rule `api-contracts` sets for every paged list.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The same query with its numbers brought inside the allowed range.</summary>
    public BaseTextListQuery Sanitized() => this with
    {
        Page = Math.Max(Page, 0),
        PageSize = Math.Clamp(PageSize, 1, MaxPageSize)
    };
}
