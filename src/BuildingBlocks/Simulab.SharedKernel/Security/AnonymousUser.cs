namespace Simulab.SharedKernel.Security;

/// <summary>No one is signed in: audit fields stay null.</summary>
public sealed class AnonymousUser : ICurrentUser
{
    public Guid? UserId => null;
}
