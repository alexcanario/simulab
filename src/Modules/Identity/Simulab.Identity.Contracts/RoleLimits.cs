namespace Simulab.Identity.Contracts;

/// <summary>
/// A role name's length (F-9, BR3), read by the Api (the authority), the EF mapping and the Web (comfort
/// checks). Counted after trimming.
/// </summary>
public static class RoleLimits
{
    public const int NameMinLength = 2;

    public const int NameMaxLength = 50;
}
