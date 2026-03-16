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

using HttpMethod = OpenSearch.Net.HttpMethod;

namespace ASC.ElasticSearch.Engine.Pipelines;

internal static class HybridSearchPipeline
{
    public const string Name = "vector-hybrid-rrf";

    private static readonly PipelineBody _body = new()
    {
        Description = "Post processor for hybrid RRF search",
        PhaseResultsProcessors =
        [
            new PhaseResultsProcessor
            {
                ScoreRankerProcessor = new ScoreRankerProcessor
                {
                    Combination = new ScoreCombination
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

    public static void Register(OpenSearchClient client)
    {
        ((IOpenSearchClient)client).LowLevel.DoRequest<VoidResponse>(
            HttpMethod.PUT,
            $"/_search/pipeline/{Uri.EscapeDataString(Name)}",
            PostData.String(JsonSerializer.Serialize(_body)));
    }

    private sealed class PipelineBody
    {
        [JsonPropertyName("description")]
        public required string Description { get; init; }

        [JsonPropertyName("phase_results_processors")]
        public required PhaseResultsProcessor[] PhaseResultsProcessors { get; init; }
    }

    private sealed class PhaseResultsProcessor
    {
        [JsonPropertyName("score-ranker-processor")]
        public required ScoreRankerProcessor ScoreRankerProcessor { get; init; }
    }

    private sealed class ScoreRankerProcessor
    {
        [JsonPropertyName("combination")]
        public required ScoreCombination Combination { get; init; }
    }

    private sealed class ScoreCombination
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
