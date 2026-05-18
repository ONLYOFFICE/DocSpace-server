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
        var response = ((IOpenSearchClient)client).LowLevel.DoRequest<VoidResponse>(
            HttpMethod.PUT,
            $"/_search/pipeline/{Uri.EscapeDataString(Name)}",
            PostData.String(JsonSerializer.Serialize(_body)));

        if (response.ApiCall is { Success: true })
        {
            return;
        }

        throw new InvalidOperationException(
            $"Failed to register OpenSearch search pipeline '{Name}': {response.ApiCall?.DebugInformation}",
            response.ApiCall?.OriginalException);
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
