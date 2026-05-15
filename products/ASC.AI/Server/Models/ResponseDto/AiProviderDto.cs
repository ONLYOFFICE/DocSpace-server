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
/// AI provider details.
/// </summary>
public class AiProviderDto
{
    /// <summary>
    /// AI provider identifier.
    /// </summary>
    /// <example>1</example>
    public int Id { get; init; }

    /// <summary>
    /// AI provider display title.
    /// </summary>
    /// <example>OpenAI</example>
    public required string Title { get; init; }

    /// <summary>
    /// AI provider type (e.g., OpenAi, Anthropic, GoogleAi).
    /// </summary>
    /// <example>0</example>
    public ProviderType Type { get; init; }

    /// <summary>
    /// API endpoint URL for the AI provider.
    /// </summary>
    /// <example>https://api.openai.com/v1</example>
    public string? Url { get; init; }

    /// <summary>
    /// Date and time when the provider was created.
    /// </summary>
    /// <example>2025-06-15T10:30:00.0000000Z</example>
    public required ApiDateTime CreatedOn { get; init; }

    /// <summary>
    /// Date and time when the provider was last modified.
    /// </summary>
    /// <example>2025-06-15T12:45:00.0000000Z</example>
    public required ApiDateTime ModifiedOn { get; init; }

    /// <summary>
    /// Indicates whether the provider's API key needs to be reset.
    /// </summary>
    /// <example>false</example>
    public bool NeedReset { get; init; }

    /// <summary>
    /// Indicates whether this provider is the default provider for the tenant.
    /// </summary>
    /// <example>true</example>
    public bool IsDefault { get; init; }
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None,
    PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public partial class ProviderMapper(ApiDateTimeHelper helper)
{
    public partial AiProviderDto MapToDto(AiProvider provider);

    private ApiDateTime MapDateTime(DateTime dateTime)
    {
        return helper.Get(dateTime);
    }
}