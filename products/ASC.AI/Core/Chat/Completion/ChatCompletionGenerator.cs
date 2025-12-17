// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.AI.Core.Chat.Completion;

public class ChatCompletionGenerator(
    IChatClient client,
    ILogger<ChatCompletionGenerator> logger,
    SocketManager socketManager,
    List<ChatMessage> messages,
    ChatHistory chatHistory,
    ChatNameGenerator chatNameGenerator,
    ChatExecutionContext context,
    IServiceScopeFactory serviceScopeFactory)
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
                        var tempTitle = chatNameGenerator.Generate(context.RawMessage);
                    
                        context.Chat = await chatHistory.AddChatAsync(
                            context.TenantId,
                            context.Agent.Id,
                            context.User.Id,
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

            if (errorCaptured)
            {
                break;
            }
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