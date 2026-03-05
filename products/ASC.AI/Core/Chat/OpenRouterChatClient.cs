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

#pragma warning disable SCME0001

using System.Text.Json.Nodes;

using OpenAI.Chat;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace ASC.AI.Core.Chat;

public class OpenRouterChatClient(IChatClient innerClient) : IChatClient
{
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
            var reasoningDetails = new JsonArray();

            foreach (var content in reasoningMessage.Contents)
            {
                if (content is not TextReasoningContent reasoning)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(reasoning.ProtectedData))
                {
                    reasoningDetails.Add(new JsonObject
                    {
                        ["type"] = "reasoning.encrypted",
                        ["data"] = reasoning.ProtectedData
                    });
                }
                else if (!string.IsNullOrEmpty(reasoning.Text))
                {
                    reasoningDetails.Add(new JsonObject
                    {
                        ["type"] = "reasoning.text",
                        ["text"] = reasoning.Text
                    });
                }
            }

            if (reasoningDetails.Count > 0)
            {
                List<ChatMessage> reasoningMessages = [reasoningMessage];
                var openAiChatMessage = reasoningMessages.AsOpenAIChatMessages().First();
                openAiChatMessage.Patch.Set("$.reasoning_details"u8, reasoningDetails.ToJsonString());

                reasoningMessage.RawRepresentation = openAiChatMessage;
            }
        }

        await foreach (var update in innerClient.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            if (update.RawRepresentation is StreamingChatCompletionUpdate rawUpdate)
            {
                if (rawUpdate.Patch.TryGetValue("$.choices[0].delta.reasoning_details[0].type"u8, out string? detailType))
                {
                    var reasoningContent = detailType switch
                    {
                        "reasoning.text" => ExtractTextReasoning(rawUpdate),
                        "reasoning.summary" => ExtractSummaryReasoning(rawUpdate),
                        "reasoning.encrypted" => ExtractEncryptedReasoning(rawUpdate),
                        _ => null
                    };

                    if (reasoningContent != null)
                    {
                        update.Contents = [reasoningContent];
                    }
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

    private static TextReasoningContent? ExtractTextReasoning(StreamingChatCompletionUpdate rawUpdate)
    {
        if (!rawUpdate.Patch.TryGetValue("$.choices[0].delta.reasoning_details[0].text"u8, out string? text)
            || string.IsNullOrEmpty(text))
        {
            return null;
        }

        var content = new TextReasoningContent(text);

        if (rawUpdate.Patch.TryGetValue("$.choices[0].delta.reasoning_details[0].signature"u8, out string? signature)
            && !string.IsNullOrEmpty(signature))
        {
            content.ProtectedData = signature;
        }

        return content;
    }

    private static TextReasoningContent? ExtractSummaryReasoning(StreamingChatCompletionUpdate rawUpdate)
    {
        if (!rawUpdate.Patch.TryGetValue("$.choices[0].delta.reasoning_details[0].summary"u8, out string? summary)
            || string.IsNullOrEmpty(summary))
        {
            return null;
        }

        return new TextReasoningContent(summary);
    }

    private static TextReasoningContent? ExtractEncryptedReasoning(StreamingChatCompletionUpdate rawUpdate)
    {
        if (!rawUpdate.Patch.TryGetValue("$.choices[0].delta.reasoning_details[0].data"u8, out string? data)
            || string.IsNullOrEmpty(data))
        {
            return null;
        }

        return new TextReasoningContent(string.Empty) { ProtectedData = data };
    }
}
