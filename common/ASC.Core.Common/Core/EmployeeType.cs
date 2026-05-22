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

namespace ASC.Core.Users;

/// <summary>
/// The user type.
/// </summary>
[Flags]
[JsonConverter(typeof(JsonStringEnumConverter<EmployeeType>))]
[EnumExtensions]
public enum EmployeeType
{
    [Description("All")]
    All = 0,

    [Description("Room admin")]
    RoomAdmin = 1,

    [Description("Guest")]
    Guest = 2,

    [Description("DocSpace admin")]
    DocSpaceAdmin = 3,

    [Description("User")]
    User = 4
}

public class EmployeeTypeComparer : IComparer<EmployeeType>
{
    private static readonly FrozenDictionary<EmployeeType, int> _priority = new Dictionary<EmployeeType, int>
    {
        { EmployeeType.DocSpaceAdmin, 4 },
        { EmployeeType.RoomAdmin, 3 },
        { EmployeeType.User, 2 },
        { EmployeeType.Guest, 1 },
        { EmployeeType.All, 0 }
    }.ToFrozenDictionary();

    private EmployeeTypeComparer() { }

    public static EmployeeTypeComparer Instance { get; } = new();

    public int Compare(EmployeeType x, EmployeeType y)
    {
        return _priority[x].CompareTo(_priority[y]);
    }
}