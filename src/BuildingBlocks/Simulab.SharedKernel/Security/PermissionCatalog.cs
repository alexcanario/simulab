namespace Simulab.SharedKernel.Security;

/// <summary>
/// The permission names one module defines (F-33, BR2). The <c>permissions</c> table belongs to
/// Identity and no module writes to another module's tables, so every module registers its own
/// catalog in the container and Identity seeds the union of what it finds there.
/// </summary>
/// <param name="Module">The module the names belong to: the part of a name before the first dot.</param>
/// <param name="Permissions">Every checkable permission the module defines.</param>
public sealed record PermissionCatalog(string Module, IReadOnlyList<string> Permissions);
