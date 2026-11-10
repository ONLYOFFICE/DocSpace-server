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


namespace ASC.Files.Tests.Tests._01_Files.CustomFilter;

/// <summary>
/// The scope of <c>PUT /files/file/:fileId/customfilter</c>: only spreadsheets take the mode, and once a member
/// switched it on, another Editing member cannot switch it off, while a Room manager can.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class CustomFilterScopeTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private async Task Share(int roomId, User user, FileShare access)
    {
        await _roomsApi.SetRoomSecurityAsync(roomId, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = access }]
        }, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("Autotest CustomFilter.docx")]
    [InlineData("Autotest CustomFilter.pptx")]
    public async Task SetCustomFilter_NotASpreadsheet_Returns400(string title)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy(title, Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetCustomFilterTagAsync(file.Id, new CustomFilterRequest(true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetCustomFilter_EditingMemberDisablesTheOwnersFilter_Returns403WhileARoomManagerMay()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest CustomFilter Scope Room");
        var ownersFile = await CreateFile("Autotest CustomFilter Owners.xlsx", room.Id);
        var membersFile = await CreateFile("Autotest CustomFilter Members.xlsx", room.Id);
        var editor = await InviteContact(EmployeeType.User);
        var manager = await InviteContact(EmployeeType.RoomAdmin);
        await Share(room.Id, editor, FileShare.Editing);
        await Share(room.Id, manager, FileShare.RoomManager);
        await _filesApi.SetCustomFilterTagAsync(ownersFile.Id, new CustomFilterRequest(true), TestContext.Current.CancellationToken);

        // the Editing member may use the mode at all: it switches it on and off on a file nobody else filtered
        await _filesClient.Authenticate(editor);
        (await _filesApi.SetCustomFilterTagAsync(membersFile.Id, new CustomFilterRequest(true), TestContext.Current.CancellationToken))
            .Response.CustomFilterEnabled.Should().BeTrue();
        (await _filesApi.SetCustomFilterTagAsync(membersFile.Id, new CustomFilterRequest(false), TestContext.Current.CancellationToken))
            .Response.CustomFilterEnabled.Should().NotBe(true);

        // Act
        var refused = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetCustomFilterTagAsync(ownersFile.Id, new CustomFilterRequest(false), TestContext.Current.CancellationToken));

        await _filesClient.Authenticate(manager);
        var managed = (await _filesApi.SetCustomFilterTagAsync(ownersFile.Id, new CustomFilterRequest(false), TestContext.Current.CancellationToken)).Response;

        // Assert
        refused.ErrorCode.Should().Be(403);
        managed.CustomFilterEnabled.Should().NotBe(true);
    }
}
