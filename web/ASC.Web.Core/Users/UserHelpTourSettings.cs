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

namespace ASC.Web.Core.Users;

public class UserHelpTourSettings : ISettings<UserHelpTourSettings>
{
    public static Guid ID => new("{DF4B94B7-42C8-4fce-AAE2-D479F3B39BDD}");

    public Dictionary<Guid, int> ModuleHelpTour { get; set; }

    public bool IsNewUser { get; set; }

    public UserHelpTourSettings GetDefault()
    {
        return new UserHelpTourSettings
        {
            ModuleHelpTour = new Dictionary<Guid, int>(),
            IsNewUser = false
        };
    }

    public DateTime LastModified { get; set; }
}

[Scope]
public class UserHelpTourHelper(SettingsManager settingsManager)
{
    public async Task SetIsNewUser(bool value)
    {
        var settings = await settingsManager.LoadForCurrentUserAsync<UserHelpTourSettings>();
        settings.IsNewUser = value;
        await settingsManager.SaveForCurrentUserAsync(settings);
    }
}