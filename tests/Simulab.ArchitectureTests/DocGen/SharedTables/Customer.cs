namespace Simulab.ArchitectureTests.DocGen.SharedTables;

public sealed class Customer
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public Address? Address { get; set; }
}
