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

namespace ASC.AI.Api;

[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("ai")]
public class ChatController(
    ChatCompletionRunner chatCompletionRunner, 
    ChatService chatService,
    EmployeeDtoHelper employeeDtoHelper,
    ApiDateTimeHelper apiDateTimeHelper,
    ApiContext apiContext,
    IMapper mapper,
    MessageExporter exporter,
    McpService mcpService,
    McpIconStore iconStore) : ControllerBase
{
    private static readonly JsonSerializerOptions _streamSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    [HttpPost("rooms/{roomId}/chats")]
    public async Task<IActionResult> StartNewChatAsync(StartNewChatRequestDto inDto)
    {
        var generator = await chatCompletionRunner.StartNewChatAsync(
            inDto.RoomId, inDto.Body.Message, inDto.Body.Files);

        var source = generator.GenerateCompletionAsync(Request.HttpContext.RequestAborted);

        await StreamSentEventAsync(source, Request.HttpContext.RequestAborted);
        
        return Ok();
    }
    
    [HttpPost("chats/{chatId}/messages")]
    public async Task<IActionResult> ContinueChatAsync(ContinueChatRequestDto inDto)
    {
        var generator = await chatCompletionRunner.StartChatAsync(
            inDto.ChatId, inDto.Body.Message, inDto.Body.Files);

        var source = generator.GenerateCompletionAsync(Request.HttpContext.RequestAborted);

        await StreamSentEventAsync(source, Request.HttpContext.RequestAborted);
        
        return Ok();
    }

    [HttpPut("chats/{chatId}")]
    public async Task<ChatDto> RenameChatAsync(RenameChatRequestDto inDto)
    {
        var chat = await chatService.RenameChatAsync(inDto.ChatId, inDto.Body.Name);
        
        return await chat.ToDtoAsync(employeeDtoHelper, apiDateTimeHelper);
    }

    [HttpGet("chats/{chatId}")]
    public async Task<ChatDto> GetChatAsync(GetChatRequestDto inDto)
    {
        var chat = await chatService.GetChatAsync(inDto.ChatId);
        return await chat.ToDtoAsync(employeeDtoHelper, apiDateTimeHelper);
    }

    [HttpGet("rooms/{roomId}/chats")]
    public async Task<List<ChatDto>> GetChatsAsync(GetChatsRequestDto inDto)
    {
        var totalCountTask = chatService.GetChatsTotalCountAsync(inDto.RoomId);
        
        var chats = chatService.GetChatsAsync(inDto.RoomId, inDto.StartIndex, inDto.Count);
        var chatsDto = await chats.SelectAwait(async x => await x.ToDtoAsync(employeeDtoHelper, apiDateTimeHelper))
            .ToListAsync();
        
        apiContext.SetCount(chatsDto.Count).SetTotalCount(await totalCountTask);
        
        return chatsDto;       
    }
    
    [HttpGet("chats/{chatId}/messages")]
    public async Task<List<MessageDto>> GetMessagesAsync(GetMessagesRequestDto inDto)
    {
        var totalCountTask = chatService.GetMessagesTotalCountAsync(inDto.ChatId);
        
        var messages = chatService.GetMessagesAsync(inDto.ChatId, inDto.StartIndex, inDto.Count);
        var messagesDto = await messages.SelectAwait(async x => await x.ToMessageDtoAsync(
            mapper, apiDateTimeHelper, iconStore)).ToListAsync();
        
        apiContext.SetCount(messagesDto.Count).SetTotalCount(await totalCountTask);
        
        return messagesDto;
    }

    [HttpDelete("chats/{chatId}")]
    public async Task<NoContentResult> DeleteChatAsync(DeleteChatRequestDto inDto)
    {
        await chatService.DeleteChatAsync(inDto.ChatId);
        return NoContent();
    }
    
    [HttpPost("chats/{chatId}/messages/export")]
    public async Task ExportChatAsync(ExportChatRequestDto inDto)
    {
        await exporter.ExportMessagesAsync(inDto.ChatId);
    }

    [HttpGet("chats/models")]
    public async Task<IEnumerable<ModelDto>> GetChatModelsAsync(GetChatModelsRequestDto inDto)
    {
        var models = await chatService.GetModelsAsync(inDto.ProviderId);
        return mapper.Map<IEnumerable<ModelData>, IEnumerable<ModelDto>>(models);
    }

    [HttpPost("chats/tool-permissions/{callId}/decision")]
    public async Task ProvidePermissionAsync(ToolDecisionRequestDto inDto)
    {
        await mcpService.ProvideMcpToolPermissionAsync(inDto.CallId, inDto.Body.Decision);
    }

    [HttpPut("rooms/{roomId}/chats/config")]
    public async Task<UserChatSettingsDto> SetUserChatsSettingsAsync(SetUserChatsSettingsRequestDto inDto)
    {
        var settings = await chatService.SetUserChatsSettingsAsync(inDto.RoomId, inDto.Body.WebSearchEnabled);
        return mapper.Map<UserChatSettings, UserChatSettingsDto>(settings);
    }
    
    [HttpGet("rooms/{roomId}/chats/config")]
    public async Task<UserChatSettingsDto> GetUserChatsSettingsAsync(GetUserChatsSettingsRequestDto inDto)
    {
        var settings = await chatService.GetUserChatsSettingsAsync(inDto.RoomId);
        return mapper.Map<UserChatSettings, UserChatSettingsDto>(settings);
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