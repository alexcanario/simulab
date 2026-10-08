using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain;
using Simulab.Catalog.Domain.Entities;

namespace Simulab.Catalog.Tests;

/// <summary>F-74 BR6 and BR7: where a row goes and how a move swaps neighbours, with no database.</summary>
public class NoticeSubjectOrderTests
{
    private static readonly Guid Edition = Guid.CreateVersion7();

    private static NoticeSubject Row(string? group, string label) =>
        NoticeSubject.Create(Edition, group, label, null).Value;

    // The rows of an edition as the store hands them: numbered, in display order.
    private static List<NoticeSubject> Numbered(params NoticeSubject[] rows)
    {
        var list = rows.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            NoticeSubjectOrder.PlaceLast(list.Take(i).ToList(), list[i]);
        }

        return list;
    }

    private static string[] Labels(IEnumerable<NoticeSubject> rows) =>
        rows.OrderBy(row => row.DisplayOrder).Select(row => row.Label).ToArray();

    // AC3: a new row goes last in its group, even when other groups come after it.
    [Fact]
    public void PlaceLast_ExistingGroup_GoesAfterTheLastRowOfThatGroup()
    {
        var a = Row("Básicos", "A1");
        var b = Row("Específicos", "B1");
        var rows = Numbered(a, b);
        var added = Row("Básicos", "A2");

        NoticeSubjectOrder.PlaceLast(rows, added);

        Labels(rows.Append(added)).Should().Equal("A1", "A2", "B1");
        added.DisplayOrder.Should().Be(2);
    }

    // BR6: a new group goes last in the edition.
    [Fact]
    public void PlaceLast_NewGroup_GoesLastInTheEdition()
    {
        var rows = Numbered(Row("Básicos", "A1"), Row("Específicos", "B1"));
        var added = Row("Gerais", "C1");

        NoticeSubjectOrder.PlaceLast(rows, added);

        Labels(rows.Append(added)).Should().Equal("A1", "B1", "C1");
    }

    [Fact]
    public void PlaceLast_NoGroupIsOneGroup()
    {
        var rows = Numbered(Row(null, "N1"), Row("Básicos", "A1"));
        var added = Row(null, "N2");

        NoticeSubjectOrder.PlaceLast(rows, added);

        Labels(rows.Append(added)).Should().Equal("N1", "N2", "A1");
    }

    // AC10 (BR7): a row that changes group goes to the end of the new group.
    [Fact]
    public void PlaceLast_ARowThatChangedGroup_GoesToTheEndOfTheNewGroup()
    {
        var a1 = Row("Básicos", "A1");
        var a2 = Row("Básicos", "A2");
        var b1 = Row("Específicos", "B1");
        var b2 = Row("Específicos", "B2");
        var rows = Numbered(a1, a2, b1, b2);

        a1.Update("Específicos", "A1", null);
        NoticeSubjectOrder.PlaceLast(rows, a1);

        Labels(rows).Should().Equal("A2", "B1", "B2", "A1");
    }

    // Groups show in the order of their first row; accents and case do not make a second group.
    [Fact]
    public void PlaceLast_GroupSpelledWithoutAccentsOrCase_JoinsTheSameGroup()
    {
        var rows = Numbered(Row("Básicos", "A1"), Row("Específicos", "B1"));
        var added = Row("basicos", "A2");

        NoticeSubjectOrder.PlaceLast(rows, added);

        Labels(rows.Append(added)).Should().Equal("A1", "A2", "B1");
    }

    // Earlier ties and gaps are repaired by the next write.
    [Fact]
    public void PlaceLast_RepairsGapsAndTies()
    {
        var a = Row("Básicos", "A1");
        var b = Row("Básicos", "A2");
        a.PlaceAtForTest(7);
        b.PlaceAtForTest(7);
        var added = Row("Básicos", "A3");

        NoticeSubjectOrder.PlaceLast([a, b], added);

        new[] { a, b, added }.Select(row => row.DisplayOrder).Should().Equal(1, 2, 3);
    }

    // AC11: three rows A, B, C in one group; moving B up gives B, A, C.
    [Fact]
    public void Move_Up_SwapsWithThePreviousRowOfTheGroup()
    {
        var a = Row("G", "Alpha");
        var b = Row("G", "Bravo");
        var c = Row("G", "Charlie");
        var rows = Numbered(a, b, c);

        var result = NoticeSubjectOrder.Move(rows, b, NoticeSubjectMoveDirection.Up);

        result.IsSuccess.Should().BeTrue();
        Labels(rows).Should().Equal("Bravo", "Alpha", "Charlie");
    }

    [Fact]
    public void Move_Down_SwapsWithTheNextRowOfTheGroup()
    {
        var a = Row("G", "Alpha");
        var b = Row("G", "Bravo");
        var c = Row("G", "Charlie");
        var rows = Numbered(a, b, c);

        var result = NoticeSubjectOrder.Move(rows, b, NoticeSubjectMoveDirection.Down);

        result.IsSuccess.Should().BeTrue();
        Labels(rows).Should().Equal("Alpha", "Charlie", "Bravo");
    }

    // AC11 and AC19: the first row cannot go up and the last cannot go down; nothing changes.
    [Fact]
    public void Move_FirstRowUp_AndLastRowDown_AreInvalidAndChangeNothing()
    {
        var a = Row("G", "Alpha");
        var b = Row("G", "Bravo");
        var rows = Numbered(a, b);

        var up = NoticeSubjectOrder.Move(rows, a, NoticeSubjectMoveDirection.Up);
        var down = NoticeSubjectOrder.Move(rows, b, NoticeSubjectMoveDirection.Down);

        up.Error!.Code.Should().Be(CatalogErrorCodes.NoticeSubjectMoveInvalid);
        down.Error!.Code.Should().Be(CatalogErrorCodes.NoticeSubjectMoveInvalid);
        Labels(rows).Should().Equal("Alpha", "Bravo");
    }

    // BR6: a row never crosses into another group, so the edge of a group is an edge.
    [Fact]
    public void Move_LastRowOfAGroupDown_DoesNotCrossIntoTheNextGroup()
    {
        var a = Row("G1", "Alpha");
        var b = Row("G2", "Bravo");
        var rows = Numbered(a, b);

        var result = NoticeSubjectOrder.Move(rows, a, NoticeSubjectMoveDirection.Down);

        result.IsFailure.Should().BeTrue();
        Labels(rows).Should().Equal("Alpha", "Bravo");
    }

    [Fact]
    public void Move_AGroupOfOneRow_CannotMoveEitherWay()
    {
        var a = Row("G1", "Alpha");
        var rows = Numbered(a, Row("G2", "Bravo"));

        NoticeSubjectOrder.Move(rows, a, NoticeSubjectMoveDirection.Up).IsFailure.Should().BeTrue();
        NoticeSubjectOrder.Move(rows, a, NoticeSubjectMoveDirection.Down).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Move_InTheSecondGroup_LeavesTheFirstGroupAlone()
    {
        var a = Row("G1", "Alpha");
        var b = Row("G2", "Bravo");
        var c = Row("G2", "Charlie");
        var rows = Numbered(a, b, c);

        NoticeSubjectOrder.Move(rows, c, NoticeSubjectMoveDirection.Up).IsSuccess.Should().BeTrue();

        Labels(rows).Should().Equal("Alpha", "Charlie", "Bravo");
    }
}

internal static class NoticeSubjectTestExtensions
{
    // DisplayOrder has a private writer and no public door sets an arbitrary value: a test that needs rows
    // left with a gap or a tie by an earlier write has to set it by reflection.
    public static void PlaceAtForTest(this NoticeSubject row, int order)
    {
        typeof(NoticeSubject).GetProperty(nameof(NoticeSubject.DisplayOrder))!.SetValue(row, order);
    }
}
