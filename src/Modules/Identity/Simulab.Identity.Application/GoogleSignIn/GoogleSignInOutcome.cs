using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.GoogleSignIn;

/// <summary>
/// Where a Google sign-in goes next (F-20): on with the account it found, or to the confirmation page when there is
/// none (BR7). Exactly one of the two is set.
/// </summary>
public sealed record GoogleSignInOutcome(User? User, GoogleIdentity? SignUpRequired)
{
    public static GoogleSignInOutcome Continue(User user) => new(user, null);

    public static GoogleSignInOutcome SignUp(GoogleIdentity identity) => new(null, identity);
}
