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
/// The AI module settings.
/// </summary>
public class AiSettingsDto
{
    /// <summary>
    /// Indicates whether document vectorization is enabled.
    /// </summary>
    /// <example>true</example>
    public bool VectorizationEnabled { get; init; }

    /// <summary>
    /// Indicates whether the embedding provider API key needs to be reconfigured.
    /// </summary>
    /// <example>false</example>
    public bool VectorizationNeedReset { get; init; }

    /// <summary>
    /// Indicates whether the AI subsystem is fully configured and operational.
    /// </summary>
    /// <example>true</example>
    public bool AiReady { get; init; }

    /// <summary>
    /// The name of the embedding model used for document vectorization.
    /// </summary>
    /// <example>text-embedding-3-small</example>
    public required string EmbeddingModel { get; init; }
    
    /// <summary>
    /// Indicates whether the system-level AI provider is enabled.
    /// </summary>
    /// <example>true</example>
    public bool SystemAiEnabled { get; init; }

    /// <summary>
    /// The identifier of the model recommended for form generation.
    /// </summary>
    /// <example>gpt-5.4</example>
    public string? RecommendedModelForForms { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None,
    PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class AiSettingsDtoMapper
{
    public static partial AiSettingsDto MapToDto(this AiSettings source);
}
