using Simulab.SharedKernel.Results;

namespace Simulab.SharedKernel.Tests;

public class ResultTests
{
    private static readonly Error Taken = new("exam_board.acronym_taken", ErrorKind.Conflict);

    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_carries_the_error_code()
    {
        var result = Result.Failure(Taken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("exam_board.acronym_taken");
        result.Error.Kind.Should().Be(ErrorKind.Conflict);
    }

    [Fact]
    public void Success_of_value_exposes_the_value()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Reading_the_value_of_a_failure_throws_with_the_code()
    {
        var result = Result.Failure<int>(Taken);

        var read = () => result.Value;

        read.Should().Throw<InvalidOperationException>().WithMessage("*exam_board.acronym_taken*");
    }

    [Fact]
    public void Failure_without_an_error_is_rejected()
    {
        var create = () => Result.Failure(null!);

        create.Should().Throw<ArgumentNullException>();
    }
}
