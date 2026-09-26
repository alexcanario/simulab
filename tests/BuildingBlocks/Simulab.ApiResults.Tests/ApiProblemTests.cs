using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Simulab.SharedKernel.Results;

namespace Simulab.ApiResults.Tests;

/// <summary>F-39: the one mapping from an <see cref="Error"/> to an HTTP answer.</summary>
public class ApiProblemTests
{
    private static ProblemDetails DetailsOf(IResult result) =>
        result.Should().BeOfType<ProblemHttpResult>().Subject.ProblemDetails;

    [Theory]
    [InlineData(ErrorKind.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorKind.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorKind.BusinessRule, StatusCodes.Status422UnprocessableEntity)]
    [InlineData(ErrorKind.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorKind.Validation, StatusCodes.Status400BadRequest)]
    public void EveryKind_MapsToItsStatus(ErrorKind kind, int expected) =>
        ApiProblem.StatusFor(kind).Should().Be(expected);

    /// <summary>A kind the mapping has never seen answers 400, not 200 and not a crash.</summary>
    [Fact]
    public void AKindOutsideTheEnum_MapsToBadRequest() =>
        ApiProblem.StatusFor((ErrorKind)999).Should().Be(StatusCodes.Status400BadRequest);

    /// <summary>The rule of presence: the mapping really covers every member the enum declares today.</summary>
    [Fact]
    public void TheMapping_CoversEveryDeclaredKind()
    {
        var kinds = Enum.GetValues<ErrorKind>();

        kinds.Should().HaveCountGreaterThan(1);
        kinds.Should().OnlyContain(kind => ApiProblem.StatusFor(kind) >= 400);
    }

    [Fact]
    public void TheAnswer_CarriesTheStatusTheCodeAndTheDetail()
    {
        var result = ApiProblem.Problem(new Error("exam.not_found", ErrorKind.NotFound, "No exam with that id."));

        var details = DetailsOf(result);
        details.Status.Should().Be(StatusCodes.Status404NotFound);
        details.Title.Should().Be("exam.not_found");
        details.Detail.Should().Be("No exam with that id.");
        details.Extensions["code"].Should().Be("exam.not_found");
    }

    [Fact]
    public void TheCodeExtension_IsThereEvenWithoutADetail()
    {
        var details = DetailsOf(ApiProblem.Problem(new Error("account.locked", ErrorKind.BusinessRule)));

        details.Detail.Should().BeNull();
        details.Extensions["code"].Should().Be("account.locked");
    }

    [Fact]
    public void AStatusOverride_WinsOverTheKind()
    {
        var details = DetailsOf(ApiProblem.Problem(
            new Error("account.locked", ErrorKind.BusinessRule),
            StatusCodes.Status423Locked));

        details.Status.Should().Be(StatusCodes.Status423Locked, "the kind alone would have said 422");
        details.Extensions["code"].Should().Be("account.locked");
    }

    [Fact]
    public void ExtensionsReachTheAnswer_BesideTheCode()
    {
        var details = DetailsOf(ApiProblem.Problem(
            new Error("account.locked", ErrorKind.BusinessRule),
            StatusCodes.Status423Locked,
            ("retryAfterSeconds", 42),
            ("attemptsLeft", 0)));

        details.Extensions["retryAfterSeconds"].Should().Be(42);
        details.Extensions["attemptsLeft"].Should().Be(0);
        details.Extensions["code"].Should().Be("account.locked");
    }

    [Fact]
    public void AStatusOverrideWithoutExtensions_IsAllowed()
    {
        var details = DetailsOf(ApiProblem.Problem(
            new Error("identity.registration_rate_limited", ErrorKind.BusinessRule),
            StatusCodes.Status429TooManyRequests));

        details.Status.Should().Be(StatusCodes.Status429TooManyRequests);
        details.Extensions.Should().ContainKey("code");
    }

    [Fact]
    public void ANullError_IsRefused() =>
        FluentActions.Invoking(() => ApiProblem.Problem(null!)).Should().Throw<ArgumentNullException>();
}
