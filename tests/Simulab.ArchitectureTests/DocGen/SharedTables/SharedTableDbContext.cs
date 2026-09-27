using Microsoft.EntityFrameworkCore;
using Simulab.Persistence;

namespace Simulab.ArchitectureTests.DocGen.SharedTables;

/// <summary>
/// F-25: a test-only model with every way two types can share a table. No module maps one yet, so the DocGen tests
/// build this model instead; it is never next to the tool, so it never reaches docs/architecture/.
/// </summary>
public sealed class SharedTableDbContext(DbContextOptions<SharedTableDbContext> options) : DbContext(options)
{
    public const string SchemaName = "shared";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        // Owned type without ToJson: its columns live in the owner's table (table splitting).
        modelBuilder.Entity<Customer>(customer =>
        {
            customer.ToTable("customers");
            customer.Property(c => c.Name).HasMaxLength(100);
            customer.OwnsOne(c => c.Address, address =>
            {
                address.Property(a => a.Street).IsRequired().HasMaxLength(200);
                address.HasIndex(a => a.City);
            });
        });

        // Complex types: one stored as columns, one as a JSON column.
        modelBuilder.Entity<Product>(product =>
        {
            product.ToTable("products");
            product.ComplexProperty(p => p.Price);
            product.ComplexProperty(p => p.Dimensions, dimensions => dimensions.ToJson("dimensions"));
        });

        // Two entity types on one table, linked key to key.
        modelBuilder.Entity<Order>(order =>
        {
            order.ToTable("orders");
            order.HasOne(o => o.Summary).WithOne().HasForeignKey<OrderSummary>(s => s.Id);
        });
        modelBuilder.Entity<OrderSummary>().ToTable("orders");

        // A real self-reference, which the diagram must keep.
        modelBuilder.Entity<Category>(category =>
        {
            category.ToTable("categories");
            category.HasOne(c => c.Parent).WithMany().HasForeignKey(c => c.ParentId);

            // F-28: the two partial-index shapes the real models do not have. The one over `name` is a visible
            // column, so it reaches the DBML schema too; the filters keep their own spacing and case on purpose,
            // because DocGen must pass the model's string through untouched.
            category.HasIndex(c => c.Name)
                .HasDatabaseName("ix_categories_named")
                .HasFilter("name IS NOT NULL");
            category.HasIndex(c => new { c.ParentId, c.Name })
                .HasDatabaseName("ux_categories_parent_name")
                .IsUniquePerTenant()
                .HasFilter("ParentId  Is Not NULL");
        });
    }
}
