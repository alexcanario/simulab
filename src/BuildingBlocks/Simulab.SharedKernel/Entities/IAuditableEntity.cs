namespace Simulab.SharedKernel.Entities;

/// <summary>Audit fields, filled by a save interceptor. Never set them by hand.</summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    Guid? UpdatedBy { get; set; }
}
