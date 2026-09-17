using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

public class ErrorTextTests : KitTestContext
{
    private ErrorText Sut => Services.GetRequiredService<ErrorText>();

    [Fact]
    public void For_KnownCode_ReturnsResourceText()
    {
        Sut.For("common.not_found").Should().Be("The item was not found. It may have been deleted.");
    }

    [Theory]
    [InlineData("exam_board.does_not_exist_yet")]
    [InlineData("")]
    [InlineData(null)]
    public void For_UnknownCode_ReturnsGenericMessageAndNeverTheCode(string? code)
    {
        var text = Sut.For(code);

        text.Should().Be("Something went wrong. Please try again.");
        if (!string.IsNullOrEmpty(code))
            text.Should().NotContain(code);
    }

    [Fact]
    public void For_KnownCodeInPortuguese_ReturnsTranslatedText()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("pt-PT");

        Sut.For("common.not_found").Should().Be("O item não foi encontrado. Pode ter sido eliminado.");
    }
}
