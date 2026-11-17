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

namespace ASC.Files.Tests.Tests._03_Rooms.Links;

/// <summary>
/// A portal member who was never invited resolves the primary external link of a Public room with
/// <c>getExternalShareData</c>; the room then answers <c>external:true</c> for that member and joins the member's
/// Rooms section.
/// </summary>
[Trait("Category", "Rooms")]
public class PublicRoomLinkJoinTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task GetExternalShareData_PublicRoomLink_PutsTheRoomIntoTheMembersRoomsSection(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Public Join " + Guid.NewGuid().ToString()[..8]);
        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        var member = await InviteContact(employeeType);
        await _filesClient.Authenticate(member);

        var before = (await _roomsApi.GetRoomsFolderAsync(searchArea: SearchArea.Active, cancellationToken: TestContext.Current.CancellationToken)).Response;
        (before.Folders ?? []).OfType<FolderDto>().Should().NotContain(f => f.Id == room.Id, "the premise is that the member was never invited");

        // Act
        await _sharingApi.GetExternalShareDataAsync(
            link.SharedLink.RequestToken,
            folderId: room.Id.ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.External.Should().BeTrue();

        var after = (await _roomsApi.GetRoomsFolderAsync(searchArea: SearchArea.Active, cancellationToken: TestContext.Current.CancellationToken)).Response;
        (after.Folders ?? []).OfType<FolderDto>().Should().Contain(f => f.Id == room.Id, "a Public room opened through its link is listed in the Rooms section");
    }
}
