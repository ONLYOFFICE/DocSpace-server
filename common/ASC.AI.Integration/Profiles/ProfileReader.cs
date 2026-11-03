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

namespace ASC.AI.Integration.Profiles;

[Scope]
public class ProfileReader(
    ProfileStorage storage,
    ByokKeyStorage byokKeyStorage,
    PortalModelsService portalModelsService,
    AiGateway aiGateway,
    BaseCommonLinkUtility linkUtility)
{
    private const string ImageModality = "image";
    private const string ToolsCapability = "tools";
    private const string ReasoningCapability = "reasoning";

    public async Task<Profile?> ReadByIdAsync(int tenantId, Guid id)
    {
        if (!aiGateway.Configured)
        {
            return await storage.ReadByIdAsync(tenantId, id);
        }

        var profiles = await ReadAllAsync(tenantId);

        return profiles.Find(p => p.Id == id);
    }

    public async Task<List<Profile>> ReadAllAsync(int tenantId, IEnumerable<Model>? models = null)
    {
        if (!aiGateway.Configured)
        {
            return await storage.ReadAllAsync(tenantId);
        }

        return await MapGatewayProfilesAsync(tenantId, models ?? await portalModelsService.ReadAsync(tenantId));
    }

    public static bool IsNotEmbedding(Model model)
    {
        return !string.Equals(model.Type, "embedding", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<Profile>> MapGatewayProfilesAsync(int tenantId, IEnumerable<Model> models)
    {
        var byokProviders = (await byokKeyStorage.ReadAllAsync(tenantId)).ToDictionary(k => k.Id, k => k.Provider);

        return
        [
            .. models
                .Where(IsNotEmbedding)
                .Select(m => MapGatewayProfile(m, byokProviders.GetValueOrDefault(m.RevisionId)))
        ];
    }

    private Profile MapGatewayProfile(Model model, string? byokProvider)
    {
        return new Profile
        {
            Id = model.RevisionId,
            Name = model.Alias,
            ProviderType = "onlyoffice",
            BaseUrl = linkUtility.GetFullAbsolutePath(string.Empty),
            ModelId = model.Id,
            Reasoning = MapReasoning(model),
            CanUseTool = HasCapability(model, ToolsCapability),
            Capabilities = MapCapabilities(model),
            UseResponsesApi = false,
            Key = "onlyoffice",
            ByokProvider = byokProvider
        };
    }

    private static ReasoningConfig MapReasoning(Model model)
    {
        var reasoning = model.Reasoning;

        return new ReasoningConfig
        {
            Thinks = reasoning is not null || HasCapability(model, ReasoningCapability),
            CanDisable = reasoning is null || !reasoning.Mandatory,
            Depths = reasoning?.SupportedEfforts?.Select(ParseDepth).OfType<ReasoningDepth>().ToArray() ?? [],
            DefaultDepth = reasoning is null
                ? null
                : reasoning.DefaultEnabled ? ParseDepth(reasoning.DefaultEffort) : ReasoningDepth.None
        };
    }

    private static ReasoningDepth? ParseDepth(string? value)
    {
        return ReasoningDepthExtensions.TryParse(value, true, out var depth) ? depth : null;
    }

    private static Capabilities MapCapabilities(Model model)
    {
        var capabilities = Capabilities.None;

        if (string.Equals(model.Type, "chat", StringComparison.OrdinalIgnoreCase))
        {
            capabilities |= Capabilities.Chat;
        }
        else if (string.Equals(model.Type, "image", StringComparison.OrdinalIgnoreCase))
        {
            capabilities |= Capabilities.Image;
        }

        if (HasModality(model.InputModalities, ImageModality))
        {
            capabilities |= Capabilities.Vision;
        }

        if (HasModality(model.OutputModalities, ImageModality))
        {
            capabilities |= Capabilities.Image;
        }

        if (HasCapability(model, ToolsCapability))
        {
            capabilities |= Capabilities.Tools;
        }

        return capabilities;
    }

    private static bool HasCapability(Model model, string capability)
    {
        return model.Capabilities?.Contains(capability, StringComparer.OrdinalIgnoreCase) == true;
    }

    private static bool HasModality(IEnumerable<string> modalities, string modality)
    {
        return modalities.Contains(modality, StringComparer.OrdinalIgnoreCase);
    }
}
