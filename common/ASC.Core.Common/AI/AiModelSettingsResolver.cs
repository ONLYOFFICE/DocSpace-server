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

#nullable enable

using ASC.Core.Common.EF.Model.Ai;

namespace ASC.Core.Common.AI;

public record ModelSettings
{
    public required string Id { get; init; }
    public required AiModelCapabilities Capabilities { get; init; }
    public string? Alias { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsRecommended { get; init; }
}

[Singleton]
public class AiModelSettingsResolver(AiConfiguration aiConfig)
{
    public ModelSettings Resolve(
        ProviderType type,
        string modelId,
        AiModelSettings? dbSettings,
        bool hasModelSettings,
        string? providerAlias = null,
        AiModelCapabilities? providerCapabilities = null)
    {
        var configModel = aiConfig.GetModel(type, modelId);
        if (configModel != null)
        {
            return new ModelSettings
            {
                Id = configModel.Id,
                Alias = configModel.Alias,
                Capabilities = configModel.Capabilities,
                IsEnabled =  dbSettings is null || dbSettings.IsEnabled,
                IsRecommended = true
            };
        }

        if (dbSettings is not null)
        {
            return new ModelSettings
            {
                Id = dbSettings.ModelId,
                Alias = dbSettings.Alias ?? providerAlias,
                Capabilities = dbSettings.Capabilities ?? providerCapabilities ?? AiModelCapabilities.Default,
                IsEnabled = dbSettings.IsEnabled
            };
        }

        // For legacy OpenAiCompatible providers (without model settings), models are enabled by default.
        var enabled = type == ProviderType.OpenAiCompatible && !hasModelSettings;

        return new ModelSettings
        {
            Id = modelId,
            Alias = providerAlias,
            Capabilities = providerCapabilities ?? AiModelCapabilities.Default,
            IsEnabled = enabled
        };
    }
}
