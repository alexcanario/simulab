using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Layout;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;
using Simulab.Web.Services;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-105 AC9 (screen part, BR10): the review pages and their menu item follow the review permission, by permission and
/// never by role name, and a role that holds only <c>questions.review</c> sees the queue and nothing else of the
/// question bank. Without the permission the router answers the app's not-found page, as for every admin page; the
/// attribute that causes it is what is pinned here.
/// </summary>
public sealed class ReviewScreensPermissionTests
{
    [Fact]
    public void ThePages_AreGatedByTheQuestionsReviewPermission_NotByManage()
    {
        foreach (var type in new[] { typeof(ReviewQueue), typeof(ReviewQuestion) })
        {
            type.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Select(attribute => attribute.Policy)
                .Should().Equal([PermissionPolicy.Prefix + QuestionBankPermissions.Review], $"{type.Name} needs questions.review and only that");
        }
    }

    [Fact]
    public void ThePages_AreAtTheQueueAndReviewRoutes_NextToTheQuestionForm()
    {
        typeof(ReviewQueue).GetCustomAttributes<RouteAttribute>().Select(route => route.Template)
            .Should().Equal("/admin/questions/review");
        typeof(ReviewQuestion).GetCustomAttributes<RouteAttribute>().Select(route => route.Template)
            .Should().Equal("/admin/questions/review/{Id:guid}");
    }

    [Fact]
    public void WebPermissions_HaveAPolicyForTheReviewPermission()
    {
        WebPermissions.All.Should().Contain(QuestionBankPermissions.Review);
    }

    [Fact]
    public void Menu_HasTheReviewQueueEntryRightAfterQuestions_WithItsIconAndText()
    {
        var entries = NavigationItems.All.ToList();
        var questions = entries.FindIndex(item => item.ResourceKey == "Nav.Questions");
        var queue = entries[questions + 1];

        queue.ResourceKey.Should().Be("Nav.ReviewQueue");
        queue.Route.Should().Be("/admin/questions/review");
        queue.Icon.Should().Be(AppIcons.Review);
        queue.Section.Should().Be(NavigationSection.Content);
        queue.RequiredPermission.Should().Be(QuestionBankPermissions.Review);
    }

    // A role holding only questions.review sees the queue and nothing else of the question bank.
    [Fact]
    public void Visible_ReviewPermissionAlone_ShowsTheQueueAndNothingElseOfTheQuestionBank()
    {
        var visible = NavigationItems.Visible(NavigationItems.All, isDevelopment: false, permission => permission == QuestionBankPermissions.Review);

        visible.Select(item => item.Route).Should().Equal("/", "/admin/questions/review");
        NavigationItems.Sections(visible).Select(section => section.Key).Should().Equal(NavigationSection.Content);
    }

    // questions.manage alone does not open the queue (BR10: the review routes need only questions.review, and no more).
    [Fact]
    public void Visible_ManageAlone_DoesNotOfferTheQueue_AndNeitherDoesNoPermission()
    {
        var withManage = NavigationItems.Visible(NavigationItems.All, isDevelopment: false, permission => permission == QuestionBankPermissions.Manage);
        var withNothing = NavigationItems.Visible(NavigationItems.All, isDevelopment: false, _ => false);

        withManage.Select(item => item.Route).Should().NotContain("/admin/questions/review");
        withNothing.Select(item => item.Route).Should().NotContain("/admin/questions/review");
    }

    [Fact]
    public void Visible_BothPermissions_ShowTheQuestionsThenTheQueueThenTheBaseTexts()
    {
        var visible = NavigationItems.Visible(
            NavigationItems.All,
            isDevelopment: false,
            permission => permission is QuestionBankPermissions.Manage or QuestionBankPermissions.Review);

        visible.Select(item => item.Route).Should().Equal("/", "/admin/questions", "/admin/questions/review", "/admin/base-texts");
    }
}
