namespace Simulab.Identity.Domain.Entities;

/// <summary>
/// One checkable permission (F-6, BR3). <see cref="Name"/> is the key and the value a policy carries
/// (<c>"Permission:identity.roles.manage"</c>) - there is no separate surrogate id to keep in sync.
/// </summary>
#pragma warning disable CA1711 // "Permission" is the glossary term (docs/glossary.md); not a collection/flags-style suffix here.
public sealed class Permission
#pragma warning restore CA1711
{
    public required string Name { get; init; }

    public string? Description { get; init; }
}
