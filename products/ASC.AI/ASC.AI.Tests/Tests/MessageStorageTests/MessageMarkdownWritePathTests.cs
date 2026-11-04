// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.AI.Tests.Tests.MessageStorageTests;

/// <summary>
/// Markdown on the write path — <c>append-user-message</c>, <c>get-message-by-id</c>, <c>read-messages</c>,
/// <c>update-message</c>, no model involved. The renderer of the chat depends on the text arriving as markdown source,
/// and every defect in that — an escaping step, a normalisation, a sanitiser that does not know where a fence begins —
/// comes back as HTTP 200. So each construct is stored and read back byte for byte.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Messages")]
public class MessageMarkdownWritePathTests(AspireAppFixture fixture) : MessagePart2TestBase(fixture)
{
    private static readonly Dictionary<string, (string Payload, string[] Fragments)> _cases = new()
    {
        ["headings, inline styles and a horizontal rule"] = (
            string.Join("\n",
                "# HEADONE",
                "## HEADTWO",
                "### HEADTHREE",
                "#### HEADFOUR",
                "##### HEADFIVE",
                "###### HEADSIX",
                "",
                "**BOLDWORD**, *ITALICWORD*, ***BOTHWORD***, ~~STRUCKWORD~~, `INLINEWORD`",
                "",
                "---",
                "",
                "Setext heading",
                "=============="),
            ["###### HEADSIX", "***BOTHWORD***", "~~STRUCKWORD~~", "`INLINEWORD`", "=============="]),

        ["ordered, nested and task lists"] = (
            string.Join("\n",
                "1. ORDEREDONE",
                "2. ORDEREDTWO",
                "10. ORDEREDTEN",
                "",
                "- OUTERONE",
                "  - NESTEDONE",
                "    - DEEPONE",
                "* STARITEM",
                "+ PLUSITEM",
                "",
                "- [ ] TASKOPEN",
                "- [x] TASKDONE",
                "",
                "Term",
                ": definition-ish line"),
            ["10. ORDEREDTEN", "    - DEEPONE", "* STARITEM", "+ PLUSITEM", "- [ ] TASKOPEN", "- [x] TASKDONE"]),

        ["blockquotes, including a nested one and a quoted list"] = (
            string.Join("\n",
                "> QUOTEONE",
                "> QUOTETWO",
                ">",
                "> > NESTEDQUOTE",
                ">",
                "> - QUOTEDITEM",
                ">",
                "> ```js",
                "> const inQuote = true;",
                "> ```",
                "",
                "plain line after the quote"),
            ["> > NESTEDQUOTE", "> - QUOTEDITEM", "> ```js"]),

        ["tables, including alignment markers and escaped pipes"] = (
            string.Join("\n",
                "| LEFTCELL | CENTERCELL | RIGHTCELL |",
                "| :--- | :---: | ---: |",
                "| 1 | 2 | 3 |",
                "| `a \\| b` | **BOLDCELL** | [LINKCELL](https://example.invalid/t) |",
                "",
                "| single |",
                "| --- |"),
            ["| :--- | :---: | ---: |", "`a \\| b`", "[LINKCELL](https://example.invalid/t)"]),

        ["fenced code blocks and every spelling of a language tag"] = (
            string.Join("\n",
                "```js",
                "const answer = 42;",
                "```",
                "",
                "```python",
                "print('hi')",
                "```",
                "",
                "```csharp",
                "var x = 1;",
                "```",
                "",
                "```c++",
                "int main() { return 0; }",
                "```",
                "",
                "```Dockerfile",
                "FROM alpine",
                "```",
                "",
                "```foobarlang",
                "unknown-lexer line",
                "```",
                "",
                "```js title=\"app.js\" {1,3}",
                "const withInfoString = true;",
                "```",
                "",
                "```",
                "no language tag at all",
                "```",
                "",
                "~~~sql",
                "select 1;",
                "~~~",
                "",
                "````markdown",
                "```js",
                "a fence inside a fence",
                "```",
                "````",
                "",
                "    four-space indented block"),
            [
                "```js", "```python", "```csharp", "```c++", "```Dockerfile", "```foobarlang", "```js title=\"app.js\" {1,3}",
                "~~~sql", "````markdown", "    four-space indented block"
            ]),

        ["code containing HTML, comparison operators and entity-like text"] = (
            string.Join("\n",
                "```csharp",
                "var items = new List<int>();",
                "if (a < b && c > d) { items.Add(1); }",
                "```",
                "",
                "```html",
                "<div class=\"wrapper\" data-x='1'>",
                "  <p>PARAWORD &amp; more</p>",
                "</div>",
                "```",
                "",
                "Inline: `a < b`, `x && y`, `&lt;not-an-entity&gt;`, `<b>tags</b>`"),
            [
                "new List<int>();", "if (a < b && c > d)", "<div class=\"wrapper\" data-x='1'>", "<p>PARAWORD &amp; more</p>",
                "`&lt;not-an-entity&gt;`", "`<b>tags</b>`"
            ]),

        ["links in every form, with the query strings and encodings intact"] = (
            string.Join("\n",
                "[PLAINLINK](https://example.invalid/page)",
                "[QUERYLINK](https://example.invalid/search?a=1&b=2&utm_source=x)",
                "[ANCHORLINK](https://example.invalid/doc#section-2)",
                "[TITLEDLINK](https://example.invalid/t 'A title')",
                "[UNICODELINK](https://example.invalid/search-🙂?q=%D1%82%D0%B5%D1%81%D1%82%20file)",
                "[PARENLINK](<https://example.invalid/a(b)c>)",
                "[SPACELINK](<https://example.invalid/a b c>)",
                "[MAILLINK](mailto:someone@example.invalid)",
                "[RELATIVELINK](/rooms/shared/filter?folder=12)",
                "",
                "<https://example.invalid/autolink>",
                "<someone@example.invalid>",
                "https://example.invalid/bare-url?x=1&y=2",
                "",
                "[REFLINK][ref] and ![IMAGEALT](https://example.invalid/i.png 'Img')",
                "",
                "[ref]: https://example.invalid/reference-target"),
            [
                "https://example.invalid/search?a=1&b=2&utm_source=x",
                "https://example.invalid/doc#section-2",
                "https://example.invalid/search-🙂?q=%D1%82%D0%B5%D1%81%D1%82%20file",
                "[PARENLINK](<https://example.invalid/a(b)c>)",
                "<https://example.invalid/autolink>",
                "https://example.invalid/bare-url?x=1&y=2",
                "[ref]: https://example.invalid/reference-target"
            ]),

        ["LaTeX in every delimiter, with the backslashes intact"] = (
            string.Join("\n",
                "Inline: $E = mc^2$, and \\(\\alpha + \\beta \\ne \\gamma\\).",
                "",
                "$$\\frac{-b \\pm \\sqrt{b^2 - 4ac}}{2a}$$",
                "",
                "\\[",
                "\\int_0^\\infty e^{-x^2}\\,dx = \\frac{\\sqrt{\\pi}}{2}",
                "\\]",
                "",
                "$$",
                "\\begin{pmatrix}",
                "1 & 2 \\\\",
                "3 & 4",
                "\\end{pmatrix}",
                "$$",
                "",
                "Escapes a renderer needs: \\%, \\_, \\{, \\}, \\&, \\$100, \\\\ and \\textbackslash"),
            [
                "$E = mc^2$",
                "\\(\\alpha + \\beta \\ne \\gamma\\)",
                "$$\\frac{-b \\pm \\sqrt{b^2 - 4ac}}{2a}$$",
                "\\int_0^\\infty e^{-x^2}\\,dx",
                "\\begin{pmatrix}",
                "1 & 2 \\\\",
                "\\%, \\_, \\{, \\}, \\&, \\$100, \\\\ and \\textbackslash"
            ])
    };

