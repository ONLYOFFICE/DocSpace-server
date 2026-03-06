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

#pragma warning disable OPENAI001

using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Microsoft;
using Mscc.GenerativeAI.Types;

namespace ASC.AI.Core.Chat;

[Scope]
public class ChatClientFactory(
    IHttpClientFactory httpClientFactory,
    IToolPermissionRequester toolPermissionRequester)
{
    public IChatClient Create(ChatClientOptions options, ToolHolder? toolHolder = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(options.Endpoint);

        ChatClientBuilder builder;

        switch (options.Provider)
        {
            case ProviderType.OpenAi:
                {
                    var openAiClient = CreateOpenAiClient(options);
                    var chatClient = openAiClient.GetResponsesClient(options.ModelId);
                    builder = chatClient.AsIChatClient().AsBuilder();
                    break;
                }
            case ProviderType.Anthropic:
                {
                    var client = new AnthropicClient(
                        new APIAuthentication(options.Key), httpClientFactory.CreateClient()).Messages;

                    builder = client.AsBuilder()
                        .ConfigureOptions(x =>
                        {
                            x.MaxOutputTokens = 6144;
                        });
                    break;
                }
            case ProviderType.GoogleAi:
                {
                    var googleAi = new GoogleAI(apiKey: options.Key, httpClientFactory: httpClientFactory);

                    var generationConfig = new GenerationConfig();

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

                    builder = new DeepSeekChatClient(chatClient.AsIChatClient()).AsBuilder();
                    break;
                }
            case ProviderType.OpenRouter:
                {
                    var openAiClient = CreateOpenAiClient(options);
                    var chatClient = openAiClient.GetChatClient(options.ModelId);

                    builder = new OpenRouterChatClient(chatClient.AsIChatClient()).AsBuilder();
                    break;
                }
            default:
                {
                    var openAiClient = CreateOpenAiClient(options);
                    var chatClient = openAiClient.GetChatClient(options.ModelId);

                    builder = chatClient.AsIChatClient().AsBuilder();
                    break;
                }
        }

        builder.ConfigureOptions(x => x.ModelId = options.ModelId);

        if (options.ReasoningEffort is { } reasoningEffort and not ChatReasoningEffort.None)
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
                    Output = ReasoningOutput.Full
                };
            });
        }

        if (toolHolder?.Tools is { Count: > 0 })
        {
            builder.ConfigureOptions(x =>
            {
                x.Tools = toolHolder.Tools;
                x.ToolMode = ChatToolMode.Auto;
                x.AllowMultipleToolCalls = false;
            });
            
            builder = builder.Use((innerClient, _) =>
            {
                var funcClient = new ManagedFunctionInvokingChatClient(
                    innerClient,
                    toolHolder,
                    toolPermissionRequester);

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
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(options.Endpoint),
            Transport = new HttpClientPipelineTransport(httpClientFactory.CreateClient())
        };

        return new OpenAIClient(credential, clientOptions);
    }
}