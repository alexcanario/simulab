using Simulab.SharedKernel.Security;

namespace Simulab.Persistence.Tests;

/// <summary>The user a test acts as. Null is anonymous, as before sign-in exists (F-5).</summary>
public sealed class TestUser : ICurrentUser
{
    public Guid? UserId { get; set; }
}
