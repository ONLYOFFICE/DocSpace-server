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

using ConfigurationConstants = ASC.Core.Configuration.Constants;
using UserConstants = ASC.Core.Users.Constants;

namespace ASC.Data.Backup.Tasks.Modules;

[Scope]
public class Helpers(InstanceCrypto instanceCrypto)
{
    private readonly Guid[] _systemUsers =
    [
        Guid.Empty,
            ConfigurationConstants.CoreSystem.ID,
            ConfigurationConstants.Guest.ID,
            UserConstants.LostUser.Id
    ];

    private readonly Guid[] _systemGroups =
    [
        Guid.Empty,
            UserConstants.LostGroupInfo.ID,
            UserConstants.GroupAdmin.ID,
            UserConstants.GroupEveryone.ID,
            UserConstants.GroupGuest.ID,
            UserConstants.GroupRoomAdmin.ID,
            UserConstants.GroupUser.ID,
            new("{EA942538-E68E-4907-9394-035336EE0BA8}"), //community product
            new("{1e044602-43b5-4d79-82f3-fd6208a11960}"), //projects product
            new("{6743007C-6F95-4d20-8C88-A8601CE5E76D}"), //crm product
            new("{E67BE73D-F9AE-4ce1-8FEC-1880CB518CB4}"), //documents product
            new("{F4D98AFD-D336-4332-8778-3C6945C81EA0}"), //people product
            new("{2A923037-8B2D-487b-9A22-5AC0918ACF3F}"), //mail product
            new("{32D24CB5-7ECE-4606-9C94-19216BA42086}"), //calendar product
            new("{37620AE5-C40B-45ce-855A-39DD7D76A1FA}"), //birthdays product
            new("{BF88953E-3C43-4850-A3FB-B1E43AD53A3E}")  //talk product
    ];

    public bool IsEmptyOrSystemUser(string id)
    {
        return string.IsNullOrEmpty(id) || Guid.TryParse(id, out var g) && _systemUsers.Contains(g);
    }

    public bool IsEmptyOrSystemGroup(string id)
    {
        return string.IsNullOrEmpty(id) || Guid.TryParse(id, out var g) && _systemGroups.Contains(g);
    }

    public string CreateHash(string s)
    {
        return !string.IsNullOrEmpty(s) && s.StartsWith("S|") ? instanceCrypto.Encrypt(Crypto.GetV(s[2..], 1, false)) : s;
    }

    public string CreateHash2(string s)
    {
        return !string.IsNullOrEmpty(s) ? "S|" + Crypto.GetV(instanceCrypto.Decrypt(s), 1, true) : s;
    }
}