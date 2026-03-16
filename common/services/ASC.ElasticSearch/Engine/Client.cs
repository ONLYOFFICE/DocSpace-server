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

using System.Text.Json;
using System.Text.Json.Serialization;

using HttpMethod = OpenSearch.Net.HttpMethod;

namespace ASC.ElasticSearch;

[Singleton]
public class Client(ILogger<Client> logger, Settings settings)
{
    public const string HybridSearchPipelineName = "vector-hybrid-rrf";

    private volatile OpenSearchClient _client;
    private static readonly Lock _locker = new();
    private const string AttachmentPipelineName = "attachments";
    private static readonly HybridSearchPipelineDefinition _hybridSearchPipelineBody = new()
    {
        Description = "Post processor for hybrid RRF search",
        PhaseResultsProcessors =
        [
            new PhaseResultsProcessorDefinition
            {
                ScoreRankerProcessor = new ScoreRankerProcessorDefinition
                {
                    Combination = new ScoreCombinationDefinition
                    {
                        Technique = "rrf",
                        RankConstant = 40,
                        Parameters = new ScoreCombinationParameters
                        {
                            Weights = [0.45, 0.55]
                        }
                    }
                }
            }
        ]
    };

    public OpenSearchClient Instance
    {
        get
        {
            if (_client != null)
            {
                return _client;
            }

            lock (_locker)
            {
                if (_client != null)
                {
                    return _client;
                }

                if (string.IsNullOrEmpty(settings.Scheme) || string.IsNullOrEmpty(settings.Host) || !settings.Port.HasValue)
                {
                    return null;
                }

                var uri = new Uri($"{settings.Scheme}://{settings.Host}:{settings.Port}");
                var connectionSettings = new ConnectionSettings(new SingleNodeConnectionPool(uri))
                    .TransferEncodingChunked()
                    .RequestTimeout(TimeSpan.FromMinutes(5))
                    .MaximumRetries(10)
                    .ThrowExceptions();

                if (settings.Authentication != null)
                {
                    connectionSettings.BasicAuthentication(settings.Authentication.Username, settings.Authentication.Password);
                }

                if (settings.ApiKey != null)
                {
                    connectionSettings.ApiKeyAuthentication(settings.ApiKey.Id, settings.ApiKey.Value);
                }

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    connectionSettings.DisableDirectStreaming().PrettyJson().EnableDebugMode(r =>
                    {
                        logger.Debug(r.DebugInformation);

                        if (r.RequestBodyInBytes != null)
                        {
                            logger.Debug($"Request: {Encoding.UTF8.GetString(r.RequestBodyInBytes)}");
                        }

                        if (r.HttpStatusCode is 403 or 500 && r.ResponseBodyInBytes != null)
                        {
                            logger.TraceResponse(Encoding.UTF8.GetString(r.ResponseBodyInBytes));
                        }
                    });
                }

                try
                {
                    var client = new OpenSearchClient(connectionSettings);
                    if (!Ping(client))
                    {
                        return client;
                    }

                    client.Ingest.PutPipeline(AttachmentPipelineName, p =>
                        p.Processors(pp =>
                            pp.Attachment<Attachment>(a =>
                                    a.Field("document.data")
                                        .TargetField("document.attachment")
                                        .IndexedCharacters(-1))
                                .Remove<Document>(x =>
                                    x.Field("document.data"))));
                    
                    ((IOpenSearchClient)client).LowLevel.DoRequest<VoidResponse>(
                        HttpMethod.PUT,
                        $"/_search/pipeline/{Uri.EscapeDataString(HybridSearchPipelineName)}",
                        PostData.String(JsonSerializer.Serialize(_hybridSearchPipelineBody)));

                    _client = client;

                    return client;

                }
                catch (Exception e)
                {
                    logger.ErrorClient(e);
                }

                return null;
            }
        }
    }

    public bool Ping()
    {
        return Ping(Instance);
    }

    private bool Ping(OpenSearchClient elasticClient)
    {
        if (elasticClient == null)
        {
            return false;
        }

        var result = elasticClient.Ping(new PingRequest());

        logger.DebugPing(result.DebugInformation);

        return result.IsValid;
    }

    private sealed class HybridSearchPipelineDefinition
    {
        [JsonPropertyName("description")]
        public required string Description { get; init; }

        [JsonPropertyName("phase_results_processors")]
        public required PhaseResultsProcessorDefinition[] PhaseResultsProcessors { get; init; }
    }

    private sealed class PhaseResultsProcessorDefinition
    {
        [JsonPropertyName("score-ranker-processor")]
        public required ScoreRankerProcessorDefinition ScoreRankerProcessor { get; init; }
    }

    private sealed class ScoreRankerProcessorDefinition
    {
        [JsonPropertyName("combination")]
        public required ScoreCombinationDefinition Combination { get; init; }
    }

    private sealed class ScoreCombinationDefinition
    {
        [JsonPropertyName("technique")]
        public required string Technique { get; init; }

        [JsonPropertyName("rank_constant")]
        public required int RankConstant { get; init; }

        [JsonPropertyName("parameters")]
        public required ScoreCombinationParameters Parameters { get; init; }
    }

    private sealed class ScoreCombinationParameters
    {
        [JsonPropertyName("weights")]
        public required double[] Weights { get; init; }
    }
}
