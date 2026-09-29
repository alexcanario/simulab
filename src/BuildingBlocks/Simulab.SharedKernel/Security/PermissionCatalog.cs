namespace Simulab.SharedKernel.Security;

/// <summary>
/// The permission names one module defines (F-33, BR2). The <c>permissions</c> table belongs to
/// Identity and no module writes to another module's tables, so every module registers its own
/// catalog in the container and Identity seeds the union of what it finds there.
/// </summary>
/// <param name="Module">The module the names belong to: the part of a name before the first dot.</param>
/// <param name="Permissions">Every checkable permission the module defines.</param>
/// <param name="InitialGrants">
/// Permission name to the roles that hold it from the start (F-36, BR10). Identity applies it only in the
/// start that creates the permission row; afterwards only the roles back office changes who holds it. It
/// never names Admin: Admin receives every permission on every start (F-6, BR3).
/// </param>
public sealed record PermissionCatalog(
    string Module,
    IReadOnlyList<string> Permissions,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? InitialGrants = null);
