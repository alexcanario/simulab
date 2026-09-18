using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>The strength bar reads the same rules the API enforces; the API stays the authority.</summary>
public class PasswordStrengthRulesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    public void Of_EmptyOrTooShort_IsWeak(string? password)
    {
        PasswordStrengthRules.Of(password).Should().Be(PasswordStrength.Weak);
    }

    [Theory]
    [InlineData("estudarmuito1")]   // length and digit, no uppercase and no symbol
    [InlineData("Estudar#")]        // uppercase and symbol, too short and no digit
    public void Of_SomeRulesMet_IsFair(string password)
    {
        PasswordStrengthRules.Of(password).Should().Be(PasswordStrength.Fair);
    }

    [Fact]
    public void Of_EveryRuleMet_IsStrong()
    {
        PasswordStrengthRules.Of("Estudar#2026!").Should().Be(PasswordStrength.Strong);
        PasswordStrengthRules.Meets("Estudar#2026!").Should().BeTrue();
    }

    [Theory]
    [InlineData("Estudar#202")]     // 11 characters
    [InlineData("estudar#2026!")]   // no uppercase
    [InlineData("EstudarMuito!")]   // no digit
    [InlineData("EstudarMuito2026")] // no symbol
    public void Meets_MissingOneRule_IsFalse(string password)
    {
        PasswordStrengthRules.Meets(password).Should().BeFalse();
    }
}
