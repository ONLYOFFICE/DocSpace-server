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

namespace ASC.Web.Core.Users;

public class UserInvitationSettings : ISettings<UserInvitationSettings>
{
    [JsonIgnore]
    public Guid ID => new("{F98B5FEF-BD81-46B0-B208-72120BB24115}");

    public int Limit { get; set; }

    public UserInvitationSettings GetDefault()
    {
        return new UserInvitationSettings() { Limit = -1 };
    }

    public bool IsDefault()
    {
        return Limit == -1;
    }
}

[Scope]
public class UserInvitationSettingsHelper(
    SettingsManager settingsManager,
    SetupInfo setupInfo,
    QuotaSocketManager quotaSocketManager)
{
    public bool LimitEnabled { get; internal set; } = setupInfo.InvitationLimit != int.MaxValue;

    public async Task<int> GetLimit()
    {
        if (!LimitEnabled)
        {
            return setupInfo.InvitationLimit;
        }

        return (await GetSettings()).Limit;
    }

    public async Task IncreaseLimit(int value = 1)
    {
        if (!LimitEnabled)
        {
            return;
        }

        var settings = await GetSettings();

        settings.Limit = int.Min(settings.Limit + value, setupInfo.InvitationLimit);

        _ = await settingsManager.SaveAsync(settings);

        await quotaSocketManager.ChangeInvitationLimitValue(settings.Limit);
    }

    public async Task ReduceLimit(int value = 1)
    {
        if (!LimitEnabled)
        {
            return;
        }

        var settings = await GetSettings();

        settings.Limit = int.Max(settings.Limit - value, 0);

        _ = await settingsManager.SaveAsync(settings);

        await quotaSocketManager.ChangeInvitationLimitValue(settings.Limit);
    }

    private async Task<UserInvitationSettings> GetSettings()
    {
        var settings = await settingsManager.LoadAsync<UserInvitationSettings>();

        if (settings.IsDefault())
        {
            settings.Limit = setupInfo.InvitationLimit;
        }

        return settings;
    }
}
