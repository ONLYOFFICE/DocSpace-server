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

namespace ASC.Files.Tests.Tests._08_Privacy;

/// <summary>
/// What must not cross the border of a private room: links to it, a copy of the room as a duplicate or
/// a template, folders moved in either direction, and a file copied through <c>copyas</c>.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomContentBoundaryTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private async Task<FolderDto> CreateRoomAsOwner()
    {
        await _filesClient.Authenticate(Owner);

        return await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
    }

    private async Task<FolderDto> CreateFolder(int parentId, string title)
    {
        return (await _foldersApi.CreateFolderAsync(parentId, new CreateFolder(title), TestContext.Current.CancellationToken)).Response;
    }

    private async Task<List<string>> FolderTitles(int folderId)
    {
        var content = (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return content.Folders.OfType<FolderDto>().Select(f => f.Title).ToList();
    }

    /// <summary>A bulk operation is refused either up front or when queued; what counts is where the folder ends up.</summary>
    private async Task TryRun(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (ApiException)
        {
            return;
        }

        await WaitLongOperation();
    }

    private static BatchRequestDto FolderBatch(int folderId, int destinationId) => new()
    {
        DestFolderId = new BatchRequestDtoAllOfDestFolderId(destinationId),
        ConflictResolveType = FileConflictResolveType.Skip,
        FileIds = [],
        FolderIds = [new(folderId)],
        ReturnSingleOperation = true
    };

    private Task TryMoveFolder(int folderId, int destinationId) =>
        TryRun(() => _filesOperationsApi.MoveBatchItemsAsync(FolderBatch(folderId, destinationId), TestContext.Current.CancellationToken));

    [Theory]
    [InlineData(LinkType.External)]
    [InlineData(LinkType.Invitation)]
    public async Task SetRoomLink_IsRefused(LinkType linkType)
    {
        var room = await CreateRoomAsOwner();

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: linkType, title: "Link"),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DuplicateRoom_MakesNoCopy()
    {
        var room = await CreateRoomAsOwner();

        await TryRun(() => _filesOperationsApi.DuplicateBatchItemsAsync(
            new DuplicateRequestDto(folderIds: [new(room.Id)]),
            TestContext.Current.CancellationToken));

        var rooms = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        rooms.Folders.OfType<FolderDto>().Should().ContainSingle(f => f.Title.StartsWith(room.Title));
    }

    [Fact]
    public async Task RoomSecurity_ReportsNoDuplicateNoCopyNoIndexExport()
    {
        var room = await CreateRoomAsOwner();
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        info.Security.Duplicate.Should().BeFalse();
        info.Security.Copy.Should().BeFalse();
        info.Security.IndexExport.Should().BeFalse();
    }

    [Fact]
    public async Task CreateTemplateFromTheRoom_IsRefused()
    {
        var room = await CreateRoomAsOwner();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(room.Id, "Autotest Template"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task MoveSubfolderOut_LeavesItInTheRoom()
    {
        var room = await CreateRoomAsOwner();
        var subfolder = await CreateFolder(room.Id, "Subfolder");
        var outside = await CreateFolder(await GetUserFolderIdAsync(Owner), "Outside");

        await TryMoveFolder(subfolder.Id, outside.Id);

        (await FolderTitles(outside.Id)).Should().BeEmpty();
        (await FolderTitles(room.Id)).Should().Contain("Subfolder");
    }

    [Fact]
    public async Task MoveOpenFolderIn_LeavesItOutside()
    {
        var room = await CreateRoomAsOwner();
        var outside = await CreateFolder(await GetUserFolderIdAsync(Owner), "Outside");

        await TryMoveFolder(outside.Id, room.Id);

        (await FolderTitles(room.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task MoveSubfolderWithinTheRoom_IsAllowed()
    {
        var room = await CreateRoomAsOwner();
        var first = await CreateFolder(room.Id, "First");
        var second = await CreateFolder(room.Id, "Second");

        await TryMoveFolder(first.Id, second.Id);

        (await FolderTitles(second.Id)).Should().Contain("First");
    }

    [Fact]
    public async Task ArchivedRoom_SubfolderCopyLeavesNoEmptyTreeOutside()
    {
        var room = await CreateRoomAsOwner();
        var subfolder = await CreateFolder(room.Id, "Subfolder");
        await ArchiveRoom(room.Id);
        var outside = await CreateFolder(await GetUserFolderIdAsync(Owner), "Outside");

        await TryRun(() => _filesOperationsApi.CopyBatchItemsAsync(FolderBatch(subfolder.Id, outside.Id), TestContext.Current.CancellationToken));

        (await FolderTitles(outside.Id)).Should().BeEmpty();
    }

    [Theory]
    [InlineData("Copy.docx")]
    [InlineData("Copy.txt")]
    public async Task CopyAs_OutOfTheRoom_IsRefused(string destTitle)
    {
        var room = await CreateRoomAsOwner();
        var file = await CreateFile("Encrypted.docx", room.Id);
        var outside = await CreateFolder(await GetUserFolderIdAsync(Owner), "Outside");

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CopyFileAsAsync(
            file.Id,
            new CopyAsRequest(destTitle, new CopyAsRequestDestFolderId(outside.Id)),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData("Copy.docx")]
    [InlineData("Copy.txt")]
    public async Task CopyAs_IntoTheRoom_IsRefused(string destTitle)
    {
        var room = await CreateRoomAsOwner();
        var outside = await CreateFolder(await GetUserFolderIdAsync(Owner), "Outside");
        var file = await CreateFile("Plain.docx", outside.Id);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CopyFileAsAsync(
            file.Id,
            new CopyAsRequest(destTitle, new CopyAsRequestDestFolderId(room.Id)),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyAs_IntoASubfolderOfTheRoom_IsRefused()
    {
        var room = await CreateRoomAsOwner();
        var subfolder = await CreateFolder(room.Id, "Subfolder");
        var file = await CreateFile("Plain.docx", await GetUserFolderIdAsync(Owner));

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CopyFileAsAsync(
            file.Id,
            new CopyAsRequest("Copy.docx", new CopyAsRequestDestFolderId(subfolder.Id)),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ArchivedRoom_FileAccessKeys_AreReadable()
    {
        await _filesClient.Authenticate(Owner);
        var ownerKey = await SetFakeKeys(publicKeyPrefix: "owner");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
        var file = await CreateFile("Encrypted.docx", room.Id);
        await ArchiveRoom(room.Id);

        var keys = (await _sharingApi.GetEncryptionAccessAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        keys.Select(k => k.PublicKey).Should().Contain(ownerKey.PublicKey);
    }
}
