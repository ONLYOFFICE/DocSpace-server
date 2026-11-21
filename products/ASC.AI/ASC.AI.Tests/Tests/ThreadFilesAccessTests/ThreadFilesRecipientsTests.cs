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

namespace ASC.AI.Tests.Tests.ThreadFilesAccessTests;

[Trait("Category", "Socket")]
[Trait("Feature", "AI/ThreadFiles")]
public class ThreadFilesRecipientsTests(AspireAppFixture fixture) : ThreadFilesTestBase(fixture)
{
    private const string ModifyFolder = "s:modify-folder";

    private static readonly TimeSpan _noDeliveryWindow = TimeSpan.FromSeconds(2);

    public enum Watcher
    {
        ProjectOwner,
        PortalAdmin,
        Member
    }

    [Fact]
    public async Task UploadToProject_ReachesTheAuthor()
    {
        var projectId = await CreateProjectAsync();
        var author = await InviteMemberAsync(projectId);
        await using var authorSocket = await WatchAsync(author, projectId);

        await FilesHttpClient.Authenticate(author);
        var file = await UploadAsync(projectId, "notes.txt");

        await authorSocket.WaitForAsync(ModifyFolder, CreatedFile(file.Id), cancellationToken: TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(Watcher.ProjectOwner)]
    [InlineData(Watcher.PortalAdmin)]
    [InlineData(Watcher.Member)]
    public async Task UploadToProject_DoesNotReachAnotherUser(Watcher watcher)
    {
        var projectId = await CreateProjectAsync();
        var author = await InviteMemberAsync(projectId);
        var other = watcher switch
        {
            Watcher.ProjectOwner => Owner,
            Watcher.PortalAdmin => await InviteContact(EmployeeType.DocSpaceAdmin, TestContext.Current.CancellationToken),
            Watcher.Member => await InviteMemberAsync(projectId),
            _ => throw new ArgumentOutOfRangeException(nameof(watcher))
        };
        await using var authorSocket = await WatchAsync(author, projectId);
        await using var otherSocket = await WatchAsync(other, projectId);

        await FilesHttpClient.Authenticate(author);
        var file = await UploadAsync(projectId, "notes.txt");

        await authorSocket.WaitForAsync(ModifyFolder, CreatedFile(file.Id), cancellationToken: TestContext.Current.CancellationToken);
        (await otherSocket.ArrivesWithinAsync(ModifyFolder, CreatedFile(file.Id), _noDeliveryWindow, TestContext.Current.CancellationToken))
            .Should().BeFalse("a file inside a project is visible to its creator only");
    }

    [Fact]
    public async Task SharedUsers_ProjectFile_ListsNoAdminsOrOwner()
    {
        var projectId = await CreateProjectAsync();
        var author = await InviteMemberAsync(projectId);
        await InviteContact(EmployeeType.DocSpaceAdmin, TestContext.Current.CancellationToken);

        await FilesHttpClient.Authenticate(author);
        var file = await UploadAsync(projectId, "notes.txt");

        var users = (await SharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        users.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadToProject_IsNotMarkedNewForTheProjectOwner()
    {
        var projectId = await CreateProjectAsync();
        var author = await InviteMemberAsync(projectId);
        var room = (await RoomsApi.CreateRoomAsync(
            new CreateRoomRequestDto($"room-{Guid.NewGuid():N}", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;
        await RoomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest([new RoomInvitation(author.Id, FileShare.ContentCreator)], notify: false),
            TestContext.Current.CancellationToken);

        await FilesHttpClient.Authenticate(author);
        await UploadAsync(projectId, "notes.txt");
        var announced = await UploadAsync(room.Id, "announced.txt");
        await FilesHttpClient.Authenticate(Owner);

        var roomNews = await NewTitlesAsync(room.Id, titles => titles.Contains(announced.Title));
        var projectNews = await NewTitlesAsync(projectId, _ => true);

        roomNews.Should().Contain(announced.Title);
        projectNews.Should().BeEmpty();
    }

    private async Task<List<string>> NewTitlesAsync(int roomId, Func<List<string>, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var groups = (await RoomsApi.GetNewRoomItemsAsync(roomId, TestContext.Current.CancellationToken)).Response;
            List<string> titles = [.. (groups ?? []).SelectMany(g => g.Items ?? []).Select(e => e.Title)];

            if (until(titles) || DateTime.UtcNow >= deadline)
            {
                return titles;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    private async Task<PortalSocket> WatchAsync(User user, int folderId)
    {
        var socket = await ConnectSocket(user);

        await socket.SubscribeAsync(true, TestContext.Current.CancellationToken, $"DIR-{folderId}");

        return socket;
    }

    private static Func<JsonElement, bool> CreatedFile(int fileId)
    {
        return payload => payload.GetProperty("cmd").GetString() == "create"
                          && payload.GetProperty("type").GetString() == "file"
                          && payload.GetProperty("id") is { ValueKind: JsonValueKind.Number } id
                          && id.GetInt32() == fileId;
    }
}
