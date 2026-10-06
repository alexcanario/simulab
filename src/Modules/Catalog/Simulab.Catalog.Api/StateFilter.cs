using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Api;

/// <summary>
/// How both exam lists read the <c>state</c> parameter (F-57 BR6): blank is no filter, a known acronym is read
/// ignoring case and surrounding spaces and handed on in its stored form, anything else is refused.
/// </summary>
internal static class StateFilter
{
    public static Error UnknownState { get; } = new(CatalogErrorCodes.ExamFilterUnknownState, ErrorKind.Validation);

    public static bool TryRead(string? value, out string? acronym)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            acronym = null;
            return true;
        }

        acronym = BrazilianStates.FindByAcronym(value)?.Acronym;
        return acronym is not null;
    }
}
