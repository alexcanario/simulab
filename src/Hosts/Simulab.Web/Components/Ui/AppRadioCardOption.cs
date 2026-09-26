namespace Simulab.Web.Components.Ui;

/// <summary>
/// One card of an <see cref="AppRadioCards{TValue}"/> (F-43, BR1): the choice itself, the line that explains it
/// and an optional <see cref="AppIcons"/> constant.
/// </summary>
/// <param name="Value">The value this card selects.</param>
/// <param name="Text">The choice, already translated.</param>
/// <param name="Description">One line saying what choosing it means. Never a rule; rules live in the hint.</param>
/// <param name="Icon">An <see cref="AppIcons"/> constant, or null for a card with no icon.</param>
public sealed record AppRadioCardOption<TValue>(TValue Value, string Text, string Description, string? Icon = null);
