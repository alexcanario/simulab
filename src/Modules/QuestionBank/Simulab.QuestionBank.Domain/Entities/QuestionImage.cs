using Simulab.SharedKernel.Entities;

namespace Simulab.QuestionBank.Domain.Entities;

/// <summary>
/// A picture a curator put in a statement, an option or a base text (F-103). The file is in the storage under
/// <see cref="BlobName"/>, a name the module made up; the row says what it is and who holds it. An image no saved text
/// holds yet is pending and has no owner (BR3); the save of a text claims the images its HTML names (BR6).
/// </summary>
public sealed class QuestionImage : TenantEntity
{
    // For EF.
    private QuestionImage()
    {
    }

    public QuestionImage(string blobName, string contentType, long sizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        BlobName = blobName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
    }

    /// <summary>The name of the file in the storage: <c>question-images/{guid}</c>, never the user's file name (BR13).</summary>
    public string BlobName { get; private set; } = string.Empty;

    /// <summary>The type decided from the file's first bytes: image/png, image/jpeg, image/webp or image/gif (BR2).</summary>
    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    /// <summary>What holds the image; null while it is pending.</summary>
    public QuestionImageOwnerKind? OwnerKind { get; private set; }

    /// <summary>The question or base text that holds the image; null while it is pending.</summary>
    public Guid? OwnerId { get; private set; }

    public bool IsPending => OwnerId is null;

    /// <summary>
    /// Makes the image the owner's. A pending image is claimed; one the same owner already holds stays; one held by
    /// another owner is refused (BR6, false) and nothing changes.
    /// </summary>
    public bool TryClaim(QuestionImageOwnerKind kind, Guid ownerId)
    {
        if (OwnerId is null)
        {
            OwnerKind = kind;
            OwnerId = ownerId;

            return true;
        }

        return OwnerKind == kind && OwnerId == ownerId;
    }
}
