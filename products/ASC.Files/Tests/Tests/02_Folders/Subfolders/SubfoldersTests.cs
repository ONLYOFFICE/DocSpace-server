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

namespace ASC.Files.Tests.Tests._02_Folders.Subfolders;

[Trait("Category", "CRUD")]
[Trait("Feature", "Folders")]
public class SubfoldersTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    public static TheoryData<FolderType> VirtualFolderTypes =>
    [
        FolderType.USER, FolderType.TRASH, FolderType.Recent, FolderType.Favorites
    ];

    /// <summary>
    /// Every room type that accepts a subfolder. <c>AiRoom</c> is deliberately absent: it refuses
    /// folder creation even for the room owner, so it is covered separately by
    /// <see cref="GetSubfolders_AiRoom_Returns200WithNoSubfolders"/>.
    /// </summary>
    public static TheoryData<RoomType> AllRoomTypes =>
    [
        RoomType.CustomRoom, RoomType.FillingFormsRoom, RoomType.EditingRoom,
        RoomType.PublicRoom, RoomType.VirtualDataRoom
    ];

    [Fact]
    public async Task GetSubfolders_ReturnsCorrectTitles()
    {
        var room = await CreateCustomRoom("Autotest Room For Subfolders Titles");
        var title1 = "Autotest Subfolder Alpha";
        var title2 = "Autotest Subfolder Beta";
        await CreateFolder(title1, room.Id);
        await CreateFolder(title2, room.Id);

        var result = await _foldersApi.GetFoldersAsync(room.Id, TestContext.Current.CancellationToken);

        var titles = result.Response.Select(f => f.Title).ToList();
        titles.Should().Contain(title1);
        titles.Should().Contain(title2);
    }

    [Fact]
    public async Task GetSubfolders_CountMatchesResponseLength()
    {
        var room = await CreateCustomRoom("Autotest Room For Subfolders Count");
        for (var i = 1; i <= 3; i++)
        {
            await CreateFolder($"Autotest Subfolder Count {i}", room.Id);
        }

        var result = await _foldersApi.GetFoldersAsync(room.Id, TestContext.Current.CancellationToken);

        result.Response.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetSubfolders_FolderWithOnlyFiles_ReturnsEmptyArray()
    {
        var folder = await CreateFolderInMy("Autotest Folder Files Only Subfolders", Owner);
        await CreateFile("Autotest File Only", folder.Id);

        var result = await _foldersApi.GetFoldersAsync(folder.Id, TestContext.Current.CancellationToken);

        result.Response.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSubfolders_Returns10Subfolders()
    {
        var room = await CreateCustomRoom("Autotest Room For 10 Subfolders");
        for (var i = 1; i <= 10; i++)
        {
            await CreateFolder($"Autotest Subfolder {i}", room.Id);
        }

        var result = await _foldersApi.GetFoldersAsync(room.Id, TestContext.Current.CancellationToken);

        result.Response.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetSubfolders_ReturnsOnlyDirectSubfolders_NotNested()
    {
        var room = await CreateCustomRoom("Autotest Room For Nested Subfolders");
        var direct = await CreateFolder("Autotest Direct Subfolder", room.Id);
        await CreateFolder("Autotest Nested Subfolder", direct.Id);

        var result = await _foldersApi.GetFoldersAsync(room.Id, TestContext.Current.CancellationToken);

        result.Response.Should().ContainSingle();
        result.Response[0].Title.Should().Be("Autotest Direct Subfolder");
    }

    [Theory]
    [MemberData(nameof(VirtualFolderTypes))]
    public async Task GetSubfolders_VirtualFolder_Returns200(FolderType folderType)
    {
        var folderId = await GetFolderIdAsync(folderType, Owner);

        var result = await _foldersApi.GetFoldersAsync(folderId, TestContext.Current.CancellationToken);

        result.Response.Should().NotBeNull();
    }

    [Fact]
    [Trait("Bug", "81464")]
    public async Task GetSubfolders_NonExistentFolderId_Returns404()
    {
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFoldersAsync(999999999, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    [Trait("Bug", "81464")]
    public async Task GetSubfolders_DeletedFolder_Returns404()
    {
        var folder = await CreateFolderInMy("Autotest Folder For Subfolders After Delete", Owner);

        await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder(deleteAfter: true, immediately: true), TestContext.Current.CancellationToken);

        // Deletion runs as a background operation, so poll until the folder is actually gone
        // before asserting on the endpoint under test.
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken);
            }
            catch (ApiException)
            {
                break;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFoldersAsync(folder.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Theory]
    [MemberData(nameof(AllRoomTypes))]
    public async Task GetSubfolders_RoomType_Returns200(RoomType roomType)
    {
        var room = await CreateRoomOfType(roomType, $"Autotest {roomType} For Subfolders");
        await CreateFolder($"Autotest Subfolder In {roomType}", room.Id);

        var result = await _foldersApi.GetFoldersAsync(room.Id, TestContext.Current.CancellationToken);

        result.Response.Should().NotBeEmpty();
    }

    /// <summary>
    /// An AI room refuses subfolder creation ("You don't have enough permission to create") even to
    /// its owner, so it is covered on its own rather than as a row of
    /// <see cref="GetSubfolders_RoomType_Returns200"/>: the listing must answer 200 and report the
    /// built-in folder the room is created with.
    /// </summary>
    [Fact]
    public async Task GetSubfolders_AiRoom_ListsBuiltInFolderAndRefusesNewOnes()
    {
        var room = await CreateAiRoom("Autotest AiRoom For Subfolders");

        var result = await _foldersApi.GetFoldersAsync(room.Id, TestContext.Current.CancellationToken);

        result.Response.Should().NotBeEmpty();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateFolder("Autotest Subfolder In AiRoom", room.Id));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetSubfolders_ArchivedRoom_OwnerReturns200()
    {
        var room = await CreateCustomRoom("Autotest Room For Archived Subfolders");
        await CreateFolder("Autotest Subfolder In Archived Room", room.Id);

        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(deleteAfter: false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        var result = await _foldersApi.GetFoldersAsync(room.Id, TestContext.Current.CancellationToken);

        result.Response.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSubfolders_ReturnsAllSubfolders_WhenCountExceeds25()
    {
        var room = await CreateCustomRoom("Autotest Room For 30 Subfolders");
        for (var i = 1; i <= 30; i++)
        {
            await CreateFolder($"Autotest Subfolder Paged {i}", room.Id);
        }

        var result = await _foldersApi.GetFoldersAsync(room.Id, TestContext.Current.CancellationToken);

        result.Response.Should().HaveCount(30);
    }

    [Fact]
    public async Task GetFolderByFolderId_CustomOrder_SortsBySetOrder()
    {
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Room For Order", roomType: RoomType.CustomRoom, indexing: true),
            TestContext.Current.CancellationToken)).Response;

        var folderA = await CreateFolder("Autotest Folder A", room.Id);
        var folderB = await CreateFolder("Autotest Folder B", room.Id);

        await _foldersApi.SetFolderOrderAsync(folderA.Id, new OrderRequestDto(2), TestContext.Current.CancellationToken);
        await _foldersApi.SetFolderOrderAsync(folderB.Id, new OrderRequestDto(1), TestContext.Current.CancellationToken);

        // sortBy "10" is CustomOrder — see getFolderSortedByCustomOrder in the TS suite.
        var content = (await _foldersApi.GetFolderByFolderIdAsync(
            room.Id, sortBy: "10", sortOrder: SortOrder.Ascending, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var titles = content.Folders
            .Select(f => f.Title)
            .Where(t => t is "Autotest Folder A" or "Autotest Folder B")
            .ToList();

        titles.IndexOf("Autotest Folder B").Should().BeLessThan(titles.IndexOf("Autotest Folder A"));
    }

    private async Task<FolderDtoInteger> CreateRoomOfType(RoomType roomType, string title) => roomType switch
    {
        RoomType.CustomRoom => await CreateCustomRoom(title),
        RoomType.FillingFormsRoom => await CreateFillingFormsRoom(title),
        RoomType.EditingRoom => await CreateCollaborationRoom(title),
        RoomType.PublicRoom => await CreatePublicRoom(title),
        RoomType.VirtualDataRoom => await CreateVirtualRoom(title),
        RoomType.AiRoom => await CreateAiRoom(title),
        _ => throw new ArgumentOutOfRangeException(nameof(roomType), roomType, "Unsupported room type for this test")
    };
}
