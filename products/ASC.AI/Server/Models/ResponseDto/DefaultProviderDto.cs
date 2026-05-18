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
/// Default AI provider information.
/// </summary>
public class DefaultProviderDto
{
    /// <summary>
    /// AI provider identifier.
    /// </summary>
    /// <example>1</example>
    public int ProviderId { get; init; }

    /// <summary>
    /// Default model identifier used with this provider.
    /// </summary>
    /// <example>gpt-4o</example>
    public required string DefaultModel { get; init; }

    /// <summary>
    /// AI provider title.
    /// </summary>
    /// <example>OpenAI</example>
    public string? ProviderTitle { get; init; }

    /// <summary>
    /// AI provider type (e.g., OpenAi, Anthropic, GoogleAi).
    /// </summary>
    /// <example>0</example>
    public ProviderType ProviderType { get; init; }

    /// <summary>
    /// Display alias of the default model.
    /// </summary>
    /// <example>GPT-4o</example>
    public string? DefaultModelAlias { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None,
    PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class DefaultProviderMapper
{
    public static partial DefaultProviderDto MapToDto(this DefaultAiProvider source);
}
