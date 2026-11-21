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

namespace ASC.AI.Tests.Tests.ChatTests;

[Trait("Category", "Threads")]
[Trait("Feature", "AI/Chat")]
public class ChatProjectThreadListTests(AspireAppFixture fixture) : ChatProjectTestBase(fixture)
{
    [Fact]
    public async Task List_ThreadStartedInAProject_IsListedForThatProjectOnly()
    {
        var (_, model) = await NewChatModelAsync();
        var projectId = await CreateProjectOnAsync(model);
        var otherProjectId = await CreateProjectOnAsync(model);
        var roomId = await CreateRoomAsync();

        var inProject = await CreateProjectThreadAsync(projectId);

        var forProject = await ListThreadIdsAsync(projectId.ToString());
        var forOtherProject = await ListThreadIdsAsync(otherProjectId.ToString());
        var forRoom = await ListThreadIdsAsync(roomId.ToString());
        var forNoEntity = await ListThreadIdsAsync(null);

        forProject.Should().Equal(inProject.ThreadId);
        forOtherProject.Should().NotContain(inProject.ThreadId);
        forRoom.Should().NotContain(inProject.ThreadId);
        forNoEntity.Should().NotContain(inProject.ThreadId);
    }

    [Fact]
    public async Task List_ThreadStartedInARoom_IsNotListedForAProject()
    {
        var (_, model) = await NewChatModelAsync();
        var projectId = await CreateProjectOnAsync(model);
        var roomId = await CreateRoomAsync();

        var inRoom = await CreateThreadAsync(profileId: model.Id, entityId: roomId.ToString());
        var inProject = await CreateProjectThreadAsync(projectId);

        var forProject = await ListThreadIdsAsync(projectId.ToString());
        var forNoEntity = await ListThreadIdsAsync(null);

        forProject.Should().Equal(inProject.ThreadId);
        forNoEntity.Should().Contain(inRoom.ThreadId);
    }

    private async Task<int> CreateRoomAsync()
    {
        await FilesHttpClient.Authenticate(Owner);

        return (await RoomsApi.CreateRoomAsync(
            new CreateRoomRequestDto($"room-{Guid.NewGuid():N}", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response.Id;
    }

    private async Task<List<string>> ListThreadIdsAsync(string? entityId)
    {
        var threads = await _aiThreads.AiThreadsListAsync(entityId!, cancellationToken: TestContext.Current.CancellationToken);

        return [.. threads.Select(t => t.ThreadId)];
    }
}
