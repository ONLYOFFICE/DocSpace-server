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
/// GET /files/rooms/fromTemplate/status — the response shape of the "create room from template"
/// operation, its lifecycle across repeated polling, and its per-user scoping. Room creation itself
/// is covered in <see cref="RoomFromTemplateTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomFromTemplateStatusTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task GetRoomCreatingStatus_AfterCreatingFromTemplate_ReturnsStatus()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Template Source", isPublic: false);
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Room From Template"), TestContext.Current.CancellationToken);

        // Act
        var status = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        status.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRoomCreatingStatus_ResponseHasStatusCodeAndTypedFields()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Status Shape", isPublic: false);
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Status Shape Room"), TestContext.Current.CancellationToken);
        await WaitForRoomFromTemplate();

        // Act
        var status = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.RoomId.Should().BePositive();
        status.Error.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GetRoomCreatingStatus_RepeatedPolling_ReturnsSameRoomId()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Repeat Status", isPublic: false);
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Repeat Status Room"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Act & Assert
        for (var i = 0; i < 3; i++)
        {
            var status = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;
            status.IsCompleted.Should().BeTrue();
            status.RoomId.Should().Be(roomId);
        }
    }

    [Fact]
    public async Task GetRoomCreatingStatus_AvailableImmediatelyAfterStart()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Immediate", isPublic: false);

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Immediate Room"), TestContext.Current.CancellationToken);
        var status = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert - isCompleted may be true or false right after start, both are valid.
        status.Should().NotBeNull();

        await WaitForRoomFromTemplate();
    }

    [Fact]
    public async Task GetRoomCreatingStatus_IsCompletedTrue_ForTemplateWithContent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = await CreateCustomRoom("Autotest Lifecycle Source");
        for (var i = 0; i < 3; i++)
        {
            await _foldersApi.CreateFolderAsync(sourceRoom.Id, new CreateFolder($"Lifecycle Folder {i}"), TestContext.Current.CancellationToken);
        }

        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(sourceRoom.Id, "Autotest Lifecycle Template"), TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Lifecycle Room"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert
        roomId.Should().BePositive();
        var status = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;
        status.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetRoomCreatingStatus_SecondOperation_OverridesPreviousRoomId()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Override", isPublic: false);

        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Override Room First"), TestContext.Current.CancellationToken);
        var firstRoomId = await WaitForRoomFromTemplate();

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Override Room Second"), TestContext.Current.CancellationToken);
        var secondRoomId = await WaitForRoomFromTemplate();

        // Assert
        secondRoomId.Should().NotBe(firstRoomId);
        var status = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;
        status.IsCompleted.Should().BeTrue();
        status.RoomId.Should().Be(secondRoomId);
    }

    [Fact]
    public async Task GetRoomCreatingStatus_FailedCreate_DoesNotProduceFakeCompletedOperation()
    {
        // Arrange - templateId 999999999 does not resolve to anything. Unlike what the TS suite
        // assumed (a 200 that queues a background operation which later fails), the current API
        // denies the request synchronously with an ApiException, so no operation is ever queued at
        // all. Either way no room is ever produced, which is what this test actually cares about.
        await _filesClient.Authenticate(Owner);

        // Act
        try
        {
            await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(999999999, "Phantom Room"), TestContext.Current.CancellationToken);
        }
        catch (ApiException)
        {
            // Expected: the request is rejected before any background operation starts.
        }

        // Assert
        var titles = await GetRoomTitles();
        titles.Should().NotContain("Phantom Room");

        var status = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;
        (status?.RoomId ?? 0).Should().BeLessThanOrEqualTo(0);
    }

    /// <remarks>
    /// Bug 81763: the status is scoped to a background operation slot that is not properly keyed to
    /// the caller, so a member with no room-creation operation of their own can still observe the
    /// portal owner's most recently completed roomId.
    /// </remarks>
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    [Trait("Bug", "81763")]
    public async Task GetRoomCreatingStatus_FreshMember_DoesNotSeeCreatorsRoomId(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate($"Autotest Status Iso {employeeType}", isPublic: false);
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, $"Owner Room For {employeeType}"), TestContext.Current.CancellationToken);
        var ownerRoomId = await WaitForRoomFromTemplate();

        var member = await InviteMember(employeeType);

        // Act
        await _filesClient.Authenticate(member);
        var status = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        (status?.RoomId ?? 0).Should().NotBe(ownerRoomId);
    }

    [Fact]
    [Trait("Bug", "81763")]
    public async Task GetRoomCreatingStatus_Owner_DoesNotSeeAdminsRoomId()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Iso Owner From Admin", isPublic: true);

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Admin Only Room"), TestContext.Current.CancellationToken);
        var adminRoomId = await WaitForRoomFromTemplate();

        // Act
        await _filesClient.Authenticate(Owner);
        var status = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        (status?.RoomId ?? 0).Should().NotBe(adminRoomId);
    }

    [Fact]
    public async Task GetRoomCreatingStatus_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetRoomCreatingStatus_FreshUser_NoPriorOperation_ReturnsEmptyStatus()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        // Act
        await _filesClient.Authenticate(admin);
        var status = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        (status?.RoomId ?? 0).Should().BeLessThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetRoomCreatingStatus_StableAcrossRepeatedCalls_NoNewOperation()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Stable", isPublic: false);
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Stable Room"), TestContext.Current.CancellationToken);
        await WaitForRoomFromTemplate();

        // Act
        var first = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;
        var second = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;
        var third = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        first.RoomId.Should().Be(second.RoomId);
        second.RoomId.Should().Be(third.RoomId);
    }

    [Fact]
    public async Task GetRoomCreatingStatus_SurvivesDeletionOfCreatedRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Survive Delete", isPublic: false);
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Room To Be Deleted After Status"), TestContext.Current.CancellationToken);
        var createdRoomId = await WaitForRoomFromTemplate();

        await _roomsApi.DeleteRoomAsync(createdRoomId, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var status = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        status.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRoomCreatingStatus_NonGetHttpMethods_Rejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string path = "api/2.0/files/rooms/fromTemplate/status";

        // Act & Assert
        using (var post = new HttpRequestMessage(HttpMethod.Post, path))
        using (var postResponse = await _filesClient.SendAsync(post, TestContext.Current.CancellationToken))
        {
            ((int)postResponse.StatusCode).Should().Be(405);
        }

        using (var put = new HttpRequestMessage(HttpMethod.Put, path))
        using (var putResponse = await _filesClient.SendAsync(put, TestContext.Current.CancellationToken))
        {
            ((int)putResponse.StatusCode).Should().Be(405);
        }

        using (var delete = new HttpRequestMessage(HttpMethod.Delete, path))
        using (var deleteResponse = await _filesClient.SendAsync(delete, TestContext.Current.CancellationToken))
        {
            ((int)deleteResponse.StatusCode).Should().Be(405);
        }
    }

    [Fact]
    public async Task GetRoomCreatingStatus_OwnersResponse_DoesNotLeakAnothersRoomId()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Sensitive", isPublic: true);

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Admin Sensitive Room"), TestContext.Current.CancellationToken);
        var adminRoomId = await WaitForRoomFromTemplate();

        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Owner Sensitive Room"), TestContext.Current.CancellationToken);
        var ownerRoomId = await WaitForRoomFromTemplate();

        // Act
        var status = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        status.RoomId.Should().Be(ownerRoomId);
        status.RoomId.Should().NotBe(adminRoomId);
        status.ToJson().Should().NotContain(adminRoomId.ToString());
    }

    [Fact]
    public async Task GetRoomCreatingStatus_InFlightCreateRoomTemplate_DoesNotAppear()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = await CreateCustomRoom("Autotest Cross Template Source");
        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(sourceRoom.Id, "Autotest Cross Template Title"), TestContext.Current.CancellationToken);

        // Act - call the room-creating status BEFORE waiting for the template operation to finish.
        var status = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert - the template-creation operation must not be exposed via the room-creating status
        // endpoint as if its templateId were a roomId.
        var templateId = await WaitForRoomTemplate();
        (status?.RoomId ?? -1).Should().NotBe(templateId);
    }

    [Fact]
    public async Task GetRoomTemplateCreatingStatus_InFlightCreateRoomFromTemplate_DoesNotAppear()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Cross From", isPublic: false);

        // Act - start creating a room from the template but do not wait for it to finish.
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Cross Room From"), TestContext.Current.CancellationToken);
        var templateStatus = (await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert - the template-status endpoint should keep referring to the previously-created
        // template, not flip to the in-flight room.
        if ((templateStatus?.TemplateId ?? 0) > 0)
        {
            templateStatus!.TemplateId.Should().Be(templateId);
        }

        var roomId = await WaitForRoomFromTemplate();
        roomId.Should().NotBe(templateId);
    }
}
