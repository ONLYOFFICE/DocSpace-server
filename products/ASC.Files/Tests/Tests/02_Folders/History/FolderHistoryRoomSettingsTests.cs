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

namespace ASC.Files.Tests.Tests._02_Folders.History;

/// <summary>
/// <c>GET /api/2.0/files/folder/{folderId}/log</c> - room-level appearance and settings actions
/// (tags, logo, color, cover, archive state, data lifetime, indexing, watermark, download
/// restriction) and the room's external links.
/// </summary>
[Trait("Category", "Folders")]
[Trait("Feature", "History")]
public class FolderHistoryRoomSettingsTests(
    AspireAppFixture fixture)
    : FolderHistoryTestBase(fixture)
{
    [Fact]
    public async Task GetFolderHistory_TagAdded_ContainsAddedRoomTags()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History AddedRoomTags");
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("AutotestHistoryTag"), TestContext.Current.CancellationToken);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.AddedRoomTags);

        // Act
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["AutotestHistoryTag"]), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.AddedRoomTags, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_TagRemoved_ContainsDeletedRoomTags()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History DeletedRoomTags");
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("AutotestHistoryTagDelete"), TestContext.Current.CancellationToken);
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["AutotestHistoryTagDelete"]), TestContext.Current.CancellationToken);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.DeletedRoomTags);

        // Act
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["AutotestHistoryTagDelete"]), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.DeletedRoomTags, displayName, timeoutSeconds: 15);
    }

    [Fact]
    public async Task GetFolderHistory_LogoSet_ContainsRoomLogoCreated()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History RoomLogoCreated");

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomLogoCreated);

        // Act
        await SetRoomLogoAsync(room.Id);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomLogoCreated, displayName, timeoutSeconds: 30);
    }

    [Fact]
    public async Task GetFolderHistory_LogoRemoved_ContainsRoomLogoDeleted()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History RoomLogoDeleted");
        await SetRoomLogoAsync(room.Id);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomLogoDeleted);

        // Act
        var deleted = (await _roomsApi.DeleteRoomLogoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var originalLogo = deleted.Logo?.Original;
        originalLogo.Should().BeNullOrEmpty();

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomLogoDeleted, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_IconColorChanged_ContainsRoomColorChanged()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History RoomColorChanged");

        // Act
        await _roomsApi.ChangeRoomCoverAsync(room.Id, new CoverRequestDto(color: "FF5733"), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomColorChanged, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_CoverChanged_ContainsRoomCoverChanged()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Folder History RoomCoverChanged");

        // Act
        await _roomsApi.ChangeRoomCoverAsync(room.Id, new CoverRequestDto(color: "1A2B3C", cover: coverId), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomCoverChanged, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_RoomRestoredFromArchive_ContainsRoomUnarchived()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History RoomUnarchived");
        await ArchiveRoom(room.Id);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomUnarchived);

        // Act
        await UnarchiveRoomAsync(room.Id);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomUnarchived, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_LifetimeSet_ContainsRoomLifeTimeSet()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History RoomLifeTimeSet");

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomLifeTimeSet);

        // Act
        await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(lifetime: new RoomDataLifetimeDto(deletePermanently: false, period: RoomDataLifetimePeriod.Day, value: 30, enabled: true)),
            TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomLifeTimeSet, displayName, timeoutSeconds: 30);
    }

    [Fact]
    public async Task GetFolderHistory_LifetimeDisabled_ContainsRoomLifeTimeDisabled()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History RoomLifeTimeDisabled");
        await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(lifetime: new RoomDataLifetimeDto(deletePermanently: false, period: RoomDataLifetimePeriod.Day, value: 30, enabled: true)),
            TestContext.Current.CancellationToken);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomLifeTimeDisabled);

        // Act
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(lifetime: new RoomDataLifetimeDto(enabled: false)), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomLifeTimeDisabled, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_IndexingEnabled_ContainsRoomIndexingEnabled()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History RoomIndexingEnabled");

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomIndexingEnabled);

        // Act
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(indexing: true), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomIndexingEnabled, displayName, timeoutSeconds: 30);
    }

    [Fact]
    public async Task GetFolderHistory_IndexingDisabled_ContainsRoomIndexingDisabled()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History RoomIndexingDisabled");
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(indexing: true), TestContext.Current.CancellationToken);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomIndexingDisabled);

        // Act
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(indexing: false), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomIndexingDisabled, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_RoomReordered_ContainsFolderIndexReordered()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History FolderIndexReordered");
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(indexing: true), TestContext.Current.CancellationToken);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.FolderIndexReordered);

        // Act
        await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.FolderIndexReordered, displayName, timeoutSeconds: 30);
    }

    [Fact]
    public async Task GetFolderHistory_WatermarkEnabled_ContainsRoomWatermarkSet()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History RoomWatermarkSet");

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomWatermarkSet);

        // Act
        await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(watermark: new WatermarkRequestDto(enabled: true, additions: WatermarkAdditions.UserName)),
            TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomWatermarkSet, displayName, timeoutSeconds: 30);
    }

    [Fact]
    public async Task GetFolderHistory_WatermarkDisabled_ContainsRoomWatermarkDisabled()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History RoomWatermarkDisabled");
        await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(watermark: new WatermarkRequestDto(enabled: true, additions: WatermarkAdditions.UserName)),
            TestContext.Current.CancellationToken);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomWatermarkDisabled);

        // Act
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(watermark: new WatermarkRequestDto(enabled: false)), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomWatermarkDisabled, displayName, timeoutSeconds: 30);
    }

    [Fact]
    public async Task GetFolderHistory_DownloadRestricted_ContainsRoomDenyDownloadEnabled()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History RoomDenyDownloadEnabled");

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomDenyDownloadEnabled);

        // Act
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(denyDownload: true), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomDenyDownloadEnabled, displayName, timeoutSeconds: 30);
    }

    [Fact]
    public async Task GetFolderHistory_DownloadUnrestricted_ContainsRoomDenyDownloadDisabled()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History RoomDenyDownloadDisabled");
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(denyDownload: true), TestContext.Current.CancellationToken);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomDenyDownloadDisabled);

        // Act
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(denyDownload: false), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomDenyDownloadDisabled, displayName, timeoutSeconds: 30);
    }

    [Fact]
    public async Task GetFolderHistory_RoomCreated_ContainsRoomExternalLinkCreated()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();

        // Act - a PublicRoom is created with a primary external link already attached.
        var room = await CreatePublicRoom("Autotest Folder History RoomExternalLinkCreated");

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomExternalLinkCreated, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_LinkRenamed_ContainsRoomExternalLinkRenamed()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreatePublicRoom("Autotest Folder History RoomExternalLinkRenamed");
        var link = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Original Link Title", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomExternalLinkRenamed);

        // Act
        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: link.SharedLink.Id, access: FileShare.Read, linkType: LinkType.External, title: "Renamed Link Title", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomExternalLinkRenamed, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_PrimaryLinkRevoked_ContainsRoomExternalLinkRevoked()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreatePublicRoom("Autotest Folder History RoomExternalLinkRevoked");
        var primaryLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Act
        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: primaryLink.SharedLink.Id, access: FileShare.None, linkType: LinkType.External, denyDownload: false),
            TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomExternalLinkRevoked, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_LinkDeleted_ContainsRoomExternalLinkDeleted()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreatePublicRoom("Autotest Folder History RoomExternalLinkDeleted");
        var link = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Link To Delete", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Act
        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: link.SharedLink.Id, access: FileShare.None, linkType: LinkType.External, title: "Link To Delete", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomExternalLinkDeleted, displayName);
    }
}
