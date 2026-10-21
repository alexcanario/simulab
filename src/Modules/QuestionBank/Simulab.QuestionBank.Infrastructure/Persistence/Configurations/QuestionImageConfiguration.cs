using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-103: the pictures of the statements, options and base texts. The owner is held by kind and id with no foreign key:
/// there are two kinds of owner, and a published version may outlive the soft-deleted question it was made from.
/// </summary>
public sealed class QuestionImageConfiguration : IEntityTypeConfiguration<QuestionImage>
{
    public const int BlobNameMaxLength = 100;
    public const int ContentTypeMaxLength = 40;
    public const string OwnerIndex = "ix_question_images_owner";
    public const string PendingIndex = "ix_question_images_pending_created_at";

    public void Configure(EntityTypeBuilder<QuestionImage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "question_images",
            table =>
            {
                table.HasComment("A picture a curator put in the statement, an option or a base text. The file is in the private storage; the row says what it is and who holds it. Owner empty means pending: no saved text holds it yet.");
                table.HasCheckConstraint("ck_question_images_size_range", "size_bytes > 0 AND size_bytes <= 2097152");
                table.HasCheckConstraint("ck_question_images_owner", "(owner_kind IS NULL) = (owner_id IS NULL)");
            });
        builder.HasKey(image => image.Id);

        // The entity makes its own id, as the question and its versions do.
        builder.Property(image => image.Id).ValueGeneratedNever();

        builder.Property(image => image.BlobName)
            .HasMaxLength(BlobNameMaxLength)
            .IsRequired()
            .HasComment("The name of the file in the storage: question-images/ and a generated id, never the user's file name.");

        builder.Property(image => image.ContentType)
            .HasMaxLength(ContentTypeMaxLength)
            .IsRequired()
            .HasComment("The type decided from the first bytes of the file: image/png, image/jpeg, image/webp or image/gif.");

        builder.Property(image => image.SizeBytes)
            .IsRequired()
            .HasComment("The size of the file in bytes, from 1 to 2 MB (2097152).");

        builder.Property(image => image.OwnerKind)
            .HasConversion<string>()
            .HasMaxLength(40)
            .HasComment("What holds the image: Question (its statement or an option) or BaseText. Null while the image is pending.");

        builder.Property(image => image.OwnerId)
            .HasComment("The question or base text that holds the image, by id with no foreign key. Null while the image is pending.");

        builder.Ignore(image => image.IsPending);

        builder.HasIndex(image => new { image.OwnerKind, image.OwnerId })
            .HasDatabaseName(OwnerIndex);

        // The sweep reads only the pending live images, oldest first.
        builder.HasIndex(image => image.CreatedAt)
            .HasDatabaseName(PendingIndex)
            .HasFilter("owner_id IS NULL AND is_deleted = false");
    }
}
