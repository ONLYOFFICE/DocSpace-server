// (c) Copyright Ascensio System SIA 2010-2023
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

using System.ComponentModel.DataAnnotations;

namespace ASC.Files.Core.Core.VirtualRooms;

/// <summary>
/// </summary>
public class RoomDataLifetime
{
    /// <summary>Specifies action</summary>
    /// <type>System.Boolean, System</type>
    public bool DeletePermanently { get; set; }

    /// <summary>Specifies time period type</summary>
    /// <type>ASC.Files.Core.ApiModels.RoomDataLifetimePeriod, ASC.Files.Core</type>
    public RoomDataLifetimePeriod Period { get; set; }

    /// <summary>Specifies time period value</summary>
    /// <type>System.Int32, System</type>
    [Range(1, 9999)]
    public int Value { get; set; }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static RoomDataLifetime Deserialize(string json)
    {
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<RoomDataLifetime>(json);
    }
}

public enum RoomDataLifetimePeriod
{
    Day = 0,
    Month = 1,
    Year = 2
}

public enum RoomDataLifetimeFilter
{
    All = 0,
    LifetimeEnabled = 1,
    LifetimeDisabled = 2
}