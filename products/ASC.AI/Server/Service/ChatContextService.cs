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

using McpServer = ASC.AI.Integration.McpServers.McpServer;
using Preferences = ASC.AI.Integration.Preferences.Preferences;

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
    private readonly AiGateway _gateway = gateway;

    private static readonly EmployeeType[] _readTypes = [EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User];

    public async Task<ChatContextDto> ReadAsync(Guid? threadId, string? entityId, string? contextEntityId, bool includeMessages)
    {
        await AssertUserHasAccessAsync(_readTypes);

        var modelsTask = ReadModelsAsync();
        var configTask = ReadConfigAsync();
        var profilesTask = ReadProfilesAsync(modelsTask);
        var threadTask = ReadThreadAsync(threadId, includeMessages);
        var webSearchTask = webSearchStorageService.ReadVerifiedAsync();
        var entityFolderTask = TryReadFolderAsync(entityId);
        var contextFolderTask = contextEntityId != entityId
            ? TryReadFolderAsync(contextEntityId)
            : Task.FromResult<Folder<int>?>(null);

        var entityFolder = await entityFolderTask;
        var contextFolder = await contextFolderTask;
        var models = await modelsTask;

        var assignmentsTask = assignmentsStorageService.ReadByScopesVerifiedAsync(entityFolder?.Id, contextFolder?.Id, models);
        var preferencesTask = preferencesStorageService.ReadByScopesVerifiedAsync(entityFolder?.Id, contextFolder?.Id);
        var toolPrefsTask = toolPrefsStorageService.ReadByScopesVerifiedAsync(entityFolder?.Id, contextFolder?.Id);
        var mcpServersTask = mcpServerStorageService.ReadByScopesVerifiedAsync(entityFolder?.Id, contextFolder?.Id);
        var entityFolderDtoTask = BuildFolderAsync(entityFolder);
        var contextFolderDtoTask = BuildFolderAsync(contextFolder);

        await Task.WhenAll(configTask, profilesTask, threadTask, webSearchTask, assignmentsTask, preferencesTask, toolPrefsTask, mcpServersTask, entityFolderDtoTask, contextFolderDtoTask);

        var scopes = new ScopeSources(await assignmentsTask, await preferencesTask, await toolPrefsTask, await mcpServersTask);
        var webSearch = await webSearchTask;
        var (thread, messages) = await threadTask;

        return new ChatContextDto
        {
            Config = await configTask,
            Profiles = await profilesTask,
            Global = scopes.Build(null, null, null),
            Entity = entityFolder is null ? null : scopes.Build(entityId, entityFolder.Id, await entityFolderDtoTask),
            ContextEntity = contextFolder is null ? null : scopes.Build(contextEntityId, contextFolder.Id, await contextFolderDtoTask),
            Thread = thread,
            Messages = messages,
            WebSearch = webSearch is null ? null : WebSearchConfigMapper.MapToDto(webSearch)
        };
    }

    private async Task<List<Model>?> ReadModelsAsync()
    {
        if (!_gateway.Configured)
        {
            return null;
        }

        var response = await _gateway.GetModelsAsync();

        return [.. response.Data];
    }

    private async Task<AiSettingsDto> ReadConfigAsync()
    {
        return (await aiSettingsService.GetAiSettingsAsync()).MapToDto();
    }

    private async Task<List<ProfileDto>> ReadProfilesAsync(Task<List<Model>?> modelsTask)
    {
        return (await profileStorageService.ReadAllVerifiedAsync(await modelsTask)).Select(ProfileMapper.MapToDto).ToList();
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

    private async Task<Folder<int>?> TryReadFolderAsync(string? entityId)
    {
        if (string.IsNullOrEmpty(entityId))
        {
            return null;
        }

        try
        {
            return await AssertUserHasAccessToFolderAsync(_readTypes, entityId);
        }
        catch (Exception e) when (e is ItemNotFoundException or SecurityException or ArgumentException)
        {
            return null;
        }
    }

    private async Task<ChatContextFolderDto?> BuildFolderAsync(Folder<int>? folder)
    {
        if (folder is null)
        {
            return null;
        }

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

    private sealed record ScopeSources(
        ScopedValues<Dictionary<ActionType, Guid>> Assignments,
        ScopedValues<Preferences?> Preferences,
        ScopedValues<Dictionary<string, ToolPreference>> ToolPrefs,
        ScopedValues<List<McpServer>> McpServers)
    {
        public ChatContextScopeDto Build(string? entityId, int? entryId, ChatContextFolderDto? folder)
        {
            var preferences = Pick(Preferences, entryId);

            return new ChatContextScopeDto
            {
                EntityId = entityId,
                Folder = folder,
                Assignments = Pick(Assignments, entryId).ToDictionary(x => x.Key.ToStringFast(), x => x.Value),
                Preferences = preferences is null ? null : PreferencesMapper.MapToDto(preferences),
                ToolPrefs = Pick(ToolPrefs, entryId),
                McpServers = [.. Pick(McpServers, entryId).Select(McpServerMapper.MapToDto)]
            };
        }

        private static T Pick<T>(ScopedValues<T> values, int? entryId)
        {
            return entryId.HasValue ? values.ByEntry[entryId.Value] : values.Global;
        }
    }
}
