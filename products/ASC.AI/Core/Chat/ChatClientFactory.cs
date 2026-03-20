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

using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Microsoft;

namespace ASC.AI.Core.Chat;

[Scope]
public class ChatClientFactory(
    IHttpClientFactory httpClientFactory,
    IToolPermissionRequester toolPermissionRequester)
{
    public IChatClient Create(ChatClientOptions options, ToolHolder? toolHolder = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrEmpty(options.Endpoint))
        {
            throw new ArgumentException("Endpoint is not configured");
        }

        ChatClientBuilder builder;

        switch (options.Provider)
        {
            case ProviderType.Anthropic:
                {
                    // CA2000: HttpClient and AnthropicClient owned by AI library
#pragma warning disable CA2000
                    var client = new AnthropicClient(
                        new APIAuthentication(options.Key), httpClientFactory.CreateClient()).Messages;
#pragma warning restore CA2000

                    builder = client.AsBuilder()
                        .ConfigureOptions(x =>
                        {
                            x.ModelId = options.ModelId;
                            x.MaxOutputTokens = 6144;
                        });
                    break;
                }
            case ProviderType.GoogleAi:
                {
                    // CA2000: GoogleAiChatClient owned by AI library
#pragma warning disable CA2000
                    var googleAi = new GoogleAI(apiKey: options.Key, httpClientFactory: httpClientFactory);
                    var client = new GoogleAiChatClient(googleAi.GenerativeModel().AsIChatClient());
#pragma warning restore CA2000

                    builder = client.AsBuilder();
                    break;
                }
            case ProviderType.DeepSeek:
                {
                    var credential = new ApiKeyCredential(options.Key);
                    // CA2000: HttpClient owned by OpenAI client via HttpClientPipelineTransport
#pragma warning disable CA2000
                    var openAiOptions = new OpenAIClientOptions
                    {
                        Endpoint = new Uri(options.Endpoint),
                        Transport = new HttpClientPipelineTransport(httpClientFactory.CreateClient())
                    };
#pragma warning restore CA2000

                    var openAiClient = new OpenAIClient(credential, openAiOptions);
                    var chatClient = openAiClient.GetChatClient(options.ModelId);

                    // CA2000: DeepSeekChatClient wraps chat client, ownership transferred
#pragma warning disable CA2000
                    builder = new DeepSeekChatClient(chatClient.AsIChatClient()).AsBuilder()
                        .ConfigureOptions(x =>
                        {
                            x.ModelId = options.ModelId;
                        });
#pragma warning restore CA2000
                    break;
                }
            default:
                {
                    var credential = new ApiKeyCredential(options.Key);
                    // CA2000: HttpClient owned by OpenAI client via HttpClientPipelineTransport
#pragma warning disable CA2000
                    var openAiOptions = new OpenAIClientOptions
                    {
                        Endpoint = new Uri(options.Endpoint),
                        Transport = new HttpClientPipelineTransport(httpClientFactory.CreateClient())
                    };
#pragma warning restore CA2000

                    var openAiClient = new OpenAIClient(credential, openAiOptions);
                    var chatClient = openAiClient.GetChatClient(options.ModelId);

                    builder = chatClient.AsIChatClient().AsBuilder();
                    break;
                }
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
                    toolPermissionRequester);

                funcClient.MaximumIterationsPerRequest = 32;
                funcClient.AllowConcurrentInvocation = true;
                funcClient.IncludeDetailedErrors = true;
                
                return funcClient;
            });
        }

        return builder.Build();
    }
}