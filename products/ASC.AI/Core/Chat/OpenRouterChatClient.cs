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

#pragma warning disable SCME0001

using OpenAI.Chat;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace ASC.AI.Core.Chat;

public class OpenRouterChatClient(IChatClient innerClient, Dictionary<string, string>? metadata = null) : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = new())
    {
        options = ConfigureOptions(options);

        return innerClient.GetResponseAsync(messages, options, cancellationToken);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = new())
    {
        options = ConfigureOptions(options);

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
            var prevAccumulator = FindAccumulator(reasoningMessage);

            if (prevAccumulator != null)
            {
                List<ChatMessage> reasoningMessages = [reasoningMessage];
                var openAiChatMessage = reasoningMessages.AsOpenAIChatMessages().First();
                openAiChatMessage.Patch.Set("$.reasoning_details"u8, prevAccumulator.Read());

                reasoningMessage.RawRepresentation = openAiChatMessage;
            }
        }

        ReasoningArrayAccumulator? accumulator = null;
        var accumulatorStored = false;

        await foreach (var update in innerClient.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            if (update.RawRepresentation is StreamingChatCompletionUpdate rawUpdate
                && rawUpdate.Patch.TryGetJson("$.choices[0].delta.reasoning_details[0]"u8, out var rawDetailJson))
            {
                accumulator ??= new ReasoningArrayAccumulator();
                accumulator.Write(rawDetailJson.Span);

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
                    if (!accumulatorStored)
                    {
                        reasoningContent.AdditionalProperties = new AdditionalPropertiesDictionary
                        {
                            [ReasoningArrayAccumulator.Key] = accumulator
                        };
                        accumulatorStored = true;
                    }

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

    private static ReasoningArrayAccumulator? FindAccumulator(ChatMessage message)
    {
        var props = message.Contents.OfType<TextReasoningContent>().FirstOrDefault()
            ?.AdditionalProperties;

        if (props?.TryGetValue(ReasoningArrayAccumulator.Key, out var value) != true
            || value is not ReasoningArrayAccumulator accumulator)
        {
            return null;
        }

        return accumulator;
    }

    private ChatOptions? ConfigureOptions(ChatOptions? options)
    {
        if (metadata == null)
        {
            return options;
        }

        options ??= new ChatOptions();
        options.RawRepresentationFactory = _ =>
        {
            var completionOptions = new ChatCompletionOptions();
            completionOptions.Patch.Set("$.metadata"u8, JsonSerializer.SerializeToUtf8Bytes(metadata));

            return completionOptions;
        };

        return options;
    }
}

internal sealed class ReasoningArrayAccumulator
{
    public const string Key = nameof(ReasoningArrayAccumulator);

    private readonly ArrayBufferWriter<byte> _buffer = new();
    private bool _finalised;

    public void Write(ReadOnlySpan<byte> rawJson)
    {
        if (_finalised)
        {
            throw new InvalidOperationException("Cannot write after the accumulator has been finalised by a call to Read().");
        }

        _buffer.Write(_buffer.WrittenCount > 0 ? ","u8 : "["u8);
        _buffer.Write(rawJson);
    }

    public ReadOnlySpan<byte> Read()
    {
        if (_finalised || _buffer.WrittenCount == 0)
        {
            return _buffer.WrittenSpan;
        }

        _buffer.Write("]"u8);
        _finalised = true;

        return _buffer.WrittenSpan;
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextReasoningDetail), "reasoning.text")]
[JsonDerivedType(typeof(SummaryReasoningDetail), "reasoning.summary")]
[JsonDerivedType(typeof(EncryptedReasoningDetail), "reasoning.encrypted")]
public abstract class ReasoningDetail;

public sealed class TextReasoningDetail : ReasoningDetail
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}

public sealed class SummaryReasoningDetail : ReasoningDetail
{
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

public sealed class EncryptedReasoningDetail : ReasoningDetail
{
    [JsonPropertyName("data")]
    public string? Data { get; set; }
}
