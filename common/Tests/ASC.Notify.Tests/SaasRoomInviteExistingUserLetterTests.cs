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
/// The room invitation for someone who already has an account (<c>saas_room_invite_existing_user</c>).
/// Where <see cref="SaasRoomInviteLetterTests"/> starts a sign-up, this one names the inviter and the
/// room and links straight into it.
/// </summary>
public class SaasRoomInviteExistingUserLetterTests : LetterTestBase
{
    private const string RoomTitle = "Room title";

    /// <summary>The room page, passed into <c>Init</c> by the caller.</summary>
    private static string RoomUrl => LetterEnvironment.PortalLink("rooms/shared/1");

    protected override string LetterId => "saas_room_invite_existing_user";

    protected override IPattern Pattern => new EmailPattern(
        () => WebstudioNotifyPatternResource.subject_saas_room_invite_existing_user,
        () => WebstudioNotifyPatternResource.pattern_saas_room_invite_existing_user);

    /// <summary>The sending code sets no top image, so the tenant letter logo is rendered instead.</summary>
    protected override string? TopGif => null;

    /// <summary>Textile letter: <c>$TrulyYours</c> is inline, not a table row of its own.</summary>
    protected override bool TrulyYoursAsTableRow => false;

    /// <summary>Mirrors <c>SaasRoomInviteExistingUserNotifyAction.Init</c>.</summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return
        [
            OrangeButton("ButtonJoinRoom", culture, RoomUrl),
            new TagValue(CommonTags.Message, RoomTitle),
            new TagValue(CommonTags.InviteLink, RoomUrl)
        ];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should().Contain(Resource("ButtonJoinRoom", culture))
            .And.Contain(RoomUrl)
            .And.Contain(RoomTitle)
            .And.Contain(AuthorName)
            .And.Contain(LetterEnvironment.PortalUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        letter.Subject.Should().Be($"You're invited to the {LetterEnvironment.LogoText} DocSpace room");

        letter.Body.Should().Contain("Hello!")
            .And.Contain($"{AuthorName} invited you to join the room");
    }
}
