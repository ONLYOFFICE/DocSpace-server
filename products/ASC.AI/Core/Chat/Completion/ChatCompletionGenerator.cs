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


namespace ASC.AI.Core.Chat.Completion;

public class ChatCompletionGenerator(
    Guid chatId,
    ChatHistory chatHistory,
    IChatClient client,
    List<ChatMessage> messages, 
    List<AITool>? tools = null)
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };
    
    public async IAsyncEnumerable<ChatCompletion> GenerateCompletionAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ChatOptions? options = null;
        
        if (tools != null && tools.Count != 0)
        {
            options = new ChatOptions
            {
                Tools = tools, 
                ToolMode = ChatToolMode.Auto,
                AllowMultipleToolCalls = true
            };
        }
        
        var responses = new List<ChatResponseUpdate>();

        try
        {
            await foreach (var response in client.GetStreamingResponseAsync(messages, options,
                               cancellationToken: cancellationToken))
            {
                responses.Add(response);

                foreach (var content in response.Contents)
                {
                    switch (content)
                    {
                        case TextContent textContent:
                            yield return new ChatCompletion(EventType.NewToken,
                                JsonSerializer.Serialize(textContent, _serializerOptions));
                            break;
                        case FunctionCallContent functionCall:
                            yield return new ChatCompletion(EventType.ToolCall,
                                JsonSerializer.Serialize(functionCall, _serializerOptions));
                            break;
                        case FunctionResultContent functionResult:
                            yield return new ChatCompletion(EventType.ToolResult,
                                JsonSerializer.Serialize(functionResult, _serializerOptions));
                            break;
                    }
                }
            }
        }
        finally
        {
            var chatResponse = responses.ToChatResponse();
            if (chatResponse.Messages.Count > 0)
            {
                await chatHistory.AddMessagesAsync(chatId, chatResponse.Messages);
            }
        }
    }
}