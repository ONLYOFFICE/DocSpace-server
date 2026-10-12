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
namespace ASC.AI.Migration;

public static class LegacyProviderMapper
{
    private const int MaxNameLength = 255;
    private const string NameSeparator = " · ";

    public static string? ToProviderType(ProviderType type)
    {
        return type switch
        {
            ProviderType.OpenAi => "openai",
            ProviderType.TogetherAi => "together",
            ProviderType.OpenAiCompatible => "openaicompatible",
            ProviderType.Anthropic => "anthropic",
            ProviderType.OpenRouter => "openrouter",
            ProviderType.DeepSeek => "deepseek",
            ProviderType.XAi => "xai",
            ProviderType.GoogleAi => "genai",
            _ => null
        };
    }

    public static ProfileData ToProfileData(DbAiProvider provider, string providerType, string modelId, DbAiModelSettings? modelSettings, string key)
    {
        var capabilities = modelSettings?.Capabilities;

        return new ProfileData
        {
            Name = BuildName(provider.Title, modelSettings?.Alias, modelId),
            ProviderType = providerType,
            BaseUrl = provider.Url,
            Key = key,
            ModelId = modelId,
            Capabilities = capabilities == null ? null : ToCapabilities(capabilities),
            Reasoning = capabilities is { Thinking: true } ? new ReasoningConfig { Thinks = true, CanDisable = true } : null,
            CanUseTool = capabilities?.ToolCalling
        };
    }

    public static Capabilities ToCapabilities(AiModelCapabilities capabilities)
    {
        var result = Capabilities.Chat;

        if (capabilities.Vision)
        {
            result |= Capabilities.Vision;
        }

        if (capabilities.ToolCalling)
        {
            result |= Capabilities.Tools;
        }

        return result;
    }

    public static string BuildName(string title, string? alias, string modelId)
    {
        var name = $"{title}{NameSeparator}{(string.IsNullOrWhiteSpace(alias) ? modelId : alias)}";

        return name.Length <= MaxNameLength ? name : name[..MaxNameLength];
    }
}
