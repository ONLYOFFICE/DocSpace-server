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

namespace ASC.Files.Tests.Tests._03_Rooms.Permissions;

[Trait("Category", "Rooms")]
public class RoomFromTemplatePermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    #region POST /files/rooms/fromtemplate - access control

    [Fact]
    public async Task CreateRoomFromTemplate_DocSpaceAdminPublicTemplate_RoomCreated()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest FromTmpl Admin Template", isPublic: true);

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(
            new CreateRoomFromTemplateDto(templateId, "Admin Room"),
            TestContext.Current.CancellationToken);

        // Assert
        var createdId = await WaitForRoomFromTemplate();
        createdId.Should().BePositive();
    }

    /// <remarks>
    /// A User or a Guest used to be able to create a room from a template even though neither has
    /// the create-room permission — bug 81662. Fixed by checking access synchronously in the controller, before the background operation is queued.
    /// </remarks>
    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    [Trait("Bug", "81662")]
    public async Task CreateRoomFromTemplate_UserOrGuest_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate($"Autotest FromTmpl {employeeType} Template", isPublic: true);

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        var roomTitle = $"{employeeType} Room";

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomFromTemplateAsync(
                new CreateRoomFromTemplateDto(templateId, roomTitle),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var titles = await GetRoomTitles();
        titles.Should().NotContain(roomTitle);
    }

    /// <remarks>
    /// A DocSpaceAdmin used to be able to create a room from a non-public template they do not own
    /// — bug 81664, closed by the same fix as 81662. Fixed by checking access synchronously in the controller, before the background operation is queued.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81664")]
    public async Task CreateRoomFromTemplate_DocSpaceAdminForeignPrivateTemplate_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest FromTmpl NoAccess Template", isPublic: false);

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomFromTemplateAsync(
                new CreateRoomFromTemplateDto(templateId, "Should Fail"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var titles = await GetRoomTitles();
        titles.Should().NotContain("Should Fail");
    }

    /// <remarks>
    /// Access to the source room does not imply access to the template, yet the room used to be
    /// created anyway — bug 81662. Fixed by checking access synchronously in the controller, before the background operation is queued.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81662")]
    public async Task CreateRoomFromTemplate_UserWithSourceRoomAccessOnly_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = await CreateCustomRoom("Autotest FromTmpl SrcOnly Source");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(sourceRoom.Id, user, FileShare.Editing);

        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(sourceRoom.Id, "Autotest FromTmpl SrcOnly Template"),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomFromTemplateAsync(
                new CreateRoomFromTemplateDto(templateId, "Should Fail"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var titles = await GetRoomTitles();
        titles.Should().NotContain("Should Fail");
    }

    [Fact]
    public async Task CreateRoomFromTemplate_TerminatedUser_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest FromTmpl Disabled Template", isPublic: true);

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        await TerminateUser(admin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomFromTemplateAsync(
                new CreateRoomFromTemplateDto(templateId, "Disabled Room"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task CreateRoomFromTemplate_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest FromTmpl Anon Template", isPublic: false);

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomFromTemplateAsync(
                new CreateRoomFromTemplateDto(templateId, "Anonymous Room"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    #endregion
}
