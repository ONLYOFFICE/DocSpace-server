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
/// The private rooms section, built the way Forms is: private rooms stay under the rooms tree but are listed only here,
/// by their own setting rather than by a room type. <c>privacyFilter=Private</c> on the Rooms area keeps meaning this
/// section, because the web client builds its section with it.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomSectionTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private async Task<(FolderDto Private, FolderDto Ordinary)> CreateOneOfEachAsOwner()
    {
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");

        return (await CreatePrivateRoom($"Private {Guid.NewGuid():N}", RoomType.CustomRoom), await CreateCustomRoom($"Ordinary {Guid.NewGuid():N}"));
    }

    private async Task<List<string>> RoomTitles(SearchArea area, RoomPrivacyFilter? privacyFilter = null)
    {
        var rooms = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: area,
            privacyFilter: privacyFilter,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        return [.. rooms.Folders.Select(f => f.Title)];
    }

    [Fact]
    public async Task RoomsArea_LeavesPrivateRoomsOut()
    {
        var (privateRoom, ordinary) = await CreateOneOfEachAsOwner();

        (await RoomTitles(SearchArea.Active)).Should().Contain(ordinary.Title).And.NotContain(privateRoom.Title);
    }

    [Fact]
    public async Task PrivateRoomsArea_ListsOnlyPrivateRooms()
    {
        var (privateRoom, ordinary) = await CreateOneOfEachAsOwner();

        (await RoomTitles(SearchArea.PrivateRooms)).Should().Contain(privateRoom.Title).And.NotContain(ordinary.Title);
    }

    [Fact]
    public async Task PrivacyFilterOnTheRoomsArea_StillSplitsTheRooms()
    {
        var (privateRoom, ordinary) = await CreateOneOfEachAsOwner();

        (await RoomTitles(SearchArea.Active, RoomPrivacyFilter.Private)).Should().Contain(privateRoom.Title).And.NotContain(ordinary.Title);
        (await RoomTitles(SearchArea.Active, RoomPrivacyFilter.NotPrivate)).Should().Contain(ordinary.Title).And.NotContain(privateRoom.Title);
    }

    [Fact]
    public async Task SectionEndpoint_ListsOnlyPrivateRooms()
    {
        var (privateRoom, ordinary) = await CreateOneOfEachAsOwner();

        var section = (await _foldersApi.GetPrivateRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        var titles = section.Folders.Select(f => f.Title).ToList();
        titles.Should().Contain(privateRoom.Title).And.NotContain(ordinary.Title);
        section.Files.Should().BeEmpty("the section is a flat list of rooms");
        section.Current.RootFolderType.Should().Be(FolderType.PrivateRooms);
    }

    [Fact]
    public async Task BrowsingTheRootById_ListsTheSameRooms()
    {
        var (privateRoom, ordinary) = await CreateOneOfEachAsOwner();
        var root = (await _foldersApi.GetPrivateRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;

        var content = (await _foldersApi.GetFolderByFolderIdAsync(root, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var titles = content.Folders.Select(f => f.Title).ToList();
        titles.Should().Contain(privateRoom.Title).And.NotContain(ordinary.Title);
    }

    [Fact]
    public async Task RootFolders_ExposeTheSection()
    {
        await _filesClient.Authenticate(Owner);

        var roots = (await _foldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        roots.Should().Contain(r => r.Current.RootFolderType == FolderType.PrivateRooms);
    }

    [Fact]
    public async Task AppTurnedOff_HidesTheRootAndTheEndpoint()
    {
        await _webApiClient.Authenticate(Owner);
        var apps = new DocSpace.API.SDK.Api.Apps.AppsApi(_webApiClient, new Configuration { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') });
        await apps.SetEnabledAsync("e2e-rooms", new SetAppEnabledRequest(false), TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        var roots = (await _foldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        roots.Should().NotContain(r => r.Current.RootFolderType == FolderType.PrivateRooms);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.GetPrivateRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task AdminOutsideTheRoom_DoesNotSeeItInTheSection()
    {
        var creator = await InviteWithKeys(EmployeeType.RoomAdmin);
        var room = await CreatePrivateRoom($"Private {Guid.NewGuid():N}", RoomType.CustomRoom);

        await _filesClient.Authenticate(Owner);

        (await RoomTitles(SearchArea.PrivateRooms)).Should().NotContain(room.Title);

        await _filesClient.Authenticate(creator);

        (await RoomTitles(SearchArea.PrivateRooms)).Should().Contain(room.Title);
    }

    [Fact]
    public async Task Member_SeesTheRoomInTheSectionAndNotInRooms()
    {
        var member = await InviteWithKeys(EmployeeType.User);
        var (privateRoom, _) = await CreateOneOfEachAsOwner();
        await InviteToRoom(privateRoom.Id, member, FileShare.Read);

        await _filesClient.Authenticate(member);

        (await RoomTitles(SearchArea.PrivateRooms)).Should().Contain(privateRoom.Title);
        (await RoomTitles(SearchArea.Active)).Should().NotContain(privateRoom.Title);
    }

    [Fact]
    public async Task ArchivedPrivateRoom_LeavesTheSectionForTheArchive()
    {
        var (privateRoom, _) = await CreateOneOfEachAsOwner();

        await _roomsApi.ArchiveRoomAsync(privateRoom.Id, new ArchiveRoomRequest { DeleteAfter = false }, TestContext.Current.CancellationToken);

        var archive = await PollUntil(() => RoomTitles(SearchArea.Archive), titles => titles.Contains(privateRoom.Title));

        archive.Should().Contain(privateRoom.Title);
        (await RoomTitles(SearchArea.PrivateRooms)).Should().NotContain(privateRoom.Title);
    }

    [Fact]
    public async Task NewItems_CountInTheSectionAndNotInRooms()
    {
        var member = await InviteWithKeys(EmployeeType.User);
        var (privateRoom, _) = await CreateOneOfEachAsOwner();
        await InviteToRoom(privateRoom.Id, member, FileShare.ContentCreator);

        await _filesClient.Authenticate(Owner);
        await UploadEncryptedFile(privateRoom.Id, "Encrypted.docx");

        await _filesClient.Authenticate(member);

        var section = await PollUntil(
            async () => (await _foldersApi.GetPrivateRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response,
            content => content.Current.New > 0);

        section.Current.New.Should().BeGreaterThan(0);

        var rooms = (await _roomsApi.GetRoomsFolderAsync(searchArea: SearchArea.Active, cancellationToken: TestContext.Current.CancellationToken)).Response;

        rooms.Current.New.Should().Be(0);
    }

    [Fact]
    public async Task RecentWithThePrivateRoomsArea_IsABadRequest()
    {
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _foldersApi.GetRecentFolderAsync(
            searchArea: SearchArea.PrivateRooms,
            cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task Group_TakesRoomsOfItsOwnSectionOnly()
    {
        var (privateRoom, ordinary) = await CreateOneOfEachAsOwner();

        var group = (await _roomGroupsApi.AddRoomGroupAsync(
            new RoomGroupRequestDto("Private group", "star", [new DuplicateRequestDtoAllOfFileIds(privateRoom.Id)], SearchArea.PrivateRooms),
            TestContext.Current.CancellationToken)).Response;

        group.SearchArea.Should().Be(SearchArea.PrivateRooms);
        group.TotalRooms.Should().Be(1);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.AddRoomGroupAsync(
            new RoomGroupRequestDto("Mixed group", "star", [new DuplicateRequestDtoAllOfFileIds(ordinary.Id)], SearchArea.PrivateRooms),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);

        var rooms = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.AddRoomGroupAsync(
            new RoomGroupRequestDto("Rooms group", "star", [new DuplicateRequestDtoAllOfFileIds(privateRoom.Id)], SearchArea.Active),
            TestContext.Current.CancellationToken));

        rooms.ErrorCode.Should().Be(400);
    }
}
