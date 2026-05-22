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

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.AI.Core.Chat.Completion;

public class ChatCompletionGenerator(
    IChatClient client,
    ILogger<ChatCompletionGenerator> logger,
    SocketManager socketManager,
    List<ChatMessage> messages,
    ChatHistory chatHistory,
    ChatExecutionContext context,
    IServiceScopeFactory serviceScopeFactory,
    AttachmentHandler attachmentHandler)
{
    public async IAsyncEnumerable<ChatCompletion> GenerateCompletionAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var responses = new List<ChatResponseUpdate>();
        var enumerator = client.GetStreamingResponseAsync(messages, cancellationToken: cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        var started = false;

        while (true)
        {
            ChatResponseUpdate response;
            var errorCaptured = false;

            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    break;
                }

                response = enumerator.Current;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                logger.ErrorWithException(e);
                response = new ChatResponseUpdate(ChatRole.Assistant, [new ErrorContent(e.Message)]);
                errorCaptured = true;
            }

            if (!errorCaptured)
            {
                if (!started)
                {
                    if (context.Chat == null)
                    {
                        var tempTitle = ChatNameGenerator.Generate(context.RawMessage);

                        context.Chat = await chatHistory.AddChatAsync(
                            context.TenantId,
                            context.Agent.Id,
                            context.User.Id,
                            context.ChatId,
                            tempTitle,
                            context.RawMessage,
                            context.Attachments);

                        _ = Task.Run(async () =>
                            {
                                var tenantId = context.TenantId;
                                var chatId = context.Chat.Id;
                                var userId = context.User.Id;
                                var userMessage = context.RawMessage;
                                var options = context.ClientOptions;
                                var agent = context.Agent;

                                await using var scope = serviceScopeFactory.CreateAsyncScope();
                                var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
                                await tenantManager.SetCurrentTenantAsync(tenantId);

                                var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
                                await securityContext.AuthenticateMeWithoutCookieAsync(userId);

                                var generator = scope.ServiceProvider.GetRequiredService<ChatNameGenerator>();
                                var history = scope.ServiceProvider.GetRequiredService<ChatHistory>();
                                var chatSocketManager = scope.ServiceProvider.GetRequiredService<SocketManager>();

                                var title = await generator.GenerateAsync(userMessage, options);
                                if (!string.IsNullOrEmpty(title))
                                {
                                    await history.UpdateChatTitleAsync(tenantId, chatId, title);
                                    await chatSocketManager.UpdateChatAsync(agent, chatId, title, userId);
                                }
                            },
                            cancellationToken: CancellationToken.None);
                    }
                    else
                    {
                        await chatHistory.UpdateChatAsync(
                            context.TenantId,
                            context.Chat.Id,
                            context.RawMessage,
                            context.Attachments);
                    }

                    yield return new MessageStartCompletion
                    {
                        ChatId = context.Chat.Id,
                        Error = context.Error
                    };

                    started = true;
                }

                responses.Add(response);
            }

            foreach (var content in response.Contents)
            {
                switch (content)
                {
                    case TextReasoningContent { Text.Length: > 0 } textReasoning:
                        yield return new ReasoningCompletion
                        {
                            Text = textReasoning.Text
                        };
                        break;
                    case TextContent textContent:
                        yield return new TextCompletion
                        {
                            Text = textContent.Text
                        };
                        break;
                    case FunctionCallContent functionCall:
                        yield return new ToolCallCompletion
                        {
                            CallId = functionCall.CallId,
                            Name = functionCall.Name,
                            Arguments = functionCall.Arguments,
                            Managed = functionCall.IsManaged(),
                            McpServerInfo = functionCall.GetMcpServerInfo()
                        };
                        break;
                    case FunctionResultContent functionResult:
                        yield return new ToolResultCompletion
                        {
                            CallId = functionResult.CallId,
                            Result = functionResult.Result
                        };
                        break;
                    case ErrorContent error:
                        yield return new ErrorCompletion
                        {
                            Message = error.Message,
                            ErrorCode = error.ErrorCode,
                            Details = error.Details
                        };
                        break;
                }
            }

            if (!errorCaptured)
            {
                continue;
            }

            await attachmentHandler.CleanupAsync(context.Attachments);
            break;
        }

        var chatResponse = responses.ToChatResponse();

        var messageId = await chatHistory.AddAssistantMessagesAsync(context.Chat!.Id, chatResponse.Messages);
        if (messageId > 0)
        {
            await socketManager.CommitMessageAsync(context.Chat.Id, messageId);
        }

        yield return new MessageStopCompletion
        {
            MessageId = messageId
        };

        await enumerator.DisposeAsync();
        await context.DisposeAsync();
    }
}
