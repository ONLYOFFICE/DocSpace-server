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

using Action = ASC.Common.Security.Authorizing.Action;

namespace ASC.Core.Users;

[Singleton]
public sealed class Constants(IConfiguration configuration)
{
    #region system group and category groups

    public static readonly Guid SysGroupCategoryId = new("{7717039D-FBE9-45ad-81C1-68A1AA10CE1F}");

    public static readonly GroupInfo GroupEveryone = new(SysGroupCategoryId)
    {
        ID = AuthConstants.Everyone.ID,
        Name = AuthConstants.Everyone.Name
    };

    public static readonly GroupInfo GroupGuest = new(SysGroupCategoryId)
    {
        ID = AuthConstants.Guest.ID,
        Name = AuthConstants.Guest.Name
    };

    public static readonly GroupInfo GroupRoomAdmin = new(SysGroupCategoryId)
    {
        ID = AuthConstants.RoomAdmin.ID,
        Name = AuthConstants.RoomAdmin.Name
    };

    public static readonly GroupInfo GroupAdmin = new(SysGroupCategoryId)
    {
        ID = AuthConstants.DocSpaceAdmin.ID,
        Name = AuthConstants.DocSpaceAdmin.Name
    };

    public static readonly GroupInfo GroupUser = new(SysGroupCategoryId)
    {
        ID = AuthConstants.User.ID,
        Name = AuthConstants.User.Name
    };

    public static readonly GroupInfo[] SystemGroups =
    [
        GroupEveryone,
        GroupGuest,
        GroupRoomAdmin,
        GroupAdmin,
        GroupUser
    ];

    public static readonly UserInfo LostUser = new()
    {
        Id = new Guid("{4A515A15-D4D6-4b8e-828E-E0586F18F3A3}"),
        FirstName = "Unknown",
        LastName = "Unknown",
        ActivationStatus = EmployeeActivationStatus.NotActivated
    };

    public static readonly UserInfo OutsideUser = new()
    {
        Id = new Guid("{E78F4C20-2F3B-4A9D-AD13-5F298BD5A3BA}"),
        FirstName = "Outside",
        LastName = "Outside",
        ActivationStatus = EmployeeActivationStatus.Activated
    };

    public UserInfo NamingPoster { get; } = new()
    {
        Id = new Guid("{17097D73-2D1E-4B36-AA07-AEB34AF993CD}"),
        FirstName = configuration["core:system:poster:name"] ?? "ONLYOFFICE Poster",
        LastName = string.Empty,
        ActivationStatus = EmployeeActivationStatus.Activated
    };

    public static readonly GroupInfo LostGroupInfo = new()
    {
        ID = new Guid("{74B9CBD1-2412-4e79-9F36-7163583E9D3A}"),
        Name = "Unknown"
    };

    #endregion


    #region authorization rules module to work with users

    public static readonly Action Action_EditUser = new(
        new Guid("{EF5E6790-F346-4b6e-B662-722BC28CB0DB}"),
        "Edit user information");

    public static readonly Action Action_AddRemoveUser = new(
        new Guid("{D5729C6F-726F-457e-995F-DB0AF58EEE69}"),
        "Add/Remove user");

    public static readonly Action Action_EditGroups = new(
        new Guid("{1D4FEEAC-0BF3-4aa9-B096-6D6B104B79B5}"),
        "Edit categories and groups");
    public static readonly Action Action_ReadGroups = new(
        new Guid("{3E74AFF2-7C0C-4089-B209-6495B8643471}"),
        "Read categories and groups");

    #endregion
}