using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Topics;

/// <summary>
/// Removes a topic from its subject (F-79, UC8, BR9). A topic that a live notice subject maps is refused
/// (F-75, BR9); otherwise this is a plain soft delete, and each later item that adds a reference to
/// <c>Topic</c> adds its own guard here.
/// </summary>
public sealed class DeleteTopicHandler(ITopicStore store)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var topic = await store.FindAsync(id, cancellationToken);
        if (topic is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.TopicNotFound, ErrorKind.NotFound));
        }

        // F-75 BR9: a live notice subject maps this topic, so removing it would leave a hole nobody sees.
        if (await store.IsMappedAsync(id, cancellationToken))
        {
            return Result.Failure(new Error(CatalogErrorCodes.TopicInUse, ErrorKind.Conflict));
        }

        store.Remove(topic);
        await store.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