    public static TheoryData<string> Cases => new(_cases.Keys);

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task AppendGetUpdate_MarkdownConstructs_SurviveVerbatim(string construct)
    {
        var (payload, fragments) = _cases[construct];

        await ExpectStoredVerbatimAsync(payload, fragments);
    }

    /// <remarks>
    /// Separate from the link case, because the claim is different: the API neither strips nor neutralises an
    /// active-scheme href, so the renderer is the only thing between the text and a click. Not an XSS finding — a
    /// JSON string has no HTML context; whether the anchor is clickable is a browser assertion. If a future build
    /// starts sanitising these on the way in, this goes red and the decision is a product one.
    /// </remarks>
    [Fact]
    public async Task AppendUserMessage_ALinkWithAnActiveScheme_IsStoredExactlyAsTyped()
    {
        var payload = string.Join("\n",
            "[SCRIPTLINK](javascript:alert(1))",
            "[DATALINK](data:text/plain;base64,SEVMTE8=)",
            "[VBLINK](vbscript:msgbox(1))",
            "[FILELINK](file:///etc/passwd)",
            "[CASELINK](JaVaScRiPt:alert(2))");

        await ExpectStoredVerbatimAsync(payload, ["javascript:alert(1)", "data:text/plain;base64,SEVMTE8=", "JaVaScRiPt:alert(2)"]);
    }
}
