using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Ganss.Xss;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain;

namespace Simulab.QuestionBank.Application.Content;

/// <summary>
/// The one place that decides what HTML a question may store (F-95, BR8 and BR8b). Every text field passes
/// through it on every write; the Web shows what comes back and never calls it. The allow-list is a single
/// list a test pins, so widening it is a visible change.
/// <para>
/// Three steps, in this order: a pre-pass with the HTML parser that drops what must lose its text as well
/// (<c>script</c>, <c>style</c>, ...), turns the editor's bullet lists into <c>ul</c> and the block wrappers the
/// allow-list lacks (<c>div</c>, <c>h1</c>) into paragraphs; the allow-list sanitizer; and a post-pass that
/// leaves a link only with an absolute http or https address and the safe <c>rel</c>.
/// </para>
/// </summary>
public sealed partial class QuestionHtmlSanitizer
{
    private static readonly string[] Allowed =
    [
        "p", "br", "strong", "b", "em", "i", "u", "s", "sub", "sup", "ul", "ol", "li", "blockquote",
        "table", "thead", "tbody", "tr", "th", "td", "a", "img"
    ];

    // F-103 BR5: the one address an img may have, the route the Web serves a stored image from. Anchored, so a
    // protocol-relative address, a query string or anything after the id is another address and loses the img.
    [GeneratedRegex(@"^/question-images/(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$")]
    private static partial Regex StoredImageAddress();

    // Removed with everything inside: their text is code or markup, never something to read.
    private static readonly string[] Dropped =
    [
        "script", "style", "iframe", "object", "embed", "svg", "math", "noscript", "template", "head", "title",
        "form", "input", "button", "select", "textarea", "link", "meta", "base", "frame", "frameset", "applet"
    ];

    // Block wrappers the allow-list lacks: they become paragraphs, so their text keeps its own line.
    private static readonly string[] BecomeParagraphs =
    [
        "div", "section", "article", "header", "footer", "aside", "nav", "main", "h1", "h2", "h3", "h4", "h5",
        "h6", "pre", "address", "figure", "figcaption"
    ];

    // Where the plain text needs a space so two blocks never run into one word.
    private static readonly string[] Blocks =
    [
        "p", "br", "ul", "ol", "li", "blockquote", "table", "thead", "tbody", "tr", "th", "td"
    ];

    private const string LinkRel = "noopener noreferrer";

    // The allow-list this instance applies: always the one list of BR8b, held per instance so a handler's
    // sanitizer is a value it owns and never a global switch.
    private readonly IReadOnlyList<string> _allowedTags = Allowed;

    /// <summary>The tags a stored text may hold (BR8).</summary>
    public static IReadOnlyList<string> AllowedTags => Allowed;

