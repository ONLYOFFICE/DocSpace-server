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

namespace ASC.Files.Tests.Tests._03_Rooms.AiHub;

/// <summary>
/// Helpers for the AI rooms section: Instructions rooms, AI projects, project sources and skills. The helpers that
/// answer a <see cref="JsonElement"/> or an <see cref="HttpResponseMessage"/> create, read and change the rooms of
/// the section over raw HTTP: they were written when the pinned SDK knew neither the room types of the section nor
/// its sources and skills routes. The helpers for the projects of a room (<c>CreateSectionRoomAsync</c>,
/// <c>CreateRoomWithProjectsAsync</c>, <c>LinkAsync</c>, <c>UnlinkAsync</c>, <c>GetRoomInfoAsync</c>,
/// <c>GetAiHubFoldersAsync</c>, <c>GetRoomProjectsAsync</c>), deleting a room, archiving it and the file operations
/// go through the SDK.
/// </summary>
public abstract class AiHubTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const int InstructionsRoomType = 10;
    protected const int AiProjectRoomType = 12;
    protected const int AiHubFolderType = 41;

    protected RawApiClient Files => new(_filesClient);

    protected async Task<JsonElement> CreateHubRoomAsync(int roomType, string title)
    {
        using var response = await CreateHubRoomResponseAsync(new { title, roomType });

        return await Files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);
    }

    protected Task<HttpResponseMessage> CreateHubRoomResponseAsync(object body)
    {
        return Files.PostAsync("api/2.0/files/rooms", body, TestContext.Current.CancellationToken);
    }

    protected async Task<int> CreateInstructionsRoomAsync(string? title = null)
    {
        return (await CreateHubRoomAsync(InstructionsRoomType, title ?? $"instructions-{Guid.NewGuid():N}")).GetProperty("id").GetInt32();
    }

    protected async Task<int> CreateProjectAsync(string? title = null)
    {
        return (await CreateHubRoomAsync(AiProjectRoomType, title ?? $"project-{Guid.NewGuid():N}")).GetProperty("id").GetInt32();
    }

    protected Task<HttpResponseMessage> GetRoomResponseAsync(int roomId)
    {
        return Files.GetAsync($"api/2.0/files/rooms/{roomId}", TestContext.Current.CancellationToken);
    }

    protected Task<HttpResponseMessage> UpdateRoomResponseAsync(int roomId, object body)
    {
        return Files.PutAsync($"api/2.0/files/rooms/{roomId}", body, TestContext.Current.CancellationToken);
    }

    protected async Task<JsonElement> GetAiHubSectionAsync(string query = "")
    {
        using var response = await Files.GetAsync($"api/2.0/files/rooms?searchArea=AiHub{query}", TestContext.Current.CancellationToken);

        return await Files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<JsonElement> GetRootSectionsAsync(string query = "")
    {
        using var response = await Files.GetAsync($"api/2.0/files/@root{query}", TestContext.Current.CancellationToken);

        return await Files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);
    }

    protected static JsonElement AiHubSectionOf(JsonElement sections)
    {
        return sections.EnumerateArray().Single(s => s.GetProperty("current").GetProperty("rootFolderType").GetInt32() == AiHubFolderType);
    }

    protected async Task<JsonElement> GetRootAiHubSectionAsync(string query = "")
    {
        return AiHubSectionOf(await GetRootSectionsAsync(query));
    }

    protected Task<HttpResponseMessage> PinRoomResponseAsync(int roomId)
    {
        return Files.PutAsync($"api/2.0/files/rooms/{roomId}/pin", null, TestContext.Current.CancellationToken);
    }

    protected Task<HttpResponseMessage> UnpinRoomResponseAsync(int roomId)
    {
        return Files.PutAsync($"api/2.0/files/rooms/{roomId}/unpin", null, TestContext.Current.CancellationToken);
    }

    protected async Task PinRoomAsync(int roomId)
    {
        using var response = await PinRoomResponseAsync(roomId);

        response.StatusCode.Should().Be(HttpStatusCode.OK, $"room {roomId} has to be pinned in Arrange");
    }

    protected static List<int> IdsOf(JsonElement section)
    {
        return section.GetProperty("folders").EnumerateArray().Select(f => f.GetProperty("id").GetInt32()).ToList();
    }

    protected async Task DeleteRoomAndWaitAsync(int roomId)
    {
        await _roomsApi.DeleteRoomAsync(roomId, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();
    }

    protected Task<HttpResponseMessage> LinkSourcesAsync(int projectId, params int[] roomIds)
    {
        return Files.PostAsync($"api/2.0/files/rooms/{projectId}/sources", new { roomIds }, TestContext.Current.CancellationToken);
    }

    protected Task<HttpResponseMessage> UnlinkSourcesAsync(int projectId, params int[] roomIds)
    {
        return Files.DeleteAsync($"api/2.0/files/rooms/{projectId}/sources", new { roomIds }, TestContext.Current.CancellationToken);
    }

    protected Task<HttpResponseMessage> GetSourcesResponseAsync(int projectId)
    {
        return Files.GetAsync($"api/2.0/files/rooms/{projectId}/sources", TestContext.Current.CancellationToken);
    }

    protected async Task<JsonElement> GetSourcesAsync(int projectId)
    {
        using var response = await GetSourcesResponseAsync(projectId);

        return await Files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);
    }

    protected Task<FolderDto> CreateSectionRoomAsync(RoomType roomType, string? title = null)
    {
        return CreateRoom(new CreateRoomRequestDto(title ?? $"Autotest {roomType} {Guid.NewGuid():N}", roomType: roomType));
    }

    protected Task LinkAsync(int projectId, params int[] roomIds)
    {
        return _roomsAiApi.LinkProjectSourcesAsync(projectId, new LinkAiProjectSourcesRequest([.. roomIds]), TestContext.Current.CancellationToken);
    }

    protected Task UnlinkAsync(int projectId, params int[] roomIds)
    {
        return _roomsAiApi.UnlinkProjectSourcesAsync(projectId, new UnlinkAiProjectSourcesRequest([.. roomIds]), TestContext.Current.CancellationToken);
    }

    protected async Task<(int RoomId, List<int> ProjectIds)> CreateRoomWithProjectsAsync(params string[] projectTitles)
    {
        var roomId = (await CreateSectionRoomAsync(RoomType.InstructionsRoom)).Id;
        var projectIds = new List<int>();

        foreach (var title in projectTitles)
        {
            var projectId = (await CreateSectionRoomAsync(RoomType.AiProjectRoom, title)).Id;
            await LinkAsync(projectId, roomId);
            projectIds.Add(projectId);
        }

        return (roomId, projectIds);
    }

    protected async Task<FolderDto> GetRoomInfoAsync(int roomId)
    {
        return (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
    }

    protected async Task<List<FolderDto>> GetAiHubFoldersAsync()
    {
        var section = await _roomsApi.GetRoomsFolderAsync(searchArea: SearchArea.AiHub, cancellationToken: TestContext.Current.CancellationToken);

        return [.. section.Response.Folders.OfType<FolderDto>()];
    }

    protected Task<AiRoomProjectArrayWrapper> GetRoomProjectsAsync(int roomId, int? count = null, int? startIndex = null)
    {
        return _roomsAiApi.GetRoomProjectsAsync(roomId, count, startIndex, TestContext.Current.CancellationToken);
    }

    protected static List<int> IdsOf(AiRoomProjectArrayWrapper page)
    {
        return [.. page.Response.Select(p => p.Id)];
    }

    protected async Task<List<int>> GetInstructionsSourceIdsAsync(int projectId)
    {
        var sources = await GetSourcesAsync(projectId);

        return sources.GetProperty("instructionsRooms").EnumerateArray().Select(r => r.GetProperty("id").GetInt32()).ToList();
    }

    protected async Task ShareRoomAsync(int roomId, Guid subjectId, FileShare access)
    {
        await _roomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest
            {
                Invitations = [new RoomInvitation { Id = subjectId, Access = access }],
                Notify = false
            },
            cancellationToken: TestContext.Current.CancellationToken);
    }

    protected async Task<Guid> CreateGroupAsync(params User[] members)
    {
        var request = new GroupRequestDto(members: [.. members.Select(m => m.Id)], groupManager: Owner.Id, groupName: Guid.NewGuid().ToString("N")[..10]);

        return (await _groupApi.AddGroupAsync(request, TestContext.Current.CancellationToken)).Response.Id;
    }

    protected Task<HttpResponseMessage> GetFolderContentResponseAsync(int folderId)
    {
        return Files.GetAsync($"api/2.0/files/{folderId}", TestContext.Current.CancellationToken);
    }

    protected Task<HttpResponseMessage> GetFileResponseAsync(int fileId)
    {
        return Files.GetAsync($"api/2.0/files/file/{fileId}", TestContext.Current.CancellationToken);
    }

    protected async Task<int> UploadSkillAsync(int folderId, string name, string description = "Reviews documents", string? fileName = null)
    {
        var content = Encoding.UTF8.GetBytes($"---\nname: {name}\ndescription: {description}\n---\n# {name}\n");
        var file = new FileParameter(fileName ?? $"{name}.md", "text/markdown", new MemoryStream(content));
        var uploaded = await _foldersApi.UploadFileAsync(folderId, file: file, cancellationToken: TestContext.Current.CancellationToken);

        return uploaded.Response.Single().Id;
    }

    protected Task<HttpResponseMessage> GetRoomSkillsResponseAsync(int roomId)
    {
        return Files.GetAsync($"api/2.0/files/rooms/{roomId}/skills", TestContext.Current.CancellationToken);
    }

    protected async Task<SkillsResult> GetRoomSkillsAsync(int roomId)
    {
        using var response = await GetRoomSkillsResponseAsync(roomId);

        return await Files.ReadAsync<SkillsResult>(response, TestContext.Current.CancellationToken);
    }

    protected Task<HttpResponseMessage> GetRoomsSkillsResponseAsync(params int[] roomIds)
    {
        var query = string.Join("&", roomIds.Select(id => $"roomIds={id}"));

        return Files.GetAsync($"api/2.0/files/rooms/skills?{query}", TestContext.Current.CancellationToken);
    }

    protected async Task<SkillsResult> GetRoomsSkillsAsync(params int[] roomIds)
    {
        using var response = await GetRoomsSkillsResponseAsync(roomIds);

        return await Files.ReadAsync<SkillsResult>(response, TestContext.Current.CancellationToken);
    }

    protected async Task MoveFileAsync(int destFolderId, int fileId)
    {
        await _filesOperationsApi.MoveBatchItemsAsync(BatchRequest(destFolderId, fileId), TestContext.Current.CancellationToken);
        await WaitLongOperation();
    }

    protected async Task CopyFileAsync(int destFolderId, int fileId)
    {
        await _filesOperationsApi.CopyBatchItemsAsync(BatchRequest(destFolderId, fileId), TestContext.Current.CancellationToken);
        await WaitLongOperation();
    }

    protected async Task DeleteFileAsync(int fileId, bool immediately = false)
    {
        var request = new DeleteBatchRequestDto
        {
            FolderIds = [],
            FileIds = [new(fileId)],
            Immediately = immediately,
            ReturnSingleOperation = true
        };

        await _filesOperationsApi.DeleteBatchItemsAsync(request, TestContext.Current.CancellationToken);
        await WaitLongOperation();
    }

    private static BatchRequestDto BatchRequest(int destFolderId, int fileId)
    {
        return new BatchRequestDto
        {
            DestFolderId = new(destFolderId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(fileId)],
            FolderIds = [],
            ReturnSingleOperation = true
        };
    }

    protected sealed record SkillsResult(List<SkillsRoom> Rooms);

    protected sealed record SkillsRoom(int Id, string Title, List<Skill> Skills);

    protected sealed record Skill(string Id, string Name, string Description);
}
