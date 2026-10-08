using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Topics;

/// <summary>
/// Removes a topic from its subject (F-79, UC8, BR9). Nothing points at a topic yet, so this is a plain soft
/// delete; each later item that adds a reference to <c>Topic</c> adds its own guard here.
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

        store.Remove(topic);
        await store.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
