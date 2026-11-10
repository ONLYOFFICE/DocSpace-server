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

/// <summary>
/// Helpers for chats inside an AI project — what the removed AI agent was in the TypeScript suite: a place with one
/// fixed model that the caller cannot override. The model of a project is its own (<c>PUT files/rooms/{id}/model</c>),
/// else the portal's Chat binding; every round and every thread that names the project as its <c>entityId</c> runs on
/// it, whatever <c>profileId</c> the composer sent (<c>applyAiProjectChatContext</c>).
/// </summary>
/// <remarks>
/// Unlike the agent, a project holds no instructions (the Files API refuses chat settings on it) and has no Chat
/// outputs folder. Its members are the owner, Project managers (<c>RoomManager</c>, RoomAdmin and above) and members
/// invited at <c>Read</c>; those three may chat, anyone else is refused.
/// </remarks>
public abstract class ChatProjectTestBase(AspireAppFixture fixture) : ChatPart3TestBase(fixture)
{
    private DocSpace.API.SDK.Api.Rooms.AIApi ProjectsApi => RoomsAiApi;

    /// <summary>Creates a project as the Owner; returns its room id.</summary>
    protected async Task<int> CreateProjectAsync()
    {
        await FilesHttpClient.Authenticate(Owner);

        return await AgentsTests.AiProjects.CreateAsync(RoomsApi);
    }

    /// <summary>Creates a project as the Owner and selects <paramref name="profile"/> as its model.</summary>
    protected async Task<int> CreateProjectOnAsync(AiProfile profile)
    {
        var projectId = await CreateProjectAsync();
        await SetProjectModelAsync(projectId, profile);

        return projectId;
    }

    /// <summary>Selects the model of a project, as the Owner.</summary>
    protected async Task SetProjectModelAsync(int projectId, AiProfile profile)
    {
        await FilesHttpClient.Authenticate(Owner);

        await AgentsTests.AiProjects.SetModelAsync(ProjectsApi, projectId, profile.Id);
    }

    /// <summary>The model a project is bound to, read as <paramref name="reader"/> (the Owner by default).</summary>
    protected async Task<string?> ProjectModelAsync(int projectId, User? reader = null)
    {
        await FilesHttpClient.Authenticate(reader ?? Owner);

        return await AgentsTests.AiProjects.GetModelAsync(ProjectsApi, projectId);
    }

    /// <summary>Invites a member of the given type into a project at <c>Read</c>, the level that may chat there.</summary>
    protected async Task<User> InviteToProjectAsync(int projectId, EmployeeType type)
    {
        var member = await InviteContact(type, TestContext.Current.CancellationToken);
        await ShareRoomAsync(projectId, member, FileShare.Read);

        return member;
    }

    /// <summary>
    /// Creates a thread in a project as whoever the public AI client is signed in as. Without a profile the request
    /// leaves <c>profileId</c> out, the way a composer with a hidden picker does.
    /// </summary>
    protected Task<AiThread> CreateProjectThreadAsync(int projectId, string? profileId = null) =>
        _aiThreads.AiThreadsCreateAsync(
            new AiThreadsCreateRequest(title: $"project-thread-{Guid.NewGuid():N}", profileId: profileId!, entityId: projectId.ToString()),
            TestContext.Current.CancellationToken);

    /// <summary>The profile a thread is stored with, null when it has none yet.</summary>
    protected async Task<string?> StoredModelOfAsync(string threadId) =>
        (await _aiThreads.AiThreadsGetByIdAsync(threadId, TestContext.Current.CancellationToken)).ProfileId;

    /// <summary>A model profile that can chat, on a provider of its own; the provider is disposed with the test.</summary>
    protected async Task<(StubModelProvider Provider, AiProfile Profile)> NewChatModelAsync()
    {
        var provider = NewProvider();
        var profile = await CreateCapableProfileAsync(provider, ChatCapabilities);

        return (provider, profile);
    }

    /// <summary>A unique question, so the requests a provider saw can be told apart by it.</summary>
    protected static string NewQuestion() => $"project-question-{Guid.NewGuid():N}";
}
