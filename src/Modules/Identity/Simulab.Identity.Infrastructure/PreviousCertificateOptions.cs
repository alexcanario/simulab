namespace Simulab.Identity.Infrastructure;

/// <summary>F-84 BR5: the bound on listing the versions of the certificates at start.</summary>
public sealed class PreviousCertificateOptions
{
    /// <summary>In total for both roles; a listing that takes longer is given up and the Api starts with the current certificates.</summary>
    public TimeSpan ListTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
