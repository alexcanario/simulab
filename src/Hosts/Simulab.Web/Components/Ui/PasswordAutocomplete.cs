namespace Simulab.Web.Components.Ui;

/// <summary>
/// Which password an <c>AppPasswordField</c> asks for, so a password manager fills the saved one or offers a
/// new one (B-5). There is no default: every page says which, and a page that does not fails the build.
/// </summary>
public enum PasswordAutocomplete
{
    /// <summary>The password the user already has (sign-in, current password): <c>current-password</c>.</summary>
    CurrentPassword,

    /// <summary>A password the user is choosing (sign-up, reset, new password): <c>new-password</c>.</summary>
    NewPassword
}
