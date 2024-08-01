// (c) Copyright Ascensio System SIA 2009-2024
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

using AutoMapper.Internal;

namespace ASC.Web.Core.Notify;
public class RoomsNotificationSettings : ISettings<RoomsNotificationSettings>
{
    public List<object> DisabledRooms { get; init; }

    [JsonIgnore]
    public Guid ID
    {
        get { return new Guid("D69680EC-58DA-40D1-8CB3-424D2F402A83"); }
    }

    public RoomsNotificationSettings GetDefault()
    {
        return new RoomsNotificationSettings
        {
            DisabledRooms = []
        };
    }
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
                disabledRooms.TryAdd(id);
            }
        }

        var newSettings = new RoomsNotificationSettings { DisabledRooms = disabledRooms.Select(r => (object)r).ToList() };
        
        await settingsManager.SaveForCurrentUserAsync(newSettings);

        return newSettings;
    }
}
