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

namespace ASC.Web.Core;

public class LogoHandler
{
    public LogoHandler(RequestDelegate _)
    {
    }

    public async Task Invoke
        (HttpContext context,
        CommonLinkUtility commonLinkUtility,
        TenantManager tenantManager,
        SettingsManager settingsManager,
        TenantWhiteLabelSettingsHelper tenantWhiteLabelSettingsHelper)
    {
        var currentTenant = tenantManager.GetCurrentTenant(false);
        if (currentTenant == null)
        {
            throw new ItemNotFoundException("tenant");
        }

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