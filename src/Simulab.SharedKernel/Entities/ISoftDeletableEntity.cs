namespace Simulab.SharedKernel.Entities;

/// <summary>Soft delete. Rows are never removed; a global query filter hides deleted rows.</summary>
public interface ISoftDeletableEntity
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}
