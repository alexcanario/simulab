using System.Text.RegularExpressions;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-183 AC7, `## Approved list` of the item: each field of a page uses the control of its kind of text. Read from the
/// page source, since several of these fields only appear deep in a flow (an expired link, a dialog, a publish step);
/// each page's own tests prove it still works.
/// </summary>
public sealed class TextKindPageFieldsTests
{
    public static TheoryData<string, string, string> Fields => new()
    {
        { "Identity/SignIn.razor", "sign-in-email", "AppEmailField" },
        { "Identity/SignUp.razor", "sign-up-email", "AppEmailField" },
        { "Identity/ForgotPassword.razor", "forgot-password-email", "AppEmailField" },
        { "Identity/ResetPassword.razor", "reset-password-email", "AppEmailField" },
        { "Identity/CheckEmail.razor", "check-email-address", "AppEmailField" },
        { "Identity/VerifyEmail.razor", "verify-email-address", "AppEmailField" },
        { "Identity/Security.razor", "security-enrol-code", "AppOtpField" },
        { "Identity/Security.razor", "security-regenerate-code", "AppCodeField" },
        { "Identity/Security.razor", "security-disable-code", "AppCodeField" },
        { "Catalog/ExamEditionForm.razor", "edition-notice-url", "AppUrlField" },
        { "Catalog/IssuingAuthorityDialog.razor", "issuing-authority-website", "AppUrlField" },
        { "Catalog/OrganizerDialog.razor", "organizer-website", "AppUrlField" },
        { "Catalog/IssuingAuthorityDialog.razor", "issuing-authority-description", "AppTextAreaField" },
        { "Catalog/OrganizerDialog.razor", "organizer-description", "AppTextAreaField" },
        { "Dev/AiDiagnostics.razor", "ai-prompt", "AppTextAreaField" },
        { "QuestionBank/QuestionForm.razor", "@PublishReasonId", "AppTextAreaField" },
        { "QuestionBank/QuestionForm.razor", "@ChangeReasonId", "AppTextAreaField" },
        { "QuestionBank/QuestionStatusReasonDialog.razor", "question-status-reason", "AppTextAreaField" },
    };

    [Theory]
    [MemberData(nameof(Fields))]
    public void Field_UsesTheControlOfItsKind(string page, string id, string control)
    {
        var source = File.ReadAllText(Path.Combine(PagesRoot(), page.Replace('/', Path.DirectorySeparatorChar)));

        ControlsOf(source, id).Should().Equal([control], because: $"{id} in {page} is a field of that kind (F-183)");
    }

    // The sign-in code step has two fields with one id: the authenticator code, then the recovery code.
    [Fact]
    public void SignInCode_IsAOneTimeCodeOrARecoveryCode()
    {
        var source = File.ReadAllText(Path.Combine(PagesRoot(), "Identity", "SignIn.razor"));

        ControlsOf(source, "sign-in-code").Should().BeEquivalentTo(["AppOtpField", "AppCodeField"]);
    }

    private static List<string> ControlsOf(string source, string id) =>
        Regex.Matches(source, $"""<(App\w+)\b[^<>]*?\bId="{Regex.Escape(id)}" """.TrimEnd(), RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value)
            .ToList();

    private static string PagesRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory!.FullName, "src", "Hosts", "Simulab.Web", "Components", "Pages");
    }
}
