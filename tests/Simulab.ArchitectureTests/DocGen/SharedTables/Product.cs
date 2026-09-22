namespace Simulab.ArchitectureTests.DocGen.SharedTables;

public sealed class Product
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public Money Price { get; set; } = new();

    public Dimensions Dimensions { get; set; } = new();
}
