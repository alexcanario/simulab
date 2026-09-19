using Simulab.SharedKernel.Messaging;

namespace Simulab.Identity.Contracts;

/// <summary>
/// An account was erased by its owner (F-10, BR13). Identity has already overwritten its own personal
/// data; a module that keeps anything about the user anonymizes it when it sees this.
/// <paramref name="UserId"/> stays valid as the pseudonym the unidentified records hang from
/// (ADR-0001 #9): it is a key, never a person.
/// </summary>
/// <param name="UserId">The account that was erased.</param>
/// <param name="ErasedAt">When the erasure was recorded.</param>
public sealed record UserErased(Guid UserId, DateTimeOffset ErasedAt) : IIntegrationEvent;
