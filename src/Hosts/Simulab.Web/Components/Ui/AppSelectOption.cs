namespace Simulab.Web.Components.Ui;

/// <summary>One choice of an <c>AppSelectField</c>. <paramref name="Lang"/> marks text written in another language (a language's native name).</summary>
public sealed record AppSelectOption<TValue>(TValue Value, string Text, string? Lang = null);
