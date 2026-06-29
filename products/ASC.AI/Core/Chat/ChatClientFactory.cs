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

#pragma warning disable OPENAI001

using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Microsoft;
using Mscc.GenerativeAI.Types;

namespace ASC.AI.Core.Chat;

[Scope]
public class ChatClientFactory(
    IHttpClientFactory httpClientFactory,
    IToolCallReceiver toolCallReceiver,
    AiConfiguration aiConfiguration)
{
    public IChatClient Create(
        ChatClientOptions options,
        Guid userId,
        ToolHolder? toolHolder = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(options.Endpoint);

        ChatClientBuilder builder;

        switch (options.Provider)
        {
            case ProviderType.XAi:
            case ProviderType.OpenAi:
                {
                    var openAiClient = CreateOpenAiClient(options);
                    var chatClient = openAiClient.GetResponsesClient();
                    // CA2000: OpenAiResponsesClient wraps a chat client, ownership transferred
#pragma warning disable CA2000
                    builder = new OpenAiResponsesClient(chatClient.AsIChatClient()).AsBuilder();
#pragma warning restore CA2000
                    break;
                }
            case ProviderType.Anthropic:
                {
                    // CA2000: HttpClient and AnthropicClient owned by AI library
#pragma warning disable CA2000
                    var client = new AnthropicClient(
                        new APIAuthentication(options.Key), httpClientFactory.CreateClient()).Messages;
#pragma warning restore CA2000

                    builder = client.AsBuilder();
                    break;
                }
            case ProviderType.GoogleAi:
                {
                    // CA2000: GoogleAiChatClient owned by AI library
#pragma warning disable CA2000
                    var googleAi = new GoogleAI(apiKey: options.Key, httpClientFactory: httpClientFactory);

                    var generationConfig = new GenerationConfig();
#pragma warning restore CA2000

                    if (options.ReasoningEffort is { } effort and not ChatReasoningEffort.None)
                    {
                        generationConfig.ThinkingConfig = new ThinkingConfig
                        {
                            IncludeThoughts = true,
                            ThinkingLevel = effort switch
                            {
                                ChatReasoningEffort.Low => ThinkingLevel.Minimal,
                                ChatReasoningEffort.Medium => ThinkingLevel.Low,
                                ChatReasoningEffort.High => ThinkingLevel.Medium,
                                ChatReasoningEffort.XHigh => ThinkingLevel.High,
                                _ => null
                            }
                        };
                    }

                    var model = googleAi.GenerativeModel(generationConfig: generationConfig);

                    builder = model.AsIChatClient().AsBuilder();
                    break;
                }
            case ProviderType.DeepSeek:
                {
                    var openAiClient = CreateOpenAiClient(options);
                    var chatClient = openAiClient.GetChatClient(options.ModelId);
                    // CA2000: DeepSeekChatClient wraps a chat client, ownership transferred
#pragma warning disable CA2000
                    builder = new DeepSeekChatClient(chatClient.AsIChatClient()).AsBuilder();
#pragma warning restore CA2000
                    break;
                }
            case ProviderType.PortalAi:
            case ProviderType.OpenRouter:
                {
                    var openAiClient = CreateOpenAiClient(options);
                    var chatClient = openAiClient.GetChatClient(options.ModelId);
                    // CA2000: OpenRouterChatClient wraps a chat client, ownership transferred
#pragma warning disable CA2000
                    var completionClient = new OpenAiChatCompletionClient(chatClient.AsIChatClient());
                    builder = new OpenRouterChatClient(completionClient, options.Metadata).AsBuilder();
                    builder.ConfigureOptions(x =>
                    {
                        x.AdditionalProperties ??= new AdditionalPropertiesDictionary();
                        x.AdditionalProperties.TryAdd("strict", false);
                    });
#pragma warning restore CA2000
                    break;
                }
            default:
                {
                    var openAiClient = CreateOpenAiClient(options);
                    var chatClient = openAiClient.GetChatClient(options.ModelId);
                    // CA2000: OpenAiChatCompletionClient wraps a chat client, ownership transferred
#pragma warning disable CA2000
                    builder = new OpenAiChatCompletionClient(chatClient.AsIChatClient()).AsBuilder();
#pragma warning restore CA2000
                    break;
                }
        }

        builder.ConfigureOptions(x => x.ModelId = options.ModelId);

        if (options.ReasoningEffort is { } reasoningEffort)
        {
            builder.ConfigureOptions(x =>
            {
                x.Reasoning = new ReasoningOptions
                {
                    Effort = reasoningEffort switch
                    {
                        ChatReasoningEffort.None => ReasoningEffort.None,
                        ChatReasoningEffort.Low => ReasoningEffort.Low,
                        ChatReasoningEffort.Medium => ReasoningEffort.Medium,
                        ChatReasoningEffort.High => ReasoningEffort.High,
                        ChatReasoningEffort.XHigh => ReasoningEffort.ExtraHigh,
                        _ => null
                    },
                    Output = reasoningEffort is not ChatReasoningEffort.None ? ReasoningOutput.Full : ReasoningOutput.None
                };

                if (x.Reasoning is { Effort: ReasoningEffort.None } &&
                    options.ModelId.Contains("gemini", StringComparison.OrdinalIgnoreCase))
                {
                    x.Reasoning.Effort = ReasoningEffort.Low;
                }
            });
        }

        var maxOutputTokens = aiConfiguration.GetEffortSettings(
            (options.ReasoningEffort ?? ChatReasoningEffort.None).ToStringLowerFast())?.MaxOutputTokens;

        if (maxOutputTokens is > 0)
        {
            builder.ConfigureOptions(x => x.MaxOutputTokens = maxOutputTokens);
        }

        if (toolHolder?.Tools is { Count: > 0 })
        {
            builder.ConfigureOptions(x =>
            {
                x.Tools = toolHolder.Tools;
                x.ToolMode = ChatToolMode.Auto;
                x.AllowMultipleToolCalls = true;
            });

            builder = builder.Use((innerClient, _) =>
            {
                var funcClient = new ManagedFunctionInvokingChatClient(
                    innerClient,
                    toolHolder,
                    toolCallReceiver,
                    userId);

                funcClient.MaximumIterationsPerRequest = 32;
                funcClient.AllowConcurrentInvocation = true;
                funcClient.IncludeDetailedErrors = true;

                return funcClient;
            });
        }

        return builder.Build();
    }

    private OpenAIClient CreateOpenAiClient(ChatClientOptions options)
    {
        var credential = new ApiKeyCredential(options.Key);

        // CA2000: HttpClient owned by OpenAI client via HttpClientPipelineTransport
#pragma warning disable CA2000
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(options.Endpoint),
            Transport = new HttpClientPipelineTransport(httpClientFactory.CreateClient())
        };
#pragma warning restore CA2000

        return new OpenAIClient(credential, clientOptions);
    }
}
