namespace Simulab.Identity.Contracts;

/// <summary>F-8 BR2, shared by the Api (the authority) and the Web (comfort check while typing).</summary>
public static class ProfileLimits
{
    /// <summary>The column's length (F-4); a longer name is refused, not cut.</summary>
    public const int FullNameMaxLength = 120;
}
