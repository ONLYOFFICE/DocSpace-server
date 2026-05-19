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
/// A single model settings entry within a provider create or update request.
/// </summary>
public class ModelSettingsItemDto : IEquatable<ModelSettingsItemDto>
{
    /// <summary>
    /// The model identifier.
    /// </summary>
    /// <example>claude-opus-4-1-20250805</example>
    [Required]
    [MaxLength(255)]
    public required string ModelId { get; init; }

    /// <summary>
    /// Whether the model is enabled for use in chat.
    /// </summary>
    /// <example>true</example>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// The display name for the model. Only applies to non-recommended models.
    /// </summary>
    /// <example>Claude Opus 4.1</example>
    [MaxLength(255)]
    public string? Alias { get; init; }

    /// <summary>
    /// The model capabilities. Only applies to non-recommended models.
    /// </summary>
    public AiModelCapabilities? Capabilities { get; init; }

    public bool Equals(ModelSettingsItemDto? other)
    {
        return other is not null && string.Equals(ModelId, other.ModelId, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ModelSettingsItemDto);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(ModelId);
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None,
    PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class ModelSettingsItemDtoMapper
{
    public static partial AiModelSettings Map(this ModelSettingsItemDto source);
}
