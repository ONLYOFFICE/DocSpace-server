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

using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;

using OpenAI.Chat;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace ASC.AI.Core.Chat;

public class DeepSeekChatClient(IChatClient innerClient) : IChatClient
{
    private static readonly Func<StreamingChatCompletionUpdate, IDictionary<string, BinaryData>> _completionAdditionalDataGetter 
        = BuildCompletionDataGetter();
    
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = new())
    {
        return innerClient.GetResponseAsync(messages, options, cancellationToken);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, 
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = new())
    {
        if (options?.ModelId != "deepseek-reasoner")
        {
            await foreach (var update in innerClient.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                yield return update;
            }
            
            yield break;
        }

        List<ChatMessage>? originalMessages;

        if (messages is not List<ChatMessage> list)
        {
            originalMessages = [.. messages];
            messages = originalMessages;
        }
        else 
        {
            originalMessages = list;
        }
        
        var lastMessage = originalMessages[^1];
        if (lastMessage.Role == ChatRole.Tool)
        {
            var reasoningMessage = originalMessages[^2];
            var reasoningContent = reasoningMessage.Contents[0] as TextReasoningContent;
            List<ChatMessage> reasoningMessages = [reasoningMessage];

            var openAiChatMessage = reasoningMessages.AsOpenAIChatMessages().First();

            var node = ModelReaderWriter.Write(openAiChatMessage).ToObjectFromJson<JsonNode>()!;
            node["reasoning_content"] = reasoningContent?.Text;

            var binaryData = BinaryData.FromString(node.ToJsonString());
            var enrichedMsg = ModelReaderWriter.Read<AssistantChatMessage>(binaryData);
            
            reasoningMessage.RawRepresentation = enrichedMsg;
        }
        
        await foreach (var update in innerClient.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            if (update.RawRepresentation is StreamingChatCompletionUpdate rawUpdate)
            {
                var reasoningContent = GetReasoningContent(rawUpdate);
                if (reasoningContent != null)
                {
                    update.Contents = [new TextReasoningContent(reasoningContent)];
                }
            }
            
            yield return update;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return innerClient.GetService(serviceType, serviceKey);
    }
    
    public void Dispose()
    {
        innerClient.Dispose();
    }

    private static string? GetReasoningContent(StreamingChatCompletionUpdate update)
    {
        var additionalData = _completionAdditionalDataGetter(update);
        
        return additionalData.TryGetValue("reasoning_content", out var binaryData) 
            ? binaryData.ToObjectFromJson<string>()
            : null;
    }
    
    private static Func<StreamingChatCompletionUpdate, IDictionary<string, BinaryData>> BuildCompletionDataGetter()
    {
        var updateType = typeof(StreamingChatCompletionUpdate);
        
        var internalChoiceProp = updateType.GetProperty("InternalChoiceDelta", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (internalChoiceProp == null)
        {
            throw new InvalidOperationException("Property 'InternalChoiceDelta' not found");
        }

        var choiceType = internalChoiceProp.PropertyType;
        
        var additionalDataProp = choiceType.GetProperty("SerializedAdditionalRawData",
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (additionalDataProp == null)
        {
            throw new InvalidOperationException("Property 'SerializedAdditionalRawData' not found");
        }
        
        var param = Expression.Parameter(updateType, "update");
        
        var choiceAccess = Expression.Property(param, internalChoiceProp);
        
        var dictAccess = Expression.Property(choiceAccess, additionalDataProp);
        
        var lambda = Expression.Lambda<Func<StreamingChatCompletionUpdate, IDictionary<string, BinaryData>>>(
            dictAccess, param);
        
        return lambda.Compile();
    }
}