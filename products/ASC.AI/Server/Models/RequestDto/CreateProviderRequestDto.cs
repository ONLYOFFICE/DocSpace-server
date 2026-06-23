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

namespace ASC.AI.Models.RequestDto;

/// <summary>
/// Request parameters for creating a new AI provider.
/// </summary>
public class CreateProviderRequestDto
{
    /// <summary>
    /// The AI provider type (e.g., OpenAi, Anthropic, GoogleAi, DeepSeek, OpenRouter, TogetherAi, XAi, OpenAiCompatible).
    /// </summary>
    /// <example>1</example>
    public ProviderType Type { get; set; }

    /// <summary>
    /// The display title for the AI provider.
    /// </summary>
    /// <example>OpenAI Provider</example>
    public required string Title { get; set; }

    /// <summary>
    /// The API endpoint URL for the AI provider. Required for OpenAiCompatible type; optional for other types that have default URLs.
    /// </summary>
    /// <example>https://api.openai.com/v1</example>
    public string? Url { get; set; }

    /// <summary>
    /// The authentication API key for the AI provider.
    /// </summary>
    /// <example>sk-example-key-123</example>
    public required string Key { get; set; }

    /// <summary>
    /// Optional list of model settings to configure atomically with the provider creation.
    /// </summary>
    /// <example>[{"modelId": "claude-opus-4-1-20250805", "isEnabled": true, "alias": "Claude Opus 4.1", "capabilities": {"vision": true, "toolCalling": true, "thinking": false}}]</example>
    public HashSet<ModelSettingsItemDto>? ModelSettings { get; set; }
}