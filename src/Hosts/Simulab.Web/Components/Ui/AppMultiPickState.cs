namespace Simulab.Web.Components.Ui;

/// <summary>F-75: where the options of an <see cref="AppMultiPickField"/> come from, and how far the load got.</summary>
public enum AppMultiPickState
{
    /// <summary>The options are there: the search is usable.</summary>
    Ready,

    /// <summary>The options are being loaded: the input is disabled and the description says so.</summary>
    Loading,

    /// <summary>The load failed: the input is disabled and the description offers another go.</summary>
    LoadFailed,

    /// <summary>The source has nothing to pick: the input is disabled and the description says so.</summary>
    Empty
}
