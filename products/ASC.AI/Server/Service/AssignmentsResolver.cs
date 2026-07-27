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

namespace ASC.AI.Service;

public record AssignmentPolicy
{
    public required ActionType ActionType { get; init; }
    public required string ModelType { get; init; }
    public required ModelTier[] TierOrder { get; init; }
    public Func<Model, bool>? Filter { get; init; }
}

[Scope]
public class AssignmentsResolver(AiGateway gateway, ILogger<AssignmentsResolver> logger)
{
    private static readonly AssignmentPolicy[] _policies =
    [
        new()
        {
            ActionType = ActionType.Default,
            ModelType = "chat",
            TierOrder = [ModelTier.Standard, ModelTier.Flagship, ModelTier.Light]
        },
        new()
        {
            ActionType = ActionType.ImageGeneration,
            ModelType = "image",
            TierOrder = [ModelTier.Standard, ModelTier.Light]
        }
    ];

    public async Task<Guid?> ResolveByTypeAsync(ActionType actionType, Guid? stored, bool applyDefaults)
    {
        if (!gateway.Configured)
        {
            return stored;
        }

        var models = await GetModelsAsync();
        if (models == null)
        {
            return stored;
        }

        if (stored.HasValue && models.Any(m => m.RevisionId == stored.Value))
        {
            return stored;
        }

        if (!applyDefaults)
        {
            return null;
        }

        var policy = Array.Find(_policies, p => p.ActionType == actionType);

        return policy != null ? PickDefault(policy, models) : null;
    }

    public async Task<Dictionary<ActionType, Guid>> ResolveAsync(Dictionary<ActionType, Guid> stored, bool applyDefaults)
    {
        if (!gateway.Configured)
        {
            return stored;
        }

        var models = await GetModelsAsync();
        if (models == null)
        {
            return stored;
        }

        var alive = models.Select(m => m.RevisionId).ToHashSet();

        var resolved = stored
            .Where(a => alive.Contains(a.Value))
            .ToDictionary(a => a.Key, a => a.Value);

        if (!applyDefaults)
        {
            return resolved;
        }

        foreach (var policy in _policies)
        {
            if (resolved.ContainsKey(policy.ActionType))
            {
                continue;
            }

            var substitute = PickDefault(policy, models);
            if (substitute.HasValue)
            {
                resolved[policy.ActionType] = substitute.Value;
            }
        }

        return resolved;
    }

    private async Task<List<Model>?> GetModelsAsync()
    {
        try
        {
            var response = await gateway.GetModelsAsync();
            return response?.Data?.ToList() ?? [];
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or TimeoutException or JsonException)
        {
            logger.WarningModelsUnavailable(e);
            return null;
        }
    }

    private static Guid? PickDefault(AssignmentPolicy policy, List<Model> models)
    {
        var candidates = models
            .Where(m => string.Equals(m.Type, policy.ModelType, StringComparison.OrdinalIgnoreCase)
                        && (policy.Filter == null || policy.Filter(m)))
            .ToList();

        foreach (var tier in policy.TierOrder)
        {
            var match = SelectByRank(candidates, tier);
            if (match != null)
            {
                return match.RevisionId;
            }
        }

        return SelectByRank(candidates, null)?.RevisionId;
    }

    private static Model? SelectByRank(List<Model> candidates, ModelTier? tier)
    {
        return candidates
            .Where(m => m.Tier == tier)
            .OrderBy(m => m.Rank ?? int.MaxValue)
            .ThenBy(m => m.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}

internal static partial class AssignmentsResolverLogger
{
    [LoggerMessage(LogLevel.Warning, "Failed to load models from AI Gateway, falling back to stored assignments")]
    public static partial void WarningModelsUnavailable(this ILogger<AssignmentsResolver> logger, Exception exception);
}
