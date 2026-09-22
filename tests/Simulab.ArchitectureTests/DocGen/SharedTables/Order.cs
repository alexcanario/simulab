namespace Simulab.ArchitectureTests.DocGen.SharedTables;

public sealed class Order
{
    public Guid Id { get; set; }

    public DateTimeOffset PlacedAt { get; set; }

    public OrderSummary Summary { get; set; } = new();
}
