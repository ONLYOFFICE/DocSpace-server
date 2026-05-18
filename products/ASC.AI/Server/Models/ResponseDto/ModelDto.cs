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

namespace ASC.AI.Models.ResponseDto;

/// <summary>
/// The AI model information.
/// </summary>
public class ModelDto
{
    /// <summary>
    /// The unique identifier of the AI provider that offers this model.
    /// </summary>
    /// <example>1</example>
    public int ProviderId { get; init; }

    /// <summary>
    /// The human-readable display name of the AI provider (e.g., "OpenAI", "Anthropic").
    /// </summary>
    /// <example>OpenAI</example>
    public required string ProviderTitle { get; init; }

    /// <summary>
    /// The model identifier as recognized by the AI provider (e.g., "gpt-4o", "claude-sonnet-4-20250514").
    /// </summary>
    /// <example>gpt-4o</example>
    public required string ModelId { get; init; }

    /// <summary>
    /// The display name for the model.
    /// </summary>
    /// <example>GPT-4o</example>
    public string? Alias { get; init; }

    /// <summary>
    /// The model capabilities (vision, tool calling, thinking).
    /// </summary>
    public AiModelCapabilities? Capabilities { get; init; }

    /// <summary>
    /// The pricing information for the model (per 1M tokens). Only available for the System AI provider.
    /// </summary>
    public AiChatPrice? Price { get; init; }

    /// <summary>
    /// The currency of the price. Only available for the System AI provider.
    /// </summary>
    public CurrencyInfo? Currency { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None,
    PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class ModelDtoMapper
{
    public static partial ModelDto MapToDto(this ModelData source);
}
