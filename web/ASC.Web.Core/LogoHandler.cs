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


namespace ASC.Web.Core;

public class LogoHandler
{
    public LogoHandler(RequestDelegate _)
    {
    }

    public async Task Invoke
        (HttpContext context,
        CommonLinkUtility commonLinkUtility,
        SettingsManager settingsManager,
        TenantWhiteLabelSettingsHelper tenantWhiteLabelSettingsHelper)
    {
        string logoTypeStr = context.Request.Query["logotype"];
        if (!Enum.TryParse(logoTypeStr, out WhiteLabelLogoType logoType))
        {
            throw new ArgumentException("logotype");
        }

        var defaultStr = context.Request.Query["default"].FirstOrDefault() ?? "false";
        if (!bool.TryParse(defaultStr, out var isDefault))
        {
            throw new ArgumentException("default");
        }

        var darkStr = context.Request.Query["dark"].FirstOrDefault() ?? "false";
        if (!bool.TryParse(darkStr, out var dark))
        {
            throw new ArgumentException("dark");
        }

        string culture = context.Request.Query["culture"];

        string path;

        if (isDefault)
        {
            path = await tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(logoType, dark, culture);
        }
        else
        {
            var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();
            path = await tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, logoType, dark, culture);
        }

        context.Response.Redirect(commonLinkUtility.GetFullAbsolutePath(path));
    }
}

public static class LogoHandlerExtensions
{
    public static IApplicationBuilder UseLogoHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LogoHandler>();
    }
}