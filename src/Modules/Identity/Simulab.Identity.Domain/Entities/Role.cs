using Microsoft.AspNetCore.Identity;

namespace Simulab.Identity.Domain.Entities;

/// <summary>
/// A seed role (Student, Curator, Admin - F-6, BR1). Global in v1: no <c>TenantId</c>, no audit, no
/// soft delete - there is no edit or delete screen yet (F-9 decides what those need). Inherits
/// <see cref="IdentityRole{TKey}"/> the same way <see cref="User"/> inherits <c>IdentityUser</c>.
/// </summary>
public sealed class Role : IdentityRole<Guid>;
