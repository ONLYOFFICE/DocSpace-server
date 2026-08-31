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

namespace ASC.AI.Service;

/// <summary>
/// Assembles the full read-only context of a chat round in one pass: the
/// access checks run once per scope here, and the per-entity services are
/// entered through their <c>*VerifiedAsync</c> internals. An inaccessible
/// folder or a foreign thread omits its section instead of failing the
/// whole response — the caller falls back to the individual endpoint and
/// gets that endpoint's error semantics.
/// </summary>
[Scope]
public class ChatContextService(
    UserManager userManager,
    AuthContext authContext,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    AiGateway gateway,
    AiSettingsService aiSettingsService,
    ProfileStorageService profileStorageService,
    AssignmentsStorageService assignmentsStorageService,
    PreferencesStorageService preferencesStorageService,
    ToolPrefsStorageService toolPrefsStorageService,
    McpServerStorageService mcpServerStorageService,
    WebSearchStorageService webSearchStorageService,
    ThreadStorageService threadStorageService,
    MessageStorageService messageStorageService) : IntegrationServiceBase(userManager, authContext, daoFactory, fileSecurity, gateway)
{
    private static readonly EmployeeType[] _readTypes = [EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User];

    public async Task<ChatContextDto> ReadAsync(Guid? threadId, string? entityId, string? contextEntityId, bool includeMessages)
    {
        await AssertUserHasAccessAsync(_readTypes);

        var configTask = ReadConfigAsync();
        var profilesTask = ReadProfilesAsync();
        var globalTask = BuildScopeAsync(null, null);
        var entityTask = TryBuildScopeAsync(entityId);
        var contextEntityTask = contextEntityId != entityId
            ? TryBuildScopeAsync(contextEntityId)
            : Task.FromResult<ChatContextScopeDto?>(null);
        var threadTask = ReadThreadAsync(threadId, includeMessages);
        var webSearchTask = webSearchStorageService.ReadVerifiedAsync();

        await Task.WhenAll(configTask, profilesTask, globalTask, entityTask, contextEntityTask, threadTask, webSearchTask);

        var webSearch = await webSearchTask;

        var (thread, messages) = await threadTask;

        return new ChatContextDto
        {
            Config = await configTask,
            Profiles = await profilesTask,
            Global = await globalTask,
            Entity = await entityTask,
            ContextEntity = await contextEntityTask,
            Thread = thread,
            Messages = messages,
            WebSearch = webSearch is null ? null : WebSearchConfigMapper.MapToDto(webSearch)
        };
    }

    private async Task<AiSettingsDto> ReadConfigAsync()
    {
        return (await aiSettingsService.GetAiSettingsAsync()).MapToDto();
    }

    private async Task<List<ProfileDto>> ReadProfilesAsync()
    {
        return (await profileStorageService.ReadAllVerifiedAsync()).Select(ProfileMapper.MapToDto).ToList();
    }

    private async Task<(ThreadDto? Thread, List<MessageDto>? Messages)> ReadThreadAsync(Guid? threadId, bool includeMessages)
    {
        if (!threadId.HasValue)
        {
            return (null, null);
        }

        var owned = await threadStorageService.ReadByIdVerifiedAsync(threadId.Value);
        if (owned is null)
        {
            return (null, null);
        }

        var thread = ThreadMapper.MapToDto(owned);
        if (!includeMessages)
        {
            return (thread, null);
        }

        var list = await messageStorageService.ReadByThreadVerifiedAsync(owned.Id);
        return (thread, list.Select(MessageMapper.MapToDto).ToList());
    }

    private async Task<ChatContextScopeDto?> TryBuildScopeAsync(string? entityId)
    {
        if (string.IsNullOrEmpty(entityId))
        {
            return null;
        }

        Folder<int>? folder;
        try
        {
            folder = await AssertUserHasAccessToFolderAsync(_readTypes, entityId);
        }
        catch (Exception e) when (e is ItemNotFoundException or SecurityException or ArgumentException)
        {
            return null;
        }

        return folder is null ? null : await BuildScopeAsync(entityId, folder);
    }

    private async Task<ChatContextScopeDto> BuildScopeAsync(string? entityId, Folder<int>? folder)
    {
        var entryId = folder?.Id;

        var assignmentsTask = assignmentsStorageService.ReadAllVerifiedAsync(entryId);
        var preferencesTask = preferencesStorageService.ReadVerifiedAsync(entryId);
        var toolPrefsTask = toolPrefsStorageService.ReadVerifiedAsync(entryId);
        var mcpServersTask = mcpServerStorageService.ReadAllVerifiedAsync(entryId);
        var folderTask = folder is null
            ? Task.FromResult<ChatContextFolderDto?>(null)
            : BuildFolderAsync(folder);

        await Task.WhenAll(assignmentsTask, preferencesTask, toolPrefsTask, mcpServersTask, folderTask);

        var preferences = await preferencesTask;

        return new ChatContextScopeDto
        {
            EntityId = entityId,
            Folder = await folderTask,
            Assignments = (await assignmentsTask).ToDictionary(x => x.Key.ToStringFast(), x => x.Value),
            Preferences = preferences is null ? null : PreferencesMapper.MapToDto(preferences),
            ToolPrefs = await toolPrefsTask,
            McpServers = (await mcpServersTask).Select(McpServerMapper.MapToDto).ToList()
        };
    }

    private async Task<ChatContextFolderDto?> BuildFolderAsync(Folder<int> folder)
    {
        var canCreateTask = FileSecurity.CanCreateAsync(folder);

        string? prompt = null;
        if (folder.IsAgent)
        {
            var chatSettings = folder.ChatSettings ?? await DaoFactory.GetFolderDao<int>().GetChatSettingsAsync(folder.Id);
            prompt = chatSettings?.Prompt;
        }

        return new ChatContextFolderDto
        {
            Id = folder.Id,
            Title = folder.Title,
            FolderType = (int)folder.FolderType,
            IsAgent = folder.IsAgent,
            Prompt = prompt,
            CanCreate = await canCreateTask
        };
    }
}
