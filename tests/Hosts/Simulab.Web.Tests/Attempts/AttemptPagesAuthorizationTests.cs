using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Simulab.ExamEngine.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Attempts;
using Simulab.Web.Services;

namespace Simulab.Web.Tests.Attempts;

/// <summary>
/// F-169 BR11 and AC16/AC17 [sec]: both attempt screens are behind <c>attempts.take</c>. A user without it gets the ordinary
/// not-found page (the router's NotAuthorized state), never a sign of the attempt; the Api then checks the owner again.
/// </summary>
public sealed class AttemptPagesAuthorizationTests
{
    [Fact]
    public void EveryAttemptPage_RequiresAttemptsTake()
    {
        var pages = typeof(TakeAttempt).Assembly.GetTypes()
            .Where(type => type.GetCustomAttributes<RouteAttribute>().Any(route => route.Template.StartsWith("/attempts/", StringComparison.Ordinal)))
            .ToList();

        pages.Select(page => page.Name).Should().BeEquivalentTo(nameof(TakeAttempt), nameof(AttemptSubmitted));
        foreach (var page in pages)
        {
            page.GetCustomAttributes<AuthorizeAttribute>().Select(attribute => attribute.Policy)
                .Should().Equal([PermissionPolicy.Prefix + ExamEnginePermissions.Take], page.Name);
        }
    }

    [Fact]
    public void TheWebBuildsAPolicyForAttemptsTake()
    {
        WebPermissions.All.Should().Contain(ExamEnginePermissions.Take);
    }

    [Fact]
    public void TheRoutes_AreTheOnesTheItemNames()
    {
        typeof(TakeAttempt).GetCustomAttributes<RouteAttribute>().Select(route => route.Template).Should().Equal("/attempts/{Id:guid}");
        typeof(AttemptSubmitted).GetCustomAttributes<RouteAttribute>().Select(route => route.Template).Should().Equal("/attempts/{Id:guid}/submitted");
    }
}
