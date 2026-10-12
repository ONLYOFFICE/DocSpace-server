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

namespace ASC.Files.Core.ApiModels;

/// <summary>The rule by which the files of a room are removed once they have been lying in it for too long.</summary>
public class RoomDataLifetimeDto
{
    /// <summary>
    /// Decides what happens to a file that has grown too old: it is erased outright, or it is moved to the trash of
    /// the account that created the room, from where it can still be brought back.
    /// </summary>
    /// <example>false</example>
    public bool DeletePermanently { get; set; }

    /// <summary>
    /// The unit the age is counted in. Months and years are counted as calendar ones, so the same number of them
    /// covers a different number of days depending on when the clean-up runs.
    /// </summary>
    /// <example>1</example>
    [EnumDataType(typeof(RoomDataLifetimePeriod))]
    public RoomDataLifetimePeriod Period { get; set; }

    /// <summary>
    /// How many periods a file may stay in the room, counted from the moment it was last changed rather than from the
    /// moment the rule was set. Files that are already older than this are removed by the next clean-up.
    /// </summary>
    /// <example>12</example>
    [Range(1, 999)]
    public int? Value { get; set; }

    /// <summary>
    /// Switches the rule on and off. Switching it off erases the rule instead of keeping it aside, so afterwards the
    /// room reports no rule at all and the other three values have to be sent again to bring it back.
    /// </summary>
    /// <example>true</example>
    public bool? Enabled { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class RoomDataLifetimeDtoMapper
{
    public static partial RoomDataLifetimeDto MapToDto(this RoomDataLifetime source);
}