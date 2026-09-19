using Microsoft.AspNetCore.Identity;
using Simulab.SharedKernel.Entities;

namespace Simulab.Identity.Domain.Entities;

/// <summary>
/// A role (F-6, F-9). Global in v1: no <c>TenantId</c>. Inherits <see cref="IdentityRole{TKey}"/> the same way
/// <see cref="User"/> inherits <c>IdentityUser</c>, so it repeats the audit and soft-delete fields itself.
/// A system role (Student, Curator, Admin) keeps its name and is never deleted (F-9, BR2); the checks that
/// return the stable error codes live in the Application handlers, which see the module's contracts.
/// </summary>
public sealed class Role : IdentityRole<Guid>, IAuditableEntity, ISoftDeletableEntity
{
    public bool IsSystem { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    /// <summary>One of the seed roles (F-6, BR1), created by the startup seed.</summary>
    public static Role CreateSystem(string name) => new() { Id = Guid.NewGuid(), Name = name, IsSystem = true };

    /// <summary>A custom role (F-9, UC2), with a name already checked by the caller.</summary>
    public static Role CreateCustom(string name) => new() { Id = Guid.NewGuid(), Name = name };

    /// <summary>A role created before F-9 (by the F-6 seed) becomes a system role on the next start.</summary>
    public void MarkAsSystem() => IsSystem = true;
}
