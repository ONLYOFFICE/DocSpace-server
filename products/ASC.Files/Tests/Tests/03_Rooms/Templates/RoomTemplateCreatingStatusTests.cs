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

/// <summary>
/// GET /files/roomtemplate/status — polling the caller's own room-template creation operation.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTemplateCreatingStatusTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task GetRoomTemplateCreatingStatus_WhileInProgress_ReturnsOk()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest InProgress Status Source");
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest InProgress Status Template"),
            TestContext.Current.CancellationToken);

        // Act + Assert: the call itself must not throw while the operation is still running.
        await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken);

        await WaitForRoomTemplate();
    }

    [Fact]
    public async Task GetRoomTemplateCreatingStatus_AfterCompletion_HasExpectedShape()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Shape Source");
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Shape Template"),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Act
        var status = (await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.TemplateId.Should().Be(templateId);
        status.Progress.Should().Be(100);
    }

    [Fact]
    public async Task GetRoomTemplateCreatingStatus_CompletedTemplateId_CanCreateRoomFromTemplate()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Usable Source");
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Usable Template"),
            TestContext.Current.CancellationToken);
        await WaitForRoomTemplate();

        var status = (await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(
            new CreateRoomFromTemplateDto(status.TemplateId, "Room From Usable Template"),
            TestContext.Current.CancellationToken);

        // Assert
        var createdId = await WaitForRoomFromTemplate();
        createdId.Should().BePositive();
    }

    [Fact]
    public async Task GetRoomTemplateCreatingStatus_ConsecutivePollsDuringActiveOperation_AllReturnOk()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Active Polls Source");
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Active Polls Template"),
            TestContext.Current.CancellationToken);

        // Act + Assert: neither poll may throw while the operation is still running.
        await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken);
        await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken);

        await WaitForRoomTemplate();
    }

    [Fact]
    public async Task GetRoomTemplateCreatingStatus_FreshUserWithNoPriorOperation_ReturnsNullResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        // Act
        await _filesClient.Authenticate(admin);
        var status = (await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        status.Should().BeNull();
    }

    /// <remarks>
    /// Bug 81692: the template-creation status is a shared, per-request lookup that used to leak
    /// whichever user's template happened to be the last one requested, instead of only ever
    /// reporting the caller's own.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81692")]
    public async Task GetRoomTemplateCreatingStatus_OwnerAndAdmin_SeeOnlyOwnTemplateId()
    {
        // Arrange: the test harness authenticates one identity at a time on a shared HttpClient, so
        // the two creations run sequentially rather than truly in parallel as in the TypeScript
        // suite - the isolation property under test does not depend on the timing.
        await _filesClient.Authenticate(Owner);
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        var ownerRoom = await CreateCustomRoom("Autotest Parallel Owner Source");
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(ownerRoom.Id, "Autotest Parallel Owner Template"),
            TestContext.Current.CancellationToken);
        var ownerTemplateId = await WaitForRoomTemplate();

        await _filesClient.Authenticate(admin);
        var adminRoom = await CreateCustomRoom("Autotest Parallel Admin Source");
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(adminRoom.Id, "Autotest Parallel Admin Template"),
            TestContext.Current.CancellationToken);
        var adminTemplateId = await WaitForRoomTemplate();

        ownerTemplateId.Should().NotBe(adminTemplateId);

        // Act
        var adminStatus = (await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;
        await _filesClient.Authenticate(Owner);
        var ownerStatus = (await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        ownerStatus.TemplateId.Should().Be(ownerTemplateId);
        adminStatus.TemplateId.Should().Be(adminTemplateId);
    }

    // The TypeScript "status after failed template creation" scenario (roomId 999999999 completing
    // asynchronously with isCompleted:true and a non-empty error) is not reproducible here: on this
    // portal CreateRoomTemplateAsync rejects a non-existent roomId synchronously (see
    // RoomTemplateCreateTests.CreateRoomTemplate_NonExistentRoomId_ShouldReturnNotFound) — no
    // operation is ever queued, so there is nothing for this endpoint to report. That synchronous
    // rejection is exactly what bug 81691 asks for, so the coverage moved there rather than being
    // ported as a "failed async operation" case.

    [Fact]
    public async Task GetRoomTemplateCreatingStatus_DocSpaceAdmin_ReturnsOwnTemplateId()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        var room = await CreateCustomRoom("Autotest Admin Own Source");
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Admin Own Template"),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Act
        var status = (await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.TemplateId.Should().Be(templateId);
    }

    /// <remarks>
    /// Bug 81692: same isolation defect as the owner/admin case above, checked from the other
    /// roles' point of view.
    /// </remarks>
    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    [Trait("Bug", "81692")]
    public async Task GetRoomTemplateCreatingStatus_OtherRoles_DoNotSeeOwnersTemplateId(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest {employeeType} Iso Source");
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, $"Autotest {employeeType} Iso Template"),
            TestContext.Current.CancellationToken);
        var ownerTemplateId = await WaitForRoomTemplate();

        var member = await InviteMember(employeeType);

        // Act
        await _filesClient.Authenticate(member);
        var status = (await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        (status?.TemplateId ?? 0).Should().NotBe(ownerTemplateId);
    }

    [Fact]
    public async Task GetRoomTemplateCreatingStatus_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetRoomTemplateCreatingStatus_TerminatedUser_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var user = await InviteMember(EmployeeType.User);

        // Authenticate as the user first, then terminate — so the still-valid session is what
        // gets invalidated, and the rejection is a 401 (revoked session) rather than a 403 a
        // fresh login as an already-disabled account would produce.
        await _filesClient.Authenticate(user);
        await TerminateUser(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
