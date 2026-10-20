using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Simulab.Storage;

public static class StorageServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IFileStorage"/>. With the connection <paramref name="connectionName"/> the app host hands the host
    /// (Azurite locally, the account's blob endpoint reached by the managed identity in the cloud), the files go to Blob
    /// Storage through the Aspire client integration. Without it, Development starts with
    /// <see cref="NotConfiguredFileStorage"/> and every other environment refuses to start (open point 1 of F-89).
    /// The client is keyed by the connection name: the Data Protection key ring of ServiceDefaults has an unkeyed one.
    /// </summary>
    public static IHostApplicationBuilder AddFileStorage(this IHostApplicationBuilder builder, string connectionName = "blobs")
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString(connectionName)))
        {
            builder.AddKeyedAzureBlobServiceClient(connectionName);
            builder.Services.AddSingleton<IFileStorage>(services =>
                new BlobFileStorage(services.GetRequiredKeyedService<BlobServiceClient>(connectionName)));
        }
        else if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddSingleton<IFileStorage, NotConfiguredFileStorage>();
        }
        else
        {
            throw new InvalidOperationException(
                $"ConnectionStrings:{connectionName} is required outside Development: the app host provides it (docs/infra.md).");
        }

        return builder;
    }
}
