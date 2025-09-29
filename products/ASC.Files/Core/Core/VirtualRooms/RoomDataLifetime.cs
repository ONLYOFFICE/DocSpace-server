// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
    [SwaggerEnum("Day")]
    Day = 0,

    [SwaggerEnum("Month")]
    Month = 1,

    [SwaggerEnum("Year")]
    Year = 2
}
