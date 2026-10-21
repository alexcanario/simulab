using System.Globalization;
using System.Resources;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-103 AC17: every text and every error code of the images exists in en, pt-BR and pt-PT with the words the item
/// gives. ResourceParityTests checks that no key is missing in a language; this pins what each one says.
/// </summary>
public sealed class QuestionImageResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string, string, string> Texts() => new()
    {
        { "RichText.Image", "Insert image", "Inserir imagem", "Inserir imagem" },
        { "RichText.Image.Title", "Insert image", "Inserir imagem", "Inserir imagem" },
        { "RichText.Image.File", "Image", "Imagem", "Imagem" },
        { "RichText.Image.File.Hint", "PNG, JPEG, WebP or GIF, up to 2 MB.", "PNG, JPEG, WebP ou GIF, até 2 MB.", "PNG, JPEG, WebP ou GIF, até 2 MB." },
        { "RichText.Image.Choose", "Choose image", "Escolher imagem", "Escolher imagem" },
        { "RichText.Image.Alt", "Alternative text", "Texto alternativo", "Texto alternativo" },
        { "RichText.Image.Decorative", "Decorative image (no alternative text)", "Imagem decorativa (sem texto alternativo)", "Imagem decorativa (sem texto alternativo)" },
        { "RichText.Image.Insert", "Insert", "Inserir", "Inserir" },
        { QuestionBankErrorCodes.QuestionImageNotAnImage, "The file is not a PNG, JPEG, WebP or GIF image.", "O arquivo não é uma imagem PNG, JPEG, WebP ou GIF.", "O ficheiro não é uma imagem PNG, JPEG, WebP ou GIF." },
        { QuestionBankErrorCodes.QuestionImageTooLarge, "The image is larger than 2 MB.", "A imagem tem mais de 2 MB.", "A imagem tem mais de 2 MB." },
        { QuestionBankErrorCodes.QuestionImageNotFound, "The image was not found.", "A imagem não foi encontrada.", "A imagem não foi encontrada." },
        { QuestionBankErrorCodes.QuestionImageAltTooLong, "The alternative text of an image has at most 250 characters.", "O texto alternativo de uma imagem tem no máximo 250 caracteres.", "O texto alternativo de uma imagem tem no máximo 250 caracteres." },
        { QuestionBankErrorCodes.QuestionImagesTooMany, "A question holds at most 10 images.", "Uma questão tem no máximo 10 imagens.", "Uma questão tem no máximo 10 imagens." },
        { QuestionBankErrorCodes.BaseTextImagesTooMany, "A base text holds at most 10 images.", "Um texto-base tem no máximo 10 imagens.", "Um texto de apoio tem no máximo 10 imagens." }
    };

    [Theory]
    [MemberData(nameof(Texts))]
    public void Resource_SaysTheWordsOfTheItem_InEachLanguage(string key, string en, string ptBr, string ptPt)
    {
        Manager.GetString(key, CultureInfo.InvariantCulture).Should().Be(en);
        Manager.GetString(key, new CultureInfo("en")).Should().Be(en);
        Manager.GetString(key, new CultureInfo("pt-BR")).Should().Be(ptBr);
        Manager.GetString(key, new CultureInfo("pt-PT")).Should().Be(ptPt);
    }

    // The code is the key the UI translates (rule: api-contracts): every code F-103 adds has a text in all three languages.
    [Theory]
    [InlineData(QuestionBankErrorCodes.QuestionImageNotAnImage)]
    [InlineData(QuestionBankErrorCodes.QuestionImageTooLarge)]
    [InlineData(QuestionBankErrorCodes.QuestionImageNotFound)]
    [InlineData(QuestionBankErrorCodes.QuestionImageAltRequired)]
    [InlineData(QuestionBankErrorCodes.QuestionImageAltTooLong)]
    [InlineData(QuestionBankErrorCodes.QuestionImageNotFoundOnSave)]
    [InlineData(QuestionBankErrorCodes.QuestionImagesTooMany)]
    [InlineData(QuestionBankErrorCodes.BaseTextImageAltRequired)]
    [InlineData(QuestionBankErrorCodes.BaseTextImageAltTooLong)]
    [InlineData(QuestionBankErrorCodes.BaseTextImageNotFound)]
    [InlineData(QuestionBankErrorCodes.BaseTextImagesTooMany)]
    public void EveryErrorCodeOfTheImages_HasATextInEachLanguage(string code)
    {
        foreach (var culture in new[] { "en", "pt-BR", "pt-PT" })
        {
            Manager.GetString(code, new CultureInfo(culture)).Should().NotBeNullOrWhiteSpace($"{code} in {culture}");
        }
    }
}
