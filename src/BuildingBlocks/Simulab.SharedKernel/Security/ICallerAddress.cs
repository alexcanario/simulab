namespace Simulab.SharedKernel.Security;

/// <summary>
/// The client address of the request being served (F-21, BR2). The host fills it from the visitor's address
/// (B-4); outside a request, and when the host does not know it, it is null.
/// </summary>
public interface ICallerAddress
{
    string? Value { get; }
}
