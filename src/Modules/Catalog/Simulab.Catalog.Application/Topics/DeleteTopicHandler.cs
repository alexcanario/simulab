using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Topics;

/// <summary>
/// Removes a topic from its subject (F-79, UC8, BR9). A topic that a live notice subject maps is refused
/// (F-75, BR9), and so is one that any module's <see cref="ITopicUsageCheck"/> says is in use (F-95, BR13);
/// otherwise this is a plain soft delete. A module that adds a reference to <c>Topic</c> registers its own
/// check and never edits this handler.
/// </summary>
public sealed class DeleteTopicHandler(ITopicStore store, IEnumerable<ITopicUsageCheck> usageChecks)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var topic = await store.FindAsync(id, cancellationToken);
        if (topic is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.TopicNotFound, ErrorKind.NotFound));
        }

        // F-75 BR9: a live notice subject maps this topic, so removing it would leave a hole nobody sees.
        if (await store.IsMappedAsync(id, cancellationToken)
            || await TopicUsageChecks.AnyAsync(usageChecks, id, cancellationToken))
        {
            return Result.Failure(new Error(CatalogErrorCodes.TopicInUse, ErrorKind.Conflict));
        }

        store.Remove(topic);
        await store.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
