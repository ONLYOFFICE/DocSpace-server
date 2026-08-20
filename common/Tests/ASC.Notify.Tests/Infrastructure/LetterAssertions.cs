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
    /// Nothing environment-specific may be baked into the pattern text:
    /// <list type="bullet">
    /// <item>the product name is <c>${LetterLogoText}</c>, so a white-labelled portal sends its own
    /// branding;</item>
    /// <item>links arrive as tags (<c>$URL1</c>, <c>${__VirtualRootPath}</c>, …) whose values the sending
    /// code resolves from <c>externalresources.json</c> and from the portal address.</item>
    /// </list>
    /// </summary>
    public static void PatternIsPortable(IPattern pattern)
    {
        foreach (var text in new[] { pattern.Subject(), pattern.Body() })
        {
            text.Should().NotContain(LetterEnvironment.LogoText,
                $"the letter must carry ${{{CommonTags.LetterLogoText}}} instead of the product name, so "
                + "white-labelled portals send their own branding");

            text.Should().NotContain("http://", "links must come from tags, not be hard-coded in the pattern")
                .And.NotContain("https://", "links must come from tags, not be hard-coded in the pattern");
        }
    }

    /// <summary>
    /// Every tag the pattern references must have been given a value: a forgotten one leaves the raw
    /// <c>$Tag</c> / <c>${Tag}</c> in the letter, which is exactly what the reader would see.
    /// </summary>
    /// <param name="whoOwesTheValue">
    /// Where a missing value should have come from, so the failure names the place to fix.
    /// </param>
    public static void NoUnresolvedTags(IPattern pattern, RenderedLetter letter, string whoOwesTheValue)
    {
        var tags = new NVelocityPatternFormatter().GetTags(pattern);

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
    public static void LinksRendered(RenderedLetter letter)
    {
        var raw = _unrenderedLink.Matches(letter.Body)
            .Select(match => match.Value)
            .ToArray();

        raw.Should().BeEmpty("a textile link was not recognised and its address is printed to the reader; "
            + "the translation most likely glues a particle or a suffix to the closing quotation mark — "
            + "put a space there");
    }
}
