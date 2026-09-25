using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Simulab.SharedKernel.Entities;

namespace Simulab.Persistence.Conventions;

/// <summary>
/// Puts the <see cref="StandardColumnComments"/> on the columns every entity repeats (F-24, BR3).
/// </summary>
/// <remarks>
/// It runs when the model is finalized, not from <c>OnModelCreating</c>: a module context can add
/// entity types after calling the base (Identity adds OpenIddict's and the borrowed job queue), and a
/// base-class loop would describe the same table in one context and not in another, leaving the two
/// snapshots disagreeing. It keys on the SharedKernel interfaces rather than on column names, so
/// <c>User</c> and <c>Role</c> — which inherit from ASP.NET Identity and only implement them — are
/// covered, while a <c>CreatedAt</c> that is not an audit field, such as the job queue's, is not.
/// </remarks>
public sealed class StandardColumnCommentConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (CarriesOwnIdentifier(clrType))
            {
                Describe(entityType, nameof(Entity.Id), StandardColumnComments.Id);
            }

            // TenantEntity carries it, and so does User, which repeats the fields instead of inheriting
            // them (the known exception in the architecture tests). Describe leaves an entity without
            // the property alone, so roles, which has no tenant, is not touched.
            if (clrType.IsAssignableTo(typeof(TenantEntity)) || clrType.IsAssignableTo(typeof(IAuditableEntity)))
            {
                Describe(entityType, nameof(TenantEntity.TenantId), StandardColumnComments.TenantId);
            }

            if (clrType.IsAssignableTo(typeof(IAuditableEntity)))
            {
                DescribeAll(entityType, StandardColumnComments.Auditable);
            }

            if (clrType.IsAssignableTo(typeof(ISoftDeletableEntity)))
            {
                DescribeAll(entityType, StandardColumnComments.SoftDeletable);
            }
        }
    }

    /// <summary>
    /// Our own surrogate key: an <see cref="Entity"/>, or one of the two ASP.NET Identity entities that
    /// carry our audit fields. Keying on "a Guid key named Id" would reach a library entity the day one
    /// arrives with that shape.
    /// </summary>
    private static bool CarriesOwnIdentifier(Type clrType) =>
        clrType.IsAssignableTo(typeof(Entity)) || clrType.IsAssignableTo(typeof(IAuditableEntity));

    private static void DescribeAll(IConventionEntityType entityType, IReadOnlyDictionary<string, string> comments)
    {
        foreach (var (property, comment) in comments)
        {
            Describe(entityType, property, comment);
        }
    }

    // A comment a configuration set by hand is Explicit and wins: CanSetComment says no and we leave it.
    private static void Describe(IConventionEntityType entityType, string propertyName, string comment)
    {
        if (entityType.FindProperty(propertyName) is { } property && property.Builder.CanSetComment(comment))
        {
            property.SetComment(comment);
        }
    }
}
