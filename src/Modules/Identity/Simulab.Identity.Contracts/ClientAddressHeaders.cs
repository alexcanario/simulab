namespace Simulab.Identity.Contracts;

/// <summary>
/// B-4: the Web calls the Api from its own server, so it sends the visitor's address in
/// <see cref="Address"/>, proven by its client secret in <see cref="Secret"/>. The Api trusts the address
/// only with the secret; otherwise it uses the connection's address.
/// </summary>
public static class ClientAddressHeaders
{
    public const string Address = "X-Simulab-Client-Address";

    public const string Secret = "X-Simulab-Client-Secret";
}
