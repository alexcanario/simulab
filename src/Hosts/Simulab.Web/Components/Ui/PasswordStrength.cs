namespace Simulab.Web.Components.Ui;

/// <summary>How close a password is to the server policy, for the strength bar.</summary>
public enum PasswordStrength
{
    Weak,
    Fair,
    Strong
}

/// <summary>
/// The same rules the API enforces (12 characters, uppercase, digit, symbol), shown while typing.
/// The UI measures for comfort; the API is the authority (rule: ui).
/// </summary>
public static class PasswordStrengthRules
{
    /// <summary>Minimum length the server requires.</summary>
    public const int MinimumLength = 12;

    public static PasswordStrength Of(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return PasswordStrength.Weak;
        }

        var met = 0;
        if (password.Length >= MinimumLength)
        {
            met++;
        }

        if (password.Any(char.IsUpper))
        {
            met++;
        }

        if (password.Any(char.IsDigit))
        {
            met++;
        }

        if (password.Any(character => !char.IsLetterOrDigit(character)))
        {
            met++;
        }

        return met switch
        {
            4 => PasswordStrength.Strong,
            >= 2 => PasswordStrength.Fair,
            _ => PasswordStrength.Weak
        };
    }

    /// <summary>True when the password meets every rule the server checks.</summary>
    public static bool Meets(string? password) => Of(password) == PasswordStrength.Strong;
}
