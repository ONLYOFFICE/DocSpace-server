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

namespace ASC.Files.Core.VirtualRooms;

/// <summary>
/// The room data lifetime information.
/// </summary>
public class RoomDataLifetime
{
    /// <summary>
    /// Specifies whether to delete the room data lifetime permanently or not.
    /// </summary>
    public bool DeletePermanently { get; set; }

    /// <summary>
    /// The room data lifetime period.
    /// </summary>
    public RoomDataLifetimePeriod Period { get; set; }

    /// <summary>
    /// The room data lifetime value.
    /// </summary>
    public int? Value { get; set; }

    /// <summary>
    /// The room data lifetime start date and time.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Specifies whether the room data lifetime is enabled or not.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Get the expiration of the room data lifetime in UTC format.
    /// </summary>
    public DateTime GetExpirationUtc()
    {
        var expiration = DateTime.UtcNow;

        if (Value.HasValue)
        {
            expiration = Period switch
            {
                RoomDataLifetimePeriod.Day => expiration.AddDays(-Value.Value),
                RoomDataLifetimePeriod.Month => expiration.AddMonths(-Value.Value),
                RoomDataLifetimePeriod.Year => expiration.AddYears(-Value.Value),
                _ => throw new Exception("Unknown lifetime period")
            };
        }
        else
        {
            return DateTime.MaxValue;
        }

        return expiration;
    }

    public override bool Equals(object obj)
    {
        if (obj is not RoomDataLifetime lifetime)
        {
            return false;
        }

        return DeletePermanently == lifetime.DeletePermanently && Period == lifetime.Period && Value == lifetime.Value;
    }

    protected bool Equals(RoomDataLifetime other)
    {
        return DeletePermanently == other.DeletePermanently && Period == other.Period && Value == other.Value;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DeletePermanently, (int)Period, Value);
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class RoomDataLifetimeMapper
{
    public static partial RoomDataLifetime Map(this RoomDataLifetimeDto source);
    public static partial RoomDataLifetime Map(this DbRoomDataLifetime source);
    public static partial DbRoomDataLifetime Map(this RoomDataLifetime source);
}

/// <summary>
/// The room data lifetime period.
/// </summary>
[EnumExtensions]
public enum RoomDataLifetimePeriod
{
    [Description("Day")]
    Day = 0,

    [Description("Month")]
    Month = 1,

    [Description("Year")]
    Year = 2
}