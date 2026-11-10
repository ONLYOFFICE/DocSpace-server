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
/// Markdown of a reply: the model's answer is scripted, so what is measured is the reply path alone — the stream, the
/// storage and <c>read-messages</c> must hand back the markdown source the model produced: no HTML, no escaping, every
/// backslash of a formula and every <c>&lt;</c> inside a fence intact, snapshots that only ever grow, and the same on
/// the regenerate path.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Messages")]
public class MessageMarkdownReplyTests(AspireAppFixture fixture) : MessagePart2TestBase(fixture)
{
    [Fact]
    public async Task ReadMessages_AReply_KeepsEveryMarkdownConstructAsSource()
    {
        using var provider = new StubModelProvider();

        var reply = (await ReplyWithAsync(provider, RenderBlock)).Text;

        foreach (var construct in new[]
                 {
                     "## RENDERCHECK", "### RENDERSUB", "**BOLDWORD**", "*ITALICWORD*", "`INLINEWORD`", "~~STRUCKWORD~~",
                     "1. ORDEREDONE", "2. ORDEREDTWO", "- OUTERONE", "  - NESTEDONE", "> QUOTEWORD",
                     // The whole href, `&` included: an entity-escaped query string is a link that goes somewhere else.
                     "[LINKWORD](https://example.invalid/docs?a=1&b=2#frag)",
                     "| CELLA | CELLB |", "| --- |", "```python", "print(\"CODEWORD\")", "---"
                 })
        {
            reply.Should().Contain(construct, $"the markdown construct {JsonSerializer.Serialize(construct)} did not survive");
        }

        ExpectNoRenderingOrEscaping(reply);
        reply.Trim().Should().Be(RenderBlock.Trim());
    }

    [Fact]
    public async Task ReadMessages_AReply_KeepsLatexDelimitersAndBackslashes()
    {
        // A formula is delimiters plus backslashes, exactly what a JSON round-trip, a markdown escaper or an
        // over-eager unescaper eat — and every one of those is a 200 with a reply that never renders.
        var block = string.Join("\n",
            "LATEXCHECK",
            "",
            "Inline: $E = mc^2$ and \\(\\alpha + \\beta\\).",
            "",
            "$$\\frac{NUMERWORD}{DENOMWORD} = \\sqrt{GAMMAWORD}$$",
            "",
            "\\[ \\int_0^1 x^2\\,dx = \\frac{1}{3} \\]");

        using var provider = new StubModelProvider();

        var reply = (await ReplyWithAsync(provider, block)).Text;

        foreach (var construct in new[]
                 {
                     "$E = mc^2$", "\\(\\alpha + \\beta\\)", "$$\\frac{NUMERWORD}{DENOMWORD} = \\sqrt{GAMMAWORD}$$", "\\[",
                     "\\int_0^1 x^2\\,dx", "\\frac{1}{3}"
                 })
        {
            reply.Should().Contain(construct, $"the LaTeX fragment {JsonSerializer.Serialize(construct)} did not survive");
        }

        // Display math needs both dollars: `$$` collapsed to `$` renders the formula inline, or not at all.
        System.Text.RegularExpressions.Regex.Count(reply, @"\$\$").Should().BeGreaterThanOrEqualTo(2, "the display-math delimiters were not delivered in pairs");

        // ...and nothing doubled or escaped the backslashes on the way through.
        foreach (var mangled in new[] { "\\\\frac", "\\\\alpha", "\\\\(", "\\$", "&#36;", "&bsol;", "\\\\[" })
        {
            reply.Should().NotContain(mangled, $"the LaTeX arrived mangled: {JsonSerializer.Serialize(mangled)}");
        }

        // And it is LaTeX source, not something already turned into markup.
        foreach (var tell in new[] { "<span", "katex", "MathJax", "<math", "&lt;" })
        {
            reply.Should().NotContain(tell, $"the formula arrived pre-rendered: {tell}");
        }

        reply.Trim().Should().Be(block.Trim());
    }

