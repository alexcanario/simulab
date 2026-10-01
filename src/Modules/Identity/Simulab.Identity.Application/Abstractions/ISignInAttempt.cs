namespace Simulab.Identity.Application.Abstractions;

/// <summary>
/// One sign-in request's side of the per-address limit (F-38). The Api owns the counter, keyed by the client
/// address; a step that only learns the account after reading its challenge (the code step) reaches it here.
/// </summary>
public interface ISignInAttempt
{
    /// <summary>The time left in the address's window, set when <see cref="TryCount"/> refused.</summary>
    TimeSpan RetryAfter { get; }

    /// <summary>
    /// Adds the account name to the address's set (BR1). False when the address is at its limit (BR3): the step
    /// then does no account work.
    /// </summary>
    bool TryCount(string accountName);

    /// <summary>BR5: a step that succeeded takes this account's own name out of the set.</summary>
    void Clear(string accountName);
}
