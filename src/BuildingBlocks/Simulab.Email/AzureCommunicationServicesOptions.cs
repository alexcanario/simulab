namespace Simulab.Email;

/// <summary>Settings of the Azure Communication Services sender, section <c>Email:AzureCommunicationServices</c>.</summary>
public sealed class AzureCommunicationServicesOptions
{
    /// <summary>
    /// Address of the Communication Service, for example <c>https://&lt;name&gt;.communication.azure.com</c>.
    /// The deploy sets it from the Bicep output; there is no key or connection string to go with it.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Client id of the user-assigned managed identity the host signs in with. Read from <c>AZURE_CLIENT_ID</c>,
    /// which the cloud sets on a host that has the identity attached; never configured by hand.
    /// </summary>
    public string? ManagedIdentityClientId { get; set; }
}
