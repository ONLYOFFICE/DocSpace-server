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
/// Core lifecycle and validation of POST /files/roomtemplate: the operation shape, roomId
/// validation and source-room-state checks. Title validation lives in
/// <see cref="RoomTemplateCreateValidationTests"/>, permission checks in
/// <see cref="RoomTemplateCreatePermissionsTests"/>, DTO-content fields and structural/edge-case
/// behaviour in <see cref="RoomTemplateContentTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTemplateCreateTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task CreateRoomTemplate_Owner_CompletesWithPositiveTemplateId()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room");

        // Act
        var response = (await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Template"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Error.Should().BeNullOrEmpty();
        var templateId = await WaitForRoomTemplate();
        templateId.Should().BePositive();

        var status = (await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;
        status.TemplateId.Should().BePositive();
        status.Error.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task CreateRoomTemplate_ResponseShape_HasPositiveTemplateIdAndNoErrorImmediately()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Op Shape Source");

        // Act
        var response = (await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Op Shape Template"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().NotBeNull();
        response.Progress.Should().BeInRange(0, 100);
        response.Error.Should().BeNullOrEmpty();

        var templateId = await WaitForRoomTemplate();
        templateId.Should().BePositive();
    }

    [Fact]
    public async Task CreateRoomTemplate_TwoConsecutiveTemplates_HaveDistinctIds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomA = await CreateCustomRoom("Autotest Seq Source A");
        var roomB = await CreateCustomRoom("Autotest Seq Source B");

        // Act
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(roomA.Id, "Autotest Seq Template A"),
            TestContext.Current.CancellationToken);
        var templateAId = await WaitForRoomTemplate();

        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(roomB.Id, "Autotest Seq Template B"),
            TestContext.Current.CancellationToken);
        var templateBId = await WaitForRoomTemplate();

        // Assert
        templateAId.Should().BePositive();
        templateBId.Should().BePositive();
        templateBId.Should().NotBe(templateAId);
    }

    [Fact]
    public async Task CreateRoomTemplate_RepeatedPollingAfterCompletion_ReturnsStableStatus()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Stable Poll Source");
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Stable Poll Template"),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Act
        var first = (await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;
        var second = (await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;
        var third = (await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        first.IsCompleted.Should().BeTrue();
        second.IsCompleted.Should().BeTrue();
        third.IsCompleted.Should().BeTrue();
        first.TemplateId.Should().Be(templateId);
        second.TemplateId.Should().Be(templateId);
        third.TemplateId.Should().Be(templateId);
    }

    /// <remarks>
    /// Bug 81692: another user's own template-creation status must not surface a template id it did
    /// not create. The status endpoint is shared per-request, and used to leak whatever template the
    /// last requester created.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81692")]
    public async Task CreateRoomTemplate_StatusDoesNotLeakAnotherUsersTemplateCreation()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        var ownerRoom = await CreateCustomRoom("Autotest Isolation Owner Source");
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(ownerRoom.Id, "Autotest Isolation Owner Template"),
            TestContext.Current.CancellationToken);
        var ownerTemplateId = await WaitForRoomTemplate();

        // Act
        await _filesClient.Authenticate(admin);
        var adminStatus = (await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

        // Assert: the admin never started a template creation, so their status must not reference
        // the owner's templateId.
        (adminStatus?.TemplateId ?? 0).Should().NotBe(ownerTemplateId);
    }

    /// <remarks>
    /// Bug 81691: creating a template from a non-existent room id used to hang a background
    /// operation instead of failing synchronously. roomId 0 falls into the same case because the
    /// typed DTO's default value for a missing/null roomId is also 0.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81691")]
    public async Task CreateRoomTemplate_RoomIdZero_ShouldReturnNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTemplateAsync(
                new RoomTemplateDto(0, "Zero RoomId"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    [Trait("Bug", "81691")]
    public async Task CreateRoomTemplate_NonExistentRoomId_ShouldReturnNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTemplateAsync(
                new RoomTemplateDto(999999999, "Missing RoomId"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("[1,2]")]
    public async Task CreateRoomTemplate_InvalidRoomIdType_BadRequest(string roomIdJson)
    {
        // RoomTemplateDto.RoomId is a plain int, so a string or an array can only be sent raw — the
        // typed client would never put this request on the wire.
        await _filesClient.Authenticate(Owner);

        var response = await SendRawRoomTemplateCreate($$"""{"roomId":{{roomIdJson}},"title":"Bad RoomId Type"}""");

        ((int)response.StatusCode).Should().Be(400);
    }

    /// <remarks>
    /// Bug 81691: a template cannot be created from a source room that no longer exists as an
    /// active room. Deleting the source must be rejected up front, not discovered later by a
    /// background operation that never completes.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81691")]
    public async Task CreateRoomTemplate_DeletedSourceRoom_ShouldReturnNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Deleted Src");
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTemplateAsync(
                new RoomTemplateDto(room.Id, "Should Not Be Created From Deleted"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);

        var titles = await GetTemplateTitles();
        titles.Should().NotContain("Should Not Be Created From Deleted");
    }

    [Fact]
    [Trait("Bug", "81691")]
    public async Task CreateRoomTemplate_ArchivedSourceRoom_ShouldReturnForbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Archived Src");
        await ArchiveRoom(room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTemplateAsync(
                new RoomTemplateDto(room.Id, "Should Not Be Created From Archived"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        var titles = await GetTemplateTitles();
        titles.Should().NotContain("Should Not Be Created From Archived");
    }

    /// <summary>
    /// Sends a raw POST /api/2.0/files/roomtemplate, for a body the generated DTO cannot express.
    /// </summary>
    private async Task<HttpResponseMessage> SendRawRoomTemplateCreate(string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/2.0/files/roomtemplate")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
