namespace Simulab.Web.Components.Ui;

/// <summary>
/// What a status means, not what colour it is (F-43, BR1). The page says which tone a value carries; the kit
/// decides how it looks, so the same meaning reads the same on every screen.
/// </summary>
public enum AppStatusTone
{
    /// <summary>Nothing good or bad: a draft, an unknown, a plain label.</summary>
    Neutral,

    /// <summary>The healthy state: active, published, verified.</summary>
    Success,

    /// <summary>Waiting or expiring: pending, about to close.</summary>
    Warning,

    /// <summary>Broken or refused: locked, failed, cancelled.</summary>
    Error,

    /// <summary>Worth noticing, with no judgement: scheduled, in review.</summary>
    Info
}
