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

using OpenAI.Chat;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace ASC.AI.Core.Chat;

public class OpenRouterChatClient(IChatClient innerClient) : IChatClient
{
    private readonly List<byte[]> _reasoningDetails = [];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, 
        ChatOptions? options = null,
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
        if (lastMessage.Role == ChatRole.Tool && _reasoningDetails.Count > 0)
        {
            var reasoningMessage = originalMessages[^2];
            var arrayJson = BuildJsonArray(_reasoningDetails);

            List<ChatMessage> reasoningMessages = [reasoningMessage];
            var openAiChatMessage = reasoningMessages.AsOpenAIChatMessages().First();
            openAiChatMessage.Patch.Set("$.reasoning_details"u8, arrayJson);

            reasoningMessage.RawRepresentation = openAiChatMessage;
        }

        _reasoningDetails.Clear();

        await foreach (var update in innerClient.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            if (update.RawRepresentation is StreamingChatCompletionUpdate rawUpdate
                && rawUpdate.Patch.TryGetJson("$.choices[0].delta.reasoning_details[0]"u8, out var rawDetailJson))
            {
                _reasoningDetails.Add(rawDetailJson.ToArray());

                var detail = JsonSerializer.Deserialize<ReasoningDetail>(rawDetailJson.Span);

                TextReasoningContent? reasoningContent = null;

                switch (detail)
                {
                    case TextReasoningDetail { Text.Length: > 0 } text:
                        {
                            reasoningContent = new TextReasoningContent(text.Text);

                            if (!string.IsNullOrEmpty(text.Signature))
                            {
                                reasoningContent.ProtectedData = text.Signature;
                            }

                            break;
                        }
                    case SummaryReasoningDetail { Summary.Length: > 0 } summary:
                        reasoningContent = new TextReasoningContent(summary.Summary);
                        break;
                    case EncryptedReasoningDetail { Data.Length: > 0 } encrypted:
                        reasoningContent = new TextReasoningContent(string.Empty) { ProtectedData = encrypted.Data };
                        break;
                }

                if (reasoningContent != null)
                {
                    update.Contents = [reasoningContent];
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

    private static byte[] BuildJsonArray(List<byte[]> elements)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartArray();

        foreach (var element in elements)
        {
            writer.WriteRawValue(element);
        }

        writer.WriteEndArray();
        writer.Flush();

        return stream.ToArray();
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextReasoningDetail), "reasoning.text")]
[JsonDerivedType(typeof(SummaryReasoningDetail), "reasoning.summary")]
[JsonDerivedType(typeof(EncryptedReasoningDetail), "reasoning.encrypted")]
public abstract class ReasoningDetail;

public class TextReasoningDetail : ReasoningDetail
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}

public class SummaryReasoningDetail : ReasoningDetail
{
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

public class EncryptedReasoningDetail : ReasoningDetail
{
    [JsonPropertyName("data")]
    public string? Data { get; set; }
}
