using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Domain;

/// <summary>
/// F-168 BR3, BR5 and BR6: a score or the points per question is a decimal inside its range with at most two decimal
/// places. PostgreSQL rounds a third place of a <c>numeric(…, 2)</c> column without a word, so the entity refuses it.
/// </summary>
public static class Scores
{
    /// <summary>True when <paramref name="value"/> is from <paramref name="min"/> to <paramref name="max"/> with at most two places.</summary>
    public static bool IsValid(decimal value, decimal min, decimal max) =>
        value >= min && value <= max && decimal.Round(value, CatalogLimits.ScoreDecimalPlaces) == value;
}
