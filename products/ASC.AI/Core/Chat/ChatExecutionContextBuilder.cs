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

namespace ASC.AI.Core.Chat;

[Scope]
public class ChatExecutionContextBuilder(
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    ChatDao chatDao,
    TenantManager tenantManager,
    AuthContext authContext,
    AiProviderService providerService,
    ChatTools chatTools,
    UserManager userManager)
{
    public async Task<ChatExecutionContext> BuildAsync(int roomId)
    {
        var folderDao = daoFactory.GetFolderDao<int>();

        var agent = await folderDao.GetFolderAsync(roomId);
        if (agent == null)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (!await fileSecurity.CanUseChatAsync(agent))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_ReadFolder);
        }

        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;

        var agentSettings = await folderDao.GetChatSettingsAsync(agent.Id);
        if (agentSettings == null)
        {
            throw new ItemNotFoundException();
        }

        var providerContextTask = providerService.GetProviderContextAsync(agentSettings.ProviderId, agentSettings.ModelId);
        var resultStorageTask = folderDao.GetFoldersAsync(agent.Id, FolderType.ResultStorage).FirstAsync().AsTask();
        var knowledgeTask = folderDao.GetFoldersAsync(agent.Id, FolderType.Knowledge).FirstAsync().AsTask();
        var chatSettingsTask = chatDao.GetUserChatSettingsAsync(tenantId, roomId, userId);
        var userTask = userManager.GetUsersAsync(userId);

        await Task.WhenAll(providerContextTask, resultStorageTask, knowledgeTask, chatSettingsTask, userTask);

        var (provider, mSettings) = await providerContextTask;
        var chatSettings = await chatSettingsTask;
        var knowledge = await knowledgeTask;
        var resultStorage = await resultStorageTask;
        var user = await userTask;

        if (!mSettings.IsEnabled)
        {
            throw new ArgumentException(ErrorMessages.ModelDisabled);
        }

        Dictionary<string, string>? metadata = null;
        if (provider.Type is ProviderType.PortalAi)
        {
            metadata = new Dictionary<string, string>
            {
                { "agent_title", agent.Title },
                { "agent_id", agent.Id.ToString() }
            };
        }

        ToolHolder tools;
        string? error = null;

        if (mSettings.Capabilities.ToolCalling)
        {
            (tools, error) = await chatTools.GetAsync(
                agent,
                chatSettings,
                knowledge.FilesCount > 0,
                resultStorage.Id,
                metadata);
        }
        else
        {
            tools = new ToolHolder();
        }

        ChatReasoningEffort? reasoningEffort = mSettings.Capabilities.Thinking
            ? chatSettings.ReasoningEffort
            : null;

        var context = new ChatExecutionContext
        {
            TenantId = tenantId,
            User = user,
            Agent = agent,
            ClientOptions = new ChatClientOptions
            {
                Provider = provider.Type,
                ProviderId = provider.Id,
                HasModelSettings = provider.HasModelSettings,
                Endpoint = provider.Url,
                Key = provider.Key,
                ModelId = mSettings.Id,
                ReasoningEffort = reasoningEffort,
                Metadata = metadata
            },
            Instruction = agentSettings.Prompt,
            ResultStorageId = resultStorage.Id,
            ChatSettings = chatSettings,
            Tools = tools,
            Error = error,
            ModelSettings = mSettings
        };

        return context;
    }
}

public class ChatExecutionContext : IAsyncDisposable
{
    public int TenantId { get; init; }
    public required UserInfo User { get; init; }
    public required Folder<int> Agent { get; init; }
    public required ChatClientOptions ClientOptions { get; init; }
    public string? Instruction { get; init; }
    public int ResultStorageId { get; init; }
    public required UserChatSettings ChatSettings { get; init; }
    public required ToolHolder Tools { get; init; }
    public ChatMessage? UserMessage { get; set; }
    public string RawMessage { get; set; } = string.Empty;
    public List<AttachmentMessageContent> Attachments { get; set; } = [];
    public Guid ChatId { get; set; }
    public ChatSession? Chat { get; set; }
    public string? Error { get; init; }
    public required ModelSettings ModelSettings { get; init; }

    public async ValueTask DisposeAsync()
    {
        await Tools.DisposeAsync();
    }
}
