# F-95 — Question text format: Markdown or sanitized HTML

Technical terms: [glossary](../../glossary.md)

Refinement material of [F-95](../F-95-single-choice-questions.md). Not an item file. Measured on 2026-10-10 in two scratch projects (net10.0, outside the repository); nothing here is in `src/`.

## The decision
How the statement, options, base text and explanation of a question are stored, edited and shown. The owner chose a visual (WYSIWYG) editor; this file compares the two formats that editor could sit on.

## Measured behaviour
| Input | Markdig 1.3.2 (`DisableHtml()`, the `LegalDocumentProvider` pipeline) | HtmlSanitizer 9.2.1039 (defaults) |
|---|---|---|
| `<script>alert(1)</script>` | text `&lt;script&gt;...` (harmless) | removed |
| `<b onclick=x>hi</b>` | text `&lt;b onclick=x&gt;...` (harmless) | `<b>hi</b>` |
| link `javascript:alert(1)` | **`<a href="javascript:alert(1)">`** | `<a>x</a>` (href dropped) |
| image `data:text/html;base64,...` | **`<img src="data:text/html;...">`** | `<img>` (src dropped) |
| `<table>...</table>` | pipe table only (`UsePipeTables`) | kept |
| superscript / subscript | `^2^`, `~2~` only with `UseEmphasisExtras` | `<sup>`, `<sub>` kept |
| inline `style` | not applicable | kept, rewritten (`color: rgba(...)`) |

Markdown with raw HTML off is therefore not safe by itself: a link and image scheme filter (only `http` and `https`) is code this item would write and test. The sanitizer does it by default.

## Side by side
| | A. Markdown with a link filter | B. HTML with a sanitizer |
|---|---|---|
| New packages in production | none (Markdig 1.3.2 is already in `Directory.Packages.props`; 1.4.0 is the latest stable, 2026-09-20) | `HtmlSanitizer` 9.2.1039 (MIT, 2026-08-28) |
| Editor on the screen | text box with live preview, no package | a rich-text editor: `Blazored.TextEditor` 1.1.3 (MIT, wraps Quill 2.0.3, last release 2025-04-11) |
| WYSIWYG editor | no maintained MIT Blazor editor found for Markdown (Blazorise packages carry their own license) | yes, as above |
| What is stored | Markdown source; rendered on read | sanitized HTML; sanitized again on every write |
| Import from Simulae (F-96, F-110) | converts HTML to Markdown, lossy for tables and inline styles | stores Simulae HTML after sanitizing, no conversion |
| Tables (past papers have them) | pipe tables, awkward to type | native, subject to the editor's table support (Quill 2 table module: not verified here, to check at build) |
| Images later (F-103) | `![alt](file-id)` rewritten at render | `<img>` with a file id, same rewrite |
| Accessibility of the editor | plain `textarea`, known | third-party widget: needs its own keyboard and screen-reader test (WCAG 2.2 AA) |
| JavaScript / hosting | none | Quill's script and style from a CDN by default; the build would host them with the app |
| Attack surface | a filter we write | a maintained sanitizer; the editor is the new surface |
| Text search over statements (F-100) | the source is clean text | HTML tags in the column; search needs a stripped copy |
| Effort inside F-95 | smaller | larger (editor wrapper, sanitizer, test of both, hosting of the script) |

## Recommendation
Given the visual editor the owner chose: **B, sanitized HTML**. A visual editor produces HTML, so Markdown would add a conversion in both directions. If the visual editor is dropped in favour of the live-preview text box, **A** is smaller and needs no package.

## What would change in the item
- B: new packages `HtmlSanitizer` and `Blazored.TextEditor` listed for approval (version and license above, checked 2026-10-10); one sanitizer in a shared place, run by the Api on every write; the Web shows the stored HTML as-is; a column with the text stripped of tags for search.
- A: no new package; one renderer with the scheme filter, run by the Api; the Web shows the rendered HTML.
