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
public class RoomTemplateCreatePermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task CreateRoomTemplate_OwnRoom_TemplateCreated(EmployeeType? employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        if (employeeType.HasValue)
        {
            var member = await InviteMember(employeeType.Value);
            await _filesClient.Authenticate(member);
        }

        var role = employeeType?.ToString() ?? "Owner";
        var room = await CreateCustomRoom($"Autotest CreateTmpl {role} Source");

        // Act
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, $"Autotest CreateTmpl {role} Template"),
            TestContext.Current.CancellationToken);

        // Assert
        var templateId = await WaitForRoomTemplate();
        templateId.Should().BePositive();
    }

    /// <remarks>
    /// Creating a template from a room the caller had no access to used to succeed instead of
    /// returning 403 — bug 81693. Fixed by checking access synchronously in the controller, before the background operation is queued.
    /// </remarks>
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    [Trait("Bug", "81693")]
    public async Task CreateRoomTemplate_NoAccessToSourceRoom_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest CreateTmpl {employeeType} NoAccess Source");

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        var templateTitle = $"{employeeType} No-Access Template";

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTemplateAsync(
                new RoomTemplateDto(room.Id, templateTitle),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var templates = await GetTemplateTitles();
        templates.Should().NotContain(templateTitle);
    }

    /// <remarks>
    /// A User invited to the source room used to be able to create a template regardless of the
    /// access level, none of which is write-level — bug 81693. Fixed by checking access synchronously in the controller, before the background operation is queued.
    /// </remarks>
    [Theory]
    [MemberData(nameof(RoomAccessData.NonManagerAccesses), MemberType = typeof(RoomAccessData))]
    [Trait("Bug", "81693")]
    public async Task CreateRoomTemplate_UserInvitedToSourceRoom_Forbidden(FileShare access)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest CreateTmpl User-{access} Source");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, access);

        await _filesClient.Authenticate(user);

        var templateTitle = $"User {access} Template";

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTemplateAsync(
                new RoomTemplateDto(room.Id, templateTitle),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var templates = await GetTemplateTitles();
        templates.Should().NotContain(templateTitle);
    }

    [Fact]
    public async Task CreateRoomTemplate_RoomAdminWithRoomManagerAccess_TemplateCreated()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest CreateTmpl RoomManager Source");

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);

        await _filesClient.Authenticate(roomAdmin);

        // Act
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "RoomManager Template"),
            TestContext.Current.CancellationToken);

        // Assert
        var templateId = await WaitForRoomTemplate();
        templateId.Should().BePositive();
    }

    [Fact]
    public async Task CreateRoomTemplate_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest CreateTmpl Anon Source");

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTemplateAsync(
                new RoomTemplateDto(room.Id, "Anonymous Template"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task CreateRoomTemplate_TerminatedUser_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(admin);
        var room = await CreateCustomRoom("Autotest CreateTmpl Disabled Source");

        await TerminateUser(admin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTemplateAsync(
                new RoomTemplateDto(room.Id, "Disabled Template"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
