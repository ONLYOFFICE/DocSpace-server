// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.AI.Api;

[Scope]
[DefaultRoute]
[ApiController]
[AiFeature]
[ControllerName("ai")]
public class ChatController(
    ChatCompletionRunner chatCompletionRunner, 
    ChatService chatService,
    EmployeeDtoHelper employeeDtoHelper,
    ApiDateTimeHelper apiDateTimeHelper,
    ApiContext apiContext,
    MessageExporter exporter,
    McpService mcpService,
    MessageDtoConverter dtoConverter) : ControllerBase
{
    private static readonly JsonSerializerOptions _streamSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    /// <summary>
    /// Start a new AI chat
    /// </summary>
    /// <remarks>
    /// Creates a new AI chat session within the specified room and sends the initial message to the configured AI provider.
    /// The response is delivered as a Server-Sent Events (SSE) stream containing completion chunks (text deltas, tool calls, tool results, and message lifecycle events)
    /// with periodic keep-alive pings every 5 seconds. File references can be included as context for the AI model.
    /// </remarks>
    /// <path>api/2.0/ai/rooms/{roomId}/chats</path>
    [Tags("AI / Chat")]
    [SwaggerResponse(200, "SSE stream of ChatCompletion events (text/event-stream)")]
    [SwaggerResponse(400, "The message is empty or one or more file attachments could not be processed")]
    [SwaggerResponse(403, "You don't have enough permission to access the chat in this room")]
    [SwaggerResponse(404, "The specified room or AI provider was not found")]
    [HttpPost("rooms/{roomId}/chats")]
    public async Task<IActionResult> StartNewChatAsync(StartNewChatRequestDto inDto)
    {
        var generator = await chatCompletionRunner.StartNewChatAsync(
            inDto.RoomId, inDto.Body.Message, inDto.Body.Files);

        var source = generator.GenerateCompletionAsync(Request.HttpContext.RequestAborted);

        await StreamSentEventAsync(source, Request.HttpContext.RequestAborted);
        
        return Ok();
    }
    
    /// <summary>
    /// Send a message to an existing AI chat
    /// </summary>
    /// <remarks>
    /// Appends a new user message to an existing chat session and streams the AI assistant's response.
    /// The full conversation history of the chat is sent to the AI provider to maintain context.
    /// The response is delivered as a Server-Sent Events (SSE) stream with periodic keep-alive pings.
    /// File references can optionally be attached to provide additional context.
    /// </remarks>
    /// <path>api/2.0/ai/chats/{chatId}/messages</path>
    [Tags("AI / Chat")]
    [SwaggerResponse(200, "SSE stream of ChatCompletion events (text/event-stream)")]
    [SwaggerResponse(400, "The message is empty or one or more file attachments could not be processed")]
    [SwaggerResponse(403, "You don't have enough permission to access the chat in this room")]
    [SwaggerResponse(404, "The specified chat, room, or AI provider was not found")]
    [HttpPost("chats/{chatId}/messages")]
    public async Task<IActionResult> ContinueChatAsync(ContinueChatRequestDto inDto)
    {
        var generator = await chatCompletionRunner.StartChatAsync(
            inDto.ChatId, inDto.Body.Message, inDto.Body.Files);

        var source = generator.GenerateCompletionAsync(Request.HttpContext.RequestAborted);

        await StreamSentEventAsync(source, Request.HttpContext.RequestAborted);
        
        return Ok();
    }

    /// <summary>
    /// Rename an AI chat
    /// </summary>
    /// <remarks>
    /// Updates the display title of an existing AI chat session owned by the current user.
    /// The new name must not exceed 255 characters.
    /// </remarks>
    /// <path>api/2.0/ai/chats/{chatId}</path>
    [Tags("AI / Chat")]
    [SwaggerResponse(200, "Updated chat session details", typeof(ChatDto))]
    [SwaggerResponse(404, "The chat with the specified ID was not found or does not belong to the current user")]
    [HttpPut("chats/{chatId}")]
    public async Task<ChatDto> RenameChatAsync(RenameChatRequestDto inDto)
    {
        var chat = await chatService.RenameChatAsync(inDto.ChatId, inDto.Body.Name);
        
        return await chat.ToDtoAsync(employeeDtoHelper, apiDateTimeHelper);
    }

    /// <summary>
    /// Get an AI chat by ID
    /// </summary>
    /// <remarks>
    /// Retrieves the metadata of a single AI chat session, including its title, creation date, and the user who created it.
    /// Only the chat owner can access their own chat sessions.
    /// </remarks>
    /// <path>api/2.0/ai/chats/{chatId}</path>
    [Tags("AI / Chat")]
    [SwaggerResponse(200, "Chat session details", typeof(ChatDto))]
    [SwaggerResponse(404, "The chat with the specified ID was not found or does not belong to the current user")]
    [HttpGet("chats/{chatId}")]
    public async Task<ChatDto> GetChatAsync(GetChatRequestDto inDto)
    {
        var chat = await chatService.GetChatAsync(inDto.ChatId);
        return await chat.ToDtoAsync(employeeDtoHelper, apiDateTimeHelper);
    }

    /// <summary>
    /// Get AI chats in a room
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of AI chat sessions that belong to the current user within the specified room.
    /// Supports pagination via the startIndex and count query parameters. The total number of chats is included in the response metadata.
    /// </remarks>
    /// <path>api/2.0/ai/rooms/{roomId}/chats</path>
    /// <collection>list</collection>
    [Tags("AI / Chat")]
    [SwaggerResponse(200, "Paginated list of chat sessions in the room", typeof(List<ChatDto>))]
    [SwaggerResponse(403, "You don't have enough permission to access chats in this room")]
    [SwaggerResponse(404, "The room with the specified ID was not found")]
    [HttpGet("rooms/{roomId}/chats")]
    public async Task<List<ChatDto>> GetChatsAsync(GetChatsRequestDto inDto)
    {
        var totalCountTask = chatService.GetChatsTotalCountAsync(inDto.RoomId);
        
        var chats = chatService.GetChatsAsync(inDto.RoomId, inDto.StartIndex, inDto.Count);
        var chatsDto = await chats.Select(async (ChatSession x, CancellationToken _) => await x.ToDtoAsync(employeeDtoHelper, apiDateTimeHelper))
            .ToListAsync();
        
        apiContext.SetCount(chatsDto.Count).SetTotalCount(await totalCountTask);
        
        return chatsDto;       
    }
    
    /// <summary>
    /// Get messages of an AI chat
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of messages from an AI chat session owned by the current user.
    /// Each message includes its role (user or assistant), content blocks (text, tool calls, attachments), and timestamp.
    /// Supports pagination via the startIndex and count query parameters. The total number of messages is included in the response metadata.
    /// </remarks>
    /// <path>api/2.0/ai/chats/{chatId}/messages</path>
    /// <collection>list</collection>
    [Tags("AI / Chat")]
    [SwaggerResponse(200, "Paginated list of messages in the chat", typeof(List<MessageDto>))]
    [SwaggerResponse(404, "The chat with the specified ID was not found or does not belong to the current user")]
    [HttpGet("chats/{chatId}/messages")]
    public async Task<List<MessageDto>> GetMessagesAsync(GetMessagesRequestDto inDto)
    {
        var totalCountTask = chatService.GetMessagesTotalCountAsync(inDto.ChatId);
        
        var messages = chatService.GetMessagesAsync(inDto.ChatId, inDto.StartIndex, inDto.Count);
        var messagesDto = await messages.Select(async (Message x, CancellationToken _) => await dtoConverter.ConvertAsync(x)).ToListAsync();
        
        apiContext.SetCount(messagesDto.Count).SetTotalCount(await totalCountTask);
        
        return messagesDto;
    }

    /// <summary>
    /// Delete an AI chat
    /// </summary>
    /// <remarks>
    /// Permanently deletes an AI chat session along with all of its messages.
    /// Only the chat owner can delete their own chat sessions. This action cannot be undone.
    /// </remarks>
    /// <path>api/2.0/ai/chats/{chatId}</path>
    [Tags("AI / Chat")]
    [SwaggerResponse(204, "The chat was successfully deleted")]
    [SwaggerResponse(404, "The chat with the specified ID was not found or does not belong to the current user")]
    [HttpDelete("chats/{chatId}")]
    public async Task<NoContentResult> DeleteChatAsync(DeleteChatRequestDto inDto)
    {
        await chatService.DeleteChatAsync(inDto.ChatId);
        return NoContent();
    }
    
    /// <summary>
    /// Export AI chat messages to a file
    /// </summary>
    /// <remarks>
    /// Exports the entire message history of an AI chat session and saves it as a document in the specified folder.
    /// The exported file is created with the provided title. Only the chat owner can export their own chat sessions.
    /// </remarks>
    /// <path>api/2.0/ai/chats/{chatId}/messages/export</path>
    [Tags("AI / Chat")]
    [SwaggerResponse(200, "The chat messages were successfully exported to the specified folder")]
    [SwaggerResponse(404, "The chat with the specified ID was not found or does not belong to the current user")]
    [HttpPost("chats/{chatId}/messages/export")]
    public async Task ExportChatAsync(ExportChatRequestDto<int> inDto)
    {
        await exporter.ExportMessagesAsync(inDto.Body.FolderId, inDto.Body.Title, inDto.ChatId);
    }

    /// <summary>
    /// Get available AI models
    /// </summary>
    /// <remarks>
    /// Returns the list of AI models available for chat conversations.
    /// Optionally filters the results to models from a specific provider when the provider query parameter is specified.
    /// Each model entry includes the provider ID, provider display name, and the model identifier.
    /// </remarks>
    /// <path>api/2.0/ai/chats/models</path>
    /// <collection>list</collection>
    [Tags("AI / Chat")]
    [SwaggerResponse(200, "List of available AI models", typeof(IEnumerable<ModelDto>))]
    [HttpGet("chats/models")]
    public async Task<IEnumerable<ModelDto>> GetChatModelsAsync(GetChatModelsRequestDto inDto)
    {
        var models = await chatService.GetModelsAsync(inDto.ProviderId);
        return models.Select(x => x.MapToDto()).ToList();
    }

    /// <summary>
    /// Submit a tool execution permission decision
    /// </summary>
    /// <remarks>
    /// Provides the user's approval or denial decision for a pending MCP (Model Context Protocol) tool execution request.
    /// When an AI assistant attempts to invoke an external tool that requires explicit user consent,
    /// the client receives a permission prompt via the SSE stream. This endpoint is used to submit the user's decision
    /// so that the AI chat session can proceed accordingly.
    /// </remarks>
    /// <path>api/2.0/ai/chats/tool-permissions/{callId}/decision</path>
    [Tags("AI / Chat")]
    [SwaggerResponse(200, "The permission decision was successfully recorded")]
    [HttpPost("chats/tool-permissions/{callId}/decision")]
    public async Task ProvidePermissionAsync(ToolDecisionRequestDto inDto)
    {
        await mcpService.ProvideMcpToolPermissionAsync(inDto.CallId, inDto.Body.Decision);
    }

    /// <summary>
    /// Update user chat settings for a room
    /// </summary>
    /// <remarks>
    /// Saves the current user's personal AI chat preferences for the specified room.
    /// Currently supports toggling the web search capability, which allows the AI assistant to search the internet when generating responses.
    /// </remarks>
    /// <path>api/2.0/ai/rooms/{roomId}/chats/config</path>
    [Tags("AI / Chat")]
    [SwaggerResponse(200, "Updated user chat settings", typeof(UserChatSettingsDto))]
    [SwaggerResponse(403, "You don't have enough permission to access chats in this room")]
    [SwaggerResponse(404, "The room with the specified ID was not found")]
    [HttpPut("rooms/{roomId}/chats/config")]
    public async Task<UserChatSettingsDto> SetUserChatsSettingsAsync(SetUserChatsSettingsRequestDto inDto)
    {
        var settings = await chatService.SetUserChatsSettingsAsync(inDto.RoomId, inDto.Body.WebSearchEnabled);
        return settings.MapToDto();
    }
    
    /// <summary>
    /// Get user chat settings for a room
    /// </summary>
    /// <remarks>
    /// Retrieves the current user's personal AI chat preferences for the specified room,
    /// including whether web search is enabled for AI-assisted responses.
    /// </remarks>
    /// <path>api/2.0/ai/rooms/{roomId}/chats/config</path>
    [Tags("AI / Chat")]
    [SwaggerResponse(200, "Current user chat settings", typeof(UserChatSettingsDto))]
    [SwaggerResponse(403, "You don't have enough permission to access chats in this room")]
    [SwaggerResponse(404, "The room with the specified ID was not found")]
    [HttpGet("rooms/{roomId}/chats/config")]
    public async Task<UserChatSettingsDto> GetUserChatsSettingsAsync(GetUserChatsSettingsRequestDto inDto)
    {
        var settings = await chatService.GetUserChatsSettingsAsync(inDto.RoomId);
        return settings.MapToDto();
    }
    
    private async Task StreamSentEventAsync(
        IAsyncEnumerable<ChatCompletion> completions,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.KeepAlive = "keep-alive";
        
        var channel = Channel.CreateUnbounded<ChatCompletion>();
        var writer = channel.Writer;
        var reader = channel.Reader;
        
        var mainTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var content in completions.WithCancellation(cancellationToken))
                {
                    await writer.WriteAsync(content, cancellationToken);
                }
            }
            finally
            {
                writer.TryComplete();
            }
        }, cancellationToken);
        
        var pingTask = Task.Run(async () =>
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    await writer.WriteAsync(PingCompletion.Instance, cancellationToken);
                }
            }
            catch
            {
                // ignored
            }
        }, cancellationToken);
        
        await foreach (var content in reader.ReadAllAsync(cancellationToken))
        {
            if (content is PingCompletion)
            {
                await Response.WriteAsync($": ping - {DateTimeOffset.UtcNow:O}\n\n", cancellationToken: cancellationToken);
            }
            else
            {
                await Response.WriteAsync($"event: {content.GetEventName()}\n", cancellationToken: cancellationToken);
            
                await Response.WriteAsync("data: ", cancellationToken: cancellationToken);
                await JsonSerializer.SerializeAsync(Response.Body, content, _streamSerializerOptions, cancellationToken: cancellationToken);
            
                await Response.WriteAsync("\n\n", cancellationToken: cancellationToken);
            }
            
            await Response.Body.FlushAsync(cancellationToken);
        }
        
        await Task.WhenAll(mainTask, pingTask);
    }

    private class PingCompletion : ChatCompletion
    {
        public static PingCompletion Instance { get; } = new();
        
        private PingCompletion() { }
        
        public override string GetEventName()
        {
            return string.Empty;
        }
    }
}