namespace Simulab.Web.Components.Ui;

/// <summary>One choice of an <c>AppSegmentedField</c>: a short word, never a sentence.</summary>
public sealed record AppSegmentOption<TValue>(TValue Value, string Text);
