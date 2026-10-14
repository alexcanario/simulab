using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Domain.Entities;

/// <summary>
/// Another name a <see cref="Topic"/> goes by (F-77, ADR-0001 #45): "Compreensão" for "Interpretação de texto".
/// It is only text. It carries no subject of its own: the subject is the topic's, so a move needs no update.
/// </summary>
public sealed class TopicAlias : TenantEntity
{
    private TopicAlias()
    {
    }

    /// <summary>The topic it points to.</summary>
    public Guid TopicId { get; private set; }

    /// <summary>The text as typed.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>The comparable form of <see cref="Name"/>: what the unique index and the lookup read.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    public static Result<TopicAlias> Create(Guid topicId, string? name) => throw new NotImplementedException();

    /// <summary>Changes the typed form of the text, for a kept alias typed again with another case or accent.</summary>
    public Result Rename(string? name) => throw new NotImplementedException();
}
