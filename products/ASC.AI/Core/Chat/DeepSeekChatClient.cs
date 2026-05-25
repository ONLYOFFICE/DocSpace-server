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

public class DeepSeekChatClient(IChatClient innerClient) : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = new())
    {
        options = DisableThinking(options);

        return innerClient.GetResponseAsync(messages, options, cancellationToken);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = new())
    {
        if (options?.Reasoning == null || options.Reasoning.Effort == ReasoningEffort.None)
        {
            options = DisableThinking(options);

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
            openAiChatMessage.Patch.Set("$.reasoning_content"u8, reasoningContent!.Text);

            reasoningMessage.RawRepresentation = openAiChatMessage;
        }

        await foreach (var update in innerClient.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            if (update.RawRepresentation is StreamingChatCompletionUpdate rawUpdate)
            {
                if (rawUpdate.Patch.TryGetValue("$.choices[0].delta.reasoning_content"u8, out string? reasoningContent)
                    && !string.IsNullOrEmpty(reasoningContent))
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

    private static ChatOptions DisableThinking(ChatOptions? options)
    {
        options ??= new ChatOptions();
        options.RawRepresentationFactory = _ =>
        {
            var completionOptions = new ChatCompletionOptions();
            completionOptions.Patch.Set("$.thinking"u8, JsonSerializer.SerializeToUtf8Bytes(new
            {
                type = "disabled"
            }));

            return completionOptions;
        };

        return options;
    }
}
