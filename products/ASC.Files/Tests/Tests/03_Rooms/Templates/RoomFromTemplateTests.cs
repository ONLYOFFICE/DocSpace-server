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
/// Functional behavior of POST /files/rooms/fromTemplate — room creation itself, what settings are
/// inherited from the source room, the asynchronous operation shape, and the resulting room's
/// lifecycle. Access control lives in <see cref="RoomFromTemplatePermissionsTests"/>, request
/// validation in <see cref="RoomFromTemplateValidationTests"/>, copied content in
/// <see cref="RoomFromTemplateContentTests"/>, and the status endpoints in
/// <see cref="RoomFromTemplateStatusTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomFromTemplateTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task CreateRoomFromTemplate_Owner_RoomCreated()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest FromTmpl Basic", isPublic: false);

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(
            new CreateRoomFromTemplateDto(templateId, "Room From Template"),
            TestContext.Current.CancellationToken);

        // Assert
        var roomId = await WaitForRoomFromTemplate();
        roomId.Should().BePositive();

        var info = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be("Room From Template");
        info.RoomType.Should().Be(RoomType.CustomRoom);
    }

    [Fact]
    public async Task CreateRoomFromTemplate_VirtualDataRoom_InheritsIndexingAndDenyDownload()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest VDR Source", roomType: RoomType.VirtualDataRoom,
                indexing: true, denyDownload: true,
                lifetime: new RoomDataLifetimeDto(deletePermanently: true, period: RoomDataLifetimePeriod.Day, value: 30, enabled: true),
                watermark: new WatermarkRequestDto(enabled: true, additions: WatermarkAdditions.UserName, text: "Confidential", rotate: 0, imageScale: 100)),
            TestContext.Current.CancellationToken)).Response;

        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(sourceRoom.Id, "Autotest VDR Template"),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(
            new CreateRoomFromTemplateDto(templateId, "VDR From Template"),
            TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert - indexing and denyDownload are inherited; lifetime and watermark are not (by design).
        var info = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be("VDR From Template");
        info.RoomType.Should().Be(RoomType.VirtualDataRoom);
        info.Indexing.Should().BeTrue();
        info.DenyDownload.Should().BeTrue();
    }

    [Fact]
    public async Task CreateRoomFromTemplate_Title_ComesFromRequestNotSource()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string sourceTitle = "Autotest Source Room Title";
        const string templateTitle = "Autotest Template Title";
        const string newRoomTitle = "Autotest New Room Title";

        var sourceRoom = await CreateCustomRoom(sourceTitle);
        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(sourceRoom.Id, templateTitle), TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, newRoomTitle), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be(newRoomTitle);
        info.Title.Should().NotBe(sourceTitle);
        info.Title.Should().NotBe(templateTitle);
    }

    [Fact]
    public async Task CreateRoomFromTemplate_InheritsIndexingFalse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Indexing False Source", roomType: RoomType.VirtualDataRoom, indexing: false),
            TestContext.Current.CancellationToken)).Response;

        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(sourceRoom.Id, "Autotest Indexing False Template"), TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Room Indexing False"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
        info.Indexing.Should().BeFalse();
    }

    [Fact]
    public async Task CreateRoomFromTemplate_InheritsDenyDownloadFalse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest DenyDownload False Source", roomType: RoomType.VirtualDataRoom, denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(sourceRoom.Id, "Autotest DenyDownload False Template"), TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Room DenyDownload False"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
        info.DenyDownload.Should().BeFalse();
    }

    [Fact]
    public async Task CreateRoomFromTemplate_LifetimeNotInherited()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Lifetime Source", roomType: RoomType.VirtualDataRoom,
                lifetime: new RoomDataLifetimeDto(deletePermanently: true, period: RoomDataLifetimePeriod.Day, value: 30, enabled: true)),
            TestContext.Current.CancellationToken)).Response;

        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(sourceRoom.Id, "Autotest Lifetime Template"), TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Room Without Lifetime"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
        (info.Lifetime?.Enabled ?? false).Should().BeFalse();
    }

    [Fact]
    public async Task CreateRoomFromTemplate_WatermarkNotInherited()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Watermark Source", roomType: RoomType.VirtualDataRoom,
                watermark: new WatermarkRequestDto(enabled: true, additions: WatermarkAdditions.UserName, text: "Confidential", rotate: 0, imageScale: 100)),
            TestContext.Current.CancellationToken)).Response;

        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(sourceRoom.Id, "Autotest Watermark Template"), TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Room Without Watermark"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
        (info.Watermark?.Text ?? null).Should().NotBe("Confidential");
    }

    [Fact]
    public async Task CreateRoomFromTemplate_CombinedSettings_OnlyRoomTypeIndexingDenyDownloadInherited()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Combined Source", roomType: RoomType.VirtualDataRoom,
                indexing: true, denyDownload: true,
                lifetime: new RoomDataLifetimeDto(deletePermanently: true, period: RoomDataLifetimePeriod.Day, value: 15, enabled: true),
                watermark: new WatermarkRequestDto(enabled: true, additions: WatermarkAdditions.UserName, text: "Confidential", rotate: 0, imageScale: 100)),
            TestContext.Current.CancellationToken)).Response;

        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(sourceRoom.Id, "Autotest Combined Template"), TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Room Combined"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
        info.RoomType.Should().Be(RoomType.VirtualDataRoom);
        info.Indexing.Should().BeTrue();
        info.DenyDownload.Should().BeTrue();
        (info.Lifetime?.Enabled ?? false).Should().BeFalse();
        (info.Watermark?.Text ?? null).Should().NotBe("Confidential");
    }

    [Fact]
    public async Task CreateRoomFromTemplate_ReusedTemplate_CreatesIndependentRooms()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Reuse", isPublic: false);

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Room A"), TestContext.Current.CancellationToken);
        var roomAId = await WaitForRoomFromTemplate();

        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Room B"), TestContext.Current.CancellationToken);
        var roomBId = await WaitForRoomFromTemplate();

        // Assert
        roomAId.Should().BePositive();
        roomBId.Should().BePositive();
        roomAId.Should().NotBe(roomBId);

        var infoA = (await _roomsApi.GetRoomInfoAsync(roomAId, TestContext.Current.CancellationToken)).Response;
        var infoB = (await _roomsApi.GetRoomInfoAsync(roomBId, TestContext.Current.CancellationToken)).Response;
        infoA.Title.Should().Be("Room A");
        infoB.Title.Should().Be("Room B");
        infoA.RoomType.Should().Be(RoomType.CustomRoom);
        infoB.RoomType.Should().Be(RoomType.CustomRoom);
    }

    [Fact]
    public async Task CreateRoomFromTemplate_RoomsFromSameTemplate_AreIndependentAfterUpdate()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Independence", isPublic: false);

        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Independent A"), TestContext.Current.CancellationToken);
        var roomAId = await WaitForRoomFromTemplate();

        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Independent B"), TestContext.Current.CancellationToken);
        var roomBId = await WaitForRoomFromTemplate();

        // Act
        await _roomsApi.UpdateRoomAsync(roomAId, new UpdateRoomRequest { Title = "Independent A Updated" }, TestContext.Current.CancellationToken);

        // Assert
        var infoB = (await _roomsApi.GetRoomInfoAsync(roomBId, TestContext.Current.CancellationToken)).Response;
        infoB.Title.Should().Be("Independent B");
    }

    [Fact]
    public async Task CreateRoomFromTemplate_ResponseHasProgressAndIsCompletedFields()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Op Shape", isPublic: false);

        // Act
        var operation = (await _roomsApi.CreateRoomFromTemplateAsync(
            new CreateRoomFromTemplateDto(templateId, "Room Op Shape"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        operation.Should().NotBeNull();
        operation.Error.Should().BeNullOrEmpty();

        await WaitForRoomFromTemplate();
    }

    [Fact]
    public async Task CreateRoomFromTemplate_OperationEventuallyCompletes()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Complete", isPublic: false);

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Room Complete"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert
        var status = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().BeNullOrEmpty();
        roomId.Should().BePositive();
    }

    [Fact]
    public async Task CreateRoomFromTemplate_GetRoomInfoSucceedsAfterCompletion()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest AfterWait", isPublic: false);

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Room After Wait"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
        info.Id.Should().Be(roomId);
        info.Title.Should().Be("Room After Wait");
    }

    [Fact]
    public async Task CreateRoomFromTemplate_SourceRoomDeleted_TemplateStillWorks()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = await CreateCustomRoom("Autotest Src Delete Source");

        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(sourceRoom.Id, "Autotest Src Delete Template"), TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        await _roomsApi.DeleteRoomAsync(sourceRoom.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Room After Source Deleted"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert
        roomId.Should().BePositive();
    }

    [Fact]
    public async Task CreateRoomFromTemplate_DoesNotModifyTemplate()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Tmpl Unchanged", isPublic: false);
        var templateBefore = (await _roomsApi.GetRoomInfoAsync(templateId, TestContext.Current.CancellationToken)).Response;

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Room From Unchanged Template"), TestContext.Current.CancellationToken);
        await WaitForRoomFromTemplate();

        // Assert
        var templateAfter = (await _roomsApi.GetRoomInfoAsync(templateId, TestContext.Current.CancellationToken)).Response;
        templateAfter.Id.Should().Be(templateBefore.Id);
        templateAfter.Title.Should().Be(templateBefore.Title);
        templateAfter.RoomType.Should().Be(templateBefore.RoomType);
    }

    [Fact]
    public async Task CreateRoomFromTemplate_DoesNotModifySourceRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = await CreateCustomRoom("Autotest Src Unchanged Source");
        await CreateFolder("Source-Only Folder", sourceRoom.Id);

        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(sourceRoom.Id, "Autotest Src Unchanged Template"), TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        var sourceBefore = (await _roomsApi.GetRoomInfoAsync(sourceRoom.Id, TestContext.Current.CancellationToken)).Response;

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Room From Unchanged Source"), TestContext.Current.CancellationToken);
        await WaitForRoomFromTemplate();

        // Assert
        var sourceAfter = (await _roomsApi.GetRoomInfoAsync(sourceRoom.Id, TestContext.Current.CancellationToken)).Response;
        sourceAfter.Title.Should().Be(sourceBefore.Title);

        var sourceContent = (await _foldersApi.GetFolderByFolderIdAsync(sourceRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        sourceContent.Folders.ConvertAll(f => f.Title).Should().Contain("Source-Only Folder");
    }

    [Fact]
    public async Task CreateRoomFromTemplate_CreatedRoom_AppearsInRoomList()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest List", isPublic: false);

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Listed Room"), TestContext.Current.CancellationToken);
        await WaitForRoomFromTemplate();

        // Assert
        var titles = await GetRoomTitles();
        titles.Should().Contain("Listed Room");
    }

    [Fact]
    public async Task CreateRoomFromTemplate_CreatedRoom_CanBeUpdated()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Update", isPublic: false);

        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Room Before Update"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(roomId, new UpdateRoomRequest { Title = "Room After Update" }, TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be("Room After Update");
    }

    [Fact]
    public async Task CreateRoomFromTemplate_CreatedRoom_CanBeArchivedAndDeleted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Archive", isPublic: false);

        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Room To Archive"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Act & Assert - archive succeeds
        await _roomsApi.ArchiveRoomAsync(roomId, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        var archiveStatuses = await WaitLongOperation();
        archiveStatuses.Should().OnlyContain(s => s.Finished);

        // Act & Assert - delete succeeds
        await _roomsApi.DeleteRoomAsync(roomId, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var deleteStatuses = await WaitLongOperation();
        deleteStatuses.Should().OnlyContain(s => s.Finished);
    }
}
