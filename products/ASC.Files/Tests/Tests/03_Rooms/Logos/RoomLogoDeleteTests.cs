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

namespace ASC.Files.Tests.Tests._03_Rooms.Logos;

/// <summary>
/// <c>DELETE /files/rooms/{id}/logo</c> — removing a room's logo. Access control lives in
/// <see cref="RoomLogoDeletePermissionsTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLogoDeleteTests(
    AspireAppFixture fixture)
    : RoomLogoTestBase(fixture)
{
    [Fact]
    public async Task DeleteLogo_ExistingLogo_Removed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithLogo("Autotest Logo Delete Room");

        // Act
        var updated = (await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Id.Should().Be(room.Id);
        updated.Logo.Original.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteLogo_ResponseHasCorrectStructure()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithLogo("Autotest Logo Delete Structure Room");

        // Act
        var updated = (await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().NotBeNullOrEmpty();
        updated.Logo.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteLogo_RoomHasNoLogo_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo No Logo Delete Room");

        // Act & Assert (does not throw)
        await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken);
    }

    /// <summary>Previously returned 500 for a non-existent room; now correctly returns 404.</summary>
    [Fact]
    [Trait("Bug", "80983")]
    public async Task DeleteLogo_NonExistentRoom_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomLogoAsync(999999999, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteLogo_ArchivedRoom_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithLogo("Autotest Logo Del Archived Room");
        await ArchiveRoom(room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteLogo_RoomTemplate_Removed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithLogo("Autotest Logo Del Template Room");

        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Logo Del Template", copyLogo: true),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Act
        var updated = (await _roomsApi.DeleteRoomLogoAsync(templateId, TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Logo.Original.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteLogo_GetRoomInfoAfterDeletion_ShowsEmptyLogo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithLogo("Autotest Logo Del Verify Room");

        // Act
        await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Logo.Original.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteLogo_RepeatedDelete_ReturnsOkEachTime()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithLogo("Autotest Logo Repeated Delete Room");

        // Act
        await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken);
        var second = (await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        second.Logo.Original.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteLogo_AfterLogoWasReplaced_RemovesLatestLogo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Replaced Then Deleted Room");

        var firstTmpFile = await UploadLogo(CreateTestImageBytes());
        await CreateLogo(room.Id, firstTmpFile);

        var secondTmpFile = await UploadLogo(CreateTestImageBytes());
        await CreateLogo(room.Id, secondTmpFile);

        // Act
        var updated = (await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Logo.Original.Should().BeNullOrEmpty();
        updated.Logo.Large.Should().BeNullOrEmpty();
        updated.Logo.Medium.Should().BeNullOrEmpty();
        updated.Logo.Small.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteLogo_OtherRoomFieldsNotChanged()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string title = "Autotest Logo Del Preserves Fields";
        var room = await CreateCustomRoom(title);

        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["AutotestLogoDelTag"]), TestContext.Current.CancellationToken);

        var tmpFile = await UploadLogo(CreateTestImageBytes());
        await CreateLogo(room.Id, tmpFile);

        // Act
        await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be(title);
        info.RoomType.Should().Be(RoomType.CustomRoom);
        info.Tags.Should().Contain("AutotestLogoDelTag");
    }

    [Fact]
    public async Task DeleteLogo_UrlsPresentBeforeAndGoneAfter()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithLogo("Autotest Logo URLs Before After");

        var before = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        before.Logo.Original.Should().NotBeNullOrEmpty();
        before.Logo.Large.Should().NotBeNullOrEmpty();
        before.Logo.Medium.Should().NotBeNullOrEmpty();
        before.Logo.Small.Should().NotBeNullOrEmpty();

        // Act
        await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        var after = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        after.Logo.Original.Should().BeNullOrEmpty();
        after.Logo.Large.Should().BeNullOrEmpty();
        after.Logo.Medium.Should().BeNullOrEmpty();
        after.Logo.Small.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteLogo_DeletedRoom_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithLogo("Autotest Logo Del After Room Deleted");

        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteLogo_ThenNewLogoCanBeUploaded()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithLogo("Autotest Logo Re-upload Room");

        await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken);

        // Act
        var secondTmpFile = await UploadLogo(CreateTestImageBytes());
        var updated = await CreateLogo(room.Id, secondTmpFile);

        // Assert
        updated.Logo.Original.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteLogo_RestoresPreviousCover()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Logo Del Resets Cover");

        await _roomsApi.ChangeRoomCoverAsync(room.Id, new CoverRequestDto("FF5733", coverId), TestContext.Current.CancellationToken);

        var tmpFile = await UploadLogo(CreateTestImageBytes());
        await CreateLogo(room.Id, tmpFile);

        // Act
        await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        // Removing the uploaded image brings back the cover the room had before it, not some
        // fixed default — the TypeScript original hard-coded "schedule", which is merely whichever
        // cover happened to be first in that environment's gallery.
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Logo.Cover.Id.Should().Be(coverId);
    }

    [Fact]
    public async Task DeleteLogo_RoomsListReflectsResetLogo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithLogo("Autotest Logo Del In List Room");

        // Act
        await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert: the typed SDK's FolderContentDto only exposes the fields common to every entry
        // type (FileEntryBaseDto), which drops id/logo — read the raw JSON instead. That is an SDK
        // defect worth reporting, not a preference.
        //
        // The list is deliberately unfiltered: a filterValue query is served from the search index
        // (FolderDao -> factoryIndexer.TrySelectIdsAsync), so a room created moments ago may not be
        // in it yet. The portal belongs to this test alone and holds exactly one room.
        using var response = await _filesClient.GetAsync(
            "api/2.0/files/rooms",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(body);

        var found = json.RootElement.GetProperty("response").GetProperty("folders")
            .EnumerateArray()
            .FirstOrDefault(f => f.GetProperty("id").GetInt32() == room.Id);

        found.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        var originalLogo = found.TryGetProperty("logo", out var logo) && logo.TryGetProperty("original", out var original)
            ? original.GetString()
            : null;
        originalLogo.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteLogo_TemplateFromRoomWithDeletedLogo_HasNoLogo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithLogo("Autotest Logo Del Then Template Source");
        await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Logo Del Then Template", copyLogo: true),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(templateId, TestContext.Current.CancellationToken)).Response;
        info.Logo.Original.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteLogo_RoomFromTemplateWithDeletedLogo_HasNoCustomLogo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithLogo("Autotest Logo Template With Deleted Logo Source");

        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Logo Template With Deleted Logo", copyLogo: true),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        await _roomsApi.DeleteRoomLogoAsync(templateId, TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(
            new CreateRoomFromTemplateDto(templateId, "Autotest Room From Template No Logo"),
            TestContext.Current.CancellationToken);
        var newRoomId = await WaitForRoomFromTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(newRoomId, TestContext.Current.CancellationToken)).Response;
        info.Logo.Original.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteLogo_SharingIsPreserved()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithLogo("Autotest Logo Del Keeps Sharing");

        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Read);

        // Act
        await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        await _filesClient.Authenticate(member);
        var info = await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken);
        info.Response.Id.Should().Be(room.Id);
    }
}