    [Fact]
    public async Task ReadMessages_ACodeBlock_KeepsHtmlAndComparisonOperators()
    {
        // A reply passes through something that strips raw markup, and that step has no idea where a fence begins:
        // a C# snippet is where it would show up as silently wrong code.
        var block = string.Join("\n",
            "CODECHECK",
            "",
            "```csharp",
            "var items = new List<int>();",
            "if (a < b && c > d) { items.Add(1); }",
            "```");

        using var provider = new StubModelProvider();

        var reply = (await ReplyWithAsync(provider, block)).Text;

        foreach (var construct in new[] { "```csharp", "new List<int>();", "if (a < b && c > d)", "items.Add(1);" })
        {
            reply.Should().Contain(construct, $"the code fragment {JsonSerializer.Serialize(construct)} did not survive the reply path");
        }

        foreach (var tell in new[] { "&lt;", "&gt;", "&amp;", "&#" })
        {
            reply.Should().NotContain(tell, $"the code inside the fence arrived entity-escaped: {tell}");
        }
    }

    [Fact]
    public async Task SendWithStream_TheStreamedSnapshots_OnlyEverGrow_AndEndOnTheStoredText()
    {
        using var provider = new StubModelProvider();

        var scripted = await ReplyWithAsync(provider, RenderBlock);
        var round = scripted.Round;

        // The protocol hands a client cumulative snapshots; one that rewrites what was already shown makes a
        // half-arrived table or fence flicker mid-render — which the final text alone cannot see. message-start
        // already carries the first snapshot (the text of the first provider chunk), so the sequence begins there.
        var snapshots = round.Frames
            .Where(f => f.TryGetProperty("type", out var type) && type.GetString() is "message-start" or "message-delta")
            .Select(f => FrameText(f))
            .OfType<string>()
            .ToList();
        snapshots.Should().HaveCountGreaterThan(1, $"the reply arrived in more than one frame, so there is a sequence to check: {round}");

        for (var i = 1; i < snapshots.Count; i++)
        {
            snapshots[i].StartsWith(snapshots[i - 1], StringComparison.Ordinal).Should().BeTrue(
                $"frame {i} does not extend frame {i - 1}: a client would have to re-render text it had already shown");
        }

        // The last snapshot is what a client holds when the stream closes, the thread what it holds after a reload.
        var streamed = round.AssistantText;
        streamed.Should().Be(snapshots[^1], round.ToString());
        scripted.Text.Trim().Should().Be(streamed!.Trim(), "the markdown shown while streaming differs from the markdown stored");
        scripted.Text.Trim().Should().Be(RenderBlock.Trim());
    }

    [Fact]
    public async Task RegenerateStream_ARegeneratedReply_IsMarkdownToo()
    {
        // Regenerate is a second inference path that stores over the first reply: any post-processing living on only
        // one of the two would leave "Try again" giving a differently formatted answer.
        using var provider = new StubModelProvider();

        var original = await ReplyWithAsync(provider, "A first, plain answer.");
        provider.Enqueue(StubModelProvider.Text(RenderBlock));

        var regenerate = await RegenerateStreamAsync(
            original.ThreadId,
            original.ProfileId,
            new AiActionArgs(prompt: new AiActionArgsPrompt(AiActionArgsPrompt.ModeEnum.Replace, "Reproduce the block verbatim.")));

        regenerate.StatusCode.Should().Be(HttpStatusCode.OK, regenerate.ToString());
        regenerate.StreamError.Should().BeNull($"{regenerate}\n{provider.Describe()}");

        var messages = await WaitForMessagesAsync(
            original.ThreadId,
            list => AssistantMessages(list).Any(m => TextOf(m)?.Contains("RENDERCHECK", StringComparison.Ordinal) == true));
        var regenerated = TextOf(AssistantMessages(messages).Last()) ?? string.Empty;

        regenerated.Should().Contain("```python", $"the regenerated reply is the new answer: {Describe(messages)}");

        foreach (var construct in new[]
                 {
                     "## RENDERCHECK", "**BOLDWORD**", "> QUOTEWORD", "[LINKWORD](https://example.invalid/docs?a=1&b=2#frag)",
                     "| CELLA | CELLB |", "```python"
                 })
        {
            regenerated.Should().Contain(construct, $"the regenerated reply lost {JsonSerializer.Serialize(construct)}");
        }

        ExpectNoRenderingOrEscaping(regenerated);
    }

    private static void ExpectNoRenderingOrEscaping(string reply)
    {
        foreach (var tell in HtmlTells)
        {
            reply.Should().NotContain(tell, $"the reply arrived rendered as HTML: {tell}");
        }

        foreach (var tell in EscapeTells)
        {
            reply.Should().NotContain(tell, $"the reply arrived escaped: {JsonSerializer.Serialize(tell)}");
        }
    }
}
