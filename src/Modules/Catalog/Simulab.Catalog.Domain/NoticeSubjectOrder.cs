using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Domain;

/// <summary>
/// The order of an edition's notice subjects (F-74, BR6 and BR7): rows of one group sit together, groups show
/// in the order of their first row, and every write renumbers the whole edition from 1, so a tie or a gap
/// left by an earlier write is repaired. Pure: it only sets <see cref="NoticeSubject.DisplayOrder"/> on the
/// rows it is given, and the handler saves them in one unit.
/// </summary>
public static class NoticeSubjectOrder
{
    /// <summary>
    /// Puts <paramref name="placed"/> last in its group — last in the edition when the group is new — and
    /// renumbers every row. <paramref name="current"/> is the edition's live rows in display order; it may or
    /// may not contain <paramref name="placed"/>.
    /// </summary>
    public static void PlaceLast(IReadOnlyList<NoticeSubject> current, NoticeSubject placed)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(placed);

        var blocks = Blocks(current.Where(row => row != placed));
        var block = blocks.Find(candidate => candidate.Key == placed.NormalizedGroup);
        if (block is null)
        {
            block = new Block(placed.NormalizedGroup);
            blocks.Add(block);
        }

        block.Rows.Add(placed);
        Number(blocks);
    }

    /// <summary>
    /// Swaps <paramref name="row"/> with its neighbour in the same group. The first row of a group cannot go up
    /// and the last cannot go down (BR6): that is <c>notice_subject.move_invalid</c>, and nothing changes.
    /// </summary>
    public static Result Move(IReadOnlyList<NoticeSubject> current, NoticeSubject row, NoticeSubjectMoveDirection direction)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(row);

        var blocks = Blocks(current);
        Number(blocks);

        var rows = blocks.Find(candidate => candidate.Key == row.NormalizedGroup)?.Rows;
        var index = rows?.IndexOf(row) ?? -1;
        var neighbour = direction == NoticeSubjectMoveDirection.Up ? index - 1 : index + 1;
        if (rows is null || index < 0 || neighbour < 0 || neighbour >= rows.Count)
        {
            return Result.Failure(new Error(CatalogErrorCodes.NoticeSubjectMoveInvalid, ErrorKind.Validation));
        }

        var other = rows[neighbour];
        var order = row.DisplayOrder;
        row.PlaceAt(other.DisplayOrder);
        other.PlaceAt(order);

        return Result.Success();
    }

    // Groups in the order their first row appears, each with its rows in the order they appear.
    private static List<Block> Blocks(IEnumerable<NoticeSubject> rows)
    {
        var blocks = new List<Block>();
        foreach (var row in rows)
        {
            var block = blocks.Find(candidate => candidate.Key == row.NormalizedGroup);
            if (block is null)
            {
                block = new Block(row.NormalizedGroup);
                blocks.Add(block);
            }

            block.Rows.Add(row);
        }

        return blocks;
    }

    private static void Number(List<Block> blocks)
    {
        var next = 1;
        foreach (var row in blocks.SelectMany(block => block.Rows))
        {
            row.PlaceAt(next++);
        }
    }

    private sealed class Block(string key)
    {
        public string Key { get; } = key;

        public List<NoticeSubject> Rows { get; } = [];
    }
}
