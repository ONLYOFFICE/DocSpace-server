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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// Tokens an AI operation consumed, as recorded in the operation metadata. A kind the provider did not report is `0`.
/// </summary>
public class OperationTokenUsageDto
{
    /// <summary>
    /// All tokens of the request: prompt plus completion.
    /// </summary>
    /// <example>20747</example>
    public long TotalTokens { get; init; }

    /// <summary>
    /// Tokens sent to the model, cached ones included.
    /// </summary>
    /// <example>19332</example>
    public long PromptTokens { get; init; }

    /// <summary>
    /// Tokens the model generated, reasoning ones included.
    /// </summary>
    /// <example>1415</example>
    public long CompletionTokens { get; init; }

    /// <summary>
    /// Part of the prompt tokens read from the provider cache.
    /// </summary>
    /// <example>19226</example>
    public long CachedTokens { get; init; }

    /// <summary>
    /// Part of the prompt tokens written to the provider cache.
    /// </summary>
    /// <example>104</example>
    public long CacheWriteTokens { get; init; }

    /// <summary>
    /// Part of the completion tokens the model spent on reasoning.
    /// </summary>
    /// <example>68</example>
    public long ReasoningTokens { get; init; }

    /// <summary>
    /// Tokens spent on images.
    /// </summary>
    /// <example>0</example>
    public long ImageTokens { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class OperationTokenUsageDtoMapper
{
    public static partial OperationTokenUsageDto Map(this OperationTokenUsage source);
}
