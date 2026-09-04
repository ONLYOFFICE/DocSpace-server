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

namespace ASC.Core.Common.Tests;

/// <summary>
/// What the non-HTML stylers do with the markup a letter is built out of. A Files letter hands the
/// same pattern to mail and to Telegram, so the body reaching <see cref="MarkDownStyler"/> carries
/// the tag values the email side produced — and <c>TagValues.OrangeButton</c> is a table whose
/// Outlook fallback sits in a conditional comment and repeats the caption as plain text. Stripping
/// the tags and keeping their text showed the reader that copy as well: "Check ready forms Check
/// ready forms", once out of the VML fallback and once as the real link.
/// </summary>
public class NotifyStylerTests
{
    private const string ButtonCaption = "Check ready forms";
    private const string ButtonUrl = "https://example.com/rooms/12";

    /// <summary>
    /// The shape <c>TagValues.OrangeButton</c> produces, trimmed to what the stylers react to: the
    /// mso fallback that carries the caption inside a conditional comment, and the anchor every
    /// other client is shown.
    /// </summary>
    private const string OrangeButton =
        """<table cellspacing="0" cellpadding="0" style="border: 0 none;"><tbody>"""
        + """<tr border="0" cellspacing="0" cellpadding="0"><td style="width: 180px;"></td>"""
        + "<!--[if mso]>"
        + """<td class="body-text" border="0" style="text-align: center; width: 230px;">"""
        + $"""<v:roundrect xmlns:v="urn:schemas-microsoft-com:vml" href="{ButtonUrl}" arcsize="5%" alt="{ButtonCaption}" target="_blank">"""
        + "<w:anchorlock/>"
        + $"""<center class="fol" style="color:#ffffff;">{ButtonCaption}</center>"""
        + "</v:roundrect></td>"
        + "<![endif]-->"
        + """<td style="text-align: center; white-space: nowrap;">"""
        + $"""<a class="fol" href="{ButtonUrl}" style="background-color:#FF6F3D;" alt="{ButtonCaption}" target="_blank">{ButtonCaption}</a>"""
        + """</td><td style="width: 180px;"></td></tr></tbody></table>""";

    private const string FormReceivedPattern = """
                                   h1. Form is filled out

                                   A new form "Report":"https://example.com/doc/5" is filled out in the room "Forms":"https://example.com/rooms/12"

                                   $OrangeButton
                                   """;

    [Fact]
    public async Task MarkDownStyler_OrangeButton_ShouldShowTheCaptionOnceAsALink()
    {
        var body = await RenderAsync(new MarkDownStyler(), FormReceivedPattern);

        Occurrences(body, ButtonCaption).Should().Be(1,
            "the caption inside the Outlook-only fallback is a second copy of the button, and the "
            + $"reader was shown it as plain text next to the link: {body}");

        body.Should().Contain($"[{ButtonCaption}]({ButtonUrl})", "the button must stay clickable");
    }

    [Fact]
    public async Task JabberStyler_OrangeButton_ShouldShowTheCaptionOnce()
    {
        var body = await RenderAsync(new JabberStyler(), FormReceivedPattern);

        Occurrences(body, ButtonCaption).Should().Be(1,
            $"the Outlook-only fallback must not reach the reader either: {body}");
    }

    /// <summary>
    /// Why <c>${LetterLogoText}</c> has to be resolved before the stylers run — which is what
    /// <c>NotifyTransferRequest.BeforeTransferRequestAsync</c> does to the tag values on its way out.
    /// The formatter substitutes a pattern once and never the values it inserted, so a reference
    /// written inside a tag value (the signature is "Truly Yours, ${LetterLogoText} Team") is still
    /// standing when the styler starts; <see cref="MarkDownStyler"/> then escapes the braces for
    /// Telegram's markdown, and no later pass can recognise it any more. That is how the wallet
    /// letter came to be signed "Truly Yours, ${LetterLogoText} Team".
    /// </summary>
    [Fact]
    public async Task MarkDownStyler_TagReferenceInsideATagValue_IsEscapedBeyondRecognition()
    {
        var body = await RenderAsync(new MarkDownStyler(), "$TrulyYours",
            new TagValue("TrulyYours", "Truly Yours, ${LetterLogoText} Team"));

        body.Should().NotContain("${LetterLogoText}",
            "a pass after the styler cannot find the reference any more, so nothing may rely on one");
    }

    private static async Task<string> RenderAsync(IPatternStyler styler, string pattern, params ITagValue[] tags)
    {
        var recipient = new DirectRecipient(Guid.NewGuid().ToString(), "Styler test", ["styler@onlyoffice.com"]);
        var message = new NoticeMessage(recipient, null, null, new TelegramPattern(() => pattern));

        message.AddArgument(tags.Length > 0 ? tags : [new TagValue("OrangeButton", OrangeButton)]);

        new NVelocityPatternFormatter().FormatMessage(message, message.Arguments);

        await styler.ApplyFormatingAsync(message);

        return message.Body;
    }

    private static int Occurrences(string text, string value)
    {
        return (text.Length - text.Replace(value, string.Empty).Length) / value.Length;
    }
}
