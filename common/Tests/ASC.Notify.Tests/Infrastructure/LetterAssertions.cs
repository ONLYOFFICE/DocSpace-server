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

namespace ASC.Notify.Tests.Infrastructure;

/// <summary>
/// The checks that hold for every letter however its tags were produced, shared by the harness that
/// declares them and the one that takes them from the action itself.
/// </summary>
internal static class LetterAssertions
{
    /// <summary>
    /// What an unrecognised textile link looks like once the styler has run: the closing quotation mark
    /// of the caption, the colon, the opening one of the address — straight or curled — and the address
    /// itself. A rendered link never keeps that shape, since it becomes an <c>&lt;a href&gt;</c>.
    /// </summary>
    private static readonly Regex _unrenderedLink =
        new("""(&#8221;|&#8220;|")\s*:\s*(&#8220;|&#8221;|")(https?://|mailto:)""", RegexOptions.Compiled);

    /// <summary>
    /// A tag reference inside another tag's value — <c>TagValues.TrulyYours</c> signs off with
    /// "Truly Yours, <c>${LetterLogoText}</c> Team", so the value is resolved further before it lands in
    /// the body and only the stretches between the references are carried through verbatim.
    /// </summary>
    private static readonly Regex _tagReference = new(@"\$\{?[a-zA-Z0-9_]+\}?", RegexOptions.Compiled);

    /// <summary>
    /// Nothing environment-specific may be baked into this culture's pattern text:
    /// <list type="bullet">
    /// <item>the product name is <c>${LetterLogoText}</c>, so a white-labelled portal sends its own
    /// branding;</item>
    /// <item>links arrive as tags (<c>$URL1</c>, <c>${__VirtualRootPath}</c>, …) whose values the sending
    /// code resolves from <c>externalresources.json</c> and from the portal address.</item>
    /// </list>
    /// </summary>
    public static void ShouldHavePortablePattern(this RenderedLetter letter)
    {
        foreach (var text in new[] { letter.SubjectPattern, letter.BodyPattern })
        {
            text.Should().NotContain(LetterEnvironment.LogoText,
                $"the letter must carry ${{{CommonTags.LetterLogoText}}} instead of the product name, so "
                + "white-labelled portals send their own branding");

            text.Should().NotContain("http://", "links must come from tags, not be hard-coded in the pattern")
                .And.NotContain("https://", "links must come from tags, not be hard-coded in the pattern");
        }
    }

    /// <summary>
    /// Every tag this culture's pattern references must have been given a value: a forgotten one leaves
    /// the raw <c>$Tag</c> / <c>${Tag}</c> in the letter, which is exactly what the reader would see.
    /// Reading the list from the culture under test is what catches a tag a translator introduced.
    /// </summary>
    /// <param name="whoOwesTheValue">
    /// Where a missing value should have come from, so the failure names the place to fix.
    /// </param>
    public static void ShouldHaveNoUnresolvedTags(this RenderedLetter letter, string whoOwesTheValue)
    {
        var tags = letter.ReferencedTags;

        tags.Should().NotBeEmpty("every letter substitutes at least the user name; an empty list means the "
            + "pattern was not found and this check would silently pass");

        foreach (var tag in tags)
        {
            var because = $"tag '{tag}' has no value in {whoOwesTheValue}";

            letter.Subject.Should().NotContain($"${tag}", because).And.NotContain($"${{{tag}}}", because);
            letter.Body.Should().NotContain($"${tag}", because).And.NotContain($"${{{tag}}}", because);
        }
    }

    /// <summary>
    /// A textile link is written <c>"caption":"address"</c> and is recognised only when nothing is glued
    /// to it. A particle in Japanese or Korean, a case suffix in Finnish or Azerbaijani, a hyphen in
    /// Armenian — any of them is enough for the parser to walk past, and it then leaves the caption, the
    /// colon and the address standing in the text, merely curling the quotation marks. The letter still
    /// renders and still contains the address, so a check that the address is present passes while the
    /// reader is looking at <c>“Help Center”:”https://…”</c>.
    /// </summary>
    public static void ShouldHaveRenderedLinks(this RenderedLetter letter)
    {
        var raw = _unrenderedLink.Matches(letter.Body)
            .Select(match => match.Value)
            .ToArray();

        raw.Should().BeEmpty("a textile link was not recognised and its address is printed to the reader; "
            + "the translation most likely glues a particle or a suffix to the closing quotation mark — "
            + "put a space there");
    }

