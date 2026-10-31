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
/// Helpers for the AI rooms section: Instructions rooms, AI projects, project sources and skills. The pinned SDK knows
/// neither the Instructions and project room types nor the sources and skills routes, and it cannot read a room of
/// those types back, so rooms of the section are created, read and changed over raw HTTP; deleting a room, archiving
/// it and the file operations go through the SDK.
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

    protected async Task<List<int>> GetInstructionsSourceIdsAsync(int projectId)
    {
        var sources = await GetSourcesAsync(projectId);

        return sources.GetProperty("instructionsRooms").EnumerateArray().Select(r => r.GetProperty("id").GetInt32()).ToList();
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
