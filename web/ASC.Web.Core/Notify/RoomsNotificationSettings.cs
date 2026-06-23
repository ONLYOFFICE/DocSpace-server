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

namespace ASC.Web.Core.Notify;
public class RoomsNotificationSettings : ISettings<RoomsNotificationSettings>
{
    public List<object> DisabledRooms { get; init; }

    public static Guid ID => new("D69680EC-58DA-40D1-8CB3-424D2F402A83");

    public RoomsNotificationSettings GetDefault()
    {
        return new RoomsNotificationSettings
        {
            DisabledRooms = []
        };
    }

    public DateTime LastModified { get; set; }
}

[Scope]
public class RoomsNotificationSettingsHelper(SettingsManager settingsManager, AuthContext authContext)
{
    public async Task<IEnumerable<string>> GetDisabledRoomsForCurrentUserAsync()
    {
        var settings = await settingsManager.LoadForCurrentUserAsync<RoomsNotificationSettings>();
        return settings.DisabledRooms.Select(r => r.ToString());
    }

    public async Task<RoomsNotificationSettings> GetSettingsForCurrentUserAsync()
    {
        return await settingsManager.LoadForCurrentUserAsync<RoomsNotificationSettings>();
    }

    public Task<bool> CheckMuteForRoomAsync(object roomsId)
    {
        return CheckMuteForRoomAsync(roomsId, authContext.CurrentAccount.ID);
    }

    public async Task<bool> CheckMuteForRoomAsync(object roomsId, Guid userId)
    {
        var settings = await settingsManager.LoadAsync<RoomsNotificationSettings>(userId);
        return settings.DisabledRooms.Select(r => r.ToString()).Contains(roomsId.ToString());
    }

    public async Task<RoomsNotificationSettings> SetForCurrentUserAsync(object roomsId, bool mute)
    {
        var disabledRooms = (await GetDisabledRoomsForCurrentUserAsync()).ToList();
        var id = roomsId.ToString();

        if (disabledRooms.Contains(id))
        {
            if (!mute)
            {
                disabledRooms.Remove(id);
            }
        }
        else
        {
            if (mute)
            {
                disabledRooms.Add(id);
            }
        }

        var newSettings = new RoomsNotificationSettings { DisabledRooms = disabledRooms.Select(object (r) => r).ToList() };

        await settingsManager.SaveForCurrentUserAsync(newSettings);

        return newSettings;
    }
}