    /// <summary>
    /// The picture at the top of the letter, whichever of the two it is. An action that sets a
    /// <c>TopGif</c> gets it; one that does not is shown the tenant letter logo, which
    /// <c>NotifyTransferRequest.AddLetterLogoAsync</c> attaches to the message and references as a
    /// <c>cid:</c> — never as a file under the image folder, which is why nothing here looks for one.
    /// </summary>
    public static void ShouldHaveTopImage(this RenderedLetter letter, List<ITagValue> tags)
    {
        var topGif = tags.Find(tag => tag.Tag == CommonTags.TopGif)?.Value as string;

        if (!string.IsNullOrEmpty(topGif))
        {
            letter.Body.Should().Contain(topGif, "the top image the action sets must reach the letter");

            return;
        }

        var logo = tags.Find(tag => tag.Tag == CommonTags.LetterLogo)?.Value as string;

        logo.Should().StartWith("cid:", "a letter without a top image is sent the letter logo as an attachment");

        letter.Body.Should().Contain(logo!, "the letter logo must be referenced by the content id it was attached under");

        tags.Should().Contain(tag => tag.Tag == CommonTags.EmbeddedAttachments,
            "the content id has to point at something, or the reader sees a broken image");
    }

    /// <summary>
    /// A tag value that is markup has to reach the letter as markup. Several of them are —
    /// <c>TagValues.OrangeButton</c> is the whole call-to-action, <c>TagValues.TrulyYours</c> the
    /// signature, <c>TableTop</c>/<c>TableBottom</c> the item table — and the reader sees the difference
    /// immediately: escaped, the letter prints its own HTML in the middle of the text.
    ///
    /// The engine hands every interpolated value through a no-textile zone that encodes it on the way in
    /// and decodes it on the way out (<c>NoTextileBlockModifier</c>), so this is the one property of that
    /// round trip a letter test can state: what the action put in a tag is what the body carries. The
    /// checks that came before this one all passed while the markup was escaped — the button URL, the
    /// caption and the site link are still present as text, just no longer clickable.
    ///
    /// A value the action escaped itself (<c>keyName.HtmlEncode()</c>, a display name) carries no raw
    /// <c>&lt;</c> and is deliberately not covered here: it must stay escaped, which is what
    /// <c>ApiKeyExpiredLetterTests</c> asserts.
    ///
    /// Only the tags this culture's pattern actually references are checked, the same list
    /// <see cref="ShouldHaveNoUnresolvedTags"/> reads. An action is free to set a tag its letter never
    /// prints — <c>profile_delete</c> is handed a <c>TrulyYours</c> and the payment warnings an
    /// <c>OrangeButton</c> that their patterns do not mention — and a value that is never substituted
    /// has nothing to say about how markup survives rendering.
    /// </summary>
    public static void ShouldRenderMarkupTags(this RenderedLetter letter, List<ITagValue> tags)
    {
        foreach (var tag in tags)
        {
            if (tag.Value is not string value || !value.Contains('<'))
            {
                continue;
            }

            if (!letter.ReferencedTags.Contains(tag.Tag))
            {
                continue;
            }

            // A value spanning several lines is wrapped line by line before the styler runs, so it has no
            // single verbatim form to look for. None of the markup tags is one today.
            if (value.Contains('\n') || value.Contains('\r'))
            {
                continue;
            }

            foreach (var fragment in _tagReference.Split(value))
            {
                if (!fragment.Contains('<'))
                {
                    continue;
                }

                letter.Body.Should().Contain(fragment,
                    $"tag '{tag.Tag}' carries markup the letter is built out of; escaped, the reader is "
                    + "shown the HTML instead of what it renders");
            }
        }
    }

    /// <summary>
    /// That the letter signed off, when the action signed it. Whether a letter has a signature at all,
    /// which resource it uses and whether it is a table row of its own are the action's decisions, so
    /// there is nothing here for a test to declare — only that what the action produced survived
    /// rendering. The wording itself belongs to <c>AssertDefaultCultureText</c>.
    /// </summary>
    public static void ShouldHaveSignature(this RenderedLetter letter, List<ITagValue> tags)
    {
        var signature = tags.Find(tag => tag.Tag == "TrulyYours")?.Value as string;

        if (string.IsNullOrEmpty(signature))
        {
            return;
        }

        letter.Body.Should().Contain(LetterEnvironment.SiteUrl,
            "the signature links to the site, so that link has to survive rendering");
    }
}
