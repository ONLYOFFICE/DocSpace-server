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

namespace ASC.Files.Tests.Tests._03_Rooms.Links;

/// <summary>
/// PUT /files/rooms/{id}/links — the additional-link limit of a Public room (its auto-created primary
/// link plus 5 additional ones) and the refusal message.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "LinkLimits")]
public class RoomLinkLimitTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SetRoomLink_PublicRoom_FiveAdditionalLinksAccepted_SixthRefusedWithLimitFive()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Public Room Link Limit");

        for (var i = 1; i <= 5; i++)
        {
            await _roomsApi.SetRoomLinkAsync(room.Id, ExternalLink($"Link {i}"), TestContext.Current.CancellationToken);
        }

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(room.Id, ExternalLink("Link 6"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain("The maximum number of links may not exceed 5");

        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;
        links.Count(l => l.SharedLink is { Primary: false }).Should().Be(5);
        links.Count(l => l.SharedLink is { Primary: true }).Should().Be(1);
    }

    private static RoomLinkRequest ExternalLink(string title)
    {
        return new RoomLinkRequest(access: FileShare.Read, title: title, linkType: LinkType.External);
    }
}
