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

namespace ASC.AI.Core.Chat.Completion;

[Scope]
public class ChatCompletionRunner(
    ChatExecutionContextBuilder contextBuilder,
    AuthContext authContext,
    ChatHistory chatHistory,
    TenantManager tenantManager,
    ChatClientFactory chatClientFactory,
    ILogger<ChatCompletionGenerator> logger,
    AttachmentHandler attachmentHandler,
    DataContentLoader dataContentLoader,
    SocketManager socketManager,
    IServiceScopeFactory serviceScopeFactory)
{
    public async Task<ChatCompletionGenerator> StartNewChatAsync(
        int roomId, string message, IEnumerable<JsonElement>? files = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);

        var context = await contextBuilder.BuildAsync(roomId);
        context.ChatId = Guid.NewGuid();

        var attachments = await GetAttachmentsAsync(context, files).ToListAsync();

        var userMessage = FormatUserMessage(message, attachments);

        var messages = new List<ChatMessage>
        {
            BuildSystemMessage(context),
            userMessage
        };

        context.UserMessage = userMessage;
        context.RawMessage = message;

        if (attachments.Count > 0)
        {
            context.Attachments = attachments;
        }

        var client = chatClientFactory.Create(context.ClientOptions, authContext.CurrentAccount.ID, context.Tools);

        return new ChatCompletionGenerator(
            client,
            logger,
            socketManager,
            messages,
            chatHistory,
            context,
            serviceScopeFactory,
            attachmentHandler);
    }

    public async Task<ChatCompletionGenerator> StartChatAsync(
        Guid chatId, string message, IEnumerable<JsonElement>? files = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);

        var tenantId = tenantManager.GetCurrentTenantId();

        var chat = await chatHistory.GetChatAsync(tenantId, chatId);
        if (chat == null)
        {
            throw new ItemNotFoundException(ErrorMessages.ChatNotFound);
        }

        if (chat.UserId != authContext.CurrentAccount.ID)
        {
            throw new SecurityException(ErrorMessages.ChatAccessDenied);
        }

        var context = await contextBuilder.BuildAsync(chat.RoomId);
        context.Chat = chat;
        context.ChatId = chat.Id;

        var attachments = await GetAttachmentsAsync(context, files).ToListAsync();

        string? restoredSchemaContext = null;
        if (!context.Tools.ContainsSystemTool(SystemToolType.FormDataQuery) &&
            !context.Tools.ContainsSystemTool(SystemToolType.FormDataAggregate) &&
            !context.Tools.ContainsSystemTool(SystemToolType.FormDataSelfJoin))
        {
            restoredSchemaContext = await RestoreFormDataToolsAsync(context, chat.Id);
        }

        var systemMessage = BuildSystemMessage(context);

        var userMessage = FormatUserMessage(message, attachments, restoredSchemaContext);

        var historyAdapter = HistoryHelper.GetAdapter(context.ClientOptions.Provider, dataContentLoader);
        var messages = await chatHistory.GetMessagesAsync(chatId, historyAdapter, systemMessage, userMessage)
            .ToListAsync();

        context.UserMessage = userMessage;
        context.RawMessage = message;

        if (attachments.Count > 0)
        {
            context.Attachments = attachments;
        }

        var client = chatClientFactory.Create(context.ClientOptions, authContext.CurrentAccount.ID, context.Tools);

        return new ChatCompletionGenerator(
            client,
            logger,
            socketManager,
            messages,
            chatHistory,
            context,
            serviceScopeFactory,
            attachmentHandler);
    }

    private static ChatMessage BuildSystemMessage(ChatExecutionContext context) =>
        new(ChatRole.System, ChatPromptTemplate.GetPrompt(
            context.Instruction,
            context.ResultStorageId,
            context.Agent.Id,
            context.Agent.Title,
            context.User.FirstName,
            context.User.Email,
            context.Tools.ContainsSystemTool(SystemToolType.KnowledgeSearch),
            context.Tools.ContainsSystemTool(SystemToolType.WebSearch),
            context.Tools.ContainsSystemTool(SystemToolType.FormDataQuery) ||
            context.Tools.ContainsSystemTool(SystemToolType.FormDataAggregate) ||
            context.Tools.ContainsSystemTool(SystemToolType.FormDataSelfJoin)));

    private async Task<string?> RestoreFormDataToolsAsync(ChatExecutionContext context, Guid chatId)
    {
        var fileId = await chatHistory.GetLastFormFileIdAsync(chatId);
        if (fileId == null)
        {
            return null;
        }

        var result = await attachmentHandler.GetFormDataToolsAsync(fileId.Value);
        if (result == null)
        {
            return null;
        }

        foreach (var tool in result.Value.Tools)
        {
            var toolType = tool.Context.Name == AggregateFormDataTool.Name
                ? SystemToolType.FormDataAggregate
                : tool.Context.Name == SelfJoinFormDataTool.Name
                    ? SystemToolType.FormDataSelfJoin
                    : SystemToolType.FormDataQuery;

            if (!context.Tools.ContainsSystemTool(toolType))
            {
                context.Tools.AddTool(toolType, tool);
            }
        }

        return result.Value.SchemaContext;
    }

    private async IAsyncEnumerable<AttachmentMessageContent> GetAttachmentsAsync(
        ChatExecutionContext context,
        IEnumerable<JsonElement>? files)
    {
        if (files == null)
        {
            yield break;
        }

        var (ids, thirdPartyIds) = FileOperationsManager.GetIds(files);

        var failedEntries = new List<FileEntry>();

        await foreach (var result in attachmentHandler.HandleAsync(context, ids, thirdPartyIds))
        {
            if (!result.Success)
            {
                failedEntries.Add(result.File);
            }

            if (result.DynamicTools != null)
            {
                foreach (var dynamicTool in result.DynamicTools)
                {
                    var toolType = dynamicTool.Context.Name == AggregateFormDataTool.Name
                        ? SystemToolType.FormDataAggregate
                        : dynamicTool.Context.Name == SelfJoinFormDataTool.Name
                            ? SystemToolType.FormDataSelfJoin
                            : SystemToolType.FormDataQuery;

                    if (!context.Tools.ContainsSystemTool(toolType))
                    {
                        context.Tools.AddTool(toolType, dynamicTool);
                    }
                }
            }

            if (result.Content != null)
            {
                yield return result.Content;
            }
        }

        if (failedEntries.Count <= 0)
        {
            yield break;
        }

        var names = string.Join(", ", failedEntries.Select(x => x.Title));
        throw new ArgumentException(string.Format(ErrorMessages.AttachmentProcessFailed, names));
    }

    private static ChatMessage FormatUserMessage(string message, List<AttachmentMessageContent> attachments, string? schemaContext = null)
    {
        if (attachments.Count == 0 && schemaContext == null)
        {
            return new ChatMessage(ChatRole.User, message);
        }

        var contents = new List<AIContent>(attachments.Count + 2);
        contents.AddRange(attachments.Select(attachment => attachment.ToAiContent()));

        if (schemaContext != null)
        {
            contents.Add(new TextContent($"[ACTIVE DATASET — use ONLY these column names for tool calls]{schemaContext}"));
        }

        contents.Add(new TextContent($"##User query: {message}"));

        return new ChatMessage { Role = ChatRole.User, Contents = contents };
    }
}
