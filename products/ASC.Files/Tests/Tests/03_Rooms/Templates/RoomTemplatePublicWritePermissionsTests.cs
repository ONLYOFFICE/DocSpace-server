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

namespace ASC.Files.Tests.Tests._03_Rooms.Templates;

[Trait("Category", "Rooms")]
public class RoomTemplatePublicWritePermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task SetTemplatePublicSettings_OwnTemplate_Updated(EmployeeType? employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        if (employeeType.HasValue)
        {
            var member = await InviteMember(employeeType.Value);
            await _filesClient.Authenticate(member);
        }

        var templateId = await CreateTemplate($"Autotest SetPublic {employeeType?.ToString() ?? "Owner"} Own", isPublic: false);

        // Act
        await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId, true), TestContext.Current.CancellationToken);

        // Assert
        var actual = (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response;
        actual.Should().BeTrue();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SetTemplatePublicSettings_OwnersTemplate_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate($"Autotest SetPublic {employeeType} OnOwner", isPublic: false);

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId, true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var actual = (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response;
        actual.Should().BeFalse();
    }

    [Fact]
    public async Task SetTemplatePublicSettings_OwnerOnAdminTemplate_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(admin);
        var templateId = await CreateTemplate("Autotest SetPublic OwnerOnAdmin", isPublic: false);

        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId, true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(admin);
        var actual = (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response;
        actual.Should().BeFalse();
    }

    [Fact]
    public async Task SetTemplatePublicSettings_InvitedSourceRoomMember_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = await CreateCustomRoom("Autotest SetPublic Invited Source");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(sourceRoom.Id, user, FileShare.Editing);

        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(sourceRoom.Id, "Autotest SetPublic Invited Template"),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId, true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var actual = (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response;
        actual.Should().BeFalse();
    }

    [Fact]
    public async Task SetTemplatePublicSettings_TerminatedUser_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(admin);
        var templateId = await CreateTemplate("Autotest SetPublic Terminated", isPublic: false);

        await TerminateUser(admin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId, true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task SetTemplatePublicSettings_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest SetPublic Anon", isPublic: false);

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId, true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
