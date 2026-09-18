namespace Simulab.SharedKernel.Entities;

/// <summary>
/// Base for every module entity: tenant, audit and soft delete.
/// <see cref="TenantId"/> is null for global data and for B2C users. It is dormant in v1 (ADR-0001, decision 7).
/// A unique index that includes it must be NULLS NOT DISTINCT.
/// </summary>
public abstract class TenantEntity : Entity, IAuditableEntity, ISoftDeletableEntity
{
    public Guid? TenantId { get; protected set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
