using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Security;

namespace Simulab.Web.Services;

/// <summary>
/// Every permission the Web builds an authorization policy for (F-33, BR2). The Api reads the same
/// names out of the container, where each module registers its <see cref="PermissionCatalog"/>; the
/// Web has no module registration to read, so it lists the catalogs here — the one place a new module
/// is added, next to its typed client in <c>Program.cs</c>.
/// </summary>
public static class WebPermissions
{
    public static readonly IReadOnlyList<PermissionCatalog> Catalogs =
    [
        new("identity", IdentityPermissions.All),
        new(CatalogPermissions.ModuleName, CatalogPermissions.All)
    ];

    public static readonly IReadOnlyList<string> All =
        [.. Catalogs.SelectMany(catalog => catalog.Permissions).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
}
