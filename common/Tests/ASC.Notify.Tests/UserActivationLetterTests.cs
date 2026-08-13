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

namespace ASC.Notify.Tests;

/// <summary>
/// The invitation a new user gets (<c>saas_user_activation_v1</c>). The three other editions send the
/// very same letter from their own actions, so this class renders the SaaS one and pins the rest to it
/// — see <see cref="Letters_AreIdentical"/>.
/// </summary>
public class UserActivationLetterTests : LetterTestBase
{
    /// <summary>The confirmation link, built by the sending code from <c>ConfirmType.Activation</c>.</summary>
    private static string ConfirmUrl => LetterEnvironment.PortalLink("confirm/Activation");

    private static readonly (string Subject, string Pattern)[] _otherEditions =
    [
        (WebstudioNotifyPatternResource.subject_enterprise_user_activation_v1,
         WebstudioNotifyPatternResource.pattern_enterprise_user_activation_v1),
        (WebstudioNotifyPatternResource.subject_enterprise_whitelabel_user_activation_v1,
         WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_user_activation_v1),
        (WebstudioNotifyPatternResource.subject_opensource_user_activation_v1,
         WebstudioNotifyPatternResource.pattern_opensource_user_activation_v1)
    ];

    protected override string LetterId => "saas_user_activation_v1";

    protected override IPattern Pattern => new EmailPattern(
        () => WebstudioNotifyPatternResource.subject_saas_user_activation_v1,
        () => WebstudioNotifyPatternResource.pattern_saas_user_activation_v1);

    /// <summary>The sending code sets a top image for this letter.</summary>
    protected override string? TopGif => LetterEnvironment.NotificationImageUrl("join_docspace.gif");

    /// <summary>The recipient is a plain user, so the signature carries the social footer.</summary>
    protected override string Footer => "social";

    /// <summary>Mirrors <c>SaasUserActivationV1NotifyAction.Init</c>.</summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return
        [
            OrangeButton("ButtonAccept", culture, ConfirmUrl),
            new TagValue(CommonTags.ActivateUrl, ConfirmUrl)
        ];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should().Contain(Resource("ButtonAccept", culture))
            .And.Contain(ConfirmUrl)
            .And.Contain(LetterEnvironment.PortalUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"You are invited to {logoText}");

        letter.Body.Should().Contain("Hello!")
            .And.Contain($"You are invited to join {logoText} at")
            .And.Contain("Accept the invitation by clicking the link:");
    }

    /// <summary>
    /// SaaS, Enterprise, Enterprise whitelabel and Opensource send one and the same invitation from four
    /// separate actions, so the four resource pairs must stay word for word identical — a change to one
    /// is a change to all.
    /// </summary>
    [Fact]
    public void Letters_AreIdentical()
    {
        foreach (var (subject, pattern) in _otherEditions)
        {
            subject.Should().Be(WebstudioNotifyPatternResource.subject_saas_user_activation_v1);
            pattern.Should().Be(WebstudioNotifyPatternResource.pattern_saas_user_activation_v1);
        }
    }
}