    /// <summary>
    /// The sanitized HTML, its plain text and the stored images it holds; empty when <paramref name="html"/> is null or only
    /// empty tags. <paramref name="allowImages"/> is off for a field that takes no picture (the explanation, F-103 BR1).
    /// <paramref name="readBlankMarkers"/> is on for the statement of a fill-in-the-blanks question (F-107, BR3, BR9): the
    /// markers are read from the text between tags only, and each reads as <c>____</c> in the plain text.
    /// </summary>
    public SanitizedHtml Sanitize(string? html, bool allowImages = true, bool readBlankMarkers = false)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return SanitizedHtml.Empty;
        }

        var safe = SanitizeAllowList(Prepare(html, allowImages)).Trim();
        var (plain, images) = ReadBack(safe);

        // Only empty tags is empty: nothing to store, so the field is the same as one that was not sent.
        return plain.Length == 0 && images.Ids.Count == 0
            ? SanitizedHtml.Empty
            : new SanitizedHtml(safe, plain)
            {
                ImageIds = images.Ids,
                ImagesWithoutAlt = images.WithoutAlt,
                AltTooLong = images.AltTooLong
            };
    }

    private static string Prepare(string html, bool allowImages)
    {
        var document = new HtmlParser().ParseDocument(html);

        foreach (var element in document.QuerySelectorAll(string.Join(',', Dropped)).ToList())
        {
            element.Remove();
        }

        KeepStoredImages(document, allowImages);

        NormalizeEditorLists(document);

        foreach (var element in document.QuerySelectorAll(string.Join(',', BecomeParagraphs)).ToList())
        {
            var paragraph = document.CreateElement("p");
            foreach (var child in element.ChildNodes.ToList())
            {
                paragraph.AppendChild(child);
            }

            element.Replace(paragraph);
        }

        return document.Body?.InnerHtml ?? string.Empty;
    }

    // BR5: an img is kept only with the address of a stored image, rewritten to its canonical form; any other address
    // (http, https, data:, javascript:, another path) removes the element. BR1: a field that takes no picture loses all.
    private static void KeepStoredImages(IDocument document, bool allowImages)
    {
        foreach (var image in document.QuerySelectorAll("img").ToList())
        {
            var match = allowImages ? StoredImageAddress().Match(image.GetAttribute("src") ?? string.Empty) : Match.Empty;
            if (!match.Success)
            {
                image.Remove();

                continue;
            }

            image.SetAttribute("src", $"/question-images/{Guid.Parse(match.Groups["id"].Value):D}");
        }
    }

    // Quill 2 writes every list as an <ol> whose <li> carry data-list="bullet" or "ordered". The sanitizer
    // drops data-*, which would turn each bullet list into a numbered one, so the type is read here and the
    // list is rebuilt: one <ul> or <ol> per run of items of the same type.
    private static void NormalizeEditorLists(IDocument document)
    {
        foreach (var list in document.QuerySelectorAll("ol").ToList())
        {
            var items = list.Children.Where(child => child.LocalName == "li").ToList();
            if (!items.Any(item => item.HasAttribute("data-list")))
            {
                continue;
            }

            var rebuilt = new List<INode>();
            IElement? current = null;
            foreach (var item in items)
            {
                var bullet = string.Equals(item.GetAttribute("data-list"), "bullet", StringComparison.OrdinalIgnoreCase);
                var name = bullet ? "ul" : "ol";
                if (current is null || current.LocalName != name)
                {
                    current = document.CreateElement(name);
                    rebuilt.Add(current);
                }

                current.AppendChild(item);
            }

            list.Replace([.. rebuilt]);
        }
    }

    private string SanitizeAllowList(string html)
    {
        var options = new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string>(_allowedTags, StringComparer.OrdinalIgnoreCase),
            AllowedAttributes = new HashSet<string>(["href", "src", "alt"], StringComparer.OrdinalIgnoreCase),
            AllowedSchemes = new HashSet<string>(["http", "https"], StringComparer.OrdinalIgnoreCase),
            AllowedCssProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };
        var sanitizer = new HtmlSanitizer(options) { KeepChildNodes = true, AllowDataAttributes = false };
        sanitizer.PostProcessNode += (_, args) => TidyLink(args.Node);

        return sanitizer.Sanitize(html);
    }

    // BR8: href is for links and only an absolute http or https address survives; a link always carries the
    // safe rel and never a target.
    private static void TidyLink(INode node)
    {
        if (node is not IElement element)
        {
            return;
        }

        // F-103 BR5: src and alt belong to img alone.
        if (!string.Equals(element.LocalName, "img", StringComparison.OrdinalIgnoreCase))
        {
            element.RemoveAttribute("src");
            element.RemoveAttribute("alt");
        }

        if (!string.Equals(element.LocalName, "a", StringComparison.OrdinalIgnoreCase))
        {
            element.RemoveAttribute("href");

            return;
        }

        var href = element.GetAttribute("href");
        var absolute = Uri.TryCreate(href, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        if (!absolute)
        {
            element.RemoveAttribute("href");
        }

        element.RemoveAttribute("target");
        element.SetAttribute("rel", LinkRel);
    }

    private readonly record struct ImagesFound(List<Guid> Ids, int WithoutAlt, int AltTooLong);

    // The plain text and the images of the HTML that is going to be stored, from one parse. The alt of a picture is part of
    // the plain text (BR8): a decorative one (an empty alt) adds nothing. A picture counts with no alt attribute at all (BR4).
    private static (string PlainText, ImagesFound Images) ReadBack(string safeHtml)
    {
        var ids = new List<Guid>();
        if (safeHtml.Length == 0)
        {
            return (string.Empty, new ImagesFound(ids, 0, 0));
        }

        var document = new HtmlParser().ParseDocument(safeHtml);
        var withoutAlt = 0;
        var altTooLong = 0;
        foreach (var image in document.QuerySelectorAll("img").ToList())
        {
            var match = StoredImageAddress().Match(image.GetAttribute("src") ?? string.Empty);
            if (match.Success)
            {
                ids.Add(Guid.Parse(match.Groups["id"].Value));
            }

            var alt = image.GetAttribute("alt");
            if (alt is null)
            {
                withoutAlt++;
            }
            else if (alt.Length > QuestionLimits.ImageAltMaxLength)
            {
                altTooLong++;
            }

            if (!string.IsNullOrWhiteSpace(alt))
            {
                image.Before(document.CreateTextNode($" {alt} "));
            }
        }

        foreach (var block in document.QuerySelectorAll(string.Join(',', Blocks)).ToList())
        {
            block.Before(document.CreateTextNode(" "));
            block.After(document.CreateTextNode(" "));
        }

        var plain = WhiteSpace().Replace(document.Body?.TextContent ?? string.Empty, " ").Trim();

        return (plain, new ImagesFound(ids, withoutAlt, altTooLong));
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpace();
}
