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

public class DisplayUserSettings : ISettings<DisplayUserSettings>
{
    public static Guid ID => new("2EF59652-E1A7-4814-BF71-FEB990149428");

    public bool IsDisableGettingStarted { get; set; }

    public DisplayUserSettings GetDefault()
    {
        return new DisplayUserSettings
        {
            IsDisableGettingStarted = false
        };
    }

    public DateTime LastModified { get; set; }
}

[Scope]
public class DisplayUserSettingsHelper(UserManager userManager, UserFormatter userFormatter, IConfiguration configuration)
{
    private string RemovedProfileName => configuration["web:removed-profile-name"] ?? "profile removed";

    public async Task<string> GetFullUserNameAsync(Guid userID, bool withHtmlEncode = true, bool returnLostUserIfRemoved = true)
    {
        return GetFullUserName(await userManager.GetUsersAsync(userID, returnLostUserIfRemoved), withHtmlEncode);
    }

    public string GetFullUserName(UserInfo userInfo, bool withHtmlEncode = true)
    {
        return GetFullUserName(userInfo, DisplayUserNameFormat.Default, withHtmlEncode);
    }

    public string GetFullUserName(UserInfo userInfo, DisplayUserNameFormat format, bool withHtmlEncode)
    {
        if (userInfo == null)
        {
            return string.Empty;
        }
        if (!userInfo.Id.Equals(Guid.Empty) && !userManager.UserExists(userInfo))
        {
            try
            {
                var resourceType = Type.GetType("ASC.Web.Core.PublicResources.Resource, ASC.Web.Core");
                var resourceProperty = resourceType.GetProperty("ProfileRemoved", BindingFlags.Static | BindingFlags.Public);

                var resourceValue = "";

                if (resourceProperty != null)
                {
                    resourceValue = (string)resourceProperty.GetValue(null);
                }

                return string.IsNullOrEmpty(resourceValue) ? RemovedProfileName : resourceValue;
            }
            catch (Exception)
            {
                return RemovedProfileName;
            }
        }
        var result = userFormatter.GetUserName(userInfo, format);

        if (string.IsNullOrWhiteSpace(result))
        {
            result = userInfo.Email;
        }

        return withHtmlEncode ? HtmlEncode(result) : result;
    }
    public string HtmlEncode(string str)
    {
        return !string.IsNullOrEmpty(str) ? HttpUtility.HtmlEncode(str) : str;
    }
}